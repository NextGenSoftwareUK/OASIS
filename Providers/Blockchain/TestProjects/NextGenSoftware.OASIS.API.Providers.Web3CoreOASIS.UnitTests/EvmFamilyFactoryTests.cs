using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.OASISBootLoader;
using NextGenSoftware.OASIS.API.Providers.EthereumOASIS;
using System.Numerics;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.UnitTests;

[TestClass]
public class EvmFamilyFactoryTests
{
    [DataTestMethod] [DataRow(false)] [DataRow(true)]
    public void EthereumV2UsesSharedOwnerAndPreservesSettingsWithoutActivation(bool history)
    {
        var settings=new EthereumOASISProviderSettings{ConnectionString="http://127.0.0.1:1",ChainId=31337,
            ChainPrivateKey="synthetic-not-used-before-activation",ContractAddress="0x0000000000000000000000000000000000000001",
            TokenEscrowContractAddress="0x0000000000000000000000000000000000000002",IsVersionControlEnabled=history};
        var p=EthereumProviderFactory.CreateForMode(settings,HyperDriveModes.V2,null!);
        Assert.IsInstanceOfType(p,typeof(EthereumOASIS_Web3Core));var shared=(EthereumOASIS_Web3Core)p;
        Assert.IsFalse(p.IsProviderActivated);Assert.AreEqual("EthereumOASIS",p.ProviderName);
        Assert.AreEqual(Core.Enums.ProviderType.EthereumOASIS,p.ProviderType.Value);
        Assert.AreEqual(settings.TokenEscrowContractAddress,shared.TokenEscrowContractAddress);Assert.AreEqual(history,shared.IsVersionControlEnabled);
        Assert.AreEqual(typeof(Web3CoreOASISBaseProvider),p.GetType().GetMethod("SaveAvatarAsync")!.DeclaringType);
    }
    [TestMethod]
    public void ExplicitLegacySelectionKeepsExistingImplementation()
    {
        var p=EthereumProviderFactory.CreateForMode(new EthereumOASISProviderSettings{ConnectionString="http://127.0.0.1:1",ChainId=31337},"legacy",null!);
        Assert.IsInstanceOfType(p,typeof(NextGenSoftware.OASIS.API.Providers.EthereumOASIS.EthereumOASIS));Assert.IsFalse(p.IsProviderActivated);
    }
    [TestMethod]
    public void SharedEthereumRequiresExplicitPositiveChainIdentity()
        =>Assert.ThrowsException<ArgumentOutOfRangeException>(()=>new EthereumOASIS_Web3Core("http://127.0.0.1:1","","",BigInteger.Zero));
}
