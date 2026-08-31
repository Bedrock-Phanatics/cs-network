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
    private byte[] _uncompressedPayload = null!;
    private byte[] _decodeBuffer = null!;
    private byte[] _encodeBuffer = null!;
    private SubPacketSlice[] _slices = null!;

    [Params(1, 10, 50)]
    public int SubPacketCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _slices = new SubPacketSlice[SubPacketCount];
        for (int i = 0; i < SubPacketCount; i++)
        {
            byte[] payload = new byte[150];
            Array.Fill(payload, (byte)(i + 1));
            _slices[i] = new SubPacketSlice(new SubPacketHeader((uint)(100 + i)), payload);
        }

        _encodeBuffer = new byte[131072];
        _decodeBuffer = new byte[131072];

        byte[] uncompressedBuf = new byte[131072];
        BatchEncoder.TryEncodeBatch(_slices, uncompressedBuf, out int uncompressedBytes, NoneCompressionCodec.Instance, 0);
        _rawUncompressedBatch = uncompressedBuf.AsSpan(0, uncompressedBytes).ToArray();

        byte[] flateBuf = new byte[131072];
        BatchEncoder.TryEncodeBatch(_slices, flateBuf, out int flateBytes, FlateCompressionCodec.Instance, 0);
        _rawFlateBatch = flateBuf.AsSpan(0, flateBytes).ToArray();

        byte[] snappyBuf = new byte[131072];
        BatchEncoder.TryEncodeBatch(_slices, snappyBuf, out int snappyBytes, SnappyCompressionCodec.Instance, 0);
        _rawSnappyBatch = snappyBuf.AsSpan(0, snappyBytes).ToArray();

        if (!BatchDecoder.TryDecode(_rawUncompressedBatch, _decodeBuffer, out int decodedLen, out _))
            throw new InvalidOperationException("Failed to decode uncompressed batch in benchmark setup.");

        _uncompressedPayload = _decodeBuffer.AsSpan(0, decodedLen).ToArray();
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
        var reader = new BatchFrameReader(_uncompressedPayload);
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
