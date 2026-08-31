using System;
using System.Buffers;
using CsNetwork.Compression;
using CsNetwork.Framing;
using Xunit;

namespace CsNetwork.Tests.Framing;

public class BatchFramingTests
{
    [Theory]
    [InlineData(CompressionAlgorithm.Flate)]
    [InlineData(CompressionAlgorithm.Snappy)]
    [InlineData(CompressionAlgorithm.None)]
    public void Batch_Encode_And_Decode_Roundtrip_Succeeds(CompressionAlgorithm algorithm)
    {
        var codec = CompressionRegistry.GetCodec(algorithm);

        SubPacketSlice[] packets = new SubPacketSlice[5];
        for (int i = 0; i < 5; i++)
        {
            byte[] payload = new byte[200];
            Array.Fill(payload, (byte)(i + 1));
            packets[i] = new SubPacketSlice(new SubPacketHeader((uint)(10 + i), (byte)(i % 4), (byte)((i + 1) % 4)), payload);
        }

        byte[] batchBuffer = new byte[65536];
        bool encodeSuccess = BatchEncoder.TryEncodeBatch(packets, batchBuffer, out int batchBytes, codec, compressionThreshold: 0);
        Assert.True(encodeSuccess);
        Assert.True(batchBytes > 2);
        Assert.Equal(BatchDecoder.BatchHeader, batchBuffer[0]);

        bool decodeSuccess = BatchDecoder.TryDecodePooled(batchBuffer.AsMemory(0, batchBytes), out var result);
        Assert.True(decodeSuccess);
        using (result)
        {
            Assert.Equal(5, result.Count);
            for (int i = 0; i < 5; i++)
            {
                var slice = result.SubPackets[i];
                Assert.Equal((uint)(10 + i), slice.Header.PacketId);
                Assert.Equal((byte)(i % 4), slice.Header.SenderSubClientId);
                Assert.Equal((byte)((i + 1) % 4), slice.Header.TargetSubClientId);
                Assert.Equal(200, slice.Length);
                Assert.True(slice.Span.SequenceEqual(packets[i].Payload.Span));
            }
        }
    }

    [Fact]
    public void Batch_Encoder_Bypasses_Compression_When_Below_Threshold()
    {
        var codec = FlateCompressionCodec.Instance;

        byte[] payload = new byte[50];
        SubPacketSlice[] packets = [new SubPacketSlice(new SubPacketHeader(1), payload)];

        byte[] batchBuffer = new byte[1024];
        bool encodeSuccess = BatchEncoder.TryEncodeBatch(packets, batchBuffer, out int batchBytes, codec, compressionThreshold: 512);

        Assert.True(encodeSuccess);
        Assert.Equal(BatchDecoder.BatchHeader, batchBuffer[0]);
        Assert.Equal((byte)CompressionAlgorithm.None, batchBuffer[1]);

        using var result = BatchDecoder.TryDecodePooled(batchBuffer.AsMemory(0, batchBytes), out var decoded) ? decoded : default;
        Assert.Equal(1, result.Count);
        Assert.Equal(50, result.SubPackets[0].Length);
    }

    [Fact]
    public void BatchFrameReader_Enumerates_SubPackets_Zero_Copy()
    {
        Span<byte> uncompressed = stackalloc byte[512];
        int pos = 0;

        pos += BatchEncoder.WriteSubPacket(uncompressed[pos..], new SubPacketHeader(10, 0, 0), "Packet 1 Payload"u8);
        pos += BatchEncoder.WriteSubPacket(uncompressed[pos..], new SubPacketHeader(20, 1, 0), "Packet 2 Payload"u8);
        pos += BatchEncoder.WriteSubPacket(uncompressed[pos..], new SubPacketHeader(30, 2, 1), "Packet 3 Payload"u8);

        var reader = new BatchFrameReader(uncompressed[..pos]);
        Assert.Equal(3, CountPackets(ref reader));

        reader = new BatchFrameReader(uncompressed[..pos]);

        Assert.True(reader.TryReadNext(out var h1, out var p1));
        Assert.Equal(10u, h1.PacketId);
        Assert.True(p1.SequenceEqual("Packet 1 Payload"u8));

        Assert.True(reader.TryReadNext(out var h2, out var p2));
        Assert.Equal(20u, h2.PacketId);
        Assert.Equal((byte)1, h2.SenderSubClientId);
        Assert.True(p2.SequenceEqual("Packet 2 Payload"u8));

        Assert.True(reader.TryReadNext(out var h3, out var p3));
        Assert.Equal(30u, h3.PacketId);
        Assert.Equal((byte)2, h3.SenderSubClientId);
        Assert.Equal((byte)1, h3.TargetSubClientId);
        Assert.True(p3.SequenceEqual("Packet 3 Payload"u8));

        Assert.False(reader.TryReadNext(out _, out _));
    }

    private static int CountPackets(ref BatchFrameReader reader)
    {
        int count = 0;
        while (reader.TryReadNext(out _, out _))
        {
            count++;
        }
        return count;
    }

