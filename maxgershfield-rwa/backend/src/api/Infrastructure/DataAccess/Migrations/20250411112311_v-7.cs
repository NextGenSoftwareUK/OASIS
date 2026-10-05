using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class v7 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WalletLinkedAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    NetworkId = table.Column<Guid>(type: "uuid", nullable: false),
                    PublicKey = table.Column<string>(type: "text", nullable: false),
                    LinkedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("PK_WalletLinkedAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WalletLinkedAccounts_Networks_NetworkId",
                        column: x => x.NetworkId,
                        principalTable: "Networks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WalletLinkedAccounts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VirtualAccounts_Address",
                table: "VirtualAccounts",
                column: "Address",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VirtualAccounts_PrivateKey",
                table: "VirtualAccounts",
                column: "PrivateKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VirtualAccounts_PublicKey",
                table: "VirtualAccounts",
                column: "PublicKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VirtualAccounts_SeedPhrase",
                table: "VirtualAccounts",
                column: "SeedPhrase",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email_PhoneNumber_UserName",
                table: "Users",
                columns: new[] { "Email", "PhoneNumber", "UserName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_PhoneNumber",
                table: "Users",
                column: "PhoneNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_UserName",
                table: "Users",
                column: "UserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Name",
                table: "Roles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NetworkTokens_Symbol",
                table: "NetworkTokens",
                column: "Symbol",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Networks_Name",
                table: "Networks",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WalletLinkedAccounts_NetworkId",
                table: "WalletLinkedAccounts",
                column: "NetworkId");

            migrationBuilder.CreateIndex(
                name: "IX_WalletLinkedAccounts_PublicKey",
                table: "WalletLinkedAccounts",
                column: "PublicKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WalletLinkedAccounts_UserId",
                table: "WalletLinkedAccounts",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WalletLinkedAccounts");

            migrationBuilder.DropIndex(
                name: "IX_VirtualAccounts_Address",
                table: "VirtualAccounts");

            migrationBuilder.DropIndex(
                name: "IX_VirtualAccounts_PrivateKey",
                table: "VirtualAccounts");

            migrationBuilder.DropIndex(
                name: "IX_VirtualAccounts_PublicKey",
                table: "VirtualAccounts");

            migrationBuilder.DropIndex(
                name: "IX_VirtualAccounts_SeedPhrase",
                table: "VirtualAccounts");

            migrationBuilder.DropIndex(
                name: "IX_Users_Email",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_Email_PhoneNumber_UserName",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_PhoneNumber",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_UserName",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Roles_Name",
                table: "Roles");

            migrationBuilder.DropIndex(
                name: "IX_NetworkTokens_Symbol",
                table: "NetworkTokens");

            migrationBuilder.DropIndex(
                name: "IX_Networks_Name",
                table: "Networks");
        }
    }
}
