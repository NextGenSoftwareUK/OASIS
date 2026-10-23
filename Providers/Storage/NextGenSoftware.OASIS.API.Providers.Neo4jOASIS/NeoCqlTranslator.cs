using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

internal sealed partial class NeoCqlTranslator
{
    internal readonly Dictionary<string, object> Parameters;
    private readonly string _node;
    private readonly string _projection;
    private readonly CosmicQueryReadAccess _access;
    internal NeoCqlTranslator(CosmicQueryReadAccess access=null):this("h","p",new Dictionary<string,object>(StringComparer.Ordinal),access){}
    private NeoCqlTranslator(string node,string projection,Dictionary<string,object> parameters,CosmicQueryReadAccess access)
    {_node=node;_projection=projection;Parameters=parameters;_access=access;}
    private string Bind(object value)
    {
        if (Parameters.Count >= 900) throw new NotSupportedException("Neo4j CQL exceeds its 900-parameter budget.");
        var name = "cql" + Parameters.Count; Parameters.Add(name, value); return "$" + name;
    }
    private static bool NullablePath(CosmicQueryPath path)
        => !path.ValueType.IsValueType || Nullable.GetUnderlyingType(path.ValueType) != null;
    private (string Value, string Guard, string Null) Operand(CosmicQueryPath path)
    {
        if (!path.IsMetadata)
        {
            var name = path.Segments.Single();
            if (!new[] { "Id", "Name", "Description", "Version", "HolonType", "IsActive", "IsPublic", "ParentHolonId", "VersionId",
                "PreviousVersionId", "CreatedByAvatarId", "ModifiedByAvatarId" }.Contains(name, StringComparer.Ordinal))
                throw new NotSupportedException("Neo4j CQL canonical path is unsupported.");
            var column = _projection+"[" + Bind("f_" + name) + "]";
            return (column, "true", column + " IS NULL");
        }
        var key = NeoCqlProjection.PathKey(path.Segments);
        var kind = _projection+"[" + Bind(key + "_kind") + "]";
        var value = _projection+"[" + Bind(key + "_value") + "]";
        var type = Nullable.GetUnderlyingType(path.ValueType) ?? path.ValueType;
        string guard;
        if (type == typeof(string)) guard = kind + "='string'";
        else if (type == typeof(bool)) guard = kind + "='boolean'";
        else if (type == typeof(Guid))
        {
            var text="CASE WHEN "+kind+"='string' THEN "+value+" ELSE null END";
            guard=kind+"='string' AND "+text+" =~ "+Bind(@"\A[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\z");
            value="toLower("+text+")";
        }
        else if (type == typeof(HolonType) || type == typeof(ProviderType))
        {
            var defined=Enum.GetValues(type).Cast<object>().Select(item=>Convert.ToInt64(item,CultureInfo.InvariantCulture)).Distinct().ToArray();
            guard=kind+"='integer' AND "+value+" IN "+Bind(defined);
        }
        else if (NeoCqlIntegerDomain.TryBounds(type,out var bounds))
        {
            guard = kind + "='integer'";
            if (type != typeof(long)) guard += " AND " + value + ">="+bounds.Minimum.ToString(CultureInfo.InvariantCulture)+
                " AND " + value + "<="+bounds.Maximum.ToString(CultureInfo.InvariantCulture);
        }
        else throw new NotSupportedException("Neo4j CQL metadata scalar domain is unsupported.");
        return (value, "(" + guard + ")", kind + "='null'");
    }
    private static string Symbol(CosmicQueryComparisonOperator operation) => operation switch {
        CosmicQueryComparisonOperator.Equal => "=", CosmicQueryComparisonOperator.NotEqual => "<>",
        CosmicQueryComparisonOperator.GreaterThan => ">", CosmicQueryComparisonOperator.GreaterThanOrEqual => ">=",
        CosmicQueryComparisonOperator.LessThan => "<", CosmicQueryComparisonOperator.LessThanOrEqual => "<=",
        _ => throw new NotSupportedException("Unknown Neo4j CQL comparison.") };
    private static object Literal(CosmicQueryLiteral literal) => literal.Kind switch {
        CosmicQueryScalarKind.String or CosmicQueryScalarKind.Boolean or CosmicQueryScalarKind.Int64 => literal.Value,
        CosmicQueryScalarKind.Guid => ((Guid)literal.Value).ToString("D"),
        _ => throw new NotSupportedException("Neo4j CQL literal domain is unsupported.") };
    private static string Total(string predicate) => "coalesce((" + predicate + "),false)";

    internal IReadOnlyList<(string Expression,bool Descending)> TranslateOrdering(IReadOnlyList<CosmicQueryOrdering> orderings)
    {
        var keys=new List<(string,bool)>();
        foreach(var ordering in orderings)
        {
            var operand=Operand(ordering.Path);
            var rank=ordering.Path.IsMetadata
                ? "CASE WHEN "+_projection+"["+Bind(NeoCqlProjection.PathKey(ordering.Path.Segments)+"_kind")+"] IS NULL THEN 0 WHEN "+operand.Null+
                    " THEN 1 WHEN "+operand.Guard+" THEN 2 ELSE 3 END"
                : "CASE WHEN "+operand.Null+" THEN 1 ELSE 2 END";
            keys.Add((rank,ordering.Descending));
            keys.Add(("CASE WHEN "+operand.Guard+" THEN "+operand.Value+" ELSE null END",ordering.Descending));
        }
        return keys;
    }

