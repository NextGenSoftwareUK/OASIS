using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

// BSON expressions only: user strings are literal values, never field/expression fragments.
internal sealed class MongoCqlTranslator
{
    private int nodes;
    internal BsonDocument Compile<T>(CosmicQuery<T> query) where T:Core.Interfaces.IHolon
    {
        var admitted=MongoCqlContract.Capabilities.Admit(query);
        if(admitted.IsError)throw new NotSupportedException(admitted.Message);
        var predicates=query.Predicates.Select(predicate=>Translate(predicate,0)).ToArray();
        var expression=predicates.Length==0?Literal(true):Op("$and",predicates);
        var filter=new BsonDocument("$expr",expression);
        var pending=new Stack<(BsonValue Value,int Depth)>();pending.Push((filter,0));
        while(pending.Count!=0)
        {
            var entry=pending.Pop();if(entry.Depth>90)throw new MongoCqlLimitException();
            if(entry.Value is BsonDocument document)foreach(var field in document)pending.Push((field.Value,entry.Depth+1));
            else if(entry.Value is BsonArray array)foreach(var value in array)pending.Push((value,entry.Depth+1));
        }
        if(filter.ToBson().Length>1024*1024)throw new MongoCqlLimitException();
        return filter;
    }
    private static BsonDocument Literal(BsonValue value)=>new("$literal",value);
    private static BsonDocument Op(string name,params BsonValue[] values)=>new(name,new BsonArray(values));
    private static BsonDocument Type(BsonValue value)=>new("$type",value);
    private static BsonDocument Choose(BsonValue test,BsonValue yes,BsonValue no)=>Op("$cond",test,yes,no);
    private static BsonValue Constant(CosmicQueryLiteral value)=>Literal(value.Kind switch{
        CosmicQueryScalarKind.Null=>BsonNull.Value,
        CosmicQueryScalarKind.String=>new BsonString((string)value.Value),
        CosmicQueryScalarKind.Boolean=>new BsonBoolean((bool)value.Value),
        CosmicQueryScalarKind.Int64=>new BsonInt64(Convert.ToInt64(value.Value,System.Globalization.CultureInfo.InvariantCulture)),
        CosmicQueryScalarKind.Guid=>new BsonString(((Guid)value.Value).ToString("D")),
        _=>throw new NotSupportedException("MongoDB CQL literal type is not admitted.")});

