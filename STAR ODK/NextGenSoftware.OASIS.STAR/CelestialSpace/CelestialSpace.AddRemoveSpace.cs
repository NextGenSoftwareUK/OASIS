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
        public OASISResult<ICelestialSpace> AddCelestialSpace(ICelestialSpace celestialSpace, bool saveCelestialSpace = true, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<ICelestialSpace> result = new OASISResult<ICelestialSpace>(celestialSpace);
            string errorMessage = $"An error occured in CelestialSpace.AddCelestialSpace adding the celestial space {LoggingHelper.GetHolonInfoForLogging(celestialSpace, "CelestialSpace")} to the celestial space {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason:";

            try
            {
                if (saveCelestialSpace)
                    result = celestialSpace.Save(saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);

                if ((saveCelestialSpace && result != null && !result.IsError) || !saveCelestialSpace)
                {
                    _celestialSpaces.Add(celestialSpace);
                    _allchildren.Add(celestialSpace);
                    RegisterCelestialSpace(celestialSpace);

                    OnCelestialSpaceAdded?.Invoke(this, new CelestialSpaceAddedEventArgs() { Result = new OASISResult<ICelestialSpace>(celestialSpace) });
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error returned from celestialSpace.Save method: {result.Message}");
                    OnCelestialSpaceError?.Invoke(this, new CelestialSpaceErrorEventArgs() { Result = result, Reason = result.Message, Exception = result.Exception });
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}", ex);
                OnCelestialSpaceError?.Invoke(this, new CelestialSpaceErrorEventArgs() { Result = result, Reason = result.Message, Exception = ex });
            }

            return result;
        }

        public async Task<OASISResult<ICelestialSpace>> AddCelestialSpaceAsync(ICelestialSpace celestialSpace, bool saveCelestialSpace = true, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<ICelestialSpace> result = new OASISResult<ICelestialSpace>(celestialSpace);
            string errorMessage = $"An error occured in CelestialSpace.AddCelestialSpaceAsync adding the celestial space {LoggingHelper.GetHolonInfoForLogging(celestialSpace, "CelestialSpace")} to the celestial space {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason:";

            try
            {
                if (saveCelestialSpace)
                    result = await celestialSpace.SaveAsync(saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);

                if ((saveCelestialSpace && result != null && !result.IsError) || !saveCelestialSpace)
                {
                    _celestialSpaces.Add(celestialSpace);
                    _allchildren.Add(celestialSpace);
                    RegisterCelestialSpace(celestialSpace);

                    OnCelestialSpaceAdded?.Invoke(this, new CelestialSpaceAddedEventArgs() { Result = new OASISResult<ICelestialSpace>(celestialSpace) });
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error returned from celestialSpace.SaveAsync method: {result.Message}");
                    OnCelestialSpaceError?.Invoke(this, new CelestialSpaceErrorEventArgs() { Result = result, Reason = result.Message, Exception = result.Exception });
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}", ex);
                OnCelestialSpaceError?.Invoke(this, new CelestialSpaceErrorEventArgs() { Result = result, Reason = result.Message, Exception = ex });
            }

            return result;
        }

        public OASISResult<ICelestialSpace> RemoveCelestialSpace(ICelestialSpace celestialSpace, Guid avatarId, bool deleteCelestialSpace = true, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<ICelestialSpace> result = new OASISResult<ICelestialSpace>(celestialSpace);
            OASISResult<IHolon> holonResult = null;
            string errorMessage = $"An error occured in CelestialSpace.RemoveCelestialSpace removing the celestial space {LoggingHelper.GetHolonInfoForLogging(celestialSpace, "CelestialSpace")} from the celestial space {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason:";

            try
            {
                if (deleteCelestialSpace)
                    holonResult = celestialSpace.Delete(avatarId, softDelete, providerType);

                if ((deleteCelestialSpace && holonResult != null && !holonResult.IsError) || !deleteCelestialSpace)
                {
                    _celestialSpaces.Remove(celestialSpace);
                    _allchildren.Remove(celestialSpace);
                    UnregisterCelestialSpace(celestialSpace);

                    OnCelestialSpaceRemoved?.Invoke(this, new CelestialSpaceRemovedEventArgs() { Result = new OASISResult<ICelestialSpace>(celestialSpace) });
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error returned from celestialSpace.Delete method: {holonResult.Message}");
                    OnCelestialSpaceError?.Invoke(this, new CelestialSpaceErrorEventArgs() { Result = result, Reason = result.Message, Exception = result.Exception });
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}", ex);
                OnCelestialSpaceError?.Invoke(this, new CelestialSpaceErrorEventArgs() { Result = result, Reason = result.Message, Exception = ex });
            }

            return result;
        }

        public async Task<OASISResult<ICelestialSpace>> RemoveCelestialSpaceAsync(ICelestialSpace celestialSpace, Guid avatarId, bool deleteCelestialSpace = true, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<ICelestialSpace> result = new OASISResult<ICelestialSpace>(celestialSpace);
            OASISResult<IHolon> holonResult = null;
            string errorMessage = $"An error occured in CelestialSpace.RemoveCelestialSpaceAsync removing the celestial space {LoggingHelper.GetHolonInfoForLogging(celestialSpace, "CelestialSpace")} from the celestial space {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason:";

            try
            {
                if (deleteCelestialSpace)
                    holonResult = await celestialSpace.DeleteAsync(avatarId, softDelete, providerType);

                if ((deleteCelestialSpace && holonResult != null && !holonResult.IsError) || !deleteCelestialSpace)
                {
                    _celestialSpaces.Remove(celestialSpace);
                    _allchildren.Remove(celestialSpace);
                    UnregisterCelestialSpace(celestialSpace);

                    OnCelestialSpaceRemoved?.Invoke(this, new CelestialSpaceRemovedEventArgs() { Result = new OASISResult<ICelestialSpace>(celestialSpace) });
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error returned from celestialSpace.DeleteAsync method: {holonResult.Message}");
                    OnCelestialSpaceError?.Invoke(this, new CelestialSpaceErrorEventArgs() { Result = result, Reason = result.Message, Exception = result.Exception });
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}", ex);
                OnCelestialSpaceError?.Invoke(this, new CelestialSpaceErrorEventArgs() { Result = result, Reason = result.Message, Exception = ex });
            }

            return result;
        }

        public OASISResult<IEnumerable<ICelestialSpace>> AddCelestialSpaces(IEnumerable<ICelestialSpace> celestialSpaces, bool saveCelestialSpaces = true, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<ICelestialSpace>> result = new OASISResult<IEnumerable<ICelestialSpace>>(celestialSpaces);
            string errorMessage = $"An error occured in CelestialSpace.AddCelestialSpaces adding {celestialSpaces.Count()} celestial spaces to the celestial space {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason:";

            try
            {
                foreach (ICelestialSpace celestialSpace in celestialSpaces)
                {
                    OASISResult<ICelestialSpace> celestialSpaceResult = AddCelestialSpace(celestialSpace, saveCelestialSpaces, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);

                    if ((celestialSpaceResult != null && !celestialSpaceResult.IsError && celestialSpaceResult.Result != null) && !continueOnError)
                        break;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}", ex);
                OnCelestialSpacesError?.Invoke(this, new CelestialSpacesErrorEventArgs() { Result = result, Reason = result.Message, Exception = ex });
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<ICelestialSpace>>> AddCelestialSpacesAsync(IEnumerable<ICelestialSpace> celestialSpaces, bool saveCelestialSpaces = true, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<ICelestialSpace>> result = new OASISResult<IEnumerable<ICelestialSpace>>(celestialSpaces);
            string errorMessage = $"An error occured in CelestialSpace.AddCelestialSpacesAsync adding {celestialSpaces.Count()} celestial spaces to the celestial space {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason:";

            try
            {
                foreach (ICelestialSpace celestialSpace in celestialSpaces)
                {
                    OASISResult<ICelestialSpace> celestialSpaceResult = await AddCelestialSpaceAsync(celestialSpace, saveCelestialSpaces, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);

                    if ((celestialSpaceResult != null && !celestialSpaceResult.IsError && celestialSpaceResult.Result != null) && !continueOnError)
                        break;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}", ex);
                OnCelestialSpacesError?.Invoke(this, new CelestialSpacesErrorEventArgs() { Result = result, Reason = result.Message, Exception = ex });
            }

            return result;
        }

        public OASISResult<IEnumerable<ICelestialSpace>> RemoveCelestialSpaces(IEnumerable<ICelestialSpace> celestialSpaces, Guid avatarId, bool deleteCelestialSpaces = true, bool softDelete = true, bool continueOnError = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<ICelestialSpace>> result = new OASISResult<IEnumerable<ICelestialSpace>>(celestialSpaces);
            string errorMessage = $"An error occured in CelestialSpace.RemoveCelestialSpaces removing {celestialSpaces.Count()} celestial spaces from the celestial space {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason:";

            try
            {
                foreach (ICelestialSpace celestialSpace in celestialSpaces)
                {
                    OASISResult<ICelestialSpace> celestialSpaceResult = RemoveCelestialSpace(celestialSpace, avatarId, deleteCelestialSpaces, softDelete, providerType);

                    if ((celestialSpaceResult != null && !celestialSpaceResult.IsError && celestialSpaceResult.Result != null) && !continueOnError)
                        break;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}", ex);
                OnCelestialSpacesError?.Invoke(this, new CelestialSpacesErrorEventArgs() { Result = result, Reason = result.Message, Exception = ex });
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<ICelestialSpace>>> RemoveCelestialSpacesAsync(IEnumerable<ICelestialSpace> celestialSpaces, Guid avatarId, bool deleteCelestialSpaces = true, bool softDelete = true, bool continueOnError = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<ICelestialSpace>> result = new OASISResult<IEnumerable<ICelestialSpace>>(celestialSpaces);
            string errorMessage = $"An error occured in CelestialSpace.RemoveCelestialSpacesAsync removing {celestialSpaces.Count()} celestial spaces from the celestial space {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason:";

            try
            {
                foreach (ICelestialSpace celestialSpace in celestialSpaces)
                {
                    OASISResult<ICelestialSpace> celestialSpaceResult = await RemoveCelestialSpaceAsync(celestialSpace, avatarId, deleteCelestialSpaces, softDelete, providerType);

                    if ((celestialSpaceResult != null && !celestialSpaceResult.IsError && celestialSpaceResult.Result != null) && !continueOnError)
                        break;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}", ex);
                OnCelestialSpacesError?.Invoke(this, new CelestialSpacesErrorEventArgs() { Result = result, Reason = result.Message, Exception = ex });
            }

            return result;
        }

        public OASISResult<IEnumerable<ICelestialSpace>> RemoveAllCelestialSpaces(bool deleteCelestialSpaces = true, bool softDelete = true, bool continueOnError = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<ICelestialSpace>> result = new OASISResult<IEnumerable<ICelestialSpace>>();
            string errorMessage = $"An error occured in CelestialSpace.RemoveAllCelestialSpaces removing {_celestialSpaces.Count()} celestial spaces from the celestial space {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason:";

            try
            {
                OASISResult<IEnumerable<ICelestialSpace>> celestialSpacesResult = RemoveCelestialSpaces(_celestialSpaces, STAR.BeamedInAvatar.Id, deleteCelestialSpaces, softDelete, continueOnError, providerType);

                if (!(celestialSpacesResult != null && !celestialSpacesResult.IsError && celestialSpacesResult.Result != null))
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured calling CelestialSpace.RemoveCelestialSpaces. Reason: {celestialSpacesResult.Result}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}", ex);
                OnCelestialSpacesError?.Invoke(this, new CelestialSpacesErrorEventArgs() { Result = result, Reason = result.Message, Exception = ex });
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<ICelestialSpace>>> RemoveAllCelestialSpacesAsync(Guid avatarId, bool deleteCelestialSpaces = true, bool softDelete = true, bool continueOnError = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<ICelestialSpace>> result = new OASISResult<IEnumerable<ICelestialSpace>>();
            string errorMessage = $"An error occured in CelestialSpace.RemoveAllCelestialSpacesAsync removing {_celestialSpaces.Count()} celestial spaces from the celestial space {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason:";

            try
            {
                OASISResult<IEnumerable<ICelestialSpace>> celestialSpacesResult = await RemoveCelestialSpacesAsync(_celestialSpaces, avatarId, deleteCelestialSpaces, softDelete, continueOnError, providerType);

                if (!(celestialSpacesResult != null && !celestialSpacesResult.IsError && celestialSpacesResult.Result != null))
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured calling CelestialSpace.RemoveCelestialSpacesAsync. Reason: {celestialSpacesResult.Result}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}", ex);
                OnCelestialSpacesError?.Invoke(this, new CelestialSpacesErrorEventArgs() { Result = result, Reason = result.Message, Exception = ex });
            }

            return result;
        }

    }
}