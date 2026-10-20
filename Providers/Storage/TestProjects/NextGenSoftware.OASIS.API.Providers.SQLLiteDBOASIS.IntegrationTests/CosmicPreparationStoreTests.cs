using FluentAssertions;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Migrations;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    // Storage conformance uses opaque random tokens, not authenticated Core payloads.
    // The store must not decrypt, compare logical intent or claim authorization.
    private static HolonWritePreparation PreparationCandidate(Guid operation, Guid actor, Guid record,
        ProviderType authority = ProviderType.SQLLiteDBOASIS)
        => new(operation, actor, authority, new[] { new ProtectedHolonPreparation(record,
            "oasis-preparation1:" + Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))) });

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task PreparationClaimReturnsTheOriginalBytesAfterProviderRestart(bool synchronous)
    {
        var op = Guid.NewGuid(); var actor = Guid.NewGuid(); var record = Guid.NewGuid(); var original = PreparationCandidate(op, actor, record);
        using (var provider = new SQLLiteDBOASIS(TestConnectionString))
        {
            (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
            var first = synchronous ? provider.ClaimHolonWritePreparation(original) : await provider.ClaimHolonWritePreparationAsync(original);
            first.IsError.Should().BeFalse(first.Message + first.Exception); first.Result.Records.Single().Envelope.Should().Be(original.Records.Single().Envelope);
        }
        using var restarted = new SQLLiteDBOASIS(TestConnectionString);
        (await restarted.ActivateProviderAsync()).IsError.Should().BeFalse();
        var other = PreparationCandidate(op, actor, Guid.NewGuid());
        var replay = synchronous ? restarted.ClaimHolonWritePreparation(other) : await restarted.ClaimHolonWritePreparationAsync(other);
        replay.IsError.Should().BeFalse(replay.Message + replay.Exception);
        replay.Result.Records.Single().HolonId.Should().Be(record);
        replay.Result.Records.Single().Envelope.Should().Be(original.Records.Single().Envelope);
        using var control = new DataContext(TestConnectionString);
        control.Holons.AsNoTracking().Should().BeEmpty(); // Claiming preparation is not a holon mutation.
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task IndependentPreparationClaimsConvergeOnOneDurableWinner(bool synchronous)
    {
        var op = Guid.NewGuid(); var actor = Guid.NewGuid(); var record = Guid.NewGuid();
        using var first = new SQLLiteDBOASIS(TestConnectionString); using var second = new SQLLiteDBOASIS(TestConnectionString);
        (await first.ActivateProviderAsync()).IsError.Should().BeFalse(); (await second.ActivateProviderAsync()).IsError.Should().BeFalse();
        var a = PreparationCandidate(op, actor, record); var b = PreparationCandidate(op, actor, record);
        using var start = new ManualResetEventSlim();
        async Task<NextGenSoftware.OASIS.Common.OASISResult<HolonWritePreparation>> Run(SQLLiteDBOASIS provider, HolonWritePreparation candidate)
        { start.Wait(); return synchronous ? provider.ClaimHolonWritePreparation(candidate) : await provider.ClaimHolonWritePreparationAsync(candidate); }
        var one = Task.Run(() => Run(first, a)); var two = Task.Run(() => Run(second, b)); start.Set();
        var results = await Task.WhenAll(one, two);
        foreach (var result in results) result.IsError.Should().BeFalse(result.Message + result.Exception);
        var winning = results[0].Result.Records.Single().Envelope;
        new[] { a.Records.Single().Envelope, b.Records.Single().Envelope }.Should().Contain(winning);
        results[1].Result.Records.Single().Envelope.Should().Be(winning);
        using var restarted = new SQLLiteDBOASIS(TestConnectionString); (await restarted.ActivateProviderAsync()).IsError.Should().BeFalse();
        restarted.ClaimHolonWritePreparation(a).Result.Records.Single().Envelope.Should().Be(winning);
    }

    [Fact]
    public async Task PreparationActorAndAuthorityFailuresDoNotDiscloseOrReplaceTheWinner()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString); (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var original = PreparationCandidate(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        provider.ClaimHolonWritePreparation(original).IsError.Should().BeFalse();
        var denied = provider.ClaimHolonWritePreparation(PreparationCandidate(original.OperationId, Guid.NewGuid(), Guid.NewGuid()));
        denied.ErrorCode.Should().Be("COSMIC_PREPARATION_ACTOR_MISMATCH"); denied.Result.Should().BeNull();
        var foreign = provider.ClaimHolonWritePreparation(PreparationCandidate(original.OperationId, original.ActorId, Guid.NewGuid(), ProviderType.MongoDBOASIS));
        foreign.ErrorCode.Should().Be("COSMIC_AUTHORITY_MISMATCH"); foreign.Result.Should().BeNull();
        provider.ClaimHolonWritePreparation(original).Result.Records.Single().Envelope.Should().Be(original.Records.Single().Envelope);
    }

    [Fact]
    public async Task MalformedStoredPreparationFailsWithoutPublishingOrOverwritingIt()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString); (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var original = PreparationCandidate(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        provider.ClaimHolonWritePreparation(original).IsError.Should().BeFalse();
        using var control = new DataContext(TestConnectionString);
        const string corrupt = "[{\"id\":\"bad\",\"id\":\"duplicate\",\"envelope\":\"oasis-preparation1:bad\"}]";
        control.Database.ExecuteSqlInterpolated($"UPDATE CosmicWritePreparation SET PayloadJson={corrupt} WHERE OperationId={original.OperationId.ToString("D")}");
        var read = provider.ClaimHolonWritePreparation(original); read.IsError.Should().BeTrue(); read.Result.Should().BeNull();
        using var connection = new Microsoft.Data.Sqlite.SqliteConnection(TestConnectionString); connection.Open();
        using var command = connection.CreateCommand(); command.CommandText = "SELECT PayloadJson FROM CosmicWritePreparation";
        command.ExecuteScalar().Should().Be(corrupt);
    }

    [Fact]
    public async Task CancelledPreparationDoesNotClaimAnOperation()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString); (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        var candidate = PreparationCandidate(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        Func<Task> claim = async () => await provider.ClaimHolonWritePreparationAsync(candidate, cancelled.Token);
        await claim.Should().ThrowAsync<OperationCanceledException>();
        using var connection = new Microsoft.Data.Sqlite.SqliteConnection(TestConnectionString); connection.Open();
        using var command = connection.CreateCommand(); command.CommandText = "SELECT COUNT(*) FROM CosmicWritePreparation";
        Convert.ToInt64(command.ExecuteScalar()).Should().Be(0);
    }

    [Fact]
    public async Task PreparationMigrationRefusesToDropRetainedClaims()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString); (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var candidate = PreparationCandidate(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        provider.ClaimHolonWritePreparation(candidate).IsError.Should().BeFalse();
        var migration = new AddCosmicWritePreparation();
        var guard = migration.DownOperations.OfType<SqlOperation>().Single().Sql;
        using var control = new DataContext(TestConnectionString);
        Action downgradeGuard = () => control.Database.ExecuteSqlRaw(guard);
        downgradeGuard.Should().Throw<Microsoft.Data.Sqlite.SqliteException>();
        provider.ClaimHolonWritePreparation(candidate).Result.Records.Single().Envelope.Should().Be(candidate.Records.Single().Envelope);
    }
}
