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
{
    public abstract partial class CelestialSpace
    {

        public async Task<OASISResult<ICelestialBodiesAndSpaces>> SaveCelestialBodiesAndSpacesAsync(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<ICelestialBodiesAndSpaces> result = new OASISResult<ICelestialBodiesAndSpaces>();
            string errorMessage = $"Error occured in CelestialSpace.SaveCelestialBodiesAndSpacesAsync saving the celestial bodies and spaces for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason:";

            try
            {
                IsSaving = true;
                IStar star = GetCelestialSpaceNearestStar(result, errorMessage);
                result = HandleSaveCelestialBodiesAndSpaces(result, await star.CelestialBodyCore.GlobalHolonData.SaveHolonsAsync(_allchildren, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType), "SaveCelestialBodiesAndSpacesAsync");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured calling star.CelestialBodyCore.GlobalHolonData.SaveHolonsAsync: {ex}", ex);
                OnCelestialSpaceError?.Invoke(this, new CelestialSpaceErrorEventArgs() { Reason = $"{result.Message}", Result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<ICelestialBodiesAndSpaces, ICelestialSpace>(result), Exception = result.Exception });
            }

            IsSaving = false;
            return result;
        }

        public OASISResult<ICelestialBodiesAndSpaces> SaveCelestialBodiesAndSpaces(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<ICelestialBodiesAndSpaces> result = new OASISResult<ICelestialBodiesAndSpaces>();
            string errorMessage = $"Error occured in CelestialSpace.SaveCelestialBodiesAndSpacesAsync saving the celestial bodies and spaces for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason:";

            try
            {
                IsSaving = true;
                IStar star = GetCelestialSpaceNearestStar(result, errorMessage);
                result = HandleSaveCelestialBodiesAndSpaces(result, star.CelestialBodyCore.GlobalHolonData.SaveHolons(_allchildren, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType), "SaveCelestialBodiesAndSpaces");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured calling star.CelestialBodyCore.GlobalHolonData.SaveHolonsAsync: {ex}", ex);
                OnCelestialSpaceError?.Invoke(this, new CelestialSpaceErrorEventArgs() { Reason = $"{result.Message}", Result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<ICelestialBodiesAndSpaces, ICelestialSpace>(result), Exception = result.Exception });
            }

            IsSaving = false;
            return result;
        }

        public async Task<OASISResult<ICelestialBodiesAndSpaces<T1, T2>>> SaveCelestialBodiesAndSpacesAsync<T1, T2>(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T1 : ICelestialBody, new() where T2 : ICelestialSpace, new()
        {
            OASISResult<ICelestialBodiesAndSpaces<T1, T2>> result = new OASISResult<ICelestialBodiesAndSpaces<T1, T2>>();
            string errorMessage = $"Error occured in CelestialSpace.SaveCelestialBodiesAndSpacesAsync<T1, T2> saving the celestial bodies and spaces for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason:";

            try
            {
                IsSaving = true;
                IStar star = GetCelestialSpaceNearestStar(result, errorMessage);
                result = HandleSaveCelestialBodiesAndSpaces(result, await star.CelestialBodyCore.GlobalHolonData.SaveHolonsAsync(_allchildren, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType), "SaveCelestialBodiesAndSpacesAsync<T1, T2>");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured calling star.CelestialBodyCore.GlobalHolonData.SaveHolonsAsync: {ex}", ex);
                OnCelestialSpaceError?.Invoke(this, new CelestialSpaceErrorEventArgs() { Reason = $"{result.Message}", Result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<ICelestialBodiesAndSpaces<T1, T2>, ICelestialSpace>(result), Exception = result.Exception });
            }

            IsSaving = false;
            return result;
        }

        public OASISResult<ICelestialBodiesAndSpaces<T1, T2>> SaveCelestialBodiesAndSpaces<T1, T2>(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T1 : ICelestialBody, new() where T2 : ICelestialSpace, new()
        {
            OASISResult<ICelestialBodiesAndSpaces<T1, T2>> result = new OASISResult<ICelestialBodiesAndSpaces<T1, T2>>();
            string errorMessage = $"Error occured in CelestialSpace.SaveCelestialBodiesAndSpaces<T1, T2> saving the celestial bodies and spaces for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason:";

            try
            {
                IsSaving = true;
                IStar star = GetCelestialSpaceNearestStar(result, errorMessage);
                result = HandleSaveCelestialBodiesAndSpaces(result, star.CelestialBodyCore.GlobalHolonData.SaveHolons(_allchildren, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType), "SaveCelestialBodiesAndSpaces<T1, T2>");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured calling star.CelestialBodyCore.GlobalHolonData.SaveHolonsAsync: {ex}", ex);
                OnCelestialSpaceError?.Invoke(this, new CelestialSpaceErrorEventArgs() { Reason = $"{result.Message}", Result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<ICelestialBodiesAndSpaces<T1, T2>, ICelestialSpace>(result), Exception = result.Exception });
            }

            IsSaving = false;
            return result;
        }

        public async Task<OASISResult<IEnumerable<ICelestialBody>>> SaveCelestialBodiesAsync(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<ICelestialBody>> result = new OASISResult<IEnumerable<ICelestialBody>>();
            string errorMessage = $"Error occured in CelestialSpace.SaveCelestialBodiesAsync saving the celestial bodies for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason:";

            try
            {
                IsSaving = true;
                IStar star = GetCelestialSpaceNearestStar(result, errorMessage);
                result = HandleSaveCelestialBodies(result, await star.CelestialBodyCore.GlobalHolonData.SaveHolonsAsync(_celestialBodies, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType), "SaveCelestialBodiesAsync");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured calling star.CelestialBodyCore.GlobalHolonData.SaveHolonsAsync: {ex}", ex);
                OnCelestialBodiesError?.Invoke(this, new CelestialBodiesErrorEventArgs() { Reason = $"{result.Message}", Result = result, Exception = result.Exception });
            }

            IsSaving = false;
            return result;
        }

        public OASISResult<IEnumerable<ICelestialBody>> SaveCelestialBodies(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<ICelestialBody>> result = new OASISResult<IEnumerable<ICelestialBody>>();
            string errorMessage = $"Error occured in CelestialSpace.SaveCelestialBodies saving the celestial bodies for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason:";

            try
            {
                IsSaving = true;
                IStar star = GetCelestialSpaceNearestStar(result, errorMessage);
                result = HandleSaveCelestialBodies(result, star.CelestialBodyCore.GlobalHolonData.SaveHolons(_celestialBodies, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType), "SaveCelestialBodies");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured calling star.CelestialBodyCore.GlobalHolonData.SaveHolonsAsync: {ex}", ex);
                OnCelestialBodiesError?.Invoke(this, new CelestialBodiesErrorEventArgs() { Reason = $"{result.Message}", Result = result, Exception = result.Exception });
            }

            IsSaving = false;
            return result;
        }

        public async Task<OASISResult<IEnumerable<T>>> SaveCelestialBodiesAsync<T>(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : ICelestialBody, new()
        {
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();
            string errorMessage = $"Error occured in CelestialSpace.SaveCelestialBodiesAsync<T> saving the celestial bodies for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason:";

            try
            {
                IsSaving = true;
                IStar star = GetCelestialSpaceNearestStar(result, errorMessage);
                result = HandleSaveCelestialBodies(result, await star.CelestialBodyCore.GlobalHolonData.SaveHolonsAsync<T>(Mapper.Convert<ICelestialBody, T>(_celestialBodies), saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType), "SaveCelestialBodiesAsync<T>");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured calling star.CelestialBodyCore.GlobalHolonData.SaveHolonsAsync: {ex}", ex);
                OnCelestialBodiesError?.Invoke(this, new CelestialBodiesErrorEventArgs() { Reason = $"{result.Message}", Result = OASISResultHelper.CopyResultToICelestialBody(result), Exception = result.Exception });
            }

            IsSaving = false;
            return result;
        }

        public async Task<OASISResult<IEnumerable<T>>> SaveCelestialBodies<T>(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : ICelestialBody, new()
        {
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();
            string errorMessage = $"Error occured in CelestialSpace.SaveCelestialBodies<T> saving the celestial bodies for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason:";

            try
            {
                IsSaving = true;
                IStar star = GetCelestialSpaceNearestStar(result, errorMessage);
                result = HandleSaveCelestialBodies(result, await star.CelestialBodyCore.GlobalHolonData.SaveHolonsAsync<T>(Mapper.Convert<ICelestialBody, T>(_celestialBodies), saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType), "SaveCelestialBodiesAsync<T>");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured calling star.CelestialBodyCore.GlobalHolonData.SaveHolonsAsync: {ex}", ex);
                OnCelestialBodiesError?.Invoke(this, new CelestialBodiesErrorEventArgs() { Reason = $"{result.Message}", Result = OASISResultHelper.CopyResultToICelestialBody(result), Exception = result.Exception });
            }

            IsSaving = false;
            return result;
        }

        public async Task<OASISResult<IEnumerable<ICelestialSpace>>> SaveCelestialSpacesAsync(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<ICelestialSpace>> result = new OASISResult<IEnumerable<ICelestialSpace>>();
            string errorMessage = $"Error occured in CelestialSpace.SaveCelestialSpacesAsync saving the celestial spaces for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason:";

            try
            {
                IsSaving = true;
                IStar star = GetCelestialSpaceNearestStar(result, errorMessage);
                result = HandleSaveCelestialSpaces(result, await star.CelestialBodyCore.GlobalHolonData.SaveHolonsAsync(_celestialSpaces, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType), "SaveCelestialSpacesAsync");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured calling star.CelestialBodyCore.GlobalHolonData.SaveHolonsAsync: {ex}", ex);
                OnCelestialSpacesError?.Invoke(this, new CelestialSpacesErrorEventArgs() { Reason = $"{result.Message}", Result = result, Exception = result.Exception });
            }

            IsSaving = false;
            return result;
        }

        public OASISResult<IEnumerable<ICelestialSpace>> SaveCelestialSpaces(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<ICelestialSpace>> result = new OASISResult<IEnumerable<ICelestialSpace>>();
            string errorMessage = $"Error occured in CelestialSpace.SaveCelestialSpaces saving the celestial spaces for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason:";

            try
            {
                IsSaving = true;
                IStar star = GetCelestialSpaceNearestStar(result, errorMessage);
                result = HandleSaveCelestialSpaces(result, star.CelestialBodyCore.GlobalHolonData.SaveHolons(_celestialSpaces, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType), "SaveCelestialSpaces");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured calling star.CelestialBodyCore.GlobalHolonData.SaveHolonsAsync: {ex}", ex);
                OnCelestialSpacesError?.Invoke(this, new CelestialSpacesErrorEventArgs() { Reason = $"{result.Message}", Result = result, Exception = result.Exception });
            }

            IsSaving = false;
            return result;
        }

        public async Task<OASISResult<IEnumerable<T>>> SaveCelestialSpacesAsync<T>(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : ICelestialBody, new()
        {
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();
            string errorMessage = $"Error occured in CelestialSpace.SaveCelestialSpacesAsync<T> saving the celestial Spaces for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason:";

            try
            {
                IsSaving = true;
                IStar star = GetCelestialSpaceNearestStar(result, errorMessage);
                result = HandleSaveCelestialSpaces(result, await star.CelestialBodyCore.GlobalHolonData.SaveHolonsAsync<T>(Mapper.Convert<ICelestialSpace, T>(_celestialSpaces), saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType), "SaveCelestialSpacesAsync<T>");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured calling star.CelestialBodyCore.GlobalHolonData.SaveHolonsAsync: {ex}", ex);
                OnCelestialSpacesError?.Invoke(this, new CelestialSpacesErrorEventArgs() { Reason = $"{result.Message}", Result = OASISResultHelper.CopyResultToICelestialSpace(result), Exception = result.Exception });
            }

            IsSaving = false;
            return result;
        }

        public async Task<OASISResult<IEnumerable<T>>> SaveCelestialSpaces<T>(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : ICelestialBody, new()
        {
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();
            string errorMessage = $"Error occured in CelestialSpace.SaveCelestialSpaces<T> saving the celestial Spaces for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason:";

            try
            {
                IsSaving = true;
                IStar star = GetCelestialSpaceNearestStar(result, errorMessage);
                result = HandleSaveCelestialSpaces(result, await star.CelestialBodyCore.GlobalHolonData.SaveHolonsAsync<T>(Mapper.Convert<ICelestialSpace, T>(_celestialSpaces), saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType), "SaveCelestialSpacesAsync<T>");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured calling star.CelestialBodyCore.GlobalHolonData.SaveHolonsAsync: {ex}", ex);
                OnCelestialSpacesError?.Invoke(this, new CelestialSpacesErrorEventArgs() { Reason = $"{result.Message}", Result = OASISResultHelper.CopyResultToICelestialSpace(result), Exception = result.Exception });
            }

            IsSaving = false;
            return result;
        }
    }
}
