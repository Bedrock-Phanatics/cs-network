using System;
using CsNetwork.Framing;
using CsNetwork.IO;
using Xunit;

namespace CsNetwork.Tests.Framing;

public class SubPacketHeaderTests
{
    [Theory]
    [InlineData(0u, (byte)0, (byte)0)]
    [InlineData(1u, (byte)0, (byte)0)]
    [InlineData(1023u, (byte)3, (byte)3)]
    [InlineData(100u, (byte)1, (byte)2)]
    [InlineData(300u, (byte)2, (byte)1)]
    public void Encode_Decode_Roundtrips_Correctly(uint packetId, byte senderSubClient, byte targetSubClient)
    {
        var header = new SubPacketHeader(packetId, senderSubClient, targetSubClient);
        uint raw = header.Encode();

        var decoded = SubPacketHeader.Decode(raw);
        Assert.Equal(packetId, decoded.PacketId);
        Assert.Equal(senderSubClient, decoded.SenderSubClientId);
        Assert.Equal(targetSubClient, decoded.TargetSubClientId);
    }

    [Fact]
    public void Bitmask_Layout_Matches_Bedrock_Specification()
    {
        var header = new SubPacketHeader(21, 2, 3);
        uint encoded = header.Encode();

        Assert.Equal(21u, encoded & 0x3FF);
        Assert.Equal(2u, (encoded >> 10) & 0x03);
        Assert.Equal(3u, (encoded >> 12) & 0x03);
    }

    [Fact]
    public void TryRead_TryWrite_PacketReader_Roundtrips()
    {
        var original = new SubPacketHeader(123, 1, 2);
        Span<byte> buffer = stackalloc byte[16];

        var writer = new PacketWriter(buffer);
        Assert.True(original.TryWrite(ref writer));

        var reader = new PacketReader(writer.WrittenSpan);
        Assert.True(SubPacketHeader.TryRead(ref reader, out var decoded));
        Assert.Equal(original, decoded);
    }
}
