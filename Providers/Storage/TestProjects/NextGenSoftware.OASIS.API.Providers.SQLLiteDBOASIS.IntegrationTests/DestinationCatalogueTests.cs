using System.Text.Json;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Theory]
    [InlineData("restart-rebind")]
    [InlineData("second-target-rollback")]
    [InlineData("history-bootstrap")]
    [InlineData("corrupt-record")]
    [InlineData("new-policy")]
    [InlineData("concurrent-first")]
    public async Task NativeDestinationCatalogueOwnsPhysicalMeaningAndSharesThePrimaryTransaction(string scenario)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var captured=await provider.CaptureStorageIdentityAsync();captured.IsError.Should().BeFalse(captured.Message);
        var source=captured.Result;
        var mongo=new CosmicStorageIdentity(ProviderType.MongoDBOASIS,new string('B',64),Guid.NewGuid());
        var neo=new CosmicStorageIdentity(ProviderType.Neo4jOASIS,new string('C',64),"native-declaration-only");
        HolonReplicationRouteBinding Route(CosmicStorageIdentity target,string policy="policy-one")=>new(source,target,policy);
        var first=OutboxCreate(new Holon{Id=Guid.NewGuid(),Name="catalogue-first"});
        var second=OutboxCreate(new Holon{Id=Guid.NewGuid(),Name="catalogue-second"});
        var journal=Path.Combine(Path.GetDirectoryName(_databasePath)!,"scoped-visibility.pending.json");
        await File.WriteAllTextAsync(journal,JsonSerializer.Serialize(new{test="destination-catalogue",scenario,database=_databasePath,
            operations=new[]{first.Write.OperationId,second.Write.OperationId},actors=new[]{first.Write.ActorId,second.Write.ActorId},ids=new[]{first.Roots[0].Id,second.Roots[0].Id}}));
        HolonReplicationEnrollment Enrol(params HolonReplicationRouteBinding[] routes)
            =>new(routes[0].SourceId,routes.Select(route=>route.TargetId),source,routes);
        var initial=scenario=="second-target-rollback" ? Enrol(Route(neo)) : Enrol(Route(mongo));
        if (scenario == "concurrent-first")
        {
            using var independent = new SQLLiteDBOASIS(TestConnectionString);
            (await independent.ActivateProviderAsync()).IsError.Should().BeFalse();
            var independentSource = await independent.CaptureStorageIdentityAsync();
            independentSource.IsError.Should().BeFalse(independentSource.Message);
            independentSource.Result.BindingFingerprint.Should().Be(source.BindingFingerprint);
            var replacement = new CosmicStorageIdentity(mongo.Authority, mongo.EndpointFingerprint, Guid.NewGuid());
            var enrollments = new[] { initial, Enrol(Route(replacement)) };
            var providers = new[] { provider, independent };
            var requests = new[] { first, second };
            var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var arrivals = 0;
            var tasks = Enumerable.Range(0, 2).Select(index => Task.Run(async () => {
                if (Interlocked.Increment(ref arrivals) == 2) ready.TrySetResult();
                await gate.Task.WaitAsync(TimeSpan.FromSeconds(10));
                return await providers[index].ExecuteReplicatingHolonWriteAsync(requests[index], enrollments[index]);
            })).ToArray();
            await ready.Task.WaitAsync(TimeSpan.FromSeconds(10));
            gate.SetResult();
            var outcomes = await Task.WhenAll(tasks);
            var winner = Enumerable.Range(0, 2).Where(index => !outcomes[index].IsError && outcomes[index].IsSaved).Should().ContainSingle().Subject;
            var loser = 1 - winner;
            outcomes[loser].IsError.Should().BeTrue();
            outcomes[loser].IsSaved.Should().BeFalse();
            outcomes[loser].ErrorCode.Should().BeOneOf(HolonDestinationRebindRequiredException.Code, "COSMIC_CONCURRENT_WRITE");
            var raceCatalogue = (IHolonReplicationDestinationCatalogue)provider.GetBoundHolonReplicationOutbox(source).Result;
            var raceApproval = await raceCatalogue.ReadDestinationBindingAsync(mongo.Authority);
            raceApproval.IsError.Should().BeFalse(raceApproval.Message + raceApproval.Exception);
            raceApproval.Result.EstablishedOperationId.Should().Be(requests[winner].Write.OperationId);
            raceApproval.Result.Route.TargetStorage.BindingFingerprint.Should().Be(enrollments[winner].NativeRoutes.Single().TargetStorage.BindingFingerprint);
            (await providers[loser].ExecuteReplicatingHolonWriteAsync(requests[winner], enrollments[winner])).IsError.Should().BeFalse();
            (await providers[loser].ExecuteReplicatingHolonWriteAsync(requests[loser], enrollments[loser])).ErrorCode.Should().Be(HolonDestinationRebindRequiredException.Code);
            using var native = new SqliteConnection(TestConnectionString);
            await native.OpenAsync();
            using var count = native.CreateCommand();
            foreach (var table in new[] { "Holon", "CosmicWriteReceipt", "CosmicHolonOutbox", "CosmicDestinationBinding" })
            {
                count.CommandText = "SELECT COUNT(*) FROM " + table;
                Convert.ToInt64(await count.ExecuteScalarAsync()).Should().Be(1);
            }
            count.CommandText = "SELECT Id FROM Holon";
            ((string)(await count.ExecuteScalarAsync())!).Should().Be(requests[winner].Roots[0].Id.ToString("D"));
            count.CommandText = "SELECT OperationId FROM CosmicWriteReceipt";
            ((string)(await count.ExecuteScalarAsync())!).Should().Be(requests[winner].Write.OperationId.ToString("D"));
            count.CommandText = "SELECT OperationId FROM CosmicHolonOutbox";
            ((string)(await count.ExecuteScalarAsync())!).Should().Be(requests[winner].Write.OperationId.ToString("D"));
            File.Delete(journal);
            return;
        }
        var saved=await provider.ExecuteReplicatingHolonWriteAsync(first,initial);
        saved.IsError.Should().BeFalse(saved.Message+saved.Exception);
        var outbox=provider.GetBoundHolonReplicationOutbox(source).Result;
        var catalogue=(IHolonReplicationDestinationCatalogue)outbox;
        var authority=scenario=="second-target-rollback" ? neo.Authority : mongo.Authority;
        using var observer=new SqliteConnection(TestConnectionString);await observer.OpenAsync();
        using var query=observer.CreateCommand();
        long Count(string table){query.CommandText="SELECT COUNT(*) FROM "+table;return Convert.ToInt64(query.ExecuteScalar());}
        async Task<long> Version(){query.CommandText="PRAGMA data_version";return Convert.ToInt64(await query.ExecuteScalarAsync());}
        var before=await Version();
        var approval=await catalogue.ReadDestinationBindingAsync(authority);
        approval.IsError.Should().BeFalse(approval.Message+approval.Exception);
        approval.Result.EstablishedOperationId.Should().Be(first.Write.OperationId);
        approval.Result.RevisionId.Should().Be(first.Write.OperationId);
        (await Version()).Should().Be(before,"catalogue reads may not create or repair approval records");
        Count("CosmicDestinationBinding").Should().Be(1);

        if(scenario=="restart-rebind")
        {
            using var independent=new SQLLiteDBOASIS(TestConnectionString);
            (await independent.ActivateProviderAsync()).IsError.Should().BeFalse();
            var independentSource=await independent.CaptureStorageIdentityAsync();
            var reread=await ((IHolonReplicationDestinationCatalogue)independent.GetBoundHolonReplicationOutbox(independentSource.Result).Result)
                .ReadDestinationBindingAsync(authority);
            reread.Result.Route.TargetStorage.BindingFingerprint.Should().Be(mongo.BindingFingerprint);
            var replay=await independent.ExecuteReplicatingHolonWriteAsync(first,initial);
            replay.IsError.Should().BeFalse(replay.Message+replay.Exception);
            var replacement=new CosmicStorageIdentity(mongo.Authority,mongo.EndpointFingerprint,Guid.NewGuid());
            var refused=await independent.ExecuteReplicatingHolonWriteAsync(second,Enrol(Route(replacement)));
            refused.ErrorCode.Should().Be(HolonDestinationRebindRequiredException.Code);
            refused.IsSaved.Should().BeFalse();
            Count("Holon").Should().Be(1);Count("CosmicWriteReceipt").Should().Be(1);Count("CosmicHolonOutbox").Should().Be(1);
        }
        else if(scenario=="second-target-rollback")
        {
            var replacement=new CosmicStorageIdentity(neo.Authority,neo.EndpointFingerprint,"different-native-declaration");
            // Mongo sorts before Neo: its new approval is inserted before the
            // second mapping conflicts. Both approvals and data must roll back.
            var refused=await provider.ExecuteReplicatingHolonWriteAsync(second,Enrol(Route(mongo),Route(replacement)));
            refused.ErrorCode.Should().Be(HolonDestinationRebindRequiredException.Code);
            Count("CosmicDestinationBinding").Should().Be(1);
            (await catalogue.ReadDestinationBindingAsync(mongo.Authority)).Result.Should().BeNull();
            Count("Holon").Should().Be(1);Count("CosmicWriteReceipt").Should().Be(1);Count("CosmicHolonOutbox").Should().Be(1);
        }
        else if(scenario=="history-bootstrap")
        {
            // Owned upgrade fault: retain the committed immutable descriptor and
            // receipt,with the new catalogue table still empty after migration.
            query.CommandText="DELETE FROM CosmicDestinationBinding";(await query.ExecuteNonQueryAsync()).Should().Be(1);
            before=await Version();
            var historical=await catalogue.ReadDestinationBindingAsync(authority);
            historical.IsError.Should().BeFalse(historical.Message);
            historical.Result.EstablishedOperationId.Should().Be(first.Write.OperationId);
            Count("CosmicDestinationBinding").Should().Be(0);
            (await Version()).Should().Be(before);
            var written=await provider.ExecuteReplicatingHolonWriteAsync(second,initial);
            written.IsError.Should().BeFalse(written.Message+written.Exception);
            (await catalogue.ReadDestinationBindingAsync(authority)).Result.EstablishedOperationId.Should().Be(first.Write.OperationId);
            Count("CosmicDestinationBinding").Should().Be(1);Count("Holon").Should().Be(2);
        }
        else if(scenario=="corrupt-record")
        {
            query.CommandText="UPDATE CosmicDestinationBinding SET RecordDigest=$bad";
            query.Parameters.AddWithValue("$bad",new string('0',64));(await query.ExecuteNonQueryAsync()).Should().Be(1);query.Parameters.Clear();
            before=await Version();
            (await catalogue.ReadDestinationBindingAsync(authority)).ErrorCode.Should().Be("COSMIC_OUTBOX_CORRUPT");
            (await provider.ExecuteReplicatingHolonWriteAsync(second,initial)).IsError.Should().BeTrue();
            (await Version()).Should().Be(before,"corrupt approval cannot be repaired by history or another write");
            Count("Holon").Should().Be(1);Count("CosmicWriteReceipt").Should().Be(1);Count("CosmicHolonOutbox").Should().Be(1);
        }
        else
        {
            var updated=Enrol(Route(mongo,"policy-two"));
            var written=await provider.ExecuteReplicatingHolonWriteAsync(second,updated);
            written.IsError.Should().BeFalse(written.Message+written.Exception);
            (await catalogue.ReadDestinationBindingAsync(authority)).Result.Route.PolicyBinding.Should().Be("policy-one",
                "establishment provenance is immutable and must not grant current actor authorization");
            query.CommandText="SELECT PayloadJson FROM CosmicHolonOutbox WHERE OperationId=$op";
            query.Parameters.AddWithValue("$op",second.Write.OperationId.ToString("D"));
            CommittedHolonReplicaPayload.Read((string)(await query.ExecuteScalarAsync())!).NativeRoutes.Single().PolicyBinding.Should().Be("policy-two");
            Count("CosmicDestinationBinding").Should().Be(1);Count("Holon").Should().Be(2);
        }
        File.Delete(journal);
    }
}
