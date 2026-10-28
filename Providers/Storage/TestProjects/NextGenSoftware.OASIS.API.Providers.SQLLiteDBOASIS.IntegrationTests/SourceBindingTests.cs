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
    public async Task CopiedSourceReceiptCannotVerifyOrReplayOriginalBoundOperation()
    {
        var copy=Path.Combine(Path.GetDirectoryName(_databasePath)!,"source-binding-copy.db");
        File.Exists(copy).Should().BeFalse("an unfinished copy must be preserved");
        var journal=Path.Combine(Path.GetDirectoryName(_databasePath)!,"scoped-visibility.pending.json");
        using(var original=new SQLLiteDBOASIS(TestConnectionString))
        {
            (await original.ActivateProviderAsync()).IsError.Should().BeFalse();
            var identity=await original.CaptureStorageIdentityAsync();identity.IsError.Should().BeFalse();
            var input=OutboxCreate(new Holon{Id=Guid.NewGuid(),Name="owned bound source"});
            var enrollment=new HolonReplicationEnrollment("owned-bound-source",new[]{"target"},identity.Result);
            var request=new ConditionalHolonWriteRequest(new ConditionalHolonWrite(input.Write.OperationId,input.Write.ActorId,
                input.Write.Authority,input.Write.Conditions,enrollment),input.Operation,input.Roots);
            await File.WriteAllTextAsync(journal,JsonSerializer.Serialize(new{copy,request.Write.OperationId,identity=identity.Result}));
            var saved=await original.ExecuteReplicatingHolonWriteAsync(request,enrollment);
            saved.IsError.Should().BeFalse(saved.Message+saved.Exception);
            (await original.ReadConditionalHolonWriteOutcomeAsync(request)).Result.ReceiptState.Should().Be(ConditionalHolonReceiptState.Committed);
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
                var replay=await copied.ExecuteReplicatingHolonWriteAsync(request,enrollment);
                replay.ErrorCode.Should().Be("COSMIC_SOURCE_STORAGE_BINDING_MISMATCH");replay.IsSaved.Should().BeFalse();
                var status=await copied.ReadConditionalHolonWriteOutcomeAsync(request);
                status.IsError.Should().BeTrue();status.IsLoaded.Should().BeFalse();status.Result.Should().BeNull();
                Convert.ToInt64(await version.ExecuteScalarAsync()).Should().Be(before,"neither mismatch nor recovery may mutate the copied queue");
            }
        }
        foreach(var path in new[]{copy,copy+"-wal",copy+"-shm"})if(File.Exists(path))File.Delete(path);
        File.Delete(journal);
    }
}
