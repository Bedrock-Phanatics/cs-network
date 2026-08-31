using System;
using System.Security.Cryptography;

namespace CsNetwork.Authentication;

public static class MojangKeys
{
    // official mojang root public key (current) in der subjectpublickeyinfo base64
    public const string RootPublicKeyBase64 = "MHYwEAYHKoZIzj0CAQYFK4EEACIDYgAECRXueJeTDqNRRgJi/vlRufByu/2G0i2Ebt6YMar5QX/R0DIIyrJMcUpruK4QveTfJSTp3Shlq4Gk34cD/4GUWwkv0DVuzeuB+tXija7HBxii03NHDbPAD0AKnLr2wdAp";

    // legacy mojang root public key (pre-2020)
    public const string LegacyRootPublicKeyBase64 = "MHYwEAYHKoZIzj0CAQYFK4EEACIDYgAE8ELkixyLcwlZryUCj1dpVN1fLR29sskhGXdloqZv//ePpdZsYlGotaMlVlgTtvxYGZTegEULXVGkWQbS1TZ7K8SCGJUrAyNgwmEnbQrHm779R3nvlFS6+/umGh3jJhhU";

    private static readonly byte[] RootPublicKeySpki = Convert.FromBase64String(RootPublicKeyBase64);
    private static readonly byte[] LegacyRootPublicKeySpki = Convert.FromBase64String(LegacyRootPublicKeyBase64);

    public static ReadOnlySpan<byte> RootPublicKeyBytes => RootPublicKeySpki;
    public static ReadOnlySpan<byte> LegacyRootPublicKeyBytes => LegacyRootPublicKeySpki;

    public static ECDsa CreateRootPublicKey()
    {
        var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        ecdsa.ImportSubjectPublicKeyInfo(RootPublicKeySpki, out _);
        return ecdsa;
    }

    public static ECDsa CreateLegacyRootPublicKey()
    {
        var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        ecdsa.ImportSubjectPublicKeyInfo(LegacyRootPublicKeySpki, out _);
        return ecdsa;
    }

    public static bool IsMojangRootKey(ReadOnlySpan<byte> spkiDer)
    {
        return spkiDer.SequenceEqual(RootPublicKeySpki) || spkiDer.SequenceEqual(LegacyRootPublicKeySpki);
    }

    public static bool IsMojangRootKey(string? base64Spki)
    {
        if (string.IsNullOrEmpty(base64Spki))
            return false;

        return string.Equals(base64Spki, RootPublicKeyBase64, StringComparison.Ordinal) ||
               string.Equals(base64Spki, LegacyRootPublicKeyBase64, StringComparison.Ordinal);
    }

    public static bool IsMojangRootKey(ECDsa key)
    {
        ArgumentNullException.ThrowIfNull(key);
        byte[] spki = key.ExportSubjectPublicKeyInfo();
        return IsMojangRootKey(spki);
    }
}
