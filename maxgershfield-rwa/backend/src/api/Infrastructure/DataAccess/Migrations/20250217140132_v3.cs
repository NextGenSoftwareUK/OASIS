using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class v3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NetworkTokens_Network_NetworkId",
                table: "NetworkTokens");

            migrationBuilder.DropForeignKey(
                name: "FK_VirtualAccounts_Network_NetworkId",
                table: "VirtualAccounts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Network",
                table: "Network");

            migrationBuilder.RenameTable(
                name: "Network",
                newName: "Networks");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Networks",
                table: "Networks",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_NetworkTokens_Networks_NetworkId",
                table: "NetworkTokens",
                column: "NetworkId",
                principalTable: "Networks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_VirtualAccounts_Networks_NetworkId",
                table: "VirtualAccounts",
                column: "NetworkId",
                principalTable: "Networks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NetworkTokens_Networks_NetworkId",
                table: "NetworkTokens");

            migrationBuilder.DropForeignKey(
                name: "FK_VirtualAccounts_Networks_NetworkId",
                table: "VirtualAccounts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Networks",
                table: "Networks");

            migrationBuilder.RenameTable(
                name: "Networks",
                newName: "Network");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Network",
                table: "Network",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_NetworkTokens_Network_NetworkId",
                table: "NetworkTokens",
                column: "NetworkId",
                principalTable: "Network",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_VirtualAccounts_Network_NetworkId",
                table: "VirtualAccounts",
                column: "NetworkId",
                principalTable: "Network",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
