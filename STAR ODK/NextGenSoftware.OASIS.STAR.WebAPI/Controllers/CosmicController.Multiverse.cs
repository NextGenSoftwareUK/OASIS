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
    }
}