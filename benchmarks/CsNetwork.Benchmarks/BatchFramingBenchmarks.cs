using System;
using BenchmarkDotNet.Attributes;
using CsNetwork.Compression;
using CsNetwork.Framing;

namespace CsNetwork.Benchmarks;

[MemoryDiagnoser]
public class BatchFramingBenchmarks
{
    private byte[] _rawUncompressedBatch = null!;
    private byte[] _rawFlateBatch = null!;
    private byte[] _rawSnappyBatch = null!;
    private byte[] _decodeBuffer = null!;
    private byte[] _encodeBuffer = null!;
    private SubPacketSlice[] _slices = null!;

    [GlobalSetup]
    public void Setup()
    {
        _slices = new SubPacketSlice[10];
        for (int i = 0; i < 10; i++)
        {
            byte[] payload = new byte[150];
            Array.Fill(payload, (byte)(i + 1));
            _slices[i] = new SubPacketSlice(new SubPacketHeader((uint)(100 + i)), payload);
        }

        _encodeBuffer = new byte[65536];
        _decodeBuffer = new byte[65536];

        byte[] uncompressedBuf = new byte[65536];
        BatchEncoder.TryEncodeBatch(_slices, uncompressedBuf, out int uncompressedBytes, NoneCompressionCodec.Instance, 0);
        _rawUncompressedBatch = uncompressedBuf.AsSpan(0, uncompressedBytes).ToArray();

        byte[] flateBuf = new byte[65536];
        BatchEncoder.TryEncodeBatch(_slices, flateBuf, out int flateBytes, FlateCompressionCodec.Instance, 0);
        _rawFlateBatch = flateBuf.AsSpan(0, flateBytes).ToArray();

        byte[] snappyBuf = new byte[65536];
        BatchEncoder.TryEncodeBatch(_slices, snappyBuf, out int snappyBytes, SnappyCompressionCodec.Instance, 0);
        _rawSnappyBatch = snappyBuf.AsSpan(0, snappyBytes).ToArray();
    }

    [Benchmark]
    public bool DecodeFlateBatch()
    {
        return BatchDecoder.TryDecode(_rawFlateBatch, _decodeBuffer, out _, out _);
    }

    [Benchmark]
    public bool DecodeSnappyBatch()
    {
        return BatchDecoder.TryDecode(_rawSnappyBatch, _decodeBuffer, out _, out _);
    }

    [Benchmark]
    public bool DecodeUncompressedBatch()
    {
        return BatchDecoder.TryDecode(_rawUncompressedBatch, _decodeBuffer, out _, out _);
    }

    [Benchmark]
    public int EnumerateSubPacketsBatchFrameReader()
    {
        var reader = new BatchFrameReader(_rawUncompressedBatch.AsSpan(2));
        int count = 0;
        while (reader.TryReadNext(out _, out _))
        {
            count++;
        }
        return count;
    }

    [Benchmark]
    public bool EncodeBatchSnappy()
    {
        return BatchEncoder.TryEncodeBatch(_slices, _encodeBuffer, out _, SnappyCompressionCodec.Instance, 0);
    }
}
