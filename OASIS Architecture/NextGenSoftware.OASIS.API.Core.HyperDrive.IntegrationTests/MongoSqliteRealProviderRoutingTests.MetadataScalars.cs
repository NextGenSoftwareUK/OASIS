using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using NativeHolon=NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Holon;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class MongoSqliteRealProviderRoutingTests
{
    private static async Task VerifyMongoScalarMetadataAsync(MongoDBOASIS mongo,SQLLiteDBOASIS sqlite,bool synchronous,IMongoCollection<NativeHolon> records)
    {
        var key="scalar."+Guid.NewGuid().ToString("N");var tag="tag."+key;
        var identity=Guid.NewGuid();
        object?[] values={"$literal-text",123,4294967296L,true,false,identity,new Dictionary<string,object>{{"nested","$literal-text"}},new[] {"$literal-text","other"},null,null};
        var fixtures=values.Select((value,index)=>
        {
            var holon=new Holon {Id=Guid.NewGuid(),Name="scalar metadata "+index,HolonType=HolonType.Quest,Version=481,IsActive=true};
            if(index!=values.Length-1) holon.MetaData[key]=value;
            holon.MetaData[tag]="yes";return holon;
        }).ToArray();
        var ids=fixtures.Select(row=>row.Id).ToArray();
        try
        {
            foreach(var fixture in fixtures)
            {
                (await mongo.SaveHolonAsync(fixture,saveChildren:false)).IsError.Should().BeFalse();
                (await sqlite.SaveHolonAsync(fixture,saveChildren:false)).IsError.Should().BeFalse();
            }
            var raw=records.Database.GetCollection<BsonDocument>("Holon");
            foreach(var index in new[] {3,4,5,6,7})
            {
                var nativeKey=await records.Find(row=>row.HolonId==fixtures[index].Id).Project(row=>row.Id).SingleAsync();
                var filter=new BsonDocument("_id",ObjectId.Parse(nativeKey));
                var stored=await raw.Find(filter).SingleAsync();
                if(index is 3 or 4)
                {
                    stored["MetaData"][key].BsonType.Should().Be(BsonType.Boolean);
                    var field=new BsonDocument("$getField",new BsonDocument {{"field",new BsonDocument("$literal",key)},{"input","$MetaData"}});
                    var converted=await raw.Aggregate().Match(filter).Project<BsonDocument>(new BsonDocument("text",new BsonDocument("$toString",field))).SingleAsync();
                    converted["text"].AsString.Should().Be(index==3 ? "true":"false","Mongo's native boolean conversion differs from the existing .NET query contract");
                }
                else if(index==5) stored["MetaData"][key].AsString.Should().Be(identity.ToString(),"the public save path must store a canonical queryable GUID string");
                else
                {
                    BsonValue compound=index==6 ? new BsonDocument("nested","$literal-text"):new BsonArray {"$literal-text","other"};
                    await raw.UpdateOneAsync(filter,Builders<BsonDocument>.Update.Set("MetaData",new BsonDocument {{key,compound},{tag,"yes"}}));
                }
            }
            var queries=new[] {"$literal-text","123","4294967296",bool.TrueString,bool.FalseString,identity.ToString(),"no-match"};
            foreach(var query in queries)
            foreach(var provider in new IOASISStorageProvider[] {mongo,sqlite})
            foreach(var mode in new[] {"scalar","all","any"})
            {
                var pairs=new Dictionary<string,string>{{key,query},{mode=="any" ? "missing."+key:tag,mode=="any" ? "never":"yes"}};
                var match=mode=="any" ? MetaKeyValuePairMatchMode.Any:MetaKeyValuePairMatchMode.All;
                var result=mode=="scalar"
                    ? (synchronous ? provider.LoadHolonsByMetaData(key,query,HolonType.Quest,loadChildren:false,version:481)
                        :await provider.LoadHolonsByMetaDataAsync(key,query,HolonType.Quest,loadChildren:false,version:481))
                    : (synchronous ? provider.LoadHolonsByMetaData(pairs,match,HolonType.Quest,loadChildren:false,version:481)
                        :await provider.LoadHolonsByMetaDataAsync(pairs,match,HolonType.Quest,loadChildren:false,version:481));
                result.IsError.Should().BeFalse(result.Exception?.ToString() ?? result.Message);
                var index=Array.IndexOf(queries,query);
                result.Result.Select(row=>row.Id).Should().Equal(index<6 ? new[] {fixtures[index].Id}:Array.Empty<Guid>(),
                    "scalar matching must be consistent: provider {0}, query {1}, mode {2}; compound/null/missing values are not scalar hits",provider.GetType().Name,query,mode);
            }
        }
        finally
        {
            await records.DeleteManyAsync(row=>ids.Contains(row.HolonId));
            foreach(var id in ids) if((await sqlite.LoadHolonAsync(id,loadChildren:false)).Result!=null)
                (await sqlite.DeleteHolonAsync(id)).IsError.Should().BeFalse();
        }
    }
}
