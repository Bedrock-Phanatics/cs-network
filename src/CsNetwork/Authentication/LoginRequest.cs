using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CsNetwork.Cryptography;

namespace CsNetwork.Authentication;

public static class LoginRequest
{
    public const int MinClientDataLength = 10;

    public static bool TryParse(
        ReadOnlySpan<byte> connectionRequestPayload,
        [NotNullWhen(true)] out LoginRequestResult? result,
        [NotNullWhen(false)] out string? error)
    {
        result = null;
        error = null;

        if (connectionRequestPayload.Length < 8)
        {
            error = "Connection request payload is too short.";
            return false;
        }

        try
        {
            int pos = 0;
            int chainLength = BinaryPrimitives.ReadInt32LittleEndian(connectionRequestPayload[pos..]);
            pos += 4;

            if (chainLength <= 0 || (uint)chainLength > (uint)(connectionRequestPayload.Length - pos - 4))
            {
                error = $"Invalid certificate chain length: {chainLength}.";
                return false;
            }

            ReadOnlySpan<byte> chainBytes = connectionRequestPayload.Slice(pos, chainLength);
            pos += chainLength;

            int clientDataLength = BinaryPrimitives.ReadInt32LittleEndian(connectionRequestPayload[pos..]);
            pos += 4;

            if (clientDataLength < MinClientDataLength || (uint)clientDataLength > (uint)(connectionRequestPayload.Length - pos))
            {
                error = $"Invalid client data length: {clientDataLength}.";
                return false;
            }

            ReadOnlySpan<byte> clientDataBytes = connectionRequestPayload.Slice(pos, clientDataLength);
            string clientDataJwt = Encoding.UTF8.GetString(clientDataBytes);

            if (!TryParseChainTokens(chainBytes, out var chainTokens, out error))
                return false;

            ECDsa? clientPublicKey = null;
            try
            {
                if (!MojangChainValidator.TryValidateChain(chainTokens, out var identity, out clientPublicKey, out bool isXboxLiveAuthenticated, out error))
                    return false;

                // verify clientData JWT signature using client public key
                if (!JwtToken.TryVerifyEs384(clientDataJwt, clientPublicKey, out string? clientDataJson))
                {
                    error = "Failed to verify ClientData JWT signature using client public key.";
                    return false;
                }

                if (!ClientData.TryParse(clientDataJson, out var clientData, out error))
                    return false;

                byte[] uncompressedKey = ExportUncompressedKey(clientPublicKey);

                result = new LoginRequestResult(identity, clientData, uncompressedKey, isXboxLiveAuthenticated);
                return true;
            }
            finally
            {
                clientPublicKey?.Dispose();
            }
        }
        catch (Exception ex)
        {
            error = $"Failed to parse login request: {ex.Message}";
            return false;
        }
    }

    public static byte[] CreateOffline(
        string displayName,
        Guid identity,
        EcdhKeyPair keyPair,
        SkinData skin,
        string gameVersion = "1.21.60",
        DeviceOS deviceOS = DeviceOS.Win10,
        string serverAddress = "127.0.0.1:19132")
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        string spkiB64 = JwtToken.ExportPublicKey(ecdsa);

        Span<byte> randomBytes = stackalloc byte[8];
        RandomNumberGenerator.Fill(randomBytes);
        long clientRandomId = BinaryPrimitives.ReadInt64LittleEndian(randomBytes) & long.MaxValue;

        var identityData = new IdentityData(displayName, identity);
        var clientData = new ClientData(
            deviceOS,
            "CsNetwork Client",
            Guid.NewGuid().ToString("N"),
            gameVersion,
            0,
            0,
            clientRandomId,
            serverAddress,
            "en_US",
            skin);

        string escapedDisplayName = JsonSerializer.Serialize(displayName);
        string headerJson = $"{{\"alg\":\"ES384\",\"x5u\":\"{spkiB64}\"}}";
        string payloadJson = $"{{\"extraData\":{{\"displayName\":{escapedDisplayName},\"identity\":\"{identity:D}\"}},\"identityPublicKey\":\"{spkiB64}\"}}";
        string chainToken = JwtToken.SignEs384(headerJson, payloadJson, ecdsa);

