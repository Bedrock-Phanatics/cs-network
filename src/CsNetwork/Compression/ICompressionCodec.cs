using System;

namespace CsNetwork.Compression;

public interface ICompressionCodec
{
    CompressionAlgorithm Algorithm { get; }

    int Compress(ReadOnlySpan<byte> source, Span<byte> destination);

    bool TryDecompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten, int maxDecompressedLength = 3 * 1024 * 1024);

    int GetMaxCompressedLength(int sourceLength);
}
