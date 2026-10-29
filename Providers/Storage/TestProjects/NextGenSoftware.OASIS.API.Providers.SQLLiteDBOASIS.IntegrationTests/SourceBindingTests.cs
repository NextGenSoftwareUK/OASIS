using System.Text.Json;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CopiedSourceReceiptCannotVerifyOrReplayOriginalBoundOperation(bool bindOriginal)
    {
        var copy=Path.Combine(Path.GetDirectoryName(_databasePath)!,"source-binding-copy.db");
        File.Exists(copy).Should().BeFalse("an unfinished copy must be preserved");
        var journal=Path.Combine(Path.GetDirectoryName(_databasePath)!,"scoped-visibility.pending.json");
        using(var original=new SQLLiteDBOASIS(TestConnectionString))
        {
            (await original.ActivateProviderAsync()).IsError.Should().BeFalse();
            var identity=await original.CaptureStorageIdentityAsync();identity.IsError.Should().BeFalse();
            var input=OutboxCreate(new Holon{Id=Guid.NewGuid(),Name="owned bound source"});
            var enrollment=new HolonReplicationEnrollment("owned-bound-source",new[]{"target"},bindOriginal ? identity.Result : null);
            var request=new ConditionalHolonWriteRequest(new ConditionalHolonWrite(input.Write.OperationId,input.Write.ActorId,
                input.Write.Authority,input.Write.Conditions,enrollment),input.Operation,input.Roots);
            await File.WriteAllTextAsync(journal,JsonSerializer.Serialize(new{copy,request.Write.OperationId,identity=identity.Result}));
            var absent = await original.ReadBoundConditionalHolonWriteOutcomeAsync(request, identity.Result);
            absent.IsError.Should().BeFalse(absent.Message + absent.Exception);
            absent.Result.ReceiptState.Should().Be(ConditionalHolonReceiptState.Absent);
            var missingBinding = await original.ReadBoundConditionalHolonWriteOutcomeAsync(request, null!);
            missingBinding.IsError.Should().BeTrue();
            missingBinding.Result.Should().BeNull();
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            var cancelled = await original.ReadBoundConditionalHolonWriteOutcomeAsync(request, identity.Result, cancellation.Token);
            cancelled.ErrorCode.Should().Be("COSMIC_OPERATION_STATUS_CANCELLED");
            var saved=await original.ExecuteReplicatingHolonWriteAsync(request,enrollment);
            saved.IsError.Should().BeFalse(saved.Message+saved.Exception);
            (await original.ReadConditionalHolonWriteOutcomeAsync(request)).Result.ReceiptState.Should().Be(ConditionalHolonReceiptState.Committed);
            var boundStatus = await original.ReadBoundConditionalHolonWriteOutcomeAsync(request, identity.Result);
            boundStatus.IsError.Should().BeFalse(boundStatus.Message + boundStatus.Exception);
            boundStatus.Result.ReceiptState.Should().Be(ConditionalHolonReceiptState.Committed);
            boundStatus.IsSaved.Should().BeFalse();
            var comparison = new CosmicStorageIdentity(NextGenSoftware.OASIS.API.Core.Enums.ProviderType.Neo4jOASIS, new string('A', 64), Guid.NewGuid());
            var projected = await original.ReadConditionalHolonReceiptEvidenceAsync(new(request, identity.Result, comparison, new[] { request.Roots.Single().Id }));
            projected.IsError.Should().BeFalse(projected.Message + projected.Exception);
            projected.Result.ReceiptEvidence.Records.Single().HolonId.Should().Be(request.Roots.Single().Id);
            projected.Result.ReceiptEvidence.OriginalSelectedRecordCount.Should().Be(1);
            projected.Result.ReceiptEvidence.ComparisonTargetFingerprint.Should().Be(comparison.BindingFingerprint);

            request.Write.ReplicationEnrollment.SourceStorageIdentity.Should().Be(bindOriginal ? identity.Result : null);
            using(var source=new SqliteConnection(TestConnectionString))
            using(var destination=new SqliteConnection($"Data Source={copy};Pooling=False"))
            {await source.OpenAsync();await destination.OpenAsync();source.BackupDatabase(destination);}
            using(var copied=new SQLLiteDBOASIS($"Data Source={copy};Pooling=False"))
            {
                (await copied.ActivateProviderAsync()).IsError.Should().BeFalse();
                var copiedIdentity=await copied.CaptureStorageIdentityAsync();copiedIdentity.IsError.Should().BeFalse();
                copiedIdentity.Result.StoreId.Should().Be(identity.Result.StoreId);
                copiedIdentity.Result.BindingFingerprint.Should().NotBe(identity.Result.BindingFingerprint);
                using var observer=new SqliteConnection($"Data Source={copy};Pooling=False");await observer.OpenAsync();
                using var version=observer.CreateCommand();version.CommandText="PRAGMA data_version";
                var before=Convert.ToInt64(await version.ExecuteScalarAsync());
                var compared = await HolonReplicationReceiptPreflight.ReadAsync(original, copied,
                    new ConditionalHolonReceiptEvidenceRequest(request, identity.Result, copiedIdentity.Result, new[] { request.Roots.Single().Id }));
                compared.IsError.Should().BeFalse(compared.Message + compared.Exception);
                compared.Result.Records.Single().Kind.Should().Be(HolonReplicationReceiptComparisonKind.UntrackedCurrentRecord);
                compared.IsSaved.Should().BeFalse();
                if (bindOriginal)
                {
                    var replay=await copied.ExecuteReplicatingHolonWriteAsync(request,enrollment);
                    replay.ErrorCode.Should().Be("COSMIC_SOURCE_STORAGE_BINDING_MISMATCH");
                    replay.IsSaved.Should().BeFalse();
                }
                var status=await copied.ReadBoundConditionalHolonWriteOutcomeAsync(request, identity.Result);
                status.IsError.Should().BeTrue();status.IsLoaded.Should().BeFalse();status.Result.Should().BeNull();
                // A type-only receipt remains verifiable against the explicitly
                // approved copy, but cannot masquerade as the original file.
                var approvedCopy = await copied.ReadBoundConditionalHolonWriteOutcomeAsync(request, copiedIdentity.Result);
                approvedCopy.IsError.Should().Be(bindOriginal);
                if (!bindOriginal) approvedCopy.Result.ReceiptState.Should().Be(ConditionalHolonReceiptState.Committed);
                Convert.ToInt64(await version.ExecuteScalarAsync()).Should().Be(before,"neither mismatch nor recovery may mutate the copied queue");
            }
        }
        foreach(var path in new[]{copy,copy+"-wal",copy+"-shm"})if(File.Exists(path))File.Delete(path);
        File.Delete(journal);
    }
}
