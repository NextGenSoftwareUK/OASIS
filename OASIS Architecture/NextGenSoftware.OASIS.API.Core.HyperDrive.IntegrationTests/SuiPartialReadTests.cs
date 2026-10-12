using System.Reflection;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.SuiOASIS;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public class SuiPartialReadTests
{
    private sealed class FaultedReadBackend(IKeyValueBackend inner, string failedKey) : IKeyValueBackend
    {
        public int FailedReads;
        public Task<string> GetAsync(string key, CancellationToken ct = default)
        {
            if (key == failedKey) { Interlocked.Increment(ref FailedReads); throw new IOException("controlled child-read interruption"); }
            return inner.GetAsync(key, ct);
        }
        public Task PutAsync(string key, string value, CancellationToken ct = default) => inner.PutAsync(key, value, ct);
        public Task DeleteAsync(string key, CancellationToken ct = default) => inner.DeleteAsync(key, ct);
        public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken ct = default) => inner.ListKeysAsync(prefix, ct);
        public Task VerifyAsync(CancellationToken ct = default) => inner.VerifyAsync(ct);
    }
    private sealed class FaultedProvider(IKeyValueBackend backend) : KeyValueStorageProviderBase(backend)
    {
        public void Configure()
        {
            ProviderName = "Owned Sui partial-read fixture";
            ProviderType = new EnumValue<ProviderType>(API.Core.Enums.ProviderType.SuiOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(API.Core.Enums.ProviderCategory.StorageAndNetwork);
        }
    }
    private static string Required(string name) => Environment.GetEnvironmentVariable(name)
        ?? throw new InvalidOperationException("Missing retained Sui fixture setting: " + name);

    [Fact]
    public async Task ActualSdkStorageRetainsValidChildrenAndV2ExposesControlledPartialRead()
    {
        var rpc = Required("OASIS_SUI_TEST_RPC"); Assert.True(new Uri(rpc).IsLoopback);
        var root = Path.GetFullPath(Required("OASIS_HYPERDRIVE_TEST_ROOT")); Assert.Equal("HyperDriveSui", Path.GetFileName(root));
        Directory.CreateDirectory(root);
        using var lease = new FileStream(Path.Combine(root, "store.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var intentPath = Path.Combine(root, "partial-read-intent.json");
        Assert.False(File.Exists(intentPath), "Reconcile retained partial-read intent before another mutation.");
        using var fixture = JsonDocument.Parse(File.ReadAllText(Required("OASIS_SUI_TEST_DEPLOYMENT")));
        var settings = fixture.RootElement;
        using var real = new SuiOASIS(rpc, "localnet", settings.GetProperty("chainId").GetString()!,
            settings.GetProperty("packageAddress").GetString()!, settings.GetProperty("privateKey").GetString()!,
            settings.GetProperty("storageObjectId").GetString()!);
        var activation = await real.ActivateProviderAsync(); Assert.False(activation.IsError, activation.Message);
        var parent = new Holon { Id = Guid.NewGuid(), Name = "owned-sui-partial-parent" };
        var healthy = new Holon { Id = Guid.NewGuid(), Name = "owned-sui-partial-healthy", ParentHolonId = parent.Id };
        var interrupted = new Holon { Id = Guid.NewGuid(), Name = "owned-sui-partial-interrupted", ParentHolonId = parent.Id };
        var records = new[] { parent, healthy, interrupted };
        using (var journal = new FileStream(intentPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            journal.Write(JsonSerializer.SerializeToUtf8Bytes(new { chainId = settings.GetProperty("chainId").GetString(),
                records = records.Select(record => new { record.Id, record.Name, record.ParentHolonId }) }));
            journal.Flush(true);
        }
        var errors = new List<Exception>();
        try
        {
            foreach (var record in records)
            {
                var saved = await real.SaveHolonAsync(record); Assert.False(saved.IsError, saved.Message);
            }
            var inner = (IKeyValueBackend)typeof(KeyValueStorageProviderBase).GetProperty("Backend", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(real)!;
            var backend = new FaultedReadBackend(inner, "oasis/holon/" + interrupted.Id.ToString("N"));
            var controlled = new FaultedProvider(backend); controlled.Configure();
            Assert.False((await controlled.ActivateProviderAsync()).IsError);
            var dna = new OASISDNA(); dna.OASIS.HyperDriveMode = HyperDriveModes.V2;
            dna.OASIS.SubscriptionConfig.PlanType = "Gold";
            var manager = new ProviderManager(null, dna) { IsAutoFailOverEnabled = false, IsAutoReplicationEnabled = false };
            manager.RegisterProvider(controlled);
            var router = new OASISHyperDrive(manager);
            foreach (var bestEffort in new[] { false, true })
            {
                var result = await router.RouteRequestAsync<IHolon>(new StorageOperationRequest {
                    Operation = "LoadHolon", HolonId = parent.Id, PreferredProvider = API.Core.Enums.ProviderType.SuiOASIS,
                    LoadChildren = true, Recursive = true, ContinueOnError = bestEffort });
                Assert.Equal(!bestEffort, result.IsError);
                if (bestEffort)
                {
                    Assert.True(result.IsWarning); Assert.Equal(parent.Id, result.Result.Id);
                    Assert.Equal(healthy.Id, result.Result.Children.Single().Id);
                    Assert.Equal("1", result.MetaData["failedHolonReadCount"]);
                    Assert.Contains(interrupted.Id.ToString(), result.MetaData["failedHolonReads"]);
                }
            }
            Assert.Equal(2, backend.FailedReads);
            var original = await real.LoadHolonAsync(interrupted.Id, loadChildren: false);
            Assert.False(original.IsError, original.Message); Assert.Equal(interrupted.Name, original.Result.Name);
        }
        catch (Exception error) { errors.Add(error); }
        finally
        {
            foreach (var record in records)
            {
                try
                {
                    var read = await real.LoadHolonAsync(record.Id, loadChildren: false);
                    if (read.IsError && read.ErrorCode == "OASIS_RECORD_NOT_FOUND" && read.Exception == null) continue;
                    Assert.False(read.IsError, read.Message);
                    if (read.Result == null) continue;
                    Assert.Equal(record.Name, read.Result.Name); Assert.Equal(record.ParentHolonId, read.Result.ParentHolonId);
                    var deleted = await real.DeleteHolonAsync(record.Id); Assert.False(deleted.IsError, deleted.Message);
                }
                catch (Exception error) { errors.Add(error); }
            }
            if (errors.Count == 0) File.Delete(intentPath);
        }
        if (errors.Count > 0) throw new AggregateException("Owned Sui partial-read fixture requires reconciliation; intent preserved.", errors);
    }
}
