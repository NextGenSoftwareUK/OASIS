using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Providers.ThreeFoldOASIS;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.ThreeFoldOASIS.IntegrationTests;

public sealed class ThreeFoldOASISIntegrationTests
{
    private static ThreeFoldOASIS CreateProvider()
    {
        string endpoint = Environment.GetEnvironmentVariable("THREEFOLD_QSS_ENDPOINT")
            ?? throw new InvalidOperationException("THREEFOLD_QSS_ENDPOINT must identify a running ThreeFold QSS S3-CAS server.");
        string accessKey = Environment.GetEnvironmentVariable("THREEFOLD_QSS_ACCESS_KEY")
            ?? throw new InvalidOperationException("THREEFOLD_QSS_ACCESS_KEY is required.");
        string secretKey = Environment.GetEnvironmentVariable("THREEFOLD_QSS_SECRET_KEY")
            ?? throw new InvalidOperationException("THREEFOLD_QSS_SECRET_KEY is required.");
        string bucket = Environment.GetEnvironmentVariable("THREEFOLD_QSS_BUCKET") ?? "oasis-integration";
        bool useSsl = bool.TryParse(Environment.GetEnvironmentVariable("THREEFOLD_QSS_USE_SSL"), out bool configured) && configured;
        return new ThreeFoldOASIS(endpoint, accessKey, secretKey, bucket, useSsl);
    }

    [Fact]
    public async Task QssAvatarCrud_RoundTripsThroughOfficialS3Protocol()
    {
        var provider = CreateProvider();
        var activation = await provider.ActivateProviderAsync();
        Assert.False(activation.IsError, activation.Message);
        Assert.True(activation.Result);
        Assert.Equal(ProviderType.ThreeFoldOASIS, provider.ProviderType.Value);

        var avatar = new Avatar
        {
            Id = Guid.NewGuid(),
            Username = $"qss-{Guid.NewGuid():N}",
            Email = $"qss-{Guid.NewGuid():N}@integration.invalid"
        };

        var save = await provider.SaveAvatarAsync(avatar);
        Assert.False(save.IsError, save.Message);
        Assert.Equal(avatar.Id.ToString(), save.Result.ProviderUniqueStorageKey[ProviderType.ThreeFoldOASIS]);

        var load = await provider.LoadAvatarAsync(avatar.Id);
        Assert.False(load.IsError, load.Message);
        Assert.Equal(avatar.Username, load.Result.Username);

        var delete = await provider.DeleteAvatarAsync(avatar.Id, softDelete: false);
        Assert.False(delete.IsError, delete.Message);
        Assert.True(delete.Result);
        await provider.DeActivateProviderAsync();
    }

    [Fact]
    public async Task QssHolonCrud_RoundTripsThroughOfficialS3Protocol()
    {
        var provider = CreateProvider();
        var activation = await provider.ActivateProviderAsync();
        Assert.False(activation.IsError, activation.Message);

        var holon = new Holon { Id = Guid.NewGuid(), Name = $"QSS holon {Guid.NewGuid():N}" };
        var save = await provider.SaveHolonAsync(holon);
        Assert.False(save.IsError, save.Message);
        Assert.Equal(holon.Id.ToString(), save.Result.ProviderUniqueStorageKey[ProviderType.ThreeFoldOASIS]);

        var load = await provider.LoadHolonAsync(holon.Id);
        Assert.False(load.IsError, load.Message);
        Assert.Equal(holon.Name, load.Result.Name);

        var delete = await provider.DeleteHolonAsync(holon.Id);
        Assert.False(delete.IsError, delete.Message);
        Assert.Equal(holon.Id, delete.Result.Id);
        await provider.DeActivateProviderAsync();
    }
}
