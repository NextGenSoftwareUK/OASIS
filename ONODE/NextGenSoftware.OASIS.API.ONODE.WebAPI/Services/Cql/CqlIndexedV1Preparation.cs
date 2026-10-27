using System;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

/// <summary>Server-owned alias authorization. Resolve once, without activation/failover or unadmitted storage I/O.</summary>
public interface ICqlIndexedV1Bindings
{
    OASISResult<CqlIndexedV1Binding> Resolve(Guid authenticatedActor, string alias);
}

/// <summary>One captured server grant, physical read owners, freshness baseline and exact tariff policy.</summary>
public sealed class CqlIndexedV1Binding
{
    public ICosmicChainQueryIndexReader Reader { get; }
    public ICosmicChainQuerySourceObserver Observer { get; }
    public CosmicIndexedReadTariffPolicy Tariffs { get; }
    public CosmicChainQueryReadRequirement Baseline { get; }
    public CosmicQueryReadAccess Access { get; }
    public int MaximumResults { get; }

    public CqlIndexedV1Binding(ICosmicChainQueryIndexReader reader, ICosmicChainQuerySourceObserver observer,
        CosmicIndexedReadTariffPolicy tariffs, CosmicChainQueryPosition minimumPosition, long maximumLagSlots,
        TimeSpan maximumHeadAge, CosmicQueryReadAccess access, int maximumResults = 100)
    {
        Reader = reader ?? throw new ArgumentNullException(nameof(reader));
        Observer = observer ?? throw new ArgumentNullException(nameof(observer));
        Tariffs = tariffs ?? throw new ArgumentNullException(nameof(tariffs));
        Access = access ?? throw new ArgumentNullException(nameof(access));
        if(maximumResults is < 1 or > 100)throw new ArgumentException("Indexed rows require a bounded server result limit.");
        MaximumResults = maximumResults;
        Baseline = new(tariffs.Source, minimumPosition, maximumLagSlots, maximumHeadAge);
    }
}

/// <summary>
/// Frozen preparation shared by reservation and execution: alias authorization,
/// actor/source policy, immutable admitted plan and conservative service-cost bound.
/// Creating this object neither admits usage nor executes SQL/source transport.
/// </summary>
public sealed class CqlIndexedV1Preparation
{
    public string Alias { get; }
    public Guid Actor { get; }
    public CqlIndexedV1Binding Binding { get; }
    public CosmicQueryPlan<Holon> Plan { get; }
    public bool IncludeMetadata { get; }
    public int MaximumResults { get; }
    public decimal MaximumServiceCostUsd { get; }

    private CqlIndexedV1Preparation(string alias, Guid actor, CqlIndexedV1Binding binding,
        CosmicQueryPlan<Holon> plan, bool includeMetadata, int maximumResults)
    {
        Alias=alias;Actor=actor;Binding=binding;Plan=plan;IncludeMetadata=includeMetadata;MaximumResults=maximumResults;
        // Even an erroneous reader exhausting its full admitted physical budget is
        // covered; this is a conservative reservation bound, never an actual invoice.
        MaximumServiceCostUsd=checked(binding.Tariffs.MaximumPhysicalReads*binding.Tariffs.Tariffs.Max(t=>t.CostUsd));
    }

    public static OASISResult<CqlIndexedV1Preparation> Prepare(Guid actor, CqlIndexedV1Request request,
        ICqlIndexedV1Bindings bindings)
    {
        try
        {
            if(actor==Guid.Empty||request==null||bindings==null)throw new ArgumentException("Authenticated actor, request and server binding registry are required.");
            // Capture every caller-owned field before invoking the server resolver.
            var alias=request.Alias;var lag=request.MaximumLagSlots;var age=request.MaximumHeadAgeSeconds;
            var query=CqlRequestCapture.Capture(request.Query);
            if(string.IsNullOrEmpty(alias)||alias.Length>64||alias.Any(c=>!(c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '-' or '_' or '.'))
                ||query.Provider!=null||query.Graph!=null||query.Take is <1 or >100)
                throw new ArgumentException("An explicit bounded alias and alias-owned leaf query are required.");
            var resolved=bindings.Resolve(actor,alias);
            if(resolved?.IsError==true)return Error("CQL_INDEX_FORBIDDEN","Server alias policy did not authorize this query.",resolved.Exception);
            if(resolved==null||resolved.IsWarning||resolved.Result==null)
                return Error("CQL_INDEX_BINDING_INVALID","Alias resolution did not return a complete server binding.");
            var binding=resolved.Result;var source=binding.Tariffs.Source;
            if(binding.Access.ActorId!=actor||binding.Access.Authority.HasValue&&binding.Access.Authority.Value!=source.SourceProvider)
                return Error("CQL_INDEX_BINDING_INVALID","The captured indexed grant does not match actor/source.");
            if(query.Take>binding.MaximumResults||lag.HasValue&&(lag<0||lag>binding.Baseline.MaximumLagSlots)
                ||age.HasValue&&(age<1||TimeSpan.FromSeconds(age.Value)>binding.Baseline.MaximumHeadAge))
                throw new ArgumentException("Caller freshness/result intent cannot weaken the server baseline.");
            var requirement=new CosmicChainQueryReadRequirement(source,binding.Baseline.MinimumPosition,
                lag??binding.Baseline.MaximumLagSlots,age.HasValue?TimeSpan.FromSeconds(age.Value):binding.Baseline.MaximumHeadAge);
            var intent=CqlV1Compiler.CompileIndexedSource(query,actor,requirement,binding.Access);
            var plan=CosmicQueryPlanner.Plan(intent,source.SourceProvider,new[]{
                new CosmicQueryTarget(source.SourceProvider,binding.Reader.IndexedRowCapabilities,binding.Reader.Source)});
            if(plan.IsError)return Error(plan.ErrorCode,"The indexed profile does not support the complete query.",plan.Exception);
            return new(new(alias,actor,binding,plan.Result,query.IncludeMetadata,query.Take));
        }
        catch(ArgumentException cause){return Error("CQL_REQUEST_INVALID","Invalid bounded indexed query description.",cause);}
        catch(Exception cause){return Error("CQL_INDEX_BINDING_FAILED","Server indexed preparation failed; no alternative was selected.",cause);}
    }

    private static OASISResult<CqlIndexedV1Preparation> Error(string code,string message,Exception cause=null)
        =>new(){IsError=true,ErrorCount=1,ErrorCode=code,Message=message,Exception=cause};
}
