using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Migrations;

[DbContext(typeof(DataContext))]
[Migration("20261018000005_AddAvatarDetailCelestialIds")]
public sealed partial class AddAvatarDetailCelestialIds:Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
        => migrationBuilder.AddColumn<string>(name:"CelestialParentIdsJson",table:"AvatarDetail",type:"TEXT",nullable:false,defaultValue:"{}");
    protected override void Down(MigrationBuilder migrationBuilder)
        => migrationBuilder.Sql("ALTER TABLE AvatarDetail DROP COLUMN CelestialParentIdsJson");
}
