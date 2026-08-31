using System;
using CsNetwork.IO;
using CsNetwork.Types;
using Xunit;

namespace CsNetwork.Tests.Vectors;

public sealed class ReferenceVectorTests
{
    [Fact]
    public void Gophertunnel_Varint_Vectors()
    {
        uint[] u32s = [0u, 1u, 127u, 128u, 255u, 2097151u, 2147483647u, 4294967295u];
        Span<byte> buffer = stackalloc byte[10];

        foreach (uint val in u32s)
        {
            int written = VarIntCodec.WriteVarUInt32(buffer, val);
            bool ok = VarIntCodec.TryReadVarUInt32(buffer[..written], out uint decoded, out int bytesRead);
            Assert.True(ok);
            Assert.Equal(val, decoded);
            Assert.Equal(written, bytesRead);
        }

        int[] i32s = [0, -1, 1, -2, 2, int.MinValue, int.MaxValue];
        foreach (int val in i32s)
        {
            int written = VarIntCodec.WriteVarInt32(buffer, val);
            bool ok = VarIntCodec.TryReadVarInt32(buffer[..written], out int decoded, out int bytesRead);
            Assert.True(ok);
            Assert.Equal(val, decoded);
            Assert.Equal(written, bytesRead);
        }
    }

    [Fact]
    public void Cloudburst_VarInt_Vectors()
    {
        (uint Value, int ExpectedSize)[] testCases = [
            (0u, 1),
            (127u, 1),
            (128u, 2),
            (16383u, 2),
            (16384u, 3),
            (2097151u, 3),
            (2097152u, 4),
            (268435455u, 4),
            (268435456u, 5),
            (uint.MaxValue, 5)
        ];

        Span<byte> buffer = stackalloc byte[10];
        foreach (var (val, expectedSize) in testCases)
        {
            int written = VarIntCodec.WriteVarUInt32(buffer, val);
            Assert.Equal(expectedSize, written);
            Assert.Equal(expectedSize, VarIntCodec.GetVarUInt32ByteCount(val));

            bool ok = VarIntCodec.TryReadVarUInt32(buffer[..written], out uint decoded, out int bytesRead);
            Assert.True(ok);
            Assert.Equal(val, decoded);
            Assert.Equal(expectedSize, bytesRead);
        }
    }
}
