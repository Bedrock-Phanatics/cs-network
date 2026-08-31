using System;

namespace CsNetwork.Compression;

public sealed class NoneCompressionCodec : ICompressionCodec
{
    public static readonly NoneCompressionCodec Instance = new();

    public CompressionAlgorithm Algorithm => CompressionAlgorithm.None;

    public int Compress(ReadOnlySpan<byte> source, Span<byte> destination)
    {
        if (destination.Length < source.Length)
            throw new ArgumentException("Destination span too small for uncompressed data.", nameof(destination));

        source.CopyTo(destination);
        return source.Length;
    }

    public bool TryDecompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten, int maxDecompressedLength = 3 * 1024 * 1024)
    {
        bytesWritten = 0;
        if (source.Length > maxDecompressedLength || destination.Length < source.Length)
            return false;

        source.CopyTo(destination);
        bytesWritten = source.Length;
        return true;
    }

    public int GetMaxCompressedLength(int sourceLength) => sourceLength;
}
