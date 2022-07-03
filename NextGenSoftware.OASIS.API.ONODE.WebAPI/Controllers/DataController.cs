using System;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Security;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
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
                    OASISResult<IOASISStorageProvider> result = OASISBootLoader.OASISBootLoader.GetAndActivateDefaultProvider();

                    if (result.IsError)
                        ErrorHandling.HandleError(ref result, string.Concat("Error calling OASISBootLoader.OASISBootLoader.GetAndActivateDefaultProvider(). Error details: ", result.Message), true, false, true);

                    _holonManager = new HolonManager(result.Result);
                }

                return _holonManager;
            }
        }

        //public DataController(IOptions<OASISDNA> OASISSettings) 
        public DataController()
        {
            //_settings = OASISSettings.Value;
        }


        ///// <summary>
        ///// Load's a holon data object for the given id.
        ///// </summary>
        ///// <param name="id"></param>
        ///// <returns></returns>
        //[Authorize]
        //[HttpGet("load-holon/{id}")]
        //public async Task<OASISHttpResponseMessage<Holon>> LoadHolon(Guid id)
        //{
        //    return await LoadHolon(id, true, true, 0, true, 0);
        //}

        ///// <summary>
        ///// Load's a holon data object for the given id.
        ///// </summary>
        ///// <param name="id"></param>
        ///// <param name="loadChildren"></param>
        ///// <param name="recursive"></param>
        ///// <param name="maxChildDepth"></param>
        ///// <param name="continueOnError"></param>
        ///// <param name="version"></param>
        ///// <returns></returns>
        //[Authorize]
        //[HttpGet("load-holon/{id}/{loadChildren}/{recursive}/{maxChildDepth}/{continueOnError}/{version}")]
        //public async Task<OASISHttpResponseMessage<Holon>> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        //{
        //    OASISResult<Holon> response = new OASISResult<Holon>();
        //    OASISResult<IHolon> result = await HolonManager.LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, version);

        //    OASISResultHelper<IHolon, Holon>.CopyResult(result, response);
        //    response.Result = (Holon)result.Result;

        //    return HttpResponseHelper.FormatResponse(response);
        //}

        ///// <summary>
        ///// Load's a holon data object for the given id.
        ///// </summary>
        ///// <param name="id"></param>
        ///// <param name="request"></param>
        ///// <returns></returns>
        //[Authorize]
        //[HttpPost("load-holon")]
        //public async Task<OASISHttpResponseMessage<Holon>> LoadHolon(LoadHolonRequest request)
        //{
        //    return await LoadHolon(request.Id, request.LoadChildren, request.Recursive, request.MaxChildDepth, request.ContinueOnError, request.Version);
        //}

        ///// <summary>
        ///// Load's a holon data object for the given id.
        ///// </summary>
        ///// <param name="id"></param>
        ///// <param name="loadChildren"></param>
        ///// <param name="recursive"></param>
        ///// <param name="maxChildDepth"></param>
        ///// <param name="continueOnError"></param>
        ///// <param name="version"></param>
        ///// <param name="providerType">Pass in the provider you wish to use.</param>
        ///// <param name="setGlobally"> Set this to false for this provider to be used only for this request or true for it to be used for all future requests too.</param>
        ///// <returns></returns>
        //[Authorize]
        //[HttpGet("load-holon/{id}/{loadChildren}/{recursive}/{maxChildDepth}/{continueOnError}/{version}/{providerType}/{setGlobally}")]
        //public async Task<OASISHttpResponseMessage<Holon>> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default, bool setGlobally = false)
        //{
        //    // TODO: Finish implementing (will tie into the HDK/ODK/Star project)
        //    GetAndActivateProvider(providerType, setGlobally);
        //    return await LoadHolon(id, loadChildren, recursive, maxChildDepth, continueOnError, version);
        //}


        /// <summary>
        /// Load's a holon data object for the given id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("load-holon/{id}")]
        public async Task<OASISHttpResponseMessage<Holon>> LoadHolon(Guid id)
        {
            return await LoadHolon(new LoadHolonRequest() { Id = id });
        }

        /// <summary>
        /// Load's a holon data object for the given id.
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
        public async Task<OASISHttpResponseMessage<Holon>> LoadHolon(LoadHolonRequest request, Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            request.Id = id;
            request.LoadChildren = loadChildren;
            request.Recursive = recursive;
            request.MaxChildDepth = maxChildDepth;
            request.ContinueOnError = continueOnError;
            request.Version = version;
            
            return await LoadHolon(request);
        }

        /// <summary>
        /// Load's a holon data object for the given id.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("load-holon")]
        public async Task<OASISHttpResponseMessage<Holon>> LoadHolon(LoadHolonRequest request)
        {
            OASISResult<Holon> response = new OASISResult<Holon>();
            
            OASISConfigResult<Holon> configResult = ConfigureOASISSettings<Holon>(request);

            if (configResult.IsError && configResult.Response != null)
                return configResult.Response;

            OASISResult<IHolon> result = await HolonManager.LoadHolonAsync(request.Id, request.LoadChildren, request.Recursive, request.MaxChildDepth, request.ContinueOnError, request.Version);
            ResetOASISSettings(request, configResult);

            OASISResultHelper<IHolon, Holon>.CopyResult(result, response);
            response.Result = (Holon)result.Result;

            return HttpResponseHelper.FormatResponse(response, System.Net.HttpStatusCode.OK, request.ShowDetailedSettings);
            //return HttpResponseHelper.FormatResponse(response, System.Net.HttpStatusCode.OK, request.ShowDetailedSettings, configResult.AutoFailOverMode, configResult.AutoReplicationMode, configResult.AutoLoadBalanceMode);
        }

        /// <summary>
        /// Load's a holon data object for the given id.
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
        public async Task<OASISHttpResponseMessage<Holon>> LoadHolon(LoadHolonRequest request, Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default, bool setGlobally = false)
        {
            // TODO: Finish implementing (will tie into the HDK/ODK/Star project)
            GetAndActivateProvider(providerType, setGlobally);
            return await LoadHolon(request, id, loadChildren, recursive, maxChildDepth, continueOnError, version);
        }

        /// <summary>
        /// Load's a holon data object for the given id.
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
        [HttpGet("load-holon/{id}/{loadChildren}/{recursive}/{maxChildDepth}/{continueOnError}/{version}/{providerType}/{setGlobally}/{autoReplicationEnabled}/{autoFailOverEnabled}/{autoLoadBalanceEnabled}/{autoReplicationProviders}/{autoFailOverProviders}/{AutoLoadBalanceProviders}/{waitForAutoReplicationResult}/{showDetailedSettings}")]
        public async Task<OASISHttpResponseMessage<Holon>> LoadHolon(LoadHolonRequest request, Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, string providerType = "Default", bool setGlobally = false, string autoReplicationEnabled = "default", string autoFailOverEnabled = "default", string autoLoadBalanceEnabled = "default", string autoReplicationProviders = "default", string autoFailOverProviders = "default", string autoLoadBalanceProviders = "default", bool waitForAutoReplicationResult = false, bool showDetailedSettings = false)
        {
            // TODO: Finish implementing (will tie into the HDK/ODK/Star project)
            //GetAndActivateProvider(providerType, setGlobally);

            request.ProviderType = providerType;
            request.SetGlobally = setGlobally;
            request.ShowDetailedSettings = showDetailedSettings;
            request.WaitForAutoReplicationResult = waitForAutoReplicationResult;
            request.AutoReplicationProviders = autoReplicationProviders;
            request.AutoFailOverProviders = autoFailOverProviders;
            request.AutoLoadBalanceProviders = autoLoadBalanceProviders;
            request.AutoReplicationEnabled = autoReplicationEnabled;
            request.AutoFailOverEnabled = autoFailOverEnabled;
            request.AutoLoadBalanceEnabled = autoLoadBalanceEnabled;

            return await LoadHolon(request, id, loadChildren, recursive, maxChildDepth, continueOnError, version);
        }

        /// <summary>
        /// Load's all holons
        /// </summary>
        /// <param name="holonType"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("load-all-holons/{holonType}")]
        public async Task<OASISHttpResponseMessage<IEnumerable<Holon>>> LoadAllHolons(HolonType holonType = HolonType.All)
        {
            return await LoadAllHolons(holonType, true, true, 0, true, 0);
        }

        /// <summary>
        /// Load's all holons
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpPost("load-all-holons")]
        public async Task<OASISHttpResponseMessage<IEnumerable<Holon>>> LoadAllHolons(LoadAllHolonsRequest request)
        {
            (OASISHttpResponseMessage<IEnumerable<Holon>> response, HolonType holonType) = ValidateHolonType<IEnumerable<Holon>>(request.HolonType);

            if (response.Result.IsError)
                return response;

            return await LoadAllHolons(holonType, request.LoadChildren, request.Recursive, request.MaxChildDepth, request.ContinueOnError, request.Version);
        }

        /// <summary>
        /// Load's all holons
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
        public async Task<OASISHttpResponseMessage<IEnumerable<Holon>>> LoadAllHolons(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            OASISResult<IEnumerable<Holon>> response = new OASISResult<IEnumerable<Holon>>();
            OASISResult<IEnumerable<IHolon>> result = await HolonManager.LoadAllHolonsAsync(holonType, loadChildren, recursive, maxChildDepth, continueOnError, version);

            OASISResultHelper<IEnumerable<IHolon>, IEnumerable<Holon>>.CopyResult(result, response);
            response.Result = Mapper.Convert(result.Result);

            return HttpResponseHelper.FormatResponse(response);
        }

        /// <summary>
        /// Load's all holons
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
        public async Task<OASISHttpResponseMessage<IEnumerable<Holon>>> LoadAllHolons(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return await LoadAllHolons(holonType, loadChildren, recursive, maxChildDepth, continueOnError, version);
        }

        /// <summary>
        /// Load's all holons for parent
        /// </summary>
        /// <param name="id"></param>
        /// <param name="holonType"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("load-holons-for-parent/{id}/{holonType}")]
        public async Task<OASISHttpResponseMessage<IEnumerable<Holon>>> LoadHolonsForParent(Guid id, HolonType holonType = HolonType.All)
        {
            return await LoadHolonsForParent(id, holonType, true, true, 0, true, 0);
        }

        /// <summary>
        /// Load's all holons for parent
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpPost("load-holons-for-parent")]
        public async Task<OASISHttpResponseMessage<IEnumerable<Holon>>> LoadHolonsForParent(LoadHolonsForParentRequest request)
        {
            (OASISHttpResponseMessage<IEnumerable<Holon>> response, HolonType holonType) = ValidateHolonType<IEnumerable<Holon>>(request.HolonType);

            if (response.Result.IsError)
                return response;

            return await LoadHolonsForParent(request.Id, holonType, request.LoadChildren, request.Recursive, request.MaxChildDepth, request.ContinueOnError, request.Version);
        }

        /// <summary>
        /// Load's all holons for parent
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
        public async Task<OASISHttpResponseMessage<IEnumerable<Holon>>> LoadHolonsForParent(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            OASISResult<IEnumerable<Holon>> response = new OASISResult<IEnumerable<Holon>>();
            OASISResult<IEnumerable<IHolon>> result = await HolonManager.LoadHolonsForParentAsync(id, holonType, loadChildren, recursive, maxChildDepth, continueOnError, version);

            OASISResultHelper<IEnumerable<IHolon>, IEnumerable<Holon>>.CopyResult(result, response);
            response.Result = Mapper.Convert(result.Result);

            return HttpResponseHelper.FormatResponse(response);
        }

        /// <summary>
        /// Load's all holons for parent
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
        public async Task<OASISHttpResponseMessage<IEnumerable<Holon>>> LoadHolonsForParent(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return await LoadHolonsForParent(id, holonType, loadChildren, recursive, maxChildDepth, continueOnError, 0);
        }

        /// <summary>
        /// Save's a holon data object.
        /// </summary>
        /// <param name="holon"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("save-holon")]
        public async Task<OASISHttpResponseMessage<Holon>> SaveHolon(Holon holon)
        {
            OASISResult<Holon> response = new OASISResult<Holon>();
            OASISResult<IHolon> result = await HolonManager.SaveHolonAsync(holon);

            OASISResultHelper<IHolon, Holon>.CopyResult(result, response);
            response.Result = (Holon)result.Result;

            return HttpResponseHelper.FormatResponse(response);
        }

        /// <summary>
        /// Save's a holon data object.
        /// </summary>
        /// <param name="holon"></param>
        /// <param name="providerType">Pass in the provider you wish to use.</param>
        /// <param name="setGlobally"> Set this to false for this provider to be used only for this request or true for it to be used for all future requests too.</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("save-holon/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<Holon>> SaveHolon(Holon holon, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return await SaveHolon(holon);
        }

        ///// <summary>
        ///// Save's a holon data object (meta data) to the given off-chain provider and then links its hash to the on-chain provider.
        ///// </summary>
        ///// <param name="holon"></param>
        ///// <param name="offChainProviderType"></param>
        ///// <param name="onChainProviderType"></param>
        ///// <returns></returns>
        //[Authorize]
        //[HttpPost("save-holon-off-chain/{holon}/{offChainProviderType}/{onChainProviderType}")]
        //public async Task<OASISHttpResponseMessage<Holon>> SaveHolon(Holon holon, ProviderType offChainProviderType, ProviderType onChainProviderType)
        //{
        //    // TODO: Finish implementing (will tie into the HDK/ODK/Star project)
        //    //GetAndActivateProvider(providerType, setGlobally);
        //    return HttpResponseHelper.FormatResponse(new OASISResult<Holon>
        //    {
        //        IsError = false,
        //        Message = "COMING SOON..."
        //    });
        //}

        /// <summary>
        /// Save's a holon data object (meta data) to the given off-chain provider and then links its hash to the on-chain provider.
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpPost("save-holon-off-chain")]
        public async Task<OASISHttpResponseMessage<Holon>> SaveHolon(SaveHolonRequest request)
        {
            // TODO: Finish implementing (will tie into the HDK/ODK/Star project)
            //GetAndActivateProvider(providerType, setGlobally);
            return HttpResponseHelper.FormatResponse(new OASISResult<Holon>
            {
                IsError = false,
                Message = "COMING SOON..."
            });
        }


        /// <summary>
        /// Delete a holon for the given id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Authorize]
        [HttpDelete("delete-holon/{id}")]
        public async Task<OASISResult<bool>> DeleteHolon(Guid id)
        {
            return await HolonManager.DeleteHolonAsync(id);
        }

        ///// <summary>
        ///// Delete a holon for the given id.
        ///// </summary>
        ///// <param name="id"></param>
        ///// <returns></returns>
        //[Authorize]
        //[HttpDelete("delete-holon")]
        //public async Task<OASISResult<bool>> DeleteHolon(//Insert Form Request here like above)
        //{
        //    return await HolonManager.DeleteHolonAsync(id);
        //}

        /// <summary>
        /// Delete a holon for the given id.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="providerType">Pass in the provider you wish to use.</param>
        /// <param name="setGlobally"> Set this to false for this provider to be used only for this request or true for it to be used for all future requests too.</param>
        /// <returns></returns>
        [Authorize]
        [HttpDelete("delete-holon/{id}/{providerType}/{setGlobally}")]
        public async Task<OASISResult<bool>> DeleteHolon(Guid id, ProviderType providerType, bool setGlobally = false)
        {
            // TODO: Finish implementing (will tie into the HDK/ODK/Star project)
            GetAndActivateProvider(providerType, setGlobally);
            return await DeleteHolon(id);
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
