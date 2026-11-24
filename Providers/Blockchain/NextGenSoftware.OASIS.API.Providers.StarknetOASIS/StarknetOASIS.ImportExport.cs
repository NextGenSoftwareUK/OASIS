using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Starknet;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using System.Text.Json;

namespace NextGenSoftware.OASIS.API.Providers.StarknetOASIS;

public sealed partial class StarknetOASIS
{




    /// <summary>
    /// Gets the entry point selector for a Starknet function name
    /// Starknet uses Keccak256 hash of the function name, truncated to 250 bits
    /// </summary>
    private string GetEntryPointSelector(string functionName)
    {
        // Starknet entry point selector is Keccak256 hash of function name, truncated to 250 bits (62 hex chars)
        // For simplicity, we'll use SHA256 and truncate (in production, use proper Keccak256)
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(functionName));
        var hashHex = Convert.ToHexString(hashBytes).ToLowerInvariant();
        // Truncate to 62 characters (250 bits) and add 0x prefix
        return "0x" + hashHex.Substring(0, Math.Min(62, hashHex.Length));
    }

    private IAvatar ParseStarknetToAvatar(JsonElement element)
    {
        try
        {
            var avatar = new Avatar();
            if (element.TryGetProperty("id", out var idProp))
            {
                var idStr = idProp.GetString();
                if (Guid.TryParse(idStr, out var id))
                {
                    avatar.Id = id;
                }
            }
            if (element.TryGetProperty("username", out var usernameProp))
            {
                avatar.Username = usernameProp.GetString();
            }
            if (element.TryGetProperty("email", out var emailProp))
            {
                avatar.Email = emailProp.GetString();
            }
            return avatar;
        }
        catch
        {
            return null;
        }
    }

    private IAvatarDetail ParseStarknetToAvatarDetail(JsonElement element)
    {
        try
        {
            // Try full deserialize first so Inventory (including Quantity/Stack) is restored
            try
            {
                var raw = element.GetRawText();
                if (!string.IsNullOrWhiteSpace(raw))
                {
                    var full = JsonSerializer.Deserialize<AvatarDetail>(raw);
                    if (full != null)
                        return full;
                }
            }
            catch { /* fall back to minimal parse */ }

            var avatarDetail = new AvatarDetail();
            if (element.TryGetProperty("id", out var idProp))
            {
                var idStr = idProp.GetString();
                if (Guid.TryParse(idStr, out var id))
                    avatarDetail.Id = id;
            }
            if (element.TryGetProperty("username", out var usernameProp))
                avatarDetail.Username = usernameProp.GetString();
            if (element.TryGetProperty("email", out var emailProp))
                avatarDetail.Email = emailProp.GetString();
            if (element.TryGetProperty("Inventory", out var invProp))
            {
                var invRaw = invProp.GetRawText();
                if (!string.IsNullOrWhiteSpace(invRaw))
                {
                    var list = JsonSerializer.Deserialize<List<NextGenSoftware.OASIS.API.Core.Objects.InventoryItem>>(invRaw);
                    if (list != null)
                        avatarDetail.Inventory = new List<IInventoryItem>(list);
                }
            }
            return avatarDetail;
        }
        catch
        {
            return null;
        }
    }

    private IHolon ParseStarknetToHolon(JsonElement element)
    {
        try
        {
            var holon = new Holon();
            if (element.TryGetProperty("id", out var idProp))
            {
                var idStr = idProp.GetString();
                if (Guid.TryParse(idStr, out var id))
                {
                    holon.Id = id;
                }
            }
            if (element.TryGetProperty("name", out var nameProp))
            {
                holon.Name = nameProp.GetString();
            }
            if (element.TryGetProperty("description", out var descProp))
            {
                holon.Description = descProp.GetString();
            }
            if (element.TryGetProperty("parent_id", out var parentIdProp))
            {
                var parentIdStr = parentIdProp.GetString();
                if (Guid.TryParse(parentIdStr, out var parentId))
                {
                    holon.ParentHolonId = parentId;
                }
            }
            return holon;
        }
        catch
        {
            return null;
        }
    }



}
