using System;
using System.IO;
using System.IO.Compression;

namespace CsNetwork.Compression;

public sealed class FlateCompressionCodec : ICompressionCodec
{
    public static readonly FlateCompressionCodec Instance = new();

    private readonly CompressionLevel _compressionLevel;

    public FlateCompressionCodec(CompressionLevel compressionLevel = CompressionLevel.Optimal)
    {
        _compressionLevel = compressionLevel;
    }

    public CompressionAlgorithm Algorithm => CompressionAlgorithm.Flate;

    public unsafe int Compress(ReadOnlySpan<byte> source, Span<byte> destination)
    {
        if (source.IsEmpty)
        {
            return 0;
        }

        fixed (byte* dstPtr = destination)
        {
            using var dstStream = new UnmanagedMemoryStream(dstPtr, destination.Length, destination.Length, FileAccess.Write);
            using (var compressor = new DeflateStream(dstStream, _compressionLevel, leaveOpen: true))
            {
                compressor.Write(source);
            }

            return (int)dstStream.Position;
        }
    }

    public bool TryDecompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten, int maxDecompressedLength = 3 * 1024 * 1024)
    {
        bytesWritten = 0;
        if (source.IsEmpty)
        {
            return true;
        }

        // rfc 1950 check cmf 0x78 and (cmf*256 + flg) % 31 == 0
        bool isZlibHeader = source.Length >= 2 &&
                            source[0] == 0x78 &&
                            (((source[0] << 8) | source[1]) % 31 == 0);

        if (isZlibHeader)
        {
            if (TryDecompressZLib(source, destination, out bytesWritten, maxDecompressedLength))
            {
                return true;
            }
        }

        return TryDecompressDeflate(source, destination, out bytesWritten, maxDecompressedLength);
    }

    private static unsafe bool TryDecompressDeflate(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten, int maxDecompressedLength)
    {
        bytesWritten = 0;
        try
        {
            fixed (byte* srcPtr = source)
            {
                using var srcStream = new UnmanagedMemoryStream(srcPtr, source.Length);
                using var decompressor = new DeflateStream(srcStream, CompressionMode.Decompress);

                int totalRead = 0;
                int maxAllowed = Math.Min(destination.Length, maxDecompressedLength);

                while (totalRead < maxAllowed)
                {
                    int read = decompressor.Read(destination[totalRead..maxAllowed]);
                    if (read == 0)
                    {
                        break;
                    }

                    totalRead += read;
                }

                Span<byte> checkExtra = stackalloc byte[1];
                if (decompressor.Read(checkExtra) > 0)
                {
                    bytesWritten = 0;
                    return false;
                }

                bytesWritten = totalRead;
                return true;
            }
        }
        catch
        {
            bytesWritten = 0;
            return false;
        }
    }

    private static unsafe bool TryDecompressZLib(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten, int maxDecompressedLength)
    {
        bytesWritten = 0;
        try
        {
            fixed (byte* srcPtr = source)
            {
                using var srcStream = new UnmanagedMemoryStream(srcPtr, source.Length);
                using var decompressor = new ZLibStream(srcStream, CompressionMode.Decompress);

                int totalRead = 0;
                int maxAllowed = Math.Min(destination.Length, maxDecompressedLength);

                while (totalRead < maxAllowed)
                {
                    int read = decompressor.Read(destination[totalRead..maxAllowed]);
                    if (read == 0)
                    {
                        break;
                    }

                    totalRead += read;
                }

                Span<byte> checkExtra = stackalloc byte[1];
                if (decompressor.Read(checkExtra) > 0)
                {
                    bytesWritten = 0;
                    return false;
                }

                bytesWritten = totalRead;
                return true;
            }
        }
        catch
        {
            bytesWritten = 0;
            return false;
        }
    }

    public int GetMaxCompressedLength(int sourceLength)
    {
        return sourceLength + ((sourceLength / 16384) + 1) * 5 + 64;
    }
}
