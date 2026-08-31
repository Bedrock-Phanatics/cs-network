using System;
using CsNetwork.IO;
using CsNetwork.Types;
using Xunit;

namespace CsNetwork.Tests.IO;

public sealed class FuzzingTests
{
    private readonly Random _random = new(1337);

    [Fact]
    public void Fuzz_VarIntCodec_RandomBytes_NeverThrows()
    {
        byte[] buffer = new byte[64];

        for (int iteration = 0; iteration < 50_000; iteration++)
        {
            _random.NextBytes(buffer);
            int len = _random.Next(0, buffer.Length);
            ReadOnlySpan<byte> span = buffer.AsSpan(0, len);

            _ = VarIntCodec.TryReadVarUInt32(span, out _, out _);
            _ = VarIntCodec.TryReadVarInt32(span, out _, out _);
            _ = VarIntCodec.TryReadVarUInt64(span, out _, out _);
            _ = VarIntCodec.TryReadVarInt64(span, out _, out _);
        }
    }

    [Fact]
    public void Fuzz_PacketReader_RandomBytes_NeverThrows()
    {
        byte[] buffer = new byte[256];

        for (int iteration = 0; iteration < 25_000; iteration++)
        {
            _random.NextBytes(buffer);
            int len = _random.Next(0, buffer.Length);
            var reader = new PacketReader(buffer.AsSpan(0, len));

            _ = reader.TryReadByte(out _);
            _ = reader.TryReadInt16LE(out _);
            _ = reader.TryReadUInt16LE(out _);
            _ = reader.TryReadInt32LE(out _);
            _ = reader.TryReadUInt32LE(out _);
            _ = reader.TryReadInt64LE(out _);
            _ = reader.TryReadUInt64LE(out _);
            _ = reader.TryReadFloat32LE(out _);
            _ = reader.TryReadFloat64LE(out _);
            _ = reader.TryReadVarUInt32(out _);
            _ = reader.TryReadVarInt32(out _);
            _ = reader.TryReadVarUInt64(out _);
            _ = reader.TryReadVarInt64(out _);
            _ = reader.TryReadString(out string? _);
            _ = reader.TryReadStringUtf8(out _);
            _ = reader.TryReadByteArray(out _);
            _ = reader.TryReadBlockPosition(out _);
            _ = reader.TryReadUuid(out _);
        }
    }

    [Fact]
    public void TruncationFuzz_ValidPayload_TruncatedAtEveryByte_FailsGracefully()
    {
        byte[] backing = new byte[256];
        var writer = new PacketWriter(backing);

        writer.WriteString("Fuzz testing Bedrock protocol");
        writer.WriteBlockPosition(new BlockPosition(100, 64, -200));
        writer.WriteUuid(new Uuid(Guid.NewGuid()));
        writer.WriteVarUInt32(1234567);
        writer.WriteVarInt64(-9876543210L);
        writer.WriteByteArray([0xDE, 0xAD, 0xBE, 0xEF]);

        byte[] validData = writer.WrittenSpan.ToArray();

        for (int i = 0; i < validData.Length; i++)
        {
            var truncated = validData.AsSpan(0, i);
            var reader = new PacketReader(truncated);

            bool allSucceeded =
                reader.TryReadString(out string? _) &&
                reader.TryReadBlockPosition(out _) &&
                reader.TryReadUuid(out _) &&
                reader.TryReadVarUInt32(out _) &&
                reader.TryReadVarInt64(out _) &&
                reader.TryReadByteArray(out _);

            Assert.False(allSucceeded);
        }
    }
}
