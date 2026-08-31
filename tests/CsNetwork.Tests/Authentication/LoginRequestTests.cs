using System;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using CsNetwork.Authentication;
using CsNetwork.Cryptography;
using Xunit;

namespace CsNetwork.Tests.Authentication;

public sealed class LoginRequestTests
{
    [Fact]
    public void CreateOfflineAndParse_Roundtrip_Succeeds()
    {
        using var ecdh = EcdhKeyPair.Create();
        Guid identity = Guid.NewGuid();
        string displayName = "CustomPlayer";
        byte[] skinImage = new byte[SkinData.Skin64x64Length];
        RandomNumberGenerator.Fill(skinImage);

        var skin = new SkinData("Custom_Skin_Id", skinImage, 64, 64);

        byte[] payload = LoginRequest.CreateOffline(
            displayName,
            identity,
            ecdh,
            skin,
            gameVersion: "1.21.60",
            deviceOS: DeviceOS.Win10,
            serverAddress: "play.example.com:19132");

        Assert.NotNull(payload);
        Assert.True(payload.Length > 0);

        bool parseSuccess = LoginRequest.TryParse(payload, out var result, out string? error);
        Assert.True(parseSuccess, error);
        Assert.NotNull(result);

        Assert.Equal(displayName, result.Identity.DisplayName);
        Assert.Equal(identity, result.Identity.Identity);
        Assert.False(result.IsXboxLiveAuthenticated);

        Assert.Equal(DeviceOS.Win10, result.Client.DeviceOS);
        Assert.Equal("1.21.60", result.Client.GameVersion);
        Assert.Equal("play.example.com:19132", result.Client.ServerAddress);
        Assert.Equal("Custom_Skin_Id", result.Client.Skin.SkinId);
        Assert.Equal(64, result.Client.Skin.SkinImageWidth);
        Assert.Equal(64, result.Client.Skin.SkinImageHeight);
        Assert.Equal(skinImage, result.Client.Skin.SkinImage);

        // verify 97-byte uncompressed public key
        Assert.Equal(97, result.ClientPublicKeyUncompressed.Length);
        Assert.Equal(0x04, result.ClientPublicKeyUncompressed[0]);
    }

    [Fact]
    public void CreateOffline_DisplayNameWithQuotesAndSpecialChars_Succeeds()
    {
        using var ecdh = EcdhKeyPair.Create();
        Guid identity = Guid.NewGuid();
        string displayName = "Player \"P\""; // 10 chars (valid offline name)
        byte[] skinImage = new byte[SkinData.Skin64x32Length];

        var skin = new SkinData("TestSkin", skinImage, 64, 32);

        byte[] payload = LoginRequest.CreateOffline(
            displayName,
            identity,
            ecdh,
            skin);

        bool parseSuccess = LoginRequest.TryParse(payload, out var result, out string? error);
        Assert.True(parseSuccess, error);
        Assert.NotNull(result);
        Assert.Equal(displayName, result.Identity.DisplayName);
    }

    [Fact]
    public void TryParse_GuestModeAuthenticationType_Rejects()
    {
        string guestJson = "{\"AuthenticationType\":1,\"chain\":[\"token\"]}";
        byte[] guestBytes = Encoding.UTF8.GetBytes(guestJson);
        byte[] clientBytes = "fake.jwt.token"u8.ToArray();

        byte[] payload = new byte[4 + guestBytes.Length + 4 + clientBytes.Length];
        BinaryPrimitives.WriteInt32LittleEndian(payload, guestBytes.Length);
        guestBytes.CopyTo(payload.AsSpan(4));

        int clientOffset = 4 + guestBytes.Length;
        BinaryPrimitives.WriteInt32LittleEndian(payload.AsSpan(clientOffset), clientBytes.Length);
        clientBytes.CopyTo(payload.AsSpan(clientOffset + 4));

        bool parseSuccess = LoginRequest.TryParse(payload, out _, out string? error);
        Assert.False(parseSuccess);
        Assert.Contains("Guest authentication mode", error, StringComparison.Ordinal);
    }

    [Fact]
    public void TryParse_TruncatedPayload_Fails()
    {
        byte[] truncated = [0x05, 0x00, 0x00, 0x00];
        bool success = LoginRequest.TryParse(truncated, out _, out string? error);
        Assert.False(success);
        Assert.Contains("too short", error, StringComparison.Ordinal);
    }

    [Fact]
    public void TryParse_InvalidChainLength_Fails()
    {
        byte[] invalid = [0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];
        bool success = LoginRequest.TryParse(invalid, out _, out string? error);
        Assert.False(success);
        Assert.Contains("Invalid certificate chain length", error, StringComparison.Ordinal);
    }
}
