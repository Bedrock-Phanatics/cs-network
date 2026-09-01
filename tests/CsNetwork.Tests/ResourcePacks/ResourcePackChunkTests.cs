using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using CsNetwork.ResourcePacks;
using Xunit;

namespace CsNetwork.Tests.ResourcePacks;

public sealed class ResourcePackChunkTests
{
    private static byte[] CreateSyntheticPackZip(string manifestJson, int dummyFileBytes = 0, string manifestEntryName = "manifest.json")
    {
        using var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            var manifestEntry = archive.CreateEntry(manifestEntryName);
            using (var writer = new StreamWriter(manifestEntry.Open(), Encoding.UTF8))
            {
                writer.Write(manifestJson);
            }

            if (dummyFileBytes > 0)
            {
                var dummyEntry = archive.CreateEntry("textures/dummy.bin");
                using var dummyStream = dummyEntry.Open();
                byte[] buffer = new byte[Math.Min(dummyFileBytes, 65536)];
                int remaining = dummyFileBytes;
                while (remaining > 0)
                {
                    int toWrite = Math.Min(remaining, buffer.Length);
                    dummyStream.Write(buffer, 0, toWrite);
                    remaining -= toWrite;
                }
            }
        }

        return ms.ToArray();
    }

    [Fact]
    public void FromBytes_ValidArchive_ComputesSha256AndExtractsManifest()
    {
        string manifestJson = """
        {
            "format_version": 2,
            "header": {
                "name": "Test Pack",
                "uuid": "11111111-1111-1111-1111-111111111111",
                "version": [1, 0, 0]
            },
            "modules": [
                {
                    "type": "resources",
                    "uuid": "22222222-2222-2222-2222-222222222222",
                    "version": [1, 0, 0]
                }
            ]
        }
        """;

        byte[] zipBytes = CreateSyntheticPackZip(manifestJson, dummyFileBytes: 1000);
        var pack = ResourcePack.FromBytes(zipBytes, contentKey: "secretKey", downloadUrl: "https://example.com/pack.zip");

        Assert.Equal("Test Pack", pack.Manifest.Header.Name);
        Assert.Equal(Guid.Parse("11111111-1111-1111-1111-111111111111"), pack.Id);
        Assert.Equal(new PackVersion(1, 0, 0), pack.Version);
        Assert.Equal(zipBytes.Length, pack.Size);
        Assert.Equal("secretKey", pack.ContentKey);
        Assert.Equal("https://example.com/pack.zip", pack.DownloadUrl);

        byte[] expectedHash = SHA256.HashData(zipBytes);
        Assert.Equal(expectedHash, pack.ContentChecksum.ToArray());
    }

    [Fact]
    public void FromBytes_NestedSubfolderManifest_ExtractsSuccessfully()
    {
        string manifestJson = """
        {
            "format_version": 2,
            "header": {
                "name": "Nested Pack",
                "uuid": "33333333-3333-3333-3333-333333333333",
                "version": [1, 2, 0]
            },
            "modules": [
                {
                    "type": "resources",
                    "uuid": "44444444-4444-4444-4444-444444444444",
                    "version": [1, 2, 0]
                }
            ]
        }
        """;

        byte[] zipBytes = CreateSyntheticPackZip(manifestJson, manifestEntryName: "subfolder/manifest.json");
        var pack = ResourcePack.FromBytes(zipBytes);

        Assert.Equal("Nested Pack", pack.Manifest.Header.Name);
    }

    [Fact]
    public void TotalChunks_CalculatesCorrectly()
    {
        string manifestJson = """
        {
            "format_version": 2,
            "header": {
                "name": "Chunk Pack",
                "uuid": "55555555-5555-5555-5555-555555555555",
                "version": [1, 0, 0]
            },
            "modules": [
                {
                    "type": "resources",
                    "uuid": "66666666-6666-6666-6666-666666666666",
                    "version": [1, 0, 0]
                }
            ]
        }
        """;

        // exact 1 MB (1048576 bytes) dummy file
        byte[] zipBytes = CreateSyntheticPackZip(manifestJson, dummyFileBytes: 1048576);
        var pack = ResourcePack.FromBytes(zipBytes);

        int chunkSize512K = 524288;
        int expectedChunks = (int)((pack.Size + chunkSize512K - 1) / chunkSize512K);

        Assert.Equal(expectedChunks, pack.TotalChunks(chunkSize512K));
        Assert.Equal(0, pack.TotalChunks(chunkSize: 0));
        Assert.Equal(0, pack.TotalChunks(chunkSize: -1));
    }

    [Fact]
    public void GetChunk_And_TryGetChunk_SlicesDataCorrectly()
    {
        string manifestJson = """
        {
            "format_version": 2,
            "header": {
                "name": "Chunk Pack",
                "uuid": "77777777-7777-7777-7777-777777777777",
                "version": [1, 0, 0]
            },
            "modules": [
                {
                    "type": "resources",
                    "uuid": "88888888-8888-8888-8888-888888888888",
                    "version": [1, 0, 0]
                }
            ]
        }
        """;

        byte[] zipBytes = CreateSyntheticPackZip(manifestJson, dummyFileBytes: 50000);
        var pack = ResourcePack.FromBytes(zipBytes);

        int chunkSize = 16384;
        int totalChunks = pack.TotalChunks(chunkSize);

        int totalBytesRead = 0;
        byte[] buffer = new byte[chunkSize];

        for (int i = 0; i < totalChunks; i++)
        {
            var chunkMemory = pack.GetChunk(i, chunkSize);
            Assert.False(chunkMemory.IsEmpty);

            bool tryReadSuccess = pack.TryGetChunk(i, chunkSize, buffer, out int bytesWritten);
            Assert.True(tryReadSuccess);
            Assert.Equal(chunkMemory.Length, bytesWritten);
            Assert.Equal(chunkMemory.Span.ToArray(), buffer[..bytesWritten]);

            totalBytesRead += bytesWritten;
        }

        Assert.Equal(pack.Size, totalBytesRead);

        // out of bounds chunk requests
        Assert.True(pack.GetChunk(totalChunks, chunkSize).IsEmpty);
        Assert.False(pack.TryGetChunk(totalChunks, chunkSize, buffer, out _));
        Assert.True(pack.GetChunk(-1, chunkSize).IsEmpty);
        Assert.False(pack.TryGetChunk(-1, chunkSize, buffer, out _));

        // 64-bit integer overflow bounds check
        Assert.True(pack.GetChunk(4096, 1048576).IsEmpty);
        Assert.False(pack.TryGetChunk(4096, 1048576, buffer, out _));
    }

    [Fact]
    public void DecompressionBomb_LargeManifest_Rejects()
    {
        // create a zip where manifest.json is 2 MB (> 1 MB safety limit)
        string largeManifest = new string(' ', 2 * 1024 * 1024);
        byte[] zipBytes = CreateSyntheticPackZip(largeManifest);

        bool success = ResourcePack.TryFromBytes(zipBytes, out _, out string? error);
        Assert.False(success);
        Assert.Contains("safety limit", error, StringComparison.Ordinal);
    }
}
