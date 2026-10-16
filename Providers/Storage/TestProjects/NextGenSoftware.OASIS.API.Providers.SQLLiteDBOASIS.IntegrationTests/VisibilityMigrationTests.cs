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
    public void InventoryPublishedPackageMigrationPreservesRows()
    {
        using var context=new DataContext("Data Source=:memory:");context.Database.OpenConnection();
        context.Database.ExecuteSqlRaw("CREATE TABLE AvatarInventory (Id INTEGER PRIMARY KEY, Name TEXT); INSERT INTO AvatarInventory VALUES (1,'owned-package');");
        context.GetService<IMigrationsAssembly>().Migrations.Should().ContainKey("20261016020000_AddInventoryPublishedPackage");
        var migration=new AddInventoryPublishedPackage();var generator=context.GetService<IMigrationsSqlGenerator>();
        foreach(var command in generator.Generate(migration.UpOperations)) context.Database.ExecuteSqlRaw(command.CommandText);
        using var check=context.Database.GetDbConnection().CreateCommand();
        check.CommandText="SELECT PublishedSTARNETHolon FROM AvatarInventory WHERE Id=1";
        check.ExecuteScalar().Should().Be(DBNull.Value);
        context.Database.ExecuteSqlRaw("UPDATE AvatarInventory SET PublishedSTARNETHolon=X'00FF13' WHERE Id=1");
        check.ExecuteScalar().Should().BeOfType<byte[]>().Which.Should().Equal(0,255,19);
        foreach(var command in generator.Generate(migration.DownOperations)) context.Database.ExecuteSqlRaw(command.CommandText);
        check.CommandText="SELECT Name FROM AvatarInventory WHERE Id=1";check.ExecuteScalar().Should().Be("owned-package");
    }

    [Fact]
    public void InventoryMediaMigrationRetainsLegacyRow()
    {
        using var context=new DataContext("Data Source=:memory:");context.Database.OpenConnection();
        context.GetService<IMigrationsAssembly>().Migrations.Should().ContainKey("20261016010000_AddInventoryMedia");
        context.Database.ExecuteSqlRaw("CREATE TABLE AvatarInventory (Id INTEGER PRIMARY KEY,Quantity INTEGER NOT NULL); INSERT INTO AvatarInventory VALUES(1,7)");
        var migration=new AddInventoryMedia();var generator=context.GetService<IMigrationsSqlGenerator>();
        foreach(var command in generator.Generate(migration.UpOperations)) context.Database.ExecuteSqlRaw(command.CommandText);
        using var check=context.Database.GetDbConnection().CreateCommand();
        foreach(var column in new[] {"Image2D","Object3D","Image2DURI","Object3DURI"})
        {
            check.CommandText=$"SELECT {column} FROM AvatarInventory WHERE Id=1";
            check.ExecuteScalar().Should().Be(DBNull.Value);
        }
        check.CommandText="UPDATE AvatarInventory SET Image2D=X'0011FF',Object3D=X'FF001729',Image2DURI='https://example.test/image',Object3DURI='models/seed.glb' WHERE Id=1";
        check.ExecuteNonQuery();
        check.CommandText="SELECT Image2D FROM AvatarInventory WHERE Id=1";((byte[])check.ExecuteScalar()!).Should().Equal(0,17,255);
        foreach(var command in generator.Generate(migration.DownOperations)) context.Database.ExecuteSqlRaw(command.CommandText);
        check.CommandText="SELECT Quantity FROM AvatarInventory WHERE Id=1";Convert.ToInt32(check.ExecuteScalar()).Should().Be(7);
    }

    [Fact]
    public void TrackingMigrationRetainsLegacyRowAndDefaultsToNull()
    {
        using var context=new DataContext("Data Source=:memory:");context.Database.OpenConnection();
        context.GetService<IMigrationsAssembly>().Migrations.Should().ContainKey("20261016000000_AddAvatarDetailTracking");
        context.Database.ExecuteSqlRaw("CREATE TABLE AvatarDetail (Id TEXT PRIMARY KEY,Karma INTEGER NOT NULL); INSERT INTO AvatarDetail VALUES('owned-row',73)");
        var migration=new AddAvatarDetailTracking();var generator=context.GetService<IMigrationsSqlGenerator>();
        foreach(var command in generator.Generate(migration.UpOperations)) context.Database.ExecuteSqlRaw(command.CommandText);
        using var check=context.Database.GetDbConnection().CreateCommand();
        foreach(var column in new[] {"ActiveQuestId","ActiveObjectiveId"})
        {
            check.CommandText=$"SELECT {column} FROM AvatarDetail WHERE Id='owned-row'";
            check.ExecuteScalar().Should().Be(DBNull.Value);
            var id=Guid.NewGuid();check.CommandText=$"UPDATE AvatarDetail SET {column}='{id}' WHERE Id='owned-row'";check.ExecuteNonQuery();
            check.CommandText=$"SELECT {column} FROM AvatarDetail WHERE Id='owned-row'";Guid.Parse((string)check.ExecuteScalar()!).Should().Be(id);
        }
        foreach(var command in generator.Generate(migration.DownOperations)) context.Database.ExecuteSqlRaw(command.CommandText);
        check.CommandText="SELECT Karma FROM AvatarDetail WHERE Id='owned-row'";Convert.ToInt32(check.ExecuteScalar()).Should().Be(73);
    }

    [Fact]
    public void HeartRateSequenceMigrationRetainsLegacySample()
    {
        using var context=new DataContext("Data Source=:memory:");context.Database.OpenConnection();
        context.GetService<IMigrationsAssembly>().Migrations.Should().ContainKey("20261015020000_AddHeartRateSequence");
        context.Database.ExecuteSqlRaw("CREATE TABLE HeartRateEntry (Id INTEGER PRIMARY KEY,HeartRateValue INTEGER NOT NULL); INSERT INTO HeartRateEntry VALUES(1,73)");
        var migration=new AddHeartRateSequence();var generator=context.GetService<IMigrationsSqlGenerator>();
        foreach(var command in generator.Generate(migration.UpOperations)) context.Database.ExecuteSqlRaw(command.CommandText);
        using var check=context.Database.GetDbConnection().CreateCommand();check.CommandText="SELECT SequenceIndex FROM HeartRateEntry WHERE Id=1";
        Convert.ToInt32(check.ExecuteScalar()).Should().Be(0);
        foreach(var command in generator.Generate(migration.DownOperations)) context.Database.ExecuteSqlRaw(command.CommandText);
        check.CommandText="SELECT HeartRateValue FROM HeartRateEntry WHERE Id=1";Convert.ToInt32(check.ExecuteScalar()).Should().Be(73);
    }

    [Fact]
    public void VisibilityMigrationPreservesOldRowsAndDefaultsPrivate()
    {
        using var context=new DataContext("Data Source=:memory:");
        context.Database.OpenConnection();
        var connection=context.Database.GetDbConnection();
        context.GetService<IMigrationsAssembly>().Migrations.Should().ContainKey("20261015000000_AddRecordVisibility");
        context.GetService<IMigrationsAssembly>().Migrations.Should().ContainKey("20261015010000_AddRecordIdentity");
        foreach(var table in new[] {"Holon","Avatar","AvatarDetail"})
        {
            using var setup=connection.CreateCommand();
            setup.CommandText=$"CREATE TABLE \"{table}\" (Id TEXT PRIMARY KEY); INSERT INTO \"{table}\" VALUES ('owned-row');";
            setup.ExecuteNonQuery();
        }
        var migration=new AddRecordVisibility();var generator=context.GetService<IMigrationsSqlGenerator>();
        foreach(var command in generator.Generate(migration.UpOperations)) context.Database.ExecuteSqlRaw(command.CommandText);
        var identity=new AddRecordIdentity();
        foreach(var command in generator.Generate(identity.UpOperations)) context.Database.ExecuteSqlRaw(command.CommandText);
        foreach(var table in new[] {"Holon","Avatar","AvatarDetail"})
        {
            using var check=connection.CreateCommand();
            check.CommandText=$"SELECT IsPublic FROM \"{table}\" WHERE Id='owned-row'";
            Convert.ToInt64(check.ExecuteScalar()).Should().Be(0);
            check.CommandText=$"SELECT VersionId FROM \"{table}\" WHERE Id='owned-row'";
            Guid.Parse((string)check.ExecuteScalar()!).Should().BeEmpty();
            check.CommandText=$"SELECT PreviousVersionId FROM \"{table}\" WHERE Id='owned-row'";
            Guid.Parse((string)check.ExecuteScalar()!).Should().BeEmpty();
            check.CommandText=$"SELECT CustomHolonType FROM \"{table}\" WHERE Id='owned-row'";
            check.ExecuteScalar().Should().Be(DBNull.Value);
            check.CommandText=$"UPDATE \"{table}\" SET IsPublic=1 WHERE Id='owned-row'";check.ExecuteNonQuery();
            check.CommandText=$"SELECT IsPublic FROM \"{table}\" WHERE Id='owned-row'";
            Convert.ToInt64(check.ExecuteScalar()).Should().Be(1);
        }
        foreach(var command in generator.Generate(identity.DownOperations)) context.Database.ExecuteSqlRaw(command.CommandText);
        foreach(var command in generator.Generate(migration.DownOperations)) context.Database.ExecuteSqlRaw(command.CommandText);
        foreach(var table in new[] {"Holon","Avatar","AvatarDetail"})
        {
            using var check=connection.CreateCommand();check.CommandText=$"SELECT Id FROM \"{table}\"";
            check.ExecuteScalar().Should().Be("owned-row");
        }
    }
}
