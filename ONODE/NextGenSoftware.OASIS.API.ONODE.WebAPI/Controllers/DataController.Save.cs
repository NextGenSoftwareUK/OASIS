using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using Solnet.Metaplex;
using System.Linq;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    public partial class DataController
    {
     /// <summary>
     /// Save's a holon data object.
     /// Set the saveChildren flag to true to save all the holon's child holon's. This defaults to true.
     /// If saveChildren is set to true, you can set the Recursive flag to true to save all the child's holon's recursively, or false to only save the first level of child holon's. This defaults to true.
     /// If saveChildren is set to true, you can set the maxChildDepth value to a custom int of how many levels down you wish to save, it defaults to 0, which means it will save to infinite depth.
     /// Set the continueOnError flag to true if you wish it to continue saving child holon's even if an error has occured, this defaults to true.
     /// Pass in the provider you wish to use.
     /// Set the autoFailOverMode to 'ON' if you wish this call to work through the the providers in the auto-failover list until it succeeds. Set it to OFF if you do not or to 'DEFAULT' to default to the global OASISDNA setting.
     /// Set the autoReplicationMode to 'ON' if you wish this call to auto-replicate to the providers in the auto-replication list. Set it to OFF if you do not or to UseGlobalDefaultInOASISDNA to 'DEFAULT' to the global OASISDNA setting.
     /// Set the autoLoadBalanceMode to 'ON' if you wish this call to use the fastest provider in your area from the auto-loadbalance list. Set it to OFF if you do not or to UseGlobalDefaultInOASISDNA to 'DEFAULT' to the global OASISDNA setting.
     /// Set the waitForAutoReplicationResult flag to true if you wish for the API to wait for the auto-replication to complete before returning the results.
     /// Set the setglobally flag to false to use these settings only for this request or true for it to be used for all future requests.
     /// Set the showDetailedSettings flag to true to view detailed settings such as the list of providers in the auto-failover, auto-replication &amp; auto-load balance lists.
     /// </summary>
     /// <returns></returns>
     [Authorize]
     [HttpPost("save-holon")]
     public async Task<OASISHttpResponseMessage<IHolon>> SaveHolon(Models.Data.SaveHolonRequest request)
     {
         OASISConfigResult<IHolon> configResult = ConfigureOASISEngine<IHolon>(request);

         if (configResult.IsError && configResult.Response != null)
             return configResult.Response;

         OASISResult<IHolon> response = await HolonManager.SaveHolonAsync(request.Holon, AvatarId, request.SaveChildren, request.Recursive, request.MaxChildDepth, request.ContinueOnError);
         ResetOASISSettings(request, configResult);

         return HttpResponseHelper.FormatResponse(response, System.Net.HttpStatusCode.OK, request.ShowDetailedSettings);

         //OASISResult<Holon> response = new OASISResult<Holon>();
         //OASISResult<IHolon> result = await HolonManager.SaveHolonAsync(request.Holon);

         //OASISResultHelper<IHolon, Holon>.CopyResult(result, response);
         //response.Result = (Holon)result.Result;

         //return HttpResponseHelper.FormatResponse(response);
     }

       
   /// <summary>
   /// Save's a holon data object.
   /// </summary>
   /// <param name="holon"></param>
   /// <returns></returns>
   [Authorize]
   [HttpPost("save-holon/{holon}")]
   public async Task<OASISHttpResponseMessage<IHolon>> SaveHolon(Holon holon)
   {
       return await SaveHolon(new Models.Data.SaveHolonRequest() { Holon = holon });

       //OASISResult<Holon> response = new OASISResult<Holon>();
       //OASISResult<IHolon> result = await HolonManager.SaveHolonAsync(holon);

       //OASISResultHelper<IHolon, Holon>.CopyResult(result, response);
       //response.Result = (Holon)result.Result;

       //return HttpResponseHelper.FormatResponse(response);
   }



        /// <summary>
        /// Save's a holon data object.
        /// Set the saveChildren flag to true to save all the holon's child holon's. This defaults to true.
        /// If saveChildren is set to true, you can set the Recursive flag to true to save all the child's holon's recursively, or false to only save the first level of child holon's. This defaults to true.
        /// If saveChildren is set to true, you can set the maxChildDepth value to a custom int of how many levels down you wish to save, it defaults to 0, which means it will save to infinite depth.
        /// Set the continueOnError flag to true if you wish it to continue saving child holon's even if an error has occured, this defaults to true.
        /// </summary>
        /// <param name="saveChildren"></param>
        /// <param name="recursive"></param>
        /// <param name="maxChildDepth"></param>
        /// <param name="continueOnError"></param>
        /// <param name="holon"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("save-holon/{saveChildren}/{recursive}/{maxChildDepth}/{continueOnError}")]
        public async Task<OASISHttpResponseMessage<IHolon>> SaveHolon(Holon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true)
        {
            return await SaveHolon(new Models.Data.SaveHolonRequest()
            {
                Holon = holon,
                SaveChildren = saveChildren,
                Recursive = recursive,
                MaxChildDepth = maxChildDepth,
                ContinueOnError = continueOnError
            });

            //GetAndActivateProvider(providerType, setGlobally);
            //return await SaveHolon(holon);
        }

        /// <summary>
        /// Save's a holon data object.
        /// Set the saveChildren flag to true to save all the holon's child holon's. This defaults to true.
        /// If saveChildren is set to true, you can set the Recursive flag to true to save all the child's holon's recursively, or false to only save the first level of child holon's. This defaults to true.
        /// If saveChildren is set to true, you can set the maxChildDepth value to a custom int of how many levels down you wish to save, it defaults to 0, which means it will save to infinite depth.
        /// Set the continueOnError flag to true if you wish it to continue saving child holon's even if an error has occured, this defaults to true.
        /// Pass in the provider you wish to use.
        /// Set the setglobally flag to false to use these settings only for this request or true for it to be used for all future requests.
        /// </summary>
        /// <param name="holon"></param>
        /// <param name="saveChildren"></param>
        /// <param name="recursive"></param>
        /// <param name="maxChildDepth"></param>
        /// <param name="continueOnError"></param>
        /// <param name="providerType">Pass in the provider you wish to use.</param>
        /// <param name="setGlobally"> Set this to false for this provider to be used only for this request or true for it to be used for all future requests too.</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("save-holon/{saveChildren}/{recursive}/{maxChildDepth}/{continueOnError}/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<IHolon>> SaveHolon(Holon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, string providerType = "Default", bool setGlobally = false)
        {
            return await SaveHolon(new Models.Data.SaveHolonRequest() 
            { 
                Holon = holon,
                SaveChildren = saveChildren,
                Recursive = recursive,
                MaxChildDepth = maxChildDepth,
                ContinueOnError = continueOnError,
                ProviderType = providerType,
                SetGlobally = setGlobally
            });

            //GetAndActivateProvider(providerType, setGlobally);
            //return await SaveHolon(holon);
        }

        /// <summary>
        /// Save's a holon data object.
        /// Set the saveChildren flag to true to save all the holon's child holon's. This defaults to true.
        /// If saveChildren is set to true, you can set the Recursive flag to true to save all the child's holon's recursively, or false to only save the first level of child holon's. This defaults to true.
        /// If saveChildren is set to true, you can set the maxChildDepth value to a custom int of how many levels down you wish to save, it defaults to 0, which means it will save to infinite depth.
        /// Set the continueOnError flag to true if you wish it to continue saving child holon's even if an error has occured, this defaults to true.
        /// Pass in the provider you wish to use.
        /// Set the autoFailOverMode to 'ON' if you wish this call to work through the the providers in the auto-failover list until it succeeds. Set it to OFF if you do not or to 'DEFAULT' to default to the global OASISDNA setting.
        /// Set the autoReplicationMode to 'ON' if you wish this call to auto-replicate to the providers in the auto-replication list. Set it to OFF if you do not or to UseGlobalDefaultInOASISDNA to 'DEFAULT' to the global OASISDNA setting.
        /// Set the autoLoadBalanceMode to 'ON' if you wish this call to use the fastest provider in your area from the auto-loadbalance list. Set it to OFF if you do not or to UseGlobalDefaultInOASISDNA to 'DEFAULT' to the global OASISDNA setting.
        /// Set the waitForAutoReplicationResult flag to true if you wish for the API to wait for the auto-replication to complete before returning the results.
        /// Set the setglobally flag to false to use these settings only for this request or true for it to be used for all future requests.
        /// Set the showDetailedSettings flag to true to view detailed settings such as the list of providers in the auto-failover, auto-replication &amp; auto-load balance lists.
        /// </summary>
        /// <param name="holon"></param>
        /// <param name="saveChildren"></param>
        /// <param name="recursive"></param>
        /// <param name="maxChildDepth"></param>
        /// <param name="continueOnError"></param>
        /// <param name="providerType">Pass in the provider you wish to use.</param>
        /// <param name="setGlobally"> Set this to false for this provider to be used only for this request or true for it to be used for all future requests too.</param>
        /// <param name="autoFailOverMode"></param>
        /// <param name="autoReplicationMode"></param>
        /// <param name="autoLoadBalanceMode"></param>
        /// <param name="autoFailOverProviders"></param>
        /// <param name="autoReplicationProviders"></param>
        /// <param name="autoLoadBalanceProviders"></param>
        /// <param name="waitForAutoReplicationResult"></param>
        /// <param name="showDetailedSettings"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("save-holon/{saveChildren}/{recursive}/{maxChildDepth}/{continueOnError}/{providerType}/{setGlobally}/{autoReplicationMode}/{autoFailOverMode}/{autoLoadBalanceMode}/{autoReplicationProviders}/{autoFailOverProviders}/{AutoLoadBalanceProviders}/{waitForAutoReplicationResult}/{showDetailedSettings}")]
        public async Task<OASISHttpResponseMessage<IHolon>> SaveHolon(Holon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, string providerType = "Default", bool setGlobally = false, string autoReplicationMode = "DEFAULT", string autoFailOverMode = "DEFAULT", string autoLoadBalanceMode = "DEFAULT", string autoReplicationProviders = "DEFAULT", string autoFailOverProviders = "DEFAULT", string autoLoadBalanceProviders = "DEFAULT", bool waitForAutoReplicationResult = false, bool showDetailedSettings = false)
        {
            return await SaveHolon(new Models.Data.SaveHolonRequest()
            {
                Holon = holon,
                SaveChildren = saveChildren,
                Recursive = recursive,
                MaxChildDepth = maxChildDepth,
                ContinueOnError = continueOnError,
                ProviderType = providerType,
                SetGlobally = setGlobally,
                AutoReplicationMode = autoReplicationMode,
                AutoFailOverMode = autoFailOverMode,
                AutoLoadBalanceMode = autoLoadBalanceMode,
                AutoReplicationProviders = autoReplicationProviders,
                AutoFailOverProviders = autoFailOverProviders,
                AutoLoadBalanceProviders = autoLoadBalanceProviders,
                WaitForAutoReplicationResult = waitForAutoReplicationResult,
                ShowDetailedSettings = showDetailedSettings
            });
        }



        
        /// <summary>
        /// Save's a holon data object (meta data) to the given off-chain provider and then links its hash to the on-chain provider.
        /// Set the showDetailedSettings flag to true to view detailed settings such as the list of providers in the auto-failover, auto-replication &amp; auto-load balance lists.
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpPost("save-holon-off-chain")]
        public async Task<OASISHttpResponseMessage<Holon>> SaveHolonOffChain(Models.Data.SaveHolonRequest request)
        {
            return HttpResponseHelper.FormatResponse(new OASISResult<Holon>
            {
                IsError = false,
                Message = "COMING SOON..."
            });
        }
    }
}
