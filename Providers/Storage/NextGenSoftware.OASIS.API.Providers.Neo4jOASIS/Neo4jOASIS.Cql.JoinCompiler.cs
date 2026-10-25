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
        var outer=binder.Bind(query.Join.Kind==CosmicJoinKind.Left);
        var equality=string.Join(" AND ",query.Join.Keys.Select((_,index)=>
            "l.keys["+index+"].s=2 AND r.keys["+index+"].s=2 AND l.keys["+index+"].v=r.keys["+index+"].v"));
        var valid="size(leftInput)<="+leftCount+" AND size(rightInput)<="+rightCount
            +" AND all(x IN leftInput+rightInput WHERE x.valid AND x.domainValid AND NOT(x.cycle OR x.traversalLimit OR x.payloadLimit))"
            +" AND reduce(total=0,x IN leftInput | total+x.bytes)<="+leftBytes
            +" AND reduce(total=0,x IN rightInput | total+x.bytes)<="+rightBytes;
        var text="CALL { CALL { "+left+" } RETURN collect(input) AS leftInput } "
            +"CALL { CALL { "+right+" } RETURN collect(input) AS rightInput } WITH leftInput,rightInput "
            +"CALL { WITH leftInput,rightInput UNWIND range(0,size(leftInput)-1) AS li "
            +"RETURN 0 AS kind,li AS lp,-1 AS rp,leftInput[li] AS proof,size(leftInput) AS nativeCount "
            +"UNION ALL WITH leftInput,rightInput UNWIND range(0,size(rightInput)-1) AS ri "
            +"RETURN 1 AS kind,-1 AS lp,ri AS rp,rightInput[ri] AS proof,size(rightInput) AS nativeCount "
            +"UNION ALL WITH leftInput,rightInput WITH leftInput,rightInput WHERE "+valid+" "
            +"CALL { WITH leftInput,rightInput UNWIND range(0,size(leftInput)-1) AS li "
            +"WITH li,leftInput[li] AS l,rightInput CALL { WITH l,rightInput "
            +"UNWIND range(0,size(rightInput)-1) AS ri WITH ri,rightInput[ri] AS r,l WHERE "+equality
            +" WITH ri ORDER BY ri ASC RETURN collect(ri) AS matches } "
            +"UNWIND CASE WHEN size(matches)=0 AND "+outer+" THEN [-1] ELSE matches END AS ri "
            +"RETURN li,ri ORDER BY li ASC,ri ASC LIMIT "+pairLimit+" } "
            +"WITH collect({l:li,r:ri}) AS pairs UNWIND pairs AS pair "
            +"RETURN 2 AS kind,pair.l AS lp,pair.r AS rp,null AS proof,size(pairs) AS nativeCount "
            +"} RETURN kind,lp,rp,proof,nativeCount ORDER BY kind ASC,lp ASC,rp ASC";
        if(text.Length>1024*1024)throw new NeoCqlLimitException("Native join exceeds its statement-size budget.");
        return new NeoCqlJoinCommand(text,parameters);

        string Source<T>(CosmicQuery<T> source,bool isLeft,int maximumRows,int maximumBytes) where T:IHolon
            => CompileCqlDetachedSource(source, parameters,
                query.Join.Keys.Select(key => isLeft ? key.Left : key.Right).ToArray(),
                query.Columns.Where(column => column.Source == (isLeft ? CosmicJoinColumnSource.Left : CosmicJoinColumnSource.Right))
                    .Select(column => column.Path).ToArray(), maximumRows, maximumBytes);
    }
}
