using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class v8 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WalletLinkedAccounts_PublicKey",
                table: "WalletLinkedAccounts");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_WalletLinkedAccounts_PublicKey",
                table: "WalletLinkedAccounts",
                column: "PublicKey",
                unique: true);

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
        }
    }
}
