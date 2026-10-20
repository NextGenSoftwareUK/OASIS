using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Migrations;

public partial class AddHolonProviderRevision : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(name: "ProviderRevisionId", table: "Holon", type: "TEXT", nullable: false, defaultValue: Guid.Empty);
        migrationBuilder.Sql(CosmicSqliteSchema.CreateReceiptTable);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Run rejection inside the migration transaction, so EF releases its native migration lock.
        // No record observation or replay receipt may be discarded by a downgrade.
        migrationBuilder.Sql("CREATE TEMP TABLE cosmic_revision_downgrade_guard(value INTEGER CHECK(value=0)); "
            + "INSERT INTO cosmic_revision_downgrade_guard SELECT COUNT(*) FROM Holon; "
            + "INSERT INTO cosmic_revision_downgrade_guard SELECT COUNT(*) FROM CosmicWriteReceipt; "
            + "DROP TABLE cosmic_revision_downgrade_guard;");
        migrationBuilder.DropColumn(name: "ProviderRevisionId", table: "Holon");
    }
}