    internal string Translate(CosmicQueryPredicate predicate)
    {
        switch (predicate)
        {
            case CosmicQueryAnyDescendant descendant:return TranslateDescendant(descendant);
            case CosmicQueryAnyChild child:
            {
                var suffix=Parameters.Count.ToString(CultureInfo.InvariantCulture);
                var node="rh"+suffix;var projection="rp"+suffix;
                var inner=new NeoCqlTranslator(node,projection,Parameters,_access);
                var visibility="true";
                if(_access!=null)
                {
                    var secured=CosmicQuery<Core.Holons.Holon>.Create().WithReadAccess(_access);
                    if(secured.IsError)throw new NotSupportedException(secured.Message);
                    visibility=inner.Translate(secured.Result.Predicates.Single());
                }
                var match=inner.Translate(child.Predicate);
                return "EXISTS { MATCH ("+_node+")-[:HAS_CHILD]->("+node+":Holon)-["+projection+":HAS_CQL_PROJECTION]->("+node+") "
                    +"WHERE "+projection+".SchemaVersion="+NeoCqlProjection.SchemaVersion.ToString(CultureInfo.InvariantCulture)
                    +" AND "+projection+".f_Id=toLower("+node+".Id) AND "+projection+".f_Id<>"+_projection+".f_Id"
                    +" AND "+projection+".f_ParentHolonId="+_projection+".f_Id"
                    +" AND coalesce("+projection+".ProviderRevisionId,'')=coalesce("+node+".ProviderRevisionId,'')"
                    +" AND (NOT "+_projection+".f_HasChildManifest OR "+projection+".f_Id IN "+_projection+".f_ChildIds)"
                    +" AND ("+visibility+") AND ("+match+") }";
            }
            case CosmicQueryBoolean constant: return constant.Value ? "true" : "false";
            case CosmicQueryLogical logical:
                return "(" + Translate(logical.Left) + (logical.IsAnd ? " AND " : " OR ") + Translate(logical.Right) + ")";
            case CosmicQueryNot not: return "(NOT " + Translate(not.Operand) + ")";
            case CosmicQueryFieldComparison fields:
                var left = Operand(fields.Left); var right = Operand(fields.Right);
                var leftValid = NullablePath(fields.Left) ? "(" + left.Guard + " OR " + left.Null + ")" : left.Guard;
                var rightValid = NullablePath(fields.Right) ? "(" + right.Guard + " OR " + right.Null + ")" : right.Guard;
                string fieldTest;
                if (fields.Operator == CosmicQueryComparisonOperator.Equal || fields.Operator == CosmicQueryComparisonOperator.NotEqual)
                {
                    var equal = "(" + Total(left.Null + " AND " + right.Null) + " OR "
                        + Total(left.Value + "=" + right.Value) + ")";
                    fieldTest = fields.Operator == CosmicQueryComparisonOperator.Equal ? equal : "NOT " + equal;
                }
                else fieldTest = Total(left.Value + Symbol(fields.Operator) + right.Value);
                return Total(leftValid + " AND " + rightValid + " AND (" + fieldTest + ")");
            case CosmicQueryMetadataTest test:
                var kind = _projection+"[" + Bind(NeoCqlProjection.PathKey(test.Segments) + "_kind") + "]";
                return test.Kind == CosmicQueryMetadataTestKind.Exists ? "(" + kind + " IS NOT NULL)" : Total(kind + "='null'");
            case CosmicQueryComparison comparison:
                var operand = Operand(comparison.Path);
                if (comparison.Literal.Kind == CosmicQueryScalarKind.Null)
                {
                    if (!NullablePath(comparison.Path)) throw new NotSupportedException("Nonnullable CQL path cannot compare with null.");
                    return comparison.Operator switch {
                        CosmicQueryComparisonOperator.Equal => Total(operand.Null),
                        CosmicQueryComparisonOperator.NotEqual => Total(operand.Guard + " AND NOT (" + operand.Null + ")"),
                        _ => throw new NotSupportedException("CQL null ordering is unsupported.") };
                }
                var compared = operand.Guard + " AND " + operand.Value + Symbol(comparison.Operator) + Bind(Literal(comparison.Literal));
                if (comparison.Operator == CosmicQueryComparisonOperator.NotEqual && NullablePath(comparison.Path))
                    compared = "(" + compared + ") OR (" + operand.Null + ")";
                return Total(compared);
            case CosmicQueryStringMatch match:
                var text = Operand(match.Path);
                var operation = match.Operator switch {
                    CosmicQueryStringOperator.Contains => " CONTAINS ", CosmicQueryStringOperator.StartsWith => " STARTS WITH ",
                    CosmicQueryStringOperator.EndsWith => " ENDS WITH ", _ => throw new NotSupportedException("Unknown CQL string operation.") };
                return Total(text.Guard + " AND " + text.Value + operation + Bind(match.Value));
            case CosmicQueryMembership membership:
                var member = Operand(membership.Path);
                var values = membership.Values.Where(value => value.Kind != CosmicQueryScalarKind.Null).Select(Literal).ToArray();
                var included = member.Guard + " AND " + member.Value + " IN " + Bind(values);
                if (NullablePath(membership.Path) && membership.Values.Any(value => value.Kind == CosmicQueryScalarKind.Null))
                    included = "(" + included + ") OR (" + member.Null + ")";
                return Total(included);
            default: throw new NotSupportedException("Neo4j CQL predicate has no admitted native translation.");
        }
    }
}
