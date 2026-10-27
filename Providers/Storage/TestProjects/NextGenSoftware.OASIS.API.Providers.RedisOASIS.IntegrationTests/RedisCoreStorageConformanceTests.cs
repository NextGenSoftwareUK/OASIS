using System.Text.Json;
using StackExchange.Redis;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Providers.TestSupport;
using Provider=NextGenSoftware.OASIS.API.Providers.RedisOASIS.RedisOASIS;

namespace NextGenSoftware.OASIS.API.Providers.RedisOASIS.IntegrationTests;

[TestClass,DoNotParallelize]
public class RedisCoreStorageConformanceTests
{
    [DataTestMethod] [DataRow(false)] [DataRow(true)]
    public async Task CoreStorageSuiteAgainstRetainedRedis(bool sync)
    {
        var endpoint=Environment.GetEnvironmentVariable("OASIS_REDIS_NATIVE_ENDPOINT");
        Assert.AreEqual("127.0.0.1:18679",endpoint);
        var options=ConfigurationOptions.Parse(endpoint);
        options.AbortOnConnectFail=true;options.ConnectRetry=0;options.BacklogPolicy=BacklogPolicy.FailFast;
        options.Password=Environment.GetEnvironmentVariable("OASIS_REDIS_NATIVE_PASSWORD");
        Assert.IsFalse(string.IsNullOrWhiteSpace(options.Password));
        Assert.IsFalse(string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(Provider.StorageKeyEnvironmentVariable)));
        var root=new DirectoryInfo(AppContext.BaseDirectory);
        while(root!=null&&!Directory.Exists(Path.Combine(root.FullName,"Scripts","TestHosts")))root=root.Parent;
        Assert.IsNotNull(root);
        var work=Path.Combine(root.FullName,"TestResults","ProviderConformanceCurrent","RedisOASIS",sync?"sync":"async");
        Directory.CreateDirectory(work);var reportPath=Path.Combine(work,"report.json");
        Assert.IsFalse(File.Exists(reportPath)||File.Exists(reportPath+".next"),"Preserve original Redis conformance fixture before rerun.");
        using var lease=new FileStream(Path.Combine(work,"environment.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        async Task Checkpoint(CoreStorageConformance.Report report)
        {
            await using(var file=new FileStream(reportPath+".next",FileMode.CreateNew,FileAccess.Write,FileShare.None))
            {await JsonSerializer.SerializeAsync(file,report);file.Flush(true);}
            File.Move(reportPath+".next",reportPath,overwrite:true);
        }
        using var provider=new Provider(options.ToString(includePassword:true),
            ()=>PersistedPrivateKeyValueBackend.ResolveEnvironmentKey(Provider.StorageKeyEnvironmentVariable),"oasis/conformance/redis/");
        var result=await CoreStorageConformance.RunAsync(provider,"RedisOASIS","native",sync,Checkpoint);
        Assert.IsTrue(result.Succeeded,result.CurrentCase+": "+result.FailureType);
    }
    [DataTestMethod] [DataRow(false)] [DataRow(true)]
    public async Task ReconcileKnownCoreSoftDeleteFixtures(bool sync)
    {
        var endpoint=Environment.GetEnvironmentVariable("OASIS_REDIS_NATIVE_ENDPOINT");Assert.AreEqual("127.0.0.1:18679",endpoint);
        var options=ConfigurationOptions.Parse(endpoint);options.Password=Environment.GetEnvironmentVariable("OASIS_REDIS_NATIVE_PASSWORD");
        Assert.IsFalse(string.IsNullOrWhiteSpace(options.Password));
        using var provider=new Provider(options.ToString(includePassword:true),()=>PersistedPrivateKeyValueBackend.ResolveEnvironmentKey(Provider.StorageKeyEnvironmentVariable),"oasis/conformance/redis/");
        Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        var root=new DirectoryInfo(AppContext.BaseDirectory);
        while(root!=null&&!Directory.Exists(Path.Combine(root.FullName,"Scripts","TestHosts")))root=root.Parent;
        Assert.IsNotNull(root);
        var reportPath=Path.Combine(root.FullName,"TestResults","ProviderConformanceCurrent","RedisOASIS",sync?"sync":"async","report.json");
        var report=JsonSerializer.Deserialize<CoreStorageConformance.Report>(await File.ReadAllTextAsync(reportPath));Assert.IsNotNull(report);
        Assert.AreEqual("RedisOASIS",report.Provider);Assert.AreEqual("native",report.Mode);Assert.AreEqual(sync,report.Synchronous);
        Assert.AreEqual("holon-delete-"+report.ChildId.ToString("N"),report.CurrentCase);Assert.AreEqual(18,report.Passed.Count);Assert.AreEqual("owner-export",report.Passed.Last());
        var marker="conformance-"+report.OwnerId.ToString("N");
        var avatar=await provider.LoadAvatarAsync(report.OwnerId);Assert.IsNotNull(avatar.Result);Assert.AreEqual(marker,avatar.Result.Username);
        var detail=await provider.LoadAvatarDetailAsync(report.OwnerId);Assert.IsNotNull(detail.Result);Assert.AreEqual(marker,detail.Result.Username);Assert.AreEqual(17,detail.Result.Karma);Assert.AreEqual(31L,Convert.ToInt64(detail.Result.XP));
        var names=new Dictionary<Guid,string>{{report.ChildId,marker+"-child"},{report.RootId,marker+"-updated"},{report.ImportId,marker+"-import"}};
        foreach(var id in report.BatchIds)names.Add(id,marker+"-batch");Assert.AreEqual(5,names.Count);
        // Verify EVERY exact synthetic record before any cleanup, never a prefix/global sweep.
        foreach(var pair in names){var row=await provider.LoadHolonAsync(pair.Key,loadChildren:false);Assert.IsNotNull(row.Result);Assert.AreEqual(pair.Key,row.Result.Id);Assert.AreEqual(report.OwnerId,row.Result.CreatedByAvatarId);Assert.AreEqual(pair.Value,row.Result.Name);
            Assert.AreEqual(pair.Key==report.ChildId,row.Result.IsDeleted);}
        var work=Environment.GetEnvironmentVariable("OASIS_REDIS_NATIVE_WORK")!;
        var journal=Path.Combine(work,"conformance-reconcile-"+(sync?"sync":"async")+".pending.json");
        var resolved=Path.ChangeExtension(journal,"resolved.json");Assert.IsFalse(File.Exists(journal)||File.Exists(resolved));
        await using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None)){await JsonSerializer.SerializeAsync(file,new{report.OwnerId,ids=names.Keys.ToArray(),state="verified-original-soft-delete-fixtures-before-cleanup"});file.Flush(true);}
        foreach(var id in names.Keys){var deleted=sync?provider.DeleteHolonSoft(id,false):await provider.DeleteHolonSoftAsync(id,false);Assert.IsFalse(deleted.IsError);Assert.IsTrue(deleted.IsDeleted);var absent=await provider.LoadHolonAsync(id,loadChildren:false);Assert.IsNull(absent.Result);Assert.IsFalse(absent.IsLoaded);}
        var deletedAvatar=sync?provider.DeleteAvatar(report.OwnerId,softDelete:false):await provider.DeleteAvatarAsync(report.OwnerId,softDelete:false);Assert.IsFalse(deletedAvatar.IsError);Assert.IsTrue(deletedAvatar.IsDeleted);
        Assert.IsNull((await provider.LoadAvatarAsync(report.OwnerId)).Result);Assert.IsNull((await provider.LoadAvatarDetailAsync(report.OwnerId)).Result);
        File.Move(journal,resolved);Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError);
    }

}
