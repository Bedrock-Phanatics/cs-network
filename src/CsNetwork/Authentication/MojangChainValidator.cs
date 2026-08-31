using System;
using System.Buffers;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CsNetwork.Authentication;

public static class MojangChainValidator
{
    public const int MaxChainLength = 5;

    public static bool TryValidateChain(
        IReadOnlyList<string> chainJwtTokens,
        [NotNullWhen(true)] out IdentityData? identity,
        [NotNullWhen(true)] out ECDsa? clientPublicKey,
        out bool isXboxLiveAuthenticated,
        [NotNullWhen(false)] out string? error)
    {
        identity = null;
        clientPublicKey = null;
        isXboxLiveAuthenticated = false;
        error = null;

        if (chainJwtTokens == null || chainJwtTokens.Count == 0)
        {
            error = "Certificate chain is empty.";
            return false;
        }

        if (chainJwtTokens.Count != 1 && chainJwtTokens.Count != 3)
        {
            error = $"Unsupported certificate chain length {chainJwtTokens.Count}. Expected 1 or 3.";
            return false;
        }

        if (chainJwtTokens.Count > MaxChainLength)
        {
            error = $"Certificate chain exceeds maximum allowed length of {MaxChainLength} tokens.";
            return false;
        }

        try
        {
            if (chainJwtTokens.Count == 1)
            {
                // single token: self-signed offline login
                return ValidateOfflineToken(chainJwtTokens[0], out identity, out clientPublicKey, out error);
            }

            return ValidateOnlineChain(chainJwtTokens, out identity, out clientPublicKey, out isXboxLiveAuthenticated, out error);
        }
        catch (Exception ex)
        {
            error = $"Chain validation failed: {ex.Message}";
            return false;
        }
    }

    private static bool ValidateOfflineToken(
        string token,
        [NotNullWhen(true)] out IdentityData? identity,
        [NotNullWhen(true)] out ECDsa? clientPublicKey,
        [NotNullWhen(false)] out string? error)
    {
        identity = null;
        clientPublicKey = null;
        error = null;

        if (!ExtractHeaderX5u(token, out string? x5uBase64, out error))
            return false;

        ECDsa? key = null;
        try
        {
            key = JwtToken.ImportPublicKey(x5uBase64);
            if (!JwtToken.TryVerifyEs384(token, key, out string? payloadJson))
            {
                error = "Failed to verify offline token signature.";
                return false;
            }

            using var doc = JsonDocument.Parse(payloadJson);
            var root = doc.RootElement;

            if (!ValidateTokenClaims(root, out error))
                return false;

            if (root.TryGetProperty("identityPublicKey", out var idKeyProp))
            {
                string? idKeyB64 = idKeyProp.GetString();
                if (!string.IsNullOrEmpty(idKeyB64) && idKeyB64 != x5uBase64)
                {
                    key.Dispose();
                    key = JwtToken.ImportPublicKey(idKeyB64);
                }
            }

            if (root.TryGetProperty("extraData", out var extraDataProp))
            {
                if (!TryParseIdentityData(extraDataProp, out identity, out error))
                    return false;
            }
            else
            {
                string displayName = root.TryGetProperty("displayName", out var dn) ? dn.GetString() ?? "" : "";
                if (!root.TryGetProperty("identity", out var idProp) ||
                    !Guid.TryParse(idProp.GetString(), out var g) ||
                    g == Guid.Empty)
                {
                    error = "Offline token missing valid 'identity' UUID claim.";
                    return false;
                }
                identity = new IdentityData(displayName, g);
            }

            if (!identity.Validate(out error))
                return false;

            clientPublicKey = key;
            key = null;
            return true;
        }
        finally
        {
            key?.Dispose();
        }
    }

    private static bool ValidateOnlineChain(
        IReadOnlyList<string> tokens,
        [NotNullWhen(true)] out IdentityData? identity,
        [NotNullWhen(true)] out ECDsa? clientPublicKey,
        out bool isXboxLiveAuthenticated,
        [NotNullWhen(false)] out string? error)
    {
        identity = null;
        clientPublicKey = null;
        isXboxLiveAuthenticated = false;
        error = null;

        if (tokens.Count != 3)
        {
            error = $"Unsupported online certificate chain length {tokens.Count}. Expected 3 tokens.";
            return false;
        }

        // token 0
        if (!ExtractHeaderX5u(tokens[0], out string? rootX5uB64, out error))
            return false;

        bool isRootKey = MojangKeys.IsMojangRootKey(rootX5uB64);

        using var rootKey = JwtToken.ImportPublicKey(rootX5uB64);
        if (!JwtToken.TryVerifyEs384(tokens[0], rootKey, out string? payload0Json))
        {
            error = "Token 0 signature verification failed.";
            return false;
        }

        using var doc0 = JsonDocument.Parse(payload0Json);
        if (!ValidateTokenClaims(doc0.RootElement, out error))
            return false;

        if (!ExtractIdentityPublicKey(payload0Json, out string? key1SpkiB64, out error))
            return false;

        // token 1
        using var key1 = JwtToken.ImportPublicKey(key1SpkiB64);
        if (!JwtToken.TryVerifyEs384(tokens[1], key1, out string? payload1Json))
        {
            error = "Token 1 signature verification failed.";
            return false;
        }

        using var doc1 = JsonDocument.Parse(payload1Json);
        if (!ValidateTokenClaims(doc1.RootElement, out error))
            return false;

        if (!ExtractIdentityPublicKey(payload1Json, out string? key2SpkiB64, out error))
            return false;

        // token 2
        using var key2 = JwtToken.ImportPublicKey(key2SpkiB64);
        if (!JwtToken.TryVerifyEs384(tokens[2], key2, out string? payload2Json))
        {
            error = "Token 2 signature verification failed.";
            return false;
        }

        using var doc2 = JsonDocument.Parse(payload2Json);
        if (!ValidateTokenClaims(doc2.RootElement, out error))
            return false;

        if (!TryExtractIdentityAndClientKey(payload2Json, out identity, out clientPublicKey, out error))
            return false;

        // only set on final success
        isXboxLiveAuthenticated = isRootKey;
        return true;
    }

