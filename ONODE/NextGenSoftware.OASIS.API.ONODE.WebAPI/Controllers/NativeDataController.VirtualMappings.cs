using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    public partial class NativeDataController
    {
        public sealed class VirtualMappingQueryRequest
        {
            public string MatchField { get; set; }
            public string MatchType { get; set; }
            public JsonElement MatchValue { get; set; }
            public string[] Select { get; set; }
            public int Take { get; set; }
        }

        public sealed class VirtualMappingDefinitionRequest
        {
            public Guid Id { get; set; }
            public string SourceName { get; set; }
            public string ConnectionName { get; set; }
            public Dictionary<string, string> Fields { get; set; }
        }

        /// <summary>Builds a private definition for the caller to commit through the conditional Holon save route.</summary>
        [HttpPost("{providerType}/virtual-mappings/prepare")]
        public async Task<ActionResult<OASISResult<Holon>>> PrepareVirtualMapping(
            ProviderType providerType, [FromBody] VirtualMappingDefinitionRequest request)
        {
            if (Avatar == null)
                return Unauthorized();
            if (request == null || request.Id == Guid.Empty || string.IsNullOrWhiteSpace(request.SourceName) ||
                request.SourceName.Length > 128 || string.IsNullOrWhiteSpace(request.ConnectionName) ||
                request.ConnectionName.Length > 128)
                return BadRequest(new OASISResult<Holon> { IsError = true, ErrorCode = "VIRTUAL_MAPPING_INVALID",
                    Message = "A mapping ID, source name and named connection are required." });

            if (OASISBootLoader.NativeImportReaders.Create(providerType) is not INativeDataQueryProvider reader)
                return BadRequest(new OASISResult<Holon> { IsError = true, ErrorCode = "VIRTUAL_HOLON_SOURCE_UNAVAILABLE",
                    Message = "The provider does not support virtual mapping queries." });

            var actor = AvatarId;
            var connection = await Program.SettingsManager.GetNativeImportConnectionAsync(actor, providerType, request.ConnectionName);
            if (connection.IsError || connection.Result == null)
                return BadRequest(new OASISResult<Holon> { IsError = true, ErrorCode = "VIRTUAL_HOLON_SOURCE_UNAVAILABLE",
                    Message = "The named connection is unavailable." });

            try
            {
                var identity = reader.DescribeNativeConnection(connection.Result);
                var mapping = new VirtualHolonMapping(request.Id, 1, actor, providerType, request.SourceName,
                    request.ConnectionName, identity, request.Fields);
                return Ok(new OASISResult<Holon>(mapping.ToDefinitionHolon()));
            }
            catch (NativeConnectionRefusedException)
            {
                return BadRequest(new OASISResult<Holon> { IsError = true, ErrorCode = "VIRTUAL_HOLON_SOURCE_UNAVAILABLE",
                    Message = "The named connection cannot be used for a virtual mapping." });
            }
            catch (ArgumentException)
            {
                return BadRequest(new OASISResult<Holon> { IsError = true, ErrorCode = "VIRTUAL_MAPPING_INVALID",
                    Message = "Use one to 32 unique approved metadata bindings." });
            }
        }

        [HttpPost("{providerType}/virtual-mappings/{id:guid}/query")]
        public async Task<ActionResult<OASISResult<IReadOnlyList<VirtualHolonRead>>>> QueryVirtualMapping(
            ProviderType providerType, Guid id, [FromBody] VirtualMappingQueryRequest request, CancellationToken cancellationToken)
        {
            if (Avatar == null)
                return Unauthorized();
            if (request == null || id == Guid.Empty)
                return BadRequest(VirtualReadError("VIRTUAL_HOLON_QUERY_INVALID", "A mapping ID and query are required."));

            object match = (request.MatchType, request.MatchValue.ValueKind) switch
            {
                ("String", JsonValueKind.String) => request.MatchValue.GetString(),
                ("Boolean", JsonValueKind.True or JsonValueKind.False) => request.MatchValue.GetBoolean(),
                ("Int32", JsonValueKind.Number) when request.MatchValue.TryGetInt32(out var number) => number,
                ("Int64", JsonValueKind.String) when long.TryParse(request.MatchValue.GetString(),
                    System.Globalization.NumberStyles.AllowLeadingSign,
                    System.Globalization.CultureInfo.InvariantCulture, out var number) => number,
                _ => null
            };
            if (match == null)
                return BadRequest(VirtualReadError("VIRTUAL_HOLON_QUERY_INVALID", "Provide a typed String, Boolean, Int32 or decimal-string Int64 match."));

            if (OASISBootLoader.NativeImportReaders.Create(providerType) is not INativeDataQueryProvider reader)
                return BadRequest(VirtualReadError("VIRTUAL_HOLON_SOURCE_UNAVAILABLE", "The provider does not support virtual mapping queries."));

            var actor = AvatarId;
            var context = HolonManager.CreateRequestContext(new ProviderRoutingOverrides(
                autoFailoverEnabled: false, autoLoadBalancingEnabled: false));
            if (context.IsError)
                return StatusCode(503, VirtualReadError("VIRTUAL_HOLON_STORE_UNAVAILABLE", "The mapping's authoritative store is unavailable."));

            var loaded = await context.Result.Manager.LoadHolonAsync(id, loadChildren: false, recursive: false, allowMissing: true);
            if (loaded.ErrorCode == OASISResultErrorCodes.RecordNotFound)
                return NotFound();
            if (loaded.IsError)
                return StatusCode(503, VirtualReadError("VIRTUAL_HOLON_STORE_UNAVAILABLE", "The mapping's authoritative store is unavailable."));
            if (loaded.Result == null || loaded.Result.Id != id || !loaded.Result.IsActive)
                return NotFound();

            string connectionName;
            try
            {
                connectionName = VirtualHolonMapping.GetDefinitionConnectionName(actor, loaded.Result, providerType);
            }
            catch (ArgumentException)
            {
                return NotFound();
            }

            var connection = await Program.SettingsManager.GetNativeImportConnectionAsync(actor, providerType, connectionName);
            if (connection.IsError || connection.Result == null)
                return BadRequest(VirtualReadError("VIRTUAL_HOLON_SOURCE_UNAVAILABLE", "The mapping's named connection is unavailable."));

            VirtualHolonMapping mapping;
            try
            {
                var identity = reader.DescribeNativeConnection(connection.Result);
                mapping = VirtualHolonMapping.FromDefinitionHolon(actor, loaded.Result, identity);
            }
            catch (ArgumentException)
            {
                return BadRequest(VirtualReadError("VIRTUAL_HOLON_CONNECTION_CHANGED", "The mapping no longer matches the named connection."));
            }
            catch (NativeConnectionRefusedException)
            {
                return BadRequest(VirtualReadError("VIRTUAL_HOLON_SOURCE_UNAVAILABLE", "The mapping's named connection is unavailable."));
            }

            return Respond(await mapping.QueryAsync(actor, reader, connection.Result, request.MatchField, match,
                request.Select, request.Take, cancellationToken));
        }

        private static OASISResult<IReadOnlyList<VirtualHolonRead>> VirtualReadError(string code, string message) =>
            new OASISResult<IReadOnlyList<VirtualHolonRead>> { IsError = true, ErrorCode = code, Message = message };
    }
}
