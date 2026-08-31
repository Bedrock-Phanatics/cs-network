using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Text;
using CsNetwork.Types;

namespace CsNetwork.IO;

public ref struct BufferPacketWriter<TWriter> where TWriter : IBufferWriter<byte>
{
    private ref TWriter _output;
    private int _bytesWritten;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BufferPacketWriter(ref TWriter output)
    {
        _output = ref output;
        _bytesWritten = 0;
    }

    public readonly int BytesWritten => _bytesWritten;

    #region Primitives (Little Endian)

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteByte(byte value)
    {
        Span<byte> span = _output.GetSpan(1);
        span[0] = value;
        _output.Advance(1);
        _bytesWritten += 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteSByte(sbyte value) => WriteByte((byte)value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteBool(bool value) => WriteByte((byte)(value ? 1 : 0));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteInt16LE(short value)
    {
        Span<byte> span = _output.GetSpan(sizeof(short));
        BinaryPrimitives.WriteInt16LittleEndian(span, value);
        _output.Advance(sizeof(short));
        _bytesWritten += sizeof(short);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteUInt16LE(ushort value)
    {
        Span<byte> span = _output.GetSpan(sizeof(ushort));
        BinaryPrimitives.WriteUInt16LittleEndian(span, value);
        _output.Advance(sizeof(ushort));
        _bytesWritten += sizeof(ushort);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteInt32LE(int value)
    {
        Span<byte> span = _output.GetSpan(sizeof(int));
        BinaryPrimitives.WriteInt32LittleEndian(span, value);
        _output.Advance(sizeof(int));
        _bytesWritten += sizeof(int);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteUInt32LE(uint value)
    {
        Span<byte> span = _output.GetSpan(sizeof(uint));
        BinaryPrimitives.WriteUInt32LittleEndian(span, value);
        _output.Advance(sizeof(uint));
        _bytesWritten += sizeof(uint);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteInt64LE(long value)
    {
        Span<byte> span = _output.GetSpan(sizeof(long));
        BinaryPrimitives.WriteInt64LittleEndian(span, value);
        _output.Advance(sizeof(long));
        _bytesWritten += sizeof(long);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteUInt64LE(ulong value)
    {
        Span<byte> span = _output.GetSpan(sizeof(ulong));
        BinaryPrimitives.WriteUInt64LittleEndian(span, value);
        _output.Advance(sizeof(ulong));
        _bytesWritten += sizeof(ulong);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteFloat32LE(float value)
    {
        Span<byte> span = _output.GetSpan(sizeof(float));
        BinaryPrimitives.WriteSingleLittleEndian(span, value);
        _output.Advance(sizeof(float));
        _bytesWritten += sizeof(float);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteFloat64LE(double value)
    {
        Span<byte> span = _output.GetSpan(sizeof(double));
        BinaryPrimitives.WriteDoubleLittleEndian(span, value);
        _output.Advance(sizeof(double));
        _bytesWritten += sizeof(double);
    }

    #endregion

    #region Primitives (Big Endian)

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteInt16BE(short value)
    {
        Span<byte> span = _output.GetSpan(sizeof(short));
        BinaryPrimitives.WriteInt16BigEndian(span, value);
        _output.Advance(sizeof(short));
        _bytesWritten += sizeof(short);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteUInt16BE(ushort value)
    {
        Span<byte> span = _output.GetSpan(sizeof(ushort));
        BinaryPrimitives.WriteUInt16BigEndian(span, value);
        _output.Advance(sizeof(ushort));
        _bytesWritten += sizeof(ushort);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteInt32BE(int value)
    {
        Span<byte> span = _output.GetSpan(sizeof(int));
        BinaryPrimitives.WriteInt32BigEndian(span, value);
        _output.Advance(sizeof(int));
        _bytesWritten += sizeof(int);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteUInt32BE(uint value)
    {
        Span<byte> span = _output.GetSpan(sizeof(uint));
        BinaryPrimitives.WriteUInt32BigEndian(span, value);
        _output.Advance(sizeof(uint));
        _bytesWritten += sizeof(uint);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteInt64BE(long value)
    {
        Span<byte> span = _output.GetSpan(sizeof(long));
        BinaryPrimitives.WriteInt64BigEndian(span, value);
        _output.Advance(sizeof(long));
        _bytesWritten += sizeof(long);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteUInt64BE(ulong value)
    {
        Span<byte> span = _output.GetSpan(sizeof(ulong));
        BinaryPrimitives.WriteUInt64BigEndian(span, value);
        _output.Advance(sizeof(ulong));
        _bytesWritten += sizeof(ulong);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteFloat32BE(float value)
    {
        Span<byte> span = _output.GetSpan(sizeof(float));
        BinaryPrimitives.WriteSingleBigEndian(span, value);
        _output.Advance(sizeof(float));
        _bytesWritten += sizeof(float);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteFloat64BE(double value)
    {
        Span<byte> span = _output.GetSpan(sizeof(double));
        BinaryPrimitives.WriteDoubleBigEndian(span, value);
        _output.Advance(sizeof(double));
        _bytesWritten += sizeof(double);
    }

    #endregion

    #region VarInts

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteVarUInt32(uint value)
    {
        Span<byte> span = _output.GetSpan(5);
        int written = VarIntCodec.WriteVarUInt32(span, value);
        _output.Advance(written);
        _bytesWritten += written;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteVarInt32(int value) => WriteVarUInt32(VarIntCodec.EncodeZigZag32(value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteVarUInt64(ulong value)
    {
        Span<byte> span = _output.GetSpan(10);
        int written = VarIntCodec.WriteVarUInt64(span, value);
        _output.Advance(written);
        _bytesWritten += written;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteVarInt64(long value) => WriteVarUInt64(VarIntCodec.EncodeZigZag64(value));

    #endregion

    #region Strings and Byte Slices

    public void WriteString(ReadOnlySpan<char> value)
    {
        int byteCount = Encoding.UTF8.GetByteCount(value);
        WriteVarUInt32((uint)byteCount);
        Span<byte> span = _output.GetSpan(byteCount);
        Encoding.UTF8.GetBytes(value, span);
        _output.Advance(byteCount);
        _bytesWritten += byteCount;
    }

    public void WriteString(string value) => WriteString(value.AsSpan());

    public void WriteStringUtf8(ReadOnlySpan<byte> utf8Bytes)
    {
        WriteVarUInt32((uint)utf8Bytes.Length);
        Span<byte> span = _output.GetSpan(utf8Bytes.Length);
        utf8Bytes.CopyTo(span);
        _output.Advance(utf8Bytes.Length);
        _bytesWritten += utf8Bytes.Length;
    }

    public void WriteByteArray(ReadOnlySpan<byte> bytes) => WriteStringUtf8(bytes);

    public void WriteRawBytes(ReadOnlySpan<byte> bytes)
    {
        Span<byte> span = _output.GetSpan(bytes.Length);
        bytes.CopyTo(span);
        _output.Advance(bytes.Length);
        _bytesWritten += bytes.Length;
    }

    #endregion

    #region Structures

    public void WriteBlockPosition(in BlockPosition position)
    {
        WriteVarInt32(position.X);
        WriteVarUInt32((uint)position.Y);
        WriteVarInt32(position.Z);
    }

    public void WriteUuid(in Uuid uuid)
    {
        Span<byte> span = _output.GetSpan(16);
        _ = uuid.TryWrite(span[..16]);
        _output.Advance(16);
        _bytesWritten += 16;
    }

    #endregion
}
