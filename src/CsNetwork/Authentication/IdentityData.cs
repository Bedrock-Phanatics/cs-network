using System;
using System.Diagnostics.CodeAnalysis;

namespace CsNetwork.Authentication;

public sealed record IdentityData(
    string DisplayName,
    Guid Identity,
    string? Xuid = null,
    string? TitleId = null,
    string? PlayFabId = null)
{
    public bool Validate([NotNullWhen(false)] out string? error)
    {
        if (Identity == Guid.Empty)
        {
            error = "Identity UUID must not be empty/nil.";
            return false;
        }

        int maxNameLen = string.IsNullOrEmpty(Xuid) ? 16 : 15;
        if (string.IsNullOrEmpty(DisplayName) || DisplayName.Length > maxNameLen)
        {
            error = $"DisplayName length must be between 1 and {maxNameLen} characters.";
            return false;
        }

        if (DisplayName[0] == ' ' || DisplayName[^1] == ' ')
        {
            error = "DisplayName cannot begin or end with a space.";
            return false;
        }

        if (DisplayName[0] >= '0' && DisplayName[0] <= '9')
        {
            error = "DisplayName cannot begin with a number.";
            return false;
        }

        if (DisplayName.Contains("  ", StringComparison.Ordinal))
        {
            error = "DisplayName cannot contain consecutive spaces.";
            return false;
        }

        if (!string.IsNullOrEmpty(Xuid) && !long.TryParse(Xuid, out _))
        {
            error = $"XUID '{Xuid}' must be parseable as a 64-bit integer.";
            return false;
        }

        error = null;
        return true;
    }
}
