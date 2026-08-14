using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.STAR.Zomes;
using NextGenSoftware.OASIS.STAR.Holons;
using NextGenSoftware.OASIS.STAR.CelestialSpace;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Events;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using static NextGenSoftware.OASIS.API.Core.Events.EventDelegates;
using System.Drawing;

namespace NextGenSoftware.OASIS.STAR.CelestialBodies
{
    public abstract partial class CelestialBody<T> where T : ICelestialBody, new()
    {
        // MAYBE THEY CAN ADD ITEMS MANUALLY TO THE COLLECTIONS WITHOUT USING THE CORRECT ADDMETHODS? THIS IS WHERE THIS COULD BE NEEDED?
        private void SetParentIdsForMoon(IGreatGrandSuperStar greatGrandSuperStar, IGrandSuperStar grandSuperStar, ISuperStar superStar, IStar star, IPlanet planet, IMoon moon)
        {
            if (moon.CelestialBodyCore.Zomes != null)
            {
                foreach (Zome zome in moon.CelestialBodyCore.Zomes)
                    ZomeHelper.SetParentIdsForZome(greatGrandSuperStar, grandSuperStar, superStar, star, planet, moon, zome);
            }
        }

        private void SetParentIdsForPlanet(IGreatGrandSuperStar greatGrandSuperStar, IGrandSuperStar grandSuperStar, ISuperStar superStar, IStar star, IPlanet planet)
        {
            if (greatGrandSuperStar != null)
            {
                planet.ParentOmniverse = greatGrandSuperStar.ParentOmniverse;
                planet.ParentOmniverseId = greatGrandSuperStar.ParentOmniverseId;
                planet.ParentGreatGrandSuperStar = greatGrandSuperStar;
                planet.ParentGreatGrandSuperStarId = greatGrandSuperStar.Id;
            }

            if (grandSuperStar != null)
            {
                planet.ParentMultiverse = grandSuperStar.ParentMultiverse;
                planet.ParentMultiverseId = grandSuperStar.ParentMultiverseId;
                planet.ParentGrandSuperStar = grandSuperStar;
                planet.ParentGrandSuperStarId = grandSuperStar.Id;
            }

            if (superStar != null)
            {
                planet.ParentDimension = superStar.ParentDimension;
                planet.ParentDimensionId = superStar.ParentDimensionId;
                planet.ParentUniverse = superStar.ParentUniverse;
                planet.ParentUniverseId = superStar.ParentUniverseId;
                planet.ParentGalaxyCluster = superStar.ParentGalaxyCluster;
                planet.ParentGalaxyClusterId = superStar.ParentGalaxyCluster.Id;
                planet.ParentGalaxy = superStar.ParentGalaxy;
                planet.ParentGalaxyId = superStar.ParentGalaxy.Id;
                planet.ParentSuperStar = superStar;
                planet.ParentSuperStarId = superStar.Id;
            }

            if (star != null)
            {
                planet.ParentSolarSystem = star.ParentSolarSystem;
                planet.ParentSolarSystemId = star.ParentSolarSystem.Id;
                planet.ParentStar = star;
                planet.ParentStarId = star.Id;
            }

            if (planet.CelestialBodyCore.Zomes != null)
            {
                foreach (IZome zome in planet.CelestialBodyCore.Zomes)
                    ZomeHelper.SetParentIdsForZome(greatGrandSuperStar, grandSuperStar, superStar, star, planet, null, zome);
            }

            if (planet.Moons != null)
            {
                foreach (IMoon moon in planet.Moons)
                    SetParentIdsForMoon(greatGrandSuperStar, grandSuperStar, superStar, star, planet, moon);
            }
        }

        private void SetParentIdsForStar(IGreatGrandSuperStar greatGrandSuperStar, IGrandSuperStar grandSuperStar, ISuperStar superStar, IStar star)
        {
            star.ParentOmniverse = greatGrandSuperStar.ParentOmniverse;
            star.ParentOmniverseId = greatGrandSuperStar.ParentOmniverseId;
            star.ParentGreatGrandSuperStar = greatGrandSuperStar;
            star.ParentGreatGrandSuperStarId = greatGrandSuperStar.Id;

            star.ParentUniverse = grandSuperStar.ParentUniverse;
            star.ParentUniverseId = grandSuperStar.ParentUniverseId;
            star.ParentGrandSuperStar = grandSuperStar;
            star.ParentGrandSuperStarId = grandSuperStar.Id;

            star.ParentGalaxy = grandSuperStar.ParentGalaxy;
            star.ParentGalaxyId = grandSuperStar.ParentGalaxy.Id;
            star.ParentSuperStar = superStar;
            star.ParentSuperStarId = superStar.Id;

            if (star.ParentSolarSystem.Planets != null)
            {
                foreach (IPlanet planet in star.ParentSolarSystem.Planets)
                {
                    SetParentIdsForPlanet(greatGrandSuperStar, grandSuperStar, superStar, star, planet);
                }
            }

            //TODO: Do we want to add Zomes to a Star? Maybe?
        }

