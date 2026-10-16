using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Migrations;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public sealed class VisibilityMigrationTests
{
    [Fact]
    public void VisibilityMigrationPreservesOldRowsAndDefaultsPrivate()
    {
        using var context=new DataContext("Data Source=:memory:");
        context.Database.OpenConnection();
        var connection=context.Database.GetDbConnection();
        context.GetService<IMigrationsAssembly>().Migrations.Should().ContainKey("20261015000000_AddRecordVisibility");
        foreach(var table in new[] {"Holon","Avatar","AvatarDetail"})
        {
            using var setup=connection.CreateCommand();
            setup.CommandText=$"CREATE TABLE \"{table}\" (Id TEXT PRIMARY KEY); INSERT INTO \"{table}\" VALUES ('owned-row');";
            setup.ExecuteNonQuery();
        }
        var migration=new AddRecordVisibility();var generator=context.GetService<IMigrationsSqlGenerator>();
        foreach(var command in generator.Generate(migration.UpOperations)) context.Database.ExecuteSqlRaw(command.CommandText);
        foreach(var table in new[] {"Holon","Avatar","AvatarDetail"})
        {
            using var check=connection.CreateCommand();
            check.CommandText=$"SELECT IsPublic FROM \"{table}\" WHERE Id='owned-row'";
            Convert.ToInt64(check.ExecuteScalar()).Should().Be(0);
            check.CommandText=$"UPDATE \"{table}\" SET IsPublic=1 WHERE Id='owned-row'";check.ExecuteNonQuery();
            check.CommandText=$"SELECT IsPublic FROM \"{table}\" WHERE Id='owned-row'";
            Convert.ToInt64(check.ExecuteScalar()).Should().Be(1);
        }
        foreach(var command in generator.Generate(migration.DownOperations)) context.Database.ExecuteSqlRaw(command.CommandText);
        foreach(var table in new[] {"Holon","Avatar","AvatarDetail"})
        {
            using var check=connection.CreateCommand();check.CommandText=$"SELECT Id FROM \"{table}\"";
            check.ExecuteScalar().Should().Be("owned-row");
        }
    }
}
