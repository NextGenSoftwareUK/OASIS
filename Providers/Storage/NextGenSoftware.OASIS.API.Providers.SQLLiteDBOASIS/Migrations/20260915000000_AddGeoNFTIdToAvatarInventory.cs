using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;

#nullable disable

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Migrations
{
    /// <summary>Adds the optional GeoNFT identity independently of an inventory item's functional category.</summary>
    [DbContext(typeof(DataContext))]
    [Migration("20260915000000_AddGeoNFTIdToAvatarInventory")]
    public partial class AddGeoNFTIdToAvatarInventory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GeoNFTId",
                table: "AvatarInventory",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "GeoNFTId", table: "AvatarInventory");
        }
    }
}
