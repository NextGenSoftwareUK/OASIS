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

        public new async Task<OASISResult<ICelestialSpace>> LoadAsync(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<ICelestialSpace> result = new OASISResult<ICelestialSpace>();
            IStar star = GetCelestialSpaceNearestStar(result, $"Error occured in CelestialSpace.LoadAsync method loading the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}.");
            result = HandleLoadCelestialSpace(result, await star.CelestialBodyCore.GlobalHolonData.LoadHolonAsync(this.Id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, providerType), "LoadAsync");
            return result;
        }

        public new OASISResult<ICelestialSpace> Load(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<ICelestialSpace> result = new OASISResult<ICelestialSpace>();
            IStar star = GetCelestialSpaceNearestStar(result, $"Error occured in CelestialSpace.Load method loading the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}.");
            result = HandleLoadCelestialSpace(result, star.CelestialBodyCore.GlobalHolonData.LoadHolon(this.Id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, providerType), "Load");
            return result;
        }

        public new async Task<OASISResult<T>> LoadAsync<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            OASISResult<T> result = new OASISResult<T>();
            IStar star = GetCelestialSpaceNearestStar(result, $"Error occured in CelestialSpace.LoadAsync<T> method loading the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}.");
            result = HandleLoadCelestialSpace(result, await star.CelestialBodyCore.GlobalHolonData.LoadHolonAsync<T>(this.Id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, providerType), "LoadAsync<T>");
            return result;
        }

        public new OASISResult<T> Load<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            OASISResult<T> result = new OASISResult<T>();
            IStar star = GetCelestialSpaceNearestStar(result, $"Error occured in CelestialSpace.Load<T> method loading the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}.");
            result = HandleLoadCelestialSpace(result, star.CelestialBodyCore.GlobalHolonData.LoadHolon<T>(this.Id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, providerType), "Load<T>");
            return result;
        }


        public async Task<OASISResult<IEnumerable<ICelestialBody>>> LoadCelestialBodiesAsync(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<ICelestialBody>> result = new OASISResult<IEnumerable<ICelestialBody>>();
            IStar star = GetCelestialSpaceNearestStar(result, $"Error occured in CelestialSpace.LoadCelestialBodiesAsync method loading the celestial bodies for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}.");
            result = HandleLoadCelestialBodies(result, await star.CelestialBodyCore.GlobalHolonData.LoadHolonsForParentAsync(this.Id, HolonType.All, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, providerType), "LoadCelestialBodiesAsync");
            return result;
        }

        public OASISResult<IEnumerable<ICelestialBody>> LoadCelestialBodies(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<ICelestialBody>> result = new OASISResult<IEnumerable<ICelestialBody>>();
            IStar star = GetCelestialSpaceNearestStar(result, $"Error occured in CelestialSpace.LoadCelestialBodies method loading the celestial bodies for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}.");
            result = HandleLoadCelestialBodies(result, star.CelestialBodyCore.GlobalHolonData.LoadHolonsForParent(this.Id, HolonType.All, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, providerType), "LoadCelestialBodies");
            return result;
        }

        public async Task<OASISResult<IEnumerable<T>>> LoadCelestialBodiesAsync<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default) where T : ICelestialBody, new()
        {
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();
            IStar star = GetCelestialSpaceNearestStar(result, $"Error occured in CelestialSpace.LoadCelestialBodiesAsync<T> method loading the celestial bodies for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}.");
            result = HandleLoadCelestialBodies(result, await star.CelestialBodyCore.GlobalHolonData.LoadHolonsForParentAsync<T>(this.Id, HolonType.All, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, providerType), "LoadCelestialBodiesAsync<T>");
            return result;
        }

        public OASISResult<IEnumerable<T>> LoadCelestialBodies<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default) where T : ICelestialBody, new()
        {
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();
            IStar star = GetCelestialSpaceNearestStar(result, $"Error occured in CelestialSpace.LoadCelestialBodies<T> method loading the celestial bodies for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}.");
            result = HandleLoadCelestialBodies(result, star.CelestialBodyCore.GlobalHolonData.LoadHolonsForParent<T>(this.Id, HolonType.All, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, providerType), "LoadCelestialBodies<T>");
            return result;
        }

        public async Task<OASISResult<IEnumerable<ICelestialSpace>>> LoadCelestialSpacesAsync(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<ICelestialSpace>> result = new OASISResult<IEnumerable<ICelestialSpace>>();
            IStar star = GetCelestialSpaceNearestStar(result, $"Error occured in CelestialSpace.LoadCelestialSpacesAsync method loading the celestial spaces for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}.");
            result = HandleLoadCelestialSpaces(result, await star.CelestialBodyCore.GlobalHolonData.LoadHolonsForParentAsync(this.Id, HolonType.All, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, providerType), "LoadCelestialSpacesAsync");
            return result;
        }

        public OASISResult<IEnumerable<ICelestialSpace>> LoadCelestialSpaces(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<ICelestialSpace>> result = new OASISResult<IEnumerable<ICelestialSpace>>();
            IStar star = GetCelestialSpaceNearestStar(result, $"Error occured in CelestialSpace.LoadCelestialSpaces method loading the celestial spaces for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}.");
            result = HandleLoadCelestialSpaces(result, star.CelestialBodyCore.GlobalHolonData.LoadHolonsForParent(this.Id, HolonType.All, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, providerType), "LoadCelestialSpaces");
            return result;
        }

        public async Task<OASISResult<IEnumerable<T>>> LoadCelestialSpacesAsync<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default) where T : ICelestialSpace, new()
        {
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();
            IStar star = GetCelestialSpaceNearestStar(result, $"Error occured in CelestialSpace.LoadCelestialSpacesAsync<T> method loading the celestial spaces for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}.");
            result = HandleLoadCelestialSpaces(result, await star.CelestialBodyCore.GlobalHolonData.LoadHolonsForParentAsync(this.Id, HolonType.All, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, providerType), "LoadCelestialSpacesAsync<T>");
            return result;
        }

        public OASISResult<IEnumerable<T>> LoadCelestialSpaces<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default) where T : ICelestialSpace, new()
        {
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();
            IStar star = GetCelestialSpaceNearestStar(result, $"Error occured in CelestialSpace.LoadCelestialSpaces<T> method loading the celestial spaces for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}.");
            result = HandleLoadCelestialSpaces(result, star.CelestialBodyCore.GlobalHolonData.LoadHolonsForParent(this.Id, HolonType.All, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, providerType), "LoadCelestialSpaces<T>");
            return result;
        }


        public async Task<OASISResult<ICelestialBodiesAndSpaces>> LoadCelestialBodiesAndSpacesAsync(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<ICelestialBodiesAndSpaces> result = new OASISResult<ICelestialBodiesAndSpaces>();
            IStar star = GetCelestialSpaceNearestStar(result, $"Error occured in CelestialSpace.LoadCelestialBodiesAndSpacesAsync method loading the celestial bodies and spaces for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}.");
            result = HandleLoadCelestialBodiesAndSpaces(result, await star.CelestialBodyCore.GlobalHolonData.LoadHolonsForParentAsync(this.Id, HolonType.All, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, providerType), "LoadCelestialBodiesAndSpacesAsync");
            return result;
        }

        public OASISResult<ICelestialBodiesAndSpaces> LoadCelestialBodiesAndSpaces(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<ICelestialBodiesAndSpaces> result = new OASISResult<ICelestialBodiesAndSpaces>();
            IStar star = GetCelestialSpaceNearestStar(result, $"Error occured in CelestialSpace.LoadCelestialBodiesAndSpaces method loading the celestial bodies and spaces for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}.");
            result = HandleLoadCelestialBodiesAndSpaces(result, star.CelestialBodyCore.GlobalHolonData.LoadHolonsForParent(this.Id, HolonType.All, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, providerType), "LoadCelestialBodiesAndSpaces");
            return result;
        }

        public async Task<OASISResult<ICelestialBodiesAndSpaces<T1, T2>>> LoadCelestialBodiesAndSpacesAsync<T1, T2>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default) where T1 : ICelestialBody, new() where T2 : ICelestialSpace, new()
        {
            OASISResult<ICelestialBodiesAndSpaces<T1, T2>> result = new OASISResult<ICelestialBodiesAndSpaces<T1, T2>>();
            IStar star = GetCelestialSpaceNearestStar(result, $"Error occured in CelestialSpace.LoadCelestialBodiesAndSpacesAsync<T1, T2> method loading the celestial bodies and spaces for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}.");
            result = HandleLoadCelestialBodiesAndSpaces(result, await star.CelestialBodyCore.GlobalHolonData.LoadHolonsForParentAsync(this.Id, HolonType.All, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, providerType), "LoadCelestialBodiesAndSpacesAsync<T>");
            return result;
        }

        public OASISResult<ICelestialBodiesAndSpaces<T1, T2>> LoadCelestialBodiesAndSpaces<T1, T2>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default) where T1 : ICelestialBody, new() where T2 : ICelestialSpace, new()
        {
            OASISResult<ICelestialBodiesAndSpaces<T1, T2>> result = new OASISResult<ICelestialBodiesAndSpaces<T1, T2>>();
            IStar star = GetCelestialSpaceNearestStar(result, $"Error occured in CelestialSpace.LoadCelestialBodiesAndSpaces<T1, T2> method loading the celestial bodies and spaces for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}.");
            result = HandleLoadCelestialBodiesAndSpaces(result, star.CelestialBodyCore.GlobalHolonData.LoadHolonsForParent(this.Id, HolonType.All, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, providerType), "LoadCelestialBodiesAndSpaces<T>");
            return result;
        }

        public new virtual async Task<OASISResult<ICelestialSpace>> SaveAsync(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<ICelestialSpace> result = new OASISResult<ICelestialSpace>();
            string errorMessage = $"Error occured in CelestialSpace.SaveAsync saving the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason:";

            try
            {
                IStar star = GetCelestialSpaceNearestStar(result, errorMessage);
                result = HandleSaveCelestialSpace(result, await star.CelestialBodyCore.GlobalHolonData.SaveHolonAsync(this, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType), "SaveAsync");

                //TODO: We could of course just save using the one line below instead of the 2 lines above but then it would break the STAR NET design of the stars being responsible for loading/saving the celestialspace/celestial bodies in its orbit.
                //HandleSaveCelestialSpace(result, await base.SaveAsync(saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType), "SaveAsync");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured calling star.CelestialBodyCore.GlobalHolonData.SaveHolonAsync: {ex}", ex);
                OnCelestialSpaceError?.Invoke(this, new CelestialSpaceErrorEventArgs() { Reason = $"{result.Message}", Result = result, Exception = result.Exception });
            }

            return result;
        }

        public new virtual OASISResult<ICelestialSpace> Save(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<ICelestialSpace> result = new OASISResult<ICelestialSpace>();
            string errorMessage = $"Error occured in CelestialSpace.Save saving the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason:";

            try
            {
                IStar star = GetCelestialSpaceNearestStar(result, errorMessage);
                SetCelestialHolonMetaData();
                result = HandleSaveCelestialSpace(result, star.CelestialBodyCore.GlobalHolonData.SaveHolon(this, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType), "Save");

                //TODO: We could of course just save using the one line below instead of the 2 lines above but then it would break the STAR NET design of the stars being responsible for loading/saving the celestialspace/celestial bodies in its orbit.
                //HandleSaveCelestialSpace(result, await base.SaveAsync(saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType), "SaveAsync");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured calling star.CelestialBodyCore.GlobalHolonData.SaveHolonAsync: {ex}", ex);
                OnCelestialSpaceError?.Invoke(this, new CelestialSpaceErrorEventArgs() { Reason = $"{result.Message}", Result = result, Exception = result.Exception });
            }

            return result;
        }

        public new virtual async Task<OASISResult<T>> SaveAsync<T>(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            OASISResult<T> result = new OASISResult<T>();
            string errorMessage = $"Error occured in CelestialSpace.SaveAsync<T> saving the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason:";

            try
            {
                IStar star = GetCelestialSpaceNearestStar(result, errorMessage);
                SetCelestialHolonMetaData();
                result = HandleSaveCelestialSpace(result, await star.CelestialBodyCore.GlobalHolonData.SaveHolonAsync<T>(this, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType), "SaveAsync");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured calling star.CelestialBodyCore.GlobalHolonData.SaveHolonAsync<T>: {ex}", ex);
                OnCelestialSpaceError?.Invoke(this, new CelestialSpaceErrorEventArgs() { Reason = $"{result.Message}", Result = OASISResultHelper.CopyResultToICelestialSpace(result), Exception = result.Exception });
            }

            return result;
        }

        public new virtual OASISResult<T> Save<T>(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            OASISResult<T> result = new OASISResult<T>();
            string errorMessage = $"Error occured in CelestialSpace.Save<T> saving the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason:";

            try
            {
                IStar star = GetCelestialSpaceNearestStar(result, errorMessage);
                SetCelestialHolonMetaData();
                result = HandleSaveCelestialSpace(result, star.CelestialBodyCore.GlobalHolonData.SaveHolon<T>(this, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType), "SaveAsync");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured calling star.CelestialBodyCore.GlobalHolonData.SaveHolon<T>: {ex}", ex);
                OnCelestialSpaceError?.Invoke(this, new CelestialSpaceErrorEventArgs() { Reason = $"{result.Message}", Result = OASISResultHelper.CopyResultToICelestialSpace(result), Exception = result.Exception });
            }

            return result;
        }
    }
}
