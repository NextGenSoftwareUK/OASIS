using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
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
