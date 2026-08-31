using System;
using System.Security.Cryptography;

namespace CsNetwork.Cryptography;

public static class BedrockKdf
{
    public const int KeyLength = 32;
    public const int IvLength = 16;
    public const int SaltLength = 16;

    public static void DeriveKey(ReadOnlySpan<byte> salt, ReadOnlySpan<byte> sharedSecret, Span<byte> destinationKey)
    {
        if (salt.Length != SaltLength)
            throw new ArgumentException($"Salt must be {SaltLength} bytes.", nameof(salt));

        if (sharedSecret.Length != EcdhKeyPair.SharedSecretLength)
            throw new ArgumentException($"Shared secret must be {EcdhKeyPair.SharedSecretLength} bytes.", nameof(sharedSecret));

        if (destinationKey.Length < KeyLength)
            throw new ArgumentException($"Destination key must be at least {KeyLength} bytes.", nameof(destinationKey));

        // bedrock key derivation: sha256(salt + sharedSecret)
        Span<byte> kdfInput = stackalloc byte[SaltLength + EcdhKeyPair.SharedSecretLength];
        salt.CopyTo(kdfInput);
        sharedSecret.CopyTo(kdfInput[SaltLength..]);

        try
        {
            SHA256.HashData(kdfInput, destinationKey[..KeyLength]);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(kdfInput);
        }
    }

    public static void DeriveIv(ReadOnlySpan<byte> key, Span<byte> destinationIv)
    {
        if (key.Length < KeyLength)
            throw new ArgumentException($"Key must be at least {KeyLength} bytes.", nameof(key));

        if (destinationIv.Length < IvLength)
            throw new ArgumentException($"Destination IV must be at least {IvLength} bytes.", nameof(destinationIv));

        // iv = key[0..12] + [0x00, 0x00, 0x00, 0x02]
        key[..12].CopyTo(destinationIv);
        destinationIv[12] = 0x00;
        destinationIv[13] = 0x00;
        destinationIv[14] = 0x00;
        destinationIv[15] = 0x02;
    }
}
