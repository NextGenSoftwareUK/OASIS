using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Migrations;

[DbContext(typeof(DataContext))]
[Migration("20261020020000_AddCosmicHolonRevisionKeyIndex")]
public sealed class AddCosmicHolonRevisionKeyIndex : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)=>migrationBuilder.Sql(CosmicSqliteSchema.CreateRevisionKeyIndex);
    protected override void Down(MigrationBuilder migrationBuilder)=>migrationBuilder.Sql("DROP INDEX IX_CosmicHolonRevision_ProviderKeyVersion");
}
