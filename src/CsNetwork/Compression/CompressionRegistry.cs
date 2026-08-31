using System;
using System.Diagnostics.CodeAnalysis;

namespace CsNetwork.Compression;

public static class CompressionRegistry
{
    public static bool TryGetCodec(CompressionAlgorithm algorithm, [NotNullWhen(true)] out ICompressionCodec? codec)
    {
        switch (algorithm)
        {
            case CompressionAlgorithm.Flate:
                codec = FlateCompressionCodec.Instance;
                return true;
            case CompressionAlgorithm.Snappy:
                codec = SnappyCompressionCodec.Instance;
                return true;
            case CompressionAlgorithm.None:
                codec = NoneCompressionCodec.Instance;
                return true;
            default:
                codec = null;
                return false;
        }
    }

    public static ICompressionCodec GetCodec(CompressionAlgorithm algorithm)
    {
        if (TryGetCodec(algorithm, out var codec))
            return codec;

        throw new NotSupportedException($"Unsupported compression algorithm {algorithm}");
    }
}
