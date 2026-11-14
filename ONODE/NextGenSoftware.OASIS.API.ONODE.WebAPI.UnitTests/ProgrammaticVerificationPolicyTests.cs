using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services;
using Xunit;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

/// <summary>A non-Wizard's suppressVerificationEmail is honoured only for a .invalid email on a dev server with the flag on.</summary>
public sealed class ProgrammaticVerificationPolicyTests
{
    private static Func<string, string> Env(params (string Key, string Value)[] vars)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (k, v) in vars) map[k] = v;
        return key => map.TryGetValue(key, out var v) ? v : null;
    }

    private static readonly Func<string, string> Dev = Env(("ASPNETCORE_ENVIRONMENT", "Development"));
    private static readonly Func<string, string> Live = Env(("ASPNETCORE_ENVIRONMENT", "Production"));

    [Fact] public void DevPlusInvalidEmailPlusFlagIsHonoured() => Assert.True(ProgrammaticVerificationPolicy.Honour("staff1@piper.invalid", true, Dev));
    [Fact] public void LivePlusInvalidEmailIsRefused() => Assert.False(ProgrammaticVerificationPolicy.Honour("staff1@piper.invalid", true, Live));
    [Fact] public void DevPlusNormalEmailIsRefused() => Assert.False(ProgrammaticVerificationPolicy.Honour("someone@gmail.com", true, Dev));
    [Fact] public void FlagOffIsRefusedEvenOnDevWithInvalidEmail() => Assert.False(ProgrammaticVerificationPolicy.Honour("staff1@piper.invalid", false, Dev));
    [Fact] public void NoEnvironmentSignalFailsClosed() => Assert.False(ProgrammaticVerificationPolicy.Honour("staff1@piper.invalid", true, Env()));

    [Theory]
    [InlineData("someone@gmail.com")]
    [InlineData("staff1@piper.invalid")]
    public void WizardIsUnaffectedByEnvironmentEmailOrFlag(string email)
    {
        Assert.True(ProgrammaticVerificationPolicy.ShouldSuppress(true, true, email, false, Live));
        Assert.True(ProgrammaticVerificationPolicy.ShouldSuppress(true, true, email, true, Dev));
    }

    [Fact]
    public void NothingIsSuppressedWhenNotRequested()
    {
        Assert.False(ProgrammaticVerificationPolicy.ShouldSuppress(false, true, "a@b.invalid", true, Dev));
        Assert.False(ProgrammaticVerificationPolicy.ShouldSuppress(false, false, "a@b.invalid", true, Dev));
    }

    [Fact]
    public void NonWizardNeedsEveryCondition()
    {
        Assert.True(ProgrammaticVerificationPolicy.ShouldSuppress(true, false, "a@b.invalid", true, Dev));
        Assert.False(ProgrammaticVerificationPolicy.ShouldSuppress(true, false, "a@b.invalid", true, Live));
        Assert.False(ProgrammaticVerificationPolicy.ShouldSuppress(true, false, "a@b.com", true, Dev));
        Assert.False(ProgrammaticVerificationPolicy.ShouldSuppress(true, false, "a@b.invalid", false, Dev));
    }

    [Theory]
    [InlineData("a@b.invalid.com")]
    [InlineData("a.invalid@b.com")]
    [InlineData("a@evil.com.invalid.evil.com")]
    [InlineData("a@b.invalid@evil.com")]
    [InlineData("a@evil.com,b@c.invalid")]
    [InlineData("a@evil.com b@c.invalid")]
    [InlineData("a@c.invalid;b@evil.com")]
    [InlineData("a@c.invalid.")]
    [InlineData("a@.invalid")]
    [InlineData("a@invalid")]
    [InlineData("a@c.invalid#")]
    [InlineData("@c.invalid")]
    [InlineData("a@")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void TrickyEmailsAreRefused(string email) => Assert.False(ProgrammaticVerificationPolicy.IsInvalidTestEmail(email));

    [Theory]
    [InlineData("staff1@piper.invalid")]
    [InlineData("Staff.One+x@PIPER.INVALID")]
    [InlineData(" staff1@a.b.invalid ")]
    public void WholeDomainInvalidSuffixIsAccepted(string email) => Assert.True(ProgrammaticVerificationPolicy.IsInvalidTestEmail(email));

    [Theory]
    [InlineData("Development")]
    [InlineData("development")]
    public void AspNetCoreDevelopmentCountsAsDev(string value) => Assert.True(ProgrammaticVerificationPolicy.IsDevelopmentEnvironment(Env(("ASPNETCORE_ENVIRONMENT", value))));

    [Theory]
    [InlineData("development")] [InlineData("Dev")]
    public void ExplicitOasisEnvironmentCountsAsDevEvenWhenAspNetIsProduction(string value) =>
        Assert.True(ProgrammaticVerificationPolicy.IsDevelopmentEnvironment(Env(("OASIS_ENVIRONMENT", value), ("ASPNETCORE_ENVIRONMENT", "Production"))));

    [Theory]
    [InlineData("development")] [InlineData("dev")]
    public void RailwayDevelopmentEnvironmentCountsAsDev(string value) =>
        Assert.True(ProgrammaticVerificationPolicy.IsDevelopmentEnvironment(Env(("RAILWAY_ENVIRONMENT_NAME", value))));

    [Theory]
    [InlineData("production")] [InlineData("Prod")] [InlineData("live")]
    public void ExplicitLiveOverridesEveryDevSignal(string value)
    {
        Assert.False(ProgrammaticVerificationPolicy.IsDevelopmentEnvironment(Env(("OASIS_ENVIRONMENT", value), ("ASPNETCORE_ENVIRONMENT", "Development"))));
        Assert.False(ProgrammaticVerificationPolicy.IsDevelopmentEnvironment(Env(("RAILWAY_ENVIRONMENT_NAME", value), ("OASIS_ENVIRONMENT", "dev"))));
        Assert.False(ProgrammaticVerificationPolicy.IsDevelopmentEnvironment(Env(("RAILWAY_ENVIRONMENT_NAME", value), ("ASPNETCORE_ENVIRONMENT", "Development"))));
    }

    [Theory]
    [InlineData(null)] [InlineData("")] [InlineData("Production")] [InlineData("Staging")] [InlineData("Testing")] [InlineData("devel")]
    public void UnknownOrMissingEnvironmentIsLive(string value) =>
        Assert.False(ProgrammaticVerificationPolicy.IsDevelopmentEnvironment(Env(("ASPNETCORE_ENVIRONMENT", value), ("OASIS_ENVIRONMENT", value))));
}
