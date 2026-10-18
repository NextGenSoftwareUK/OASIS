using System.Text.Json;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.TRONOASIS;
using NextGenSoftware.OASIS.OASISBootLoader;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed class TronEscrowConfigurationTests
{
    [Fact]
    public void OmittedConfigurationDoesNotEnablePerDepositProtocol()
    {
        var settings=JsonSerializer.Deserialize<TRONOASISProviderSettings>("{}");
        Assert.Null(settings!.IdBoundTokenEscrow);Assert.Null(TronEscrowConfiguration.Create(settings.IdBoundTokenEscrow));
    }

    [Fact]
    public void JsonMappingSnapshotsExplicitNonSecretSettings()
    {
        var address=new TronNet.TronECKey(new string('1',64),TronNet.TronNetwork.MainNet).GetPublicAddress();
        var settings=JsonSerializer.Deserialize<TRONOASISProviderSettings>(JsonSerializer.Serialize(new TRONOASISProviderSettings{
            IdBoundTokenEscrow=new(){ContractAddress=address,RuntimeSha256=new string('a',64)}}))!;
        var mapped=TronEscrowConfiguration.Create(settings.IdBoundTokenEscrow);
        settings.IdBoundTokenEscrow.ContractAddress="changed";settings.IdBoundTokenEscrow.RuntimeSha256="changed";
        Assert.Equal(address,mapped.ContractAddress);Assert.Equal(new string('a',64),mapped.RuntimeSha256);
        Assert.False(new TRONOASIS(idBoundEscrow:mapped).IsProviderActivated);
    }

    [Theory]
    [InlineData("address")] [InlineData("hash")] [InlineData("shared-contract")]
    public void InvalidOrSharedConfigurationCannotConstructProvider(string field)
    {
        var address=new TronNet.TronECKey(new string('1',64),TronNet.TronNetwork.MainNet).GetPublicAddress();
        var settings=new TronIdBoundEscrowSettings{ContractAddress=field=="address"?"invalid":address,
            RuntimeSha256=field=="hash"?"invalid":new string('a',64)};
        Assert.ThrowsAny<Exception>(()=>new TRONOASIS(escrowContractAddress:field=="shared-contract"?address:"",
            idBoundEscrow:TronEscrowConfiguration.Create(settings)));
    }
}
