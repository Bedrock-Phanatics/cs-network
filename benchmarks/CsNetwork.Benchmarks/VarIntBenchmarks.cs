using System;
using BenchmarkDotNet.Attributes;
using CsNetwork.IO;

namespace CsNetwork.Benchmarks;

[MemoryDiagnoser]
public class VarIntBenchmarks
{
    private byte[] _u32Buffer = null!;
    private byte[] _i32Buffer = null!;
    private byte[] _u64Buffer = null!;
    private byte[] _i64Buffer = null!;

    [Params(0u, 127u, 16384u, 2097152u, uint.MaxValue)]
    public uint UInt32Value { get; set; }

    [Params(0, -1, 100, -100000, int.MaxValue, int.MinValue)]
    public int Int32Value { get; set; }

    [Params(0UL, 127UL, 16384UL, 2097152UL, ulong.MaxValue)]
    public ulong UInt64Value { get; set; }

    [Params(0L, -1L, 1000000L, -1000000000L, long.MaxValue, long.MinValue)]
    public long Int64Value { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _u32Buffer = new byte[16];
        _i32Buffer = new byte[16];
        _u64Buffer = new byte[16];
        _i64Buffer = new byte[16];

        VarIntCodec.WriteVarUInt32(_u32Buffer, UInt32Value);
        VarIntCodec.WriteVarInt32(_i32Buffer, Int32Value);
        VarIntCodec.WriteVarUInt64(_u64Buffer, UInt64Value);
        VarIntCodec.WriteVarInt64(_i64Buffer, Int64Value);
    }

    [Benchmark]
    public int WriteVarUInt32()
    {
        return VarIntCodec.WriteVarUInt32(_u32Buffer, UInt32Value);
    }

    [Benchmark]
    public bool ReadVarUInt32()
    {
        return VarIntCodec.TryReadVarUInt32(_u32Buffer, out _, out _);
    }

    [Benchmark]
    public int WriteVarInt32()
    {
        return VarIntCodec.WriteVarInt32(_i32Buffer, Int32Value);
    }

    [Benchmark]
    public bool ReadVarInt32()
    {
        return VarIntCodec.TryReadVarInt32(_i32Buffer, out _, out _);
    }

    [Benchmark]
    public int WriteVarUInt64()
    {
        return VarIntCodec.WriteVarUInt64(_u64Buffer, UInt64Value);
    }

    [Benchmark]
    public bool ReadVarUInt64()
    {
        return VarIntCodec.TryReadVarUInt64(_u64Buffer, out _, out _);
    }

    [Benchmark]
    public int WriteVarInt64()
    {
        return VarIntCodec.WriteVarInt64(_i64Buffer, Int64Value);
    }

    [Benchmark]
    public bool ReadVarInt64()
    {
        return VarIntCodec.TryReadVarInt64(_i64Buffer, out _, out _);
    }
}
