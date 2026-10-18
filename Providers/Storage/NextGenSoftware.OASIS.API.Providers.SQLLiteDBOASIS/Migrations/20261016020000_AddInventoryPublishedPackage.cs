using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Migrations;

[DbContext(typeof(DataContext))]
[Migration("20261016020000_AddInventoryPublishedPackage")]
public sealed class AddInventoryPublishedPackage : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
        => migrationBuilder.AddColumn<byte[]>(name:"PublishedSTARNETHolon",table:"AvatarInventory",type:"BLOB",nullable:true);

    protected override void Down(MigrationBuilder migrationBuilder)
        => migrationBuilder.Sql("ALTER TABLE AvatarInventory DROP COLUMN PublishedSTARNETHolon");
}
