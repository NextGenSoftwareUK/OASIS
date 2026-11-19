using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Security;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlV1Web4NativeTests
{
    public class OwnedPropertyBoundary : PropertyOutcomeBoundary
    {
        public readonly HashSet<Guid> SelectedCommittedOperations = new();
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            if (method!.Name == nameof(IConditionalHolonPropertyStorageProvider.ExecuteConditionalHolonPropertyWriteAsync))
            {
                PropertyAsyncWrites++;
                return Selected((ConditionalHolonPropertyWriteRequest)args![0]!, (CancellationToken)args[1]!);
            }
            return base.Invoke(method, args);
        }
        private async Task<OASISResult<IEnumerable<IHolon>>> Selected(
            ConditionalHolonPropertyWriteRequest request, CancellationToken token)
        {
            var write = request.WriteRequest.Write;
            await BeforeResource(write.OperationId, write.Conditions.Select(row => row.HolonId));
            var before = StoredRecordJson.SerializeRuntime(Source);
            var result = await ((IConditionalHolonPropertyStorageProvider)Target).ExecuteConditionalHolonPropertyWriteAsync(request, token);
            Assert.AreEqual(before, StoredRecordJson.SerializeRuntime(Source), "The real native call must not publish caller state.");
            if (!result.IsError)
            {
                SelectedCommittedOperations.Add(write.OperationId);
                if (AfterSuccessfulMutation != null) await AfterSuccessfulMutation();
            }
            return result;
        }
    }

    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task OwnedSelectedFieldsUseNativePermissionsEncryptionAndPrivatePublication(string providerName)
        => RunPropertyNativeFixture(providerName, true, false, true, false, false, true);

    private static async Task<int> VerifyOwnedSelectedNativeWrites(IOASISStorageProvider provider, HolonManager manager,
        OwnedPropertyBoundary probe, ProviderType authority, Holon source, Guid actor, Guid writer,
        HolonMetadataEncryptionPolicy policy, HashSet<Guid> operations)
    {
        var checks = 0;
        Guid Revision(IHolon value) => CosmicConcurrency.GetProviderRevisionId(value, authority)!.Value;
        var originalRevision = Revision(source);
        var originalComparison = HolonChangeTracking.GetPropertyValues(source, "/Description").Result.OriginalJson;
        var caller = StoredRecordJson.Project<Holon>(source);
        caller.MetaData = new();
        caller.MetadataEncryptionOverride = policy;
        HolonChangeTracking.AcceptChanges(caller, false);
        caller.Name = "dirty unselected local name";
        caller.Description = "selected field-only description";
        probe.Source = caller;
        CosmicWriteOptions Options(Guid? expected = null)
        {
            var operation = Guid.NewGuid(); operations.Add(operation);
            return new(operation, new Dictionary<Guid, Guid?> { [source.Id] = expected ?? Revision(caller) });
        }
        async Task<Holon> Read(int version)
        {
            var loaded = await provider.LoadHolonAsync(source.Id, false, false, 0, true, false, 0);
            Assert.IsFalse(loaded.IsError, loaded.Message + loaded.Exception);
            Assert.AreEqual(version, loaded.Result.Version);
            var raw = StoredRecordJson.Project<Holon>(loaded.Result);
            Assert.IsTrue(raw.MetaData.ContainsKey("__oasis_enc__"));
            Assert.IsFalse(raw.MetaData.ContainsKey("secret"));
            Assert.IsFalse(raw.MetaData.ContainsKey("private-sibling"));
            raw.MetadataEncryptionOverride = policy;
            var decrypted = manager.DecryptHolonMetaData(raw);
            Assert.IsFalse(decrypted.IsError, decrypted.Message + decrypted.Exception);
            return raw;
        }
        void Hidden()
        {
            Assert.IsFalse(caller.MetaData.ContainsKey("private-sibling"));
            Assert.IsFalse(caller.MetaData.ContainsKey(HolonPermissions.MetadataKey));
            Assert.IsFalse(caller.MetaData.ContainsKey("original"));
            Assert.IsFalse(caller.MetaData.ContainsKey("__oasis_enc__"));
            Assert.AreEqual("dirty unselected local name", caller.Name);
        }
        var firstOptions = Options();
        var first = await manager.SaveOwnedHolonAsync(caller, writer, firstOptions, new[] { "/Description" },
            saveChildren: false, providerType: authority);
        Assert.IsFalse(first.IsError, first.Message + first.Exception); Assert.IsTrue(first.IsSaved);
        Assert.AreSame(caller, first.Result);
        var native = await Read(2);
        Assert.AreEqual("original", native.Name); Assert.AreEqual(caller.Description, native.Description);
        Assert.AreEqual("secret-before", native.MetaData["secret"].ToString());
        Assert.AreEqual("private-native", native.MetaData["private-sibling"].ToString());
        Assert.AreEqual(2, caller.Version); Hidden();
        Assert.AreEqual("\"original\"", HolonChangeTracking.GetPropertyValues(caller, "/Name").Result.OriginalJson);
        Assert.IsTrue(HolonChangeTracking.HasChanges(caller)); checks++;

        caller.MetaData["secret"] = "selected-secret-after";
        var second = await manager.SaveOwnedHolonsAsync(new[] { caller }, writer, Options(),
            new Dictionary<Guid, IReadOnlyList<string>> { [caller.Id] = new[] { "/MetaData/secret" } },
            saveChildren: false, providerType: authority);
        Assert.IsFalse(second.IsError, second.Message + second.Exception);
        native = await Read(3);
        Assert.AreEqual("selected-secret-after", native.MetaData["secret"].ToString());
        Assert.AreEqual("private-native", native.MetaData["private-sibling"].ToString());
        Assert.AreEqual(9007199254740993L, Convert.ToInt64(native.MetaData["original"].ToString()));
        Hidden(); checks++;

        var beforeRefusal = Revision(native);
        var stale = await manager.SaveOwnedHolonAsync(caller, writer, Options(originalRevision), new[] { "/Description" },
            saveChildren: false, providerType: authority);
        Assert.IsTrue(stale.IsError); Assert.AreEqual(CosmicConcurrencyException.Code, stale.ErrorCode);
        Assert.AreEqual(beforeRefusal, Revision(await Read(3))); Hidden(); checks++;

        var calls = probe.PropertyAsyncWrites;
        var denied = await manager.SaveOwnedHolonAsync(caller, writer, Options(), new[] { "/Name" },
            saveChildren: false, providerType: authority);
        Assert.IsTrue(denied.IsError); Assert.AreEqual("COSMIC_WRITE_FORBIDDEN", denied.ErrorCode);
        Assert.AreEqual(calls, probe.PropertyAsyncWrites); checks++;

        caller.MetaData[HolonPermissions.MetadataKey] = HolonPermissionSet.Empty.With(
            b => b.SetGrant(HolonPermissionSet.Key(writer), HolonPermission.All)).Encode().Result;
        var permissionPath = "/MetaData/" + HolonPermissions.MetadataKey.Replace("~", "~0").Replace("/", "~1");
        var manageDenied = await manager.SaveOwnedHolonAsync(caller, writer, Options(), new[] { permissionPath },
            saveChildren: false, providerType: authority);
        Assert.IsTrue(manageDenied.IsError); Assert.AreEqual("COSMIC_MANAGE_FORBIDDEN", manageDenied.ErrorCode);
        Assert.AreEqual(calls, probe.PropertyAsyncWrites);
        caller.MetaData.Remove(HolonPermissions.MetadataKey); checks++;

        var invalid = await manager.SaveOwnedHolonAsync(caller, writer, Options(), new[] { "/ProviderMetaData" },
            saveChildren: false, providerType: authority);
        Assert.IsTrue(invalid.IsError); Assert.AreEqual(calls, probe.PropertyAsyncWrites);
        Assert.AreEqual(beforeRefusal, Revision(await Read(3))); checks++;

        caller.Description = "selected before pending";
        Assert.IsFalse(HolonChangeTracking.SetPropertyModified(caller, "/Description").IsError);
        probe.AfterSuccessfulMutation = () =>
        {
            caller.Description = "late selected local edit";
            Assert.IsFalse(HolonChangeTracking.SetPropertyModified(caller, "/Description").IsError);
            return Task.CompletedTask;
        };
        try
        {
            var pending = await manager.SaveOwnedHolonAsync(caller, writer, Options(), new[] { "/Description" },
                saveChildren: false, providerType: authority);
            Assert.IsFalse(pending.IsError, pending.Message + pending.Exception);
        }
        finally { probe.AfterSuccessfulMutation = null; }
        native = await Read(4);
        Assert.AreEqual("selected before pending", native.Description);
        Assert.AreEqual("late selected local edit", caller.Description);
        var values = HolonChangeTracking.GetPropertyValues(caller, "/Description").Result;
        Assert.AreEqual("\"selected before pending\"", values.OriginalJson);
        Assert.IsTrue(values.IsExplicitlyModified); Hidden(); checks++;

        caller.MetaData.Remove("secret");
        var removed = await manager.SaveOwnedHolonAsync(caller, writer, Options(), new[] { "/MetaData/secret" },
            saveChildren: false, providerType: authority);
        Assert.IsFalse(removed.IsError, removed.Message + removed.Exception);
        native = await Read(5);
        Assert.IsFalse(native.MetaData.ContainsKey("secret"));
        Assert.AreEqual("private-native", native.MetaData["private-sibling"].ToString());
        Assert.AreEqual("late selected local edit", caller.Description);
        Hidden(); checks++;

        var replayCaller = StoredRecordJson.Project<Holon>(source);
        replayCaller.MetaData = new();
        replayCaller.MetadataEncryptionOverride = policy;
        replayCaller.Description = "selected field-only description";
        replayCaller.Name = "private replay caller name";
        probe.Source = replayCaller;
        var replay = await manager.SaveOwnedHolonAsync(replayCaller, writer, firstOptions, new[] { "/Description" },
            saveChildren: false, providerType: authority);
        Assert.IsFalse(replay.IsError, replay.Message + replay.Exception);
        Assert.AreEqual(2, replayCaller.Version);
        Assert.AreEqual("private replay caller name", replayCaller.Name);
        Assert.IsFalse(replayCaller.MetaData.ContainsKey("private-sibling"));
        Assert.AreEqual(Revision(native), Revision(await Read(5))); checks++;

        Assert.AreEqual(4, probe.SelectedCommittedOperations.Count);
        Assert.AreEqual(1, probe.MutationIntents.Count, "Only the initial create may use ordinary whole-record dispatch.");
        Assert.AreEqual(originalComparison, HolonChangeTracking.GetPropertyValues(source, "/Description").Result.OriginalJson);
        Assert.AreEqual(originalRevision, Revision(source));
        Assert.IsFalse(HolonChangeTracking.HasChanges(source)); checks++;
        Assert.AreEqual(10, checks);
        return checks;
    }
}
