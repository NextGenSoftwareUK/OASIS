using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    /// <summary>
    /// General biometric authentication for OASIS Avatars.
    ///
    /// Biometrics are independent of HerzID — any avatar can enroll a voice biometric
    /// and optionally require it for login or sensitive operations.
    ///
    /// Feature-gated: all endpoints return 404 when Biometric.Enabled = false in OASISDNA.
    /// Voice endpoints additionally require Biometric.VoiceEnabled = true.
    ///
    /// All endpoints require OASIS JWT authentication.
    /// </summary>
    [ApiController]
    [Route("api/biometric")]
    [Authorize]
    public class BiometricController : OASISControllerBase
    {
        private readonly IVoiceBiometricService _voice;

        private BiometricSettings Cfg =>
            OASISDNAManager.OASISDNA?.OASIS?.Security?.Biometric ?? new BiometricSettings();

        private bool BiometricEnabled => Cfg.Enabled;
        private bool VoiceEnabled => BiometricEnabled && Cfg.VoiceEnabled;

        public BiometricController(IVoiceBiometricService voice)
        {
            _voice = voice;
        }

        // ── Status ────────────────────────────────────────────────────────────

        /// <summary>Returns the biometric enrollment status of the authenticated avatar.</summary>
        [HttpGet("status")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult Status()
        {
            if (!BiometricEnabled)
                return NotFound(new { error = "Biometric authentication is not enabled on this OASIS instance." });

            var avatar = Avatar;
            if (avatar == null) return Unauthorized();

            return Ok(new
            {
                biometricEnrolled = avatar.BiometricEnrolled,
                voiceEnrolled     = !string.IsNullOrEmpty(avatar.VoiceprintId),
                requireForLogin   = Cfg.RequireForLogin,
                requireForSensitiveOps = Cfg.RequireForSensitiveOps,
                voiceServiceAvailable  = _voice.IsAvailable,
            });
        }

        // ── Voice: Enroll ─────────────────────────────────────────────────────

        /// <summary>
        /// Enroll the authenticated avatar's voice biometric.
        ///
        /// Upload a WAV/OGG/MP3 audio file (minimum ~20 seconds of clear speech per Azure requirements).
        /// Azure creates a speaker profile, stores the voiceprint model, and returns a profile GUID
        /// that OASIS saves encrypted on the Avatar. The raw audio is never stored.
        ///
        /// Subsequent calls add more audio to the same profile (Azure accumulates enrollment).
        /// To start fresh, call DELETE /api/biometric/voice first.
        /// </summary>
        [HttpPost("voice/enroll")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> VoiceEnroll(IFormFile audio)
        {
            if (!VoiceEnabled)
                return NotFound(new { error = "Voice biometrics are not enabled on this OASIS instance." });

            if (!_voice.IsAvailable)
                return StatusCode(503, new { error = "Azure Speaker Recognition is not configured. Contact the OASIS administrator." });

            var avatar = Avatar;
            if (avatar == null) return Unauthorized();

            if (audio == null || audio.Length == 0)
                return BadRequest(new { error = "Audio file is required (WAV, OGG, or MP3, minimum ~20 s of clear speech)." });

            // If avatar already has a profile, Azure will add to it; if not, a new profile is created
            VoiceEnrollResult result;
            await using (var stream = audio.OpenReadStream())
            {
                var ct = audio.ContentType ?? "audio/wav";
                result = await _voice.EnrollAsync(stream, ct);
            }

            if (!result.Success)
                return BadRequest(new { error = result.Message, profileId = result.ProfileId });

            // Persist profile ID on Avatar
            avatar.VoiceprintId    = result.ProfileId;
            avatar.BiometricEnrolled = true;
            var saveResult = await avatar.SaveAsync();
            if (saveResult.IsError)
                return StatusCode(500, new { error = "Failed to save voiceprint ID to avatar.", detail = saveResult.Message });

            return Ok(new
            {
                profileId                    = result.ProfileId,
                enrollmentSecondsAccumulated = result.EnrollmentSecondsAccumulated,
                enrollmentComplete           = result.EnrollmentComplete,
                message                      = result.Message
            });
        }

        // ── Voice: Verify ─────────────────────────────────────────────────────

        /// <summary>
        /// Verify the authenticated avatar's voice biometric.
        ///
        /// Upload a short audio clip (minimum ~5 seconds of free speech).
        /// Returns a score and whether the voice matches the enrolled profile.
        ///
        /// This can be called after login as a step-up authentication for sensitive operations.
        /// </summary>
        [HttpPost("voice/verify")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> VoiceVerify(IFormFile audio)
        {
            if (!VoiceEnabled)
                return NotFound(new { error = "Voice biometrics are not enabled on this OASIS instance." });

            var avatar = Avatar;
            if (avatar == null) return Unauthorized();

            if (string.IsNullOrEmpty(avatar.VoiceprintId))
                return BadRequest(new { error = "No voice biometric enrolled. Call POST /api/biometric/voice/enroll first." });

            if (audio == null || audio.Length == 0)
                return BadRequest(new { error = "Audio file is required." });

            VoiceVerifyResult result;
            await using (var stream = audio.OpenReadStream())
            {
                var ct = audio.ContentType ?? "audio/wav";
                result = await _voice.VerifyAsync(avatar.VoiceprintId, stream, ct);
            }

            return Ok(new
            {
                accepted = result.Accepted,
                score    = result.Score,
                message  = result.Message,
            });
        }

        // ── Voice: Delete ─────────────────────────────────────────────────────

        /// <summary>
        /// Removes the authenticated avatar's voice biometric profile from Azure and from the Avatar.
        ///
        /// After this call, the avatar's VoiceprintId is cleared and BiometricEnrolled may be set to
        /// false if no other biometric factors are enrolled.
        /// </summary>
        [HttpDelete("voice")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> VoiceDelete()
        {
            if (!VoiceEnabled)
                return NotFound(new { error = "Voice biometrics are not enabled on this OASIS instance." });

            var avatar = Avatar;
            if (avatar == null) return Unauthorized();

            if (string.IsNullOrEmpty(avatar.VoiceprintId))
                return BadRequest(new { error = "No voice biometric enrolled for this avatar." });

            await _voice.DeleteProfileAsync(avatar.VoiceprintId);

            avatar.VoiceprintId    = null;
            avatar.BiometricEnrolled = false; // no other biometric factors yet
            var saveResult = await avatar.SaveAsync();
            if (saveResult.IsError)
                return StatusCode(500, new { error = "Failed to clear voiceprint ID from avatar.", detail = saveResult.Message });

            return Ok(new { message = "Voice biometric profile removed successfully." });
        }

        // ── Verify (by avatar ID — admin use) ─────────────────────────────────

        /// <summary>
        /// Admin endpoint: verify a specific avatar's voice (e.g. for customer support identity checks).
        /// Requires the caller's clearance level to be >= 8 (Flame Keeper) when HerzID is enabled,
        /// or an admin-level OASIS token otherwise.
        /// </summary>
        [HttpPost("voice/verify/{avatarId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> VoiceVerifyByAvatarId(Guid avatarId, IFormFile audio)
        {
            if (!VoiceEnabled)
                return NotFound(new { error = "Voice biometrics are not enabled on this OASIS instance." });

            var caller = Avatar;
            if (caller == null) return Unauthorized();

            // Clearance check: HerzID Flame Keeper (8+) or the avatar themselves
            var isAdmin = caller.HerzClearanceLevel >= 8;
            var isSelf  = caller.AvatarId == avatarId;
            if (!isAdmin && !isSelf)
                return StatusCode(403, new { error = "Clearance level 8 (Flame Keeper) required to verify another avatar's biometric." });

            var loadResult = await Program.AvatarManager.LoadAvatarAsync(avatarId);
            if (loadResult.IsError || loadResult.Result == null)
                return BadRequest(new { error = "Avatar not found." });

            var target = loadResult.Result;
            if (string.IsNullOrEmpty(target.VoiceprintId))
                return BadRequest(new { error = "Target avatar has no voice biometric enrolled." });

            if (audio == null || audio.Length == 0)
                return BadRequest(new { error = "Audio file is required." });

            VoiceVerifyResult result;
            await using (var stream = audio.OpenReadStream())
            {
                var ct = audio.ContentType ?? "audio/wav";
                result = await _voice.VerifyAsync(target.VoiceprintId, stream, ct);
            }

            return Ok(new { accepted = result.Accepted, score = result.Score, message = result.Message });
        }
    }
}
