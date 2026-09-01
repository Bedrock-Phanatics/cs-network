using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;

namespace CsNetwork.ResourcePacks;

public sealed record PackHeader(
    string Name,
    string Description,
    Guid Uuid,
    PackVersion Version,
    PackVersion MinEngineVersion,
    string? BaseGameVersion = null)
{
    public bool Validate([NotNullWhen(false)] out string? error)
    {
        if (Uuid == Guid.Empty)
        {
            error = "Header UUID must not be empty (nil).";
            return false;
        }

        if (string.IsNullOrEmpty(Name))
        {
            error = "Header Name must not be empty.";
            return false;
        }

        error = null;
        return true;
    }
}

public sealed record PackModule(
    string Type,
    Guid Uuid,
    string Description,
    PackVersion Version)
{
    public bool Validate([NotNullWhen(false)] out string? error)
    {
        if (Uuid == Guid.Empty)
        {
            error = "Module UUID must not be empty (nil).";
            return false;
        }

        if (string.IsNullOrEmpty(Type))
        {
            error = "Module Type must not be empty.";
            return false;
        }

        error = null;
        return true;
    }
}

public sealed record PackDependency(
    Guid Uuid,
    PackVersion Version);

public sealed record PackMetadata(
    IReadOnlyList<string> Authors,
    string? License = null,
    string? Url = null);

