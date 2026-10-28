using System.Text.Json;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Fact]
    public async Task RouteInventoryIncludesUnconfiguredBlockedLeasedDeferredHeadsAndRejectsCorruption()
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var first=OutboxCreate(new Holon{Id=Guid.NewGuid()});
        var later=OutboxCreate(new Holon{Id=Guid.NewGuid()});
        var targets=new[]{"Route-A","route-a","zz-old-1","zz-old-2"};
        var journal=Path.Combine(Path.GetDirectoryName(_databasePath)!,"scoped-visibility.pending.json");
        await File.WriteAllTextAsync(journal,JsonSerializer.Serialize(new{test="route-inventory",operations=new[]{first.Write.OperationId,later.Write.OperationId}}));
        var enrollment=new HolonReplicationEnrollment("inventory-source",targets);
        (await provider.ExecuteReplicatingHolonWriteAsync(first,enrollment)).IsError.Should().BeFalse();
        var source=provider.GetHolonReplicationOutbox().Result;
        var inventory=(IHolonReplicationRouteInventoryOutbox)source;
        async Task<HolonDeliveryClaim> Claim(string target)
        {
            var claim=await source.ClaimNextAsync(target,"owned-inventory",TimeSpan.FromSeconds(30));
            claim.IsError.Should().BeFalse(claim.Message);claim.Result.Should().NotBeNull();return claim.Result;
        }
        var leased=await Claim(targets[0]);
        var blocked=await Claim(targets[1]);
        (await source.FinishWithRecoveryAsync(blocked,HolonDeliveryOutcome.ReconciliationRequired,
            new HolonDeliveryRecoveryEvidence(false,Array.Empty<string>()),"OWNED_ROUTE_REQUIRES_RECONCILIATION")).IsError.Should().BeFalse();
        (await source.FinishAsync(await Claim(targets[2]),HolonDeliveryOutcome.Completed)).IsError.Should().BeFalse();
        (await source.FinishAsync(await Claim(targets[3]),HolonDeliveryOutcome.Retry,errorCode:"OWNED_RETRY_WAIT",nextAttemptUtc:DateTime.UtcNow.AddMinutes(10))).IsError.Should().BeFalse();
        (await provider.ExecuteReplicatingHolonWriteAsync(later,enrollment)).IsError.Should().BeFalse();
        using var observer=new SqliteConnection(TestConnectionString);await observer.OpenAsync();
        using var version=observer.CreateCommand();version.CommandText="PRAGMA data_version";
        var before=Convert.ToInt64(await version.ExecuteScalarAsync());
        var page=await inventory.ReadRouteHeadsAsync(4);
        page.IsError.Should().BeFalse(page.Message+page.Exception);
        page.Result.Heads.Select(head=>head.TargetId).Should().Equal(targets);
        page.Result.NextAfterTargetId.Should().BeNull();
        var heads=page.Result.Heads;
        heads[0].State.Should().Be(HolonDeliveryState.InFlight);heads[0].Sequence.Should().Be(leased.Sequence);
        heads[1].State.Should().Be(HolonDeliveryState.ReconciliationRequired);
        heads[2].OperationId.Should().Be(later.Write.OperationId);heads[2].AttemptCount.Should().Be(0);
        heads[3].OperationId.Should().Be(first.Write.OperationId);heads[3].State.Should().Be(HolonDeliveryState.Pending);
        heads[3].AttemptCount.Should().Be(1);
        var second=await inventory.ReadRouteHeadsAsync(2,targets[1]);
        second.Result.Heads.Select(head=>head.TargetId).Should().Equal(targets.Skip(2));
        Convert.ToInt64(await version.ExecuteScalarAsync()).Should().Be(before,"inventory must not claim or update delivery state");
        using var corrupt=observer.CreateCommand();
        corrupt.CommandText="UPDATE CosmicHolonOutbox SET PayloadDigest='owned-invalid-digest' WHERE OperationId=$operation AND TargetId=$target";
        corrupt.Parameters.AddWithValue("$operation",first.Write.OperationId.ToString("D"));corrupt.Parameters.AddWithValue("$target",targets[3]);
        (await corrupt.ExecuteNonQueryAsync()).Should().Be(1);
        var rejected=await inventory.ReadRouteHeadsAsync(4);
        rejected.IsError.Should().BeTrue();rejected.ErrorCode.Should().Be("COSMIC_OUTBOX_CORRUPT");rejected.Result.Should().BeNull();
        File.Delete(journal); // All mutations and deliberate corruption are known owned fixture data.
    }
}
