using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class EthereumEscrowConfigurationTests
{
    [Fact]
    public void EscrowConfigurationRoundTripsSeparatelyFromStorageContract()
    {
        var dna=new OASISDNA();
        dna.OASIS.StorageProviders=new StorageProviderSettings {
            EthereumOASIS=new EthereumOASISProviderSettings {
                ContractAddress="0x"+new string('1',40),TokenEscrowContractAddress="0x"+new string('2',40) } };
        var restored=JsonConvert.DeserializeObject<OASISDNA>(JsonConvert.SerializeObject(dna))!;
        Assert.Equal(dna.OASIS.StorageProviders.EthereumOASIS.ContractAddress,restored.OASIS.StorageProviders.EthereumOASIS.ContractAddress);
        Assert.Equal(dna.OASIS.StorageProviders.EthereumOASIS.TokenEscrowContractAddress,restored.OASIS.StorageProviders.EthereumOASIS.TokenEscrowContractAddress);
        Assert.NotEqual(restored.OASIS.StorageProviders.EthereumOASIS.ContractAddress,restored.OASIS.StorageProviders.EthereumOASIS.TokenEscrowContractAddress);
        Assert.Null(new EthereumOASISProviderSettings().TokenEscrowContractAddress);
    }
}
