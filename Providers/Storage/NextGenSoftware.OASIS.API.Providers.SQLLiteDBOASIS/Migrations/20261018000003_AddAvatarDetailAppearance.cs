using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Migrations;

[DbContext(typeof(DataContext))]
[Migration("20261018000003_AddAvatarDetailAppearance")]
public sealed class AddAvatarDetailAppearance:Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name:"Portrait",table:"AvatarDetail",type:"TEXT",nullable:true);
        migrationBuilder.AddColumn<string>(name:"UmaJson",table:"AvatarDetail",type:"TEXT",nullable:true);
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("ALTER TABLE AvatarDetail DROP COLUMN UmaJson");
        migrationBuilder.Sql("ALTER TABLE AvatarDetail DROP COLUMN Portrait");
    }
}