public sealed record PackManifest(
    int FormatVersion,
    PackHeader Header,
    IReadOnlyList<PackModule> Modules,
    IReadOnlyList<PackDependency> Dependencies,
    IReadOnlyList<string> Capabilities,
    PackMetadata? Metadata = null,
    bool IsWorldTemplate = false)
{
    public bool IsResourcePack
    {
        get
        {
            foreach (var m in Modules)
            {
                if (m.Type.Equals("resources", StringComparison.OrdinalIgnoreCase) ||
                    m.Type.Equals("interface", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }
    }

    public bool IsBehaviorPack
    {
        get
        {
            foreach (var m in Modules)
            {
                if (m.Type.Equals("data", StringComparison.OrdinalIgnoreCase) ||
                    m.Type.Equals("client_data", StringComparison.OrdinalIgnoreCase) ||
                    m.Type.Equals("script", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }
    }

    public bool HasScripts
    {
        get
        {
            foreach (var m in Modules)
            {
                if (m.Type.Equals("client_data", StringComparison.OrdinalIgnoreCase) ||
                    m.Type.Equals("script", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }
    }

    public bool IsRaytracingCapable
    {
        get
        {
            foreach (var cap in Capabilities)
            {
                if (cap.Equals("raytracing", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
    }

    public bool Validate([NotNullWhen(false)] out string? error)
    {
        if (FormatVersion is not (1 or 2))
        {
            error = $"Unsupported manifest format_version {FormatVersion}. Expected 1 or 2.";
            return false;
        }

        if (Header == null)
        {
            error = "Manifest header is null.";
            return false;
        }

        if (!Header.Validate(out error))
        {
            return false;
        }

        if (Modules == null || Modules.Count == 0)
        {
            error = "Manifest must contain at least one module.";
            return false;
        }

        foreach (var mod in Modules)
        {
            if (!mod.Validate(out error))
                return false;
        }

        error = null;
        return true;
    }

    public static bool TryParse(ReadOnlySpan<byte> utf8JsonBytes, [NotNullWhen(true)] out PackManifest? manifest, [NotNullWhen(false)] out string? error)
    {
        manifest = null;
        error = null;

        if (utf8JsonBytes.IsEmpty)
        {
            error = "Manifest JSON buffer is empty.";
            return false;
        }

        // skip utf-8 bom if present
        if (utf8JsonBytes.Length >= 3 && utf8JsonBytes[0] == 0xEF && utf8JsonBytes[1] == 0xBB && utf8JsonBytes[2] == 0xBF)
        {
            utf8JsonBytes = utf8JsonBytes[3..];
        }

        try
        {
            var options = new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };

            using var doc = JsonDocument.Parse(utf8JsonBytes.ToArray(), options);
            var root = doc.RootElement;

            int formatVersion = root.TryGetProperty("format_version", out var fvProp) ? fvProp.GetInt32() : 2;

            if (!root.TryGetProperty("header", out var headerProp))
            {
                error = "Manifest missing 'header' property.";
                return false;
            }

            string headerName = headerProp.TryGetProperty("name", out var hn) ? hn.GetString() ?? "" : "";
            string headerDesc = headerProp.TryGetProperty("description", out var hd) ? hd.GetString() ?? "" : "";
            string headerUuidStr = headerProp.TryGetProperty("uuid", out var hu) ? hu.GetString() ?? "" : "";
            if (string.IsNullOrEmpty(headerUuidStr))
            {
                error = "Header UUID must not be empty.";
                return false;
            }

            if (!Guid.TryParse(headerUuidStr, out var headerUuid) || headerUuid == Guid.Empty)
            {
                error = $"Header UUID '{headerUuidStr}' is not a valid GUID.";
                return false;
            }

            var headerVer = ParseVersion(headerProp, "version");
            var minEngineVer = ParseVersion(headerProp, "min_engine_version");
            string? baseGameVer = headerProp.TryGetProperty("base_game_version", out var bgv) ? bgv.GetString() : null;

            var header = new PackHeader(headerName, headerDesc, headerUuid, headerVer, minEngineVer, baseGameVer);

            var modules = new List<PackModule>();
            if (root.TryGetProperty("modules", out var modulesProp) && modulesProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var modEl in modulesProp.EnumerateArray())
                {
                    string modType = modEl.TryGetProperty("type", out var mt) ? mt.GetString() ?? "" : "";
                    string modDesc = modEl.TryGetProperty("description", out var md) ? md.GetString() ?? "" : "";
                    string modUuidStr = modEl.TryGetProperty("uuid", out var mu) ? mu.GetString() ?? "" : "";
                    if (string.IsNullOrEmpty(modUuidStr))
                    {
                        error = "Module UUID must not be empty.";
                        return false;
                    }
                    if (!Guid.TryParse(modUuidStr, out var modUuid) || modUuid == Guid.Empty)
                    {
                        error = $"Module UUID '{modUuidStr}' is not a valid GUID.";
                        return false;
                    }
                    var modVer = ParseVersion(modEl, "version");
                    modules.Add(new PackModule(modType, modUuid, modDesc, modVer));
                }
            }

            var dependencies = new List<PackDependency>();
            if (root.TryGetProperty("dependencies", out var depProp) && depProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var depEl in depProp.EnumerateArray())
                {
                    if (depEl.TryGetProperty("uuid", out var du) && Guid.TryParse(du.GetString(), out var depUuid))
                    {
                        var depVer = ParseVersion(depEl, "version");
                        dependencies.Add(new PackDependency(depUuid, depVer));
                    }
                }
            }

            var capabilities = new List<string>();
            if (root.TryGetProperty("capabilities", out var capProp) && capProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var capEl in capProp.EnumerateArray())
                {
                    if (capEl.GetString() is { } capStr && !string.IsNullOrEmpty(capStr))
                        capabilities.Add(capStr);
                }
            }

            PackMetadata? metadata = null;
            if (root.TryGetProperty("metadata", out var metaProp) && metaProp.ValueKind == JsonValueKind.Object)
            {
                var authors = new List<string>();
                if (metaProp.TryGetProperty("authors", out var authProp) && authProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var a in authProp.EnumerateArray())
                    {
                        if (a.GetString() is { } authorName)
                            authors.Add(authorName);
                    }
                }
                string? license = metaProp.TryGetProperty("license", out var licProp) ? licProp.GetString() : null;
                string? url = metaProp.TryGetProperty("url", out var urlProp) ? urlProp.GetString() : null;
                metadata = new PackMetadata(authors, license, url);
            }

            manifest = new PackManifest(formatVersion, header, modules, dependencies, capabilities, metadata);
            return manifest.Validate(out error);
        }
        catch (Exception ex)
        {
            error = $"Failed to parse manifest JSON: {ex.Message}";
            return false;
        }
    }

    public static bool TryParse(string json, [NotNullWhen(true)] out PackManifest? manifest, [NotNullWhen(false)] out string? error)
    {
        ArgumentNullException.ThrowIfNull(json);
        int maxByteCount = Encoding.UTF8.GetMaxByteCount(json.Length);
        if (maxByteCount <= 2048)
        {
            Span<byte> utf8Span = stackalloc byte[maxByteCount];
            int written = Encoding.UTF8.GetBytes(json, utf8Span);
            return TryParse(utf8Span[..written], out manifest, out error);
        }

        byte[] bytes = Encoding.UTF8.GetBytes(json);
        return TryParse(bytes, out manifest, out error);
    }

    private static PackVersion ParseVersion(JsonElement parent, string propertyName)
    {
        if (!parent.TryGetProperty(propertyName, out var prop))
            return PackVersion.Empty;

        if (prop.ValueKind == JsonValueKind.Array)
        {
            int major = 0, minor = 0, patch = 0;
            int idx = 0;
            foreach (var el in prop.EnumerateArray())
            {
                if (idx == 0) major = el.GetInt32();
                else if (idx == 1) minor = el.GetInt32();
                else if (idx == 2) patch = el.GetInt32();
                idx++;
            }
            return new PackVersion(major, minor, patch);
        }

        if (prop.ValueKind == JsonValueKind.String && prop.GetString() is { } str)
        {
            if (PackVersion.TryParse(str, out var v))
                return v;
        }

        return PackVersion.Empty;
    }
}
