using System;
using System.Buffers.Binary;
using System.Security.Cryptography;

namespace CsNetwork.Cryptography;

public sealed class BedrockCipherSession : IDisposable
{
    public const int ChecksumLength = 8;

    private readonly byte[] _key;
    private readonly AesCtrCipher _senderCipher;
    private readonly AesCtrCipher _receiverCipher;

    private ulong _sendCounter;
    private ulong _receiveCounter;
    private bool _disposed;

    public BedrockCipherSession(ReadOnlySpan<byte> key)
    {
        if (key.Length != BedrockKdf.KeyLength)
            throw new ArgumentException($"Key must be {BedrockKdf.KeyLength} bytes.", nameof(key));

        _key = key.ToArray();
        Span<byte> sendIv = stackalloc byte[BedrockKdf.IvLength];
        Span<byte> recvIv = stackalloc byte[BedrockKdf.IvLength];
        BedrockKdf.DeriveIv(_key, sendIv);
        BedrockKdf.DeriveIv(_key, recvIv);

        _senderCipher = new AesCtrCipher(_key, sendIv);
        _receiverCipher = new AesCtrCipher(_key, recvIv);
        _sendCounter = 0;
        _receiveCounter = 0;
    }

    public BedrockCipherSession(ReadOnlySpan<byte> key, ReadOnlySpan<byte> sendIv, ReadOnlySpan<byte> receiveIv)
    {
        if (key.Length != BedrockKdf.KeyLength)
            throw new ArgumentException($"Key must be {BedrockKdf.KeyLength} bytes.", nameof(key));

        _key = key.ToArray();
        _senderCipher = new AesCtrCipher(_key, sendIv);
        _receiverCipher = new AesCtrCipher(_key, receiveIv);
        _sendCounter = 0;
        _receiveCounter = 0;
    }

    public ulong SendCounter => _sendCounter;
    public ulong ReceiveCounter => _receiveCounter;

    public int Encrypt(ReadOnlySpan<byte> plainPayload, Span<byte> destinationCiphertext)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        int totalLen = plainPayload.Length + ChecksumLength;
        if (destinationCiphertext.Length < totalLen)
            throw new ArgumentException("Destination span is too small for encrypted payload + checksum.", nameof(destinationCiphertext));

        Span<byte> unencryptedFrame = stackalloc byte[totalLen];
        plainPayload.CopyTo(unencryptedFrame);

        ComputeChecksum(_sendCounter, plainPayload, _key, unencryptedFrame[plainPayload.Length..]);
        _senderCipher.Transform(unencryptedFrame, destinationCiphertext[..totalLen]);

        _sendCounter++;
        return totalLen;
    }

    public bool TryDecrypt(ReadOnlySpan<byte> ciphertextWithChecksum, Span<byte> destinationPlaintext, out int plaintextLength)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        plaintextLength = 0;

        if (ciphertextWithChecksum.Length < ChecksumLength)
            return false;

        int plainLen = ciphertextWithChecksum.Length - ChecksumLength;
        if (destinationPlaintext.Length < plainLen)
            return false;

        Span<byte> decryptedFrame = stackalloc byte[ciphertextWithChecksum.Length];
        _receiverCipher.Transform(ciphertextWithChecksum, decryptedFrame);

        ReadOnlySpan<byte> decryptedPayload = decryptedFrame[..plainLen];
        ReadOnlySpan<byte> receivedChecksum = decryptedFrame[plainLen..];

        Span<byte> expectedChecksum = stackalloc byte[ChecksumLength];
        ComputeChecksum(_receiveCounter, decryptedPayload, _key, expectedChecksum);

        // constant-time comparison to prevent timing attacks
        if (!CryptographicOperations.FixedTimeEquals(receivedChecksum, expectedChecksum))
            return false;

        decryptedPayload.CopyTo(destinationPlaintext[..plainLen]);
        plaintextLength = plainLen;
        _receiveCounter++;
        return true;
    }

    public int EncryptBatch(ReadOnlySpan<byte> batchPayloadWith0xFE, Span<byte> destinationCiphertext)
    {
        if (batchPayloadWith0xFE.IsEmpty || batchPayloadWith0xFE[0] != 0xFE)
            throw new ArgumentException("Batch frame must begin with 0xFE header.", nameof(batchPayloadWith0xFE));

        destinationCiphertext[0] = 0xFE;
        int encrypted = Encrypt(batchPayloadWith0xFE[1..], destinationCiphertext[1..]);
        return 1 + encrypted;
    }

    public bool TryDecryptBatch(ReadOnlySpan<byte> encryptedBatchWith0xFE, Span<byte> destinationPlaintext, out int plaintextLength)
    {
        plaintextLength = 0;
        if (encryptedBatchWith0xFE.IsEmpty || encryptedBatchWith0xFE[0] != 0xFE)
            return false;

        destinationPlaintext[0] = 0xFE;
        if (!TryDecrypt(encryptedBatchWith0xFE[1..], destinationPlaintext[1..], out int decryptedLen))
            return false;

        plaintextLength = 1 + decryptedLen;
        return true;
    }

    private static void ComputeChecksum(ulong counter, ReadOnlySpan<byte> payload, ReadOnlySpan<byte> key, Span<byte> destinationChecksum)
    {
        // bedrock packet checksum: sha256(counter_le + payload + key)[..8]
        Span<byte> counterBytes = stackalloc byte[8];
        BinaryPrimitives.WriteUInt64LittleEndian(counterBytes, counter);

        using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        sha.AppendData(counterBytes);
        sha.AppendData(payload);
        sha.AppendData(key);

        Span<byte> fullHash = stackalloc byte[32];
        sha.GetCurrentHash(fullHash);
        fullHash[..ChecksumLength].CopyTo(destinationChecksum);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _senderCipher.Dispose();
            _receiverCipher.Dispose();
            CryptographicOperations.ZeroMemory(_key);
            _disposed = true;
        }
    }
}
