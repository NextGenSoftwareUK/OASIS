using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Providers.TestSupport;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass,DoNotParallelize]
public class CoreStorageConformanceTests
{
    [DataTestMethod] [DataRow(false)] [DataRow(true)]
    public async Task CommonInterfaceRunsAgainstTheExistingModelBackend(bool sync)
    {
        var backend=new InMemoryBackend();var provider=new TestProvider(backend);
        var report=await CoreStorageConformance.RunAsync(provider,"KeyValueStorageProviderBase","controlled",sync,_=>Task.CompletedTask);
        Assert.IsTrue(report.Succeeded,report.CurrentCase+": "+report.FailureType);
        Assert.AreEqual("",report.FailureContract);
        Assert.AreEqual(CoreStorageConformance.ChecksPerMode,report.Passed.Count);
        Assert.IsTrue(report.FixtureCleanupComplete);
        Assert.AreEqual(0,backend.Items.Count,"Healthy suite must remove its owned records.");
    }

#if OASIS_NATIVE_STORAGE_CONFORMANCE
    [DataTestMethod] [DataRow(false)] [DataRow(true)]
    public async Task SameInterfaceSuiteRunsAgainstNativeLocalFile(bool sync)
    {
        var root=new DirectoryInfo(AppContext.BaseDirectory);
        while(root!=null&&!Directory.Exists(Path.Combine(root.FullName,"Scripts","TestHosts")))root=root.Parent;
        Assert.IsNotNull(root);
        var work=Path.Combine(root.FullName,"TestResults","ProviderConformanceCurrent","LocalFileOASIS",sync?"sync":"async");
        var reportPath=Path.Combine(work,"report.json");var data=Path.Combine(work,"data");
        Assert.IsFalse(File.Exists(reportPath)||File.Exists(reportPath+".next"),"Review previous conformance evidence before rerun.");
        Directory.CreateDirectory(work);Directory.CreateDirectory(data);
        Assert.IsFalse(Directory.EnumerateFiles(data,"*",SearchOption.AllDirectories).Any(),"Never adopt existing native test records.");
        using var lease=new FileStream(Path.Combine(work,"environment.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        var provider=new NextGenSoftware.OASIS.API.Providers.LocalFileOASIS.LocalFileOASIS(storageDirectory:data);
        async Task Checkpoint(CoreStorageConformance.Report report)
        {
            var next=reportPath+".next";
            await using(var file=new FileStream(next,FileMode.CreateNew,FileAccess.Write,FileShare.None))
            {await JsonSerializer.SerializeAsync(file,report);file.Flush(true);}
            File.Move(next,reportPath,overwrite:true);
        }
        var result=await CoreStorageConformance.RunAsync(provider,"LocalFileOASIS","native",sync,Checkpoint);
        Assert.IsTrue(result.Succeeded,result.CurrentCase+": "+result.FailureType);
        Assert.IsFalse(Directory.EnumerateFiles(data,"*",SearchOption.AllDirectories).Any(),"Successful suite leaves no owned payload files.");
    }

#endif

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task SoftDeleteContractUsesDefaultApiBeforeExactFixtureCleanup(bool sync)
    {
        var backend=new InMemoryBackend();var provider=new LifecycleModel(backend,""){IsVersionControlEnabled=true};
        var report=await CoreStorageConformance.RunAsync(provider,"soft-model","controlled",sync,_=>Task.CompletedTask,
            async(id,synchronous)=>{
                var historical=await provider.LoadHolonAsync(id,loadChildren:false,version:1);
                Assert.IsFalse(historical.IsError);Assert.IsNotNull(historical.Result);Assert.IsFalse(historical.Result.IsDeleted);
                return await provider.HardCleanup(id,synchronous);
            },expectedDefaultDeletion:"soft");
        Assert.IsTrue(report.Succeeded,report.CurrentCase+": "+report.FailureType);
        Assert.AreEqual(26,report.Passed.Count);Assert.AreEqual(5,report.DeletionKinds.Count);
        Assert.IsTrue(report.DeletionKinds.Values.All(kind=>kind=="soft"));Assert.AreEqual(5,report.HardCleanedFixtures.Count);
        Assert.IsTrue(report.FixtureCleanupComplete);Assert.AreEqual(0,backend.Items.Count);
    }

    [DataTestMethod]
    [DataRow("no-ack",false)][DataRow("no-ack",true)]
    [DataRow("no-tombstone",false)][DataRow("no-tombstone",true)]
    [DataRow("no-cleanup",false)][DataRow("no-cleanup",true)]
    [DataRow("ineffective-cleanup",false)][DataRow("ineffective-cleanup",true)]
    [DataRow("stale-collection",false)][DataRow("stale-collection",true)]
    [DataRow("transport-absence",false)][DataRow("transport-absence",true)]
    [DataRow("unexpected-hard",false)][DataRow("unexpected-hard",true)]
    public async Task InvalidDeleteOrCleanupCannotEarnAConformancePass(string fault,bool sync)
    {
        var backend=new InMemoryBackend();var provider=new LifecycleModel(backend,fault);
        Func<Guid,bool,Task<NextGenSoftware.OASIS.Common.OASISResult<bool>>> cleanup=fault=="no-cleanup"?null:
            (id,synchronous)=>fault=="ineffective-cleanup"?Task.FromResult(new NextGenSoftware.OASIS.Common.OASISResult<bool>(true){IsDeleted=true}):provider.HardCleanup(id,synchronous);
        var report=await CoreStorageConformance.RunAsync(provider,"fault-model","controlled",sync,_=>Task.CompletedTask,cleanup,expectedDefaultDeletion:"soft");
        Assert.IsFalse(report.Succeeded);Assert.IsFalse(report.FixtureCleanupComplete);Assert.AreEqual(18,report.Passed.Count);
        Assert.AreEqual(0,report.HardCleanedFixtures.Count);Assert.AreEqual("System.InvalidOperationException",report.FailureType);
        Assert.IsFalse(string.IsNullOrWhiteSpace(report.FailureContract));
    }

    [DataTestMethod,DataRow(false),DataRow(true)]
    public async Task ForeignProviderExceptionTextCannotBecomeAContractDiagnostic(bool sync)
    {
        var report=await CoreStorageConformance.RunAsync(new ForeignFailureModel(),"foreign-failure","controlled",sync,_=>Task.CompletedTask);
        Assert.IsFalse(report.Succeeded);Assert.AreEqual("activate",report.CurrentCase);
        Assert.AreEqual("System.InvalidOperationException",report.FailureType);Assert.AreEqual("",report.FailureContract);
        Assert.IsFalse(JsonSerializer.Serialize(report).Contains("synthetic-private-value",StringComparison.Ordinal));
    }

    private sealed class ForeignFailureModel : KeyValueStorageProviderBase
    {
        public ForeignFailureModel():base(new InMemoryBackend()){}
        private static Exception Failure()
        {
            var error=new InvalidOperationException("synthetic-private-value");
            error.Data["FailureContract"]="synthetic-private-value";return error;
        }
        public override NextGenSoftware.OASIS.Common.OASISResult<bool> ActivateProvider()=>throw Failure();
        public override Task<NextGenSoftware.OASIS.Common.OASISResult<bool>> ActivateProviderAsync()=>Task.FromException<NextGenSoftware.OASIS.Common.OASISResult<bool>>(Failure());
    }

    private sealed class LifecycleModel : KeyValueStorageProviderBase
    {
        private readonly string _fault;
        private Guid _lastCleaned;
        public LifecycleModel(InMemoryBackend backend,string fault):base(backend,"lifecycle-conformance/"){_fault=fault;ProviderName="LifecycleModel";ProviderType=new NextGenSoftware.Utilities.EnumValue<NextGenSoftware.OASIS.API.Core.Enums.ProviderType>(NextGenSoftware.OASIS.API.Core.Enums.ProviderType.VercelKVOASIS);}
        public override async Task<NextGenSoftware.OASIS.Common.OASISResult<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon>> DeleteHolonAsync(Guid id)
        {
            if(_fault is "no-ack" or "no-tombstone")return new(){IsDeleted=true,DeletedCount=_fault=="no-ack"?0:1};
            if(_fault=="unexpected-hard")return await DeleteHolonRecordAsync(id,false);
            return await DeleteHolonRecordAsync(id,true);
        }
        public async Task<NextGenSoftware.OASIS.Common.OASISResult<bool>> HardCleanup(Guid id,bool sync)
        {
            var deleted=await DeleteHolonRecordAsync(id,false);_lastCleaned=id;
            return new(!deleted.IsError&&deleted.IsDeleted){IsError=deleted.IsError,IsDeleted=deleted.IsDeleted,DeletedCount=deleted.DeletedCount};
        }
        public override Task<NextGenSoftware.OASIS.Common.OASISResult<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon>> LoadHolonAsync(Guid id,bool loadChildren=true,bool recursive=true,int maxChildDepth=0,bool continueOnError=true,bool loadChildrenFromProvider=false,int version=0)
        {
            if(_fault=="transport-absence"&&id==_lastCleaned)return Task.FromResult(new NextGenSoftware.OASIS.Common.OASISResult<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon>{IsError=true,ErrorCode="TRANSPORT_TIMEOUT",Exception=new TimeoutException()});
            return base.LoadHolonAsync(id,loadChildren,recursive,maxChildDepth,continueOnError,loadChildrenFromProvider,version);
        }
        public override async Task<NextGenSoftware.OASIS.Common.OASISResult<System.Collections.Generic.IEnumerable<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon>>> LoadAllHolonsAsync(
            NextGenSoftware.OASIS.API.Core.Enums.HolonType type=NextGenSoftware.OASIS.API.Core.Enums.HolonType.All,bool loadChildren=true,bool recursive=true,int maxChildDepth=0,int curentChildDepth=0,bool continueOnError=true,bool loadChildrenFromProvider=false,int version=0)
        {
            var result=await base.LoadAllHolonsAsync(type,loadChildren,recursive,maxChildDepth,curentChildDepth,continueOnError,loadChildrenFromProvider,version);
            if(_fault=="stale-collection"&&result.Result!=null){
                var rows=result.Result.Select(row=>{if(row.IsDeleted){row.DeletedDate=DateTime.MinValue;row.IsActive=true;}return row;}).ToArray();
                result.Result=rows;result.LoadedCount=rows.Length;
            }
            return result;
        }
    }

}
