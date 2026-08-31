using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace CsNetwork.IO;

public static class VarIntCodec
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint EncodeZigZag32(int value) => (uint)((value << 1) ^ (value >> 31));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int DecodeZigZag32(uint value) => (int)(value >> 1) ^ -(int)(value & 1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong EncodeZigZag64(long value) => (ulong)((value << 1) ^ (value >> 63));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long DecodeZigZag64(ulong value) => (long)(value >> 1) ^ -(long)(value & 1UL);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetVarUInt32ByteCount(uint value)
    {
        if (value == 0)
            return 1;

        // compute ceil(bits / 7) via lzcnt intrinsic
        int bits = 32 - BitOperations.LeadingZeroCount(value);
        return (bits + 6) / 7;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetVarInt32ByteCount(int value) =>
        GetVarUInt32ByteCount(EncodeZigZag32(value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetVarUInt64ByteCount(ulong value)
    {
        if (value == 0)
            return 1;

        int bits = 64 - BitOperations.LeadingZeroCount(value);
        return (bits + 6) / 7;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetVarInt64ByteCount(long value) =>
        GetVarUInt64ByteCount(EncodeZigZag64(value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int WriteVarUInt32(Span<byte> destination, uint value)
    {
        int count = GetVarUInt32ByteCount(value);
        if (destination.Length < count)
            return 0;

        // unrolled fast path for small numbers common in network headers
        if ((value & ~0x7Fu) == 0)
        {
            destination[0] = (byte)value;
            return 1;
        }

        if ((value & ~0x3FFFu) == 0)
        {
            destination[0] = (byte)(value | 0x80);
            destination[1] = (byte)(value >> 7);
            return 2;
        }

        if ((value & ~0x1FFFFFu) == 0)
        {
            destination[0] = (byte)(value | 0x80);
            destination[1] = (byte)((value >> 7) | 0x80);
            destination[2] = (byte)(value >> 14);
            return 3;
        }

        if ((value & ~0x0FFFFFFFu) == 0)
        {
            destination[0] = (byte)(value | 0x80);
            destination[1] = (byte)((value >> 7) | 0x80);
            destination[2] = (byte)((value >> 14) | 0x80);
            destination[3] = (byte)(value >> 21);
            return 4;
        }

        destination[0] = (byte)(value | 0x80);
        destination[1] = (byte)((value >> 7) | 0x80);
        destination[2] = (byte)((value >> 14) | 0x80);
        destination[3] = (byte)((value >> 21) | 0x80);
        destination[4] = (byte)(value >> 28);
        return 5;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int WriteVarInt32(Span<byte> destination, int value) =>
        WriteVarUInt32(destination, EncodeZigZag32(value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryReadVarUInt32(ReadOnlySpan<byte> source, out uint value, out int bytesRead)
    {
        uint result = 0;
        int shift = 0;
        int max = Math.Min(source.Length, 5);

        for (int i = 0; i < max; i++)
        {
            byte b = source[i];
            if (i == 4)
            {
                // 5th byte can only hold 4 bits (bits 28..31) and no msb continuation
                if ((b & 0xF0) != 0)
                {
                    value = 0;
                    bytesRead = 0;
                    return false;
                }

                result |= (uint)b << shift;
                value = result;
                bytesRead = 5;
                return true;
            }

            result |= (uint)(b & 0x7F) << shift;
            if ((b & 0x80) == 0)
            {
                value = result;
                bytesRead = i + 1;
                return true;
            }

            shift += 7;
        }

        value = 0;
        bytesRead = 0;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryReadVarInt32(ReadOnlySpan<byte> source, out int value, out int bytesRead)
    {
        if (TryReadVarUInt32(source, out uint raw, out bytesRead))
        {
            value = DecodeZigZag32(raw);
            return true;
        }

        value = 0;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int WriteVarUInt64(Span<byte> destination, ulong value)
    {
        int required = GetVarUInt64ByteCount(value);
        if (destination.Length < required)
            return 0;

        int index = 0;
        while (value >= 0x80UL)
        {
            destination[index++] = (byte)(value | 0x80UL);
            value >>= 7;
        }

        destination[index++] = (byte)value;
        return index;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int WriteVarInt64(Span<byte> destination, long value) =>
        WriteVarUInt64(destination, EncodeZigZag64(value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryReadVarUInt64(ReadOnlySpan<byte> source, out ulong value, out int bytesRead)
    {
        ulong result = 0;
        int shift = 0;
        int max = Math.Min(source.Length, 10);

        for (int i = 0; i < max; i++)
        {
            byte b = source[i];
            if (i == 9)
            {
                // 10th byte max 1 bit since 9*7=63 bits are already read
                if ((b & 0xFE) != 0)
                {
                    value = 0;
                    bytesRead = 0;
                    return false;
                }

                result |= (ulong)(b & 0x01) << shift;
                value = result;
                bytesRead = 10;
                return true;
            }

            result |= (ulong)(b & 0x7F) << shift;
            if ((b & 0x80) == 0)
            {
                value = result;
                bytesRead = i + 1;
                return true;
            }

            shift += 7;
        }

        value = 0;
        bytesRead = 0;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryReadVarInt64(ReadOnlySpan<byte> source, out long value, out int bytesRead)
    {
        if (TryReadVarUInt64(source, out ulong raw, out bytesRead))
        {
            value = DecodeZigZag64(raw);
            return true;
        }

        value = 0;
        return false;
    }
}