        private void SetParentIdsForSuperStar(IGreatGrandSuperStar greatGrandSuperStar, IGrandSuperStar grandSuperStar, ISuperStar superStar)
        {
            foreach (IStar star in superStar.ParentGalaxy.Stars)
            {
                // Stars outside of a Solar System (does not have any planets)
                SetParentIdsForStar(greatGrandSuperStar, grandSuperStar, superStar, star);
            }

            foreach (ISolarSystem solarSystem in superStar.ParentGalaxy.SolarSystems)
            {
                solarSystem.ParentOmniverse = greatGrandSuperStar.ParentOmniverse;
                solarSystem.ParentOmniverseId = greatGrandSuperStar.ParentOmniverseId;
                solarSystem.ParentGreatGrandSuperStar = greatGrandSuperStar;
                solarSystem.ParentGreatGrandSuperStarId = greatGrandSuperStar.Id;

                solarSystem.ParentUniverse = grandSuperStar.ParentUniverse;
                solarSystem.ParentUniverseId = grandSuperStar.ParentUniverseId;
                solarSystem.ParentGrandSuperStar = grandSuperStar;
                solarSystem.ParentGrandSuperStarId = grandSuperStar.Id;

                solarSystem.ParentGalaxy = grandSuperStar.ParentGalaxy;
                solarSystem.ParentGalaxyId = grandSuperStar.ParentGalaxy.Id;
                solarSystem.ParentSuperStar = superStar;
                solarSystem.ParentSuperStarId = superStar.Id;

                SetParentIdsForStar(greatGrandSuperStar, grandSuperStar, superStar, solarSystem.Star);
            }
        }

        private void SetParentIdsForGrandSuperStar(IGreatGrandSuperStar greatGrandSuperStar, IGrandSuperStar grandSuperStar)
        {
            Mapper.MapParentCelestialBodyProperties(greatGrandSuperStar, grandSuperStar);
            grandSuperStar.ParentGreatGrandSuperStar = greatGrandSuperStar;
            grandSuperStar.ParentGreatGrandSuperStarId = greatGrandSuperStar.Id;

            SetParentIdsForMultiverseDimension(grandSuperStar.ParentMultiverse.Dimensions.FirstDimension, grandSuperStar);
            SetParentIdsForMultiverseDimension(grandSuperStar.ParentMultiverse.Dimensions.SecondDimension, grandSuperStar);
            SetParentIdsForMultiverseDimension(grandSuperStar.ParentMultiverse.Dimensions.ThirdDimension, grandSuperStar);
            SetParentIdsForMultiverseDimension(grandSuperStar.ParentMultiverse.Dimensions.FourthDimension, grandSuperStar);
            SetParentIdsForMultiverseDimension(grandSuperStar.ParentMultiverse.Dimensions.FifthDimension, grandSuperStar);
            SetParentIdsForMultiverseDimension(grandSuperStar.ParentMultiverse.Dimensions.SixthDimension, grandSuperStar);
            SetParentIdsForMultiverseDimension(grandSuperStar.ParentMultiverse.Dimensions.SeventhDimension, grandSuperStar);
        }

