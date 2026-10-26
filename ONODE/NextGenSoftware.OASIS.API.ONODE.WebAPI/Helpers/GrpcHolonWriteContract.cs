using System;
using System.Collections.Generic;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Grpc;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;

/// <summary>One V2 holon wire contract across REST and gRPC. Protobuf supplies
/// the operation envelope; the existing bounded serializer owns recursive holons
/// and stored child-role observations. No payload chooses a CLR type.</summary>
internal static class GrpcHolonWriteContract
{
    private static readonly JsonSerializerOptions JsonOptions = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        HolonJsonContracts.Configure(options);
        return options;
    }

    internal static OASISResult<SaveHolonRequest> Decode(SaveHolonGrpcRequest wire)
    {
        if (wire == null || wire.Holon != null || string.IsNullOrWhiteSpace(wire.HolonJson) ||
            wire.ExpectedRevisions.Count > 10000)
            return Invalid("V2 requires one recursive holon_json payload and no legacy holon field.");
        if (wire.SaveChildrenForAllProviders)
            return Invalid("V2 conditional writes select one authority; cross-provider graph propagation requires replication enrollment.");
        Guid? operation = null;
        if (!string.IsNullOrEmpty(wire.OperationId))
        {
            if (!Guid.TryParseExact(wire.OperationId, "D", out var parsed) || parsed == Guid.Empty)
                return Invalid("Supply a canonical nonempty operation identity.");
            operation = parsed;
        }
        var conditions = new Dictionary<Guid, Guid?>();
        foreach (var pair in wire.ExpectedRevisions)
        {
            if (!Guid.TryParseExact(pair.Key, "D", out var id) || id == Guid.Empty)
                return Invalid("Expected revision keys must be canonical nonempty record identities.");
            Guid? revision = null;
            if (pair.Value.Length != 0)
            {
                if (!Guid.TryParseExact(pair.Value, "D", out var parsed))
                    return Invalid("Expected revisions must be canonical provider tokens or empty create-only values.");
                revision = parsed;
            }
            if (!conditions.TryAdd(id, revision)) return Invalid("Duplicate record revision conditions.");
        }
        try
        {
            var holon = JsonSerializer.Deserialize<Holon>(wire.HolonJson, JsonOptions);
            if (holon == null) return Invalid("A holon object is required.");
            return new OASISResult<SaveHolonRequest>(new SaveHolonRequest
            {
                Holon = holon, OperationId = operation,
                ExpectedRevisions = conditions.Count == 0 ? null : conditions,
                ProviderType = wire.ProviderType, SetGlobally = wire.SetGlobally,
                SaveChildren = wire.SaveChildren, Recursive = wire.Recursive,
                MaxChildDepth = wire.MaxChildDepth, ContinueOnError = wire.ContinueOnError
            });
        }
        catch (JsonException) { return Invalid("The recursive holon JSON is invalid or exceeds the wire depth limit."); }
    }

    internal static HolonResponse Encode(OASISResult<IHolon> result)
    {
        var response = new HolonResponse
        {
            IsError = result.IsError, ErrorCode = result.ErrorCode ?? string.Empty,
            Message = result.Message ?? string.Empty, IsSaved = result.IsSaved, ErrorCount = result.ErrorCount
        };
        if (!result.IsError && result.Result != null)
            response.HolonJson = JsonSerializer.Serialize<IHolon>(result.Result, JsonOptions);
        if (result.MetaData != null)
        {
            foreach (var pair in result.MetaData) response.ResultMetadata.Add(pair.Key, pair.Value ?? string.Empty);
            if (result.MetaData.TryGetValue("COSMIC.CommittedDelta", out var delta))
                response.CommittedDeltaJson = delta ?? string.Empty;
        }
        return response;
    }

    private static OASISResult<SaveHolonRequest> Invalid(string message) =>
        new OASISResult<SaveHolonRequest> { IsError = true, ErrorCount = 1,
            ErrorCode = "COSMIC_WRITE_INTENT_INVALID", Message = message };
}
