using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.Common;
using SaveHolonRequest = NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data.SaveHolonRequest;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers
{
    /// <summary>Existing authenticated REST save routes share this V2 boundary.
    /// The controller supplies caller identity/role captured before any await.</summary>
    public static class HolonWriteEndpoint
    {
        public const string OperationHeader = "X-COSMIC-Operation-Id";

        public static Task<OASISResult<IHolon>> SaveAsync(HolonManager manager,
            SaveHolonRequest request, Guid actor, bool trustedWizard, string operationHeader = null)
        {
            var invalid = Validate(manager, request, actor, operationHeader, out var options, out var provider);
            if (invalid != null)
                return Task.FromResult(Error<IHolon>(invalid.Value.Code, invalid.Value.Message));

            if (request.Holon == null)
                return Task.FromResult(Error<IHolon>("COSMIC_WRITE_INTENT_INVALID", "A holon is required."));

            // Preserve the existing server-authenticated Wizard policy, but
            // require conditional intent; there is no unconditional retry.
            return trustedWizard
                ? manager.SaveHolonAsync(request.Holon, actor, options,
                    request.SaveChildren, request.Recursive, request.MaxChildDepth, provider)
                : manager.SaveOwnedHolonAsync(request.Holon, actor, options,
                    request.SaveChildren, request.Recursive, request.MaxChildDepth, provider);
        }

        public static Task<OASISResult<IEnumerable<IHolon>>> SaveBatchAsync(HolonManager manager,
            SaveHolonsRequest request, Guid actor, bool trustedWizard, string operationHeader = null)
        {
            var invalid = Validate(manager, request, actor, operationHeader, out var options, out var provider);
            if (invalid != null)
                return Task.FromResult(Error<IEnumerable<IHolon>>(invalid.Value.Code, invalid.Value.Message));

            if (request.Holons == null || request.Holons.Count == 0 || request.Holons.Count > 10000)
                return Task.FromResult(Error<IEnumerable<IHolon>>(
                    "COSMIC_WRITE_INTENT_INVALID", "A bounded nonempty batch is required."));

            return trustedWizard
                ? manager.SaveHolonsAsync(request.Holons, actor, options,
                    request.SaveChildren, request.Recursive, request.MaxChildDepth, provider)
                : manager.SaveOwnedHolonsAsync(request.Holons, actor, options,
                    request.SaveChildren, request.Recursive, request.MaxChildDepth, provider);
        }

        private static (string Code, string Message)? Validate(HolonManager manager,
            HolonWriteIntentRequest request, Guid actor, string operationHeader,
            out CosmicWriteOptions options, out ProviderType provider)
        {
            options = null;
            provider = ProviderType.Default;
            if (actor == Guid.Empty)
                return ("COSMIC_WRITE_ACTOR_REQUIRED", "An authenticated caller is required.");
            if (manager?.OASISDNA?.OASIS?.HyperDriveMode != HyperDriveModes.V2)
                return ("COSMIC_OWNED_WRITE_V2_REQUIRED", "This write boundary requires HyperDrive V2.");
            var operation = request?.OperationId;
            if (!string.IsNullOrEmpty(operationHeader))
            {
                if (!Guid.TryParseExact(operationHeader, "D", out var headerId) || headerId == Guid.Empty ||
                    (operation.HasValue && operation.Value != headerId))
                    return ("COSMIC_WRITE_INTENT_INVALID", "Supply one canonical operation header agreeing with any body operation identity.");
                operation = headerId;
            }
            if (request == null || !operation.HasValue || operation == Guid.Empty ||
                request.MaxChildDepth < 0 || request.ExpectedRevisions?.Count > 10000)
                return ("COSMIC_WRITE_INTENT_INVALID", "Stable operation intent and bounded valid conditions are required.");
            if (!string.IsNullOrWhiteSpace(request.ProviderType) &&
                (!Enum.TryParse(request.ProviderType, true, out provider) || !Enum.IsDefined(typeof(ProviderType), provider) || provider == ProviderType.None))
                return ("COSMIC_WRITE_INTENT_INVALID", "A valid provider is required.");

            var admission = PreparedStorageExecution.Current;
            if (admission != null && !admission.IsForAvatar(actor))
                return ("COSMIC_WRITE_ADMISSION_MISMATCH", "Write admission does not belong to this caller.");

            options = new CosmicWriteOptions(operation.Value, request.ExpectedRevisions);
            return null;
        }

        public static HttpStatusCode Status<T>(OASISResult<T> result) => !result.IsError
            ? HttpStatusCode.OK
            : result.ErrorCode switch
            {
                "COSMIC_WRITE_ACTOR_REQUIRED" => HttpStatusCode.Unauthorized,
                "COSMIC_WRITE_FORBIDDEN" => HttpStatusCode.Forbidden,
                "COSMIC_CONCURRENCY_CONFLICT" => HttpStatusCode.Conflict,
                "COSMIC_WRITE_INTENT_INVALID" or "COSMIC_EXPECTED_REVISION_REQUIRED" => HttpStatusCode.BadRequest,
                _ => HttpStatusCode.ServiceUnavailable
            };

        private static OASISResult<T> Error<T>(string code, string message) =>
            new OASISResult<T> { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message };
    }
}
