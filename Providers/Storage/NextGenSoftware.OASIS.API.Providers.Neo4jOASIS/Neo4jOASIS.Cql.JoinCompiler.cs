using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

internal sealed class NeoCqlJoinCommand
{
    internal string Text { get; }
    internal Dictionary<string,object> Parameters { get; }
    internal NeoCqlJoinCommand(string text,Dictionary<string,object> parameters)
    { Text=text;Parameters=parameters; }
}

public partial class Neo4jOASIS
{
    // Capture source values once inside a single native statement. Neo4j remains
    // read committed; this is not a repeatable-snapshot capability grant.
    internal static NeoCqlJoinCommand CompileCqlJoin<TLeft,TRight,TKey,TResult>(
        CosmicJoinSelection<TLeft,TRight,TKey,TResult> query,CosmicJoinExecutionLimits limits)
        where TLeft:IHolon where TRight:IHolon
    {
        if(query==null || limits==null || limits.Admit(query.Join.Options).IsError)
            throw new ArgumentException("A join selection and admitted remaining limits are required.");
        var parameters=new Dictionary<string,object>(StringComparer.Ordinal);
        var left=Source(query.Join.Left,true,limits.MaximumLeftRows,limits.MaximumLeftPayloadBytes);
        var right=Source(query.Join.Right,false,limits.MaximumRightRows,limits.MaximumRightPayloadBytes);
        var binder=new NeoCqlTranslator("h","p",parameters,null);
        var pairLimit=binder.Bind((query.OutputLimit.HasValue
            ? limits.MaximumPairs
            : Math.Min(limits.MaximumPairs,(long)limits.MaximumResults))+1);
        var leftCount=binder.Bind(limits.MaximumLeftRows);var rightCount=binder.Bind(limits.MaximumRightRows);
        var leftBytes=binder.Bind(limits.MaximumLeftPayloadBytes);var rightBytes=binder.Bind(limits.MaximumRightPayloadBytes);
        var outer = binder.Bind(query.Join.Kind is CosmicJoinKind.Left or CosmicJoinKind.Full);
        var equality=string.Join(" AND ",query.Join.Keys.Select((key,index) =>
        {
            var comparison = key.Comparison switch
            {
                CosmicJoinComparison.Equal => "=",
                CosmicJoinComparison.NotEqual => "<>",
                CosmicJoinComparison.LessThan => "<",
                CosmicJoinComparison.LessThanOrEqual => "<=",
                CosmicJoinComparison.GreaterThan => ">",
                CosmicJoinComparison.GreaterThanOrEqual => ">=",
                _ => throw new NotSupportedException("Unknown native join comparison.")
            };
            return "l.keys["+index+"].s=2 AND r.keys["+index+"].s=2 AND l.keys["+index+"].v"
                + comparison + "r.keys["+index+"].v";
        }));
        var valid="size(leftInput)<="+leftCount+" AND size(rightInput)<="+rightCount
            +" AND all(x IN leftInput+rightInput WHERE x.valid AND x.domainValid AND NOT(x.cycle OR x.traversalLimit OR x.payloadLimit))"
            +" AND reduce(total=0,x IN leftInput | total+x.bytes)<="+leftBytes
            +" AND reduce(total=0,x IN rightInput | total+x.bytes)<="+rightBytes;
        var leftPairs = "WITH leftInput,rightInput UNWIND range(0,size(leftInput)-1) AS li "
            + "WITH li,leftInput[li] AS l,rightInput CALL { WITH l,rightInput "
            + "UNWIND range(0,size(rightInput)-1) AS ri WITH ri,rightInput[ri] AS r,l"
            + (query.Join.Kind == CosmicJoinKind.Cross ? "" : " WHERE " + equality)
            + " WITH ri ORDER BY ri ASC RETURN collect(ri) AS matches } "
            + "UNWIND CASE WHEN size(matches)=0 AND " + outer + " THEN [-1] ELSE matches END AS ri "
            + "RETURN li,ri ORDER BY li ASC,ri ASC LIMIT " + pairLimit;
        var pairRows = leftPairs;
        if (query.Join.Kind == CosmicJoinKind.Full)
        {
            // A left position just past the captured list separates unmatched right rows from real left rows.
            pairRows = "WITH leftInput,rightInput CALL { " + leftPairs
                + " UNION ALL WITH leftInput,rightInput UNWIND range(0,size(rightInput)-1) AS ri "
                + "WITH leftInput,rightInput[ri] AS r,ri WHERE none(l IN leftInput WHERE " + equality + ") "
                + "RETURN size(leftInput) AS li,ri ORDER BY ri ASC LIMIT " + pairLimit
                + " } RETURN li,ri ORDER BY li ASC,ri ASC LIMIT " + pairLimit;
        }
        var navigation=query.Join.RestrictsRightToLeftParents;
        var import=navigation ? "WITH leftInput " : "";
        var leftProof=navigation
            ? "WITH leftInput,li,leftInput[li] AS entry RETURN 0 AS kind,li AS lp,-1 AS rp,"
                +"entry{.id,.visible,.creator,.valid,.domainValid,.cycle,.traversalLimit,.payloadLimit,.bytes,.keys,.values,.ordering}"
                +" AS proof,size(leftInput) AS nativeCount "
            : "RETURN 0 AS kind,li AS lp,-1 AS rp,leftInput[li] AS proof,size(leftInput) AS nativeCount ";
        var output = "RETURN kind,lp,rp,proof,nativeCount ORDER BY kind ASC,lp ASC,rp ASC";
        var values = new List<string>();
        var orderTerms = new List<string>();
        if (query.OutputOrdering.Count != 0)
        {
            foreach (var order in query.OutputOrdering)
            {
                var column = query.Columns[order.Column];
                var index = query.Columns.Take(order.Column).Count(item => item.Source == column.Source);
                var position = column.Source == CosmicJoinColumnSource.Left ? "lp" : "rp";
                var input = column.Source == CosmicJoinColumnSource.Left ? "leftInput" : "rightInput";
                var cell = input + "[" + position + "].values[" + index + "]";
                var direction = order.Descending ? " DESC" : " ASC";
                var alias = "outputValue" + values.Count;
                values.Add("CASE WHEN kind=2 AND " + position + ">=0 AND " + position
                    + "<size(" + input + ") AND " + cell + ".s=2 THEN " + cell + ".v END AS " + alias);
                orderTerms.Add("CASE WHEN " + alias + " IS NULL THEN 0 ELSE 1 END" + direction);
                orderTerms.Add(alias + direction);
            }
            output = "WITH kind,lp,rp,proof,nativeCount," + string.Join(",", values)
                + " RETURN kind,lp,rp,proof,nativeCount ORDER BY kind ASC," + string.Join(",", orderTerms) + ",lp ASC,rp ASC";
        }
        var rightProof = query.SelectsChildEntity
            ? "WITH rightInput,ri,rightInput[ri] AS entry RETURN 1 AS kind,-1 AS lp,ri AS rp,"
                + "entry{.id,.visible,.creator,.valid,.domainValid,.cycle,.traversalLimit,.payloadLimit,.bytes,.keys,.values,.ordering}"
                + " AS proof,size(rightInput) AS nativeCount "
            : "RETURN 1 AS kind,-1 AS lp,ri AS rp,rightInput[ri] AS proof,size(rightInput) AS nativeCount ";
        if (query.SelectsChildEntity)
        {
            var entityLimit = binder.Bind(limits.MaximumPayloadBytes);
            var entityOrder = values.Count == 0 ? "" : "," + string.Join(",", values);
            output = "WITH kind,lp,rp,proof,nativeCount,CASE WHEN kind=2 THEN rightInput[rp] END AS selected"
                + entityOrder + " "
                + "OPTIONAL MATCH (child:Holon) WHERE elementId(child)=selected.entityLocator "
                + "AND coalesce(child.ProviderRevisionId,'')=selected.entityRevision "
                + "RETURN kind,lp,rp,proof,nativeCount,CASE WHEN child IS NOT NULL THEN "
                + "{id:child.Id,revision:child.ProviderRevisionId,snapshot:CASE WHEN size(child.SnapshotJson)<="
                + entityLimit + " THEN child.SnapshotJson END} END AS entity ORDER BY kind ASC,"
                + string.Join(",", orderTerms.Concat(new[] { "lp ASC", "rp ASC" }));
        }
        var pairProof = "CALL { " + pairRows + " } "
            + "WITH collect({l:li,r:ri}) AS pairs UNWIND pairs AS pair "
            + "RETURN 2 AS kind,pair.l AS lp,pair.r AS rp,null AS proof,size(pairs) AS nativeCount ";
        if (query.OutputLimit is int requestedLimit)
        {
            // Count stays available when the requested window is empty.
            var pagingValues = values.Select(value => value.Replace("kind=2 AND ", "")).ToArray();
            var offset = binder.Bind(query.OutputOffset);
            var end = binder.Bind((long)query.OutputOffset + requestedLimit);
            pairProof = "CALL { WITH leftInput,rightInput CALL { " + pairRows + " } "
                + "WITH leftInput,rightInput,li AS lp,ri AS rp "
                + (pagingValues.Length == 0 ? "" : "WITH lp,rp," + string.Join(",", pagingValues) + " ")
                + "ORDER BY " + string.Join(",", orderTerms.Concat(new[] { "lp ASC", "rp ASC" }))
                + " RETURN collect({kind:2,l:lp,r:rp}) AS pairs } "
                + "UNWIND pairs[" + offset + ".." + end + "]+[{kind:3,l:-1,r:-1}] AS pair "
                + "RETURN pair.kind AS kind,pair.l AS lp,pair.r AS rp,null AS proof,size(pairs) AS nativeCount ";
        }
        var text="CALL { CALL { "+left+" } RETURN collect(input) AS leftInput } "
            +"CALL { "+import+"CALL { "+import+right+" } RETURN collect(input) AS rightInput } WITH leftInput,rightInput "
            +"CALL { WITH leftInput,rightInput UNWIND range(0,size(leftInput)-1) AS li "+leftProof
            +"UNION ALL WITH leftInput,rightInput UNWIND range(0,size(rightInput)-1) AS ri "
            +rightProof
            +"UNION ALL WITH leftInput,rightInput WITH leftInput,rightInput WHERE "+valid+" "
            + pairProof + "} "+output;
        if(text.Length>1024*1024)throw new NeoCqlLimitException("Native join exceeds its statement-size budget.");
        return new NeoCqlJoinCommand(text,parameters);

        string Source<T>(CosmicQuery<T> source,bool isLeft,int maximumRows,int maximumBytes) where T:IHolon
            => CompileCqlDetachedSource(source, parameters,
                query.Join.Keys.Select(key => isLeft ? key.Left : key.Right).ToArray(),
                query.Columns.Where(column => column.Source == (isLeft ? CosmicJoinColumnSource.Left : CosmicJoinColumnSource.Right))
                    .Select(column => column.Path).ToArray(), maximumRows, maximumBytes,
                scopeChildren: !isLeft && query.Join.RestrictsRightToLeftParents,
                includeChildMembership: isLeft && query.Join.RestrictsRightToLeftParents,
                includeEntityLocator: !isLeft && query.SelectsChildEntity);
    }
}
