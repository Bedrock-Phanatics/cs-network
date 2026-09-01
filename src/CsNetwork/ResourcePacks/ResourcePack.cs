using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;

namespace CsNetwork.ResourcePacks;

public sealed class ResourcePack
{
    public const int DefaultChunkSize = 524288; // 512 kb standard bedrock chunk size
    public const int MaxChunkSize = 1048576; // 1 mb max chunk size
    public const int MaxManifestDecompressedBytes = 1048576; // 1 mb zip bomb limit for manifest.json
    public const long MaxPackSize = int.MaxValue; // 2 gib max supported in-memory pack size

    private readonly byte[] _checksum;

    public PackManifest Manifest { get; }
    public Guid Id => Manifest.Header.Uuid;
    public PackVersion Version => Manifest.Header.Version;
    public long Size => RawContent.Length;
    public ReadOnlyMemory<byte> RawContent { get; }
    public ReadOnlyMemory<byte> ContentChecksum => _checksum;
    public string? ContentKey { get; }
    public string? DownloadUrl { get; }
    public string? SubPackName { get; }
    public string? ContentIdentity { get; }
    public bool RaytracingCapable => Manifest.IsRaytracingCapable;

    public ResourcePack(
        PackManifest manifest,
        ReadOnlyMemory<byte> rawContent,
        byte[] checksum,
        string? contentKey = null,
        string? downloadUrl = null,
        string? subPackName = null,
        string? contentIdentity = null)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(checksum);

        Manifest = manifest;
        RawContent = rawContent;
        _checksum = checksum;
        ContentKey = contentKey;
        DownloadUrl = downloadUrl;
        SubPackName = subPackName;
        ContentIdentity = contentIdentity;
    }

    public int TotalChunks(int chunkSize = DefaultChunkSize)
    {
        if (chunkSize <= 0 || Size <= 0)
            return 0;

        return (int)((Size + chunkSize - 1) / chunkSize);
    }

    public ReadOnlyMemory<byte> GetChunk(int chunkIndex, int chunkSize = DefaultChunkSize)
    {
        if (chunkIndex < 0 || chunkSize <= 0)
            return ReadOnlyMemory<byte>.Empty;

        long offset = (long)chunkIndex * chunkSize;
        if (offset > int.MaxValue || offset >= Size)
            return ReadOnlyMemory<byte>.Empty;

        int length = (int)Math.Min((long)chunkSize, Size - offset);
        return RawContent.Slice((int)offset, length);
    }

    public bool TryGetChunk(int chunkIndex, int chunkSize, Span<byte> destination, out int bytesWritten)
    {
        bytesWritten = 0;
        if (chunkIndex < 0 || chunkSize <= 0)
            return false;

        long offset = (long)chunkIndex * chunkSize;
        if (offset > int.MaxValue || offset >= Size)
            return false;

        int length = (int)Math.Min((long)chunkSize, Size - offset);
        if (destination.Length < length)
            return false;

        RawContent.Span.Slice((int)offset, length).CopyTo(destination);
        bytesWritten = length;
        return true;
    }

    public static bool TryFromBytes(
        byte[] archiveBytes,
        [NotNullWhen(true)] out ResourcePack? pack,
        [NotNullWhen(false)] out string? error,
        string? contentKey = null,
        string? downloadUrl = null,
        string? subPackName = null,
        string? contentIdentity = null)
    {
        pack = null;
        error = null;

        if (archiveBytes == null || archiveBytes.Length == 0)
        {
            error = "Archive byte buffer is empty.";
            return false;
        }

        if (archiveBytes.LongLength > MaxPackSize)
        {
            error = $"Archive size ({archiveBytes.LongLength} bytes) exceeds maximum supported pack size of {MaxPackSize} bytes (2 GiB).";
            return false;
        }

        try
        {
            using var stream = new MemoryStream(archiveBytes, writable: false);
            using var zip = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);

            ZipArchiveEntry? manifestEntry = null;
            bool isWorldTemplate = false;

            foreach (var entry in zip.Entries)
            {
                string fileName = Path.GetFileName(entry.FullName);
                if (manifestEntry == null && fileName.Equals("manifest.json", StringComparison.OrdinalIgnoreCase))
                {
                    manifestEntry = entry;
                }

                if (fileName.Equals("level.dat", StringComparison.OrdinalIgnoreCase))
                {
                    isWorldTemplate = true;
                }
            }

            if (manifestEntry == null)
            {
                error = "manifest.json not found in archive.";
                return false;
            }

            if (manifestEntry.Length > MaxManifestDecompressedBytes)
            {
                error = $"manifest.json uncompressed size ({manifestEntry.Length} bytes) exceeds safety limit of {MaxManifestDecompressedBytes} bytes.";
                return false;
            }

            using var entryStream = manifestEntry.Open();
            using var manifestMs = new MemoryStream();
            byte[] buffer = new byte[8192];
            int totalRead = 0;
            int read;

            while ((read = entryStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                totalRead += read;
                if (totalRead > MaxManifestDecompressedBytes)
                {
                    error = $"manifest.json decompressed stream exceeded maximum {MaxManifestDecompressedBytes} bytes safety limit.";
                    return false;
                }
                manifestMs.Write(buffer, 0, read);
            }

            byte[] manifestBytes = manifestMs.ToArray();
            if (!PackManifest.TryParse(manifestBytes, out var manifest, out error))
                return false;

            if (isWorldTemplate)
            {
                manifest = manifest with { IsWorldTemplate = true };
            }

            byte[] checksum = SHA256.HashData(archiveBytes);
            pack = new ResourcePack(manifest, archiveBytes, checksum, contentKey, downloadUrl, subPackName, contentIdentity);
            return true;
        }
        catch (Exception ex)
        {
            error = $"Failed to read resource pack archive: {ex.Message}";
            return false;
        }
    }

    public static ResourcePack FromBytes(
        byte[] archiveBytes,
        string? contentKey = null,
        string? downloadUrl = null,
        string? subPackName = null,
        string? contentIdentity = null)
    {
        if (!TryFromBytes(archiveBytes, out var pack, out string? error, contentKey, downloadUrl, subPackName, contentIdentity))
            throw new InvalidDataException(error);

        return pack;
    }

    public static ResourcePack FromStream(
        Stream stream,
        string? contentKey = null,
        string? downloadUrl = null,
        string? subPackName = null,
        string? contentIdentity = null)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return FromBytes(ms.ToArray(), contentKey, downloadUrl, subPackName, contentIdentity);
    }
}