        private void SetParentIdsForMultiverseDimension(IMultiverseDimension dimension, IGrandSuperStar grandSuperStar)
        {
            Mapper.MapParentCelestialBodyProperties(grandSuperStar, dimension);
            Mapper.MapParentCelestialBodyProperties(dimension, dimension.Universe);
            dimension.Universe.ParentDimension = dimension;
            dimension.Universe.ParentDimensionId = dimension.Id;

            foreach (IStar star in dimension.Universe.Stars)
            {
                // Stars that are outside of a Galaxy (do not have a superstar).
                star.ParentUniverse = dimension.Universe;
                star.ParentUniverseId = dimension.Universe.Id;

                SetParentIdsForStar(null, grandSuperStar, null, star);
            }

            foreach (IPlanet planet in dimension.Universe.Planets)
            {
                // Planets that are outside of a Galaxy (do not have a superstar).
                planet.ParentUniverse = dimension.Universe;
                planet.ParentUniverseId = dimension.Universe.Id;

                SetParentIdsForPlanet(null, grandSuperStar, null, null, planet);
            }

            foreach (ISolarSystem solarSystem in dimension.Universe.SolarSystems)
            {
                // SolarSystems that are outside of a Galaxy (do not have a superstar).
                solarSystem.ParentUniverse = dimension.Universe;
                solarSystem.ParentUniverseId = dimension.Universe.Id;

                //TODO: Implement method below:
                //SetParentIdsForSolarSystems(greatGrandSuperStar, grandSuperStar, null, null, solarSystem);
            }

            foreach (INebula nebula in dimension.Universe.Nebulas)
            {
                // SolarSystems that are outside of a Galaxy (do not have a superstar).
                nebula.ParentUniverse = dimension.Universe;
                nebula.ParentUniverseId = dimension.Universe.Id;

                //TODO: Implement method below:
                //SetParentIdsForNebulas(greatGrandSuperStar, grandSuperStar, null, null, nebula);
            }

            //TODO: Add rest of CelestialBodies/Spaces in Universe here...

            foreach (IGalaxyCluster galaxyCluster in dimension.Universe.GalaxyClusters)
            {
                Mapper.MapParentCelestialBodyProperties(dimension.Universe, galaxyCluster);
                galaxyCluster.ParentUniverse = dimension.Universe;
                galaxyCluster.ParentUniverseId = dimension.Universe.Id;

                foreach (IGalaxy galaxy in galaxyCluster.Galaxies)
                {
                    Mapper.MapParentCelestialBodyProperties(galaxyCluster, galaxy);
                    galaxy.ParentGalaxyCluster = galaxyCluster;
                    galaxy.ParentGalaxyClusterId = galaxyCluster.Id;

                    //SetParentIdsForSuperStar(greatGrandSuperStar, grandSuperStar, galaxy.SuperStar);
                    SetParentIdsForSuperStar(null, grandSuperStar, galaxy.SuperStar);
                }
            }

            ThirdDimension thirdDimension = dimension as ThirdDimension;

            if (thirdDimension != null)
            {
                Mapper.MapParentCelestialBodyProperties(grandSuperStar, thirdDimension.MagicVerse);
                //Mapper.MapParentCelestialBodyProperties(grandSuperStar, thirdDimension.UniversePrime);

                foreach (IUniverse universe in thirdDimension.ParallelUniverses)
                    Mapper.MapParentCelestialBodyProperties(grandSuperStar, universe);
            }
        }

        private void SetParentIdsForGreatGrandSuperStar(IGreatGrandSuperStar greatGrandSuperStar)
        {
            foreach (IMultiverse multiverse in greatGrandSuperStar.ParentOmniverse.Multiverses)
            {
                Mapper.MapParentCelestialBodyProperties(greatGrandSuperStar, multiverse);
                multiverse.ParentGreatGrandSuperStar = greatGrandSuperStar;
                multiverse.ParentGreatGrandSuperStarId = greatGrandSuperStar.Id;

                foreach (IUniverse universe in multiverse.Dimensions.ThirdDimension.ParallelUniverses)
                {
                    universe.ParentOmniverse = greatGrandSuperStar.ParentOmniverse;
                    universe.ParentOmniverseId = greatGrandSuperStar.ParentOmniverseId;
                    universe.ParentGreatGrandSuperStar = greatGrandSuperStar;
                    universe.ParentGreatGrandSuperStarId = greatGrandSuperStar.Id;

                    //SetParentIdsForGrandSuperStar(greatGrandSuperStar, universe.GrandSuperStar);
                    SetParentIdsForGrandSuperStar(greatGrandSuperStar, universe.ParentGrandSuperStar);
                }
            }
        }

