using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using Xunit;
using Provider=NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests;

public sealed class HostedMongoNearbyTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task NearbyReadsActualCollectionsWithMicrodegreesAndFullRecords(bool avatars)
    {
        var connection=Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION") ?? throw new InvalidOperationException("Owned replica required.");
        const string database="oasis_owned_nearby_current";
        var client=new MongoClient(connection);
        Assert.DoesNotContain(database,(await (await client.ListDatabaseNamesAsync()).ToListAsync()));
        var provider=new Provider(connection,database);var verified=false;
        var records=new List<IHolonBase>();
        try
        {
            var activated=await provider.ActivateProviderAsync();Assert.False(activated.IsError,activated.Message);
            for(var i=0;i<4;i++)
            {
                IHolonBase record=avatars ? new Avatar {Username="nearby-"+i,Email="nearby"+i+"@example.invalid",FirstName="complete profile"}:new Holon();
                record.Id=Guid.NewGuid();record.Name="nearby "+i;record.Description="complete record";record.IsActive=i!=2;
                record.HolonType=avatars ? HolonType.Avatar:HolonType.Park;
                record.MetaData=new Dictionary<string,object> {{"proof","preserved"}};
                if(i!=3) {record.MetaData["Latitude"]=i==1 ? "30.002":"30.0005";record.MetaData["Longitude"]="20";}
                records.Add(record);
                if(avatars) {var saved=await provider.SaveAvatarAsync((IAvatar)record);Assert.False(saved.IsError,saved.Message);}
                else {var saved=await provider.SaveHolonAsync((IHolon)record,saveChildren:false);Assert.False(saved.IsError,saved.Message);}
            }
            var prior=CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture=new CultureInfo("fr-FR");
                if(avatars)
                {
                    var nearby=await provider.GetAvatarsNearMeAsync(30000000,20000000,100);
                    Assert.False(nearby.IsError,nearby.Message);Assert.True(nearby.IsLoaded);
                    var found=Assert.Single(nearby.Result);Assert.Equal(records[0].Id,found.Id);Assert.Equal("complete profile",found.FirstName);
                    Assert.Equal("preserved",found.MetaData["proof"].ToString());
                    Assert.Empty(((IOASISNETProvider)provider).GetAvatarsNearMe(30000000,20000000,0).Result);
                    records[0].MetaData["Latitude"]="NaN";
                    var saved=await provider.SaveAvatarAsync((IAvatar)records[0]);Assert.False(saved.IsError,saved.Message);
                    var corrupt=await provider.GetAvatarsNearMeAsync(30000000,20000000,100);
                    Assert.True(corrupt.IsError);Assert.False(corrupt.IsLoaded);Assert.Null(corrupt.Result);Assert.IsType<InvalidDataException>(corrupt.Exception);
                }
                else
                {
                    var nearby=await provider.GetHolonsNearMeAsync(30000000,20000000,100,HolonType.Park);
                    Assert.False(nearby.IsError,nearby.Message);Assert.True(nearby.IsLoaded);
                    var found=Assert.Single(nearby.Result);Assert.Equal(records[0].Id,found.Id);Assert.Equal("complete record",found.Description);
                    Assert.Equal("preserved",found.MetaData["proof"].ToString());
                    Assert.Empty(((IOASISNETProvider)provider).GetHolonsNearMe(30000000,20000000,0,HolonType.All).Result);
                    Assert.Empty((await provider.GetHolonsNearMeAsync(30000000,20000000,100,HolonType.Holon)).Result);
                    records[0].MetaData.Remove("Longitude");
                    var saved=await provider.SaveHolonAsync((IHolon)records[0],saveChildren:false);Assert.False(saved.IsError,saved.Message);
                    var corrupt=await provider.GetHolonsNearMeAsync(30000000,20000000,100,HolonType.All);
                    Assert.True(corrupt.IsError);Assert.False(corrupt.IsLoaded);Assert.Null(corrupt.Result);Assert.IsType<InvalidDataException>(corrupt.Exception);
                }
            }
            finally {CultureInfo.CurrentCulture=prior;}
            verified=true;
        }
        finally
        {
            await provider.DeActivateProviderAsync();
            if(verified) await client.DropDatabaseAsync(database);
        }
    }
}
