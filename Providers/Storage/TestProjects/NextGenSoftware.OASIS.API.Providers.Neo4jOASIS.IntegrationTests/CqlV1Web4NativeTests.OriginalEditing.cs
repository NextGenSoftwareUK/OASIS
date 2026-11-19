using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Security;
using NextGenSoftware.OASIS.API.Core.Serialization;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlV1Web4NativeTests
{
    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task EditableOriginalsPreserveNativeConditionsAndInflightComparisons(string providerName)
        => RunPropertyNativeFixture(providerName, true, false, true, false, false, false, true);

    private static async Task<int> VerifyOriginalEditingNativeWrites(IOASISStorageProvider provider, HolonManager manager,
        OwnedPropertyBoundary probe, ProviderType authority, Holon source, Guid actor, Guid writer,
        HolonMetadataEncryptionPolicy policy, HashSet<Guid> operations)
    {
        var checks = 0;
        Guid Revision(IHolon value) => CosmicConcurrency.GetProviderRevisionId(value, authority)!.Value;
        Guid Accepted(IHolon value)
        {
            Assert.IsTrue(HolonChangeTracking.TryGetAcceptedRevision(value, authority, out var revision));
            return revision;
        }
        void Good(NextGenSoftware.OASIS.Common.OASISResult<bool> result)
            => Assert.IsFalse(result.IsError, result.Message + result.Exception);
        CosmicWriteOptions Options()
        {
            var operation = Guid.NewGuid(); operations.Add(operation);
            return new(operation);
        }
        async Task<Holon> Read(int version)
        {
            var loaded = await provider.LoadHolonAsync(source.Id, false, false, 0, true, false, 0);
            Assert.IsFalse(loaded.IsError, loaded.Message + loaded.Exception);
            Assert.AreEqual(version, loaded.Result.Version);
            var value = StoredRecordJson.Project<Holon>(loaded.Result);
            Assert.IsTrue(value.MetaData.ContainsKey("__oasis_enc__"));
            Assert.IsFalse(value.MetaData.ContainsKey("private-sibling"));
            value.MetadataEncryptionOverride = policy;
            Good(manager.DecryptHolonMetaData(value));
            return value;
        }
        Holon Client(IHolon stored)
        {
            var value = StoredRecordJson.Project<Holon>(stored);
            value.MetaData = new(); value.MetadataEncryptionOverride = policy;
            HolonChangeTracking.AcceptChanges(value, false);
            return value;
        }
        void Private(Holon value)
        {
            Assert.IsFalse(value.MetaData.ContainsKey("private-sibling"));
            Assert.IsFalse(value.MetaData.ContainsKey("original"));
            Assert.IsFalse(value.MetaData.ContainsKey("secret"));
            Assert.IsFalse(value.Original.MetaData.ContainsKey("private-sibling"));
        }
        var firstNative = await Read(1);
        var rights = new HolonAccessResolver(writer, includePublic: false).WithGroups(Array.Empty<Guid>()).Resolve(firstNative);
        Assert.IsTrue(rights.FieldAllows("description", HolonPermission.Write));
        Assert.IsFalse(rights.CanSee);
        var caller = Client(firstNative); var initialRevision = Accepted(caller);
        caller.Name = "local current"; caller.Description = "first original write";
        var currentBefore = StoredRecordJson.SerializeRuntime(caller);
        Good(HolonChangeTracking.SetOriginalPropertyValue(caller, "/Name", "client comparison"));
        Assert.AreEqual(currentBefore, StoredRecordJson.SerializeRuntime(caller));
        Assert.AreEqual(initialRevision, Accepted(caller)); Private(caller); checks++;

        probe.Source = caller;
        var firstOptions = Options();
        var first = await manager.SaveOwnedHolonAsync(caller, writer, firstOptions, new[] { "/Description" },
            saveChildren: false, providerType: authority);
        Assert.IsFalse(first.IsError, first.Message + first.Exception);
        var native = await Read(2);
        Assert.AreEqual("original", native.Name); Assert.AreEqual("first original write", native.Description);
        Assert.AreEqual("local current", caller.Name); Assert.AreEqual("client comparison", caller.Original.Name);
        Assert.AreEqual(Revision(native), Accepted(caller)); Private(caller); checks++;

        var beforeOwner = Accepted(caller);
        native.Name = "owner advanced";
        var ownerWrite = await manager.SaveOwnedHolonAsync(native, actor, Options(), new[] { "/Name" },
            saveChildren: false, providerType: authority);
        Assert.IsFalse(ownerWrite.IsError, ownerWrite.Message + ownerWrite.Exception);
        native = await Read(3);
        CosmicConcurrency.SetProviderRevisionId(caller.Original, authority, Revision(native));
        Good(HolonChangeTracking.SetOriginalPropertyValue(caller, "/Name", caller.Name));
        Assert.AreEqual(beforeOwner, Accepted(caller));
        var stale = await manager.SaveOwnedHolonAsync(caller, writer, Options(), new[] { "/Description" },
            saveChildren: false, providerType: authority);
        Assert.IsTrue(stale.IsError); Assert.AreEqual(CosmicConcurrencyException.Code, stale.ErrorCode);
        Assert.AreEqual(Revision(native), Revision(await Read(3))); Private(caller); checks++;

        var fresh = Client(native);
        fresh.Name = "late local name"; fresh.Description = "pending original write";
        Good(HolonChangeTracking.SetPropertyModified(fresh, "/Description"));
        probe.Source = fresh;
        probe.AfterSuccessfulMutation = () =>
        {
            Good(HolonChangeTracking.SetOriginalPropertyValues(fresh, new Dictionary<string, object>
                { ["/Description"] = "in-flight comparison", ["/Name"] = "rebased name" }));
            return Task.CompletedTask;
        };
        try
        {
            var pending = await manager.SaveOwnedHolonAsync(fresh, writer, Options(), new[] { "/Description" },
                saveChildren: false, providerType: authority);
            Assert.IsFalse(pending.IsError, pending.Message + pending.Exception);
        }
        finally { probe.AfterSuccessfulMutation = null; }
        native = await Read(4);
        Assert.AreEqual("pending original write", native.Description);
        Assert.AreEqual("pending original write", fresh.Description);
        Assert.AreEqual("\"in-flight comparison\"", HolonChangeTracking.GetPropertyValues(fresh, "/Description").Result.OriginalJson);
        Assert.AreEqual("rebased name", fresh.Original.Name);
        Assert.IsFalse(HolonChangeTracking.GetPropertyValues(fresh, "/Description").Result.IsExplicitlyModified);
        Assert.AreEqual(Revision(native), Accepted(fresh)); Private(fresh); checks++;

        fresh.Description = "after original rebase";
        var after = await manager.SaveOwnedHolonAsync(fresh, writer, Options(), new[] { "/Description" },
            saveChildren: false, providerType: authority);
        Assert.IsFalse(after.IsError, after.Message + after.Exception);
        native = await Read(5);
        Assert.AreEqual("after original rebase", native.Description);
        Assert.AreEqual(Revision(native), Accepted(fresh));
        Assert.AreEqual("late local name", fresh.Name); Assert.AreEqual("rebased name", fresh.Original.Name);
        Private(fresh); checks++;

        var calls = probe.PropertyAsyncWrites;
        fresh.MetaData[HolonPermissions.MetadataKey] = HolonPermissionSet.Empty.With(
            b => b.SetGrant(HolonPermissionSet.Key(writer), HolonPermission.All)).Encode().Result;
        var path = "/MetaData/" + HolonPermissions.MetadataKey.Replace("~", "~0").Replace("/", "~1");
        Good(HolonChangeTracking.SetOriginalPropertyValue(fresh, path, fresh.MetaData[HolonPermissions.MetadataKey]));
        var manage = await manager.SaveOwnedHolonAsync(fresh, writer, Options(), new[] { path },
            saveChildren: false, providerType: authority);
        Assert.IsTrue(manage.IsError); Assert.AreEqual("COSMIC_MANAGE_FORBIDDEN", manage.ErrorCode);
        Assert.AreEqual(calls, probe.PropertyAsyncWrites);
        fresh.MetaData.Remove(HolonPermissions.MetadataKey); checks++;

        Assert.IsTrue(HolonChangeTracking.SetOriginalPropertyValue(fresh, "/ProviderMetaData",
            new Dictionary<ProviderType, Dictionary<string, string>>()).IsError);
        Assert.AreEqual(Revision(native), Accepted(fresh));
        Assert.AreEqual(calls, probe.PropertyAsyncWrites); checks++;

        HolonChangeTracking.MarkIncompleteChildSelection(fresh);
        Good(HolonChangeTracking.SetOriginalPropertyValue(fresh, "/Name", "partial comparison"));
        Assert.ThrowsException<InvalidOperationException>(() => HolonChangeTracking.ValidateChildSelectionForSave(fresh));
        Assert.AreEqual(calls, probe.PropertyAsyncWrites); checks++;

        var replayCaller = Client(source);
        replayCaller.Name = "local replay"; replayCaller.Description = "first original write";
        Good(HolonChangeTracking.SetOriginalPropertyValues(replayCaller, new Dictionary<string, object>
            { ["/Name"] = "replay comparison", ["/Description"] = "different comparison original" }));
        probe.Source = replayCaller;
        var replay = await manager.SaveOwnedHolonAsync(replayCaller, writer, firstOptions, new[] { "/Description" },
            saveChildren: false, providerType: authority);
        Assert.IsFalse(replay.IsError, replay.Message + replay.Exception);
        Assert.AreEqual(2, replayCaller.Version);
        Assert.AreEqual("local replay", replayCaller.Name); Assert.AreEqual("replay comparison", replayCaller.Original.Name);
        Assert.AreEqual(Revision(native), Revision(await Read(5))); Private(replayCaller); checks++;

        Assert.AreEqual(4, probe.SelectedCommittedOperations.Count);
        Assert.AreEqual(1, probe.MutationIntents.Count);
        Assert.AreEqual(initialRevision, Revision(source));
        Assert.IsFalse(HolonChangeTracking.HasChanges(source, false));
        Assert.AreEqual("owner advanced", native.Name);
        Assert.AreEqual("private-native", native.MetaData["private-sibling"].ToString());
        Assert.AreEqual("secret-before", native.MetaData["secret"].ToString());
        checks++;
        Assert.AreEqual(10, checks);
        return checks;
    }
}
