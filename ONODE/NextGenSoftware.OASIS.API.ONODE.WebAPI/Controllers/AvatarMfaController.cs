using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Mfa;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    public class MfaVerifyRequest
    {
        /// <summary>The mfaChallengeToken returned by authenticate.</summary>
        public string ChallengeToken { get; set; }
        /// <summary>The 6-digit code from the authenticator app.</summary>
        public string Code { get; set; }
        /// <summary>One of the one-time recovery codes (instead of Code).</summary>
        public string RecoveryCode { get; set; }
    }

    public class MfaCodeRequest { public string Code { get; set; } public string RecoveryCode { get; set; } }
    public class MfaStatusResponse { public bool Enabled { get; set; } public int RecoveryCodesLeft { get; set; } public bool NodeEnabled { get; set; } }
    public class MfaRecoveryCodesResponse { public List<string> RecoveryCodes { get; set; } }
    public class MfaEventDto { public DateTime Timestamp { get; set; } public Guid ActorId { get; set; } public string Kind { get; set; } public string Detail { get; set; } }

    /// <summary>Second sign-in step (authenticator app code) and its self-service management. Everything here is inert while OASIS.Security.Mfa.Enabled is false.</summary>
    [Route("api/avatar")]
    [ApiController]
    public class AvatarMfaController : OASISControllerBase
    {
        private MfaService Mfa => HttpContext.RequestServices.GetRequiredService<MfaService>();

        private OASISHttpResponseMessage<T> Ok<T>(T value, string message)
            => HttpResponseHelper.FormatResponse(new OASISResult<T> { Result = value, IsLoaded = true, Message = message }, HttpStatusCode.OK, false);

        private OASISHttpResponseMessage<T> Fail<T>(string message, HttpStatusCode status)
            => TestDataHelper.CreateErrorResponse<T>(message, null, status);

        private string Ip() => Request.Headers.ContainsKey("X-Forwarded-For") ? Request.Headers["X-Forwarded-For"].ToString() : HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString();

        /// <summary>
        /// Second sign-in step: send the mfaChallengeToken from authenticate plus a code (or a recovery code). On success returns the same
        /// signed-in avatar response as authenticate, including the JWT and refresh token.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("authenticate-mfa")]
        public async Task<OASISHttpResponseMessage<IAvatar>> AuthenticateMfa(MfaVerifyRequest request)
        {
            if (!Mfa.IsEnabled) return Fail<IAvatar>("MFA is not enabled on this node.", HttpStatusCode.NotFound);
            var verified = await Mfa.VerifyAsync(request?.ChallengeToken, request?.Code, request?.RecoveryCode);
            if (!verified.Success) return Fail<IAvatar>(verified.Error ?? "Authentication failed.", HttpStatusCode.Unauthorized);
            var result = await Program.AvatarManager.CompleteSecondFactorAsync(verified.AvatarId, Ip());
            if (result.IsError || result.Result == null) return HttpResponseHelper.FormatResponse(result, HttpStatusCode.Unauthorized, false);
            if (!string.IsNullOrEmpty(result.Result.RefreshToken))
                Response.Cookies.Append("refreshToken", result.Result.RefreshToken, new CookieOptions { HttpOnly = true, Expires = DateTime.UtcNow.AddDays(7) });
            return HttpResponseHelper.FormatResponse(result, HttpStatusCode.OK, false);
        }

        [Authorize]
        [HttpGet("mfa/status")]
        public async Task<OASISHttpResponseMessage<MfaStatusResponse>> Status()
        {
            if (!Mfa.IsEnabled) return Ok(new MfaStatusResponse { NodeEnabled = false }, "MFA is not enabled on this node.");
            var s = await Mfa.StatusAsync(AvatarId);
            return Ok(new MfaStatusResponse { NodeEnabled = true, Enabled = s.Enabled, RecoveryCodesLeft = s.RecoveryCodesLeft }, "MFA status.");
        }

        /// <summary>Starts enrolment: returns the secret and the otpauth:// link (QR code) for the authenticator app. MFA is not on until /mfa/confirm.</summary>
        [Authorize]
        [HttpPost("mfa/enrol")]
        public async Task<OASISHttpResponseMessage<MfaEnrolment>> Enrol()
        {
            try { return Ok(await Mfa.BeginEnrolAsync(AvatarId, Avatar?.Username ?? AvatarId.ToString()), "Scan the code, then confirm with a code from the app."); }
            catch (InvalidOperationException ex) { return Fail<MfaEnrolment>(ex.Message, HttpStatusCode.BadRequest); }
        }

        /// <summary>Confirms enrolment with the first code and returns the one-time recovery codes (shown only once).</summary>
        [Authorize]
        [HttpPost("mfa/confirm")]
        public async Task<OASISHttpResponseMessage<MfaRecoveryCodesResponse>> Confirm(MfaCodeRequest request)
        {
            try { return Ok(new MfaRecoveryCodesResponse { RecoveryCodes = await Mfa.ConfirmEnrolAsync(AvatarId, request?.Code) }, "MFA is now on. Store the recovery codes safely; they are shown once."); }
            catch (UnauthorizedAccessException ex) { return Fail<MfaRecoveryCodesResponse>(ex.Message, HttpStatusCode.Unauthorized); }
            catch (InvalidOperationException ex) { return Fail<MfaRecoveryCodesResponse>(ex.Message, HttpStatusCode.BadRequest); }
        }

        /// <summary>Turns MFA off. Needs a current code or a recovery code. Avatar types listed in MFA.AvatarTypesThatCannotDisable must ask an administrator.</summary>
        [Authorize]
        [HttpPost("mfa/disable")]
        public async Task<OASISHttpResponseMessage<bool>> Disable(MfaCodeRequest request)
        {
            try
            {
                var locked = OASISBootLoader.OASISBootLoader.OASISDNA?.OASIS?.Security?.Mfa?.AvatarTypesThatCannotDisable;
                var cannot = locked != null && Avatar?.AvatarType != null && locked.Contains(Avatar.AvatarType.Value.ToString(), StringComparer.OrdinalIgnoreCase);
                await Mfa.DisableAsync(AvatarId, request?.Code, request?.RecoveryCode, cannot);
                return Ok(true, "MFA turned off.");
            }
            catch (UnauthorizedAccessException ex) { return Fail<bool>(ex.Message, HttpStatusCode.Unauthorized); }
            catch (InvalidOperationException ex) { return Fail<bool>(ex.Message, HttpStatusCode.BadRequest); }
        }

        /// <summary>Replaces the recovery codes (the old ones stop working). Needs a current code.</summary>
        [Authorize]
        [HttpPost("mfa/recovery-codes")]
        public async Task<OASISHttpResponseMessage<MfaRecoveryCodesResponse>> RegenerateRecoveryCodes(MfaCodeRequest request)
        {
            try { return Ok(new MfaRecoveryCodesResponse { RecoveryCodes = await Mfa.RegenerateRecoveryCodesAsync(AvatarId, request?.Code) }, "New recovery codes."); }
            catch (UnauthorizedAccessException ex) { return Fail<MfaRecoveryCodesResponse>(ex.Message, HttpStatusCode.Unauthorized); }
            catch (InvalidOperationException ex) { return Fail<MfaRecoveryCodesResponse>(ex.Message, HttpStatusCode.BadRequest); }
        }

        /// <summary>Administrator (Wizard) reset for a lost device: removes the avatar's MFA so it can enrol again. Recorded with the administrator's id.</summary>
        [Authorize]
        [HttpPost("mfa/reset/{avatarId}")]
        public async Task<OASISHttpResponseMessage<bool>> Reset(Guid avatarId)
        {
            if (Avatar?.AvatarType?.Value != AvatarType.Wizard) return Fail<bool>("Forbidden. Only an administrator can reset MFA.", HttpStatusCode.Forbidden);
            try { await Mfa.ResetAsync(avatarId, AvatarId); return Ok(true, "MFA reset."); }
            catch (InvalidOperationException ex) { return Fail<bool>(ex.Message, HttpStatusCode.BadRequest); }
        }

        /// <summary>Enrolment, verification, recovery and reset events for the signed-in avatar (a Wizard may pass avatarId).</summary>
        [Authorize]
        [HttpGet("mfa/events")]
        public async Task<OASISHttpResponseMessage<List<MfaEventDto>>> Events([FromQuery] Guid? avatarId = null, [FromQuery] int limit = 50)
        {
            var target = avatarId ?? AvatarId;
            if (target != AvatarId && Avatar?.AvatarType?.Value != AvatarType.Wizard) return Fail<List<MfaEventDto>>("Forbidden.", HttpStatusCode.Forbidden);
            if (!Mfa.IsEnabled) return Fail<List<MfaEventDto>>("MFA is not enabled on this node.", HttpStatusCode.NotFound);
            var events = await Mfa.EventsAsync(target, limit);
            return Ok(events.Select(e => new MfaEventDto { Timestamp = e.Timestamp, ActorId = e.ActorId, Kind = e.Kind, Detail = e.Detail }).ToList(), "MFA events.");
        }
    }
}
