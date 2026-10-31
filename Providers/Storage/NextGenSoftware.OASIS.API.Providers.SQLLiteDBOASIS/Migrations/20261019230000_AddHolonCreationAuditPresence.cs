using Microsoft.EntityFrameworkCore.Migrations;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Migrations;

public partial class AddHolonCreationAuditPresence : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(name:"HasCreatedProviderType",table:"Holon",type:"INTEGER",nullable:false,defaultValue:true);
        migrationBuilder.AddColumn<bool>(name:"HasCreatedOASISType",table:"Holon",type:"INTEGER",nullable:false,defaultValue:true);
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("CREATE TEMP TABLE cosmic_audit_presence_guard(value INTEGER CHECK(value=0)); "
            +"INSERT INTO cosmic_audit_presence_guard SELECT COUNT(*) FROM Holon WHERE NOT HasCreatedProviderType OR NOT HasCreatedOASISType; "
            +"DROP TABLE cosmic_audit_presence_guard;");
        migrationBuilder.DropColumn("HasCreatedProviderType","Holon");migrationBuilder.DropColumn("HasCreatedOASISType","Holon");
    }
}
