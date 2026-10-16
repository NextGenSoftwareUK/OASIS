using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Migrations;

[DbContext(typeof(DataContext))]
[Migration("20261015000000_AddRecordVisibility")]
public sealed class AddRecordVisibility : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        foreach(var table in new[] {"Holon","Avatar","AvatarDetail"})
            migrationBuilder.AddColumn<bool>(name:"IsPublic",table:table,type:"INTEGER",nullable:false,defaultValue:false);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // The pinned SQLite runtime supports native DROP COLUMN. No table rebuild
        // is needed for these unindexed columns, preserving unrelated row fields.
        foreach(var table in new[] {"Holon","Avatar","AvatarDetail"})
            migrationBuilder.Sql($"ALTER TABLE \"{table}\" DROP COLUMN \"IsPublic\"");
    }
}