        //TODO: HOPE TO OBSOLETE THIS METHOD ASAP!
        //TODO: Check if we still need this? Hopefully can be replaced by the SetParentIds method called in PrepareHolonForSaving method in HolonManager...
        private void SetParentIds()
        {
            switch (this.HolonType)
            {
                case HolonType.GreatGrandSuperStar:
                    {
                        //TODO: Check if we still need this? Hopefully can be replaced by the SetParentIds method called in PrepareHolonForSaving method in HolonManager...
                        SetParentIdsForGreatGrandSuperStar((IGreatGrandSuperStar)this);

                        //If the parent Omniverse is not already saving (and it's children) then begin saving them now...

                        //OBSOLETE: ALL CHILDREN ARE NOW SAVED IN HOLONMANAGER VIA THE CELESTIALBODY.SAVE METHOD (SAVES ALL CHILDREN RECURSIVELY VIA THE ALLCHILDEN PROPERTY).
                        //TODO: NEED TO CHECK WHY WE ARE CALLING SAVE ON THE PARENT OMNIVERSE? DO THE CHILDREN BELONG TO THE OMNIVERSE OR THE GREAT GRAND SUPER STAR?


                        //if (saveChildren && !((IGreatGrandSuperStar)this).ParentOmniverse.IsSaving)
                        //{
                        //    OASISResult<ICelestialSpace> celestialSpaceResult = await ((IGreatGrandSuperStar)this).ParentOmniverse.SaveAsync(saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);

                        //    if (!(celestialSpaceResult != null && !celestialSpaceResult.IsError && celestialSpaceResult.Result != null))
                        //    {
                        //        OASISErrorHandling.HandleWarning(ref result, $"There was an error in CelestialBody.SaveAsync method whilst saving the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialBody")} (GreatGrandSuperStar) ParentOmniverse. Reason: {celestialSpaceResult.Message}");
                        //        OnCelestialBodyError?.Invoke(this, new CelestialBodyErrorEventArgs() { Result = result });

                        //        if (!continueOnError)
                        //        {
                        //            IsSaving = false;
                        //            return result;
                        //        }
                        //    }
                        //}
                    }
                    break;

                case HolonType.GrandSuperStar:
                    {
                        //TODO: Check if we still need this? Hopefully can be replaced by the SetParentIds method called in PrepareHolonForSaving method in HolonManager...
                        SetParentIdsForGrandSuperStar(this.ParentGreatGrandSuperStar, (IGrandSuperStar)this);

                        //if (saveChildren && !((IGrandSuperStar)this).ParentMultiverse.IsSaving)
                        //{
                        //    OASISResult<ICelestialSpace> celestialSpaceResult = await ((IGrandSuperStar)this).ParentMultiverse.SaveAsync(saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);

                        //    if (!(celestialSpaceResult != null && !celestialSpaceResult.IsError && celestialSpaceResult.Result != null))
                        //    {
                        //        OASISErrorHandling.HandleWarning(ref result, $"There was an error in CelestialBody.SaveAsync method whilst saving the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialBody")} (GrandSuperStar) ParentMultiverse. Reason: {celestialSpaceResult.Message}");
                        //        OnCelestialBodyError?.Invoke(this, new CelestialBodyErrorEventArgs() { Result = result });

                        //        if (!continueOnError)
                        //        {
                        //            IsSaving = false;
                        //            return result;
                        //        }
                        //    }
                        //}
                    }
                    break;

                case HolonType.SuperStar:
                    {
                        //TODO: Check if we still need this? Hopefully can be replaced by the SetParentIds method called in PrepareHolonForSaving method in HolonManager...
                        SetParentIdsForSuperStar(this.ParentGreatGrandSuperStar, this.ParentGrandSuperStar, (ISuperStar)this);

                        //if (saveChildren && !((ISuperStar)this).ParentGalaxy.IsSaving)
                        //{
                        //    OASISResult<ICelestialSpace> celestialSpaceResult = await ((ISuperStar)this).ParentGalaxy.SaveAsync(saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);

                        //    if (!(celestialSpaceResult != null && !celestialSpaceResult.IsError && celestialSpaceResult.Result != null))
                        //    {
                        //        OASISErrorHandling.HandleWarning(ref result, $"There was an error in CelestialBody.SaveAsync method whilst saving the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialBody")} (SuperStar) ParentGalaxy. Reason: {celestialSpaceResult.Message}");
                        //        OnCelestialBodyError?.Invoke(this, new CelestialBodyErrorEventArgs() { Result = result });

                        //        if (!continueOnError)
                        //        {
                        //            IsSaving = false;
                        //            return result;
                        //        }
                        //    }
                        //}
                    }
                    break;

                case HolonType.Star:
                    {
                        //TODO: Check if we still need this? Hopefully can be replaced by the SetParentIds method called in PrepareHolonForSaving method in HolonManager...
                        SetParentIdsForStar(this.ParentGreatGrandSuperStar, this.ParentGrandSuperStar, this.ParentSuperStar, (IStar)this);

                        //if (saveChildren && !((IStar)this).ParentSolarSystem.IsSaving)
                        //{
                        //    OASISResult<ICelestialSpace> celestialSpaceResult = await ((IStar)this).ParentSolarSystem.SaveAsync(saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);

                        //    if (!(celestialSpaceResult != null && !celestialSpaceResult.IsError && celestialSpaceResult.Result != null))
                        //    {
                        //        OASISErrorHandling.HandleWarning(ref result, $"There was an error in CelestialBody.SaveAsync method whilst saving the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialBody")} (Star) ParentSolarSystem. Reason: {celestialSpaceResult.Message}");
                        //        OnCelestialBodyError?.Invoke(this, new CelestialBodyErrorEventArgs() { Result = result });

                        //        if (!continueOnError)
                        //        {
                        //            IsSaving = false;
                        //            return result;
                        //        }
                        //    }
                        //}
                    }
                    break;

                case HolonType.Planet:
                    {
                        //TODO: Check if we still need this? Hopefully can be replaced by the SetParentIds method called in PrepareHolonForSaving method in HolonManager...
                        SetParentIdsForPlanet(this.ParentGreatGrandSuperStar, this.ParentGrandSuperStar, this.ParentSuperStar, this.ParentStar, (IPlanet)this);
                    }
                    break;
            }
        }

    }
}
