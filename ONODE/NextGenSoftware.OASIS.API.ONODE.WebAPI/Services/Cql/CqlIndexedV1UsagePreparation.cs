using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

/// <summary>Exact indexed-route preparation using the stock ledger and prepared-storage middleware.</summary>
public sealed class CqlIndexedV1UsagePreparation(ICqlIndexedV1Bindings bindings,
    IPreparedStorageAdmissionFactory otherStorageFactory=null) : IUsageRequestPreparation,IPreparedStorageAdmissionFactory
{
    private static readonly object PendingKey=new();
    private sealed record Pending(CqlIndexedV1Preparation Preparation,string RawFingerprint);
    private static bool IsIndexed(HttpContext context)=>context.Request.Method=="POST"
        && (string.Equals(context.Request.Path.Value?.TrimEnd('/'),"/api/data/query-cql/indexed",StringComparison.OrdinalIgnoreCase)
            ||IsScalar(context));
    private static bool IsScalar(HttpContext context)=>string.Equals(context.Request.Path.Value?.TrimEnd('/'),
        "/api/data/query-cql/indexed/scalar",StringComparison.OrdinalIgnoreCase);

    public async Task<UsageEndpointPolicy> PrepareAsync(HttpContext context,UsageEndpointPolicy selectedPolicy,
        string rawRequestFingerprint,CancellationToken cancellationToken)
    {
        if(!IsIndexed(context))return selectedPolicy;
        if(bindings==null||context.Items.ContainsKey(PendingKey)||selectedPolicy==null
            ||!selectedPolicy.RequiresProviderMeasurement||selectedPolicy.FixedCostUsd!=0
            ||selectedPolicy.MaximumNativeFeeCostUsd.GetValueOrDefault()!=0)
            throw new UsageProtocolException("CQL_INDEX_POLICY_INVALID","Indexed reads require one explicit measured read-only route policy.",503);
        var subject=context.User.FindFirstValue(ClaimTypes.NameIdentifier)??context.User.FindFirstValue("sub");
        if(context.User.Identity?.IsAuthenticated!=true||!Guid.TryParseExact(subject,"D",out var actor)
            ||context.Items["Avatar"] is not IAvatar avatar||avatar.Id!=actor)
            throw new UsageProtocolException("CQL_AUTHENTICATION_REQUIRED","Indexed preparation requires the authenticated avatar.",401);
        if(!context.Request.HasJsonContentType())
            throw new UsageProtocolException("CQL_REQUEST_INVALID","Indexed queries require JSON.",415);
        NextGenSoftware.OASIS.Common.OASISResult<CqlIndexedV1Preparation> captured;
        try{
            var options=context.RequestServices.GetRequiredService<IOptions<Microsoft.AspNetCore.Mvc.JsonOptions>>().Value.JsonSerializerOptions;
            captured=IsScalar(context)
                ? CqlIndexedV1Preparation.Prepare(actor,await JsonSerializer.DeserializeAsync<CqlIndexedScalarV1Request>(context.Request.Body,options,cancellationToken),bindings)
                : CqlIndexedV1Preparation.Prepare(actor,await JsonSerializer.DeserializeAsync<CqlIndexedV1Request>(context.Request.Body,options,cancellationToken),bindings);
        }catch(JsonException){throw new UsageProtocolException("CQL_REQUEST_INVALID","Invalid bounded indexed query JSON.",400);}
        finally{context.Request.Body.Position=0;}
        if(captured.IsError)
            throw new UsageProtocolException(captured.ErrorCode,"Indexed request was not admitted by the server binding.",
                captured.ErrorCode=="CQL_INDEX_FORBIDDEN"?403:captured.ErrorCode=="CQL_REQUEST_INVALID"||captured.ErrorCode=="CQL_CAPABILITY_UNSUPPORTED"?400:503);
        var preparation=captured.Result;
        if(selectedPolicy.MaximumProviderAttempts<preparation.Binding.Tariffs.MaximumPhysicalReads)
            throw new UsageProtocolException("CQL_INDEX_POLICY_INVALID","The alias read budget exceeds the selected route's physical-attempt ceiling.",503);
        // Return a new server policy. Native route configuration and its fingerprint
        // remain untouched; reserve conservative dollars and units before any read.
        var policy=new UsageEndpointPolicy{
            UsesGraphQlResponseEnvelope=selectedPolicy.UsesGraphQlResponseEnvelope,
            MeterCategory=selectedPolicy.MeterCategory,ReservedUnits=Math.Max(selectedPolicy.ReservedUnits,preparation.MaximumUnits),
            EstimatedCostUsd=Math.Max(selectedPolicy.EstimatedCostUsd,preparation.MaximumServiceCostUsd),
            FixedCostUsd=0,PricingCatalogueVersion=selectedPolicy.PricingCatalogueVersion,
            RequiresProviderMeasurement=true,MaximumProviderAttempts=selectedPolicy.MaximumProviderAttempts,MaximumNativeFeeCostUsd=0
        };
        context.Items[PendingKey]=new Pending(preparation,rawRequestFingerprint);
        return policy;
    }

    public async Task<PreparedStorageExecution> CreateAsync(HttpContext context,UsageExecutionHandle execution,CancellationToken cancellationToken)
    {
        if(!IsIndexed(context))
        {
            if(otherStorageFactory==null)throw new UsageProtocolException("CQL_INDEX_POLICY_INVALID","No storage factory is configured for this other route.",503);
            return await otherStorageFactory.CreateAsync(context,execution,cancellationToken);
        }
        cancellationToken.ThrowIfCancellationRequested();
        if(!context.Items.TryGetValue(PendingKey,out var value)||value is not Pending pending)
            throw new UsageProtocolException("CQL_INDEX_PREPARATION_REQUIRED","Prepare the indexed request in the stock usage pipeline before storage admission.",503);
        var bound=CqlIndexedV1RequestContext.BindReservation(context,pending.Preparation,execution,pending.RawFingerprint);
        if(bound.IsError)throw new UsageProtocolException(bound.ErrorCode,bound.Message,503);
        return new PreparedStorageExecution(execution,"cql-indexed."+execution.OperationId,new Dictionary<ProviderType,string>(),
            Array.Empty<ProviderType>(),null,null,false,new[]{pending.Preparation.Binding.Tariffs});
    }
}

