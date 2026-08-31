using System;
using CsNetwork.Authentication;
using Xunit;

namespace CsNetwork.Tests.Authentication;

public sealed class IdentityValidationTests
{
    [Theory]
    [InlineData("Player1")]
    [InlineData("Steve")]
    [InlineData("Alex")]
    [InlineData("Valid Player")]
    public void Validate_ValidOfflineName_Succeeds(string name)
    {
        var identity = new IdentityData(name, Guid.NewGuid());
        bool valid = identity.Validate(out string? error);
        Assert.True(valid, error);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(" Player")]
    [InlineData("Player ")]
    [InlineData("123Player")]
    [InlineData("Player  Two")]
    [InlineData("NameIsTooLongToBeValid123")]
    public void Validate_InvalidDisplayName_Fails(string invalidName)
    {
        var identity = new IdentityData(invalidName, Guid.NewGuid());
        bool valid = identity.Validate(out string? error);
        Assert.False(valid);
        Assert.NotNull(error);
    }

    [Fact]
    public void Validate_EmptyGuid_Fails()
    {
        var identity = new IdentityData("Steve", Guid.Empty);
        bool valid = identity.Validate(out string? error);
        Assert.False(valid);
        Assert.Contains("must not be empty", error, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_InvalidXuid_Fails()
    {
        var identity = new IdentityData("Steve", Guid.NewGuid(), Xuid: "not_a_number");
        bool valid = identity.Validate(out string? error);
        Assert.False(valid);
        Assert.Contains("must be parseable as a 64-bit integer", error, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_ValidXuid_Succeeds()
    {
        var identity = new IdentityData("Steve", Guid.NewGuid(), Xuid: "2535412345678901");
        bool valid = identity.Validate(out string? error);
        Assert.True(valid, error);
    }
}
