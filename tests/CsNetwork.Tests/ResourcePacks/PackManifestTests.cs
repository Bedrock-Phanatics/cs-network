using System;
using CsNetwork.ResourcePacks;
using Xunit;

namespace CsNetwork.Tests.ResourcePacks;

public sealed class PackManifestTests
{
    [Fact]
    public void TryParse_Format2WithJsoncCommentsAndTrailingCommas_Succeeds()
    {
        string jsonc = """
        {
            // format version of the manifest
            "format_version": 2,
            "header": {
                "name": "Faithful 32x",
                "description": "High resolution textures",
                "uuid": "d13be602-5369-42b7-a365-5be13289069d",
                "version": [1, 1, 0],
                "min_engine_version": "1.20.0",
            },
            /* modules array defines contents */
            "modules": [
                {
                    "type": "resources",
                    "uuid": "439b1a0d-6e84-482a-a226-5b4d081f9b33",
                    "version": [1, 1, 0],
                },
            ],
            "capabilities": [
                "raytracing",
            ],
            "metadata": {
                "authors": ["Faithful Team"],
                "license": "Custom",
            }
        }
        """;

        bool success = PackManifest.TryParse(jsonc, out var manifest, out string? error);
        Assert.True(success, error);
        Assert.NotNull(manifest);

        Assert.Equal(2, manifest.FormatVersion);
        Assert.Equal("Faithful 32x", manifest.Header.Name);
        Assert.Equal("High resolution textures", manifest.Header.Description);
        Assert.Equal(Guid.Parse("d13be602-5369-42b7-a365-5be13289069d"), manifest.Header.Uuid);
        Assert.Equal(new PackVersion(1, 1, 0), manifest.Header.Version);
        Assert.Equal(new PackVersion(1, 20, 0), manifest.Header.MinEngineVersion);

        Assert.Single(manifest.Modules);
        Assert.Equal("resources", manifest.Modules[0].Type);
        Assert.Equal(Guid.Parse("439b1a0d-6e84-482a-a226-5b4d081f9b33"), manifest.Modules[0].Uuid);

        Assert.True(manifest.IsResourcePack);
        Assert.False(manifest.IsBehaviorPack);
        Assert.False(manifest.HasScripts);
        Assert.True(manifest.IsRaytracingCapable);

        Assert.NotNull(manifest.Metadata);
        Assert.Single(manifest.Metadata.Authors);
        Assert.Equal("Faithful Team", manifest.Metadata.Authors[0]);
    }

    [Fact]
    public void TryParse_BehaviorPackWithScripts_Succeeds()
    {
        string json = """
        {
            "format_version": 2,
            "header": {
                "name": "Custom Scripts",
                "description": "Scripting pack",
                "uuid": "a0000000-0000-0000-0000-000000000001",
                "version": [1, 0, 0]
            },
            "modules": [
                {
                    "type": "client_data",
                    "uuid": "b0000000-0000-0000-0000-000000000002",
                    "version": [1, 0, 0]
                }
            ]
        }
        """;

        bool success = PackManifest.TryParse(json, out var manifest, out string? error);
        Assert.True(success, error);
        Assert.NotNull(manifest);

        Assert.False(manifest.IsResourcePack);
        Assert.True(manifest.IsBehaviorPack);
        Assert.True(manifest.HasScripts);
    }

    [Fact]
    public void TryParse_MissingHeaderUuid_Fails()
    {
        string json = """
        {
            "format_version": 2,
            "header": {
                "name": "No UUID Pack",
                "version": [1, 0, 0]
            },
            "modules": [
                {
                    "type": "resources",
                    "uuid": "439b1a0d-6e84-482a-a226-5b4d081f9b33",
                    "version": [1, 0, 0]
                }
            ]
        }
        """;

        bool success = PackManifest.TryParse(json, out _, out string? error);
        Assert.False(success);
        Assert.Contains("UUID must not be empty", error, StringComparison.Ordinal);
    }

    [Fact]
    public void TryParse_EmptyModules_Fails()
    {
        string json = """
        {
            "format_version": 2,
            "header": {
                "name": "Empty Modules Pack",
                "uuid": "d13be602-5369-42b7-a365-5be13289069d",
                "version": [1, 0, 0]
            },
            "modules": []
        }
        """;

        bool success = PackManifest.TryParse(json, out _, out string? error);
        Assert.False(success);
        Assert.Contains("at least one module", error, StringComparison.Ordinal);
    }
}
