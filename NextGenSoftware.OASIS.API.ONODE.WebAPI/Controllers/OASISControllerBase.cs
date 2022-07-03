using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models;
using System;
using System.Collections.Generic;
using System.Net;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    public class OASISControllerBase : ControllerBase
    {
       // public IOptions<OASISSettings> OASISSettings;

        public IAvatar Avatar
        {
            get
            {
                if (HttpContext.Items.ContainsKey("Avatar") && HttpContext.Items["Avatar"] != null)
                    return (IAvatar)HttpContext.Items["Avatar"];

                //if (HttpContext.Session.GetString("Avatar") != null)
                //    return JsonSerializer.Deserialize<IAvatar>(HttpContext.Session.GetString("Avatar"));

                return null;
            }
            set
            {
                HttpContext.Items["Avatar"] = value;
                //HttpContext.Session.SetString("Avatar", JsonSerializer.Serialize(value));
            }
        }

        //public OASISControllerBase(IOptions<OASISSettings> settings)
        public OASISControllerBase()
        {
            //OASISSettings = settings;
           // OASISProviderManager.OASISSettings = settings.Value;
        }

        //TODO: REMOVE ASAP, NOT USED ANYMORE
        //public OASISControllerBase(IOptions<OASISDNA> settings)
        //{
        //    //OASISSettings = settings;
        //    // OASISProviderManager.OASISSettings = settings.Value;
        //}

        protected IOASISStorageProvider GetAndActivateDefaultProvider()
        {
            OASISResult<IOASISStorageProvider> result = OASISBootLoader.OASISBootLoader.GetAndActivateDefaultProvider();

            //TODO: Eventually want to replace all exceptions with OASISResult throughout the OASIS because then it makes sure errors are handled properly and friendly messages are shown (plus less overhead of throwing an entire stack trace!)
            if (result.IsError)
                ErrorHandling.HandleError(ref result, string.Concat("Error calling OASISDNAManager.GetAndActivateDefaultProvider(). Error details: ", result.Message), true, false, true);

            return result.Result;
        }

        protected IOASISStorageProvider GetAndActivateProvider(ProviderType providerType, bool setGlobally = false)
        {
            // TODO: Everywhere I have had to just return the .Result object of OASISResult we need to check for errors and handle correcty.
            // Or maybe better in this case and in the Managers (AvatarManager/HolonManager) just change the return types to OASISResult<T> and pass it up to show any errors/messages to UI if needed...
            // But in meantime if you need to show any errors just throw an exception (ONLY TEMP!)
            return OASISBootLoader.OASISBootLoader.GetAndActivateProvider(providerType, null, false, setGlobally).Result;
        }

        protected IOASISStorageProvider GetAndActivateProvider(ProviderType providerType, string customConnectionString = null, bool forceRegister = false, bool setGlobally = false)
        {
            // TODO: Everywhere I have had to just return the .Result object of OASISResult we need to check for errors and handle correcty.
            // Or maybe better in this case and in the Managers (AvatarManager/HolonManager) just change the return types to OASISResult<T> and pass it up to show any errors/messages to UI if needed...
            // But in meantime if you need to show any errors just throw an exception (ONLY TEMP!)
            return OASISBootLoader.OASISBootLoader.GetAndActivateProvider(providerType, customConnectionString, forceRegister, setGlobally).Result;
        }

        protected OASISConfigResult<T> ConfigureOASISSettings<T>(OASISRequest request)
        {
            OASISConfigResult<T> result = new OASISConfigResult<T>();
            object providerTypeObject = null;
            ProviderType providerTypeOverride = ProviderType.Default;
            bool autoReplicationEnabledTemp = true;
            bool autoFailOverEnabledTemp = true;
            bool autoLoadBalanceEnabledTemp = true;

            if (request.AutoReplicationEnabled != "default" && !bool.TryParse(request.AutoReplicationEnabled, out autoReplicationEnabledTemp))
            {
                result.IsError = true;
                result.Response = HttpResponseHelper.FormatResponse(new OASISResult<T>() { IsError = true, Message = $"AutoReplicationEnabled must be either true, false or default but found {request.AutoReplicationEnabled}" });
                return result;
            }

            if (request.AutoFailOverEnabled != "default" && !bool.TryParse(request.AutoFailOverEnabled, out autoFailOverEnabledTemp))
            {
                result.IsError = true;
                result.Response = HttpResponseHelper.FormatResponse(new OASISResult<T>() { IsError = true, Message = $"AutoFailOverEnabled must be either true, false or default but found {request.AutoFailOverEnabled}" });
                return result;
            }
                
            if (request.AutoLoadBalanceEnabled != "default" && !bool.TryParse(request.AutoLoadBalanceEnabled, out autoLoadBalanceEnabledTemp))
            {
                result.IsError = true;
                result.Response = HttpResponseHelper.FormatResponse(new OASISResult<T>() { IsError = true, Message = $"AutoLoadBlanaceEnabled must be either true, false or default but found {request.AutoLoadBalanceEnabled}" });
                return result;
            }

            if (!string.IsNullOrEmpty(request.ProviderType) && !Enum.TryParse(typeof(ProviderType), request.ProviderType, out providerTypeObject))
            {
                result.Response = HttpResponseHelper.FormatResponse(new OASISResult<T>() { Message = $"The ProviderType {request.ProviderType} passed in is invalid. It must be one of the following types: {EnumHelper.GetEnumValues(typeof(ProviderType), EnumHelperListType.ItemsSeperatedByComma)}.", IsError = true }, HttpStatusCode.BadRequest);
                return result;
            }

            if (!string.IsNullOrEmpty(request.AutoReplicationProviders))
            {
                if (request.AutoReplicationProviders != "default")
                {
                    OASISResult<IEnumerable<ProviderType>> listResult = ProviderManager.GetProvidersFromList("AutoReplication", request.AutoReplicationProviders);

                    if (listResult.WarningCount > 0)
                    {
                        result.IsError = true;
                        result.Response = HttpResponseHelper.FormatResponse(new OASISResult<T>() { Message = listResult.Message }, HttpStatusCode.BadRequest);
                        return result;
                    }
                }
            }

            if (!string.IsNullOrEmpty(request.AutoFailOverProviders))
            {
                if (request.AutoFailOverProviders != "default")
                {
                    OASISResult<IEnumerable<ProviderType>> listResult = ProviderManager.GetProvidersFromList("AutoFailOver", request.AutoFailOverProviders);

                    if (listResult.WarningCount > 0)
                    {
                        result.IsError = true;
                        result.Response = HttpResponseHelper.FormatResponse(new OASISResult<T>() { Message = listResult.Message }, HttpStatusCode.BadRequest);
                        return result;
                    }
                }
            }

            if (!string.IsNullOrEmpty(request.AutoLoadBalanceProviders))
            {
                if (request.AutoLoadBalanceProviders != "default")
                {
                    OASISResult<IEnumerable<ProviderType>> listResult = ProviderManager.GetProvidersFromList("AutoLoadBalance", request.AutoLoadBalanceProviders);

                    if (listResult.WarningCount > 0)
                    {
                        result.IsError = true;
                        result.Response = HttpResponseHelper.FormatResponse(new OASISResult<T>() { Message = listResult.Message }, HttpStatusCode.BadRequest);
                        return result;
                    }
                }
            }

            if (providerTypeObject != null)
                providerTypeOverride = (ProviderType)providerTypeObject;

            if (providerTypeOverride != ProviderType.Default && providerTypeOverride != ProviderType.None)
                GetAndActivateProvider(providerTypeOverride, request.SetGlobally);

            //if (request.AutoReplicationEnabled.HasValue)
            //    autoReplicationMode = request.AutoReplicationEnabled.Value ? AutoReplicationMode.True : AutoReplicationMode.False;

            //if (request.AutoFailOverEnabled.HasValue)
            //    autoFailOverMode = request.AutoFailOverEnabled.Value ? AutoFailOverMode.True : AutoFailOverMode.False;

            //if (request.AutoLoadBalanceEnabled.HasValue)
            //    autoLoadBalanceMode = request.AutoLoadBalanceEnabled.Value ? AutoLoadBalanceMode.True : AutoLoadBalanceMode.False;

            result.AutoReplicationMode = request.AutoReplicationEnabled == "default" ? AutoReplicationMode.UseGlobalDefaultInOASISDNA : autoReplicationEnabledTemp == true ? AutoReplicationMode.True : AutoReplicationMode.False;
            result.AutoFailOverMode = request.AutoFailOverEnabled == "default" ? AutoFailOverMode.UseGlobalDefaultInOASISDNA : autoFailOverEnabledTemp == true ? AutoFailOverMode.True : AutoFailOverMode.False;
            result.AutoLoadBalanceMode = request.AutoLoadBalanceEnabled == "default" ? AutoLoadBalanceMode.UseGlobalDefaultInOASISDNA : autoLoadBalanceEnabledTemp == true ? AutoLoadBalanceMode.True : AutoLoadBalanceMode.False;

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

                if (request.AutoReplicationProviders == "default")
                    ProviderManager.SetAndReplaceAutoReplicationListForProviders(OASISBootLoader.OASISBootLoader.OASISDNA.OASIS.StorageProviders.AutoReplicationProviders);
                else
                    ProviderManager.SetAndReplaceAutoReplicationListForProviders(request.AutoReplicationProviders);
            }

            if (!string.IsNullOrEmpty(request.AutoFailOverProviders))
            {
                result.PreviousAutoFailOverList = ProviderManager.GetProviderAutoFailOverListAsString();

                if (request.AutoFailOverProviders == "default")
                    ProviderManager.SetAndReplaceAutoFailOverListForProviders(OASISBootLoader.OASISBootLoader.OASISDNA.OASIS.StorageProviders.AutoFailOverProviders);
                else
                    ProviderManager.SetAndReplaceAutoFailOverListForProviders(request.AutoFailOverProviders);
            }

            if (!string.IsNullOrEmpty(request.AutoLoadBalanceProviders))
            {
                result.PreviousAutoLoadBalanaceList = ProviderManager.GetProviderAutoLoadBalanceListAsString();

                if (request.AutoLoadBalanceProviders == "default")
                    ProviderManager.SetAndReplaceAutoLoadBalanceListForProviders(OASISBootLoader.OASISBootLoader.OASISDNA.OASIS.StorageProviders.AutoLoadBalanceProviders);
                else
                    ProviderManager.SetAndReplaceAutoLoadBalanceListForProviders(request.AutoLoadBalanceProviders);
            }

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
    }
}
