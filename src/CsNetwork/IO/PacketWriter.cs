using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Text;
using CsNetwork.Types;

namespace CsNetwork.IO;

public ref struct PacketWriter
{
    private readonly Span<byte> _buffer;
    private int _bytesWritten;

    public PacketWriter(Span<byte> buffer)
    {
        _buffer = buffer;
        _bytesWritten = 0;
    }

    public readonly int BytesWritten => _bytesWritten;
    public readonly int Position => _bytesWritten;
    public readonly int Capacity => _buffer.Length;
    public readonly int FreeCapacity => _buffer.Length - _bytesWritten;
    public readonly int Remaining => FreeCapacity;
    public readonly ReadOnlySpan<byte> WrittenSpan => _buffer[.._bytesWritten];
    public readonly Span<byte> FreeSpan => _buffer[_bytesWritten..];
    public readonly Span<byte> UnwrittenSpan => FreeSpan;

    #region Primitives

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryWriteByte(byte value)
    {
        if (_bytesWritten >= _buffer.Length)
        {
            return false;
        }

        _buffer[_bytesWritten++] = value;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteByte(byte value)
    {
        if (!TryWriteByte(value))
        {
            throw new InvalidOperationException("Buffer too small for byte.");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryWriteSByte(sbyte value) => TryWriteByte((byte)value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteSByte(sbyte value) => WriteByte((byte)value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryWriteBool(bool value) => TryWriteByte(value ? (byte)1 : (byte)0);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteBool(bool value) => WriteByte(value ? (byte)1 : (byte)0);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryWriteInt16LE(short value)
    {
        if (FreeCapacity < sizeof(short))
        {
            return false;
        }

        BinaryPrimitives.WriteInt16LittleEndian(FreeSpan, value);
        _bytesWritten += sizeof(short);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteInt16LE(short value)
    {
        if (!TryWriteInt16LE(value))
        {
            throw new InvalidOperationException("Buffer too small for int16 LE.");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryWriteUInt16LE(ushort value)
    {
        if (FreeCapacity < sizeof(ushort))
        {
            return false;
        }

        BinaryPrimitives.WriteUInt16LittleEndian(FreeSpan, value);
        _bytesWritten += sizeof(ushort);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteUInt16LE(ushort value)
    {
        if (!TryWriteUInt16LE(value))
        {
            throw new InvalidOperationException("Buffer too small for uint16 LE.");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryWriteInt32LE(int value)
    {
        if (FreeCapacity < sizeof(int))
        {
            return false;
        }

        BinaryPrimitives.WriteInt32LittleEndian(FreeSpan, value);
        _bytesWritten += sizeof(int);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteInt32LE(int value)
    {
        if (!TryWriteInt32LE(value))
        {
            throw new InvalidOperationException("Buffer too small for int32 LE.");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryWriteUInt32LE(uint value)
    {
        if (FreeCapacity < sizeof(uint))
        {
            return false;
        }

        BinaryPrimitives.WriteUInt32LittleEndian(FreeSpan, value);
        _bytesWritten += sizeof(uint);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteUInt32LE(uint value)
    {
        if (!TryWriteUInt32LE(value))
        {
            throw new InvalidOperationException("Buffer too small for uint32 LE.");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryWriteInt64LE(long value)
    {
        if (FreeCapacity < sizeof(long))
        {
            return false;
        }

        BinaryPrimitives.WriteInt64LittleEndian(FreeSpan, value);
        _bytesWritten += sizeof(long);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteInt64LE(long value)
    {
        if (!TryWriteInt64LE(value))
        {
            throw new InvalidOperationException("Buffer too small for int64 LE.");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryWriteUInt64LE(ulong value)
    {
        if (FreeCapacity < sizeof(ulong))
        {
            return false;
        }

        BinaryPrimitives.WriteUInt64LittleEndian(FreeSpan, value);
        _bytesWritten += sizeof(ulong);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteUInt64LE(ulong value)
    {
        if (!TryWriteUInt64LE(value))
        {
            throw new InvalidOperationException("Buffer too small for uint64 LE.");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryWriteFloat32LE(float value)
    {
        if (FreeCapacity < sizeof(float))
        {
            return false;
        }

        BinaryPrimitives.WriteSingleLittleEndian(FreeSpan, value);
        _bytesWritten += sizeof(float);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteFloat32LE(float value)
    {
        if (!TryWriteFloat32LE(value))
        {
            throw new InvalidOperationException("Buffer too small for float32 LE.");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryWriteFloat64LE(double value)
    {
        if (FreeCapacity < sizeof(double))
        {
            return false;
        }

        BinaryPrimitives.WriteDoubleLittleEndian(FreeSpan, value);
        _bytesWritten += sizeof(double);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteFloat64LE(double value)
    {
        if (!TryWriteFloat64LE(value))
        {
            throw new InvalidOperationException("Buffer too small for float64 LE.");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryWriteInt16BE(short value)
    {
        if (FreeCapacity < sizeof(short))
        {
            return false;
        }

        BinaryPrimitives.WriteInt16BigEndian(FreeSpan, value);
        _bytesWritten += sizeof(short);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteInt16BE(short value)
    {
        if (!TryWriteInt16BE(value))
        {
            throw new InvalidOperationException("Buffer too small for int16 BE.");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryWriteUInt16BE(ushort value)
    {
        if (FreeCapacity < sizeof(ushort))
        {
            return false;
        }

        BinaryPrimitives.WriteUInt16BigEndian(FreeSpan, value);
        _bytesWritten += sizeof(ushort);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteUInt16BE(ushort value)
    {
        if (!TryWriteUInt16BE(value))
        {
            throw new InvalidOperationException("Buffer too small for uint16 BE.");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryWriteInt32BE(int value)
    {
        if (FreeCapacity < sizeof(int))
        {
            return false;
        }

        BinaryPrimitives.WriteInt32BigEndian(FreeSpan, value);
        _bytesWritten += sizeof(int);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteInt32BE(int value)
    {
        if (!TryWriteInt32BE(value))
        {
            throw new InvalidOperationException("Buffer too small for int32 BE.");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryWriteUInt32BE(uint value)
    {
        if (FreeCapacity < sizeof(uint))
        {
            return false;
        }

        BinaryPrimitives.WriteUInt32BigEndian(FreeSpan, value);
        _bytesWritten += sizeof(uint);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteUInt32BE(uint value)
    {
        if (!TryWriteUInt32BE(value))
        {
            throw new InvalidOperationException("Buffer too small for uint32 BE.");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryWriteInt64BE(long value)
    {
        if (FreeCapacity < sizeof(long))
        {
            return false;
        }

        BinaryPrimitives.WriteInt64BigEndian(FreeSpan, value);
        _bytesWritten += sizeof(long);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteInt64BE(long value)
    {
        if (!TryWriteInt64BE(value))
        {
            throw new InvalidOperationException("Buffer too small for int64 BE.");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryWriteUInt64BE(ulong value)
    {
        if (FreeCapacity < sizeof(ulong))
        {
            return false;
        }

        BinaryPrimitives.WriteUInt64BigEndian(FreeSpan, value);
        _bytesWritten += sizeof(ulong);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteUInt64BE(ulong value)
    {
        if (!TryWriteUInt64BE(value))
        {
            throw new InvalidOperationException("Buffer too small for uint64 BE.");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryWriteFloat32BE(float value)
    {
        if (FreeCapacity < sizeof(float))
        {
            return false;
        }

        BinaryPrimitives.WriteSingleBigEndian(FreeSpan, value);
        _bytesWritten += sizeof(float);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteFloat32BE(float value)
    {
        if (!TryWriteFloat32BE(value))
        {
            throw new InvalidOperationException("Buffer too small for float32 BE.");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryWriteFloat64BE(double value)
    {
        if (FreeCapacity < sizeof(double))
        {
            return false;
        }

        BinaryPrimitives.WriteDoubleBigEndian(FreeSpan, value);
        _bytesWritten += sizeof(double);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteFloat64BE(double value)
    {
        if (!TryWriteFloat64BE(value))
        {
            throw new InvalidOperationException("Buffer too small for float64 BE.");
        }
    }

    #endregion

    #region VarInts

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryWriteVarUInt32(uint value)
    {
        int written = VarIntCodec.WriteVarUInt32(FreeSpan, value);
        if (written > 0)
        {
            _bytesWritten += written;
            return true;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteVarUInt32(uint value)
    {
        if (!TryWriteVarUInt32(value))
        {
            throw new InvalidOperationException("Buffer too small for varuint32.");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryWriteVarInt32(int value)
    {
        int written = VarIntCodec.WriteVarInt32(FreeSpan, value);
        if (written > 0)
        {
            _bytesWritten += written;
            return true;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteVarInt32(int value)
    {
        if (!TryWriteVarInt32(value))
        {
            throw new InvalidOperationException("Buffer too small for varint32.");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryWriteVarUInt64(ulong value)
    {
        int written = VarIntCodec.WriteVarUInt64(FreeSpan, value);
        if (written > 0)
        {
            _bytesWritten += written;
            return true;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteVarUInt64(ulong value)
    {
        if (!TryWriteVarUInt64(value))
        {
            throw new InvalidOperationException("Buffer too small for varuint64.");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryWriteVarInt64(long value)
    {
        int written = VarIntCodec.WriteVarInt64(FreeSpan, value);
        if (written > 0)
        {
            _bytesWritten += written;
            return true;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteVarInt64(long value)
    {
        if (!TryWriteVarInt64(value))
        {
            throw new InvalidOperationException("Buffer too small for varint64.");
        }
    }

    #endregion

    #region Strings and Byte Slices

    public bool TryWriteString(ReadOnlySpan<char> value)
    {
        int byteCount = Encoding.UTF8.GetByteCount(value);
        int lenVarIntBytes = VarIntCodec.GetVarUInt32ByteCount((uint)byteCount);

        if (FreeCapacity < lenVarIntBytes + byteCount)
        {
            return false;
        }

        _ = TryWriteVarUInt32((uint)byteCount);
        Encoding.UTF8.GetBytes(value, FreeSpan);
        _bytesWritten += byteCount;
        return true;
    }

    public void WriteString(ReadOnlySpan<char> value)
    {
        if (!TryWriteString(value))
        {
            throw new InvalidOperationException("Buffer too small for string.");
        }
    }

    public bool TryWriteString(string value) => TryWriteString(value.AsSpan());

    public void WriteString(string value) => WriteString(value.AsSpan());

    public bool TryWriteStringUtf8(ReadOnlySpan<byte> utf8Bytes)
    {
        int lenVarIntBytes = VarIntCodec.GetVarUInt32ByteCount((uint)utf8Bytes.Length);
        if (FreeCapacity < lenVarIntBytes + utf8Bytes.Length)
        {
            return false;
        }

        _ = TryWriteVarUInt32((uint)utf8Bytes.Length);
        utf8Bytes.CopyTo(FreeSpan);
        _bytesWritten += utf8Bytes.Length;
        return true;
    }

    public void WriteStringUtf8(ReadOnlySpan<byte> utf8Bytes)
    {
        if (!TryWriteStringUtf8(utf8Bytes))
        {
            throw new InvalidOperationException("Buffer too small for UTF-8 string bytes.");
        }
    }

    public bool TryWriteByteArray(ReadOnlySpan<byte> bytes) => TryWriteStringUtf8(bytes);

    public void WriteByteArray(ReadOnlySpan<byte> bytes) => WriteStringUtf8(bytes);

    public bool TryWriteRawBytes(ReadOnlySpan<byte> bytes)
    {
        if (FreeCapacity < bytes.Length)
        {
            return false;
        }

        bytes.CopyTo(FreeSpan);
        _bytesWritten += bytes.Length;
        return true;
    }

    public void WriteRawBytes(ReadOnlySpan<byte> bytes)
    {
        if (!TryWriteRawBytes(bytes))
        {
            throw new InvalidOperationException("Buffer too small for raw bytes.");
        }
    }

    #endregion

    #region Structures

    public bool TryWriteBlockPosition(in BlockPosition position)
    {
        int required = VarIntCodec.GetVarInt32ByteCount(position.X) +
                       VarIntCodec.GetVarInt32ByteCount(position.Y) +
                       VarIntCodec.GetVarInt32ByteCount(position.Z);

        if (FreeCapacity < required)
        {
            return false;
        }

        return TryWriteVarInt32(position.X) &&
               TryWriteVarUInt32((uint)position.Y) &&
               TryWriteVarInt32(position.Z);
    }

    public void WriteBlockPosition(in BlockPosition position)
    {
        if (!TryWriteBlockPosition(position))
        {
            throw new InvalidOperationException("Buffer too small for BlockPosition.");
        }
    }

    public bool TryWriteUuid(in Uuid uuid)
    {
        if (FreeCapacity < 16)
        {
            return false;
        }

        if (uuid.TryWrite(FreeSpan[..16]))
        {
            _bytesWritten += 16;
            return true;
        }

        return false;
    }

    public void WriteUuid(in Uuid uuid)
    {
        if (!TryWriteUuid(uuid))
        {
            throw new InvalidOperationException("Buffer too small for Uuid.");
        }
    }

    #endregion
}
