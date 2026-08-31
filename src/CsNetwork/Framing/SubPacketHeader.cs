using System;
using CsNetwork.IO;

namespace CsNetwork.Framing;

public readonly record struct SubPacketHeader(uint PacketId, byte SenderSubClientId = 0, byte TargetSubClientId = 0)
{
    public const uint PacketIdMask = 0x3FF;
    public const uint SenderSubClientIdMask = 0x03;
    public const uint TargetSubClientIdMask = 0x03;

    public const int SenderSubClientIdShift = 10;
    public const int TargetSubClientIdShift = 12;

    public uint RawHeader =>
        (PacketId & PacketIdMask) |
        ((uint)(SenderSubClientId & SenderSubClientIdMask) << SenderSubClientIdShift) |
        ((uint)(TargetSubClientId & TargetSubClientIdMask) << TargetSubClientIdShift);

    public uint Encode() => RawHeader;

    public static SubPacketHeader FromRawHeader(uint raw)
    {
        uint packetId = raw & PacketIdMask;
        byte sender = (byte)((raw >> SenderSubClientIdShift) & SenderSubClientIdMask);
        byte target = (byte)((raw >> TargetSubClientIdShift) & TargetSubClientIdMask);
        return new SubPacketHeader(packetId, sender, target);
    }

    public static SubPacketHeader Decode(uint raw) => FromRawHeader(raw);

    public bool TryWrite(ref PacketWriter writer) => writer.TryWriteVarUInt32(RawHeader);

    public static bool TryRead(ref PacketReader reader, out SubPacketHeader header)
    {
        if (reader.TryReadVarUInt32(out uint raw))
        {
            header = FromRawHeader(raw);
            return true;
        }

        header = default;
        return false;
    }
}
