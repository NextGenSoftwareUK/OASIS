using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_HolonEntities",
                table: "HolonEntities");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AvatarEntities",
                table: "AvatarEntities");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AvatarDetailEntities",
                table: "AvatarDetailEntities");

            migrationBuilder.DropColumn(
                name: "CreatedByAvatarId",
                table: "HolonEntities");

            migrationBuilder.DropColumn(
                name: "DeletedByAvatarId",
                table: "HolonEntities");

            migrationBuilder.DropColumn(
                name: "ModifiedByAvatarId",
                table: "HolonEntities");

            migrationBuilder.DropColumn(
                name: "CreatedByAvatarId",
                table: "AvatarEntities");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "AvatarEntities");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "AvatarEntities");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "AvatarEntities");

            migrationBuilder.DropColumn(
                name: "ModifiedByAvatarId",
                table: "AvatarEntities");

            migrationBuilder.DropColumn(
                name: "ModifiedDate",
                table: "AvatarEntities");

            migrationBuilder.DropColumn(
                name: "PreviousVersionId",
                table: "AvatarEntities");

            migrationBuilder.DropColumn(
                name: "HolonId",
                table: "AvatarDetailEntities");

            migrationBuilder.DropColumn(
                name: "Image2D",
                table: "AvatarDetailEntities");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "AvatarDetailEntities");

            migrationBuilder.RenameTable(
                name: "HolonEntities",
                newName: "holons");

            migrationBuilder.RenameTable(
                name: "AvatarEntities",
                newName: "avatars");

            migrationBuilder.RenameTable(
                name: "AvatarDetailEntities",
                newName: "avatar_details");

            migrationBuilder.RenameColumn(
                name: "Version",
                table: "holons",
                newName: "version");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "holons",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "holons",
                newName: "description");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "holons",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "PreviousVersionId",
                table: "holons",
                newName: "previous_version_id");

            migrationBuilder.RenameColumn(
                name: "ParentZomeId",
                table: "holons",
                newName: "parent_zome_id");

            migrationBuilder.RenameColumn(
                name: "ParentUniverseId",
                table: "holons",
                newName: "parent_universe_id");

            migrationBuilder.RenameColumn(
                name: "ParentSuperStarId",
                table: "holons",
                newName: "parent_super_star_id");

            migrationBuilder.RenameColumn(
                name: "ParentStarId",
                table: "holons",
                newName: "parent_star_id");

            migrationBuilder.RenameColumn(
                name: "ParentSolarSystemId",
                table: "holons",
                newName: "parent_solar_system_id");

            migrationBuilder.RenameColumn(
                name: "ParentPlanetId",
                table: "holons",
                newName: "parent_planet_id");

            migrationBuilder.RenameColumn(
                name: "ParentOmniverseId",
                table: "holons",
                newName: "parent_omniverse_id");

            migrationBuilder.RenameColumn(
                name: "ParentMultiverseId",
                table: "holons",
                newName: "parent_multiverse_id");

            migrationBuilder.RenameColumn(
                name: "ParentMoonId",
                table: "holons",
                newName: "parent_moon_id");

            migrationBuilder.RenameColumn(
                name: "ParentHolonId",
                table: "holons",
                newName: "parent_holon_id");

            migrationBuilder.RenameColumn(
                name: "ParentGreatGrandSuperStarId",
                table: "holons",
                newName: "parent_great_grand_super_star_id");

            migrationBuilder.RenameColumn(
                name: "ParentGrandSuperStarId",
                table: "holons",
                newName: "parent_grand_super_star_id");

            migrationBuilder.RenameColumn(
                name: "ParentGalaxyId",
                table: "holons",
                newName: "parent_galaxy_id");

            migrationBuilder.RenameColumn(
                name: "ParentGalaxyClusterId",
                table: "holons",
                newName: "parent_galaxy_cluster_id");

            migrationBuilder.RenameColumn(
                name: "ParentDimensionId",
                table: "holons",
                newName: "parent_dimension_id");

            migrationBuilder.RenameColumn(
                name: "ParentCelestialSpaceId",
                table: "holons",
                newName: "parent_celestial_space_id");

            migrationBuilder.RenameColumn(
                name: "ParentCelestialBodyId",
                table: "holons",
                newName: "parent_celestial_body_id");

            migrationBuilder.RenameColumn(
                name: "ModifiedDate",
                table: "holons",
                newName: "modified_date");

            migrationBuilder.RenameColumn(
                name: "IsChanged",
                table: "holons",
                newName: "is_changed");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "holons",
                newName: "is_active");

            migrationBuilder.RenameColumn(
                name: "HolonId",
                table: "holons",
                newName: "holon_id");

            migrationBuilder.RenameColumn(
                name: "DeletedDate",
                table: "holons",
                newName: "deleted_date");

            migrationBuilder.RenameColumn(
                name: "CreatedDate",
                table: "holons",
                newName: "created_date");

            migrationBuilder.RenameColumn(
                name: "Version",
                table: "avatars",
                newName: "version");

            migrationBuilder.RenameColumn(
                name: "Verified",
                table: "avatars",
                newName: "verified");

            migrationBuilder.RenameColumn(
                name: "Username",
                table: "avatars",
                newName: "username");

            migrationBuilder.RenameColumn(
                name: "Title",
                table: "avatars",
                newName: "title");

            migrationBuilder.RenameColumn(
                name: "Password",
                table: "avatars",
                newName: "password");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "avatars",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Email",
                table: "avatars",
                newName: "email");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "avatars",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "VerificationToken",
                table: "avatars",
                newName: "verification_token");

            migrationBuilder.RenameColumn(
                name: "ResetTokenExpires",
                table: "avatars",
                newName: "reset_token_expires");

            migrationBuilder.RenameColumn(
                name: "ResetToken",
                table: "avatars",
                newName: "reset_token");

            migrationBuilder.RenameColumn(
                name: "RefreshToken",
                table: "avatars",
                newName: "refresh_token");

            migrationBuilder.RenameColumn(
                name: "PasswordReset",
                table: "avatars",
                newName: "password_reset");

            migrationBuilder.RenameColumn(
                name: "LastName",
                table: "avatars",
                newName: "last_name");

            migrationBuilder.RenameColumn(
                name: "LastBeamedOut",
                table: "avatars",
                newName: "last_beamed_out");

            migrationBuilder.RenameColumn(
                name: "LastBeamedIn",
                table: "avatars",
                newName: "last_beamed_in");

            migrationBuilder.RenameColumn(
                name: "JwtToken",
                table: "avatars",
                newName: "jwt_token");

            migrationBuilder.RenameColumn(
                name: "IsBeamedIn",
                table: "avatars",
                newName: "is_beamed_in");

            migrationBuilder.RenameColumn(
                name: "HolonId",
                table: "avatars",
                newName: "holon_id");

            migrationBuilder.RenameColumn(
                name: "FirstName",
                table: "avatars",
                newName: "first_name");

            migrationBuilder.RenameColumn(
                name: "DeletedDate",
                table: "avatars",
                newName: "deleted_date");

            migrationBuilder.RenameColumn(
                name: "DeletedByAvatarId",
                table: "avatars",
                newName: "deleted_by_avatar_id");

            migrationBuilder.RenameColumn(
                name: "AcceptTerms",
                table: "avatars",
                newName: "accept_terms");

            migrationBuilder.RenameColumn(
                name: "IsChanged",
                table: "avatars",
                newName: "avatar_type");

            migrationBuilder.RenameColumn(
                name: "XP",
                table: "avatar_details",
                newName: "xp");

            migrationBuilder.RenameColumn(
                name: "Version",
                table: "avatar_details",
                newName: "version");

            migrationBuilder.RenameColumn(
                name: "Username",
                table: "avatar_details",
                newName: "username");

            migrationBuilder.RenameColumn(
                name: "Town",
                table: "avatar_details",
                newName: "town");

            migrationBuilder.RenameColumn(
                name: "Postcode",
                table: "avatar_details",
                newName: "postcode");

            migrationBuilder.RenameColumn(
                name: "Mobile",
                table: "avatar_details",
                newName: "mobile");

            migrationBuilder.RenameColumn(
                name: "Level",
                table: "avatar_details",
                newName: "level");

            migrationBuilder.RenameColumn(
                name: "Landline",
                table: "avatar_details",
                newName: "landline");

            migrationBuilder.RenameColumn(
                name: "Karma",
                table: "avatar_details",
                newName: "karma");

            migrationBuilder.RenameColumn(
                name: "Email",
                table: "avatar_details",
                newName: "email");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "avatar_details",
                newName: "description");

            migrationBuilder.RenameColumn(
                name: "DOB",
                table: "avatar_details",
                newName: "dob");

            migrationBuilder.RenameColumn(
                name: "Country",
                table: "avatar_details",
                newName: "country");

            migrationBuilder.RenameColumn(
                name: "Address",
                table: "avatar_details",
                newName: "address");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "avatar_details",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UmaJson",
                table: "avatar_details",
                newName: "uma_json");

            migrationBuilder.RenameColumn(
                name: "PreviousVersionId",
                table: "avatar_details",
                newName: "previous_version_id");

            migrationBuilder.RenameColumn(
                name: "ParentZomeId",
                table: "avatar_details",
                newName: "parent_zome_Id");

            migrationBuilder.RenameColumn(
                name: "ParentUniverseId",
                table: "avatar_details",
                newName: "parent_universe_id");

            migrationBuilder.RenameColumn(
                name: "ParentSuperStarId",
                table: "avatar_details",
                newName: "parent_super_star_id");

            migrationBuilder.RenameColumn(
                name: "ParentStarId",
                table: "avatar_details",
                newName: "parent_star_id");

            migrationBuilder.RenameColumn(
                name: "ParentSolarSystemId",
                table: "avatar_details",
                newName: "parent_solar_system_id");

            migrationBuilder.RenameColumn(
                name: "ParentPlanetId",
                table: "avatar_details",
                newName: "parent_planet_id");

            migrationBuilder.RenameColumn(
                name: "ParentOmniverseId",
                table: "avatar_details",
                newName: "parent_omniverse_id");

            migrationBuilder.RenameColumn(
                name: "ParentMultiverseId",
                table: "avatar_details",
                newName: "parent_multiverse_id");

            migrationBuilder.RenameColumn(
                name: "ParentMoonId",
                table: "avatar_details",
                newName: "parent_moon_id");

            migrationBuilder.RenameColumn(
                name: "ParentHolonId",
                table: "avatar_details",
                newName: "parent_holon_id");

            migrationBuilder.RenameColumn(
                name: "ParentGreatGrandSuperStarId",
                table: "avatar_details",
                newName: "parent_great_grand_super_star_id");

            migrationBuilder.RenameColumn(
                name: "ParentGrandSuperStarId",
                table: "avatar_details",
                newName: "parent_grand_super_star_id");

            migrationBuilder.RenameColumn(
                name: "ParentGalaxyId",
                table: "avatar_details",
                newName: "parent_galaxy_id");

            migrationBuilder.RenameColumn(
                name: "ParentGalaxyClusterId",
                table: "avatar_details",
                newName: "parent_galaxy_cluster_id");

            migrationBuilder.RenameColumn(
                name: "ParentDimensionId",
                table: "avatar_details",
                newName: "parent_dimension_id");

            migrationBuilder.RenameColumn(
                name: "ParentCelestialSpaceId",
                table: "avatar_details",
                newName: "parent_celestial_space_id");

            migrationBuilder.RenameColumn(
                name: "ParentCelestialBodyId",
                table: "avatar_details",
                newName: "parent_celestial_body_id");

            migrationBuilder.RenameColumn(
                name: "ModifiedDate",
                table: "avatar_details",
                newName: "modified_date");

            migrationBuilder.RenameColumn(
                name: "ModifiedByAvatarId",
                table: "avatar_details",
                newName: "modified_by_avatar_id");

            migrationBuilder.RenameColumn(
                name: "IsChanged",
                table: "avatar_details",
                newName: "is_changed");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "avatar_details",
                newName: "is_active");

            migrationBuilder.RenameColumn(
                name: "DeletedDate",
                table: "avatar_details",
                newName: "deleted_date");

            migrationBuilder.RenameColumn(
                name: "DeletedByAvatarId",
                table: "avatar_details",
                newName: "deleted_by_avatar_id");

            migrationBuilder.RenameColumn(
                name: "CreatedDate",
                table: "avatar_details",
                newName: "created_date");

            migrationBuilder.RenameColumn(
                name: "CreatedByAvatarId",
                table: "avatar_details",
                newName: "created_by_avatar_id");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "holons",
                type: "NVARCHAR(100)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "description",
                table: "holons",
                type: "NVARCHAR(300)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                table: "holons",
                type: "NVARCHAR(64)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<Guid>(
                name: "previous_version_id",
                table: "holons",
                type: "NVARCHAR(64)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<Guid>(
                name: "parent_zome_id",
                table: "holons",
                type: "NVARCHAR(64)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<Guid>(
                name: "parent_universe_id",
                table: "holons",
                type: "NVARCHAR(64)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<Guid>(
                name: "parent_super_star_id",
                table: "holons",
                type: "NVARCHAR(64)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<Guid>(
                name: "parent_star_id",
                table: "holons",
                type: "NVARCHAR(64)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<Guid>(
                name: "parent_solar_system_id",
                table: "holons",
                type: "NVARCHAR(64)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<Guid>(
                name: "parent_planet_id",
                table: "holons",
                type: "NVARCHAR(64)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<Guid>(
                name: "parent_omniverse_id",
                table: "holons",
                type: "NVARCHAR(64)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<Guid>(
                name: "parent_multiverse_id",
                table: "holons",
                type: "NVARCHAR(64)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<Guid>(
                name: "parent_moon_id",
                table: "holons",
                type: "NVARCHAR(64)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<Guid>(
                name: "parent_holon_id",
                table: "holons",
                type: "NVARCHAR(64)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<Guid>(
                name: "parent_great_grand_super_star_id",
                table: "holons",
                type: "NVARCHAR(64)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<Guid>(
                name: "parent_grand_super_star_id",
                table: "holons",
                type: "NVARCHAR(64)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<Guid>(
                name: "parent_galaxy_id",
                table: "holons",
                type: "NVARCHAR(64)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<Guid>(
                name: "parent_galaxy_cluster_id",
                table: "holons",
                type: "NVARCHAR(64)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<Guid>(
                name: "parent_dimension_id",
                table: "holons",
                type: "NVARCHAR(64)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<Guid>(
                name: "parent_celestial_space_id",
                table: "holons",
                type: "NVARCHAR(64)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<Guid>(
                name: "parent_celestial_body_id",
                table: "holons",
                type: "NVARCHAR(64)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<DateTime>(
                name: "modified_date",
                table: "holons",
                type: "DATETIME",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<bool>(
                name: "is_changed",
                table: "holons",
                type: "BOOLEAN",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<bool>(
                name: "is_active",
                table: "holons",
                type: "BOOLEAN",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<Guid>(
                name: "holon_id",
                table: "holons",
                type: "NVARCHAR(64)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<DateTime>(
                name: "deleted_date",
                table: "holons",
                type: "DATETIME",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_date",
                table: "holons",
                type: "DATETIME",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AddColumn<long>(
                name: "created_oasis_type",
                table: "holons",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "created_provider_type",
                table: "holons",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "dimension_level",
                table: "holons",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<bool>(
                name: "is_new_holon",
                table: "holons",
                type: "BOOLEAN",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_saving",
                table: "holons",
                type: "BOOLEAN",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "sub_dimension_level",
                table: "holons",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<Guid>(
                name: "version_id",
                table: "holons",
                type: "NVARCHAR(64)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<DateTime>(
                name: "verified",
                table: "avatars",
                type: "DATETIME",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "username",
                table: "avatars",
                type: "NVARCHAR(100)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "title",
                table: "avatars",
                type: "NVARCHAR(200)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "password",
                table: "avatars",
                type: "NVARCHAR(100)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "avatars",
                type: "NVARCHAR(100)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "email",
                table: "avatars",
                type: "NVARCHAR(20)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "id",
                table: "avatars",
                type: "NVARCHAR(64)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "verification_token",
                table: "avatars",
                type: "NVARCHAR(100)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<DateTime>(
                name: "reset_token_expires",
                table: "avatars",
                type: "DATETIME",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "reset_token",
                table: "avatars",
                type: "NVARCHAR(100)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "refresh_token",
                table: "avatars",
                type: "NVARCHAR(100)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<DateTime>(
                name: "password_reset",
                table: "avatars",
                type: "DATETIME",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "last_name",
                table: "avatars",
                type: "NVARCHAR(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<DateTime>(
                name: "last_beamed_out",
                table: "avatars",
                type: "DATETIME",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "last_beamed_in",
                table: "avatars",
                type: "DATETIME",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "jwt_token",
                table: "avatars",
                type: "NVARCHAR(100)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<bool>(
                name: "is_beamed_in",
                table: "avatars",
                type: "BOOLEAN",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<Guid>(
                name: "holon_id",
                table: "avatars",
                type: "NVARCHAR(64)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "first_name",
                table: "avatars",
                type: "NVARCHAR(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<DateTime>(
                name: "deleted_date",
                table: "avatars",
                type: "DATETIME",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<Guid>(
                name: "deleted_by_avatar_id",
                table: "avatars",
                type: "NVARCHAR(64)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<bool>(
                name: "accept_terms",
                table: "avatars",
                type: "BOOLEAN",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<string>(
                name: "avatar_id",
                table: "avatars",
                type: "NVARCHAR(64)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "username",
                table: "avatar_details",
                type: "NVARCHAR(100)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "town",
                table: "avatar_details",
                type: "NVARCHAR(100)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "postcode",
                table: "avatar_details",
                type: "NVARCHAR(100)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "mobile",
                table: "avatar_details",
                type: "NVARCHAR(100)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "landline",
                table: "avatar_details",
                type: "NVARCHAR(100)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "email",
                table: "avatar_details",
                type: "NVARCHAR(100)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "description",
                table: "avatar_details",
                type: "NVARCHAR(300)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<DateTime>(
                name: "dob",
                table: "avatar_details",
                type: "DATETIME",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "country",
                table: "avatar_details",
                type: "NVARCHAR(100)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "address",
                table: "avatar_details",
                type: "NVARCHAR(100)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                table: "avatar_details",
                type: "NVARCHAR(64)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "uma_json",
                table: "avatar_details",
                type: "NVARCHAR(100)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<Guid>(
                name: "previous_version_id",
                table: "avatar_details",
                type: "NVARCHAR(64)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<Guid>(
                name: "parent_zome_Id",
                table: "avatar_details",
                type: "NVARCHAR(64)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<Guid>(
                name: "parent_universe_id",
                table: "avatar_details",
                type: "NVARCHAR(64)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<Guid>(
                name: "parent_super_star_id",
                table: "avatar_details",
                type: "NVARCHAR(64)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<Guid>(
                name: "parent_star_id",
                table: "avatar_details",
                type: "NVARCHAR(64)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<Guid>(
                name: "parent_solar_system_id",
                table: "avatar_details",
                type: "NVARCHAR(64)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<Guid>(
                name: "parent_planet_id",
                table: "avatar_details",
                type: "NVARCHAR(64)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<Guid>(
                name: "parent_omniverse_id",
                table: "avatar_details",
                type: "NVARCHAR(64)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<Guid>(
                name: "parent_multiverse_id",
                table: "avatar_details",
                type: "NVARCHAR(64)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<Guid>(
                name: "parent_moon_id",
                table: "avatar_details",
                type: "NVARCHAR(64)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<Guid>(
                name: "parent_holon_id",
                table: "avatar_details",
                type: "NVARCHAR(64)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<Guid>(
                name: "parent_great_grand_super_star_id",
                table: "avatar_details",
                type: "NVARCHAR(64)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<Guid>(
                name: "parent_grand_super_star_id",
                table: "avatar_details",
                type: "NVARCHAR(64)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<Guid>(
                name: "parent_galaxy_id",
                table: "avatar_details",
                type: "NVARCHAR(64)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<Guid>(
                name: "parent_galaxy_cluster_id",
                table: "avatar_details",
                type: "NVARCHAR(64)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<Guid>(
                name: "parent_dimension_id",
                table: "avatar_details",
                type: "NVARCHAR(64)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<Guid>(
                name: "parent_celestial_space_id",
                table: "avatar_details",
                type: "NVARCHAR(64)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<Guid>(
                name: "parent_celestial_body_id",
                table: "avatar_details",
                type: "NVARCHAR(64)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<DateTime>(
                name: "modified_date",
                table: "avatar_details",
                type: "DATETIME",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<Guid>(
                name: "modified_by_avatar_id",
                table: "avatar_details",
                type: "NVARCHAR(64)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<bool>(
                name: "is_changed",
                table: "avatar_details",
                type: "BOOLEAN",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<bool>(
                name: "is_active",
                table: "avatar_details",
                type: "BOOLEAN",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<DateTime>(
                name: "deleted_date",
                table: "avatar_details",
                type: "DATETIME",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<Guid>(
                name: "deleted_by_avatar_id",
                table: "avatar_details",
                type: "NVARCHAR(64)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_date",
                table: "avatar_details",
                type: "DATETIME",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<Guid>(
                name: "created_by_avatar_id",
                table: "avatar_details",
                type: "NVARCHAR(64)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddColumn<string>(
                name: "county",
                table: "avatar_details",
                type: "NVARCHAR(100)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "created_oasis_type",
                table: "avatar_details",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "created_provider_type",
                table: "avatar_details",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "dimension_level",
                table: "avatar_details",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "favourite_colour",
                table: "avatar_details",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<bool>(
                name: "is_new_holon",
                table: "avatar_details",
                type: "BOOLEAN",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_saving",
                table: "avatar_details",
                type: "BOOLEAN",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "model_3d",
                table: "avatar_details",
                type: "NVARCHAR(100)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "portrait",
                table: "avatar_details",
                type: "NVARCHAR(100)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "star_cli_colour",
                table: "avatar_details",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "sub_dimension_level",
                table: "avatar_details",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<Guid>(
                name: "version_id",
                table: "avatar_details",
                type: "NVARCHAR(64)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddPrimaryKey(
                name: "PK_holons",
                table: "holons",
                column: "holon_id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_avatars",
                table: "avatars",
                columns: new[] { "id", "avatar_id" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_avatar_details",
                table: "avatar_details",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "idx_holon_id",
                table: "holons",
                column: "holon_id");

            migrationBuilder.CreateIndex(
                name: "IX_holons_holon_id",
                table: "holons",
                column: "holon_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_avatar_id",
                table: "avatars",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "IX_avatars_id_avatar_id_email",
                table: "avatars",
                columns: new[] { "id", "avatar_id", "email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_avatar_dtl_id",
                table: "avatar_details",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "IX_avatar_details_id_email",
                table: "avatar_details",
                columns: new[] { "id", "email" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_holons",
                table: "holons");

            migrationBuilder.DropIndex(
                name: "idx_holon_id",
                table: "holons");

            migrationBuilder.DropIndex(
                name: "IX_holons_holon_id",
                table: "holons");

            migrationBuilder.DropPrimaryKey(
                name: "PK_avatars",
                table: "avatars");

            migrationBuilder.DropIndex(
                name: "idx_avatar_id",
                table: "avatars");

            migrationBuilder.DropIndex(
                name: "IX_avatars_id_avatar_id_email",
                table: "avatars");

            migrationBuilder.DropPrimaryKey(
                name: "PK_avatar_details",
                table: "avatar_details");

            migrationBuilder.DropIndex(
                name: "idx_avatar_dtl_id",
                table: "avatar_details");

            migrationBuilder.DropIndex(
                name: "IX_avatar_details_id_email",
                table: "avatar_details");

            migrationBuilder.DropColumn(
                name: "created_oasis_type",
                table: "holons");

            migrationBuilder.DropColumn(
                name: "created_provider_type",
                table: "holons");

            migrationBuilder.DropColumn(
                name: "dimension_level",
                table: "holons");

            migrationBuilder.DropColumn(
                name: "is_new_holon",
                table: "holons");

            migrationBuilder.DropColumn(
                name: "is_saving",
                table: "holons");

            migrationBuilder.DropColumn(
                name: "sub_dimension_level",
                table: "holons");

            migrationBuilder.DropColumn(
                name: "version_id",
                table: "holons");

            migrationBuilder.DropColumn(
                name: "avatar_id",
                table: "avatars");

            migrationBuilder.DropColumn(
                name: "county",
                table: "avatar_details");

            migrationBuilder.DropColumn(
                name: "created_oasis_type",
                table: "avatar_details");

            migrationBuilder.DropColumn(
                name: "created_provider_type",
                table: "avatar_details");

            migrationBuilder.DropColumn(
                name: "dimension_level",
                table: "avatar_details");

            migrationBuilder.DropColumn(
                name: "favourite_colour",
                table: "avatar_details");

            migrationBuilder.DropColumn(
                name: "is_new_holon",
                table: "avatar_details");

            migrationBuilder.DropColumn(
                name: "is_saving",
                table: "avatar_details");

            migrationBuilder.DropColumn(
                name: "model_3d",
                table: "avatar_details");

            migrationBuilder.DropColumn(
                name: "portrait",
                table: "avatar_details");

            migrationBuilder.DropColumn(
                name: "star_cli_colour",
                table: "avatar_details");

            migrationBuilder.DropColumn(
                name: "sub_dimension_level",
                table: "avatar_details");

            migrationBuilder.DropColumn(
                name: "version_id",
                table: "avatar_details");

            migrationBuilder.RenameTable(
                name: "holons",
                newName: "HolonEntities");

            migrationBuilder.RenameTable(
                name: "avatars",
                newName: "AvatarEntities");

            migrationBuilder.RenameTable(
                name: "avatar_details",
                newName: "AvatarDetailEntities");

            migrationBuilder.RenameColumn(
                name: "version",
                table: "HolonEntities",
                newName: "Version");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "HolonEntities",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "HolonEntities",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "description",
                table: "HolonEntities",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "previous_version_id",
                table: "HolonEntities",
                newName: "PreviousVersionId");

            migrationBuilder.RenameColumn(
                name: "parent_zome_id",
                table: "HolonEntities",
                newName: "ParentZomeId");

            migrationBuilder.RenameColumn(
                name: "parent_universe_id",
                table: "HolonEntities",
                newName: "ParentUniverseId");

            migrationBuilder.RenameColumn(
                name: "parent_super_star_id",
                table: "HolonEntities",
                newName: "ParentSuperStarId");

            migrationBuilder.RenameColumn(
                name: "parent_star_id",
                table: "HolonEntities",
                newName: "ParentStarId");

            migrationBuilder.RenameColumn(
                name: "parent_solar_system_id",
                table: "HolonEntities",
                newName: "ParentSolarSystemId");

            migrationBuilder.RenameColumn(
                name: "parent_planet_id",
                table: "HolonEntities",
                newName: "ParentPlanetId");

            migrationBuilder.RenameColumn(
                name: "parent_omniverse_id",
                table: "HolonEntities",
                newName: "ParentOmniverseId");

            migrationBuilder.RenameColumn(
                name: "parent_multiverse_id",
                table: "HolonEntities",
                newName: "ParentMultiverseId");

            migrationBuilder.RenameColumn(
                name: "parent_moon_id",
                table: "HolonEntities",
                newName: "ParentMoonId");

            migrationBuilder.RenameColumn(
                name: "parent_holon_id",
                table: "HolonEntities",
                newName: "ParentHolonId");

            migrationBuilder.RenameColumn(
                name: "parent_great_grand_super_star_id",
                table: "HolonEntities",
                newName: "ParentGreatGrandSuperStarId");

            migrationBuilder.RenameColumn(
                name: "parent_grand_super_star_id",
                table: "HolonEntities",
                newName: "ParentGrandSuperStarId");

            migrationBuilder.RenameColumn(
                name: "parent_galaxy_id",
                table: "HolonEntities",
                newName: "ParentGalaxyId");

            migrationBuilder.RenameColumn(
                name: "parent_galaxy_cluster_id",
                table: "HolonEntities",
                newName: "ParentGalaxyClusterId");

            migrationBuilder.RenameColumn(
                name: "parent_dimension_id",
                table: "HolonEntities",
                newName: "ParentDimensionId");

            migrationBuilder.RenameColumn(
                name: "parent_celestial_space_id",
                table: "HolonEntities",
                newName: "ParentCelestialSpaceId");

            migrationBuilder.RenameColumn(
                name: "parent_celestial_body_id",
                table: "HolonEntities",
                newName: "ParentCelestialBodyId");

            migrationBuilder.RenameColumn(
                name: "modified_date",
                table: "HolonEntities",
                newName: "ModifiedDate");

            migrationBuilder.RenameColumn(
                name: "is_changed",
                table: "HolonEntities",
                newName: "IsChanged");

            migrationBuilder.RenameColumn(
                name: "is_active",
                table: "HolonEntities",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "deleted_date",
                table: "HolonEntities",
                newName: "DeletedDate");

            migrationBuilder.RenameColumn(
                name: "created_date",
                table: "HolonEntities",
                newName: "CreatedDate");

            migrationBuilder.RenameColumn(
                name: "holon_id",
                table: "HolonEntities",
                newName: "HolonId");

            migrationBuilder.RenameColumn(
                name: "version",
                table: "AvatarEntities",
                newName: "Version");

            migrationBuilder.RenameColumn(
                name: "verified",
                table: "AvatarEntities",
                newName: "Verified");

            migrationBuilder.RenameColumn(
                name: "username",
                table: "AvatarEntities",
                newName: "Username");

            migrationBuilder.RenameColumn(
                name: "title",
                table: "AvatarEntities",
                newName: "Title");

            migrationBuilder.RenameColumn(
                name: "password",
                table: "AvatarEntities",
                newName: "Password");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "AvatarEntities",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "email",
                table: "AvatarEntities",
                newName: "Email");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "AvatarEntities",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "verification_token",
                table: "AvatarEntities",
                newName: "VerificationToken");

            migrationBuilder.RenameColumn(
                name: "reset_token_expires",
                table: "AvatarEntities",
                newName: "ResetTokenExpires");

            migrationBuilder.RenameColumn(
                name: "reset_token",
                table: "AvatarEntities",
                newName: "ResetToken");

            migrationBuilder.RenameColumn(
                name: "refresh_token",
                table: "AvatarEntities",
                newName: "RefreshToken");

            migrationBuilder.RenameColumn(
                name: "password_reset",
                table: "AvatarEntities",
                newName: "PasswordReset");

            migrationBuilder.RenameColumn(
                name: "last_name",
                table: "AvatarEntities",
                newName: "LastName");

            migrationBuilder.RenameColumn(
                name: "last_beamed_out",
                table: "AvatarEntities",
                newName: "LastBeamedOut");

            migrationBuilder.RenameColumn(
                name: "last_beamed_in",
                table: "AvatarEntities",
                newName: "LastBeamedIn");

            migrationBuilder.RenameColumn(
                name: "jwt_token",
                table: "AvatarEntities",
                newName: "JwtToken");

            migrationBuilder.RenameColumn(
                name: "is_beamed_in",
                table: "AvatarEntities",
                newName: "IsBeamedIn");

            migrationBuilder.RenameColumn(
                name: "holon_id",
                table: "AvatarEntities",
                newName: "HolonId");

            migrationBuilder.RenameColumn(
                name: "first_name",
                table: "AvatarEntities",
                newName: "FirstName");

            migrationBuilder.RenameColumn(
                name: "deleted_date",
                table: "AvatarEntities",
                newName: "DeletedDate");

            migrationBuilder.RenameColumn(
                name: "deleted_by_avatar_id",
                table: "AvatarEntities",
                newName: "DeletedByAvatarId");

            migrationBuilder.RenameColumn(
                name: "accept_terms",
                table: "AvatarEntities",
                newName: "AcceptTerms");

            migrationBuilder.RenameColumn(
                name: "avatar_type",
                table: "AvatarEntities",
                newName: "IsChanged");

            migrationBuilder.RenameColumn(
                name: "xp",
                table: "AvatarDetailEntities",
                newName: "XP");

            migrationBuilder.RenameColumn(
                name: "version",
                table: "AvatarDetailEntities",
                newName: "Version");

            migrationBuilder.RenameColumn(
                name: "username",
                table: "AvatarDetailEntities",
                newName: "Username");

            migrationBuilder.RenameColumn(
                name: "town",
                table: "AvatarDetailEntities",
                newName: "Town");

            migrationBuilder.RenameColumn(
                name: "postcode",
                table: "AvatarDetailEntities",
                newName: "Postcode");

            migrationBuilder.RenameColumn(
                name: "mobile",
                table: "AvatarDetailEntities",
                newName: "Mobile");

            migrationBuilder.RenameColumn(
                name: "level",
                table: "AvatarDetailEntities",
                newName: "Level");

            migrationBuilder.RenameColumn(
                name: "landline",
                table: "AvatarDetailEntities",
                newName: "Landline");

            migrationBuilder.RenameColumn(
                name: "karma",
                table: "AvatarDetailEntities",
                newName: "Karma");

            migrationBuilder.RenameColumn(
                name: "email",
                table: "AvatarDetailEntities",
                newName: "Email");

            migrationBuilder.RenameColumn(
                name: "dob",
                table: "AvatarDetailEntities",
                newName: "DOB");

            migrationBuilder.RenameColumn(
                name: "description",
                table: "AvatarDetailEntities",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "country",
                table: "AvatarDetailEntities",
                newName: "Country");

            migrationBuilder.RenameColumn(
                name: "address",
                table: "AvatarDetailEntities",
                newName: "Address");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "AvatarDetailEntities",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "uma_json",
                table: "AvatarDetailEntities",
                newName: "UmaJson");

            migrationBuilder.RenameColumn(
                name: "previous_version_id",
                table: "AvatarDetailEntities",
                newName: "PreviousVersionId");

            migrationBuilder.RenameColumn(
                name: "parent_zome_Id",
                table: "AvatarDetailEntities",
                newName: "ParentZomeId");

            migrationBuilder.RenameColumn(
                name: "parent_universe_id",
                table: "AvatarDetailEntities",
                newName: "ParentUniverseId");

            migrationBuilder.RenameColumn(
                name: "parent_super_star_id",
                table: "AvatarDetailEntities",
                newName: "ParentSuperStarId");

            migrationBuilder.RenameColumn(
                name: "parent_star_id",
                table: "AvatarDetailEntities",
                newName: "ParentStarId");

            migrationBuilder.RenameColumn(
                name: "parent_solar_system_id",
                table: "AvatarDetailEntities",
                newName: "ParentSolarSystemId");

            migrationBuilder.RenameColumn(
                name: "parent_planet_id",
                table: "AvatarDetailEntities",
                newName: "ParentPlanetId");

            migrationBuilder.RenameColumn(
                name: "parent_omniverse_id",
                table: "AvatarDetailEntities",
                newName: "ParentOmniverseId");

            migrationBuilder.RenameColumn(
                name: "parent_multiverse_id",
                table: "AvatarDetailEntities",
                newName: "ParentMultiverseId");

            migrationBuilder.RenameColumn(
                name: "parent_moon_id",
                table: "AvatarDetailEntities",
                newName: "ParentMoonId");

            migrationBuilder.RenameColumn(
                name: "parent_holon_id",
                table: "AvatarDetailEntities",
                newName: "ParentHolonId");

            migrationBuilder.RenameColumn(
                name: "parent_great_grand_super_star_id",
                table: "AvatarDetailEntities",
                newName: "ParentGreatGrandSuperStarId");

            migrationBuilder.RenameColumn(
                name: "parent_grand_super_star_id",
                table: "AvatarDetailEntities",
                newName: "ParentGrandSuperStarId");

            migrationBuilder.RenameColumn(
                name: "parent_galaxy_id",
                table: "AvatarDetailEntities",
                newName: "ParentGalaxyId");

            migrationBuilder.RenameColumn(
                name: "parent_galaxy_cluster_id",
                table: "AvatarDetailEntities",
                newName: "ParentGalaxyClusterId");

            migrationBuilder.RenameColumn(
                name: "parent_dimension_id",
                table: "AvatarDetailEntities",
                newName: "ParentDimensionId");

            migrationBuilder.RenameColumn(
                name: "parent_celestial_space_id",
                table: "AvatarDetailEntities",
                newName: "ParentCelestialSpaceId");

            migrationBuilder.RenameColumn(
                name: "parent_celestial_body_id",
                table: "AvatarDetailEntities",
                newName: "ParentCelestialBodyId");

            migrationBuilder.RenameColumn(
                name: "modified_date",
                table: "AvatarDetailEntities",
                newName: "ModifiedDate");

            migrationBuilder.RenameColumn(
                name: "modified_by_avatar_id",
                table: "AvatarDetailEntities",
                newName: "ModifiedByAvatarId");

            migrationBuilder.RenameColumn(
                name: "is_changed",
                table: "AvatarDetailEntities",
                newName: "IsChanged");

            migrationBuilder.RenameColumn(
                name: "is_active",
                table: "AvatarDetailEntities",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "deleted_date",
                table: "AvatarDetailEntities",
                newName: "DeletedDate");

            migrationBuilder.RenameColumn(
                name: "deleted_by_avatar_id",
                table: "AvatarDetailEntities",
                newName: "DeletedByAvatarId");

            migrationBuilder.RenameColumn(
                name: "created_date",
                table: "AvatarDetailEntities",
                newName: "CreatedDate");

            migrationBuilder.RenameColumn(
                name: "created_by_avatar_id",
                table: "AvatarDetailEntities",
                newName: "CreatedByAvatarId");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "HolonEntities",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "NVARCHAR(100)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "HolonEntities",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "NVARCHAR(64)");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "HolonEntities",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "NVARCHAR(300)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "PreviousVersionId",
                table: "HolonEntities",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "NVARCHAR(64)");

            migrationBuilder.AlterColumn<Guid>(
                name: "ParentZomeId",
                table: "HolonEntities",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "NVARCHAR(64)");

            migrationBuilder.AlterColumn<Guid>(
                name: "ParentUniverseId",
                table: "HolonEntities",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "NVARCHAR(64)");

            migrationBuilder.AlterColumn<Guid>(
                name: "ParentSuperStarId",
                table: "HolonEntities",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "NVARCHAR(64)");

            migrationBuilder.AlterColumn<Guid>(
                name: "ParentStarId",
                table: "HolonEntities",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "NVARCHAR(64)");

            migrationBuilder.AlterColumn<Guid>(
                name: "ParentSolarSystemId",
                table: "HolonEntities",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "NVARCHAR(64)");

            migrationBuilder.AlterColumn<Guid>(
                name: "ParentPlanetId",
                table: "HolonEntities",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "NVARCHAR(64)");

            migrationBuilder.AlterColumn<Guid>(
                name: "ParentOmniverseId",
                table: "HolonEntities",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "NVARCHAR(64)");

            migrationBuilder.AlterColumn<Guid>(
                name: "ParentMultiverseId",
                table: "HolonEntities",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "NVARCHAR(64)");

            migrationBuilder.AlterColumn<Guid>(
                name: "ParentMoonId",
                table: "HolonEntities",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "NVARCHAR(64)");

            migrationBuilder.AlterColumn<Guid>(
                name: "ParentHolonId",
                table: "HolonEntities",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "NVARCHAR(64)");

            migrationBuilder.AlterColumn<Guid>(
                name: "ParentGreatGrandSuperStarId",
                table: "HolonEntities",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "NVARCHAR(64)");

            migrationBuilder.AlterColumn<Guid>(
                name: "ParentGrandSuperStarId",
                table: "HolonEntities",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "NVARCHAR(64)");

            migrationBuilder.AlterColumn<Guid>(
                name: "ParentGalaxyId",
                table: "HolonEntities",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "NVARCHAR(64)");

            migrationBuilder.AlterColumn<Guid>(
                name: "ParentGalaxyClusterId",
                table: "HolonEntities",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "NVARCHAR(64)");

            migrationBuilder.AlterColumn<Guid>(
                name: "ParentDimensionId",
                table: "HolonEntities",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "NVARCHAR(64)");

            migrationBuilder.AlterColumn<Guid>(
                name: "ParentCelestialSpaceId",
                table: "HolonEntities",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "NVARCHAR(64)");

            migrationBuilder.AlterColumn<Guid>(
                name: "ParentCelestialBodyId",
                table: "HolonEntities",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "NVARCHAR(64)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ModifiedDate",
                table: "HolonEntities",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "DATETIME");

            migrationBuilder.AlterColumn<bool>(
                name: "IsChanged",
                table: "HolonEntities",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "BOOLEAN");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "HolonEntities",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "BOOLEAN");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedDate",
                table: "HolonEntities",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "DATETIME");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedDate",
                table: "HolonEntities",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "DATETIME");

            migrationBuilder.AlterColumn<Guid>(
                name: "HolonId",
                table: "HolonEntities",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "NVARCHAR(64)");

            migrationBuilder.AddColumn<string>(
                name: "CreatedByAvatarId",
                table: "HolonEntities",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DeletedByAvatarId",
                table: "HolonEntities",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ModifiedByAvatarId",
                table: "HolonEntities",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Verified",
                table: "AvatarEntities",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "DATETIME",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "AvatarEntities",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "NVARCHAR(100)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "AvatarEntities",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "NVARCHAR(200)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Password",
                table: "AvatarEntities",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "NVARCHAR(100)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AvatarEntities",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "NVARCHAR(100)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "AvatarEntities",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "NVARCHAR(20)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "AvatarEntities",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "NVARCHAR(64)");

            migrationBuilder.AlterColumn<string>(
                name: "VerificationToken",
                table: "AvatarEntities",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "NVARCHAR(100)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "ResetTokenExpires",
                table: "AvatarEntities",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "DATETIME",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ResetToken",
                table: "AvatarEntities",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "NVARCHAR(100)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RefreshToken",
                table: "AvatarEntities",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "NVARCHAR(100)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "PasswordReset",
                table: "AvatarEntities",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "DATETIME",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LastName",
                table: "AvatarEntities",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "NVARCHAR(50)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastBeamedOut",
                table: "AvatarEntities",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "DATETIME",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastBeamedIn",
                table: "AvatarEntities",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "DATETIME",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "JwtToken",
                table: "AvatarEntities",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "NVARCHAR(100)",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsBeamedIn",
                table: "AvatarEntities",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "BOOLEAN");

            migrationBuilder.AlterColumn<Guid>(
                name: "HolonId",
                table: "AvatarEntities",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "NVARCHAR(64)");

            migrationBuilder.AlterColumn<string>(
                name: "FirstName",
                table: "AvatarEntities",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "NVARCHAR(50)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedDate",
                table: "AvatarEntities",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "DATETIME");

            migrationBuilder.AlterColumn<string>(
                name: "DeletedByAvatarId",
                table: "AvatarEntities",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "NVARCHAR(64)");

            migrationBuilder.AlterColumn<bool>(
                name: "AcceptTerms",
                table: "AvatarEntities",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "BOOLEAN");

            migrationBuilder.AddColumn<string>(
                name: "CreatedByAvatarId",
                table: "AvatarEntities",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "AvatarEntities",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "AvatarEntities",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "AvatarEntities",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedByAvatarId",
                table: "AvatarEntities",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedDate",
                table: "AvatarEntities",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "PreviousVersionId",
                table: "AvatarEntities",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "AvatarDetailEntities",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "NVARCHAR(100)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Town",
                table: "AvatarDetailEntities",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "NVARCHAR(100)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Postcode",
                table: "AvatarDetailEntities",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "NVARCHAR(100)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Mobile",
                table: "AvatarDetailEntities",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "NVARCHAR(100)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Landline",
                table: "AvatarDetailEntities",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "NVARCHAR(100)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "AvatarDetailEntities",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "NVARCHAR(100)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DOB",
                table: "AvatarDetailEntities",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "DATETIME");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "AvatarDetailEntities",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "NVARCHAR(300)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Country",
                table: "AvatarDetailEntities",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "NVARCHAR(100)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "AvatarDetailEntities",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "NVARCHAR(100)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "AvatarDetailEntities",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "NVARCHAR(64)");

            migrationBuilder.AlterColumn<string>(
                name: "UmaJson",
                table: "AvatarDetailEntities",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "NVARCHAR(100)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "PreviousVersionId",
                table: "AvatarDetailEntities",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "NVARCHAR(64)");

            migrationBuilder.AlterColumn<Guid>(
                name: "ParentZomeId",
                table: "AvatarDetailEntities",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "NVARCHAR(64)");

            migrationBuilder.AlterColumn<Guid>(
                name: "ParentUniverseId",
                table: "AvatarDetailEntities",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "NVARCHAR(64)");

            migrationBuilder.AlterColumn<Guid>(
                name: "ParentSuperStarId",
                table: "AvatarDetailEntities",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "NVARCHAR(64)");

            migrationBuilder.AlterColumn<Guid>(
                name: "ParentStarId",
                table: "AvatarDetailEntities",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "NVARCHAR(64)");

            migrationBuilder.AlterColumn<Guid>(
                name: "ParentSolarSystemId",
                table: "AvatarDetailEntities",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "NVARCHAR(64)");

            migrationBuilder.AlterColumn<Guid>(
                name: "ParentPlanetId",
                table: "AvatarDetailEntities",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "NVARCHAR(64)");

            migrationBuilder.AlterColumn<Guid>(
                name: "ParentOmniverseId",
                table: "AvatarDetailEntities",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "NVARCHAR(64)");

            migrationBuilder.AlterColumn<Guid>(
                name: "ParentMultiverseId",
                table: "AvatarDetailEntities",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "NVARCHAR(64)");

            migrationBuilder.AlterColumn<Guid>(
                name: "ParentMoonId",
                table: "AvatarDetailEntities",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "NVARCHAR(64)");

            migrationBuilder.AlterColumn<Guid>(
                name: "ParentHolonId",
                table: "AvatarDetailEntities",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "NVARCHAR(64)");

            migrationBuilder.AlterColumn<Guid>(
                name: "ParentGreatGrandSuperStarId",
                table: "AvatarDetailEntities",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "NVARCHAR(64)");

            migrationBuilder.AlterColumn<Guid>(
                name: "ParentGrandSuperStarId",
                table: "AvatarDetailEntities",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "NVARCHAR(64)");

            migrationBuilder.AlterColumn<Guid>(
                name: "ParentGalaxyId",
                table: "AvatarDetailEntities",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "NVARCHAR(64)");

            migrationBuilder.AlterColumn<Guid>(
                name: "ParentGalaxyClusterId",
                table: "AvatarDetailEntities",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "NVARCHAR(64)");

            migrationBuilder.AlterColumn<Guid>(
                name: "ParentDimensionId",
                table: "AvatarDetailEntities",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "NVARCHAR(64)");

            migrationBuilder.AlterColumn<Guid>(
                name: "ParentCelestialSpaceId",
                table: "AvatarDetailEntities",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "NVARCHAR(64)");

            migrationBuilder.AlterColumn<Guid>(
                name: "ParentCelestialBodyId",
                table: "AvatarDetailEntities",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "NVARCHAR(64)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ModifiedDate",
                table: "AvatarDetailEntities",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "DATETIME");

            migrationBuilder.AlterColumn<string>(
                name: "ModifiedByAvatarId",
                table: "AvatarDetailEntities",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "NVARCHAR(64)");

            migrationBuilder.AlterColumn<bool>(
                name: "IsChanged",
                table: "AvatarDetailEntities",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "BOOLEAN");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "AvatarDetailEntities",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "BOOLEAN");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedDate",
                table: "AvatarDetailEntities",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "DATETIME");

            migrationBuilder.AlterColumn<string>(
                name: "DeletedByAvatarId",
                table: "AvatarDetailEntities",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "NVARCHAR(64)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedDate",
                table: "AvatarDetailEntities",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "DATETIME");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedByAvatarId",
                table: "AvatarDetailEntities",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "NVARCHAR(64)");

            migrationBuilder.AddColumn<Guid>(
                name: "HolonId",
                table: "AvatarDetailEntities",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "Image2D",
                table: "AvatarDetailEntities",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "AvatarDetailEntities",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_HolonEntities",
                table: "HolonEntities",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AvatarEntities",
                table: "AvatarEntities",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AvatarDetailEntities",
                table: "AvatarDetailEntities",
                column: "Id");
        }
    }
}
