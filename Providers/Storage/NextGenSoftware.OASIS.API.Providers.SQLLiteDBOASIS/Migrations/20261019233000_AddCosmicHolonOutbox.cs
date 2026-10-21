using Microsoft.EntityFrameworkCore.Migrations;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Migrations;

public partial class AddCosmicHolonOutbox : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)=>migrationBuilder.Sql(CosmicSqliteSchema.CreateOutboxTable);
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("CREATE TEMP TABLE cosmic_outbox_downgrade_guard(value INTEGER CHECK(value=0)); "
            +"INSERT INTO cosmic_outbox_downgrade_guard SELECT COUNT(*) FROM CosmicHolonOutbox; "
            +"DROP TABLE cosmic_outbox_downgrade_guard;");
        migrationBuilder.DropTable("CosmicHolonOutbox");
    }
}
