using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.OASIS.Providers.TestSupport;

namespace NextGenSoftware.OASIS.API.Providers.EdgeStorage.ProtocolTests;

// Provider + official SDK controlled-HTTP contracts, not hosted AWS acceptance.
[TestClass]
public class AwsS3GraphLifecycleTests
{
    [DataTestMethod]
    [DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task Export_version_is_independent_of_avatar_identity_over_real_sdk(bool sync,bool email)
    {
        using var store = new AwsS3EncryptedLifecycleTests.Store(); var key = RandomNumberGenerator.GetBytes(32);
        try
        {
            using var provider = store.Open(key); Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            var avatar = new Avatar { Id = Guid.NewGuid(), Username = "private-export-owner", Email = "private-export@example.invalid", IsActive = true };
            Assert.IsFalse((await provider.SaveAvatarAsync(avatar)).IsError); Assert.AreEqual(1, avatar.Version);
            var own = new Holon { Id = Guid.NewGuid(), CreatedByAvatarId = avatar.Id };
            var foreign = new Holon { Id = Guid.NewGuid(), CreatedByAvatarId = Guid.NewGuid() };
            for (var version = 1; version <= 3; version++)
            {
                own.Name = "private-export-own-" + version; foreign.Name = "private-export-foreign-" + version;
                var saved = await provider.SaveHolonAsync(own, saveChildren: false); Assert.IsFalse(saved.IsError, saved.Message + saved.Exception);
                Assert.IsFalse((await provider.SaveHolonAsync(foreign, saveChildren: false)).IsError);
            }
            var before = store.Objects.OrderBy(p => p.Key, StringComparer.Ordinal).ToArray();
            var result = email
                ? sync ? provider.ExportAllDataForAvatarByEmail(avatar.Email, 3) : await provider.ExportAllDataForAvatarByEmailAsync(avatar.Email, 3)
                : sync ? provider.ExportAllDataForAvatarByUsername(avatar.Username, 3) : await provider.ExportAllDataForAvatarByUsernameAsync(avatar.Username, 3);
            Assert.IsFalse(result.IsError, result.Message + result.Exception); Assert.IsTrue(result.IsLoaded); Assert.AreEqual(1, result.LoadedCount);
            Assert.AreEqual(own.Id, result.Result.Single().Id); Assert.AreEqual(3, result.Result.Single().Version);
            Assert.AreEqual("private-export-own-3", result.Result.Single().Name);
            Assert.AreEqual(1, (await provider.LoadAvatarAsync(avatar.Id)).Result.Version);
            var allVersions=email
                ?sync?provider.ExportAllDataForAvatarByEmail(avatar.Email,-1):await provider.ExportAllDataForAvatarByEmailAsync(avatar.Email,-1)
                :sync?provider.ExportAllDataForAvatarByUsername(avatar.Username,-1):await provider.ExportAllDataForAvatarByUsernameAsync(avatar.Username,-1);
            Assert.IsFalse(allVersions.IsError,allVersions.Message+allVersions.Exception);Assert.IsTrue(allVersions.IsLoaded);Assert.AreEqual(3,allVersions.LoadedCount);
            CollectionAssert.AreEqual(new[]{1,2,3},allVersions.Result.Select(row=>row.Version).ToArray());
            Assert.IsTrue(allVersions.Result.All(row=>row.Id==own.Id&&row.CreatedByAvatarId==avatar.Id));
            var requests = store.Requests;
            var invalid = email ? await provider.ExportAllDataForAvatarByEmailAsync(avatar.Email, -2)
                : await provider.ExportAllDataForAvatarByUsernameAsync(avatar.Username, -2);
            Assert.IsTrue(invalid.IsError); Assert.IsInstanceOfType<ArgumentOutOfRangeException>(invalid.Exception);
            Assert.AreEqual(requests, store.Requests, "Invalid export version must fail before SDK identity reads.");
            CollectionAssert.AreEqual(before, store.Objects.OrderBy(p => p.Key, StringComparer.Ordinal).ToArray());
            store.AssertPrivate(avatar.Username, avatar.Email, "private-export-own-3");
        }
        finally { CryptographicOperations.ZeroMemory(key); }
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task Named_roles_root_only_saves_and_independent_child_versions_are_preserved(bool sync)
    {
        using var store = new AwsS3EncryptedLifecycleTests.Store(); var key = RandomNumberGenerator.GetBytes(32);
        try
        {
            using var provider = store.Open(key);
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            var owner = new NamedChildOwner { Id = Guid.NewGuid(), Name = "private-role-owner" };
            var child = new Holon { Id = Guid.NewGuid(), Name = "private-member-v1" }; owner.Components.Add(child);
            var created = sync ? provider.SaveHolon(owner) : await provider.SaveHolonAsync(owner);
            Assert.IsFalse(created.IsError, created.Message + created.Exception);
            Assert.AreEqual(owner.Id, child.ParentHolonId); Assert.AreEqual(1, owner.Version); Assert.AreEqual(1, child.Version);
            var manifest = owner.ChildRoleManifestJson; Assert.IsFalse(string.IsNullOrWhiteSpace(manifest));
            child.Name = "private-member-v2";
            var childUpdate = sync ? provider.SaveHolon(child, saveChildren: false) : await provider.SaveHolonAsync(child, saveChildren: false);
            Assert.IsFalse(childUpdate.IsError, childUpdate.Message + childUpdate.Exception); Assert.AreEqual(2, child.Version);
            var childBefore = OasisJson.Serialize((await provider.LoadHolonAsync(child.Id, loadChildren: false)).Result);
            var scalar = new Holon { Id = owner.Id, Name = "private-role-owner-v2" };
            var rootOnly = sync ? provider.SaveHolon(scalar, saveChildren: false) : await provider.SaveHolonAsync(scalar, saveChildren: false);
            Assert.IsFalse(rootOnly.IsError, rootOnly.Message + rootOnly.Exception); Assert.AreEqual(manifest, scalar.ChildRoleManifestJson);
            Assert.AreEqual(childBefore, OasisJson.Serialize((await provider.LoadHolonAsync(child.Id, loadChildren: false)).Result));
            var archived = sync ? provider.LoadHolon(owner.Id, loadChildrenFromProvider: true, version: 1)
                : await provider.LoadHolonAsync(owner.Id, loadChildrenFromProvider: true, version: 1);
            Assert.IsFalse(archived.IsError, archived.Message + archived.Exception);
            Assert.AreEqual("private-role-owner", archived.Result.Name); Assert.AreEqual(1, archived.Result.Version);
            Assert.AreEqual("private-member-v2", archived.Result.Children.Single().Name);
            Assert.AreEqual(2, archived.Result.Children.Single().Version, "Version filters roots only; children keep independent latest versions.");
            owner.Components.Clear(); owner.Name = "private-unlinked-owner";
            var unlinked = sync ? provider.SaveHolon(owner) : await provider.SaveHolonAsync(owner);
            Assert.IsFalse(unlinked.IsError, unlinked.Message + unlinked.Exception);
            var bytesBeforeRead = store.Objects.OrderBy(p => p.Key, StringComparer.Ordinal).ToArray();
            var empty = await provider.LoadHolonAsync(owner.Id);
            Assert.IsFalse(empty.IsError, empty.Message + empty.Exception); Assert.AreEqual(0, empty.Result.Children.Count);
            Assert.IsFalse((await provider.LoadHolonAsync(child.Id, loadChildren: false)).IsError);
            CollectionAssert.AreEqual(bytesBeforeRead, store.Objects.OrderBy(p => p.Key, StringComparer.Ordinal).ToArray(), "Reads cannot repair/delete objects.");
            store.AssertPrivate(owner.Id.ToString(), child.Id.ToString(), "private-role-owner", "private-member-v2");
        }
        finally { CryptographicOperations.ZeroMemory(key); }
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task Import_export_parent_lookup_depth_and_null_metadata_use_existing_shared_contract(bool sync)
    {
        using var store = new AwsS3EncryptedLifecycleTests.Store(); var key = RandomNumberGenerator.GetBytes(32);
        try
        {
            using var provider = store.Open(key);
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            var root = new Holon { Id = Guid.NewGuid(), Name = "private-import-root", MetaData = null };
            var child = new Holon { Id = Guid.NewGuid(), Name = "private-import-child", MetaData = new() { ["kind"] = "private-leaf" } };
            var grandchild = new Holon { Id = Guid.NewGuid(), Name = "private-import-grandchild" };
            root.Children.Add(child); child.Children.Add(grandchild);
            var imported = sync ? provider.Import(new[] { root }) : await provider.ImportAsync(new[] { root });
            Assert.IsFalse(imported.IsError, imported.Message + imported.Exception);
            var lookup = sync ? provider.LoadHolon(root.Id.ToString(), maxChildDepth: 1) : await provider.LoadHolonAsync(root.Id.ToString(), maxChildDepth: 1);
            Assert.IsFalse(lookup.IsError, lookup.Message + lookup.Exception);
            Assert.AreEqual(child.Id, lookup.Result.Children.Single().Id); Assert.AreEqual(0, lookup.Result.Children.Single().Children.Count);
            var parent = sync ? provider.LoadHolonsForParent(root.Id, loadChildren: false) : await provider.LoadHolonsForParentAsync(root.Id, loadChildren: false);
            Assert.IsFalse(parent.IsError, parent.Message + parent.Exception); Assert.AreEqual(child.Id, parent.Result.Single().Id);
            var filtered = sync ? provider.LoadHolonsByMetaData("kind", "private-leaf", loadChildren: false)
                : await provider.LoadHolonsByMetaDataAsync("kind", "private-leaf", loadChildren: false);
            Assert.IsFalse(filtered.IsError, filtered.Message + filtered.Exception); Assert.AreEqual(child.Id, filtered.Result.Single().Id);
            var exported = sync ? provider.ExportAll() : await provider.ExportAllAsync();
            Assert.IsFalse(exported.IsError, exported.Message + exported.Exception);
            CollectionAssert.AreEquivalent(new[] { root.Id, child.Id, grandchild.Id }, exported.Result.Select(h => h.Id).ToArray());
            store.AssertPrivate(root.Id.ToString(), child.Id.ToString(), grandchild.Id.ToString(), "private-import-root", "private-leaf");
        }
        finally { CryptographicOperations.ZeroMemory(key); }
    }
}
