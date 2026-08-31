using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using CsNetwork.Compression;
using CsNetwork.IO;

namespace CsNetwork.Framing;

public static class BatchDecoder
{
    public const byte BatchHeader = 0xFE;
    public const int MaximumInBatch = 812;
    public const int MaxDecompressedSize = 3 * 1024 * 1024;

    public static bool TryDecode(ReadOnlySpan<byte> source, Span<byte> destination, out int decompressedLength, out int subPacketCount, int maxDecompressedLength = MaxDecompressedSize)
    {
        decompressedLength = 0;
        subPacketCount = 0;

        if (source.Length < 2 || source[0] != BatchHeader)
            return false;

        var algorithm = (CompressionAlgorithm)source[1];
        if (!CompressionRegistry.TryGetCodec(algorithm, out var codec))
            return false;

        ReadOnlySpan<byte> compressedPayload = source[2..];
        if (!codec.TryDecompress(compressedPayload, destination, out decompressedLength, maxDecompressedLength))
            return false;

        ReadOnlySpan<byte> decompressed = destination[..decompressedLength];
        int pos = 0;

        while (pos < decompressed.Length)
        {
            if (subPacketCount >= MaximumInBatch)
                return false;

            if (!VarIntCodec.TryReadVarUInt32(decompressed[pos..], out uint sliceLength, out int lenBytes))
                return false;

            pos += lenBytes;
            if ((uint)decompressed.Length - (uint)pos < sliceLength)
                return false;

            pos += (int)sliceLength;
            subPacketCount++;
        }

        return true;
    }

    public static bool TryDecodePooled(ReadOnlyMemory<byte> source, out BatchDecodeResult result, int maxDecompressedLength = MaxDecompressedSize)
    {
        result = default;
        ReadOnlySpan<byte> sourceSpan = source.Span;

        if (sourceSpan.Length < 2 || sourceSpan[0] != BatchHeader)
            return false;

        var algorithm = (CompressionAlgorithm)sourceSpan[1];
        if (!CompressionRegistry.TryGetCodec(algorithm, out var codec))
            return false;

        ReadOnlySpan<byte> compressedPayload = sourceSpan[2..];
        byte[] rentedDecompressed = ArrayPool<byte>.Shared.Rent(maxDecompressedLength);

        try
        {
            if (!codec.TryDecompress(compressedPayload, rentedDecompressed, out int decompressedLength, maxDecompressedLength))
            {
                ArrayPool<byte>.Shared.Return(rentedDecompressed);
                return false;
            }

            ReadOnlyMemory<byte> decompressedMem = rentedDecompressed.AsMemory(0, decompressedLength);
            ReadOnlySpan<byte> decompressedSpan = decompressedMem.Span;

            SubPacketSlice[] rentedSlices = ArrayPool<SubPacketSlice>.Shared.Rent(MaximumInBatch);
            int subPacketCount = 0;
            int pos = 0;

            while (pos < decompressedSpan.Length)
            {
                if (subPacketCount >= MaximumInBatch)
                {
                    ArrayPool<SubPacketSlice>.Shared.Return(rentedSlices);
                    ArrayPool<byte>.Shared.Return(rentedDecompressed);
                    return false;
                }

                if (!VarIntCodec.TryReadVarUInt32(decompressedSpan[pos..], out uint sliceLength, out int lenBytes))
                {
                    ArrayPool<SubPacketSlice>.Shared.Return(rentedSlices);
                    ArrayPool<byte>.Shared.Return(rentedDecompressed);
                    return false;
                }

                pos += lenBytes;
                if ((uint)decompressedSpan.Length - (uint)pos < sliceLength)
                {
                    ArrayPool<SubPacketSlice>.Shared.Return(rentedSlices);
                    ArrayPool<byte>.Shared.Return(rentedDecompressed);
                    return false;
                }

                int sliceLen = (int)sliceLength;
                ReadOnlyMemory<byte> fullSliceMem = decompressedMem.Slice(pos, sliceLen);
                ReadOnlySpan<byte> fullSliceSpan = fullSliceMem.Span;

                if (!VarIntCodec.TryReadVarUInt32(fullSliceSpan, out uint rawHeader, out int headerLenBytes))
                {
                    ArrayPool<SubPacketSlice>.Shared.Return(rentedSlices);
                    ArrayPool<byte>.Shared.Return(rentedDecompressed);
                    return false;
                }

                var header = SubPacketHeader.FromRawHeader(rawHeader);
                ReadOnlyMemory<byte> packetPayload = fullSliceMem[headerLenBytes..];

                rentedSlices[subPacketCount++] = new SubPacketSlice(header, packetPayload);
                pos += sliceLen;
            }

            result = new BatchDecodeResult(rentedDecompressed, decompressedLength, rentedSlices, subPacketCount);
            return true;
        }
        catch
        {
            ArrayPool<byte>.Shared.Return(rentedDecompressed);
            return false;
        }
    }
}
