using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Migrations;

[DbContext(typeof(DataContext))]
[Migration("20261020010000_AddCosmicHolonRevisionArchive")]
public sealed class AddCosmicHolonRevisionArchive : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)=>migrationBuilder.Sql(CosmicSqliteSchema.CreateRevisionArchive);
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("CREATE TEMP TABLE cosmic_revision_downgrade_guard(value INTEGER CHECK(value=0)); "
            +"INSERT INTO cosmic_revision_downgrade_guard SELECT COUNT(*) FROM CosmicHolonRevision; "
            +"DROP TABLE cosmic_revision_downgrade_guard;");
        migrationBuilder.DropTable("CosmicHolonRevision");
    }
}
