using System;
using CsNetwork.Authentication;
using Xunit;

namespace CsNetwork.Tests.Authentication;

public sealed class ClientDataTests
{
    private static SkinData CreateValidSkin() => new("ValidSkin", new byte[SkinData.Skin64x64Length], 64, 64);

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Validate_ValidUIProfile_Succeeds(int uiProfile)
    {
        var client = new ClientData(
            DeviceOS.Win10,
            "PC",
            "device-id",
            "1.21.60",
            0,
            uiProfile,
            123456L,
            "127.0.0.1:19132",
            "en_US",
            CreateValidSkin());

        Assert.True(client.Validate(out string? error), error);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    [InlineData(99)]
    public void Validate_InvalidUIProfile_Fails(int uiProfile)
    {
        var client = new ClientData(
            DeviceOS.Win10,
            "PC",
            "device-id",
            "1.21.60",
            0,
            uiProfile,
            123456L,
            "127.0.0.1:19132",
            "en_US",
            CreateValidSkin());

        Assert.False(client.Validate(out string? error));
        Assert.Contains("UIProfile value", error, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(13)]
    [InlineData(15)]
    public void Validate_ValidDeviceOS_Succeeds(int os)
    {
        var client = new ClientData(
            (DeviceOS)os,
            "Model",
            "device-id",
            "1.21.60",
            0,
            0,
            123456L,
            "127.0.0.1:19132",
            "en_US",
            CreateValidSkin());

        Assert.True(client.Validate(out string? error), error);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(16)]
    public void Validate_InvalidDeviceOS_Fails(int os)
    {
        var client = new ClientData(
            (DeviceOS)os,
            "Model",
            "device-id",
            "1.21.60",
            0,
            0,
            123456L,
            "127.0.0.1:19132",
            "en_US",
            CreateValidSkin());

        Assert.False(client.Validate(out string? error));
        Assert.Contains("DeviceOS value", error, StringComparison.Ordinal);
    }
}
