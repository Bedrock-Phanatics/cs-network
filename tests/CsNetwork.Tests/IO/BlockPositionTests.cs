using System;
using CsNetwork.IO;
using CsNetwork.Types;
using Xunit;

namespace CsNetwork.Tests.IO;

public sealed class BlockPositionTests
{
    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(100, 64, -200)]
    [InlineData(-1, -1, -1)]
    [InlineData(int.MaxValue, int.MinValue, 0)]
    [InlineData(-30000000, 256, 30000000)]
    public void BlockPosition_Roundtrip(int x, int y, int z)
    {
        var pos = new BlockPosition(x, y, z);
        Span<byte> buffer = stackalloc byte[32];

        var writer = new PacketWriter(buffer);
        writer.WriteBlockPosition(pos);

        var reader = new PacketReader(writer.WrittenSpan);
        bool success = reader.TryReadBlockPosition(out var readPos);

        Assert.True(success);
        Assert.Equal(pos, readPos);
        Assert.Equal(0, reader.Remaining);
    }
}
