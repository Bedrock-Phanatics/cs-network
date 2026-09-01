using System;
using System.Text.Json;
using CsNetwork.ResourcePacks;
using Xunit;

namespace CsNetwork.Tests.ResourcePacks;

public sealed class PackVersionTests
{
    [Fact]
    public void JsonDeserialize_ArrayFormat_Succeeds()
    {
        string json = "[1, 2, 3]";
        var version = JsonSerializer.Deserialize<PackVersion>(json);

        Assert.Equal(new PackVersion(1, 2, 3), version);
        Assert.Equal("1.2.3", version.ToString());
    }

    [Fact]
    public void JsonDeserialize_NegativeArrayFormat_ThrowsJsonException()
    {
        string json = "[-1, 2, 3]";
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<PackVersion>(json));
    }

    [Fact]
    public void JsonDeserialize_StringFormat_Succeeds()
    {
        string json = "\"2.4.6\"";
        var version = JsonSerializer.Deserialize<PackVersion>(json);

        Assert.Equal(new PackVersion(2, 4, 6), version);
        Assert.Equal("2.4.6", version.ToString());
    }

    [Fact]
    public void JsonSerialize_WritesArrayFormat()
    {
        var version = new PackVersion(1, 0, 0);
        string json = JsonSerializer.Serialize(version);

        Assert.Equal("[1,0,0]", json);
    }

    [Theory]
    [InlineData("1.0.0", 1, 0, 0)]
    [InlineData("0.12.34", 0, 12, 34)]
    [InlineData(" 10.20.30 ", 10, 20, 30)]
    public void TryParse_ValidStrings_Succeeds(string input, int expectedMajor, int expectedMinor, int expectedPatch)
    {
        bool success = PackVersion.TryParse(input, out var version);
        Assert.True(success);
        Assert.Equal(new PackVersion(expectedMajor, expectedMinor, expectedPatch), version);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1")]
    [InlineData("1.0")]
    [InlineData("1.0.0.0")]
    [InlineData("a.b.c")]
    [InlineData("-1.0.0")]
    public void TryParse_InvalidStrings_Fails(string input)
    {
        bool success = PackVersion.TryParse(input, out var version);
        Assert.False(success);
        Assert.Equal(PackVersion.Empty, version);
    }

    [Fact]
    public void Comparison_OrdersCorrectly()
    {
        var v1 = new PackVersion(1, 0, 0);
        var v2 = new PackVersion(1, 1, 0);
        var v3 = new PackVersion(1, 1, 1);
        var v4 = new PackVersion(2, 0, 0);

        Assert.True(v1 < v2);
        Assert.True(v2 < v3);
        Assert.True(v3 < v4);
        Assert.True(v4 > v1);
        Assert.True(v1 <= new PackVersion(1, 0, 0));
        Assert.True(v1 >= new PackVersion(1, 0, 0));
    }
}
