using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using BenchmarkDotNet.Attributes;
using CsNetwork.ResourcePacks;

namespace CsNetwork.Benchmarks;

[MemoryDiagnoser]
public class ResourcePackBenchmarks
{
    private ResourcePack _pack = null!;
    private byte[] _chunkBuffer = null!;
    private string _manifestJsonc = null!;
    private byte[] _manifestUtf8Bytes = null!;

    [GlobalSetup]
    public void Setup()
    {
        _manifestJsonc = """
        {
            // format version of the manifest
            "format_version": 2,
            "header": {
                "name": "Benchmark Pack",
                "description": "High performance textures",
                "uuid": "d13be602-5369-42b7-a365-5be13289069d",
                "version": [1, 1, 0],
                "min_engine_version": "1.20.0",
            },
            "modules": [
                {
                    "type": "resources",
                    "uuid": "439b1a0d-6e84-482a-a226-5b4d081f9b33",
                    "version": [1, 1, 0],
                },
            ],
            "capabilities": [
                "raytracing",
            ]
        }
        """;

        _manifestUtf8Bytes = Encoding.UTF8.GetBytes(_manifestJsonc);

        using var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry("manifest.json");
            using (var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false)))
            {
                writer.Write(_manifestJsonc);
            }

            var dummy = archive.CreateEntry("dummy.bin");
            using (var dummyStream = dummy.Open())
            {
                byte[] data = new byte[1048576]; // 1 mb
                dummyStream.Write(data);
            }
        }

        _pack = ResourcePack.FromBytes(ms.ToArray());
        _chunkBuffer = new byte[ResourcePack.DefaultChunkSize];

        if (!PackManifest.TryParse(_manifestUtf8Bytes, out _, out string? err))
            throw new InvalidOperationException($"Failed to parse benchmark setup manifest: {err}");

        if (!PackVersion.TryParse("1.21.60", out _))
            throw new InvalidOperationException("Failed to parse benchmark setup version.");

        if (_pack.TotalChunks() <= 0)
            throw new InvalidOperationException("Benchmark setup pack has no chunks.");
    }

    [Benchmark]
    public ReadOnlyMemory<byte> GetChunk()
    {
        return _pack.GetChunk(0, ResourcePack.DefaultChunkSize);
    }

    [Benchmark]
    public bool TryGetChunk()
    {
        return _pack.TryGetChunk(0, ResourcePack.DefaultChunkSize, _chunkBuffer, out _);
    }

    [Benchmark]
    public bool ParseJsoncManifest()
    {
        return PackManifest.TryParse(_manifestUtf8Bytes, out _, out _);
    }

    [Benchmark]
    public bool ParsePackVersion()
    {
        return PackVersion.TryParse("1.21.60", out _);
    }
}
