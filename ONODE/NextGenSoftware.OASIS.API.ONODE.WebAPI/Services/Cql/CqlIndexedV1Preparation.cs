using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Managers;
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
    public HolonManager Manager { get; }

    public CqlIndexedV1Binding(ICosmicChainQueryIndexReader reader, ICosmicChainQuerySourceObserver observer,
        CosmicIndexedReadTariffPolicy tariffs, CosmicChainQueryPosition minimumPosition, long maximumLagSlots,
        TimeSpan maximumHeadAge, CosmicQueryReadAccess access, HolonManager manager, int maximumResults = 100)
    {
        Reader = reader ?? throw new ArgumentNullException(nameof(reader));
        Observer = observer ?? throw new ArgumentNullException(nameof(observer));
        Tariffs = tariffs ?? throw new ArgumentNullException(nameof(tariffs));
        Access = access ?? throw new ArgumentNullException(nameof(access));
        Manager = manager ?? throw new ArgumentNullException(nameof(manager));
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
    public CosmicProjectionPlan<Holon,CosmicProjectionRecord> ProjectionPlan { get; }
    public CosmicScalarPlan<Holon,int> CountPlan { get; private set; }
    public CosmicScalarPlan<Holon,long> LongCountPlan { get; private set; }
    public CosmicScalarPlan<Holon,bool> AnyPlan { get; private set; }
    public bool IsScalar => CountPlan != null || LongCountPlan != null || AnyPlan != null;
    public bool IncludeMetadata { get; }
    public int MaximumResults { get; }
    public decimal MaximumServiceCostUsd { get; }
    public long MaximumUnits => Binding.Tariffs.MaximumPhysicalReads;
    public string RequestFingerprint { get; }

    private CqlIndexedV1Preparation(string alias, Guid actor, CqlIndexedV1Binding binding,
        CosmicQueryPlan<Holon> plan, bool includeMetadata, int maximumResults, string fingerprint,CosmicProjectionPlan<Holon,CosmicProjectionRecord> projectionPlan)
    {
        RequestFingerprint=fingerprint;ProjectionPlan=projectionPlan;
        Alias=alias;Actor=actor;Binding=binding;Plan=plan;IncludeMetadata=includeMetadata;MaximumResults=maximumResults;
        // Even an erroneous reader exhausting its full admitted physical budget is
        // covered; this is a conservative reservation bound, never an actual invoice.
        MaximumServiceCostUsd=binding.Tariffs.MaximumServiceCostUsd;
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
            var columns=CqlRequestCapture.CaptureColumns(request.Columns);
            if(columns!=null&&query.IncludeMetadata)throw new ArgumentException("Selected columns cannot request the entity metadata envelope.");
            if(string.IsNullOrEmpty(alias)||alias.Length>64||alias.Any(c=>!(c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '-' or '_' or '.'))
                ||query.Provider!=null||query.Graph!=null||query.Take is <1 or >100)
                throw new ArgumentException("An explicit bounded alias and alias-owned leaf query are required.");
            var fingerprint=Fingerprint(new CqlIndexedV1Request{Alias=alias,Query=query,Columns=columns,MaximumLagSlots=lag,MaximumHeadAgeSeconds=age});
            var resolved=Resolve(actor,alias,lag,age,query.Take,bindings,out var requirement);
            if(resolved.IsError)return Error(resolved.ErrorCode,resolved.Message,resolved.Exception);
            var binding=resolved.Result;var source=binding.Tariffs.Source;
            var intent=CqlV1Compiler.CompileIndexedSource(query,actor,requirement,binding.Access);
            var plan=CosmicQueryPlanner.Plan(intent,source.SourceProvider,new[]{
                new CosmicQueryTarget(source.SourceProvider,binding.Reader.IndexedRowCapabilities,binding.Reader.Source)});
            if(plan.IsError)return Error(plan.ErrorCode,"The indexed profile does not support the complete query.",plan.Exception);
            CosmicProjectionPlan<Holon,CosmicProjectionRecord> projectionPlan=null;
            if(columns!=null){
                if(binding.Reader is not ICosmicChainQueryIndexProjectionReader selectedReader)
                    return Error("CQL_CAPABILITY_UNSUPPORTED","The indexed alias has no native selected-column contract.");
                var selected=CosmicProjectionPlanner.Plan(CqlV1Compiler.CompileIndexedProjection(intent,columns),source.SourceProvider,new[]{
                    new CosmicProjectionTarget(source.SourceProvider,selectedReader.IndexedRowCapabilities,selectedReader.IndexedProjectionCapabilities,selectedReader.Source)});
                if(selected.IsError)return Error(selected.ErrorCode,"The indexed alias does not support the complete selected shape.",selected.Exception);
                projectionPlan=selected.Result;
            }
            return new(new(alias,actor,binding,plan.Result,query.IncludeMetadata,query.Take,fingerprint,projectionPlan));
        }
        catch(ArgumentException cause){return Error("CQL_REQUEST_INVALID","Invalid bounded indexed query description.",cause);}
        catch(Exception cause){return Error("CQL_INDEX_BINDING_FAILED","Server indexed preparation failed; no alternative was selected.",cause);}
    }

    public static OASISResult<CqlIndexedV1Preparation> Prepare(Guid actor, CqlIndexedScalarV1Request request,
        ICqlIndexedV1Bindings bindings)
    {
        try
        {
            if(actor==Guid.Empty||request==null||bindings==null)throw new ArgumentException("Actor, request and bindings are required.");
            var alias=request.Alias;var lag=request.MaximumLagSlots;var age=request.MaximumHeadAgeSeconds;
            var query=CqlRequestCapture.Capture(request.Query);
            if(string.IsNullOrEmpty(alias)||alias.Length>64||alias.Any(c=>!(c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '-' or '_' or '.'))
                ||query.Provider!=null||query.Graph!=null||query.Selection!=null||query.Take is <0 or >100
                ||query.Operation is not ("Count" or "LongCount" or "Any"))
                throw new ArgumentException("Use an alias-owned Count, LongCount or Any source with an optional bounded window.");
            var fingerprint=Fingerprint(new CqlIndexedScalarV1Request{Alias=alias,Query=query,MaximumLagSlots=lag,MaximumHeadAgeSeconds=age});
            var resolved=Resolve(actor,alias,lag,age,query.Take,bindings,out var requirement);
            if(resolved.IsError)return Error(resolved.ErrorCode,resolved.Message,resolved.Exception);
            var binding=resolved.Result;var source=binding.Tariffs.Source;
            if(binding.Reader is not ICosmicChainQueryIndexScalarReader reader)
                return Error("CQL_CAPABILITY_UNSUPPORTED","The indexed alias has no native cardinality contract.");
            var intent=CqlV1Compiler.CompileIndexedScalarSource(query,actor,requirement,binding.Access);
            var targets=new[]{new CosmicScalarTarget(source.SourceProvider,reader.IndexedRowCapabilities,reader.IndexedScalarCapabilities,reader.Source)};
            var prepared=new CqlIndexedV1Preparation(alias,actor,binding,null,false,0,fingerprint,null);
            switch(query.Operation)
            {
                case "Count":
                    var count=CosmicScalarPlanner.Plan(intent.Count().Result,source.SourceProvider,targets);
                    if(count.IsError)return Error(count.ErrorCode,count.Message,count.Exception);
                    prepared.CountPlan=count.Result;break;
                case "LongCount":
                    var longCount=CosmicScalarPlanner.Plan(intent.LongCount().Result,source.SourceProvider,targets);
                    if(longCount.IsError)return Error(longCount.ErrorCode,longCount.Message,longCount.Exception);
                    prepared.LongCountPlan=longCount.Result;break;
                default:
                    var any=CosmicScalarPlanner.Plan(intent.Any().Result,source.SourceProvider,targets);
                    if(any.IsError)return Error(any.ErrorCode,any.Message,any.Exception);
                    prepared.AnyPlan=any.Result;break;
            }
            return new(prepared);
        }
        catch(ArgumentException cause){return Error("CQL_REQUEST_INVALID","Invalid bounded indexed scalar intent.",cause);}
        catch(Exception cause){return Error("CQL_INDEX_BINDING_FAILED","Server indexed scalar preparation failed.",cause);}
    }

    private static OASISResult<CqlIndexedV1Binding> Resolve(Guid actor,string alias,long? lag,int? age,int? take,
        ICqlIndexedV1Bindings bindings,out CosmicChainQueryReadRequirement requirement)
    {
        requirement=null;
        var resolved=bindings.Resolve(actor,alias);
        if(resolved?.IsError==true)return BindingError("CQL_INDEX_FORBIDDEN","Server alias policy did not authorize this query.",resolved.Exception);
        if(resolved==null||resolved.IsWarning||resolved.Result==null)
            return BindingError("CQL_INDEX_BINDING_INVALID","Alias resolution did not return a complete server binding.");
        var binding=resolved.Result;var source=binding.Tariffs.Source;
        if(binding.Access.ActorId!=actor||binding.Access.Authority.HasValue&&binding.Access.Authority.Value!=source.SourceProvider)
            return BindingError("CQL_INDEX_BINDING_INVALID","The captured indexed grant does not match actor/source.");
        if(take.HasValue&&take.Value>binding.MaximumResults||lag.HasValue&&(lag<0||lag>binding.Baseline.MaximumLagSlots)
            ||age.HasValue&&(age<1||TimeSpan.FromSeconds(age.Value)>binding.Baseline.MaximumHeadAge))
            throw new ArgumentException("Caller freshness/result intent cannot weaken the server baseline.");
        requirement=new CosmicChainQueryReadRequirement(source,binding.Baseline.MinimumPosition,
            lag??binding.Baseline.MaximumLagSlots,age.HasValue?TimeSpan.FromSeconds(age.Value):binding.Baseline.MaximumHeadAge);
        return new(binding);
    }

    private static OASISResult<CqlIndexedV1Binding> BindingError(string code,string message,Exception cause=null)
        =>new(){IsError=true,ErrorCount=1,ErrorCode=code,Message=message,Exception=cause};

    internal bool Matches(CqlIndexedScalarV1Request request)
    {
        if(request==null||!IsScalar)return false;
        return string.Equals(RequestFingerprint,Fingerprint(new CqlIndexedScalarV1Request{Alias=request.Alias,
            Query=CqlRequestCapture.Capture(request.Query),MaximumLagSlots=request.MaximumLagSlots,
            MaximumHeadAgeSeconds=request.MaximumHeadAgeSeconds}),StringComparison.Ordinal);
    }

    private static string Fingerprint(CqlIndexedScalarV1Request request)
    {
        var wire=JsonSerializer.SerializeToUtf8Bytes(new{Contract="WEB4.CQL.indexed.scalar.v1",Request=request});
        if(wire.Length>32768)throw new ArgumentException("Indexed scalar intent exceeds the bounded wire contract.");
        return Convert.ToHexString(SHA256.HashData(wire)).ToLowerInvariant();
    }

    internal bool Matches(CqlIndexedV1Request request)
    {
        if(request==null)return false;
        var captured=new CqlIndexedV1Request{Alias=request.Alias,Query=CqlRequestCapture.Capture(request.Query),
            Columns=CqlRequestCapture.CaptureColumns(request.Columns),MaximumLagSlots=request.MaximumLagSlots,MaximumHeadAgeSeconds=request.MaximumHeadAgeSeconds};
        return string.Equals(RequestFingerprint,Fingerprint(captured),StringComparison.Ordinal);
    }

    private static string Fingerprint(CqlIndexedV1Request request)
    {
        // Include a route/version domain so this admission cannot become another result shape.
        var wire=JsonSerializer.SerializeToUtf8Bytes(new{Contract=request.Columns==null?"WEB4.CQL.indexed.rows.v1":"WEB4.CQL.indexed.projection.v1",Request=request},
            new JsonSerializerOptions{DefaultIgnoreCondition=System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault});
        if(wire.Length>32768)throw new ArgumentException("Indexed intent exceeds the bounded wire contract.");
        return Convert.ToHexString(SHA256.HashData(wire)).ToLowerInvariant();
    }

    private static OASISResult<CqlIndexedV1Preparation> Error(string code,string message,Exception cause=null)
        =>new(){IsError=true,ErrorCount=1,ErrorCode=code,Message=message,Exception=cause};
}
