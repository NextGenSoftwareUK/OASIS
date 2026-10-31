using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.OASIS.Providers.TestSupport;
using SharedRedis=NextGenSoftware.OASIS.API.Providers.RedisOASIS.RedisOASIS;
using KeyDb=NextGenSoftware.OASIS.API.Providers.KeyDBOASIS.KeyDBOASIS;
using Valkey=NextGenSoftware.OASIS.API.Providers.ValKeyOASIS.ValKeyOASIS;
using Dragonfly=NextGenSoftware.OASIS.API.Providers.DragonflyOASIS.DragonflyOASIS;
using Garnet=NextGenSoftware.OASIS.API.Providers.GarnetOASIS.GarnetOASIS;
namespace NextGenSoftware.OASIS.API.Providers.RedisOASIS.UnitTests;

// Product identities exercise the ONE canonical suite over a controlled encrypted backend.
// No Redis server may stand in for native acceptance of KeyDB/Valkey/Dragonfly/Garnet.
[TestClass,DoNotParallelize]
public class RedisFamilyContractTests
{
    private static SharedRedis Controlled(string name,IKeyValueBackend backend,Func<byte[]> key)=>name switch{
        "KeyDBOASIS"=>new KeyDb(backend,key),"ValKeyOASIS"=>new Valkey(backend,key),
        "DragonflyOASIS"=>new Dragonfly(backend,key),"GarnetOASIS"=>new Garnet(backend,key),_=>throw new ArgumentException(nameof(name))};
    private static SharedRedis Native(string name,Func<byte[]> key)=>name switch{
        "KeyDBOASIS"=>new KeyDb("127.0.0.1:1",key),"ValKeyOASIS"=>new Valkey("127.0.0.1:1",key),
        "DragonflyOASIS"=>new Dragonfly("127.0.0.1:1",key),"GarnetOASIS"=>new Garnet("127.0.0.1:1",key),_=>throw new ArgumentException(nameof(name))};

    [DataTestMethod]
    [DataRow("KeyDBOASIS","configuration","localhost:6379")][DataRow("ValKeyOASIS","connectionString","localhost:6379")]
    [DataRow("DragonflyOASIS","connectionString","localhost:6379")][DataRow("GarnetOASIS","connectionString","localhost:3278")]
    public void IdentityAndConstructorContractRemainProductSpecific(string name,string parameter,string endpoint)
    {
        using var provider=Native(name,()=>throw new InvalidOperationException("Construction must not resolve key/connect."));
        Assert.AreEqual(name,provider.ProviderName);Assert.AreEqual(Enum.Parse<ProviderType>(name),provider.ProviderType.Value);
        Assert.IsFalse(provider.IsProviderActivated);Assert.IsInstanceOfType<IOASISStorageProvider>(provider);Assert.IsInstanceOfType<IOASISDBStorageProvider>(provider);
        var argument=provider.GetType().GetConstructor(new[]{typeof(string)})!.GetParameters().Single();
        Assert.AreEqual(parameter,argument.Name);Assert.AreEqual(endpoint,argument.DefaultValue);
        Assert.AreEqual(typeof(KeyValueStorageProviderBase),provider.GetType().GetMethod("SaveAvatarAsync",new[]{typeof(IAvatar)})!.DeclaringType);
        Assert.AreEqual(typeof(SharedRedis),provider.GetType().GetMethod("DeleteHolonAsync",new[]{typeof(Guid)})!.DeclaringType);
    }

    [DataTestMethod][DataRow("KeyDBOASIS")][DataRow("ValKeyOASIS")][DataRow("DragonflyOASIS")][DataRow("GarnetOASIS")]
    public async Task MissingPersistedKeyFailsBeforeNativeTransport(string name)
    {
        var missing=new InvalidOperationException("Controlled missing persisted key");var calls=0;
        using var provider=Native(name,()=>{calls++;throw missing;});
        var result=await provider.ActivateProviderAsync();Assert.IsTrue(result.IsError);Assert.IsFalse(provider.IsProviderActivated);
        Assert.AreSame(missing,result.Exception);Assert.AreEqual(1,calls);
    }

