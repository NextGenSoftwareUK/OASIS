using System.Text.Json;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.OASISBootLoader;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed class SuiEscrowConfigurationTests
{
    [Theory]
    [InlineData("{}")] [InlineData("{\"IdBoundTokenEscrow\":null}")]
    public void OmissionDoesNotEnablePerDepositCustody(string json)
        => Assert.Null(SuiEscrowConfiguration.Create(JsonSerializer.Deserialize<SuiOASISProviderSettings>(json)!));

    [Fact]
    public void ExplicitJsonSnapshotsCanonicalObjectWithoutStorageActivation()
    {
        var id = "0x" + new string('2', 64);
        var settings = JsonSerializer.Deserialize<SuiOASISProviderSettings>(JsonSerializer.Serialize(new SuiOASISProviderSettings {
            IdBoundTokenEscrow = new() { EscrowObjectId = id }
        }))!;
        var selected = SuiEscrowConfiguration.Create(settings)!;
        settings.IdBoundTokenEscrow.EscrowObjectId = "changed";
        Assert.Equal(id, selected.EscrowObjectId);
        using var provider = new API.Providers.SuiOASIS.SuiOASIS(contractAddress: "0x1", tokenEscrow: selected);
        Assert.False(provider.IsProviderActivated);
    }

    [Theory]
    [InlineData("")] [InlineData("0x2")] [InlineData(null)]
    [InlineData("0xAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
    public void InvalidExplicitObjectCannotFallBackToAggregate(string? id)
        => Assert.Throws<ArgumentException>(() => SuiEscrowConfiguration.Create(new() {
            IdBoundTokenEscrow = new() { EscrowObjectId = id! }
        }));
}
