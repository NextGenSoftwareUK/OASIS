using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS;

/// <summary>Bound native identity/content selection; no general EF parity claim.</summary>
public sealed class OracleCqlStatement
{
    public string Sql { get; }
    public ReadOnlyCollection<Guid> Values { get; }
    public ReadOnlyCollection<string> TextValues { get; }
    internal OracleCqlStatement(string predicate,Guid[] values,string[] textValues)
    {
        Sql="SELECT ID,DATA_JSON,HOLON_TYPE,IS_DELETED,PARENT_HOLON_ID FROM OASIS_HOLONS WHERE IS_DELETED=0 AND ("+
            predicate+") AND ROWNUM<=129 ORDER BY ID";
        Values=Array.AsReadOnly((Guid[])values.Clone());
        TextValues=Array.AsReadOnly((string[])textValues.Clone());
    }
}

public static class OracleCqlCompiler
{
    public static CosmicQueryCapabilities Capabilities { get; }=new(
        CosmicQueryFeatures.BooleanConstant|CosmicQueryFeatures.Membership|CosmicQueryFeatures.And|CosmicQueryFeatures.Or|CosmicQueryFeatures.Paging|
        CosmicQueryFeatures.Metadata|CosmicQueryFeatures.StringContains,
        new[]{
            new CosmicQueryFieldCapability("Id",typeof(Guid),new[]{CosmicQueryComparisonOperator.Equal},Array.Empty<CosmicQueryStringOperator>(),false),
            new CosmicQueryFieldCapability("Name",typeof(string),new[]{CosmicQueryComparisonOperator.Equal},new[]{CosmicQueryStringOperator.Contains}),
            new CosmicQueryFieldCapability("Description",typeof(string),new[]{CosmicQueryComparisonOperator.Equal},new[]{CosmicQueryStringOperator.Contains})
        },new[]{new CosmicQueryMetadataCapability(typeof(string),new[]{CosmicQueryComparisonOperator.Equal})});

    public static OASISResult<OracleCqlStatement> Compile<T>(CosmicQuery<T> query) where T:IHolon
    {
        try
        {
            if(query==null)return Error("CQL_PLAN_INVALID","A captured query is required.");
            var admitted=Capabilities.Admit(query);
            if(admitted.IsError)return Error(admitted.ErrorCode,admitted.Message);
            if(query.ReadAccess?.Authority!=ProviderType.OracleDBOASIS)
                return Error("CQL_ACCESS_INVALID","This Oracle profile requires a complete server-authorized Oracle record catalogue.");
            var values=new List<Guid>();
            var texts=new List<string>();
            string Bind(CosmicQueryLiteral literal)
            {
                if(literal.Kind!=CosmicQueryScalarKind.Guid||literal.Value is not Guid value)
                    throw new ArgumentException("Oracle RAW identity predicates require GUID literals.");
                if(values.Count+texts.Count>=512)throw new ArgumentException("The bounded query exceeds512 native identity parameters.");
                var name=":cql"+values.Count;values.Add(value);return name;
            }
            string TextPredicate(CosmicQueryPath path,string value,bool contains)
            {
                if(value.Contains('\0')||Encoding.UTF8.GetByteCount(value)>4000||values.Count+texts.Count>=512)
                    throw new ArgumentException("Oracle content values require NUL-free strings of at most4000UTF8bytes within512parameters.");
                var segments=path.IsMetadata?new[]{"MetaData"}.Concat(path.Segments).ToArray():path.Segments.ToArray();
                if(segments.Any(segment=>segment.Contains('\0')))
                    throw new ArgumentException("Oracle metadata keys cannot contain NUL.");
                // Oracle requires a literal JSON path. Encode each member as JSON,
                // then escape the SQL string literal; values remain PASSING binds.
                var current="@";var guards=new List<string>{"@.type() == \"object\""};
                for(var index=0;index<segments.Length;index++)
                {
                    current+="."+Newtonsoft.Json.JsonConvert.SerializeObject(segments[index]);
                    if(index+1<segments.Length)guards.Add(current+".type() == \"object\"");
                }
                guards.Add(current+".type() == \"string\"");
                guards.Add(current+(contains?" has substring $v":" == $v"));
                var filter="$?("+string.Join(" && ",guards)+")";
                if(Encoding.UTF8.GetByteCount(filter)>4000)
                    throw new ArgumentException("The encoded Oracle JSON path exceeds4000UTF8bytes.");
                var parameter=":cqltext"+texts.Count;texts.Add(value);
                return "JSON_EXISTS(DATA_JSON, '"+filter.Replace("'","''")+"' PASSING "+parameter+" AS \"v\" ERROR ON ERROR)";
            }
            string Predicate(CosmicQueryPredicate predicate)=>predicate switch
            {
                CosmicQueryBoolean boolean=>boolean.Value?"1=1":"1=0",
                CosmicQueryLogical logical=>"("+Predicate(logical.Left)+(logical.IsAnd?" AND ":" OR ")+Predicate(logical.Right)+")",
                CosmicQueryComparison comparison when comparison.Path.ValueType==typeof(string)=>TextPredicate(comparison.Path,(string)comparison.Literal.Value,false),
                CosmicQueryStringMatch match=>TextPredicate(match.Path,match.Value,true),
                CosmicQueryComparison comparison=>"ID="+Bind(comparison.Literal),
                CosmicQueryMembership membership=>membership.Values.Count==0?"1=0":"ID IN ("+string.Join(",",membership.Values.Select(Bind))+")",
                _=>throw new ArgumentException("Only admitted canonical identity predicates are supported.")
            };
            var sql=string.Join(" AND ",query.Predicates.Select(Predicate).Select(part=>"("+part+")"));
            return new(new OracleCqlStatement(sql,values.ToArray(),texts.ToArray()));
        }
        catch(ArgumentException error){return Error("CQL_CAPABILITY_UNSUPPORTED",error.Message);}
    }
    private static OASISResult<OracleCqlStatement> Error(string code,string message)
        =>new(){IsError=true,ErrorCount=1,ErrorCode=code,Message=message};
}
