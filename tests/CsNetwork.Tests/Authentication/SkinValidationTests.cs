using System;
using CsNetwork.Authentication;
using Xunit;

namespace CsNetwork.Tests.Authentication;

public sealed class SkinValidationTests
{
    [Theory]
    [InlineData(64, 32, 64 * 32 * 4)]
    [InlineData(64, 64, 64 * 64 * 4)]
    [InlineData(128, 128, 128 * 128 * 4)]
    public void Validate_StandardSkinResolutions_Succeeds(int width, int height, int expectedBytes)
    {
        byte[] skinBytes = new byte[expectedBytes];
        var skin = new SkinData("Standard_Skin", skinBytes, width, height);

        bool valid = skin.Validate(out string? error);
        Assert.True(valid, error);
    }

    [Fact]
    public void Validate_BufferLengthMismatch_Fails()
    {
        byte[] invalidBytes = new byte[100];
        var skin = new SkinData("Invalid_Buffer", invalidBytes, 64, 64);

        bool valid = skin.Validate(out string? error);
        Assert.False(valid);
        Assert.Contains("Skin image size mismatch", error, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_UnsupportedResolution_Fails()
    {
        byte[] bytes = new byte[32 * 32 * 4];
        var skin = new SkinData("Invalid_Res", bytes, 32, 32);

        bool valid = skin.Validate(out string? error);
        Assert.False(valid);
        Assert.Contains("Invalid skin resolution", error, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_EmptySkinId_Fails()
    {
        byte[] skinBytes = new byte[64 * 64 * 4];
        var skin = new SkinData("", skinBytes, 64, 64);

        bool valid = skin.Validate(out string? error);
        Assert.False(valid);
        Assert.Contains("SkinId must not be empty", error, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_ValidCape_Succeeds()
    {
        byte[] skinBytes = new byte[64 * 64 * 4];
        byte[] capeBytes = new byte[64 * 32 * 4];
        var skin = new SkinData("SkinWithCape", skinBytes, 64, 64, capeBytes, 64, 32, "Cape_123");

        bool valid = skin.Validate(out string? error);
        Assert.True(valid, error);
    }

    [Fact]
    public void Validate_InvalidCapeLength_Fails()
    {
        byte[] skinBytes = new byte[64 * 64 * 4];
        byte[] invalidCape = new byte[50];
        var skin = new SkinData("SkinWithBadCape", skinBytes, 64, 64, invalidCape, 64, 32);

        bool valid = skin.Validate(out string? error);
        Assert.False(valid);
        Assert.Contains("Cape image size mismatch", error, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_CapeIntegerOverflowDimension_Fails()
    {
        byte[] skinBytes = new byte[64 * 64 * 4];
        byte[] emptyCape = [];
        // 32768 * 32768 * 4 overflows standard 32-bit int to 0
        var skin = new SkinData("OverflowCape", skinBytes, 64, 64, emptyCape, 32768, 32768);

        bool valid = skin.Validate(out string? error);
        Assert.False(valid);
        Assert.Contains("Invalid cape resolution", error, StringComparison.Ordinal);
    }
}
