using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class MongoSqliteRealProviderRoutingTests
{
    private static async Task VerifyMongoActivationRecoveryAsync(string uri,string databaseName,IMongoDatabase admin,bool synchronous)
    {
        foreach(var retrySynchronous in new[] {false,true})
        {
            const string app="owned-mongo-activation-recovery";
            var provider=new MongoDBOASIS(uri+"&appName="+app,databaseName);
            try
            {
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument {{"configureFailPoint","failCommand"},{"mode","alwaysOn"},
                    {"data",new BsonDocument {{"failCommands",new BsonArray {"createIndexes"}},{"appName",app},{"errorCode",2}}}});
                try
                {
                    var failed=synchronous ? provider.ActivateProvider():await provider.ActivateProviderAsync();
                    failed.IsError.Should().BeTrue();failed.Result.Should().BeFalse();provider.IsProviderActivated.Should().BeFalse();
                    failed.Exception.Should().BeOfType<MongoCommandException>().Which.Code.Should().Be(2);
                }
                finally {await admin.RunCommandAsync<BsonDocument>(new BsonDocument {{"configureFailPoint","failCommand"},{"mode","off"}});}
                var failedContext=provider.Database;
                var retry=retrySynchronous ? provider.ActivateProvider():await provider.ActivateProviderAsync();
                retry.IsError.Should().BeFalse(retry.Exception?.ToString() ?? retry.Message);retry.Result.Should().BeTrue();
                var search=new SearchParams {SearchOnlyForCurrentAvatar=false,SearchGroups=new() {new SearchTextGroup {
                    SearchQuery="owned activation",SearchHolons=true,HolonSearchParams=new Core.Interfaces.SearchHolonParams {Name=true}}}};
                var read=retrySynchronous ? provider.Search(search):await provider.SearchAsync(search);
                read.IsError.Should().BeFalse(read.Exception?.ToString() ?? read.Message);
                read.IsLoaded.Should().BeTrue();read.Result.NumberOfResults.Should().Be(0);
                failedContext.Should().BeNull("initial activation must not publish a context before every required index/repository is ready");
                var id=Guid.NewGuid();
                (await provider.SaveHolonAsync(new Holon {Id=id,Name="owned activation holon"},saveChildren:false)).IsError.Should().BeFalse();
                (await provider.SaveAvatarAsync(new Avatar {Id=id,Username="owned-activation-"+id,Email=id+"@example.invalid",
                    AvatarType=new EnumValue<AvatarType>(AvatarType.User)})).IsError.Should().BeFalse();
                (await provider.SaveAvatarDetailAsync(new AvatarDetail {Id=id,Username="owned-activation-"+id,XP=73})).IsError.Should().BeFalse();
                (await provider.LoadAvatarDetailAsync(id)).Result.XP.Should().Be(73);
                (await provider.DeleteAvatarAsync(id,softDelete:false)).IsError.Should().BeFalse();
                await provider.Database.Holon.DeleteManyAsync(row=>row.HolonId==id);
            }
            finally {await provider.DeActivateProviderAsync();}
        }
    }
}
