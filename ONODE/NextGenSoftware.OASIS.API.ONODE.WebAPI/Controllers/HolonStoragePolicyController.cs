using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers;

/// <summary>Placement preferences owned by the authenticated avatar. These actions never move existing records.</summary>
[ApiController]
[Authorize]
[Route("api/settings/holon-storage-policy")]
public sealed class HolonStoragePolicyController : OASISControllerBase
{
    [HttpGet]
    public async Task<ActionResult<OASISResult<Dictionary<string, object>>>> Get([FromQuery] Guid? applicationId = null)
    {
        if (AvatarId == Guid.Empty) return Unauthorized(MissingOwner<Dictionary<string, object>>());
        var selected = await Program.SettingsManager.GetHolonStoragePolicyAsync(AvatarId, applicationId);
        if (selected.IsError || selected.Result == null)
            return new OASISResult<Dictionary<string, object>> { IsError = true,
                ErrorCode = selected.ErrorCode, Message = selected.Message,
                Exception = selected.Exception, MetaData = selected.MetaData };
        return new OASISResult<Dictionary<string, object>>(Snapshot(selected.Result));
    }

    [HttpPut]
    [RequestSizeLimit(1024)]
    public async Task<ActionResult<OASISResult<bool>>> Put([FromBody] JsonElement policy,
        [FromQuery] Guid? applicationId = null)
    {
        if (AvatarId == Guid.Empty) return Unauthorized(MissingOwner<bool>());
        HolonStoragePolicy parsed;
        try
        {
            if (policy.ValueKind != JsonValueKind.Object) throw new FormatException("A storage policy must be an object.");
            parsed = HolonStoragePolicyCodec.Deserialize(policy.GetRawText());
        }
        catch (Exception cause) when (cause is ArgumentException or FormatException or JsonException)
        {
            return BadRequest(new OASISResult<bool> { IsError = true,
                ErrorCode = "HOLON_STORAGE_POLICY_INVALID", Message = cause.Message, Exception = cause });
        }
        return await Program.SettingsManager.SaveHolonStoragePolicyAsync(AvatarId, applicationId, parsed);
    }

    [HttpDelete]
    public async Task<ActionResult<OASISResult<bool>>> Delete([FromQuery] Guid? applicationId = null)
    {
        if (AvatarId == Guid.Empty) return Unauthorized(MissingOwner<bool>());
        return await Program.SettingsManager.ResetHolonStoragePolicyAsync(AvatarId, applicationId);
    }

    private static OASISResult<T> MissingOwner<T>() => new OASISResult<T> { IsError = true,
        ErrorCode = "AUTHENTICATION_REQUIRED", Message = "Sign in to manage your storage preferences." };

    private static object PolicyJson(HolonStoragePolicy policy)
    {
        if (policy == null) return null;
        using var document = JsonDocument.Parse(HolonStoragePolicyCodec.Serialize(policy));
        return document.RootElement.Clone();
    }

    private static Dictionary<string, object> Snapshot(SettingsManager.HolonStoragePolicySelection selected) => new()
    {
        ["applicationId"] = selected.ApplicationId?.ToString("D"),
        ["avatarPolicy"] = PolicyJson(selected.AvatarPolicy),
        ["applicationPolicy"] = PolicyJson(selected.ApplicationPolicy),
        ["effectivePolicy"] = PolicyJson(selected.EffectivePolicy),
        ["memoryCacheSupported"] = HolonStoragePolicyAdmission.SupportsReadCache(HolonReadCacheMode.Memory)
    };
}