public static class CqlIndexedV1AdmissionHosting
{
    private static readonly object PriorFactoryKey=new();

    /// <summary>Register after the server's alias registry/native storage policy; exact routes remain explicitly configured.</summary>
    public static IServiceCollection AddCqlIndexedReadAdmission(this IServiceCollection services)
    {
        if(services.Any(d=>d.ServiceType==typeof(IUsageRequestPreparation)||d.ServiceType==typeof(CqlIndexedV1UsagePreparation)))
            throw new InvalidOperationException("Compose request preparation explicitly; duplicate preparation owners are not allowed.");
        var prior=services.Where(d=>d.ServiceType==typeof(IPreparedStorageAdmissionFactory)&&!d.IsKeyedService).ToArray();
        if(prior.Length>1||prior.Any(d=>d.Lifetime!=ServiceLifetime.Singleton))
            throw new InvalidOperationException("Indexed admission requires one existing singleton storage factory or none.");
        if(!services.Any(d=>d.ServiceType==typeof(ICqlIndexedV1Bindings)))
            throw new InvalidOperationException("Register the server-owned indexed alias registry before admission hosting.");
        if(prior.Length==1)
        {
            var descriptor=prior[0];services.Remove(descriptor);
            // Keyed DI preserves the existing factory's ownership/disposal semantics.
            if(descriptor.ImplementationInstance!=null)
                services.AddKeyedSingleton<IPreparedStorageAdmissionFactory>(PriorFactoryKey,(IPreparedStorageAdmissionFactory)descriptor.ImplementationInstance);
            else if(descriptor.ImplementationFactory!=null)
                services.AddKeyedSingleton<IPreparedStorageAdmissionFactory>(PriorFactoryKey,(provider,_)=>
                    (IPreparedStorageAdmissionFactory)descriptor.ImplementationFactory(provider));
            else services.AddKeyedSingleton(typeof(IPreparedStorageAdmissionFactory),PriorFactoryKey,descriptor.ImplementationType);
        }
        services.AddSingleton<CqlIndexedV1UsagePreparation>(provider=>new(provider.GetRequiredService<ICqlIndexedV1Bindings>(),
            provider.GetKeyedService<IPreparedStorageAdmissionFactory>(PriorFactoryKey)));
        services.AddSingleton<IUsageRequestPreparation>(provider=>provider.GetRequiredService<CqlIndexedV1UsagePreparation>());
        services.AddSingleton<IPreparedStorageAdmissionFactory>(provider=>provider.GetRequiredService<CqlIndexedV1UsagePreparation>());
        return services;
    }
}
