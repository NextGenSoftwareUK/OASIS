using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Migrations;

[DbContext(typeof(DataContext))]
[Migration("20261020050000_AddCosmicWritePreparation")]
public sealed partial class AddCosmicWritePreparation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql(CosmicSqliteSchema.CreatePreparationTable);
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("CREATE TEMP TABLE cosmic_preparation_downgrade_guard(value INTEGER CHECK(value=0)); "
            + "INSERT INTO cosmic_preparation_downgrade_guard SELECT COUNT(*) FROM CosmicWritePreparation; "
            + "DROP TABLE cosmic_preparation_downgrade_guard;");
        migrationBuilder.DropTable("CosmicWritePreparation");
    }
}
