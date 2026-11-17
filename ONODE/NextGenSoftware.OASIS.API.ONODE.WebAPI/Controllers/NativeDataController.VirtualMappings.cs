using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
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

        public sealed class VirtualMappingSnapshotRequest
        {
            public Guid RowId { get; set; }
            public VirtualMappingQueryRequest Query { get; set; }
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
        public ActionResult<OASISResult<Holon>> PrepareVirtualMapping(
            ProviderType providerType, [FromBody] VirtualMappingDefinitionRequest request)
        {
            if (Avatar == null)
                return Unauthorized();
            if (request == null || request.Id == Guid.Empty || string.IsNullOrWhiteSpace(request.SourceName) ||
                request.SourceName.Length > 128 || string.IsNullOrWhiteSpace(request.ConnectionName) ||
                request.ConnectionName.Length > 128)
                return BadRequest(new OASISResult<Holon> { IsError = true, ErrorCode = "VIRTUAL_MAPPING_INVALID",
                    Message = "A mapping ID, source name and named connection are required." });

            var actor = AvatarId;
            try
            {
                if (request.ConnectionName == VirtualHolonMapping.SandboxConnectionName &&
                    !CqlV1Providers.IsSandboxVirtualMapping(providerType, request.SourceName, request.Fields))
                    return BadRequest(new OASISResult<Holon> { IsError = true, ErrorCode = "VIRTUAL_MAPPING_INVALID",
                        Message = "Use the approved sample collection fields for sandbox mappings." });

                var binding = CqlV1Providers.ResolveVirtualConnection(
                    Avatar, providerType, request.SourceName, request.ConnectionName);
                if (binding == null)
                    return BadRequest(new OASISResult<Holon> { IsError = true, ErrorCode = "VIRTUAL_HOLON_SOURCE_UNAVAILABLE",
                        Message = "The named connection is unavailable." });
                var mapping = new VirtualHolonMapping(request.Id, 1, actor, providerType, request.SourceName,
                    request.ConnectionName, binding.Identity, request.Fields);
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

            var match = ParseVirtualMatch(request);
            if (match == null)
                return BadRequest(VirtualReadError("VIRTUAL_HOLON_QUERY_INVALID", "Provide a typed String, Boolean, Int32 or decimal-string Int64 match."));

            var result = await CqlV1Providers.QueryOwnedVirtualMappingAsync(HolonManager, ProviderManager.Instance.CurrentStorageProvider, id, Avatar,
                providerType, request.MatchField, match, request.Select, request.Take, cancellationToken);
            if (result.ErrorCode == "VIRTUAL_MAPPING_NOT_FOUND")
                return NotFound();
            if (result.ErrorCode == "VIRTUAL_HOLON_STORE_UNAVAILABLE")
                return StatusCode(503, result);
            return Respond(result);
        }

        [HttpPost("{providerType}/virtual-mappings/{id:guid}/snapshot/prepare")]
        public async Task<ActionResult<OASISResult<Holon>>> PrepareVirtualSnapshot(
            ProviderType providerType, Guid id, [FromBody] VirtualMappingSnapshotRequest request,
            CancellationToken cancellationToken)
        {
            var avatar = Avatar;
            if (avatar == null)
                return Unauthorized();
            if (id == Guid.Empty || request == null || request.RowId == Guid.Empty || request.Query == null ||
                ParseVirtualMatch(request.Query) is not { } match)
                return BadRequest(SnapshotError("VIRTUAL_SNAPSHOT_INVALID", "Provide a mapping, selected row and typed query."));

            var query = request.Query;
            var rows = await CqlV1Providers.QueryOwnedVirtualMappingAsync(HolonManager,
                ProviderManager.Instance.CurrentStorageProvider, id, avatar, providerType, query.MatchField,
                match, query.Select, query.Take, cancellationToken);
            if (rows.IsError)
            {
                var error = SnapshotError(rows.ErrorCode, rows.Message);
                return rows.ErrorCode switch
                {
                    "VIRTUAL_MAPPING_NOT_FOUND" => NotFound(error),
                    "VIRTUAL_HOLON_QUERY_INVALID" or "NATIVE_QUERY_INVALID" or "VIRTUAL_HOLON_CONNECTION_CHANGED" => BadRequest(error),
                    "NATIVE_QUERY_PAYLOAD_LIMIT" => StatusCode(413, error),
                    _ => StatusCode(503, error)
                };
            }
            var row = rows.Result.SingleOrDefault(value => value.Id == request.RowId);
            if (row == null)
                return NotFound(SnapshotError("VIRTUAL_SNAPSHOT_ROW_NOT_FOUND", "The selected row is outside the current query result."));

            try { return Ok(new OASISResult<Holon>(row.PrepareSnapshot(avatar.Id))); }
            catch (ArgumentException)
            {
                return BadRequest(SnapshotError("VIRTUAL_SNAPSHOT_INVALID", "The selected fields cannot form a private snapshot."));
            }
        }

        private static OASISResult<Holon> SnapshotError(string code, string message) =>
            new OASISResult<Holon> { IsError = true, ErrorCode = code, Message = message };

        private static object ParseVirtualMatch(VirtualMappingQueryRequest request) =>
            (request.MatchType, request.MatchValue.ValueKind) switch
            {
                ("String", JsonValueKind.String) => request.MatchValue.GetString(),
                ("Boolean", JsonValueKind.True or JsonValueKind.False) => request.MatchValue.GetBoolean(),
                ("Int32", JsonValueKind.Number) when request.MatchValue.TryGetInt32(out var number) => number,
                ("Int64", JsonValueKind.String) when long.TryParse(request.MatchValue.GetString(),
                    System.Globalization.NumberStyles.AllowLeadingSign,
                    System.Globalization.CultureInfo.InvariantCulture, out var number) => number,
                _ => null
            };

        private static OASISResult<IReadOnlyList<VirtualHolonRead>> VirtualReadError(string code, string message) =>
            new OASISResult<IReadOnlyList<VirtualHolonRead>> { IsError = true, ErrorCode = code, Message = message };
    }
}
