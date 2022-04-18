using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteOASIS.Migrations
{
    public partial class SQLLiteOASIS : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AvatarDetailEntities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UmaJson = table.Column<string>(type: "TEXT", nullable: false),
                    Image2D = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    Username = table.Column<string>(type: "TEXT", nullable: false),
                    Address = table.Column<string>(type: "TEXT", nullable: false),
                    Postcode = table.Column<string>(type: "TEXT", nullable: false),
                    Town = table.Column<string>(type: "TEXT", nullable: false),
                    Country = table.Column<string>(type: "TEXT", nullable: false),
                    Landline = table.Column<string>(type: "TEXT", nullable: false),
                    Mobile = table.Column<string>(type: "TEXT", nullable: false),
                    DOB = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Karma = table.Column<int>(type: "INTEGER", nullable: false),
                    XP = table.Column<int>(type: "INTEGER", nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    ParentOmniverseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ParentMultiverseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ParentUniverseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ParentDimensionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ParentGalaxyClusterId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ParentGalaxyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ParentSolarSystemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ParentGreatGrandSuperStarId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ParentGrandSuperStarId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ParentSuperStarId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ParentStarId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ParentPlanetId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ParentMoonId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ParentCelestialSpaceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ParentCelestialBodyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ParentZomeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ParentHolonId = table.Column<Guid>(type: "TEXT", nullable: false),
                    IsChanged = table.Column<bool>(type: "INTEGER", nullable: false),
                    HolonId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    PreviousVersionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedByAvatarId = table.Column<string>(type: "TEXT", nullable: false),
                    ModifiedByAvatarId = table.Column<string>(type: "TEXT", nullable: false),
                    DeletedByAvatarId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvatarDetailEntities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AvatarEntities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    FirstName = table.Column<string>(type: "TEXT", nullable: false),
                    LastName = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    Username = table.Column<string>(type: "TEXT", nullable: false),
                    Password = table.Column<string>(type: "TEXT", nullable: false),
                    AcceptTerms = table.Column<bool>(type: "INTEGER", nullable: false),
                    VerificationToken = table.Column<string>(type: "TEXT", nullable: false),
                    Verified = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ResetToken = table.Column<string>(type: "TEXT", nullable: false),
                    JwtToken = table.Column<string>(type: "TEXT", nullable: false),
                    RefreshToken = table.Column<string>(type: "TEXT", nullable: false),
                    ResetTokenExpires = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PasswordReset = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastBeamedIn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastBeamedOut = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsBeamedIn = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsChanged = table.Column<bool>(type: "INTEGER", nullable: false),
                    HolonId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    PreviousVersionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedByAvatarId = table.Column<string>(type: "TEXT", nullable: false),
                    ModifiedByAvatarId = table.Column<string>(type: "TEXT", nullable: false),
                    DeletedByAvatarId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvatarEntities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HolonEntities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ParentOmniverseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ParentMultiverseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ParentUniverseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ParentDimensionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ParentGalaxyClusterId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ParentGalaxyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ParentSolarSystemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ParentGreatGrandSuperStarId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ParentGrandSuperStarId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ParentSuperStarId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ParentStarId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ParentPlanetId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ParentMoonId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ParentCelestialSpaceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ParentCelestialBodyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ParentZomeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ParentHolonId = table.Column<Guid>(type: "TEXT", nullable: false),
                    IsChanged = table.Column<bool>(type: "INTEGER", nullable: false),
                    HolonId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    PreviousVersionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedByAvatarId = table.Column<string>(type: "TEXT", nullable: false),
                    ModifiedByAvatarId = table.Column<string>(type: "TEXT", nullable: false),
                    DeletedByAvatarId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HolonEntities", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AvatarDetailEntities");

            migrationBuilder.DropTable(
                name: "AvatarEntities");

            migrationBuilder.DropTable(
                name: "HolonEntities");
        }
    }
}
