using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Migrations;

[DbContext(typeof(DataContext))]
[Migration("20261031180000_AddReplicaInitializationCompletion")]
public sealed class AddReplicaInitializationCompletion : Migration
{
    protected override void Up(MigrationBuilder builder)
        =>builder.Sql(CosmicSqliteSchema.CreateOutboxInitializationCompletionTable);

    protected override void Down(MigrationBuilder builder)
    {
        builder.Sql("CREATE TEMP TABLE cosmic_initialization_downgrade_guard(value INTEGER CHECK(value=0)); "
            +"INSERT INTO cosmic_initialization_downgrade_guard SELECT COUNT(*) FROM CosmicHolonOutboxInitializationCompletion; "
            +"DROP TABLE cosmic_initialization_downgrade_guard; DROP TABLE CosmicHolonOutboxInitializationCompletion;");
    }
}
