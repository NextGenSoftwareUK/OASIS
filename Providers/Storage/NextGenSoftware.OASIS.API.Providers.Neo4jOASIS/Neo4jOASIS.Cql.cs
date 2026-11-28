using System;
using System.Collections.Generic;
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
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public partial class Neo4jOASIS : ICosmicQueryStorageProvider, ICosmicJoinFetchStorageProvider
{
    ProviderType ICosmicQueryStorageProvider.QueryProviderType => Core.Enums.ProviderType.Neo4jOASIS;
    CosmicQueryCapabilities ICosmicQueryStorageProvider.QueryCapabilities => CqlCapabilities;
    public static CosmicQueryCapabilities CqlCapabilities => NeoCqlContract.Capabilities;

    public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
        => ProjectCqlRowsAsync(ExecuteCqlReadAsync(plan, maximumResults, cancellationToken));

    private sealed class CqlReadOutcome
    {
        internal IReadOnlyList<IHolon> Rows;
        internal long Count;
        internal CqlReadOutcome(IReadOnlyList<IHolon> rows,long count=0) { Rows=rows; Count=count; }
    }

    private static async Task<OASISResult<IReadOnlyList<IHolon>>> ProjectCqlRowsAsync(Task<OASISResult<CqlReadOutcome>> operation)
    {
        var read=await operation.ConfigureAwait(false);
        var result=Core.Helpers.OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(read,new OASISResult<IReadOnlyList<IHolon>>());
        if (!read.IsError) result.Result=read.Result.Rows;
        return result;
    }

    private async Task<OASISResult<CqlReadOutcome>> ExecuteCqlReadAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults, CancellationToken cancellationToken, CosmicEntityCompositionPlan<T> composition=null,
        bool countOnly=false,CosmicSetQueryPlan<T> set=null,Action<string,IReadOnlyDictionary<string,object>> explain=null,
        NeoCqlJoinFetch joinFetch=null) where T:IHolon
    {
        OASISResult<CqlReadOutcome> Error(string code,string message,Exception cause=null)
            => new(){IsError=true,ErrorCount=1,ErrorCode=code,Message=message,Exception=cause};
        NeoCqlReadBudget readBudget = null;
        try
        {
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != Core.Enums.ProviderType.Neo4jOASIS)
                return Error("CQL_PLAN_INVALID", "Neo4j execution requires an exclusively Neo4j plan.");
            if (maximumResults < 1 || maximumResults > 10000)
                return Error("CQL_QUERY_LIMIT", "Neo4j CQL result limit must be between one and 10,000.");
            var admission = CqlCapabilities.AdmitPlan(plan);
            if (admission.IsError) return Error(admission.ErrorCode, admission.Message);
            if (composition != null)
            {
                var stages = EntityCompositionCapabilities.Admit(composition.Query);
                if (stages.IsError) return Error(stages.ErrorCode, stages.Message);
            }
            if(set!=null)
            {
                var admitted=CosmicSetQueryPlanner.Plan(set.Query,Core.Enums.ProviderType.Neo4jOASIS,
                    new CosmicQueryTarget(Core.Enums.ProviderType.Neo4jOASIS,CqlCapabilities),SetQueryCapabilities,set.Options);
                if(admitted.IsError)return Error(admitted.ErrorCode,admitted.Message);
            }
            var compiled=CompileCqlSource(plan.Query);
            var translator=compiled.Translator;
            var descendantPredicate=compiled.DescendantPredicate;
            var cycle=compiled.Cycle;
            var traversalLimit=compiled.TraversalLimit;
            var keys=translator.TranslateOrdering(plan.Query.Orderings);
            var rootBudget=plan.Query.GraphOptions==null?maximumResults:Math.Min(maximumResults,plan.Query.GraphOptions.MaximumNodes);
            var fetch=plan.TailIndex.HasValue ? (plan.Query.Limit == 0 ? 0 : 1)
                : plan.Query.Limit.HasValue?Math.Min(plan.Query.Limit.Value,rootBudget+1):rootBudget+1;
            translator.Parameters.Add("limit", fetch);
            translator.Parameters.Add("offset",plan.Query.Offset);
            cancellationToken.ThrowIfCancellationRequested();
            if(explain==null && composition == null && set == null && fetch==0 && !translator.HasRelationship)return new OASISResult<CqlReadOutcome>(new CqlReadOutcome(Array.Empty<IHolon>())){IsLoaded=true};
            if (!IsQuerySourceActivated || _driver == null) return Error("CQL_PROVIDER_INACTIVE", "Neo4j provider is not activated.");
            // Validation is outside the page branch: SKIP must never hide an invalid
            // projection. Both branches execute inside one native read query.
            var source=compiled.Source;
            var aliases=keys.Select((_,index)=>"cqlSort"+index).ToArray();
            var directions=keys.Select((key,index)=>aliases[index]+(key.Descending?" DESC":" ASC")).ToArray();
            var project=string.Concat(keys.Select((key,index)=>","+key.Expression+" AS "+aliases[index]));
            var returnKeys=string.Concat(aliases.Select(alias=>","+alias));
            var nullKeys=string.Concat(aliases.Select(alias=>",null AS "+alias));
            var pageOrder=string.Join(",",directions.Concat(new[]{"p.f_Id ASC"}));
            var finalOrder=string.Join(",",new[]{"valid ASC"}.Concat(directions).Concat(new[]{"toLower(id) ASC"}));
            var selectedWindow = "WITH h,p"+project+" ORDER BY "+pageOrder+" SKIP $offset LIMIT $limit ";
            if (plan.TailIndex.HasValue)
            {
                translator.Parameters.Add("tailOffset", plan.TailIndex.Value - 1);
                var sourceLimit = "";
                if (plan.Query.Limit.HasValue)
                {
                    translator.Parameters.Add("sourceLimit", plan.Query.Limit.Value);
                    sourceLimit = " LIMIT $sourceLimit";
                }
                var reverse = keys.Select((key,index)=>aliases[index]+(key.Descending?" ASC":" DESC"));
                selectedWindow = "WITH h,p"+project+" ORDER BY "+pageOrder+" SKIP $offset"+sourceLimit
                    +" WITH h,p"+returnKeys+" ORDER BY "+string.Join(",",reverse.Concat(new[]{"p.f_Id DESC"}))
                    +" SKIP $tailOffset LIMIT $limit ";
            }
            var eligibleSource=compiled.EligibleSource;
            var traversalError=translator.TraversalClauses.Count==0?"":
                " UNION ALL "+eligibleSource+"WHERE "+cycle+" OR "+traversalLimit+
                " RETURN h.Id AS id,h.ProviderRevisionId AS revision,false AS valid,null AS snapshot,"+
                cycle+" AS traversalCycle,"+traversalLimit+" AS traversalLimit"+nullKeys+" LIMIT 1";
            var rowSource="CALL { "+source+"WHERE NOT valid RETURN h.Id AS id,h.ProviderRevisionId AS revision,false AS valid,null AS snapshot,"+
                "false AS traversalCycle,false AS traversalLimit"+nullKeys+" LIMIT 1"+traversalError+" UNION ALL "+
                eligibleSource+"WHERE ("+descendantPredicate+") AND NOT("+cycle+" OR "+traversalLimit+") "+
                selectedWindow+
                "RETURN h.Id AS id,h.ProviderRevisionId AS revision,true AS valid, "
                + "CASE WHEN size(h.SnapshotJson)<=2097152 THEN h.SnapshotJson ELSE null END AS snapshot,"
                + "false AS traversalCycle,false AS traversalLimit"+returnKeys+" } ";
            var query=rowSource+"RETURN id,revision,valid,snapshot,traversalCycle,traversalLimit ORDER BY "+finalOrder;
            if (joinFetch != null)
            {
                // Join fetch extends this one root compiler with one correlated child subquery in the same command.
                if (plan.TailIndex.HasValue || plan.PostWindowPredicate != null || composition != null || set != null || countOnly
                    || plan.Query.GraphOptions != null || translator.HasRelationship || translator.TraversalClauses.Count != 0)
                    throw new NotSupportedException("Neo4j join fetch requires a plain root window without relationship predicates.");
                query=rowSource+joinFetch.Compile(translator.Parameters)
                    +" RETURN id,revision,valid,snapshot,traversalCycle,traversalLimit,children ORDER BY "+finalOrder;
                joinFetch.Command=query;
            }
            if (plan.PostWindowPredicate != null) query = CompileCqlPostWindowElement(compiled, plan);
            if (composition != null) query = CompileCqlEntityComposition(compiled, composition, maximumResults,countOnly);
            if(set!=null)query=CompileCqlSet(compiled,set,maximumResults,countOnly:countOnly);
            if(explain!=null){explain(query,translator.Parameters);return new OASISResult<CqlReadOutcome>(new CqlReadOutcome(Array.Empty<IHolon>())){IsLoaded=true};}
            using var budget=readBudget=new NeoCqlReadBudget(plan.Query.GraphOptions?.MaximumPayloadBytes??32*1024*1024,cancellationToken);
            budget.Check();
            await using var readOwner = await LeaseCqlReadAsync(budget.Token).ConfigureAwait(false);
            using var session = readOwner.Driver.AsyncSession(config => config.WithDefaultAccessMode(AccessMode.Read));
            using var transaction = await session.BeginTransactionAsync(config => config.WithTimeout(TimeSpan.FromSeconds(30))).ConfigureAwait(false);
            budget.Check();
            if (joinFetch != null) joinFetch.NativeCommands++;
            var cursor = await transaction.RunAsync(query, translator.Parameters).ConfigureAwait(false);
            long nativeCount=0;
            var options=composition?.Options??(set==null?null:new CosmicEntityCompositionOptions(
                Math.Max(set.Options.MaximumLeftInputRows,set.Options.MaximumRightInputRows),set.Options.MaximumInputPayloadBytes));
            var rows=await ReadCqlRowsAsync(cursor,joinFetch==null?rootBudget:rootBudget+1,budget,options,
                countOnly ? new Action<long>(count=>nativeCount=count) : null,
                set==null?null:checked((int)set.MaximumLeafRows),joinFetch?.Children).ConfigureAwait(false);
            if(!countOnly && plan.Query.GraphOptions!=null)
            {
                Task<IReadOnlyList<IHolon>> ReadChildren(NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.CosmicQueryChildSelection selection,CancellationToken token)
                    => ReadCqlChildrenAsync(selection,transaction,budget);
                var graph=plan.Query.ReadAccess==null
                    ? await NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.HolonGraphQueryExpansion.ExpandAsync(
                        rows,plan.Query.GraphOptions,ReadChildren,cancellationToken,budget.Check).ConfigureAwait(false)
                    : await NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.HolonGraphQueryExpansion.ExpandAsync(
                        rows,plan.Query.GraphOptions,plan.Query.ReadAccess,ReadChildren,cancellationToken,budget.Check).ConfigureAwait(false);
                if(graph.IsError)return Core.Helpers.OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(graph,new OASISResult<CqlReadOutcome>());
                rows=graph.Result;
            }
            budget.Check();
            await transaction.CommitAsync().ConfigureAwait(false);
            budget.Check();
            return new OASISResult<CqlReadOutcome>(new CqlReadOutcome(rows,nativeCount)) { IsLoaded = true };
        }
        catch (Exception cause) when (cancellationToken.IsCancellationRequested)
        { return Error("CQL_QUERY_CANCELLED", "Neo4j CQL read was cancelled.", cause); }
        catch (Exception cause) when (readBudget?.IsTimedOut == true)
        { return Error("CQL_QUERY_TIMEOUT", "Neo4j CQL read exceeded its cumulative deadline.", cause); }
        catch (NeoCqlNotReadyException cause)
        { return Error("CQL_PROVIDER_NOT_READY", cause.Message, cause); }
        catch (NeoCqlLimitException cause) { return Error("CQL_QUERY_LIMIT",cause.Message,cause); }
        catch (NeoCqlTimeoutException cause) { return Error("CQL_QUERY_TIMEOUT",cause.Message,cause); }
        catch (Neo4jException cause) when(cause.Code=="Neo.ClientError.Transaction.TransactionTimedOut"
            || cause.Code=="Neo.TransientError.Transaction.TransactionTimedOut"
            || cause.Code=="Neo.ClientError.Transaction.TransactionTimedOutClientConfiguration")
        { return Error(cancellationToken.IsCancellationRequested?"CQL_QUERY_CANCELLED":"CQL_QUERY_TIMEOUT",cause.Message,cause); }
        catch (OperationCanceledException cause) { return Error("CQL_QUERY_CANCELLED", cause.Message, cause); }
        catch (NotSupportedException cause) { return Error("CQL_CAPABILITY_UNSUPPORTED", cause.Message, cause); }
        catch (InvalidDataException cause) { return Error("CQL_DATA_INVALID", cause.Message, cause); }
        catch (Exception cause) { return Error("CQL_PROVIDER_ERROR", cause.Message, cause); }
    }

    /// <summary>A13 join fetch: the admitted root window and a correlated OPTIONAL MATCH/collect of each root's
    /// authorized direct children, executed as ONE Cypher command.</summary>
    public async Task<OASISResult<CosmicJoinFetchPacket>> ExecuteCosmicJoinFetchAsync<TRoot, TChild>(CosmicJoinFetchPlan<TRoot, TChild> plan,
        CancellationToken cancellationToken = default) where TRoot : IHolon where TChild : IHolon
    {
        if (plan?.Roots == null || plan.Children == null)
            return new OASISResult<CosmicJoinFetchPacket> { IsError = true, ErrorCount = 1, ErrorCode = "CQL_PLAN_INVALID", Message = "An admitted join-fetch plan is required." };
        var request = new NeoCqlJoinFetch(plan.Children.ReadAccess, plan.Children.Predicates, plan.MaximumChildren);
        var read = await ExecuteCqlReadAsync(plan.Roots, plan.MaximumRoots, cancellationToken, joinFetch: request).ConfigureAwait(false);
        var result = Core.Helpers.OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(read, new OASISResult<CosmicJoinFetchPacket>());
        if (!read.IsError) result.Result = new CosmicJoinFetchPacket(read.Result.Rows, request.Children, request.NativeCommands, "Cypher", request.Command);
        return result;
    }

    private sealed class NeoCqlJoinFetch
    {
        private readonly CosmicQueryReadAccess readAccess;
        private readonly IReadOnlyList<CosmicQueryPredicate> predicates;
        private readonly int maximumChildren;
        internal readonly List<IHolon> Children = new();
        internal string Command;
        internal int NativeCommands;
        internal NeoCqlJoinFetch(CosmicQueryReadAccess readAccess, IReadOnlyList<CosmicQueryPredicate> predicates, int maximumChildren)
        { this.readAccess = readAccess; this.predicates = predicates; this.maximumChildren = maximumChildren; }

        // Parameters are shared with the root translator so bound names never collide.
        internal string Compile(Dictionary<string, object> parameters)
        {
            var children = new NeoCqlTranslator("c", "cp", parameters, readAccess);
            var predicate = predicates.Count == 0 ? "true" : string.Join(" AND ", predicates.Select(children.Translate));
            if (children.HasRelationship || children.TraversalClauses.Count != 0)
                throw new NotSupportedException("Neo4j join fetch admits direct-child predicates without relationship traversal.");
            var limit = children.Bind(maximumChildren + 1);
            return "CALL (id,valid) { OPTIONAL MATCH (c:Holon)-[cp:HAS_CQL_PROJECTION]->(c) "
                + "WHERE valid AND cp.SchemaVersion=$schema AND cp.f_ParentHolonId=toLower(id) AND (" + predicate + ") "
                + "WITH c,cp ORDER BY cp.f_Id LIMIT " + limit + " "
                + "RETURN collect(CASE WHEN c IS NULL THEN null ELSE {id:c.Id,revision:c.ProviderRevisionId,"
                + "snapshot:CASE WHEN size(c.SnapshotJson)<=2097152 THEN c.SnapshotJson ELSE null END} END) AS children } ";
        }
    }

    private static OASISResult<IReadOnlyList<IHolon>> CqlFailure(string code, string message, Exception cause = null)
        => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = cause };
}
