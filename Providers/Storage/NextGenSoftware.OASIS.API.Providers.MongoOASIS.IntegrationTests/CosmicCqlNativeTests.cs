using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.DNA;
using Xunit;
using Xunit.Abstractions;
using Provider=NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests;

public sealed class CosmicCqlNativeTests(ITestOutputHelper output)
{
    public sealed class RatedCoffee:Holon
    {
        [NextGenSoftware.OASIS.API.Core.CustomAttrbiutes.CustomOASISProperty]
        public int Rating {get;set;}
    }
    private static HolonManager Manager(Provider provider)
    {
        var dna=new OASISDNA{OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS{HyperDriveMode=HyperDriveModes.V2,
            StorageProviders=new StorageProviderSettings{LogSwitchingProviders=false}}};
        var registry=new ProviderManager(null,dna){IsAutoFailOverEnabled=false,IsAutoReplicationEnabled=false,IsAutoLoadBalanceEnabled=false};
        return new HolonManager(provider,dna,registry);
    }
    [Fact]
    public async Task NativeProjectedPayloadBudgetRejectsWithoutPublishingPartialRows()
    {
        const string uri="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.Equal(uri,Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION"));
        var workspace=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../.."));
        using var lease=File.Open(Path.Combine(workspace,"TestResults/SubscriptionUsageCurrent/cosmic-cql-environment.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        const string name="cosmic_cql_fixture";var client=new MongoClient(uri);
        using var names=await client.ListDatabaseNamesAsync();Assert.DoesNotContain(name,await names.ToListAsync());
        var database=client.GetDatabase(name);await database.CreateCollectionAsync("CosmicOwnedFixture");
        var provider=new Provider(uri,name);var complete=false;
        try
        {
            Assert.False((await provider.ActivateProviderAsync()).IsError);
            var payload=new string('x',8*1024*1024);
            var roots=Enumerable.Range(0,9).Select(index=>new Holon{Id=Guid.NewGuid(),Name="owned CQL bounded payload "+index,
                MetaData=new(){["payload"]=payload}}).ToArray();
            foreach(var root in roots)
            {
                var seeded=await provider.SaveHolonAsync(root,saveChildren:false);
                Assert.False(seeded.IsError,seeded.Message+seeded.Exception);
            }
            var ids=roots.Select(root=>root.Id).ToArray();var query=CosmicQuery<Holon>.Create().Where(root=>ids.Contains(root.Id));
            Assert.False(query.IsError,query.Message);
            var plan=CosmicQueryPlanner.Plan(query.Result,ProviderType.MongoDBOASIS,new[]{new CosmicQueryTarget(ProviderType.MongoDBOASIS,Provider.CqlCapabilities)});
            Assert.False(plan.IsError,plan.Message);
            var result=await provider.ExecuteCosmicQueryAsync(plan.Result,maximumResults:100);
            Assert.True(result.IsError);Assert.Equal("CQL_QUERY_LIMIT",result.ErrorCode);Assert.Null(result.Result);Assert.False(result.IsLoaded);
            Assert.Equal(9,await database.GetCollection<BsonDocument>("Holon").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
            complete=true;
        }
        finally
        {
            await provider.DeActivateProviderAsync();
            if(complete)await client.DropDatabaseAsync(name);
            else output.WriteLine("Preserved owned cosmic_cql_fixture payload-budget evidence for independent reconciliation.");
        }
    }
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task NativePredicatesPreserveMetadataSemanticsAndConditionalWriteBack(bool arrayDictionary)
    {
        const string uri="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.Equal(uri,Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION"));
        var workspace=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../.."));
        using var lease=File.Open(Path.Combine(workspace,"TestResults/SubscriptionUsageCurrent/cosmic-cql-environment.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        const string name="cosmic_cql_fixture";var client=new MongoClient(uri);
        using var names=await client.ListDatabaseNamesAsync();Assert.DoesNotContain(name,await names.ToListAsync());
        var database=client.GetDatabase(name);await database.CreateCollectionAsync("CosmicOwnedFixture");
        var provider=new Provider(uri,name);var complete=false;
        Holon Row(string label,int version)=>new(){Id=Guid.NewGuid(),Name=label,Version=version,VersionId=Guid.NewGuid()};
        var a=Row("$where.coffee😀\0end",2);a.Description="espresso%_";
        a.MetaData=new(){["stats"]=new Dictionary<string,object>{{"score",7}},["a.b"]="literal",["$tag"]="coffee",["nullable"]=null,
            ["number"]=9007199254740993L,["flag"]=true,["optional"]=7,["Rating"]=7,["Name"]="forged metadata name"};
        var b=Row("tea",3);b.MetaData=new(){["stats"]=new Dictionary<string,object>{{"score","7"}},["a"]=new Dictionary<string,object>{{"b","nested"}},["flag"]=false,["Rating"]=8};
        var c=Row("coffee",4);c.MetaData=new(){["stats"]=new Dictionary<string,object>{{"score",8}},["nullable"]="value",["number"]=9L,["optional"]="7",["Rating"]=9};
        var d=Row(null,1);d.MetaData=new(){["stats"]=new object[]{new Dictionary<string,object>{{"score",7}}},["nullable"]=null,["optional"]=null};
        var child=Row("owned unselected child",1);a.Children.Add(child);
        var roots=new[]{a,b,c,d};var ids=roots.Select(root=>root.Id).ToArray();
        CosmicQueryPlan<Holon> Plan(Expression<Func<Holon,bool>> predicate)
        {
            var query=CosmicQuery<Holon>.Create().Where(root=>ids.Contains(root.Id));Assert.False(query.IsError,query.Message);
            query=query.Result.Where(predicate);Assert.False(query.IsError,query.Message);
            var plan=CosmicQueryPlanner.Plan(query.Result,ProviderType.MongoDBOASIS,new[]{new CosmicQueryTarget(ProviderType.MongoDBOASIS,Provider.CqlCapabilities)});
            Assert.False(plan.IsError,plan.Message);return plan.Result;
        }
        try
        {
            Assert.False((await provider.ActivateProviderAsync()).IsError);
            var seeded=await provider.SaveHolonsAsync(roots);Assert.False(seeded.IsError,seeded.Message+seeded.Exception);
            var rows=database.GetCollection<BsonDocument>("Holon");
            if(arrayDictionary)
                foreach(var root in roots)
                {
                    var native=await rows.Find(new BsonDocument("HolonId",root.Id.ToString("D"))).SingleAsync();
                    var pairs=new BsonArray(native["MetaData"].AsBsonDocument.Select(pair=>(BsonValue)new BsonArray{pair.Name,pair.Value}));
                    await rows.UpdateOneAsync(new BsonDocument("_id",native["_id"]),new BsonDocument("$set",new BsonDocument("MetaData",pairs)));
                }
            var manager=Manager(provider);ICosmicQueryStorageProvider adapter=provider;
            async Task<IHolon[]> Check(Expression<Func<Holon,bool>> predicate,params Holon[] expected)
            {
                var result=await manager.ExecuteQueryAsync(Plan(predicate),new[]{adapter});Assert.False(result.IsError,result.Message+result.Exception);
                var values=result.Result.Providers.Single().Rows.Cast<IHolon>().ToArray();
                Assert.Equal(expected.Select(root=>root.Id).OrderBy(id=>id),values.Select(root=>root.Id).OrderBy(id=>id));
                Assert.All(values,value=>{Assert.Empty(value.Children);Assert.False(value.HasHolonChanged());});return values;
            }
            await Check(root=>root.Name.StartsWith("$where",StringComparison.Ordinal)&&root.Description.Contains("%_"),a);
            await Check(root=>root.Name.EndsWith("\0end",StringComparison.Ordinal),a);
            await Check(root=>root.Version>=3,b,c);
            await Check(root=>root.Version!=3,a,c,d);
            var emptyId=Guid.Empty;await Check(root=>root.ParentHolonId!=emptyId);
            await Check(root=>root.IsPublic!=true,a,b,c,d);
            await Check(root=>root.Name==null,d);
            await Check(root=>root.Name!="tea",a,c,d);
            await Check(root=>root.Description!="espresso%_",b,c,d);
            await Check(root=>CosmicQueryMetadata.Value<int>(root,"stats","score")>=7,a,c);
            await Check(root=>! (CosmicQueryMetadata.Value<int>(root,"stats","score")>=7),b,d);
            await Check(root=>CosmicQueryMetadata.Value<string>(root,"a.b")=="literal",a);
            await Check(root=>CosmicQueryMetadata.Value<string>(root,"a","b")=="nested",b);
            await Check(root=>CosmicQueryMetadata.Value<string>(root,"$tag")=="coffee",a);
            await Check(root=>CosmicQueryMetadata.Exists(root,"nullable"),a,c,d);
            await Check(root=>CosmicQueryMetadata.IsNull(root,"nullable"),a,d);
            await Check(root=>!CosmicQueryMetadata.Exists(root,"nullable"),b);
            await Check(root=>CosmicQueryMetadata.Value<string>(root,"nullable")==null,a,d);
            await Check(root=>CosmicQueryMetadata.Value<string>(root,"nullable")!=null,c);
            await Check(root=>CosmicQueryMetadata.Value<int?>(root,"optional")==null,d);
            await Check(root=>CosmicQueryMetadata.Value<int?>(root,"optional")!=null,a);
            await Check(root=>CosmicQueryMetadata.Value<int?>(root,"optional")!=7,d);
            await Check(root=>CosmicQueryMetadata.Value<string>(root,"nullable")!="value",a,d);
            await Check(root=>!(CosmicQueryMetadata.Value<int?>(root,"optional")!=7),a,b,c);
            await Check(root=>CosmicQueryMetadata.Value<long>(root,"number")==9007199254740993L,a);
            await Check(root=>CosmicQueryMetadata.Value<int>(root,"number")==9,c);
            await Check(root=>CosmicQueryMetadata.Value<bool>(root,"flag")==true,a);
            await Check(root=>CosmicQueryMetadata.Value<string>(root,"$tag").EndsWith("fee",StringComparison.Ordinal),a);
            var selections=new[]{"coffee","tea"};await Check(root=>selections.Contains(root.Name),b,c);
            var target=Guid.Parse("aabbccdd-1234-5678-9abc-def012345678");
            var other=Guid.Parse("12345678-1234-5678-9abc-def012345678");
            var originalA=await rows.Find(new BsonDocument("HolonId",a.Id.ToString("D"))).SingleAsync();
            var guidVectors=new (BsonValue Value,bool Domain,bool Equal)[]{
                (new BsonString(target.ToString("D")),true,true),
                (new BsonString(target.ToString("D").ToUpperInvariant()),true,true),
                (new BsonBinaryData(target,GuidRepresentation.Standard),true,true),
                (new BsonBinaryData(target,GuidRepresentation.CSharpLegacy),true,true),
                (new BsonBinaryData(other,GuidRepresentation.Standard),true,false),
                (new BsonBinaryData(other,GuidRepresentation.CSharpLegacy),true,false),
                (new BsonString(other.ToString("D")),true,false),
                (BsonNull.Value,false,false),
                (new BsonString(target.ToString("N")),false,false),
                (new BsonString("{"+target+"}"),false,false),
                (new BsonString(target+"\n"),false,false),
                (new BsonString(target+"\0"),false,false),
                (new BsonString("zzbbccdd-1234-5678-9abc-def012345678"),false,false),
                (new BsonBinaryData(target.ToByteArray(),BsonBinarySubType.Binary),false,false),
                (new BsonBinaryData(new byte[15],BsonBinarySubType.Binary),false,false),
                (new BsonInt32(7),false,false),(new BsonArray{target.ToString("D")},false,false)};
            var guidMembers=new[]{target};var nullableGuidMembers=new Guid?[]{null,target};
            foreach(var vector in guidVectors)
            {
                var metadata=originalA["MetaData"].DeepClone();
                if(arrayDictionary)metadata.AsBsonArray.Add(new BsonArray{"guid.probe",vector.Value});
                else metadata.AsBsonDocument.Add("guid.probe",vector.Value);
                await rows.UpdateOneAsync(new BsonDocument("_id",originalA["_id"]),new BsonDocument("$set",new BsonDocument("MetaData",metadata)));
                await Check(root=>CosmicQueryMetadata.Value<Guid>(root,"guid.probe")==target,vector.Equal?new[]{a}:Array.Empty<Holon>());
                await Check(root=>CosmicQueryMetadata.Value<Guid>(root,"guid.probe")!=target,vector.Domain&&!vector.Equal?new[]{a}:Array.Empty<Holon>());
                await Check(root=>CosmicQueryMetadata.Value<Guid?>(root,"guid.probe")!=target,(vector.Domain&&!vector.Equal)||vector.Value.IsBsonNull?new[]{a}:Array.Empty<Holon>());
                await Check(root=>CosmicQueryMetadata.Value<Guid?>(root,"guid.probe")!=null,vector.Domain?new[]{a}:Array.Empty<Holon>());
                await Check(root=>guidMembers.Contains(CosmicQueryMetadata.Value<Guid>(root,"guid.probe")),vector.Equal?new[]{a}:Array.Empty<Holon>());
                await Check(root=>nullableGuidMembers.Contains(CosmicQueryMetadata.Value<Guid?>(root,"guid.probe")),vector.Equal||vector.Value.IsBsonNull?new[]{a}:Array.Empty<Holon>());
                await Check(root=>!(CosmicQueryMetadata.Value<Guid>(root,"guid.probe")==target),vector.Equal?new[]{b,c,d}:roots);
            }
            await rows.UpdateOneAsync(new BsonDocument("_id",originalA["_id"]),new BsonDocument("$set",new BsonDocument("MetaData",originalA["MetaData"])));
            var limited=await manager.ExecuteQueryAsync(Plan(root=>true),new[]{adapter},maximumResults:1);
            Assert.True(limited.IsError);Assert.Null(limited.Result);Assert.Equal("CQL_QUERY_LIMIT",limited.ErrorCode);
            using var cancelled=new CancellationTokenSource();cancelled.Cancel();
            Assert.Equal("CQL_QUERY_CANCELLED",(await provider.ExecuteCosmicQueryAsync(Plan(root=>true),cancellationToken:cancelled.Token)).ErrorCode);
            var aId=a.Id;var selected=(await Check(root=>root.Id==aId,a)).Single();var before=await rows.Find(new BsonDocument("HolonId",a.Id.ToString("D"))).SingleAsync();
            var noOp=await manager.SaveHolonAsync(selected,Guid.NewGuid(),saveChildren:false,providerType:ProviderType.MongoDBOASIS);
            Assert.False(noOp.IsError,noOp.Message+noOp.Exception);Assert.False(noOp.IsSaved);
            Assert.Equal(before.ToBson(),(await rows.Find(new BsonDocument("HolonId",a.Id.ToString("D"))).SingleAsync()).ToBson());
            var childBefore=(await rows.Find(new BsonDocument("HolonId",child.Id.ToString("D"))).SingleAsync()).ToBson();
            selected.Description="owned CQL conditional edit";
            var written=await manager.SaveHolonAsync(selected,Guid.NewGuid(),new CosmicWriteOptions(Guid.NewGuid()),saveChildren:false,providerType:ProviderType.MongoDBOASIS);
            Assert.False(written.IsError,written.Message+written.Exception);Assert.True(written.IsSaved);
            Assert.Equal(childBefore,(await rows.Find(new BsonDocument("HolonId",child.Id.ToString("D"))).SingleAsync()).ToBson());
            var linked=await provider.LoadHolonAsync(a.Id);Assert.False(linked.IsError,linked.Message+linked.Exception);Assert.Equal(child.Id,linked.Result.Children.Single().Id);
            var typed=CosmicQuery<RatedCoffee>.Create().Where(root=>ids.Contains(root.Id)&&root.Rating>=7);Assert.False(typed.IsError,typed.Message);
            var typedPlan=CosmicQueryPlanner.Plan(typed.Result,ProviderType.MongoDBOASIS,new[]{new CosmicQueryTarget(ProviderType.MongoDBOASIS,Provider.CqlCapabilities)});
            Assert.False(typedPlan.IsError,typedPlan.Message);
            var typedResult=await manager.ExecuteQueryAsync(typedPlan.Result,new[]{adapter});Assert.False(typedResult.IsError,typedResult.Message+typedResult.Exception);
            var rated=typedResult.Result.Providers.Single().Rows.Single(root=>root.Id==aId);
            Assert.Equal(7,rated.Rating);Assert.Equal(a.Name,rated.Name);Assert.False(rated.HasHolonChanged());
            rated.Rating=10;
            var typedWrite=await manager.SaveHolonAsync(rated,Guid.NewGuid(),new CosmicWriteOptions(Guid.NewGuid()),saveChildren:false,providerType:ProviderType.MongoDBOASIS);
            Assert.False(typedWrite.IsError,typedWrite.Message+typedWrite.Exception);Assert.False(rated.HasHolonChanged());
            var reloaded=await provider.LoadHolonAsync(aId);Assert.False(reloaded.IsError,reloaded.Message+reloaded.Exception);Assert.Equal("10",reloaded.Result.MetaData["Rating"].ToString());
            complete=true;
        }
        finally
        {
            await provider.DeActivateProviderAsync();
            if(complete)await client.DropDatabaseAsync(name);
            else output.WriteLine("Preserved owned cosmic_cql_fixture query evidence for independent reconciliation; arrayDictionary="+arrayDictionary);
        }
    }
}
