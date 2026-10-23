using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public partial class Neo4jOASIS
{
    // Read committed is the native guarantee. All graph commands stay in this
    // transaction; no public loader or second session can change the read owner.
    private static async Task<IReadOnlyList<IHolon>> ReadCqlChildrenAsync(
        CosmicQueryChildSelection selection,IAsyncTransaction transaction,NeoCqlReadBudget budget)
    {
        var rows=new List<IHolon>();
        var parent=selection.ParentId;
        var type=selection.ChildType;
        var members=selection.MemberIds;
        var batches=members==null?1:(members.Count+CosmicQueryExpression.MaximumMembershipValues-1)/CosmicQueryExpression.MaximumMembershipValues;
        for(var batch=0;batch<batches;batch++)
        {
            budget.Check();
            var query=CosmicQuery<Holon>.Create().Where(child=>child.ParentHolonId==parent);
            if(type!=HolonType.All)query=query.Where(child=>child.HolonType==type);
            if(members!=null)
            {
                var selected=members.Skip(batch*CosmicQueryExpression.MaximumMembershipValues)
                    .Take(CosmicQueryExpression.MaximumMembershipValues).ToArray();
                query=query.Where(child=>selected.Contains(child.Id));
            }
            if(query.IsError)throw new InvalidDataException(query.Message);
            var translator=new NeoCqlTranslator();
            var predicate=string.Join(" AND ",query.Result.Predicates.Select(translator.Translate));
            var remaining=selection.MaximumNodes-rows.Count;
            translator.Parameters.Add("schema",NeoCqlProjection.SchemaVersion);
            translator.Parameters.Add("parent",parent.ToString("D"));
            translator.Parameters.Add("limit",remaining+1);
            var source="MATCH (parent:Holon)-[edge:HAS_CHILD]->(h:Holon) WHERE toLower(parent.Id)=$parent "
                +"WITH h,count(edge) AS incoming "
                +"OPTIONAL MATCH (h)-[p:HAS_CQL_PROJECTION]->(h) "
                +"WITH h,incoming,collect(p) AS projections "
                +"WITH h,head(projections) AS p,incoming=1 AND size(projections)=1 AS uniqueProjection "
                +"WITH h,p,coalesce(uniqueProjection AND p.SchemaVersion=$schema AND p.f_Id=toLower(h.Id) "
                +"AND p.f_ParentHolonId=$parent AND h.SnapshotJson IS NOT NULL "
                +"AND coalesce(p.ProviderRevisionId,'')=coalesce(h.ProviderRevisionId,''),false) AS valid ";
            // Invalid native relationships/projections cannot disappear behind a
            // type/member predicate. The valid branch performs native selection.
            var text="CALL { "+source+"WHERE NOT valid RETURN h.Id AS id,h.ProviderRevisionId AS revision,false AS valid,null AS snapshot LIMIT 1 "
                +"UNION ALL "+source+"WHERE valid AND ("+predicate+") WITH h,p ORDER BY p.f_Id LIMIT $limit "
                +"RETURN h.Id AS id,h.ProviderRevisionId AS revision,true AS valid,"
                +"CASE WHEN size(h.SnapshotJson)<=2097152 THEN h.SnapshotJson ELSE null END AS snapshot "
                +"} RETURN id,revision,valid,snapshot ORDER BY valid ASC,toLower(id) ASC";
            budget.Check();
            var cursor=await transaction.RunAsync(text,translator.Parameters).ConfigureAwait(false);
            var chunk=await ReadCqlRowsAsync(cursor,remaining,budget).ConfigureAwait(false);
            rows.AddRange(chunk);
        }
        return rows.AsReadOnly();
    }

    private static async Task<IReadOnlyList<IHolon>> ReadCqlRowsAsync(IResultCursor cursor,int maximumRows,NeoCqlReadBudget budget)
    {
        var rows=new List<IHolon>();
        while(await cursor.FetchAsync().ConfigureAwait(false))
        {
            budget.Check();
            var row=cursor.Current;
            if(!row["valid"].As<bool>())throw new InvalidDataException("Neo4j CQL relationship/projection ownership or schema is invalid.");
            if(rows.Count>=maximumRows)throw new NeoCqlLimitException("Neo4j CQL exceeds its declared node/result limit.");
            var snapshot=row["snapshot"].As<string>();
            budget.Reserve(snapshot);
            var holon=StoredRecordJson.DeserializeStoredHolon(snapshot);
            if(!Guid.TryParse(row["id"].As<string>(),out var id) || holon.Id!=id)
                throw new InvalidDataException("Neo4j CQL snapshot identity differs from its native identity.");
            var physical=row["revision"].As<string>();
            var revision=Guid.Empty;
            if(physical!=null && !Guid.TryParse(physical,out revision))throw new InvalidDataException("Neo4j CQL native revision is invalid.");
            if(CosmicConcurrency.GetProviderRevisionId(holon,Core.Enums.ProviderType.Neo4jOASIS)!=revision)
                throw new InvalidDataException("Neo4j CQL snapshot and native revision observations disagree.");
            CosmicConcurrency.SetProviderRevisionId(holon,Core.Enums.ProviderType.Neo4jOASIS,revision);
            holon.IsNewHolon=false;
            rows.Add(holon);
        }
        budget.Check();
        return rows.AsReadOnly();
    }

    private sealed class NeoCqlReadBudget
    {
        private readonly Stopwatch elapsed=Stopwatch.StartNew();
        private readonly CancellationToken cancellation;
        private readonly int maximumBytes;
        private long bytes;
        internal NeoCqlReadBudget(int maximumBytes,CancellationToken cancellation)
        {this.maximumBytes=maximumBytes;this.cancellation=cancellation;}
        internal void Check()
        {
            cancellation.ThrowIfCancellationRequested();
            if(elapsed.Elapsed>=TimeSpan.FromSeconds(30))throw new NeoCqlTimeoutException();
        }
        internal void Reserve(string snapshot)
        {
            Check();
            if(snapshot==null)throw new NeoCqlLimitException("Neo4j CQL record exceeds its payload budget.");
            var size=Encoding.UTF8.GetByteCount(snapshot);
            bytes+=size;
            if(size>8*1024*1024 || bytes>maximumBytes)throw new NeoCqlLimitException("Neo4j CQL exceeds its cumulative payload budget.");
        }
    }
    private sealed class NeoCqlLimitException(string message):Exception(message);
    private sealed class NeoCqlTimeoutException():Exception("Neo4j CQL exceeded its cumulative native deadline.");
}
