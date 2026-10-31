using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Migrations;

[DbContext(typeof(DataContext))]
[Migration("20261016000000_AddAvatarDetailTracking")]
public sealed class AddAvatarDetailTracking : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(name:"ActiveQuestId",table:"AvatarDetail",type:"TEXT",nullable:true);
        migrationBuilder.AddColumn<Guid>(name:"ActiveObjectiveId",table:"AvatarDetail",type:"TEXT",nullable:true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("ALTER TABLE AvatarDetail DROP COLUMN ActiveObjectiveId");
        migrationBuilder.Sql("ALTER TABLE AvatarDetail DROP COLUMN ActiveQuestId");
    }
}
