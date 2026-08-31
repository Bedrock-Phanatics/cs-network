using System;

namespace CsNetwork.Types;

public readonly record struct BlockPosition(int X, int Y, int Z)
{
    public static readonly BlockPosition Zero = new(0, 0, 0);

    public override string ToString() => $"({X}, {Y}, {Z})";
}
