using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class v17 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RwaTokens_VirtualAccounts_VirtualAccountId",
                table: "RwaTokens");

            migrationBuilder.AlterColumn<Guid>(
                name: "VirtualAccountId",
                table: "RwaTokens",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "WalletLinkedAccountId",
                table: "RwaTokens",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RwaTokens_WalletLinkedAccountId",
                table: "RwaTokens",
                column: "WalletLinkedAccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_RwaTokens_VirtualAccounts_VirtualAccountId",
                table: "RwaTokens",
                column: "VirtualAccountId",
                principalTable: "VirtualAccounts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RwaTokens_WalletLinkedAccounts_WalletLinkedAccountId",
                table: "RwaTokens",
                column: "WalletLinkedAccountId",
                principalTable: "WalletLinkedAccounts",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RwaTokens_VirtualAccounts_VirtualAccountId",
                table: "RwaTokens");

            migrationBuilder.DropForeignKey(
                name: "FK_RwaTokens_WalletLinkedAccounts_WalletLinkedAccountId",
                table: "RwaTokens");

            migrationBuilder.DropIndex(
                name: "IX_RwaTokens_WalletLinkedAccountId",
                table: "RwaTokens");

            migrationBuilder.DropColumn(
                name: "WalletLinkedAccountId",
                table: "RwaTokens");

            migrationBuilder.AlterColumn<Guid>(
                name: "VirtualAccountId",
                table: "RwaTokens",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_RwaTokens_VirtualAccounts_VirtualAccountId",
                table: "RwaTokens",
                column: "VirtualAccountId",
                principalTable: "VirtualAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
