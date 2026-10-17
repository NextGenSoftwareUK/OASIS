using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Net.Http;
using Newtonsoft.Json;
using EOSNewYork.EOSCore.ActionArgs;
using EOSNewYork.EOSCore.Response.API;
using EOSNewYork.EOSCore.Utilities;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.SEEDSOASIS.Membranes;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Events;
//using NextGenSoftware.OASIS.API.Providers.TelosOASIS;

namespace NextGenSoftware.OASIS.API.Providers.SEEDSOASIS
{
    public partial class SEEDSOASIS
    {

        // Stub implementations for IOASISStorageProvider interface
        public OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0)
        {
            return LoadAvatarAsync(id, version).Result;
        }

        public async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var response = new OASISResult<IAvatar>();

            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate SEEDS provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Use EOSIO SDK ChainAPI to read table rows instead of raw HTTP
                TableRows rows = await TelosOASIS.EOSIOOASIS.ChainAPI.GetTableRowsAsync(SEEDS_EOSIO_ACCOUNT_TEST, SEEDS_EOSIO_ACCOUNT_TEST, "avatars", "true", id.ToString(), id.ToString(), 1);

                if (rows != null && rows.rows != null && rows.rows.Count > 0)
                {
                    var avatarJson = JsonConvert.SerializeObject(rows.rows[0]);
                    var avatar = ParseSEEDSToAvatar(System.Text.Json.JsonSerializer.Deserialize<JsonElement>(avatarJson));
                    response.Result = avatar;
                    response.IsError = false;
                    response.Message = "Avatar loaded from SEEDS blockchain successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, "Avatar not found on SEEDS blockchain");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar from SEEDS: {ex.Message}");
            }

            return response;
        }

        public OASISResult<IAvatar> LoadAvatarByEmail(string email, int version = 0)
        {
            return LoadAvatarByEmailAsync(email, version).Result;
        }

        public async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string email, int version = 0)
        {
            var response = new OASISResult<IAvatar>();

            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate SEEDS provider: {activateResult.Message}");
                        return response;
                    }
                }

                TableRows rows = await TelosOASIS.EOSIOOASIS.ChainAPI.GetTableRowsAsync(SEEDS_EOSIO_ACCOUNT_TEST, SEEDS_EOSIO_ACCOUNT_TEST, "avatars", "true", email, email, 1);

                if (rows != null && rows.rows != null && rows.rows.Count > 0)
                {
                    var avatarJson = JsonConvert.SerializeObject(rows.rows[0]);
                    var avatar = ParseSEEDSToAvatar(System.Text.Json.JsonSerializer.Deserialize<JsonElement>(avatarJson));
                    response.Result = avatar;
                    response.IsError = false;
                    response.Message = "Avatar loaded by email from SEEDS blockchain successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, "Avatar not found on SEEDS blockchain");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by email from SEEDS: {ex.Message}");
            }

            return response;
        }

        public OASISResult<IAvatar> LoadAvatarByUsername(string username, int version = 0)
        {
            return LoadAvatarByUsernameAsync(username, version).Result;
        }

        public async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string username, int version = 0)
        {
            var result = new OASISResult<IAvatar>();

            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate SEEDS provider: {activateResult.Message}");
                        return result;
                    }
                }

                TableRows rows = await TelosOASIS.EOSIOOASIS.ChainAPI.GetTableRowsAsync(SEEDS_EOSIO_ACCOUNT_TEST, SEEDS_EOSIO_ACCOUNT_TEST, "avatars", "true", username, username, 1);

                if (rows != null && rows.rows != null && rows.rows.Count > 0)
                {
                    var avatarJson = JsonConvert.SerializeObject(rows.rows[0]);
                    var avatar = ParseSEEDSToAvatar(System.Text.Json.JsonSerializer.Deserialize<JsonElement>(avatarJson));

                    result.Result = avatar;
                    result.IsError = false;
                    result.Message = "Avatar loaded from SEEDS blockchain successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found in SEEDS blockchain");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar from SEEDS: {ex.Message}");
            }

            return result;
        }

        public OASISResult<IAvatar> LoadAvatarByVerificationToken(string verificationToken, int version = 0)
        {
            return LoadAvatarByVerificationTokenAsync(verificationToken, version).Result;
        }

        public async Task<OASISResult<IAvatar>> LoadAvatarByVerificationTokenAsync(string verificationToken, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate SEEDS provider: {activateResult.Message}");
                        return result;
                    }
                }

                TableRows rows = await TelosOASIS.EOSIOOASIS.ChainAPI.GetTableRowsAsync(SEEDS_EOSIO_ACCOUNT_TEST, SEEDS_EOSIO_ACCOUNT_TEST, "avatars", "true", verificationToken, verificationToken, 1);

                if (rows != null && rows.rows != null && rows.rows.Count > 0)
                {
                    var avatarJson = JsonConvert.SerializeObject(rows.rows[0]);
                    result.Result = ParseSEEDSToAvatar(System.Text.Json.JsonSerializer.Deserialize<JsonElement>(avatarJson));
                    result.IsError = false;
                    result.Message = "Avatar loaded by verification token from SEEDS blockchain successfully";
                }
                else
                    OASISErrorHandling.HandleError(ref result, "Avatar not found on SEEDS blockchain");
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by verification token from SEEDS: {ex.Message}");
            }
            return result;
        }

        public OASISResult<IAvatar> LoadAvatarByResetToken(string resetToken, int version = 0)
        {
            return LoadAvatarByResetTokenAsync(resetToken, version).Result;
        }

        public async Task<OASISResult<IAvatar>> LoadAvatarByResetTokenAsync(string resetToken, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate SEEDS provider: {activateResult.Message}");
                        return result;
                    }
                }

                TableRows rows = await TelosOASIS.EOSIOOASIS.ChainAPI.GetTableRowsAsync(SEEDS_EOSIO_ACCOUNT_TEST, SEEDS_EOSIO_ACCOUNT_TEST, "avatars", "true", resetToken, resetToken, 1);

                if (rows != null && rows.rows != null && rows.rows.Count > 0)
                {
                    var avatarJson = JsonConvert.SerializeObject(rows.rows[0]);
                    result.Result = ParseSEEDSToAvatar(System.Text.Json.JsonSerializer.Deserialize<JsonElement>(avatarJson));
                    result.IsError = false;
                    result.Message = "Avatar loaded by reset token from SEEDS blockchain successfully";
                }
                else
                    OASISErrorHandling.HandleError(ref result, "Avatar not found on SEEDS blockchain");
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by reset token from SEEDS: {ex.Message}");
            }
            return result;
        }

        public OASISResult<IAvatar> LoadAvatarByRefreshToken(string refreshToken, int version = 0)
        {
            return LoadAvatarByRefreshTokenAsync(refreshToken, version).Result;
        }

        public async Task<OASISResult<IAvatar>> LoadAvatarByRefreshTokenAsync(string refreshToken, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate SEEDS provider: {activateResult.Message}");
                        return result;
                    }
                }

                TableRows rows = await TelosOASIS.EOSIOOASIS.ChainAPI.GetTableRowsAsync(SEEDS_EOSIO_ACCOUNT_TEST, SEEDS_EOSIO_ACCOUNT_TEST, "avatars", "true", refreshToken, refreshToken, 1);

                if (rows != null && rows.rows != null && rows.rows.Count > 0)
                {
                    var avatarJson = JsonConvert.SerializeObject(rows.rows[0]);
                    result.Result = ParseSEEDSToAvatar(System.Text.Json.JsonSerializer.Deserialize<JsonElement>(avatarJson));
                    result.IsError = false;
                    result.Message = "Avatar loaded by refresh token from SEEDS blockchain successfully";
                }
                else
                    OASISErrorHandling.HandleError(ref result, "Avatar not found on SEEDS blockchain");
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by refresh token from SEEDS: {ex.Message}");
            }
            return result;
        }

        public OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
        {
            return LoadAllAvatarsAsync(version).Result;
        }

        public async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();

            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate SEEDS provider: {activateResult.Message}");
                        return result;
                    }
                }

                TableRows rows = await TelosOASIS.EOSIOOASIS.ChainAPI.GetTableRowsAsync(SEEDS_EOSIO_ACCOUNT_TEST, SEEDS_EOSIO_ACCOUNT_TEST, "avatars", "true", 0, -1, 1000);

                if (rows != null && rows.rows != null)
                {
                    var avatars = new List<IAvatar>();
                    foreach (var row in rows.rows)
                    {
                        var avatarJson = JsonConvert.SerializeObject(row);
                        var avatar = ParseSEEDSToAvatar(System.Text.Json.JsonSerializer.Deserialize<JsonElement>(avatarJson));
                        if (avatar != null)
                            avatars.Add(avatar);
                    }

                    result.Result = avatars;
                    result.IsError = false;
                    result.Message = $"Loaded {avatars.Count} avatars from SEEDS blockchain successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to load avatars from SEEDS blockchain");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error loading avatars from SEEDS: {ex.Message}");
            }

            return result;
        }

        public OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
        {
            return LoadAvatarDetailAsync(id, version).Result;
        }

        public async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();

            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate SEEDS provider: {activateResult.Message}");
                        return result;
                    }
                }

                TableRows rows = await TelosOASIS.EOSIOOASIS.ChainAPI.GetTableRowsAsync(SEEDS_EOSIO_ACCOUNT_TEST, SEEDS_EOSIO_ACCOUNT_TEST, "avatardetails", "true", id.ToString(), id.ToString(), 1);

                if (rows != null && rows.rows != null && rows.rows.Count > 0)
                {
                    var avatarDetailJson = JsonConvert.SerializeObject(rows.rows[0]);
                    var avatarDetail = ParseSEEDSToAvatarDetail(System.Text.Json.JsonSerializer.Deserialize<JsonElement>(avatarDetailJson));

                    result.Result = avatarDetail;
                    result.IsError = false;
                    result.Message = "Avatar detail loaded from SEEDS blockchain successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar detail not found in SEEDS blockchain");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail from SEEDS: {ex.Message}");
            }

            return result;
        }

        public OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string email, int version = 0)
        {
            return LoadAvatarDetailByEmailAsync(email, version).Result;
        }

        public async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string email, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate SEEDS provider: {activateResult.Message}");
                        return result;
                    }
                }

                TableRows rows = await TelosOASIS.EOSIOOASIS.ChainAPI.GetTableRowsAsync(SEEDS_EOSIO_ACCOUNT_TEST, SEEDS_EOSIO_ACCOUNT_TEST, "avatardetails", "true", email, email, 1);

                if (rows != null && rows.rows != null && rows.rows.Count > 0)
                {
                    var avatarDetailJson = JsonConvert.SerializeObject(rows.rows[0]);
                    var avatarDetail = ParseSEEDSToAvatarDetail(System.Text.Json.JsonSerializer.Deserialize<JsonElement>(avatarDetailJson));

                    result.Result = avatarDetail;
                    result.IsError = false;
                    result.Message = "Avatar detail loaded from SEEDS blockchain successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar detail not found in SEEDS blockchain");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail from SEEDS: {ex.Message}");
            }

            return result;
        }

        public OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string username, int version = 0)
        {
            return LoadAvatarDetailByUsernameAsync(username, version).Result;
        }

        public async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string username, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate SEEDS provider: {activateResult.Message}");
                        return result;
                    }
                }

                TableRows rows = await TelosOASIS.EOSIOOASIS.ChainAPI.GetTableRowsAsync(SEEDS_EOSIO_ACCOUNT_TEST, SEEDS_EOSIO_ACCOUNT_TEST, "avatardetails", "true", username, username, 1);

                if (rows != null && rows.rows != null && rows.rows.Count > 0)
                {
                    var avatarDetailJson = JsonConvert.SerializeObject(rows.rows[0]);
                    var avatarDetail = ParseSEEDSToAvatarDetail(System.Text.Json.JsonSerializer.Deserialize<JsonElement>(avatarDetailJson));

                    result.Result = avatarDetail;
                    result.IsError = false;
                    result.Message = "Avatar detail loaded from SEEDS blockchain successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar detail not found in SEEDS blockchain");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail from SEEDS: {ex.Message}");
            }

            return result;
        }

        public OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
        {
            return LoadAllAvatarDetailsAsync(version).Result;
        }

        public async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate SEEDS provider: {activateResult.Message}");
                        return result;
                    }
                }

                TableRows rows = await TelosOASIS.EOSIOOASIS.ChainAPI.GetTableRowsAsync(SEEDS_EOSIO_ACCOUNT_TEST, SEEDS_EOSIO_ACCOUNT_TEST, "avatardetails", "true", 0, -1, 1000);

                if (rows != null && rows.rows != null)
                {
                    var avatarDetails = new List<IAvatarDetail>();
                    foreach (var row in rows.rows)
                    {
                        var avatarDetailJson = JsonConvert.SerializeObject(row);
                        var avatarDetail = ParseSEEDSToAvatarDetail(System.Text.Json.JsonSerializer.Deserialize<JsonElement>(avatarDetailJson));
                        if (avatarDetail != null)
                            avatarDetails.Add(avatarDetail);
                    }

                    result.Result = avatarDetails;
                    result.IsError = false;
                    result.Message = $"Loaded {avatarDetails.Count} avatar details from SEEDS blockchain successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to load avatar details from SEEDS blockchain");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar details from SEEDS: {ex.Message}");
            }

            return result;
        }

    }
}
