using System.Security.Cryptography;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using OASIS.ProviderTests.Fixtures;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    private static CqlRoleOwner OrdinaryRoleGraph()
    {
        var root = new CqlRoleOwner { Id = Guid.NewGuid(), Name = "owned ordinary roles" };
        // Deliberately reverse identity order. Persist collection order, not a DB sort.
        root.Children.Add(new Holon { Id = Guid.Parse("ffffffff-ffff-ffff-ffff-fffffffffffe"), Name = "first" });
        root.Children.Add(new Holon { Id = Guid.Parse("00000000-0000-0000-0000-000000000001"), Name = "second" });
        root.Extras.Add(new CqlRoleItem { Id = Guid.NewGuid(), Label = "extra",
            MetaData = new() { ["Label"] = "extra" } });
        return root;
    }

    private static string ExpectedOrdinaryRoles(CqlRoleOwner root)
        => HolonChildRoleManifestJson.Serialize(new HolonChildRoleManifest(1, root.Id, new[]
        {
            new HolonChildRole("Children", root.Children.Select(child => child.Id).ToArray()),
            new HolonChildRole("Extras", root.Extras.Select(child => child.Id).ToArray())
        }));

    private async Task<(string Path, byte[] Bytes)> BeginOrdinaryRoleIntent(Holon root)
    {
        var path = Path.Combine(Path.GetDirectoryName(_databasePath)!, "scoped-visibility.pending.json");
        var nodes = new List<Core.Interfaces.IHolon> { root };
        for (var index = 0; index < nodes.Count; index++) nodes.AddRange(nodes[index].AllChildren);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(new
        {
            state = "before-owned-ordinary-role-write",
            ids = nodes.Select(child => child.Id).ToArray(),
            expectedRoles = HolonChildRoleManifestJson.Serialize(
                HolonChildCollectionState.Capture(root).CreateManifest(root.Id, child => child.Id))
        });
        await using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write,
            FileShare.None, 4096, FileOptions.WriteThrough);
        await file.WriteAsync(bytes);
        file.Flush(true);
        return (path, bytes);
    }

    private static async Task EndOrdinaryRoleIntent((string Path, byte[] Bytes) intent)
    {
        SHA256.HashData(await File.ReadAllBytesAsync(intent.Path)).Should().Equal(SHA256.HashData(intent.Bytes));
        File.Delete(intent.Path);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OrdinaryRoleNativeLoadGrantsOnlyFullyObservedReplacement(bool partial)
    {
        var root = new Holon { Id = Guid.NewGuid(), Name = "native role read" };
        var child = new Holon { Id = Guid.NewGuid() };
        child.Children.Add(new Holon { Id = Guid.NewGuid() });
        root.Children.Add(child);
        var intent = await BeginOrdinaryRoleIntent(root);
        bool rejected;
        using (var provider = new SQLLiteDBOASIS(TestConnectionString))
        {
            (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
            var initial = await provider.SaveHolonAsync(root);
            initial.IsError.Should().BeFalse(initial.Exception?.ToString());
            var before = await provider.LoadAllHolonsAsync(loadChildren: false);
            before.IsError.Should().BeFalse();
            var snapshots = before.Result.ToDictionary(row => row.Id, row => StoredRecordJson.Serialize(row));
            var loaded = await provider.LoadHolonAsync(root.Id, maxChildDepth: partial ? 1 : 0);
            loaded.IsError.Should().BeFalse(loaded.Exception?.ToString());
            loaded.Result.Name = "edited native role read";
            var saved = await provider.SaveHolonAsync(loaded.Result);
            saved.ErrorCode.Should().NotBe("SQLITE_WRITE_RECONCILIATION_REQUIRED");
            rejected = saved.IsError;
            var after = await provider.LoadAllHolonsAsync(loadChildren: false);
            after.IsError.Should().BeFalse();
            after.Result.Select(row => row.Id).Should().BeEquivalentTo(snapshots.Keys);
            if (rejected)
                foreach (var row in after.Result) StoredRecordJson.Serialize(row).Should().Be(snapshots[row.Id]);
            else
                after.Result.Single(row => row.Id == root.Id).Name.Should().Be("edited native role read");
        }
        await EndOrdinaryRoleIntent(intent);
        rejected.Should().Be(partial, "a depth frontier cannot acquire full observation through loading or saving");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OrdinaryRoleSavePersistsOrderAndRootOnlyWritePreservesStoredMembership(bool synchronous)
    {
        var root = OrdinaryRoleGraph();
        var intent = await BeginOrdinaryRoleIntent(root);
        string? initialRoles;
        string? finalRoles;
        string? acknowledgedRoles;
        using (var provider = new SQLLiteDBOASIS(TestConnectionString))
        {
            (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
            var saved = synchronous ? provider.SaveHolon(root) : await provider.SaveHolonAsync(root);
            saved.IsError.Should().BeFalse(saved.Exception?.ToString());
            saved.IsSaved.Should().BeTrue();
            var initial = await provider.LoadHolonAsync(root.Id, loadChildren: false);
            initial.IsError.Should().BeFalse(initial.Exception?.ToString());
            initialRoles = ((Holon)initial.Result).ChildRoleManifestJson;
            var childrenBefore = new Dictionary<Guid, string>();
            foreach (var child in root.AllChildren)
            {
                var loaded = await provider.LoadHolonAsync(child.Id, loadChildren: false);
                loaded.IsError.Should().BeFalse();
                childrenBefore.Add(child.Id, StoredRecordJson.Serialize(loaded.Result));
            }

            // Root-only replacement cannot clear or forge store-owned membership.
            var replacement = new Holon { Id = root.Id, Name = "root-only edit",
                ChildRoleManifestJson = HolonChildRoleManifestJson.Serialize(new HolonChildRoleManifest(
                    1, root.Id, new[] { new HolonChildRole("Children", Array.Empty<Guid>()) })) };
            var updated = synchronous ? provider.SaveHolon(replacement, saveChildren: false)
                : await provider.SaveHolonAsync(replacement, saveChildren: false);
            updated.IsError.Should().BeFalse(updated.Exception?.ToString());
            updated.IsSaved.Should().BeTrue();
            acknowledgedRoles = ((Holon)updated.Result).ChildRoleManifestJson;
            var current = await provider.LoadHolonAsync(root.Id, loadChildren: false);
            current.IsError.Should().BeFalse();
            finalRoles = ((Holon)current.Result).ChildRoleManifestJson;
            foreach (var child in root.AllChildren)
            {
                var loaded = await provider.LoadHolonAsync(child.Id, loadChildren: false);
                loaded.IsError.Should().BeFalse();
                StoredRecordJson.Serialize(loaded.Result).Should().Be(childrenBefore[child.Id]);
            }
            var all = await provider.LoadAllHolonsAsync(loadChildren: false);
            all.IsError.Should().BeFalse();
            all.Result.Select(row => row.Id).Should().BeEquivalentTo(new[] { root.Id }.Concat(root.AllChildren.Select(child => child.Id)));
        }
        // Both writes acknowledged and native IDs/children are verified. Retire
        // only this owned intent before reporting a semantic assertion failure.
        await EndOrdinaryRoleIntent(intent);
        var expected = ExpectedOrdinaryRoles(root);
        initialRoles.Should().Be(expected, "the write owner captures both collection roles and their order");
        finalRoles.Should().Be(expected, "a root-only write preserves the protected stored observation");
        acknowledgedRoles.Should().Be(expected, "the acknowledgement describes committed membership");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OrdinaryRoleSavePublishesObservationOnlyAfterAcknowledgement(bool synchronous)
    {
        var root = OrdinaryRoleGraph();
        var intent = await BeginOrdinaryRoleIntent(root);
        using (var provider = new SQLLiteDBOASIS(TestConnectionString))
        {
            (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
            var saved = synchronous ? provider.SaveHolon(root) : await provider.SaveHolonAsync(root);
            saved.IsError.Should().BeFalse(saved.Exception?.ToString());
            saved.IsSaved.Should().BeTrue();
            var all = await provider.LoadAllHolonsAsync(loadChildren: false);
            all.IsError.Should().BeFalse();
            all.Result.Select(row => row.Id).Should().BeEquivalentTo(new[] { root.Id }.Concat(root.AllChildren.Select(child => child.Id)));
        }
        await EndOrdinaryRoleIntent(intent);
        root.ChildRoleManifestJson.Should().Be(ExpectedOrdinaryRoles(root));
        Action validate = () => HolonChangeTracking.ValidateChildSelectionForSave(root);
        validate.Should().NotThrow("only the acknowledged complete graph grants future selection ownership");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OrdinaryRoleRollbackDoesNotPublishObservationOrParentEdges(bool synchronous)
    {
        var root = OrdinaryRoleGraph();
        var intent = await BeginOrdinaryRoleIntent(root);
        using (var provider = new SQLLiteDBOASIS(TestConnectionString))
        {
            (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
            using var control = new DataContext(TestConnectionString);
            control.Database.ExecuteSqlRaw("CREATE TRIGGER RejectOwnedRole BEFORE INSERT ON Holon BEGIN SELECT RAISE(ABORT, 'owned role rejection'); END;");
            var saved = synchronous ? provider.SaveHolon(root) : await provider.SaveHolonAsync(root);
            saved.IsError.Should().BeTrue();
            saved.IsSaved.Should().BeFalse();
            saved.ErrorCode.Should().NotBe("SQLITE_WRITE_RECONCILIATION_REQUIRED");
            var all = await provider.LoadAllHolonsAsync(loadChildren: false);
            all.IsError.Should().BeFalse();
            all.Result.Should().BeEmpty();
        }
        await EndOrdinaryRoleIntent(intent);
        root.ChildRoleManifestJson.Should().BeNull();
        root.AllChildren.Select(child => child.ParentHolonId).Should().OnlyContain(id => id == Guid.Empty);
    }
}
