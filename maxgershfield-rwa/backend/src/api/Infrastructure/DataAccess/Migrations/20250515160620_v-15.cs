using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class v15 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RwaTokenOwnershipTransfers_VirtualAccounts_BuyerWalletId",
                table: "RwaTokenOwnershipTransfers");

            migrationBuilder.AddForeignKey(
                name: "FK_RwaTokenOwnershipTransfers_WalletLinkedAccounts_BuyerWallet~",
                table: "RwaTokenOwnershipTransfers",
                column: "BuyerWalletId",
                principalTable: "WalletLinkedAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RwaTokenOwnershipTransfers_WalletLinkedAccounts_BuyerWallet~",
                table: "RwaTokenOwnershipTransfers");

            migrationBuilder.AddForeignKey(
                name: "FK_RwaTokenOwnershipTransfers_VirtualAccounts_BuyerWalletId",
                table: "RwaTokenOwnershipTransfers",
                column: "BuyerWalletId",
                principalTable: "VirtualAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
