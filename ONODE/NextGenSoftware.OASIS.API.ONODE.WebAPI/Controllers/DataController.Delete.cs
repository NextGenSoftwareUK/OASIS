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
        /// Delete a holon for the given id. Set SoftDelete to true if you wish this holon to be kept (can be un-deleted later) or to false to permanently delete (cannot be recovered).
        /// Pass in the provider you wish to use.
        /// Set the autoFailOverMode to 'ON' if you wish this call to work through the the providers in the auto-failover list until it succeeds. Set it to OFF if you do not or to 'DEFAULT' to default to the global OASISDNA setting.
        /// Set the autoReplicationMode to 'ON' if you wish this call to auto-replicate to the providers in the auto-replication list. Set it to OFF if you do not or to UseGlobalDefaultInOASISDNA to 'DEFAULT' to the global OASISDNA setting.
        /// Set the autoLoadBalanceMode to 'ON' if you wish this call to use the fastest provider in your area from the auto-loadbalance list. Set it to OFF if you do not or to UseGlobalDefaultInOASISDNA to 'DEFAULT' to the global OASISDNA setting.
        /// Set the waitForAutoReplicationResult flag to true if you wish for the API to wait for the auto-replication to complete before returning the results.
        /// Set the setglobally flag to false to use these settings only for this request or true for it to be used for all future requests.
        /// Set the showDetailedSettings flag to true to view detailed settings such as the list of providers in the auto-failover, auto-replication &amp; auto-load balance lists.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [Authorize]
        [HttpDelete("delete-holon")]
        public async Task<OASISHttpResponseMessage<IHolon>> DeleteHolon(DeleteHolonRequest request)
        {
            OASISConfigResult<IHolon> configResult = ConfigureOASISEngine<IHolon>(request);

            if (configResult.IsError && configResult.Response != null)
                return configResult.Response;

            OASISResult<IHolon> response = await HolonManager.DeleteHolonAsync(request.Id, AvatarId, request.SoftDelete);
            ResetOASISSettings(request, configResult);

            return HttpResponseHelper.FormatResponse(response, System.Net.HttpStatusCode.OK, request.ShowDetailedSettings);
        }

        /// <summary>
        /// Delete a holon for the given id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Authorize]
        [HttpDelete("delete-holon/{id}")]
        public async Task<OASISHttpResponseMessage<IHolon>> DeleteHolon(Guid id)
        {
            return await DeleteHolon(new DeleteHolonRequest() { Id = id });
        }

        /// <summary>
        /// Delete a holon for the given id. Set SoftDelete to true if you wish this holon to be kept (can be un-deleted later) or to false to permanently delete (cannot be recovered).
        /// </summary>
        /// <param name="id"></param>
        /// <param name="softDelete"></param>
        /// <returns></returns>
        [Authorize]
        [HttpDelete("delete-holon/{id}/{softDelete}")]
        public async Task<OASISHttpResponseMessage<IHolon>> DeleteHolon(Guid id, bool softDelete = true)
        {
            return await DeleteHolon(new DeleteHolonRequest() { Id = id, SoftDelete = softDelete });
        }

        /// <summary>
        /// Delete a holon for the given id. Set SoftDelete to true if you wish this holon to be kept (can be un-deleted later) or to false to permanently delete (cannot be recovered).
        /// Pass in the provider you wish to use.
        /// Set the setglobally flag to false to use these settings only for this request or true for it to be used for all future requests.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="softDelete"></param>
        /// <param name="providerType">Pass in the provider you wish to use.</param>
        /// <param name="setGlobally"> Set this to false for this provider to be used only for this request or true for it to be used for all future requests too.</param>
        /// <returns></returns>
        [Authorize]
        [HttpDelete("delete-holon/{id}/{softDelete}/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<IHolon>> DeleteHolon(Guid id, bool softDelete = true, string providerType = "", bool setGlobally = false)
        {
            return await DeleteHolon(new DeleteHolonRequest()
            {
                Id = id,
                SoftDelete = softDelete,
                ProviderType = providerType,
                SetGlobally = setGlobally
            });
        }

        /// <summary>
        /// Delete a holon for the given id. Set SoftDelete to true if you wish this holon to be kept (can be un-deleted later) or to false to permanently delete (cannot be recovered).
        /// Pass in the provider you wish to use.
        /// Set the autoFailOverMode to 'ON' if you wish this call to work through the the providers in the auto-failover list until it succeeds. Set it to OFF if you do not or to 'DEFAULT' to default to the global OASISDNA setting.
        /// Set the autoReplicationMode to 'ON' if you wish this call to auto-replicate to the providers in the auto-replication list. Set it to OFF if you do not or to UseGlobalDefaultInOASISDNA to 'DEFAULT' to the global OASISDNA setting.
        /// Set the autoLoadBalanceMode to 'ON' if you wish this call to use the fastest provider in your area from the auto-loadbalance list. Set it to OFF if you do not or to UseGlobalDefaultInOASISDNA to 'DEFAULT' to the global OASISDNA setting.
        /// Set the waitForAutoReplicationResult flag to true if you wish for the API to wait for the auto-replication to complete before returning the results.
        /// Set the setglobally flag to false to use these settings only for this request or true for it to be used for all future requests.
        /// Set the showDetailedSettings flag to true to view detailed settings such as the list of providers in the auto-failover, auto-replication &amp; auto-load balance lists.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="softDelete"></param>
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
        [HttpDelete("delete-holon/{id}/{softDelete}/{providerType}/{setGlobally}/{autoReplicationMode}/{autoFailOverMode}/{autoLoadBalanceMode}/{autoReplicationProviders}/{autoFailOverProviders}/{autoLoadBalanceProviders}/{waitForAutoReplicationResult}/{showDetailedSettings}")]
        public async Task<OASISHttpResponseMessage<IHolon>> DeleteHolon(Guid id, bool softDelete = true, string providerType = "", bool setGlobally = false, string autoReplicationMode = "DEFAULT", string autoFailOverMode = "DEFAULT", string autoLoadBalanceMode = "DEFAULT", string autoReplicationProviders = "DEFAULT", string autoFailOverProviders = "DEFAULT", string autoLoadBalanceProviders = "DEFAULT", bool waitForAutoReplicationResult = false, bool showDetailedSettings = false)
        {
            return await DeleteHolon(new DeleteHolonRequest()
            {
                Id = id,
                SoftDelete = softDelete,
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

    }
}
