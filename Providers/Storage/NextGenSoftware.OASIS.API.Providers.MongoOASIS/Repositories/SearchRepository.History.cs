using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Helpers;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;

public partial class SearchRepository
{
    internal static BsonDocument SelectSearchVersion(BsonDocument document,int version,bool avatar)
    {
        if(version<1)throw new ArgumentOutOfRangeException(nameof(version));
        IReadOnlyList<IHolonBase> history=avatar
            ?MongoNativeAccountHistory.ReadAvatar(document).Cast<IHolonBase>().ToArray()
            :MongoNativeHolonHistory.Read(document).Cast<IHolonBase>().ToArray();
        var selected=history.SingleOrDefault(row=>row.Version==version);
        if(selected==null)return null;
        BsonDocument snapshot=null;
        if(document["Version"].ToInt32()==version)snapshot=(BsonDocument)document.DeepClone();
        else if(document.TryGetValue(MongoNativeHolonHistory.Field,out var archive))
            foreach(var entry in archive["records"].AsBsonArray)
                if(entry.AsBsonDocument.TryGetValue("bson",out var bson)&&bson["Version"].ToInt32()==version){snapshot=(BsonDocument)bson.DeepClone();break;}
        if(snapshot==null)
        {
            // The validated schema1 JSON format cannot recover BSON subtypes it never retained.
            if(avatar)throw new InvalidDataException("Mongo avatar search archive lacks its native snapshot.");
            snapshot=DataHelper.ConvertOASISHolonToMongoEntity((IHolon)selected,false).ToBsonDocument();
            snapshot["_id"]=document["_id"].DeepClone();
        }
        snapshot.Remove(MongoNativeHolonHistory.Field);
        return snapshot;
    }

    private async Task<List<T>> ReadHistoricalSearchMatchesAsync<T>(IMongoCollection<T> collection,FilterDefinition<T> filter,
        int version,bool synchronous,Dictionary<Type,List<BsonDocument>> cache)
    {
        if(!cache.TryGetValue(typeof(T),out var documents))
        {
            if(typeof(T)!=typeof(Holon)&&typeof(T)!=typeof(Avatar))throw new NotSupportedException("Unsupported native search family.");
            documents=new List<BsonDocument>();
            var raw=collection.Database.GetCollection<BsonDocument>(collection.CollectionNamespace.CollectionName);
            using var cursor=synchronous?raw.FindSync(FilterDefinition<BsonDocument>.Empty):await raw.FindAsync(FilterDefinition<BsonDocument>.Empty).ConfigureAwait(false);
            while(synchronous?cursor.MoveNext():await cursor.MoveNextAsync().ConfigureAwait(false))
                foreach(var document in cursor.Current){var selected=SelectSearchVersion(document,version,typeof(T)==typeof(Avatar));if(selected!=null)documents.Add(selected);}
            cache.Add(typeof(T),documents);
        }
        var rendered=filter.Render(new RenderArgs<T>(collection.DocumentSerializer,collection.Settings.SerializerRegistry));
        var result=new List<T>();var batch=new BsonArray();var bytes=0;
        async Task Flush()
        {
            if(batch.Count==0)return;
            PipelineDefinition<NoPipelineInput,T> pipeline=new[]{new BsonDocument("$documents",new BsonDocument("$literal",batch)),new BsonDocument("$match",rendered)};
            using var cursor=synchronous?collection.Database.Aggregate(pipeline):await collection.Database.AggregateAsync(pipeline).ConfigureAwait(false);
            result.AddRange(synchronous?cursor.ToList():await cursor.ToListAsync().ConfigureAwait(false));batch=new BsonArray();bytes=0;
        }
        foreach(var document in documents){var size=document.ToBson().Length;if(batch.Count>0&&(batch.Count>=256||(long)bytes+size>1024*1024))await Flush().ConfigureAwait(false);batch.Add(document);bytes=checked(bytes+size);}
        await Flush().ConfigureAwait(false);
        return result;
    }
}
