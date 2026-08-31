using System;
using CsNetwork.IO;

namespace CsNetwork.Framing;

public ref struct BatchFrameReader
{
    private readonly ReadOnlySpan<byte> _buffer;
    private int _position;

    public BatchFrameReader(ReadOnlySpan<byte> buffer)
    {
        _buffer = buffer;
        _position = 0;
    }

    public readonly int Position => _position;
    public readonly int Remaining => _buffer.Length - _position;
    public readonly bool IsEmpty => _position >= _buffer.Length;

    public bool TryReadNext(out SubPacketHeader header, out ReadOnlySpan<byte> payload)
    {
        header = default;
        payload = default;

        if (_position >= _buffer.Length)
            return false;

        ReadOnlySpan<byte> remaining = _buffer[_position..];
        if (!VarIntCodec.TryReadVarUInt32(remaining, out uint sliceLength, out int lenBytes))
            return false;

        _position += lenBytes;
        if ((uint)_buffer.Length - (uint)_position < sliceLength)
            return false;

        ReadOnlySpan<byte> sliceSpan = _buffer.Slice(_position, (int)sliceLength);
        if (!VarIntCodec.TryReadVarUInt32(sliceSpan, out uint rawHeader, out int headerLenBytes))
            return false;

        header = SubPacketHeader.FromRawHeader(rawHeader);
        payload = sliceSpan[headerLenBytes..];
        _position += (int)sliceLength;

        return true;
    }
}
