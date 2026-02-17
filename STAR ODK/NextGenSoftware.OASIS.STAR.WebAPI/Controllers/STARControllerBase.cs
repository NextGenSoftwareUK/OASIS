using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Exceptions;
using NextGenSoftware.OASIS.Common;
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
                    if (HttpContext.Items["Avatar"] is IAvatar avatar)
                        return avatar;

                    if (HttpContext.Items["Avatar"] is OASISResult<IAvatar> avatarResult && avatarResult.Result is not null)
                        return avatarResult.Result;
                }

                return null;
            }
            set
            {
                HttpContext.Items["Avatar"] = value;
            }
        }

        public Guid AvatarId
        {
            get
            {
                if (Avatar != null && Avatar.Id != Guid.Empty)
                    return Avatar.Id;

                if (Request.Headers.TryGetValue("X-Avatar-Id", out var avatarHeader) &&
                    Guid.TryParse(avatarHeader.ToString(), out var headerAvatarId) &&
                    headerAvatarId != Guid.Empty)
                {
                    return headerAvatarId;
                }

                return ResolveAvatarIdFromBearerToken();
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

