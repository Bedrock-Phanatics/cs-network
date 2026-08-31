using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace CsNetwork.Cryptography;

public sealed class AesCtrCipher : IDisposable
{
    public const int BlockSize = 16;

    private readonly Aes _aes;
    private readonly byte[] _counterBlock = new byte[BlockSize];
    private readonly byte[] _keystreamBlock = new byte[BlockSize];
    private int _keystreamOffset = BlockSize;
    private bool _disposed;

    public AesCtrCipher(ReadOnlySpan<byte> key, ReadOnlySpan<byte> initialIv)
    {
        if (key.Length != 16 && key.Length != 24 && key.Length != 32)
        {
            throw new ArgumentException("Key must be 16, 24, or 32 bytes.", nameof(key));
        }

        if (initialIv.Length != BlockSize)
        {
            throw new ArgumentException("Initial IV must be 16 bytes.", nameof(initialIv));
        }

        _aes = Aes.Create();
        _aes.Key = key.ToArray();
        _aes.Mode = CipherMode.ECB;
        _aes.Padding = PaddingMode.None;

        initialIv.CopyTo(_counterBlock);
        _keystreamOffset = BlockSize;
    }

    public void Transform(ReadOnlySpan<byte> source, Span<byte> destination)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (destination.Length < source.Length)
        {
            throw new ArgumentException("Destination span too small.", nameof(destination));
        }

        int srcIndex = 0;

        while (_keystreamOffset < BlockSize && srcIndex < source.Length)
        {
            destination[srcIndex] = (byte)(source[srcIndex] ^ _keystreamBlock[_keystreamOffset++]);
            srcIndex++;
        }

        // 64-bit parallel xor over 16-byte blocks
        while (srcIndex + BlockSize <= source.Length)
        {
            _aes.EncryptEcb(_counterBlock, _keystreamBlock, PaddingMode.None);
            IncrementCounter(_counterBlock);

            ulong s0 = BinaryPrimitives.ReadUInt64LittleEndian(source[srcIndex..]);
            ulong s1 = BinaryPrimitives.ReadUInt64LittleEndian(source[(srcIndex + 8)..]);
            ulong k0 = BinaryPrimitives.ReadUInt64LittleEndian(_keystreamBlock.AsSpan(0, 8));
            ulong k1 = BinaryPrimitives.ReadUInt64LittleEndian(_keystreamBlock.AsSpan(8, 8));

            BinaryPrimitives.WriteUInt64LittleEndian(destination[srcIndex..], s0 ^ k0);
            BinaryPrimitives.WriteUInt64LittleEndian(destination[(srcIndex + 8)..], s1 ^ k1);

            srcIndex += BlockSize;
        }

        // trailing bytes
        if (srcIndex < source.Length)
        {
            _aes.EncryptEcb(_counterBlock, _keystreamBlock, PaddingMode.None);
            IncrementCounter(_counterBlock);
            _keystreamOffset = 0;

            while (srcIndex < source.Length)
            {
                destination[srcIndex] = (byte)(source[srcIndex] ^ _keystreamBlock[_keystreamOffset++]);
                srcIndex++;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void TransformInPlace(Span<byte> buffer)
    {
        Transform(buffer, buffer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void IncrementCounter(Span<byte> counter)
    {
        // 128-bit big-endian counter increment
        for (int i = counter.Length - 1; i >= 0; i--)
        {
            if (++counter[i] != 0)
            {
                break;
            }
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            CryptographicOperations.ZeroMemory(_counterBlock);
            CryptographicOperations.ZeroMemory(_keystreamBlock);
            _aes.Dispose();
            _disposed = true;
        }
    }
}
