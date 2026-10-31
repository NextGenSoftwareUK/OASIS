using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Migrations;
[DbContext(typeof(DataContext))]
[Migration("20261020040000_AddCosmicHolonRevisionParentIndex")]
public sealed class AddCosmicHolonRevisionParentIndex : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)=>migrationBuilder.Sql(CosmicSqliteSchema.CreateRevisionParentIndex);
    protected override void Down(MigrationBuilder migrationBuilder)=>migrationBuilder.Sql("DROP INDEX IX_CosmicHolonRevision_ParentVersion");
}
