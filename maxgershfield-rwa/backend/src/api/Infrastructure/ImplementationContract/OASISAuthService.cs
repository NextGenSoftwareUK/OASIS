namespace Infrastructure.ImplementationContract;

using Application.Contracts;
using Application.DTOs.Account.OASIS;
using Application.DTOs.Account.Requests;
using BuildingBlocks.Extensions.Http;
using BuildingBlocks.Extensions.Logger;
using Domain.Constants;
using System.Net.Http.Json;
using System.Text.Json;

/// <summary>
/// Service for OASIS Avatar API authentication and user management
/// </summary>
public sealed class OASISAuthService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<OASISAuthService> logger) : IOASISAuthService
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient(HttpClientNames.OASISClient);

    public async Task<OASISResult<AvatarAuthResponse>> RegisterAsync(RegisterRequest request)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(RegisterAsync), date);

        try
        {
            var registerPayload = new
            {
                username = request.UserName,
                email = request.EmailAddress,
                password = request.Password
            };

            string url = $"{_httpClient.BaseAddress}/api/avatar/register";
            
            using var response = await _httpClient.PostAsJsonAsync(url, registerPayload);
            string responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning($"OASIS registration failed: {response.StatusCode} - {responseContent}");
                logger.OperationCompleted(nameof(RegisterAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
                return new OASISResult<AvatarAuthResponse>
                {
                    IsError = true,
                    Message = $"Registration failed: {response.StatusCode}"
                };
            }

            // Parse OASIS API response structure
            // OASIS returns: { result: { Result: { jwtToken: "...", avatarId: "..." }, isError: false }, ... }
            var jsonDoc = JsonDocument.Parse(responseContent);
            var root = jsonDoc.RootElement;

            // Handle nested result structure
            JsonElement resultElement = root;
            if (root.TryGetProperty("result", out var resultProp))
            {
                resultElement = resultProp;
            }

            bool isError = resultElement.TryGetProperty("isError", out var isErrorProp) && isErrorProp.GetBoolean() ||
                          resultElement.TryGetProperty("IsError", out var isErrorProp2) && isErrorProp2.GetBoolean();

            if (isError)
            {
                string errorMsg = resultElement.TryGetProperty("message", out var msgProp) ? msgProp.GetString() ?? "Unknown error" :
                                 resultElement.TryGetProperty("Message", out var msgProp2) ? msgProp2.GetString() ?? "Unknown error" :
                                 "Registration failed";
                
                logger.LogWarning($"OASIS registration error: {errorMsg}");
                logger.OperationCompleted(nameof(RegisterAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
                return new OASISResult<AvatarAuthResponse>
                {
                    IsError = true,
                    Message = errorMsg
                };
            }

            // Extract Result (capital R) which contains the actual data
            JsonElement dataElement = resultElement;
            if (resultElement.TryGetProperty("Result", out var resultData))
            {
                dataElement = resultData;
            }
            else if (resultElement.TryGetProperty("result", out var resultData2))
            {
                dataElement = resultData2;
            }

            // Extract avatar data
            string jwtToken = dataElement.TryGetProperty("jwtToken", out var tokenProp) ? tokenProp.GetString() ?? "" :
                             dataElement.TryGetProperty("JwtToken", out var tokenProp2) ? tokenProp2.GetString() ?? "" : "";
            
            string avatarId = dataElement.TryGetProperty("avatarId", out var avatarIdProp) ? avatarIdProp.GetString() ?? "" :
                             dataElement.TryGetProperty("AvatarId", out var avatarIdProp2) ? avatarIdProp2.GetString() ?? "" :
                             dataElement.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? "" :
                             dataElement.TryGetProperty("Id", out var idProp2) ? idProp2.GetString() ?? "" : "";
            
            string username = dataElement.TryGetProperty("username", out var userProp) ? userProp.GetString() ?? "" :
                             dataElement.TryGetProperty("Username", out var userProp2) ? userProp2.GetString() ?? "" :
                             request.UserName;
            
            string email = dataElement.TryGetProperty("email", out var emailProp) ? emailProp.GetString() ?? "" :
                          dataElement.TryGetProperty("Email", out var emailProp2) ? emailProp2.GetString() ?? "" :
                          request.EmailAddress;

            string? firstName = dataElement.TryGetProperty("firstName", out var fnProp) ? fnProp.GetString() :
                               dataElement.TryGetProperty("FirstName", out var fnProp2) ? fnProp2.GetString() : null;

            string? lastName = dataElement.TryGetProperty("lastName", out var lnProp) ? lnProp.GetString() :
                              dataElement.TryGetProperty("LastName", out var lnProp2) ? lnProp2.GetString() : null;

            string? fullName = dataElement.TryGetProperty("fullName", out var fullProp) ? fullProp.GetString() :
                              dataElement.TryGetProperty("FullName", out var fullProp2) ? fullProp2.GetString() : null;

            if (string.IsNullOrEmpty(jwtToken))
            {
                logger.LogError($"OASIS registration succeeded but no token received. Response: {responseContent}");
                logger.OperationCompleted(nameof(RegisterAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
                return new OASISResult<AvatarAuthResponse>
                {
                    IsError = true,
                    Message = "Registration succeeded but no token received"
                };
            }

            var authResponse = new AvatarAuthResponse
            {
                JwtToken = jwtToken,
                AvatarId = avatarId,
                Username = username,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                FullName = fullName
            };

            logger.OperationCompleted(nameof(RegisterAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return new OASISResult<AvatarAuthResponse>
            {
                IsError = false,
                Result = authResponse
            };
        }
        catch (Exception ex)
        {
            logger.OperationException(nameof(RegisterAsync), ex.Message);
            logger.OperationCompleted(nameof(RegisterAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return new OASISResult<AvatarAuthResponse>
            {
                IsError = true,
                Message = $"Registration failed: {ex.Message}"
            };
        }
    }

    public async Task<OASISResult<AvatarAuthResponse>> LoginAsync(LoginRequest request)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(LoginAsync), date);

        try
        {
            var loginPayload = new
            {
                username = request.Email, // OASIS can use email as username
                password = request.Password
            };

            string url = $"{_httpClient.BaseAddress}/api/avatar/authenticate";
            
            using var response = await _httpClient.PostAsJsonAsync(url, loginPayload);
            string responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning($"OASIS authentication failed: {response.StatusCode} - {responseContent}");
                logger.OperationCompleted(nameof(LoginAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
                return new OASISResult<AvatarAuthResponse>
                {
                    IsError = true,
                    Message = "Invalid email or password"
                };
            }

            // Parse OASIS API response structure
            var jsonDoc = JsonDocument.Parse(responseContent);
            var root = jsonDoc.RootElement;

            JsonElement resultElement = root;
            if (root.TryGetProperty("result", out var resultProp))
            {
                resultElement = resultProp;
            }

            bool isError = resultElement.TryGetProperty("isError", out var isErrorProp) && isErrorProp.GetBoolean() ||
                          resultElement.TryGetProperty("IsError", out var isErrorProp2) && isErrorProp2.GetBoolean();

            if (isError)
            {
                string errorMsg = resultElement.TryGetProperty("message", out var msgProp) ? msgProp.GetString() ?? "Authentication failed" :
                                 resultElement.TryGetProperty("Message", out var msgProp2) ? msgProp2.GetString() ?? "Authentication failed" :
                                 "Invalid email or password";
                
                logger.LogWarning($"OASIS authentication error: {errorMsg}");
                logger.OperationCompleted(nameof(LoginAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
                return new OASISResult<AvatarAuthResponse>
                {
                    IsError = true,
                    Message = errorMsg
                };
            }

            // Extract Result (capital R) which contains the actual data
            JsonElement dataElement = resultElement;
            if (resultElement.TryGetProperty("Result", out var resultData))
            {
                dataElement = resultData;
            }
            else if (resultElement.TryGetProperty("result", out var resultData2))
            {
                dataElement = resultData2;
            }

            // Extract avatar data
            string jwtToken = dataElement.TryGetProperty("jwtToken", out var tokenProp) ? tokenProp.GetString() ?? "" :
                             dataElement.TryGetProperty("JwtToken", out var tokenProp2) ? tokenProp2.GetString() ?? "" : "";
            
            string avatarId = dataElement.TryGetProperty("avatarId", out var avatarIdProp) ? avatarIdProp.GetString() ?? "" :
                             dataElement.TryGetProperty("AvatarId", out var avatarIdProp2) ? avatarIdProp2.GetString() ?? "" :
                             dataElement.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? "" :
                             dataElement.TryGetProperty("Id", out var idProp2) ? idProp2.GetString() ?? "" : "";
            
            string username = dataElement.TryGetProperty("username", out var userProp) ? userProp.GetString() ?? "" :
                             dataElement.TryGetProperty("Username", out var userProp2) ? userProp2.GetString() ?? "" : "";
            
            string email = dataElement.TryGetProperty("email", out var emailProp) ? emailProp.GetString() ?? "" :
                          dataElement.TryGetProperty("Email", out var emailProp2) ? emailProp2.GetString() ?? "" :
                          request.Email;

            string? firstName = dataElement.TryGetProperty("firstName", out var fnProp) ? fnProp.GetString() :
                               dataElement.TryGetProperty("FirstName", out var fnProp2) ? fnProp2.GetString() : null;

            string? lastName = dataElement.TryGetProperty("lastName", out var lnProp) ? lnProp.GetString() :
                              dataElement.TryGetProperty("LastName", out var lnProp2) ? lnProp2.GetString() : null;

            string? fullName = dataElement.TryGetProperty("fullName", out var fullProp) ? fullProp.GetString() :
                              dataElement.TryGetProperty("FullName", out var fullProp2) ? fullProp2.GetString() : null;

            if (string.IsNullOrEmpty(jwtToken))
            {
                logger.LogError($"OASIS authentication succeeded but no token received. Response: {responseContent}");
                logger.OperationCompleted(nameof(LoginAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
                return new OASISResult<AvatarAuthResponse>
                {
                    IsError = true,
                    Message = "Authentication succeeded but no token received"
                };
            }

            var authResponse = new AvatarAuthResponse
            {
                JwtToken = jwtToken,
                AvatarId = avatarId,
                Username = username,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                FullName = fullName
            };

            logger.OperationCompleted(nameof(LoginAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return new OASISResult<AvatarAuthResponse>
            {
                IsError = false,
                Result = authResponse
            };
        }
        catch (Exception ex)
        {
            logger.OperationException(nameof(LoginAsync), ex.Message);
            logger.OperationCompleted(nameof(LoginAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return new OASISResult<AvatarAuthResponse>
            {
                IsError = true,
                Message = $"Login failed: {ex.Message}"
            };
        }
    }

    public async Task<OASISResult<Avatar>> GetAvatarAsync(string avatarId)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(GetAvatarAsync), date);

        try
        {
            string url = $"{_httpClient.BaseAddress}/api/avatar/{avatarId}";
            
            using var response = await _httpClient.GetAsync(url);
            string responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning($"Failed to get avatar {avatarId}: {response.StatusCode}");
                logger.OperationCompleted(nameof(GetAvatarAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
                return new OASISResult<Avatar>
                {
                    IsError = true,
                    Message = $"Failed to get avatar: {response.StatusCode}"
                };
            }

            var jsonDoc = JsonDocument.Parse(responseContent);
            var root = jsonDoc.RootElement;

            // Parse avatar data (similar structure handling as above)
            JsonElement avatarElement = root;
            if (root.TryGetProperty("result", out var resultProp))
            {
                if (resultProp.TryGetProperty("Result", out var resultData))
                {
                    avatarElement = resultData;
                }
                else
                {
                    avatarElement = resultProp;
                }
            }

            var avatar = new Avatar
            {
                AvatarId = avatarElement.TryGetProperty("avatarId", out var idProp) ? idProp.GetString() ?? avatarId :
                          avatarElement.TryGetProperty("AvatarId", out var idProp2) ? idProp2.GetString() ?? avatarId : avatarId,
                Username = avatarElement.TryGetProperty("username", out var userProp) ? userProp.GetString() ?? "" :
                          avatarElement.TryGetProperty("Username", out var userProp2) ? userProp2.GetString() ?? "" : "",
                Email = avatarElement.TryGetProperty("email", out var emailProp) ? emailProp.GetString() ?? "" :
                       avatarElement.TryGetProperty("Email", out var emailProp2) ? emailProp2.GetString() ?? "" : "",
                FirstName = avatarElement.TryGetProperty("firstName", out var fnProp) ? fnProp.GetString() :
                           avatarElement.TryGetProperty("FirstName", out var fnProp2) ? fnProp2.GetString() : null,
                LastName = avatarElement.TryGetProperty("lastName", out var lnProp) ? lnProp.GetString() :
                          avatarElement.TryGetProperty("LastName", out var lnProp2) ? lnProp2.GetString() : null,
                FullName = avatarElement.TryGetProperty("fullName", out var fullProp) ? fullProp.GetString() :
                          avatarElement.TryGetProperty("FullName", out var fullProp2) ? fullProp2.GetString() : null
            };

            logger.OperationCompleted(nameof(GetAvatarAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return new OASISResult<Avatar>
            {
                IsError = false,
                Result = avatar
            };
        }
        catch (Exception ex)
        {
            logger.OperationException(nameof(GetAvatarAsync), ex.Message);
            logger.OperationCompleted(nameof(GetAvatarAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return new OASISResult<Avatar>
            {
                IsError = true,
                Message = $"Failed to get avatar: {ex.Message}"
            };
        }
    }

    public async Task<OASISResult<bool>> SyncAvatarAsync(string avatarId)
    {
        // This would sync avatar data with local user database
        // For now, just return success - implementation can be added later
        await Task.CompletedTask;
        return new OASISResult<bool>
        {
            IsError = false,
            Result = true
        };
    }

    public async Task<OASISResult<string>> LinkWalletAsync(string avatarId, string walletAddress, string network)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(LinkWalletAsync), date);

        try
        {
            var updatePayload = new
            {
                avatarId = avatarId,
                walletAddress = walletAddress,
                network = network
            };

            string url = $"{_httpClient.BaseAddress}/api/avatar/update";
            
            using var response = await _httpClient.PostAsJsonAsync(url, updatePayload);
            string responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning($"Failed to link wallet: {response.StatusCode} - {responseContent}");
                logger.OperationCompleted(nameof(LinkWalletAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
                return new OASISResult<string>
                {
                    IsError = true,
                    Message = $"Failed to link wallet: {response.StatusCode}"
                };
            }

            logger.OperationCompleted(nameof(LinkWalletAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return new OASISResult<string>
            {
                IsError = false,
                Result = walletAddress
            };
        }
        catch (Exception ex)
        {
            logger.OperationException(nameof(LinkWalletAsync), ex.Message);
            logger.OperationCompleted(nameof(LinkWalletAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return new OASISResult<string>
            {
                IsError = true,
                Message = $"Failed to link wallet: {ex.Message}"
            };
        }
    }
}



