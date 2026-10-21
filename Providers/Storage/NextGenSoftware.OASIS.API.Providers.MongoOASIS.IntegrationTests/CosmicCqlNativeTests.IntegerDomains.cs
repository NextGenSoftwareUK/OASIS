using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests;

public sealed partial class CosmicCqlNativeTests
{
    private async Task CheckNarrowIntegerDomains(IMongoCollection<BsonDocument> rows,bool arrayDictionary,Holon a,Holon b,
        CosmicQuery<Holon> baseline,Func<Expression<Func<Holon,bool>>,Holon[],Task<IHolon[]>> check,
        Func<CosmicQuery<Holon>,Holon[],Task> window)
    {
        var originalA=await rows.Find(new BsonDocument("HolonId",a.Id.ToString("D"))).SingleAsync();
        var originalB=await rows.Find(new BsonDocument("HolonId",b.Id.ToString("D"))).SingleAsync();
        async Task Set(BsonDocument original,BsonValue value,bool reference=true)
        {
            var metadata=original["MetaData"].DeepClone();
            void Add(string key,BsonValue item)
            {if(arrayDictionary)metadata.AsBsonArray.Add(new BsonArray{key,item});else metadata.AsBsonDocument.Add(key,item);}
            Add("narrow",value);if(reference)Add("narrow.reference",value);
            var ack=await rows.UpdateOneAsync(new BsonDocument("_id",original["_id"]),new BsonDocument("$set",new BsonDocument("MetaData",metadata)));
            Assert.Equal(1,ack.MatchedCount);
        }
        foreach(var type in new[]{typeof(byte),typeof(sbyte),typeof(short),typeof(ushort),typeof(uint),typeof(HolonType),typeof(ProviderType)})
        {
            (long min,long max)=type==typeof(byte)?(0L,255L):type==typeof(sbyte)?(-128L,127L):
                type==typeof(short)?(-32768L,32767L):type==typeof(ushort)?(0L,65535L):(0L,4294967295L);
            var enumValues=type.IsEnum?Enum.GetValues(type).Cast<object>().Select(value=>Convert.ToInt64(value)).Distinct().OrderBy(value=>value).ToArray():Array.Empty<long>();
            if(type.IsEnum){min=enumValues.First();max=enumValues.Last();}
            var owner=Expression.Parameter(typeof(Holon),"h");
            MethodCallExpression Path(Type declared,string key)=>Expression.Call(typeof(CosmicQueryMetadata),nameof(CosmicQueryMetadata.Value),new[]{declared},owner,
                Expression.NewArrayInit(typeof(string),Expression.Constant(key)));
            var path=Path(type,"narrow");var nullable=typeof(Nullable<>).MakeGenericType(type);var nullablePath=Path(nullable,"narrow");
            Expression Wide(Expression expression)=>Expression.Convert(type.IsEnum?Expression.Convert(expression,Enum.GetUnderlyingType(type)):expression,typeof(long));
            var membership=Array.CreateInstance(nullable,2);membership.SetValue(type.IsEnum?Enum.ToObject(type,min):Convert.ChangeType(min,type),1);
            var memberPredicate=Expression.Lambda<Func<Holon,bool>>(Expression.Call(typeof(Enumerable),nameof(Enumerable.Contains),new[]{nullable},
                Expression.Constant(membership,nullable.MakeArrayType()),nullablePath),owner);
            var vectors=new (BsonValue Value,bool Valid,long Number)[]{
                (new BsonInt64(min),true,min),(new BsonInt64(max),true,max),(new BsonInt32((int)min),true,min),
                (new BsonInt64(min-1),false,0),(new BsonInt64(max+1),false,0),(BsonNull.Value,false,0),
                (new BsonString("1"),false,0),(new BsonDouble(1.5),false,0),(BsonBoolean.True,false,0),
                (new BsonArray{1},false,0),(new BsonDecimal128(1),false,0),(new BsonDouble(1),false,0)};
            if(type.IsEnum)
            {
                var gap=Enumerable.Range(0,10000).Select(value=>(long)value).First(value=>!enumValues.Contains(value));
                vectors=vectors.Append(((BsonValue)new BsonInt64(gap),false,0L)).ToArray();
            }
            foreach(var vector in vectors)
            {
                output.WriteLine("Narrow integer vector: "+type.Name+" "+vector.Value.ToJson());
                await Set(originalA,vector.Value);
                foreach(var operation in new[]{ExpressionType.Equal,ExpressionType.NotEqual,ExpressionType.GreaterThan,ExpressionType.GreaterThanOrEqual,ExpressionType.LessThan,ExpressionType.LessThanOrEqual})
                {
                    var predicate=Expression.Lambda<Func<Holon,bool>>(Expression.MakeBinary(operation,Wide(path),Expression.Constant(max)),owner);
                    var matches=vector.Valid && (operation switch{
                        ExpressionType.Equal=>vector.Number==max,ExpressionType.NotEqual=>vector.Number!=max,
                        ExpressionType.GreaterThan=>vector.Number>max,ExpressionType.GreaterThanOrEqual=>vector.Number>=max,
                        ExpressionType.LessThan=>vector.Number<max,_=>vector.Number<=max});
                    await check(predicate,matches?new[]{a}:Array.Empty<Holon>());
                }
                var fields=Expression.Lambda<Func<Holon,bool>>(Expression.Equal(path,Path(type,"narrow.reference")),owner);
                await check(fields,vector.Valid?new[]{a}:Array.Empty<Holon>());
                await check(memberPredicate,vector.Value.IsBsonNull || vector.Valid && vector.Number==min?new[]{a}:Array.Empty<Holon>());
            }
            await Set(originalA,new BsonInt64(min));await Set(originalB,new BsonInt64(max));
            var admitted=Expression.Lambda<Func<Holon,bool>>(Expression.GreaterThanOrEqual(Wide(path),Expression.Constant(min)),owner);
            var ordered=baseline.Where(admitted).OrderByDescending(Expression.Lambda<Func<Holon,long>>(Wide(path),owner));
            Assert.False(ordered.IsError,ordered.Message);await window(ordered.Result,new[]{b,a});
            await rows.UpdateOneAsync(new BsonDocument("_id",originalB["_id"]),new BsonDocument("$set",new BsonDocument("MetaData",originalB["MetaData"])));
        }
        await rows.UpdateOneAsync(new BsonDocument("_id",originalA["_id"]),new BsonDocument("$set",new BsonDocument("MetaData",originalA["MetaData"])));
    }
}
