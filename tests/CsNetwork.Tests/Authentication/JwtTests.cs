using System;
using System.Security.Cryptography;
using System.Text;
using CsNetwork.Authentication;
using Xunit;

namespace CsNetwork.Tests.Authentication;

public sealed class JwtTests
{
    [Fact]
    public void SignAndVerify_ValidEs384_Succeeds()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        string spki = JwtToken.ExportPublicKey(ecdsa);

        string headerJson = $"{{\"alg\":\"ES384\",\"x5u\":\"{spki}\"}}";
        string payloadJson = "{\"displayName\":\"Steve\",\"extra\":12345}";

        string jwt = JwtToken.SignEs384(headerJson, payloadJson, ecdsa);
        Assert.NotNull(jwt);

        using var importedKey = JwtToken.ImportPublicKey(spki);
        bool verified = JwtToken.TryVerifyEs384(jwt, importedKey, out string? verifiedPayload);

        Assert.True(verified);
        Assert.Equal(payloadJson, verifiedPayload);
    }

    [Fact]
    public void ImportPublicKey_NonP384Curve_ThrowsCryptographicException()
    {
        // P-256 key must be rejected
        using var p256 = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        string p256Spki = Convert.ToBase64String(p256.ExportSubjectPublicKeyInfo());

        Assert.ThrowsAny<CryptographicException>(() => JwtToken.ImportPublicKey(p256Spki));

        // P-521 key must be rejected
        using var p521 = ECDsa.Create(ECCurve.NamedCurves.nistP521);
        string p521Spki = Convert.ToBase64String(p521.ExportSubjectPublicKeyInfo());

        Assert.ThrowsAny<CryptographicException>(() => JwtToken.ImportPublicKey(p521Spki));
    }

    [Fact]
    public void Verify_TamperedPayload_Fails()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        string spki = JwtToken.ExportPublicKey(ecdsa);

        string headerJson = $"{{\"alg\":\"ES384\",\"x5u\":\"{spki}\"}}";
        string payloadJson = "{\"displayName\":\"Steve\"}";

        string jwt = JwtToken.SignEs384(headerJson, payloadJson, ecdsa);

        string[] parts = jwt.Split('.');
        string tamperedPayload = parts[1] + "extra";
        string tamperedJwt = $"{parts[0]}.{tamperedPayload}.{parts[2]}";

        bool verified = JwtToken.TryVerifyEs384(tamperedJwt, ecdsa, out _);
        Assert.False(verified);
    }

    [Fact]
    public void Verify_WrongKey_Fails()
    {
        using var key1 = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        using var key2 = ECDsa.Create(ECCurve.NamedCurves.nistP384);

        string jwt = JwtToken.SignEs384("{\"alg\":\"ES384\"}", "{\"test\":true}", key1);
        bool verified = JwtToken.TryVerifyEs384(jwt, key2, out _);

        Assert.False(verified);
    }

    [Fact]
    public void TryParse_MalformedTokens_Fails()
    {
        Assert.False(JwtToken.TryParse("", out _, out _, out _));
        Assert.False(JwtToken.TryParse("abc", out _, out _, out _));
        Assert.False(JwtToken.TryParse("abc.def", out _, out _, out _));
        Assert.False(JwtToken.TryParse("abc.def.", out _, out _, out _));
        Assert.False(JwtToken.TryParse(".def.ghi", out _, out _, out _));
    }
}
