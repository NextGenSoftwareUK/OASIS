using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Globalization;
using EOSNewYork.EOSCore.Response.API;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.GetAccount;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using System.Threading;

namespace NextGenSoftware.OASIS.API.Providers.TelosOASIS
{
    public partial class TelosOASIS
    {
        private IAvatar ParseTelosToAvatar(JsonElement telosData)
        {
            try
            {
                var avatar = new Avatar();
                
                if (telosData.TryGetProperty("id", out var id))
                    avatar.Id = Guid.TryParse(id.GetString(), out var guid) ? guid : Guid.NewGuid();
                
                if (telosData.TryGetProperty("username", out var username))
                    avatar.Username = username.GetString();
                
                if (telosData.TryGetProperty("email", out var email))
                    avatar.Email = email.GetString();
                
                if (telosData.TryGetProperty("first_name", out var firstName) || telosData.TryGetProperty("firstName", out firstName))
                    avatar.FirstName = firstName.GetString();
                
                if (telosData.TryGetProperty("last_name", out var lastName) || telosData.TryGetProperty("lastName", out lastName))
                    avatar.LastName = lastName.GetString();
                
                if (telosData.TryGetProperty("avatar_type", out var avatarType) || telosData.TryGetProperty("avatarType", out avatarType))
                {
                    if (Enum.TryParse<AvatarType>(avatarType.GetString(), out var type))
                        avatar.AvatarType = new EnumValue<AvatarType>(type);
                }
                
                if (telosData.TryGetProperty("created_date", out var createdDate) || telosData.TryGetProperty("createdDate", out createdDate))
                {
                    if (DateTime.TryParse(createdDate.GetString(), out var created))
                        avatar.CreatedDate = created;
                }
                
                if (telosData.TryGetProperty("modified_date", out var modifiedDate) || telosData.TryGetProperty("modifiedDate", out modifiedDate))
                {
                    if (DateTime.TryParse(modifiedDate.GetString(), out var modified))
                        avatar.ModifiedDate = modified;
                }
                
                return avatar;
            }
            catch (Exception)
            {
                return new Avatar();
            }
        }

        /// <summary>
        /// Parse Telos blockchain response to Holon object
        /// </summary>
        private IHolon ParseTelosToHolon(JsonElement telosData)
        {
            try
            {
                var holon = new Holon();
                
                if (telosData.TryGetProperty("id", out var id))
                    holon.Id = Guid.TryParse(id.GetString(), out var guid) ? guid : Guid.NewGuid();
                
                if (telosData.TryGetProperty("name", out var name))
                    holon.Name = name.GetString();
                
                if (telosData.TryGetProperty("description", out var description))
                    holon.Description = description.GetString();
                
                if (telosData.TryGetProperty("holon_type", out var holonType) || telosData.TryGetProperty("holonType", out holonType))
                {
                    if (Enum.TryParse<HolonType>(holonType.GetString(), out var type))
                        holon.HolonType = type;
                }
                
                if (telosData.TryGetProperty("parent_holon_id", out var parentId) || telosData.TryGetProperty("parentHolonId", out parentId))
                {
                    if (Guid.TryParse(parentId.GetString(), out var parentGuid))
                        holon.ParentHolonId = parentGuid;
                }
                
                if (telosData.TryGetProperty("created_date", out var createdDate) || telosData.TryGetProperty("createdDate", out createdDate))
                {
                    if (DateTime.TryParse(createdDate.GetString(), out var created))
                        holon.CreatedDate = created;
                }
                
                if (telosData.TryGetProperty("modified_date", out var modifiedDate) || telosData.TryGetProperty("modifiedDate", out modifiedDate))
                {
                    if (DateTime.TryParse(modifiedDate.GetString(), out var modified))
                        holon.ModifiedDate = modified;
                }
                
                // Parse metadata if present
                if (telosData.TryGetProperty("metadata", out var metadata) || telosData.TryGetProperty("metaData", out metadata))
                {
                    holon.MetaData = new Dictionary<string, object>();
                    foreach (var prop in metadata.EnumerateObject())
                    {
                        holon.MetaData[prop.Name] = prop.Value.GetString();
                    }
                }
                
                return holon;
            }
            catch (Exception)
            {
                return new Holon();
            }
        }

        /// <summary>
        /// Parse Telos blockchain response to AvatarDetail object
        /// </summary>
        private IAvatarDetail ParseTelosToAvatarDetail(JsonElement telosData)
        {
            try
            {
                var avatarDetail = new AvatarDetail();
                
                if (telosData.TryGetProperty("id", out var id))
                    avatarDetail.Id = Guid.TryParse(id.GetString(), out var guid) ? guid : Guid.NewGuid();
                
                // Note: IAvatarDetail doesn't have AvatarId property, using Id instead
                // The avatar_id from Telos represents the parent avatar's ID
                // if (telosData.TryGetProperty("avatar_id", out var avatarId) || telosData.TryGetProperty("avatarId", out avatarId))
                //     avatarDetail.Id = Guid.TryParse(avatarId.GetString(), out var avatarGuid) ? avatarGuid : Guid.NewGuid();
                
                if (telosData.TryGetProperty("username", out var username))
                    avatarDetail.Username = username.GetString();
                
                if (telosData.TryGetProperty("email", out var email))
                    avatarDetail.Email = email.GetString();
                
                return avatarDetail;
            }
            catch (Exception)
            {
                return new AvatarDetail();
            }
        }

        /// <summary>
        /// Parse Telos blockchain response to Holon object
        /// </summary>
        private IHolon ParseTelosToHolon(string telosJson)
        {
            try
            {
                var telosData = JsonSerializer.Deserialize<JsonElement>(telosJson);
                return ParseTelosToHolon(telosData);
            }
            catch (Exception)
            {
                return new Holon();
            }
        }

        /// <summary>
        /// Parse Telos blockchain response to list of Holon objects
        /// </summary>
        private IEnumerable<IHolon> ParseTelosToHolons(string telosJson)
        {
            try
            {
                var telosData = JsonSerializer.Deserialize<JsonElement>(telosJson);
                
                if (telosData.TryGetProperty("result", out var result) &&
                    result.TryGetProperty("rows", out var rows) &&
                    rows.ValueKind == JsonValueKind.Array)
                {
                    var holons = new List<IHolon>();
                    foreach (var row in rows.EnumerateArray())
                    {
                        var holon = ParseTelosToHolon(row);
                        if (holon != null)
                            holons.Add(holon);
                    }
                    return holons;
                }
                
                return new List<IHolon>();
            }
            catch (Exception)
            {
                return new List<IHolon>();
            }
        }


    }
}
