using System;
using System.IO.Compression;
using CsNetwork.Compression;
using Xunit;

namespace CsNetwork.Tests.Compression;

public class CompressionCodecTests
{
    [Theory]
    [InlineData(CompressionAlgorithm.Flate)]
    [InlineData(CompressionAlgorithm.Snappy)]
    [InlineData(CompressionAlgorithm.None)]
    public void Compress_And_Decompress_Roundtrip_Succeeds(CompressionAlgorithm algorithm)
    {
        var codec = CompressionRegistry.GetCodec(algorithm);

        byte[] original = new byte[4096];
        new Random(42).NextBytes(original);

        byte[] compressed = new byte[codec.GetMaxCompressedLength(original.Length)];
        int compressedLen = codec.Compress(original, compressed);
        Assert.True(compressedLen > 0);

        byte[] decompressed = new byte[original.Length];
        bool success = codec.TryDecompress(compressed.AsSpan(0, compressedLen), decompressed, out int decompressedLen);

        Assert.True(success);
        Assert.Equal(original.Length, decompressedLen);
        Assert.Equal(original, decompressed);
    }

    [Theory]
    [InlineData(CompressionAlgorithm.Flate, (ushort)0x0000)]
    [InlineData(CompressionAlgorithm.Snappy, (ushort)0x0001)]
    [InlineData(CompressionAlgorithm.None, (ushort)0xFFFF)]
    public void CompressionAlgorithm_NetworkSettings_16Bit_Roundtrip(CompressionAlgorithm algorithm, ushort expectedNetworkSettingsId)
    {
        ushort id = algorithm.ToNetworkSettingsId();
        Assert.Equal(expectedNetworkSettingsId, id);

        var converted = CompressionAlgorithmExtensions.FromNetworkSettingsId(id);
        Assert.Equal(algorithm, converted);

        Assert.True(CompressionAlgorithmExtensions.TryFromNetworkSettingsId(id, out var tryConverted));
        Assert.Equal(algorithm, tryConverted);
    }

    [Fact]
    public void Flate_Decompress_Supports_ZLib_Header()
    {
        var codec = FlateCompressionCodec.Instance;
        byte[] original = "Hello Bedrock Networking with RFC 1950 ZLib Header!"u8.ToArray();

        using var ms = new System.IO.MemoryStream();
        using (var zlib = new ZLibStream(ms, CompressionLevel.Optimal, leaveOpen: true))
        {
            zlib.Write(original);
        }
        byte[] zlibCompressed = ms.ToArray();

        byte[] decompressed = new byte[original.Length];
        bool success = codec.TryDecompress(zlibCompressed, decompressed, out int decompressedLen);

        Assert.True(success);
        Assert.Equal(original.Length, decompressedLen);
        Assert.Equal(original, decompressed);
    }

    [Fact]
    public void Snappy_Decompress_Rejects_Oversized_Preambles()
    {
        var codec = SnappyCompressionCodec.Instance;
        byte[] largeData = new byte[64 * 1024];
        Array.Fill(largeData, (byte)0x41);

        byte[] compressed = new byte[codec.GetMaxCompressedLength(largeData.Length)];
        int compressedLen = codec.Compress(largeData, compressed);

        byte[] smallDest = new byte[1024];
        bool success = codec.TryDecompress(compressed.AsSpan(0, compressedLen), smallDest, out int written, maxDecompressedLength: 1024);

        Assert.False(success);
        Assert.Equal(0, written);
    }

    [Fact]
    public void Flate_Decompress_Aborts_On_Decompression_Bomb()
    {
        var codec = FlateCompressionCodec.Instance;

        byte[] bombData = new byte[1024 * 1024];
        byte[] compressed = new byte[codec.GetMaxCompressedLength(bombData.Length)];
        int compressedLen = codec.Compress(bombData, compressed);

        byte[] dest = new byte[100 * 1024];
        bool success = codec.TryDecompress(compressed.AsSpan(0, compressedLen), dest, out int written, maxDecompressedLength: 50 * 1024);

        Assert.False(success);
        Assert.Equal(0, written);
    }

    [Theory]
    [InlineData(CompressionAlgorithm.Flate)]
    [InlineData(CompressionAlgorithm.Snappy)]
    public void Corrupted_Data_Returns_False_Without_Throwing(CompressionAlgorithm algorithm)
    {
        var codec = CompressionRegistry.GetCodec(algorithm);
        byte[] corrupted = [0xFF, 0xFE, 0xFD, 0xFC, 0xFB, 0xFA];

        byte[] dest = new byte[1024];
        bool success = codec.TryDecompress(corrupted, dest, out int written);

        Assert.False(success);
        Assert.Equal(0, written);
    }
}
