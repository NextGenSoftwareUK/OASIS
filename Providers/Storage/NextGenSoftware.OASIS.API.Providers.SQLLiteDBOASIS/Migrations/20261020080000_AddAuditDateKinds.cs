using Microsoft.EntityFrameworkCore.Migrations;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Migrations;

public partial class AddAuditDateKinds : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        foreach(var table in new[]{"Holon","Avatar","AvatarDetail"})
            foreach(var column in new[]{"CreatedDateKind","ModifiedDateKind","DeletedDateKind"})
                migrationBuilder.AddColumn<int>(name:column,table:table,type:"INTEGER",nullable:false,defaultValue:0);
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("CREATE TEMP TABLE cosmic_audit_kind_guard(value INTEGER CHECK(value=0));");
        foreach(var table in new[]{"Holon","Avatar","AvatarDetail"})
            migrationBuilder.Sql("INSERT INTO cosmic_audit_kind_guard SELECT COUNT(*) FROM "+table+" WHERE CreatedDateKind<>0 OR ModifiedDateKind<>0 OR DeletedDateKind<>0;");
        migrationBuilder.Sql("DROP TABLE cosmic_audit_kind_guard;");
        foreach(var table in new[]{"Holon","Avatar","AvatarDetail"})
            foreach(var column in new[]{"CreatedDateKind","ModifiedDateKind","DeletedDateKind"})
                migrationBuilder.DropColumn(column,table);
    }
}
