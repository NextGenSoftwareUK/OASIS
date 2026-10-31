using System.Numerics;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.API.Providers.EthereumOASIS.ContractDefinition;
using Avatar=NextGenSoftware.OASIS.API.Core.Holons.Avatar;
using AvatarDetail=NextGenSoftware.OASIS.API.Core.Holons.AvatarDetail;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.IntegrationTests;

public partial class EthereumOASISIntegrationTests
{
    private sealed class MutateHistoryCallerProvider(string address,Action mutate):EthereumOASIS(Host,PrivateKey,EthereumOASISIntegrationTests.ChainId,address)
    {
        public string? Acknowledged;
        protected override async Task<TransactionReceipt> PollStorageReceiptAsync(string transactionId)
        {
            var receipt=await base.PollStorageReceiptAsync(transactionId);
            Assert.AreEqual(BigInteger.One,receipt.Status.Value);
            Acknowledged=transactionId;mutate();return receipt;
        }
    }

    [DataTestMethod]
    [DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task ProfileHistoryRefusesPublicationAfterAcknowledgedCallerIdentityChange(bool detail,bool synchronous)
    {
        var (reader,address)=await CreateActivatedProviderAsync();
        var original=Guid.NewGuid();
        IHolonBase source=detail?new AvatarDetail{Id=original}:new Avatar{Id=original};
        var provider=new MutateHistoryCallerProvider(address,()=>source.Id=Guid.NewGuid()){IsVersionControlEnabled=true};
        Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        try {
            Exception cause;
            if(detail){
                var result=synchronous?provider.SaveAvatarDetail((IAvatarDetail)source):await provider.SaveAvatarDetailAsync((IAvatarDetail)source);
                Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.IsFalse(result.IsSaved);cause=result.Exception;
            }else{
                var result=synchronous?provider.SaveAvatar((IAvatar)source):await provider.SaveAvatarAsync((IAvatar)source);
                Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.IsFalse(result.IsSaved);cause=result.Exception;
            }
            Assert.IsInstanceOfType(cause,typeof(EthereumPartialStorageMutationException));
            Assert.AreEqual(provider.Acknowledged,((EthereumPartialStorageMutationException)cause).OriginalTransactionId);
            Assert.AreNotEqual(original,source.Id);Assert.AreEqual(0,source.Version);Assert.AreEqual(Guid.Empty,source.VersionId);
            Assert.IsFalse(source.ProviderUniqueStorageKey.ContainsKey(Core.Enums.ProviderType.EthereumOASIS));
            var service=new NextGenSoftwareOASISService(new Web3(new Account(PrivateKey,ChainId),Host),address);
            var key=new BigInteger(HashUtility.GetNumericHash(original.ToString()));
            var loaded=detail? (IHolonBase)(await reader.LoadAvatarDetailAsync(original)).Result:(await reader.LoadAvatarAsync(original)).Result;
            Assert.IsNotNull(loaded);Assert.AreEqual(original,loaded.Id);Assert.AreEqual(1,loaded.Version);
            var changed=new BigInteger(HashUtility.GetNumericHash(source.Id.ToString()));
            var missing=detail?(await service.GetAvatarDetailByIdQueryAsync(changed)).ReturnValue1.EntityId:(await service.GetAvatarByIdQueryAsync(changed)).ReturnValue1.EntityId;
            Assert.AreEqual(BigInteger.Zero,missing);
            TransactionReceipt deleted=detail?
                await service.ContractHandler.SendRequestAndWaitForReceiptAsync(new DeleteAvatarDetailByIdentityFunction{EntityId=key,Identity=original.ToString()}):
                await service.ContractHandler.SendRequestAndWaitForReceiptAsync(new DeleteAvatarByIdentityFunction{EntityId=key,Identity=original.ToString()});
            Assert.AreEqual(BigInteger.One,deleted.Status.Value);
            var graphId=Guid.NewGuid();source=new Core.Holons.Holon{Id=graphId};
            var graph=synchronous?provider.SaveHolon((IHolon)source):await provider.SaveHolonAsync((IHolon)source);
            Assert.IsTrue(graph.IsError);Assert.IsNull(graph.Result);Assert.IsFalse(graph.IsSaved);
            Assert.IsInstanceOfType(graph.Exception,typeof(EthereumPartialStorageMutationException));
            Assert.AreEqual(provider.Acknowledged,((EthereumPartialStorageMutationException)graph.Exception).OriginalTransactionId);
            Assert.AreEqual(0,source.Version);Assert.AreEqual(Guid.Empty,source.VersionId);
            Assert.IsFalse(source.ProviderUniqueStorageKey.ContainsKey(Core.Enums.ProviderType.EthereumOASIS));
            var nativeGraph=await reader.LoadHolonAsync(graphId,loadChildren:false);Assert.IsFalse(nativeGraph.IsError,nativeGraph.Message);Assert.AreEqual(1,nativeGraph.Result.Version);
            Assert.AreEqual(BigInteger.Zero,(await service.GetHolonByIdQueryAsync(HashUtility.GetNumericHash(source.Id.ToString()))).ReturnValue1.EntityId);
            var graphCleanup=await reader.DeleteHolonAsync(graphId);Assert.IsFalse(graphCleanup.IsError,graphCleanup.Message);Assert.IsTrue(graphCleanup.IsDeleted);
            Assert.AreEqual(BigInteger.Zero,(await service.GetHolonByIdQueryAsync(HashUtility.GetNumericHash(graphId.ToString()))).ReturnValue1.EntityId);
        }finally{await provider.DeActivateProviderAsync();await reader.DeActivateProviderAsync();}
    }

    [DataTestMethod]
    [DataRow(false)][DataRow(true)]
    public async Task ProfileHistoryRejectsOldContractCapabilityWithoutBroadcast(bool detail)
    {
        var path=Path.Combine(Path.GetDirectoryName(Environment.GetEnvironmentVariable("ETHEREUMOASIS_TEST_FIXTURE_JSON"))!,"Protocol3WithoutCheckedStorageFixture.json");
        using var fixture=JsonDocument.Parse(await File.ReadAllTextAsync(path));
        var account=new Account(PrivateKey,ChainId);var web3=new Web3(account,Host);
        var deployed=await web3.Eth.DeployContract.SendRequestAndWaitForReceiptAsync(fixture.RootElement.GetProperty("abi").GetRawText(),fixture.RootElement.GetProperty("bytecode").GetString(),account.Address,new Nethereum.Hex.HexTypes.HexBigInteger(4000000));
        var provider=new EthereumOASIS(Host,PrivateKey,ChainId,deployed.ContractAddress){IsVersionControlEnabled=true};
        Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        try {
            var before=await web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(account.Address);
            if(detail){var source=new AvatarDetail{Id=Guid.NewGuid()};var result=await provider.SaveAvatarDetailAsync(source);Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.AreEqual(0,source.Version);}
            else {var source=new Avatar{Id=Guid.NewGuid()};var result=await provider.SaveAvatarAsync(source);Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.AreEqual(0,source.Version);}
            var root=new Core.Holons.Holon{Id=Guid.Empty};var child=new Core.Holons.Holon{Id=Guid.Empty};root.Children.Add(child);
            var graph=detail?provider.SaveHolons(new[]{root}):await provider.SaveHolonsAsync(new[]{root});
            Assert.IsTrue(graph.IsError);Assert.IsNull(graph.Result);Assert.IsFalse(graph.IsSaved);
            Assert.AreEqual(Guid.Empty,root.Id);Assert.AreEqual(Guid.Empty,child.Id);Assert.AreEqual(Guid.Empty,child.ParentHolonId);
            var after=await web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(account.Address);
            Assert.AreEqual(before.Value,after.Value);
        }finally{await provider.DeActivateProviderAsync();}
    }
}
