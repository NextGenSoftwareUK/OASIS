using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Migrations
{
    /// <summary>Adds NftId, GameSource, ItemType, Stack to AvatarInventory so backend persists and returns NftId for [NFT] prefix in Doom/Quake.</summary>
    public partial class AddNftIdAndInventoryFieldsToAvatarInventory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GameSource",
                table: "AvatarInventory",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HolonType",
                table: "AvatarInventory",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ItemType",
                table: "AvatarInventory",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NftId",
                table: "AvatarInventory",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Stack",
                table: "AvatarInventory",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "GameSource", table: "AvatarInventory");
            migrationBuilder.DropColumn(name: "HolonType", table: "AvatarInventory");
            migrationBuilder.DropColumn(name: "ItemType", table: "AvatarInventory");
            migrationBuilder.DropColumn(name: "NftId", table: "AvatarInventory");
            migrationBuilder.DropColumn(name: "Stack", table: "AvatarInventory");
        }
    }
}
