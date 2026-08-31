using System;
using System.Security.Cryptography;
using CsNetwork.Authentication;
using Xunit;

namespace CsNetwork.Tests.Authentication;

public sealed class ChainValidationTests
{
    [Fact]
    public void ValidateChain_SelfSignedOfflineChain_Succeeds()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        string spki = JwtToken.ExportPublicKey(ecdsa);

        string headerJson = $"{{\"alg\":\"ES384\",\"x5u\":\"{spki}\"}}";
        string payloadJson = $"{{\"extraData\":{{\"displayName\":\"OfflinePlayer\",\"identity\":\"{Guid.NewGuid():D}\"}},\"identityPublicKey\":\"{spki}\"}}";

        string token = JwtToken.SignEs384(headerJson, payloadJson, ecdsa);

        bool success = MojangChainValidator.TryValidateChain(
            [token],
            out var identity,
            out var clientKey,
            out bool isXboxLiveAuthenticated,
            out string? error);

        using (clientKey)
        {
            Assert.True(success, error);
            Assert.NotNull(identity);
            Assert.NotNull(clientKey);
            Assert.Equal("OfflinePlayer", identity.DisplayName);
            Assert.False(isXboxLiveAuthenticated);
        }
    }

    [Fact]
    public void ValidateChain_Synthetic3TokenChain_Succeeds()
    {
        using var rootKey = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        using var key1 = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        using var clientKey = ECDsa.Create(ECCurve.NamedCurves.nistP384);

        string rootSpki = JwtToken.ExportPublicKey(rootKey);
        string key1Spki = JwtToken.ExportPublicKey(key1);
        string clientSpki = JwtToken.ExportPublicKey(clientKey);

        // token 0 is signed by rootKey declares key1
        string header0 = $"{{\"alg\":\"ES384\",\"x5u\":\"{rootSpki}\"}}";
        string payload0 = $"{{\"identityPublicKey\":\"{key1Spki}\"}}";
        string token0 = JwtToken.SignEs384(header0, payload0, rootKey);

        // token 1 is signed by key1, declares key1 (or intermediate)
        string header1 = "{\"alg\":\"ES384\"}";
        string payload1 = $"{{\"identityPublicKey\":\"{key1Spki}\"}}";
        string token1 = JwtToken.SignEs384(header1, payload1, key1);

        // token 2 is signed by key1, declares client extraData and clientSpki
        Guid id = Guid.NewGuid();
        string header2 = "{\"alg\":\"ES384\"}";
        string payload2 = $"{{\"extraData\":{{\"displayName\":\"OnlinePlayer\",\"identity\":\"{id:D}\",\"XUID\":\"253512345678\"}},\"identityPublicKey\":\"{clientSpki}\"}}";
        string token2 = JwtToken.SignEs384(header2, payload2, key1);

        bool success = MojangChainValidator.TryValidateChain(
            [token0, token1, token2],
            out var identity,
            out var extractedClientKey,
            out bool isXboxLiveAuthenticated,
            out string? error);

        using (extractedClientKey)
        {
            Assert.True(success, error);
            Assert.NotNull(identity);
            Assert.NotNull(extractedClientKey);
            Assert.Equal("OnlinePlayer", identity.DisplayName);
            Assert.Equal("253512345678", identity.Xuid);
            Assert.False(isXboxLiveAuthenticated);
        }
    }

    [Fact]
    public void ValidateChain_MissingOrInvalidIdentityUuid_Fails()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        string spki = JwtToken.ExportPublicKey(ecdsa);

        // invalid empty identity
        string headerJson = $"{{\"alg\":\"ES384\",\"x5u\":\"{spki}\"}}";
        string payloadJson = $"{{\"extraData\":{{\"displayName\":\"Player\",\"identity\":\"not-a-valid-guid\"}},\"identityPublicKey\":\"{spki}\"}}";

        string token = JwtToken.SignEs384(headerJson, payloadJson, ecdsa);

        bool success = MojangChainValidator.TryValidateChain(
            [token],
            out _,
            out var clientKey,
            out _,
            out string? error);

        using (clientKey)
        {
            Assert.False(success);
            Assert.Contains("identity claim must be a valid non-empty UUID", error, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void ValidateChain_ExpiredToken_Fails()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        string spki = JwtToken.ExportPublicKey(ecdsa);

        long pastExp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 1000;
        string headerJson = $"{{\"alg\":\"ES384\",\"x5u\":\"{spki}\"}}";
        string payloadJson = $"{{\"exp\":{pastExp},\"extraData\":{{\"displayName\":\"Player\",\"identity\":\"{Guid.NewGuid():D}\"}},\"identityPublicKey\":\"{spki}\"}}";

        string token = JwtToken.SignEs384(headerJson, payloadJson, ecdsa);

        bool success = MojangChainValidator.TryValidateChain(
            [token],
            out _,
            out var clientKey,
            out _,
            out string? error);

        using (clientKey)
        {
            Assert.False(success);
            Assert.Contains("Token has expired", error, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void ValidateChain_ExceedsMaxChainLength_Fails()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        string spki = JwtToken.ExportPublicKey(ecdsa);
        string headerJson = $"{{\"alg\":\"ES384\",\"x5u\":\"{spki}\"}}";
        string payloadJson = $"{{\"identityPublicKey\":\"{spki}\"}}";
        string token = JwtToken.SignEs384(headerJson, payloadJson, ecdsa);

        string[] longChain = [token, token, token, token, token, token]; // 6 tokens > MaxChainLength (5)

        bool success = MojangChainValidator.TryValidateChain(
            longChain,
            out _,
            out _,
            out _,
            out string? error);

        Assert.False(success);
        Assert.Contains("exceeds maximum allowed length", error, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidateChain_BrokenSignatureInChain_Fails()
    {
        using var rootKey = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        using var key1 = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        using var wrongKey = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        using var clientKey = ECDsa.Create(ECCurve.NamedCurves.nistP384);

        string rootSpki = JwtToken.ExportPublicKey(rootKey);
        string key1Spki = JwtToken.ExportPublicKey(key1);
        string clientSpki = JwtToken.ExportPublicKey(clientKey);

        string header0 = $"{{\"alg\":\"ES384\",\"x5u\":\"{rootSpki}\"}}";
        string payload0 = $"{{\"identityPublicKey\":\"{key1Spki}\"}}";
        string token0 = JwtToken.SignEs384(header0, payload0, rootKey);

        // token 1 signed by wrongKey instead of key1
        string header1 = "{\"alg\":\"ES384\"}";
        string payload1 = $"{{\"identityPublicKey\":\"{key1Spki}\"}}";
        string token1 = JwtToken.SignEs384(header1, payload1, wrongKey);

        string header2 = "{\"alg\":\"ES384\"}";
        string payload2 = $"{{\"extraData\":{{\"displayName\":\"Player\",\"identity\":\"{Guid.NewGuid():D}\"}},\"identityPublicKey\":\"{clientSpki}\"}}";
        string token2 = JwtToken.SignEs384(header2, payload2, key1);

        bool success = MojangChainValidator.TryValidateChain(
            [token0, token1, token2],
            out _,
            out var extractedClientKey,
            out _,
            out string? error);

        using (extractedClientKey)
        {
            Assert.False(success);
            Assert.Contains("Token 1 signature verification failed", error, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void ValidateChain_EmptyChain_Fails()
    {
        bool success = MojangChainValidator.TryValidateChain(
            [],
            out _,
            out _,
            out _,
            out string? error);

        Assert.False(success);
        Assert.Contains("Certificate chain is empty", error, StringComparison.Ordinal);
    }
}
