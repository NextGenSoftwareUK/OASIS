using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Enums;
using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Fact]
    public async Task LegacyDatabaseUpgradePreservesLogicalLineageAndUsesARealEmptyTokenForExistingRows()
    {
        var logical = Guid.NewGuid(); var previous = Guid.NewGuid(); var id = Guid.NewGuid();
        using (var original = new SQLLiteDBOASIS(TestConnectionString))
        {
            (await original.ActivateProviderAsync()).IsError.Should().BeFalse();
            (await original.SaveHolonAsync(new Holon { Id = id, Name = "owned pre-token legacy row", Version = 3,
                VersionId = logical, PreviousVersionId = previous }, saveChildren: false)).IsError.Should().BeFalse();
        }
        using (var legacy = new DataContext(TestConnectionString))
            legacy.Database.ExecuteSqlRaw("ALTER TABLE Holon DROP COLUMN ProviderRevisionId");
        using var upgraded = new SQLLiteDBOASIS(TestConnectionString);
        (await upgraded.ActivateProviderAsync()).IsError.Should().BeFalse();
        var read = await upgraded.LoadHolonAsync(id, loadChildren: false);
        read.IsError.Should().BeFalse(read.Message + read.Exception);
        read.Result.VersionId.Should().Be(logical); read.Result.PreviousVersionId.Should().Be(previous);
        Revision(read.Result).Should().Be(Guid.Empty);
        upgraded.ExecuteConditionalHolonWrite(ConditionalRequest(read.Result, null)).ErrorCode.Should().Be(CosmicConcurrencyException.Code);
        var saved = upgraded.ExecuteConditionalHolonWrite(ConditionalRequest(read.Result, Guid.Empty));
        saved.IsError.Should().BeFalse(saved.Message + saved.Exception);
        saved.Result.Single().PreviousVersionId.Should().Be(logical); saved.Result.Single().Version.Should().Be(4);
        Revision(saved.Result.Single()).Should().NotBeEmpty();
    }

    [Fact]
    public async Task RawWritesPreserveLogicalLineageButAlwaysInvalidateTheLocalConcurrencyToken()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var logical = Guid.NewGuid(); var previousLogical = Guid.NewGuid();
        var submitted = new Holon { Id = Guid.NewGuid(), Version = 7, VersionId = logical,
            PreviousVersionId = previousLogical, Name = "logical content" };
        var created = await provider.SaveHolonAsync(submitted, saveChildren: false);
        created.IsError.Should().BeFalse(created.Message + created.Exception);
        created.Result.VersionId.Should().Be(logical); created.Result.PreviousVersionId.Should().Be(previousLogical);
        var firstToken = Revision(created.Result); firstToken.Should().NotBeEmpty(); firstToken.Should().NotBe(logical);
        created.Result.Name = "raw replacement with same logical identity";
        CosmicConcurrency.SetProviderRevisionId(created.Result, ProviderType.SQLLiteDBOASIS, firstToken);
        var replacement = await provider.SaveHolonAsync(created.Result, saveChildren: false);
        replacement.IsError.Should().BeFalse(replacement.Message + replacement.Exception);
        replacement.Result.VersionId.Should().Be(logical); replacement.Result.PreviousVersionId.Should().Be(previousLogical);
        Revision(replacement.Result).Should().NotBe(firstToken);
        var stale = provider.ExecuteConditionalHolonWrite(ConditionalRequest(submitted, firstToken));
        stale.ErrorCode.Should().Be(CosmicConcurrencyException.Code);
        var conflict = ((CosmicConcurrencyException)stale.Exception).Conflicts.Single();
        conflict.ActualRevisionId.Should().Be(Revision(replacement.Result)); conflict.ActualVersionId.Should().Be(logical);
        provider.ExecuteConditionalHolonWrite(ConditionalRequest(submitted, logical)).ErrorCode.Should().Be(CosmicConcurrencyException.Code);
        var accepted = provider.ExecuteConditionalHolonWrite(ConditionalRequest(submitted, Revision(replacement.Result)));
        accepted.IsError.Should().BeFalse(accepted.Message + accepted.Exception);
        accepted.Result.Single().Version.Should().Be(8); accepted.Result.Single().PreviousVersionId.Should().Be(logical);
    }

    [Fact]
    public async Task AConditionalRequestCannotBeAppliedToAnotherAuthority()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var root = new Holon { Id = Guid.NewGuid() };
        var foreign = new ConditionalHolonWriteRequest(new ConditionalHolonWrite(Guid.NewGuid(), Guid.NewGuid(),
            ProviderType.MongoDBOASIS, new[] { new HolonWriteCondition(root.Id, null) }), ConditionalHolonOperation.Save, new[] { root });
        provider.ExecuteConditionalHolonWrite(foreign).ErrorCode.Should().Be("COSMIC_AUTHORITY_MISMATCH");
        using var control = new DataContext(TestConnectionString);
        control.Holons.AsNoTracking().Should().BeEmpty();
    }

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)] [InlineData(true, false)] [InlineData(true, true)]
    public async Task ConditionalBatchPublishesSelectedGraphAndNextSaveIsNoOp(bool v2, bool synchronous)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var manager = CosmicManager(provider, v2); var actor = Guid.NewGuid();
        var child = new Holon { Name = "conditional batch child" };
        var first = new Holon { Name = "first", Children = new List<IHolon> { child } };
        var second = new Holon { Name = "second" }; var roots = new[] { first, second };
        var saved = synchronous
            ? manager.SaveHolons(roots, actor, new CosmicWriteOptions(Guid.NewGuid()))
            : await manager.SaveHolonsAsync(roots, actor, new CosmicWriteOptions(Guid.NewGuid()));
        saved.IsError.Should().BeFalse(saved.Message + saved.Exception);
        first.Id.Should().NotBeEmpty(); second.Id.Should().NotBeEmpty(); child.Id.Should().NotBeEmpty();
        first.Version.Should().Be(1); second.Version.Should().Be(1); child.Version.Should().Be(1);
        first.HasHolonChanged().Should().BeFalse(); second.HasHolonChanged().Should().BeFalse();
        var clean = await manager.SaveHolonsAsync(roots, actor, new CosmicWriteOptions(Guid.NewGuid()));
        clean.IsError.Should().BeFalse(clean.Message); clean.IsSaved.Should().BeFalse();
        using var control = new DataContext(TestConnectionString);
        control.Holons.AsNoTracking().Count().Should().Be(3);
    }

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)] [InlineData(true, false)] [InlineData(true, true)]
    public async Task ConditionalDeleteRejectsStaleClientAndPersistsReplayableTombstone(bool softDelete, bool synchronous)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var manager = CosmicManager(provider, true); var actor = Guid.NewGuid();
        var root = new Holon { Name = "before deletion" };
        (await manager.SaveHolonAsync(root, actor, new CosmicWriteOptions(Guid.NewGuid()))).IsError.Should().BeFalse();
        var stale = (await manager.LoadHolonAsync(root.Id)).Result;
        root.Name = "newer edit";
        (await manager.SaveHolonAsync(root, actor, new CosmicWriteOptions(Guid.NewGuid()))).IsError.Should().BeFalse();
        var rejected = synchronous
            ? manager.DeleteHolon(stale, actor, new CosmicWriteOptions(Guid.NewGuid()), softDelete)
            : await manager.DeleteHolonAsync(stale, actor, new CosmicWriteOptions(Guid.NewGuid()), softDelete);
        rejected.ErrorCode.Should().Be(CosmicConcurrencyException.Code);
        var providerRevision = Revision(root);
        var revision = root.VersionId; var operation = Guid.NewGuid();
        var beforeDelete = StoredRecordJson.Deserialize<Holon>(StoredRecordJson.Serialize<IHolon>(root));
        var deleted = synchronous
            ? manager.DeleteHolon(root, actor, new CosmicWriteOptions(operation), softDelete)
            : await manager.DeleteHolonAsync(root, actor, new CosmicWriteOptions(operation), softDelete);
        deleted.IsError.Should().BeFalse(deleted.Message + deleted.Exception);
        deleted.IsDeleted.Should().BeTrue(); deleted.IsSaved.Should().BeFalse();
        root.DeletedByAvatarId.Should().Be(actor); root.DeletedDate.Kind.Should().Be(DateTimeKind.Utc);
        root.Version.Should().Be(3); root.PreviousVersionId.Should().Be(revision);
        root.VersionId.Should().NotBe(revision);
        using var control = new DataContext(TestConnectionString);
        control.Holons.AsNoTracking().Count().Should().Be(softDelete ? 1 : 0);
        var replayRequest = new ConditionalHolonWriteRequest(new ConditionalHolonWrite(operation, actor, ProviderType.SQLLiteDBOASIS,
            new[] { new HolonWriteCondition(root.Id, providerRevision) }),
            softDelete ? ConditionalHolonOperation.SoftDelete : ConditionalHolonOperation.HardDelete,
            new[] { beforeDelete }, false, false);
        // Replay uses the exact submitted record; the receipt is bound to intent and preconditions.
        // Retain the pre-delete projection explicitly in stateless callers.
        var replay = provider.ExecuteConditionalHolonWrite(replayRequest);
        replay.IsError.Should().BeFalse(replay.Message + replay.Exception);
        replay.IsDeleted.Should().BeTrue(); replay.Result.Single().VersionId.Should().Be(root.VersionId);
    }

    [Fact]
    public async Task SimultaneousNativeWritersProduceExactlyOneWinner()
    {
        using var first = new SQLLiteDBOASIS(TestConnectionString);
        using var second = new SQLLiteDBOASIS(TestConnectionString);
        (await first.ActivateProviderAsync()).IsError.Should().BeFalse();
        (await second.ActivateProviderAsync()).IsError.Should().BeFalse();
        var id = Guid.NewGuid();
        var seed = first.ExecuteConditionalHolonWrite(ConditionalRequest(new Holon { Id = id }, null));
        seed.IsError.Should().BeFalse(seed.Message + seed.Exception);
        var revision = Revision(seed.Result.Single());
        using var barrier = new Barrier(2);
        Task<NextGenSoftware.OASIS.Common.OASISResult<IEnumerable<IHolon>>> Write(SQLLiteDBOASIS provider, string name)
            => Task.Run(() => {
                if (!barrier.SignalAndWait(TimeSpan.FromSeconds(10))) throw new TimeoutException("Writer coordination failed.");
                return provider.ExecuteConditionalHolonWrite(ConditionalRequest(new Holon { Id = id, Name = name }, revision));
            });
        var outcomes = await Task.WhenAll(Write(first, "first"), Write(second, "second"));
        outcomes.Count(result => !result.IsError).Should().Be(1);
        outcomes.Count(result => result.ErrorCode == CosmicConcurrencyException.Code).Should().Be(1);
        (await first.LoadHolonAsync(id)).Result.Version.Should().Be(2);
    }

    [Fact]
    public async Task StaleChildConditionRejectsTheEntireGraphBeforeAnyRootUpdate()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var child = new Holon { Id = Guid.NewGuid(), Name = "original-child" };
        var root = new Holon { Id = Guid.NewGuid(), Name = "original-root", Children = new List<IHolon> { child } };
        var created = provider.ExecuteConditionalHolonWrite(ConditionalRequest(root, null, conditions: new[] {
            new HolonWriteCondition(root.Id, null), new HolonWriteCondition(child.Id, null) }));
        created.IsError.Should().BeFalse(created.Message + created.Exception);
        var storedRoot = created.Result.Single(); var storedChild = storedRoot.Children.Single();
        var rootLogicalVersion = storedRoot.VersionId;
        var rootRevision = Revision(storedRoot); var childRevision = Revision(storedChild);
        storedChild.Name = "child-winner";
        provider.ExecuteConditionalHolonWrite(ConditionalRequest(storedChild, childRevision)).IsError.Should().BeFalse();
        storedRoot.Name = "root-must-not-commit";
        var rejected = provider.ExecuteConditionalHolonWrite(ConditionalRequest(storedRoot, rootRevision, conditions: new[] {
            new HolonWriteCondition(root.Id, rootRevision), new HolonWriteCondition(child.Id, childRevision) }));
        rejected.ErrorCode.Should().Be(CosmicConcurrencyException.Code);
        var loaded = (await provider.LoadHolonAsync(root.Id)).Result;
        loaded.Name.Should().Be("original-root"); loaded.VersionId.Should().Be(rootLogicalVersion);
        loaded.Children.Single().Name.Should().Be("child-winner");
    }

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)] [InlineData(true, false)] [InlineData(true, true)]
    public async Task ManagerConditionalSavePublishesOnlyWinnerAndRetainsLoserEdits(bool v2, bool synchronous)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        using var other = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        (await other.ActivateProviderAsync()).IsError.Should().BeFalse();
        var manager = CosmicManager(provider, v2); var second = CosmicManager(other, v2);
        var root = new Holon { Name = "created by conditional manager" }; var actor = Guid.NewGuid();
        var created = synchronous
            ? manager.SaveHolon(root, actor, new CosmicWriteOptions(Guid.NewGuid()))
            : await manager.SaveHolonAsync(root, actor, new CosmicWriteOptions(Guid.NewGuid()));
        created.IsError.Should().BeFalse(created.Message + created.Exception);
        root.Id.Should().NotBeEmpty(); root.Version.Should().Be(1); root.HasHolonChanged().Should().BeFalse();
        var loaded = await second.LoadHolonAsync(root.Id, providerType: NextGenSoftware.OASIS.API.Core.Enums.ProviderType.SQLLiteDBOASIS);
        loaded.IsError.Should().BeFalse(loaded.Message + loaded.Exception);
        var staleRevision = loaded.Result.VersionId;
        root.Name = "winning edit"; loaded.Result.Name = "pending losing edit";
        var winner = synchronous
            ? manager.SaveHolon(root, actor, new CosmicWriteOptions(Guid.NewGuid()))
            : await manager.SaveHolonAsync(root, actor, new CosmicWriteOptions(Guid.NewGuid()));
        winner.IsError.Should().BeFalse(winner.Message + winner.Exception);
        var loser = synchronous
            ? second.SaveHolon(loaded.Result, actor, new CosmicWriteOptions(Guid.NewGuid()))
            : await second.SaveHolonAsync(loaded.Result, actor, new CosmicWriteOptions(Guid.NewGuid()));
        loser.ErrorCode.Should().Be(CosmicConcurrencyException.Code);
        loaded.Result.Name.Should().Be("pending losing edit");
        loaded.Result.VersionId.Should().Be(staleRevision); loaded.Result.HasHolonChanged().Should().BeTrue();
    }

    private static Guid Revision(IHolon holon) => CosmicConcurrency.GetProviderRevisionId(holon, ProviderType.SQLLiteDBOASIS)
        ?? throw new InvalidOperationException("The SQLite acknowledgement must include its provider revision.");

    private static ConditionalHolonWriteRequest ConditionalRequest(IHolon root, Guid? expected, Guid? operation = null,
        Guid? actor = null, IEnumerable<HolonWriteCondition> conditions = null)
        => new(new ConditionalHolonWrite(operation ?? Guid.NewGuid(), actor ?? Guid.Empty, ProviderType.SQLLiteDBOASIS,
            conditions ?? new[] { new HolonWriteCondition(root.Id, expected) }), ConditionalHolonOperation.Save, new[] { root });

    [Fact]
    public async Task IndependentClientsRejectStaleRevisionAndReplayOriginalReceiptAcrossRestart()
    {
        var id = Guid.NewGuid(); var op = Guid.NewGuid(); var actor = Guid.NewGuid();
        var original = new Holon { Id = id, Name = "create" };
        Guid firstRevision; Guid firstLogicalVersion;
        using (var first = new SQLLiteDBOASIS(TestConnectionString))
        using (var second = new SQLLiteDBOASIS(TestConnectionString))
        {
            (await first.ActivateProviderAsync()).IsError.Should().BeFalse();
            (await second.ActivateProviderAsync()).IsError.Should().BeFalse();
            var created = first.ExecuteConditionalHolonWrite(ConditionalRequest(original, null, op, actor));
            created.IsError.Should().BeFalse(created.Message + created.Exception);
            firstRevision = Revision(created.Result.Single());
            firstLogicalVersion = created.Result.Single().VersionId;
            original.VersionId.Should().BeEmpty("provider acknowledgements do not publish into caller instances");
            var a = (await first.LoadHolonAsync(id)).Result;
            var b = (await second.LoadHolonAsync(id)).Result;
            a.Name = "winner"; b.Name = "loser";
            var updated = first.ExecuteConditionalHolonWrite(ConditionalRequest(a, firstRevision));
            updated.IsError.Should().BeFalse(updated.Message + updated.Exception);
            updated.Result.Single().Version.Should().Be(2);
            var stale = await second.ExecuteConditionalHolonWriteAsync(ConditionalRequest(b, firstRevision));
            stale.ErrorCode.Should().Be(CosmicConcurrencyException.Code);
            stale.Exception.Should().BeOfType<CosmicConcurrencyException>().Which.Conflicts.Should().ContainSingle();
        }
        using var restarted = new SQLLiteDBOASIS(TestConnectionString);
        (await restarted.ActivateProviderAsync()).IsError.Should().BeFalse();
        var replay = restarted.ExecuteConditionalHolonWrite(ConditionalRequest(original, null, op, actor));
        replay.IsError.Should().BeFalse(replay.Message + replay.Exception);
        Revision(replay.Result.Single()).Should().Be(firstRevision);
        replay.Result.Single().VersionId.Should().Be(firstLogicalVersion);
        replay.Result.Single().Name.Should().Be("create");
        (await restarted.LoadHolonAsync(id)).Result.Name.Should().Be("winner");
        original.Name = "different intent";
        restarted.ExecuteConditionalHolonWrite(ConditionalRequest(original, null, op, actor))
            .ErrorCode.Should().Be("COSMIC_OPERATION_ID_REUSED");
    }

    [Fact]
    public async Task NativeChildFailureRollsBackRecordsAndReceiptAndAllowsExactRetry()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        using var control = new DataContext(TestConnectionString);
        control.Database.ExecuteSqlRaw("CREATE TRIGGER reject_conditional_child BEFORE INSERT ON Holon WHEN NEW.Name = 'reject-conditional' BEGIN SELECT RAISE(ABORT, 'controlled failure'); END;");
        var child = new Holon { Id = Guid.NewGuid(), Name = "reject-conditional" };
        var root = new Holon { Id = Guid.NewGuid(), Children = new List<IHolon> { child } };
        var request = ConditionalRequest(root, null, conditions: new[] {
            new HolonWriteCondition(root.Id, null), new HolonWriteCondition(child.Id, null) });
        var rejected = provider.ExecuteConditionalHolonWrite(request);
        rejected.ErrorCode.Should().Be("COSMIC_CONDITIONAL_WRITE_FAILED");
        control.Holons.AsNoTracking().Should().BeEmpty();
        control.Database.ExecuteSqlRaw("DROP TRIGGER reject_conditional_child");
        var retry = provider.ExecuteConditionalHolonWrite(request);
        retry.IsError.Should().BeFalse(retry.Message + retry.Exception);
        retry.Result.Single().Children.Should().ContainSingle();
        control.Holons.AsNoTracking().Count().Should().Be(2);
    }

    [Fact]
    public async Task RawProviderReplacementInvalidatesConditionalRevision()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var root = new Holon { Id = Guid.NewGuid(), Name = "first" };
        var created = provider.ExecuteConditionalHolonWrite(ConditionalRequest(root, null));
        created.IsError.Should().BeFalse(created.Message + created.Exception);
        var stored = created.Result.Single(); var revision = Revision(stored);
        stored.Name = "raw replacement";
        var raw = await provider.SaveHolonAsync(stored);
        raw.IsError.Should().BeFalse(raw.Message + raw.Exception);
        provider.ExecuteConditionalHolonWrite(ConditionalRequest(root, revision))
            .ErrorCode.Should().Be(CosmicConcurrencyException.Code);
    }
}
