using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Theory][InlineData(false)][InlineData(true)]
    public async Task AllDeclaredWritableDetailScalarsSurviveInsertUpdateClearAndAllSelectors(bool synchronous)
    {
        var fields=typeof(IAvatarDetail).GetProperties().Where(property=>property.CanWrite&&property.Name!="Id"&&
            new[]{typeof(string),typeof(int),typeof(long),typeof(DateTime),typeof(ConsoleColor),typeof(Guid)}.Contains(Nullable.GetUnderlyingType(property.PropertyType)??property.PropertyType)).ToArray();
        fields.Length.Should().Be(19,"the complete declared writable scalar contract must not shrink silently");
        using var provider=new SQLLiteDBOASIS(TestConnectionString);(await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var source=new AvatarDetail{Id=Guid.NewGuid()};
        for(var revision=0;revision<4;revision++){
            foreach(var field in fields){
                var kind=Nullable.GetUnderlyingType(field.PropertyType)??field.PropertyType;
                object? value=kind==typeof(string)?(field.Name is "Username" or "Email"?field.Name+"-"+source.Id.ToString("N")+"-"+revision:
                    revision==2?null:revision==3?"":field.Name+"-🌳-"+revision):
                    kind==typeof(int)?100+revision:kind==typeof(long)?(object)(300L+revision):
                    kind==typeof(DateTime)?new DateTime(2020+revision,1,2,3,4,5,DateTimeKind.Utc):
                    kind==typeof(ConsoleColor)?(ConsoleColor)(revision+(field.Name=="FavouriteColour"?2:10)):revision>=2?null:Guid.NewGuid();
                field.SetValue(source,value);
            }
            var saved=synchronous?provider.SaveAvatarDetail(source):await provider.SaveAvatarDetailAsync(source);
            saved.IsError.Should().BeFalse(saved.Exception?.ToString());
            using(var control=new DataContext(TestConnectionString))control.AvatarDetails.AsNoTracking().Single(row=>row.Id==source.Id.ToString()).Country.Should().Be(source.Country);
            using var reader=new SQLLiteDBOASIS(TestConnectionString);(await reader.ActivateProviderAsync()).IsError.Should().BeFalse();
            foreach(var loaded in new[]{reader.LoadAvatarDetail(source.Id),await reader.LoadAvatarDetailAsync(source.Id),
                reader.LoadAvatarDetailByUsername(source.Username),await reader.LoadAvatarDetailByUsernameAsync(source.Username),
                reader.LoadAvatarDetailByEmail(source.Email),await reader.LoadAvatarDetailByEmailAsync(source.Email)}){
                loaded.IsError.Should().BeFalse(loaded.Exception?.ToString());loaded.Result.Id.Should().Be(source.Id);
                foreach(var field in fields)field.GetValue(loaded.Result).Should().Be(field.GetValue(source),field.Name+" revision "+revision);
                loaded.Result.Level.Should().Be(source.Level);
            }
        }
    }
}
