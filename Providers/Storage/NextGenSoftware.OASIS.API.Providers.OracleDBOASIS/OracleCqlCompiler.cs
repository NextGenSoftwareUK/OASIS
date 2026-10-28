using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS;

/// <summary>Bound native primary-key selection; no JSON/content-query or general EF parity claim.</summary>
public sealed class OracleCqlStatement
{
    public string Sql { get; }
    public ReadOnlyCollection<Guid> Values { get; }
    internal OracleCqlStatement(string predicate,Guid[] values)
    {
        Sql="SELECT ID,DATA_JSON,HOLON_TYPE,IS_DELETED,PARENT_HOLON_ID FROM OASIS_HOLONS WHERE IS_DELETED=0 AND ("+
            predicate+") AND ROWNUM<=129 ORDER BY ID";
        Values=Array.AsReadOnly((Guid[])values.Clone());
    }
}

public static class OracleCqlCompiler
{
    public static CosmicQueryCapabilities Capabilities { get; }=new(
        CosmicQueryFeatures.BooleanConstant|CosmicQueryFeatures.Membership|CosmicQueryFeatures.And|CosmicQueryFeatures.Or|CosmicQueryFeatures.Paging,
        new[]{new CosmicQueryFieldCapability("Id",typeof(Guid),new[]{CosmicQueryComparisonOperator.Equal},
            Array.Empty<CosmicQueryStringOperator>(),false)});

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
            string Bind(CosmicQueryLiteral literal)
            {
                if(literal.Kind!=CosmicQueryScalarKind.Guid||literal.Value is not Guid value)
                    throw new ArgumentException("Oracle RAW identity predicates require GUID literals.");
                if(values.Count>=512)throw new ArgumentException("The bounded query exceeds512 native identity parameters.");
                var name=":cql"+values.Count;values.Add(value);return name;
            }
            string Predicate(CosmicQueryPredicate predicate)=>predicate switch
            {
                CosmicQueryBoolean boolean=>boolean.Value?"1=1":"1=0",
                CosmicQueryLogical logical=>"("+Predicate(logical.Left)+(logical.IsAnd?" AND ":" OR ")+Predicate(logical.Right)+")",
                CosmicQueryComparison comparison=>"ID="+Bind(comparison.Literal),
                CosmicQueryMembership membership=>membership.Values.Count==0?"1=0":"ID IN ("+string.Join(",",membership.Values.Select(Bind))+")",
                _=>throw new ArgumentException("Only admitted canonical identity predicates are supported.")
            };
            var sql=string.Join(" AND ",query.Predicates.Select(Predicate).Select(part=>"("+part+")"));
            return new(new OracleCqlStatement(sql,values.ToArray()));
        }
        catch(ArgumentException error){return Error("CQL_CAPABILITY_UNSUPPORTED",error.Message);}
    }
    private static OASISResult<OracleCqlStatement> Error(string code,string message)
        =>new(){IsError=true,ErrorCount=1,ErrorCode=code,Message=message};
}
