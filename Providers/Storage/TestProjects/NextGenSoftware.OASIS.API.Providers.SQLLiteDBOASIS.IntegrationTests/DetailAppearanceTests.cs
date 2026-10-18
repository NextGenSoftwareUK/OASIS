using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Theory][InlineData(false)][InlineData(true)]
    public async Task LegacyDetailAppearanceUpgradeKeepsProfileAndDefaultsMissingValuesToNull(bool synchronous)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);(await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var source=new AvatarDetail{Id=Guid.NewGuid(),Username="legacy-appearance",XP=41,Karma=67};
        (await provider.SaveAvatarDetailAsync(source)).IsError.Should().BeFalse();
        using(var control=new DataContext(TestConnectionString)){
            control.Database.ExecuteSqlRaw("ALTER TABLE AvatarDetail DROP COLUMN Portrait");
            control.Database.ExecuteSqlRaw("ALTER TABLE AvatarDetail DROP COLUMN UmaJson");
        }
        var activated=synchronous?provider.ActivateProvider():await provider.ActivateProviderAsync();activated.IsError.Should().BeFalse(activated.Exception?.ToString());
        var loaded=await provider.LoadAvatarDetailAsync(source.Id);loaded.IsError.Should().BeFalse(loaded.Exception?.ToString());
        loaded.Result.Portrait.Should().BeNull();loaded.Result.UmaJson.Should().BeNull();loaded.Result.XP.Should().Be(41);loaded.Result.Karma.Should().Be(67);
        source.Portrait="opaque portrait";source.UmaJson="opaque UMA value";
        var saved=synchronous?provider.SaveAvatarDetail(source):await provider.SaveAvatarDetailAsync(source);saved.IsError.Should().BeFalse(saved.Exception?.ToString());
        using var reader=new SQLLiteDBOASIS(TestConnectionString);(await reader.ActivateProviderAsync()).IsError.Should().BeFalse();
        var restored=(await reader.LoadAvatarDetailAsync(source.Id)).Result;restored.Portrait.Should().Be(source.Portrait);restored.UmaJson.Should().Be(source.UmaJson);
    }
    [Theory][InlineData(false)][InlineData(true)]
    public async Task DetailAppearanceStringsSurviveNativeInsertUpdateAndIndependentRead(bool synchronous)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);(await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var source=new AvatarDetail{Id=Guid.NewGuid(),Username="appearance-values",XP=43,Karma=79};
        for(var revision=0;revision<4;revision++){
            source.Portrait=revision==2?null:revision==3?"":"https://example.invalid/portrait-"+revision+"?label=🌳";
            source.UmaJson=revision==2?null:revision==3?"":"{\"revision\":"+revision+",\"name\":\"🌳 avatar\"}";
            var saved=synchronous?provider.SaveAvatarDetail(source):await provider.SaveAvatarDetailAsync(source);saved.IsError.Should().BeFalse(saved.Exception?.ToString());
            using var reader=new SQLLiteDBOASIS(TestConnectionString);(await reader.ActivateProviderAsync()).IsError.Should().BeFalse();
            var loaded=synchronous?reader.LoadAvatarDetail(source.Id):await reader.LoadAvatarDetailAsync(source.Id);loaded.IsError.Should().BeFalse(loaded.Exception?.ToString());
            loaded.Result.Portrait.Should().Be(source.Portrait);loaded.Result.UmaJson.Should().Be(source.UmaJson);loaded.Result.XP.Should().Be(43);loaded.Result.Karma.Should().Be(79);
            using var control=new DataContext(TestConnectionString);using var connection=control.Database.GetDbConnection();connection.Open();
            using var command=connection.CreateCommand();command.CommandText="SELECT Portrait,UmaJson FROM AvatarDetail WHERE Id=$id";
            var parameter=command.CreateParameter();parameter.ParameterName="$id";parameter.Value=source.Id.ToString();command.Parameters.Add(parameter);
            using var row=command.ExecuteReader();row.Read().Should().BeTrue();
            (row.IsDBNull(0)?null:row.GetString(0)).Should().Be(source.Portrait);(row.IsDBNull(1)?null:row.GetString(1)).Should().Be(source.UmaJson);
        }
    }
}
