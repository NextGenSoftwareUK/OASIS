using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Migrations;

[DbContext(typeof(DataContext))]
[Migration("20261029160000_AddCosmicMigrationMutationDigest")]
public sealed class AddCosmicMigrationMutationDigest : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql(CosmicSqliteSchema.AddMigrationMutationDigest);
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("CREATE TEMP TABLE cosmic_mutation_downgrade_guard (Count INTEGER CHECK(Count=0)); "
            + "INSERT INTO cosmic_mutation_downgrade_guard SELECT COUNT(*) FROM CosmicMigrationFence WHERE MutationDigest IS NOT NULL; "
            + "DROP TABLE cosmic_mutation_downgrade_guard; ALTER TABLE CosmicMigrationFence DROP COLUMN MutationDigest;");
    }
}
