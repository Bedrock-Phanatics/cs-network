using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using CsNetwork.ResourcePacks;
using Xunit;

namespace CsNetwork.Tests.ResourcePacks;

public sealed class ResourcePackRegistryTests
{
    private static ResourcePack CreatePack(Guid uuid, PackVersion version, string name)
    {
        string manifestJson = $$"""
        {
            "format_version": 2,
            "header": {
                "name": "{{name}}",
                "uuid": "{{uuid:D}}",
                "version": [{{version.Major}}, {{version.Minor}}, {{version.Patch}}]
            },
            "modules": [
                {
                    "type": "resources",
                    "uuid": "{{Guid.NewGuid():D}}",
                    "version": [{{version.Major}}, {{version.Minor}}, {{version.Patch}}]
                }
            ]
        }
        """;

        using var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry("manifest.json");
            using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
            writer.Write(manifestJson);
        }

        return ResourcePack.FromBytes(ms.ToArray());
    }

    [Fact]
    public void Registry_RegisterAndLookup_Succeeds()
    {
        var registry = new ResourcePackRegistry();
        Guid id1 = Guid.NewGuid();
        var v1 = new PackVersion(1, 0, 0);
        var pack1 = CreatePack(id1, v1, "Pack 1");

        Guid id2 = Guid.NewGuid();
        var v2 = new PackVersion(2, 1, 0);
        var pack2 = CreatePack(id2, v2, "Pack 2");

        registry.Register(pack1);
        registry.Register(pack2);

        Assert.Equal(2, registry.Count);
        Assert.Equal(2, registry.AllPacks.Count);

        Assert.True(registry.TryGet(id1, out var retrieved1));
        Assert.Same(pack1, retrieved1);

        Assert.True(registry.TryGet(id2, v2, out var retrieved2));
        Assert.Same(pack2, retrieved2);

        Assert.False(registry.TryGet(id2, new PackVersion(3, 0, 0), out _));
        Assert.False(registry.TryGet(Guid.NewGuid(), out _));

        Assert.True(registry.Remove(id1));
        Assert.Equal(1, registry.Count);
        Assert.False(registry.TryGet(id1, out _));

        registry.Clear();
        Assert.Equal(0, registry.Count);
    }

    [Fact]
    public void Registry_ConcurrentRegisterAndLookup_ThreadSafe()
    {
        var registry = new ResourcePackRegistry();
        const int count = 50;
        var packs = new ResourcePack[count];

        for (int i = 0; i < count; i++)
        {
            packs[i] = CreatePack(Guid.NewGuid(), new PackVersion(1, i, 0), $"ConcurrentPack {i}");
        }

        Parallel.For(0, count, i =>
        {
            registry.Register(packs[i]);
            Assert.True(registry.TryGet(packs[i].Id, out var retrieved));
            Assert.Same(packs[i], retrieved);
            Assert.True(registry.TryGet(packs[i].Id, packs[i].Version, out var retrievedByVersion));
            Assert.Same(packs[i], retrievedByVersion);
        });

        Assert.Equal(count, registry.Count);
        Assert.Equal(count, registry.AllPacks.Count);
    }
}
