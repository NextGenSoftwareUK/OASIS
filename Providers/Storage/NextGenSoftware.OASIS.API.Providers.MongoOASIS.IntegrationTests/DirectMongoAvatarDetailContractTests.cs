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
using NextGenSoftware.OASIS.API.Core.Objects;

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
                var inventory=new InventoryItem{Id=Guid.NewGuid(),Name="inventory-"+revision,
                    Description="owned-item-"+revision,Image2D=new byte[]{1,2,(byte)revision},Object3D=new byte[]{4,5,(byte)revision},
                    Image2DURI=new Uri($"https://example.invalid/image-{revision}"),Object3DURI=new Uri($"https://example.invalid/object-{revision}"),
                    Quantity=revision*3,Stack=revision==1,GameSource="Our World",ItemType=NextGenSoftware.OASIS.API.Core.Enums.InventoryItemType.Weapon,
                    NftId=revision==1?Guid.NewGuid():Guid.Empty,GeoNFTId=revision==2?Guid.NewGuid():Guid.Empty,
                    Rarity="rarity-"+revision,MaxQuantity=revision*10,Weight=revision*1.25f,IsUsable=revision==1,IsTradeable=revision==2,
                    AcquiredOn=new DateTime(2020+revision,1,2,3,4,5,DateTimeKind.Utc),LastUsedOn=new DateTime(2020+revision,2,3,4,5,6,DateTimeKind.Utc),
                    Properties=new System.Collections.Generic.Dictionary<string,object>{{"revision","value-"+revision}}};
                detail.Inventory=new System.Collections.Generic.List<IInventoryItem>{inventory};
                void VerifyInventory(System.Collections.Generic.IEnumerable<IInventoryItem> values){
                    var actual=Assert.Single(values);Assert.Equal(inventory.Id,actual.Id);Assert.Equal(inventory.Name,actual.Name);
                    Assert.Equal(inventory.Description,actual.Description);
                    foreach(var property in typeof(IInventoryItem).GetProperties()){
                        var expected=property.GetValue(inventory);var restored=property.GetValue(actual);
                        if(expected is byte[] bytes)Assert.Equal(bytes,(byte[])restored!);
                        else if(expected is System.Collections.Generic.Dictionary<string,object> properties){
                            var found=(System.Collections.Generic.Dictionary<string,object>)restored!;
                            Assert.Equal(properties.Count,found.Count);
                            foreach(var pair in properties)Assert.Equal(pair.Value,found[pair.Key]);
                        }else Assert.Equal(expected,restored);
                    }
                }
                detail.Chakras=IndependentAvatarChakras.Create(revision);
                void VerifyChakras(IAvatarChakras actual){
                    Assert.NotNull(actual);
                    foreach(var slot in typeof(IAvatarChakras).GetProperties())
                        Assert.Equal(Newtonsoft.Json.JsonConvert.SerializeObject(slot.GetValue(detail.Chakras)),
                            Newtonsoft.Json.JsonConvert.SerializeObject(slot.GetValue(actual)));
                }
                foreach(var field in IndependentAvatarCollections.Fields)
                    IndependentAvatarCollections.Assign(detail,field,IndependentAvatarCollections.Create(field,revision));
                void VerifyCollections(object actual,bool native){
                    foreach(var field in IndependentAvatarCollections.Fields){
                        var slot=IndependentAvatarCollections.Slot(field);
                        var expected=((System.Collections.IList)slot.GetValue(detail)!)[0]!;
                        var items=(System.Collections.IList)(native?typeof(StoredDetail).GetProperty(field)!.GetValue(actual):slot.GetValue(actual))!;
                        Assert.Single(items.Cast<object>());
                        foreach(var property in slot.PropertyType.GetGenericArguments()[0].GetProperties())
                            Assert.Equal(IndependentAvatarCollections.Scalar(property,expected),IndependentAvatarCollections.Scalar(property,items[0]!));
                    }
                }
                var skills=new IndependentAvatarSkills();var skillValue=revision*100;
                var skillProperties=typeof(IAvatarSkills).GetProperties();Assert.Equal(16,skillProperties.Length);
                foreach(var property in skillProperties)property.SetValue(skills,skillValue++);
                detail.Skills=skills;
                detail.Stats=new IndependentAvatarStats();detail.SuperPowers=new IndependentAvatarPowers();
                detail.Aura=new IndependentAvatarAura();detail.HumanDesign=new IndependentHumanDesign();
                var structuredFields=new[]{"Stats","SuperPowers","Aura","HumanDesign"};
                foreach(var field in structuredFields){
                    var slot=typeof(IAvatarDetail).GetProperty(field)!;var number=revision*100;
                    foreach(var property in slot.PropertyType.GetProperties()){
                        object value=property.PropertyType==typeof(int)?number++:property.PropertyType==typeof(string)?"human-type-"+revision:
                            new AvatarStat{Current=number++,Max=number+100};
                        property.SetValue(slot.GetValue(detail),value);
                    }
                }
                void VerifyStructured(object actual,bool native){
                    foreach(var field in structuredFields){
                        var slot=typeof(IAvatarDetail).GetProperty(field)!;var expected=slot.GetValue(detail);
                        var restored=native?typeof(StoredDetail).GetProperty(field)!.GetValue(actual):slot.GetValue(actual);
                        Assert.NotNull(restored);
                        foreach(var property in slot.PropertyType.GetProperties()){
                            if(property.GetValue(expected) is AvatarStat stat){
                                var found=(AvatarStat)property.GetValue(restored)!;Assert.NotNull(found);
                                Assert.Equal(stat.Current,found.Current);Assert.Equal(stat.Max,found.Max);
                            }else Assert.Equal(property.GetValue(expected),property.GetValue(restored));
                        }
                    }
                }
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
                VerifyStructured(raw,true);VerifyStructured(saved.Result,false);
                VerifyCollections(raw,true);VerifyCollections(saved.Result,false);
                VerifyChakras(raw.Chakras);VerifyChakras(saved.Result.Chakras);
                VerifyInventory(raw.Inventory);VerifyInventory(saved.Result.Inventory);
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
                        VerifyStructured(loaded.Result,false);
                        VerifyCollections(loaded.Result,false);
                        VerifyChakras(loaded.Result.Chakras);
                        VerifyInventory(loaded.Result.Inventory);
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
