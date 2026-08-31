using System;

namespace CsNetwork.Compression;

public enum CompressionAlgorithm : byte
{
    Flate = 0x00,
    Snappy = 0x01,
    None = 0xFF
}

public static class CompressionAlgorithmExtensions
{
    public static ushort ToNetworkSettingsId(this CompressionAlgorithm algorithm)
    {
        return algorithm switch
        {
            CompressionAlgorithm.Flate => 0x0000,
            CompressionAlgorithm.Snappy => 0x0001,
            CompressionAlgorithm.None => 0xFFFF,
            _ => (ushort)algorithm
        };
    }

    public static CompressionAlgorithm FromNetworkSettingsId(ushort networkSettingsAlgorithm)
    {
        return networkSettingsAlgorithm switch
        {
            0x0000 => CompressionAlgorithm.Flate,
            0x0001 => CompressionAlgorithm.Snappy,
            0xFFFF => CompressionAlgorithm.None,
            _ => (CompressionAlgorithm)(byte)networkSettingsAlgorithm
        };
    }

    public static bool TryFromNetworkSettingsId(ushort networkSettingsAlgorithm, out CompressionAlgorithm algorithm)
    {
        switch (networkSettingsAlgorithm)
        {
            case 0x0000:
                algorithm = CompressionAlgorithm.Flate;
                return true;
            case 0x0001:
                algorithm = CompressionAlgorithm.Snappy;
                return true;
            case 0xFFFF:
                algorithm = CompressionAlgorithm.None;
                return true;
            default:
                algorithm = (CompressionAlgorithm)(byte)networkSettingsAlgorithm;
                return false;
        }
    }
}
