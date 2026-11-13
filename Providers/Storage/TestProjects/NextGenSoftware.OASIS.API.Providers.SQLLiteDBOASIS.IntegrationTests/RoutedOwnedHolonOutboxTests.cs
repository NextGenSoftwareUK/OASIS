using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    // Only the authority is native in this test. Targets advertise the receiver
    // contract, but must never receive an inline call before durable delivery.
    public class RoutedReplicaDeclaration : DispatchProxy, IBoundCommittedHolonReplicaStorageProvider, ICosmicStorageIdentityProvider
    {
        public ProviderType Identity;

        protected override object? Invoke(MethodInfo? method, object?[]? args) => method!.Name switch
        {
            "get_IsProviderActivated" => true,
            "get_ProviderType" => new EnumValue<ProviderType>(Identity),
            "get_ProviderCategory" => new EnumValue<ProviderCategory>(ProviderCategory.Storage),
            "get_ProviderName" => "declaration-only-" + Identity,
            _ => throw new InvalidOperationException("A declaration-only target must not be invoked: " + method.Name)
        };

        // A declared native identity tests enrollment only; no target delivery is claimed here.
        public Task<OASISResult<CosmicStorageIdentity>> CaptureStorageIdentityAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new OASISResult<CosmicStorageIdentity>(RoutedTargetIdentity(Identity)));
        public OASISResult<IEnumerable<IHolon>> ApplyBoundCommittedHolonReplica(CommittedHolonReplicaRequest request, CosmicStorageIdentity expectedStorage)
            => throw new InvalidOperationException("Primary writes cannot deliver inline.");
        public Task<OASISResult<IEnumerable<IHolon>>> ApplyBoundCommittedHolonReplicaAsync(CommittedHolonReplicaRequest request, CosmicStorageIdentity expectedStorage, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Primary writes cannot deliver inline.");
        public OASISResult<IEnumerable<IHolon>> ApplyCommittedHolonReplica(CommittedHolonReplicaRequest request)
            => throw new InvalidOperationException("Delivery is not inline with the primary transaction.");

        public Task<OASISResult<IEnumerable<IHolon>>> ApplyCommittedHolonReplicaAsync(
            CommittedHolonReplicaRequest request, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Delivery is not inline with the primary transaction.");
    }

    private static CosmicStorageIdentity RoutedTargetIdentity(ProviderType type)
        => new(type, new string('A', 64), "declaration-only-" + type);

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task RoutedOwnedSaveCommitsGraphReceiptAndEffectiveTargetsOrRollsBackTogether(bool batch, bool failOutbox)
    {
        await Task.Run(async () =>
        {
            var actor = Guid.NewGuid();
            var operation = Guid.NewGuid();
            var journal = Path.Combine(Path.GetDirectoryName(_databasePath)!, "scoped-visibility.pending.json");
            await File.WriteAllTextAsync(journal, JsonSerializer.Serialize(new
            { test = nameof(RoutedOwnedSaveCommitsGraphReceiptAndEffectiveTargetsOrRollsBackTogether), actor, operation, batch, failOutbox, database = _databasePath }));

            using (var provider = new SQLLiteDBOASIS(TestConnectionString))
            {
                (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
                var nativeSource = (await provider.CaptureStorageIdentityAsync()).Result;
                string BoundTarget(ProviderType type) => new HolonReplicationRouteBinding(nativeSource,
                    RoutedTargetIdentity(type), HolonReplicationRoutes.DefaultPolicyBinding).TargetId;
                var boundSourceId = new HolonReplicationRouteBinding(nativeSource,
                    RoutedTargetIdentity(ProviderType.MongoDBOASIS), HolonReplicationRoutes.DefaultPolicyBinding).SourceId;
                if (failOutbox)
                {
                    using var control = new DataContext(TestConnectionString);
                    // The second target fails after the first obligation was inserted.
                    control.Database.ExecuteSqlRaw($"CREATE TRIGGER routed_owned_fault BEFORE INSERT ON CosmicHolonOutbox WHEN NEW.TargetId='{BoundTarget(ProviderType.Neo4jOASIS)}' BEGIN SELECT RAISE(ABORT,'routed owned outbox fault'); END");
                }

                var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
                { HyperDriveMode = HyperDriveModes.V2, StorageProviders = new StorageProviderSettings { LogSwitchingProviders = false } } };
                var providers = new ProviderManager(null, dna)
                { IsAutoFailOverEnabled = false, IsAutoReplicationEnabled = false, IsAutoLoadBalanceEnabled = false };
                providers.RegisterProvider(provider);
                foreach (var identity in new[] { ProviderType.MongoDBOASIS, ProviderType.Neo4jOASIS })
                {
                    var target = DispatchProxy.Create<IOASISStorageProvider, RoutedReplicaDeclaration>();
                    ((RoutedReplicaDeclaration)target).Identity = identity;
                    providers.RegisterProvider(target);
                }

                // Node defaults disable replication. The avatar enables Mongo;
                // this request selects both targets. Nothing mutates node defaults.
                OASISRequestContext.AvatarRoutingOverrides = new ProviderRoutingOverrides(
                    autoReplicationEnabled: true, replicationProviders: new[] { ProviderType.MongoDBOASIS });
                OASISRequestContext.RequestRoutingOverrides = new ProviderRoutingOverrides(
                    replicationProviders: new[] { ProviderType.MongoDBOASIS, ProviderType.Neo4jOASIS });
                var manager = new HolonManager(null, dna, providers);
                var inputs = Enumerable.Range(0, batch ? 2 : 1).Select(index => new Holon { Name = "routed-owned-" + index }).ToArray();
                var options = new CosmicWriteOptions(operation); // Enrollment comes only from effective routing.

                if (batch)
                {
                    var result = await manager.SaveOwnedHolonsAsync(inputs, actor, options,
                        saveChildren: false, providerType: ProviderType.SQLLiteDBOASIS);
                    result.IsError.Should().Be(failOutbox, result.Message + result.Exception);
                    result.IsSaved.Should().Be(!failOutbox);
                }
                else
                {
                    var result = await manager.SaveOwnedHolonAsync(inputs[0], actor, options,
                        saveChildren: false, providerType: ProviderType.SQLLiteDBOASIS);
                    result.IsError.Should().Be(failOutbox, result.Message + result.Exception);
                    result.IsSaved.Should().Be(!failOutbox);
                }

                using var connection = new SqliteConnection(TestConnectionString);
                await connection.OpenAsync();
                using var command = connection.CreateCommand();
                foreach (var table in new[] { "CosmicWriteReceipt", "CosmicHolonOutbox" })
                {
                    command.CommandText = "SELECT COUNT(*) FROM " + table;
                    Convert.ToInt64(await command.ExecuteScalarAsync()).Should().Be(failOutbox ? 0 : table == "CosmicHolonOutbox" ? 2 : 1);
                }
                using var inspect = new DataContext(TestConnectionString);
                (await inspect.Holons.AsNoTracking().CountAsync()).Should().Be(failOutbox ? 0 : inputs.Length);

                if (failOutbox)
                {
                    inputs.Should().OnlyContain(root => root.Id == Guid.Empty && root.Version == 0 && HolonChangeTracking.HasChanges(root));
                }
                else
                {
                    inputs.Should().OnlyContain(root => root.Id != Guid.Empty && root.Version == 1 && !HolonChangeTracking.HasChanges(root));
                    command.CommandText = "SELECT TargetId,PayloadJson FROM CosmicHolonOutbox ORDER BY TargetId";
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        foreach (var target in new[] { ProviderType.MongoDBOASIS, ProviderType.Neo4jOASIS })
                        {
                            (await reader.ReadAsync()).Should().BeTrue();
                            reader.GetString(0).Should().Be(BoundTarget(target));
                            var frozen = CommittedHolonReplicaPayload.Read(reader.GetString(1));
                            frozen.OperationId.Should().Be(operation);
                            frozen.ActorId.Should().Be(actor);
                            frozen.SourceId.Should().Be(boundSourceId);
                            frozen.NativeRoutes.Should().HaveCount(2);
                            frozen.NativeRoutes.Should().OnlyContain(route => route.SourceStorage.BindingFingerprint == nativeSource.BindingFingerprint);
                            frozen.NativeRoutes.Single(route => route.TargetStorage.Authority == target).TargetId.Should().Be(BoundTarget(target));
                            frozen.Roots.Select(root => root.VersionId).Should().BeEquivalentTo(inputs.Select(root => root.VersionId));
                        }
                        (await reader.ReadAsync()).Should().BeFalse();
                    }

                    // A native row cannot be retargeted outside its immutable
                    // descriptor set, even when the payload/digest are unchanged.
                    var boundOutbox = new SqliteHolonReplicationOutbox(TestConnectionString,null,nativeSource);
                    (await boundOutbox.ReadRouteHeadsAsync()).IsError.Should().BeFalse();
                    command.CommandText = "UPDATE CosmicHolonOutbox SET TargetId=$bad WHERE TargetId=$original AND OperationId=$operation";
                    command.Parameters.AddWithValue("$bad","un-enrolled-target");
                    command.Parameters.AddWithValue("$original",BoundTarget(ProviderType.MongoDBOASIS));
                    command.Parameters.AddWithValue("$operation",operation.ToString("D"));
                    (await command.ExecuteNonQueryAsync()).Should().Be(1);
                    (await boundOutbox.ReadRouteHeadsAsync()).ErrorCode.Should().Be("COSMIC_OUTBOX_CORRUPT");
                    (await boundOutbox.ClaimNextAsync("un-enrolled-target","owned-probe",TimeSpan.FromMinutes(1)))
                        .ErrorCode.Should().Be("COSMIC_OUTBOX_CORRUPT");
                    command.CommandText = "SELECT AttemptCount FROM CosmicHolonOutbox WHERE TargetId=$bad AND OperationId=$operation";
                    Convert.ToInt64(await command.ExecuteScalarAsync()).Should().Be(0);
                    command.CommandText = "UPDATE CosmicHolonOutbox SET TargetId=$original WHERE TargetId=$bad AND OperationId=$operation";
                    (await command.ExecuteNonQueryAsync()).Should().Be(1);
                    command.Parameters.Clear();
                    (await boundOutbox.ReadRouteHeadsAsync()).IsError.Should().BeFalse();

                    var noOp = await manager.SaveOwnedHolonsAsync(inputs, actor, new CosmicWriteOptions(Guid.NewGuid()),
                        saveChildren: false, providerType: ProviderType.SQLLiteDBOASIS);
                    noOp.IsError.Should().BeFalse(noOp.Message);
                    noOp.IsSaved.Should().BeFalse();
                    command.CommandText = "SELECT COUNT(*) FROM CosmicHolonOutbox";
                    Convert.ToInt64(await command.ExecuteScalarAsync()).Should().Be(2);
                    command.CommandText = "SELECT COUNT(*) FROM CosmicWriteReceipt";
                    Convert.ToInt64(await command.ExecuteScalarAsync()).Should().Be(1);
                }
            }

            // Independent reopened connection verifies durable state, not an EF cache.
            using (var reopened = new SqliteConnection(TestConnectionString))
            {
                await reopened.OpenAsync();
                using var command = reopened.CreateCommand();
                command.CommandText = "SELECT COUNT(*) FROM CosmicHolonOutbox";
                Convert.ToInt64(await command.ExecuteScalarAsync()).Should().Be(failOutbox ? 0 : 2);
            }
            File.Delete(journal); // The existing fixture owner removes only its exact DB files.
        });
    }
}
