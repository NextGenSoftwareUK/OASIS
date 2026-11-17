using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    public partial class DataController
    {
        /// <summary>
        /// Checks the tamper-evident seal chain of a holon's saved versions: verified, broken (with the version where it breaks), partial,
        /// unsealed or unavailable. Same read rule as the audit trail: only callers who may read the holon can ask.
        /// </summary>
        [Authorize]
        [HttpGet("holon-seal/verify/{id}")]
        public async Task<OASISHttpResponseMessage<HolonSealVerification>> VerifyHolonSeal(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                    return TestDataHelper.CreateErrorResponse<HolonSealVerification>("A holon id is required.", null, System.Net.HttpStatusCode.BadRequest);
                var denied = await CheckHolonSealReadAsync(id);
                if (denied != null)
                    return TestDataHelper.CreateErrorResponse<HolonSealVerification>(denied.Value.Message, null, denied.Value.Status);
                var verification = await HolonManager.VerifyHolonSealAsync(id);
                var result = new OASISResult<HolonSealVerification> { Result = verification, IsLoaded = true, Message = verification.Message };
                return HttpResponseHelper.FormatResponse(result, System.Net.HttpStatusCode.OK, false);
            }
            catch (Exception ex)
            {
                return TestDataHelper.CreateErrorResponse<HolonSealVerification>($"Error verifying holon seal: {ex.Message}", ex, System.Net.HttpStatusCode.BadRequest);
            }
        }

        // Same read rule as load-holon and the audit trail. Null = allowed.
        private async Task<(string Message, System.Net.HttpStatusCode Status)?> CheckHolonSealReadAsync(Guid id)
        {
            var wizard = Avatar?.AvatarType?.Value == AvatarType.Wizard;
            var latest = await HolonManager.LoadHolonAsync(id, false, false, 0, true, false, HolonType.All, 0);
            if (latest == null || latest.IsError || latest.Result == null) return ("Holon not found.", System.Net.HttpStatusCode.NotFound);
            if (!wizard && await HolonManager.ProjectForAvatarAsync(latest.Result, AvatarId) == null)
                return ("Forbidden. You do not have permission to access this holon.", System.Net.HttpStatusCode.Forbidden);
            return null;
        }
    }
}
