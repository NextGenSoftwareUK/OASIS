using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using NextGenSoftware.OASIS.Common;
using System.Collections.ObjectModel;
using NextGenSoftware.OASIS.STAR.Holons;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Events;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using static NextGenSoftware.OASIS.API.Core.Events.EventDelegates;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{    public abstract partial class CelestialSpace
    {
        public OASISResult<ICelestialBody> AddCelestialBody(ICelestialBody celestialBody, bool saveCelestialBody = true, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<ICelestialBody> result = new OASISResult<ICelestialBody>(celestialBody);
            string errorMessage = $"An error occured in CelestialSpace.AddCelestialBody adding the celestial body {LoggingHelper.GetHolonInfoForLogging(celestialBody, "CelestialBody")} to the celestial space {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason:";

            try
            {
                if (saveCelestialBody)
                    result = celestialBody.Save(saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);

                if ((saveCelestialBody && result != null && !result.IsError) || !saveCelestialBody)
                {
                    _celestialBodies.Add(celestialBody);
                    _allchildren.Add(celestialBody);
                    RegisterCelestialBody(celestialBody);

                    OnCelestialBodyAdded?.Invoke(this, new CelestialBodyAddedEventArgs() { Result = new OASISResult<ICelestialBody>(celestialBody) });
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error returned from celestialBody.Save method: {result.Message}");
                    OnCelestialBodyError?.Invoke(this, new CelestialBodyErrorEventArgs() { Result = result, Reason = result.Message, Exception = result.Exception });
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}", ex);
                OnCelestialBodyError?.Invoke(this, new CelestialBodyErrorEventArgs() { Result = result, Reason = result.Message, Exception = ex });
            }

            return result;
        }

        public async Task<OASISResult<ICelestialBody>> AddCelestialBodyAsync(ICelestialBody celestialBody, bool saveCelestialBody = true, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<ICelestialBody> result = new OASISResult<ICelestialBody>(celestialBody);
            string errorMessage = $"An error occured in CelestialSpace.AddCelestialBodyAsync adding the celestial body {LoggingHelper.GetHolonInfoForLogging(celestialBody, "CelestialBody")} to the celestial space {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason:";

            try
            {
                if (saveCelestialBody)
                    result = await celestialBody.SaveAsync(saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);

                if ((saveCelestialBody && result != null && !result.IsError) || !saveCelestialBody)
                {
                    _celestialBodies.Add(celestialBody);
                    _allchildren.Add(celestialBody);
                    RegisterCelestialBody(celestialBody);

                    OnCelestialBodyAdded?.Invoke(this, new CelestialBodyAddedEventArgs() { Result = new OASISResult<ICelestialBody>(celestialBody) });
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error returned from celestialBody.SaveAsync method: {result.Message}");
                    OnCelestialBodyError?.Invoke(this, new CelestialBodyErrorEventArgs() { Result = result, Reason = result.Message, Exception = result.Exception });
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}", ex);
                OnCelestialBodyError?.Invoke(this, new CelestialBodyErrorEventArgs() { Result = result, Reason = result.Message, Exception = ex });
            }

            return result;
        }

        public OASISResult<ICelestialBody> RemoveCelestialBody(ICelestialBody celestialBody, Guid avatarId, bool deleteCelestialBody = true, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<ICelestialBody> result = new OASISResult<ICelestialBody>(celestialBody);
            OASISResult<IHolon> holonResult = null;
            string errorMessage = $"An error occured in CelestialSpace.RemoveCelestialBody removing the celestial body {LoggingHelper.GetHolonInfoForLogging(celestialBody, "CelestialBody")} from the celestial space {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason:";

            try
            {
                if (deleteCelestialBody)
                    holonResult = celestialBody.Delete(avatarId, softDelete, providerType);

                if ((deleteCelestialBody && holonResult != null && !holonResult.IsError) || !deleteCelestialBody)
                {
                    _celestialBodies.Remove(celestialBody);
                    _allchildren.Remove(celestialBody);
                    UnregisterCelestialBody(celestialBody);

                    OnCelestialBodyRemoved?.Invoke(this, new CelestialBodyRemovedEventArgs() { Result = new OASISResult<ICelestialBody>(celestialBody) });
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error returned from celestialBody.Delete method: {holonResult.Message}");
                    OnCelestialBodyError?.Invoke(this, new CelestialBodyErrorEventArgs() { Result = result, Reason = result.Message, Exception = result.Exception });
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}", ex);
                OnCelestialBodyError?.Invoke(this, new CelestialBodyErrorEventArgs() { Result = result, Reason = result.Message, Exception = ex });
            }

            return result;
        }

        public async Task<OASISResult<ICelestialBody>> RemoveCelestialBodyAsync(ICelestialBody celestialBody, Guid avatarId, bool deleteCelestialBody = true, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<ICelestialBody> result = new OASISResult<ICelestialBody>(celestialBody);
            OASISResult<IHolon> holonResult = null;
            string errorMessage = $"An error occured in CelestialSpace.RemoveCelestialBodyAsync removing the celestial body {LoggingHelper.GetHolonInfoForLogging(celestialBody, "CelestialBody")} from the celestial space {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason:";

            try
            {
                if (deleteCelestialBody)
                    holonResult = await celestialBody.DeleteAsync(avatarId, softDelete, providerType);

                if ((deleteCelestialBody && holonResult != null && !holonResult.IsError) || !deleteCelestialBody)
                {
                    _celestialBodies.Remove(celestialBody);
                    _allchildren.Remove(celestialBody);
                    UnregisterCelestialBody(celestialBody);

                    OnCelestialBodyRemoved?.Invoke(this, new CelestialBodyRemovedEventArgs() { Result = new OASISResult<ICelestialBody>(celestialBody) });
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error returned from celestialBody.DeleteAsync method: {holonResult.Message}");
                    OnCelestialBodyError?.Invoke(this, new CelestialBodyErrorEventArgs() { Result = result, Reason = result.Message, Exception = result.Exception });
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}", ex);
                OnCelestialBodyError?.Invoke(this, new CelestialBodyErrorEventArgs() { Result = result, Reason = result.Message, Exception = ex });
            }

            return result;
        }

        public OASISResult<IEnumerable<ICelestialBody>> AddCelestialBodies(IEnumerable<ICelestialBody> celestialBodies, bool saveCelestialBodies = true, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<ICelestialBody>> result = new OASISResult<IEnumerable<ICelestialBody>>(celestialBodies);
            string errorMessage = $"An error occured in CelestialSpace.AddCelestialBodies adding {celestialBodies.Count()} celestial bodies to the celestial space {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason:";

            try
            {
                if (saveCelestialBodies)
                {
                    foreach (ICelestialBody celestialBody in celestialBodies)
                    {
                        OASISResult<ICelestialBody> celestialBodyResult = AddCelestialBody(celestialBody, saveCelestialBodies, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);

                        if ((celestialBodyResult != null && !celestialBodyResult.IsError && celestialBodyResult.Result != null) && !continueOnError)
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}", ex);
                OnCelestialBodiesError?.Invoke(this, new CelestialBodiesErrorEventArgs() { Result = result, Reason = result.Message, Exception = ex });
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<ICelestialBody>>> AddCelestialBodiesAsync(IEnumerable<ICelestialBody> celestialBodies, bool saveCelestialBodies = true, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<ICelestialBody>> result = new OASISResult<IEnumerable<ICelestialBody>>(celestialBodies);
            string errorMessage = $"An error occured in CelestialSpace.AddCelestialBodiesAsync adding {celestialBodies.Count()} celestial bodies to the celestial space {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason:";

            try
            {
                if (saveCelestialBodies)
                {
                    foreach (ICelestialBody celestialBody in celestialBodies)
                    {
                        OASISResult<ICelestialBody> celestialBodyResult = await AddCelestialBodyAsync(celestialBody, saveCelestialBodies, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);

                        if ((celestialBodyResult != null && !celestialBodyResult.IsError && celestialBodyResult.Result != null) && !continueOnError)
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}", ex);
                OnCelestialBodiesError?.Invoke(this, new CelestialBodiesErrorEventArgs() { Result = result, Reason = result.Message, Exception = ex });
            }

            return result;
        }

        public OASISResult<IEnumerable<ICelestialBody>> RemoveCelestialBodies(IEnumerable<ICelestialBody> celestialBodies, Guid avatarId, bool deleteCelestialBodies = true, bool softDelete = true, bool continueOnError = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<ICelestialBody>> result = new OASISResult<IEnumerable<ICelestialBody>>(celestialBodies);
            string errorMessage = $"An error occured in CelestialSpace.RemoveCelestialBodies removing {celestialBodies.Count()} celestial bodies from the celestial space {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason:";

            try
            {
                foreach (ICelestialBody celestialBody in celestialBodies)
                {
                    OASISResult<ICelestialBody> celestialBodyResult = RemoveCelestialBody(celestialBody, avatarId, deleteCelestialBodies, softDelete, providerType);

                    if ((celestialBodyResult != null && !celestialBodyResult.IsError && celestialBodyResult.Result != null) && !continueOnError)
                        break;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}", ex);
                OnCelestialBodiesError?.Invoke(this, new CelestialBodiesErrorEventArgs() { Result = result, Reason = result.Message, Exception = ex });
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<ICelestialBody>>> RemoveCelestialBodiesAsync(IEnumerable<ICelestialBody> celestialBodies, Guid avatarId, bool deleteCelestialBodies = true, bool softDelete = true, bool continueOnError = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<ICelestialBody>> result = new OASISResult<IEnumerable<ICelestialBody>>(celestialBodies);
            string errorMessage = $"An error occured in CelestialSpace.RemoveCelestialBodiesAsync removing {celestialBodies.Count()} celestial bodies from the celestial space {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason:";

            try
            {
                foreach (ICelestialBody celestialBody in celestialBodies)
                {
                    OASISResult<ICelestialBody> celestialBodyResult = await RemoveCelestialBodyAsync(celestialBody, avatarId, deleteCelestialBodies, softDelete, providerType);

                    if ((celestialBodyResult != null && !celestialBodyResult.IsError && celestialBodyResult.Result != null) && !continueOnError)
                        break;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}", ex);
                OnCelestialBodiesError?.Invoke(this, new CelestialBodiesErrorEventArgs() { Result = result, Reason = result.Message, Exception = ex });
            }

            return result;
        }

        public OASISResult<IEnumerable<ICelestialBody>> RemoveAllCelestialBodies(bool deleteCelestialBodies = true, bool softDelete = true, bool continueOnError = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<ICelestialBody>> result = new OASISResult<IEnumerable<ICelestialBody>>(_celestialBodies);
            string errorMessage = $"An error occured in CelestialSpace.RemoveAllCelestialBodies removing {_celestialBodies.Count()} celestial bodies from the celestial space {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason:";

            try
            {
                OASISResult<IEnumerable<ICelestialBody>> celestialBodiesResult = RemoveCelestialBodies(_celestialBodies, STAR.BeamedInAvatar.Id, deleteCelestialBodies, softDelete, continueOnError, providerType);

                if (celestialBodiesResult != null && !celestialBodiesResult.IsError && celestialBodiesResult.Result != null)
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured calling CelestialSpace.RemoveCelestialBodies. Reason: {celestialBodiesResult.Result}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}", ex);
                OnCelestialBodiesError?.Invoke(this, new CelestialBodiesErrorEventArgs() { Result = result, Reason = result.Message, Exception = ex });
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<ICelestialBody>>> RemoveAllCelestialBodiesAsync(Guid avatarId, bool deleteCelestialBodies = true, bool softDelete = true, bool continueOnError = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<ICelestialBody>> result = new OASISResult<IEnumerable<ICelestialBody>>(_celestialBodies);
            string errorMessage = $"An error occured in CelestialSpace.RemoveAllCelestialBodiesAsync removing {_celestialBodies.Count()} celestial bodies from the celestial space {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason:";

            try
            {
                OASISResult<IEnumerable<ICelestialBody>> celestialBodiesResult = await RemoveCelestialBodiesAsync(_celestialBodies, avatarId, deleteCelestialBodies, softDelete, continueOnError, providerType);

                if (celestialBodiesResult != null && !celestialBodiesResult.IsError && celestialBodiesResult.Result != null)
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured calling CelestialSpace.RemoveCelestialBodies. Reason: {celestialBodiesResult.Result}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}", ex);
                OnCelestialBodiesError?.Invoke(this, new CelestialBodiesErrorEventArgs() { Result = result, Reason = result.Message, Exception = ex });
            }

            return result;
        }

    }
}