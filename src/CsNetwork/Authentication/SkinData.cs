using System;
using System.Diagnostics.CodeAnalysis;

namespace CsNetwork.Authentication;

public sealed record SkinData(
    string SkinId,
    byte[] SkinImage,
    int SkinImageWidth,
    int SkinImageHeight,
    byte[]? CapeImage = null,
    int CapeImageWidth = 0,
    int CapeImageHeight = 0,
    string? CapeId = null,
    string? SkinResourcePatch = null,
    string? GeometryData = null,
    string? GeometryDataEngineVersion = null,
    string? AnimationData = null,
    string? SkinColor = null,
    string? ArmSize = "wide",
    string? PlayFabId = null,
    bool IsPremium = false,
    bool IsPersona = false,
    bool IsCapeOnClassicSkin = false,
    bool IsTrusted = true)
{
    public const int Skin64x32Length = 64 * 32 * 4;
    public const int Skin64x64Length = 64 * 64 * 4;
    public const int Skin128x128Length = 128 * 128 * 4;

    public bool Validate([NotNullWhen(false)] out string? error)
    {
        if (string.IsNullOrEmpty(SkinId))
        {
            error = "SkinId must not be empty.";
            return false;
        }

        if (SkinImage == null || SkinImageWidth <= 0 || SkinImageHeight <= 0)
        {
            error = "Skin image buffer and dimensions must be non-empty and positive.";
            return false;
        }

        if (!IsValidSkinDimension(SkinImageWidth, SkinImageHeight))
        {
            error = $"Invalid skin resolution: {SkinImageWidth}x{SkinImageHeight}. Allowed: 64x32, 64x64, 128x128.";
            return false;
        }

        long expectedSkinBytes = (long)SkinImageWidth * SkinImageHeight * 4;
        if (SkinImage.Length != expectedSkinBytes)
        {
            error = $"Skin image size mismatch: expected {expectedSkinBytes} bytes ({SkinImageWidth}x{SkinImageHeight} RGBA), got {SkinImage.Length} bytes.";
            return false;
        }

        if (CapeImage != null && (CapeImage.Length > 0 || CapeImageWidth > 0 || CapeImageHeight > 0))
        {
            if (CapeImageWidth <= 0 || CapeImageHeight <= 0 || !IsValidCapeDimension(CapeImageWidth, CapeImageHeight))
            {
                error = $"Invalid cape resolution: {CapeImageWidth}x{CapeImageHeight}. Allowed: 64x32, 128x128.";
                return false;
            }

            long expectedCapeBytes = (long)CapeImageWidth * CapeImageHeight * 4;
            if (CapeImage.Length != expectedCapeBytes)
            {
                error = $"Cape image size mismatch: expected {expectedCapeBytes} bytes ({CapeImageWidth}x{CapeImageHeight} RGBA), got {CapeImage.Length} bytes.";
                return false;
            }
        }

        error = null;
        return true;
    }

    public static bool IsValidSkinDimension(int width, int height)
    {
        return (width == 64 && height == 32) ||
               (width == 64 && height == 64) ||
               (width == 128 && height == 128);
    }

    public static bool IsValidCapeDimension(int width, int height)
    {
        return (width == 64 && height == 32) ||
               (width == 128 && height == 128);
    }
}
