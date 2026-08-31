using System;
using CsNetwork.IO;
using Xunit;

namespace CsNetwork.Tests.IO;

public sealed class VarIntCodecTests
{
    [Theory]
    [InlineData(0u, new byte[] { 0x00 })]
    [InlineData(1u, new byte[] { 0x01 })]
    [InlineData(127u, new byte[] { 0x7F })]
    [InlineData(128u, new byte[] { 0x80, 0x01 })]
    [InlineData(255u, new byte[] { 0xFF, 0x01 })]
    [InlineData(16383u, new byte[] { 0xFF, 0x7F })]
    [InlineData(16384u, new byte[] { 0x80, 0x80, 0x01 })]
    [InlineData(2097151u, new byte[] { 0xFF, 0xFF, 0x7F })]
    [InlineData(2097152u, new byte[] { 0x80, 0x80, 0x80, 0x01 })]
    [InlineData(268435455u, new byte[] { 0xFF, 0xFF, 0xFF, 0x7F })]
    [InlineData(268435456u, new byte[] { 0x80, 0x80, 0x80, 0x80, 0x01 })]
    [InlineData(uint.MaxValue, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0x0F })]
    public void VarUInt32_Roundtrip_MatchesExpectedBytes(uint value, byte[] expectedBytes)
    {
        Span<byte> buffer = stackalloc byte[10];
        int written = VarIntCodec.WriteVarUInt32(buffer, value);

        Assert.Equal(expectedBytes.Length, written);
        Assert.Equal(expectedBytes, buffer[..written].ToArray());
        Assert.Equal(expectedBytes.Length, VarIntCodec.GetVarUInt32ByteCount(value));

        bool success = VarIntCodec.TryReadVarUInt32(expectedBytes, out uint result, out int bytesRead);
        Assert.True(success);
        Assert.Equal(value, result);
        Assert.Equal(expectedBytes.Length, bytesRead);
    }

    [Theory]
    [InlineData(0, new byte[] { 0x00 })]
    [InlineData(-1, new byte[] { 0x01 })]
    [InlineData(1, new byte[] { 0x02 })]
    [InlineData(-2, new byte[] { 0x03 })]
    [InlineData(2, new byte[] { 0x04 })]
    [InlineData(63, new byte[] { 0x7E })]
    [InlineData(-64, new byte[] { 0x7F })]
    [InlineData(64, new byte[] { 0x80, 0x01 })]
    [InlineData(-65, new byte[] { 0x81, 0x01 })]
    [InlineData(int.MaxValue, new byte[] { 0xFE, 0xFF, 0xFF, 0xFF, 0x0F })]
    [InlineData(int.MinValue, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0x0F })]
    public void VarInt32_ZigZag_Roundtrip_MatchesExpectedBytes(int value, byte[] expectedBytes)
    {
        Span<byte> buffer = stackalloc byte[10];
        int written = VarIntCodec.WriteVarInt32(buffer, value);

        Assert.Equal(expectedBytes.Length, written);
        Assert.Equal(expectedBytes, buffer[..written].ToArray());
        Assert.Equal(expectedBytes.Length, VarIntCodec.GetVarInt32ByteCount(value));

        bool success = VarIntCodec.TryReadVarInt32(expectedBytes, out int result, out int bytesRead);
        Assert.True(success);
        Assert.Equal(value, result);
        Assert.Equal(expectedBytes.Length, bytesRead);
    }

    [Theory]
    [InlineData(0UL, new byte[] { 0x00 })]
    [InlineData(1UL, new byte[] { 0x01 })]
    [InlineData(127UL, new byte[] { 0x7F })]
    [InlineData(128UL, new byte[] { 0x80, 0x01 })]
    [InlineData(ulong.MaxValue, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x01 })]
    public void VarUInt64_Roundtrip_MatchesExpectedBytes(ulong value, byte[] expectedBytes)
    {
        Span<byte> buffer = stackalloc byte[15];
        int written = VarIntCodec.WriteVarUInt64(buffer, value);

        Assert.Equal(expectedBytes.Length, written);
        Assert.Equal(expectedBytes, buffer[..written].ToArray());
        Assert.Equal(expectedBytes.Length, VarIntCodec.GetVarUInt64ByteCount(value));

        bool success = VarIntCodec.TryReadVarUInt64(expectedBytes, out ulong result, out int bytesRead);
        Assert.True(success);
        Assert.Equal(value, result);
        Assert.Equal(expectedBytes.Length, bytesRead);
    }

    [Theory]
    [InlineData(0L, new byte[] { 0x00 })]
    [InlineData(-1L, new byte[] { 0x01 })]
    [InlineData(1L, new byte[] { 0x02 })]
    [InlineData(-2L, new byte[] { 0x03 })]
    [InlineData(2L, new byte[] { 0x04 })]
    [InlineData(long.MaxValue, new byte[] { 0xFE, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x01 })]
    [InlineData(long.MinValue, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x01 })]
    public void VarInt64_ZigZag_Roundtrip_MatchesExpectedBytes(long value, byte[] expectedBytes)
    {
        Span<byte> buffer = stackalloc byte[15];
        int written = VarIntCodec.WriteVarInt64(buffer, value);

        Assert.Equal(expectedBytes.Length, written);
        Assert.Equal(expectedBytes, buffer[..written].ToArray());
        Assert.Equal(expectedBytes.Length, VarIntCodec.GetVarInt64ByteCount(value));

        bool success = VarIntCodec.TryReadVarInt64(expectedBytes, out long result, out int bytesRead);
        Assert.True(success);
        Assert.Equal(value, result);
        Assert.Equal(expectedBytes.Length, bytesRead);
    }

    [Fact]
    public void VarUInt32_Rejects_MoreThan5Bytes()
    {
        byte[] malformed = [0x80, 0x80, 0x80, 0x80, 0x80, 0x01];
        bool success = VarIntCodec.TryReadVarUInt32(malformed, out _, out int bytesRead);
        Assert.False(success);
        Assert.Equal(0, bytesRead);
    }

    [Fact]
    public void VarUInt32_Rejects_5thByte_Overflow()
    {
        byte[] malformed = [0x80, 0x80, 0x80, 0x80, 0x10];
        bool success = VarIntCodec.TryReadVarUInt32(malformed, out _, out int bytesRead);
        Assert.False(success);
        Assert.Equal(0, bytesRead);
    }

    [Fact]
    public void VarUInt64_Rejects_MoreThan10Bytes()
    {
        byte[] malformed = [0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x01];
        bool success = VarIntCodec.TryReadVarUInt64(malformed, out _, out int bytesRead);
        Assert.False(success);
        Assert.Equal(0, bytesRead);
    }

    [Fact]
    public void VarUInt64_Rejects_10thByte_Overflow()
    {
        byte[] malformed = [0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x02];
        bool success = VarIntCodec.TryReadVarUInt64(malformed, out _, out int bytesRead);
        Assert.False(success);
        Assert.Equal(0, bytesRead);
    }

    [Fact]
    public void VarIntCodec_VarUInt64_TenthByte_ContinuationBit_Rejects()
    {
        byte[] malformed = [0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80];
        bool success = VarIntCodec.TryReadVarUInt64(malformed, out _, out int bytesRead);
        Assert.False(success);
        Assert.Equal(0, bytesRead);
    }

    [Fact]
    public void TryRead_ReturnsFalse_OnEmptySpan()
    {
        Assert.False(VarIntCodec.TryReadVarUInt32([], out _, out _));
        Assert.False(VarIntCodec.TryReadVarInt32([], out _, out _));
        Assert.False(VarIntCodec.TryReadVarUInt64([], out _, out _));
        Assert.False(VarIntCodec.TryReadVarInt64([], out _, out _));
    }
}
