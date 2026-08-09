using System;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Avatar;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using System.Security.Cryptography;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS
{
    public partial class NEAROASIS
    {
        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            var response = new OASISResult<bool>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate NEAR provider: {activateResult.Message}");
                        return response;
                    }
                }

                var saveResult = await SaveHolonsAsync(holons);
                response.Result = !saveResult.IsError;
                response.IsError = saveResult.IsError;
                if (saveResult.IsError)
                {
                    response.Message = saveResult.Message;
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error importing holons to NEAR: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
        {
            return ImportAsync(holons).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate NEAR provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Export all holons from NEAR smart contract
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = "dontcare",
                    method = "query",
                    @params = new
                    {
                        request_type = "call_function",
                        finality = "final",
                        account_id = "oasis.near",
                        method_name = "export_all_holons",
                        args_base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes("{}"))
                    }
                };

                var jsonContent = JsonSerializer.Serialize(rpcRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var jsonResponse = await httpResponse.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<JsonElement>(jsonResponse);
                    
                    if (result.TryGetProperty("result", out var resultElement))
                    {
                        var holons = new List<IHolon>();
                        if (resultElement.TryGetProperty("result", out var dataElement))
                        {
                            var dataString = dataElement.GetString();
                            if (!string.IsNullOrEmpty(dataString))
                            {
                                var holonData = JsonSerializer.Deserialize<List<JsonElement>>(dataString);
                                foreach (var holonJson in holonData)
                                {
                                    var holon = new Holon
                                    {
                                        Id = Guid.Parse(holonJson.GetProperty("id").GetString() ?? Guid.Empty.ToString()),
                                        Name = holonJson.GetProperty("name").GetString() ?? "",
                                        Description = holonJson.GetProperty("description").GetString() ?? "",
                                        Version = version
                                    };
                                    holons.Add(holon);
                                }
                            }
                        }
                        response.Result = holons;
                        response.IsError = false;
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Export failed");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to export from NEAR: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error exporting from NEAR: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
        {
            return ExportAllAsync(version).Result;
        }

        // Missing methods for avatar data export
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate NEAR provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Export all data for avatar from NEAR smart contract
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = "dontcare",
                    method = "query",
                    @params = new
                    {
                        request_type = "call_function",
                        finality = "final",
                        account_id = "oasis.near",
                        method_name = "export_avatar_data",
                        args_base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{{\"avatar_id\":\"{avatarId}\"}}"))
                    }
                };

                var jsonContent = JsonSerializer.Serialize(rpcRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var jsonResponse = await httpResponse.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<JsonElement>(jsonResponse);
                    
                    if (result.TryGetProperty("result", out var resultElement))
                    {
                        var holons = new List<IHolon>();
                        if (resultElement.TryGetProperty("result", out var dataElement))
                        {
                            var dataString = dataElement.GetString();
                            if (!string.IsNullOrEmpty(dataString))
                            {
                                var holonData = JsonSerializer.Deserialize<List<JsonElement>>(dataString);
                                foreach (var holonJson in holonData)
                                {
                                    var holon = new Holon
                                    {
                                        Id = Guid.Parse(holonJson.GetProperty("id").GetString() ?? Guid.Empty.ToString()),
                                        Name = holonJson.GetProperty("name").GetString() ?? "",
                                        Description = holonJson.GetProperty("description").GetString() ?? "",
                                        Version = version
                                    };
                                    holons.Add(holon);
                                }
                            }
                        }
                        response.Result = holons;
                        response.IsError = false;
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Export failed");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to export avatar data from NEAR: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error exporting avatar data from NEAR: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
        {
            return ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string email, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate NEAR provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Export all data for avatar by email from NEAR smart contract
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = "dontcare",
                    method = "query",
                    @params = new
                    {
                        request_type = "call_function",
                        finality = "final",
                        account_id = "oasis.near",
                        method_name = "export_avatar_data_by_email",
                        args_base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{{\"email\":\"{email}\"}}"))
                    }
                };

                var jsonContent = JsonSerializer.Serialize(rpcRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var jsonResponse = await httpResponse.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<JsonElement>(jsonResponse);
                    
                    if (result.TryGetProperty("result", out var resultElement))
                    {
                        var holons = new List<IHolon>();
                        if (resultElement.TryGetProperty("result", out var dataElement))
                        {
                            var dataString = dataElement.GetString();
                            if (!string.IsNullOrEmpty(dataString))
                            {
                                var holonData = JsonSerializer.Deserialize<List<JsonElement>>(dataString);
                                foreach (var holonJson in holonData)
                                {
                                    var holon = new Holon
                                    {
                                        Id = Guid.Parse(holonJson.GetProperty("id").GetString() ?? Guid.Empty.ToString()),
                                        Name = holonJson.GetProperty("name").GetString() ?? "",
                                        Description = holonJson.GetProperty("description").GetString() ?? "",
                                        Version = version
                                    };
                                    holons.Add(holon);
                                }
                            }
                        }
                        response.Result = holons;
                        response.IsError = false;
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Export failed");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to export avatar data from NEAR: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error exporting avatar data from NEAR: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string email, int version = 0)
        {
            return ExportAllDataForAvatarByEmailAsync(email, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string username, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate NEAR provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Export all data for avatar by username from NEAR smart contract
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = "dontcare",
                    method = "query",
                    @params = new
                    {
                        request_type = "call_function",
                        finality = "final",
                        account_id = "oasis.near",
                        method_name = "export_avatar_data_by_username",
                        args_base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{{\"username\":\"{username}\"}}"))
                    }
                };

                var jsonContent = JsonSerializer.Serialize(rpcRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var jsonResponse = await httpResponse.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<JsonElement>(jsonResponse);
                    
                    if (result.TryGetProperty("result", out var resultElement))
                    {
                        var holons = new List<IHolon>();
                        if (resultElement.TryGetProperty("result", out var dataElement))
                        {
                            var dataString = dataElement.GetString();
                            if (!string.IsNullOrEmpty(dataString))
                            {
                                var holonData = JsonSerializer.Deserialize<List<JsonElement>>(dataString);
                                foreach (var holonJson in holonData)
                                {
                                    var holon = new Holon
                                    {
                                        Id = Guid.Parse(holonJson.GetProperty("id").GetString() ?? Guid.Empty.ToString()),
                                        Name = holonJson.GetProperty("name").GetString() ?? "",
                                        Description = holonJson.GetProperty("description").GetString() ?? "",
                                        Version = version
                                    };
                                    holons.Add(holon);
                                }
                            }
                        }
                        response.Result = holons;
                        response.IsError = false;
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Export failed");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to export avatar data from NEAR: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error exporting avatar data from NEAR: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string username, int version = 0)
        {
            return ExportAllDataForAvatarByUsernameAsync(username, version).Result;
        }

        // Missing methods for avatar deletion
        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string username, bool softDelete = true)
        {
            var response = new OASISResult<bool>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate NEAR provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Delete avatar by username from NEAR smart contract
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = "dontcare",
                    method = "query",
                    @params = new
                    {
                        request_type = "call_function",
                        finality = "final",
                        account_id = "oasis.near",
                        method_name = "delete_avatar_by_username",
                        args_base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{{\"username\":\"{username}\",\"soft_delete\":{softDelete.ToString().ToLower()}}}"))
                    }
                };

                var jsonContent = JsonSerializer.Serialize(rpcRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    response.Result = true;
                    response.IsError = false;
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to delete avatar from NEAR: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error deleting avatar from NEAR: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<bool> DeleteAvatar(string username, bool softDelete = true)
        {
            return DeleteAvatarAsync(username, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string username, bool softDelete = true)
        {
            return await DeleteAvatarAsync(username, softDelete);
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string username, bool softDelete = true)
        {
            return DeleteAvatarByUsernameAsync(username, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string email, bool softDelete = true)
        {
            var response = new OASISResult<bool>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate NEAR provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Delete avatar by email from NEAR smart contract
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = "dontcare",
                    method = "query",
                    @params = new
                    {
                        request_type = "call_function",
                        finality = "final",
                        account_id = "oasis.near",
                        method_name = "delete_avatar_by_email",
                        args_base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{{\"email\":\"{email}\",\"soft_delete\":{softDelete.ToString().ToLower()}}}"))
                    }
                };

                var jsonContent = JsonSerializer.Serialize(rpcRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    response.Result = true;
                    response.IsError = false;
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to delete avatar from NEAR: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error deleting avatar from NEAR: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<bool> DeleteAvatarByEmail(string email, bool softDelete = true)
        {
            return DeleteAvatarByEmailAsync(email, softDelete).Result;
        }

        // Missing IOASISNETProvider methods
        public OASISResult<IEnumerable<IAvatar>> GetAvatarsNearMe(long x, long y, int radius)
        {
            var response = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = ActivateProviderAsync().GetAwaiter().GetResult();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate NEAR provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Get avatars near me from NEAR blockchain
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = "dontcare",
                    method = "query",
                    @params = new
                    {
                        request_type = "call_function",
                        finality = "final",
                        account_id = "oasis.near",
                        method_name = "get_avatars_near_me",
                        args_base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{{\"x\":{x},\"y\":{y},\"radius\":{radius}}}"))
                    }
                };

                var jsonContent = JsonSerializer.Serialize(rpcRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = _httpClient.PostAsync("", content).Result;

                if (httpResponse.IsSuccessStatusCode)
                {
                    var jsonResponse = httpResponse.Content.ReadAsStringAsync().Result;
                    var result = JsonSerializer.Deserialize<JsonElement>(jsonResponse);
                    
                    if (result.TryGetProperty("result", out var resultElement))
                    {
                        var avatars = new List<IAvatar>();
                        if (resultElement.TryGetProperty("result", out var dataElement))
                        {
                            var dataString = dataElement.GetString();
                            if (!string.IsNullOrEmpty(dataString))
                            {
                                var avatarData = JsonSerializer.Deserialize<List<JsonElement>>(dataString);
                                foreach (var avatarJson in avatarData)
                                {
                                    var avatar = new Avatar
                                    {
                                        Id = Guid.Parse(avatarJson.GetProperty("id").GetString() ?? Guid.Empty.ToString()),
                                        Username = avatarJson.GetProperty("username").GetString() ?? "",
                                        Email = avatarJson.GetProperty("email").GetString() ?? "",
                                        Version = 0
                                    };
                                    avatars.Add(avatar);
                                }
                            }
                        }
                        response.Result = avatars;
                        response.IsError = false;
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "No avatars found");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to get avatars from NEAR: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error getting avatars from NEAR: {ex.Message}");
            }
            return response;
        }

        public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(long x, long y, int radius, HolonType holonType)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = ActivateProviderAsync().GetAwaiter().GetResult();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate NEAR provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Get holons near me from NEAR blockchain
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = "dontcare",
                    method = "query",
                    @params = new
                    {
                        request_type = "call_function",
                        finality = "final",
                        account_id = "oasis.near",
                        method_name = "get_holons_near_me",
                        args_base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{{\"x\":{x},\"y\":{y},\"radius\":{radius},\"holon_type\":\"{holonType}\"}}"))
                    }
                };

                var jsonContent = JsonSerializer.Serialize(rpcRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = _httpClient.PostAsync("", content).Result;

                if (httpResponse.IsSuccessStatusCode)
                {
                    var jsonResponse = httpResponse.Content.ReadAsStringAsync().Result;
                    var result = JsonSerializer.Deserialize<JsonElement>(jsonResponse);
                    
                    if (result.TryGetProperty("result", out var resultElement))
                    {
                        var holons = new List<IHolon>();
                        if (resultElement.TryGetProperty("result", out var dataElement))
                        {
                            var dataString = dataElement.GetString();
                            if (!string.IsNullOrEmpty(dataString))
                            {
                                var holonData = JsonSerializer.Deserialize<List<JsonElement>>(dataString);
                                foreach (var holonJson in holonData)
                                {
                                    var holon = new Holon
                                    {
                                        Id = Guid.Parse(holonJson.GetProperty("id").GetString() ?? Guid.Empty.ToString()),
                                        Name = holonJson.GetProperty("name").GetString() ?? "",
                                        Description = holonJson.GetProperty("description").GetString() ?? "",
                                        Version = 0
                                    };
                                    holons.Add(holon);
                                }
                            }
                        }
                        response.Result = holons;
                        response.IsError = false;
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "No holons found");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to get holons from NEAR: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error getting holons from NEAR: {ex.Message}");
            }
            return response;
        }

        // Missing IOASISNFTProvider methods

    }
}
