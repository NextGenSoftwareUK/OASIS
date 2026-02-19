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
    public class CosmicController : STARControllerBase
    {
        private COSMICManager _cosmicManager = null;
        private static readonly SemaphoreSlim _bootLock = new(1, 1);

        private IActionResult ValidateAvatarId<T>()
        {
            // Skip validation if test mode is enabled - let the method try to execute and return test data on failure
            if (UseTestDataWhenLiveDataNotAvailable)
                return null;
                
            if (AvatarId == Guid.Empty)
            {
                return BadRequest(new OASISResult<T>
                {
                    IsError = true,
                    Message = "AvatarId is required but was not found. Please authenticate or provide X-Avatar-Id header."
                });
            }
            return null;
        }

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

        #region Property Access

        /// <summary>
        /// Gets the Omniverse instance.
        /// </summary>
        /// <returns>The Omniverse instance.</returns>
        /// <response code="200">Omniverse retrieved successfully</response>
        /// <response code="400">Error retrieving Omniverse</response>
        [HttpGet("omniverse")]
        [ProducesResponseType(typeof(OASISResult<IOmiverse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IOmiverse>), StatusCodes.Status400BadRequest)]
        public IActionResult GetOmniverse()
        {
            try
            {
                var omniverse = CosmicManager.Omiverse;

                // Return test data if setting is enabled and real data is null
                if (UseTestDataWhenLiveDataNotAvailable && omniverse == null)
                {
                    return Ok(new OASISResult<IOmiverse>
                    {
                        Result = null,
                        IsError = false,
                        Message = "Omniverse retrieved successfully (using test mode - real data unavailable)"
                    });
                }

                if (omniverse == null)
                {
                    return BadRequest(new OASISResult<IOmiverse>
                    {
                        IsError = true,
                        Message = "Omniverse not available and test data is disabled"
                    });
                }

                return Ok(new OASISResult<IOmiverse>
                {
                    Result = omniverse,
                    IsError = false,
                    Message = "Omniverse retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    return Ok(new OASISResult<IOmiverse>
                    {
                        Result = null,
                        IsError = false,
                        Message = "Omniverse retrieved successfully (using test mode - real data unavailable)"
                    });
                }
                return HandleException<IOmiverse>(ex, "GetOmniverse");
            }
        }

        #endregion

        #region Get Children Methods

        /// <summary>
        /// Gets children of a specific type for a parent holon.
        /// </summary>
        /// <param name="parentId">The parent holon ID.</param>
        /// <param name="parentHolonType">The parent holon type.</param>
        /// <param name="childHolonType">The child holon type.</param>
        /// <returns>List of child holons.</returns>
        /// <response code="200">Children retrieved successfully</response>
        /// <response code="400">Error retrieving children</response>
        [HttpGet("children/{parentId}")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<IHolon>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<IHolon>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetChildrenForParent(Guid parentId, [FromQuery] string parentHolonType, [FromQuery] string childHolonType)
        {
            try
            {
                var parentTypeEnum = Enum.Parse<HolonType>(parentHolonType);
                var childTypeEnum = Enum.Parse<HolonType>(childHolonType);
                var result = await CosmicManager.GetChildrenForParentAsync<IHolon>(parentId, parentTypeEnum, childTypeEnum);
                
                // Return test data if setting is enabled and result is null, has error, or result is null
                if (UseTestDataWhenLiveDataNotAvailable && TestDataHelper.ShouldUseTestData(result))
                {
                    var testHolons = TestDataHelper.GetTestHolons(3);
                    return Ok(TestDataHelper.CreateSuccessResult<IEnumerable<IHolon>>(testHolons, "Children retrieved successfully (using test data)"));
                }
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    var testHolons = TestDataHelper.GetTestHolons(3);
                    return Ok(TestDataHelper.CreateSuccessResult<IEnumerable<IHolon>>(testHolons, "Children retrieved successfully (using test data)"));
                }
                return HandleException<IEnumerable<IHolon>>(ex, "GetChildrenForParent");
            }
        }

        #endregion

        #region Search Methods

        /// <summary>
        /// Searches children for a parent holon.
        /// </summary>
        /// <param name="searchTerm">The search term.</param>
        /// <param name="parentId">The parent holon ID.</param>
        /// <param name="parentHolonType">The parent holon type.</param>
        /// <param name="childHolonType">The child holon type.</param>
        /// <returns>List of matching holons.</returns>
        /// <response code="200">Search completed successfully</response>
        /// <response code="400">Error performing search</response>
        [HttpGet("search-children")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<IHolon>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<IHolon>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SearchChildrenForParent([FromQuery] string searchTerm, [FromQuery] Guid parentId, [FromQuery] string parentHolonType, [FromQuery] string childHolonType)
        {
            try
            {
                var parentTypeEnum = Enum.Parse<HolonType>(parentHolonType);
                var childTypeEnum = Enum.Parse<HolonType>(childHolonType);
                var result = await CosmicManager.SearchChildrenForParentAsync(searchTerm, parentId, parentTypeEnum, childTypeEnum);
                
                // Return test data if setting is enabled and result is null, has error, or result is null
                if (UseTestDataWhenLiveDataNotAvailable && TestDataHelper.ShouldUseTestData(result))
                {
                    var testHolons = TestDataHelper.GetTestHolons(3);
                    return Ok(TestDataHelper.CreateSuccessResult<IEnumerable<IHolon>>(testHolons, "Search completed successfully (using test data)"));
                }
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    var testHolons = TestDataHelper.GetTestHolons(3);
                    return Ok(TestDataHelper.CreateSuccessResult<IEnumerable<IHolon>>(testHolons, "Search completed successfully (using test data)"));
                }
                return HandleException<IEnumerable<IHolon>>(ex, "SearchChildrenForParent");
            }
        }

        /// <summary>
        /// Searches holons for a parent (async).
        /// </summary>
        /// <param name="searchTerm">The search term.</param>
        /// <param name="parentId">The parent holon ID.</param>
        /// <param name="parentHolonType">The parent holon type.</param>
        /// <param name="childHolonType">The child holon type.</param>
        /// <returns>List of matching holons.</returns>
        /// <response code="200">Search completed successfully</response>
        /// <response code="400">Error performing search</response>
        [HttpGet("search-holons")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<IHolon>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<IHolon>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SearchHolonsForParent([FromQuery] string searchTerm, [FromQuery] Guid parentId, [FromQuery] string parentHolonType, [FromQuery] string childHolonType)
        {
            try
            {
                var parentTypeEnum = Enum.Parse<HolonType>(parentHolonType);
                var childTypeEnum = Enum.Parse<HolonType>(childHolonType);
                var result = await CosmicManager.SearchHolonsForParentAsync<NextGenSoftware.OASIS.API.Core.Holons.Holon>(searchTerm, AvatarId, parentId, null, MetaKeyValuePairMatchMode.All, false, parentTypeEnum, ProviderType.Default, true, true, 0, true, false, childTypeEnum, 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<IHolon>>
                {
                    IsError = true,
                    Message = $"Error searching holons: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Searches holons for a parent (synchronous).
        /// </summary>
        /// <param name="searchTerm">The search term.</param>
        /// <param name="parentId">The parent holon ID.</param>
        /// <param name="parentHolonType">The parent holon type.</param>
        /// <param name="childHolonType">The child holon type.</param>
        /// <param name="searchOnlyForCurrentAvatar">Whether to search only for current avatar.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>List of matching holons.</returns>
        /// <response code="200">Search completed successfully</response>
        /// <response code="400">Error performing search</response>
        [HttpGet("search-holons-sync")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<IHolon>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<IHolon>>), StatusCodes.Status400BadRequest)]
        public IActionResult SearchHolonsForParentSync([FromQuery] string searchTerm, [FromQuery] Guid parentId, [FromQuery] string parentHolonType, [FromQuery] string childHolonType, [FromQuery] bool searchOnlyForCurrentAvatar = true, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            try
            {
                var holonTypeEnum = Enum.Parse<HolonType>(parentHolonType);
                var result = CosmicManager.SearchHolonsForParent<NextGenSoftware.OASIS.API.Core.Holons.Holon>(searchTerm, AvatarId, parentId, null, MetaKeyValuePairMatchMode.All, searchOnlyForCurrentAvatar, holonTypeEnum, providerType);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<IHolon>>
                {
                    IsError = true,
                    Message = $"Error searching holons: {ex.Message}",
                    Exception = ex
                });
            }
        }

        #endregion

        #region Omniverse Methods

        /// <summary>
        /// Saves an Omniverse.
        /// </summary>
        /// <param name="omniverse">The Omniverse to save.</param>
        /// <returns>The saved Omniverse.</returns>
        /// <response code="200">Omniverse saved successfully</response>
        /// <response code="400">Error saving Omniverse</response>
        [HttpPost("omniverse")]
        [ProducesResponseType(typeof(OASISResult<IOmiverse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IOmiverse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SaveOmniverse([FromBody] IOmiverse omniverse)
        {
            try
            {
                // Validate AvatarId FIRST before any property access that might throw
                if (AvatarId == Guid.Empty)
                {
                    // Return test data if setting is enabled, otherwise return error
                    if (UseTestDataWhenLiveDataNotAvailable)
                    {
                        return Ok(new OASISResult<IOmiverse>
                        {
                            Result = null,
                            IsError = false,
                            Message = "Omniverse saved successfully (using test mode - real data unavailable)"
                        });
                    }
                    return BadRequest(new OASISResult<IOmiverse>
                    {
                        IsError = true,
                        Message = "AvatarId is required but was not found. Please authenticate or provide X-Avatar-Id header."
                    });
                }

                if (omniverse == null)
                {
                    // Return test data if setting is enabled, otherwise return error
                    if (UseTestDataWhenLiveDataNotAvailable)
                    {
                        return Ok(new OASISResult<IOmiverse>
                        {
                            Result = null,
                            IsError = false,
                            Message = "Omniverse saved successfully (using test mode - real data unavailable)"
                        });
                    }
                    return BadRequest(new OASISResult<IOmiverse>
                    {
                        IsError = true,
                        Message = "Omniverse cannot be null. Please provide a valid Omniverse object in the request body."
                    });
                }

                await EnsureOASISBootedAsync();
                var result = await CosmicManager.SaveOmniverseAsync(omniverse);
                
                // Return test data if setting is enabled and result is null, has error, or result is null
                if (UseTestDataWhenLiveDataNotAvailable && TestDataHelper.ShouldUseTestData(result))
                {
                    return Ok(new OASISResult<IOmiverse>
                    {
                        Result = null,
                        IsError = false,
                        Message = "Omniverse saved successfully (using test mode - real data unavailable)"
                    });
                }
                
                if (result.IsError)
                    return BadRequest(result);
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    return Ok(new OASISResult<IOmiverse>
                    {
                        Result = null,
                        IsError = false,
                        Message = "Omniverse saved successfully (using test mode - real data unavailable)"
                    });
                }
                return HandleException<IOmiverse>(ex, "SaveOmniverse");
            }
        }

        /// <summary>
        /// Updates an Omniverse.
        /// </summary>
        /// <param name="omniverse">The Omniverse to update.</param>
        /// <param name="saveChildren">Whether to save children.</param>
        /// <param name="recursive">Whether to save recursively.</param>
        /// <param name="maxChildDepth">Maximum child depth.</param>
        /// <param name="continueOnError">Whether to continue on error.</param>
        /// <param name="saveChildrenOnProvider">Whether to save children on provider.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>The updated Omniverse.</returns>
        /// <response code="200">Omniverse updated successfully</response>
        /// <response code="400">Error updating Omniverse</response>
        [HttpPut("omniverse")]
        [ProducesResponseType(typeof(OASISResult<IOmiverse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IOmiverse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateOmniverse([FromBody] IOmiverse omniverse, [FromQuery] bool saveChildren = true, [FromQuery] bool recursive = true, [FromQuery] int maxChildDepth = 0, [FromQuery] bool continueOnError = true, [FromQuery] bool saveChildrenOnProvider = false, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            try
            {
                if (omniverse == null)
                {
                    return BadRequest(new OASISResult<IOmiverse>
                    {
                        IsError = true,
                        Message = "Omniverse cannot be null. Please provide a valid Omniverse object in the request body."
                    });
                }

                if (AvatarId == Guid.Empty)
                {
                    return BadRequest(new OASISResult<IOmiverse>
                    {
                        IsError = true,
                        Message = "AvatarId is required but was not found. Please authenticate or provide X-Avatar-Id header."
                    });
                }

                await EnsureOASISBootedAsync();
                var result = await CosmicManager.UpdateOmniverseAsync(omniverse, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
                
                if (result.IsError)
                    return BadRequest(result);
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<IOmiverse>(ex, "UpdateOmniverse");
            }
        }

        /// <summary>
        /// Deletes an Omniverse.
        /// </summary>
        /// <param name="omniverseId">The Omniverse ID.</param>
        /// <param name="softDelete">Whether to soft delete.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>True if deleted successfully.</returns>
        /// <response code="200">Omniverse deleted successfully</response>
        /// <response code="400">Error deleting Omniverse</response>
        [HttpDelete("omniverse/{omniverseId}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteOmniverse(Guid omniverseId, [FromQuery] bool softDelete = true, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            try
            {
                await EnsureOASISBootedAsync();
                var result = await CosmicManager.DeleteOmniverseAsync(omniverseId, softDelete, providerType);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "deleting Omniverse");
            }
        }

        #endregion

        #region Multiverse Methods

        /// <summary>
        /// Adds a Multiverse to an Omniverse.
        /// </summary>
        /// <param name="parentOmniverseId">The parent Omniverse ID.</param>
        /// <param name="multiverse">The Multiverse to add.</param>
        /// <returns>The added Multiverse.</returns>
        /// <response code="200">Multiverse added successfully</response>
        /// <response code="400">Error adding Multiverse</response>
        [HttpPost("omniverse/{parentOmniverseId}/multiverse")]
        [ProducesResponseType(typeof(OASISResult<IMultiverse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IMultiverse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddMultiverse(Guid parentOmniverseId, [FromBody] IMultiverse multiverse)
        {
            try
            {
                if (multiverse == null)
                {
                    return BadRequest(new OASISResult<IMultiverse>
                    {
                        IsError = true,
                        Message = "Multiverse cannot be null. Please provide a valid Multiverse object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<IMultiverse>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureOASISBootedAsync();
                var result = await CosmicManager.AddMultiverseAsync(parentOmniverseId, multiverse);
                
                if (result.IsError)
                    return BadRequest(result);
                
                return Ok(result);
            }
            catch (OASISException ex)
            {
                return BadRequest(new OASISResult<IMultiverse>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
            catch (Exception ex)
            {
                return HandleException<IMultiverse>(ex, "adding Multiverse");
            }
        }

        /// <summary>
        /// Updates a Multiverse.
        /// </summary>
        /// <param name="multiverse">The Multiverse to update.</param>
        /// <param name="saveChildren">Whether to save children.</param>
        /// <param name="recursive">Whether to save recursively.</param>
        /// <param name="maxChildDepth">Maximum child depth.</param>
        /// <param name="continueOnError">Whether to continue on error.</param>
        /// <param name="saveChildrenOnProvider">Whether to save children on provider.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>The updated Multiverse.</returns>
        /// <response code="200">Multiverse updated successfully</response>
        /// <response code="400">Error updating Multiverse</response>
        [HttpPut("multiverse")]
        [ProducesResponseType(typeof(OASISResult<IMultiverse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IMultiverse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateMultiverse([FromBody] IMultiverse multiverse, [FromQuery] bool saveChildren = true, [FromQuery] bool recursive = true, [FromQuery] int maxChildDepth = 0, [FromQuery] bool continueOnError = true, [FromQuery] bool saveChildrenOnProvider = false, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            try
            {
                if (multiverse == null)
                {
                    return BadRequest(new OASISResult<IMultiverse>
                    {
                        IsError = true,
                        Message = "Multiverse cannot be null. Please provide a valid Multiverse object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<IMultiverse>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureOASISBootedAsync();
                var result = await CosmicManager.UpdateMultiverseAsync(multiverse, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
                return Ok(result);
            }
            catch (OASISException ex)
            {
                return BadRequest(new OASISResult<IMultiverse>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
            catch (Exception ex)
            {
                return HandleException<IMultiverse>(ex, "updating Multiverse");
            }
        }

        /// <summary>
        /// Deletes a Multiverse.
        /// </summary>
        /// <param name="multiverseId">The Multiverse ID.</param>
        /// <param name="softDelete">Whether to soft delete.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>True if deleted successfully.</returns>
        /// <response code="200">Multiverse deleted successfully</response>
        /// <response code="400">Error deleting Multiverse</response>
        [HttpDelete("multiverse/{multiverseId}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteMultiverse(Guid multiverseId, [FromQuery] bool softDelete = true, [FromQuery] ProviderType providerType = ProviderType.Default)
        { try { await EnsureOASISBootedAsync(); var result = await CosmicManager.DeleteMultiverseAsync(multiverseId, softDelete, providerType);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "deleting Multiverse");
            }
        }

        #endregion

        #region Universe Methods

        /// <summary>
        /// Adds a Universe to a Multiverse.
        /// </summary>
        /// <param name="parentMultiverseId">The parent Multiverse ID.</param>
        /// <param name="universe">The Universe to add.</param>
        /// <returns>The added Universe.</returns>
        /// <response code="200">Universe added successfully</response>
        /// <response code="400">Error adding Universe</response>
        [HttpPost("multiverse/{parentMultiverseId}/universe")]
        [ProducesResponseType(typeof(OASISResult<IUniverse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IUniverse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddUniverse(Guid parentMultiverseId, [FromBody] IUniverse universe)
        {
            try
            {
                if (universe == null)
                {
                    return BadRequest(new OASISResult<IUniverse>
                    {
                        IsError = true,
                        Message = "Universe cannot be null. Please provide a valid Universe object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<IUniverse>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureOASISBootedAsync();
                var result = await CosmicManager.AddUniverseAsync(parentMultiverseId, universe);
                
                if (result.IsError)
                    return BadRequest(result);
                return Ok(result);
            }
            catch (OASISException ex)
            {
                return BadRequest(new OASISResult<IUniverse>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
            catch (Exception ex)
            {
                return HandleException<IUniverse>(ex, "adding Universe");
            }
        }

        /// <summary>
        /// Updates a Universe.
        /// </summary>
        /// <param name="universe">The Universe to update.</param>
        /// <param name="saveChildren">Whether to save children.</param>
        /// <param name="recursive">Whether to save recursively.</param>
        /// <param name="maxChildDepth">Maximum child depth.</param>
        /// <param name="continueOnError">Whether to continue on error.</param>
        /// <param name="saveChildrenOnProvider">Whether to save children on provider.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>The updated Universe.</returns>
        /// <response code="200">Universe updated successfully</response>
        /// <response code="400">Error updating Universe</response>
        [HttpPut("universe")]
        [ProducesResponseType(typeof(OASISResult<IUniverse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IUniverse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateUniverse([FromBody] IUniverse universe, [FromQuery] bool saveChildren = true, [FromQuery] bool recursive = true, [FromQuery] int maxChildDepth = 0, [FromQuery] bool continueOnError = true, [FromQuery] bool saveChildrenOnProvider = false, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            try
            {
                if (universe == null)
                {
                    return BadRequest(new OASISResult<IUniverse>
                    {
                        IsError = true,
                        Message = "Universe cannot be null. Please provide a valid Universe object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<IUniverse>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureOASISBootedAsync();
                var result = await CosmicManager.UpdateUniverseAsync(universe, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
                return Ok(result);
            }
            catch (OASISException ex)
            {
                return BadRequest(new OASISResult<IUniverse>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
            catch (Exception ex)
            {
                return HandleException<IUniverse>(ex, "updating Universe");
            }
        }

        /// <summary>
        /// Deletes a Universe.
        /// </summary>
        /// <param name="universeId">The Universe ID.</param>
        /// <param name="softDelete">Whether to soft delete.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>True if deleted successfully.</returns>
        /// <response code="200">Universe deleted successfully</response>
        /// <response code="400">Error deleting Universe</response>
        [HttpDelete("universe/{universeId}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteUniverse(Guid universeId, [FromQuery] bool softDelete = true, [FromQuery] ProviderType providerType = ProviderType.Default)
        { try { await EnsureOASISBootedAsync(); var result = await CosmicManager.DeleteUniverseAsync(universeId, softDelete, providerType);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "deleting Universe");
            }
        }

        #endregion

        #region GalaxyCluster Methods

        /// <summary>
        /// Adds a GalaxyCluster to a Universe.
        /// </summary>
        /// <param name="parentUniverseId">The parent Universe ID.</param>
        /// <param name="galaxyCluster">The GalaxyCluster to add.</param>
        /// <returns>The added GalaxyCluster.</returns>
        /// <response code="200">GalaxyCluster added successfully</response>
        /// <response code="400">Error adding GalaxyCluster</response>
        [HttpPost("universe/{parentUniverseId}/galaxy-cluster")]
        [ProducesResponseType(typeof(OASISResult<IGalaxyCluster>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IGalaxyCluster>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddGalaxyCluster(Guid parentUniverseId, [FromBody] IGalaxyCluster galaxyCluster)
        {
            try
            {
                if (galaxyCluster == null)
                {
                    return BadRequest(new OASISResult<IGalaxyCluster>
                    {
                        IsError = true,
                        Message = "GalaxyCluster cannot be null. Please provide a valid GalaxyCluster object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<IGalaxyCluster>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureOASISBootedAsync();
                var result = await CosmicManager.AddGalaxyClusterAsync(parentUniverseId, galaxyCluster);
                
                if (result.IsError)
                    return BadRequest(result);
                return Ok(result);
            }
            catch (OASISException ex)
            {
                return BadRequest(new OASISResult<IGalaxyCluster>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
            catch (Exception ex)
            {
                return HandleException<IGalaxyCluster>(ex, "adding GalaxyCluster");
            }
        }

        /// <summary>
        /// Updates a GalaxyCluster.
        /// </summary>
        /// <param name="galaxyCluster">The GalaxyCluster to update.</param>
        /// <param name="saveChildren">Whether to save children.</param>
        /// <param name="recursive">Whether to save recursively.</param>
        /// <param name="maxChildDepth">Maximum child depth.</param>
        /// <param name="continueOnError">Whether to continue on error.</param>
        /// <param name="saveChildrenOnProvider">Whether to save children on provider.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>The updated GalaxyCluster.</returns>
        /// <response code="200">GalaxyCluster updated successfully</response>
        /// <response code="400">Error updating GalaxyCluster</response>
        [HttpPut("galaxy-cluster")]
        [ProducesResponseType(typeof(OASISResult<IGalaxyCluster>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IGalaxyCluster>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateGalaxyCluster([FromBody] IGalaxyCluster galaxyCluster, [FromQuery] bool saveChildren = true, [FromQuery] bool recursive = true, [FromQuery] int maxChildDepth = 0, [FromQuery] bool continueOnError = true, [FromQuery] bool saveChildrenOnProvider = false, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            try
            {
                if (galaxyCluster == null)
                {
                    return BadRequest(new OASISResult<IGalaxyCluster>
                    {
                        IsError = true,
                        Message = "GalaxyCluster cannot be null. Please provide a valid GalaxyCluster object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<IGalaxyCluster>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureOASISBootedAsync();
                var result = await CosmicManager.UpdateGalaxyClusterAsync(galaxyCluster, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
                return Ok(result);
            }
            catch (OASISException ex)
            {
                return BadRequest(new OASISResult<IGalaxyCluster>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
            catch (Exception ex)
            {
                return HandleException<IGalaxyCluster>(ex, "updating GalaxyCluster");
            }
        }

        /// <summary>
        /// Deletes a GalaxyCluster.
        /// </summary>
        /// <param name="galaxyClusterId">The GalaxyCluster ID.</param>
        /// <param name="softDelete">Whether to soft delete.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>True if deleted successfully.</returns>
        /// <response code="200">GalaxyCluster deleted successfully</response>
        /// <response code="400">Error deleting GalaxyCluster</response>
        [HttpDelete("galaxy-cluster/{galaxyClusterId}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteGalaxyCluster(Guid galaxyClusterId, [FromQuery] bool softDelete = true, [FromQuery] ProviderType providerType = ProviderType.Default)
        { try { await EnsureOASISBootedAsync(); var result = await CosmicManager.DeleteGalaxyClusterAsync(galaxyClusterId, softDelete, providerType);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "deleting GalaxyCluster");
            }
        }

        #endregion

        #region Galaxy Methods

        /// <summary>
        /// Adds a Galaxy to a GalaxyCluster.
        /// </summary>
        /// <param name="parentGalaxyClusterId">The parent GalaxyCluster ID.</param>
        /// <param name="galaxy">The Galaxy to add.</param>
        /// <returns>The added Galaxy.</returns>
        /// <response code="200">Galaxy added successfully</response>
        /// <response code="400">Error adding Galaxy</response>
        [HttpPost("galaxy-cluster/{parentGalaxyClusterId}/galaxy")]
        [ProducesResponseType(typeof(OASISResult<IGalaxy>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IGalaxy>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddGalaxy(Guid parentGalaxyClusterId, [FromBody] IGalaxy galaxy)
        {
            try
            {
                if (galaxy == null)
                {
                    return BadRequest(new OASISResult<IGalaxy>
                    {
                        IsError = true,
                        Message = "Galaxy cannot be null. Please provide a valid Galaxy object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<IGalaxy>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureOASISBootedAsync();
                var result = await CosmicManager.AddGalaxyAsync(parentGalaxyClusterId, galaxy);
                
                if (result.IsError)
                    return BadRequest(result);
                
                return Ok(result);
            }
            catch (OASISException ex)
            {
                return BadRequest(new OASISResult<IGalaxy>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
            catch (Exception ex)
            {
                return HandleException<IGalaxy>(ex, "adding Galaxy");
            }
        }

        /// <summary>
        /// Updates a Galaxy.
        /// </summary>
        /// <param name="galaxy">The Galaxy to update.</param>
        /// <param name="saveChildren">Whether to save children.</param>
        /// <param name="recursive">Whether to save recursively.</param>
        /// <param name="maxChildDepth">Maximum child depth.</param>
        /// <param name="continueOnError">Whether to continue on error.</param>
        /// <param name="saveChildrenOnProvider">Whether to save children on provider.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>The updated Galaxy.</returns>
        /// <response code="200">Galaxy updated successfully</response>
        /// <response code="400">Error updating Galaxy</response>
        [HttpPut("galaxy")]
        [ProducesResponseType(typeof(OASISResult<IGalaxy>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IGalaxy>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateGalaxy([FromBody] IGalaxy galaxy, [FromQuery] bool saveChildren = true, [FromQuery] bool recursive = true, [FromQuery] int maxChildDepth = 0, [FromQuery] bool continueOnError = true, [FromQuery] bool saveChildrenOnProvider = false, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            try
            {
                if (galaxy == null)
                {
                    return BadRequest(new OASISResult<IGalaxy>
                    {
                        IsError = true,
                        Message = "Galaxy cannot be null. Please provide a valid Galaxy object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<IGalaxy>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureOASISBootedAsync();
                var result = await CosmicManager.UpdateGalaxyAsync(galaxy, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
                return Ok(result);
            }
            catch (OASISException ex)
            {
                return BadRequest(new OASISResult<IGalaxy>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
            catch (Exception ex)
            {
                return HandleException<IGalaxy>(ex, "updating Galaxy");
            }
        }

        /// <summary>
        /// Deletes a Galaxy.
        /// </summary>
        /// <param name="galaxyId">The Galaxy ID.</param>
        /// <param name="softDelete">Whether to soft delete.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>True if deleted successfully.</returns>
        /// <response code="200">Galaxy deleted successfully</response>
        /// <response code="400">Error deleting Galaxy</response>
        [HttpDelete("galaxy/{galaxyId}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteGalaxy(Guid galaxyId, [FromQuery] bool softDelete = true, [FromQuery] ProviderType providerType = ProviderType.Default)
        { try { await EnsureOASISBootedAsync(); var result = await CosmicManager.DeleteGalaxyAsync(galaxyId, softDelete, providerType);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "deleting Galaxy");
            }
        }

        #endregion

        #region SolarSystem Methods

        /// <summary>
        /// Adds a SolarSystem to a Galaxy.
        /// </summary>
        /// <param name="parentGalaxyId">The parent Galaxy ID.</param>
        /// <param name="solarSystem">The SolarSystem to add.</param>
        /// <returns>The added SolarSystem.</returns>
        /// <response code="200">SolarSystem added successfully</response>
        /// <response code="400">Error adding SolarSystem</response>
        [HttpPost("galaxy/{parentGalaxyId}/solar-system")]
        [ProducesResponseType(typeof(OASISResult<ISolarSystem>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<ISolarSystem>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddSolarSystem(Guid parentGalaxyId, [FromBody] ISolarSystem solarSystem)
        {
            try
            {
                if (solarSystem == null)
                {
                    return BadRequest(new OASISResult<ISolarSystem>
                    {
                        IsError = true,
                        Message = "SolarSystem cannot be null. Please provide a valid SolarSystem object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<ISolarSystem>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureOASISBootedAsync();
                var result = await CosmicManager.AddSolarSystemAsync(parentGalaxyId, solarSystem);
                
                if (result.IsError)
                    return BadRequest(result);
                
                return Ok(result);
            }
            catch (OASISException ex)
            {
                return BadRequest(new OASISResult<ISolarSystem>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
            catch (Exception ex)
            {
                return HandleException<ISolarSystem>(ex, "adding SolarSystem");
            }
        }

        /// <summary>
        /// Updates a SolarSystem.
        /// </summary>
        /// <param name="solarSystem">The SolarSystem to update.</param>
        /// <param name="saveChildren">Whether to save children.</param>
        /// <param name="recursive">Whether to save recursively.</param>
        /// <param name="maxChildDepth">Maximum child depth.</param>
        /// <param name="continueOnError">Whether to continue on error.</param>
        /// <param name="saveChildrenOnProvider">Whether to save children on provider.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>The updated SolarSystem.</returns>
        /// <response code="200">SolarSystem updated successfully</response>
        /// <response code="400">Error updating SolarSystem</response>
        [HttpPut("solar-system")]
        [ProducesResponseType(typeof(OASISResult<ISolarSystem>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<ISolarSystem>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateSolarSystem([FromBody] ISolarSystem solarSystem, [FromQuery] bool saveChildren = true, [FromQuery] bool recursive = true, [FromQuery] int maxChildDepth = 0, [FromQuery] bool continueOnError = true, [FromQuery] bool saveChildrenOnProvider = false, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            try
            {
                if (solarSystem == null)
                {
                    return BadRequest(new OASISResult<ISolarSystem>
                    {
                        IsError = true,
                        Message = "SolarSystem cannot be null. Please provide a valid SolarSystem object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<ISolarSystem>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureOASISBootedAsync();
                var result = await CosmicManager.UpdateSolarSystemAsync(solarSystem, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
                return Ok(result);
            }
            catch (OASISException ex)
            {
                return BadRequest(new OASISResult<ISolarSystem>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
            catch (Exception ex)
            {
                return HandleException<ISolarSystem>(ex, "updating SolarSystem");
            }
        }

        /// <summary>
        /// Deletes a SolarSystem.
        /// </summary>
        /// <param name="solarSystemId">The SolarSystem ID.</param>
        /// <param name="softDelete">Whether to soft delete.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>True if deleted successfully.</returns>
        /// <response code="200">SolarSystem deleted successfully</response>
        /// <response code="400">Error deleting SolarSystem</response>
        [HttpDelete("solar-system/{solarSystemId}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteSolarSystem(Guid solarSystemId, [FromQuery] bool softDelete = true, [FromQuery] ProviderType providerType = ProviderType.Default)
        { try { await EnsureOASISBootedAsync(); var result = await CosmicManager.DeleteSolarSystemAsync(solarSystemId, softDelete, providerType);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "deleting SolarSystem");
            }
        }

        #endregion

        #region Star Methods

        /// <summary>
        /// Adds a Star to a Galaxy.
        /// </summary>
        /// <param name="parentGalaxyId">The parent Galaxy ID.</param>
        /// <param name="star">The Star to add.</param>
        /// <returns>The added Star.</returns>
        /// <response code="200">Star added successfully</response>
        /// <response code="400">Error adding Star</response>
        [HttpPost("galaxy/{parentGalaxyId}/star")]
        [ProducesResponseType(typeof(OASISResult<IStar>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IStar>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddStar(Guid parentGalaxyId, [FromBody] IStar star)
        {
            try
            {
                if (star == null)
                {
                    return BadRequest(new OASISResult<IStar>
                    {
                        IsError = true,
                        Message = "Star cannot be null. Please provide a valid Star object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<IStar>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureOASISBootedAsync();
                var result = await CosmicManager.AddStarAsync(parentGalaxyId, star);
                
                if (result.IsError)
                    return BadRequest(result);
                
                return Ok(result);
            }
            catch (OASISException ex)
            {
                return BadRequest(new OASISResult<IStar>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
            catch (Exception ex)
            {
                return HandleException<IStar>(ex, "adding Star");
            }
        }

        /// <summary>
        /// Updates a Star.
        /// </summary>
        /// <param name="star">The Star to update.</param>
        /// <param name="saveChildren">Whether to save children.</param>
        /// <param name="recursive">Whether to save recursively.</param>
        /// <param name="maxChildDepth">Maximum child depth.</param>
        /// <param name="continueOnError">Whether to continue on error.</param>
        /// <param name="saveChildrenOnProvider">Whether to save children on provider.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>The updated Star.</returns>
        /// <response code="200">Star updated successfully</response>
        /// <response code="400">Error updating Star</response>
        [HttpPut("star")]
        [ProducesResponseType(typeof(OASISResult<IStar>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IStar>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateStar([FromBody] IStar star, [FromQuery] bool saveChildren = true, [FromQuery] bool recursive = true, [FromQuery] int maxChildDepth = 0, [FromQuery] bool continueOnError = true, [FromQuery] bool saveChildrenOnProvider = false, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            try
            {
                if (star == null)
                {
                    return BadRequest(new OASISResult<IStar>
                    {
                        IsError = true,
                        Message = "Star cannot be null. Please provide a valid Star object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<IStar>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureOASISBootedAsync();
                var result = await CosmicManager.UpdateStarAsync(star, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
                return Ok(result);
            }
            catch (OASISException ex)
            {
                return BadRequest(new OASISResult<IStar>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
            catch (Exception ex)
            {
                return HandleException<IStar>(ex, "updating Star");
            }
        }

        /// <summary>
        /// Deletes a Star.
        /// </summary>
        /// <param name="starId">The Star ID.</param>
        /// <param name="softDelete">Whether to soft delete.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>True if deleted successfully.</returns>
        /// <response code="200">Star deleted successfully</response>
        /// <response code="400">Error deleting Star</response>
        [HttpDelete("star/{starId}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteStar(Guid starId, [FromQuery] bool softDelete = true, [FromQuery] ProviderType providerType = ProviderType.Default)
        { try { await EnsureOASISBootedAsync(); var result = await CosmicManager.DeleteStarAsync(starId, softDelete, providerType);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "deleting Star");
            }
        }

        #endregion

        #region Planet Methods

        /// <summary>
        /// Adds a Planet to a SolarSystem.
        /// </summary>
        /// <param name="parentSolarSystemId">The parent SolarSystem ID.</param>
        /// <param name="planet">The Planet to add.</param>
        /// <returns>The added Planet.</returns>
        /// <response code="200">Planet added successfully</response>
        /// <response code="400">Error adding Planet</response>
        [HttpPost("solar-system/{parentSolarSystemId}/planet")]
        [ProducesResponseType(typeof(OASISResult<IPlanet>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IPlanet>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddPlanet(Guid parentSolarSystemId, [FromBody] IPlanet planet)
        {
            try
            {
                if (planet == null)
                {
                    return BadRequest(new OASISResult<IPlanet>
                    {
                        IsError = true,
                        Message = "Planet cannot be null. Please provide a valid Planet object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<IPlanet>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureOASISBootedAsync();
                var result = await CosmicManager.AddPlanetAsync(parentSolarSystemId, planet);
                
                if (result.IsError)
                    return BadRequest(result);
                
                return Ok(result);
            }
            catch (OASISException ex)
            {
                return BadRequest(new OASISResult<IPlanet>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
            catch (Exception ex)
            {
                return HandleException<IPlanet>(ex, "adding Planet");
            }
        }

        /// <summary>
        /// Updates a Planet.
        /// </summary>
        /// <param name="planet">The Planet to update.</param>
        /// <param name="saveChildren">Whether to save children.</param>
        /// <param name="recursive">Whether to save recursively.</param>
        /// <param name="maxChildDepth">Maximum child depth.</param>
        /// <param name="continueOnError">Whether to continue on error.</param>
        /// <param name="saveChildrenOnProvider">Whether to save children on provider.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>The updated Planet.</returns>
        /// <response code="200">Planet updated successfully</response>
        /// <response code="400">Error updating Planet</response>
        [HttpPut("planet")]
        [ProducesResponseType(typeof(OASISResult<IPlanet>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IPlanet>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdatePlanet([FromBody] IPlanet planet, [FromQuery] bool saveChildren = true, [FromQuery] bool recursive = true, [FromQuery] int maxChildDepth = 0, [FromQuery] bool continueOnError = true, [FromQuery] bool saveChildrenOnProvider = false, [FromQuery] ProviderType providerType = ProviderType.Default)
        { try { await EnsureOASISBootedAsync(); var result = await CosmicManager.UpdatePlanetAsync(planet, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<IPlanet>(ex, "updating Planet");
            }
        }

        /// <summary>
        /// Deletes a Planet.
        /// </summary>
        /// <param name="planetId">The Planet ID.</param>
        /// <param name="softDelete">Whether to soft delete.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>True if deleted successfully.</returns>
        /// <response code="200">Planet deleted successfully</response>
        /// <response code="400">Error deleting Planet</response>
        [HttpDelete("planet/{planetId}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeletePlanet(Guid planetId, [FromQuery] bool softDelete = true, [FromQuery] ProviderType providerType = ProviderType.Default)
        { try { await EnsureOASISBootedAsync(); var result = await CosmicManager.DeletePlanetAsync(planetId, softDelete, providerType);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "deleting Planet");
            }
        }

        #endregion

        #region Moon Methods

        /// <summary>
        /// Adds a Moon to a Planet.
        /// </summary>
        /// <param name="parentPlanetId">The parent Planet ID.</param>
        /// <param name="moon">The Moon to add.</param>
        /// <returns>The added Moon.</returns>
        /// <response code="200">Moon added successfully</response>
        /// <response code="400">Error adding Moon</response>
        [HttpPost("planet/{parentPlanetId}/moon")]
        [ProducesResponseType(typeof(OASISResult<IMoon>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IMoon>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddMoon(Guid parentPlanetId, [FromBody] IMoon moon)
        {
            try
            {
                if (moon == null)
                {
                    return BadRequest(new OASISResult<IMoon>
                    {
                        IsError = true,
                        Message = "Moon cannot be null. Please provide a valid Moon object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<IMoon>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureOASISBootedAsync();
                var planetLoad = await CosmicManager.Data.LoadHolonAsync(parentPlanetId);
                if (planetLoad.IsError || planetLoad.Result == null)
                {
                    return BadRequest(new OASISResult<IMoon>
                    {
                        IsError = true,
                        Message = $"Error loading parent planet: {planetLoad.Message}"
                    });
                }

                var planet = planetLoad.Result as IPlanet;
                if (planet == null)
                {
                    return BadRequest(new OASISResult<IMoon>
                    {
                        IsError = true,
                        Message = "Parent holon is not a Planet"
                    });
                }

                var result = await CosmicManager.AddMoonAsync(planet, moon);
                return Ok(result);
            }
            catch (OASISException ex)
            {
                return BadRequest(new OASISResult<IMoon>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
            catch (Exception ex)
            {
                return HandleException<IMoon>(ex, "adding Moon");
            }
        }

        /// <summary>
        /// Updates a Moon.
        /// </summary>
        /// <param name="moon">The Moon to update.</param>
        /// <param name="saveChildren">Whether to save children.</param>
        /// <param name="recursive">Whether to save recursively.</param>
        /// <param name="maxChildDepth">Maximum child depth.</param>
        /// <param name="continueOnError">Whether to continue on error.</param>
        /// <param name="saveChildrenOnProvider">Whether to save children on provider.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>The updated Moon.</returns>
        /// <response code="200">Moon updated successfully</response>
        /// <response code="400">Error updating Moon</response>
        [HttpPut("moon")]
        [ProducesResponseType(typeof(OASISResult<IMoon>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IMoon>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateMoon([FromBody] IMoon moon, [FromQuery] bool saveChildren = true, [FromQuery] bool recursive = true, [FromQuery] int maxChildDepth = 0, [FromQuery] bool continueOnError = true, [FromQuery] bool saveChildrenOnProvider = false, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            try
            {
                if (moon == null)
                {
                    return BadRequest(new OASISResult<IMoon>
                    {
                        IsError = true,
                        Message = "Moon cannot be null. Please provide a valid Moon object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<IMoon>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureOASISBootedAsync();
                var result = await CosmicManager.UpdateMoonAsync(moon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
                return Ok(result);
            }
            catch (OASISException ex)
            {
                return BadRequest(new OASISResult<IMoon>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
            catch (Exception ex)
            {
                return HandleException<IMoon>(ex, "updating Moon");
            }
        }

        /// <summary>
        /// Deletes a Moon.
        /// </summary>
        /// <param name="moonId">The Moon ID.</param>
        /// <param name="softDelete">Whether to soft delete.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>True if deleted successfully.</returns>
        /// <response code="200">Moon deleted successfully</response>
        /// <response code="400">Error deleting Moon</response>
        [HttpDelete("moon/{moonId}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteMoon(Guid moonId, [FromQuery] bool softDelete = true, [FromQuery] ProviderType providerType = ProviderType.Default)
        { try { await EnsureOASISBootedAsync(); var result = await CosmicManager.DeleteMoonAsync(moonId, softDelete, providerType);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "deleting Moon");
            }
        }

        #endregion

        #region Asteroid Methods

        /// <summary>
        /// Adds an Asteroid to a Galaxy.
        /// </summary>
        /// <param name="parentGalaxyId">The parent Galaxy ID.</param>
        /// <param name="asteroid">The Asteroid to add.</param>
        /// <returns>The added Asteroid.</returns>
        /// <response code="200">Asteroid added successfully</response>
        /// <response code="400">Error adding Asteroid</response>
        [HttpPost("galaxy/{parentGalaxyId}/asteroid")]
        [ProducesResponseType(typeof(OASISResult<IAsteroid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IAsteroid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddAsteroid(Guid parentGalaxyId, [FromBody] IAsteroid asteroid)
        {
            try
            {
                if (asteroid == null)
                {
                    return BadRequest(new OASISResult<IAsteroid>
                    {
                        IsError = true,
                        Message = "Asteroid cannot be null. Please provide a valid Asteroid object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<IAsteroid>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureOASISBootedAsync();
                var galaxyLoadResult = await CosmicManager.Data.LoadHolonAsync(parentGalaxyId, childHolonType: HolonType.Galaxy);
                if (galaxyLoadResult.IsError || galaxyLoadResult.Result == null)
                {
                    return BadRequest(new OASISResult<IAsteroid>
                    {
                        IsError = true,
                        Message = $"Error loading galaxy: {galaxyLoadResult.Message}"
                    });
                }
                var galaxy = galaxyLoadResult.Result as IGalaxy;
                if (galaxy == null)
                {
                    return BadRequest(new OASISResult<IAsteroid>
                    {
                        IsError = true,
                        Message = "Loaded holon is not a galaxy"
                    });
                }
                var result = await CosmicManager.AddAsteroidAsync(galaxy, asteroid);
                return Ok(result);
            }
            catch (OASISException ex)
            {
                return BadRequest(new OASISResult<IAsteroid>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
            catch (Exception ex)
            {
                return HandleException<IAsteroid>(ex, "adding Asteroid");
            }
        }

        /// <summary>
        /// Updates an Asteroid.
        /// </summary>
        /// <param name="asteroid">The Asteroid to update.</param>
        /// <param name="saveChildren">Whether to save children.</param>
        /// <param name="recursive">Whether to save recursively.</param>
        /// <param name="maxChildDepth">Maximum child depth.</param>
        /// <param name="continueOnError">Whether to continue on error.</param>
        /// <param name="saveChildrenOnProvider">Whether to save children on provider.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>The updated Asteroid.</returns>
        /// <response code="200">Asteroid updated successfully</response>
        /// <response code="400">Error updating Asteroid</response>
        [HttpPut("asteroid")]
        [ProducesResponseType(typeof(OASISResult<IAsteroid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IAsteroid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateAsteroid([FromBody] IAsteroid asteroid, [FromQuery] bool saveChildren = true, [FromQuery] bool recursive = true, [FromQuery] int maxChildDepth = 0, [FromQuery] bool continueOnError = true, [FromQuery] bool saveChildrenOnProvider = false, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            try
            {
                if (asteroid == null)
                {
                    return BadRequest(new OASISResult<IAsteroid>
                    {
                        IsError = true,
                        Message = "Asteroid cannot be null. Please provide a valid Asteroid object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<IAsteroid>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureOASISBootedAsync();
                var result = await CosmicManager.UpdateAsteroidAsync(asteroid, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
                return Ok(result);
            }
            catch (OASISException ex)
            {
                return BadRequest(new OASISResult<IAsteroid>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
            catch (Exception ex)
            {
                return HandleException<IAsteroid>(ex, "updating Asteroid");
            }
        }

        /// <summary>
        /// Deletes an Asteroid.
        /// </summary>
        /// <param name="asteroidId">The Asteroid ID.</param>
        /// <param name="softDelete">Whether to soft delete.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>True if deleted successfully.</returns>
        /// <response code="200">Asteroid deleted successfully</response>
        /// <response code="400">Error deleting Asteroid</response>
        [HttpDelete("asteroid/{asteroidId}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteAsteroid(Guid asteroidId, [FromQuery] bool softDelete = true, [FromQuery] ProviderType providerType = ProviderType.Default)
        { try { await EnsureOASISBootedAsync(); var result = await CosmicManager.DeleteAsteroidAsync(asteroidId, softDelete, providerType);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "deleting Asteroid");
            }
        }

        #endregion

        #region Comet Methods

        /// <summary>
        /// Adds a Comet to a Galaxy.
        /// </summary>
        /// <param name="parentGalaxyId">The parent Galaxy ID.</param>
        /// <param name="comet">The Comet to add.</param>
        /// <returns>The added Comet.</returns>
        /// <response code="200">Comet added successfully</response>
        /// <response code="400">Error adding Comet</response>
        [HttpPost("galaxy/{parentGalaxyId}/comet")]
        [ProducesResponseType(typeof(OASISResult<IComet>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IComet>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddComet(Guid parentGalaxyId, [FromBody] IComet comet)
        {
            try
            {
                if (comet == null)
                {
                    return BadRequest(new OASISResult<IComet>
                    {
                        IsError = true,
                        Message = "Comet cannot be null. Please provide a valid Comet object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<IComet>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureOASISBootedAsync();
                var galaxyLoadResult = await CosmicManager.Data.LoadHolonAsync(parentGalaxyId, childHolonType: HolonType.Galaxy);
                if (galaxyLoadResult.IsError || galaxyLoadResult.Result == null)
                {
                    return BadRequest(new OASISResult<IComet>
                    {
                        IsError = true,
                        Message = $"Error loading galaxy: {galaxyLoadResult.Message}"
                    });
                }
                var galaxy = galaxyLoadResult.Result as IGalaxy;
                if (galaxy == null)
                {
                    return BadRequest(new OASISResult<IComet>
                    {
                        IsError = true,
                        Message = "Loaded holon is not a galaxy"
                    });
                }
                var result = await CosmicManager.AddCometAsync(galaxy, comet);
                return Ok(result);
            }
            catch (OASISException ex)
            {
                return BadRequest(new OASISResult<IComet>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
            catch (Exception ex)
            {
                return HandleException<IComet>(ex, "adding Comet");
            }
        }

        /// <summary>
        /// Updates a Comet.
        /// </summary>
        /// <param name="comet">The Comet to update.</param>
        /// <param name="saveChildren">Whether to save children.</param>
        /// <param name="recursive">Whether to save recursively.</param>
        /// <param name="maxChildDepth">Maximum child depth.</param>
        /// <param name="continueOnError">Whether to continue on error.</param>
        /// <param name="saveChildrenOnProvider">Whether to save children on provider.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>The updated Comet.</returns>
        /// <response code="200">Comet updated successfully</response>
        /// <response code="400">Error updating Comet</response>
        [HttpPut("comet")]
        [ProducesResponseType(typeof(OASISResult<IComet>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IComet>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateComet([FromBody] IComet comet, [FromQuery] bool saveChildren = true, [FromQuery] bool recursive = true, [FromQuery] int maxChildDepth = 0, [FromQuery] bool continueOnError = true, [FromQuery] bool saveChildrenOnProvider = false, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            try
            {
                if (comet == null)
                {
                    return BadRequest(new OASISResult<IComet>
                    {
                        IsError = true,
                        Message = "Comet cannot be null. Please provide a valid Comet object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<IComet>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureOASISBootedAsync();
                var result = await CosmicManager.UpdateCometAsync(comet, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
                return Ok(result);
            }
            catch (OASISException ex)
            {
                return BadRequest(new OASISResult<IComet>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
            catch (Exception ex)
            {
                return HandleException<IComet>(ex, "updating Comet");
            }
        }

        /// <summary>
        /// Deletes a Comet.
        /// </summary>
        /// <param name="cometId">The Comet ID.</param>
        /// <param name="softDelete">Whether to soft delete.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>True if deleted successfully.</returns>
        /// <response code="200">Comet deleted successfully</response>
        /// <response code="400">Error deleting Comet</response>
        [HttpDelete("comet/{cometId}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteComet(Guid cometId, [FromQuery] bool softDelete = true, [FromQuery] ProviderType providerType = ProviderType.Default)
        { try { await EnsureOASISBootedAsync(); var result = await CosmicManager.DeleteCometAsync(cometId, softDelete, providerType);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "deleting Comet");
            }
        }

        #endregion

        #region Meteroid Methods

        /// <summary>
        /// Adds a Meteroid to a Galaxy.
        /// </summary>
        /// <param name="parentGalaxyId">The parent Galaxy ID.</param>
        /// <param name="meteroid">The Meteroid to add.</param>
        /// <returns>The added Meteroid.</returns>
        /// <response code="200">Meteroid added successfully</response>
        /// <response code="400">Error adding Meteroid</response>
        [HttpPost("galaxy/{parentGalaxyId}/meteroid")]
        [ProducesResponseType(typeof(OASISResult<IMeteroid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IMeteroid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddMeteroid(Guid parentGalaxyId, [FromBody] IMeteroid meteroid)
        {
            try
            {
                if (meteroid == null)
                {
                    return BadRequest(new OASISResult<IMeteroid>
                    {
                        IsError = true,
                        Message = "Meteroid cannot be null. Please provide a valid Meteroid object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<IMeteroid>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureOASISBootedAsync();
                var galaxyLoadResult = await CosmicManager.Data.LoadHolonAsync(parentGalaxyId, childHolonType: HolonType.Galaxy);
                if (galaxyLoadResult.IsError || galaxyLoadResult.Result == null)
                {
                    return BadRequest(new OASISResult<IMeteroid>
                    {
                        IsError = true,
                        Message = $"Error loading galaxy: {galaxyLoadResult.Message}"
                    });
                }
                var galaxy = galaxyLoadResult.Result as IGalaxy;
                if (galaxy == null)
                {
                    return BadRequest(new OASISResult<IMeteroid>
                    {
                        IsError = true,
                        Message = "Loaded holon is not a galaxy"
                    });
                }
                var result = await CosmicManager.AddMeteroidAsync(galaxy, meteroid);
                return Ok(result);
            }
            catch (OASISException ex)
            {
                return BadRequest(new OASISResult<IMeteroid>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
            catch (Exception ex)
            {
                return HandleException<IMeteroid>(ex, "adding Meteroid");
            }
        }

        /// <summary>
        /// Updates a Meteroid.
        /// </summary>
        /// <param name="meteroid">The Meteroid to update.</param>
        /// <param name="saveChildren">Whether to save children.</param>
        /// <param name="recursive">Whether to save recursively.</param>
        /// <param name="maxChildDepth">Maximum child depth.</param>
        /// <param name="continueOnError">Whether to continue on error.</param>
        /// <param name="saveChildrenOnProvider">Whether to save children on provider.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>The updated Meteroid.</returns>
        /// <response code="200">Meteroid updated successfully</response>
        /// <response code="400">Error updating Meteroid</response>
        [HttpPut("meteroid")]
        [ProducesResponseType(typeof(OASISResult<IMeteroid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IMeteroid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateMeteroid([FromBody] IMeteroid meteroid, [FromQuery] bool saveChildren = true, [FromQuery] bool recursive = true, [FromQuery] int maxChildDepth = 0, [FromQuery] bool continueOnError = true, [FromQuery] bool saveChildrenOnProvider = false, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            try
            {
                if (meteroid == null)
                {
                    return BadRequest(new OASISResult<IMeteroid>
                    {
                        IsError = true,
                        Message = "Meteroid cannot be null. Please provide a valid Meteroid object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<IMeteroid>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureOASISBootedAsync();
                var result = await CosmicManager.UpdateMeteroidAsync(meteroid, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
                return Ok(result);
            }
            catch (OASISException ex)
            {
                return BadRequest(new OASISResult<IMeteroid>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
            catch (Exception ex)
            {
                return HandleException<IMeteroid>(ex, "updating Meteroid");
            }
        }

        /// <summary>
        /// Deletes a Meteroid.
        /// </summary>
        /// <param name="meteroidId">The Meteroid ID.</param>
        /// <param name="softDelete">Whether to soft delete.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>True if deleted successfully.</returns>
        /// <response code="200">Meteroid deleted successfully</response>
        /// <response code="400">Error deleting Meteroid</response>
        [HttpDelete("meteroid/{meteroidId}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteMeteroid(Guid meteroidId, [FromQuery] bool softDelete = true, [FromQuery] ProviderType providerType = ProviderType.Default)
        { try { await EnsureOASISBootedAsync(); var result = await CosmicManager.DeleteMeteroidAsync(meteroidId, softDelete, providerType);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "deleting Meteroid");
            }
        }

        #endregion

        #region Collection Methods

        /// <summary>
        /// Gets all planets for a solar system.
        /// </summary>
        /// <param name="solarSystemId">The SolarSystem ID.</param>
        /// <returns>List of planets.</returns>
        /// <response code="200">Planets retrieved successfully</response>
        /// <response code="400">Error retrieving planets</response>
        [HttpGet("solar-system/{solarSystemId}/planets")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<IPlanet>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<IPlanet>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetPlanetsForSolarSystem(Guid solarSystemId)
        { try { await EnsureOASISBootedAsync(); var result = await CosmicManager.GetPlanetsForSolarSystemAsync(solarSystemId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<IPlanet>>
                {
                    IsError = true,
                    Message = $"Error retrieving planets: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Gets all solar systems for a galaxy.
        /// </summary>
        /// <param name="galaxyId">The Galaxy ID.</param>
        /// <returns>List of solar systems.</returns>
        /// <response code="200">Solar systems retrieved successfully</response>
        /// <response code="400">Error retrieving solar systems</response>
        [HttpGet("galaxy/{galaxyId}/solar-systems")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<ISolarSystem>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<ISolarSystem>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetSolarSystemsForGalaxy(Guid galaxyId)
        { try { await EnsureOASISBootedAsync(); var result = await CosmicManager.GetSolarSystemsForGalaxyAsync(galaxyId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<ISolarSystem>>
                {
                    IsError = true,
                    Message = $"Error retrieving solar systems: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Gets all stars for a galaxy.
        /// </summary>
        /// <param name="galaxyId">The Galaxy ID.</param>
        /// <returns>List of stars.</returns>
        /// <response code="200">Stars retrieved successfully</response>
        /// <response code="400">Error retrieving stars</response>
        [HttpGet("galaxy/{galaxyId}/stars")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<IStar>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<IStar>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetStarsForGalaxy(Guid galaxyId)
        { try { await EnsureOASISBootedAsync(); var result = await CosmicManager.GetStarsForGalaxyAsync(galaxyId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<IStar>>
                {
                    IsError = true,
                    Message = $"Error retrieving stars: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Gets all planets for a galaxy.
        /// </summary>
        /// <param name="galaxyId">The Galaxy ID.</param>
        /// <returns>List of planets.</returns>
        /// <response code="200">Planets retrieved successfully</response>
        /// <response code="400">Error retrieving planets</response>
        [HttpGet("galaxy/{galaxyId}/planets")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<IPlanet>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<IPlanet>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetPlanetsForGalaxy(Guid galaxyId)
        { try { await EnsureOASISBootedAsync(); var result = await CosmicManager.GetPlanetsForGalaxyAsync(galaxyId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<IPlanet>>
                {
                    IsError = true,
                    Message = $"Error retrieving planets: {ex.Message}",
                    Exception = ex
                });
            }
        }

        /// <summary>
        /// Gets all moons for a galaxy.
        /// </summary>
        /// <param name="galaxyId">The Galaxy ID.</param>
        /// <returns>List of moons.</returns>
        /// <response code="200">Moons retrieved successfully</response>
        /// <response code="400">Error retrieving moons</response>
        [HttpGet("galaxy/{galaxyId}/moons")]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<IMoon>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IEnumerable<IMoon>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetMoonsForGalaxy(Guid galaxyId)
        { try { await EnsureOASISBootedAsync(); var result = await CosmicManager.GetMoonsForGalaxyAsync(galaxyId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<IEnumerable<IMoon>>
                {
                    IsError = true,
                    Message = $"Error retrieving moons: {ex.Message}",
                    Exception = ex
                });
            }
        }

        #endregion

        #region Nebula Methods

        /// <summary>
        /// Updates a Nebula.
        /// </summary>
        /// <param name="nebula">The Nebula to update.</param>
        /// <param name="saveChildren">Whether to save children.</param>
        /// <param name="recursive">Whether to save recursively.</param>
        /// <param name="maxChildDepth">Maximum child depth.</param>
        /// <param name="continueOnError">Whether to continue on error.</param>
        /// <param name="saveChildrenOnProvider">Whether to save children on provider.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>The updated Nebula.</returns>
        /// <response code="200">Nebula updated successfully</response>
        /// <response code="400">Error updating Nebula</response>
        [HttpPut("nebula")]
        [ProducesResponseType(typeof(OASISResult<INebula>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<INebula>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateNebula([FromBody] INebula nebula, [FromQuery] bool saveChildren = true, [FromQuery] bool recursive = true, [FromQuery] int maxChildDepth = 0, [FromQuery] bool continueOnError = true, [FromQuery] bool saveChildrenOnProvider = false, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            try
            {
                if (nebula == null)
                {
                    return BadRequest(new OASISResult<INebula>
                    {
                        IsError = true,
                        Message = "Nebula cannot be null. Please provide a valid Nebula object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<INebula>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureOASISBootedAsync();
                var result = await CosmicManager.UpdateNebulaAsync(nebula, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
                return Ok(result);
            }
            catch (OASISException ex)
            {
                return BadRequest(new OASISResult<INebula>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
            catch (Exception ex)
            {
                return HandleException<INebula>(ex, "updating Nebula");
            }
        }

        /// <summary>
        /// Deletes a Nebula.
        /// </summary>
        /// <param name="nebulaId">The Nebula ID.</param>
        /// <param name="softDelete">Whether to soft delete.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>True if deleted successfully.</returns>
        /// <response code="200">Nebula deleted successfully</response>
        /// <response code="400">Error deleting Nebula</response>
        [HttpDelete("nebula/{nebulaId}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteNebula(Guid nebulaId, [FromQuery] bool softDelete = true, [FromQuery] ProviderType providerType = ProviderType.Default)
        { try { await EnsureOASISBootedAsync(); var result = await CosmicManager.DeleteNebulaAsync(nebulaId, softDelete, providerType);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "deleting Nebula");
            }
        }

        #endregion

        #region SuperVerse Methods

        /// <summary>
        /// Updates a SuperVerse.
        /// </summary>
        /// <param name="superVerse">The SuperVerse to update.</param>
        /// <param name="saveChildren">Whether to save children.</param>
        /// <param name="recursive">Whether to save recursively.</param>
        /// <param name="maxChildDepth">Maximum child depth.</param>
        /// <param name="continueOnError">Whether to continue on error.</param>
        /// <param name="saveChildrenOnProvider">Whether to save children on provider.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>The updated SuperVerse.</returns>
        /// <response code="200">SuperVerse updated successfully</response>
        /// <response code="400">Error updating SuperVerse</response>
        [HttpPut("superverse")]
        [ProducesResponseType(typeof(OASISResult<ISuperVerse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<ISuperVerse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateSuperVerse([FromBody] ISuperVerse superVerse, [FromQuery] bool saveChildren = true, [FromQuery] bool recursive = true, [FromQuery] int maxChildDepth = 0, [FromQuery] bool continueOnError = true, [FromQuery] bool saveChildrenOnProvider = false, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            try
            {
                if (superVerse == null)
                {
                    return BadRequest(new OASISResult<ISuperVerse>
                    {
                        IsError = true,
                        Message = "SuperVerse cannot be null. Please provide a valid SuperVerse object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<ISuperVerse>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureOASISBootedAsync();
                var result = await CosmicManager.UpdateSuperVerseAsync(superVerse, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
                return Ok(result);
            }
            catch (OASISException ex)
            {
                return BadRequest(new OASISResult<ISuperVerse>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
            catch (Exception ex)
            {
                return HandleException<ISuperVerse>(ex, "updating SuperVerse");
            }
        }

        /// <summary>
        /// Deletes a SuperVerse.
        /// </summary>
        /// <param name="superVerseId">The SuperVerse ID.</param>
        /// <param name="softDelete">Whether to soft delete.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>True if deleted successfully.</returns>
        /// <response code="200">SuperVerse deleted successfully</response>
        /// <response code="400">Error deleting SuperVerse</response>
        [HttpDelete("superverse/{superVerseId}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteSuperVerse(Guid superVerseId, [FromQuery] bool softDelete = true, [FromQuery] ProviderType providerType = ProviderType.Default)
        { try { await EnsureOASISBootedAsync(); var result = await CosmicManager.DeleteSuperVerseAsync(superVerseId, softDelete, providerType);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "deleting SuperVerse");
            }
        }

        #endregion

        #region WormHole Methods

        /// <summary>
        /// Updates a WormHole.
        /// </summary>
        /// <param name="wormHole">The WormHole to update.</param>
        /// <param name="saveChildren">Whether to save children.</param>
        /// <param name="recursive">Whether to save recursively.</param>
        /// <param name="maxChildDepth">Maximum child depth.</param>
        /// <param name="continueOnError">Whether to continue on error.</param>
        /// <param name="saveChildrenOnProvider">Whether to save children on provider.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>The updated WormHole.</returns>
        /// <response code="200">WormHole updated successfully</response>
        /// <response code="400">Error updating WormHole</response>
        [HttpPut("wormhole")]
        [ProducesResponseType(typeof(OASISResult<IWormHole>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IWormHole>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateWormHole([FromBody] IWormHole wormHole, [FromQuery] bool saveChildren = true, [FromQuery] bool recursive = true, [FromQuery] int maxChildDepth = 0, [FromQuery] bool continueOnError = true, [FromQuery] bool saveChildrenOnProvider = false, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            try
            {
                if (wormHole == null)
                {
                    return BadRequest(new OASISResult<IWormHole>
                    {
                        IsError = true,
                        Message = "WormHole cannot be null. Please provide a valid WormHole object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<IWormHole>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureOASISBootedAsync();
                var result = await CosmicManager.UpdateWormHoleAsync(wormHole, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
                return Ok(result);
            }
            catch (OASISException ex)
            {
                return BadRequest(new OASISResult<IWormHole>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
            catch (Exception ex)
            {
                return HandleException<IWormHole>(ex, "updating WormHole");
            }
        }

        /// <summary>
        /// Deletes a WormHole.
        /// </summary>
        /// <param name="wormHoleId">The WormHole ID.</param>
        /// <param name="softDelete">Whether to soft delete.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>True if deleted successfully.</returns>
        /// <response code="200">WormHole deleted successfully</response>
        /// <response code="400">Error deleting WormHole</response>
        [HttpDelete("wormhole/{wormHoleId}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteWormHole(Guid wormHoleId, [FromQuery] bool softDelete = true, [FromQuery] ProviderType providerType = ProviderType.Default)
        { try { await EnsureOASISBootedAsync(); var result = await CosmicManager.DeleteWormHoleAsync(wormHoleId, softDelete, providerType);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "deleting WormHole");
            }
        }

        #endregion

        #region BlackHole Methods

        /// <summary>
        /// Updates a BlackHole.
        /// </summary>
        /// <param name="blackHole">The BlackHole to update.</param>
        /// <param name="saveChildren">Whether to save children.</param>
        /// <param name="recursive">Whether to save recursively.</param>
        /// <param name="maxChildDepth">Maximum child depth.</param>
        /// <param name="continueOnError">Whether to continue on error.</param>
        /// <param name="saveChildrenOnProvider">Whether to save children on provider.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>The updated BlackHole.</returns>
        /// <response code="200">BlackHole updated successfully</response>
        /// <response code="400">Error updating BlackHole</response>
        [HttpPut("blackhole")]
        [ProducesResponseType(typeof(OASISResult<IBlackHole>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IBlackHole>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateBlackHole([FromBody] IBlackHole blackHole, [FromQuery] bool saveChildren = true, [FromQuery] bool recursive = true, [FromQuery] int maxChildDepth = 0, [FromQuery] bool continueOnError = true, [FromQuery] bool saveChildrenOnProvider = false, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            try
            {
                if (blackHole == null)
                {
                    return BadRequest(new OASISResult<IBlackHole>
                    {
                        IsError = true,
                        Message = "BlackHole cannot be null. Please provide a valid BlackHole object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<IBlackHole>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureOASISBootedAsync();
                var result = await CosmicManager.UpdateBlackHoleAsync(blackHole, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
                return Ok(result);
            }
            catch (OASISException ex)
            {
                return BadRequest(new OASISResult<IBlackHole>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
            catch (Exception ex)
            {
                return HandleException<IBlackHole>(ex, "updating BlackHole");
            }
        }

        /// <summary>
        /// Deletes a BlackHole.
        /// </summary>
        /// <param name="blackHoleId">The BlackHole ID.</param>
        /// <param name="softDelete">Whether to soft delete.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>True if deleted successfully.</returns>
        /// <response code="200">BlackHole deleted successfully</response>
        /// <response code="400">Error deleting BlackHole</response>
        [HttpDelete("blackhole/{blackHoleId}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteBlackHole(Guid blackHoleId, [FromQuery] bool softDelete = true, [FromQuery] ProviderType providerType = ProviderType.Default)
        { try { await EnsureOASISBootedAsync(); var result = await CosmicManager.DeleteBlackHoleAsync(blackHoleId, softDelete, providerType);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "deleting BlackHole");
            }
        }

        #endregion

        #region Portal Methods

        /// <summary>
        /// Updates a Portal.
        /// </summary>
        /// <param name="portal">The Portal to update.</param>
        /// <param name="saveChildren">Whether to save children.</param>
        /// <param name="recursive">Whether to save recursively.</param>
        /// <param name="maxChildDepth">Maximum child depth.</param>
        /// <param name="continueOnError">Whether to continue on error.</param>
        /// <param name="saveChildrenOnProvider">Whether to save children on provider.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>The updated Portal.</returns>
        /// <response code="200">Portal updated successfully</response>
        /// <response code="400">Error updating Portal</response>
        [HttpPut("portal")]
        [ProducesResponseType(typeof(OASISResult<IPortal>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IPortal>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdatePortal([FromBody] IPortal portal, [FromQuery] bool saveChildren = true, [FromQuery] bool recursive = true, [FromQuery] int maxChildDepth = 0, [FromQuery] bool continueOnError = true, [FromQuery] bool saveChildrenOnProvider = false, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            try
            {
                if (portal == null)
                {
                    return BadRequest(new OASISResult<IPortal>
                    {
                        IsError = true,
                        Message = "Portal cannot be null. Please provide a valid Portal object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<IPortal>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureOASISBootedAsync();
                var result = await CosmicManager.UpdatePortalAsync(portal, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
                return Ok(result);
            }
            catch (OASISException ex)
            {
                return BadRequest(new OASISResult<IPortal>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
            catch (Exception ex)
            {
                return HandleException<IPortal>(ex, "updating Portal");
            }
        }

        /// <summary>
        /// Deletes a Portal.
        /// </summary>
        /// <param name="portalId">The Portal ID.</param>
        /// <param name="softDelete">Whether to soft delete.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>True if deleted successfully.</returns>
        /// <response code="200">Portal deleted successfully</response>
        /// <response code="400">Error deleting Portal</response>
        [HttpDelete("portal/{portalId}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeletePortal(Guid portalId, [FromQuery] bool softDelete = true, [FromQuery] ProviderType providerType = ProviderType.Default)
        { try { await EnsureOASISBootedAsync(); var result = await CosmicManager.DeletePortalAsync(portalId, softDelete, providerType);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "deleting Portal");
            }
        }

        #endregion

        #region StarGate Methods

        /// <summary>
        /// Updates a StarGate.
        /// </summary>
        /// <param name="starGate">The StarGate to update.</param>
        /// <param name="saveChildren">Whether to save children.</param>
        /// <param name="recursive">Whether to save recursively.</param>
        /// <param name="maxChildDepth">Maximum child depth.</param>
        /// <param name="continueOnError">Whether to continue on error.</param>
        /// <param name="saveChildrenOnProvider">Whether to save children on provider.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>The updated StarGate.</returns>
        /// <response code="200">StarGate updated successfully</response>
        /// <response code="400">Error updating StarGate</response>
        [HttpPut("stargate")]
        [ProducesResponseType(typeof(OASISResult<IStarGate>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IStarGate>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateStarGate([FromBody] IStarGate starGate, [FromQuery] bool saveChildren = true, [FromQuery] bool recursive = true, [FromQuery] int maxChildDepth = 0, [FromQuery] bool continueOnError = true, [FromQuery] bool saveChildrenOnProvider = false, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            try
            {
                if (starGate == null)
                {
                    return BadRequest(new OASISResult<IStarGate>
                    {
                        IsError = true,
                        Message = "StarGate cannot be null. Please provide a valid StarGate object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<IStarGate>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureOASISBootedAsync();
                var result = await CosmicManager.UpdateStarGateAsync(starGate, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
                return Ok(result);
            }
            catch (OASISException ex)
            {
                return BadRequest(new OASISResult<IStarGate>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
            catch (Exception ex)
            {
                return HandleException<IStarGate>(ex, "updating StarGate");
            }
        }

        /// <summary>
        /// Deletes a StarGate.
        /// </summary>
        /// <param name="starGateId">The StarGate ID.</param>
        /// <param name="softDelete">Whether to soft delete.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>True if deleted successfully.</returns>
        /// <response code="200">StarGate deleted successfully</response>
        /// <response code="400">Error deleting StarGate</response>
        [HttpDelete("stargate/{starGateId}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteStarGate(Guid starGateId, [FromQuery] bool softDelete = true, [FromQuery] ProviderType providerType = ProviderType.Default)
        { try { await EnsureOASISBootedAsync(); var result = await CosmicManager.DeleteStarGateAsync(starGateId, softDelete, providerType);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "deleting StarGate");
            }
        }

        #endregion

        #region SpaceTimeDistortion Methods

        /// <summary>
        /// Updates a SpaceTimeDistortion.
        /// </summary>
        /// <param name="distortion">The SpaceTimeDistortion to update.</param>
        /// <param name="saveChildren">Whether to save children.</param>
        /// <param name="recursive">Whether to save recursively.</param>
        /// <param name="maxChildDepth">Maximum child depth.</param>
        /// <param name="continueOnError">Whether to continue on error.</param>
        /// <param name="saveChildrenOnProvider">Whether to save children on provider.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>The updated SpaceTimeDistortion.</returns>
        /// <response code="200">SpaceTimeDistortion updated successfully</response>
        /// <response code="400">Error updating SpaceTimeDistortion</response>
        [HttpPut("spacetime-distortion")]
        [ProducesResponseType(typeof(OASISResult<ISpaceTimeDistortion>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<ISpaceTimeDistortion>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateSpaceTimeDistortion([FromBody] ISpaceTimeDistortion distortion, [FromQuery] bool saveChildren = true, [FromQuery] bool recursive = true, [FromQuery] int maxChildDepth = 0, [FromQuery] bool continueOnError = true, [FromQuery] bool saveChildrenOnProvider = false, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            try
            {
                if (distortion == null)
                {
                    return BadRequest(new OASISResult<ISpaceTimeDistortion>
                    {
                        IsError = true,
                        Message = "SpaceTimeDistortion cannot be null. Please provide a valid SpaceTimeDistortion object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<ISpaceTimeDistortion>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureOASISBootedAsync();
                var result = await CosmicManager.UpdateSpaceTimeDistortionAsync(distortion, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
                return Ok(result);
            }
            catch (OASISException ex)
            {
                return BadRequest(new OASISResult<ISpaceTimeDistortion>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
            catch (Exception ex)
            {
                return HandleException<ISpaceTimeDistortion>(ex, "updating SpaceTimeDistortion");
            }
        }

        /// <summary>
        /// Deletes a SpaceTimeDistortion.
        /// </summary>
        /// <param name="distortionId">The SpaceTimeDistortion ID.</param>
        /// <param name="softDelete">Whether to soft delete.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>True if deleted successfully.</returns>
        /// <response code="200">SpaceTimeDistortion deleted successfully</response>
        /// <response code="400">Error deleting SpaceTimeDistortion</response>
        [HttpDelete("spacetime-distortion/{distortionId}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteSpaceTimeDistortion(Guid distortionId, [FromQuery] bool softDelete = true, [FromQuery] ProviderType providerType = ProviderType.Default)
        { try { await EnsureOASISBootedAsync(); var result = await CosmicManager.DeleteSpaceTimeDistortionAsync(distortionId, softDelete, providerType);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "deleting SpaceTimeDistortion");
            }
        }

        #endregion

        #region SpaceTimeAbnormally Methods

        /// <summary>
        /// Updates a SpaceTimeAbnormally.
        /// </summary>
        /// <param name="abnormally">The SpaceTimeAbnormally to update.</param>
        /// <param name="saveChildren">Whether to save children.</param>
        /// <param name="recursive">Whether to save recursively.</param>
        /// <param name="maxChildDepth">Maximum child depth.</param>
        /// <param name="continueOnError">Whether to continue on error.</param>
        /// <param name="saveChildrenOnProvider">Whether to save children on provider.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>The updated SpaceTimeAbnormally.</returns>
        /// <response code="200">SpaceTimeAbnormally updated successfully</response>
        /// <response code="400">Error updating SpaceTimeAbnormally</response>
        [HttpPut("spacetime-abnormally")]
        [ProducesResponseType(typeof(OASISResult<ISpaceTimeAbnormally>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<ISpaceTimeAbnormally>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateSpaceTimeAbnormally([FromBody] ISpaceTimeAbnormally abnormally, [FromQuery] bool saveChildren = true, [FromQuery] bool recursive = true, [FromQuery] int maxChildDepth = 0, [FromQuery] bool continueOnError = true, [FromQuery] bool saveChildrenOnProvider = false, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            try
            {
                if (abnormally == null)
                {
                    return BadRequest(new OASISResult<ISpaceTimeAbnormally>
                    {
                        IsError = true,
                        Message = "SpaceTimeAbnormally cannot be null. Please provide a valid SpaceTimeAbnormally object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<ISpaceTimeAbnormally>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureOASISBootedAsync();
                var result = await CosmicManager.UpdateSpaceTimeAbnormallyAsync(abnormally, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
                return Ok(result);
            }
            catch (OASISException ex)
            {
                return BadRequest(new OASISResult<ISpaceTimeAbnormally>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
            catch (Exception ex)
            {
                return HandleException<ISpaceTimeAbnormally>(ex, "updating SpaceTimeAbnormally");
            }
        }

        /// <summary>
        /// Deletes a SpaceTimeAbnormally.
        /// </summary>
        /// <param name="abnormallyId">The SpaceTimeAbnormally ID.</param>
        /// <param name="softDelete">Whether to soft delete.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>True if deleted successfully.</returns>
        /// <response code="200">SpaceTimeAbnormally deleted successfully</response>
        /// <response code="400">Error deleting SpaceTimeAbnormally</response>
        [HttpDelete("spacetime-abnormally/{abnormallyId}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteSpaceTimeAbnormally(Guid abnormallyId, [FromQuery] bool softDelete = true, [FromQuery] ProviderType providerType = ProviderType.Default)
        { try { await EnsureOASISBootedAsync(); var result = await CosmicManager.DeleteSpaceTimeAbnormallyAsync(abnormallyId, softDelete, providerType);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "deleting SpaceTimeAbnormally");
            }
        }

        #endregion

        #region TemporalRift Methods

        /// <summary>
        /// Updates a TemporalRift.
        /// </summary>
        /// <param name="rift">The TemporalRift to update.</param>
        /// <param name="saveChildren">Whether to save children.</param>
        /// <param name="recursive">Whether to save recursively.</param>
        /// <param name="maxChildDepth">Maximum child depth.</param>
        /// <param name="continueOnError">Whether to continue on error.</param>
        /// <param name="saveChildrenOnProvider">Whether to save children on provider.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>The updated TemporalRift.</returns>
        /// <response code="200">TemporalRift updated successfully</response>
        /// <response code="400">Error updating TemporalRift</response>
        [HttpPut("temporal-rift")]
        [ProducesResponseType(typeof(OASISResult<ITemporalRift>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<ITemporalRift>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateTemporalRift([FromBody] ITemporalRift rift, [FromQuery] bool saveChildren = true, [FromQuery] bool recursive = true, [FromQuery] int maxChildDepth = 0, [FromQuery] bool continueOnError = true, [FromQuery] bool saveChildrenOnProvider = false, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            try
            {
                if (rift == null)
                {
                    return BadRequest(new OASISResult<ITemporalRift>
                    {
                        IsError = true,
                        Message = "TemporalRift cannot be null. Please provide a valid TemporalRift object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<ITemporalRift>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureOASISBootedAsync();
                var result = await CosmicManager.UpdateTemporalRiftAsync(rift, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
                return Ok(result);
            }
            catch (OASISException ex)
            {
                return BadRequest(new OASISResult<ITemporalRift>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
            catch (Exception ex)
            {
                return HandleException<ITemporalRift>(ex, "updating TemporalRift");
            }
        }

        /// <summary>
        /// Deletes a TemporalRift.
        /// </summary>
        /// <param name="riftId">The TemporalRift ID.</param>
        /// <param name="softDelete">Whether to soft delete.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>True if deleted successfully.</returns>
        /// <response code="200">TemporalRift deleted successfully</response>
        /// <response code="400">Error deleting TemporalRift</response>
        [HttpDelete("temporal-rift/{riftId}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteTemporalRift(Guid riftId, [FromQuery] bool softDelete = true, [FromQuery] ProviderType providerType = ProviderType.Default)
        { try { await EnsureOASISBootedAsync(); var result = await CosmicManager.DeleteTemporalRiftAsync(riftId, softDelete, providerType);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "deleting TemporalRift");
            }
        }

        #endregion

        #region StarDust Methods

        /// <summary>
        /// Updates a StarDust.
        /// </summary>
        /// <param name="starDust">The StarDust to update.</param>
        /// <param name="saveChildren">Whether to save children.</param>
        /// <param name="recursive">Whether to save recursively.</param>
        /// <param name="maxChildDepth">Maximum child depth.</param>
        /// <param name="continueOnError">Whether to continue on error.</param>
        /// <param name="saveChildrenOnProvider">Whether to save children on provider.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>The updated StarDust.</returns>
        /// <response code="200">StarDust updated successfully</response>
        /// <response code="400">Error updating StarDust</response>
        [HttpPut("stardust")]
        [ProducesResponseType(typeof(OASISResult<IStarDust>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IStarDust>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateStarDust([FromBody] IStarDust starDust, [FromQuery] bool saveChildren = true, [FromQuery] bool recursive = true, [FromQuery] int maxChildDepth = 0, [FromQuery] bool continueOnError = true, [FromQuery] bool saveChildrenOnProvider = false, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            try
            {
                if (starDust == null)
                {
                    return BadRequest(new OASISResult<IStarDust>
                    {
                        IsError = true,
                        Message = "StarDust cannot be null. Please provide a valid StarDust object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<IStarDust>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureOASISBootedAsync();
                var result = await CosmicManager.UpdateStarDustAsync(starDust, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
                return Ok(result);
            }
            catch (OASISException ex)
            {
                return BadRequest(new OASISResult<IStarDust>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
            catch (Exception ex)
            {
                return HandleException<IStarDust>(ex, "updating StarDust");
            }
        }

        /// <summary>
        /// Deletes a StarDust.
        /// </summary>
        /// <param name="starDustId">The StarDust ID.</param>
        /// <param name="softDelete">Whether to soft delete.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>True if deleted successfully.</returns>
        /// <response code="200">StarDust deleted successfully</response>
        /// <response code="400">Error deleting StarDust</response>
        [HttpDelete("stardust/{starDustId}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteStarDust(Guid starDustId, [FromQuery] bool softDelete = true, [FromQuery] ProviderType providerType = ProviderType.Default)
        { try { await EnsureOASISBootedAsync(); var result = await CosmicManager.DeleteStarDustAsync(starDustId, softDelete, providerType);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "deleting StarDust");
            }
        }

        #endregion

        #region CosmicWave Methods

        /// <summary>
        /// Updates a CosmicWave.
        /// </summary>
        /// <param name="wave">The CosmicWave to update.</param>
        /// <param name="saveChildren">Whether to save children.</param>
        /// <param name="recursive">Whether to save recursively.</param>
        /// <param name="maxChildDepth">Maximum child depth.</param>
        /// <param name="continueOnError">Whether to continue on error.</param>
        /// <param name="saveChildrenOnProvider">Whether to save children on provider.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>The updated CosmicWave.</returns>
        /// <response code="200">CosmicWave updated successfully</response>
        /// <response code="400">Error updating CosmicWave</response>
        [HttpPut("cosmic-wave")]
        [ProducesResponseType(typeof(OASISResult<ICosmicWave>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<ICosmicWave>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateCosmicWave([FromBody] ICosmicWave wave, [FromQuery] bool saveChildren = true, [FromQuery] bool recursive = true, [FromQuery] int maxChildDepth = 0, [FromQuery] bool continueOnError = true, [FromQuery] bool saveChildrenOnProvider = false, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            try
            {
                if (wave == null)
                {
                    return BadRequest(new OASISResult<ICosmicWave>
                    {
                        IsError = true,
                        Message = "CosmicWave cannot be null. Please provide a valid CosmicWave object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<ICosmicWave>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureOASISBootedAsync();
                var result = await CosmicManager.UpdateCosmicWaveAsync(wave, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
                return Ok(result);
            }
            catch (OASISException ex)
            {
                return BadRequest(new OASISResult<ICosmicWave>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
            catch (Exception ex)
            {
                return HandleException<ICosmicWave>(ex, "updating CosmicWave");
            }
        }

        /// <summary>
        /// Deletes a CosmicWave.
        /// </summary>
        /// <param name="waveId">The CosmicWave ID.</param>
        /// <param name="softDelete">Whether to soft delete.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>True if deleted successfully.</returns>
        /// <response code="200">CosmicWave deleted successfully</response>
        /// <response code="400">Error deleting CosmicWave</response>
        [HttpDelete("cosmic-wave/{waveId}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteCosmicWave(Guid waveId, [FromQuery] bool softDelete = true, [FromQuery] ProviderType providerType = ProviderType.Default)
        { try { await EnsureOASISBootedAsync(); var result = await CosmicManager.DeleteCosmicWaveAsync(waveId, softDelete, providerType);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "deleting CosmicWave");
            }
        }

        #endregion

        #region CosmicRay Methods

        /// <summary>
        /// Updates a CosmicRay.
        /// </summary>
        /// <param name="ray">The CosmicRay to update.</param>
        /// <param name="saveChildren">Whether to save children.</param>
        /// <param name="recursive">Whether to save recursively.</param>
        /// <param name="maxChildDepth">Maximum child depth.</param>
        /// <param name="continueOnError">Whether to continue on error.</param>
        /// <param name="saveChildrenOnProvider">Whether to save children on provider.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>The updated CosmicRay.</returns>
        /// <response code="200">CosmicRay updated successfully</response>
        /// <response code="400">Error updating CosmicRay</response>
        [HttpPut("cosmic-ray")]
        [ProducesResponseType(typeof(OASISResult<ICosmicRay>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<ICosmicRay>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateCosmicRay([FromBody] ICosmicRay ray, [FromQuery] bool saveChildren = true, [FromQuery] bool recursive = true, [FromQuery] int maxChildDepth = 0, [FromQuery] bool continueOnError = true, [FromQuery] bool saveChildrenOnProvider = false, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            try
            {
                if (ray == null)
                {
                    return BadRequest(new OASISResult<ICosmicRay>
                    {
                        IsError = true,
                        Message = "CosmicRay cannot be null. Please provide a valid CosmicRay object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<ICosmicRay>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureOASISBootedAsync();
                var result = await CosmicManager.UpdateCosmicRayAsync(ray, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
                return Ok(result);
            }
            catch (OASISException ex)
            {
                return BadRequest(new OASISResult<ICosmicRay>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
            catch (Exception ex)
            {
                return HandleException<ICosmicRay>(ex, "updating CosmicRay");
            }
        }

        /// <summary>
        /// Deletes a CosmicRay.
        /// </summary>
        /// <param name="rayId">The CosmicRay ID.</param>
        /// <param name="softDelete">Whether to soft delete.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>True if deleted successfully.</returns>
        /// <response code="200">CosmicRay deleted successfully</response>
        /// <response code="400">Error deleting CosmicRay</response>
        [HttpDelete("cosmic-ray/{rayId}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteCosmicRay(Guid rayId, [FromQuery] bool softDelete = true, [FromQuery] ProviderType providerType = ProviderType.Default)
        { try { await EnsureOASISBootedAsync(); var result = await CosmicManager.DeleteCosmicRayAsync(rayId, softDelete, providerType);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "deleting CosmicRay");
            }
        }

        #endregion

        #region GravitationalWave Methods

        /// <summary>
        /// Updates a GravitationalWave.
        /// </summary>
        /// <param name="wave">The GravitationalWave to update.</param>
        /// <param name="saveChildren">Whether to save children.</param>
        /// <param name="recursive">Whether to save recursively.</param>
        /// <param name="maxChildDepth">Maximum child depth.</param>
        /// <param name="continueOnError">Whether to continue on error.</param>
        /// <param name="saveChildrenOnProvider">Whether to save children on provider.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>The updated GravitationalWave.</returns>
        /// <response code="200">GravitationalWave updated successfully</response>
        /// <response code="400">Error updating GravitationalWave</response>
        [HttpPut("gravitational-wave")]
        [ProducesResponseType(typeof(OASISResult<IGravitationalWave>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<IGravitationalWave>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateGravitationalWave([FromBody] IGravitationalWave wave, [FromQuery] bool saveChildren = true, [FromQuery] bool recursive = true, [FromQuery] int maxChildDepth = 0, [FromQuery] bool continueOnError = true, [FromQuery] bool saveChildrenOnProvider = false, [FromQuery] ProviderType providerType = ProviderType.Default)
        {
            try
            {
                if (wave == null)
                {
                    return BadRequest(new OASISResult<IGravitationalWave>
                    {
                        IsError = true,
                        Message = "GravitationalWave cannot be null. Please provide a valid GravitationalWave object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<IGravitationalWave>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureOASISBootedAsync();
                var result = await CosmicManager.UpdateGravitationalWaveAsync(wave, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
                return Ok(result);
            }
            catch (OASISException ex)
            {
                return BadRequest(new OASISResult<IGravitationalWave>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
            catch (Exception ex)
            {
                return HandleException<IGravitationalWave>(ex, "updating GravitationalWave");
            }
        }

        /// <summary>
        /// Deletes a GravitationalWave.
        /// </summary>
        /// <param name="waveId">The GravitationalWave ID.</param>
        /// <param name="softDelete">Whether to soft delete.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>True if deleted successfully.</returns>
        /// <response code="200">GravitationalWave deleted successfully</response>
        /// <response code="400">Error deleting GravitationalWave</response>
        [HttpDelete("gravitational-wave/{waveId}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteGravitationalWave(Guid waveId, [FromQuery] bool softDelete = true, [FromQuery] ProviderType providerType = ProviderType.Default)
        { try { await EnsureOASISBootedAsync(); var result = await CosmicManager.DeleteGravitationalWaveAsync(waveId, softDelete, providerType);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "deleting GravitationalWave");
            }
        }

        #endregion
    }
}

