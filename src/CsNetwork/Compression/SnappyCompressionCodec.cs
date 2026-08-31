using System;
using Snappier;

namespace CsNetwork.Compression;

public sealed class SnappyCompressionCodec : ICompressionCodec
{
    public static readonly SnappyCompressionCodec Instance = new();

    public CompressionAlgorithm Algorithm => CompressionAlgorithm.Snappy;

    public int Compress(ReadOnlySpan<byte> source, Span<byte> destination)
    {
        return Snappy.Compress(source, destination);
    }

    public bool TryDecompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten, int maxDecompressedLength = 3 * 1024 * 1024)
    {
        bytesWritten = 0;
        if (source.IsEmpty)
        {
            return true;
        }

        try
        {
            int uncompressedLength = Snappy.GetUncompressedLength(source);
            if (uncompressedLength < 0 || uncompressedLength > maxDecompressedLength || destination.Length < uncompressedLength)
            {
                return false;
            }

            bytesWritten = Snappy.Decompress(source, destination);
            return bytesWritten == uncompressedLength;
        }
        catch
        {
            bytesWritten = 0;
            return false;
        }
    }

    public int GetMaxCompressedLength(int sourceLength) => Snappy.GetMaxCompressedLength(sourceLength);
}
