using NextGenSoftware.OASIS.API.Core.Enums;
using Xunit;

namespace NextGenSoftware.OASIS.OASISBootLoader.UnitTests;

public sealed class FirestoreFixtureFactAttribute : FactAttribute
{
    public FirestoreFixtureFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("OASIS_FIRESTORE_BOOTLOADER_TEST") != "1")
            Skip = "Requires the dedicated Firestore emulator fixture.";
    }
}

[Collection("BootLoader registration state")]
public sealed class FirestoreNativeStartupTests
{
    [FirestoreFixtureFact]
    public async Task BootLoaderActivatesAndDeactivatesFirestore()
    {
        Assert.Equal("127.0.0.1:18180", Environment.GetEnvironmentVariable("FIRESTORE_EMULATOR_HOST"));
        var dna = Environment.GetEnvironmentVariable("OASIS_FIRESTORE_BOOTLOADER_DNA");
        Assert.False(string.IsNullOrWhiteSpace(dna));
        var boot = await OASISBootLoader.BootOASISForAdmittedStorageAsync(dna!);
        Assert.False(boot.IsError, boot.Message);
        Assert.True(boot.Result);

        var activation = await OASISBootLoader.GetAndActivateStorageProviderAsync(ProviderType.GoogleCloudOASIS);
        Assert.False(activation.IsError, activation.Message);
        var provider = Assert.IsAssignableFrom<NextGenSoftware.OASIS.API.Core.Interfaces.IOASISStorageProvider>(activation.Result);
        try
        {
            Assert.Equal(ProviderType.GoogleCloudOASIS, provider.ProviderType.Value);
            Assert.True(provider.IsProviderActivated);
        }
        finally
        {
            var deactivation = await provider.DeActivateProviderAsync();
            Assert.False(deactivation.IsError, deactivation.Message);
            Assert.True(deactivation.Result);
            Assert.False(provider.IsProviderActivated);
        }
    }
}
