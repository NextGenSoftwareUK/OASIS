using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class v13 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RwaTokenOwnershipTransfer_RwaTokens_RwaTokenId",
                table: "RwaTokenOwnershipTransfer");

            migrationBuilder.DropForeignKey(
                name: "FK_RwaTokenOwnershipTransfer_VirtualAccounts_BuyerWalletId",
                table: "RwaTokenOwnershipTransfer");

            migrationBuilder.DropForeignKey(
                name: "FK_RwaTokenOwnershipTransfer_VirtualAccounts_SellerWalletId",
                table: "RwaTokenOwnershipTransfer");

            migrationBuilder.DropForeignKey(
                name: "FK_RwaTokenPriceHistory_RwaTokens_RwaTokenId",
                table: "RwaTokenPriceHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_RwaTokenPriceHistory_VirtualAccounts_OwnerId",
                table: "RwaTokenPriceHistory");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RwaTokenPriceHistory",
                table: "RwaTokenPriceHistory");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RwaTokenOwnershipTransfer",
                table: "RwaTokenOwnershipTransfer");

            migrationBuilder.RenameTable(
                name: "RwaTokenPriceHistory",
                newName: "RwaTokenPriceHistories");

            migrationBuilder.RenameTable(
                name: "RwaTokenOwnershipTransfer",
                newName: "RwaTokenOwnershipTransfers");

            migrationBuilder.RenameIndex(
                name: "IX_RwaTokenPriceHistory_RwaTokenId",
                table: "RwaTokenPriceHistories",
                newName: "IX_RwaTokenPriceHistories_RwaTokenId");

            migrationBuilder.RenameIndex(
                name: "IX_RwaTokenPriceHistory_OwnerId",
                table: "RwaTokenPriceHistories",
                newName: "IX_RwaTokenPriceHistories_OwnerId");

            migrationBuilder.RenameIndex(
                name: "IX_RwaTokenOwnershipTransfer_SellerWalletId",
                table: "RwaTokenOwnershipTransfers",
                newName: "IX_RwaTokenOwnershipTransfers_SellerWalletId");

            migrationBuilder.RenameIndex(
                name: "IX_RwaTokenOwnershipTransfer_RwaTokenId",
                table: "RwaTokenOwnershipTransfers",
                newName: "IX_RwaTokenOwnershipTransfers_RwaTokenId");

            migrationBuilder.RenameIndex(
                name: "IX_RwaTokenOwnershipTransfer_BuyerWalletId",
                table: "RwaTokenOwnershipTransfers",
                newName: "IX_RwaTokenOwnershipTransfers_BuyerWalletId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RwaTokenPriceHistories",
                table: "RwaTokenPriceHistories",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RwaTokenOwnershipTransfers",
                table: "RwaTokenOwnershipTransfers",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RwaTokenOwnershipTransfers_RwaTokens_RwaTokenId",
                table: "RwaTokenOwnershipTransfers",
                column: "RwaTokenId",
                principalTable: "RwaTokens",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RwaTokenOwnershipTransfers_VirtualAccounts_BuyerWalletId",
                table: "RwaTokenOwnershipTransfers",
                column: "BuyerWalletId",
                principalTable: "VirtualAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RwaTokenOwnershipTransfers_VirtualAccounts_SellerWalletId",
                table: "RwaTokenOwnershipTransfers",
                column: "SellerWalletId",
                principalTable: "VirtualAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RwaTokenPriceHistories_RwaTokens_RwaTokenId",
                table: "RwaTokenPriceHistories",
                column: "RwaTokenId",
                principalTable: "RwaTokens",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RwaTokenPriceHistories_VirtualAccounts_OwnerId",
                table: "RwaTokenPriceHistories",
                column: "OwnerId",
                principalTable: "VirtualAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RwaTokenOwnershipTransfers_RwaTokens_RwaTokenId",
                table: "RwaTokenOwnershipTransfers");

            migrationBuilder.DropForeignKey(
                name: "FK_RwaTokenOwnershipTransfers_VirtualAccounts_BuyerWalletId",
                table: "RwaTokenOwnershipTransfers");

            migrationBuilder.DropForeignKey(
                name: "FK_RwaTokenOwnershipTransfers_VirtualAccounts_SellerWalletId",
                table: "RwaTokenOwnershipTransfers");

            migrationBuilder.DropForeignKey(
                name: "FK_RwaTokenPriceHistories_RwaTokens_RwaTokenId",
                table: "RwaTokenPriceHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_RwaTokenPriceHistories_VirtualAccounts_OwnerId",
                table: "RwaTokenPriceHistories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RwaTokenPriceHistories",
                table: "RwaTokenPriceHistories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RwaTokenOwnershipTransfers",
                table: "RwaTokenOwnershipTransfers");

            migrationBuilder.RenameTable(
                name: "RwaTokenPriceHistories",
                newName: "RwaTokenPriceHistory");

            migrationBuilder.RenameTable(
                name: "RwaTokenOwnershipTransfers",
                newName: "RwaTokenOwnershipTransfer");

            migrationBuilder.RenameIndex(
                name: "IX_RwaTokenPriceHistories_RwaTokenId",
                table: "RwaTokenPriceHistory",
                newName: "IX_RwaTokenPriceHistory_RwaTokenId");

            migrationBuilder.RenameIndex(
                name: "IX_RwaTokenPriceHistories_OwnerId",
                table: "RwaTokenPriceHistory",
                newName: "IX_RwaTokenPriceHistory_OwnerId");

            migrationBuilder.RenameIndex(
                name: "IX_RwaTokenOwnershipTransfers_SellerWalletId",
                table: "RwaTokenOwnershipTransfer",
                newName: "IX_RwaTokenOwnershipTransfer_SellerWalletId");

            migrationBuilder.RenameIndex(
                name: "IX_RwaTokenOwnershipTransfers_RwaTokenId",
                table: "RwaTokenOwnershipTransfer",
                newName: "IX_RwaTokenOwnershipTransfer_RwaTokenId");

            migrationBuilder.RenameIndex(
                name: "IX_RwaTokenOwnershipTransfers_BuyerWalletId",
                table: "RwaTokenOwnershipTransfer",
                newName: "IX_RwaTokenOwnershipTransfer_BuyerWalletId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RwaTokenPriceHistory",
                table: "RwaTokenPriceHistory",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RwaTokenOwnershipTransfer",
                table: "RwaTokenOwnershipTransfer",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RwaTokenOwnershipTransfer_RwaTokens_RwaTokenId",
                table: "RwaTokenOwnershipTransfer",
                column: "RwaTokenId",
                principalTable: "RwaTokens",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RwaTokenOwnershipTransfer_VirtualAccounts_BuyerWalletId",
                table: "RwaTokenOwnershipTransfer",
                column: "BuyerWalletId",
                principalTable: "VirtualAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RwaTokenOwnershipTransfer_VirtualAccounts_SellerWalletId",
                table: "RwaTokenOwnershipTransfer",
                column: "SellerWalletId",
                principalTable: "VirtualAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RwaTokenPriceHistory_RwaTokens_RwaTokenId",
                table: "RwaTokenPriceHistory",
                column: "RwaTokenId",
                principalTable: "RwaTokens",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RwaTokenPriceHistory_VirtualAccounts_OwnerId",
                table: "RwaTokenPriceHistory",
                column: "OwnerId",
                principalTable: "VirtualAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
