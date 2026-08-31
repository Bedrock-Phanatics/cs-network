using System;
using System.Security.Cryptography;
using CsNetwork.Cryptography;
using Xunit;

namespace CsNetwork.Tests.Cryptography;

public sealed class CryptoTests
{
    [Fact]
    public void EcdhKeyPair_Generate_And_Export_UncompressedPublicKey_Matches97Bytes()
    {
        using var keyPair = EcdhKeyPair.Create();
        byte[] pubKey = keyPair.GetUncompressedPublicKey();

        Assert.Equal(97, pubKey.Length);
        Assert.Equal(0x04, pubKey[0]);
    }

    [Fact]
    public void EcdhKeyPair_Alice_And_Bob_Derive_Same_SharedSecret()
    {
        using var alice = EcdhKeyPair.Create();
        using var bob = EcdhKeyPair.Create();

        byte[] alicePublic = alice.GetUncompressedPublicKey();
        byte[] bobPublic = bob.GetUncompressedPublicKey();

        Span<byte> aliceSecret = stackalloc byte[48];
        Span<byte> bobSecret = stackalloc byte[48];

        bool aliceSuccess = alice.TryDeriveSharedSecret(bobPublic, aliceSecret, out int aliceBytes);
        bool bobSuccess = bob.TryDeriveSharedSecret(alicePublic, bobSecret, out int bobBytes);

        Assert.True(aliceSuccess);
        Assert.True(bobSuccess);
        Assert.Equal(48, aliceBytes);
        Assert.Equal(48, bobBytes);
        Assert.True(aliceSecret.SequenceEqual(bobSecret));
    }

    [Fact]
    public void BedrockKdf_DeriveKey_MatchesExpectedSha256()
    {
        byte[] salt = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16];
        byte[] sharedSecret = new byte[48];
        Array.Fill(sharedSecret, (byte)0x42);

        Span<byte> derivedKey = stackalloc byte[32];
        BedrockKdf.DeriveKey(salt, sharedSecret, derivedKey);

        byte[] combined = new byte[salt.Length + sharedSecret.Length];
        salt.CopyTo(combined, 0);
        sharedSecret.CopyTo(combined, salt.Length);
        byte[] expected = SHA256.HashData(combined);

