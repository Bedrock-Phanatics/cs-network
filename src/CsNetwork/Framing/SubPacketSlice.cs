using System;

namespace CsNetwork.Framing;

public readonly record struct SubPacketSlice(SubPacketHeader Header, ReadOnlyMemory<byte> Payload)
{
    public int Length => Payload.Length;

    public ReadOnlySpan<byte> Span => Payload.Span;
}
