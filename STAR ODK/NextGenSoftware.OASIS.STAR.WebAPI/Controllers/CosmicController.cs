using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Exceptions;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using System.Collections.Generic;
using System.Threading;

namespace NextGenSoftware.OASIS.STAR.WebAPI.Controllers
{
    /// <summary>
    /// COSMIC ORM management endpoints for creating, updating, and managing COSMIC celestial bodies and spaces.
    /// COSMICManager exposes the full COSMIC ORM / Omniverse object model to the STAR API.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public partial class CosmicController : STARControllerBase
    {
        private COSMICManager? _cosmicManager = null;
        private static readonly SemaphoreSlim _bootLock = new(1, 1);
        private static readonly NextGenSoftware.OASIS.API.Native.EndPoint.STARAPI _starAPI = new NextGenSoftware.OASIS.API.Native.EndPoint.STARAPI(new NextGenSoftware.OASIS.STAR.DNA.STARDNA());

        protected override NextGenSoftware.OASIS.API.Native.EndPoint.STARAPI GetStarAPI() => _starAPI;

        private async Task EnsureOASISBootedAsync()
        {
            if (OASISBootLoader.OASISBootLoader.IsOASISBooted)
                return;

            await _bootLock.WaitAsync();
            try
            {
                if (OASISBootLoader.OASISBootLoader.IsOASISBooted)
                    return;

                var bootResult = await OASISBootLoader.OASISBootLoader.BootOASISAsync(OASISBootLoader.OASISBootLoader.OASISDNAPath);
                if (bootResult.IsError)
                    throw new OASISException($"Failed to boot OASIS: {bootResult.Message}");
            }
            finally
            {
                _bootLock.Release();
            }
        }

        private COSMICManager CosmicManager
        {
            get
            {
                // Validate AvatarId first - this should be checked in controller methods, but double-check here
                var avatarId = AvatarId;
                if (avatarId == Guid.Empty)
                {
                    // If test mode is enabled, throw exception that will be caught and return test data
                    // Otherwise, throw validation exception
                    if (UseTestDataWhenLiveDataNotAvailable)
                        throw new Exception("AvatarId is required but was not found. Test mode enabled - will return test data.");
                    throw new OASISException("AvatarId is required but was not found. Please authenticate or provide X-Avatar-Id header.");
                }

                if (_cosmicManager == null)
                {
                    if (!OASISBootLoader.OASISBootLoader.IsOASISBooted)
                    {
                        // If test mode is enabled, throw exception that will be caught and return test data
                        if (UseTestDataWhenLiveDataNotAvailable)
                            throw new Exception("OASIS is not booted. Test mode enabled - will return test data.");
                        throw new OASISException("OASIS is not booted. Please boot the OASIS before accessing the COSMIC property!");
                    }

                    var providerResult = Task.Run(OASISBootLoader.OASISBootLoader.GetAndActivateDefaultStorageProviderAsync).Result;
                    if (providerResult.IsError)
                    {
                        // If test mode is enabled, throw exception that will be caught and return test data
                        if (UseTestDataWhenLiveDataNotAvailable)
                            throw new Exception($"Error getting storage provider: {providerResult.Message}. Test mode enabled - will return test data.");
                        throw new OASISException($"Error getting storage provider: {providerResult.Message}");
                    }

                    _cosmicManager = new COSMICManager(providerResult.Result, avatarId, OASISBootLoader.OASISBootLoader.OASISDNA);
                }

                return _cosmicManager;
            }
        }


    }
}
