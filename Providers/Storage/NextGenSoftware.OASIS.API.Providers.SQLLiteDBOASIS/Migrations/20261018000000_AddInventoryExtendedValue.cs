using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Migrations;

[DbContext(typeof(DataContext))]
[Migration("20261018000000_AddInventoryExtendedValue")]
public sealed class AddInventoryExtendedValue:Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
        => migrationBuilder.AddColumn<string>(name:"ExtendedValueJson",table:"AvatarInventory",type:"TEXT",nullable:false,defaultValue:"{}");

    protected override void Down(MigrationBuilder migrationBuilder)
        => migrationBuilder.Sql("ALTER TABLE AvatarInventory DROP COLUMN ExtendedValueJson");
}
