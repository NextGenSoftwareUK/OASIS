using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Migrations;

[DbContext(typeof(DataContext))]
[Migration("20261028183000_AddCosmicDestinationBindings")]
public sealed class AddCosmicDestinationBindings : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)=>migrationBuilder.Sql(CosmicSqliteSchema.CreateDestinationBindings);
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("CREATE TEMP TABLE cosmic_destination_downgrade_guard(value INTEGER CHECK(value=0)); "
            +"INSERT INTO cosmic_destination_downgrade_guard SELECT COUNT(*) FROM CosmicDestinationBinding; "
            +"DROP TABLE cosmic_destination_downgrade_guard;");
        migrationBuilder.DropTable("CosmicDestinationBinding");
    }
}
