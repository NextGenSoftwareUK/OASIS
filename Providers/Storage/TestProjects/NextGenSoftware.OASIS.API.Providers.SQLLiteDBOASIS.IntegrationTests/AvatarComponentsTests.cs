using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    public static IEnumerable<object[]> ComponentFaultCases()
    {
        foreach(var synchronous in new[] {true,false})
        foreach(var table in new[] {"AvatarAttributes","AvatarAura","AvatarSkills","AvatarSuperPowers","AvatarHumanDesign","AvatarStats"})
        foreach(var operation in new[] {"INSERT","UPDATE","DELETE"})
            yield return new object[] {synchronous,table,operation};
    }

    private static AvatarDetail ComponentRecord(Guid id,int offset) => new()
    {
        Id=id,Username="component-"+id.ToString("N"),Email=id+"@components.invalid",Karma=offset+1,XP=offset+2,
        Model3D="model://component/"+offset,
        Attributes=DistinctValues<AvatarAttributes>(offset+10),Aura=DistinctValues<AvatarAura>(offset+30),
        Skills=DistinctValues<AvatarSkills>(offset+50),SuperPowers=DistinctValues<AvatarSuperPowers>(offset+80),
        HumanDesign=new HumanDesign {Type="owned-type-"+offset},
        Stats=new AvatarStats {HP=new AvatarStat {Current=offset+4},Mana=new AvatarStat {Current=offset+5},
            Stamina=new AvatarStat {Current=offset+6},Energy=new AvatarStat {Current=offset+7}}
    };

    private static void AssertComponents(IAvatarDetail actual,IAvatarDetail expected)
    {
        actual.Karma.Should().Be(expected.Karma);actual.XP.Should().Be(expected.XP);actual.Model3D.Should().Be(expected.Model3D);
        actual.Attributes.Should().BeEquivalentTo(expected.Attributes);actual.Aura.Should().BeEquivalentTo(expected.Aura);
        actual.Skills.Should().BeEquivalentTo(expected.Skills);actual.SuperPowers.Should().BeEquivalentTo(expected.SuperPowers);
        actual.HumanDesign.Should().BeEquivalentTo(expected.HumanDesign);actual.Stats.Should().BeEquivalentTo(expected.Stats);
    }

    [Theory]
    [MemberData(nameof(ComponentFaultCases))]
    public async Task RejectedComponentMutationCannotLeakThroughLaterSave(bool synchronous,string table,string operation)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var id=Guid.NewGuid();var original=ComponentRecord(id,100);var unrelated=ComponentRecord(Guid.NewGuid(),300);
        if(operation!="INSERT") (await provider.SaveAvatarDetailAsync(original)).IsError.Should().BeFalse();
        (await provider.SaveAvatarDetailAsync(unrelated)).IsError.Should().BeFalse();
        var proposed=ComponentRecord(id,200);
        if(operation=="DELETE")
        {
            proposed.Attributes=null;proposed.Aura=null;proposed.Skills=null;proposed.SuperPowers=null;
            proposed.HumanDesign=null;proposed.Stats=null;
        }
        // Table/operation come only from the fixed MemberData allowlist; the owner
        // predicate is a generated GUID. Never install a trigger in a user DB.
        var alias=operation=="DELETE" ? "OLD":"NEW";
        using var control=new DataContext(TestConnectionString);
        control.Database.ExecuteSqlRaw($"CREATE TRIGGER reject_owned_component BEFORE {operation} ON \"{table}\" WHEN {alias}.AvatarId='{id}' BEGIN SELECT RAISE(ABORT,'owned component rejection'); END;");
        try
        {
            var rejected=synchronous ? provider.SaveAvatarDetail(proposed):await provider.SaveAvatarDetailAsync(proposed);
            rejected.IsError.Should().BeTrue();rejected.IsSaved.Should().BeFalse();rejected.Result.Should().BeNull();
            var marker=rejected.Exception.Should().BeOfType<SqliteMutationReconciliationException>().Which;
            marker.RecordId.Should().Be(id);marker.RecordIds.Should().Equal(id);marker.OriginalTransactionId.Should().BeEmpty();
            var native=marker.InnerException.Should().BeOfType<DbUpdateException>().Which.InnerException.Should().BeOfType<SqliteException>().Which;
            native.SqliteErrorCode.Should().Be(19);native.SqliteExtendedErrorCode.Should().Be(1811);
            native.Message.Should().Contain("owned component rejection");
        }
        finally {control.Database.ExecuteSqlRaw("DROP TRIGGER reject_owned_component;");}
        // The reused context must not defer the rejected owner graph to another save.
        (await provider.SaveHolonAsync(new Holon {Id=Guid.NewGuid(),Name="unrelated successful save"},saveChildren:false)).IsError.Should().BeFalse();
        unrelated.XP++;
        (await provider.SaveAvatarDetailAsync(unrelated)).IsError.Should().BeFalse();
        using var reader=new SQLLiteDBOASIS(TestConnectionString);
        (await reader.ActivateProviderAsync()).IsError.Should().BeFalse();
        var owner=await reader.LoadAvatarDetailAsync(id);
        if(operation=="INSERT") owner.Result.Should().BeNull();else AssertComponents(owner.Result,original);
        AssertComponents((await reader.LoadAvatarDetailAsync(unrelated.Id)).Result,unrelated);
    }

    private static T DistinctValues<T>(int offset) where T:new()
    {
        var value=new T();var ordinal=0;
        foreach(var property in typeof(T).GetProperties().Where(p=>p.CanWrite && p.PropertyType==typeof(int)))
            property.SetValue(value,offset+ ++ordinal);
        return value;
    }

    [Theory]
    [InlineData(true,false)]
    [InlineData(false,false)]
    [InlineData(true,true)]
    [InlineData(false,true)]
    public async Task DetailComponentsSurviveIndependentReadsUpdatesRemovalAndRecreation(bool synchronous,bool startEmpty)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var detail=new AvatarDetail {Id=Guid.NewGuid(),Username="component-fixture",Email="components@test.invalid",Karma=73,XP=89};
        foreach(var phase in startEmpty ? new[] {0,1,2,0,3}:new[] {1,2,0,3})
        {
            detail.Model3D=phase==0 ? null:$"model://owned/{phase}";
            detail.Attributes=phase==0 ? null:DistinctValues<AvatarAttributes>(phase*100);
            detail.Aura=phase==0 ? null:DistinctValues<AvatarAura>(phase*200);
            detail.Skills=phase==0 ? null:DistinctValues<AvatarSkills>(phase*300);
            detail.SuperPowers=phase==0 ? null:DistinctValues<AvatarSuperPowers>(phase*400);
            detail.HumanDesign=phase==0 ? null:new HumanDesign {Type=$"owned-type-{phase}"};
            detail.Stats=phase==0 ? null:new AvatarStats();
            if(detail.Stats!=null)
            {
                detail.Stats.HP.Current=phase+21;detail.Stats.Mana.Current=phase+31;
                detail.Stats.Stamina.Current=phase+41;detail.Stats.Energy.Current=phase+51;
            }
            var saved=synchronous ? provider.SaveAvatarDetail(detail):await provider.SaveAvatarDetailAsync(detail);
            saved.IsError.Should().BeFalse(saved.Message);
            using var reader=new SQLLiteDBOASIS(TestConnectionString);
            (await reader.ActivateProviderAsync()).IsError.Should().BeFalse();
            var results=new[] {
                synchronous ? reader.LoadAvatarDetail(detail.Id):await reader.LoadAvatarDetailAsync(detail.Id),
                synchronous ? reader.LoadAvatarDetailByUsername(detail.Username):await reader.LoadAvatarDetailByUsernameAsync(detail.Username),
                synchronous ? reader.LoadAvatarDetailByEmail(detail.Email):await reader.LoadAvatarDetailByEmailAsync(detail.Email)};
            foreach(var loaded in results)
            {
                loaded.IsError.Should().BeFalse(loaded.Message);var actual=loaded.Result;
                actual.Model3D.Should().Be(detail.Model3D);actual.Karma.Should().Be(73);actual.XP.Should().Be(89);
                actual.Attributes.Should().BeEquivalentTo(detail.Attributes);actual.Aura.Should().BeEquivalentTo(detail.Aura);
                actual.Skills.Should().BeEquivalentTo(detail.Skills);actual.SuperPowers.Should().BeEquivalentTo(detail.SuperPowers);
                actual.HumanDesign.Should().BeEquivalentTo(detail.HumanDesign);
                actual.Stats.Should().BeEquivalentTo(detail.Stats);
            }
        }
    }
}
