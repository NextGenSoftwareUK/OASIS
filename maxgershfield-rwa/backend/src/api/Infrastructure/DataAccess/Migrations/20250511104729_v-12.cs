using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class v12 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RwaTokenPriceHistory_VirtualAccounts_CurrentOwnerId",
                table: "RwaTokenPriceHistory");

            migrationBuilder.RenameColumn(
                name: "CurrentOwnerId",
                table: "RwaTokenPriceHistory",
                newName: "OwnerId");

            migrationBuilder.RenameIndex(
                name: "IX_RwaTokenPriceHistory_CurrentOwnerId",
                table: "RwaTokenPriceHistory",
                newName: "IX_RwaTokenPriceHistory_OwnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_RwaTokenPriceHistory_VirtualAccounts_OwnerId",
                table: "RwaTokenPriceHistory",
                column: "OwnerId",
                principalTable: "VirtualAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RwaTokenPriceHistory_VirtualAccounts_OwnerId",
                table: "RwaTokenPriceHistory");

            migrationBuilder.RenameColumn(
                name: "OwnerId",
                table: "RwaTokenPriceHistory",
                newName: "CurrentOwnerId");

            migrationBuilder.RenameIndex(
                name: "IX_RwaTokenPriceHistory_OwnerId",
                table: "RwaTokenPriceHistory",
                newName: "IX_RwaTokenPriceHistory_CurrentOwnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_RwaTokenPriceHistory_VirtualAccounts_CurrentOwnerId",
                table: "RwaTokenPriceHistory",
                column: "CurrentOwnerId",
                principalTable: "VirtualAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
