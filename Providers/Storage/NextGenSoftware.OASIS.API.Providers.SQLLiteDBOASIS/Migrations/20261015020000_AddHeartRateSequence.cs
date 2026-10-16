using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Migrations;

[DbContext(typeof(DataContext))]
[Migration("20261015020000_AddHeartRateSequence")]
public sealed class AddHeartRateSequence : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
        => migrationBuilder.AddColumn<int>(name:"SequenceIndex",table:"HeartRateEntry",type:"INTEGER",nullable:false,defaultValue:0);

    protected override void Down(MigrationBuilder migrationBuilder)
        => migrationBuilder.Sql("ALTER TABLE HeartRateEntry DROP COLUMN SequenceIndex");
}
