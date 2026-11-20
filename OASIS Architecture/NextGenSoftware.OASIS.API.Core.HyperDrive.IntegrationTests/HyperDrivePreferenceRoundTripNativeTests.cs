using System.Reflection;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class AvatarProfileCommitStampNativeTests
{
    [Theory]
    [InlineData("MongoDBOASIS")]
    [InlineData("SQLLiteDBOASIS")]
    [InlineData("Neo4jOASIS")]
    public async Task HyperDrivePreferencesNativeRoundTripUsesScopedProviderAndPreservesCategories(string providerName)
    {
        RequireOwnedFixtures();
        var id=Guid.NewGuid();
        IOASISStorageProvider? owner=null;
        Func<Task<string>>? sqliteMetadata=null;
        IAsyncDisposable? asyncOwner=null;IDisposable? syncOwner=null;
        try
        {
            switch(providerName)
            {
                case "MongoDBOASIS":
                    var mongo=new MongoStore();asyncOwner=mongo;await mongo.Activate();owner=mongo.Provider;break;
                case "SQLLiteDBOASIS":
                    var sqlite=new SqliteStore();syncOwner=sqlite;owner=sqlite.Provider;
                    sqliteMetadata=()=>sqlite.ReadMetadataJsonNativeAsync(id);
                    var activation=await owner.ActivateProviderAsync();Assert.False(activation.IsError,activation.Message);break;
                case "Neo4jOASIS":
                    var neo=new BoltStore();asyncOwner=neo;await neo.Activate(id);owner=neo.Provider;break;
                default:throw new ArgumentOutOfRangeException(nameof(providerName));
            }
            var password=PasswordEncryptionHelper.HashPassword("owned-routing-preference-password",null);
            var before=await SeedRoutingStores([owner],id,password:password);
            var routing=Routing([owner],false,HyperDriveModes.V2);
            var settings=(SettingsManager)Activator.CreateInstance(typeof(SettingsManager),
                BindingFlags.Instance|BindingFlags.NonPublic,null,[null,routing.OASISDNA,routing],null)!;
            var privacy=await settings.UpdatePrivacySettingsAsync(id,new Dictionary<string,object>{{"profileVisibility","private"}});
            Assert.False(privacy.IsError,privacy.Message);Assert.True(privacy.IsSaved);
            var initial=await settings.UpdateHyperDriveSettingsAsync(id,new Dictionary<string,object>
            {
                [AvatarHyperDriveSettings.AutoFailoverEnabled]=true,
                [AvatarHyperDriveSettings.AutoFailoverProviders]=providerName,
                [AvatarHyperDriveSettings.AutoLoadBalancingEnabled]=false
            });
            Assert.False(initial.IsError,initial.Message);Assert.True(initial.Result);Assert.True(initial.IsSaved);
            var patch=await settings.UpdateHyperDriveSettingsAsync(id,new Dictionary<string,object>
                {{AvatarHyperDriveSettings.AutoReplicationEnabled,false}});
            Assert.False(patch.IsError,patch.Message);Assert.True(patch.IsSaved);
            var account=await owner.LoadAvatarAsync(id);Assert.False(account.IsError,account.Message);
            Assert.Equal(before.FirstName,account.Result.FirstName);Assert.Equal(password,account.Result.Password);
            Assert.Equal("private",StoredPrivacy(account.Result));
            if(sqliteMetadata!=null)
            {
                using var raw=System.Text.Json.JsonDocument.Parse(await sqliteMetadata());
                Assert.Equal("private",raw.RootElement.GetProperty("preferences").GetProperty("privacy")
                    .GetProperty("profileVisibility").GetString());
            }
            var captured=AvatarHyperDriveSettings.FromAvatar(account.Result);
            Assert.False(captured.IsError,captured.Message);Assert.NotNull(captured.Result);
            Assert.True(captured.Result.AutoFailoverEnabled);Assert.False(captured.Result.AutoReplicationEnabled);
            Assert.False(captured.Result.AutoLoadBalancingEnabled);
            Assert.Equal(new[]{owner.ProviderType.Value},captured.Result.FailoverProviders);
            var visible=await settings.GetHyperDriveSettingsAsync(id);Assert.False(visible.IsError,visible.Message);
            var declared=Assert.IsType<Dictionary<string,object>>(visible.Result["avatarOverrides"]);
            Assert.Equal(providerName,Assert.IsType<string>(declared[AvatarHyperDriveSettings.AutoFailoverProviders]));
            var effective=Assert.IsType<Dictionary<string,object>>(visible.Result["effective"]);
            Assert.False(effective.ContainsKey("error"));
            Assert.Equal(new[]{providerName},Assert.IsAssignableFrom<IEnumerable<string>>(
                effective[AvatarHyperDriveSettings.AutoFailoverProviders]));
            var clear=await settings.UpdateHyperDriveSettingsAsync(id,new Dictionary<string,object>
                {{AvatarHyperDriveSettings.AutoFailoverProviders,"DEFAULT"}});
            Assert.False(clear.IsError,clear.Message);Assert.True(clear.IsSaved);
            var final=await owner.LoadAvatarAsync(id);Assert.False(final.IsError,final.Message);
            var json=NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.CaptureRuntimeToken(final.Result.MetaData);
            Assert.Null(json["preferences"]?[AvatarHyperDriveSettings.Category]?[AvatarHyperDriveSettings.AutoFailoverProviders]);
            Assert.Equal("private",StoredPrivacy(final.Result));
            var inherited=AvatarHyperDriveSettings.FromAvatar(final.Result);
            Assert.False(inherited.IsError,inherited.Message);Assert.NotNull(inherited.Result);
            Assert.Null(inherited.Result.FailoverProviders);Assert.True(inherited.Result.AutoFailoverEnabled);
            Console.WriteLine("HyperDrive native preference merge capture and inheritance verified: "+providerName);
        }
        finally
        {
            try { if(asyncOwner!=null)await asyncOwner.DisposeAsync(); }
            finally
            {
                syncOwner?.Dispose();
                Console.WriteLine("Owned HyperDrive preference clients/data cleanup verified: "+providerName);
            }
        }
    }
}
