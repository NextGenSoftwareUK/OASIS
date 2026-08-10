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
    public partial class CosmicController
    {
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
                var (parentTypeEnum, parentValidationError) = ValidateAndParseHolonType<IEnumerable<IHolon>>(parentHolonType, "parentHolonType");
                if (parentValidationError != null)
                    return parentValidationError;

                var (childTypeEnum, childValidationError) = ValidateAndParseHolonType<IEnumerable<IHolon>>(childHolonType, "childHolonType");
                if (childValidationError != null)
                    return childValidationError;
                var result = await CosmicManager.GetChildrenForParentAsync<IHolon>(parentId, parentTypeEnum, childTypeEnum);
                
                // Return test data if setting is enabled and result is null, has error, or result is null
                if (UseTestDataWhenLiveDataNotAvailable && (result == null || result.IsError || result.Result == null))
                {
                    var testHolons = new List<IHolon>();
                    return Ok(new OASISResult<IEnumerable<IHolon>>
                    {
                        Result = testHolons,
                        IsError = false,
                        Message = "Children retrieved successfully (using test data)"
                    });
                }
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    var testHolons = new List<IHolon>();
                    return Ok(new OASISResult<IEnumerable<IHolon>>
                    {
                        Result = testHolons,
                        IsError = false,
                        Message = "Children retrieved successfully (using test data)"
                    });
                }
                return HandleException<IEnumerable<IHolon>>(ex, "GetChildrenForParent");
            }
        }



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
                var (parentTypeEnum, parentValidationError) = ValidateAndParseHolonType<IEnumerable<IHolon>>(parentHolonType, "parentHolonType");
                if (parentValidationError != null)
                    return parentValidationError;

                var (childTypeEnum, childValidationError) = ValidateAndParseHolonType<IEnumerable<IHolon>>(childHolonType, "childHolonType");
                if (childValidationError != null)
                    return childValidationError;
                var result = await CosmicManager.SearchChildrenForParentAsync(searchTerm, parentId, parentTypeEnum, childTypeEnum);
                
                // Return test data if setting is enabled and result is null, has error, or result is null
                if (UseTestDataWhenLiveDataNotAvailable && (result == null || result.IsError || result.Result == null))
                {
                    var testHolons = new List<IHolon>();
                    return Ok(new OASISResult<IEnumerable<IHolon>>
                    {
                        Result = testHolons,
                        IsError = false,
                        Message = "Search completed successfully (using test data)"
                    });
                }
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    var testHolons = new List<IHolon>();
                    return Ok(new OASISResult<IEnumerable<IHolon>>
                    {
                        Result = testHolons,
                        IsError = false,
                        Message = "Search completed successfully (using test data)"
                    });
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
                var (parentTypeEnum, parentValidationError) = ValidateAndParseHolonType<IEnumerable<IHolon>>(parentHolonType, "parentHolonType");
                if (parentValidationError != null)
                    return parentValidationError;

                var (childTypeEnum, childValidationError) = ValidateAndParseHolonType<IEnumerable<IHolon>>(childHolonType, "childHolonType");
                if (childValidationError != null)
                    return childValidationError;
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
                var (holonTypeEnum, validationError) = ValidateAndParseHolonType<IEnumerable<IHolon>>(parentHolonType, "parentHolonType");
                if (validationError != null)
                    return validationError;
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
                EnsureLoggedInAvatar(); // Ensure AvatarManager.LoggedInAvatar is set before SaveAsync() calls
                var result = await CosmicManager.SaveOmniverseAsync(omniverse);
                
                // Return test data if setting is enabled and result is null, has error, or result is null
                if (UseTestDataWhenLiveDataNotAvailable && (result == null || result.IsError || result.Result == null))
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
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    return Ok(new OASISResult<IOmiverse>
                    {
                        Result = null,
                        IsError = false,
                        Message = "Omniverse saved successfully (using test mode - real data unavailable)"
                    });
                }
                // Use HandleException so validation/client errors → 400, real server bugs → 500 (not masked)
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
                    // Return test data if setting is enabled, otherwise return error
                    if (UseTestDataWhenLiveDataNotAvailable)
                    {
                        return Ok(new OASISResult<IOmiverse>
                        {
                            Result = null,
                            IsError = false,
                            Message = "Omniverse updated successfully (using test mode - real data unavailable)"
                        });
                    }
                    return BadRequest(new OASISResult<IOmiverse>
                    {
                        IsError = true,
                        Message = "Omniverse cannot be null. Please provide a valid Omniverse object in the request body."
                    });
                }

                if (AvatarId == Guid.Empty)
                {
                    // Return test data if setting is enabled, otherwise return error
                    if (UseTestDataWhenLiveDataNotAvailable)
                    {
                        return Ok(new OASISResult<IOmiverse>
                        {
                            Result = null,
                            IsError = false,
                            Message = "Omniverse updated successfully (using test mode - real data unavailable)"
                        });
                    }
                    return BadRequest(new OASISResult<IOmiverse>
                    {
                        IsError = true,
                        Message = "AvatarId is required but was not found. Please authenticate or provide X-Avatar-Id header."
                    });
                }

                await EnsureOASISBootedAsync();
                EnsureLoggedInAvatar(); // Ensure AvatarManager.LoggedInAvatar is set before SaveAsync() calls
                EnsureLoggedInAvatar(); // Ensure AvatarManager.LoggedInAvatar is set before SaveAsync() calls
                var result = await CosmicManager.UpdateOmniverseAsync(omniverse, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
                
                // Return test data if setting is enabled and result is null, has error, or result is null
                if (UseTestDataWhenLiveDataNotAvailable && (result == null || result.IsError || result.Result == null))
                {
                    return Ok(new OASISResult<IOmiverse>
                    {
                        Result = null,
                        IsError = false,
                        Message = "Omniverse updated successfully (using test mode - real data unavailable)"
                    });
                }
                
                if (result.IsError)
                    return BadRequest(result);
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    return Ok(new OASISResult<IOmiverse>
                    {
                        Result = null,
                        IsError = false,
                        Message = "Omniverse updated successfully (using test mode - real data unavailable)"
                    });
                }
                // Use HandleException so validation/client errors → 400, real server bugs → 500 (not masked)
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
                EnsureLoggedInAvatar(); // Ensure AvatarManager.LoggedInAvatar is set before SaveAsync() calls
                var result = await CosmicManager.DeleteOmniverseAsync(omniverseId, softDelete, providerType);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "deleting Omniverse");
            }
        }



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
                    // Return test data if setting is enabled, otherwise return error
                    if (UseTestDataWhenLiveDataNotAvailable)
                    {
                        return Ok(new OASISResult<IMultiverse>
                        {
                            Result = null,
                            IsError = false,
                            Message = "Multiverse added successfully (using test mode - real data unavailable)"
                        });
                    }
                    return BadRequest(new OASISResult<IMultiverse>
                    {
                        IsError = true,
                        Message = "Multiverse cannot be null. Please provide a valid Multiverse object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<IMultiverse>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureOASISBootedAsync();
                EnsureLoggedInAvatar(); // Ensure AvatarManager.LoggedInAvatar is set before SaveAsync() calls
                EnsureLoggedInAvatar(); // Ensure AvatarManager.LoggedInAvatar is set before SaveAsync() calls
                var result = await CosmicManager.AddMultiverseAsync(parentOmniverseId, multiverse);
                
                // Return test data if setting is enabled and result is null, has error, or result is null
                if (UseTestDataWhenLiveDataNotAvailable && (result == null || result.IsError || result.Result == null))
                {
                    return Ok(new OASISResult<IMultiverse>
                    {
                        Result = null,
                        IsError = false,
                        Message = "Multiverse added successfully (using test mode - real data unavailable)"
                    });
                }
                
                if (result.IsError)
                    return BadRequest(result);
                
                return Ok(result);
            }
            catch (OASISException ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    return Ok(new OASISResult<IMultiverse>
                    {
                        Result = null,
                        IsError = false,
                        Message = "Multiverse added successfully (using test mode - real data unavailable)"
                    });
                }
                return BadRequest(new OASISResult<IMultiverse>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
            catch (Exception ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    return Ok(new OASISResult<IMultiverse>
                    {
                        Result = null,
                        IsError = false,
                        Message = "Multiverse added successfully (using test mode - real data unavailable)"
                    });
                }
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
                    // Return test data if setting is enabled, otherwise return error
                    if (UseTestDataWhenLiveDataNotAvailable)
                    {
                        return Ok(new OASISResult<IMultiverse>
                        {
                            Result = null,
                            IsError = false,
                            Message = "Multiverse updated successfully (using test mode - real data unavailable)"
                        });
                    }
                    return BadRequest(new OASISResult<IMultiverse>
                    {
                        IsError = true,
                        Message = "Multiverse cannot be null. Please provide a valid Multiverse object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<IMultiverse>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureOASISBootedAsync();
                EnsureLoggedInAvatar(); // Ensure AvatarManager.LoggedInAvatar is set before SaveAsync() calls
                EnsureLoggedInAvatar(); // Ensure AvatarManager.LoggedInAvatar is set before SaveAsync() calls
                var result = await CosmicManager.UpdateMultiverseAsync(multiverse, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
                
                // Return test data if setting is enabled and result is null, has error, or result is null
                if (UseTestDataWhenLiveDataNotAvailable && (result == null || result.IsError || result.Result == null))
                {
                    return Ok(new OASISResult<IMultiverse>
                    {
                        Result = null,
                        IsError = false,
                        Message = "Multiverse updated successfully (using test mode - real data unavailable)"
                    });
                }
                
                if (result.IsError)
                    return BadRequest(result);
                
                return Ok(result);
            }
            catch (OASISException ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    return Ok(new OASISResult<IMultiverse>
                    {
                        Result = null,
                        IsError = false,
                        Message = "Multiverse updated successfully (using test mode - real data unavailable)"
                    });
                }
                return BadRequest(new OASISResult<IMultiverse>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
            catch (Exception ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    return Ok(new OASISResult<IMultiverse>
                    {
                        Result = null,
                        IsError = false,
                        Message = "Multiverse updated successfully (using test mode - real data unavailable)"
                    });
                }
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
                    // Return test data if setting is enabled, otherwise return error
                    if (UseTestDataWhenLiveDataNotAvailable)
                    {
                        return Ok(new OASISResult<IUniverse>
                        {
                            Result = null,
                            IsError = false,
                            Message = "Universe added successfully (using test mode - real data unavailable)"
                        });
                    }
                    return BadRequest(new OASISResult<IUniverse>
                    {
                        IsError = true,
                        Message = "Universe cannot be null. Please provide a valid Universe object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<IUniverse>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureOASISBootedAsync();
                EnsureLoggedInAvatar(); // Ensure AvatarManager.LoggedInAvatar is set before SaveAsync() calls
                EnsureLoggedInAvatar(); // Ensure AvatarManager.LoggedInAvatar is set before SaveAsync() calls
                var result = await CosmicManager.AddUniverseAsync(parentMultiverseId, universe);
                
                // Return test data if setting is enabled and result is null, has error, or result is null
                if (UseTestDataWhenLiveDataNotAvailable && (result == null || result.IsError || result.Result == null))
                {
                    return Ok(new OASISResult<IUniverse>
                    {
                        Result = null,
                        IsError = false,
                        Message = "Universe added successfully (using test mode - real data unavailable)"
                    });
                }
                
                if (result.IsError)
                    return BadRequest(result);
                return Ok(result);
            }
            catch (OASISException ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    return Ok(new OASISResult<IUniverse>
                    {
                        Result = null,
                        IsError = false,
                        Message = "Universe added successfully (using test mode - real data unavailable)"
                    });
                }
                return BadRequest(new OASISResult<IUniverse>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
            catch (Exception ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    return Ok(new OASISResult<IUniverse>
                    {
                        Result = null,
                        IsError = false,
                        Message = "Universe added successfully (using test mode - real data unavailable)"
                    });
                }
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
                    // Return test data if setting is enabled, otherwise return error
                    if (UseTestDataWhenLiveDataNotAvailable)
                    {
                        return Ok(new OASISResult<IUniverse>
                        {
                            Result = null,
                            IsError = false,
                            Message = "Universe updated successfully (using test mode - real data unavailable)"
                        });
                    }
                    return BadRequest(new OASISResult<IUniverse>
                    {
                        IsError = true,
                        Message = "Universe cannot be null. Please provide a valid Universe object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<IUniverse>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureOASISBootedAsync();
                EnsureLoggedInAvatar(); // Ensure AvatarManager.LoggedInAvatar is set before SaveAsync() calls
                EnsureLoggedInAvatar(); // Ensure AvatarManager.LoggedInAvatar is set before SaveAsync() calls
                var result = await CosmicManager.UpdateUniverseAsync(universe, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
                
                // Return test data if setting is enabled and result is null, has error, or result is null
                if (UseTestDataWhenLiveDataNotAvailable && (result == null || result.IsError || result.Result == null))
                {
                    return Ok(new OASISResult<IUniverse>
                    {
                        Result = null,
                        IsError = false,
                        Message = "Universe updated successfully (using test mode - real data unavailable)"
                    });
                }
                
                if (result.IsError)
                    return BadRequest(result);
                
                return Ok(result);
            }
            catch (OASISException ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    return Ok(new OASISResult<IUniverse>
                    {
                        Result = null,
                        IsError = false,
                        Message = "Universe updated successfully (using test mode - real data unavailable)"
                    });
                }
                return BadRequest(new OASISResult<IUniverse>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
            catch (Exception ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    return Ok(new OASISResult<IUniverse>
                    {
                        Result = null,
                        IsError = false,
                        Message = "Universe updated successfully (using test mode - real data unavailable)"
                    });
                }
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
                    // Return test data if setting is enabled, otherwise return error
                    if (UseTestDataWhenLiveDataNotAvailable)
                    {
                        return Ok(new OASISResult<IGalaxyCluster>
                        {
                            Result = null,
                            IsError = false,
                            Message = "GalaxyCluster added successfully (using test mode - real data unavailable)"
                        });
                    }
                    return BadRequest(new OASISResult<IGalaxyCluster>
                    {
                        IsError = true,
                        Message = "GalaxyCluster cannot be null. Please provide a valid GalaxyCluster object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<IGalaxyCluster>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureOASISBootedAsync();
                EnsureLoggedInAvatar(); // Ensure AvatarManager.LoggedInAvatar is set before SaveAsync() calls
                var result = await CosmicManager.AddGalaxyClusterAsync(parentUniverseId, galaxyCluster);
                
                // Return test data if setting is enabled and result is null, has error, or result is null
                if (UseTestDataWhenLiveDataNotAvailable && (result == null || result.IsError || result.Result == null))
                {
                    return Ok(new OASISResult<IGalaxyCluster>
                    {
                        Result = null,
                        IsError = false,
                        Message = "GalaxyCluster added successfully (using test mode - real data unavailable)"
                    });
                }
                
                if (result.IsError)
                    return BadRequest(result);
                return Ok(result);
            }
            catch (OASISException ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    return Ok(new OASISResult<IGalaxyCluster>
                    {
                        Result = null,
                        IsError = false,
                        Message = "GalaxyCluster added successfully (using test mode - real data unavailable)"
                    });
                }
                return BadRequest(new OASISResult<IGalaxyCluster>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
            catch (Exception ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    return Ok(new OASISResult<IGalaxyCluster>
                    {
                        Result = null,
                        IsError = false,
                        Message = "GalaxyCluster added successfully (using test mode - real data unavailable)"
                    });
                }
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
                    // Return test data if setting is enabled, otherwise return error
                    if (UseTestDataWhenLiveDataNotAvailable)
                    {
                        return Ok(new OASISResult<IGalaxyCluster>
                        {
                            Result = null,
                            IsError = false,
                            Message = "GalaxyCluster updated successfully (using test mode - real data unavailable)"
                        });
                    }
                    return BadRequest(new OASISResult<IGalaxyCluster>
                    {
                        IsError = true,
                        Message = "GalaxyCluster cannot be null. Please provide a valid GalaxyCluster object in the request body."
                    });
                }

                var avatarCheck = ValidateAvatarId<IGalaxyCluster>();
                if (avatarCheck != null) return avatarCheck;

                await EnsureOASISBootedAsync();
                EnsureLoggedInAvatar(); // Ensure AvatarManager.LoggedInAvatar is set before SaveAsync() calls
                var result = await CosmicManager.UpdateGalaxyClusterAsync(galaxyCluster, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
                
                // Return test data if setting is enabled and result is null, has error, or result is null
                if (UseTestDataWhenLiveDataNotAvailable && (result == null || result.IsError || result.Result == null))
                {
                    return Ok(new OASISResult<IGalaxyCluster>
                    {
                        Result = null,
                        IsError = false,
                        Message = "GalaxyCluster updated successfully (using test mode - real data unavailable)"
                    });
                }
                
                if (result.IsError)
                    return BadRequest(result);
                
                return Ok(result);
            }
            catch (OASISException ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    return Ok(new OASISResult<IGalaxyCluster>
                    {
                        Result = null,
                        IsError = false,
                        Message = "GalaxyCluster updated successfully (using test mode - real data unavailable)"
                    });
                }
                return BadRequest(new OASISResult<IGalaxyCluster>
                {
                    IsError = true,
                    Message = ex.Message,
                    Exception = ex
                });
            }
            catch (Exception ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    return Ok(new OASISResult<IGalaxyCluster>
                    {
                        Result = null,
                        IsError = false,
                        Message = "GalaxyCluster updated successfully (using test mode - real data unavailable)"
                    });
                }
                return HandleException<IGalaxyCluster>(ex, "updating GalaxyCluster");
            }
        }

    }
}
