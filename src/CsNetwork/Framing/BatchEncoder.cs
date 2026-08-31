using System;
using System.Buffers;
using CsNetwork.Compression;
using CsNetwork.IO;

namespace CsNetwork.Framing;

public static class BatchEncoder
{
    public const byte BatchHeader = 0xFE;

    public static int WriteSubPacket(Span<byte> destination, SubPacketHeader header, ReadOnlySpan<byte> payload)
    {
        uint rawHeader = header.RawHeader;
        int headerBytes = VarIntCodec.GetVarUInt32ByteCount(rawHeader);
        uint fullLength = (uint)(headerBytes + payload.Length);
        int lengthBytes = VarIntCodec.GetVarUInt32ByteCount(fullLength);

        if (destination.Length < lengthBytes + headerBytes + payload.Length)
            return 0;

        VarIntCodec.WriteVarUInt32(destination, fullLength);
        VarIntCodec.WriteVarUInt32(destination[lengthBytes..], rawHeader);
        payload.CopyTo(destination[(lengthBytes + headerBytes)..]);

        return lengthBytes + headerBytes + payload.Length;
    }

    public static bool TryEncodeBatch(
        ReadOnlySpan<SubPacketSlice> subPackets,
        Span<byte> destination,
        out int bytesWritten,
        ICompressionCodec compressionCodec,
        int compressionThreshold = 512)
    {
        bytesWritten = 0;
        ArgumentNullException.ThrowIfNull(compressionCodec);

        if (destination.Length < 2)
            return false;

        int totalUncompressedSize = 0;
        for (int i = 0; i < subPackets.Length; i++)
        {
            var packet = subPackets[i];
            uint rawHeader = packet.Header.RawHeader;
            int headerBytes = VarIntCodec.GetVarUInt32ByteCount(rawHeader);
            uint fullLength = (uint)(headerBytes + packet.Length);
            int lengthBytes = VarIntCodec.GetVarUInt32ByteCount(fullLength);
            totalUncompressedSize += lengthBytes + (int)fullLength;
        }

        byte[] rentedUncompressed = ArrayPool<byte>.Shared.Rent(totalUncompressedSize);
        try
        {
            Span<byte> uncompressedSpan = rentedUncompressed.AsSpan(0, totalUncompressedSize);
            int pos = 0;

            for (int i = 0; i < subPackets.Length; i++)
            {
                var packet = subPackets[i];
                int written = WriteSubPacket(uncompressedSpan[pos..], packet.Header, packet.Span);
                pos += written;
            }

            destination[0] = BatchHeader;

            // bypass compression if below configured threshold
            if (totalUncompressedSize < compressionThreshold || compressionCodec.Algorithm == CompressionAlgorithm.None)
            {
                destination[1] = (byte)CompressionAlgorithm.None;
                if (destination.Length < 2 + totalUncompressedSize)
                    return false;

                uncompressedSpan.CopyTo(destination[2..]);
                bytesWritten = 2 + totalUncompressedSize;
                return true;
            }

            destination[1] = (byte)compressionCodec.Algorithm;
            int compressedLength = compressionCodec.Compress(uncompressedSpan, destination[2..]);
            bytesWritten = 2 + compressedLength;
            return true;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rentedUncompressed);
        }
    }
}
