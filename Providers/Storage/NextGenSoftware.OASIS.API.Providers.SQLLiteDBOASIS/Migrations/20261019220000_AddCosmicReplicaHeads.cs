using Microsoft.EntityFrameworkCore.Migrations;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Migrations;

public partial class AddCosmicReplicaHeads : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)=>migrationBuilder.Sql(CosmicSqliteSchema.CreateReplicaHeadTable);
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("CREATE TEMP TABLE cosmic_replica_downgrade_guard(value INTEGER CHECK(value=0)); "
            +"INSERT INTO cosmic_replica_downgrade_guard SELECT COUNT(*) FROM CosmicReplicaHead; "
            +"DROP TABLE cosmic_replica_downgrade_guard;");
        migrationBuilder.DropTable("CosmicReplicaHead");
    }
}
