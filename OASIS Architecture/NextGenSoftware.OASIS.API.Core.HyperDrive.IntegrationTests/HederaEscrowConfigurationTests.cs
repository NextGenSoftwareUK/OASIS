using System.Text.Json;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.HashgraphOASIS;
using NextGenSoftware.OASIS.OASISBootLoader;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed class HederaEscrowConfigurationTests
{
    [Fact]
    public void OmittedProtocolKeepsExistingAggregateSelection()
    {
        var settings=JsonSerializer.Deserialize<HashgraphOASISProviderSettings>("{}")!;
        Assert.Equal("aggregate-v1",settings.TokenEscrowProtocol);
        Assert.Equal(HederaTokenEscrowProtocol.AggregateV1,HederaEscrowConfiguration.Create(settings).Protocol);
    }

    [Theory]
    [InlineData("aggregate-v1",HederaTokenEscrowProtocol.AggregateV1)]
    [InlineData("id-bound-v1",HederaTokenEscrowProtocol.IdBoundV1)]
    public void JsonMappingCapturesOnlyExplicitProtocolAndConfiguredBounds(string protocol,HederaTokenEscrowProtocol expected)
    {
        var settings=JsonSerializer.Deserialize<HashgraphOASISProviderSettings>(JsonSerializer.Serialize(new HashgraphOASISProviderSettings{
            TokenEscrowProtocol=protocol,TokenEscrowContractId="0.0.2001",TokenEscrowRuntimeCodeHash="0x"+new string('1',64),
            TokenEscrowGasLimit=400000,TokenEscrowMaxFeeTinybars=100000000}))!;
        var captured=HederaEscrowConfiguration.Create(settings);
        settings.TokenEscrowProtocol="changed";settings.TokenEscrowContractId="changed";settings.TokenEscrowMaxFeeTinybars=1;
        Assert.Equal(expected,captured.Protocol);Assert.Equal("0.0.2001",captured.ContractId);
        Assert.Equal("0x"+new string('1',64),captured.RuntimeCodeHash);Assert.Equal(400000,captured.GasLimit);
        Assert.Equal(100000000,captured.MaxFeeTinybars);
        Assert.False(new HashgraphOASIS(tokenEscrowConfiguration:captured).IsProviderActivated);
    }

    [Theory]
    [InlineData("unknown")] [InlineData("")] [InlineData(null)] [InlineData("ID-BOUND-V1")]
    public void InvalidExplicitProtocolCannotFallBackToAggregate(string? protocol)
    {
        Assert.Throws<ArgumentException>(()=>HederaEscrowConfiguration.Create(new(){TokenEscrowProtocol=protocol!}));
    }
}
