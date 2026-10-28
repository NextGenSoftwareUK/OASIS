using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public partial class Neo4jOASIS
{
    // One detached source owner for joins and selected-result composition.
    private static string CompileCqlDetachedSource<T>(CosmicQuery<T> source,
        Dictionary<string, object> parameters, IReadOnlyList<CosmicQueryPath> keyPaths,
        IReadOnlyList<CosmicQueryPath> valuePaths, int maximumRows, int maximumBytes, CosmicEntityCompositionPlan<T> entity=null) where T : IHolon
        {
            var compiled=CompileCqlSource(source,parameters);var translator=compiled.Translator;
            var cells=new List<(string State,string Value)>();var invalid=new List<string>();
            var keys=new List<string>();var values=new List<string>();var order=new List<string>();
            foreach(var path in keyPaths) keys.Add(Cell(path,false));
            foreach(var path in valuePaths) values.Add(Cell(path,false));
            foreach(var item in source.Orderings)order.Add(Cell(item.Path,true));
            var sorts=translator.TranslateOrdering(source.Orderings);
            var aliases=sorts.Select((_,index)=>"joinSort"+index).ToArray();
            var sortValues=string.Concat(sorts.Select((sort,index)=>","+sort.Expression+" AS "+aliases[index]));
            var returned=string.Concat(aliases.Select(alias=>","+alias));
            var emptySort=string.Concat(aliases.Select(alias=>",null AS "+alias));
            var directions=sorts.Select((sort,index)=>aliases[index]+(sort.Descending?" DESC":" ASC")).ToArray();
            var sourceOrder=string.Join(",",directions.Concat(new[]{"p.f_Id ASC"}));
            var outputOrder=string.Join(",",new[]{"input.valid ASC"}.Concat(directions).Concat(new[]{"input.id ASC"}));
            var nativeCells=string.Concat(cells.Select((cell,index)=>","+cell.State+" AS js"+index+","+cell.Value+" AS jv"+index));
            var returnedCells=string.Concat(cells.Select((_,index)=>",js"+index+",jv"+index));
            var bytes=(48+cells.Count*16).ToString(CultureInfo.InvariantCulture)
                +string.Concat(cells.Select((_,index)=>"+CASE WHEN jv"+index+" IS NOT NULL AND jv"+index+" IS :: STRING THEN size(jv"+index+") ELSE 16 END"));
            var fetch=source.Limit.HasValue?Math.Min(source.Limit.Value,maximumRows+1):maximumRows+1;
            var take=translator.Bind(fetch);var offset=translator.Bind(source.Offset);var byteLimit=translator.Bind(maximumBytes);
            var empty="{id:h.Id,visible:false,creator:null,valid:false,domainValid:false,cycle:false,traversalLimit:false,payloadLimit:false,bytes:0,keys:[],values:[],ordering:[]}";
            var traversalError=translator.TraversalClauses.Count==0?"":" UNION ALL "+compiled.EligibleSource
                +"WHERE "+compiled.Cycle+" OR "+compiled.TraversalLimit
                +" RETURN {id:h.Id,visible:false,creator:null,valid:false,domainValid:false,cycle:"+compiled.Cycle
                +",traversalLimit:"+compiled.TraversalLimit+",payloadLimit:false,bytes:0,keys:[],values:[],ordering:[]} AS input"+emptySort+" LIMIT 1";
            if(entity!=null)
            {
                var relation=CompileCqlEntityComposition(compiled,entity,10000,relationOnly:true);
                var validDomain=invalid.Count==0 ? "true" : "NOT("+string.Join(" OR ",invalid)+")";
                var map="{id:p.f_Id,visible:p.f_IsPublic,creator:p.f_CreatedByAvatarId,valid:true,domainValid:"+validDomain
                    +",cycle:false,traversalLimit:false,payloadLimit:joinBytes>"+byteLimit+",bytes:joinBytes,keys:["+string.Join(",",keys)
                    +"],values:CASE WHEN joinBytes>"+byteLimit+" THEN [] ELSE ["+string.Join(",",values)+"] END,"
                    +"ordering:CASE WHEN joinBytes>"+byteLimit+" THEN [] ELSE ["+string.Join(",",order)+"] END}";
                return "CALL { "+relation+" } WITH collect({valid:entityValid,cycle:entityCycle,limited:entityLimit,source:entitySource,selected:selected}) AS scopes "
                    +"WITH scopes,head([scope IN scopes WHERE scope.valid | scope]) AS outcome "
                    +"CALL { WITH scopes UNWIND scopes AS scope UNWIND CASE WHEN NOT scope.valid OR scope.cycle OR scope.limited "
                    +"THEN [{h:null,p:null,valid:scope.valid,cycle:scope.cycle,limited:scope.limited}] "
                    +"ELSE [item IN scope.source | {h:item.h,p:item.p,valid:true,cycle:false,limited:false}] END AS entry "
                    +"RETURN collect({id:entry.h.Id,revision:entry.h.ProviderRevisionId,valid:entry.valid,"
                    +"snapshot:CASE WHEN size(entry.h.SnapshotJson)<=2097152 THEN entry.h.SnapshotJson ELSE null END,"
                    +"traversalCycle:entry.cycle,traversalLimit:entry.limited}) AS entityProofs } "
                    +"CALL { WITH outcome UNWIND CASE WHEN outcome IS NULL OR outcome.cycle OR outcome.limited THEN [] "
                    +"ELSE range(0,size(outcome.selected)-1) END AS position WITH outcome.selected[position] AS item,position "
                    +"WITH item.h AS h,item.p AS p,position WITH h,p,position"+nativeCells+" WITH h,p,position"+returnedCells
                    +","+bytes+" AS joinBytes WITH "+map+" AS input,position ORDER BY position RETURN collect(input) AS input } RETURN input,entityProofs";
            }
            return "CALL { "+compiled.Source+"WHERE NOT valid RETURN "+empty+" AS input"+emptySort+" LIMIT 1"
                +traversalError+" UNION ALL "+compiled.EligibleSource
                +"WHERE ("+compiled.DescendantPredicate+") AND NOT("+compiled.Cycle+" OR "+compiled.TraversalLimit+") "
                +"WITH h,p"+sortValues+" ORDER BY "+sourceOrder+" SKIP "+offset+" LIMIT "+take+" "
                +"WITH h,p"+returned+nativeCells+" WITH h,p"+returned+returnedCells+","+bytes+" AS joinBytes "
                +"RETURN {id:p.f_Id,visible:p.f_IsPublic,creator:p.f_CreatedByAvatarId,valid:true,domainValid:NOT("
                +(invalid.Count==0?"false":string.Join(" OR ",invalid))
                +"),cycle:false,traversalLimit:false,payloadLimit:joinBytes>"+byteLimit+",bytes:joinBytes,"
                +"keys:CASE WHEN joinBytes>"+byteLimit+" THEN [] ELSE ["+string.Join(",",keys)+"] END,"
                +"values:CASE WHEN joinBytes>"+byteLimit+" THEN [] ELSE ["+string.Join(",",values)+"] END,"
                +"ordering:CASE WHEN joinBytes>"+byteLimit+" THEN [] ELSE ["+string.Join(",",order)+"] END} AS input"+returned
                +" } RETURN input ORDER BY "+outputOrder;

            string Cell(CosmicQueryPath path,bool ordering)
            {
                var index=cells.Count;cells.Add(translator.TranslateProjectionPath(path));
                var required=!ordering && path.ValueType.IsValueType && Nullable.GetUnderlyingType(path.ValueType)==null;
                invalid.Add("js"+index+(required?"<>2":"=3"));
                return "{s:js"+index+",v:jv"+index+"}";
            }
        }
}
