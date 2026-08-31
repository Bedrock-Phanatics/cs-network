using System;
using BenchmarkDotNet.Attributes;
using CsNetwork.Cryptography;

namespace CsNetwork.Benchmarks;

[MemoryDiagnoser]
public class CryptoBenchmarks
{
    private byte[] _key = null!;
    private byte[] _salt = null!;
    private byte[] _sharedSecret = null!;
    private byte[] _plaintext = null!;
    private byte[] _ciphertext = null!;
    private byte[] _destination = null!;
    private byte[] _rawBatch = null!;
    private byte[] _encryptedBatch = null!;
    private BedrockCipherSession _senderSession = null!;
    private BedrockCipherSession _receiverSession = null!;
    private AesCtrCipher _aesCtr = null!;

    [GlobalSetup]
    public void Setup()
    {
        _key = new byte[32];
        _salt = new byte[16];
        _sharedSecret = new byte[48];
        Random.Shared.NextBytes(_key);
        Random.Shared.NextBytes(_salt);
        Random.Shared.NextBytes(_sharedSecret);

        _plaintext = new byte[1024];
        Random.Shared.NextBytes(_plaintext);

        _destination = new byte[2048];

        _senderSession = new BedrockCipherSession(_key);
        _receiverSession = new BedrockCipherSession(_key);

        _ciphertext = new byte[_plaintext.Length + BedrockCipherSession.ChecksumLength];
        _senderSession.Encrypt(_plaintext, _ciphertext);

        _rawBatch = new byte[1025];
        _rawBatch[0] = 0xFE;
        _plaintext.CopyTo(_rawBatch.AsSpan(1));

        _encryptedBatch = new byte[_rawBatch.Length + BedrockCipherSession.ChecksumLength];
        _senderSession.EncryptBatch(_rawBatch, _encryptedBatch);

        Span<byte> iv = stackalloc byte[16];
        BedrockKdf.DeriveIv(_key, iv);
        _aesCtr = new AesCtrCipher(_key, iv);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _senderSession.Dispose();
        _receiverSession.Dispose();
        _aesCtr.Dispose();
    }

    [Benchmark]
    public void DeriveKey()
    {
        Span<byte> dest = stackalloc byte[32];
        BedrockKdf.DeriveKey(_salt, _sharedSecret, dest);
    }

    [Benchmark]
    public void DeriveIv()
    {
        Span<byte> dest = stackalloc byte[16];
        BedrockKdf.DeriveIv(_key, dest);
    }

    [Benchmark]
    public void AesCtrTransform1KB()
    {
        _aesCtr.Transform(_plaintext, _destination);
    }

    [Benchmark]
    public int SessionEncryptPayload()
    {
        return _senderSession.Encrypt(_plaintext, _destination);
    }

    [Benchmark]
    public bool SessionDecryptPayload()
    {
        return _receiverSession.TryDecrypt(_ciphertext, _destination, out _);
    }

    [Benchmark]
    public int SessionEncryptBatch()
    {
        return _senderSession.EncryptBatch(_rawBatch, _destination);
    }

    [Benchmark]
    public bool SessionDecryptBatch()
    {
        return _receiverSession.TryDecryptBatch(_encryptedBatch, _destination, out _);
    }
}
