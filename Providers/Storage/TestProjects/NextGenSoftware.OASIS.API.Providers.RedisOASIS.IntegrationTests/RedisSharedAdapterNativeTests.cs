using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using StackExchange.Redis;

namespace NextGenSoftware.OASIS.API.Providers.RedisOASIS.IntegrationTests;

[TestClass,DoNotParallelize]
public class RedisSharedAdapterNativeTests
{
    [TestMethod,TestCategory("OwnedNativeSharedAdapter")]
    public async Task NativeStateComparisonEnumerationAndScopedCleanup()
    {
        Assert.AreEqual("127.0.0.1:18679",Environment.GetEnvironmentVariable("OASIS_REDIS_NATIVE_ENDPOINT"));
        var work=Path.GetFullPath(Environment.GetEnvironmentVariable("OASIS_REDIS_NATIVE_WORK")??throw new InvalidOperationException("Explicit proof directory required."));
        Assert.AreEqual(Path.GetFullPath("C:/Source/OASIS/TestResults/RedisCurrent"),work);
        var pending=Path.Combine(work,"shared-adapter.pending.json");var resolved=Path.Combine(work,"shared-adapter.resolved.json");
        Assert.IsFalse(File.Exists(pending));Assert.IsFalse(File.Exists(resolved));
        var options=ConfigurationOptions.Parse("127.0.0.1:18679,abortConnect=true,connectRetry=0");
        options.Password=Environment.GetEnvironmentVariable("OASIS_REDIS_NATIVE_PASSWORD")??throw new InvalidOperationException("Existing protected test credential required.");
        options.BacklogPolicy=BacklogPolicy.FailFast;
        using var mux=await ConnectionMultiplexer.ConnectAsync(options);var db=mux.GetDatabase();
        var prefix="oasis/redis-adapter-native/"+Guid.NewGuid().ToString("N")+"/";
        var keys=new[]{prefix+"first",prefix+"second"};
        Assert.IsTrue((await db.StringGetAsync(keys.Select(key=>(RedisKey)key).ToArray())).All(value=>value.IsNull));
        var type=typeof(RedisOASIS).Assembly.GetType("NextGenSoftware.OASIS.API.Providers.RedisOASIS.RedisKeyValueBackend",true)!;
        Func<Task<IDatabase>> database=()=>Task.FromResult(db);
        Func<Task<IReadOnlyList<IServer>>> primaries=()=>Task.FromResult<IReadOnlyList<IServer>>(mux.GetEndPoints().Select(endpoint=>mux.GetServer(endpoint)).Where(server=>!server.IsReplica).ToArray());
        var backend=(IConditionalKeyValueBackend)Activator.CreateInstance(type,new object[]{database,primaries,prefix})!;
        await backend.VerifyAsync();Assert.IsNull(await backend.ReadObservedAsync(keys[0]));
        // Durable ownership evidence BEFORE any mutation. On any failure preserve
        // this intent and server; never compensate/replay an uncertain operation.
        using(var intent=new FileStream(pending,FileMode.CreateNew,FileAccess.Write,FileShare.None))
        {
            JsonSerializer.Serialize(intent,new{schema=1,fixture="redis-shared-adapter",keys,initial="verified-absent",plannedValues=new[]{"alpha","beta","one","two"}});intent.Flush(true);
        }
        await backend.CreateOnlyAsync(keys[0],"alpha");
        var original=await backend.ReadObservedAsync(keys[0]);Assert.IsNotNull(original);Assert.AreEqual("alpha",original.Value);
        await Assert.ThrowsExceptionAsync<StorageLifecycleConflictException>(()=>backend.CreateOnlyAsync(keys[0],"not-created"));
        Assert.AreEqual("alpha",(string?)await db.StringGetAsync(keys[0]));
        await backend.ReplaceUnchangedAsync(keys[0],"beta",original.Revision);
        var current=await backend.ReadObservedAsync(keys[0]);Assert.IsNotNull(current);Assert.AreEqual("beta",current.Value);
        Assert.AreNotEqual(original.Revision,current.Revision);
        await Assert.ThrowsExceptionAsync<StorageLifecycleConflictException>(()=>backend.ReplaceUnchangedAsync(keys[0],"lost-update",original.Revision));
        await Assert.ThrowsExceptionAsync<StorageLifecycleConflictException>(()=>backend.DeleteUnchangedAsync(keys[0],original.Revision));
        await Assert.ThrowsExceptionAsync<ArgumentException>(()=>backend.ReplaceUnchangedAsync(keys[1],"wrong-scope",current.Revision));
        using(var cancellation=new CancellationTokenSource())
        {
            cancellation.Cancel();await Assert.ThrowsExceptionAsync<OperationCanceledException>(()=>backend.CreateOnlyAsync(keys[1],"never-dispatched",cancellation.Token));
        }
        Assert.IsFalse(await db.KeyExistsAsync(keys[1]));
        await backend.DeleteUnchangedAsync(keys[0],current.Revision);Assert.IsNull(await backend.GetAsync(keys[0]));
        await backend.DeleteAsync(keys[0]); // Acknowledged absence is idempotent, not a transport error.
        await backend.CreateOnlyAsync(keys[0],"one");await backend.PutAsync(keys[1],"two");
        CollectionAssert.AreEquivalent(keys.ToArray(),(await backend.ListKeysAsync(prefix)).ToArray());
        var expected=new Dictionary<string,string>{{keys[0],"one"},{keys[1],"two"}};
        var observations=new Dictionary<string,KeyValueObservation>();
        foreach(var key in keys)
        {
            var observation=await backend.ReadObservedAsync(key);Assert.IsNotNull(observation);
            Assert.AreEqual(expected[key],observation.Value,"Exact final native state required BEFORE any cleanup.");observations.Add(key,observation);
        }
        foreach(var key in keys)await backend.DeleteUnchangedAsync(key,observations[key].Revision);
        Assert.IsTrue((await db.StringGetAsync(keys.Select(key=>(RedisKey)key).ToArray())).All(value=>value.IsNull));
        Assert.AreEqual(0,(await backend.ListKeysAsync(prefix)).Count);
        File.Move(pending,resolved);
    }
}
