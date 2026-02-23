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
    /// <summary>
    /// Data management endpoints for CRUD operations on holons and data objects.
    /// Provides comprehensive data storage, retrieval, and management capabilities across all OASIS providers.
    /// </summary>
    [ApiController]
    [Route("api/data")]
    public class DataController : OASISControllerBase
    {
        //  OASISDNA _settings;
        HolonManager _holonManager = null;

        HolonManager HolonManager
        {
            get
            {
                if (_holonManager == null)
                {
                    OASISResult<IOASISStorageProvider> result = Task.Run(OASISBootLoader.OASISBootLoader.GetAndActivateDefaultStorageProviderAsync).Result;

                    if (result.IsError)
                        OASISErrorHandling.HandleError(ref result, string.Concat("Error calling OASISBootLoader.OASISBootLoader.GetAndActivateDefaultStorageProvider(). Error details: ", result.Message));

                    _holonManager = new HolonManager(result.Result);
                }

                return _holonManager;
            }
        }

        public DataController()
        {

        }

        /// <summary>
        /// Load's a holon data object for the given id.
        /// Set the loadChildren flag to true to load all the holon's child holon's. This defaults to true.
        /// If loadChildren is set to true, you can set the Recursive flag to true to load all the child's holon's recursively, or false to only load the first level of child holon's. This defaults to true.
        /// If loadChildren is set to true, you can set the maxChildDepth value to a custom int of how many levels down you wish to load, it defaults to 0, which means it will load to infinite depth.
        /// Set the continueOnError flag to true if you wish it to continue loading child holon's even if an error has occured, this defaults to true.
        /// Set the Version int to the version of the holon you wish to load (defaults to 0) which means the latest version.
        /// Pass in the provider you wish to use.
        /// Set the autoFailOverMode to 'ON' if you wish this call to work through the the providers in the auto-failover list until it succeeds. Set it to OFF if you do not or to 'DEFAULT' to default to the global OASISDNA setting.
        /// Set the autoReplicationMode to 'ON' if you wish this call to auto-replicate to the providers in the auto-replication list. Set it to OFF if you do not or to UseGlobalDefaultInOASISDNA to 'DEFAULT' to the global OASISDNA setting.
        /// Set the autoLoadBalanceMode to 'ON' if you wish this call to use the fastest provider in your area from the auto-loadbalance list. Set it to OFF if you do not or to UseGlobalDefaultInOASISDNA to 'DEFAULT' to the global OASISDNA setting.
        /// Set the waitForAutoReplicationResult flag to true if you wish for the API to wait for the auto-replication to complete before returning the results.
        /// Set the setglobally flag to false to use these settings only for this request or true for it to be used for all future requests.
        /// Set the showDetailedSettings flag to true to view detailed settings such as the list of providers in the auto-failover, auto-replication &amp; auto-load balance lists.
        /// </summary>
        /// <param name="request">The load holon request containing ID and configuration options.</param>
        /// <returns>OASIS result containing the loaded holon or error details.</returns>
        /// <response code="200">Holon loaded successfully</response>
        /// <response code="400">Error loading holon</response>
        /// <response code="401">Unauthorized - authentication required</response>
        [Authorize]
        [HttpPost("load-holon")]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<Holon>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(OASISHttpResponseMessage<string>), StatusCodes.Status401Unauthorized)]
        public async Task<OASISHttpResponseMessage<Holon>> LoadHolon(Models.Data.LoadHolonRequest request)
        {
            //OASISResult<Holon> response = new OASISResult<Holon>();
            OASISHttpResponseMessage<Holon> response;
            (response, HolonType childHolonType) = ValidateHolonType<Holon>(request.ChildHolonType);

            OASISConfigResult<Holon> configResult = await ConfigureOASISEngineAsync<Holon>(request);

            if (configResult.IsError && configResult.Response != null)
                return configResult.Response;

            OASISResult<IHolon> result = null;
            try
            {
                result = await HolonManager.LoadHolonAsync(request.Id, request.LoadChildren, request.Recursive, request.MaxChildDepth, request.ContinueOnError, request.LoadChildrenFromProvider, childHolonType, request.Version);

                ResetOASISSettings(request, configResult);

                // Return test data if setting is enabled and result is null, has error, or result is null
                if (UseTestDataWhenLiveDataNotAvailable && (result == null || result.IsError || result.Result == null))
                {
                    var testHolon = TestDataHelper.GetTestHolon(request.Id);
                    return TestDataHelper.CreateSuccessResponse<Holon>(testHolon, "Holon loaded successfully (using test data)", System.Net.HttpStatusCode.OK);
                }

                OASISResultHelper<IHolon, Holon>.CopyResult(result, response.Result);
                response.Result.Result = (Holon)result.Result;

                return HttpResponseHelper.FormatResponse(response, System.Net.HttpStatusCode.OK, request.ShowDetailedSettings);
            }
            catch (Exception ex)
            {
                ResetOASISSettings(request, configResult);

                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    var testHolon = TestDataHelper.GetTestHolon(request.Id);
                    return TestDataHelper.CreateSuccessResponse<Holon>(testHolon, "Holon loaded successfully (using test data)", System.Net.HttpStatusCode.OK);
                }
                return TestDataHelper.CreateErrorResponse<Holon>($"Error loading holon: {ex.Message}", ex, System.Net.HttpStatusCode.BadRequest);
            }
        }


        /// <summary>
        /// Load's a holon data object for the given id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("load-holon/{id}")]
        public async Task<OASISHttpResponseMessage<Holon>> LoadHolon(Guid id)
        {
            return await LoadHolon(new Models.Data.LoadHolonRequest() { Id = id });
        }

        /// <summary>
        /// Load's a holon data object for the given id.
        /// Set the loadChildren flag to true to load all the holon's child holon's. This defaults to true.
        /// If loadChildren is set to true, you can set the Recursive flag to true to load all the child's holon's recursively, or false to only load the first level of child holon's. This defaults to true.
        /// If loadChildren is set to true, you can set the maxChildDepth value to a custom int of how many levels down you wish to load, it defaults to 0, which means it will load to infinite depth.
        /// Set the continueOnError flag to true if you wish it to continue loading child holon's even if an error has occured, this defaults to true.
        /// Set the Version int to the version of the holon you wish to load (defaults to 0) which means the latest version.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="loadChildren"></param>
        /// <param name="recursive"></param>
        /// <param name="maxChildDepth"></param>
        /// <param name="continueOnError"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("load-holon/{id}/{loadChildren}/{recursive}/{maxChildDepth}/{continueOnError}/{version}")]
        public async Task<OASISHttpResponseMessage<Holon>> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return await LoadHolon(new Models.Data.LoadHolonRequest()
            {
                Id = id,
                LoadChildren = loadChildren,
                Recursive = recursive,
                MaxChildDepth = maxChildDepth,
                ContinueOnError = continueOnError,
                Version = version
            });
        }

        /// <summary>
        /// Load's a holon data object for the given id.
        /// Set the loadChildren flag to true to load all the holon's child holon's. This defaults to true.
        /// If loadChildren is set to true, you can set the Recursive flag to true to load all the child's holon's recursively, or false to only load the first level of child holon's. This defaults to true.
        /// If loadChildren is set to true, you can set the maxChildDepth value to a custom int of how many levels down you wish to load, it defaults to 0, which means it will load to infinite depth.
        /// Set the continueOnError flag to true if you wish it to continue loading child holon's even if an error has occured, this defaults to true.
        /// Set the Version int to the version of the holon you wish to load (defaults to 0) which means the latest version.
        /// Pass in the provider you wish to use.
        /// Set the setglobally flag to false to use these settings only for this request or true for it to be used for all future requests.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="loadChildren"></param>
        /// <param name="recursive"></param>
        /// <param name="maxChildDepth"></param>
        /// <param name="continueOnError"></param>
        /// <param name="version"></param>
        /// <param name="providerType">Pass in the provider you wish to use.</param>
        /// <param name="setGlobally"> Set this to false for this provider to be used only for this request or true for it to be used for all future requests too.</param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("load-holon/{id}/{loadChildren}/{recursive}/{maxChildDepth}/{continueOnError}/{version}/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<Holon>> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, string providerType = "", bool setGlobally = false)
        {
            return await LoadHolon(new Models.Data.LoadHolonRequest()
            {
                Id = id,
                LoadChildren = loadChildren,
                Recursive = recursive,
                MaxChildDepth = maxChildDepth,
                ContinueOnError = continueOnError,
                Version = version,
                ProviderType = providerType,
                SetGlobally = setGlobally
            });
        }


      
        /// <summary>
        /// Load's a holon data object for the given id.
        /// Set the loadChildren flag to true to load all the holon's child holon's. This defaults to true.
        /// If loadChildren is set to true, you can set the Recursive flag to true to load all the child's holon's recursively, or false to only load the first level of child holon's. This defaults to true.
        /// If loadChildren is set to true, you can set the maxChildDepth value to a custom int of how many levels down you wish to load, it defaults to 0, which means it will load to infinite depth.
        /// Set the continueOnError flag to true if you wish it to continue loading child holon's even if an error has occured, this defaults to true.
        /// Set the Version int to the version of the holon you wish to load (defaults to 0) which means the latest version.
        /// Pass in the provider you wish to use.
        /// Set the autoFailOverMode to 'ON' if you wish this call to work through the the providers in the auto-failover list until it succeeds. Set it to OFF if you do not or to 'DEFAULT' to default to the global OASISDNA setting.
        /// Set the autoReplicationMode to 'ON' if you wish this call to auto-replicate to the providers in the auto-replication list. Set it to OFF if you do not or to UseGlobalDefaultInOASISDNA to 'DEFAULT' to the global OASISDNA setting.
        /// Set the autoLoadBalanceMode to 'ON' if you wish this call to use the fastest provider in your area from the auto-loadbalance list. Set it to OFF if you do not or to UseGlobalDefaultInOASISDNA to 'DEFAULT' to the global OASISDNA setting.
        /// Set the waitForAutoReplicationResult flag to true if you wish for the API to wait for the auto-replication to complete before returning the results.
        /// Set the setglobally flag to false to use these settings only for this request or true for it to be used for all future requests.
        /// Set the showDetailedSettings flag to true to view detailed settings such as the list of providers in the auto-failover, auto-replication &amp; auto-load balance lists.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="loadChildren"></param>
        /// <param name="recursive"></param>
        /// <param name="maxChildDepth"></param>
        /// <param name="continueOnError"></param>
        /// <param name="version"></param>
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
        [HttpGet("load-holon/{id}/{loadChildren}/{recursive}/{maxChildDepth}/{continueOnError}/{version}/{providerType}/{setGlobally}/{autoReplicationMode}/{autoFailOverMode}/{autoLoadBalanceMode}/{autoReplicationProviders}/{autoFailOverProviders}/{autoLoadBalanceProviders}/{waitForAutoReplicationResult}/{showDetailedSettings}")]
        public async Task<OASISHttpResponseMessage<Holon>> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, string providerType = "Default", bool setGlobally = false, string autoReplicationMode = "DEFAULT", string autoFailOverMode = "DEFAULT", string autoLoadBalanceMode = "DEFAULT", string autoReplicationProviders = "DEFAULT", string autoFailOverProviders = "DEFAULT", string autoLoadBalanceProviders = "DEFAULT", bool waitForAutoReplicationResult = false, bool showDetailedSettings = false)
        {
            return await LoadHolon(new Models.Data.LoadHolonRequest()
            {
                Id = id,
                LoadChildren = loadChildren,
                Recursive = recursive,
                MaxChildDepth = maxChildDepth,
                ContinueOnError = continueOnError,
                Version = version,
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
        /// Load's all holons for the given HolonType. Use 'All' to load all holons.
        /// Set the loadChildren flag to true to load all the holon's child holon's. This defaults to true.
        /// If loadChildren is set to true, you can set the Recursive flag to true to load all the child's holon's recursively, or false to only load the first level of child holon's. This defaults to true.
        /// If loadChildren is set to true, you can set the maxChildDepth value to a custom int of how many levels down you wish to load, it defaults to 0, which means it will load to infinite depth.
        /// Set the continueOnError flag to true if you wish it to continue loading child holon's even if an error has occured, this defaults to true.
        /// Set the Version int to the version of the holon you wish to load (defaults to 0) which means the latest version.
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
        [HttpPost("load-all-holons")]
        public async Task<OASISHttpResponseMessage<IEnumerable<Holon>>> LoadAllHolons(LoadAllHolonsRequest request)
        {
            try
            {
                OASISHttpResponseMessage<IEnumerable<Holon>> response;
                (response, HolonType holonType) = ValidateHolonType<IEnumerable<Holon>>(request.HolonType);
                (response, HolonType childHolonType) = ValidateHolonType<IEnumerable<Holon>>(request.ChildHolonType);

                if (response.Result.IsError)
                    return response;

                OASISConfigResult<IEnumerable<Holon>> configResult = ConfigureOASISEngine<IEnumerable<Holon>>(request);

                if (configResult.IsError && configResult.Response != null)
                    return configResult.Response;

                OASISResult<IEnumerable<IHolon>> result = null;
                try
                {
                    result = await HolonManager.LoadAllHolonsAsync(holonType, request.LoadChildren, request.Recursive, request.MaxChildDepth, request.ContinueOnError, request.LoadChildrenFromProvider, childHolonType, request.Version);

                    ResetOASISSettings(request, configResult);

                    // Return test data if setting is enabled and result is null, has error, or is empty
                    if (UseTestDataWhenLiveDataNotAvailable && (result == null || result.IsError || result.Result == null || !result.Result.Any()))
                    {
                        var testHolons = TestDataHelper.GetTestHolons(5);
                        return TestDataHelper.CreateSuccessResponse<IEnumerable<Holon>>(testHolons, "Holons loaded successfully (using test data)", System.Net.HttpStatusCode.OK);
                    }

                    OASISResultHelper<IHolon, Holon>.CopyResult(result, response.Result);
                    var holons = Mapper.Convert<IHolon, Holon>(result.Result);
                    response.Result.Result = holons as IList<Holon> ?? holons?.ToList() ?? new List<Holon>();

                    return HttpResponseHelper.FormatResponse(response, System.Net.HttpStatusCode.OK, request.ShowDetailedSettings);
                }
                catch (Exception ex)
                {
                    ResetOASISSettings(request, configResult);

                    // Return test data if setting is enabled, otherwise return error
                    if (UseTestDataWhenLiveDataNotAvailable)
                    {
                        var testHolons = TestDataHelper.GetTestHolons(5);
                        return TestDataHelper.CreateSuccessResponse<IEnumerable<Holon>>(testHolons, "Holons loaded successfully (using test data)", System.Net.HttpStatusCode.OK);
                    }
                    return TestDataHelper.CreateErrorResponse<IEnumerable<Holon>>($"Error loading holons: {ex.Message}", ex);
                }
            }
            catch (Exception ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    var testHolons = TestDataHelper.GetTestHolons(5);
                    return TestDataHelper.CreateSuccessResponse<IEnumerable<Holon>>(testHolons, "Holons loaded successfully (using test data)", System.Net.HttpStatusCode.OK);
                }
                return TestDataHelper.CreateErrorResponse<IEnumerable<Holon>>($"Error loading holons: {ex.Message}", ex);
            }
        }


        /// <summary>
        /// Load's all holons for the given HolonType. Use 'All' to load all holons.
        /// </summary>
        /// <param name="holonType"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("load-all-holons/{holonType}")]
        public async Task<OASISHttpResponseMessage<IEnumerable<Holon>>> LoadAllHolons(string holonType)
        {
            try
            {
                var result = await LoadAllHolons(new LoadAllHolonsRequest() { HolonType = holonType });

                // Return test data if setting is enabled and result is null, has error, or is empty
                if (UseTestDataWhenLiveDataNotAvailable && (result == null || result.Result == null || result.Result.IsError || result.Result.Result == null || !result.Result.Result.Any()))
                {
                    var testHolons = TestDataHelper.GetTestHolons(5);
                    return TestDataHelper.CreateSuccessResponse<IEnumerable<Holon>>(testHolons, "Holons loaded successfully (using test data)");
                }

                return result;
            }
            catch (Exception ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    var testHolons = TestDataHelper.GetTestHolons(5);
                    return TestDataHelper.CreateSuccessResponse<IEnumerable<Holon>>(testHolons, "Holons loaded successfully (using test data)");
                }
                return TestDataHelper.CreateErrorResponse<IEnumerable<Holon>>($"Error loading holons: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Load's all holons for the given HolonType. Use 'All' to load all holons.
        /// Set the loadChildren flag to true to load all the holon's child holon's. This defaults to true.
        /// If loadChildren is set to true, you can set the Recursive flag to true to load all the child's holon's recursively, or false to only load the first level of child holon's. This defaults to true.
        /// If loadChildren is set to true, you can set the maxChildDepth value to a custom int of how many levels down you wish to load, it defaults to 0, which means it will load to infinite depth.
        /// Set the continueOnError flag to true if you wish it to continue loading child holon's even if an error has occured, this defaults to true.
        /// Set the Version int to the version of the holon you wish to load (defaults to 0) which means the latest version.
        /// </summary>
        /// <param name="holonType"></param>
        /// <param name="loadChildren"></param>
        /// <param name="recursive"></param>
        /// <param name="maxChildDepth"></param>
        /// <param name="continueOnError"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("load-all-holons/{holonType}/{loadChildren}/{recursive}/{maxChildDepth}/{continueOnError}/{version}")]
        public async Task<OASISHttpResponseMessage<IEnumerable<Holon>>> LoadAllHolons(string holonType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return await LoadAllHolons(new LoadAllHolonsRequest()
            {
                HolonType = holonType,
                LoadChildren = loadChildren,
                Recursive = recursive,
                MaxChildDepth = maxChildDepth,
                ContinueOnError = continueOnError,
                Version = version
            });

            //OASISResult<IEnumerable<Holon>> response = new OASISResult<IEnumerable<Holon>>();
            //OASISResult<IEnumerable<IHolon>> result = await HolonManager.LoadAllHolonsAsync(holonType, loadChildren, recursive, maxChildDepth, continueOnError, version);

            //OASISResultHelper<IEnumerable<IHolon>, IEnumerable<Holon>>.CopyResult(result, response);
            //response.Result = Mapper.Convert(result.Result);

            //return HttpResponseHelper.FormatResponse(response);
        }

       
      /// <summary>
      /// Load's all holons for the given HolonType. Use 'All' to load all holons.
      /// Set the loadChildren flag to true to load all the holon's child holon's. This defaults to true.
      /// If loadChildren is set to true, you can set the Recursive flag to true to load all the child's holon's recursively, or false to only load the first level of child holon's. This defaults to true.
      /// If loadChildren is set to true, you can set the maxChildDepth value to a custom int of how many levels down you wish to load, it defaults to 0, which means it will load to infinite depth.
      /// Set the continueOnError flag to true if you wish it to continue loading child holon's even if an error has occured, this defaults to true.
      /// Set the Version int to the version of the holon you wish to load (defaults to 0) which means the latest version.
      /// Pass in the provider you wish to use.
      /// Set the setglobally flag to false to use these settings only for this request or true for it to be used for all future requests.
      /// </summary>
      /// <param name="holonType"></param>
      /// <param name="loadChildren"></param>
      /// <param name="recursive"></param>
      /// <param name="maxChildDepth"></param>
      /// <param name="continueOnError"></param>
      /// <param name="version"></param>
      /// <param name="providerType">Pass in the provider you wish to use.</param>
      /// <param name="setGlobally"> Set this to false for this provider to be used only for this request or true for it to be used for all future requests too.</param>
      /// <returns></returns>
      [Authorize]
      [HttpGet("load-all-holons/{holonType}/{loadChildren}/{recursive}/{maxChildDepth}/{continueOnError}/{version}/{providerType}/{setGlobally}")]
      public async Task<OASISHttpResponseMessage<IEnumerable<Holon>>> LoadAllHolons(string holonType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, string providerType = "Default", bool setGlobally = false)
      {
          return await LoadAllHolons(new LoadAllHolonsRequest()
          {
              HolonType = holonType,
              LoadChildren = loadChildren,
              Recursive = recursive,
              MaxChildDepth = maxChildDepth,
              ContinueOnError = continueOnError,
              Version = version,
              ProviderType = providerType,
              SetGlobally = setGlobally
          });
      }

      /// <summary>
      /// Load's all holons for the given HolonType. Use 'All' to load all holons.
      /// Set the loadChildren flag to true to load all the holon's child holon's. This defaults to true.
      /// If loadChildren is set to true, you can set the Recursive flag to true to load all the child's holon's recursively, or false to only load the first level of child holon's. This defaults to true.
      /// If loadChildren is set to true, you can set the maxChildDepth value to a custom int of how many levels down you wish to load, it defaults to 0, which means it will load to infinite depth.
      /// Set the continueOnError flag to true if you wish it to continue loading child holon's even if an error has occured, this defaults to true.
      /// Set the Version int to the version of the holon you wish to load (defaults to 0) which means the latest version.
      /// Pass in the provider you wish to use.
      /// Set the autoFailOverMode to 'ON' if you wish this call to work through the the providers in the auto-failover list until it succeeds. Set it to OFF if you do not or to 'DEFAULT' to default to the global OASISDNA setting.
      /// Set the autoReplicationMode to 'ON' if you wish this call to auto-replicate to the providers in the auto-replication list. Set it to OFF if you do not or to UseGlobalDefaultInOASISDNA to 'DEFAULT' to the global OASISDNA setting.
      /// Set the autoLoadBalanceMode to 'ON' if you wish this call to use the fastest provider in your area from the auto-loadbalance list. Set it to OFF if you do not or to UseGlobalDefaultInOASISDNA to 'DEFAULT' to the global OASISDNA setting.
      /// Set the waitForAutoReplicationResult flag to true if you wish for the API to wait for the auto-replication to complete before returning the results.
      /// Set the setglobally flag to false to use these settings only for this request or true for it to be used for all future requests.
      /// Set the showDetailedSettings flag to true to view detailed settings such as the list of providers in the auto-failover, auto-replication &amp; auto-load balance lists.
      /// </summary>
      /// <param name="holonType"></param>
      /// <param name="loadChildren"></param>
      /// <param name="recursive"></param>
      /// <param name="maxChildDepth"></param>
      /// <param name="continueOnError"></param>
      /// <param name="version"></param>
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
      [HttpGet("load-all-holons/{holonType}/{loadChildren}/{recursive}/{maxChildDepth}/{continueOnError}/{version}/{providerType}/{setGlobally}/{autoReplicationMode}/{autoFailOverMode}/{autoLoadBalanceMode}/{autoReplicationProviders}/{autoFailOverProviders}/{autoLoadBalanceProviders}/{waitForAutoReplicationResult}/{showDetailedSettings}")]
      public async Task<OASISHttpResponseMessage<IEnumerable<Holon>>> LoadAllHolons(string holonType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, string providerType = "Default", bool setGlobally = false, string autoReplicationMode = "DEFAULT", string autoFailOverMode = "DEFAULT", string autoLoadBalanceMode = "DEFAULT", string autoReplicationProviders = "DEFAULT", string autoFailOverProviders = "DEFAULT", string autoLoadBalanceProviders = "DEFAULT", bool waitForAutoReplicationResult = false, bool showDetailedSettings = false)
      {
          return await LoadAllHolons(new LoadAllHolonsRequest()
          {
              HolonType = holonType,
              LoadChildren = loadChildren,
              Recursive = recursive,
              MaxChildDepth = maxChildDepth,
              ContinueOnError = continueOnError,
              Version = version,
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
      /// Load's all holons for the given parent and the given HolonType. Use 'All' to load all holons.
      /// Set the loadChildren flag to true to load all the holon's child holon's. This defaults to true.
      /// If loadChildren is set to true, you can set the Recursive flag to true to load all the child's holon's recursively, or false to only load the first level of child holon's. This defaults to true.
      /// If loadChildren is set to true, you can set the maxChildDepth value to a custom int of how many levels down you wish to load, it defaults to 0, which means it will load to infinite depth.
      /// Set the continueOnError flag to true if you wish it to continue loading child holon's even if an error has occured, this defaults to true.
      /// Set the Version int to the version of the holon you wish to load (defaults to 0) which means the latest version.
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
      [HttpPost("load-holons-for-parent")]
      public async Task<OASISHttpResponseMessage<IEnumerable<Holon>>> LoadHolonsForParent(LoadHolonsForParentRequest request)
      {
          OASISHttpResponseMessage<IEnumerable<Holon>> response;
          (response, HolonType holonType) = ValidateHolonType<IEnumerable<Holon>>(request.HolonType);
          (response, HolonType childHolonType) = ValidateHolonType<IEnumerable<Holon>>(request.ChildHolonType);

          if (response.Result.IsError)
            return response;

          OASISConfigResult<IEnumerable<Holon>> configResult = ConfigureOASISEngine<IEnumerable<Holon>>(request);

          if (configResult.IsError && configResult.Response != null)
              return configResult.Response;

            //HolonType holonType = HolonType.All;
            //Object holonTypeObject = null;

            //if (Enum.TryParse(typeof(HolonType), request.ChildHolonType, out holonTypeObject))
            //    holonType = (HolonType)holonTypeObject;
            //else
            //    return new OASISResult<IEnumerable<Holon>>() { IsError = true, Message = $"The FromProviderType is not a valid OASIS NFT Provider. It must be one of the following:  {EnumHelper.GetEnumValues(typeof(ProviderType), EnumHelperListType.ItemsSeperatedByComma)}" };


          OASISResult<IEnumerable<IHolon>> result = await HolonManager.LoadHolonsForParentAsync(request.Id, holonType, request.LoadChildren, request.Recursive, request.MaxChildDepth, request.ContinueOnError, request.LoadChildrenFromProvider, 0, childHolonType, request.Version);

          OASISResultHelper<IHolon, Holon>.CopyResult(result, response.Result);
          response.Result.Result = Mapper.Convert<IHolon, Holon>(result.Result);
          ResetOASISSettings(request, configResult);

          return HttpResponseHelper.FormatResponse(response, System.Net.HttpStatusCode.OK, request.ShowDetailedSettings);

          //(OASISHttpResponseMessage<IEnumerable<Holon>> response, HolonType holonType) = ValidateHolonType<IEnumerable<Holon>>(request.HolonType);

          //if (response.Result.IsError)
          //    return response;

          //return await LoadHolonsForParent(request.Id, holonType, request.LoadChildren, request.Recursive, request.MaxChildDepth, request.ContinueOnError, request.Version);
      }

      /// <summary>
      /// Load's all holons for the given parent and the given HolonType. Use 'All' to load all holons.
      /// </summary>
      /// <param name="id"></param>
      /// <param name="holonType"></param>
      /// <returns></returns>
      [Authorize]
      [HttpGet("load-holons-for-parent/{id}/{holonType}")]
      public async Task<OASISHttpResponseMessage<IEnumerable<Holon>>> LoadHolonsForParent(Guid id, string holonType)
      {
          return await LoadHolonsForParent(new LoadHolonsForParentRequest() { Id = id, HolonType = holonType });
          //return await LoadHolonsForParent(id, holonType, true, true, 0, true, 0);
      }


      
     /// <summary>
     /// Load's all holons for the given parent and the given HolonType. Use 'All' to load all holons.
     /// Set the loadChildren flag to true to load all the holon's child holon's. This defaults to true.
     /// If loadChildren is set to true, you can set the Recursive flag to true to load all the child's holon's recursively, or false to only load the first level of child holon's. This defaults to true.
     /// If loadChildren is set to true, you can set the maxChildDepth value to a custom int of how many levels down you wish to load, it defaults to 0, which means it will load to infinite depth.
     /// Set the continueOnError flag to true if you wish it to continue loading child holon's even if an error has occured, this defaults to true.
     /// Set the Version int to the version of the holon you wish to load (defaults to 0) which means the latest version.
     /// </summary>
     /// <param name="id"></param>
     /// <param name="holonType"></param>
     /// <param name="loadChildren"></param>
     /// <param name="recursive"></param>
     /// <param name="maxChildDepth"></param>
     /// <param name="continueOnError"></param>
     /// <param name="version"></param>
     /// <returns></returns>
     [Authorize]
     [HttpGet("load-holons-for-parent/{id}/{holonType}/{loadChildren}/{recursive}/{maxChildDepth}/{continueOnError}/{version}")]
     public async Task<OASISHttpResponseMessage<IEnumerable<Holon>>> LoadHolonsForParent(Guid id, string holonType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
     {
         return await LoadHolonsForParent(new LoadHolonsForParentRequest()
         {
             Id = id,
             HolonType = holonType,
             LoadChildren = loadChildren,
             Recursive = recursive,
             MaxChildDepth = maxChildDepth,
             ContinueOnError = continueOnError,
             Version = version
         });

         //OASISResult<IEnumerable<Holon>> response = new OASISResult<IEnumerable<Holon>>();
         //OASISResult<IEnumerable<IHolon>> result = await HolonManager.LoadHolonsForParentAsync(id, holonType, loadChildren, recursive, maxChildDepth, continueOnError, version);

         //OASISResultHelper<IEnumerable<IHolon>, IEnumerable<Holon>>.CopyResult(result, response);
         //response.Result = Mapper.Convert(result.Result);

         //return HttpResponseHelper.FormatResponse(response);
     }

     /// <summary>
     /// Load's all holons for the given parent and the given HolonType. Use 'All' to load all holons.
     /// Set the loadChildren flag to true to load all the holon's child holon's. This defaults to true.
     /// If loadChildren is set to true, you can set the Recursive flag to true to load all the child's holon's recursively, or false to only load the first level of child holon's. This defaults to true.
     /// If loadChildren is set to true, you can set the maxChildDepth value to a custom int of how many levels down you wish to load, it defaults to 0, which means it will load to infinite depth.
     /// Set the continueOnError flag to true if you wish it to continue loading child holon's even if an error has occured, this defaults to true.
     /// Set the Version int to the version of the holon you wish to load (defaults to 0) which means the latest version.
     /// Pass in the provider you wish to use.
     /// Set the setglobally flag to false to use these settings only for this request or true for it to be used for all future requests.
     /// </summary>
     /// <param name="id"></param>
     /// <param name="holonType"></param>
     /// <param name="loadChildren"></param>
     /// <param name="recursive"></param>
     /// <param name="maxChildDepth"></param>
     /// <param name="continueOnError"></param>
     /// <param name="version"></param>
     /// <param name="providerType">Pass in the provider you wish to use.</param>
     /// <param name="setGlobally"> Set this to false for this provider to be used only for this request or true for it to be used for all future requests too.</param>
     /// <returns></returns>
     [Authorize]
     [HttpGet("load-holons-for-parent/{id}/{holonType}/{loadChildren}/{recursive}/{maxChildDepth}/{continueOnError}/{version}/{providerType}/{setGlobally}")]
     public async Task<OASISHttpResponseMessage<IEnumerable<Holon>>> LoadHolonsForParent(Guid id, string holonType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, string providerType = "Default", bool setGlobally = false)
     {
         return await LoadHolonsForParent(new LoadHolonsForParentRequest()
         {
             Id = id,
             HolonType = holonType,
             LoadChildren = loadChildren,
             Recursive = recursive,
             MaxChildDepth = maxChildDepth,
             ContinueOnError = continueOnError,
             Version = version,
             ProviderType = providerType,
             SetGlobally = setGlobally
         });

         //GetAndActivateProvider(providerType, setGlobally);
         //return await LoadHolonsForParent(id, holonType, loadChildren, recursive, maxChildDepth, continueOnError, 0);
     }

     /// <summary>
     /// Load's all holons for the given parent and the given HolonType. Use 'All' to load all holons.
     /// Set the loadChildren flag to true to load all the holon's child holon's. This defaults to true.
     /// If loadChildren is set to true, you can set the Recursive flag to true to load all the child's holon's recursively, or false to only load the first level of child holon's. This defaults to true.
     /// If loadChildren is set to true, you can set the maxChildDepth value to a custom int of how many levels down you wish to load, it defaults to 0, which means it will load to infinite depth.
     /// Set the continueOnError flag to true if you wish it to continue loading child holon's even if an error has occured, this defaults to true.
     /// Set the Version int to the version of the holon you wish to load (defaults to 0) which means the latest version.
     /// Pass in the provider you wish to use.
     /// Set the autoFailOverMode to 'ON' if you wish this call to work through the the providers in the auto-failover list until it succeeds. Set it to OFF if you do not or to 'DEFAULT' to default to the global OASISDNA setting.
     /// Set the autoReplicationMode to 'ON' if you wish this call to auto-replicate to the providers in the auto-replication list. Set it to OFF if you do not or to UseGlobalDefaultInOASISDNA to 'DEFAULT' to the global OASISDNA setting.
     /// Set the autoLoadBalanceMode to 'ON' if you wish this call to use the fastest provider in your area from the auto-loadbalance list. Set it to OFF if you do not or to UseGlobalDefaultInOASISDNA to 'DEFAULT' to the global OASISDNA setting.
     /// Set the waitForAutoReplicationResult flag to true if you wish for the API to wait for the auto-replication to complete before returning the results.
     /// Set the setglobally flag to false to use these settings only for this request or true for it to be used for all future requests.
     /// Set the showDetailedSettings flag to true to view detailed settings such as the list of providers in the auto-failover, auto-replication &amp; auto-load balance lists.
     /// </summary>
     /// <param name="id"></param>
     /// <param name="holonType"></param>
     /// <param name="loadChildren"></param>
     /// <param name="recursive"></param>
     /// <param name="maxChildDepth"></param>
     /// <param name="continueOnError"></param>
     /// <param name="version"></param>
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
     [HttpGet("load-holons-for-parent/{id}/{holonType}/{loadChildren}/{recursive}/{maxChildDepth}/{continueOnError}/{version}/{providerType}/{setGlobally}/{autoReplicationMode}/{autoFailOverMode}/{autoLoadBalanceMode}/{autoReplicationProviders}/{autoFailOverProviders}/{autoLoadBalanceProviders}/{waitForAutoReplicationResult}/{showDetailedSettings}")]
     public async Task<OASISHttpResponseMessage<IEnumerable<Holon>>> LoadHolonsForParent(Guid id, string holonType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, string providerType = "Default", bool setGlobally = false, string autoReplicationMode = "DEFAULT", string autoFailOverMode = "DEFAULT", string autoLoadBalanceMode = "DEFAULT", string autoReplicationProviders = "DEFAULT", string autoFailOverProviders = "DEFAULT", string autoLoadBalanceProviders = "DEFAULT", bool waitForAutoReplicationResult = false, bool showDetailedSettings = true)
     {
         return await LoadHolonsForParent(new LoadHolonsForParentRequest()
         {
             Id = id,
             HolonType = holonType,
             LoadChildren = loadChildren,
             Recursive = recursive,
             MaxChildDepth = maxChildDepth,
             ContinueOnError = continueOnError,
             Version = version,
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
        public async Task<OASISHttpResponseMessage<string>> LoadData(LoadDataRequest request)
        {
            try
            {
                OASISConfigResult<string> configResult = ConfigureOASISEngine<string>(request);

                if (configResult.IsError && configResult.Response != null)
                    return configResult.Response;

                OASISResult<string> response = AvatarManager.Instance.LoadData(request?.Key, AvatarId);
                ResetOASISSettings(request, configResult);

                return HttpResponseHelper.FormatResponse(response, System.Net.HttpStatusCode.OK, request.ShowDetailedSettings);
            }
            catch (Exception ex)
            {
                return TestDataHelper.CreateErrorResponse<string>($"Error loading data: {ex.Message}", ex, System.Net.HttpStatusCode.OK);
            }
        }

        /// <summary>
        /// Loads custom data with the given key from the current logged in avatar.
        /// </summary>
        /// <param name="key">The key for the data.</param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("load-data/{key}/{value}")]
        public async Task<OASISHttpResponseMessage<string>> LoadData(string key)
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
        public async Task<OASISHttpResponseMessage<string>> LoadData(string key, string providerType = "", bool setGlobally = false)
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
        public async Task<OASISHttpResponseMessage<string>> LoadData(string key, string providerType = "", bool setGlobally = false, string autoReplicationMode = "DEFAULT", string autoFailOverMode = "DEFAULT", string autoLoadBalanceMode = "DEFAULT", string autoReplicationProviders = "DEFAULT", string autoFailOverProviders = "DEFAULT", string autoLoadBalanceProviders = "DEFAULT", bool waitForAutoReplicationResult = false, bool showDetailedSettings = false)
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
