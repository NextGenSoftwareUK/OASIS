using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Migrations;

[DbContext(typeof(DataContext))]
[Migration("20261019000001_OptionalGiftChakra")]
public sealed partial class OptionalGiftChakra:Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // The explicit AvatarId relationship replaces the redundant shadow navigation.
        migrationBuilder.DropForeignKey(name:"FK_AvatarGifts_AvatarDetail_AvatarDetailId",table:"AvatarGifts");
        migrationBuilder.DropIndex(name:"IX_AvatarGifts_AvatarDetailId",table:"AvatarGifts");
        migrationBuilder.DropColumn(name:"AvatarDetailId",table:"AvatarGifts");
        migrationBuilder.AlterColumn<long>(name:"AvatarChakraId",table:"AvatarGifts",type:"INTEGER",nullable:true,oldClrType:typeof(long),oldType:"INTEGER");
        migrationBuilder.AddColumn<int>(name:"SequenceIndex",table:"AvatarGifts",type:"INTEGER",nullable:false,defaultValue:0);
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Refuse to invent a chakra for standalone gifts when downgrading.
        migrationBuilder.Sql("CREATE TEMP TABLE owned_gift_downgrade_guard(value INTEGER CHECK(value=0)); INSERT INTO owned_gift_downgrade_guard SELECT COUNT(*) FROM AvatarGifts WHERE AvatarChakraId IS NULL; DROP TABLE owned_gift_downgrade_guard;");
        migrationBuilder.DropColumn(name:"SequenceIndex",table:"AvatarGifts");
        migrationBuilder.AlterColumn<long>(name:"AvatarChakraId",table:"AvatarGifts",type:"INTEGER",nullable:false,oldClrType:typeof(long),oldType:"INTEGER",oldNullable:true);
        migrationBuilder.AddColumn<string>(name:"AvatarDetailId",table:"AvatarGifts",type:"TEXT",nullable:true);
        migrationBuilder.CreateIndex(name:"IX_AvatarGifts_AvatarDetailId",table:"AvatarGifts",column:"AvatarDetailId");
        migrationBuilder.AddForeignKey(name:"FK_AvatarGifts_AvatarDetail_AvatarDetailId",table:"AvatarGifts",column:"AvatarDetailId",principalTable:"AvatarDetail",principalColumn:"Id");
    }
}
