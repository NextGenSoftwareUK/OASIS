using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    public partial class SettingsController
    {
        [Authorize]
        [HttpGet("holon-storage-policy")]
        public async Task<IActionResult> GetHolonStoragePolicy([FromQuery] Guid? applicationId = null)
        {
            if (Avatar == null) return Unauthorized();
            if (applicationId == Guid.Empty) return BadRequest(InvalidPolicy());
            var read = await Program.SettingsManager.GetHolonStoragePolicyAsync(Avatar.Id, applicationId);
            if (read.IsError || read.Result == null) return StatusCode(409, read);
            var value = read.Result;
            return Ok(new OASISResult<object>(new {
                applicationId,
                avatarPolicy = Wire(value.AvatarPolicy),
                applicationPolicy = Wire(value.ApplicationPolicy),
                effectivePolicy = Wire(value.EffectivePolicy),
                // Execution capabilities are explicit; the UI must not advertise an unimplemented cache.
                memoryCacheSupported = false
            }));
        }

        [Authorize]
        [HttpPut("holon-storage-policy")]
        public async Task<IActionResult> UpdateHolonStoragePolicy([FromBody] JsonElement policy,
            [FromQuery] Guid? applicationId = null)
        {
            if (Avatar == null) return Unauthorized();
            if (applicationId == Guid.Empty) return BadRequest(InvalidPolicy());
            HolonStoragePolicy parsed;
            try { parsed = HolonStoragePolicyCodec.Deserialize(policy.GetRawText()); }
            catch (Exception cause) when (cause is ArgumentException or FormatException or JsonException)
            { return BadRequest(InvalidPolicy()); }
            var saved = await Program.SettingsManager.SaveHolonStoragePolicyAsync(Avatar.Id, applicationId, parsed);
            if (saved.IsError)
                return saved.ErrorCode == "HOLON_STORAGE_POLICY_UNSUPPORTED" ? StatusCode(422, saved) : StatusCode(409, saved);
            return Ok(saved);
        }

        [Authorize]
        [HttpDelete("holon-storage-policy")]
        public async Task<IActionResult> ResetHolonStoragePolicy([FromQuery] Guid? applicationId = null)
        {
            if (Avatar == null) return Unauthorized();
            if (applicationId == Guid.Empty) return BadRequest(InvalidPolicy());
            var reset = await Program.SettingsManager.ResetHolonStoragePolicyAsync(Avatar.Id, applicationId);
            return reset.IsError ? StatusCode(409, reset) : Ok(reset);
        }

        private static JsonElement? Wire(HolonStoragePolicy policy) => policy == null
            ? null : JsonSerializer.Deserialize<JsonElement>(HolonStoragePolicyCodec.Serialize(policy));

        private static OASISResult<bool> InvalidPolicy() => new OASISResult<bool> {
            IsError = true, ErrorCode = "HOLON_STORAGE_POLICY_INVALID",
            Message = "Provide a supported versioned policy and an optional non-empty application ID."
        };
    }
}