    [Fact]
    public void Batch_Decoder_Rejects_Batches_Exceeding_812_SubPackets()
    {
        SubPacketSlice[] packets = new SubPacketSlice[813];
        byte[] payload = [0x01, 0x02];
        for (int i = 0; i < 813; i++)
        {
            packets[i] = new SubPacketSlice(new SubPacketHeader((uint)(i % 100)), payload);
        }

        byte[] batchBuffer = new byte[100_000];
        Assert.True(BatchEncoder.TryEncodeBatch(packets, batchBuffer, out int batchBytes, NoneCompressionCodec.Instance, 0));

        bool success = BatchDecoder.TryDecodePooled(batchBuffer.AsMemory(0, batchBytes), out var result);
        Assert.False(success);
        Assert.True(result.IsEmpty);
    }

    [Fact]
    public void BatchDecoder_TryDecode_StrictlyEnforces_812_Limit()
    {
        SubPacketSlice[] packets812 = new SubPacketSlice[812];
        byte[] payload = [0x01];
        for (int i = 0; i < 812; i++)
        {
            packets812[i] = new SubPacketSlice(new SubPacketHeader((uint)(i % 100)), payload);
        }

        byte[] batchBuffer = new byte[100_000];
        Assert.True(BatchEncoder.TryEncodeBatch(packets812, batchBuffer, out int batchBytes812, NoneCompressionCodec.Instance, 0));

        byte[] decompressed = new byte[100_000];
        Assert.True(BatchDecoder.TryDecode(batchBuffer.AsSpan(0, batchBytes812), decompressed, out int decompLen812, out int count812));
        Assert.Equal(812, count812);

        SubPacketSlice[] packets813 = new SubPacketSlice[813];
        for (int i = 0; i < 813; i++)
        {
            packets813[i] = new SubPacketSlice(new SubPacketHeader((uint)(i % 100)), payload);
        }

        Assert.True(BatchEncoder.TryEncodeBatch(packets813, batchBuffer, out int batchBytes813, NoneCompressionCodec.Instance, 0));
        Assert.False(BatchDecoder.TryDecode(batchBuffer.AsSpan(0, batchBytes813), decompressed, out _, out _));
    }

    [Fact]
    public void BatchDecodeResult_DoubleDispose_IsIdempotent_And_Safe()
    {
        SubPacketSlice[] packets = [new SubPacketSlice(new SubPacketHeader(1), new byte[10])];
        byte[] batchBuffer = new byte[128];
        Assert.True(BatchEncoder.TryEncodeBatch(packets, batchBuffer, out int batchBytes, NoneCompressionCodec.Instance, 0));

        Assert.True(BatchDecoder.TryDecodePooled(batchBuffer.AsMemory(0, batchBytes), out var result));
        Assert.False(result.IsEmpty);

        result.Dispose();
        Assert.True(result.IsEmpty);
        Assert.Equal(0, result.Count);
        Assert.True(result.DecompressedMemory.IsEmpty);
        Assert.True(result.SubPackets.IsEmpty);

        result.Dispose();
    }

    [Fact]
    public void BatchDecoder_LargePacketLength_RejectsCleanly_WithoutOverflow()
    {
        byte[] malformedBatch = [
            BatchDecoder.BatchHeader,
            (byte)CompressionAlgorithm.None,
            0x81, 0x80, 0x80, 0x80, 0x08,
            0x01, 0x02, 0x03, 0x04
        ];

        byte[] dest = new byte[1024];
        bool tryDecodeSuccess = BatchDecoder.TryDecode(malformedBatch, dest, out _, out _);
        Assert.False(tryDecodeSuccess);

        bool tryDecodePooledSuccess = BatchDecoder.TryDecodePooled(malformedBatch, out var result);
        Assert.False(tryDecodePooledSuccess);
        Assert.True(result.IsEmpty);
    }

    [Fact]
    public void Batch_Decoder_Rejects_Invalid_Batch_Header()
    {
        byte[] corrupted = [0xAA, 0xFF, 0x05, 0x01, 0x02];
        bool success = BatchDecoder.TryDecodePooled(corrupted, out var result);
        Assert.False(success);
        Assert.True(result.IsEmpty);
    }

    [Fact]
    public void Batch_Decoder_Rejects_Unknown_Compression_Algorithm()
    {
        byte[] corrupted = [0xFE, 0x55, 0x05, 0x01, 0x02];
        bool success = BatchDecoder.TryDecodePooled(corrupted, out var result);
        Assert.False(success);
        Assert.True(result.IsEmpty);
    }

    [Fact]
    public void Batch_Decoder_Rejects_Truncated_SubPacket_Lengths()
    {
        byte[] truncated = [0xFE, 0xFF, 0x64, 0x01, 0x02];
        bool success = BatchDecoder.TryDecodePooled(truncated, out var result);
        Assert.False(success);
        Assert.True(result.IsEmpty);
    }
}
