using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Exceptions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace NextGenSoftware.OASIS.STAR.WebAPI.Controllers
{
    public class STARControllerBase : ControllerBase
    {
        public IAvatar Avatar
        {
            get
            {
                if (HttpContext.Items.ContainsKey("Avatar") && HttpContext.Items["Avatar"] != null)
                {
                    IAvatar avatar = null;
                    if (HttpContext.Items["Avatar"] is IAvatar av)
                        avatar = av;
                    else if (HttpContext.Items["Avatar"] is OASISResult<IAvatar> avatarResult && avatarResult.Result is not null)
                        avatar = avatarResult.Result;

                    // Set AvatarManager.LoggedInAvatar so SaveAsync() methods can access the AvatarId
                    // This is required because SaveAsync() uses AvatarManager.LoggedInAvatar.AvatarId internally
                    if (avatar != null && (AvatarManager.LoggedInAvatar == null || AvatarManager.LoggedInAvatar.Id != avatar.Id))
                    {
                        AvatarManager.LoggedInAvatar = avatar;
                    }

                    return avatar;
                }

                return null;
            }
            set
            {
                HttpContext.Items["Avatar"] = value;
                // Set AvatarManager.LoggedInAvatar when Avatar is set
                // This is required because SaveAsync() methods use AvatarManager.LoggedInAvatar.AvatarId internally
                if (value != null)
                {
                    AvatarManager.LoggedInAvatar = value;
                }
            }
        }

        public Guid AvatarId
        {
            get
            {
                if (Avatar != null && Avatar.Id != Guid.Empty)
                    return Avatar.Id;

                Guid avatarId = Guid.Empty;

                if (Request.Headers.TryGetValue("X-Avatar-Id", out var avatarHeader) &&
                    Guid.TryParse(avatarHeader.ToString(), out var headerAvatarId) &&
                    headerAvatarId != Guid.Empty)
                {
                    avatarId = headerAvatarId;
                }
                else
                {
                    avatarId = ResolveAvatarIdFromBearerToken();
                }

                // If we have an AvatarId but Avatar is not loaded, create a minimal avatar
                // and set AvatarManager.LoggedInAvatar so SaveAsync() methods can access it
                if (avatarId != Guid.Empty && Avatar == null)
                {
                    var minimalAvatar = new NextGenSoftware.OASIS.API.Core.Holons.Avatar { Id = avatarId };
                    AvatarManager.LoggedInAvatar = minimalAvatar;
                }

                return avatarId;
            }
        }

        public STARControllerBase()
        {
        }
        
        /// <summary>
        /// Gets whether generic exception handling is enabled (default: true).
        /// Set ENABLE_GENERIC_EXCEPTION_HANDLING=false to disable in dev/test mode.
        /// </summary>
        protected bool EnableGenericExceptionHandling
        {
            get
            {
                var config = HttpContext.RequestServices.GetService<Microsoft.Extensions.Configuration.IConfiguration>();
                if (config == null)
                    return true; // Default to enabled
                    
                return config.GetValue<bool>("EnableGenericExceptionHandling", 
                    bool.Parse(Environment.GetEnvironmentVariable("ENABLE_GENERIC_EXCEPTION_HANDLING") ?? "true"));
            }
        }

        /// <summary>
        /// Gets whether to use test data when live data is not available (default: false).
        /// Set OASIS:UseTestDataWhenLiveDataNotAvailable=true in appsettings.json or 
        /// USE_TEST_DATA_WHEN_LIVE_DATA_NOT_AVAILABLE=true as environment variable to enable.
        /// </summary>
        protected bool UseTestDataWhenLiveDataNotAvailable
        {
            get
            {
                var config = HttpContext.RequestServices.GetService<Microsoft.Extensions.Configuration.IConfiguration>();
                if (config == null)
                    return false; // Default to disabled
                    
                return config.GetValue<bool>("OASIS:UseTestDataWhenLiveDataNotAvailable", 
                    bool.Parse(Environment.GetEnvironmentVariable("USE_TEST_DATA_WHEN_LIVE_DATA_NOT_AVAILABLE") ?? "false"));
            }
        }
        
        /// <summary>
        /// Handles exceptions with proper logging and error response.
        /// - Validation errors (OASISException, missing args) → 400 BadRequest
        /// - Real exceptions → 500 InternalServerError
        /// - If ENABLE_GENERIC_EXCEPTION_HANDLING is ON: Returns friendly messages
        /// - If ENABLE_GENERIC_EXCEPTION_HANDLING is OFF: Returns raw error details
        /// </summary>
        protected IActionResult HandleException<T>(Exception ex, string operationName)
        {
            // Always log the error first
            OASISErrorHandling.HandleError($"Error in {operationName}: {ex.Message}", ex, includeStackTrace: true);
            
            // Check if this is a validation error (400) or real exception (500)
            bool isValidationError = ex is OASISException ||
                                    ex.Message.Contains("required", StringComparison.OrdinalIgnoreCase) ||
                                    ex.Message.Contains("missing", StringComparison.OrdinalIgnoreCase) ||
                                    ex.Message.Contains("invalid", StringComparison.OrdinalIgnoreCase) ||
                                    ex.Message.Contains("cannot be null", StringComparison.OrdinalIgnoreCase) ||
                                    ex.Message.Contains("AvatarId is required", StringComparison.OrdinalIgnoreCase);
            
            if (isValidationError)
            {
                // Validation error - return 400
                var errorResult = new OASISResult<T>
                {
                    IsError = true,
                    Exception = ex
                };
                
                if (EnableGenericExceptionHandling)
                {
                    // Friendly message for validation errors
                    errorResult.Message = $"Invalid args were passed to {operationName}. {ex.Message}";
                }
                else
                {
                    // Raw error details in dev/test mode
                    errorResult.Message = ex.Message;
                    errorResult.DetailedMessage = ex.ToString();
                }
                
                return BadRequest(errorResult);
            }
            else
            {
                // Real exception - return 500
                var errorResult = new OASISResult<T>
                {
                    IsError = true,
                    Exception = ex
                };
                
                if (EnableGenericExceptionHandling)
                {
                    // Friendly message for production
                    errorResult.Message = "Oooops. Sorry something broke, it has been logged and we are looking into it!";
                    // Don't expose internal details to client in production
                }
                else
                {
                    // Raw error details in dev/test mode
                    errorResult.Message = $"Unexpected error in {operationName}: {ex.Message}";
                    errorResult.DetailedMessage = ex.ToString();
                }
                
                return StatusCode(500, errorResult);
            }
        }

        /// <summary>
        /// Validates and parses a HolonType enum value. Returns a validation error with valid values if parsing fails.
        /// </summary>
        /// <typeparam name="T">The return type for the error response.</typeparam>
        /// <param name="value">The string value to parse.</param>
        /// <param name="parameterName">The name of the parameter being validated (for error message).</param>
        /// <returns>Tuple containing the parsed HolonType and an optional error IActionResult. If error is not null, parsing failed.</returns>
        protected (HolonType holonType, IActionResult? error) ValidateAndParseHolonType<T>(string value, string parameterName = "holonType")
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return (HolonType.Default, null);
            }

            // Try case-insensitive parsing
            if (Enum.TryParse<HolonType>(value, true, out var result))
            {
                return (result, null);
            }

            // If parsing fails, return validation error with list of valid values
            var validValues = EnumHelper.GetEnumValues(typeof(HolonType), EnumHelperListType.ItemsSeperatedByComma);
            var errorResult = BadRequest(new OASISResult<T>
            {
                IsError = true,
                Message = $"The {parameterName} '{value}' is not valid. It must be one of the following values: {validValues}"
            });

            return (HolonType.Default, errorResult);
        }

        /// <summary>
        /// Validates and parses any enum value. Returns a validation error with valid values if parsing fails.
        /// </summary>
        /// <typeparam name="TEnum">The enum type to parse.</typeparam>
        /// <typeparam name="T">The return type for the error response.</typeparam>
        /// <param name="value">The string value to parse.</param>
        /// <param name="parameterName">The name of the parameter being validated (for error message).</param>
        /// <returns>Tuple containing the parsed enum value and an optional error IActionResult. If error is not null, parsing failed.</returns>
        protected (TEnum enumValue, IActionResult? error) ValidateAndParseEnum<TEnum, T>(string value, string parameterName) where TEnum : struct, Enum
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return (default(TEnum), null);
            }

            // Try case-insensitive parsing
            if (Enum.TryParse<TEnum>(value, true, out var result))
            {
                return (result, null);
            }

            // If parsing fails, return validation error with list of valid values
            var validValues = EnumHelper.GetEnumValues(typeof(TEnum), EnumHelperListType.ItemsSeperatedByComma);
            var errorResult = BadRequest(new OASISResult<T>
            {
                IsError = true,
                Message = $"The {parameterName} '{value}' is not valid. It must be one of the following values: {validValues}"
            });

            return (default(TEnum), errorResult);
        }

        /// <summary>
        /// Ensures AvatarManager.LoggedInAvatar is set before calling manager methods that use SaveAsync().
        /// This is critical because SaveAsync() methods use AvatarManager.LoggedInAvatar.AvatarId internally.
        /// </summary>
        protected void EnsureLoggedInAvatar()
        {
            // If AvatarManager.LoggedInAvatar is already set and matches our Avatar, we're good
            if (AvatarManager.LoggedInAvatar != null && Avatar != null && 
                AvatarManager.LoggedInAvatar.Id == Avatar.Id && Avatar.Id != Guid.Empty)
            {
                return;
            }

            // Try to get Avatar from HttpContext first
            if (Avatar != null && Avatar.Id != Guid.Empty)
            {
                AvatarManager.LoggedInAvatar = Avatar;
                return;
            }

            // If Avatar is not set, try to get AvatarId and create minimal avatar
            var avatarId = AvatarId;
            if (avatarId != Guid.Empty)
            {
                // Create minimal avatar if we don't have one
                if (Avatar == null)
                {
                    Avatar = new NextGenSoftware.OASIS.API.Core.Holons.Avatar { Id = avatarId };
                }
                AvatarManager.LoggedInAvatar = Avatar;
            }
        }

        private Guid ResolveAvatarIdFromBearerToken()
        {
            try
            {
                if (!Request.Headers.TryGetValue("Authorization", out var authHeaderValue))
                    return Guid.Empty;

                var authHeader = authHeaderValue.ToString();
                if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    return Guid.Empty;

                var token = authHeader["Bearer ".Length..].Trim();
                if (string.IsNullOrWhiteSpace(token))
                    return Guid.Empty;

                var parts = token.Split('.');
                if (parts.Length < 2)
                    return Guid.Empty;

                var payload = parts[1].Replace('-', '+').Replace('_', '/');
                switch (payload.Length % 4)
                {
                    case 2: payload += "=="; break;
                    case 3: payload += "="; break;
                }

                var bytes = Convert.FromBase64String(payload);
                using var doc = JsonDocument.Parse(bytes);
                if (doc.RootElement.ValueKind != JsonValueKind.Object)
                    return Guid.Empty;

                foreach (var property in doc.RootElement.EnumerateObject())
                {
                    if ((string.Equals(property.Name, "id", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(property.Name, "avatarId", StringComparison.OrdinalIgnoreCase)) &&
                        property.Value.ValueKind == JsonValueKind.String &&
                        Guid.TryParse(property.Value.GetString(), out var parsed))
                    {
                        return parsed;
                    }
                }
            }
            catch
            {
            }

            return Guid.Empty;
        }
    }
}

