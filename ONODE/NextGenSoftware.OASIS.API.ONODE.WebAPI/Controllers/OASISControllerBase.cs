using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Exceptions;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using Microsoft.Extensions.Configuration;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    public class OASISControllerBase : ControllerBase
    {
        public IAvatar Avatar
        {
            get
            {
                //AvatarManager.LoggedInAvatarSessions[HttpContext.Session.Id].Id

                if (HttpContext.Items.ContainsKey("Avatar") && HttpContext.Items["Avatar"] != null)
                    return (IAvatar)HttpContext.Items["Avatar"];

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
                return Avatar != null ? Avatar.Id : Guid.Empty;
            }
        }

        public OASISControllerBase()
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
                var config = HttpContext.RequestServices.GetService(typeof(Microsoft.Extensions.Configuration.IConfiguration)) as Microsoft.Extensions.Configuration.IConfiguration;
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
                var config = HttpContext.RequestServices.GetService(typeof(Microsoft.Extensions.Configuration.IConfiguration)) as Microsoft.Extensions.Configuration.IConfiguration;
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
        /// Handles exceptions with proper logging and error response for WEB4 API (returns OASISHttpResponseMessage).
        /// - Validation errors (OASISException, missing args) → 400 BadRequest
        /// - Real exceptions → 500 InternalServerError
        /// - If ENABLE_GENERIC_EXCEPTION_HANDLING is ON: Returns friendly messages
        /// - If ENABLE_GENERIC_EXCEPTION_HANDLING is OFF: Returns raw error details
        /// </summary>
        protected OASISHttpResponseMessage<T> HandleExceptionForWeb4<T>(Exception ex, string operationName)
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
            
            var errorResult = new OASISResult<T>
            {
                IsError = true,
                Exception = ex
            };
            
            if (isValidationError)
            {
                // Validation error - return 400
                if (EnableGenericExceptionHandling)
                {
                    errorResult.Message = $"Invalid args were passed to {operationName}. {ex.Message}";
                }
                else
                {
                    errorResult.Message = ex.Message;
                    errorResult.DetailedMessage = ex.ToString();
                }
                
                return HttpResponseHelper.FormatResponse(errorResult, HttpStatusCode.BadRequest);
            }
            else
            {
                // Real exception - return 500
                if (EnableGenericExceptionHandling)
                {
                    errorResult.Message = "Oooops. Sorry something broke, it has been logged and we are looking into it!";
                }
                else
                {
                    errorResult.Message = $"Unexpected error in {operationName}: {ex.Message}";
                    errorResult.DetailedMessage = ex.ToString();
                }
                
                return HttpResponseHelper.FormatResponse(errorResult, HttpStatusCode.InternalServerError);
            }
        }

        protected OASISResult<IOASISStorageProvider> GetAndActivateDefaultStorageProvider()
        {
            OASISResult<IOASISStorageProvider> result = OASISBootLoader.OASISBootLoader.GetAndActivateDefaultStorageProvider();

            if (result.IsError)
                OASISErrorHandling.HandleError(ref result, string.Concat("Error calling OASISDNAManager.GetAndActivateDefaultStorageProvider(). Error details: ", result.Message));

            return result;
        }

        protected async Task<OASISResult<IOASISStorageProvider>> GetAndActivateDefaultStorageProviderAsync()
        {
            OASISResult<IOASISStorageProvider> result = await OASISBootLoader.OASISBootLoader.GetAndActivateDefaultStorageProviderAsync();

            if (result.IsError)
                OASISErrorHandling.HandleError(ref result, string.Concat("Error calling OASISDNAManager.GetAndActivateDefaultStorageProvider(). Error details: ", result.Message));

            return result;
        }

        protected OASISResult<IOASISStorageProvider> GetAndActivateProvider(ProviderType providerType, bool setGlobally = false)
        {
            return OASISBootLoader.OASISBootLoader.GetAndActivateStorageProvider(providerType, null, false, setGlobally);
        }

        protected async Task<OASISResult<IOASISStorageProvider>> GetAndActivateProviderAsync(ProviderType providerType, bool setGlobally = false)
        {
            return await OASISBootLoader.OASISBootLoader.GetAndActivateStorageProviderAsync(providerType, null, false, setGlobally);
        }

        protected OASISResult<IOASISStorageProvider> GetAndActivateProvider(ProviderType providerType, string customConnectionString = null, bool forceRegister = false, bool setGlobally = false)
        {
            return OASISBootLoader.OASISBootLoader.GetAndActivateStorageProvider(providerType, customConnectionString, forceRegister, setGlobally);
        }

        protected async Task<OASISResult<IOASISStorageProvider>> GetAndActivateProviderAsync(ProviderType providerType, string customConnectionString = null, bool forceRegister = false, bool setGlobally = false)
        {
            return await OASISBootLoader.OASISBootLoader.GetAndActivateStorageProviderAsync(providerType, customConnectionString, forceRegister, setGlobally);
        }

        protected OASISConfigResult<T> ConfigureOASISEngine<T>(OASISRequest request)
        {
            (OASISConfigResult<T> result, ProviderType providerTypeOverride) = ConfigureOASISEngineInternal<T>(request);

            if (providerTypeOverride != ProviderType.Default && providerTypeOverride != ProviderType.None)
                GetAndActivateProvider(providerTypeOverride, request.SetGlobally);

            return result;
        }

        protected async Task<OASISConfigResult<T>> ConfigureOASISEngineAsync<T>(OASISRequest request)
        {
            (OASISConfigResult<T> result, ProviderType providerTypeOverride) = ConfigureOASISEngineInternal<T>(request);

            if (providerTypeOverride != ProviderType.Default && providerTypeOverride != ProviderType.None)
                await GetAndActivateProviderAsync(providerTypeOverride, request.SetGlobally);

            return result;
        }

        protected bool ResetOASISSettings<T>(OASISRequest request, OASISConfigResult<T> result)
        {
            if (!request.SetGlobally)
                ProviderManager.Instance.IsAutoFailOverEnabled = result.PreviousAutoFailOverEnabled;

            if (!request.SetGlobally)
                ProviderManager.Instance.IsAutoReplicationEnabled = result.PreviousAutoReplicationEnabled;

            if (!request.SetGlobally)
                ProviderManager.Instance.IsAutoLoadBalanceEnabled = result.PreviousAutoLoadBalanaceEnabled;


            if (result.PreviousAutoReplicationList != null && !request.SetGlobally)
                ProviderManager.Instance.SetAndReplaceAutoReplicationListForProviders(result.PreviousAutoReplicationList);

            if (result.PreviousAutoFailOverList != null && !request.SetGlobally)
                ProviderManager.Instance.SetAndReplaceAutoFailOverListForProviders(result.PreviousAutoFailOverList);

            if (result.PreviousAutoLoadBalanaceList != null && !request.SetGlobally)
                ProviderManager.Instance.SetAndReplaceAutoLoadBalanceListForProviders(result.PreviousAutoLoadBalanaceList);

            return true;
        }

        private (OASISConfigResult<T>, ProviderType) ConfigureOASISEngineInternal<T>(OASISRequest request)
        {
            OASISConfigResult<T> result = new OASISConfigResult<T>();
            object providerTypeObject = null;
            ProviderType providerTypeOverride = ProviderType.Default;

            if (!string.IsNullOrEmpty(request.AutoReplicationMode) && request.AutoReplicationMode.ToUpper() != "ON" && request.AutoReplicationMode.ToUpper() != "OFF" && request.AutoReplicationMode.ToUpper() != "DEFAULT")
            {
                result.IsError = true;
                result.Response = HttpResponseHelper.FormatResponse(new OASISResult<T>() { IsError = true, Message = $"AutoReplicationMode must be either 'ON', 'OFF' or 'DEFAULT' but found {request.AutoReplicationMode}" });
                return (result, ProviderType.None);
            }

            if (!string.IsNullOrEmpty(request.AutoFailOverMode) && request.AutoFailOverMode.ToUpper() != "ON" && request.AutoFailOverMode.ToUpper() != "OFF" && request.AutoFailOverMode.ToUpper() != "DEFAULT")
            {
                result.IsError = true;
                result.Response = HttpResponseHelper.FormatResponse(new OASISResult<T>() { IsError = true, Message = $"AutoFailOverMode must be either 'ON', 'OFF' or 'DEFAULT' but found {request.AutoFailOverMode}" });
                return (result, ProviderType.None);
            }

            if (!string.IsNullOrEmpty(request.AutoLoadBalanceMode) && request.AutoLoadBalanceMode.ToUpper() != "ON" && request.AutoLoadBalanceMode.ToUpper() != "OFF" && request.AutoLoadBalanceMode.ToUpper() != "DEFAULT")
            {
                result.IsError = true;
                result.Response = HttpResponseHelper.FormatResponse(new OASISResult<T>() { IsError = true, Message = $"AutoLoadBalanceMode must be either 'ON', 'OFF' or 'DEFAULT' but found {request.AutoReplicationMode}" });
                return (result, ProviderType.None);
            }

            if (!string.IsNullOrEmpty(request.ProviderType) && !Enum.TryParse(typeof(ProviderType), request.ProviderType, out providerTypeObject))
            {
                result.Response = HttpResponseHelper.FormatResponse(new OASISResult<T>() { Message = $"The ProviderType {request.ProviderType} passed in is invalid. It must be one of the following types: {EnumHelper.GetEnumValues(typeof(ProviderType), EnumHelperListType.ItemsSeperatedByComma)}.", IsError = true }, HttpStatusCode.BadRequest);
                return (result, ProviderType.None);
            }

            if (!string.IsNullOrEmpty(request.AutoReplicationProviders))
            {
                if (request.AutoReplicationProviders.ToUpper() != "DEFAULT")
                {
                    OASISResult<IEnumerable<ProviderType>> listResult = ProviderManager.Instance.GetProvidersFromList("AutoReplication", request.AutoReplicationProviders);

                    if (listResult.WarningCount > 0)
                    {
                        result.IsError = true;
                        result.Response = HttpResponseHelper.FormatResponse(new OASISResult<T>() { Message = listResult.Message }, HttpStatusCode.BadRequest);
                        return (result, ProviderType.None);
                    }
                }
            }

            if (!string.IsNullOrEmpty(request.AutoFailOverProviders))
            {
                if (request.AutoFailOverProviders.ToUpper() != "DEFAULT")
                {
                    OASISResult<IEnumerable<ProviderType>> listResult = ProviderManager.Instance.GetProvidersFromList("AutoFailOver", request.AutoFailOverProviders);

                    if (listResult.WarningCount > 0)
                    {
                        result.IsError = true;
                        result.Response = HttpResponseHelper.FormatResponse(new OASISResult<T>() { Message = listResult.Message }, HttpStatusCode.BadRequest);
                        return (result, ProviderType.None);
                    }
                }
            }

            if (!string.IsNullOrEmpty(request.AutoLoadBalanceProviders))
            {
                if (request.AutoLoadBalanceProviders.ToUpper() != "DEFAULT")
                {
                    OASISResult<IEnumerable<ProviderType>> listResult = ProviderManager.Instance.GetProvidersFromList("AutoLoadBalance", request.AutoLoadBalanceProviders);

                    if (listResult.WarningCount > 0)
                    {
                        result.IsError = true;
                        result.Response = HttpResponseHelper.FormatResponse(new OASISResult<T>() { Message = listResult.Message }, HttpStatusCode.BadRequest);
                        return (result, ProviderType.None);
                    }
                }
            }

            if (providerTypeObject != null)
                providerTypeOverride = (ProviderType)providerTypeObject;

            //if (providerTypeOverride != ProviderType.Default && providerTypeOverride != ProviderType.None)
            //    GetAndActivateProvider(providerTypeOverride, request.SetGlobally);


            switch (request.AutoReplicationMode.ToUpper())
            {
                case "ON":
                    result.AutoReplicationMode = AutoReplicationMode.True;
                    break;

                case "OFF":
                    result.AutoReplicationMode = AutoReplicationMode.False;
                    break;

                case "DEFAULT":
                    result.AutoReplicationMode = AutoReplicationMode.UseGlobalDefaultInOASISDNA;
                    break;
            }

            switch (request.AutoFailOverMode.ToUpper())
            {
                case "ON":
                    result.AutoFailOverMode = AutoFailOverMode.True;
                    break;

                case "OFF":
                    result.AutoFailOverMode = AutoFailOverMode.False;
                    break;

                case "DEFAULT":
                    result.AutoFailOverMode = AutoFailOverMode.UseGlobalDefaultInOASISDNA;
                    break;
            }

            switch (request.AutoLoadBalanceMode.ToUpper())
            {
                case "ON":
                    result.AutoLoadBalanceMode = AutoLoadBalanceMode.True;
                    break;

                case "OFF":
                    result.AutoLoadBalanceMode = AutoLoadBalanceMode.False;
                    break;

                case "DEFAULT":
                    result.AutoLoadBalanceMode = AutoLoadBalanceMode.UseGlobalDefaultInOASISDNA;
                    break;
            }

            //result.AutoReplicationMode = request.AutoReplicationMode == "DEFAULT" ? AutoReplicationMode.UseGlobalDefaultInOASISDNA : AutoReplicationModeTemp == true ? AutoReplicationMode.True : AutoReplicationMode.False;
            //result.AutoFailOverMode = request.AutoFailOverEnabled == "DEFAULT" ? AutoFailOverMode.UseGlobalDefaultInOASISDNA : autoFailOverEnabledTemp == true ? AutoFailOverMode.True : AutoFailOverMode.False;
            //result.AutoLoadBalanceMode = request.AutoLoadBalanceMode == "DEFAULT" ? AutoLoadBalanceMode.UseGlobalDefaultInOASISDNA : AutoLoadBalanceModeTemp == true ? AutoLoadBalanceMode.True : AutoLoadBalanceMode.False;

            result.PreviousAutoFailOverEnabled = ProviderManager.Instance.IsAutoFailOverEnabled;
            result.PreviousAutoReplicationEnabled = ProviderManager.Instance.IsAutoReplicationEnabled;
            result.PreviousAutoLoadBalanaceEnabled = ProviderManager.Instance.IsAutoLoadBalanceEnabled;

            if (result.AutoReplicationMode == AutoReplicationMode.True)
                ProviderManager.Instance.IsAutoReplicationEnabled = true;

            else if (result.AutoReplicationMode == AutoReplicationMode.False)
                ProviderManager.Instance.IsAutoReplicationEnabled = false;


            if (result.AutoFailOverMode == AutoFailOverMode.True)
                ProviderManager.Instance.IsAutoFailOverEnabled = true;

            else if (result.AutoFailOverMode == AutoFailOverMode.False)
                ProviderManager.Instance.IsAutoFailOverEnabled = false;


            if (result.AutoLoadBalanceMode == AutoLoadBalanceMode.True)
                ProviderManager.Instance.IsAutoLoadBalanceEnabled = true;

            else if (result.AutoLoadBalanceMode == AutoLoadBalanceMode.False)
                ProviderManager.Instance.IsAutoLoadBalanceEnabled = false;



            if (!string.IsNullOrEmpty(request.AutoReplicationProviders))
            {
                result.PreviousAutoReplicationList = ProviderManager.Instance.GetProvidersThatAreAutoReplicatingAsString();

                if (request.AutoReplicationProviders == "DEFAULT")
                    ProviderManager.Instance.SetAndReplaceAutoReplicationListForProviders(OASISBootLoader.OASISBootLoader.OASISDNA.OASIS.StorageProviders.AutoReplicationProviders);
                else
                    ProviderManager.Instance.SetAndReplaceAutoReplicationListForProviders(request.AutoReplicationProviders);
            }

            if (!string.IsNullOrEmpty(request.AutoFailOverProviders))
            {
                result.PreviousAutoFailOverList = ProviderManager.Instance.GetProviderAutoFailOverListAsString();

                if (request.AutoFailOverProviders == "DEFAULT")
                    ProviderManager.Instance.SetAndReplaceAutoFailOverListForProviders(OASISBootLoader.OASISBootLoader.OASISDNA.OASIS.StorageProviders.AutoFailOverProviders);
                else
                    ProviderManager.Instance.SetAndReplaceAutoFailOverListForProviders(request.AutoFailOverProviders);
            }

            if (!string.IsNullOrEmpty(request.AutoLoadBalanceProviders))
            {
                result.PreviousAutoLoadBalanaceList = ProviderManager.Instance.GetProviderAutoLoadBalanceListAsString();

                if (request.AutoLoadBalanceProviders == "DEFAULT")
                    ProviderManager.Instance.SetAndReplaceAutoLoadBalanceListForProviders(OASISBootLoader.OASISBootLoader.OASISDNA.OASIS.StorageProviders.AutoLoadBalanceProviders);
                else
                    ProviderManager.Instance.SetAndReplaceAutoLoadBalanceListForProviders(request.AutoLoadBalanceProviders);
            }

            return (result, providerTypeOverride);
        }
    }
}
