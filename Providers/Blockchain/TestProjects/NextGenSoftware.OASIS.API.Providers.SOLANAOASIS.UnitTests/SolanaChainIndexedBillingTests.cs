using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.Models;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Repositories;
using Solnet.Rpc;
using Solnet.Wallet;
using Solnet.Wallet.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.UnitTests;

[TestClass,DoNotParallelize]
public partial class SolanaChainIndexedBillingTests
{
    [DataTestMethod,DataRow("rows"),DataRow("empty"),DataRow("budget"),DataRow("begin"),DataRow("receipt"),DataRow("source-begin"),DataRow("source-receipt"),DataRow("http-rows"),DataRow("http-empty"),DataRow("http-missing"),DataRow("http-network"),DataRow("http-minimum-conflict"),DataRow("http-minimum-valid"),DataRow("http-minimum-empty"),DataRow("http-minimum-missing")]
    public async Task RealSqlAndCanonicalSolanaReadsRetainSeparatePreparedAttemptReceipts(string mode)
    {
        var empty=mode is "empty" or "http-empty" or "http-minimum-empty";
        var root=new DirectoryInfo(AppContext.BaseDirectory);while(root!=null&&!Directory.Exists(Path.Combine(root.FullName,"Scripts","TestHosts")))root=root.Parent;
        Assert.IsNotNull(root);var folder=Path.Combine(root.FullName,"TestResults","CosmicChainIngestion");Directory.CreateDirectory(folder);
        var database=Path.Combine(folder,"index.db");foreach(var path in new[]{database,database+"-wal",database+"-shm"})Assert.IsFalse(File.Exists(path),"Preserve unknown fixture.");
        var account=new Account();var rpc=DispatchProxy.Create<IRpcClient,SolanaChainIndexObservationTests.RangeRpc>();var proxy=(SolanaChainIndexObservationTests.RangeRpc)(object)rpc;
        proxy.Writer=account.PublicKey.Key;var key=RandomNumberGenerator.GetBytes(32);var actor=Guid.NewGuid();
        try{
            var repository=new SolanaRepository(account,rpc,()=> (byte[])key.Clone(),SolanaReadFixture.From(rpc));
            var id=Guid.NewGuid();
            string Memo(SolanaHolonDto dto)=>Encoders.Base58.EncodeData(Encoding.UTF8.GetBytes((string)typeof(SolanaRepository).GetMethod("EncodeEntity",BindingFlags.Instance|BindingFlags.NonPublic)!.MakeGenericMethod(typeof(SolanaHolonDto)).Invoke(repository,new object[]{dto})!));
            proxy.Memos["old"]=Memo(new SolanaHolonDto{Id=id,Version=1,VersionId=Guid.NewGuid(),Name="old",CreatedByAvatarId=actor});
            proxy.Memos["new"]=Memo(new SolanaHolonDto{Id=id,Version=2,VersionId=Guid.NewGuid(),Name="latest",CreatedByAvatarId=actor});
            var source=new CosmicChainQuerySource(ProviderType.SolanaOASIS,SolanaChainIndexObservationTests.RangeRpc.Genesis,ProviderType.SQLLiteDBOASIS,Guid.NewGuid(),2,
                SolanaRepository.ChainHolonCatalogueHash(proxy.Writer,SolanaChainIndexObservationTests.RangeRpc.Genesis),1);
            var requirement=new CosmicChainQueryReadRequirement(source,new(10,SolanaChainIndexObservationTests.RangeRpc.Hash(10)),0,TimeSpan.FromMinutes(1));
            using var index=new SqliteCosmicChainIndexStore(database,source,key,canonicalRowProjection:true);
            var published=await CosmicChainQueryIngestion.PublishAsync(repository,index,requirement,null);
            Assert.IsFalse(published.IsError,published.Exception?.ToString());
            var dna=new OASISDNA{OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS{HyperDriveMode=HyperDriveModes.V2}};
            var manager=new HolonManager(null,dna,new ProviderManager(null,dna));
            if(mode.StartsWith("http-",StringComparison.Ordinal)){
                await VerifyHttpAsync(manager,index,repository,source,requirement,actor,proxy,empty,mode);
                return;
            }
            var query=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(source.SourceProvider).Result).WithIndexedSource(requirement).Result
                .WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(actor).Result).Result;
            var created=CosmicLinq.CreateIndexed(manager,index,repository,query);Assert.IsFalse(created.IsError,created.Message);
            var selectedName=empty?"missing":"latest";
            var selected=created.Result.Where(row=>row.Name==selectedName).OrderBy(row=>row.Name).Take(1);
            var outbox=DispatchProxy.Create<IUsageOutboxStore,EvidenceProxy>();var evidence=(EvidenceProxy)(object)outbox;evidence.Mode=mode;
            var authority=DispatchProxy.Create<IUsageReservationAuthority,EvidenceProxy>();
            var handle=new UsageExecutionHandle(new UsageOutboxOperation{Id="wired-index-billing",UserId=actor.ToString("D"),ExecutionOwner="owner",Policy=new UsageEndpointPolicy{RequiresProviderMeasurement=true,ReservedUnits=100,EstimatedCostUsd=0.1m,MaximumProviderAttempts=100}},
                outbox,new Web4UsageLedgerOptions(),new SubscriptionAuthorizationResult(),authority);
            var tariffs=new CosmicIndexedReadTariffPolicy(source,mode=="budget"?4:100,CosmicIndexedReadTariffPolicy.Required.Select(operation=>new CosmicIndexedReadTariff(operation,0.001m,"wired-index-v1")));
            var prepared=new PreparedStorageExecution(handle,"wired-index",new Dictionary<ProviderType,string>(),Array.Empty<ProviderType>(),null,null,false,new[]{tariffs});
            proxy.Calls=0;
            var result=await prepared.RunAsync(()=>selected.ToCosmicIndexedPageAsync());
            if(mode is not ("rows" or "empty")){
                Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);
                Assert.AreEqual(mode=="budget"?"CQL_QUERY_LIMIT":"CQL_INDEXED_ATTEMPT_UNCERTAIN",result.ErrorCode,result.Message+result.Exception);
                if(mode is "begin" or "receipt" or "source-begin")Assert.AreEqual(0,proxy.Calls);
                if(mode=="source-receipt")Assert.AreEqual(1,proxy.Calls);
                if(mode=="budget"){Assert.AreEqual(3,proxy.Calls);Assert.AreEqual(4,evidence.Receipts.Count);}
                Assert.AreEqual(0,proxy.Writes);return;
            }
            Assert.IsFalse(result.IsError,result.Message+result.Exception);
            Assert.AreEqual(empty?0:1,result.Result.Rows.Count);
            Assert.AreEqual(empty?4:7,evidence.Receipts.Count);
            Assert.AreEqual(evidence.Begins.Count,evidence.Receipts.Count);
            Assert.AreEqual(1,evidence.Receipts.Count(row=>row.Provider==ProviderType.SQLLiteDBOASIS.ToString()));
            Assert.AreEqual(proxy.Calls,evidence.Receipts.Count(row=>row.Provider==ProviderType.SolanaOASIS.ToString()));
            foreach(var receipt in evidence.Receipts){Assert.IsTrue(receipt.Model.Contains(tariffs.BindingHash));Assert.AreEqual(1L,receipt.Units);}
            if(!empty){Assert.AreEqual("latest",result.Result.Rows.Single().Name);Assert.AreEqual("new",result.Result.Receipts.Single().CommitReference);Assert.IsFalse(HolonChangeTracking.HasChanges(result.Result.Rows.Single()));}
            Assert.AreEqual(0,proxy.Writes);
        }finally{CryptographicOperations.ZeroMemory(key);foreach(var path in new[]{database,database+"-wal",database+"-shm"})if(File.Exists(path))File.Delete(path);}
    }

    public class EvidenceProxy:DispatchProxy
    {
        public string Mode="";
        public readonly List<UsageProviderAttempt> Begins=new();
        public readonly List<UsageProviderReceipt> Receipts=new();
        protected override object? Invoke(MethodInfo? method,object?[]? args)
        {
            if(method!.Name==nameof(IUsageOutboxStore.BeginProviderAttemptAsync)){var attempt=(UsageProviderAttempt)args![2]!;Begins.Add(attempt);if(Mode=="begin"||Mode=="source-begin"&&attempt.Provider==ProviderType.SolanaOASIS.ToString())throw new IOException("begin outcome unknown");return Task.CompletedTask;}
            if(method.Name==nameof(IUsageOutboxStore.AppendReceiptAsync)){var receipt=(UsageProviderReceipt)args![2]!;receipt.Validate();if(Mode=="receipt"||Mode=="source-receipt"&&receipt.Provider==ProviderType.SolanaOASIS.ToString())throw new IOException("acknowledgement outcome unknown");Assert.IsTrue(Begins.Any(begin=>begin.ClientRequestId==receipt.ClientRequestId));Receipts.Add(receipt);return Task.CompletedTask;}
            throw new InvalidOperationException("Unexpected ledger/authority operation: "+method.Name);
        }
    }
}
