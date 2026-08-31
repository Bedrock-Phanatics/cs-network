using System;
using System.Buffers.Binary;

namespace CsNetwork.Types;

public readonly record struct Uuid(Guid Value)
{
    public static readonly Uuid Empty = new(Guid.Empty);

    public static Uuid NewUuid() => new(Guid.NewGuid());

    public static bool TryRead(ReadOnlySpan<byte> source, out Uuid result)
    {
        if (source.Length < 16)
        {
            result = default;
            return false;
        }

        // bedrock sends two 64-bit little endian ints instead of standard rfc4122
        ulong msbLE = BinaryPrimitives.ReadUInt64LittleEndian(source);
        ulong lsbLE = BinaryPrimitives.ReadUInt64LittleEndian(source[8..]);

        Span<byte> rfcBytes = stackalloc byte[16];
        BinaryPrimitives.WriteUInt64BigEndian(rfcBytes, msbLE);
        BinaryPrimitives.WriteUInt64BigEndian(rfcBytes[8..], lsbLE);

        result = new Uuid(new Guid(rfcBytes, bigEndian: true));
        return true;
    }

    public bool TryWrite(Span<byte> destination)
    {
        if (destination.Length < 16)
            return false;

        Span<byte> rfcBytes = stackalloc byte[16];
        if (!Value.TryWriteBytes(rfcBytes, bigEndian: true, out _))
            return false;

        ulong msb = BinaryPrimitives.ReadUInt64BigEndian(rfcBytes);
        ulong lsb = BinaryPrimitives.ReadUInt64BigEndian(rfcBytes[8..]);

        BinaryPrimitives.WriteUInt64LittleEndian(destination, msb);
        BinaryPrimitives.WriteUInt64LittleEndian(destination[8..], lsb);
        return true;
    }

    public override string ToString() => Value.ToString();
}
