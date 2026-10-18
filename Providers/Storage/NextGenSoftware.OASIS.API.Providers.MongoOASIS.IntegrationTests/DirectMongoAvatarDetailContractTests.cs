using System;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using Xunit;
using Provider=NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;
using StoredDetail=NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.AvatarDetail;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests;

public sealed class DirectMongoAvatarDetailContractTests
{
    [Theory][InlineData(false)][InlineData(true)]
    public async Task PublicDetailSaveReadAndUpdatePreserveScalarProfileFields(bool synchronous)
    {
        var connection=Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION")
            ??throw new InvalidOperationException("Owned Mongo replica connection is required.");
        const string database="oasis_direct_avatar_detail_contract";
        var collection=new MongoClient(connection).GetDatabase(database).GetCollection<StoredDetail>("AvatarDetail");
        var id=Guid.NewGuid();var filter=Builders<StoredDetail>.Filter.Eq(row=>row.HolonId,id);
        var provider=new Provider(connection,database);
        try{
            var activated=await provider.ActivateProviderAsync();Assert.False(activated.IsError,activated.Message);Assert.True(activated.Result);
            IAvatarDetail detail=new NextGenSoftware.OASIS.API.Core.Holons.AvatarDetail{
                Id=id,Username="direct-detail-"+id.ToString("N"),Email=id.ToString("N")+"@example.invalid",XP=37,Karma=83};
            var scalarProperties=typeof(IAvatarDetail).GetProperties().Where(property=>property.CanWrite && property.Name!="Id" &&
                new[]{typeof(string),typeof(int),typeof(long),typeof(DateTime),typeof(ConsoleColor),typeof(Guid)}
                    .Contains(Nullable.GetUnderlyingType(property.PropertyType)??property.PropertyType)).ToArray();
            Assert.True(scalarProperties.Length>=19,"Do not silently shrink the profile-field contract.");
            for(var revision=1;revision<=2;revision++){
                var skills=new IndependentAvatarSkills();var skillValue=revision*100;
                var skillProperties=typeof(IAvatarSkills).GetProperties();Assert.Equal(16,skillProperties.Length);
                foreach(var property in skillProperties)property.SetValue(skills,skillValue++);
                detail.Skills=skills;
                foreach(var property in scalarProperties){
                    var type=Nullable.GetUnderlyingType(property.PropertyType)??property.PropertyType;
                    object value=type==typeof(string)? $"revision-{revision}-{property.Name}-{id:N}":
                        type==typeof(int)?37*revision:type==typeof(long)?83L*revision:
                        type==typeof(DateTime)?new DateTime(2000+revision,2,3,4,5,6,DateTimeKind.Utc):
                        type==typeof(ConsoleColor)?(revision==1?ConsoleColor.Green:ConsoleColor.Cyan):Guid.NewGuid();
                    if(property.Name=="Email")value=$"{id:N}-{revision}@example.invalid";
                    property.SetValue(detail,value);
                }
                detail.Description="revision-description-"+revision;
                var saved=synchronous?provider.SaveAvatarDetail(detail):await provider.SaveAvatarDetailAsync(detail);
                Assert.False(saved.IsError,saved.Message);Assert.NotNull(saved.Result);
                var raw=await collection.Find(filter).SingleAsync();Assert.Equal(detail.Description,raw.Description);
                foreach(var property in skillProperties){
                    Assert.Equal(property.GetValue(skills),property.GetValue(raw.Skills));
                    Assert.Equal(property.GetValue(skills),property.GetValue(saved.Result.Skills));
                }
                foreach(var property in scalarProperties){
                    var nativeProperty=typeof(StoredDetail).GetProperty(property.Name);Assert.NotNull(nativeProperty);
                    Assert.Equal(property.GetValue(detail),nativeProperty.GetValue(raw));
                    Assert.Equal(property.GetValue(detail),property.GetValue(saved.Result));
                }
                // Independent provider instance proves this is persisted state, not a caller cache.
                var reader=new Provider(connection,database);
                try{
                    var ready=await reader.ActivateProviderAsync();Assert.False(ready.IsError,ready.Message);
                    foreach(var loaded in new[]{reader.LoadAvatarDetail(id),await reader.LoadAvatarDetailAsync(id),
                        reader.LoadAvatarDetailByUsername(detail.Username),await reader.LoadAvatarDetailByUsernameAsync(detail.Username),
                        reader.LoadAvatarDetailByEmail(detail.Email),await reader.LoadAvatarDetailByEmailAsync(detail.Email)}){
                        Assert.False(loaded.IsError,loaded.Message);Assert.NotNull(loaded.Result);
                        Assert.Equal(id,loaded.Result.Id);Assert.Equal(detail.Description,loaded.Result.Description);
                        foreach(var property in scalarProperties)Assert.Equal(property.GetValue(detail),property.GetValue(loaded.Result));
                        foreach(var property in skillProperties)Assert.Equal(property.GetValue(skills),property.GetValue(loaded.Result.Skills));
                    }
                }finally{await reader.DeActivateProviderAsync();}
            }
        }finally{
            await provider.DeActivateProviderAsync();
            // Only this case's identity; retain the one empty reusable database/schema.
            await collection.DeleteManyAsync(filter);
        }
    }
}
