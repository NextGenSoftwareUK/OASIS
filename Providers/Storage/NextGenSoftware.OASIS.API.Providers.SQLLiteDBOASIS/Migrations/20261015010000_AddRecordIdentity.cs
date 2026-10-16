using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Migrations;

[DbContext(typeof(DataContext))]
[Migration("20261015010000_AddRecordIdentity")]
public sealed class AddRecordIdentity : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        foreach(var table in new[] {"Holon","Avatar","AvatarDetail"})
        {
            migrationBuilder.AddColumn<Guid>(name:"VersionId",table:table,type:"TEXT",nullable:false,defaultValue:Guid.Empty);
            migrationBuilder.AddColumn<Guid>(name:"PreviousVersionId",table:table,type:"TEXT",nullable:false,defaultValue:Guid.Empty);
            migrationBuilder.AddColumn<string>(name:"CustomHolonType",table:table,type:"TEXT",nullable:true);
        }
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        foreach(var table in new[] {"Holon","Avatar","AvatarDetail"})
            foreach(var column in new[] {"VersionId","PreviousVersionId","CustomHolonType"})
                migrationBuilder.Sql($"ALTER TABLE \"{table}\" DROP COLUMN \"{column}\"");
    }
}
