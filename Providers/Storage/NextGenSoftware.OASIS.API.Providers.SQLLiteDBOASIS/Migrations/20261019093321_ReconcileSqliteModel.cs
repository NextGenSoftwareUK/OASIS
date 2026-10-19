using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Migrations
{
    /// <inheritdoc />
    public partial class ReconcileSqliteModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Optional legacy identities may be absent, but malformed non-null identities must not be rewritten.
            foreach (var column in new[] { "NftId", "GeoNFTId" })
                migrationBuilder.Sql($"CREATE TEMP TABLE owned_inventory_guid_guard(value INTEGER CHECK(value=0)); INSERT INTO owned_inventory_guid_guard SELECT COUNT(*) FROM AvatarInventory WHERE {column} IS NOT NULL AND (length({column})<>36 OR substr({column},9,1)<>'-' OR substr({column},14,1)<>'-' OR substr({column},19,1)<>'-' OR substr({column},24,1)<>'-' OR length(replace({column},'-',''))<>32 OR lower(replace({column},'-','')) GLOB '*[^0-9a-f]*'); DROP TABLE owned_inventory_guid_guard;");
            // Frozen legacy enum-name mapping: unknown/absent types fail atomically instead of casting to Potion.
            migrationBuilder.Sql("CREATE TEMP TABLE owned_inventory_type_guard(value INTEGER CHECK(value=0)); INSERT INTO owned_inventory_type_guard SELECT COUNT(*) FROM AvatarInventory WHERE ItemType IS NULL OR CAST(ItemType AS TEXT) NOT IN ('Potion','0','Weapon','1','RareWeapon','2','LegendaryWeapon','3','EpicWeapon','4','UniqueWeapon','5','MagicalWeapon','6','EnchantedWeapon','7','MythicalWeapon','8','DivineWeapon','9','Armour','10','RareArmour','11','LegendaryArmour','12','EpicArmour','13','UniqueArmour','14','MagicalArmour','15','EnchantedArmour','16','MythicalArmour','17','DivineArmour','18','Shield','19','RareShield','20','LegendaryShield','21','EpicShield','22','UniqueShield','23','MagicalShield','24','EnchantedShield','25','MythicalShield','26','DivineShield','27','Accessory','28','Consumable','29','QuestItem','30','KeyItem','31','Material','32','Treasure','33','Gold','34','Gemstone','35','Relic','36','Scroll','37','Book','38','Recipe','39','Artifact','40','Tool','41','Spell','42','Summon','43','Upgrade','44','CraftingComponent','45','Currency','46','Miscellaneous','47','Collectible','48','CommonItem','49','RareItem','50','UniqueItem','51','LegendaryItem','52','EpicItem','53','SpecialItem','54','MythicalItem','55','DivineItem','56','EnchantedItem','57','EventItem','58','SeasonalItem','59','LimitedEditionItem','60','CustomItem','61','Ammo','62','Armor','63','Powerup','64','Health','65','Monster','66','Nature','67'); DROP TABLE owned_inventory_type_guard; UPDATE AvatarInventory SET ItemType=CASE ItemType WHEN 'Potion' THEN '0' WHEN 'Weapon' THEN '1' WHEN 'RareWeapon' THEN '2' WHEN 'LegendaryWeapon' THEN '3' WHEN 'EpicWeapon' THEN '4' WHEN 'UniqueWeapon' THEN '5' WHEN 'MagicalWeapon' THEN '6' WHEN 'EnchantedWeapon' THEN '7' WHEN 'MythicalWeapon' THEN '8' WHEN 'DivineWeapon' THEN '9' WHEN 'Armour' THEN '10' WHEN 'RareArmour' THEN '11' WHEN 'LegendaryArmour' THEN '12' WHEN 'EpicArmour' THEN '13' WHEN 'UniqueArmour' THEN '14' WHEN 'MagicalArmour' THEN '15' WHEN 'EnchantedArmour' THEN '16' WHEN 'MythicalArmour' THEN '17' WHEN 'DivineArmour' THEN '18' WHEN 'Shield' THEN '19' WHEN 'RareShield' THEN '20' WHEN 'LegendaryShield' THEN '21' WHEN 'EpicShield' THEN '22' WHEN 'UniqueShield' THEN '23' WHEN 'MagicalShield' THEN '24' WHEN 'EnchantedShield' THEN '25' WHEN 'MythicalShield' THEN '26' WHEN 'DivineShield' THEN '27' WHEN 'Accessory' THEN '28' WHEN 'Consumable' THEN '29' WHEN 'QuestItem' THEN '30' WHEN 'KeyItem' THEN '31' WHEN 'Material' THEN '32' WHEN 'Treasure' THEN '33' WHEN 'Gold' THEN '34' WHEN 'Gemstone' THEN '35' WHEN 'Relic' THEN '36' WHEN 'Scroll' THEN '37' WHEN 'Book' THEN '38' WHEN 'Recipe' THEN '39' WHEN 'Artifact' THEN '40' WHEN 'Tool' THEN '41' WHEN 'Spell' THEN '42' WHEN 'Summon' THEN '43' WHEN 'Upgrade' THEN '44' WHEN 'CraftingComponent' THEN '45' WHEN 'Currency' THEN '46' WHEN 'Miscellaneous' THEN '47' WHEN 'Collectible' THEN '48' WHEN 'CommonItem' THEN '49' WHEN 'RareItem' THEN '50' WHEN 'UniqueItem' THEN '51' WHEN 'LegendaryItem' THEN '52' WHEN 'EpicItem' THEN '53' WHEN 'SpecialItem' THEN '54' WHEN 'MythicalItem' THEN '55' WHEN 'DivineItem' THEN '56' WHEN 'EnchantedItem' THEN '57' WHEN 'EventItem' THEN '58' WHEN 'SeasonalItem' THEN '59' WHEN 'LimitedEditionItem' THEN '60' WHEN 'CustomItem' THEN '61' WHEN 'Ammo' THEN '62' WHEN 'Armor' THEN '63' WHEN 'Powerup' THEN '64' WHEN 'Health' THEN '65' WHEN 'Monster' THEN '66' WHEN 'Nature' THEN '67' ELSE ItemType END;");
            // Preserve the old unique row key AND the public token Id before rebuilding its primary key.
            migrationBuilder.Sql("ALTER TABLE RefreshTokens RENAME COLUMN Id TO StorageId; ALTER TABLE RefreshTokens ADD COLUMN Id INTEGER NOT NULL DEFAULT 0; UPDATE RefreshTokens SET Id=StorageId;");
            migrationBuilder.Sql("CREATE TEMP TABLE owned_token_owner_guard(value INTEGER CHECK(value=0)); INSERT INTO owned_token_owner_guard SELECT COUNT(*) FROM RefreshTokens WHERE AvatarId IS NOT NULL AND AvatarModelId IS NOT NULL AND AvatarId<>AvatarModelId; DROP TABLE owned_token_owner_guard; UPDATE RefreshTokens SET AvatarId=AvatarModelId WHERE AvatarId IS NULL AND AvatarModelId IS NOT NULL;");
            migrationBuilder.DropForeignKey(
                name: "FK_RefreshTokens_Avatar_AvatarModelId",
                table: "RefreshTokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RefreshTokens",
                table: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_AvatarModelId",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "AvatarModelId",
                table: "RefreshTokens");

            migrationBuilder.RenameColumn(
                name: "NunmerActiveAvatars",
                table: "SuperStar",
                newName: "NumberActiveAvatars");

            migrationBuilder.RenameColumn(
                name: "NunmerActiveAvatars",
                table: "Star",
                newName: "NumberActiveAvatars");

            migrationBuilder.RenameColumn(
                name: "NunmerActiveAvatars",
                table: "Planet",
                newName: "NumberActiveAvatars");

            migrationBuilder.RenameColumn(
                name: "NunmerActiveAvatars",
                table: "Moon",
                newName: "NumberActiveAvatars");

            migrationBuilder.RenameColumn(
                name: "NunmerActiveAvatars",
                table: "Meteroid",
                newName: "NumberActiveAvatars");

            migrationBuilder.RenameColumn(
                name: "NunmerActiveAvatars",
                table: "GreatGrandSuperStar",
                newName: "NumberActiveAvatars");

            migrationBuilder.RenameColumn(
                name: "NunmerActiveAvatars",
                table: "GrandSuperStar",
                newName: "NumberActiveAvatars");

            migrationBuilder.RenameColumn(
                name: "NunmerActiveAvatars",
                table: "Comet",
                newName: "NumberActiveAvatars");

            migrationBuilder.RenameColumn(
                name: "Telekineseis",
                table: "AvatarSuperPowers",
                newName: "Telekinesis");

            migrationBuilder.RenameColumn(
                name: "Invulerability",
                table: "AvatarSuperPowers",
                newName: "Invulnerability");

            migrationBuilder.RenameColumn(
                name: "RangeCombat",
                table: "AvatarSkills",
                newName: "RangedCombat");

            migrationBuilder.RenameColumn(
                name: "MelleeCombat",
                table: "AvatarSkills",
                newName: "MeleeCombat");

            migrationBuilder.RenameColumn(
                name: "NunmerActiveAvatars",
                table: "Asteroid",
                newName: "NumberActiveAvatars");

            migrationBuilder.AddColumn<string>(
                name: "AvatarModelId",
                table: "ProviderKey",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LocalProviderKey",
                table: "Holon",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetaDataJson",
                table: "Holon",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderKeysJson",
                table: "Holon",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "NftId",
                table: "AvatarInventory",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ItemType",
                table: "AvatarInventory",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "GeoNFTId",
                table: "AvatarInventory",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AcquiredOn",
                table: "AvatarInventory",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsTradeable",
                table: "AvatarInventory",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsUsable",
                table: "AvatarInventory",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "ItemId",
                table: "AvatarInventory",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUsedOn",
                table: "AvatarInventory",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "MaxQuantity",
                table: "AvatarInventory",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "MetaDataJson",
                table: "AvatarInventory",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PropertiesJson",
                table: "AvatarInventory",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Rarity",
                table: "AvatarInventory",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "Weight",
                table: "AvatarInventory",
                type: "REAL",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AlterColumn<int>(
                name: "CreatedProviderType",
                table: "AvatarDetail",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<int>(
                name: "CreatedOASISType",
                table: "AvatarDetail",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<string>(
                name: "MetaDataJson",
                table: "AvatarDetail",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "YogaPose",
                table: "AvatarChakras",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<int>(
                name: "Element",
                table: "AvatarChakras",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<string>(
                name: "LocalProviderKey",
                table: "Avatar",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetaDataJson",
                table: "Avatar",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ParentHolonId",
                table: "Avatar",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "ProviderKeysJson",
                table: "Avatar",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderWalletsJson",
                table: "Avatar",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_RefreshTokens",
                table: "RefreshTokens",
                column: "StorageId");

            migrationBuilder.CreateTable(
                name: "AvatarWalletStore",
                columns: table => new
                {
                    AvatarId = table.Column<string>(type: "TEXT", nullable: false),
                    WalletsJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvatarWalletStore", x => x.AvatarId);
                    table.ForeignKey(
                        name: "FK_AvatarWalletStore_Avatar_AvatarId",
                        column: x => x.AvatarId,
                        principalTable: "Avatar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_AvatarId",
                table: "RefreshTokens",
                column: "AvatarId");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderKey_AvatarModelId",
                table: "ProviderKey",
                column: "AvatarModelId");

            migrationBuilder.CreateIndex(
                name: "IX_Holon_LocalProviderKey",
                table: "Holon",
                column: "LocalProviderKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Avatar_LocalProviderKey",
                table: "Avatar",
                column: "LocalProviderKey",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ProviderKey_Avatar_AvatarModelId",
                table: "ProviderKey",
                column: "AvatarModelId",
                principalTable: "Avatar",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RefreshTokens_Avatar_AvatarId",
                table: "RefreshTokens",
                column: "AvatarId",
                principalTable: "Avatar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
            // These newly introduced maps did not exist in legacy records; initialize their canonical empty representation.
            foreach(var table in new[]{"Holon","Avatar","AvatarDetail","AvatarInventory"})
                migrationBuilder.Sql($"UPDATE {table} SET MetaDataJson='{{}}' WHERE MetaDataJson IS NULL");
            migrationBuilder.Sql("UPDATE Holon SET ProviderKeysJson='{}' WHERE ProviderKeysJson IS NULL; UPDATE Avatar SET ProviderKeysJson='{}' WHERE ProviderKeysJson IS NULL; UPDATE Avatar SET ProviderWalletsJson='{}' WHERE ProviderWalletsJson IS NULL; UPDATE AvatarInventory SET PropertiesJson='{}' WHERE PropertiesJson IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // This reconciliation adds representations the legacy schema cannot preserve.
            // Downgrades are supported only with empty affected tables, never by discarding records.
            migrationBuilder.Sql("CREATE TEMP TABLE owned_reconciliation_down_guard(value INTEGER CHECK(value=0)); INSERT INTO owned_reconciliation_down_guard SELECT (SELECT COUNT(*) FROM Avatar)+(SELECT COUNT(*) FROM AvatarDetail)+(SELECT COUNT(*) FROM Holon)+(SELECT COUNT(*) FROM AvatarInventory)+(SELECT COUNT(*) FROM AvatarWalletStore)+(SELECT COUNT(*) FROM RefreshTokens)+(SELECT COUNT(*) FROM ProviderKey)+(SELECT COUNT(*) FROM AvatarChakras); DROP TABLE owned_reconciliation_down_guard;");
            migrationBuilder.DropForeignKey(
                name: "FK_ProviderKey_Avatar_AvatarModelId",
                table: "ProviderKey");

            migrationBuilder.DropForeignKey(
                name: "FK_RefreshTokens_Avatar_AvatarId",
                table: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "AvatarWalletStore");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RefreshTokens",
                table: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_AvatarId",
                table: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_ProviderKey_AvatarModelId",
                table: "ProviderKey");

            migrationBuilder.DropIndex(
                name: "IX_Holon_LocalProviderKey",
                table: "Holon");

            migrationBuilder.DropIndex(
                name: "IX_Avatar_LocalProviderKey",
                table: "Avatar");

            migrationBuilder.DropColumn(
                name: "StorageId",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "AvatarModelId",
                table: "ProviderKey");

            migrationBuilder.DropColumn(
                name: "LocalProviderKey",
                table: "Holon");

            migrationBuilder.DropColumn(
                name: "MetaDataJson",
                table: "Holon");

            migrationBuilder.DropColumn(
                name: "ProviderKeysJson",
                table: "Holon");

            migrationBuilder.DropColumn(
                name: "AcquiredOn",
                table: "AvatarInventory");

            migrationBuilder.DropColumn(
                name: "IsTradeable",
                table: "AvatarInventory");

            migrationBuilder.DropColumn(
                name: "IsUsable",
                table: "AvatarInventory");

            migrationBuilder.DropColumn(
                name: "ItemId",
                table: "AvatarInventory");

            migrationBuilder.DropColumn(
                name: "LastUsedOn",
                table: "AvatarInventory");

            migrationBuilder.DropColumn(
                name: "MaxQuantity",
                table: "AvatarInventory");

            migrationBuilder.DropColumn(
                name: "MetaDataJson",
                table: "AvatarInventory");

            migrationBuilder.DropColumn(
                name: "PropertiesJson",
                table: "AvatarInventory");

            migrationBuilder.DropColumn(
                name: "Rarity",
                table: "AvatarInventory");

            migrationBuilder.DropColumn(
                name: "Weight",
                table: "AvatarInventory");

            migrationBuilder.DropColumn(
                name: "MetaDataJson",
                table: "AvatarDetail");

            migrationBuilder.DropColumn(
                name: "LocalProviderKey",
                table: "Avatar");

            migrationBuilder.DropColumn(
                name: "MetaDataJson",
                table: "Avatar");

            migrationBuilder.DropColumn(
                name: "ParentHolonId",
                table: "Avatar");

            migrationBuilder.DropColumn(
                name: "ProviderKeysJson",
                table: "Avatar");

            migrationBuilder.DropColumn(
                name: "ProviderWalletsJson",
                table: "Avatar");

            migrationBuilder.RenameColumn(
                name: "NumberActiveAvatars",
                table: "SuperStar",
                newName: "NunmerActiveAvatars");

            migrationBuilder.RenameColumn(
                name: "NumberActiveAvatars",
                table: "Star",
                newName: "NunmerActiveAvatars");

            migrationBuilder.RenameColumn(
                name: "NumberActiveAvatars",
                table: "Planet",
                newName: "NunmerActiveAvatars");

            migrationBuilder.RenameColumn(
                name: "NumberActiveAvatars",
                table: "Moon",
                newName: "NunmerActiveAvatars");

            migrationBuilder.RenameColumn(
                name: "NumberActiveAvatars",
                table: "Meteroid",
                newName: "NunmerActiveAvatars");

            migrationBuilder.RenameColumn(
                name: "NumberActiveAvatars",
                table: "GreatGrandSuperStar",
                newName: "NunmerActiveAvatars");

            migrationBuilder.RenameColumn(
                name: "NumberActiveAvatars",
                table: "GrandSuperStar",
                newName: "NunmerActiveAvatars");

            migrationBuilder.RenameColumn(
                name: "NumberActiveAvatars",
                table: "Comet",
                newName: "NunmerActiveAvatars");

            migrationBuilder.RenameColumn(
                name: "Telekinesis",
                table: "AvatarSuperPowers",
                newName: "Telekineseis");

            migrationBuilder.RenameColumn(
                name: "Invulnerability",
                table: "AvatarSuperPowers",
                newName: "Invulerability");

            migrationBuilder.RenameColumn(
                name: "RangedCombat",
                table: "AvatarSkills",
                newName: "RangeCombat");

            migrationBuilder.RenameColumn(
                name: "MeleeCombat",
                table: "AvatarSkills",
                newName: "MelleeCombat");

            migrationBuilder.RenameColumn(
                name: "NumberActiveAvatars",
                table: "Asteroid",
                newName: "NunmerActiveAvatars");

            migrationBuilder.AddColumn<string>(
                name: "AvatarModelId",
                table: "RefreshTokens",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NftId",
                table: "AvatarInventory",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "ItemType",
                table: "AvatarInventory",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<string>(
                name: "GeoNFTId",
                table: "AvatarInventory",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<int>(
                name: "CreatedProviderType",
                table: "AvatarDetail",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CreatedOASISType",
                table: "AvatarDetail",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "YogaPose",
                table: "AvatarChakras",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Element",
                table: "AvatarChakras",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_RefreshTokens",
                table: "RefreshTokens",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_AvatarModelId",
                table: "RefreshTokens",
                column: "AvatarModelId");

            migrationBuilder.AddForeignKey(
                name: "FK_RefreshTokens_Avatar_AvatarModelId",
                table: "RefreshTokens",
                column: "AvatarModelId",
                principalTable: "Avatar",
                principalColumn: "Id");
        }
    }
}