    private static bool ValidateTokenClaims(JsonElement root, [NotNullWhen(false)] out string? error)
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        const long clockSkewSeconds = 300; // 5 minutes

        if (root.TryGetProperty("exp", out var expProp) && expProp.TryGetInt64(out long exp))
        {
            if (now > exp + clockSkewSeconds)
            {
                error = $"Token has expired (exp: {exp}, now: {now}).";
                return false;
            }
        }

        if (root.TryGetProperty("nbf", out var nbfProp) && nbfProp.TryGetInt64(out long nbf))
        {
            if (now < nbf - clockSkewSeconds)
            {
                error = $"Token is not yet valid (nbf: {nbf}, now: {now}).";
                return false;
            }
        }

        error = null;
        return true;
    }

    private static bool TryExtractIdentityAndClientKey(
        string payloadJson,
        [NotNullWhen(true)] out IdentityData? identity,
        [NotNullWhen(true)] out ECDsa? clientPublicKey,
        [NotNullWhen(false)] out string? error)
    {
        identity = null;
        clientPublicKey = null;
        error = null;

        using var doc = JsonDocument.Parse(payloadJson);
        var root = doc.RootElement;

        if (!root.TryGetProperty("extraData", out var extraDataProp))
        {
            error = "Leaf token missing 'extraData' claim.";
            return false;
        }

        if (!TryParseIdentityData(extraDataProp, out identity, out error))
            return false;

        if (!identity.Validate(out error))
            return false;

        if (!root.TryGetProperty("identityPublicKey", out var clientKeyProp) || clientKeyProp.GetString() is not { } clientKeyB64)
        {
            error = "Leaf token missing 'identityPublicKey' claim.";
            return false;
        }

        clientPublicKey = JwtToken.ImportPublicKey(clientKeyB64);
        return true;
    }

    private static bool ExtractHeaderX5u(string jwt, [NotNullWhen(true)] out string? x5uBase64, [NotNullWhen(false)] out string? error)
    {
        x5uBase64 = null;
        error = null;

        if (!JwtToken.TryParse(jwt, out var headerSpan, out _, out _))
        {
            error = "Failed to parse JWT dot-separated parts.";
            return false;
        }

        int maxBytes = Base64Url.GetMaxDecodedLength(headerSpan.Length);
        Span<byte> headerBytes = maxBytes <= 1024 ? stackalloc byte[maxBytes] : new byte[maxBytes];

        if (!Base64Url.TryDecodeFromChars(headerSpan, headerBytes, out int written))
        {
            error = "Failed to decode JWT header Base64Url.";
            return false;
        }

        var jsonReader = new Utf8JsonReader(headerBytes[..written]);
        using var doc = JsonDocument.ParseValue(ref jsonReader);
        if (doc.RootElement.TryGetProperty("x5u", out var x5uProp) && x5uProp.GetString() is { } x5u)
        {
            x5uBase64 = x5u;
            return true;
        }

        error = "JWT header missing 'x5u' public key claim.";
        return false;
    }

    private static bool ExtractIdentityPublicKey(string payloadJson, [NotNullWhen(true)] out string? identityPublicKey, [NotNullWhen(false)] out string? error)
    {
        identityPublicKey = null;
        error = null;

        using var doc = JsonDocument.Parse(payloadJson);
        if (doc.RootElement.TryGetProperty("identityPublicKey", out var prop) && prop.GetString() is { } keyB64)
        {
            identityPublicKey = keyB64;
            return true;
        }

        error = "Payload missing 'identityPublicKey' claim.";
        return false;
    }

    private static bool TryParseIdentityData(JsonElement extraData, [NotNullWhen(true)] out IdentityData? identity, [NotNullWhen(false)] out string? error)
    {
        identity = null;
        error = null;

        string displayName = extraData.TryGetProperty("displayName", out var dn) ? dn.GetString() ?? "" : "";
        string identityStr = extraData.TryGetProperty("identity", out var id) ? id.GetString() ?? "" : "";
        string? xuid = extraData.TryGetProperty("XUID", out var x) ? x.GetString() : null;
        string? titleId = extraData.TryGetProperty("titleId", out var t) ? t.GetString() : null;
        string? playFabId = extraData.TryGetProperty("playFabId", out var pf) ? pf.GetString() : null;

        if (!Guid.TryParse(identityStr, out var guid) || guid == Guid.Empty)
        {
            error = $"identity claim must be a valid non-empty UUID, got '{identityStr}'.";
            return false;
        }

        identity = new IdentityData(displayName, guid, xuid, titleId, playFabId);
        return true;
    }
}
