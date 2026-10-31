using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Theory][InlineData(false)][InlineData(true)]
    public async Task LegacyDetailParentSchemaUpgradeKeepsExistingProfileAndUsesEmptyIds(bool synchronous)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);(await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var source=new AvatarDetail{Id=Guid.NewGuid(),Username="legacy-parent-columns",XP=31,Karma=71};
        (await provider.SaveAvatarDetailAsync(source)).IsError.Should().BeFalse();
        using(var control=new DataContext(TestConnectionString)){
            control.Database.ExecuteSqlRaw("ALTER TABLE AvatarDetail DROP COLUMN ParentHolonId3");
            control.Database.ExecuteSqlRaw("ALTER TABLE AvatarDetail DROP COLUMN ParentHolonId2");
            control.Database.ExecuteSqlRaw("ALTER TABLE AvatarDetail DROP COLUMN ParentHolonId");
        }
        var activated=synchronous?provider.ActivateProvider():await provider.ActivateProviderAsync();
        activated.IsError.Should().BeFalse(activated.Exception?.ToString());
        var loaded=await provider.LoadAvatarDetailAsync(source.Id);loaded.IsError.Should().BeFalse(loaded.Exception?.ToString());
        loaded.Result.ParentHolonId.Should().Be(Guid.Empty);loaded.Result.ParentHolonId2.Should().Be(Guid.Empty);loaded.Result.ParentHolonId3.Should().Be(Guid.Empty);
        loaded.Result.XP.Should().Be(31);loaded.Result.Karma.Should().Be(71);loaded.Result.Username.Should().Be(source.Username);
        source.ParentHolonId=Guid.NewGuid();source.ParentHolonId2=Guid.NewGuid();source.ParentHolonId3=Guid.NewGuid();
        var saved=synchronous?provider.SaveAvatarDetail(source):await provider.SaveAvatarDetailAsync(source);saved.IsError.Should().BeFalse(saved.Exception?.ToString());
        using var reader=new SQLLiteDBOASIS(TestConnectionString);(await reader.ActivateProviderAsync()).IsError.Should().BeFalse();
        var restored=(await reader.LoadAvatarDetailAsync(source.Id)).Result;
        restored.ParentHolonId.Should().Be(source.ParentHolonId);restored.ParentHolonId2.Should().Be(source.ParentHolonId2);restored.ParentHolonId3.Should().Be(source.ParentHolonId3);
    }
    [Theory][InlineData(false)][InlineData(true)]
    public async Task DetailParentIdentitiesSurviveNativeInsertUpdateAndIndependentReader(bool synchronous)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);(await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var source=new AvatarDetail{Id=Guid.NewGuid(),Username="parent-identities",XP=47,Karma=83};
        for(var revision=0;revision<2;revision++){
            source.ParentHolonId=Guid.NewGuid();source.ParentHolonId2=Guid.NewGuid();source.ParentHolonId3=Guid.NewGuid();
            var saved=synchronous?provider.SaveAvatarDetail(source):await provider.SaveAvatarDetailAsync(source);
            saved.IsError.Should().BeFalse(saved.Exception?.ToString());
            using var reader=new SQLLiteDBOASIS(TestConnectionString);(await reader.ActivateProviderAsync()).IsError.Should().BeFalse();
            var loaded=synchronous?reader.LoadAvatarDetail(source.Id):await reader.LoadAvatarDetailAsync(source.Id);
            loaded.IsError.Should().BeFalse(loaded.Exception?.ToString());
            loaded.Result.ParentHolonId.Should().Be(source.ParentHolonId);loaded.Result.ParentHolonId2.Should().Be(source.ParentHolonId2);loaded.Result.ParentHolonId3.Should().Be(source.ParentHolonId3);
            loaded.Result.XP.Should().Be(47);loaded.Result.Karma.Should().Be(83);
            using var control=new DataContext(TestConnectionString);
            using var connection=control.Database.GetDbConnection();connection.Open();using var command=connection.CreateCommand();
            command.CommandText="SELECT ParentHolonId,ParentHolonId2,ParentHolonId3 FROM AvatarDetail WHERE Id=$id";
            var parameter=command.CreateParameter();parameter.ParameterName="$id";parameter.Value=source.Id.ToString();command.Parameters.Add(parameter);
            using var row=command.ExecuteReader();row.Read().Should().BeTrue();
            Guid.Parse(row.GetString(0)).Should().Be(source.ParentHolonId);Guid.Parse(row.GetString(1)).Should().Be(source.ParentHolonId2);Guid.Parse(row.GetString(2)).Should().Be(source.ParentHolonId3);
        }
    }
}
