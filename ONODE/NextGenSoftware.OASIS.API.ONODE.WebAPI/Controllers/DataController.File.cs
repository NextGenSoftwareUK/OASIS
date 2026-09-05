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
        /// Saves a file and returns the id linked to the holon that it is stored in.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("save-file")]
        public async Task<OASISHttpResponseMessage<Guid>> SaveFile(SaveFileRequest request)
        {
            OASISConfigResult<Guid> configResult = ConfigureOASISEngine<Guid>(request);

            if (configResult.IsError && configResult.Response != null)
                return configResult.Response;

            OASISResult<Guid> response = await HolonManager.SaveFileAsync(request.Data, AvatarId);
            ResetOASISSettings(request, configResult);

            return HttpResponseHelper.FormatResponse(response, System.Net.HttpStatusCode.OK, request.ShowDetailedSettings);
        }

        /// <summary>
        /// Saves a file and returns the id linked to the holon that it is stored in.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("save-file/{data}")]
        public async Task<OASISHttpResponseMessage<Guid>> SaveFile(byte[] data)
        {
            return await SaveFile(new SaveFileRequest { Data = data });
        }

        /// <summary>
        /// Saves a file and returns the id linked to the holon that it is stored in.
        /// Pass in the provider you wish to use.
        /// Set the setglobally flag to false to use these settings only for this request or true for it to be used for all future requests.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="providerType">Pass in the provider you wish to use.</param>
        /// <param name="setGlobally"> Set this to false for this provider to be used only for this request or true for it to be used for all future requests too.</param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("save-file/{data}/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<Guid>> SaveFile(byte[] data, string providerType = "", bool setGlobally = false)
        {
            return await SaveFile(new SaveFileRequest()
            {
                Data = data,
                ProviderType = providerType,
                SetGlobally = setGlobally
            });
        }

        /// <summary>
        /// Saves a file and returns the id linked to the holon that it is stored in.
        /// Set the autoFailOverMode to 'ON' if you wish this call to work through the the providers in the auto-failover list until it succeeds. Set it to OFF if you do not or to 'DEFAULT' to default to the global OASISDNA setting.
        /// Set the autoReplicationMode to 'ON' if you wish this call to auto-replicate to the providers in the auto-replication list. Set it to OFF if you do not or to UseGlobalDefaultInOASISDNA to 'DEFAULT' to the global OASISDNA setting.
        /// Set the autoLoadBalanceMode to 'ON' if you wish this call to use the fastest provider in your area from the auto-loadbalance list. Set it to OFF if you do not or to UseGlobalDefaultInOASISDNA to 'DEFAULT' to the global OASISDNA setting.
        /// Set the waitForAutoReplicationResult flag to true if you wish for the API to wait for the auto-replication to complete before returning the results.
        /// Set the setglobally flag to false to use these settings only for this request or true for it to be used for all future requests.
        /// Set the showDetailedSettings flag to true to view detailed settings such as the list of providers in the auto-failover, auto-replication &amp; auto-load balance lists.
        /// </summary>
        /// <param name="data"></param>
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
        [HttpGet("save-file/{data}/{providerType}/{setGlobally}/{autoReplicationMode}/{autoFailOverMode}/{autoLoadBalanceMode}/{autoReplicationProviders}/{autoFailOverProviders}/{autoLoadBalanceProviders}/{waitForAutoReplicationResult}/{showDetailedSettings}")]
        public async Task<OASISHttpResponseMessage<Guid>> SaveFile(byte[] data, string providerType = "", bool setGlobally = false, string autoReplicationMode = "DEFAULT", string autoFailOverMode = "DEFAULT", string autoLoadBalanceMode = "DEFAULT", string autoReplicationProviders = "DEFAULT", string autoFailOverProviders = "DEFAULT", string autoLoadBalanceProviders = "DEFAULT", bool waitForAutoReplicationResult = false, bool showDetailedSettings = false)
        {
            return await SaveFile(new SaveFileRequest()
            {
                Data = data,
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
        /// Loads a file with the given id.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("load-file")]
        public async Task<OASISHttpResponseMessage<byte[]>> LoadFile(LoadFileRequest request)
        {
            OASISConfigResult<byte[]> configResult = ConfigureOASISEngine<byte[]>(request);

            if (configResult.IsError && configResult.Response != null)
                return configResult.Response;

            OASISResult<byte[]> response = await HolonManager.LoadFileAsync(request.Id, AvatarId);
            ResetOASISSettings(request, configResult);

            return HttpResponseHelper.FormatResponse(response, System.Net.HttpStatusCode.OK, request.ShowDetailedSettings);
        }

        /// <summary>
        /// Loads a file with the given id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("load-file/{id}")]
        public async Task<OASISHttpResponseMessage<byte[]>> LoadFile(Guid id)
        {
           return await LoadFile(new LoadFileRequest { Id = id });
        }

        /// <summary>
        /// Loads a file with the given id.
        /// Pass in the provider you wish to use.
        /// Set the setglobally flag to false to use these settings only for this request or true for it to be used for all future requests.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="providerType">Pass in the provider you wish to use.</param>
        /// <param name="setGlobally"> Set this to false for this provider to be used only for this request or true for it to be used for all future requests too.</param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("load-file/{id}/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<byte[]>> LoadFile(Guid id, string providerType = "", bool setGlobally = false)
        {
            return await LoadFile(new LoadFileRequest()
            {
                Id = id,
                ProviderType = providerType,
                SetGlobally = setGlobally
            });
        }

        /// <summary>
        /// Loads a file with the given id.
        /// Set the autoFailOverMode to 'ON' if you wish this call to work through the the providers in the auto-failover list until it succeeds. Set it to OFF if you do not or to 'DEFAULT' to default to the global OASISDNA setting.
        /// Set the autoReplicationMode to 'ON' if you wish this call to auto-replicate to the providers in the auto-replication list. Set it to OFF if you do not or to UseGlobalDefaultInOASISDNA to 'DEFAULT' to the global OASISDNA setting.
        /// Set the autoLoadBalanceMode to 'ON' if you wish this call to use the fastest provider in your area from the auto-loadbalance list. Set it to OFF if you do not or to UseGlobalDefaultInOASISDNA to 'DEFAULT' to the global OASISDNA setting.
        /// Set the waitForAutoReplicationResult flag to true if you wish for the API to wait for the auto-replication to complete before returning the results.
        /// Set the setglobally flag to false to use these settings only for this request or true for it to be used for all future requests.
        /// Set the showDetailedSettings flag to true to view detailed settings such as the list of providers in the auto-failover, auto-replication &amp; auto-load balance lists.
        /// </summary>
        /// <param name="id"></param>
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
        [HttpGet("load-file/{id}/{providerType}/{setGlobally}/{autoReplicationMode}/{autoFailOverMode}/{autoLoadBalanceMode}/{autoReplicationProviders}/{autoFailOverProviders}/{autoLoadBalanceProviders}/{waitForAutoReplicationResult}/{showDetailedSettings}")]
        public async Task<OASISHttpResponseMessage<byte[]>> LoadFile(Guid id, bool softDelete = true, string providerType = "", bool setGlobally = false, string autoReplicationMode = "DEFAULT", string autoFailOverMode = "DEFAULT", string autoLoadBalanceMode = "DEFAULT", string autoReplicationProviders = "DEFAULT", string autoFailOverProviders = "DEFAULT", string autoLoadBalanceProviders = "DEFAULT", bool waitForAutoReplicationResult = false, bool showDetailedSettings = false)
        {
            return await LoadFile(new LoadFileRequest()
            {
                Id = id,
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
        /// Saves custom data with a given key to the current logged in avatar.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("save-data")]
        public async Task<OASISHttpResponseMessage<bool>> SaveData(SaveDataRequest request)
        {
            OASISConfigResult<bool> configResult = ConfigureOASISEngine<bool>(request);

            if (configResult.IsError && configResult.Response != null)
                return configResult.Response;

            OASISResult<bool> response = AvatarManager.Instance.SaveData(request.Key, request.Value, AvatarId);
            ResetOASISSettings(request, configResult);

            return HttpResponseHelper.FormatResponse(response, System.Net.HttpStatusCode.OK, request.ShowDetailedSettings);
        }

        /// <summary>
        /// Saves custom data with a given key to the current logged in avatar.
        /// </summary>
        /// <param name="key">The key for the data.</param>
        /// <param name="value">The value for the data.</param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("save-data/{key}/{value}")]
        public async Task<OASISHttpResponseMessage<bool>> SaveData(string key, string value)
        {
            return await SaveData(new SaveDataRequest
            {
                Key = key,
                Value = value
            });
        }

        /// <summary>
        /// Saves custom data with a given key to the current logged in avatar.
        /// Pass in the provider you wish to use.
        /// Set the setglobally flag to false to use these settings only for this request or true for it to be used for all future requests.
        /// </summary>
        /// <param name="key">The key for the data.</param>
        /// <param name="value">The value for the data.</param>
        /// <param name="providerType">Pass in the provider you wish to use.</param>
        /// <param name="setGlobally"> Set this to false for this provider to be used only for this request or true for it to be used for all future requests too.</param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("save-data/{key}/{value}/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<bool>> SaveData(string key, string value, string providerType = "", bool setGlobally = false)
        {
            return await SaveData(new SaveDataRequest()
            {
                Key = key,
                Value = value,
                ProviderType = providerType,
                SetGlobally = setGlobally
            });
        }

        /// <summary>
        /// Saves custom data with a given key to the current logged in avatar.
        /// Set the autoFailOverMode to 'ON' if you wish this call to work through the the providers in the auto-failover list until it succeeds. Set it to OFF if you do not or to 'DEFAULT' to default to the global OASISDNA setting.
        /// Set the autoReplicationMode to 'ON' if you wish this call to auto-replicate to the providers in the auto-replication list. Set it to OFF if you do not or to UseGlobalDefaultInOASISDNA to 'DEFAULT' to the global OASISDNA setting.
        /// Set the autoLoadBalanceMode to 'ON' if you wish this call to use the fastest provider in your area from the auto-loadbalance list. Set it to OFF if you do not or to UseGlobalDefaultInOASISDNA to 'DEFAULT' to the global OASISDNA setting.
        /// Set the waitForAutoReplicationResult flag to true if you wish for the API to wait for the auto-replication to complete before returning the results.
        /// Set the setglobally flag to false to use these settings only for this request or true for it to be used for all future requests.
        /// Set the showDetailedSettings flag to true to view detailed settings such as the list of providers in the auto-failover, auto-replication &amp; auto-load balance lists.
        /// </summary>
        /// <param name="key">The key for the data.</param>
        /// <param name="value">The value for the data.</param>
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
        [HttpGet("save-data/{key}/{value}/{providerType}/{setGlobally}/{autoReplicationMode}/{autoFailOverMode}/{autoLoadBalanceMode}/{autoReplicationProviders}/{autoFailOverProviders}/{autoLoadBalanceProviders}/{waitForAutoReplicationResult}/{showDetailedSettings}")]
        public async Task<OASISHttpResponseMessage<bool>> SaveData(string key, string value, string providerType = "", bool setGlobally = false, string autoReplicationMode = "DEFAULT", string autoFailOverMode = "DEFAULT", string autoLoadBalanceMode = "DEFAULT", string autoReplicationProviders = "DEFAULT", string autoFailOverProviders = "DEFAULT", string autoLoadBalanceProviders = "DEFAULT", bool waitForAutoReplicationResult = false, bool showDetailedSettings = false)
        {
            return await SaveData(new SaveDataRequest()
            {
                Key = key,
                Value = value,
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
        /// Loads custom data with the given key from the current logged in avatar.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("load-data")]
        public async Task<ActionResult<OASISHttpResponseMessage<string>>> LoadData(LoadDataRequest request)
        {
            try
            {
                OASISConfigResult<string> configResult = ConfigureOASISEngine<string>(request);

                if (configResult.IsError && configResult.Response != null)
                    return Ok(configResult.Response);

                OASISResult<string> response = AvatarManager.Instance.LoadData(request?.Key, AvatarId);
                ResetOASISSettings(request, configResult);

                return Ok(HttpResponseHelper.FormatResponse(response, System.Net.HttpStatusCode.OK, request.ShowDetailedSettings));
            }
            catch (Exception ex)
            {
                var errorResponse = TestDataHelper.CreateErrorResponse<string>($"Error loading data: {ex.Message}", ex, System.Net.HttpStatusCode.InternalServerError);
                return StatusCode(500, errorResponse);
            }
        }

        /// <summary>
        /// Loads custom data with the given key from the current logged in avatar.
        /// </summary>
        /// <param name="key">The key for the data.</param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("load-data/{key}/{value}")]
        public async Task<ActionResult<OASISHttpResponseMessage<string>>> LoadData(string key)
        {
            return await LoadData(new LoadDataRequest
            {
                Key = key
            });
        }

        /// <summary>
        /// Loads custom data with the given key from the current logged in avatar.
        /// Pass in the provider you wish to use.
        /// Set the setglobally flag to false to use these settings only for this request or true for it to be used for all future requests.
        /// </summary>
        /// <param name="key">The key for the data.</param>
        /// <param name="providerType">Pass in the provider you wish to use.</param>
        /// <param name="setGlobally"> Set this to false for this provider to be used only for this request or true for it to be used for all future requests too.</param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("load-data/{key}/{value}/{providerType}/{setGlobally}")]
        public async Task<ActionResult<OASISHttpResponseMessage<string>>> LoadData(string key, string providerType = "", bool setGlobally = false)
        {
            return await LoadData(new LoadDataRequest()
            {
                Key = key,
                ProviderType = providerType,
                SetGlobally = setGlobally
            });
        }

        /// <summary>
        /// Loads custom data with the given key from the current logged in avatar.
        /// Set the autoFailOverMode to 'ON' if you wish this call to work through the the providers in the auto-failover list until it succeeds. Set it to OFF if you do not or to 'DEFAULT' to default to the global OASISDNA setting.
        /// Set the autoReplicationMode to 'ON' if you wish this call to auto-replicate to the providers in the auto-replication list. Set it to OFF if you do not or to UseGlobalDefaultInOASISDNA to 'DEFAULT' to the global OASISDNA setting.
        /// Set the autoLoadBalanceMode to 'ON' if you wish this call to use the fastest provider in your area from the auto-loadbalance list. Set it to OFF if you do not or to UseGlobalDefaultInOASISDNA to 'DEFAULT' to the global OASISDNA setting.
        /// Set the waitForAutoReplicationResult flag to true if you wish for the API to wait for the auto-replication to complete before returning the results.
        /// Set the setglobally flag to false to use these settings only for this request or true for it to be used for all future requests.
        /// Set the showDetailedSettings flag to true to view detailed settings such as the list of providers in the auto-failover, auto-replication &amp; auto-load balance lists.
        /// </summary>
        /// <param name="key">The key for the data.</param>
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
        [HttpGet("load-data/{key}/{value}/{providerType}/{setGlobally}/{autoReplicationMode}/{autoFailOverMode}/{autoLoadBalanceMode}/{autoReplicationProviders}/{autoFailOverProviders}/{autoLoadBalanceProviders}/{waitForAutoReplicationResult}/{showDetailedSettings}")]
        public async Task<ActionResult<OASISHttpResponseMessage<string>>> LoadData(string key, string providerType = "", bool setGlobally = false, string autoReplicationMode = "DEFAULT", string autoFailOverMode = "DEFAULT", string autoLoadBalanceMode = "DEFAULT", string autoReplicationProviders = "DEFAULT", string autoFailOverProviders = "DEFAULT", string autoLoadBalanceProviders = "DEFAULT", bool waitForAutoReplicationResult = false, bool showDetailedSettings = false)
        {
            return await LoadData(new LoadDataRequest()
            {
                Key = key,
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

        private (OASISHttpResponseMessage<T>, HolonType) ValidateHolonType<T>(string holonType)
        {
            object holonTypeObject = null;

            if (!string.IsNullOrEmpty(holonType) && !Enum.TryParse(typeof(HolonType), holonType, out holonTypeObject))
                return (HttpResponseHelper.FormatResponse(new OASISResult<T>() { IsError = true, Message = $"The HolonType {holonType} is not valid. It must be one of the following values: {EnumHelper.GetEnumValues(typeof(HolonType), EnumHelperListType.ItemsSeperatedByComma)}" }), HolonType.All);
            else
                return (HttpResponseHelper.FormatResponse(new OASISResult<T>()), (HolonType)holonTypeObject);
        }
    }
}
