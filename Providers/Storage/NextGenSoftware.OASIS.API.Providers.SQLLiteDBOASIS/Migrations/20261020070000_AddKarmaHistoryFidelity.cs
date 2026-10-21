using Microsoft.EntityFrameworkCore.Migrations;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Migrations;

public partial class AddKarmaHistoryFidelity : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        foreach(var column in new[]{"SequenceIndex","DateKind"})
            migrationBuilder.AddColumn<int>(name:column,table:"KarmaAkashicRecord",type:"INTEGER",nullable:false,defaultValue:0);
        foreach(var column in new[]{"HasKarmaSource","HasKarmaEarntOrLost","HasKarmaTypePositive","HasKarmaTypeNegative","HasProvider"})
            migrationBuilder.AddColumn<bool>(name:column,table:"KarmaAkashicRecord",type:"INTEGER",nullable:false,defaultValue:true);
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("CREATE TEMP TABLE cosmic_karma_fidelity_guard(value INTEGER CHECK(value=0)); "
            +"INSERT INTO cosmic_karma_fidelity_guard SELECT COUNT(*) FROM KarmaAkashicRecord WHERE DateKind<>0 OR SequenceIndex<>0 "
            +"OR NOT HasKarmaSource OR NOT HasKarmaEarntOrLost OR NOT HasKarmaTypePositive OR NOT HasKarmaTypeNegative OR NOT HasProvider; "
            +"DROP TABLE cosmic_karma_fidelity_guard;");
        foreach(var column in new[]{"SequenceIndex","DateKind","HasKarmaSource","HasKarmaEarntOrLost","HasKarmaTypePositive","HasKarmaTypeNegative","HasProvider"})
            migrationBuilder.DropColumn(column,"KarmaAkashicRecord");
    }
}
