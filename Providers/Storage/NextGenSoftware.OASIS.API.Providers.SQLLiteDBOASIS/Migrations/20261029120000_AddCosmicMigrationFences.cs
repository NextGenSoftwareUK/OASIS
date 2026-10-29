using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Migrations;

[DbContext(typeof(DataContext))]
[Migration("20261029120000_AddCosmicMigrationFences")]
public sealed class AddCosmicMigrationFences : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql(CosmicSqliteSchema.CreateMigrationFence);
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("CREATE TEMP TABLE cosmic_fence_downgrade_guard (Count INTEGER CHECK(Count=0)); "
            + "INSERT INTO cosmic_fence_downgrade_guard SELECT COUNT(*) FROM CosmicMigrationFence; "
            + "DROP TABLE cosmic_fence_downgrade_guard;");
        migrationBuilder.DropTable("CosmicMigrationFence");
    }
}
