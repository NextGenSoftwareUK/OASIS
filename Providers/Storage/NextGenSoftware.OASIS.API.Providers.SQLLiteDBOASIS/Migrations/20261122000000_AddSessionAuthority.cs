using Microsoft.EntityFrameworkCore.Migrations;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Migrations;

public partial class AddSessionAuthority : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.AddColumn<string>(name: "AuthorityProvider", table: "RefreshTokens", type: "TEXT", nullable: true);

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("CREATE TEMP TABLE oasis_session_authority_guard(value INTEGER CHECK(value=0));");
        migrationBuilder.Sql("INSERT INTO oasis_session_authority_guard SELECT COUNT(*) FROM RefreshTokens WHERE AuthorityProvider IS NOT NULL;");
        migrationBuilder.Sql("DROP TABLE oasis_session_authority_guard;");
        migrationBuilder.DropColumn(name: "AuthorityProvider", table: "RefreshTokens");
    }
}
