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
        var pairLimit=binder.Bind(Math.Min(limits.MaximumPairs,(long)limits.MaximumResults)+1);
        var leftCount=binder.Bind(limits.MaximumLeftRows);var rightCount=binder.Bind(limits.MaximumRightRows);
        var leftBytes=binder.Bind(limits.MaximumLeftPayloadBytes);var rightBytes=binder.Bind(limits.MaximumRightPayloadBytes);
        var outer = binder.Bind(query.Join.Kind is CosmicJoinKind.Left or CosmicJoinKind.Full);
        var equality=string.Join(" AND ",query.Join.Keys.Select((_,index)=>
            "l.keys["+index+"].s=2 AND r.keys["+index+"].s=2 AND l.keys["+index+"].v=r.keys["+index+"].v"));
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
        if (query.OutputOrderColumn is int order)
        {
            var column = query.Columns[order];
            var index = query.Columns.Take(order).Count(item => item.Source == column.Source);
            var position = column.Source == CosmicJoinColumnSource.Left ? "lp" : "rp";
            var input = column.Source == CosmicJoinColumnSource.Left ? "leftInput" : "rightInput";
            var cell = input + "[" + position + "].values[" + index + "]";
            var direction = query.OutputOrderDescending ? " DESC" : " ASC";
            output = "WITH kind,lp,rp,proof,nativeCount,CASE WHEN kind=2 AND " + position + ">=0 AND " + position
                + "<size(" + input + ") AND " + cell + ".s=2 THEN " + cell + ".v END AS outputValue "
                + "RETURN kind,lp,rp,proof,nativeCount ORDER BY kind ASC,"
                + "CASE WHEN outputValue IS NULL THEN 0 ELSE 1 END" + direction + ",outputValue" + direction
                + ",lp ASC,rp ASC";
        }
        var text="CALL { CALL { "+left+" } RETURN collect(input) AS leftInput } "
            +"CALL { "+import+"CALL { "+import+right+" } RETURN collect(input) AS rightInput } WITH leftInput,rightInput "
            +"CALL { WITH leftInput,rightInput UNWIND range(0,size(leftInput)-1) AS li "+leftProof
            +"UNION ALL WITH leftInput,rightInput UNWIND range(0,size(rightInput)-1) AS ri "
            +"RETURN 1 AS kind,-1 AS lp,ri AS rp,rightInput[ri] AS proof,size(rightInput) AS nativeCount "
            +"UNION ALL WITH leftInput,rightInput WITH leftInput,rightInput WHERE "+valid+" "
            +"CALL { "+pairRows+" } "
            +"WITH collect({l:li,r:ri}) AS pairs UNWIND pairs AS pair "
            +"RETURN 2 AS kind,pair.l AS lp,pair.r AS rp,null AS proof,size(pairs) AS nativeCount "
            +"} "+output;
        if(text.Length>1024*1024)throw new NeoCqlLimitException("Native join exceeds its statement-size budget.");
        return new NeoCqlJoinCommand(text,parameters);

        string Source<T>(CosmicQuery<T> source,bool isLeft,int maximumRows,int maximumBytes) where T:IHolon
            => CompileCqlDetachedSource(source, parameters,
                query.Join.Keys.Select(key => isLeft ? key.Left : key.Right).ToArray(),
                query.Columns.Where(column => column.Source == (isLeft ? CosmicJoinColumnSource.Left : CosmicJoinColumnSource.Right))
                    .Select(column => column.Path).ToArray(), maximumRows, maximumBytes,
                scopeChildren: !isLeft && query.Join.RestrictsRightToLeftParents,
                includeChildMembership: isLeft && query.Join.RestrictsRightToLeftParents);
    }
}
