using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core;
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

                    return avatar;
                }

                return null;
            }
            set
            {
                HttpContext.Items["Avatar"] = value;
                if (value != null)
                {
                    OASISRequestContext.CurrentAvatarId = value.Id;
                    OASISRequestContext.CurrentAvatar = value;
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

                if (avatarId != Guid.Empty && Avatar == null)
                {
                    OASISRequestContext.CurrentAvatarId = avatarId;
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
            
            // Check if this is a validation/client error (400) or real server bug (500)
            bool isValidationError = ex is OASISException ||
                                    ex is ArgumentException ||
                                    ex is ArgumentNullException ||
                                    ex.Message.Contains("required", StringComparison.OrdinalIgnoreCase) ||
                                    ex.Message.Contains("missing", StringComparison.OrdinalIgnoreCase) ||
                                    ex.Message.Contains("invalid", StringComparison.OrdinalIgnoreCase) ||
                                    ex.Message.Contains("cannot be null", StringComparison.OrdinalIgnoreCase) ||
                                    ex.Message.Contains("AvatarId is required", StringComparison.OrdinalIgnoreCase) ||
                                    ex.Message.Contains("not valid", StringComparison.OrdinalIgnoreCase) ||
                                    ex.Message.Contains("must be one of", StringComparison.OrdinalIgnoreCase);
            
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
        /// Validates create-style request: Name is required, Description is optional. Returns BadRequest with friendly message if invalid.
        /// Request body null should be checked by the controller before calling this (e.g. if (request == null) return BadRequest(...)).
        /// </summary>
        protected IActionResult? ValidateCreateRequest(string? name, string? description = null, bool requireName = true)
        {
            if (requireName && string.IsNullOrWhiteSpace(name))
                return BadRequest(new OASISResult<object> { IsError = true, Message = "The Name field is required. Please provide a non-empty Name." });
            return null;
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
        /// Ensures request-scoped avatar is set (OASISRequestContext) before calling manager methods that use SaveAsync().
        /// SaveAsync() resolves the current avatar via AvatarManager.LoggedInAvatar, which reads from OASISRequestContext first.
        /// </summary>
        protected void EnsureLoggedInAvatar()
        {
            if (OASISRequestContext.CurrentAvatarId.HasValue && OASISRequestContext.CurrentAvatarId.Value != Guid.Empty)
                return;
            if (Avatar != null && Avatar.Id != Guid.Empty)
            {
                OASISRequestContext.CurrentAvatarId = Avatar.Id;
                OASISRequestContext.CurrentAvatar = Avatar;
                return;
            }
            var avatarId = AvatarId;
            if (avatarId != Guid.Empty)
            {
                OASISRequestContext.CurrentAvatarId = avatarId;
                if (Avatar == null)
                    Avatar = new NextGenSoftware.OASIS.API.Core.Holons.Avatar { Id = avatarId };
                OASISRequestContext.CurrentAvatar = Avatar;
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

