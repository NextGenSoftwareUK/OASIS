using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Migrations;

[DbContext(typeof(DataContext))]
[Migration("20261029180000_AddCosmicMigrationReplicaAdoption")]
public sealed class AddCosmicMigrationReplicaAdoption : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql(CosmicSqliteSchema.CreateMigrationReplicaAdoption);
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("CREATE TEMP TABLE cosmic_adoption_downgrade_guard (Count INTEGER CHECK(Count=0)); "
            + "INSERT INTO cosmic_adoption_downgrade_guard SELECT COUNT(*) FROM CosmicMigrationReplicaAdoption; "
            + "DROP TABLE cosmic_adoption_downgrade_guard; DROP TABLE CosmicMigrationReplicaAdoption;");
    }
}
