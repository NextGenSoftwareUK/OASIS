using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.Utilities;
using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Theory][InlineData(false)][InlineData(true)]
    public async Task NativeGiftInsertFailureRollsBackDetailAndOriginalGift(bool synchronous)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var detail=new AvatarDetail{Id=Guid.NewGuid(),Username="owned-gift-rollback",XP=21};
        detail.Gifts.Add(new ContractGift{GiftType=Enum.GetValues<KarmaTypePositive>()[0],KarmaSourceTitle="original"});
        (await provider.SaveAvatarDetailAsync(detail)).IsError.Should().BeFalse();
        using(var control=new DataContext(TestConnectionString))
            await control.Database.ExecuteSqlRawAsync("CREATE TRIGGER owned_gift_insert_failure BEFORE INSERT ON AvatarGifts BEGIN SELECT RAISE(ABORT,'owned gift insert fault'); END");
        detail.XP=999;
        detail.Gifts=new List<IAvatarGift>{new ContractGift{GiftType=Enum.GetValues<KarmaTypePositive>()[0],KarmaSourceTitle="replacement"}};
        var rejected=synchronous?provider.SaveAvatarDetail(detail):await provider.SaveAvatarDetailAsync(detail);
        rejected.IsError.Should().BeTrue();rejected.IsSaved.Should().BeFalse();
        using var reader=new SQLLiteDBOASIS(TestConnectionString);
        (await reader.ActivateProviderAsync()).IsError.Should().BeFalse();
        var loaded=await reader.LoadAvatarDetailAsync(detail.Id);
        loaded.IsError.Should().BeFalse(loaded.Message);
        loaded.Result.XP.Should().Be(21);
        loaded.Result.Gifts.Should().ContainSingle().Which.KarmaSourceTitle.Should().Be("original");
    }

    [Theory][InlineData(false,false)][InlineData(true,false)][InlineData(false,true)][InlineData(true,true)]
    public async Task RefusedLegacyGiftUpgradeRollsBackEarlierSchemaChanges(bool synchronous,bool occupiedTarget)
    {
        using(var setup=new SQLLiteDBOASIS(TestConnectionString))
            (await setup.ActivateProviderAsync()).IsError.Should().BeFalse();
        string originalSchema;
        using(var control=new DataContext(TestConnectionString)){
            var connection=control.Database.GetDbConnection();await connection.OpenAsync();
            using var command=connection.CreateCommand();
            command.CommandText="SELECT sql FROM sqlite_master WHERE type='table' AND name='AvatarGifts'";
            var schema=(string)(await command.ExecuteScalarAsync())!;
            command.CommandText=schema.Replace("CREATE TABLE \"AvatarGifts\"","CREATE TABLE \"owned_legacy_gifts\"")
                .Replace("\"AvatarChakraId\" INTEGER NULL","\"AvatarChakraId\" INTEGER NOT NULL");
            await command.ExecuteNonQueryAsync();
            command.CommandText="DROP TABLE AvatarGifts; ALTER TABLE owned_legacy_gifts RENAME TO AvatarGifts; ALTER TABLE AvatarGifts DROP COLUMN SequenceIndex";
            await command.ExecuteNonQueryAsync();
            command.CommandText=occupiedTarget?"CREATE TABLE AvatarGifts_OptionalChakra (marker TEXT); INSERT INTO AvatarGifts_OptionalChakra VALUES ('preserve')"
                :"CREATE TABLE owned_gift_reference (gift INTEGER REFERENCES AvatarGifts(Id))";
            await command.ExecuteNonQueryAsync();
            command.CommandText="SELECT sql FROM sqlite_master WHERE type='table' AND name='AvatarGifts'";
            originalSchema=(string)(await command.ExecuteScalarAsync())!;
        }
        using(var provider=new SQLLiteDBOASIS(TestConnectionString)){
            var rejected=synchronous?provider.ActivateProvider():await provider.ActivateProviderAsync();
            rejected.IsError.Should().BeTrue();
        }
        using var verify=new DataContext(TestConnectionString);
        var native=verify.Database.GetDbConnection();await native.OpenAsync();using var check=native.CreateCommand();
        check.CommandText="SELECT sql FROM sqlite_master WHERE type='table' AND name='AvatarGifts'";
        (await check.ExecuteScalarAsync()).Should().Be(originalSchema);
        check.CommandText="SELECT COUNT(*) FROM pragma_table_info('AvatarGifts') WHERE name='SequenceIndex'";
        Convert.ToInt32(await check.ExecuteScalarAsync()).Should().Be(0);
        if(occupiedTarget){check.CommandText="SELECT marker FROM AvatarGifts_OptionalChakra";(await check.ExecuteScalarAsync()).Should().Be("preserve");}
        else{check.CommandText="SELECT COUNT(*) FROM sqlite_master WHERE name='owned_gift_reference'";Convert.ToInt32(await check.ExecuteScalarAsync()).Should().Be(1);}
    }
    [Theory]
    [InlineData("QuestItem","not-a-guid",false)]
    [InlineData("UnknownLegacyType","11111111-1111-1111-1111-111111111111",false)]
    [InlineData("QuestItem","11111111-1111-1111-1111-111111111111",true)]
    public async Task InventoryReconciliationValidatesLegacyValuesBeforeConversion(string itemType,string nftId,bool accepted)
    {
        using var context=new DataContext(TestConnectionString);
        var migrator=context.GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrator>();
        await migrator.MigrateAsync("20261019000001_OptionalGiftChakra");
        await context.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO AvatarInventory (Id,Quantity,ItemType,NftId) VALUES (1,2,{itemType},{nftId})");
        if(accepted){
            await migrator.MigrateAsync();
            var item=await context.Set<InventoryItemModel>().AsNoTracking().SingleAsync();
            item.NftId.Should().Be(Guid.Parse(nftId));
            item.MetaDataJson.Should().Be("{}");item.PropertiesJson.Should().Be("{}");
        }else{
            await Assert.ThrowsAnyAsync<Exception>(()=>migrator.MigrateAsync());
            (await context.Database.GetAppliedMigrationsAsync()).Should().NotContain("20261019093321_ReconcileSqliteModel");
            var connection=context.Database.GetDbConnection();await connection.OpenAsync();
            using var command=connection.CreateCommand();command.CommandText="SELECT ItemType || '|' || NftId FROM AvatarInventory WHERE Id=1";
            (await command.ExecuteScalarAsync()).Should().Be(itemType+"|"+nftId);
        }
    }
    [Fact]
    public void InventorySchemaMigrationsAreDiscoverableInChronologicalOrder()
    {
        using var context=new DataContext(TestConnectionString);
        var migrations=context.Database.GetMigrations().ToArray();
        migrations.Should().Contain("20250227000000_AddNftIdAndInventoryFieldsToAvatarInventory");
        migrations.Should().Contain("20260915000000_AddGeoNFTIdToAvatarInventory");
        Array.IndexOf(migrations,"20250227000000_AddNftIdAndInventoryFieldsToAvatarInventory").Should()
            .BeLessThan(Array.IndexOf(migrations,"20260915000000_AddGeoNFTIdToAvatarInventory"));
    }
    [Fact]
    public async Task ReconciliationMigrationPreservesDistinctLegacyTokenKeysAndPayloads()
    {
        using(var legacy=new DataContext(TestConnectionString)){
            var migrator=legacy.GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrator>();
            await migrator.MigrateAsync("20261019000001_OptionalGiftChakra");
            legacy.Database.ExecuteSqlRaw("INSERT INTO RefreshTokens (Id,Token,Expires,IsExpired,Created,IsActive) VALUES (7,'owned-seven','2026-10-19',0,'2026-10-19',1),(23,'owned-twenty-three','2026-10-19',0,'2026-10-19',1)");
            await migrator.MigrateAsync();
        }
        using var verify=new DataContext(TestConnectionString);
        var rows=verify.Set<RefreshTokenModel>().AsNoTracking().OrderBy(row=>row.Id).ToList();
        rows.Select(row=>row.Id).Should().Equal(7,23);rows.Select(row=>row.StorageId).Should().Equal(7,23);
        rows.Select(row=>row.Token).Should().Equal("owned-seven","owned-twenty-three");
    }
    [Fact]
    public void CurrentSqliteSnapshotMatchesActualRuntimeSchemaModel()
    {
        using var context=new DataContext(TestConnectionString);
        var snapshot=context.GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrationsAssembly>().ModelSnapshot!.Model;
        var initialized=context.GetService<Microsoft.EntityFrameworkCore.Infrastructure.IModelRuntimeInitializer>().Initialize(snapshot,designTime:true);
        var changes=context.GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrationsModelDiffer>().GetDifferences(initialized.GetRelationalModel(),context.GetService<Microsoft.EntityFrameworkCore.Metadata.IDesignTimeModel>().Model.GetRelationalModel());
        changes.Should().BeEmpty();
    }
    [Fact]
    public async Task GiftEfMigrationExecutesUpAndRefusesLossyDownThenRoundTrips()
    {
        using(var initial=new DataContext(TestConnectionString)){
            var snapshot=initial.GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrationsAssembly>().ModelSnapshot!.Model;
            var initialized=initial.GetService<Microsoft.EntityFrameworkCore.Infrastructure.IModelRuntimeInitializer>().Initialize(snapshot,designTime:true);
            var changes=initial.GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrationsModelDiffer>().GetDifferences(initialized.GetRelationalModel(),initial.GetService<Microsoft.EntityFrameworkCore.Metadata.IDesignTimeModel>().Model.GetRelationalModel());
            changes.Should().BeEmpty(string.Join("; ",changes.Select(change=>change.GetType().Name+":"+change.GetType().GetProperty("Table")?.GetValue(change)+"/"+change.GetType().GetProperty("Name")?.GetValue(change))));
            await initial.Database.MigrateAsync();
        }
        var id=Guid.NewGuid();
        using(var provider=new SQLLiteDBOASIS(TestConnectionString)){
            (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
            var detail=new AvatarDetail{Id=id,Username="migration-gift-owner"};
            detail.Gifts.Add(new ContractGift{GiftType=Enum.GetValues<KarmaTypePositive>()[0],KarmaSourceTitle="must-not-discard"});
            (await provider.SaveAvatarDetailAsync(detail)).IsError.Should().BeFalse();
        }
        using(var downgrade=new DataContext(TestConnectionString)){
            var migrator=downgrade.GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrator>();
            await Assert.ThrowsAnyAsync<Exception>(()=>migrator.MigrateAsync("20261018000005_AddAvatarDetailCelestialIds"));
        }
        using(var provider=new SQLLiteDBOASIS(TestConnectionString)){
            (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
            var loaded=await provider.LoadAvatarDetailAsync(id);
            loaded.IsError.Should().BeFalse(loaded.Message);
            loaded.Result.Should().NotBeNull(loaded.Message);
            var detail=loaded.Result;detail.Gifts.Should().ContainSingle().Which.KarmaSourceTitle.Should().Be("must-not-discard");
            detail.Gifts.Clear();(await provider.SaveAvatarDetailAsync(detail)).IsError.Should().BeFalse();
            var deleted=await provider.DeleteAvatarDetailAsync(id,softDelete:false);
            deleted.IsError.Should().BeFalse(deleted.Message);
        }
        using(var migration=new DataContext(TestConnectionString)){
            // The reconciliation cannot represent populated current records in the old schema.
            // Explicitly remove this owned fixture only after proving refusal preserved it.
            var migrator=migration.GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrator>();
            await migrator.MigrateAsync("20261018000005_AddAvatarDetailCelestialIds");
            await migrator.MigrateAsync();
        }
    }
    private sealed class ContractGift:IAvatarGift
    {
        public Guid AvatarId{get;set;}public DateTime GiftEarnt{get;set;}public KarmaTypePositive GiftType{get;set;}
        public EnumValue<KarmaSourceType> KarmaSource{get;set;}=new(Enum.GetValues<KarmaSourceType>()[0]);
        public EnumValue<ProviderType> Provider{get;set;}=new(ProviderType.SQLLiteDBOASIS);
        public string KarmaSourceDesc{get;set;}="owned interface gift";public string KarmaSourceTitle{get;set;}="";public string WebLink{get;set;}="https://example.invalid/gift";
    }
    [Theory][InlineData(false)][InlineData(true)]
    public async Task InterfaceGiftsPreserveOrderAcrossNativeReplaceAndClear(bool synchronous)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);(await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var detail=new AvatarDetail{Id=Guid.NewGuid(),Username="interface-gift-owner"};
        for(var revision=0;revision<3;revision++){
            var expected=revision==0?new[]{"first","second"}:new[]{"updated second 🌳","updated first"};
            detail.Gifts=revision==2?null!:expected.Select(title=>(IAvatarGift)new ContractGift{GiftType=Enum.GetValues<KarmaTypePositive>()[0],GiftEarnt=new DateTime(2026,10,19,0,0,revision,DateTimeKind.Utc),KarmaSourceTitle=title}).ToList();
            var saved=synchronous?provider.SaveAvatarDetail(detail):await provider.SaveAvatarDetailAsync(detail);saved.IsError.Should().BeFalse(saved.Exception?.ToString());
            if(detail.Gifts!=null)detail.Gifts.Should().OnlyContain(gift=>gift.AvatarId==Guid.Empty,"owner normalization must not mutate caller gifts");
            using var reader=new SQLLiteDBOASIS(TestConnectionString);(await reader.ActivateProviderAsync()).IsError.Should().BeFalse();
            var loaded=synchronous?reader.LoadAvatarDetail(detail.Id):await reader.LoadAvatarDetailAsync(detail.Id);loaded.IsError.Should().BeFalse(loaded.Exception?.ToString());
            loaded.Result.Gifts.Select(gift=>gift.KarmaSourceTitle).Should().Equal(revision==2?Array.Empty<string>():expected);
            if(revision==2)loaded.Result.Gifts.Should().BeEmpty();
            else loaded.Result.Gifts.Should().OnlyContain(gift=>gift.AvatarId==detail.Id);
            using var control=new DataContext(TestConnectionString);var native=control.Set<AvatarGiftModel>().AsNoTracking().Where(gift=>gift.AvatarId==detail.Id.ToString()).OrderBy(gift=>gift.SequenceIndex).ToList();
            native.Count.Should().Be(revision==2?0:2);native.Select(gift=>gift.SequenceIndex).Should().Equal(revision==2?Array.Empty<int>():new[]{0,1});
        }
    }
    [Theory][InlineData(false)][InlineData(true)]
    public async Task ForeignGiftOwnerCannotChangeNativeDetailOrGifts(bool synchronous)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);(await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var detail=new AvatarDetail{Id=Guid.NewGuid(),Username="gift-owner-validation",XP=21};
        detail.Gifts.Add(new ContractGift{GiftType=Enum.GetValues<KarmaTypePositive>()[0],KarmaSourceTitle="original"});
        (await provider.SaveAvatarDetailAsync(detail)).IsError.Should().BeFalse();
        detail.XP=999;detail.Gifts=new List<IAvatarGift>{new ContractGift{AvatarId=Guid.NewGuid(),GiftType=Enum.GetValues<KarmaTypePositive>()[0],KarmaSourceTitle="foreign"}};
        var rejected=synchronous?provider.SaveAvatarDetail(detail):await provider.SaveAvatarDetailAsync(detail);rejected.IsError.Should().BeTrue();rejected.IsSaved.Should().BeFalse();
        using var reader=new SQLLiteDBOASIS(TestConnectionString);(await reader.ActivateProviderAsync()).IsError.Should().BeFalse();
        var actual=(await reader.LoadAvatarDetailAsync(detail.Id)).Result;actual.XP.Should().Be(21);actual.Gifts.Should().ContainSingle().Which.KarmaSourceTitle.Should().Be("original");
    }
    [Theory][InlineData(false,false)][InlineData(true,false)][InlineData(false,true)][InlineData(true,true)]
    public async Task LegacyGiftChakraUpgradePreservesRowsObjectsAndSequence(bool synchronous,bool empty)
    {
        var owner=Guid.NewGuid();
        using(var setup=new SQLLiteDBOASIS(TestConnectionString)){
            (await setup.ActivateProviderAsync()).IsError.Should().BeFalse();
            (await setup.SaveAvatarDetailAsync(new AvatarDetail{Id=owner,Username="legacy-gift-owner"})).IsError.Should().BeFalse();
        }
        long giftId,chakraId;
        using(var control=new DataContext(TestConnectionString)){
            var chakra=new AvatarChakraModel{AvatarId=owner.ToString(),Name="retained chakra"};control.Set<AvatarChakraModel>().Add(chakra);control.SaveChanges();chakraId=chakra.Id;
            var gift=new AvatarGiftModel{AvatarId=owner.ToString(),AvatarChakraId=chakraId,KarmaSourceTitle="chakra-owned gift"};control.Set<AvatarGiftModel>().Add(gift);control.SaveChanges();giftId=gift.Id;
            using var connection=control.Database.GetDbConnection();connection.Open();using var command=connection.CreateCommand();
            command.CommandText="SELECT sql FROM sqlite_master WHERE type='table' AND name='AvatarGifts'";var schema=(string)command.ExecuteScalar()!;
            schema.Should().Contain("\"AvatarChakraId\" INTEGER NULL");
            command.CommandText=schema.Replace("CREATE TABLE \"AvatarGifts\"","CREATE TABLE \"gift_legacy_fixture\"").Replace("\"AvatarChakraId\" INTEGER NULL","\"AvatarChakraId\" INTEGER NOT NULL");command.ExecuteNonQuery();
            command.CommandText="INSERT INTO gift_legacy_fixture SELECT * FROM AvatarGifts; DROP TABLE AvatarGifts; ALTER TABLE gift_legacy_fixture RENAME TO AvatarGifts; UPDATE sqlite_sequence SET seq=999 WHERE name='AvatarGifts'; CREATE INDEX owned_gift_owner_index ON AvatarGifts(AvatarId); CREATE TRIGGER owned_gift_update_probe AFTER UPDATE ON AvatarGifts BEGIN SELECT 1; END;";command.ExecuteNonQuery();
            if(empty){command.CommandText="DELETE FROM AvatarGifts";command.ExecuteNonQuery();}
        }
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        var active=synchronous?provider.ActivateProvider():await provider.ActivateProviderAsync();active.IsError.Should().BeFalse(active.Exception?.ToString());
        using(var control=new DataContext(TestConnectionString)){
            if(!empty){var gift=control.Set<AvatarGiftModel>().AsNoTracking().Single(row=>row.Id==giftId);gift.AvatarChakraId.Should().Be(chakraId);gift.KarmaSourceTitle.Should().Be("chakra-owned gift");}
            using var connection=control.Database.GetDbConnection();connection.Open();using var command=connection.CreateCommand();
            command.CommandText="SELECT count(*) FROM sqlite_master WHERE name IN ('owned_gift_owner_index','owned_gift_update_probe')";Convert.ToInt32(command.ExecuteScalar()).Should().Be(2);
            command.CommandText="SELECT seq FROM sqlite_sequence WHERE name='AvatarGifts'";Convert.ToInt64(command.ExecuteScalar()).Should().Be(999);
            command.CommandText="PRAGMA foreign_key_check(AvatarGifts)";using var reader=command.ExecuteReader();reader.Read().Should().BeFalse();
        }
        var detail=(await provider.LoadAvatarDetailAsync(owner)).Result;
        detail.Gifts.Should().BeEmpty();
        detail.Gifts.Add(new AvatarGift{AvatarId=owner,Provider=new(ProviderType.SQLLiteDBOASIS),KarmaSource=new(Enum.GetValues<KarmaSourceType>()[0]),GiftType=Enum.GetValues<KarmaTypePositive>()[0],KarmaSourceTitle="new standalone"});
        var saved=synchronous?provider.SaveAvatarDetail(detail):await provider.SaveAvatarDetailAsync(detail);saved.IsError.Should().BeFalse(saved.Exception?.ToString());
        using var verify=new DataContext(TestConnectionString);
        if(!empty)verify.Set<AvatarGiftModel>().AsNoTracking().Single(row=>row.Id==giftId).AvatarChakraId.Should().Be(chakraId);
        verify.Set<AvatarGiftModel>().AsNoTracking().Single(row=>row.AvatarChakraId==null).Id.Should().BeGreaterThan(999);
    }
    [Theory][InlineData(false)][InlineData(true)]
    public async Task DetailGiftContractSurvivesNativeInsertAndIndependentRead(bool synchronous)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var detail=new AvatarDetail{Id=Guid.NewGuid(),Username="owned-gift-contract"};
        detail.Gifts.Add(new AvatarGift{AvatarId=detail.Id,GiftEarnt=new DateTime(2026,10,19,0,0,0,DateTimeKind.Utc),
            GiftType=Enum.GetValues<KarmaTypePositive>()[0],KarmaSource=new EnumValue<KarmaSourceType>(Enum.GetValues<KarmaSourceType>()[0]),
            Provider=new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS),KarmaSourceTitle="owned gift",KarmaSourceDesc="preserve gift description",WebLink="https://example.invalid/gift"});
        var saved=synchronous?provider.SaveAvatarDetail(detail):await provider.SaveAvatarDetailAsync(detail);
        saved.IsError.Should().BeFalse(saved.Exception?.ToString());
        using var reader=new SQLLiteDBOASIS(TestConnectionString);
        (await reader.ActivateProviderAsync()).IsError.Should().BeFalse();
        var loaded=synchronous?reader.LoadAvatarDetail(detail.Id):await reader.LoadAvatarDetailAsync(detail.Id);
        loaded.IsError.Should().BeFalse(loaded.Exception?.ToString());
        loaded.Result.Gifts.Should().BeEquivalentTo(detail.Gifts);
    }
}
