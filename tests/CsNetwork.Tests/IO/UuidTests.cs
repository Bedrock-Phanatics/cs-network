using System;
using CsNetwork.IO;
using CsNetwork.Types;
using Xunit;

namespace CsNetwork.Tests.IO;

public sealed class UuidTests
{
    [Fact]
    public void Uuid_Roundtrip_LittleEndianWireFormat()
    {
        var guid = Guid.Parse("00112233-4455-6677-8899-aabbccddeeff");
        var uuid = new Uuid(guid);

        Span<byte> buffer = stackalloc byte[16];
        var writer = new PacketWriter(buffer);
        writer.TryWriteUuid(uuid);

        Assert.Equal(16, writer.Position);

        var reader = new PacketReader(writer.WrittenSpan);
        bool success = reader.TryReadUuid(out var readUuid);

        Assert.True(success);
        Assert.Equal(uuid, readUuid);
        Assert.Equal(guid, readUuid.Value);
    }

    [Fact]
    public void Uuid_WireFormat_Matches_BedrockSpecification()
    {
        var uuid = new Uuid(Guid.Parse("00000000-0000-0000-0000-000000000001"));
        byte[] expectedWire = [
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        ];

        Span<byte> buffer = stackalloc byte[16];
        var writer = new PacketWriter(buffer);
        writer.TryWriteUuid(uuid);

        Assert.Equal(expectedWire, writer.WrittenSpan.ToArray());

        var reader = new PacketReader(expectedWire);
        Assert.True(reader.TryReadUuid(out var decoded));
        Assert.Equal(uuid, decoded);
    }
}