        Assert.True(derivedKey.SequenceEqual(expected));
    }

    [Fact]
    public void BedrockKdf_DeriveIv_MatchesBedrockSpec()
    {
        byte[] key = new byte[32];
        for (int i = 0; i < 32; i++)
        {
            key[i] = (byte)(i + 1);
        }

        Span<byte> iv = stackalloc byte[16];
        BedrockKdf.DeriveIv(key, iv);

        Assert.True(iv[..12].SequenceEqual(key.AsSpan(0, 12)));
        Assert.Equal(0x00, iv[12]);
        Assert.Equal(0x00, iv[13]);
        Assert.Equal(0x00, iv[14]);
        Assert.Equal(0x02, iv[15]);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(15)]
    [InlineData(16)]
    [InlineData(17)]
    [InlineData(31)]
    [InlineData(32)]
    [InlineData(100)]
    [InlineData(1024)]
    public void AesCtrCipher_Roundtrip_Arbitrary_Lengths(int length)
    {
        byte[] key = new byte[32];
        byte[] iv = new byte[16];
        Random.Shared.NextBytes(key);
        Random.Shared.NextBytes(iv);

        byte[] original = new byte[length];
        Random.Shared.NextBytes(original);

        using var encryptor = new AesCtrCipher(key, iv);
        using var decryptor = new AesCtrCipher(key, iv);

        byte[] encrypted = new byte[length];
        encryptor.Transform(original, encrypted);

        byte[] decrypted = new byte[length];
        decryptor.Transform(encrypted, decrypted);

        Assert.Equal(original, decrypted);
    }

    [Fact]
    public void BedrockCipherSession_Packet_Encrypt_Decrypt_Roundtrip()
    {
        byte[] key = new byte[32];
        Random.Shared.NextBytes(key);

        using var sender = new BedrockCipherSession(key);
        using var receiver = new BedrockCipherSession(key);

        for (int i = 0; i < 5; i++)
        {
            byte[] payload = new byte[150 + i * 20];
            Random.Shared.NextBytes(payload);

            byte[] encrypted = new byte[payload.Length + BedrockCipherSession.ChecksumLength];
            int encryptedBytes = sender.Encrypt(payload, encrypted);
            Assert.Equal(payload.Length + 8, encryptedBytes);
            Assert.Equal((ulong)(i + 1), sender.SendCounter);

            byte[] decrypted = new byte[payload.Length];
            bool success = receiver.TryDecrypt(encrypted, decrypted, out int decryptedBytes);

            Assert.True(success);
            Assert.Equal(payload.Length, decryptedBytes);
            Assert.Equal((ulong)(i + 1), receiver.ReceiveCounter);
            Assert.Equal(payload, decrypted);
        }
    }

    [Fact]
    public void BedrockCipherSession_Batch_Encrypt_Decrypt_Roundtrip()
    {
        byte[] key = new byte[32];
        Random.Shared.NextBytes(key);

        using var sender = new BedrockCipherSession(key);
        using var receiver = new BedrockCipherSession(key);

        byte[] rawBatch = [0xFE, 0x01, 0x02, 0x03, 0x04, 0x05];

        byte[] encryptedBatch = new byte[rawBatch.Length + BedrockCipherSession.ChecksumLength];
        int encLen = sender.EncryptBatch(rawBatch, encryptedBatch);
        Assert.Equal(rawBatch.Length + 8, encLen);
        Assert.Equal(0xFE, encryptedBatch[0]);

        byte[] decryptedBatch = new byte[rawBatch.Length];
        bool success = receiver.TryDecryptBatch(encryptedBatch, decryptedBatch, out int decLen);

        Assert.True(success);
        Assert.Equal(rawBatch.Length, decLen);
        Assert.Equal(0xFE, decryptedBatch[0]);
        Assert.Equal(rawBatch, decryptedBatch);
    }

    [Fact]
    public void BedrockCipherSession_CorruptedCiphertext_BitFlip_Rejects()
    {
        byte[] key = new byte[32];
        Random.Shared.NextBytes(key);

        using var sender = new BedrockCipherSession(key);
        using var receiver = new BedrockCipherSession(key);

        byte[] payload = "Sensitive Bedrock Payload"u8.ToArray();
        byte[] encrypted = new byte[payload.Length + BedrockCipherSession.ChecksumLength];
        sender.Encrypt(payload, encrypted);

        encrypted[5] ^= 0x01;

        byte[] dest = new byte[payload.Length];
        bool success = receiver.TryDecrypt(encrypted, dest, out int written);

        Assert.False(success);
        Assert.Equal(0, written);
    }

    [Fact]
    public void BedrockCipherSession_Out_Of_Order_Replay_Rejects()
    {
        byte[] key = new byte[32];
        Random.Shared.NextBytes(key);

        using var sender = new BedrockCipherSession(key);
        using var receiver = new BedrockCipherSession(key);

        byte[] p1 = "Packet 1"u8.ToArray();
        byte[] p2 = "Packet 2"u8.ToArray();

        byte[] e1 = new byte[p1.Length + 8];
        byte[] e2 = new byte[p2.Length + 8];

        sender.Encrypt(p1, e1);
        sender.Encrypt(p2, e2);

        byte[] dest = new byte[64];
        bool success = receiver.TryDecrypt(e2, dest, out _);

        Assert.False(success);
    }

    [Fact]
    public void BedrockCipherSession_Truncated_Ciphertext_Rejects()
    {
        byte[] key = new byte[32];
        using var session = new BedrockCipherSession(key);

        byte[] truncated = [0x01, 0x02, 0x03];
        byte[] dest = new byte[64];

        Assert.False(session.TryDecrypt(truncated, dest, out _));
    }

    [Fact]
    public void BedrockCipherSession_Dispose_IsIdempotent_And_Safe()
    {
        byte[] key = new byte[32];
        var session = new BedrockCipherSession(key);

        session.Dispose();
        session.Dispose();
    }
}
