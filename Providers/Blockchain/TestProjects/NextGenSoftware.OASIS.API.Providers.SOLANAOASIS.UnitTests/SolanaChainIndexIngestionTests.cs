using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.Models;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Repositories;
using Solnet.Rpc;
using Solnet.Wallet;
using Solnet.Wallet.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.UnitTests;

[TestClass,DoNotParallelize]
public class SolanaChainIndexIngestionTests
{
    [DataTestMethod,DataRow("valid"),DataRow("pruned"),DataRow("missing")]
    public async Task ActualCanonicalObserverAndSharedCoordinatorPublishOnlyCompleteIndependentEvidence(string mode)
    {
        var root=new DirectoryInfo(AppContext.BaseDirectory);while(root!=null&&!Directory.Exists(Path.Combine(root.FullName,"Scripts","TestHosts")))root=root.Parent;
        Assert.IsNotNull(root);var folder=Path.Combine(root.FullName,"TestResults","CosmicChainIngestion");Directory.CreateDirectory(folder);
        var database=Path.Combine(folder,"index.db");foreach(var path in new[]{database,database+"-wal",database+"-shm"})Assert.IsFalse(File.Exists(path),"Preserve unknown fixture.");
        var account=new Account();var rpc=DispatchProxy.Create<IRpcClient,SolanaChainIndexObservationTests.RangeRpc>();var proxy=(SolanaChainIndexObservationTests.RangeRpc)(object)rpc;
        proxy.Mode=mode;proxy.Writer=account.PublicKey.Key;var key=RandomNumberGenerator.GetBytes(32);
        try{
            var repository=new SolanaRepository(account,rpc,()=> (byte[])key.Clone(),SolanaReadFixture.From(rpc));
            var id=Guid.NewGuid();
            string Memo(SolanaHolonDto dto)=>Encoders.Base58.EncodeData(Encoding.UTF8.GetBytes((string)typeof(SolanaRepository).GetMethod("EncodeEntity",BindingFlags.Instance|BindingFlags.NonPublic)!.MakeGenericMethod(typeof(SolanaHolonDto)).Invoke(repository,new object[]{dto})!));
            proxy.Memos["old"]=Memo(new SolanaHolonDto{Id=id,Version=1,VersionId=Guid.NewGuid()});
            proxy.Memos["new"]=Memo(new SolanaHolonDto{Id=id,Version=2,VersionId=Guid.NewGuid(),IsDeleted=true,DeletedDate=DateTime.UtcNow});
            var source=new CosmicChainQuerySource(ProviderType.SolanaOASIS,SolanaChainIndexObservationTests.RangeRpc.Genesis,ProviderType.SQLLiteDBOASIS,Guid.NewGuid(),1,
                SolanaRepository.ChainHolonCatalogueHash(proxy.Writer,SolanaChainIndexObservationTests.RangeRpc.Genesis),1);
            var requirement=new CosmicChainQueryReadRequirement(source,new(10,SolanaChainIndexObservationTests.RangeRpc.Hash(10)),0,TimeSpan.FromMinutes(1));
            using(var store=new SqliteCosmicChainIndexStore(database,source,key)){
                var result=await CosmicChainQueryIngestion.PublishAsync(repository,store,requirement,null);
                if(mode=="valid"){
                    Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.IsTrue(result.IsSaved);Assert.AreEqual(2,result.SavedCount);
                }else{Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.IsFalse(File.Exists(database));}
            }
            if(mode=="valid"){
                using var restarted=new SqliteCosmicChainIndexStore(database,source,key);var snapshot=restarted.ReadSnapshot();Assert.IsFalse(snapshot.IsError,snapshot.Exception?.ToString());
                Assert.AreEqual(10L,snapshot.Result.Checkpoint.Position.Slot);Assert.AreEqual(2,snapshot.Result.History.Count);Assert.IsTrue(snapshot.Result.History[1].Receipt.Record.IsDeleted);
                Assert.AreEqual("old",snapshot.Result.History[0].Receipt.CommitReference);Assert.AreEqual("new",snapshot.Result.History[1].Receipt.CommitReference);
            }
            Assert.AreEqual(0,proxy.Writes);
        }finally{
            CryptographicOperations.ZeroMemory(key);
            foreach(var path in new[]{database,database+"-wal",database+"-shm"})if(File.Exists(path))File.Delete(path);
        }
    }
}
