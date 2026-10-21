using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

// BSON expressions only: user strings are literal values, never field/expression fragments.
internal sealed partial class MongoCqlTranslator
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
        CosmicQueryScalarKind.UtcDateTime=>new BsonInt64(((DateTime)value.Value).Ticks),
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
    private static bool GuidMetadata(CosmicQueryPath path)=>path.IsMetadata&&(Nullable.GetUnderlyingType(path.ValueType)??path.ValueType)==typeof(Guid);
    private static BsonValue GuidOperand(BsonValue value)=>Choose(Op("$eq",Type(value),Literal("string")),new BsonDocument("$toLower",value),value);
    private static IEnumerable<BsonValue> GuidLiterals(Guid value)
    {
        yield return new BsonString(value.ToString("D"));
        yield return new BsonBinaryData(value,GuidRepresentation.Standard);
        yield return new BsonBinaryData(value,GuidRepresentation.CSharpLegacy);
    }
    private static BsonValue GuidDomain(BsonValue value)
    {
        var text=new BsonDocument("$regexMatch",new BsonDocument{{"input",value},{"regex",Literal(@"\A[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\z")}});
        // BSON BinData ordering is length, subtype, unsigned bytes. These inclusive
        // ranges admit exactly 16-byte UUID subtypes already supported by our reader.
        BsonValue BinaryRange(BsonBinarySubType subtype)=>Op("$and",
            Op("$gte",value,Literal(new BsonBinaryData(new byte[16],subtype))),
            Op("$lte",value,Literal(new BsonBinaryData(Enumerable.Repeat((byte)255,16).ToArray(),subtype))));
        return Choose(Op("$eq",Type(value),Literal("string")),text,
            Op("$and",Op("$eq",Type(value),Literal("binData")),Op("$or",BinaryRange(BsonBinarySubType.UuidStandard),BinaryRange(BsonBinarySubType.UuidLegacy))));
    }
    private static BsonValue Domain(BsonValue value,Type declared,bool metadata=false)
    {
        var type=Nullable.GetUnderlyingType(declared)??declared;
        if(metadata&&type==typeof(Guid))return GuidDomain(value);
        if(type==typeof(DateTime))return Op("$and",Op("$eq",Type(value),Literal("long")),
            Op("$gte",value,Literal(DateTime.MinValue.Ticks)),Op("$lte",value,Literal(DateTime.MaxValue.Ticks)));
        if(type==typeof(string)||type==typeof(Guid))return Op("$eq",Type(value),Literal("string"));
        if(type==typeof(bool))return Op("$eq",Type(value),Literal("bool"));
        if(type==typeof(byte)||type==typeof(sbyte)||type==typeof(short)||type==typeof(ushort)||type==typeof(uint)
            ||type==typeof(int)||type==typeof(long)||type.IsEnum)
        {
            BsonValue guard=Op("$in",Type(value),Literal(new BsonArray{"int","long"}));
            if(type!=typeof(long))
            {
                long minimum=type==typeof(byte)||type==typeof(ushort)||type==typeof(uint)?0:
                    type==typeof(sbyte)?sbyte.MinValue:type==typeof(short)?short.MinValue:int.MinValue;
                long maximum=type==typeof(byte)?byte.MaxValue:type==typeof(sbyte)?sbyte.MaxValue:
                    type==typeof(short)?short.MaxValue:type==typeof(ushort)?ushort.MaxValue:type==typeof(uint)?uint.MaxValue:int.MaxValue;
                guard=Op("$and",guard,Op("$gte",value,Literal(minimum)),Op("$lte",value,Literal(maximum)));
            }
            return guard;
        }
        throw new NotSupportedException("MongoDB CQL path type is not admitted.");
    }
    private static BsonValue Metadata(IReadOnlyList<string> segments,Func<BsonValue,BsonValue> predicate,string prefix="cqlm")
    {
        if(segments.Any(segment=>segment.IndexOf('\0')>=0))throw new NotSupportedException("MongoDB metadata field names cannot contain NUL.");
        // Both dictionary representations are already part of this provider's read contract.
        BsonValue root=Choose(Op("$eq",Type("$MetaData"),Literal("array")),new BsonDocument("$arrayToObject","$MetaData"),"$MetaData");
        BsonValue Walk(BsonValue input,int index)
        {
            var name=prefix+index;BsonValue current="$$"+name;
            var body=index==segments.Count?predicate(current):Walk(new BsonDocument("$getField",new BsonDocument{
                {"field",Literal(segments[index])},{"input",Choose(Op("$eq",Type(current),Literal("object")),current,Literal(new BsonDocument()))}}),index+1);
            return new BsonDocument("$let",new BsonDocument{{"vars",new BsonDocument(name,input)},{"in",body}});
        }
        return Walk(root,0);
    }
    private static BsonValue MetadataDate(BsonValue value,IReadOnlyList<string> segments)
    {
        var pointer="/"+string.Join("/",segments.Select(segment=>segment.Replace("~","~0").Replace("/","~1")));
        var matches=new BsonDocument("$filter",new BsonDocument{
            {"input",Op("$ifNull","$MetaDataExactUtcTicks",Literal(new BsonArray()))},
            {"as","cqldateentry"},{"cond",Op("$eq","$$cqldateentry.Path",Literal(pointer))}});
        // Legacy records have only their actual millisecond projection. New records
        // carry the exact ticks in the existing storage-owned precision companion.
        var projected=Op("$add",Op("$multiply",new BsonDocument("$toLong",value),Literal(TimeSpan.TicksPerMillisecond)),
            Literal(DateTime.UnixEpoch.Ticks));
        var exact=new BsonDocument("$let",new BsonDocument{
            {"vars",new BsonDocument("cqldateentries",matches)},
            {"in",Choose(Op("$eq",new BsonDocument("$size","$$cqldateentries"),Literal(0)),projected,
                new BsonDocument("$let",new BsonDocument{{"vars",new BsonDocument("cqldatefirst",Op("$arrayElemAt","$$cqldateentries",Literal(0)))},{"in","$$cqldatefirst.Ticks"}}))}});
        return Choose(Op("$eq",Type(value),Literal("date")),exact,
            Choose(Op("$eq",Type(value),Literal("null")),Literal(BsonNull.Value),Literal(new BsonDocument())));
    }
    private static BsonValue At(CosmicQueryPath path,Func<BsonValue,BsonValue> predicate,string prefix="cqlm")
        =>path.IsMetadata?Metadata(path.Segments,value=>predicate((Nullable.GetUnderlyingType(path.ValueType)??path.ValueType)==typeof(DateTime)
            ?MetadataDate(value,path.Segments):value),prefix):predicate(Canonical(path));
    private static BsonValue ComparableGuid(BsonValue value)
    {
        var text = new BsonDocument("$replaceAll", new BsonDocument {
            {"input", new BsonDocument("$toLower", value)}, {"find", Literal("-")}, {"replacement", Literal("")} });
        // MongoDB emits uppercase hex; string UUIDs normalize to lowercase.
        var hex = new BsonDocument("$toLower", new BsonDocument("$convert", new BsonDocument {
            {"input", value}, {"to", "string"}, {"format", "hex"} }));
        var legacy = Op("$and",
            Op("$gte", value, Literal(new BsonBinaryData(new byte[16], BsonBinarySubType.UuidLegacy))),
            Op("$lte", value, Literal(new BsonBinaryData(Enumerable.Repeat((byte)255,16).ToArray(), BsonBinarySubType.UuidLegacy))));
        var reversed = Op("$concat", new[] {6,4,2,0,10,8,14,12}
            .Select(offset => (BsonValue)Op("$substrBytes", "$$cqlguidhex", Literal(offset), Literal(2)))
            .Concat(new[] {(BsonValue)Op("$substrBytes", "$$cqlguidhex", Literal(16), Literal(16))}).ToArray());
        var binary = new BsonDocument("$let", new BsonDocument {
            {"vars", new BsonDocument("cqlguidhex", hex)}, {"in", Choose(legacy, reversed, "$$cqlguidhex")} });
        return Choose(Op("$eq",Type(value),Literal("null")), Literal(BsonNull.Value),
            Choose(Op("$eq",Type(value),Literal("string")), text, binary));
    }
    private static BsonValue FieldDomain(BsonValue value,CosmicQueryPath path)
    {
        var valid = Domain(value,path.ValueType,path.IsMetadata);
        return path.ValueType==typeof(string) || Nullable.GetUnderlyingType(path.ValueType)!=null
            ? Op("$or",valid,Op("$eq",Type(value),Literal("null"))) : valid;
    }
    private static BsonValue FieldComparison(CosmicQueryFieldComparison fields)
        => At(fields.Left,left=>At(fields.Right,right=> {
            var operation=fields.Operator switch {
                CosmicQueryComparisonOperator.Equal=>"$eq", CosmicQueryComparisonOperator.NotEqual=>"$ne",
                CosmicQueryComparisonOperator.GreaterThan=>"$gt", CosmicQueryComparisonOperator.GreaterThanOrEqual=>"$gte",
                CosmicQueryComparisonOperator.LessThan=>"$lt", CosmicQueryComparisonOperator.LessThanOrEqual=>"$lte",
                _=>throw new NotSupportedException("MongoDB CQL field operator is unsupported.") };
            var valid=Op("$and",FieldDomain(left,fields.Left),FieldDomain(right,fields.Right));
            var guid=(Nullable.GetUnderlyingType(fields.Left.ValueType)??fields.Left.ValueType)==typeof(Guid);
            var comparison=Op(operation,guid?ComparableGuid(left):left,guid?ComparableGuid(right):right);
            if(fields.Operator!=CosmicQueryComparisonOperator.Equal && fields.Operator!=CosmicQueryComparisonOperator.NotEqual)
                comparison=Op("$and",Op("$ne",Type(left),Literal("null")),Op("$ne",Type(right),Literal("null")),comparison);
            return Choose(valid,comparison,Literal(false));
        },"cqlright"),"cqlleft");
    private BsonValue Translate(CosmicQueryPredicate predicate,int depth)
    {
        if(++nodes>2048||depth>64)throw new MongoCqlLimitException();
        switch(predicate)
        {
            case CosmicQueryFieldComparison fields:return FieldComparison(fields);
            case CosmicQueryBoolean boolean:return Literal(boolean.Value);
            case CosmicQueryLogical logical:return Op(logical.IsAnd?"$and":"$or",Translate(logical.Left,depth+1),Translate(logical.Right,depth+1));
            case CosmicQueryNot not:return Op("$not",Translate(not.Operand,depth+1));
            case CosmicQueryMetadataTest test:return Metadata(test.Segments,value=>test.Kind==CosmicQueryMetadataTestKind.IsNull
                ?Op("$eq",Type(value),Literal("null")):Op("$ne",Type(value),Literal("missing")));
            case CosmicQueryComparison comparison:return At(comparison.Path,value=>{
                if(comparison.Literal.Kind==CosmicQueryScalarKind.Null)
                    return comparison.Operator==CosmicQueryComparisonOperator.Equal?Op("$eq",Type(value),Literal("null"))
                        :Domain(value,comparison.Path.ValueType,comparison.Path.IsMetadata);
                var symbol=comparison.Operator switch{
                    CosmicQueryComparisonOperator.Equal=>"$eq",CosmicQueryComparisonOperator.NotEqual=>"$ne",
                    CosmicQueryComparisonOperator.GreaterThan=>"$gt",CosmicQueryComparisonOperator.GreaterThanOrEqual=>"$gte",
                    CosmicQueryComparisonOperator.LessThan=>"$lt",CosmicQueryComparisonOperator.LessThanOrEqual=>"$lte",
                    _=>throw new NotSupportedException("Unknown CQL comparison.")};
                var domain=Domain(value,comparison.Path.ValueType,comparison.Path.IsMetadata);
                BsonValue comparisonTest;
                if(GuidMetadata(comparison.Path))
                {
                    comparisonTest=Op("$in",GuidOperand(value),Literal(new BsonArray(GuidLiterals((Guid)comparison.Literal.Value))));
                    if(comparison.Operator==CosmicQueryComparisonOperator.NotEqual)comparisonTest=Op("$not",comparisonTest);
                }
                else comparisonTest=Op(symbol,value,Constant(comparison.Literal));
                var test=Choose(domain,comparisonTest,Literal(false));
                if(comparison.Operator!=CosmicQueryComparisonOperator.NotEqual)return test;
                if(!comparison.Path.IsMetadata)
                    return Op("$not",Choose(domain,Op("$eq",value,Constant(comparison.Literal)),Literal(false)));
                // Sparse metadata keeps missing/wrong-type paths outside the atomic domain,
                // while an explicit nullable null differs from every non-null literal.
                return comparison.Path.ValueType==typeof(string)||Nullable.GetUnderlyingType(comparison.Path.ValueType)!=null
                    ?Op("$or",test,Op("$eq",Type(value),Literal("null"))):test;
            });
            case CosmicQueryMembership membership:return At(membership.Path,value=>{
                var guard=Domain(value,membership.Path.ValueType,membership.Path.IsMetadata);
                if(membership.Values.Any(item=>item.Kind==CosmicQueryScalarKind.Null))guard=Op("$or",guard,Op("$eq",Type(value),Literal("null")));
                var guid=GuidMetadata(membership.Path);
                var values=new BsonArray(membership.Values.SelectMany(item=>guid&&item.Kind==CosmicQueryScalarKind.Guid
                    ?GuidLiterals((Guid)item.Value):new[]{((BsonDocument)Constant(item))["$literal"]}));
                return Choose(guard,Op("$in",guid?GuidOperand(value):value,Literal(values)),Literal(false));
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
