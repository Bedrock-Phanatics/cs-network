using System;
using System.Text;
using CsNetwork.IO;
using CsNetwork.Types;
using Xunit;

namespace CsNetwork.Tests.IO;

public sealed class PacketReaderAndWriterTests
{
    [Fact]
    public void Primitives_Roundtrip()
    {
        byte[] backing = new byte[256];
        var writer = new PacketWriter(backing);

        writer.WriteByte(0x42);
        writer.WriteSByte(-42);
        writer.WriteBool(true);
        writer.WriteBool(false);
        writer.WriteInt16LE(-1234);
        writer.WriteUInt16LE(54321);
        writer.WriteInt32LE(-987654);
        writer.WriteUInt32LE(3000000000u);
        writer.WriteInt64LE(-1234567890123456L);
        writer.WriteUInt64LE(12345678901234567890UL);
        writer.WriteFloat32LE(3.14159f);
        writer.WriteFloat64LE(2.718281828459045);

        writer.WriteInt16BE(-1234);
        writer.WriteUInt16BE(54321);
        writer.WriteInt32BE(-987654);
        writer.WriteUInt32BE(3000000000u);
        writer.WriteInt64BE(-1234567890123456L);
        writer.WriteUInt64BE(12345678901234567890UL);
        writer.WriteFloat32BE(3.14159f);
        writer.WriteFloat64BE(2.718281828459045);

        var reader = new PacketReader(writer.WrittenSpan);

        Assert.True(reader.TryReadByte(out byte b));
        Assert.Equal(0x42, b);

        Assert.True(reader.TryReadSByte(out sbyte sb));
        Assert.Equal(-42, sb);

        Assert.True(reader.TryReadBool(out bool b1));
        Assert.True(b1);

        Assert.True(reader.TryReadBool(out bool b2));
        Assert.False(b2);

        Assert.True(reader.TryReadInt16LE(out short s));
        Assert.Equal(-1234, s);

        Assert.True(reader.TryReadUInt16LE(out ushort us));
        Assert.Equal(54321, us);

        Assert.True(reader.TryReadInt32LE(out int i));
        Assert.Equal(-987654, i);

        Assert.True(reader.TryReadUInt32LE(out uint ui));
        Assert.Equal(3000000000u, ui);

        Assert.True(reader.TryReadInt64LE(out long l));
        Assert.Equal(-1234567890123456L, l);

        Assert.True(reader.TryReadUInt64LE(out ulong ul));
        Assert.Equal(12345678901234567890UL, ul);

        Assert.True(reader.TryReadFloat32LE(out float f));
        Assert.Equal(3.14159f, f);

        Assert.True(reader.TryReadFloat64LE(out double d));
        Assert.Equal(2.718281828459045, d);

        Assert.True(reader.TryReadInt16BE(out short sBE));
        Assert.Equal(-1234, sBE);

        Assert.True(reader.TryReadUInt16BE(out ushort usBE));
        Assert.Equal(54321, usBE);

        Assert.True(reader.TryReadInt32BE(out int iBE));
        Assert.Equal(-987654, iBE);

        Assert.True(reader.TryReadUInt32BE(out uint uiBE));
        Assert.Equal(3000000000u, uiBE);

        Assert.True(reader.TryReadInt64BE(out long lBE));
        Assert.Equal(-1234567890123456L, lBE);

        Assert.True(reader.TryReadUInt64BE(out ulong ulBE));
        Assert.Equal(12345678901234567890UL, ulBE);

        Assert.True(reader.TryReadFloat32BE(out float fBE));
        Assert.Equal(3.14159f, fBE);

        Assert.True(reader.TryReadFloat64BE(out double dBE));
        Assert.Equal(2.718281828459045, dBE);

        Assert.Equal(0, reader.Remaining);
    }

