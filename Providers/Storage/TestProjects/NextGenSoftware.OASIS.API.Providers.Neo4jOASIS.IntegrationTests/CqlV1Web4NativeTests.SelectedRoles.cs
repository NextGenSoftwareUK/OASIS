using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Serialization;
using RoleOwner = NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests.Neo4jNativeAvatarTests.NativeRoleOwner;
using RoleMember = NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests.Neo4jNativeAvatarTests.NativeRoleMember;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlV1Web4NativeTests
{
    private static async Task<int> VerifyTypedRoleSelectedNative(IOASISStorageProvider provider,
        HolonManager manager, OwnedPropertyBoundary probe, ProviderType authority, Guid actor, Guid id)
    {
        var checks = 0;
        Guid Revision(IHolon value) => CosmicConcurrency.GetProviderRevisionId(value, authority)!.Value;
        async Task<Holon> Read(Guid record)
        {
            var loaded = await provider.LoadHolonAsync(record, false, false, 0, true, false, 0);
            Assert.IsFalse(loaded.IsError, loaded.Message + loaded.Exception); Assert.IsNotNull(loaded.Result);
            return StoredRecordJson.Project<Holon>(loaded.Result);
        }
        HolonChildRoleManifest Manifest(Holon value)
        {
            Assert.IsNotNull(value.ChildRoleManifestJson);
            return HolonChildRoleManifestJson.Deserialize(value.ChildRoleManifestJson);
        }
        void Roles(Holon value, Guid ordinary, params Guid[] components)
        {
            var manifest = Manifest(value); Assert.AreEqual(id, manifest.ParentId);
            Assert.AreEqual(2, manifest.Roles.Count);
            CollectionAssert.AreEqual(new[] { ordinary }, manifest.Roles.Single(role => role.Name == "Children").ChildIds.ToArray());
            CollectionAssert.AreEqual(components, manifest.Roles.Single(role => role.Name == "Components").ChildIds.ToArray());
        }
        async Task<RoleOwner> Complete()
        {
            var stored = await Read(id); var manifest = Manifest(stored);
            var values = new Dictionary<Guid, IHolon>();
            foreach (var member in manifest.Roles.SelectMany(role => role.ChildIds).Distinct()) values.Add(member, await Read(member));
            var typed = StoredRecordJson.Project<RoleOwner>(stored);
            var hydrated = HolonChildRoleHydration.ApplyFullySelected(typed, manifest, member => values[member]);
            Assert.IsFalse(hydrated.IsError, hydrated.Message + hydrated.Exception);
            HolonChangeTracking.AcceptChanges(typed, true); return typed;
        }
        var root = new RoleOwner { Id = id, Name = "typed role parent", Description = "initial", IsActive = true };
        root.Children.Add(new Holon { Name = "ordinary child", IsActive = true });
        root.Components.Add(new RoleMember { Name = "component child", IsActive = true });
        probe.Source = root;
        var created = await manager.SaveOwnedHolonAsync(root, actor, new CosmicWriteOptions(Guid.NewGuid()),
            saveChildren: true, providerType: authority);
        Assert.IsFalse(created.IsError, created.Message + created.Exception); Assert.IsTrue(created.IsSaved);
        var ordinary = root.Children.Single().Id; var component = root.Components.Single().Id;
        Assert.AreNotEqual(Guid.Empty, ordinary); Assert.AreNotEqual(Guid.Empty, component);
        var firstNative = await Read(id); Roles(firstNative, ordinary, component);
        Assert.AreEqual(1, firstNative.Version); Assert.AreEqual(1, (await Read(ordinary)).Version);
        Assert.AreEqual(1, (await Read(component)).Version); checks++;

        var replay = await Complete();
        var scalarOptions = new CosmicWriteOptions(Guid.NewGuid(), new Dictionary<Guid, Guid?> { [id] = Revision(root) });
        root.Name = "excluded local typed name"; root.Description = "typed selected scalar";
        Assert.IsFalse(HolonChangeTracking.SetPropertyModified(root, "/Description").IsError);
        probe.Source = root;
        var scalar = await manager.SaveOwnedHolonAsync(root, actor, scalarOptions, new[] { "/Description" },
            saveChildren: false, providerType: authority);
        Assert.IsFalse(scalar.IsError, scalar.Message + scalar.Exception); Assert.IsTrue(scalar.IsSaved);
        var secondNative = await Read(id); Roles(secondNative, ordinary, component);
        Assert.AreEqual(2, secondNative.Version); Assert.AreEqual("typed selected scalar", secondNative.Description);
        Assert.AreEqual("typed role parent", secondNative.Name); Assert.AreEqual("excluded local typed name", root.Name);
        Assert.AreEqual("\"typed role parent\"", HolonChangeTracking.GetPropertyValues(root, "/Name").Result.OriginalJson);
        Assert.IsFalse(HolonChangeTracking.GetPropertyValues(root, "/Description").Result.IsExplicitlyModified);
        Assert.AreEqual(1, (await Read(ordinary)).Version); Assert.AreEqual(1, (await Read(component)).Version); checks++;

        var partial = StoredRecordJson.Project<RoleOwner>(secondNative);
        var actualComponent = await Read(component); var resolutions = new List<Guid>();
        var view = HolonChildRoleHydration.ApplySelectedView(partial, Manifest(secondNative), new[] { component }, member =>
        {
            resolutions.Add(member); Assert.AreEqual(component, member); return actualComponent;
        });
        Assert.IsFalse(view.IsError, view.Message + view.Exception);
        CollectionAssert.AreEqual(new[] { component }, resolutions.ToArray());
        Assert.AreEqual(0, partial.Children.Count); Assert.AreEqual(1, partial.Components.Count);
        HolonChangeTracking.AcceptChanges(partial, false);
        partial.Description = "partial scalar"; partial.Name = "excluded partial name";
        Assert.IsFalse(HolonChangeTracking.SetPropertyModified(partial, "/Description").IsError);
        var beforeCaller = StoredRecordJson.SerializeRuntime(partial);
        var original = HolonChangeTracking.GetPropertyValues(partial, "/Description").Result.OriginalJson;
        var beforeWrites = probe.PropertyAsyncWrites + probe.MutationIntents.Count;
        probe.Source = partial;
        var denied = await manager.SaveOwnedHolonAsync(partial, actor, new CosmicWriteOptions(Guid.NewGuid()),
            new[] { "/Description" }, saveChildren: true, providerType: authority);
        Assert.IsTrue(denied.IsError);
        StringAssert.Contains(denied.Exception?.Message ?? denied.Message, "fully observed");
        Assert.AreEqual(beforeCaller, StoredRecordJson.SerializeRuntime(partial));
        Assert.AreEqual(original, HolonChangeTracking.GetPropertyValues(partial, "/Description").Result.OriginalJson);
        Assert.IsTrue(HolonChangeTracking.GetPropertyValues(partial, "/Description").Result.IsExplicitlyModified);
        Assert.AreEqual(beforeWrites, probe.PropertyAsyncWrites + probe.MutationIntents.Count);
        Assert.AreEqual(StoredRecordJson.SerializeRuntime(secondNative), StoredRecordJson.SerializeRuntime(await Read(id))); checks++;

        var partialSaved = await manager.SaveOwnedHolonAsync(partial, actor, new CosmicWriteOptions(Guid.NewGuid()),
            new[] { "/Description" }, saveChildren: false, providerType: authority);
        Assert.IsFalse(partialSaved.IsError, partialSaved.Message + partialSaved.Exception);
        var thirdNative = await Read(id); Roles(thirdNative, ordinary, component);
        Assert.AreEqual(3, thirdNative.Version); Assert.AreEqual("partial scalar", thirdNative.Description);
        Assert.AreEqual("typed role parent", thirdNative.Name); Assert.AreEqual("excluded partial name", partial.Name);
        Assert.AreEqual(0, partial.Children.Count); Assert.AreEqual(1, partial.Components.Count);
        Assert.IsFalse(HolonChangeTracking.GetPropertyValues(partial, "/Description").Result.IsExplicitlyModified);
        var stillPartial = await manager.SaveOwnedHolonAsync(partial, actor, new CosmicWriteOptions(Guid.NewGuid()),
            new[] { "/Description" }, saveChildren: true, providerType: authority);
        Assert.IsTrue(stillPartial.IsError);
        StringAssert.Contains(stillPartial.Exception?.Message ?? stillPartial.Message, "fully observed");
        Assert.AreEqual(3, (await Read(id)).Version); checks++;

        var full = await Complete();
        full.Description = "typed selected graph"; full.Name = "excluded full graph name";
        full.Components.Add(new RoleMember { Name = "second component", IsActive = true });
        probe.Source = full;
        var graph = await manager.SaveOwnedHolonAsync(full, actor, new CosmicWriteOptions(Guid.NewGuid()),
            new[] { "/Description" }, saveChildren: true, providerType: authority);
        Assert.IsFalse(graph.IsError, graph.Message + graph.Exception); Assert.IsTrue(graph.IsSaved);
        var added = full.Components.Last().Id; Assert.AreNotEqual(Guid.Empty, added);
        var fourthNative = await Read(id); Roles(fourthNative, ordinary, component, added);
        Assert.AreEqual(4, fourthNative.Version); Assert.AreEqual("typed selected graph", fourthNative.Description);
        Assert.AreEqual("typed role parent", fourthNative.Name); Assert.AreEqual("excluded full graph name", full.Name);
        Assert.AreEqual(2, (await Read(ordinary)).Version); Assert.AreEqual(2, (await Read(component)).Version);
        var newMember = await Read(added); Assert.AreEqual(1, newMember.Version);
        Assert.AreEqual(id, newMember.ParentHolonId); Assert.AreEqual(actor, newMember.CreatedByAvatarId); checks++;

        replay.Description = "typed selected scalar"; replay.Name = "replay excluded name"; probe.Source = replay;
        var replayed = await manager.SaveOwnedHolonAsync(replay, actor, scalarOptions, new[] { "/Description" },
            saveChildren: false, providerType: authority);
        Assert.IsFalse(replayed.IsError, replayed.Message + replayed.Exception);
        Assert.AreEqual(2, replay.Version); Assert.AreEqual("replay excluded name", replay.Name);
        Assert.AreEqual(StoredRecordJson.SerializeRuntime(fourthNative), StoredRecordJson.SerializeRuntime(await Read(id)));
        Roles(await Read(id), ordinary, component, added);
        Assert.AreEqual(1, probe.MutationIntents.Count, "Only initial creation may use a whole graph write.");
        Assert.AreEqual(4, probe.PropertyAsyncWrites); Assert.AreEqual(3, probe.SelectedCommittedOperations.Count);
        checks++; return checks;
    }
}
