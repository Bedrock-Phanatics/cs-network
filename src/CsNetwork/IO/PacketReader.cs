using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Text;
using CsNetwork.Types;

namespace CsNetwork.IO;

public ref struct PacketReader
{
    private readonly ReadOnlySpan<byte> _buffer;
    private int _position;

    public PacketReader(ReadOnlySpan<byte> buffer)
    {
        _buffer = buffer;
        _position = 0;
    }

    public readonly int Position => _position;
    public readonly int Remaining => _buffer.Length - _position;
    public readonly ReadOnlySpan<byte> UnreadSpan => _buffer[_position..];

    #region Primitive Readers

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadByte(out byte value)
    {
        if (_position >= _buffer.Length)
        {
            value = 0;
            return false;
        }

        value = _buffer[_position++];
        return true;
    }

    public byte ReadByte()
    {
        if (!TryReadByte(out byte value))
            throw new InvalidOperationException("Buffer underflow reading byte.");
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadSByte(out sbyte value)
    {
        if (TryReadByte(out byte b))
        {
            value = (sbyte)b;
            return true;
        }

        value = 0;
        return false;
    }

    public sbyte ReadSByte() => (sbyte)ReadByte();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadBool(out bool value)
    {
        if (TryReadByte(out byte b))
        {
            value = b != 0;
            return true;
        }

        value = false;
        return false;
    }

    public bool ReadBool() => ReadByte() != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadInt16LE(out short value)
    {
        if (Remaining < sizeof(short))
        {
            value = 0;
            return false;
        }

        value = BinaryPrimitives.ReadInt16LittleEndian(UnreadSpan);
        _position += sizeof(short);
        return true;
    }

    public short ReadInt16LE()
    {
        if (!TryReadInt16LE(out short value))
            throw new InvalidOperationException("Buffer underflow reading Int16 LE.");
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadUInt16LE(out ushort value)
    {
        if (Remaining < sizeof(ushort))
        {
            value = 0;
            return false;
        }

        value = BinaryPrimitives.ReadUInt16LittleEndian(UnreadSpan);
        _position += sizeof(ushort);
        return true;
    }

    public ushort ReadUInt16LE()
    {
        if (!TryReadUInt16LE(out ushort value))
            throw new InvalidOperationException("Buffer underflow reading UInt16 LE.");
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadInt32LE(out int value)
    {
        if (Remaining < sizeof(int))
        {
            value = 0;
            return false;
        }

        value = BinaryPrimitives.ReadInt32LittleEndian(UnreadSpan);
        _position += sizeof(int);
        return true;
    }

    public int ReadInt32LE()
    {
        if (!TryReadInt32LE(out int value))
            throw new InvalidOperationException("Buffer underflow reading Int32 LE.");
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadUInt32LE(out uint value)
    {
        if (Remaining < sizeof(uint))
        {
            value = 0;
            return false;
        }

        value = BinaryPrimitives.ReadUInt32LittleEndian(UnreadSpan);
        _position += sizeof(uint);
        return true;
    }

    public uint ReadUInt32LE()
    {
        if (!TryReadUInt32LE(out uint value))
            throw new InvalidOperationException("Buffer underflow reading UInt32 LE.");
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadInt64LE(out long value)
    {
        if (Remaining < sizeof(long))
        {
            value = 0;
            return false;
        }

        value = BinaryPrimitives.ReadInt64LittleEndian(UnreadSpan);
        _position += sizeof(long);
        return true;
    }

    public long ReadInt64LE()
    {
        if (!TryReadInt64LE(out long value))
            throw new InvalidOperationException("Buffer underflow reading Int64 LE.");
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadUInt64LE(out ulong value)
    {
        if (Remaining < sizeof(ulong))
        {
            value = 0;
            return false;
        }

        value = BinaryPrimitives.ReadUInt64LittleEndian(UnreadSpan);
        _position += sizeof(ulong);
        return true;
    }

    public ulong ReadUInt64LE()
    {
        if (!TryReadUInt64LE(out ulong value))
            throw new InvalidOperationException("Buffer underflow reading UInt64 LE.");
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadFloat32LE(out float value)
    {
        if (Remaining < sizeof(float))
        {
            value = 0;
            return false;
        }

        value = BinaryPrimitives.ReadSingleLittleEndian(UnreadSpan);
        _position += sizeof(float);
        return true;
    }

    public float ReadFloat32LE()
    {
        if (!TryReadFloat32LE(out float value))
            throw new InvalidOperationException("Buffer underflow reading float LE.");
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadFloat64LE(out double value)
    {
        if (Remaining < sizeof(double))
        {
            value = 0;
            return false;
        }

        value = BinaryPrimitives.ReadDoubleLittleEndian(UnreadSpan);
        _position += sizeof(double);
        return true;
    }

    public double ReadFloat64LE()
    {
        if (!TryReadFloat64LE(out double value))
            throw new InvalidOperationException("Buffer underflow reading double LE.");
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadInt16BE(out short value)
    {
        if (Remaining < sizeof(short))
        {
            value = 0;
            return false;
        }

        value = BinaryPrimitives.ReadInt16BigEndian(UnreadSpan);
        _position += sizeof(short);
        return true;
    }

    public short ReadInt16BE()
    {
        if (!TryReadInt16BE(out short value))
            throw new InvalidOperationException("Buffer underflow reading Int16 BE.");
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadUInt16BE(out ushort value)
    {
        if (Remaining < sizeof(ushort))
        {
            value = 0;
            return false;
        }

        value = BinaryPrimitives.ReadUInt16BigEndian(UnreadSpan);
        _position += sizeof(ushort);
        return true;
    }

    public ushort ReadUInt16BE()
    {
        if (!TryReadUInt16BE(out ushort value))
            throw new InvalidOperationException("Buffer underflow reading UInt16 BE.");
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadInt32BE(out int value)
    {
        if (Remaining < sizeof(int))
        {
            value = 0;
            return false;
        }

        value = BinaryPrimitives.ReadInt32BigEndian(UnreadSpan);
        _position += sizeof(int);
        return true;
    }

    public int ReadInt32BE()
    {
        if (!TryReadInt32BE(out int value))
            throw new InvalidOperationException("Buffer underflow reading Int32 BE.");
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadUInt32BE(out uint value)
    {
        if (Remaining < sizeof(uint))
        {
            value = 0;
            return false;
        }

        value = BinaryPrimitives.ReadUInt32BigEndian(UnreadSpan);
        _position += sizeof(uint);
        return true;
    }

    public uint ReadUInt32BE()
    {
        if (!TryReadUInt32BE(out uint value))
            throw new InvalidOperationException("Buffer underflow reading UInt32 BE.");
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadInt64BE(out long value)
    {
        if (Remaining < sizeof(long))
        {
            value = 0;
            return false;
        }

        value = BinaryPrimitives.ReadInt64BigEndian(UnreadSpan);
        _position += sizeof(long);
        return true;
    }

    public long ReadInt64BE()
    {
        if (!TryReadInt64BE(out long value))
            throw new InvalidOperationException("Buffer underflow reading Int64 BE.");
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadUInt64BE(out ulong value)
    {
        if (Remaining < sizeof(ulong))
        {
            value = 0;
            return false;
        }

        value = BinaryPrimitives.ReadUInt64BigEndian(UnreadSpan);
        _position += sizeof(ulong);
        return true;
    }

    public ulong ReadUInt64BE()
    {
        if (!TryReadUInt64BE(out ulong value))
            throw new InvalidOperationException("Buffer underflow reading UInt64 BE.");
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadFloat32BE(out float value)
    {
        if (Remaining < sizeof(float))
        {
            value = 0;
            return false;
        }

        value = BinaryPrimitives.ReadSingleBigEndian(UnreadSpan);
        _position += sizeof(float);
        return true;
    }

    public float ReadFloat32BE()
    {
        if (!TryReadFloat32BE(out float value))
            throw new InvalidOperationException("Buffer underflow reading float BE.");
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadFloat64BE(out double value)
    {
        if (Remaining < sizeof(double))
        {
            value = 0;
            return false;
        }

        value = BinaryPrimitives.ReadDoubleBigEndian(UnreadSpan);
        _position += sizeof(double);
        return true;
    }

    public double ReadFloat64BE()
    {
        if (!TryReadFloat64BE(out double value))
            throw new InvalidOperationException("Buffer underflow reading double BE.");
        return value;
    }

    #endregion

    #region VarInts

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadVarUInt32(out uint value)
    {
        if (VarIntCodec.TryReadVarUInt32(UnreadSpan, out value, out int bytesRead))
        {
            _position += bytesRead;
            return true;
        }

        return false;
    }

    public uint ReadVarUInt32()
    {
        if (!TryReadVarUInt32(out uint value))
            throw new InvalidOperationException("Buffer underflow reading VarUInt32.");
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadVarInt32(out int value)
    {
        if (VarIntCodec.TryReadVarInt32(UnreadSpan, out value, out int bytesRead))
        {
            _position += bytesRead;
            return true;
        }

        return false;
    }

    public int ReadVarInt32()
    {
        if (!TryReadVarInt32(out int value))
            throw new InvalidOperationException("Buffer underflow reading VarInt32.");
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadVarUInt64(out ulong value)
    {
        if (VarIntCodec.TryReadVarUInt64(UnreadSpan, out value, out int bytesRead))
        {
            _position += bytesRead;
            return true;
        }

        return false;
    }

    public ulong ReadVarUInt64()
    {
        if (!TryReadVarUInt64(out ulong value))
            throw new InvalidOperationException("Buffer underflow reading VarUInt64.");
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadVarInt64(out long value)
    {
        if (VarIntCodec.TryReadVarInt64(UnreadSpan, out value, out int bytesRead))
        {
            _position += bytesRead;
            return true;
        }

        return false;
    }

    public long ReadVarInt64()
    {
        if (!TryReadVarInt64(out long value))
            throw new InvalidOperationException("Buffer underflow reading VarInt64.");
        return value;
    }

    #endregion

    #region Strings and Byte Slices

    public bool TryReadString(out string? value, int maxLength = 32768)
    {
        int startPos = _position;
        if (!TryReadVarUInt32(out uint length) || length > (uint)maxLength || Remaining < (int)length)
        {
            _position = startPos;
            value = null;
            return false;
        }

        if (length == 0)
        {
            value = string.Empty;
            return true;
        }

        ReadOnlySpan<byte> strBytes = _buffer.Slice(_position, (int)length);
        _position += (int)length;

        try
        {
            value = Encoding.UTF8.GetString(strBytes);
            return true;
        }
        catch (DecoderFallbackException)
        {
            _position = startPos;
            value = null;
            return false;
        }
    }

    public string ReadString(int maxLength = 32768)
    {
        if (!TryReadString(out string? value, maxLength) || value == null)
            throw new InvalidOperationException("Buffer underflow reading string.");
        return value;
    }

    public bool TryReadString(Span<char> destination, out int charsWritten, int maxLength = 32768)
    {
        charsWritten = 0;
        int startPos = _position;
        if (!TryReadVarUInt32(out uint length) || length > (uint)maxLength || Remaining < (int)length)
        {
            _position = startPos;
            return false;
        }

        if (length == 0)
        {
            return true;
        }

        ReadOnlySpan<byte> strBytes = _buffer.Slice(_position, (int)length);
        if (Encoding.UTF8.GetMaxCharCount((int)length) > destination.Length)
        {
            int actualCharCount = Encoding.UTF8.GetCharCount(strBytes);
            if (actualCharCount > destination.Length)
            {
                _position = startPos;
                return false;
            }
        }

        if (Encoding.UTF8.TryGetChars(strBytes, destination, out charsWritten))
        {
            _position += (int)length;
            return true;
        }

        _position = startPos;
        charsWritten = 0;
        return false;
    }

    public bool TryReadStringUtf8(out ReadOnlySpan<byte> utf8Bytes, int maxLength = 32768)
    {
        int startPos = _position;
        if (!TryReadVarUInt32(out uint length) || length > (uint)maxLength || Remaining < (int)length)
        {
            _position = startPos;
            utf8Bytes = default;
            return false;
        }

        utf8Bytes = _buffer.Slice(_position, (int)length);
        _position += (int)length;
        return true;
    }

    public bool TryReadByteArray(out ReadOnlySpan<byte> bytes, int maxLength = int.MaxValue)
    {
        int startPos = _position;
        if (!TryReadVarUInt32(out uint length) || length > (uint)maxLength || Remaining < (int)length)
        {
            _position = startPos;
            bytes = default;
            return false;
        }

        bytes = _buffer.Slice(_position, (int)length);
        _position += (int)length;
        return true;
    }

    public bool TryReadRawBytes(int count, out ReadOnlySpan<byte> bytes)
    {
        if (count < 0 || Remaining < count)
        {
            bytes = default;
            return false;
        }

        bytes = _buffer.Slice(_position, count);
        _position += count;
        return true;
    }

    public bool TryReadRawBytes(Span<byte> destination)
    {
        if (Remaining < destination.Length)
        {
            return false;
        }

        _buffer.Slice(_position, destination.Length).CopyTo(destination);
        _position += destination.Length;
        return true;
    }

    #endregion

    #region Structures

    public bool TryReadBlockPosition(out BlockPosition position)
    {
        int startPos = _position;
        if (TryReadVarInt32(out int x) &&
            TryReadVarUInt32(out uint y) &&
            TryReadVarInt32(out int z))
        {
            position = new BlockPosition(x, (int)y, z);
            return true;
        }

        _position = startPos;
        position = default;
        return false;
    }

    public BlockPosition ReadBlockPosition()
    {
        if (!TryReadBlockPosition(out var pos))
            throw new InvalidOperationException("Buffer underflow reading BlockPosition.");
        return pos;
    }

    public bool TryReadUuid(out Uuid uuid)
    {
        if (Remaining < 16)
        {
            uuid = default;
            return false;
        }

        if (Uuid.TryRead(UnreadSpan[..16], out uuid))
        {
            _position += 16;
            return true;
        }

        return false;
    }

    public Uuid ReadUuid()
    {
        if (!TryReadUuid(out var uuid))
            throw new InvalidOperationException("Buffer underflow reading Uuid.");
        return uuid;
    }

    #endregion
}