    [Fact]
    public void String_Roundtrip_UTF8()
    {
        string original = "Hello Minecraft Bedrock! \u00a7aColor \u2764 \ud83d\ude80";
        Span<byte> buffer = stackalloc byte[256];

        var writer = new PacketWriter(buffer);
        writer.WriteString(original);

        var reader = new PacketReader(writer.WrittenSpan);
        Assert.True(reader.TryReadString(out string? result));
        Assert.Equal(original, result);
        Assert.Equal(0, reader.Remaining);
    }

    [Fact]
    public void String_ZeroAllocation_Utf8Span_Roundtrip()
    {
        ReadOnlySpan<byte> utf8Input = "ZeroAllocationBedrock"u8;
        Span<byte> buffer = stackalloc byte[64];

        var writer = new PacketWriter(buffer);
        writer.WriteStringUtf8(utf8Input);

        var reader = new PacketReader(writer.WrittenSpan);
        Assert.True(reader.TryReadStringUtf8(out ReadOnlySpan<byte> readUtf8));
        Assert.True(utf8Input.SequenceEqual(readUtf8));
        Assert.Equal(0, reader.Remaining);
    }

    [Fact]
    public void ByteArray_Roundtrip()
    {
        byte[] payload = [0x01, 0x02, 0x03, 0x04, 0x05, 0xAA, 0xBB, 0xCC];
        Span<byte> buffer = stackalloc byte[64];

        var writer = new PacketWriter(buffer);
        writer.WriteByteArray(payload);

        var reader = new PacketReader(writer.WrittenSpan);
        Assert.True(reader.TryReadByteArray(out ReadOnlySpan<byte> readBytes));
        Assert.True(payload.AsSpan().SequenceEqual(readBytes));
        Assert.Equal(0, reader.Remaining);
    }

    [Fact]
    public void String_Exceeding_MaxLength_Fails()
    {
        string longStr = new string('A', 100);
        Span<byte> buffer = stackalloc byte[200];

        var writer = new PacketWriter(buffer);
        writer.WriteString(longStr);

        var reader = new PacketReader(writer.WrittenSpan);
        Assert.False(reader.TryReadString(out string? _, maxLength: 50));
    }

    [Fact]
    public void BufferUnderflow_ReturnsFalse_NeverThrows()
    {
        byte[] shortBuf = [0x01];
        var reader = new PacketReader(shortBuf);

        Assert.False(reader.TryReadInt16LE(out _));
        Assert.False(reader.TryReadInt32LE(out _));
        Assert.False(reader.TryReadInt64LE(out _));
        Assert.False(reader.TryReadFloat32LE(out _));
        Assert.False(reader.TryReadFloat64LE(out _));
        Assert.False(reader.TryReadUuid(out _));
        Assert.False(reader.TryReadBlockPosition(out _));
    }

    [Fact]
    public void BufferPacketWriter_WithArrayBufferWriter_MatchesPacketWriter()
    {
        var arrayWriter = new System.Buffers.ArrayBufferWriter<byte>();
        var writer = new BufferPacketWriter<System.Buffers.ArrayBufferWriter<byte>>(ref arrayWriter);

        writer.WriteString("Streaming Bedrock Data");
        writer.WriteVarUInt32(999999);
        writer.WriteBlockPosition(new BlockPosition(10, 20, 30));
        writer.WriteUuid(new Uuid(Guid.Parse("12345678-1234-1234-1234-123456789abc")));

        var reader = new PacketReader(arrayWriter.WrittenSpan);
        Assert.True(reader.TryReadString(out string? str));
        Assert.Equal("Streaming Bedrock Data", str);

        Assert.True(reader.TryReadVarUInt32(out uint val));
        Assert.Equal(999999u, val);

        Assert.True(reader.TryReadBlockPosition(out BlockPosition pos));
        Assert.Equal(new BlockPosition(10, 20, 30), pos);

        Assert.True(reader.TryReadUuid(out Uuid uuid));
        Assert.Equal(Guid.Parse("12345678-1234-1234-1234-123456789abc"), uuid.Value);

        Assert.Equal(0, reader.Remaining);
    }
}
