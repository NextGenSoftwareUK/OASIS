using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Migrations;

public partial class AddCosmicHolonRecoveryEvidence : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        foreach(var column in CosmicSqliteSchema.OutboxRecoveryColumns())migrationBuilder.Sql($"ALTER TABLE CosmicHolonOutbox ADD COLUMN {column.Key} {column.Value}");
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("CREATE TEMP TABLE cosmic_recovery_downgrade_guard(value INTEGER CHECK(value=0)); "
            +"INSERT INTO cosmic_recovery_downgrade_guard SELECT COUNT(*) FROM CosmicHolonOutbox WHERE RecoveryEvidenceJson IS NOT NULL OR RecoveryEvidenceDigest IS NOT NULL; DROP TABLE cosmic_recovery_downgrade_guard;");
        foreach(var column in CosmicSqliteSchema.OutboxRecoveryColumns().Keys.Reverse())migrationBuilder.Sql($"ALTER TABLE CosmicHolonOutbox DROP COLUMN {column}");
    }
}
