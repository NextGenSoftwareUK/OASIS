using System.Text.Json;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.OASISBootLoader;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed class CosmosEscrowConfigurationTests
{
    [Theory]
    [InlineData("{}")] [InlineData("{\"IdBoundTokenEscrow\":null}")]
    public void OmittedOrNullConfigurationDoesNotEnableDeposits(string json)
        => Assert.Null(CosmosEscrowConfiguration.Create(JsonSerializer.Deserialize<CosmosBlockChainOASISProviderSettings>(json)!));

    [Fact]
    public void ExplicitJsonCapturesImmutableReviewedContractAndChecksum()
    {
        var settings = JsonSerializer.Deserialize<CosmosBlockChainOASISProviderSettings>(JsonSerializer.Serialize(new CosmosBlockChainOASISProviderSettings {
            ContractAddress = "fixture-contract", IdBoundTokenEscrow = new() { ContractAddress = "fixture-contract", CodeSha256 = new string('c', 64) }
        }))!;
        var selected = CosmosEscrowConfiguration.Create(settings)!;
        settings.IdBoundTokenEscrow.ContractAddress = "changed"; settings.IdBoundTokenEscrow.CodeSha256 = "changed";
        Assert.Equal("fixture-contract", selected.ContractAddress); Assert.Equal(new string('c', 64), selected.CodeSha256);
        using var provider = new API.Providers.CosmosBlockChainOASIS.CosmosBlockChainOASIS(contractAddress: "fixture-contract", tokenEscrow: selected);
        Assert.False(provider.IsProviderActivated);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("wrong", "cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc")]
    [InlineData("fixture-contract", "CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC")]
    [InlineData("fixture-contract", "c")]
    public void InvalidExplicitConfigurationCannotDowngradeToFreeze(string address, string checksum)
        => Assert.Throws<ArgumentException>(() => CosmosEscrowConfiguration.Create(new() { ContractAddress = "fixture-contract",
            IdBoundTokenEscrow = new() { ContractAddress = address, CodeSha256 = checksum } }));
}
