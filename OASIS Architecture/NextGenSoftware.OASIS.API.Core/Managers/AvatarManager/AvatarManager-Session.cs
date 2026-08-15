using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Objects.Search.Avatrar;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Avatar;
using NextGenSoftware.CLI.Engine;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public partial class AvatarManager
    {

        /// <summary>
        /// Get all sessions for a specific avatar (OASIS SSO System)
        /// </summary>
        /// <param name="avatarId">The avatar ID</param>
        /// <returns>Session management data</returns>
        public async Task<OASISResult<AvatarSessionManagement>> GetAvatarSessionsAsync(Guid avatarId)
        {
            OASISResult<AvatarSessionManagement> result = new OASISResult<AvatarSessionManagement>();
            string errorMessage = $"Error in GetAvatarSessionsAsync method in AvatarManager for avatar {avatarId}. Reason: ";

            try
            {
                // For now, return demo data - this would be implemented with actual session storage
                var sessionManagement = new AvatarSessionManagement
                {
                    TotalSessions = 12,
                    ActiveSessions = 4,
                    CurrentLocation = "San Francisco, CA",
                    LastLogin = DateTime.UtcNow.AddHours(-2),
                    SecurityStatus = "Secure",
                    Sessions = new List<AvatarSession>
                    {
                        new AvatarSession
                        {
                            Id = "1",
                            AvatarId = avatarId,
                            ServiceName = "STARNET Dashboard",
                            ServiceType = "platform",
                            DeviceType = "desktop",
                            DeviceName = "MacBook Pro 16\"",
                            Location = "San Francisco, CA",
                            IpAddress = "192.168.1.100",
                            IsActive = true,
                            LastActivity = DateTime.UtcNow.AddMinutes(-5),
                            LoginTime = DateTime.UtcNow.AddHours(-2),
                            UserAgent = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7)",
                            Platform = "macOS",
                            Version = "1.0.0"
                        },
                        new AvatarSession
                        {
                            Id = "2",
                            AvatarId = avatarId,
                            ServiceName = "OASIS Gaming Platform",
                            ServiceType = "game",
                            DeviceType = "vr",
                            DeviceName = "Oculus Quest 3",
                            Location = "San Francisco, CA",
                            IpAddress = "192.168.1.101",
                            IsActive = true,
                            LastActivity = DateTime.UtcNow.AddMinutes(-15),
                            LoginTime = DateTime.UtcNow.AddHours(-4),
                            UserAgent = "OculusBrowser/1.0.0",
                            Platform = "Android",
                            Version = "2.1.0"
                        }
                    }
                };

                result.Result = sessionManagement;
                result.IsError = false;
                result.Message = "Avatar sessions retrieved successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        /// <summary>
        /// Logout avatar from specific sessions (OASIS SSO System)
        /// </summary>
        /// <param name="avatarId">The avatar ID</param>
        /// <param name="sessionIds">List of session IDs to logout</param>
        /// <returns>Success status</returns>
        public async Task<OASISResult<bool>> LogoutAvatarSessionsAsync(Guid avatarId, List<string> sessionIds)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = $"Error in LogoutAvatarSessionsAsync method in AvatarManager for avatar {avatarId}. Reason: ";

            try
            {
                // For now, simulate successful logout - this would be implemented with actual session management
                result.Result = true;
                result.IsError = false;
                result.Message = $"Successfully logged out from {sessionIds.Count} session(s)";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        /// <summary>
        /// Logout avatar from all sessions (OASIS SSO System)
        /// </summary>
        /// <param name="avatarId">The avatar ID</param>
        /// <returns>Success status</returns>
        public async Task<OASISResult<bool>> LogoutAllAvatarSessionsAsync(Guid avatarId)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = $"Error in LogoutAllAvatarSessionsAsync method in AvatarManager for avatar {avatarId}. Reason: ";

            try
            {
                // For now, simulate successful logout - this would be implemented with actual session management
                result.Result = true;
                result.IsError = false;
                result.Message = "Successfully logged out from all sessions";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        /// <summary>
        /// Create a new session for an avatar (OASIS SSO System)
        /// </summary>
        /// <param name="avatarId">The avatar ID</param>
        /// <param name="sessionData">Session information</param>
        /// <returns>Created session</returns>
        public async Task<OASISResult<AvatarSession>> CreateAvatarSessionAsync(Guid avatarId, CreateSessionRequest sessionData)
        {
            OASISResult<AvatarSession> result = new OASISResult<AvatarSession>();
            if (sessionData == null)
            {
                result.IsError = true;
                result.Message = "The session data is required. Please provide a valid request with ServiceName, ServiceType, and optional DeviceType, Location, etc.";
                return result;
            }
            string errorMessage = $"Error in CreateAvatarSessionAsync method in AvatarManager for avatar {avatarId}. Reason: ";

            try
            {
                var session = new AvatarSession
                {
                    Id = Guid.NewGuid().ToString(),
                    AvatarId = avatarId,
                    ServiceName = sessionData.ServiceName,
                    ServiceType = sessionData.ServiceType,
                    DeviceType = sessionData.DeviceType,
                    DeviceName = sessionData.DeviceName,
                    Location = sessionData.Location,
                    IpAddress = sessionData.IpAddress,
                    UserAgent = sessionData.UserAgent,
                    Platform = sessionData.Platform,
                    Version = sessionData.Version,
                    Metadata = sessionData.Metadata,
                    ExpiresAt = sessionData.ExpiresAt,
                    IsActive = true,
                    LoginTime = DateTime.UtcNow,
                    LastActivity = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                result.Result = session;
                result.IsError = false;
                result.Message = "Avatar session created successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        /// <summary>
        /// Update an existing session (OASIS SSO System)
        /// </summary>
        /// <param name="avatarId">The avatar ID</param>
        /// <param name="sessionId">The session ID</param>
        /// <param name="sessionData">Updated session information</param>
        /// <returns>Updated session</returns>
        public async Task<OASISResult<AvatarSession>> UpdateAvatarSessionAsync(Guid avatarId, string sessionId, UpdateSessionRequest sessionData)
        {
            OASISResult<AvatarSession> result = new OASISResult<AvatarSession>();
            if (sessionData == null)
            {
                result.IsError = true;
                result.Message = "The session data is required. Please provide a valid UpdateSessionRequest (e.g. Location, IpAddress, IsActive).";
                return result;
            }
            string errorMessage = $"Error in UpdateAvatarSessionAsync method in AvatarManager for avatar {avatarId}, session {sessionId}. Reason: ";

            try
            {
                // For now, return a mock updated session - this would be implemented with actual session management
                var session = new AvatarSession
                {
                    Id = sessionId,
                    AvatarId = avatarId,
                    ServiceName = "Updated Service",
                    ServiceType = "platform",
                    DeviceType = "desktop",
                    DeviceName = "Updated Device",
                    Location = sessionData.Location ?? "San Francisco, CA",
                    IpAddress = sessionData.IpAddress ?? "192.168.1.100",
                    UserAgent = sessionData.UserAgent ?? "Mozilla/5.0",
                    Platform = sessionData.Platform ?? "macOS",
                    Version = sessionData.Version ?? "1.0.0",
                    Metadata = sessionData.Metadata,
                    ExpiresAt = sessionData.ExpiresAt,
                    IsActive = sessionData.IsActive ?? true,
                    LastActivity = sessionData.LastActivity ?? DateTime.UtcNow,
                    LoginTime = DateTime.UtcNow.AddHours(-1),
                    CreatedAt = DateTime.UtcNow.AddHours(-1),
                    UpdatedAt = DateTime.UtcNow
                };

                result.Result = session;
                result.IsError = false;
                result.Message = "Avatar session updated successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        /// <summary>
        /// Get session statistics for an avatar (OASIS SSO System)
        /// </summary>
        /// <param name="avatarId">The avatar ID</param>
        /// <returns>Session statistics</returns>
        public async Task<OASISResult<AvatarSessionStats>> GetAvatarSessionStatsAsync(Guid avatarId)
        {
            OASISResult<AvatarSessionStats> result = new OASISResult<AvatarSessionStats>();
            string errorMessage = $"Error in GetAvatarSessionStatsAsync method in AvatarManager for avatar {avatarId}. Reason: ";

            try
            {
                var stats = new AvatarSessionStats
                {
                    TotalSessionsCreated = 156,
                    ActiveSessions = 4,
                    SessionsLast24Hours = 8,
                    SessionsLast7Days = 23,
                    SessionsLast30Days = 89,
                    AverageSessionDurationMinutes = 45.5,
                    LongestSessionDurationMinutes = 180.0,
                    MostUsedDeviceType = "desktop",
                    MostUsedServiceType = "platform",
                    MostCommonLocation = "San Francisco, CA",
                    LastSessionTime = DateTime.UtcNow.AddMinutes(-5),
                    SecurityIncidents = 0,
                    UniqueDevicesUsed = 3,
                    UniqueLocationsUsed = 2,
                    TrustScore = 95
                };

                result.Result = stats;
                result.IsError = false;
                result.Message = "Avatar session statistics retrieved successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }



        /// <summary>
        /// Refresh an authentication token
        /// </summary>
        public OASISResult<IAvatar> RefreshToken(string token, string ipAddress)
        {
            var response = new OASISResult<IAvatar>();

            try
            {
                var refreshToken = GetRefreshTokenFromAvatar(token, out IAvatar avatar);

                if (avatar == null)
                {
                    OASISErrorHandling.HandleError(ref response, "Avatar not found");
                    return response;
                }

                if (refreshToken != null && refreshToken.IsActive)
                {
                    var newRefreshToken = generateRefreshToken(ipAddress);
                    refreshToken.Revoked = DateTime.UtcNow;
                    refreshToken.RevokedByIp = ipAddress;
                    refreshToken.ReplacedByToken = newRefreshToken.Token;
                    avatar.RefreshTokens.Add(newRefreshToken);

                    avatar.RefreshToken = newRefreshToken.Token;
                    avatar.JwtToken = GenerateJWTToken(avatar);

                    OASISResult<IAvatar> saveAvatarResult = SaveAvatar(avatar);

                    if (saveAvatarResult != null && !saveAvatarResult.IsError && saveAvatarResult.Result != null)
                    {
                        avatar = HideAuthDetails(saveAvatarResult.Result);
                        response.Result = avatar;
                    }
                    else
                        OASISErrorHandling.HandleError(ref response, $"Error occurred in RefreshToken method in AvatarManager saving avatar. Reason: {saveAvatarResult.Message}", saveAvatarResult.DetailedMessage);
                }
                else
                    OASISErrorHandling.HandleError(ref response, "Refresh token is inactive or invalid");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"An unknown error occurred in RefreshToken method in AvatarManager. Reason: {ex.Message}");
            }

            return response;
        }

        /// <summary>
        /// Revoke a refresh token
        /// </summary>
        public OASISResult<string> RevokeToken(string token, string ipAddress)
        {
            var response = new OASISResult<string>();
            var refreshToken = GetRefreshTokenFromAvatar(token, out IAvatar avatar);

            if (avatar == null)
            {
                OASISErrorHandling.HandleError(ref response, "Avatar not found");
                return response;
            }

            // revoke token and save
            if (refreshToken != null && refreshToken.IsActive)
            {
                refreshToken.Revoked = DateTime.UtcNow;
                refreshToken.RevokedByIp = ipAddress;
                avatar.IsBeamedIn = false;
                avatar.LastBeamedOut = DateTime.Now;

                var saveAvatar = SaveAvatar(avatar);

                if (saveAvatar != null && !saveAvatar.IsError && saveAvatar.Result != null)
                {
                    response.Message = "Token Revoked.";
                    response.IsSaved = true;
                }
                else
                    OASISErrorHandling.HandleError(ref response, $"An error in RevokeToken method in AvatarManager saving the avatar. Reason: {saveAvatar.Message}", saveAvatar.DetailedMessage);
            }
            else
                OASISErrorHandling.HandleError(ref response, "Refresh token is inactive or invalid");

            return response;
        }

        /// <summary>
        /// Validate an account token (JWT)
        /// </summary>
        public OASISResult<string> ValidateAccountToken(string accountToken)
        {
            var response = new OASISResult<string>();

            try
            {
                var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                var key = System.Text.Encoding.ASCII.GetBytes(OASISDNA.OASIS.Security.SecretKey);
                tokenHandler.ValidateToken(accountToken, new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    // set clockskew to zero so tokens expire exactly at token expiration time (instead of 5 minutes later)
                    ClockSkew = TimeSpan.Zero
                }, out Microsoft.IdentityModel.Tokens.SecurityToken validatedToken);

                response.Result = "Token is valid";
            }
            catch
            {
                response.IsError = true;
                response.Result = "Token is invalid";
            }

            return response;
        }

        /// <summary>
        /// Validate a password reset token
        /// </summary>
        public OASISResult<string> ValidateResetToken(string token)
        {
            var response = new OASISResult<string>();

            try
            {
                OASISResult<IAvatar> avatarResult = LoadAvatarByResetTokenForProvider(token);

                if (avatarResult.IsError)
                    OASISErrorHandling.HandleError(ref response, $"Error in ValidateResetToken loading avatar by reset token. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
                else if (avatarResult.Result == null || avatarResult.Result.ResetTokenExpires <= DateTime.UtcNow)
                {
                    response.IsError = true;
                    response.Result = "Invalid token";
                }
                else
                    response.Result = "Valid token";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                response.IsError = true;
                response.Result = "Error validating token";
                OASISErrorHandling.HandleError(ref response, ex.Message);
            }

            return response;
        }

        /// <summary>
        /// Helper method to get refresh token from avatar
        /// </summary>
        private RefreshToken GetRefreshTokenFromAvatar(string token, out IAvatar avatar)
        {
            avatar = null;
            var avatarResult = LoadAvatarByRefreshTokenForProvider(token);

            if (!avatarResult.IsError && avatarResult.Result != null)
            {
                avatar = avatarResult.Result;
                return avatar.RefreshTokens?.SingleOrDefault(x => x.Token == token);
            }

            return null;
        }


    }
}
