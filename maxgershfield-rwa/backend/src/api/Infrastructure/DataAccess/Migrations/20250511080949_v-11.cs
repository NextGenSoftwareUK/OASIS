using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class v11 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RwaTokenOwnershipTransfer",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RwaTokenId = table.Column<Guid>(type: "uuid", nullable: false),
                    BuyerWalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    SellerWalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    Price = table.Column<decimal>(type: "numeric", nullable: false),
                    TransactionDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TransactionHash = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedByIp = table.Column<string>(type: "text", nullable: true),
                    UpdatedByIp = table.Column<List<string>>(type: "text[]", nullable: true),
                    DeletedByIp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RwaTokenOwnershipTransfer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RwaTokenOwnershipTransfer_RwaTokens_RwaTokenId",
                        column: x => x.RwaTokenId,
                        principalTable: "RwaTokens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RwaTokenOwnershipTransfer_VirtualAccounts_BuyerWalletId",
                        column: x => x.BuyerWalletId,
                        principalTable: "VirtualAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RwaTokenOwnershipTransfer_VirtualAccounts_SellerWalletId",
                        column: x => x.SellerWalletId,
                        principalTable: "VirtualAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RwaTokenPriceHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RwaTokenId = table.Column<Guid>(type: "uuid", nullable: false),
                    OldPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    NewPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CurrentOwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedByIp = table.Column<string>(type: "text", nullable: true),
                    UpdatedByIp = table.Column<List<string>>(type: "text[]", nullable: true),
                    DeletedByIp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RwaTokenPriceHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RwaTokenPriceHistory_RwaTokens_RwaTokenId",
                        column: x => x.RwaTokenId,
                        principalTable: "RwaTokens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RwaTokenPriceHistory_VirtualAccounts_CurrentOwnerId",
                        column: x => x.CurrentOwnerId,
                        principalTable: "VirtualAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RwaTokenOwnershipTransfer_BuyerWalletId",
                table: "RwaTokenOwnershipTransfer",
                column: "BuyerWalletId");

            migrationBuilder.CreateIndex(
                name: "IX_RwaTokenOwnershipTransfer_RwaTokenId",
                table: "RwaTokenOwnershipTransfer",
                column: "RwaTokenId");

            migrationBuilder.CreateIndex(
                name: "IX_RwaTokenOwnershipTransfer_SellerWalletId",
                table: "RwaTokenOwnershipTransfer",
                column: "SellerWalletId");

            migrationBuilder.CreateIndex(
                name: "IX_RwaTokenPriceHistory_CurrentOwnerId",
                table: "RwaTokenPriceHistory",
                column: "CurrentOwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_RwaTokenPriceHistory_RwaTokenId",
                table: "RwaTokenPriceHistory",
                column: "RwaTokenId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RwaTokenOwnershipTransfer");

            migrationBuilder.DropTable(
                name: "RwaTokenPriceHistory");
        }
    }
}
