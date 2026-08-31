using System;
using BenchmarkDotNet.Attributes;
using CsNetwork.IO;
using CsNetwork.Types;

namespace CsNetwork.Benchmarks;

[MemoryDiagnoser]
public class PacketCodecBenchmarks
{
    private byte[] _buffer = null!;
    private readonly BlockPosition _pos = new(100, 64, -200);
    private readonly Uuid _uuid = new(Guid.Parse("12345678-1234-1234-1234-123456789abc"));
    private readonly byte[] _rawPayload = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08];

    [GlobalSetup]
    public void Setup()
    {
        _buffer = new byte[256];
        var writer = new PacketWriter(_buffer);
        writer.WriteInt32LE(12345);
        writer.WriteFloat32LE(3.14159f);
        writer.WriteVarUInt32(999999);
        writer.WriteBlockPosition(_pos);
        writer.WriteUuid(_uuid);
        writer.WriteStringUtf8("MinecraftBedrock"u8);
        writer.WriteByteArray(_rawPayload);
    }

    [Benchmark]
    public int WriteCompositePacket()
    {
        var writer = new PacketWriter(_buffer);
        writer.WriteInt32LE(12345);
        writer.WriteFloat32LE(3.14159f);
        writer.WriteVarUInt32(999999);
        writer.WriteBlockPosition(_pos);
        writer.WriteUuid(_uuid);
        writer.WriteStringUtf8("MinecraftBedrock"u8);
        writer.WriteByteArray(_rawPayload);
        return writer.BytesWritten;
    }

    [Benchmark]
    public bool ReadCompositePacket()
    {
        var reader = new PacketReader(_buffer);
        return reader.TryReadInt32LE(out _) &&
               reader.TryReadFloat32LE(out _) &&
               reader.TryReadVarUInt32(out _) &&
               reader.TryReadBlockPosition(out _) &&
               reader.TryReadUuid(out _) &&
               reader.TryReadStringUtf8(out _) &&
               reader.TryReadByteArray(out _);
    }
}
