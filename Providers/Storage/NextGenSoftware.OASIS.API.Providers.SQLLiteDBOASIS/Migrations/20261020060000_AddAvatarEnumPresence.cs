using Microsoft.EntityFrameworkCore.Migrations;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Migrations;

public partial class AddAvatarEnumPresence : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        foreach(var column in new[]{"HasAvatarType","HasCreatedProviderType","HasCreatedOASISType"})
            migrationBuilder.AddColumn<bool>(name:column,table:"Avatar",type:"INTEGER",nullable:false,defaultValue:true);
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("CREATE TEMP TABLE cosmic_avatar_presence_guard(value INTEGER CHECK(value=0)); "
            +"INSERT INTO cosmic_avatar_presence_guard SELECT COUNT(*) FROM Avatar WHERE NOT HasAvatarType OR NOT HasCreatedProviderType OR NOT HasCreatedOASISType; "
            +"DROP TABLE cosmic_avatar_presence_guard;");
        foreach(var column in new[]{"HasAvatarType","HasCreatedProviderType","HasCreatedOASISType"})
            migrationBuilder.DropColumn(column,"Avatar");
    }
}
