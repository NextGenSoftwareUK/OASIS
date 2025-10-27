using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Migrations
{
    public partial class InitialModelMig : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Avatar",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    AvatarType = table.Column<int>(type: "INTEGER", nullable: false),
                    HolonType = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Title = table.Column<string>(type: "TEXT", nullable: true),
                    FirstName = table.Column<string>(type: "TEXT", nullable: true),
                    LastName = table.Column<string>(type: "TEXT", nullable: true),
                    FullName = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Username = table.Column<string>(type: "TEXT", nullable: true),
                    Email = table.Column<string>(type: "TEXT", nullable: true),
                    Password = table.Column<string>(type: "TEXT", nullable: true),
                    AcceptTerms = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsVerified = table.Column<bool>(type: "INTEGER", nullable: false),
                    JwtToken = table.Column<string>(type: "TEXT", nullable: true),
                    RefreshToken = table.Column<string>(type: "TEXT", nullable: true),
                    PasswordReset = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ResetToken = table.Column<string>(type: "TEXT", nullable: true),
                    ResetTokenExpires = table.Column<DateTime>(type: "TEXT", nullable: true),
                    VerificationToken = table.Column<string>(type: "TEXT", nullable: true),
                    Verified = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Image2D = table.Column<string>(type: "TEXT", nullable: true),
                    Karma = table.Column<int>(type: "INTEGER", nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    XP = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedProviderType = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedOASISType = table.Column<int>(type: "INTEGER", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsChanged = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsNewHolon = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByAvatarId = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedByAvatarId = table.Column<string>(type: "TEXT", nullable: true),
                    DeletedByAvatarId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Avatar", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AvatarDetail",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    HolonType = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: true),
                    FirstName = table.Column<string>(type: "TEXT", nullable: true),
                    LastName = table.Column<string>(type: "TEXT", nullable: true),
                    DOB = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Address = table.Column<string>(type: "TEXT", nullable: true),
                    Town = table.Column<string>(type: "TEXT", nullable: true),
                    County = table.Column<string>(type: "TEXT", nullable: true),
                    Country = table.Column<string>(type: "TEXT", nullable: true),
                    Postcode = table.Column<string>(type: "TEXT", nullable: true),
                    Mobile = table.Column<string>(type: "TEXT", nullable: true),
                    Landline = table.Column<string>(type: "TEXT", nullable: true),
                    Username = table.Column<string>(type: "TEXT", nullable: true),
                    Password = table.Column<string>(type: "TEXT", nullable: true),
                    Email = table.Column<string>(type: "TEXT", nullable: true),
                    Image2D = table.Column<string>(type: "TEXT", nullable: true),
                    Model3D = table.Column<string>(type: "TEXT", nullable: true),
                    FavouriteColour = table.Column<int>(type: "INTEGER", nullable: false),
                    STARCLIColour = table.Column<int>(type: "INTEGER", nullable: false),
                    AvatarType = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedOASISType = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedProviderType = table.Column<int>(type: "INTEGER", nullable: false),
                    Karma = table.Column<int>(type: "INTEGER", nullable: false),
                    XP = table.Column<int>(type: "INTEGER", nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsChanged = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsNewHolon = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByAvatarId = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedByAvatarId = table.Column<string>(type: "TEXT", nullable: true),
                    DeletedByAvatarId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvatarDetail", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Dimension",
                columns: table => new
                {
                    DimesionId = table.Column<string>(type: "TEXT", nullable: false),
                    DimensionLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    HolonId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dimension", x => x.DimesionId);
                });

            migrationBuilder.CreateTable(
                name: "GrandSuperStar",
                columns: table => new
                {
                    StarId = table.Column<string>(type: "TEXT", nullable: false),
                    HolonId = table.Column<string>(type: "TEXT", nullable: true),
                    SpaceQuadrant = table.Column<int>(type: "INTEGER", nullable: false),
                    SpaceSector = table.Column<int>(type: "INTEGER", nullable: false),
                    SuperGalacticLatitute = table.Column<float>(type: "REAL", nullable: false),
                    SuperGalacticLongitute = table.Column<float>(type: "REAL", nullable: false),
                    GalacticLatitute = table.Column<float>(type: "REAL", nullable: false),
                    GalacticLongitute = table.Column<float>(type: "REAL", nullable: false),
                    HorizontalLatitute = table.Column<float>(type: "REAL", nullable: false),
                    HorizontalLongitute = table.Column<float>(type: "REAL", nullable: false),
                    EquatorialLatitute = table.Column<float>(type: "REAL", nullable: false),
                    EquatorialLongitute = table.Column<float>(type: "REAL", nullable: false),
                    EclipticLatitute = table.Column<float>(type: "REAL", nullable: false),
                    EclipticLongitute = table.Column<float>(type: "REAL", nullable: false),
                    Size = table.Column<int>(type: "INTEGER", nullable: false),
                    Radius = table.Column<int>(type: "INTEGER", nullable: false),
                    Age = table.Column<int>(type: "INTEGER", nullable: false),
                    Mass = table.Column<int>(type: "INTEGER", nullable: false),
                    Temperature = table.Column<int>(type: "INTEGER", nullable: false),
                    Weight = table.Column<int>(type: "INTEGER", nullable: false),
                    GravitaionalPull = table.Column<int>(type: "INTEGER", nullable: false),
                    OrbitPositionFromParentStar = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentOrbitAngleOfParentStar = table.Column<int>(type: "INTEGER", nullable: false),
                    DistanceFromParentStarInMetres = table.Column<int>(type: "INTEGER", nullable: false),
                    RotationSpeed = table.Column<int>(type: "INTEGER", nullable: false),
                    TiltAngle = table.Column<int>(type: "INTEGER", nullable: false),
                    NumberRegisteredAvatars = table.Column<int>(type: "INTEGER", nullable: false),
                    NunmerActiveAvatars = table.Column<int>(type: "INTEGER", nullable: false),
                    GenesisType = table.Column<int>(type: "INTEGER", nullable: false),
                    Luminosity = table.Column<int>(type: "INTEGER", nullable: false),
                    StarType = table.Column<int>(type: "INTEGER", nullable: false),
                    StarClassification = table.Column<int>(type: "INTEGER", nullable: false),
                    StarBinaryType = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GrandSuperStar", x => x.StarId);
                });

            migrationBuilder.CreateTable(
                name: "GreatGrandSuperStar",
                columns: table => new
                {
                    StarId = table.Column<string>(type: "TEXT", nullable: false),
                    HolonId = table.Column<string>(type: "TEXT", nullable: true),
                    SpaceQuadrant = table.Column<int>(type: "INTEGER", nullable: false),
                    SpaceSector = table.Column<int>(type: "INTEGER", nullable: false),
                    SuperGalacticLatitute = table.Column<float>(type: "REAL", nullable: false),
                    SuperGalacticLongitute = table.Column<float>(type: "REAL", nullable: false),
                    GalacticLatitute = table.Column<float>(type: "REAL", nullable: false),
                    GalacticLongitute = table.Column<float>(type: "REAL", nullable: false),
                    HorizontalLatitute = table.Column<float>(type: "REAL", nullable: false),
                    HorizontalLongitute = table.Column<float>(type: "REAL", nullable: false),
                    EquatorialLatitute = table.Column<float>(type: "REAL", nullable: false),
                    EquatorialLongitute = table.Column<float>(type: "REAL", nullable: false),
                    EclipticLatitute = table.Column<float>(type: "REAL", nullable: false),
                    EclipticLongitute = table.Column<float>(type: "REAL", nullable: false),
                    Size = table.Column<int>(type: "INTEGER", nullable: false),
                    Radius = table.Column<int>(type: "INTEGER", nullable: false),
                    Age = table.Column<int>(type: "INTEGER", nullable: false),
                    Mass = table.Column<int>(type: "INTEGER", nullable: false),
                    Temperature = table.Column<int>(type: "INTEGER", nullable: false),
                    Weight = table.Column<int>(type: "INTEGER", nullable: false),
                    GravitaionalPull = table.Column<int>(type: "INTEGER", nullable: false),
                    OrbitPositionFromParentStar = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentOrbitAngleOfParentStar = table.Column<int>(type: "INTEGER", nullable: false),
                    DistanceFromParentStarInMetres = table.Column<int>(type: "INTEGER", nullable: false),
                    RotationSpeed = table.Column<int>(type: "INTEGER", nullable: false),
                    TiltAngle = table.Column<int>(type: "INTEGER", nullable: false),
                    NumberRegisteredAvatars = table.Column<int>(type: "INTEGER", nullable: false),
                    NunmerActiveAvatars = table.Column<int>(type: "INTEGER", nullable: false),
                    GenesisType = table.Column<int>(type: "INTEGER", nullable: false),
                    Luminosity = table.Column<int>(type: "INTEGER", nullable: false),
                    StarType = table.Column<int>(type: "INTEGER", nullable: false),
                    StarClassification = table.Column<int>(type: "INTEGER", nullable: false),
                    StarBinaryType = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GreatGrandSuperStar", x => x.StarId);
                });

            migrationBuilder.CreateTable(
                name: "SuperStar",
                columns: table => new
                {
                    StarId = table.Column<string>(type: "TEXT", nullable: false),
                    HolonId = table.Column<string>(type: "TEXT", nullable: true),
                    SpaceQuadrant = table.Column<int>(type: "INTEGER", nullable: false),
                    SpaceSector = table.Column<int>(type: "INTEGER", nullable: false),
                    SuperGalacticLatitute = table.Column<float>(type: "REAL", nullable: false),
                    SuperGalacticLongitute = table.Column<float>(type: "REAL", nullable: false),
                    GalacticLatitute = table.Column<float>(type: "REAL", nullable: false),
                    GalacticLongitute = table.Column<float>(type: "REAL", nullable: false),
                    HorizontalLatitute = table.Column<float>(type: "REAL", nullable: false),
                    HorizontalLongitute = table.Column<float>(type: "REAL", nullable: false),
                    EquatorialLatitute = table.Column<float>(type: "REAL", nullable: false),
                    EquatorialLongitute = table.Column<float>(type: "REAL", nullable: false),
                    EclipticLatitute = table.Column<float>(type: "REAL", nullable: false),
                    EclipticLongitute = table.Column<float>(type: "REAL", nullable: false),
                    Size = table.Column<int>(type: "INTEGER", nullable: false),
                    Radius = table.Column<int>(type: "INTEGER", nullable: false),
                    Age = table.Column<int>(type: "INTEGER", nullable: false),
                    Mass = table.Column<int>(type: "INTEGER", nullable: false),
                    Temperature = table.Column<int>(type: "INTEGER", nullable: false),
                    Weight = table.Column<int>(type: "INTEGER", nullable: false),
                    GravitaionalPull = table.Column<int>(type: "INTEGER", nullable: false),
                    OrbitPositionFromParentStar = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentOrbitAngleOfParentStar = table.Column<int>(type: "INTEGER", nullable: false),
                    DistanceFromParentStarInMetres = table.Column<int>(type: "INTEGER", nullable: false),
                    RotationSpeed = table.Column<int>(type: "INTEGER", nullable: false),
                    TiltAngle = table.Column<int>(type: "INTEGER", nullable: false),
                    NumberRegisteredAvatars = table.Column<int>(type: "INTEGER", nullable: false),
                    NunmerActiveAvatars = table.Column<int>(type: "INTEGER", nullable: false),
                    GenesisType = table.Column<int>(type: "INTEGER", nullable: false),
                    Luminosity = table.Column<int>(type: "INTEGER", nullable: false),
                    StarType = table.Column<int>(type: "INTEGER", nullable: false),
                    StarClassification = table.Column<int>(type: "INTEGER", nullable: false),
                    StarBinaryType = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuperStar", x => x.StarId);
                });

            migrationBuilder.CreateTable(
                name: "Universe",
                columns: table => new
                {
                    UniverseId = table.Column<string>(type: "TEXT", nullable: false),
                    HolonId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Universe", x => x.UniverseId);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    AvatarId = table.Column<string>(type: "TEXT", nullable: true),
                    Token = table.Column<string>(type: "TEXT", nullable: true),
                    Expires = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsExpired = table.Column<bool>(type: "INTEGER", nullable: false),
                    Created = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByIp = table.Column<string>(type: "TEXT", nullable: true),
                    Revoked = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RevokedByIp = table.Column<string>(type: "TEXT", nullable: true),
                    ReplacedByToken = table.Column<string>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    AvatarModelId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Avatar_AvatarModelId",
                        column: x => x.AvatarModelId,
                        principalTable: "Avatar",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AvatarAchievements",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AvatarId = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    AchievementType = table.Column<int>(type: "INTEGER", nullable: false),
                    AchievementEarnt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    KarmaSourceTitle = table.Column<string>(type: "TEXT", nullable: true),
                    KarmaSourceDesc = table.Column<string>(type: "TEXT", nullable: true),
                    WebLink = table.Column<string>(type: "TEXT", nullable: true),
                    KarmaSource = table.Column<int>(type: "INTEGER", nullable: false),
                    Provider = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvatarAchievements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AvatarAchievements_AvatarDetail_AvatarId",
                        column: x => x.AvatarId,
                        principalTable: "AvatarDetail",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AvatarAttributes",
                columns: table => new
                {
                    AvatarId = table.Column<string>(type: "TEXT", nullable: false),
                    Strength = table.Column<int>(type: "INTEGER", nullable: false),
                    Speed = table.Column<int>(type: "INTEGER", nullable: false),
                    Dexterity = table.Column<int>(type: "INTEGER", nullable: false),
                    Toughness = table.Column<int>(type: "INTEGER", nullable: false),
                    Wisdom = table.Column<int>(type: "INTEGER", nullable: false),
                    Intelligence = table.Column<int>(type: "INTEGER", nullable: false),
                    Magic = table.Column<int>(type: "INTEGER", nullable: false),
                    Vitality = table.Column<int>(type: "INTEGER", nullable: false),
                    Endurance = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvatarAttributes", x => x.AvatarId);
                    table.ForeignKey(
                        name: "FK_AvatarAttributes_AvatarDetail_AvatarId",
                        column: x => x.AvatarId,
                        principalTable: "AvatarDetail",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AvatarAura",
                columns: table => new
                {
                    AvatarId = table.Column<string>(type: "TEXT", nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<int>(type: "INTEGER", nullable: false),
                    Progress = table.Column<int>(type: "INTEGER", nullable: false),
                    Brightness = table.Column<int>(type: "INTEGER", nullable: false),
                    Size = table.Column<int>(type: "INTEGER", nullable: false),
                    ColourRed = table.Column<int>(type: "INTEGER", nullable: false),
                    ColourGreen = table.Column<int>(type: "INTEGER", nullable: false),
                    ColourBlue = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvatarAura", x => x.AvatarId);
                    table.ForeignKey(
                        name: "FK_AvatarAura_AvatarDetail_AvatarId",
                        column: x => x.AvatarId,
                        principalTable: "AvatarDetail",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AvatarChakras",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    SanskritName = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    WhatItControls = table.Column<string>(type: "TEXT", nullable: true),
                    YogaPose = table.Column<int>(type: "INTEGER", nullable: false),
                    WhenItDevelops = table.Column<string>(type: "TEXT", nullable: true),
                    Element = table.Column<int>(type: "INTEGER", nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    Progress = table.Column<int>(type: "INTEGER", nullable: false),
                    XP = table.Column<int>(type: "INTEGER", nullable: false),
                    AvatarDetailId = table.Column<string>(type: "TEXT", nullable: true),
                    AvatarId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvatarChakras", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AvatarChakras_AvatarDetail_AvatarDetailId",
                        column: x => x.AvatarDetailId,
                        principalTable: "AvatarDetail",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AvatarChakras_AvatarDetail_AvatarId",
                        column: x => x.AvatarId,
                        principalTable: "AvatarDetail",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AvatarHumanDesign",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    AvatarDetailId = table.Column<string>(type: "TEXT", nullable: true),
                    AvatarId = table.Column<string>(type: "TEXT", nullable: true),
                    Type = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvatarHumanDesign", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AvatarHumanDesign_AvatarDetail_AvatarDetailId",
                        column: x => x.AvatarDetailId,
                        principalTable: "AvatarDetail",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AvatarHumanDesign_AvatarDetail_AvatarId",
                        column: x => x.AvatarId,
                        principalTable: "AvatarDetail",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AvatarInventory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AvatarId = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvatarInventory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AvatarInventory_AvatarDetail_AvatarId",
                        column: x => x.AvatarId,
                        principalTable: "AvatarDetail",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AvatarSkills",
                columns: table => new
                {
                    AvatarId = table.Column<string>(type: "TEXT", nullable: false),
                    Fishing = table.Column<int>(type: "INTEGER", nullable: false),
                    Farming = table.Column<int>(type: "INTEGER", nullable: false),
                    Research = table.Column<int>(type: "INTEGER", nullable: false),
                    Science = table.Column<int>(type: "INTEGER", nullable: false),
                    Negotiating = table.Column<int>(type: "INTEGER", nullable: false),
                    Translating = table.Column<int>(type: "INTEGER", nullable: false),
                    MelleeCombat = table.Column<int>(type: "INTEGER", nullable: false),
                    RangeCombat = table.Column<int>(type: "INTEGER", nullable: false),
                    SpellCasting = table.Column<int>(type: "INTEGER", nullable: false),
                    Meditation = table.Column<int>(type: "INTEGER", nullable: false),
                    Yoga = table.Column<int>(type: "INTEGER", nullable: false),
                    Mindfulness = table.Column<int>(type: "INTEGER", nullable: false),
                    Engineering = table.Column<int>(type: "INTEGER", nullable: false),
                    FireStarting = table.Column<int>(type: "INTEGER", nullable: false),
                    Computers = table.Column<int>(type: "INTEGER", nullable: false),
                    Languages = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvatarSkills", x => x.AvatarId);
                    table.ForeignKey(
                        name: "FK_AvatarSkills_AvatarDetail_AvatarId",
                        column: x => x.AvatarId,
                        principalTable: "AvatarDetail",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AvatarSpells",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AvatarId = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    FireDamage = table.Column<int>(type: "INTEGER", nullable: false),
                    WaterDamage = table.Column<int>(type: "INTEGER", nullable: false),
                    IceDamage = table.Column<int>(type: "INTEGER", nullable: false),
                    WindDamage = table.Column<int>(type: "INTEGER", nullable: false),
                    LightningDamage = table.Column<int>(type: "INTEGER", nullable: false),
                    PoisonDamage = table.Column<int>(type: "INTEGER", nullable: false),
                    HealingPower = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvatarSpells", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AvatarSpells_AvatarDetail_AvatarId",
                        column: x => x.AvatarId,
                        principalTable: "AvatarDetail",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AvatarStats",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    AvatarDetailId = table.Column<string>(type: "TEXT", nullable: true),
                    AvatarId = table.Column<string>(type: "TEXT", nullable: true),
                    HP_Current = table.Column<int>(type: "INTEGER", nullable: false),
                    HP_Max = table.Column<int>(type: "INTEGER", nullable: false),
                    Mana_Current = table.Column<int>(type: "INTEGER", nullable: false),
                    Mana_Max = table.Column<int>(type: "INTEGER", nullable: false),
                    Energy_Current = table.Column<int>(type: "INTEGER", nullable: false),
                    Energy_Max = table.Column<int>(type: "INTEGER", nullable: false),
                    Staminia_Current = table.Column<int>(type: "INTEGER", nullable: false),
                    Staminia_Max = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvatarStats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AvatarStats_AvatarDetail_AvatarDetailId",
                        column: x => x.AvatarDetailId,
                        principalTable: "AvatarDetail",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AvatarStats_AvatarDetail_AvatarId",
                        column: x => x.AvatarId,
                        principalTable: "AvatarDetail",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AvatarSuperPowers",
                columns: table => new
                {
                    AvatarId = table.Column<string>(type: "TEXT", nullable: false),
                    Flight = table.Column<int>(type: "INTEGER", nullable: false),
                    Telekineseis = table.Column<int>(type: "INTEGER", nullable: false),
                    Telepathy = table.Column<int>(type: "INTEGER", nullable: false),
                    Teleportation = table.Column<int>(type: "INTEGER", nullable: false),
                    RemoteViewing = table.Column<int>(type: "INTEGER", nullable: false),
                    AstralProjection = table.Column<int>(type: "INTEGER", nullable: false),
                    SuperStrength = table.Column<int>(type: "INTEGER", nullable: false),
                    SuperSpeed = table.Column<int>(type: "INTEGER", nullable: false),
                    Invulerability = table.Column<int>(type: "INTEGER", nullable: false),
                    HeatVision = table.Column<int>(type: "INTEGER", nullable: false),
                    XRayVision = table.Column<int>(type: "INTEGER", nullable: false),
                    FreezeBreath = table.Column<int>(type: "INTEGER", nullable: false),
                    BioLocatation = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvatarSuperPowers", x => x.AvatarId);
                    table.ForeignKey(
                        name: "FK_AvatarSuperPowers_AvatarDetail_AvatarId",
                        column: x => x.AvatarId,
                        principalTable: "AvatarDetail",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GeneKeys",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AvatarDetailId = table.Column<string>(type: "TEXT", nullable: true),
                    AvatarId = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Shadow = table.Column<string>(type: "TEXT", nullable: true),
                    Gift = table.Column<string>(type: "TEXT", nullable: true),
                    Sidhi = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeneKeys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GeneKeys_AvatarDetail_AvatarDetailId",
                        column: x => x.AvatarDetailId,
                        principalTable: "AvatarDetail",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_GeneKeys_AvatarDetail_AvatarId",
                        column: x => x.AvatarId,
                        principalTable: "AvatarDetail",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "HeartRateEntry",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AvatarDetailId = table.Column<string>(type: "TEXT", nullable: true),
                    AvatarId = table.Column<string>(type: "TEXT", nullable: true),
                    HeartRateValue = table.Column<int>(type: "INTEGER", nullable: false),
                    TimeStamp = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HeartRateEntry", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HeartRateEntry_AvatarDetail_AvatarDetailId",
                        column: x => x.AvatarDetailId,
                        principalTable: "AvatarDetail",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_HeartRateEntry_AvatarDetail_AvatarId",
                        column: x => x.AvatarId,
                        principalTable: "AvatarDetail",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "KarmaAkashicRecord",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AvatarId = table.Column<string>(type: "TEXT", nullable: true),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Karma = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalKarma = table.Column<int>(type: "INTEGER", nullable: false),
                    KarmaSourceTitle = table.Column<string>(type: "TEXT", nullable: true),
                    KarmaSourceDesc = table.Column<string>(type: "TEXT", nullable: true),
                    WebLink = table.Column<string>(type: "TEXT", nullable: true),
                    KarmaSource = table.Column<int>(type: "INTEGER", nullable: false),
                    KarmaEarntOrLost = table.Column<int>(type: "INTEGER", nullable: false),
                    KarmaTypePositive = table.Column<int>(type: "INTEGER", nullable: false),
                    KarmaTypeNegative = table.Column<int>(type: "INTEGER", nullable: false),
                    Provider = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KarmaAkashicRecord", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KarmaAkashicRecord_AvatarDetail_AvatarId",
                        column: x => x.AvatarId,
                        principalTable: "AvatarDetail",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProviderPrivateKey",
                columns: table => new
                {
                    ProviderId = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: true),
                    OwnerId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderPrivateKey", x => x.ProviderId);
                    table.ForeignKey(
                        name: "FK_ProviderPrivateKey_AvatarDetail_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "AvatarDetail",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProviderPublicKey",
                columns: table => new
                {
                    ProviderId = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: true),
                    OwnerId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderPublicKey", x => x.ProviderId);
                    table.ForeignKey(
                        name: "FK_ProviderPublicKey_AvatarDetail_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "AvatarDetail",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProviderWalletAddress",
                columns: table => new
                {
                    ProviderId = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: true),
                    OwnerId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderWalletAddress", x => x.ProviderId);
                    table.ForeignKey(
                        name: "FK_ProviderWalletAddress_AvatarDetail_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "AvatarDetail",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "GalaxyCluster",
                columns: table => new
                {
                    GalaxyClusterId = table.Column<string>(type: "TEXT", nullable: false),
                    UniverseId = table.Column<string>(type: "TEXT", nullable: true),
                    HolonId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GalaxyCluster", x => x.GalaxyClusterId);
                    table.ForeignKey(
                        name: "FK_GalaxyCluster_Universe_UniverseId",
                        column: x => x.UniverseId,
                        principalTable: "Universe",
                        principalColumn: "UniverseId");
                });

            migrationBuilder.CreateTable(
                name: "AvatarGifts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GiftType = table.Column<int>(type: "INTEGER", nullable: false),
                    GiftEarnt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    KarmaSourceTitle = table.Column<string>(type: "TEXT", nullable: true),
                    KarmaSourceDesc = table.Column<string>(type: "TEXT", nullable: true),
                    WebLink = table.Column<string>(type: "TEXT", nullable: true),
                    KarmaSource = table.Column<int>(type: "INTEGER", nullable: false),
                    Provider = table.Column<int>(type: "INTEGER", nullable: false),
                    AvatarDetailId = table.Column<string>(type: "TEXT", nullable: true),
                    AvatarId = table.Column<string>(type: "TEXT", nullable: true),
                    AvatarChakraId = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvatarGifts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AvatarGifts_AvatarChakras_AvatarChakraId",
                        column: x => x.AvatarChakraId,
                        principalTable: "AvatarChakras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AvatarGifts_AvatarDetail_AvatarDetailId",
                        column: x => x.AvatarDetailId,
                        principalTable: "AvatarDetail",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AvatarGifts_AvatarDetail_AvatarId",
                        column: x => x.AvatarId,
                        principalTable: "AvatarDetail",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Crystal",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AvatarChakraId = table.Column<long>(type: "INTEGER", nullable: false),
                    Name = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    ProtectionLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    EnergisingLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    GroundingLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    CleansingLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    AmplifyicationLevel = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Crystal", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Crystal_AvatarChakras_AvatarChakraId",
                        column: x => x.AvatarChakraId,
                        principalTable: "AvatarChakras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Galaxy",
                columns: table => new
                {
                    GalaxyId = table.Column<string>(type: "TEXT", nullable: false),
                    GalaxyClusterId = table.Column<string>(type: "TEXT", nullable: true),
                    SuperStarStarId = table.Column<string>(type: "TEXT", nullable: true),
                    HolonId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Galaxy", x => x.GalaxyId);
                    table.ForeignKey(
                        name: "FK_Galaxy_GalaxyCluster_GalaxyClusterId",
                        column: x => x.GalaxyClusterId,
                        principalTable: "GalaxyCluster",
                        principalColumn: "GalaxyClusterId");
                    table.ForeignKey(
                        name: "FK_Galaxy_SuperStar_SuperStarStarId",
                        column: x => x.SuperStarStarId,
                        principalTable: "SuperStar",
                        principalColumn: "StarId");
                });

            migrationBuilder.CreateTable(
                name: "Nebula",
                columns: table => new
                {
                    NebulaId = table.Column<string>(type: "TEXT", nullable: false),
                    GalaxyId = table.Column<string>(type: "TEXT", nullable: true),
                    UniverseId = table.Column<string>(type: "TEXT", nullable: true),
                    HolonId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Nebula", x => x.NebulaId);
                    table.ForeignKey(
                        name: "FK_Nebula_Galaxy_GalaxyId",
                        column: x => x.GalaxyId,
                        principalTable: "Galaxy",
                        principalColumn: "GalaxyId");
                    table.ForeignKey(
                        name: "FK_Nebula_Universe_UniverseId",
                        column: x => x.UniverseId,
                        principalTable: "Universe",
                        principalColumn: "UniverseId");
                });

            migrationBuilder.CreateTable(
                name: "Star",
                columns: table => new
                {
                    StarId = table.Column<string>(type: "TEXT", nullable: false),
                    GalaxyId = table.Column<string>(type: "TEXT", nullable: true),
                    GalaxyClusterId = table.Column<string>(type: "TEXT", nullable: true),
                    UniverseId = table.Column<string>(type: "TEXT", nullable: true),
                    HolonId = table.Column<string>(type: "TEXT", nullable: true),
                    SpaceQuadrant = table.Column<int>(type: "INTEGER", nullable: false),
                    SpaceSector = table.Column<int>(type: "INTEGER", nullable: false),
                    SuperGalacticLatitute = table.Column<float>(type: "REAL", nullable: false),
                    SuperGalacticLongitute = table.Column<float>(type: "REAL", nullable: false),
                    GalacticLatitute = table.Column<float>(type: "REAL", nullable: false),
                    GalacticLongitute = table.Column<float>(type: "REAL", nullable: false),
                    HorizontalLatitute = table.Column<float>(type: "REAL", nullable: false),
                    HorizontalLongitute = table.Column<float>(type: "REAL", nullable: false),
                    EquatorialLatitute = table.Column<float>(type: "REAL", nullable: false),
                    EquatorialLongitute = table.Column<float>(type: "REAL", nullable: false),
                    EclipticLatitute = table.Column<float>(type: "REAL", nullable: false),
                    EclipticLongitute = table.Column<float>(type: "REAL", nullable: false),
                    Size = table.Column<int>(type: "INTEGER", nullable: false),
                    Radius = table.Column<int>(type: "INTEGER", nullable: false),
                    Age = table.Column<int>(type: "INTEGER", nullable: false),
                    Mass = table.Column<int>(type: "INTEGER", nullable: false),
                    Temperature = table.Column<int>(type: "INTEGER", nullable: false),
                    Weight = table.Column<int>(type: "INTEGER", nullable: false),
                    GravitaionalPull = table.Column<int>(type: "INTEGER", nullable: false),
                    OrbitPositionFromParentStar = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentOrbitAngleOfParentStar = table.Column<int>(type: "INTEGER", nullable: false),
                    DistanceFromParentStarInMetres = table.Column<int>(type: "INTEGER", nullable: false),
                    RotationSpeed = table.Column<int>(type: "INTEGER", nullable: false),
                    TiltAngle = table.Column<int>(type: "INTEGER", nullable: false),
                    NumberRegisteredAvatars = table.Column<int>(type: "INTEGER", nullable: false),
                    NunmerActiveAvatars = table.Column<int>(type: "INTEGER", nullable: false),
                    GenesisType = table.Column<int>(type: "INTEGER", nullable: false),
                    Luminosity = table.Column<int>(type: "INTEGER", nullable: false),
                    StarType = table.Column<int>(type: "INTEGER", nullable: false),
                    StarClassification = table.Column<int>(type: "INTEGER", nullable: false),
                    StarBinaryType = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Star", x => x.StarId);
                    table.ForeignKey(
                        name: "FK_Star_Galaxy_GalaxyId",
                        column: x => x.GalaxyId,
                        principalTable: "Galaxy",
                        principalColumn: "GalaxyId");
                    table.ForeignKey(
                        name: "FK_Star_GalaxyCluster_GalaxyClusterId",
                        column: x => x.GalaxyClusterId,
                        principalTable: "GalaxyCluster",
                        principalColumn: "GalaxyClusterId");
                    table.ForeignKey(
                        name: "FK_Star_Universe_UniverseId",
                        column: x => x.UniverseId,
                        principalTable: "Universe",
                        principalColumn: "UniverseId");
                });

            migrationBuilder.CreateTable(
                name: "SolarSystem",
                columns: table => new
                {
                    SolarSystemId = table.Column<string>(type: "TEXT", nullable: false),
                    GalaxyId = table.Column<string>(type: "TEXT", nullable: true),
                    GalaxyClusterId = table.Column<string>(type: "TEXT", nullable: true),
                    UniverseId = table.Column<string>(type: "TEXT", nullable: true),
                    StarId = table.Column<string>(type: "TEXT", nullable: true),
                    HolonId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolarSystem", x => x.SolarSystemId);
                    table.ForeignKey(
                        name: "FK_SolarSystem_Galaxy_GalaxyId",
                        column: x => x.GalaxyId,
                        principalTable: "Galaxy",
                        principalColumn: "GalaxyId");
                    table.ForeignKey(
                        name: "FK_SolarSystem_GalaxyCluster_GalaxyClusterId",
                        column: x => x.GalaxyClusterId,
                        principalTable: "GalaxyCluster",
                        principalColumn: "GalaxyClusterId");
                    table.ForeignKey(
                        name: "FK_SolarSystem_Star_StarId",
                        column: x => x.StarId,
                        principalTable: "Star",
                        principalColumn: "StarId");
                    table.ForeignKey(
                        name: "FK_SolarSystem_Universe_UniverseId",
                        column: x => x.UniverseId,
                        principalTable: "Universe",
                        principalColumn: "UniverseId");
                });

            migrationBuilder.CreateTable(
                name: "Asteroid",
                columns: table => new
                {
                    AsteroidId = table.Column<string>(type: "TEXT", nullable: false),
                    SolarSystemId = table.Column<string>(type: "TEXT", nullable: true),
                    GalaxyId = table.Column<string>(type: "TEXT", nullable: true),
                    GalaxyClusterId = table.Column<string>(type: "TEXT", nullable: true),
                    UniverseId = table.Column<string>(type: "TEXT", nullable: true),
                    HolonId = table.Column<string>(type: "TEXT", nullable: true),
                    SpaceQuadrant = table.Column<int>(type: "INTEGER", nullable: false),
                    SpaceSector = table.Column<int>(type: "INTEGER", nullable: false),
                    SuperGalacticLatitute = table.Column<float>(type: "REAL", nullable: false),
                    SuperGalacticLongitute = table.Column<float>(type: "REAL", nullable: false),
                    GalacticLatitute = table.Column<float>(type: "REAL", nullable: false),
                    GalacticLongitute = table.Column<float>(type: "REAL", nullable: false),
                    HorizontalLatitute = table.Column<float>(type: "REAL", nullable: false),
                    HorizontalLongitute = table.Column<float>(type: "REAL", nullable: false),
                    EquatorialLatitute = table.Column<float>(type: "REAL", nullable: false),
                    EquatorialLongitute = table.Column<float>(type: "REAL", nullable: false),
                    EclipticLatitute = table.Column<float>(type: "REAL", nullable: false),
                    EclipticLongitute = table.Column<float>(type: "REAL", nullable: false),
                    Size = table.Column<int>(type: "INTEGER", nullable: false),
                    Radius = table.Column<int>(type: "INTEGER", nullable: false),
                    Age = table.Column<int>(type: "INTEGER", nullable: false),
                    Mass = table.Column<int>(type: "INTEGER", nullable: false),
                    Temperature = table.Column<int>(type: "INTEGER", nullable: false),
                    Weight = table.Column<int>(type: "INTEGER", nullable: false),
                    GravitaionalPull = table.Column<int>(type: "INTEGER", nullable: false),
                    OrbitPositionFromParentStar = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentOrbitAngleOfParentStar = table.Column<int>(type: "INTEGER", nullable: false),
                    DistanceFromParentStarInMetres = table.Column<int>(type: "INTEGER", nullable: false),
                    RotationSpeed = table.Column<int>(type: "INTEGER", nullable: false),
                    TiltAngle = table.Column<int>(type: "INTEGER", nullable: false),
                    NumberRegisteredAvatars = table.Column<int>(type: "INTEGER", nullable: false),
                    NunmerActiveAvatars = table.Column<int>(type: "INTEGER", nullable: false),
                    GenesisType = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Asteroid", x => x.AsteroidId);
                    table.ForeignKey(
                        name: "FK_Asteroid_Galaxy_GalaxyId",
                        column: x => x.GalaxyId,
                        principalTable: "Galaxy",
                        principalColumn: "GalaxyId");
                    table.ForeignKey(
                        name: "FK_Asteroid_GalaxyCluster_GalaxyClusterId",
                        column: x => x.GalaxyClusterId,
                        principalTable: "GalaxyCluster",
                        principalColumn: "GalaxyClusterId");
                    table.ForeignKey(
                        name: "FK_Asteroid_SolarSystem_SolarSystemId",
                        column: x => x.SolarSystemId,
                        principalTable: "SolarSystem",
                        principalColumn: "SolarSystemId");
                    table.ForeignKey(
                        name: "FK_Asteroid_Universe_UniverseId",
                        column: x => x.UniverseId,
                        principalTable: "Universe",
                        principalColumn: "UniverseId");
                });

            migrationBuilder.CreateTable(
                name: "Comet",
                columns: table => new
                {
                    CometId = table.Column<string>(type: "TEXT", nullable: false),
                    SolarSystemId = table.Column<string>(type: "TEXT", nullable: true),
                    GalaxyId = table.Column<string>(type: "TEXT", nullable: true),
                    GalaxyClusterId = table.Column<string>(type: "TEXT", nullable: true),
                    UniverseId = table.Column<string>(type: "TEXT", nullable: true),
                    HolonId = table.Column<string>(type: "TEXT", nullable: true),
                    SpaceQuadrant = table.Column<int>(type: "INTEGER", nullable: false),
                    SpaceSector = table.Column<int>(type: "INTEGER", nullable: false),
                    SuperGalacticLatitute = table.Column<float>(type: "REAL", nullable: false),
                    SuperGalacticLongitute = table.Column<float>(type: "REAL", nullable: false),
                    GalacticLatitute = table.Column<float>(type: "REAL", nullable: false),
                    GalacticLongitute = table.Column<float>(type: "REAL", nullable: false),
                    HorizontalLatitute = table.Column<float>(type: "REAL", nullable: false),
                    HorizontalLongitute = table.Column<float>(type: "REAL", nullable: false),
                    EquatorialLatitute = table.Column<float>(type: "REAL", nullable: false),
                    EquatorialLongitute = table.Column<float>(type: "REAL", nullable: false),
                    EclipticLatitute = table.Column<float>(type: "REAL", nullable: false),
                    EclipticLongitute = table.Column<float>(type: "REAL", nullable: false),
                    Size = table.Column<int>(type: "INTEGER", nullable: false),
                    Radius = table.Column<int>(type: "INTEGER", nullable: false),
                    Age = table.Column<int>(type: "INTEGER", nullable: false),
                    Mass = table.Column<int>(type: "INTEGER", nullable: false),
                    Temperature = table.Column<int>(type: "INTEGER", nullable: false),
                    Weight = table.Column<int>(type: "INTEGER", nullable: false),
                    GravitaionalPull = table.Column<int>(type: "INTEGER", nullable: false),
                    OrbitPositionFromParentStar = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentOrbitAngleOfParentStar = table.Column<int>(type: "INTEGER", nullable: false),
                    DistanceFromParentStarInMetres = table.Column<int>(type: "INTEGER", nullable: false),
                    RotationSpeed = table.Column<int>(type: "INTEGER", nullable: false),
                    TiltAngle = table.Column<int>(type: "INTEGER", nullable: false),
                    NumberRegisteredAvatars = table.Column<int>(type: "INTEGER", nullable: false),
                    NunmerActiveAvatars = table.Column<int>(type: "INTEGER", nullable: false),
                    GenesisType = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comet", x => x.CometId);
                    table.ForeignKey(
                        name: "FK_Comet_Galaxy_GalaxyId",
                        column: x => x.GalaxyId,
                        principalTable: "Galaxy",
                        principalColumn: "GalaxyId");
                    table.ForeignKey(
                        name: "FK_Comet_GalaxyCluster_GalaxyClusterId",
                        column: x => x.GalaxyClusterId,
                        principalTable: "GalaxyCluster",
                        principalColumn: "GalaxyClusterId");
                    table.ForeignKey(
                        name: "FK_Comet_SolarSystem_SolarSystemId",
                        column: x => x.SolarSystemId,
                        principalTable: "SolarSystem",
                        principalColumn: "SolarSystemId");
                    table.ForeignKey(
                        name: "FK_Comet_Universe_UniverseId",
                        column: x => x.UniverseId,
                        principalTable: "Universe",
                        principalColumn: "UniverseId");
                });

            migrationBuilder.CreateTable(
                name: "Meteroid",
                columns: table => new
                {
                    MeteroidId = table.Column<string>(type: "TEXT", nullable: false),
                    SolarSystemId = table.Column<string>(type: "TEXT", nullable: true),
                    GalaxyId = table.Column<string>(type: "TEXT", nullable: true),
                    GalaxyClusterId = table.Column<string>(type: "TEXT", nullable: true),
                    UniverseId = table.Column<string>(type: "TEXT", nullable: true),
                    HolonId = table.Column<string>(type: "TEXT", nullable: true),
                    SpaceQuadrant = table.Column<int>(type: "INTEGER", nullable: false),
                    SpaceSector = table.Column<int>(type: "INTEGER", nullable: false),
                    SuperGalacticLatitute = table.Column<float>(type: "REAL", nullable: false),
                    SuperGalacticLongitute = table.Column<float>(type: "REAL", nullable: false),
                    GalacticLatitute = table.Column<float>(type: "REAL", nullable: false),
                    GalacticLongitute = table.Column<float>(type: "REAL", nullable: false),
                    HorizontalLatitute = table.Column<float>(type: "REAL", nullable: false),
                    HorizontalLongitute = table.Column<float>(type: "REAL", nullable: false),
                    EquatorialLatitute = table.Column<float>(type: "REAL", nullable: false),
                    EquatorialLongitute = table.Column<float>(type: "REAL", nullable: false),
                    EclipticLatitute = table.Column<float>(type: "REAL", nullable: false),
                    EclipticLongitute = table.Column<float>(type: "REAL", nullable: false),
                    Size = table.Column<int>(type: "INTEGER", nullable: false),
                    Radius = table.Column<int>(type: "INTEGER", nullable: false),
                    Age = table.Column<int>(type: "INTEGER", nullable: false),
                    Mass = table.Column<int>(type: "INTEGER", nullable: false),
                    Temperature = table.Column<int>(type: "INTEGER", nullable: false),
                    Weight = table.Column<int>(type: "INTEGER", nullable: false),
                    GravitaionalPull = table.Column<int>(type: "INTEGER", nullable: false),
                    OrbitPositionFromParentStar = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentOrbitAngleOfParentStar = table.Column<int>(type: "INTEGER", nullable: false),
                    DistanceFromParentStarInMetres = table.Column<int>(type: "INTEGER", nullable: false),
                    RotationSpeed = table.Column<int>(type: "INTEGER", nullable: false),
                    TiltAngle = table.Column<int>(type: "INTEGER", nullable: false),
                    NumberRegisteredAvatars = table.Column<int>(type: "INTEGER", nullable: false),
                    NunmerActiveAvatars = table.Column<int>(type: "INTEGER", nullable: false),
                    GenesisType = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Meteroid", x => x.MeteroidId);
                    table.ForeignKey(
                        name: "FK_Meteroid_Galaxy_GalaxyId",
                        column: x => x.GalaxyId,
                        principalTable: "Galaxy",
                        principalColumn: "GalaxyId");
                    table.ForeignKey(
                        name: "FK_Meteroid_GalaxyCluster_GalaxyClusterId",
                        column: x => x.GalaxyClusterId,
                        principalTable: "GalaxyCluster",
                        principalColumn: "GalaxyClusterId");
                    table.ForeignKey(
                        name: "FK_Meteroid_SolarSystem_SolarSystemId",
                        column: x => x.SolarSystemId,
                        principalTable: "SolarSystem",
                        principalColumn: "SolarSystemId");
                    table.ForeignKey(
                        name: "FK_Meteroid_Universe_UniverseId",
                        column: x => x.UniverseId,
                        principalTable: "Universe",
                        principalColumn: "UniverseId");
                });

            migrationBuilder.CreateTable(
                name: "Planet",
                columns: table => new
                {
                    PlanetId = table.Column<string>(type: "TEXT", nullable: false),
                    SolarSystemId = table.Column<string>(type: "TEXT", nullable: true),
                    GalaxyId = table.Column<string>(type: "TEXT", nullable: true),
                    GalaxyClusterId = table.Column<string>(type: "TEXT", nullable: true),
                    UniverseId = table.Column<string>(type: "TEXT", nullable: true),
                    HolonId = table.Column<string>(type: "TEXT", nullable: true),
                    SpaceQuadrant = table.Column<int>(type: "INTEGER", nullable: false),
                    SpaceSector = table.Column<int>(type: "INTEGER", nullable: false),
                    SuperGalacticLatitute = table.Column<float>(type: "REAL", nullable: false),
                    SuperGalacticLongitute = table.Column<float>(type: "REAL", nullable: false),
                    GalacticLatitute = table.Column<float>(type: "REAL", nullable: false),
                    GalacticLongitute = table.Column<float>(type: "REAL", nullable: false),
                    HorizontalLatitute = table.Column<float>(type: "REAL", nullable: false),
                    HorizontalLongitute = table.Column<float>(type: "REAL", nullable: false),
                    EquatorialLatitute = table.Column<float>(type: "REAL", nullable: false),
                    EquatorialLongitute = table.Column<float>(type: "REAL", nullable: false),
                    EclipticLatitute = table.Column<float>(type: "REAL", nullable: false),
                    EclipticLongitute = table.Column<float>(type: "REAL", nullable: false),
                    Size = table.Column<int>(type: "INTEGER", nullable: false),
                    Radius = table.Column<int>(type: "INTEGER", nullable: false),
                    Age = table.Column<int>(type: "INTEGER", nullable: false),
                    Mass = table.Column<int>(type: "INTEGER", nullable: false),
                    Temperature = table.Column<int>(type: "INTEGER", nullable: false),
                    Weight = table.Column<int>(type: "INTEGER", nullable: false),
                    GravitaionalPull = table.Column<int>(type: "INTEGER", nullable: false),
                    OrbitPositionFromParentStar = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentOrbitAngleOfParentStar = table.Column<int>(type: "INTEGER", nullable: false),
                    DistanceFromParentStarInMetres = table.Column<int>(type: "INTEGER", nullable: false),
                    RotationSpeed = table.Column<int>(type: "INTEGER", nullable: false),
                    TiltAngle = table.Column<int>(type: "INTEGER", nullable: false),
                    NumberRegisteredAvatars = table.Column<int>(type: "INTEGER", nullable: false),
                    NunmerActiveAvatars = table.Column<int>(type: "INTEGER", nullable: false),
                    GenesisType = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Planet", x => x.PlanetId);
                    table.ForeignKey(
                        name: "FK_Planet_Galaxy_GalaxyId",
                        column: x => x.GalaxyId,
                        principalTable: "Galaxy",
                        principalColumn: "GalaxyId");
                    table.ForeignKey(
                        name: "FK_Planet_GalaxyCluster_GalaxyClusterId",
                        column: x => x.GalaxyClusterId,
                        principalTable: "GalaxyCluster",
                        principalColumn: "GalaxyClusterId");
                    table.ForeignKey(
                        name: "FK_Planet_SolarSystem_SolarSystemId",
                        column: x => x.SolarSystemId,
                        principalTable: "SolarSystem",
                        principalColumn: "SolarSystemId");
                    table.ForeignKey(
                        name: "FK_Planet_Universe_UniverseId",
                        column: x => x.UniverseId,
                        principalTable: "Universe",
                        principalColumn: "UniverseId");
                });

            migrationBuilder.CreateTable(
                name: "Moon",
                columns: table => new
                {
                    MoonId = table.Column<string>(type: "TEXT", nullable: false),
                    ParentPlanetId = table.Column<string>(type: "TEXT", nullable: true),
                    HolonId = table.Column<string>(type: "TEXT", nullable: true),
                    SpaceQuadrant = table.Column<int>(type: "INTEGER", nullable: false),
                    SpaceSector = table.Column<int>(type: "INTEGER", nullable: false),
                    SuperGalacticLatitute = table.Column<float>(type: "REAL", nullable: false),
                    SuperGalacticLongitute = table.Column<float>(type: "REAL", nullable: false),
                    GalacticLatitute = table.Column<float>(type: "REAL", nullable: false),
                    GalacticLongitute = table.Column<float>(type: "REAL", nullable: false),
                    HorizontalLatitute = table.Column<float>(type: "REAL", nullable: false),
                    HorizontalLongitute = table.Column<float>(type: "REAL", nullable: false),
                    EquatorialLatitute = table.Column<float>(type: "REAL", nullable: false),
                    EquatorialLongitute = table.Column<float>(type: "REAL", nullable: false),
                    EclipticLatitute = table.Column<float>(type: "REAL", nullable: false),
                    EclipticLongitute = table.Column<float>(type: "REAL", nullable: false),
                    Size = table.Column<int>(type: "INTEGER", nullable: false),
                    Radius = table.Column<int>(type: "INTEGER", nullable: false),
                    Age = table.Column<int>(type: "INTEGER", nullable: false),
                    Mass = table.Column<int>(type: "INTEGER", nullable: false),
                    Temperature = table.Column<int>(type: "INTEGER", nullable: false),
                    Weight = table.Column<int>(type: "INTEGER", nullable: false),
                    GravitaionalPull = table.Column<int>(type: "INTEGER", nullable: false),
                    OrbitPositionFromParentStar = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentOrbitAngleOfParentStar = table.Column<int>(type: "INTEGER", nullable: false),
                    DistanceFromParentStarInMetres = table.Column<int>(type: "INTEGER", nullable: false),
                    RotationSpeed = table.Column<int>(type: "INTEGER", nullable: false),
                    TiltAngle = table.Column<int>(type: "INTEGER", nullable: false),
                    NumberRegisteredAvatars = table.Column<int>(type: "INTEGER", nullable: false),
                    NunmerActiveAvatars = table.Column<int>(type: "INTEGER", nullable: false),
                    GenesisType = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Moon", x => x.MoonId);
                    table.ForeignKey(
                        name: "FK_Moon_Planet_ParentPlanetId",
                        column: x => x.ParentPlanetId,
                        principalTable: "Planet",
                        principalColumn: "PlanetId");
                });

            migrationBuilder.CreateTable(
                name: "Holon",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ParentHolonId = table.Column<string>(type: "TEXT", nullable: true),
                    HolonType = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    IsNewHolon = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsChanged = table.Column<bool>(type: "INTEGER", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    ParentDimensionDimesionId = table.Column<string>(type: "TEXT", nullable: true),
                    DimensionLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    SubDimensionLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    ParentStarStarId = table.Column<string>(type: "TEXT", nullable: true),
                    ParentSuperStarStarId = table.Column<string>(type: "TEXT", nullable: true),
                    ParentGrandSuperStarStarId = table.Column<string>(type: "TEXT", nullable: true),
                    ParentGreatGrandSuperStarStarId = table.Column<string>(type: "TEXT", nullable: true),
                    ParentMoonMoonId = table.Column<string>(type: "TEXT", nullable: true),
                    ParentPlanetPlanetId = table.Column<string>(type: "TEXT", nullable: true),
                    ParentSolarSystemSolarSystemId = table.Column<string>(type: "TEXT", nullable: true),
                    ParentGalaxyGalaxyId = table.Column<string>(type: "TEXT", nullable: true),
                    ParentGalaxyClusterGalaxyClusterId = table.Column<string>(type: "TEXT", nullable: true),
                    ParentUniverseUniverseId = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedProviderType = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedOASISType = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByAvatarId = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedByAvatarId = table.Column<string>(type: "TEXT", nullable: true),
                    DeletedByAvatarId = table.Column<string>(type: "TEXT", nullable: true),
                    HolonModelId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Holon", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Holon_Dimension_ParentDimensionDimesionId",
                        column: x => x.ParentDimensionDimesionId,
                        principalTable: "Dimension",
                        principalColumn: "DimesionId");
                    table.ForeignKey(
                        name: "FK_Holon_Galaxy_ParentGalaxyGalaxyId",
                        column: x => x.ParentGalaxyGalaxyId,
                        principalTable: "Galaxy",
                        principalColumn: "GalaxyId");
                    table.ForeignKey(
                        name: "FK_Holon_GalaxyCluster_ParentGalaxyClusterGalaxyClusterId",
                        column: x => x.ParentGalaxyClusterGalaxyClusterId,
                        principalTable: "GalaxyCluster",
                        principalColumn: "GalaxyClusterId");
                    table.ForeignKey(
                        name: "FK_Holon_GrandSuperStar_ParentGrandSuperStarStarId",
                        column: x => x.ParentGrandSuperStarStarId,
                        principalTable: "GrandSuperStar",
                        principalColumn: "StarId");
                    table.ForeignKey(
                        name: "FK_Holon_GreatGrandSuperStar_ParentGreatGrandSuperStarStarId",
                        column: x => x.ParentGreatGrandSuperStarStarId,
                        principalTable: "GreatGrandSuperStar",
                        principalColumn: "StarId");
                    table.ForeignKey(
                        name: "FK_Holon_Holon_HolonModelId",
                        column: x => x.HolonModelId,
                        principalTable: "Holon",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Holon_Moon_ParentMoonMoonId",
                        column: x => x.ParentMoonMoonId,
                        principalTable: "Moon",
                        principalColumn: "MoonId");
                    table.ForeignKey(
                        name: "FK_Holon_Planet_ParentPlanetPlanetId",
                        column: x => x.ParentPlanetPlanetId,
                        principalTable: "Planet",
                        principalColumn: "PlanetId");
                    table.ForeignKey(
                        name: "FK_Holon_SolarSystem_ParentSolarSystemSolarSystemId",
                        column: x => x.ParentSolarSystemSolarSystemId,
                        principalTable: "SolarSystem",
                        principalColumn: "SolarSystemId");
                    table.ForeignKey(
                        name: "FK_Holon_Star_ParentStarStarId",
                        column: x => x.ParentStarStarId,
                        principalTable: "Star",
                        principalColumn: "StarId");
                    table.ForeignKey(
                        name: "FK_Holon_SuperStar_ParentSuperStarStarId",
                        column: x => x.ParentSuperStarStarId,
                        principalTable: "SuperStar",
                        principalColumn: "StarId");
                    table.ForeignKey(
                        name: "FK_Holon_Universe_ParentUniverseUniverseId",
                        column: x => x.ParentUniverseUniverseId,
                        principalTable: "Universe",
                        principalColumn: "UniverseId");
                });

            migrationBuilder.CreateTable(
                name: "MetaData",
                columns: table => new
                {
                    Property = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: true),
                    OwnerId = table.Column<string>(type: "TEXT", nullable: true),
                    AvatarModelId = table.Column<string>(type: "TEXT", nullable: true),
                    HolonModelId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetaData", x => x.Property);
                    table.ForeignKey(
                        name: "FK_MetaData_Avatar_AvatarModelId",
                        column: x => x.AvatarModelId,
                        principalTable: "Avatar",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MetaData_AvatarDetail_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "AvatarDetail",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MetaData_Holon_HolonModelId",
                        column: x => x.HolonModelId,
                        principalTable: "Holon",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProviderKey",
                columns: table => new
                {
                    ProviderId = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: true),
                    OwnerId = table.Column<string>(type: "TEXT", nullable: true),
                    AvatarDetailModelId = table.Column<string>(type: "TEXT", nullable: true),
                    HolonModelId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderKey", x => x.ProviderId);
                    table.ForeignKey(
                        name: "FK_ProviderKey_AvatarDetail_AvatarDetailModelId",
                        column: x => x.AvatarDetailModelId,
                        principalTable: "AvatarDetail",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProviderKey_Holon_HolonModelId",
                        column: x => x.HolonModelId,
                        principalTable: "Holon",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProviderMetaData",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProviderId = table.Column<int>(type: "INTEGER", nullable: false),
                    Property = table.Column<string>(type: "TEXT", nullable: true),
                    Value = table.Column<string>(type: "TEXT", nullable: true),
                    OwnerId = table.Column<string>(type: "TEXT", nullable: true),
                    HolonModelId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderMetaData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProviderMetaData_Holon_HolonModelId",
                        column: x => x.HolonModelId,
                        principalTable: "Holon",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Asteroid_GalaxyClusterId",
                table: "Asteroid",
                column: "GalaxyClusterId");

            migrationBuilder.CreateIndex(
                name: "IX_Asteroid_GalaxyId",
                table: "Asteroid",
                column: "GalaxyId");

            migrationBuilder.CreateIndex(
                name: "IX_Asteroid_SolarSystemId",
                table: "Asteroid",
                column: "SolarSystemId");

            migrationBuilder.CreateIndex(
                name: "IX_Asteroid_UniverseId",
                table: "Asteroid",
                column: "UniverseId");

            migrationBuilder.CreateIndex(
                name: "IX_AvatarAchievements_AvatarId",
                table: "AvatarAchievements",
                column: "AvatarId");

            migrationBuilder.CreateIndex(
                name: "IX_AvatarChakras_AvatarDetailId",
                table: "AvatarChakras",
                column: "AvatarDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_AvatarChakras_AvatarId",
                table: "AvatarChakras",
                column: "AvatarId");

            migrationBuilder.CreateIndex(
                name: "IX_AvatarGifts_AvatarChakraId",
                table: "AvatarGifts",
                column: "AvatarChakraId");

            migrationBuilder.CreateIndex(
                name: "IX_AvatarGifts_AvatarDetailId",
                table: "AvatarGifts",
                column: "AvatarDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_AvatarGifts_AvatarId",
                table: "AvatarGifts",
                column: "AvatarId");

            migrationBuilder.CreateIndex(
                name: "IX_AvatarHumanDesign_AvatarDetailId",
                table: "AvatarHumanDesign",
                column: "AvatarDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_AvatarHumanDesign_AvatarId",
                table: "AvatarHumanDesign",
                column: "AvatarId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AvatarInventory_AvatarId",
                table: "AvatarInventory",
                column: "AvatarId");

            migrationBuilder.CreateIndex(
                name: "IX_AvatarSpells_AvatarId",
                table: "AvatarSpells",
                column: "AvatarId");

            migrationBuilder.CreateIndex(
                name: "IX_AvatarStats_AvatarDetailId",
                table: "AvatarStats",
                column: "AvatarDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_AvatarStats_AvatarId",
                table: "AvatarStats",
                column: "AvatarId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Comet_GalaxyClusterId",
                table: "Comet",
                column: "GalaxyClusterId");

            migrationBuilder.CreateIndex(
                name: "IX_Comet_GalaxyId",
                table: "Comet",
                column: "GalaxyId");

            migrationBuilder.CreateIndex(
                name: "IX_Comet_SolarSystemId",
                table: "Comet",
                column: "SolarSystemId");

            migrationBuilder.CreateIndex(
                name: "IX_Comet_UniverseId",
                table: "Comet",
                column: "UniverseId");

            migrationBuilder.CreateIndex(
                name: "IX_Crystal_AvatarChakraId",
                table: "Crystal",
                column: "AvatarChakraId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Galaxy_GalaxyClusterId",
                table: "Galaxy",
                column: "GalaxyClusterId");

            migrationBuilder.CreateIndex(
                name: "IX_Galaxy_SuperStarStarId",
                table: "Galaxy",
                column: "SuperStarStarId");

            migrationBuilder.CreateIndex(
                name: "IX_GalaxyCluster_UniverseId",
                table: "GalaxyCluster",
                column: "UniverseId");

            migrationBuilder.CreateIndex(
                name: "IX_GeneKeys_AvatarDetailId",
                table: "GeneKeys",
                column: "AvatarDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_GeneKeys_AvatarId",
                table: "GeneKeys",
                column: "AvatarId");

            migrationBuilder.CreateIndex(
                name: "IX_HeartRateEntry_AvatarDetailId",
                table: "HeartRateEntry",
                column: "AvatarDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_HeartRateEntry_AvatarId",
                table: "HeartRateEntry",
                column: "AvatarId");

            migrationBuilder.CreateIndex(
                name: "IX_Holon_HolonModelId",
                table: "Holon",
                column: "HolonModelId");

            migrationBuilder.CreateIndex(
                name: "IX_Holon_ParentDimensionDimesionId",
                table: "Holon",
                column: "ParentDimensionDimesionId");

            migrationBuilder.CreateIndex(
                name: "IX_Holon_ParentGalaxyClusterGalaxyClusterId",
                table: "Holon",
                column: "ParentGalaxyClusterGalaxyClusterId");

            migrationBuilder.CreateIndex(
                name: "IX_Holon_ParentGalaxyGalaxyId",
                table: "Holon",
                column: "ParentGalaxyGalaxyId");

            migrationBuilder.CreateIndex(
                name: "IX_Holon_ParentGrandSuperStarStarId",
                table: "Holon",
                column: "ParentGrandSuperStarStarId");

            migrationBuilder.CreateIndex(
                name: "IX_Holon_ParentGreatGrandSuperStarStarId",
                table: "Holon",
                column: "ParentGreatGrandSuperStarStarId");

            migrationBuilder.CreateIndex(
                name: "IX_Holon_ParentMoonMoonId",
                table: "Holon",
                column: "ParentMoonMoonId");

            migrationBuilder.CreateIndex(
                name: "IX_Holon_ParentPlanetPlanetId",
                table: "Holon",
                column: "ParentPlanetPlanetId");

            migrationBuilder.CreateIndex(
                name: "IX_Holon_ParentSolarSystemSolarSystemId",
                table: "Holon",
                column: "ParentSolarSystemSolarSystemId");

            migrationBuilder.CreateIndex(
                name: "IX_Holon_ParentStarStarId",
                table: "Holon",
                column: "ParentStarStarId");

            migrationBuilder.CreateIndex(
                name: "IX_Holon_ParentSuperStarStarId",
                table: "Holon",
                column: "ParentSuperStarStarId");

            migrationBuilder.CreateIndex(
                name: "IX_Holon_ParentUniverseUniverseId",
                table: "Holon",
                column: "ParentUniverseUniverseId");

            migrationBuilder.CreateIndex(
                name: "IX_KarmaAkashicRecord_AvatarId",
                table: "KarmaAkashicRecord",
                column: "AvatarId");

            migrationBuilder.CreateIndex(
                name: "IX_MetaData_AvatarModelId",
                table: "MetaData",
                column: "AvatarModelId");

            migrationBuilder.CreateIndex(
                name: "IX_MetaData_HolonModelId",
                table: "MetaData",
                column: "HolonModelId");

            migrationBuilder.CreateIndex(
                name: "IX_MetaData_OwnerId",
                table: "MetaData",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Meteroid_GalaxyClusterId",
                table: "Meteroid",
                column: "GalaxyClusterId");

            migrationBuilder.CreateIndex(
                name: "IX_Meteroid_GalaxyId",
                table: "Meteroid",
                column: "GalaxyId");

            migrationBuilder.CreateIndex(
                name: "IX_Meteroid_SolarSystemId",
                table: "Meteroid",
                column: "SolarSystemId");

            migrationBuilder.CreateIndex(
                name: "IX_Meteroid_UniverseId",
                table: "Meteroid",
                column: "UniverseId");

            migrationBuilder.CreateIndex(
                name: "IX_Moon_ParentPlanetId",
                table: "Moon",
                column: "ParentPlanetId");

            migrationBuilder.CreateIndex(
                name: "IX_Nebula_GalaxyId",
                table: "Nebula",
                column: "GalaxyId");

            migrationBuilder.CreateIndex(
                name: "IX_Nebula_UniverseId",
                table: "Nebula",
                column: "UniverseId");

            migrationBuilder.CreateIndex(
                name: "IX_Planet_GalaxyClusterId",
                table: "Planet",
                column: "GalaxyClusterId");

            migrationBuilder.CreateIndex(
                name: "IX_Planet_GalaxyId",
                table: "Planet",
                column: "GalaxyId");

            migrationBuilder.CreateIndex(
                name: "IX_Planet_SolarSystemId",
                table: "Planet",
                column: "SolarSystemId");

            migrationBuilder.CreateIndex(
                name: "IX_Planet_UniverseId",
                table: "Planet",
                column: "UniverseId");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderKey_AvatarDetailModelId",
                table: "ProviderKey",
                column: "AvatarDetailModelId");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderKey_HolonModelId",
                table: "ProviderKey",
                column: "HolonModelId");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderMetaData_HolonModelId",
                table: "ProviderMetaData",
                column: "HolonModelId");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderPrivateKey_OwnerId",
                table: "ProviderPrivateKey",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderPublicKey_OwnerId",
                table: "ProviderPublicKey",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderWalletAddress_OwnerId",
                table: "ProviderWalletAddress",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_AvatarModelId",
                table: "RefreshTokens",
                column: "AvatarModelId");

            migrationBuilder.CreateIndex(
                name: "IX_SolarSystem_GalaxyClusterId",
                table: "SolarSystem",
                column: "GalaxyClusterId");

            migrationBuilder.CreateIndex(
                name: "IX_SolarSystem_GalaxyId",
                table: "SolarSystem",
                column: "GalaxyId");

            migrationBuilder.CreateIndex(
                name: "IX_SolarSystem_StarId",
                table: "SolarSystem",
                column: "StarId");

            migrationBuilder.CreateIndex(
                name: "IX_SolarSystem_UniverseId",
                table: "SolarSystem",
                column: "UniverseId");

            migrationBuilder.CreateIndex(
                name: "IX_Star_GalaxyClusterId",
                table: "Star",
                column: "GalaxyClusterId");

            migrationBuilder.CreateIndex(
                name: "IX_Star_GalaxyId",
                table: "Star",
                column: "GalaxyId");

            migrationBuilder.CreateIndex(
                name: "IX_Star_UniverseId",
                table: "Star",
                column: "UniverseId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Asteroid");

            migrationBuilder.DropTable(
                name: "AvatarAchievements");

            migrationBuilder.DropTable(
                name: "AvatarAttributes");

            migrationBuilder.DropTable(
                name: "AvatarAura");

            migrationBuilder.DropTable(
                name: "AvatarGifts");

            migrationBuilder.DropTable(
                name: "AvatarHumanDesign");

            migrationBuilder.DropTable(
                name: "AvatarInventory");

            migrationBuilder.DropTable(
                name: "AvatarSkills");

            migrationBuilder.DropTable(
                name: "AvatarSpells");

            migrationBuilder.DropTable(
                name: "AvatarStats");

            migrationBuilder.DropTable(
                name: "AvatarSuperPowers");

            migrationBuilder.DropTable(
                name: "Comet");

            migrationBuilder.DropTable(
                name: "Crystal");

            migrationBuilder.DropTable(
                name: "GeneKeys");

            migrationBuilder.DropTable(
                name: "HeartRateEntry");

            migrationBuilder.DropTable(
                name: "KarmaAkashicRecord");

            migrationBuilder.DropTable(
                name: "MetaData");

            migrationBuilder.DropTable(
                name: "Meteroid");

            migrationBuilder.DropTable(
                name: "Nebula");

            migrationBuilder.DropTable(
                name: "ProviderKey");

            migrationBuilder.DropTable(
                name: "ProviderMetaData");

            migrationBuilder.DropTable(
                name: "ProviderPrivateKey");

            migrationBuilder.DropTable(
                name: "ProviderPublicKey");

            migrationBuilder.DropTable(
                name: "ProviderWalletAddress");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "AvatarChakras");

            migrationBuilder.DropTable(
                name: "Holon");

            migrationBuilder.DropTable(
                name: "Avatar");

            migrationBuilder.DropTable(
                name: "AvatarDetail");

            migrationBuilder.DropTable(
                name: "Dimension");

            migrationBuilder.DropTable(
                name: "GrandSuperStar");

            migrationBuilder.DropTable(
                name: "GreatGrandSuperStar");

            migrationBuilder.DropTable(
                name: "Moon");

            migrationBuilder.DropTable(
                name: "Planet");

            migrationBuilder.DropTable(
                name: "SolarSystem");

            migrationBuilder.DropTable(
                name: "Star");

            migrationBuilder.DropTable(
                name: "Galaxy");

            migrationBuilder.DropTable(
                name: "GalaxyCluster");

            migrationBuilder.DropTable(
                name: "SuperStar");

            migrationBuilder.DropTable(
                name: "Universe");
        }
    }
}
