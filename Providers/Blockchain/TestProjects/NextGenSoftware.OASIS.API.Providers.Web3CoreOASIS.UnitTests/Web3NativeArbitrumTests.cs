using System.Numerics;
using System.Reflection;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.UnitTests;

public partial class Web3NativeStorageTests
{
    [TestMethod]
    public async Task ActualArbitrumWrapperEncryptsRecordsAndRunsV2Policies()
    {
        Assert.AreEqual("http://127.0.0.1:18545",Environment.GetEnvironmentVariable("ETHEREUMOASIS_TEST_RPC"));
        var key=Environment.GetEnvironmentVariable("ETHEREUMOASIS_TEST_PRIVATE_KEY")!;
        var web3=new Web3(new Account(key,31337),"http://127.0.0.1:18545");
        Assert.AreEqual(new BigInteger(31337),(await web3.Eth.ChainId.SendRequestAsync()).Value);
        var deployment=await web3.Eth.GetContractDeploymentHandler<Deployment>()
            .SendRequestAndWaitForReceiptAsync(new Deployment {Gas=new HexBigInteger(8000000)});
        Assert.IsTrue(deployment.Succeeded());
        var provider=new ArbitrumOASIS.ArbitrumOASIS_Web3Core("http://127.0.0.1:18545",key,deployment.ContractAddress,31337);
        try
        {
            var activation=await provider.ActivateProviderAsync();Assert.IsFalse(activation.IsError,activation.Message);
            Assert.AreEqual(ProviderType.ArbitrumOASIS,provider.ProviderType.Value);
            var avatar=new Avatar {Id=Guid.NewGuid(),Username="owned-private",Email="arbitrum-private@example.invalid",IsActive=true,IsPublic=false};
            var detail=new AvatarDetail {Id=avatar.Id,Username=avatar.Username,Email=avatar.Email,XP=321,Karma=29,IsActive=true,IsPublic=false};
            detail.Inventory.Add(new InventoryItem {Id=Guid.NewGuid(),Name="owned arbitrum seed"});
            Assert.IsFalse((await provider.SaveAvatarAsync(avatar)).IsError);
            Assert.IsFalse((await provider.SaveAvatarDetailAsync(detail)).IsError);
            Assert.AreEqual(avatar.Email,(await provider.LoadAvatarAsync(avatar.Id)).Result.Email);
            Assert.AreEqual(321L,(await provider.LoadAvatarDetailAsync(avatar.Id)).Result.XP);
            var sdk=(Web3CoreOASIS)typeof(Web3CoreOASISBaseProvider).GetField("_web3CoreOASIS",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(provider)!;
            foreach(var kind in new[] {"avatar","detail"})
            {
                var records=await sdk.ReadCollectionAsync(kind);Assert.AreEqual(1,records.Count);
                var stored=Encoding.UTF8.GetString(records.Single().Info);
                Assert.IsFalse(stored.Contains(avatar.Email,StringComparison.Ordinal));
                Assert.IsFalse(stored.Contains(avatar.Username,StringComparison.Ordinal));
            }
            await VerifyV2Async(provider,avatar.Id);
        }
        finally {await provider.DeActivateProviderAsync();}
    }
}
