using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddFractionalOwnerships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FractionalOwnerships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RwaTokenId = table.Column<Guid>(type: "uuid", nullable: false),
                    BuyerWalletLinkedAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    FractionAmount = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    TokenCount = table.Column<int>(type: "integer", nullable: false),
                    MintAddress = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    MintTransactionHash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    TransferTransactionHash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    TransferSuccessful = table.Column<bool>(type: "boolean", nullable: false),
                    MetadataUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
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
                    table.PrimaryKey("PK_FractionalOwnerships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FractionalOwnerships_RwaTokens_RwaTokenId",
                        column: x => x.RwaTokenId,
                        principalTable: "RwaTokens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FractionalOwnerships_WalletLinkedAccounts_BuyerWalletLinked~",
                        column: x => x.BuyerWalletLinkedAccountId,
                        principalTable: "WalletLinkedAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Trusts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    SettlorName = table.Column<string>(type: "text", nullable: false),
                    TrusteeName = table.Column<string>(type: "text", nullable: false),
                    TrustProtector = table.Column<string>(type: "text", nullable: true),
                    QualifiedCustodian = table.Column<string>(type: "text", nullable: false),
                    Jurisdiction = table.Column<string>(type: "text", nullable: false),
                    DurationYears = table.Column<int>(type: "integer", nullable: false),
                    TrustType = table.Column<string>(type: "text", nullable: false),
                    MinimumPropertyValue = table.Column<decimal>(type: "numeric", nullable: false),
                    AnnualDistributionRate = table.Column<decimal>(type: "numeric", nullable: false),
                    ReserveFundRate = table.Column<decimal>(type: "numeric", nullable: false),
                    TrusteeFeeRate = table.Column<decimal>(type: "numeric", nullable: false),
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
                    table.PrimaryKey("PK_Trusts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrustProperties",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TrustId = table.Column<Guid>(type: "uuid", nullable: false),
                    PropertyAddress = table.Column<string>(type: "text", nullable: false),
                    PropertyType = table.Column<string>(type: "text", nullable: false),
                    PropertyValue = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalSquareFootage = table.Column<int>(type: "integer", nullable: false),
                    AnnualRentalIncome = table.Column<decimal>(type: "numeric", nullable: false),
                    AnnualExpenses = table.Column<decimal>(type: "numeric", nullable: false),
                    NetIncome = table.Column<decimal>(type: "numeric", nullable: false),
                    TokenSupply = table.Column<int>(type: "integer", nullable: false),
                    TokenPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    TokenNaming = table.Column<string>(type: "text", nullable: false),
                    TitleDocumentHash = table.Column<string>(type: "text", nullable: false),
                    AppraisalDocumentHash = table.Column<string>(type: "text", nullable: false),
                    InsuranceDocumentHash = table.Column<string>(type: "text", nullable: false),
                    SurveyDocumentHash = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("PK_TrustProperties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrustProperties_Trusts_TrustId",
                        column: x => x.TrustId,
                        principalTable: "Trusts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContractGenerations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TrustId = table.Column<Guid>(type: "uuid", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uuid", nullable: true),
                    ContractType = table.Column<string>(type: "text", nullable: false),
                    Blockchain = table.Column<string>(type: "text", nullable: false),
                    GeneratedContract = table.Column<string>(type: "text", nullable: false),
                    ContractHash = table.Column<string>(type: "text", nullable: false),
                    AbiData = table.Column<string>(type: "text", nullable: true),
                    BytecodeData = table.Column<string>(type: "text", nullable: true),
                    DeploymentAddress = table.Column<string>(type: "text", nullable: true),
                    DeploymentStatus = table.Column<string>(type: "text", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    GeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeployedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_ContractGenerations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContractGenerations_TrustProperties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "TrustProperties",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ContractGenerations_Trusts_TrustId",
                        column: x => x.TrustId,
                        principalTable: "Trusts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContractGenerations_PropertyId",
                table: "ContractGenerations",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractGenerations_TrustId",
                table: "ContractGenerations",
                column: "TrustId");

            migrationBuilder.CreateIndex(
                name: "IX_FractionalOwnerships_BuyerWalletLinkedAccountId",
                table: "FractionalOwnerships",
                column: "BuyerWalletLinkedAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_FractionalOwnerships_MintAddress",
                table: "FractionalOwnerships",
                column: "MintAddress");

            migrationBuilder.CreateIndex(
                name: "IX_FractionalOwnerships_RwaTokenId",
                table: "FractionalOwnerships",
                column: "RwaTokenId");

            migrationBuilder.CreateIndex(
                name: "IX_TrustProperties_TrustId",
                table: "TrustProperties",
                column: "TrustId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContractGenerations");

            migrationBuilder.DropTable(
                name: "FractionalOwnerships");

            migrationBuilder.DropTable(
                name: "TrustProperties");

            migrationBuilder.DropTable(
                name: "Trusts");
        }
    }
}