    [DataTestMethod]
    [DataRow("KeyDBOASIS",false)][DataRow("KeyDBOASIS",true)][DataRow("ValKeyOASIS",false)][DataRow("ValKeyOASIS",true)]
    [DataRow("DragonflyOASIS",false)][DataRow("DragonflyOASIS",true)][DataRow("GarnetOASIS",false)][DataRow("GarnetOASIS",true)]
    public async Task InvalidAliasesDoNotResolveKeysOrConnect(string name,bool sync)
    {
        using var provider=Native(name,()=>throw new InvalidOperationException("Invalid GUID must not request key/connect."));
        var result=sync?provider.DeleteHolonSoft("not-a-guid",false):await provider.DeleteHolonSoftAsync("not-a-guid",false);
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.Result);Assert.IsFalse(result.IsDeleted);Assert.AreEqual(0,result.DeletedCount);
        Assert.IsInstanceOfType<ArgumentException>(result.Exception);
    }

    [TestMethod]
    public async Task KeyDbNamedProviderKeyAliasesRemainCallable()
    {
        using var provider=new KeyDb("127.0.0.1:1",()=>throw new InvalidOperationException("Invalid alias must not connect."));
        Assert.IsTrue((await provider.LoadHolonByProviderKeyAsync(pk:"not-a-guid")).IsError);
        Assert.IsTrue(provider.LoadHolonByProviderKey(pk:"not-a-guid").IsError);
        Assert.IsTrue((await provider.DeleteHolonSoftAsync(pk:"not-a-guid",softDelete:false)).IsError);
        Assert.IsTrue(provider.DeleteHolonSoft(pk:"not-a-guid",softDelete:false).IsError);
    }

    [DataTestMethod]
    [DataRow("KeyDBOASIS",false)][DataRow("KeyDBOASIS",true)][DataRow("ValKeyOASIS",false)][DataRow("ValKeyOASIS",true)]
    [DataRow("DragonflyOASIS",false)][DataRow("DragonflyOASIS",true)][DataRow("GarnetOASIS",false)][DataRow("GarnetOASIS",true)]
    public async Task ActualFacadeRunsCanonicalStorageSuiteOverControlledBackend(string name,bool sync)
    {
        var root=new DirectoryInfo(AppContext.BaseDirectory);while(root!=null&&!Directory.Exists(Path.Combine(root.FullName,"Scripts","TestHosts")))root=root.Parent;Assert.IsNotNull(root);
        var work=Path.Combine(root.FullName,"TestResults","ProviderConformanceCurrent",name,sync?"sync":"async");Directory.CreateDirectory(work);
        var reportPath=Path.Combine(work,"report.json");Assert.IsFalse(File.Exists(reportPath)||File.Exists(reportPath+".next"),"Preserve prior reports before another run.");
        using var lease=new FileStream(Path.Combine(work,"environment.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        async Task Checkpoint(CoreStorageConformance.Report report){await using(var file=new FileStream(reportPath+".next",FileMode.CreateNew,FileAccess.Write,FileShare.None)){await JsonSerializer.SerializeAsync(file,report);file.Flush(true);}File.Move(reportPath+".next",reportPath,true);}
        var key=RandomNumberGenerator.GetBytes(32);
        try{
            using var provider=Controlled(name,new RedisControlledStore(),()=>(byte[])key.Clone());provider.IsVersionControlEnabled=true;
            var report=await CoreStorageConformance.RunAsync(provider,name,"controlled",sync,Checkpoint,
                (id,synchronous)=>synchronous?Task.FromResult(provider.DeleteHolonSoft(id,false)):provider.DeleteHolonSoftAsync(id,false),expectedDefaultDeletion:"soft");
            Assert.IsTrue(report.Succeeded,report.CurrentCase+": "+report.FailureType);Assert.IsTrue(report.FixtureCleanupComplete);
            Assert.AreEqual(26,report.Passed.Count);Assert.AreEqual(5,report.HardCleanedFixtures.Count);
        }finally{CryptographicOperations.ZeroMemory(key);}
    }
}