        string clientDataJwt = JwtToken.SignEs384(headerJson, clientData.ToJson(), ecdsa);

        string chainJson = $"{{\"chain\":[\"{chainToken}\"]}}";
        byte[] chainJsonBytes = Encoding.UTF8.GetBytes(chainJson);
        byte[] clientDataJwtBytes = Encoding.UTF8.GetBytes(clientDataJwt);

        byte[] payload = new byte[4 + chainJsonBytes.Length + 4 + clientDataJwtBytes.Length];
        BinaryPrimitives.WriteInt32LittleEndian(payload, chainJsonBytes.Length);
        chainJsonBytes.CopyTo(payload.AsSpan(4));

        int clientOffset = 4 + chainJsonBytes.Length;
        BinaryPrimitives.WriteInt32LittleEndian(payload.AsSpan(clientOffset), clientDataJwtBytes.Length);
        clientDataJwtBytes.CopyTo(payload.AsSpan(clientOffset + 4));

        return payload;
    }

    private static bool TryParseChainTokens(
        ReadOnlySpan<byte> chainBytes,
        [NotNullWhen(true)] out List<string>? tokens,
        [NotNullWhen(false)] out string? error)
    {
        tokens = null;
        error = null;

        var jsonReader = new Utf8JsonReader(chainBytes);
        using var doc = JsonDocument.ParseValue(ref jsonReader);
        var root = doc.RootElement;

        // check and reject guest authentication type (AuthenticationType == 1)
        if (root.TryGetProperty("AuthenticationType", out var authTypeProp) && authTypeProp.GetInt32() == 1)
        {
            error = "Guest authentication mode (AuthenticationType 1) is not supported.";
            return false;
        }

        JsonElement chainElement = default;
        if (root.TryGetProperty("chain", out var directChain) && directChain.ValueKind == JsonValueKind.Array)
        {
            chainElement = directChain;
        }
        else if (root.TryGetProperty("Certificate", out var certProp))
        {
            if (certProp.ValueKind == JsonValueKind.Object && certProp.TryGetProperty("chain", out var nestedChain))
            {
                chainElement = nestedChain;
            }
            else if (certProp.ValueKind == JsonValueKind.String && certProp.GetString() is { } certStr)
            {
                using var innerDoc = JsonDocument.Parse(certStr);
                if (innerDoc.RootElement.TryGetProperty("chain", out var innerChain))
                {
                    tokens = [];
                    foreach (var elem in innerChain.EnumerateArray())
                    {
                        if (elem.GetString() is { } token)
                            tokens.Add(token);
                    }
                    return tokens.Count > 0;
                }
            }
        }

        if (chainElement.ValueKind != JsonValueKind.Array)
        {
            error = "Failed to find valid 'chain' array in certificate payload.";
            return false;
        }

        tokens = [];
        foreach (var elem in chainElement.EnumerateArray())
        {
            if (elem.GetString() is { } token)
                tokens.Add(token);
        }

        if (tokens.Count == 0)
        {
            error = "'chain' array is empty.";
            return false;
        }

        return true;
    }

    private static byte[] ExportUncompressedKey(ECDsa ecdsa)
    {
        var ecParams = ecdsa.ExportParameters(false);
        if (ecParams.Q.X == null || ecParams.Q.Y == null)
            throw new InvalidOperationException("Failed to export public key coordinates.");

        byte[] uncompressed = new byte[97];
        uncompressed[0] = 0x04;

        int xOffset = 1 + (48 - ecParams.Q.X.Length);
        ecParams.Q.X.CopyTo(uncompressed, xOffset);

        int yOffset = 49 + (48 - ecParams.Q.Y.Length);
        ecParams.Q.Y.CopyTo(uncompressed, yOffset);

        return uncompressed;
    }
}

public sealed record LoginRequestResult(
    IdentityData Identity,
    ClientData Client,
    byte[] ClientPublicKeyUncompressed,
    bool IsXboxLiveAuthenticated);
