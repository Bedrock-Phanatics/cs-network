using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CsNetwork.ResourcePacks;

[JsonConverter(typeof(PackVersionJsonConverter))]
public readonly record struct PackVersion(int Major, int Minor, int Patch) : IComparable<PackVersion>, ISpanFormattable
{
    public static readonly PackVersion Empty = new(0, 0, 0);

    public int CompareTo(PackVersion other)
    {
        int cmp = Major.CompareTo(other.Major);
        if (cmp != 0) return cmp;
        cmp = Minor.CompareTo(other.Minor);
        if (cmp != 0) return cmp;
        return Patch.CompareTo(other.Patch);
    }

    public static bool operator <(PackVersion left, PackVersion right) => left.CompareTo(right) < 0;
    public static bool operator <=(PackVersion left, PackVersion right) => left.CompareTo(right) <= 0;
    public static bool operator >(PackVersion left, PackVersion right) => left.CompareTo(right) > 0;
    public static bool operator >=(PackVersion left, PackVersion right) => left.CompareTo(right) >= 0;

    public override string ToString() => $"{Major}.{Minor}.{Patch}";

    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
    {
        return destination.TryWrite($"{Major}.{Minor}.{Patch}", out charsWritten);
    }

    string IFormattable.ToString(string? format, IFormatProvider? formatProvider) => ToString();

    public static bool TryParse(ReadOnlySpan<char> span, out PackVersion version)
    {
        version = Empty;
        span = span.Trim();
        if (span.IsEmpty)
            return false;

        int firstDot = span.IndexOf('.');
        if (firstDot <= 0)
            return false;

        if (!int.TryParse(span[..firstDot], out int major) || major < 0)
            return false;

        ReadOnlySpan<char> remaining = span[(firstDot + 1)..];
        int secondDot = remaining.IndexOf('.');
        if (secondDot <= 0)
            return false;

        if (!int.TryParse(remaining[..secondDot], out int minor) || minor < 0)
            return false;

        ReadOnlySpan<char> patchSpan = remaining[(secondDot + 1)..];
        if (!int.TryParse(patchSpan, out int patch) || patch < 0)
            return false;

        version = new PackVersion(major, minor, patch);
        return true;
    }
}

public sealed class PackVersionJsonConverter : JsonConverter<PackVersion>
{
    public override PackVersion Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.StartArray)
        {
            reader.Read();
            int major = reader.GetInt32();
            reader.Read();
            int minor = reader.GetInt32();
            reader.Read();
            int patch = reader.GetInt32();

            if (major < 0 || minor < 0 || patch < 0)
                throw new JsonException("PackVersion components must be non-negative.");

            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                // skip extra elements if any in bedrock format
            }

            return new PackVersion(major, minor, patch);
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            string? str = reader.GetString();
            if (str != null && PackVersion.TryParse(str, out var ver))
                return ver;

            throw new JsonException($"Invalid PackVersion string: '{str}'. Expected 'Major.Minor.Patch'.");
        }

        throw new JsonException($"Unexpected token type {reader.TokenType} when parsing PackVersion.");
    }

    public override void Write(Utf8JsonWriter writer, PackVersion value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteStartArray();
        writer.WriteNumberValue(value.Major);
        writer.WriteNumberValue(value.Minor);
        writer.WriteNumberValue(value.Patch);
        writer.WriteEndArray();
    }
}
