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
        private void RegisterCelestialBodies(IEnumerable<ICelestialBody> celestialBodies, bool unregisterExistingBodiesFirst = true)
        {
            if (unregisterExistingBodiesFirst)
                UnregisterAllCelestialSpaces();

            foreach (ICelestialBody celestialBody in _celestialBodies)
                RegisterCelestialBody(celestialBody);
        }

        private void RegisterCelestialBody(ICelestialBody celestialBody)
        {
            celestialBody.OnCelestialBodyLoaded += CelestialBody_OnCelestialBodyLoaded;
            celestialBody.OnCelestialBodySaved += CelestialBody_OnCelestialBodySaved;
            celestialBody.OnCelestialBodyError += CelestialBody_OnCelestialBodyError;
            celestialBody.OnHolonLoaded += CelestialBody_OnHolonLoaded;
            celestialBody.OnHolonSaved += CelestialBody_OnHolonSaved;
            celestialBody.OnHolonError += CelestialBody_OnHolonError;
            celestialBody.OnHolonsLoaded += CelestialBody_OnHolonsLoaded;
            celestialBody.OnHolonsSaved += CelestialBody_OnHolonsSaved;
            celestialBody.OnHolonsError += CelestialBody_OnHolonsError;
            celestialBody.OnZomeLoaded += CelestialBody_OnZomeLoaded;
            celestialBody.OnZomeSaved += CelestialBody_OnZomeSaved;
            celestialBody.OnZomeError += CelestialBody_OnZomeError;
            celestialBody.OnZomesLoaded += CelestialBody_OnZomesLoaded;
            celestialBody.OnZomesSaved += CelestialBody_OnZomesSaved;
            celestialBody.OnZomesError += CelestialBody_OnZomesError;
        }

        private void RegisterCelestialSpaces(IEnumerable<ICelestialSpace> celestialSpaces, bool unregisterExistingSpacesFirst = true)
        {
            if (unregisterExistingSpacesFirst)
                UnregisterAllCelestialSpaces();

            foreach (ICelestialSpace celestialSpace in _celestialSpaces)
                RegisterCelestialSpace(celestialSpace);
        }

        private void RegisterCelestialSpace(ICelestialSpace celestialSpace)
        {
            celestialSpace.OnCelestialSpaceLoaded += CelestialSpace_OnCelestialSpaceLoaded;
            celestialSpace.OnCelestialSpaceSaved += CelestialSpace_OnCelestialSpaceSaved;
            celestialSpace.OnCelestialSpaceError += CelestialSpace_OnCelestialSpaceError;
            celestialSpace.OnCelestialSpacesLoaded += CelestialSpace_OnCelestialSpacesLoaded;
            celestialSpace.OnCelestialSpacesSaved += CelestialSpace_OnCelestialSpacesSaved;
            celestialSpace.OnCelestialSpacesError += CelestialSpace_OnCelestialSpacesError;
            celestialSpace.OnCelestialBodyLoaded += CelestialSpace_OnCelestialBodyLoaded;
            celestialSpace.OnCelestialBodySaved += CelestialSpace_OnCelestialBodySaved;
            celestialSpace.OnCelestialBodyError += CelestialSpace_OnCelestialBodyError;
            celestialSpace.OnCelestialBodiesLoaded += CelestialSpace_OnCelestialBodiesLoaded;
            celestialSpace.OnCelestialBodiesSaved += CelestialSpace_OnCelestialBodiesSaved;
            celestialSpace.OnCelestialBodiesError += CelestialSpace_OnCelestialBodiesError;
            celestialSpace.OnHolonLoaded += CelestialSpace_OnHolonLoaded;
            celestialSpace.OnHolonSaved += CelestialSpace_OnHolonSaved;
            celestialSpace.OnHolonError += CelestialSpace_OnHolonError;
            celestialSpace.OnHolonsLoaded += CelestialSpace_OnHolonsLoaded;
            celestialSpace.OnHolonsSaved += CelestialSpace_OnHolonsSaved;
            celestialSpace.OnHolonsError += CelestialSpace_OnHolonsError;
            celestialSpace.OnZomeLoaded += CelestialSpace_OnZomeLoaded;
            celestialSpace.OnZomeSaved += CelestialSpace_OnZomeSaved;
            celestialSpace.OnZomeError += CelestialSpace_OnZomeError;
            celestialSpace.OnZomesLoaded += CelestialSpace_OnZomesLoaded;
            celestialSpace.OnZomesSaved += CelestialSpace_OnZomesSaved;
            celestialSpace.OnZomesError += CelestialSpace_OnZomesError;
        }

        private void UnregisterCelestialBody(ICelestialBody celestialBody)
        {
            celestialBody.OnCelestialBodyLoaded -= CelestialBody_OnCelestialBodyLoaded;
            celestialBody.OnCelestialBodySaved -= CelestialBody_OnCelestialBodySaved;
            celestialBody.OnCelestialBodyError -= CelestialBody_OnCelestialBodyError;
            celestialBody.OnHolonLoaded -= CelestialBody_OnHolonLoaded;
            celestialBody.OnHolonSaved -= CelestialBody_OnHolonSaved;
            celestialBody.OnHolonError -= CelestialBody_OnHolonError;
            celestialBody.OnHolonsLoaded -= CelestialBody_OnHolonsLoaded;
            celestialBody.OnHolonsSaved -= CelestialBody_OnHolonsSaved;
            celestialBody.OnHolonsError -= CelestialBody_OnHolonsError;
            celestialBody.OnZomeLoaded -= CelestialBody_OnZomeLoaded;
            celestialBody.OnZomeSaved -= CelestialBody_OnZomeSaved;
            celestialBody.OnZomeError -= CelestialBody_OnZomeError;
            celestialBody.OnZomesLoaded -= CelestialBody_OnZomesLoaded;
            celestialBody.OnZomesSaved -= CelestialBody_OnZomesSaved;
            celestialBody.OnZomesError -= CelestialBody_OnZomesError;
        }

        private void UnregisterAllCelestialBodies()
        {
            //First unsubscibe events to prevent any memory leaks.
            foreach (ICelestialBody celestialBody in _celestialBodies)
                UnregisterCelestialBody(celestialBody);

           // _celestialBodies = new List<ICelestialBody>();
        }

        private void UnregisterAllCelestialSpaces()
        {
            //First unsubscibe events to prevent any memory leaks.
            foreach (ICelestialSpace celestialSpace in _celestialSpaces)
                UnregisterCelestialSpace(celestialSpace);

           // _celestialSpaces = new List<ICelestialSpace>();
        }

        private void UnregisterCelestialSpace(ICelestialSpace celestialSpace)
        {
            celestialSpace.OnCelestialSpaceLoaded -= CelestialSpace_OnCelestialSpaceLoaded;
            celestialSpace.OnCelestialSpaceSaved -= CelestialSpace_OnCelestialSpaceSaved;
            celestialSpace.OnCelestialSpaceError -= CelestialSpace_OnCelestialSpaceError;
            celestialSpace.OnCelestialSpacesLoaded -= CelestialSpace_OnCelestialSpacesLoaded;
            celestialSpace.OnCelestialSpacesSaved -= CelestialSpace_OnCelestialSpacesSaved;
            celestialSpace.OnCelestialSpacesError -= CelestialSpace_OnCelestialSpacesError;
            celestialSpace.OnCelestialBodyLoaded -= CelestialSpace_OnCelestialBodyLoaded;
            celestialSpace.OnCelestialBodySaved -= CelestialSpace_OnCelestialBodySaved;
            celestialSpace.OnCelestialBodyError -= CelestialSpace_OnCelestialBodyError;
            celestialSpace.OnCelestialBodiesLoaded -= CelestialSpace_OnCelestialBodiesLoaded;
            celestialSpace.OnCelestialBodiesSaved -= CelestialSpace_OnCelestialBodiesSaved;
            celestialSpace.OnCelestialBodiesError -= CelestialSpace_OnCelestialBodiesError;
            celestialSpace.OnHolonLoaded -= CelestialSpace_OnHolonLoaded;
            celestialSpace.OnHolonSaved -= CelestialSpace_OnHolonSaved;
            celestialSpace.OnHolonError -= CelestialSpace_OnHolonError;
            celestialSpace.OnHolonsLoaded -= CelestialSpace_OnHolonsLoaded;
            celestialSpace.OnHolonsSaved -= CelestialSpace_OnHolonsSaved;
            celestialSpace.OnHolonsError -= CelestialSpace_OnHolonsError;
            celestialSpace.OnZomeLoaded -= CelestialSpace_OnZomeLoaded;
            celestialSpace.OnZomeSaved -= CelestialSpace_OnZomeSaved;
            celestialSpace.OnZomeError -= CelestialSpace_OnZomeError;
            celestialSpace.OnZomesLoaded -= CelestialSpace_OnZomesLoaded;
            celestialSpace.OnZomesSaved -= CelestialSpace_OnZomesSaved;
            celestialSpace.OnZomesError -= CelestialSpace_OnZomesError;
        }

        private IStar GetCelestialSpaceNearestStar(OASISResult<ICelestialBodiesAndSpaces> result, string errorMessage)
        {
            IStar star = GetCelestialSpaceNearestStar();

            if (star == null)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage}. Could not find the nearest star for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}.");
                OnCelestialSpacesError?.Invoke(this, new CelestialSpacesErrorEventArgs() { Reason = $"{result.Message}", Result = CreateCelestialSpacesResult(result), Exception = result.Exception });
                OnCelestialBodiesError?.Invoke(this, new CelestialBodiesErrorEventArgs() { Reason = $"{result.Message}", Result = CreateCelestialBodiesResult(result), Exception = result.Exception });
            }

            return star;
        }

        private IStar GetCelestialSpaceNearestStar<T1, T2>(OASISResult<ICelestialBodiesAndSpaces<T1, T2>> result, string errorMessage) where T1 : ICelestialBody where T2 : ICelestialSpace, new()
        {
            IStar star = GetCelestialSpaceNearestStar();

            if (star == null)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage}. Could not find the nearest star for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}.");
                OnCelestialSpacesError?.Invoke(this, new CelestialSpacesErrorEventArgs() { Reason = $"{result.Message}", Result = CreateCelestialSpacesResult(result), Exception = result.Exception });
                OnCelestialBodiesError?.Invoke(this, new CelestialBodiesErrorEventArgs() { Reason = $"{result.Message}", Result = CreateCelestialBodiesResult(result), Exception = result.Exception });
            }

            return star;
        }

        private IStar GetCelestialSpaceNearestStar<T>(OASISResult<IEnumerable<T>> result, string errorMessage) where T : IHolon
        {
            IStar star = GetCelestialSpaceNearestStar();

            if (star == null)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage}. Could not find the nearest star for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}.");
                OnCelestialSpacesError?.Invoke(this, new CelestialSpacesErrorEventArgs() { Reason = $"{result.Message}", Result = OASISResultHelper.CopyResultToICelestialSpace(result), Exception = result.Exception });
            }

            return star;
        }

        private IStar GetCelestialSpaceNearestStar<T>(OASISResult<T> result, string errorMessage) where T : IHolon
        {
            IStar star = GetCelestialSpaceNearestStar();

            if (star == null)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Could not find the nearest star for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}.");
                OnCelestialSpaceError?.Invoke(this, new CelestialSpaceErrorEventArgs() { Reason = $"{result.Message}", Result = OASISResultHelper.CopyResultToICelestialSpace(result), Exception = result.Exception });
            }

            return star;
        }

        private IStar GetCelestialSpaceNearestStar()
        {
            switch (this.HolonType)
            {
                case HolonType.Omniverse:
                    NearestStar = ParentGreatGrandSuperStar;
                    break;

                case HolonType.Multiverse:
                case HolonType.Universe:
                    NearestStar = ParentGrandSuperStar;
                    break;

                case HolonType.Galaxy:
                case HolonType.GalaxyCluster:
                    NearestStar = ParentSuperStar;
                    break;

                case HolonType.SolarSystem:
                    NearestStar = ParentStar;
                    break;

                    //case HolonType.Omniverse:
                    //    NearestStar = ParentGreatGrandSuperStar != null ? ParentGreatGrandSuperStar : STAR.DefaultGreatGrandSuperStar;
                    //    break;

                    //case HolonType.Multiverse:
                    //case HolonType.Universe:
                    //    NearestStar = ParentGrandSuperStar != null ? ParentGrandSuperStar : STAR.DefaultGrandSuperStar;
                    //    break;

                    //case HolonType.Galaxy:
                    //case HolonType.GalaxyCluster:
                    //    NearestStar = ParentSuperStar != null ? ParentSuperStar : STAR.DefaultSuperStar;
                    //    break;

                    //case HolonType.SolarSystem:
                    //    NearestStar = ParentStar != null ? ParentStar : STAR.DefaultStar;
                    //    break;
            }

            //If we could not find the nearest star then we keep going up the chain of stars (STARNET/STARCHAIN) till we find one! ;-)
            if (NearestStar == null)
            {
                if (this.ParentStar != null)
                    NearestStar = ParentStar;

                else if (this.ParentSuperStar != null)
                    NearestStar = ParentSuperStar;

                else if (this.ParentGrandSuperStar != null)
                    NearestStar = ParentGrandSuperStar;

                else if (this.ParentGreatGrandSuperStar != null)
                    NearestStar = ParentGreatGrandSuperStar;

                else
                    NearestStar = STAR.DefaultGreatGrandSuperStar; //This is Godhead/Source (there is only ever one and is always avaiable to everyone! ;-) )
            }

            return NearestStar;
        }

        private OASISResult<ICelestialSpace> HandleLoadCelestialSpace(OASISResult<ICelestialSpace> result, OASISResult<IHolon> holonResult, string methodName)
        {
            result = OASISResultHelper.CopyResultToICelestialSpace(holonResult);

            if (result != null && !result.IsError && result.Result != null)
            {
                if (result.Result.Children != null)
                {
                    _celestialBodies = GetCelestialBodies(result.Result.Children).ToList();
                    _celestialSpaces = GetCelestialSpaces(result.Result.Children).ToList();

                    RegisterCelestialBodies(this.CelestialBodies);
                    RegisterCelestialSpaces(this.CelestialSpaces);
                }

                OnCelestialSpaceLoaded?.Invoke(this, new CelestialSpaceLoadedEventArgs() { Result = result });
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"An errror occured in CelestialSpace.{methodName} whilst loading the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason: {holonResult.Message}");
                OnCelestialSpaceError?.Invoke(this, new CelestialSpaceErrorEventArgs() { Reason = $"{result.Message}", Result = result, Exception = result.Exception });
            }

            return result;
        }


        private OASISResult<IEnumerable<ICelestialSpace>> HandleLoadCelestialSpaces(OASISResult<IEnumerable<ICelestialSpace>> result, OASISResult<IEnumerable<IHolon>> holonResult, string methodName)
        {
            result = OASISResultHelper.CopyResultToICelestialSpace(holonResult);

            if (result != null && !result.IsError && result.Result != null)
            {
                _celestialSpaces = GetCelestialSpaces(result.Result).ToList();
                RegisterCelestialSpaces(this.CelestialSpaces);
                OnCelestialSpacesLoaded?.Invoke(this, new CelestialSpacesLoadedEventArgs() { Result = result });
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"An errror occured in CelestialSpace.{methodName} whilst loading the celestial bodies for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason: {holonResult.Message}");
                OnCelestialSpacesError?.Invoke(this, new CelestialSpacesErrorEventArgs() { Reason = $"{result.Message}", Result = result, Exception = result.Exception });
            }

            return result;
        }

        private OASISResult<T1> HandleLoadCelestialSpace<T1, T2>(OASISResult<T1> result, OASISResult<T2> holonResult, string methodName) where T1 : IHolon, new() where T2 : IHolon
        {
            result = OASISResultHelper.CopyResultAndCreateToResultObjectIfNull<T2, T1>(holonResult);

            if (result != null && !result.IsError && result.Result != null)
            {
                if (result.Result.Children != null)
                {
                    _celestialBodies = GetCelestialBodies(result.Result.Children).ToList();
                    _celestialSpaces = GetCelestialSpaces(result.Result.Children).ToList();

                    RegisterCelestialBodies(this.CelestialBodies);
                    RegisterCelestialSpaces(this.CelestialSpaces);
                }

                OnCelestialSpaceLoaded?.Invoke(this, new CelestialSpaceLoadedEventArgs() { Result = OASISResultHelper.CopyResultToICelestialSpace(result) });
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"An errror occured in CelestialSpace.{methodName} whilst loading the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason: {holonResult.Message}");
                OnCelestialSpaceError?.Invoke(this, new CelestialSpaceErrorEventArgs() { Reason = $"{result.Message}", Result = OASISResultHelper.CopyResultToICelestialSpace(result), Exception = result.Exception });
            }

            return result;
        }


        private OASISResult<IEnumerable<T1>> HandleLoadCelestialSpaces<T1, T2>(OASISResult<IEnumerable<T1>> result, OASISResult<IEnumerable<T2>> holonResult, string methodName) where T1 : IHolon, new() where T2 : IHolon
        {
            result = OASISResultHelper.CopyResultAndCreateToResultObjectIfNull<T2, T1>(holonResult);

            if (result != null && !result.IsError && result.Result != null)
            {
                _celestialSpaces = GetCelestialSpaces(result.Result).ToList();
                RegisterCelestialSpaces(this.CelestialSpaces);
                OnCelestialSpacesLoaded?.Invoke(this, new CelestialSpacesLoadedEventArgs() { Result = OASISResultHelper.CopyResultToICelestialSpace(result) });
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"An errror occured in CelestialSpace.{methodName} whilst loading the celestial bodies for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason: {holonResult.Message}");
                OnCelestialSpacesError?.Invoke(this, new CelestialSpacesErrorEventArgs() { Reason = $"{result.Message}", Result = OASISResultHelper.CopyResultToICelestialSpace(result), Exception = result.Exception });
            }

            return result;
        }

        private OASISResult<IEnumerable<ICelestialBody>> HandleLoadCelestialBodies(OASISResult<IEnumerable<ICelestialBody>> result, OASISResult<IEnumerable<IHolon>> holonResult, string methodName)
        {
            result = OASISResultHelper.CopyResultToICelestialBody(holonResult);

            if (result != null && !result.IsError && result.Result != null)
            {
                _celestialBodies = GetCelestialBodies(result.Result).ToList();
                RegisterCelestialBodies(this.CelestialBodies);
                OnCelestialBodiesLoaded?.Invoke(this, new CelestialBodiesLoadedEventArgs() { Result = result });
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"An errror occured in CelestialSpace.{methodName} whilst loading the celestial spaces for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason: {holonResult.Message}");
                OnCelestialBodiesError?.Invoke(this, new CelestialBodiesErrorEventArgs() { Reason = $"{result.Message}", Result = result, Exception = result.Exception });
            }

            return result;
        }

        private OASISResult<IEnumerable<T1>> HandleLoadCelestialBodies<T1, T2>(OASISResult<IEnumerable<T1>> result, OASISResult<IEnumerable<T2>> holonResult, string methodName) where T1 : IHolon, new() where T2 : IHolon
        {
            result = OASISResultHelper.CopyResultAndCreateToResultObjectIfNull<T2, T1>(holonResult);

            if (result != null && !result.IsError && result.Result != null)
            {
                _celestialBodies = GetCelestialBodies(result.Result).ToList();
                RegisterCelestialBodies(this.CelestialBodies);
                OnCelestialBodiesLoaded?.Invoke(this, new CelestialBodiesLoadedEventArgs() { Result = OASISResultHelper.CopyResultToICelestialBody(result) });
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"An errror occured in CelestialSpace.{methodName} whilst loading the celestial spaces for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason: {holonResult.Message}");
                OnCelestialBodiesError?.Invoke(this, new CelestialBodiesErrorEventArgs() { Reason = $"{result.Message}", Result = OASISResultHelper.CopyResultToICelestialBody(result), Exception = result.Exception });
            }

            return result;
        }

        private OASISResult<ICelestialBodiesAndSpaces> HandleLoadCelestialBodiesAndSpaces<T>(OASISResult<ICelestialBodiesAndSpaces> result, OASISResult<IEnumerable<T>> holonResult, string methodName) where T : IHolon
        {
            result = MapCelestialBodieAndSpacessResult(holonResult, result);
            OASISResult<IEnumerable<ICelestialBody>> celesialBodiesResult = CreateCelestialBodiesResult(result);
            OASISResult<IEnumerable<ICelestialSpace>> celesialSpacessResult = CreateCelestialSpacesResult(result);

            if (result != null && !result.IsError && result.Result != null)
            {
                _celestialBodies = result.Result.CelestialBodies.ToList();
                _celestialSpaces = result.Result.CelestialSpaces.ToList();

                RegisterCelestialBodies(this.CelestialBodies);
                RegisterCelestialSpaces(this.CelestialSpaces);

                OnCelestialBodiesLoaded?.Invoke(this, new CelestialBodiesLoadedEventArgs() { Result = celesialBodiesResult });
                OnCelestialSpacesLoaded?.Invoke(this, new CelestialSpacesLoadedEventArgs() { Result = celesialSpacessResult });
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"An errror occured in CelestialSpace.{methodName} whilst loading the celestial bodies and spaces for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason: {holonResult.Message}");
                OnCelestialBodiesError?.Invoke(this, new CelestialBodiesErrorEventArgs() { Reason = $"{result.Message}", Result = celesialBodiesResult, Exception = result.Exception });
                OnCelestialSpacesError?.Invoke(this, new CelestialSpacesErrorEventArgs() { Reason = $"{result.Message}", Result = celesialSpacessResult, Exception = result.Exception });
            }

            return result;
        }

        private OASISResult<ICelestialBodiesAndSpaces<T1, T2>> HandleLoadCelestialBodiesAndSpaces<T1, T2, T3>(OASISResult<ICelestialBodiesAndSpaces<T1, T2>> result, OASISResult<IEnumerable<T3>> holonResult, string methodName) where T1 : ICelestialBody where T2 : ICelestialSpace, new() where T3 : IHolon
        {
            result = MapCelestialBodieAndSpacessResult(holonResult, result);
            OASISResult<IEnumerable<ICelestialBody>> celesialBodiesResult = CreateCelestialBodiesResult(result);
            OASISResult<IEnumerable<ICelestialSpace>> celesialSpacessResult = CreateCelestialSpacesResult(result);

            if (result != null && !result.IsError && result.Result != null)
            {
                _celestialBodies = Mapper.Convert<T1, ICelestialBody>(result.Result.CelestialBodies).ToList();
                _celestialSpaces = Mapper.Convert<T2, ICelestialSpace>(result.Result.CelestialSpaces).ToList();

                RegisterCelestialBodies(this.CelestialBodies);
                RegisterCelestialSpaces(this.CelestialSpaces);

                OnCelestialBodiesLoaded?.Invoke(this, new CelestialBodiesLoadedEventArgs() { Result = celesialBodiesResult });
                OnCelestialSpacesLoaded?.Invoke(this, new CelestialSpacesLoadedEventArgs() { Result = celesialSpacessResult });
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"An errror occured in CelestialSpace.{methodName} whilst loading the celestial bodies and spaces for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason: {holonResult.Message}");
                OnCelestialBodiesError?.Invoke(this, new CelestialBodiesErrorEventArgs() { Reason = $"{result.Message}", Result = celesialBodiesResult, Exception = result.Exception });
                OnCelestialSpacesError?.Invoke(this, new CelestialSpacesErrorEventArgs() { Reason = $"{result.Message}", Result = celesialSpacessResult, Exception = result.Exception });
            }

            return result;
        }

        private OASISResult<ICelestialSpace> HandleSaveCelestialSpace(OASISResult<ICelestialSpace> result, OASISResult<IHolon> holonResult, string methodName)
        {
            string errorMessage = $"An errror occured in CelestialSpace.{methodName} whilst saving the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason:";

            try
            {
                result = OASISResultHelper.CopyResultToICelestialSpace(holonResult);

                if (result != null && !result.IsError && result.Result != null)
                    OnCelestialSpaceSaved?.Invoke(this, new CelestialSpaceSavedEventArgs() { Result = result });
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} {holonResult.Message}");
                    OnCelestialSpaceError?.Invoke(this, new CelestialSpaceErrorEventArgs() { Reason = $"{result.Message}", Result = OASISResultHelper.CopyResultToICelestialSpace(result), Exception = result.Exception });
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}", ex);
                OnCelestialSpaceError?.Invoke(this, new CelestialSpaceErrorEventArgs() { Reason = $"{result.Message}", Result = result, Exception = result.Exception });
            }

            return result;
        }

        private OASISResult<T1> HandleSaveCelestialSpace<T1, T2>(OASISResult<T1> result, OASISResult<T2> holonResult, string methodName) where T1 : IHolon, new() where T2 : IHolon, new()
        {
            string errorMessage = $"An errror occured in CelestialSpace.{methodName} whilst saving the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason:";

            try
            {
                result = OASISResultHelper.CopyResultAndCreateToResultObjectIfNull<T2, T1>(holonResult);

                if (result != null && !result.IsError && result.Result != null)
                    OnCelestialSpaceSaved?.Invoke(this, new CelestialSpaceSavedEventArgs() { Result = OASISResultHelper.CopyResultToICelestialSpace(result) });
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} {holonResult.Message}");
                    OnCelestialSpaceError?.Invoke(this, new CelestialSpaceErrorEventArgs() { Reason = $"{result.Message}", Result = OASISResultHelper.CopyResultToICelestialSpace(result), Exception = result.Exception });
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}", ex);
                OnCelestialSpaceError?.Invoke(this, new CelestialSpaceErrorEventArgs() { Reason = $"{result.Message}", Result = OASISResultHelper.CopyResultToICelestialSpace(result), Exception = result.Exception });
            }

            return result;
        }

        private OASISResult<IEnumerable<T1>> HandleSaveCelestialBodies<T1, T2>(OASISResult<IEnumerable<T1>> result, OASISResult<IEnumerable<T2>> holonResult, string methodName) where T1 : IHolon, new() where T2 : IHolon
        {
            result = OASISResultHelper.CopyResultAndCreateToResultObjectIfNull<T2, T1>(holonResult);

            if (result != null && !result.IsError && result.Result != null)
                OnCelestialBodiesSaved?.Invoke(this, new CelestialBodiesSavedEventArgs() { Result = OASISResultHelper.CopyResultToICelestialBody(result) });
            else
            {
                OASISErrorHandling.HandleError(ref result, $"An errror occured in CelestialSpace.{methodName} whilst loading the celestial spaces for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason: {holonResult.Message}");
                OnCelestialBodiesError?.Invoke(this, new CelestialBodiesErrorEventArgs() { Reason = $"{result.Message}", Result = OASISResultHelper.CopyResultToICelestialBody(result), Exception = result.Exception });
            }

            return result;
        }

        private OASISResult<IEnumerable<ICelestialBody>> HandleSaveCelestialBodies(OASISResult<IEnumerable<ICelestialBody>> result, OASISResult<IEnumerable<IHolon>> holonResult, string methodName)
        {
            result = OASISResultHelper.CopyResultToICelestialBody(holonResult);

            if (result != null && !result.IsError && result.Result != null)
                OnCelestialBodiesSaved?.Invoke(this, new CelestialBodiesSavedEventArgs() { Result = OASISResultHelper.CopyResultToICelestialBody(result) });
            else
            {
                OASISErrorHandling.HandleError(ref result, $"An errror occured in CelestialSpace.{methodName} whilst loading the celestial spaces for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason: {holonResult.Message}");
                OnCelestialBodiesError?.Invoke(this, new CelestialBodiesErrorEventArgs() { Reason = $"{result.Message}", Result = OASISResultHelper.CopyResultToICelestialBody(result), Exception = result.Exception });
            }

            return result;
        }

        private OASISResult<IEnumerable<ICelestialSpace>> HandleSaveCelestialSpaces(OASISResult<IEnumerable<ICelestialSpace>> result, OASISResult<IEnumerable<IHolon>> holonResult, string methodName)
        {
            result = OASISResultHelper.CopyResultToICelestialSpace(holonResult);

            if (result != null && !result.IsError && result.Result != null)
                OnCelestialSpacesSaved?.Invoke(this, new CelestialSpacesSavedEventArgs() { Result = OASISResultHelper.CopyResultToICelestialSpace(result) });
            else
            {
                OASISErrorHandling.HandleError(ref result, $"An errror occured in CelestialSpace.{methodName} whilst loading the celestial spaces for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason: {holonResult.Message}");
                OnCelestialSpacesError?.Invoke(this, new CelestialSpacesErrorEventArgs() { Reason = $"{result.Message}", Result = OASISResultHelper.CopyResultToICelestialSpace(result), Exception = result.Exception });
            }

            return result;
        }

        private OASISResult<IEnumerable<T1>> HandleSaveCelestialSpaces<T1, T2>(OASISResult<IEnumerable<T1>> result, OASISResult<IEnumerable<T2>> holonResult, string methodName) where T1 : IHolon, new() where T2 : IHolon, new()
        {
            result = OASISResultHelper.CopyResultAndCreateToResultObjectIfNull<T2, T1>(holonResult);

            if (result != null && !result.IsError && result.Result != null)
                OnCelestialSpacesSaved?.Invoke(this, new CelestialSpacesSavedEventArgs() { Result = OASISResultHelper.CopyResultToICelestialSpace(result) });
            else
            {
                OASISErrorHandling.HandleError(ref result, $"An errror occured in CelestialSpace.{methodName} whilst loading the celestial spaces for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason: {holonResult.Message}");
                OnCelestialSpacesError?.Invoke(this, new CelestialSpacesErrorEventArgs() { Reason = $"{result.Message}", Result = OASISResultHelper.CopyResultToICelestialSpace(result), Exception = result.Exception });
            }

            return result;
        }

        private OASISResult<ICelestialBodiesAndSpaces> HandleSaveCelestialBodiesAndSpaces<T>(OASISResult<ICelestialBodiesAndSpaces> result, OASISResult<IEnumerable<T>> holonResult, string methodName) where T : IHolon
        {
            result = MapCelestialBodieAndSpacessResult(holonResult, result);
            OASISResult<IEnumerable<ICelestialBody>> celesialBodiesResult = CreateCelestialBodiesResult(result);
            OASISResult<IEnumerable<ICelestialSpace>> celesialSpacessResult = CreateCelestialSpacesResult(result);

            if (result != null && !result.IsError && result.Result != null)
            {
                _celestialBodies = result.Result.CelestialBodies.ToList();
                _celestialSpaces = result.Result.CelestialSpaces.ToList();

                RegisterCelestialBodies(this.CelestialBodies);
                RegisterCelestialSpaces(this.CelestialSpaces);

                OnCelestialBodiesSaved?.Invoke(this, new CelestialBodiesSavedEventArgs() { Result = celesialBodiesResult });
                OnCelestialSpacesSaved?.Invoke(this, new CelestialSpacesSavedEventArgs() { Result = celesialSpacessResult });
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"An errror occured in CelestialSpace.{methodName} whilst saving the celestial bodies and spaces for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason: {holonResult.Message}");
                OnCelestialBodiesError?.Invoke(this, new CelestialBodiesErrorEventArgs() { Reason = $"{result.Message}", Result = celesialBodiesResult, Exception = result.Exception });
                OnCelestialSpacesError?.Invoke(this, new CelestialSpacesErrorEventArgs() { Reason = $"{result.Message}", Result = celesialSpacessResult, Exception = result.Exception });
            }

            return result;
        }

        private OASISResult<ICelestialBodiesAndSpaces<T1, T2>> HandleSaveCelestialBodiesAndSpaces<T1, T2, T3>(OASISResult<ICelestialBodiesAndSpaces<T1, T2>> result, OASISResult<IEnumerable<T3>> holonResult, string methodName) where T1 : ICelestialBody where T2 : ICelestialSpace, new() where T3 : IHolon
        {
            result = MapCelestialBodieAndSpacessResult(holonResult, result);
            OASISResult<IEnumerable<ICelestialBody>> celesialBodiesResult = CreateCelestialBodiesResult(result);
            OASISResult<IEnumerable<ICelestialSpace>> celesialSpacessResult = CreateCelestialSpacesResult(result);

            if (result != null && !result.IsError && result.Result != null)
            {
                _celestialBodies = Mapper.Convert<T1, ICelestialBody>(result.Result.CelestialBodies).ToList();
                _celestialSpaces = Mapper.Convert<T2, ICelestialSpace>(result.Result.CelestialSpaces).ToList();

                RegisterCelestialBodies(this.CelestialBodies);
                RegisterCelestialSpaces(this.CelestialSpaces);

                OnCelestialBodiesSaved?.Invoke(this, new CelestialBodiesSavedEventArgs() { Result = celesialBodiesResult });
                OnCelestialSpacesSaved?.Invoke(this, new CelestialSpacesSavedEventArgs() { Result = celesialSpacessResult });
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"An errror occured in CelestialSpace.{methodName} whilst saving the celestial bodies and spaces for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason: {holonResult.Message}");
                OnCelestialBodiesError?.Invoke(this, new CelestialBodiesErrorEventArgs() { Reason = $"{result.Message}", Result = celesialBodiesResult, Exception = result.Exception });
                OnCelestialSpacesError?.Invoke(this, new CelestialSpacesErrorEventArgs() { Reason = $"{result.Message}", Result = celesialSpacessResult, Exception = result.Exception });
            }

            return result;
        }

        private OASISResult<ICelestialBodiesAndSpaces> MapCelestialBodieAndSpacessResult<T>(OASISResult<IEnumerable<T>> holonResult, OASISResult<ICelestialBodiesAndSpaces> result) where T : IHolon
        {
            OASISResult<ICelestialBodiesAndSpaces> celesialBodiesAndSpacesResult = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<T>, ICelestialBodiesAndSpaces>(holonResult);
            celesialBodiesAndSpacesResult.Result.CelestialSpaces = GetCelestialSpaces(holonResult.Result);
            celesialBodiesAndSpacesResult.Result.CelestialBodies = GetCelestialBodies(holonResult.Result);
            return celesialBodiesAndSpacesResult;
        }

        private OASISResult<ICelestialBodiesAndSpaces<T1, T2>> MapCelestialBodieAndSpacessResult<T1, T2, T3>(OASISResult<IEnumerable<T3>> holonResult, OASISResult<ICelestialBodiesAndSpaces<T1, T2>> result) where T1 : ICelestialBody where T2 : ICelestialSpace where T3 : IHolon
        {
            OASISResult<ICelestialBodiesAndSpaces<T1, T2>> celesialBodiesAndSpacesResult = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<T3>, ICelestialBodiesAndSpaces<T1, T2>>(holonResult);
            celesialBodiesAndSpacesResult.Result.CelestialSpaces = Mapper.Convert<T2>(GetCelestialSpaces(holonResult.Result));
            celesialBodiesAndSpacesResult.Result.CelestialBodies = Mapper.Convert<T1>(GetCelestialSpaces(holonResult.Result));
            return celesialBodiesAndSpacesResult;
        }

        private OASISResult<IEnumerable<ICelestialBody>> CreateCelestialBodiesResult(OASISResult<ICelestialBodiesAndSpaces> result)
        {
            OASISResult<IEnumerable<ICelestialBody>> celesialBodiesResult = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<ICelestialBodiesAndSpaces, IEnumerable<ICelestialBody>>(result);
            celesialBodiesResult.Result = result.Result.CelestialBodies;
            return celesialBodiesResult;
        }

        private OASISResult<IEnumerable<ICelestialBody>> CreateCelestialBodiesResult<T1, T2>(OASISResult<ICelestialBodiesAndSpaces<T1, T2>> result) where T1 : ICelestialBody where T2 : ICelestialSpace
        {
            OASISResult<IEnumerable<ICelestialBody>> celesialBodiesResult = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<ICelestialBodiesAndSpaces<T1, T2>, IEnumerable<ICelestialBody>>(result);
            celesialBodiesResult.Result = Mapper.Convert<T1, ICelestialBody>(result.Result.CelestialBodies);
            return celesialBodiesResult;
        }

        private OASISResult<IEnumerable<ICelestialSpace>> CreateCelestialSpacesResult(OASISResult<ICelestialBodiesAndSpaces> result)
        {
            OASISResult<IEnumerable<ICelestialSpace>> celesialSpacesResult = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<ICelestialBodiesAndSpaces, IEnumerable<ICelestialSpace>>(result);
            celesialSpacesResult.Result = result.Result.CelestialSpaces;
            return celesialSpacesResult;
        }

        private OASISResult<IEnumerable<ICelestialSpace>> CreateCelestialSpacesResult<T1, T2>(OASISResult<ICelestialBodiesAndSpaces<T1, T2>> result) where T1 : ICelestialBody where T2 : ICelestialSpace, new()
        {
            OASISResult<IEnumerable<ICelestialSpace>> celesialBodiesResult = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<ICelestialBodiesAndSpaces<T1, T2>, IEnumerable<ICelestialSpace>>(result);
            celesialBodiesResult.Result = Mapper.Convert<T2, ICelestialSpace>(result.Result.CelestialSpaces);
            return celesialBodiesResult;
        }

        private IEnumerable<ICelestialBody> GetCelestialBodies<T>(IEnumerable<T> childHolons) where T : IHolon
        {
            List<ICelestialBody> celestialBodies = new List<ICelestialBody>();

            foreach (IHolon child in childHolons)
            {
                switch (child.HolonType)
                {
                    case HolonType.Comet:
                    case HolonType.Asteroid:
                    case HolonType.BlackHole:
                    case HolonType.Moon:
                    case HolonType.Planet:
                    case HolonType.Star:
                    case HolonType.SuperStar:
                    case HolonType.GrandSuperStar:
                    case HolonType.GreatGrandSuperStar:
                    case HolonType.Meteroid:
                        celestialBodies.Add((ICelestialBody)child);
                        //this.CelestialBodies.Add((ICelestialBody)child);
                        break;
                }
            }

            return celestialBodies;
        }

        private IEnumerable<ICelestialSpace> GetCelestialSpaces<T>(IEnumerable<T> childHolons) where T : IHolon
        {
            List<ICelestialSpace> celestialSpaces = new List<ICelestialSpace>();

            foreach (IHolon child in childHolons)
            {
                switch (child.HolonType)
                {
                    case HolonType.CosmicRay:
                    case HolonType.CosmicWave:
                    case HolonType.Dimension:
                    case HolonType.Galaxy:
                    case HolonType.GalaxyCluster:
                    case HolonType.GravitationalWave:
                    case HolonType.Multiverse:
                    case HolonType.Nebula:
                    case HolonType.Omniverse:
                    case HolonType.Portal:
                    case HolonType.SolarSystem:
                    case HolonType.SpaceTimeAbnormally:
                    case HolonType.SpaceTimeDistortion:
                    case HolonType.StarDust:
                    case HolonType.SuperVerse:
                    case HolonType.TemporalRift:
                    case HolonType.Universe:
                    case HolonType.WormHole:
                        celestialSpaces.Add((ICelestialSpace)child);
                        //this.CelestialSpaces.Add((ICelestialSpace)child);
                        break;
                }
            }

            return celestialSpaces;
        }

        private void SetCelestialHolonMetaData()
        {
            //CelestialHolon Properties
            MetaData["Age"] = this.Age;
            MetaData["Colour"] = this.Colour;
            MetaData["EclipticLatitute"] = this.EclipticLatitute;
            MetaData["EclipticLongitute"] = this.EclipticLongitute;
            MetaData["EquatorialLatitute"] = this.EquatorialLatitute;
            MetaData["EquatorialLongitute"] = this.EquatorialLongitute;
            MetaData["GalacticLatitute"] = this.GalacticLatitute;
            MetaData["GalacticLongitute"] = this.GalacticLongitute;
            MetaData["HorizontalLatitute"] = this.HorizontalLatitute;
            MetaData["HorizontalLongitute"] = this.HorizontalLongitute;
            MetaData["Radius"] = this.Radius;
            MetaData["Size"] = this.Size;
            MetaData["SpaceQuadrant"] = this.SpaceQuadrant;
            MetaData["SpaceSector"] = this.SpaceSector;
            MetaData["SuperGalacticLatitute"] = this.SuperGalacticLatitute;
            MetaData["SuperGalacticLongitute"] = this.SuperGalacticLongitute;
            MetaData["Temperature"] = this.Temperature;
        }

        private void CelestialSpace_OnCelestialSpaceLoaded(object sender, CelestialSpaceLoadedEventArgs e)
        {
            OnCelestialSpaceLoaded?.Invoke(sender, e);
        }

        private void CelestialSpace_OnCelestialSpaceSaved(object sender, CelestialSpaceSavedEventArgs e)
        {
            OnCelestialSpaceSaved?.Invoke(sender, e);
        }

        private void CelestialSpace_OnCelestialSpaceError(object sender, CelestialSpaceErrorEventArgs e)
        {
            OnCelestialSpaceError?.Invoke(sender, e);
        }

        private void CelestialSpace_OnCelestialSpacesLoaded(object sender, CelestialSpacesLoadedEventArgs e)
        {
            OnCelestialSpacesLoaded?.Invoke(sender, e);
        }

        private void CelestialSpace_OnCelestialSpacesSaved(object sender, CelestialSpacesSavedEventArgs e)
        {
            OnCelestialSpacesSaved?.Invoke(sender, e);
        }

        private void CelestialSpace_OnCelestialSpacesError(object sender, CelestialSpacesErrorEventArgs e)
        {
            OnCelestialSpacesError?.Invoke(sender, e);
        }

        private void CelestialSpace_OnCelestialBodyLoaded(object sender, CelestialBodyLoadedEventArgs e)
        {
            OnCelestialBodyLoaded?.Invoke(sender, e);
        }

        private void CelestialSpace_OnCelestialBodySaved(object sender, CelestialBodySavedEventArgs e)
        {
            OnCelestialBodySaved?.Invoke(sender, e);
        }

        private void CelestialSpace_OnCelestialBodyError(object sender, CelestialBodyErrorEventArgs e)
        {
            OnCelestialBodyError?.Invoke(sender, e);
        }

        private void CelestialSpace_OnCelestialBodiesLoaded(object sender, CelestialBodiesLoadedEventArgs e)
        {
            OnCelestialBodiesLoaded?.Invoke(sender, e);
        }

        private void CelestialSpace_OnCelestialBodiesSaved(object sender, CelestialBodiesSavedEventArgs e)
        {
            OnCelestialBodiesSaved?.Invoke(sender, e);
        }

        private void CelestialSpace_OnCelestialBodiesError(object sender, CelestialBodiesErrorEventArgs e)
        {
            OnCelestialBodiesError?.Invoke(sender, e);
        }

        private void CelestialSpace_OnZomeLoaded(object sender, ZomeLoadedEventArgs e)
        {
            OnZomeLoaded?.Invoke(sender, e);
        }

        private void CelestialSpace_OnZomeSaved(object sender, ZomeSavedEventArgs e)
        {
            OnZomeSaved?.Invoke(sender, e);
        }

        private void CelestialSpace_OnZomeError(object sender, ZomeErrorEventArgs e)
        {
            OnZomeError?.Invoke(sender, e);
        }

        private void CelestialSpace_OnZomesLoaded(object sender, ZomesLoadedEventArgs e)
        {
            OnZomesLoaded?.Invoke(sender, e);
        }

        private void CelestialSpace_OnZomesSaved(object sender, ZomesSavedEventArgs e)
        {
            OnZomesSaved?.Invoke(sender, e);
        }

        private void CelestialSpace_OnZomesError(object sender, ZomesErrorEventArgs e)
        {
            OnZomesError?.Invoke(sender, e);
        }

        private void CelestialSpace_OnHolonLoaded(object sender, HolonLoadedEventArgs e)
        {
            OnHolonLoaded?.Invoke(sender, e);
        }

        private void CelestialSpace_OnHolonSaved(object sender, HolonSavedEventArgs e)
        {
            OnHolonSaved?.Invoke(sender, e);
        }

        private void CelestialSpace_OnHolonError(object sender, HolonErrorEventArgs e)
        {
            OnHolonError?.Invoke(sender, e);
        }

        private void CelestialSpace_OnHolonsLoaded(object sender, HolonsLoadedEventArgs e)
        {
            OnHolonsLoaded?.Invoke(sender, e);
        }

        private void CelestialSpace_OnHolonsSaved(object sender, HolonsSavedEventArgs e)
        {
            OnHolonsSaved?.Invoke(sender, e);
        }

        private void CelestialSpace_OnHolonsError(object sender, HolonsErrorEventArgs e)
        {
            OnHolonsError?.Invoke(sender, e);
        }

        private void CelestialBody_OnCelestialBodyLoaded(object sender, CelestialBodyLoadedEventArgs e)
        {
            OnCelestialBodyLoaded?.Invoke(sender, e);
        }

        private void CelestialBody_OnCelestialBodySaved(object sender, CelestialBodySavedEventArgs e)
        {
            OnCelestialBodySaved?.Invoke(sender, e);
        }

        private void CelestialBody_OnCelestialBodyError(object sender, CelestialBodyErrorEventArgs e)
        {
            OnCelestialBodyError?.Invoke(sender, e);
        }

        private void CelestialBody_OnZomeLoaded(object sender, ZomeLoadedEventArgs e)
        {
            OnZomeLoaded?.Invoke(sender, e);
        }

        private void CelestialBody_OnZomeSaved(object sender, ZomeSavedEventArgs e)
        {
            OnZomeSaved?.Invoke(sender, e);
        }

        private void CelestialBody_OnZomeError(object sender, ZomeErrorEventArgs e)
        {
            OnZomeError?.Invoke(sender, e);
        }

        private void CelestialBody_OnZomesLoaded(object sender, ZomesLoadedEventArgs e)
        {
            OnZomesLoaded?.Invoke(sender, e);
        }

        private void CelestialBody_OnZomesSaved(object sender, ZomesSavedEventArgs e)
        {
            OnZomesSaved?.Invoke(sender, e);
        }

        private void CelestialBody_OnZomesError(object sender, ZomesErrorEventArgs e)
        {
            OnZomesError?.Invoke(sender, e);
        }

        private void CelestialBody_OnHolonLoaded(object sender, HolonLoadedEventArgs e)
        {
            OnHolonLoaded?.Invoke(sender, e);
        }

        private void CelestialBody_OnHolonSaved(object sender, HolonSavedEventArgs e)
        {
            OnHolonSaved?.Invoke(sender, e);
        }

        private void CelestialBody_OnHolonError(object sender, HolonErrorEventArgs e)
        {
            OnHolonError?.Invoke(sender, e);
        }

        private void CelestialBody_OnHolonsLoaded(object sender, HolonsLoadedEventArgs e)
        {
            OnHolonsLoaded?.Invoke(sender, e);
        }

        private void CelestialBody_OnHolonsSaved(object sender, HolonsSavedEventArgs e)
        {
            OnHolonsSaved?.Invoke(sender, e);
        }

        private void CelestialBody_OnHolonsError(object sender, HolonsErrorEventArgs e)
        {
            OnHolonsError?.Invoke(sender, e);
        }
    }
}
