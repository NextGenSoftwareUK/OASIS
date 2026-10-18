using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Migrations;

[DbContext(typeof(DataContext))]
[Migration("20261016010000_AddInventoryMedia")]
public sealed class AddInventoryMedia : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<byte[]>(name:"Image2D",table:"AvatarInventory",type:"BLOB",nullable:true);
        migrationBuilder.AddColumn<byte[]>(name:"Object3D",table:"AvatarInventory",type:"BLOB",nullable:true);
        migrationBuilder.AddColumn<string>(name:"Image2DURI",table:"AvatarInventory",type:"TEXT",nullable:true);
        migrationBuilder.AddColumn<string>(name:"Object3DURI",table:"AvatarInventory",type:"TEXT",nullable:true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        foreach(var column in new[] {"Image2D","Object3D","Image2DURI","Object3DURI"})
            migrationBuilder.Sql($"ALTER TABLE AvatarInventory DROP COLUMN {column}");
    }
}
