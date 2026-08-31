using System;
using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using CsNetwork.Authentication;
using CsNetwork.Cryptography;

namespace CsNetwork.Benchmarks;

[MemoryDiagnoser]
public class AuthBenchmarks
{
    private ECDsa _ecdsaKey = null!;
    private string _jwtToken = null!;
    private byte[] _offlineLoginPayload = null!;

    [GlobalSetup]
    public void Setup()
    {
        _ecdsaKey = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        string spki = JwtToken.ExportPublicKey(_ecdsaKey);

        string headerJson = $"{{\"alg\":\"ES384\",\"x5u\":\"{spki}\"}}";
        string payloadJson = "{\"extraData\":{\"displayName\":\"BenchmarkPlayer\",\"identity\":\"00000000-0000-0000-0000-000000000001\"}}";
        _jwtToken = JwtToken.SignEs384(headerJson, payloadJson, _ecdsaKey);

        using var ecdh = EcdhKeyPair.Create();
        byte[] skinImage = new byte[SkinData.Skin64x64Length];
        var skin = new SkinData("BenchmarkSkin", skinImage, 64, 64);
        _offlineLoginPayload = LoginRequest.CreateOffline(
            "BenchmarkPlayer",
            Guid.NewGuid(),
            ecdh,
            skin);

        if (!JwtToken.TryVerifyEs384(_jwtToken, _ecdsaKey, out _))
            throw new InvalidOperationException("Failed to verify benchmark setup JWT token.");

        if (!LoginRequest.TryParse(_offlineLoginPayload, out _, out string? err))
            throw new InvalidOperationException($"Failed to parse benchmark setup login request: {err}");
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _ecdsaKey.Dispose();
    }

    [Benchmark]
    public bool VerifyEs384Jwt()
    {
        return JwtToken.TryVerifyEs384(_jwtToken, _ecdsaKey, out _);
    }

    [Benchmark]
    public bool ParseLoginRequest()
    {
        return LoginRequest.TryParse(_offlineLoginPayload, out _, out _);
    }
}
