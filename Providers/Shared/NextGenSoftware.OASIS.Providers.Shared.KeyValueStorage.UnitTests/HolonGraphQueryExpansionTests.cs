using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Serialization;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class HolonGraphQueryExpansionTests
{
    private static void Manifest(Holon owner, params Guid[] children)
        => owner.ChildRoleManifestJson = HolonChildRoleManifestJson.Serialize(new HolonChildRoleManifest(
            1, owner.Id, new[] { new HolonChildRole("Children", children) }));

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task AccessBoundAssemblyRejectsForeignNodesWithoutFilteringThem(bool foreignRoot)
    {
        var actor = Guid.NewGuid();
        var policy = CosmicQueryReadAccess.OwnerOrPublic(actor).Result;
        var root = new Holon { Id = Guid.NewGuid(), IsPublic = !foreignRoot };
        var child = new Holon { Id = Guid.NewGuid(), ParentHolonId = root.Id };
        Manifest(root, child.Id);
        var reads = 0;
        var result = await HolonGraphQueryExpansion.ExpandAsync(new[] { root },
            CosmicQueryGraphOptions.Create().Result, policy, (selection, _) =>
            {
                reads++;
                Assert.AreSame(policy, selection.ReadAccess);
                return Task.FromResult<IReadOnlyList<IHolon>>(new[] { child });
            });
        Assert.IsTrue(result.IsError);
        Assert.AreEqual("CQL_VISIBILITY_VIOLATION", result.ErrorCode);
        Assert.IsNull(result.Result);
        Assert.AreEqual(foreignRoot ? 0 : 1, reads);
    }

    [TestMethod]
    public async Task AccessBoundAssemblyKeepsHiddenMembershipButBudgetsOnlySelectedNodes()
    {
        var actor = Guid.NewGuid();
        var policy = CosmicQueryReadAccess.OwnerOrPublic(actor).Result;
        var root = new Holon { Id = Guid.NewGuid(), IsPublic = true };
        var child = new Holon { Id = Guid.NewGuid(), ParentHolonId = root.Id, CreatedByAvatarId = actor };
        Manifest(root, Guid.NewGuid(), child.Id, Guid.NewGuid());
        Manifest(child);
        var observation = root.ChildRoleManifestJson;
        var result = await HolonGraphQueryExpansion.ExpandAsync(new[] { root },
            CosmicQueryGraphOptions.Create(maximumNodes: 2).Result, policy, (selection, _) =>
            {
                Assert.AreSame(policy, selection.ReadAccess);
                Assert.AreEqual(3, selection.MemberIds.Count);
                Assert.AreEqual(1, selection.MaximumNodes);
                return Task.FromResult<IReadOnlyList<IHolon>>(new[] { child });
            });
        Assert.IsFalse(result.IsError, result.Message);
        Assert.AreEqual(observation, root.ChildRoleManifestJson);
        CollectionAssert.AreEqual(new[] { child.Id }, root.Children.Select(value => value.Id).ToArray());
    }

    [TestMethod]
    public async Task AccessBoundOverloadCannotSilentlyBecomeUnrestricted()
    {
        var result = await HolonGraphQueryExpansion.ExpandAsync(Array.Empty<IHolon>(),
            CosmicQueryGraphOptions.Create().Result, null,
            (_, _) => throw new InvalidOperationException("No native read is permitted."));
        Assert.IsTrue(result.IsError);
        Assert.AreEqual("CQL_ACCESS_INVALID", result.ErrorCode);
    }

    [TestMethod]
    public async Task NativeResultsAreReassembledInPersistedRoleOrder()
    {
        var root = new Holon { Id = Guid.NewGuid() };
        var first = new Holon { Id = Guid.NewGuid(), ParentHolonId = root.Id };
        var second = new Holon { Id = Guid.NewGuid(), ParentHolonId = root.Id };
        Manifest(root, first.Id, second.Id);
        Manifest(first); Manifest(second);
        var reads = 0;
        var result = await HolonGraphQueryExpansion.ExpandAsync(new[] { root }, CosmicQueryGraphOptions.Create().Result,
            (selection, _) =>
            {
                reads++;
                CollectionAssert.AreEqual(new[] { first.Id, second.Id }, selection.MemberIds.ToArray());
                return Task.FromResult<IReadOnlyList<IHolon>>(new[] { second, first });
            });

        Assert.IsFalse(result.IsError, result.Message);
        Assert.AreEqual(1, reads);
        CollectionAssert.AreEqual(new[] { first.Id, second.Id }, result.Result.Single().Children.Select(child => child.Id).ToArray());
    }

    [TestMethod]
    public async Task PendingRecordsConsumePayloadBeforeAnotherNativeRead()
    {
        var first = new Holon { Id = Guid.NewGuid() };
        var second = new Holon { Id = Guid.NewGuid() };
        var rootBytes = Encoding.UTF8.GetByteCount(StoredRecordJson.Serialize(first)) +
            Encoding.UTF8.GetByteCount(StoredRecordJson.Serialize(second));
        var reads = 0;
        var result = await HolonGraphQueryExpansion.ExpandAsync(new[] { first, second },
            CosmicQueryGraphOptions.Create(maximumPayloadBytes: rootBytes + 256).Result,
            (selection, _) =>
            {
                reads++;
                return Task.FromResult<IReadOnlyList<IHolon>>(new[] {
                    new Holon { Id = Guid.NewGuid(), ParentHolonId = selection.ParentId, Description = new string('x', 2048) } });
            });

        Assert.IsTrue(result.IsError);
        Assert.AreEqual("CQL_QUERY_LIMIT", result.ErrorCode);
        Assert.IsNull(result.Result);
        Assert.AreEqual(1, reads);
    }

    [TestMethod]
    public async Task AnOversizeKnownMembershipRejectsBeforeNativeIo()
    {
        var root = new Holon { Id = Guid.NewGuid() };
        Manifest(root, Guid.NewGuid(), Guid.NewGuid());
        var result = await HolonGraphQueryExpansion.ExpandAsync(new[] { root },
            CosmicQueryGraphOptions.Create(maximumNodes: 2).Result,
            (_, _) => throw new InvalidOperationException("The known node budget must reject before child I/O."));

        Assert.IsTrue(result.IsError);
        Assert.AreEqual("CQL_QUERY_LIMIT", result.ErrorCode);
    }

    [DataTestMethod]
    [DataRow(0)] [DataRow(1)] [DataRow(2)]
    public async Task InvalidNativeEdgesNeverPublishAnEarlierRoot(int fault)
    {
        var root = new Holon { Id = Guid.NewGuid() };
        var child = new Holon { Id = Guid.NewGuid(), ParentHolonId = fault == 0 ? Guid.NewGuid() : root.Id };
        if (fault == 2) { root.ParentHolonId = root.Id; child = root; }
        var selected = fault == 1 ? new[] { child, child } : new[] { child };
        var result = await HolonGraphQueryExpansion.ExpandAsync(new[] { root },
            CosmicQueryGraphOptions.Create(maxChildDepth: 1).Result,
            (_, _) => Task.FromResult<IReadOnlyList<IHolon>>(selected));

        Assert.IsTrue(result.IsError);
        Assert.AreEqual("CQL_DATA_INVALID", result.ErrorCode);
        Assert.IsNull(result.Result);
    }

    [TestMethod]
    public async Task NativeReadCancellationDoesNotBecomeAPartialGraph()
    {
        var root = new Holon { Id = Guid.NewGuid() };
        using var cancellation = new CancellationTokenSource();
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(async () =>
            await HolonGraphQueryExpansion.ExpandAsync(new[] { root }, CosmicQueryGraphOptions.Create().Result,
                (_, _) => { cancellation.Cancel(); return Task.FromResult<IReadOnlyList<IHolon>>(Array.Empty<IHolon>()); },
                cancellation.Token));
    }

    [TestMethod]
    public async Task FilteredMembershipRetainsTheCompleteStoreObservation()
    {
        var root = new Holon { Id = Guid.NewGuid() };
        var quest = new Holon { Id = Guid.NewGuid(), ParentHolonId = root.Id, HolonType = HolonType.Quest };
        var hidden = Guid.NewGuid();
        Manifest(root, quest.Id, hidden);
        var before = root.ChildRoleManifestJson;
        var result = await HolonGraphQueryExpansion.ExpandAsync(new[] { root },
            CosmicQueryGraphOptions.Create(childType: HolonType.Quest, maxChildDepth: 1).Result,
            (selection, _) =>
            {
                Assert.AreEqual(2, selection.MemberIds.Count);
                return Task.FromResult<IReadOnlyList<IHolon>>(new[] { quest });
            });

        Assert.IsFalse(result.IsError, result.Message);
        Assert.AreEqual(before, ((Holon)result.Result.Single()).ChildRoleManifestJson);
        Assert.AreEqual(quest.Id, result.Result.Single().Children.Single().Id);
    }
}
