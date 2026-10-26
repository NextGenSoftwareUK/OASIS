using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.Common;
using SaveHolonRequest = NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data.SaveHolonRequest;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers
{
    /// <summary>Authenticated save/delete routes share this V2 boundary.
    /// The controller supplies caller identity/role captured before any await.</summary>
    public static class HolonWriteEndpoint
    {
        public const string OperationHeader = "X-COSMIC-Operation-Id";
        public const string HolonIdHeader = "X-COSMIC-Holon-Id";
        public const string ExpectedRevisionHeader = "X-COSMIC-Expected-Revision";

        public static OASISResult<T> ValidateEngineMode<T>(string mode) =>
            mode == HyperDriveModes.V2 || mode == HyperDriveModes.Legacy
                ? new OASISResult<T>()
                : Error<T>("COSMIC_WRITE_ENGINE_MODE_REQUIRED", "An explicit supported engine mode is required before writing.");

        public static OASISResult<T> ConfigurationError<T>(string message) =>
            Error<T>("COSMIC_WRITE_CONFIGURATION_INVALID", message ?? "The save configuration is invalid.");

        /// <summary>Validate hosted intent before configuration or manager acquisition.
        /// Configured hosts must not let an unenrolled alias bypass admission.</summary>
        public static OASISResult<T> ValidateIntent<T>(HolonWriteIntentRequest request,
            Guid actor, string operationHeader, bool requireAdmission)
            => ValidateIntent<T>((IHolonWriteIntentRequest)request, actor, operationHeader, requireAdmission);

        public static OASISResult<T> ValidateIntent<T>(IHolonWriteIntentRequest request,
            Guid actor, string operationHeader, bool requireAdmission)
        {
            var invalid = Validate(null, request, actor, operationHeader, out _, out _,
                managerRequired: false, requireAdmission: requireAdmission);
            return invalid == null ? new OASISResult<T>()
                : Error<T>(invalid.Value.Code, invalid.Value.Message);
        }

        /// <summary>URL aliases retain the same revision intent through a
        /// canonical header. A body condition must agree; neither replaces the other.</summary>
        public static OASISResult<IHolon> BindDeleteRevision(DeleteHolonRequest request, string header)
        {
            if (string.IsNullOrEmpty(header)) return new OASISResult<IHolon>();
            if (request == null || request.Id == Guid.Empty || !Guid.TryParseExact(header, "D", out var revision) ||
                request.ExpectedRevisions != null && (request.ExpectedRevisions.Count != 1 ||
                    !request.ExpectedRevisions.TryGetValue(request.Id, out var body) || body != revision))
                return Error<IHolon>("COSMIC_WRITE_INTENT_INVALID", "Supply one canonical revision header agreeing with any body condition.");
            request.ExpectedRevisions = new Dictionary<Guid, Guid?> { [request.Id] = revision };
            return new OASISResult<IHolon>();
        }

        public static async Task<OASISResult<IHolon>> DeleteAsync(HolonManager manager,
            DeleteHolonRequest request, Guid actor, bool trustedWizard, string operationHeader = null)
        {
            var invalid = Validate(manager, request, actor, operationHeader, out var options, out var provider);
            if (invalid != null) return Error<IHolon>(invalid.Value.Code, invalid.Value.Message);
            var id = request.Id;
            var softDelete = request.SoftDelete;
            var key = request.ProviderKey;
            if (!trustedWizard)
                return key == null
                    ? await manager.DeleteOwnedHolonAsync(id, actor, options, softDelete, provider)
                    : await manager.DeleteOwnedHolonByProviderKeyAsync(key, id, actor, options, softDelete, provider);

            // Preserve the existing server-authenticated Wizard policy while
            // retaining a real observation, caller revision and pinned authority.
            var observed = key == null
                ? await manager.LoadHolonForWriteAsync(id, actor, provider)
                : await manager.LoadHolonForWriteByProviderKeyAsync(key, actor, provider);
            if (observed.IsError)
                return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<HolonAuthorityRead, IHolon>(observed);
            if (observed.Result.Record.Id != id)
                return Error<IHolon>("COSMIC_WRITE_INTENT_INVALID", "The native key and logical record identity do not agree.");
            return await manager.DeleteHolonAsync(observed.Result.Record, actor, options,
                softDelete, observed.Result.Authority);
        }

        public static Task<OASISResult<IHolon>> SaveAsync(HolonManager manager,
            SaveHolonRequest request, Guid actor, bool trustedWizard, string operationHeader = null)
        {
            var invalid = Validate(manager, request, actor, operationHeader, out var options, out var provider);
            if (invalid != null)
                return Task.FromResult(Error<IHolon>(invalid.Value.Code, invalid.Value.Message));

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

            return trustedWizard
                ? manager.SaveHolonsAsync(request.Holons, actor, options,
                    request.SaveChildren, request.Recursive, request.MaxChildDepth, provider)
                : manager.SaveOwnedHolonsAsync(request.Holons, actor, options,
                    request.SaveChildren, request.Recursive, request.MaxChildDepth, provider);
        }

        private static (string Code, string Message)? Validate(HolonManager manager,
            IHolonWriteIntentRequest request, Guid actor, string operationHeader,
            out CosmicWriteOptions options, out ProviderType provider,
            bool managerRequired = true, bool requireAdmission = false)
        {
            options = null;
            provider = ProviderType.Default;
            if (actor == Guid.Empty)
                return ("COSMIC_WRITE_ACTOR_REQUIRED", "An authenticated caller is required.");
            if (managerRequired && manager?.OASISDNA?.OASIS?.HyperDriveMode != HyperDriveModes.V2)
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
                (!Enum.TryParse(request.ProviderType, true, out provider) || !Enum.IsDefined(typeof(ProviderType), provider) ||
                    provider == ProviderType.None || provider == ProviderType.All))
                return ("COSMIC_WRITE_INTENT_INVALID", "A valid provider is required.");

            if (request is SaveHolonRequest single && single.Holon == null)
                return ("COSMIC_WRITE_INTENT_INVALID", "A holon is required.");
            if (request is SaveHolonsRequest batch &&
                (batch.Holons == null || batch.Holons.Count == 0 || batch.Holons.Count > 10000))
                return ("COSMIC_WRITE_INTENT_INVALID", "A bounded nonempty batch is required.");
            if (request is DeleteHolonRequest deletion)
            {
                if (deletion.ProviderKey != null && (string.IsNullOrWhiteSpace(deletion.ProviderKey) || deletion.ProviderKey.Length > 4096))
                    return ("COSMIC_WRITE_INTENT_INVALID", "A bounded nonempty native key is required.");
                if (deletion.Id == Guid.Empty)
                    return ("COSMIC_WRITE_INTENT_INVALID", "A nonempty record identity is required for deletion.");
                if (deletion.ExpectedRevisions == null || deletion.ExpectedRevisions.Count != 1 ||
                    !deletion.ExpectedRevisions.TryGetValue(deletion.Id, out var revision) || !revision.HasValue)
                    return ("COSMIC_EXPECTED_REVISION_REQUIRED", "Supply exactly this record's original provider revision for deletion.");
            }

            var admission = PreparedStorageExecution.Current;
            if (requireAdmission && admission == null)
                return ("COSMIC_WRITE_ADMISSION_REQUIRED", "This configured host requires an admitted write route.");
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
                OASISResultErrorCodes.RecordNotFound => HttpStatusCode.NotFound,
                "COSMIC_WRITE_INTENT_INVALID" or "COSMIC_WRITE_CONFIGURATION_INVALID" or
                    "COSMIC_EXPECTED_REVISION_REQUIRED" => HttpStatusCode.BadRequest,
                _ => HttpStatusCode.ServiceUnavailable
            };

        private static OASISResult<T> Error<T>(string code, string message) =>
            new OASISResult<T> { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message };
    }
}
