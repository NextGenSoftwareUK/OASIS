using Microsoft.EntityFrameworkCore.Migrations;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Migrations;

public partial class AddHolonPersistentValues : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
        =>migrationBuilder.AddColumn<string>(name:"PersistentValueJson",table:"Holon",type:"TEXT",nullable:false,defaultValue:"{}");
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("CREATE TEMP TABLE cosmic_extension_downgrade_guard(value INTEGER CHECK(value=0)); "
            +"INSERT INTO cosmic_extension_downgrade_guard SELECT COUNT(*) FROM Holon WHERE PersistentValueJson <> '{}'; "
            +"DROP TABLE cosmic_extension_downgrade_guard;");
        migrationBuilder.DropColumn(name:"PersistentValueJson",table:"Holon");
    }
}
