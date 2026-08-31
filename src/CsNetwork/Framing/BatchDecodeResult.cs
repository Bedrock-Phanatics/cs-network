using System;
using System.Buffers;

namespace CsNetwork.Framing;

public struct BatchDecodeResult : IDisposable
{
    private byte[]? _rentedDecompressed;
    private SubPacketSlice[]? _rentedSlices;
    private readonly int _sliceCount;
    private readonly int _decompressedLength;

    public BatchDecodeResult(byte[] rentedDecompressed, int decompressedLength, SubPacketSlice[] rentedSlices, int sliceCount)
    {
        _rentedDecompressed = rentedDecompressed;
        _decompressedLength = decompressedLength;
        _rentedSlices = rentedSlices;
        _sliceCount = sliceCount;
    }

    public static BatchDecodeResult Empty => default;

    public readonly bool IsEmpty => _rentedDecompressed == null || _sliceCount == 0;
    public readonly int Count => _rentedDecompressed != null ? _sliceCount : 0;
    public readonly ReadOnlyMemory<byte> DecompressedMemory => _rentedDecompressed != null ? _rentedDecompressed.AsMemory(0, _decompressedLength) : ReadOnlyMemory<byte>.Empty;
    public readonly ReadOnlySpan<SubPacketSlice> SubPackets => _rentedSlices != null ? _rentedSlices.AsSpan(0, _sliceCount) : ReadOnlySpan<SubPacketSlice>.Empty;

    public void Dispose()
    {
        byte[]? decompressed = _rentedDecompressed;
        _rentedDecompressed = null;
        if (decompressed != null)
        {
            ArrayPool<byte>.Shared.Return(decompressed);
        }

        SubPacketSlice[]? slices = _rentedSlices;
        _rentedSlices = null;
        if (slices != null)
        {
            ArrayPool<SubPacketSlice>.Shared.Return(slices);
        }
    }
}
