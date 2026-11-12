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
    public partial class DataController : OASISControllerBase
    {
        //  OASISDNA _settings;
        HolonManager _holonManager = null;

        HolonManager HolonManager
        {
            get
            {
                if (_holonManager == null)
                {
                    // Ordinary V2 reads also need an unbound manager: binding the
                    // node default here turns Default into an explicit provider
                    // before request-owned HyperDrive balancing can run.
                    if(AdmittedStorageManagerAcquisition.TryCreate(
                        ()=>OASISBootLoader.OASISBootLoader.OASISDNA,
                        ()=>ProviderManager.Instance.CurrentStorageProvider,
                        (provider,dna)=>new HolonManager(null,dna,ProviderManager.Instance),out HolonManager admitted, admissionOnly: false))
                    {
                        _holonManager=admitted;
                        return _holonManager;
                    }
                    OASISResult<IOASISStorageProvider> result = Task.Run(OASISBootLoader.OASISBootLoader.GetAndActivateDefaultStorageProviderAsync).Result;

                    if (result.IsError)
                        OASISErrorHandling.HandleError(ref result, string.Concat("Error calling OASISBootLoader.OASISBootLoader.GetAndActivateDefaultStorageProvider(). Error details: ", result.Message));

                    _holonManager = new HolonManager(result.Result);
                }

                return _holonManager;
            }
        }

        // Explicit trusted composition keeps provider ownership in the manager.
        // The public parameterless constructor remains the default MVC binding.
        internal DataController(HolonManager manager)
        {
            _holonManager = manager ?? throw new ArgumentNullException(nameof(manager));
        }

        public DataController()
        {

        }

        private static OASISResult<T> RequestContextError<T>(OASISResult<HolonRequestContext> context) =>
            new OASISResult<T>
            {
                IsError = true, ErrorCount = 1, ErrorCode = context.ErrorCode,
                Message = context.Message, Exception = context.Exception
            };

    }
}
