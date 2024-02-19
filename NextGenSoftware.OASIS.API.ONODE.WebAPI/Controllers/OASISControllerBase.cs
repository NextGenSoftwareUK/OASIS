using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.ONode.WebAPI.Helpers;
using NextGenSoftware.OASIS.API.ONode.WebAPI.Models;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONode.WebAPI.Controllers
{
    public class OASISControllerBase : ControllerBase
    {
        public IAvatar Avatar
        {
            get
            {
                if (HttpContext.Items.ContainsKey("Avatar") && HttpContext.Items["Avatar"] != null)
                    return (IAvatar)HttpContext.Items["Avatar"];

                return null;
            }
            set
            {
                HttpContext.Items["Avatar"] = value;
            }
        }

        public OASISControllerBase()
        {

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

        //protected OASISConfigResult<T> ConfigureOASISEngine<T>(OASISRequest request)
        //{
        //    OASISConfigResult<T> result = new OASISConfigResult<T>();
        //    object providerTypeObject = null;
        //    ProviderType providerTypeOverride = ProviderType.Default;

        //    if (!string.IsNullOrEmpty(request.AutoReplicationMode) && request.AutoReplicationMode.ToUpper() != "ON" && request.AutoReplicationMode.ToUpper() != "OFF" && request.AutoReplicationMode.ToUpper() != "DEFAULT")
        //    {
        //        result.IsError = true;
        //        result.Response = HttpResponseHelper.FormatResponse(new OASISResult<T>() { IsError = true, Message = $"AutoReplicationMode must be either 'ON', 'OFF' or 'DEFAULT' but found {request.AutoReplicationMode}" });
        //        return result;
        //    }

        //    if (!string.IsNullOrEmpty(request.AutoFailOverMode) && request.AutoFailOverMode.ToUpper() != "ON" && request.AutoFailOverMode.ToUpper() != "OFF" && request.AutoFailOverMode.ToUpper() != "DEFAULT")
        //    {
        //        result.IsError = true;
        //        result.Response = HttpResponseHelper.FormatResponse(new OASISResult<T>() { IsError = true, Message = $"AutoFailOverMode must be either 'ON', 'OFF' or 'DEFAULT' but found {request.AutoFailOverMode}" });
        //        return result;
        //    }

        //    if (!string.IsNullOrEmpty(request.AutoLoadBalanceMode) && request.AutoLoadBalanceMode.ToUpper() != "ON" && request.AutoLoadBalanceMode.ToUpper() != "OFF" && request.AutoLoadBalanceMode.ToUpper() != "DEFAULT")
        //    {
        //        result.IsError = true;
        //        result.Response = HttpResponseHelper.FormatResponse(new OASISResult<T>() { IsError = true, Message = $"AutoLoadBalanceMode must be either 'ON', 'OFF' or 'DEFAULT' but found {request.AutoReplicationMode}" });
        //        return result;
        //    }

        //    if (!string.IsNullOrEmpty(request.ProviderType) && !Enum.TryParse(typeof(ProviderType), request.ProviderType, out providerTypeObject))
        //    {
        //        result.Response = HttpResponseHelper.FormatResponse(new OASISResult<T>() { Message = $"The ProviderType {request.ProviderType} passed in is invalid. It must be one of the following types: {EnumHelper.GetEnumValues(typeof(ProviderType), EnumHelperListType.ItemsSeperatedByComma)}.", IsError = true }, HttpStatusCode.BadRequest);
        //        return result;
        //    }

        //    if (!string.IsNullOrEmpty(request.AutoReplicationProviders))
        //    {
        //        if (request.AutoReplicationProviders.ToUpper() != "DEFAULT")
        //        {
        //            OASISResult<IEnumerable<ProviderType>> listResult = ProviderManager.GetProvidersFromList("AutoReplication", request.AutoReplicationProviders);

        //            if (listResult.WarningCount > 0)
        //            {
        //                result.IsError = true;
        //                result.Response = HttpResponseHelper.FormatResponse(new OASISResult<T>() { Message = listResult.Message }, HttpStatusCode.BadRequest);
        //                return result;
        //            }
        //        }
        //    }

        //    if (!string.IsNullOrEmpty(request.AutoFailOverProviders))
        //    {
        //        if (request.AutoFailOverProviders.ToUpper() != "DEFAULT")
        //        {
        //            OASISResult<IEnumerable<ProviderType>> listResult = ProviderManager.GetProvidersFromList("AutoFailOver", request.AutoFailOverProviders);

        //            if (listResult.WarningCount > 0)
        //            {
        //                result.IsError = true;
        //                result.Response = HttpResponseHelper.FormatResponse(new OASISResult<T>() { Message = listResult.Message }, HttpStatusCode.BadRequest);
        //                return result;
        //            }
        //        }
        //    }

        //    if (!string.IsNullOrEmpty(request.AutoLoadBalanceProviders))
        //    {
        //        if (request.AutoLoadBalanceProviders.ToUpper() != "DEFAULT")
        //        {
        //            OASISResult<IEnumerable<ProviderType>> listResult = ProviderManager.GetProvidersFromList("AutoLoadBalance", request.AutoLoadBalanceProviders);

        //            if (listResult.WarningCount > 0)
        //            {
        //                result.IsError = true;
        //                result.Response = HttpResponseHelper.FormatResponse(new OASISResult<T>() { Message = listResult.Message }, HttpStatusCode.BadRequest);
        //                return result;
        //            }
        //        }
        //    }

        //    if (providerTypeObject != null)
        //        providerTypeOverride = (ProviderType)providerTypeObject;

        //    if (providerTypeOverride != ProviderType.Default && providerTypeOverride != ProviderType.None)
        //        GetAndActivateProvider(providerTypeOverride, request.SetGlobally);


        //    switch (request.AutoReplicationMode.ToUpper())
        //    {
        //        case "ON":
        //            result.AutoReplicationMode = AutoReplicationMode.True;
        //            break;

        //        case "OFF":
        //            result.AutoReplicationMode = AutoReplicationMode.False;
        //            break;

        //        case "DEFAULT":
        //            result.AutoReplicationMode = AutoReplicationMode.UseGlobalDefaultInOASISDNA;
        //            break;
        //    }

        //    switch (request.AutoFailOverMode.ToUpper())
        //    {
        //        case "ON":
        //            result.AutoFailOverMode = AutoFailOverMode.True;
        //            break;

        //        case "OFF":
        //            result.AutoFailOverMode = AutoFailOverMode.False;
        //            break;

        //        case "DEFAULT":
        //            result.AutoFailOverMode = AutoFailOverMode.UseGlobalDefaultInOASISDNA;
        //            break;
        //    }

        //    switch (request.AutoLoadBalanceMode.ToUpper())
        //    {
        //        case "ON":
        //            result.AutoLoadBalanceMode = AutoLoadBalanceMode.True;
        //            break;

        //        case "OFF":
        //            result.AutoLoadBalanceMode = AutoLoadBalanceMode.False;
        //            break;

        //        case "DEFAULT":
        //            result.AutoLoadBalanceMode = AutoLoadBalanceMode.UseGlobalDefaultInOASISDNA;
        //            break;
        //    }

        //    //result.AutoReplicationMode = request.AutoReplicationMode == "DEFAULT" ? AutoReplicationMode.UseGlobalDefaultInOASISDNA : AutoReplicationModeTemp == true ? AutoReplicationMode.True : AutoReplicationMode.False;
        //    //result.AutoFailOverMode = request.AutoFailOverEnabled == "DEFAULT" ? AutoFailOverMode.UseGlobalDefaultInOASISDNA : autoFailOverEnabledTemp == true ? AutoFailOverMode.True : AutoFailOverMode.False;
        //    //result.AutoLoadBalanceMode = request.AutoLoadBalanceMode == "DEFAULT" ? AutoLoadBalanceMode.UseGlobalDefaultInOASISDNA : AutoLoadBalanceModeTemp == true ? AutoLoadBalanceMode.True : AutoLoadBalanceMode.False;

        //    result.PreviousAutoFailOverEnabled = ProviderManager.IsAutoFailOverEnabled;
        //    result.PreviousAutoReplicationEnabled = ProviderManager.IsAutoReplicationEnabled;
        //    result.PreviousAutoLoadBalanaceEnabled = ProviderManager.IsAutoLoadBalanceEnabled;

        //    if (result.AutoReplicationMode == AutoReplicationMode.True)
        //        ProviderManager.IsAutoReplicationEnabled = true;

        //    else if (result.AutoReplicationMode == AutoReplicationMode.False)
        //        ProviderManager.IsAutoReplicationEnabled = false;


        //    if (result.AutoFailOverMode == AutoFailOverMode.True)
        //        ProviderManager.IsAutoFailOverEnabled = true;

        //    else if (result.AutoFailOverMode == AutoFailOverMode.False)
        //        ProviderManager.IsAutoFailOverEnabled = false;


        //    if (result.AutoLoadBalanceMode == AutoLoadBalanceMode.True)
        //        ProviderManager.IsAutoLoadBalanceEnabled = true;

        //    else if (result.AutoLoadBalanceMode == AutoLoadBalanceMode.False)
        //        ProviderManager.IsAutoLoadBalanceEnabled = false;



        //    if (!string.IsNullOrEmpty(request.AutoReplicationProviders))
        //    {
        //        result.PreviousAutoReplicationList = ProviderManager.GetProvidersThatAreAutoReplicatingAsString();

        //        if (request.AutoReplicationProviders == "DEFAULT")
        //            ProviderManager.SetAndReplaceAutoReplicationListForProviders(OASISBootLoader.OASISBootLoader.OASISDNA.OASIS.StorageProviders.AutoReplicationProviders);
        //        else
        //            ProviderManager.SetAndReplaceAutoReplicationListForProviders(request.AutoReplicationProviders);
        //    }

        //    if (!string.IsNullOrEmpty(request.AutoFailOverProviders))
        //    {
        //        result.PreviousAutoFailOverList = ProviderManager.GetProviderAutoFailOverListAsString();

        //        if (request.AutoFailOverProviders == "DEFAULT")
        //            ProviderManager.SetAndReplaceAutoFailOverListForProviders(OASISBootLoader.OASISBootLoader.OASISDNA.OASIS.StorageProviders.AutoFailOverProviders);
        //        else
        //            ProviderManager.SetAndReplaceAutoFailOverListForProviders(request.AutoFailOverProviders);
        //    }

        //    if (!string.IsNullOrEmpty(request.AutoLoadBalanceProviders))
        //    {
        //        result.PreviousAutoLoadBalanaceList = ProviderManager.GetProviderAutoLoadBalanceListAsString();

        //        if (request.AutoLoadBalanceProviders == "DEFAULT")
        //            ProviderManager.SetAndReplaceAutoLoadBalanceListForProviders(OASISBootLoader.OASISBootLoader.OASISDNA.OASIS.StorageProviders.AutoLoadBalanceProviders);
        //        else
        //            ProviderManager.SetAndReplaceAutoLoadBalanceListForProviders(request.AutoLoadBalanceProviders);
        //    }

        //    return result;
        //}

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
                ProviderManager.IsAutoFailOverEnabled = result.PreviousAutoFailOverEnabled;

            if (!request.SetGlobally)
                ProviderManager.IsAutoReplicationEnabled = result.PreviousAutoReplicationEnabled;

            if (!request.SetGlobally)
                ProviderManager.IsAutoLoadBalanceEnabled = result.PreviousAutoLoadBalanaceEnabled;


            if (result.PreviousAutoReplicationList != null && !request.SetGlobally)
                ProviderManager.SetAndReplaceAutoReplicationListForProviders(result.PreviousAutoReplicationList);

            if (result.PreviousAutoFailOverList != null && !request.SetGlobally)
                ProviderManager.SetAndReplaceAutoFailOverListForProviders(result.PreviousAutoFailOverList);

            if (result.PreviousAutoLoadBalanaceList != null && !request.SetGlobally)
                ProviderManager.SetAndReplaceAutoLoadBalanceListForProviders(result.PreviousAutoLoadBalanaceList);

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
                    OASISResult<IEnumerable<ProviderType>> listResult = ProviderManager.GetProvidersFromList("AutoReplication", request.AutoReplicationProviders);

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
                    OASISResult<IEnumerable<ProviderType>> listResult = ProviderManager.GetProvidersFromList("AutoFailOver", request.AutoFailOverProviders);

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
                    OASISResult<IEnumerable<ProviderType>> listResult = ProviderManager.GetProvidersFromList("AutoLoadBalance", request.AutoLoadBalanceProviders);

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

            result.PreviousAutoFailOverEnabled = ProviderManager.IsAutoFailOverEnabled;
            result.PreviousAutoReplicationEnabled = ProviderManager.IsAutoReplicationEnabled;
            result.PreviousAutoLoadBalanaceEnabled = ProviderManager.IsAutoLoadBalanceEnabled;

            if (result.AutoReplicationMode == AutoReplicationMode.True)
                ProviderManager.IsAutoReplicationEnabled = true;

            else if (result.AutoReplicationMode == AutoReplicationMode.False)
                ProviderManager.IsAutoReplicationEnabled = false;


            if (result.AutoFailOverMode == AutoFailOverMode.True)
                ProviderManager.IsAutoFailOverEnabled = true;

            else if (result.AutoFailOverMode == AutoFailOverMode.False)
                ProviderManager.IsAutoFailOverEnabled = false;


            if (result.AutoLoadBalanceMode == AutoLoadBalanceMode.True)
                ProviderManager.IsAutoLoadBalanceEnabled = true;

            else if (result.AutoLoadBalanceMode == AutoLoadBalanceMode.False)
                ProviderManager.IsAutoLoadBalanceEnabled = false;



            if (!string.IsNullOrEmpty(request.AutoReplicationProviders))
            {
                result.PreviousAutoReplicationList = ProviderManager.GetProvidersThatAreAutoReplicatingAsString();

                if (request.AutoReplicationProviders == "DEFAULT")
                    ProviderManager.SetAndReplaceAutoReplicationListForProviders(OASISBootLoader.OASISBootLoader.OASISDNA.OASIS.StorageProviders.AutoReplicationProviders);
                else
                    ProviderManager.SetAndReplaceAutoReplicationListForProviders(request.AutoReplicationProviders);
            }

            if (!string.IsNullOrEmpty(request.AutoFailOverProviders))
            {
                result.PreviousAutoFailOverList = ProviderManager.GetProviderAutoFailOverListAsString();

                if (request.AutoFailOverProviders == "DEFAULT")
                    ProviderManager.SetAndReplaceAutoFailOverListForProviders(OASISBootLoader.OASISBootLoader.OASISDNA.OASIS.StorageProviders.AutoFailOverProviders);
                else
                    ProviderManager.SetAndReplaceAutoFailOverListForProviders(request.AutoFailOverProviders);
            }

            if (!string.IsNullOrEmpty(request.AutoLoadBalanceProviders))
            {
                result.PreviousAutoLoadBalanaceList = ProviderManager.GetProviderAutoLoadBalanceListAsString();

                if (request.AutoLoadBalanceProviders == "DEFAULT")
                    ProviderManager.SetAndReplaceAutoLoadBalanceListForProviders(OASISBootLoader.OASISBootLoader.OASISDNA.OASIS.StorageProviders.AutoLoadBalanceProviders);
                else
                    ProviderManager.SetAndReplaceAutoLoadBalanceListForProviders(request.AutoLoadBalanceProviders);
            }

            return (result, providerTypeOverride);
        }
    }
}