    private static BsonValue Canonical(CosmicQueryPath path)
    {
        var name=path.Segments.Single();BsonValue value="$"+(name=="Id"?"HolonId":name);
        if(path.ValueType==typeof(Guid))
        {
            value=Op("$ifNull",value,Literal(Guid.Empty.ToString("D")));
            return new BsonDocument("$toLower",Choose(Op("$eq",value,Literal("")),Literal(Guid.Empty.ToString("D")),value));
        }
        if(path.ValueType==typeof(string))return Op("$ifNull",value,Literal(BsonNull.Value));
        return Op("$ifNull",value,Literal(path.ValueType==typeof(bool)?new BsonBoolean(name=="IsActive"):new BsonInt32(0)));
    }
    private static BsonValue Domain(BsonValue value,Type declared)
    {
        var type=Nullable.GetUnderlyingType(declared)??declared;
        if(type==typeof(string)||type==typeof(Guid))return Op("$eq",Type(value),Literal("string"));
        if(type==typeof(bool))return Op("$eq",Type(value),Literal("bool"));
        if(type==typeof(int)||type==typeof(long)||type.IsEnum)
        {
            BsonValue guard=Op("$in",Type(value),Literal(new BsonArray{"int","long"}));
            if(type!=typeof(long))guard=Op("$and",guard,Op("$gte",value,Literal(int.MinValue)),Op("$lte",value,Literal(int.MaxValue)));
            return guard;
        }
        throw new NotSupportedException("MongoDB CQL path type is not admitted.");
    }
    private static BsonValue Metadata(IReadOnlyList<string> segments,Func<BsonValue,BsonValue> predicate)
    {
        if(segments.Any(segment=>segment.IndexOf('\0')>=0))throw new NotSupportedException("MongoDB metadata field names cannot contain NUL.");
        // Both dictionary representations are already part of this provider's read contract.
        BsonValue root=Choose(Op("$eq",Type("$MetaData"),Literal("array")),new BsonDocument("$arrayToObject","$MetaData"),"$MetaData");
        BsonValue Walk(BsonValue input,int index)
        {
            var name="cqlm"+index;BsonValue current="$$"+name;
            var body=index==segments.Count?predicate(current):Walk(new BsonDocument("$getField",new BsonDocument{
                {"field",Literal(segments[index])},{"input",Choose(Op("$eq",Type(current),Literal("object")),current,Literal(new BsonDocument()))}}),index+1);
            return new BsonDocument("$let",new BsonDocument{{"vars",new BsonDocument(name,input)},{"in",body}});
        }
        return Walk(root,0);
    }
    private static BsonValue At(CosmicQueryPath path,Func<BsonValue,BsonValue> predicate)
        =>path.IsMetadata?Metadata(path.Segments,predicate):predicate(Canonical(path));
    private BsonValue Translate(CosmicQueryPredicate predicate,int depth)
    {
        if(++nodes>2048||depth>64)throw new MongoCqlLimitException();
        switch(predicate)
        {
            case CosmicQueryBoolean boolean:return Literal(boolean.Value);
            case CosmicQueryLogical logical:return Op(logical.IsAnd?"$and":"$or",Translate(logical.Left,depth+1),Translate(logical.Right,depth+1));
            case CosmicQueryNot not:return Op("$not",Translate(not.Operand,depth+1));
            case CosmicQueryMetadataTest test:return Metadata(test.Segments,value=>test.Kind==CosmicQueryMetadataTestKind.IsNull
                ?Op("$eq",Type(value),Literal("null")):Op("$ne",Type(value),Literal("missing")));
            case CosmicQueryComparison comparison:return At(comparison.Path,value=>{
                if(comparison.Literal.Kind==CosmicQueryScalarKind.Null)
                    return comparison.Operator==CosmicQueryComparisonOperator.Equal?Op("$eq",Type(value),Literal("null"))
                        :Domain(value,comparison.Path.ValueType);
                var symbol=comparison.Operator switch{
                    CosmicQueryComparisonOperator.Equal=>"$eq",CosmicQueryComparisonOperator.NotEqual=>"$ne",
                    CosmicQueryComparisonOperator.GreaterThan=>"$gt",CosmicQueryComparisonOperator.GreaterThanOrEqual=>"$gte",
                    CosmicQueryComparisonOperator.LessThan=>"$lt",CosmicQueryComparisonOperator.LessThanOrEqual=>"$lte",
                    _=>throw new NotSupportedException("Unknown CQL comparison.")};
                var domain=Domain(value,comparison.Path.ValueType);
                var test=Choose(domain,Op(symbol,value,Constant(comparison.Literal)),Literal(false));
                if(comparison.Operator!=CosmicQueryComparisonOperator.NotEqual)return test;
                if(!comparison.Path.IsMetadata)
                    return Op("$not",Choose(domain,Op("$eq",value,Constant(comparison.Literal)),Literal(false)));
                // Sparse metadata keeps missing/wrong-type paths outside the atomic domain,
                // while an explicit nullable null differs from every non-null literal.
                return comparison.Path.ValueType==typeof(string)||Nullable.GetUnderlyingType(comparison.Path.ValueType)!=null
                    ?Op("$or",test,Op("$eq",Type(value),Literal("null"))):test;
            });
            case CosmicQueryMembership membership:return At(membership.Path,value=>{
                var guard=Domain(value,membership.Path.ValueType);
                if(membership.Values.Any(item=>item.Kind==CosmicQueryScalarKind.Null))guard=Op("$or",guard,Op("$eq",Type(value),Literal("null")));
                var values=new BsonArray(membership.Values.Select(item=>((BsonDocument)Constant(item))["$literal"]));
                return Choose(guard,Op("$in",value,Literal(values)),Literal(false));
            });
            case CosmicQueryStringMatch match:return At(match.Path,value=>{
                BsonValue content=Literal(match.Value);BsonValue expression;
                if(match.Operator==CosmicQueryStringOperator.EndsWith)
                {
                    var offset=Op("$subtract",new BsonDocument("$strLenBytes",value),new BsonDocument("$strLenBytes",content));
                    expression=Choose(Op("$gte",offset,Literal(0)),Op("$eq",Op("$indexOfBytes",value,content,offset),offset),Literal(false));
                }
                else expression=Op(match.Operator==CosmicQueryStringOperator.Contains?"$gte":"$eq",Op("$indexOfBytes",value,content),Literal(0));
                return Choose(Domain(value,typeof(string)),expression,Literal(false));
            });
            default:throw new NotSupportedException("MongoDB CQL predicate is unsupported.");
        }
    }
}
internal sealed class MongoCqlLimitException():Exception("MongoDB CQL exceeds its node, BSON-depth, one-MiB query, result-count or 64-MiB projected-payload budget.");
