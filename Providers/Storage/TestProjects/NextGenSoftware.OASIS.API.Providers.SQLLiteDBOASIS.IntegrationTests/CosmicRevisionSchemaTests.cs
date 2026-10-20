using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using Xunit;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Fact]
    public async Task RevisionArchiveDowngradeCannotDiscardRetainedHistory()
    {
        using var context=new DataContext(TestConnectionString);
        await context.Database.MigrateAsync();
        context.Database.ExecuteSqlRaw("INSERT INTO CosmicHolonRevision(HolonId,ProviderRevisionId,Version,VersionId,RecordJson,RecordDigest,ArchivedAtUtc) VALUES({0},{1},1,{2},{3},{4},{5})",
            Guid.NewGuid().ToString("D"),Guid.NewGuid().ToString("D"),Guid.NewGuid().ToString("D"),"{}",new string('0',64),DateTime.UtcNow.ToString("O"));
        Func<Task> down=()=>context.GetService<IMigrator>().MigrateAsync("20261020000000_AddCosmicHolonRecoveryEvidence");
        await down.Should().ThrowAsync<SqliteException>();
        (await context.Database.GetAppliedMigrationsAsync()).Should().Contain("20261020010000_AddCosmicHolonRevisionArchive");
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ActivationCreatesNativeRevisionArchiveWithPhysicalRevisionOwnership(bool synchronous)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        var activated=synchronous ? provider.ActivateProvider():await provider.ActivateProviderAsync();
        activated.IsError.Should().BeFalse(activated.Message+activated.Exception);
        using var control=new DataContext(TestConnectionString);
        control.Database.GetMigrations().Should().Contain("20261020010000_AddCosmicHolonRevisionArchive");
        control.Database.OpenConnection();
        using var command=control.Database.GetDbConnection().CreateCommand();
        command.CommandText="SELECT COUNT(*) FROM sqlite_master WHERE type='index' AND name='IX_CosmicHolonRevision_LogicalVersion'";
        Convert.ToInt32(command.ExecuteScalar()).Should().Be(1);
        var parent=Guid.NewGuid().ToString("D");var revision=Guid.NewGuid().ToString("D");
        void Insert(string token)
        {
            command.Parameters.Clear();
            command.CommandText="INSERT INTO CosmicHolonRevision(HolonId,ProviderRevisionId,Version,VersionId,RecordJson,RecordDigest,ArchivedAtUtc) VALUES($id,$token,1,$version,'{}',$digest,$at)";
            foreach(var pair in new Dictionary<string,object>{{"$id",parent},{"$token",token},{"$version",Guid.NewGuid().ToString("D")},
                {"$digest",new string('0',64)},{"$at",DateTime.UtcNow.ToString("O")}})
            {var parameter=command.CreateParameter();parameter.ParameterName=pair.Key;parameter.Value=pair.Value;command.Parameters.Add(parameter);}
            command.ExecuteNonQuery();
        }
        Insert(revision);
        Action duplicate=()=>Insert(revision);
        duplicate.Should().Throw<SqliteException>().Which.SqliteErrorCode.Should().Be(19);
        Insert(Guid.NewGuid().ToString("D"));
        command.Parameters.Clear();command.CommandText="SELECT COUNT(*) FROM CosmicHolonRevision";
        Convert.ToInt32(command.ExecuteScalar()).Should().Be(2,"distinct physical observations can share a logical version number");
    }
}
