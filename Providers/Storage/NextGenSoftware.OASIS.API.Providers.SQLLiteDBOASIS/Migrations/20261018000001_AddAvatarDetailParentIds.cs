using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Migrations;

[DbContext(typeof(DataContext))]
[Migration("20261018000001_AddAvatarDetailParentIds")]
public sealed class AddAvatarDetailParentIds:Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        foreach(var name in new[]{"ParentHolonId","ParentHolonId2","ParentHolonId3"})
            migrationBuilder.AddColumn<Guid>(name:name,table:"AvatarDetail",type:"TEXT",nullable:false,defaultValue:Guid.Empty);
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("ALTER TABLE AvatarDetail DROP COLUMN ParentHolonId3");
        migrationBuilder.Sql("ALTER TABLE AvatarDetail DROP COLUMN ParentHolonId2");
        migrationBuilder.Sql("ALTER TABLE AvatarDetail DROP COLUMN ParentHolonId");
    }
}
