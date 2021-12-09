using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using NextGenSoftware.OASIS.API.Core.Events;
using static NextGenSoftware.OASIS.API.Core.Events.Events;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.STAR.Zomes;
using NextGenSoftware.OASIS.STAR.CelestialSpace;
using Mapster;
using NextGenSoftware.OASIS.STAR.Holons;

namespace NextGenSoftware.OASIS.STAR.CelestialBodies
{
    public abstract class CelestialBody : CelestialHolon, ICelestialBody
    {
        public ICelestialBodyCore CelestialBodyCore { get; set; } // This is the core zome of the planet (OAPP), which links to all the other planet zomes/holons...
                                                                  // public GenesisType GenesisType { get; set; }
                                                                  //public OASISAPIManager OASISAPI = new OASISAPIManager(new List<IOASISProvider>() { new SEEDSOASIS() });

        //public bool IsInitialized
        //{
        //    get
        //    {
        //        return CelestialBodyCore != null;
        //    }
        //}

        //public event Initialized OnInitialized;
        public event CelestialBodyLoaded OnCelestialBodyLoaded;
        public event CelestialBodySaved OnCelestialBodySaved;
        public event CelestialBodyError OnCelestialBodyError;
        public event HolonsLoaded OnHolonsLoaded;
        public event HolonSaved OnHolonSaved;
        public event HolonLoaded OnHolonLoaded;
        public event ZomesLoaded OnZomesLoaded;
        public event ZomeError OnZomeError;

        //public SpaceQuadrantType SpaceQuadrant { get; set; }
        //public int SpaceSector { get; set; }
        //public float SuperGalacticLatitute { get; set; }
        //public float SuperGalacticLongitute { get; set; }
        //public float GalacticLatitute { get; set; }
        //public float GalacticLongitute { get; set; }
        //public float HorizontalLatitute { get; set; }
        //public float HorizontalLongitute { get; set; }
        //public float EquatorialLatitute { get; set; }
        //public float EquatorialLongitute { get; set; }
        //public float EclipticLatitute { get; set; }
        //public float EclipticLongitute { get; set; }
        //public Color Colour { get; set; }
        //public int Size { get; set; }
        //public int Radius { get; set; }
        //public int Age { get; set; }
        public int Mass { get; set; }
        public int Density { get; set; }
        public int RotationPeriod { get; set; } //How long it takes to rotate on its axis.
        public int OrbitPeriod { get; set; } //How long it takes to orbit its ParentStar.
        //public int Temperature { get; set; }
        public int Weight { get; set; }
        public int GravitaionalPull { get; set; }
        public int OrbitPositionFromParentStar { get; set; }
        //public int OrbitPositionFromParentSuperStar { get; set; } //Only applies to SolarSystems. //TODO: Maybe better to make SolarSystem.ParentStar point to the SuperStar it orbits rather than the Star at the centre of it?
        public int CurrentOrbitAngleOfParentStar { get; set; } //Angle between 0 and 360 degrees of how far around the orbit it it of its parent star.
        public int DistanceFromParentStarInMetres { get; set; }
        public int RotationSpeed { get; set; }
        public int TiltAngle { get; set; }
        public int NumberRegisteredAvatars { get; set; }
        public int NunmerActiveAvatars { get; set; }

        public CelestialBody(HolonType holonType) : base(holonType)
        {
            Initialize();
        }

        public CelestialBody(Guid id, HolonType holonType) : base(id, holonType)
        {
            Initialize();
        }

        public CelestialBody(Dictionary<ProviderType, string> providerKey, HolonType holonType) : base(providerKey, holonType)
        {
            Initialize();
        }
        
        public async Task<OASISResult<ICelestialBody>> SaveAsync(bool saveChildren = true, bool continueOnError = true)
        {
            OASISResult<ICelestialBody> result = new OASISResult<ICelestialBody>(this);
            OASISResult<IHolon> celestialBodyHolonResult = new OASISResult<IHolon>();
            OASISResult<IZome> celestialBodyChildrenResult = null;
            OASISResult<IEnumerable<IHolon>> holonsResult = null;
            OASISResult<IHolon> holonResult = null;

            if (this.Children == null)
                this.Children = new ObservableCollection<IHolon>();

            // Only save if the holon has any changes.
            if (!HasHolonChanged())
            {
                result.Message = "No changes need saving";
                return result;
            }

            //TODO: Don't think we need to save here to get the Id (saves having to save it twice!), we can generate it here instead and set the IsNewHolon property so the providers know it is a new holon (they use to check if the Id had been set).
            if (this.Id == Guid.Empty)
            {
                Id = Guid.NewGuid();
                IsNewHolon = true;
            }

            // TODO: Not sure if ParentStar and ParentPlanet will be set?
            switch (this.HolonType)
            {
                case HolonType.GreatGrandSuperStar:
                    {
                        SetParentIdsForGreatGrandSuperStar((IGreatGrandSuperStar)this);
                        
                        //TODO: NEED TO ADD SAVE METHODS TO EVERY CELESTIALSPACEHOLON, WHICH WILL RECURSIVELY SAVE ALL IT'S CHILDREN, SAME AS CELESTIALBODY
                        //THIS WILL REPLACE ALL THE MANUAL CODE BELOW! ;-) DOH! LOL
                        if (saveChildren)
                        {
                            holonsResult = await CelestialBodyCore.SaveHolonsAsync(Mapper<IMultiverse, Holon>.MapBaseHolonProperties(((IGreatGrandSuperStar)this).ParentOmiverse.Multiverses));

                            if (!continueOnError && (holonsResult.IsError || holonsResult.Result == null))
                            {
                                result.IsError = true;
                                result.Message = string.Concat("Error occured saving GreatGrandSuperStar.ParentOmiverse.Multiverses. Error Details: ", holonsResult.Message);
                                return result;
                            }

                            //TODO: Finish this later...
                            foreach (IMultiverse multiverse in ParentOmiverse.Multiverses)
                            {
                                holonResult = await CelestialBodyCore.SaveHolonAsync(Mapper<IUniverse, Holon>.MapBaseHolonProperties(multiverse.Dimensions.ThirdDimension.UniversePrime));

                                if (!continueOnError && (holonResult.IsError || holonResult.Result == null))
                                {
                                    result.IsError = true;
                                    result.Message = string.Concat("Error occured saving GreatGrandSuperStar.ParentOmiverse.Multiverses[].Dimensions.ThirdDimension.UniversePrime. Error Details: ", holonResult.Message);
                                    return result;
                                }

                                holonResult = await CelestialBodyCore.SaveHolonAsync(Mapper<IUniverse, Holon>.MapBaseHolonProperties(multiverse.Dimensions.ThirdDimension.MagicVerse));

                                if (!continueOnError && (holonResult.IsError || holonResult.Result == null))
                                {
                                    result.IsError = true;
                                    result.Message = string.Concat("Error occured saving GreatGrandSuperStar.ParentOmiverse.Multiverses[].Dimensions.ThirdDimension.MagicVerse. Error Details: ", holonResult.Message);
                                    return result;
                                }

                                holonsResult = await CelestialBodyCore.SaveHolonsAsync(Mapper<IMultiverse, Holon>.MapBaseHolonProperties(((IGreatGrandSuperStar)this).ParentOmiverse.Multiverses));

                                if (!continueOnError && (holonsResult.IsError || holonsResult.Result == null))
                                {
                                    result.IsError = true;
                                    result.Message = string.Concat("Error occured saving GreatGrandSuperStar.ParentOmiverse.Multiverses[].Dimensions.ThirdDimension.ParallelUniverses. Error Details: ", holonsResult.Message);
                                    return result;
                                }

                                celestialBodyChildrenResult = await SaveCelestialBodyChildrenAsync((IEnumerable<IZome>)multiverse.Dimensions.ThirdDimension.ParallelUniverses);

                                //Need to save all other dimensions too... ;-)
                            }
                        }
                    }
                    break;

                case HolonType.GrandSuperStar:
                    {
                        SetParentIdsForGrandSuperStar(this.ParentGreatGrandSuperStar, (IGrandSuperStar)this);

                        if (saveChildren)
                        {
                            holonsResult = await CelestialBodyCore.SaveHolonsAsync(Mapper<IGalaxyCluster, Holon>.MapBaseHolonProperties(((IGrandSuperStar)this).ParentUniverse.GalaxyClusters));

                            if (!continueOnError && (holonsResult.IsError || holonsResult.Result == null))
                            {
                                result.IsError = true;
                                result.Message = string.Concat("Error occured saving GrandSuperStar.ParentUniverse.GalaxyClusters. Error Details: ", holonsResult.Message);
                                return result;
                            }

                            foreach (IGalaxyCluster galaxyCluster in ((IGrandSuperStar)this).ParentUniverse.GalaxyClusters)
                            {
                                holonsResult = await CelestialBodyCore.SaveHolonsAsync(Mapper<IGalaxy, Holon>.MapBaseHolonProperties(galaxyCluster.Galaxies));

                                if (!continueOnError && (holonsResult.IsError || holonsResult.Result == null))
                                {
                                    result.IsError = true;
                                    result.Message = string.Concat("Error occured saving GrandSuperStar.ParentUniverse.GalaxyClusters[].Galaxies. Error Details: ", holonsResult.Message);
                                    return result;
                                }

                                //TODO: Finish this later...
                            }
                        }
                    }
                    break;

                case HolonType.SuperStar:
                    {
                        SetParentIdsForSuperStar(this.ParentGreatGrandSuperStar, this.ParentGrandSuperStar, (ISuperStar)this);

                        //TODO: Finish this later...
                        // if (saveChildren)
                        //   celestialBodyChildrenResult = await SaveCelestialBodyChildrenAsync((IEnumerable<IZome>)((ISuperStar)this).ParentGalaxy.Stars);
                    }
                    break;

                case HolonType.Star:
                    {
                        SetParentIdsForStar(this.ParentGreatGrandSuperStar, this.ParentGrandSuperStar, this.ParentSuperStar, (IStar)this);

                        //TODO: Finish this later...
                        //  if (saveChildren)
                        //     celestialBodyChildrenResult = await SaveCelestialBodyChildrenAsync((IEnumerable<IZome>)((IStar)this).ParentSolarSystem.Planets);
                    }
                    break;

                case HolonType.Planet:
                    {
                        SetParentIdsForPlanet(this.ParentGreatGrandSuperStar, this.ParentGrandSuperStar, this.ParentSuperStar, this.ParentStar, (IPlanet)this);

                        //TODO: Finish this later...
                        // if (saveChildren)
                        //    celestialBodyChildrenResult = await SaveCelestialBodyChildrenAsync((IEnumerable<IZome>)((IPlanet)this).Moons);
                    }
                    break;
            }

            if (saveChildren)
                celestialBodyChildrenResult = await SaveCelestialBodyChildrenAsync(CelestialBodyCore.Zomes);


            // Finally we need to save again so the child holon ids's are stored in the graph...
            // TODO: We may not need to do this save again in future since when we load the zome we could lazy load its child holons seperatley from their parentIds.
            // But loading the celestialbody with all its child holons will be faster than loading them seperatley (but only if the current OASIS Provider supports this, so far MongoDBOASIS does).

            celestialBodyHolonResult = await CelestialBodyCore.SaveCelestialBodyAsync(this);
            result.Message = celestialBodyHolonResult.Message;
            result.IsSaved = celestialBodyHolonResult.IsSaved;
            result.IsError = celestialBodyHolonResult.IsError;

            if (!celestialBodyHolonResult.IsError && celestialBodyHolonResult.Result != null)
                //celestialBodyHolonResult.Result.Adapt(this);
                //Mapper<IHolon, CelestialBody>.MapBaseHolonProperties(celestialBodyHolonResult.Result, this);
                SetProperties(celestialBodyHolonResult.Result);

            return result;
        }

        public async Task<OASISResult<ICelestialBody>> SaveAsync<T>(bool saveChildren = true, bool continueOnError = true) where T : ICelestialBody, new()
        {
            OASISResult<ICelestialBody> result = new OASISResult<ICelestialBody>(this);
            OASISResult<T> celestialBodyHolonResult = new OASISResult<T>();
            OASISResult<IZome> celestialBodyChildrenResult = null;
            OASISResult<IEnumerable<IHolon>> holonsResult = null;
            OASISResult<IHolon> holonResult = null;

            if (this.Children == null)
                this.Children = new ObservableCollection<IHolon>();

            // Only save if the holon has any changes.
            if (!HasHolonChanged())
            {
                result.Message = "No changes need saving";
                return result;
            }

            //TODO: Don't think we need to save here to get the Id (saves having to save it twice!), we can generate it here instead and set the IsNewHolon property so the providers know it is a new holon (they use to check if the Id had been set).
            if (this.Id == Guid.Empty)
            {
                Id = Guid.NewGuid();
                IsNewHolon = true;
            }

            // TODO: Not sure if ParentStar and ParentPlanet will be set?
            switch (this.HolonType)
            {
                case HolonType.GreatGrandSuperStar:
                    {
                        SetParentIdsForGreatGrandSuperStar((IGreatGrandSuperStar)this);

                        //TODO: NEED TO ADD SAVE METHODS TO EVERY CELESTIALSPACEHOLON, WHICH WILL RECURSIVELY SAVE ALL IT'S CHILDREN, SAME AS CELESTIALBODY
                        //THIS WILL REPLACE ALL THE MANUAL CODE BELOW! ;-) DOH! LOL
                        if (saveChildren)
                        {
                            holonsResult = await CelestialBodyCore.SaveHolonsAsync(Mapper<IMultiverse, Holon>.MapBaseHolonProperties(((IGreatGrandSuperStar)this).ParentOmiverse.Multiverses));

                            if (!continueOnError && (holonsResult.IsError || holonsResult.Result == null))
                            {
                                result.IsError = true;
                                result.Message = string.Concat("Error occured saving GreatGrandSuperStar.ParentOmiverse.Multiverses. Error Details: ", holonsResult.Message);
                                return result;
                            }

                            //TODO: Finish this later...
                            foreach (IMultiverse multiverse in ParentOmiverse.Multiverses)
                            {
                                OASISResult<Universe> universeResult = await CelestialBodyCore.SaveHolonAsync<Universe>(Mapper<IUniverse, Holon>.MapBaseHolonProperties(multiverse.Dimensions.ThirdDimension.UniversePrime));

                                if (!continueOnError && (universeResult.IsError || universeResult.Result == null))
                                {
                                    result.IsError = true;
                                    result.Message = string.Concat("Error occured saving GreatGrandSuperStar.ParentOmiverse.Multiverses[].Dimensions.ThirdDimension.UniversePrime. Error Details: ", universeResult.Message);
                                    return result;
                                }

                                OASISResult<Universe> magicverseResult = await CelestialBodyCore.SaveHolonAsync<Universe>(Mapper<IUniverse, Holon>.MapBaseHolonProperties(multiverse.Dimensions.ThirdDimension.MagicVerse));

                                if (!continueOnError && (magicverseResult.IsError || magicverseResult.Result == null))
                                {
                                    result.IsError = true;
                                    result.Message = string.Concat("Error occured saving GreatGrandSuperStar.ParentOmiverse.Multiverses[].Dimensions.ThirdDimension.MagicVerse. Error Details: ", magicverseResult.Message);
                                    return result;
                                }

                                holonsResult = await CelestialBodyCore.SaveHolonsAsync(Mapper<IMultiverse, Holon>.MapBaseHolonProperties(((IGreatGrandSuperStar)this).ParentOmiverse.Multiverses));

                                if (!continueOnError && (holonsResult.IsError || holonsResult.Result == null))
                                {
                                    result.IsError = true;
                                    result.Message = string.Concat("Error occured saving GreatGrandSuperStar.ParentOmiverse.Multiverses[].Dimensions.ThirdDimension.ParallelUniverses. Error Details: ", holonsResult.Message);
                                    return result;
                                }

                                celestialBodyChildrenResult = await SaveCelestialBodyChildrenAsync((IEnumerable<IZome>)multiverse.Dimensions.ThirdDimension.ParallelUniverses);

                                //Need to save all other dimensions too... ;-)
                            }
                        }
                    }
                    break;

                case HolonType.GrandSuperStar:
                    {
                        SetParentIdsForGrandSuperStar(this.ParentGreatGrandSuperStar, (IGrandSuperStar)this);

                        if (saveChildren)
                        {
                            holonsResult = await CelestialBodyCore.SaveHolonsAsync(Mapper<IGalaxyCluster, Holon>.MapBaseHolonProperties(((IGrandSuperStar)this).ParentUniverse.GalaxyClusters));

                            if (!continueOnError && (holonsResult.IsError || holonsResult.Result == null))
                            {
                                result.IsError = true;
                                result.Message = string.Concat("Error occured saving GrandSuperStar.ParentUniverse.GalaxyClusters. Error Details: ", holonsResult.Message);
                                return result;
                            }

                            foreach (IGalaxyCluster galaxyCluster in ((IGrandSuperStar)this).ParentUniverse.GalaxyClusters)
                            {
                                holonsResult = await CelestialBodyCore.SaveHolonsAsync(Mapper<IGalaxy, Holon>.MapBaseHolonProperties(galaxyCluster.Galaxies));

                                if (!continueOnError && (holonsResult.IsError || holonsResult.Result == null))
                                {
                                    result.IsError = true;
                                    result.Message = string.Concat("Error occured saving GrandSuperStar.ParentUniverse.GalaxyClusters[].Galaxies. Error Details: ", holonsResult.Message);
                                    return result;
                                }

                                //TODO: Finish this later...
                            }
                        }
                    }
                    break;

                case HolonType.SuperStar:
                    {
                        SetParentIdsForSuperStar(this.ParentGreatGrandSuperStar, this.ParentGrandSuperStar, (ISuperStar)this);

                        //TODO: Finish this later...
                        // if (saveChildren)
                        //   celestialBodyChildrenResult = await SaveCelestialBodyChildrenAsync((IEnumerable<IZome>)((ISuperStar)this).ParentGalaxy.Stars);
                    }
                    break;

                case HolonType.Star:
                    {
                        SetParentIdsForStar(this.ParentGreatGrandSuperStar, this.ParentGrandSuperStar, this.ParentSuperStar, (IStar)this);

                        //TODO: Finish this later...
                        //  if (saveChildren)
                        //     celestialBodyChildrenResult = await SaveCelestialBodyChildrenAsync((IEnumerable<IZome>)((IStar)this).ParentSolarSystem.Planets);
                    }
                    break;

                case HolonType.Planet:
                    {
                        SetParentIdsForPlanet(this.ParentGreatGrandSuperStar, this.ParentGrandSuperStar, this.ParentSuperStar, this.ParentStar, (IPlanet)this);

                        //TODO: Finish this later...
                        // if (saveChildren)
                        //    celestialBodyChildrenResult = await SaveCelestialBodyChildrenAsync((IEnumerable<IZome>)((IPlanet)this).Moons);
                    }
                    break;
            }

            if (saveChildren)
                celestialBodyChildrenResult = await SaveCelestialBodyChildrenAsync(CelestialBodyCore.Zomes);


            // Finally we need to save again so the child holon ids's are stored in the graph...
            // TODO: We may not need to do this save again in future since when we load the zome we could lazy load its child holons seperatley from their parentIds.
            // But loading the celestialbody with all its child holons will be faster than loading them seperatley (but only if the current OASIS Provider supports this, so far MongoDBOASIS does).

            celestialBodyHolonResult = await CelestialBodyCore.SaveCelestialBodyAsync<T>(this);
            result.Message = celestialBodyHolonResult.Message;
            result.IsSaved = celestialBodyHolonResult.IsSaved;
            result.IsError = celestialBodyHolonResult.IsError;

            if (!celestialBodyHolonResult.IsError && celestialBodyHolonResult.Result != null)
                //celestialBodyHolonResult.Result.Adapt(this);
                //Mapper<IHolon, CelestialBody>.MapBaseHolonProperties(celestialBodyHolonResult.Result, this);
                SetProperties(celestialBodyHolonResult.Result);

            return result;
        }

        public OASISResult<ICelestialBody> Save(bool saveChildren = true, bool continueOnError = true)
        {
            return SaveAsync(saveChildren, continueOnError).Result; //TODO: Best way of doing this?
        }

        public OASISResult<ICelestialBody> Save<T>(bool saveChildren = true, bool continueOnError = true) where T : ICelestialBody, new()
        {
            return SaveAsync<T>(saveChildren, continueOnError).Result; //TODO: Best way of doing this?
        }

        //TODO: Do we need to use ICelestialBody or IZome here? It will call different Saves depending which we use...
        private async Task<OASISResult<IZome>> SaveCelestialBodyChildrenAsync(IEnumerable<IZome> zomes)
        {
            OASISResult<IZome> result = new OASISResult<IZome>();

            if (zomes != null)
            {
                foreach (IZome zome in zomes)
                {
                    if (zome.HasHolonChanged())
                    {
                        result = await zome.SaveAsync();

                        if (result.IsError)
                            break;
                    }
                }
            }

            //TODO: Improve result/error handling
            return result;
        }

        private void SetProperties(IHolon holon)
        {
            this.Id = holon.Id;
            this.ProviderKey = holon.ProviderKey;
            this.CelestialBodyCore.Id = holon.Id;
            this.CelestialBodyCore.ProviderKey = holon.ProviderKey;
            this.Name = holon.Name;
            this.Description = holon.Description;
            this.HolonType = holon.HolonType;
            this.ParentGreatGrandSuperStar = holon.ParentGreatGrandSuperStar;
            this.ParentGreatGrandSuperStarId = holon.ParentGreatGrandSuperStarId;
            this.ParentGrandSuperStar = holon.ParentGrandSuperStar;
            this.ParentGrandSuperStarId = holon.ParentGrandSuperStarId;
            this.ParentSuperStar = holon.ParentSuperStar;
            this.ParentSuperStarId = holon.ParentSuperStarId;
            this.ParentStar = holon.ParentStar;
            this.ParentStarId = holon.ParentStarId;
            this.ParentPlanet = holon.ParentPlanet;
            this.ParentPlanetId = holon.ParentPlanetId;
            this.ParentMoon = holon.ParentMoon;
            this.ParentMoonId = holon.ParentMoonId;
            this.ParentZome = holon.ParentZome;
            this.ParentZomeId = holon.ParentZomeId;
            this.ParentHolon = holon.ParentHolon;
            this.ParentHolonId = holon.ParentHolonId;
            this.ParentOmiverse = holon.ParentOmiverse;
            this.ParentOmiverseId = holon.ParentOmiverseId;
            this.ParentUniverse = holon.ParentUniverse;
            this.ParentUniverseId = holon.ParentUniverseId;
            this.ParentGalaxy = holon.ParentGalaxy;
            this.ParentGalaxyId = holon.ParentGalaxyId;
            this.ParentSolarSystem = holon.ParentSolarSystem;
            this.ParentSolarSystemId = holon.ParentSolarSystemId;
            this.Children = holon.Children;
            this.Nodes = holon.Nodes;
            this.CreatedByAvatar = holon.CreatedByAvatar;
            this.CreatedByAvatarId = holon.CreatedByAvatarId;
            this.CreatedDate = holon.CreatedDate;
            this.ModifiedByAvatar = holon.ModifiedByAvatar;
            this.ModifiedByAvatarId = holon.ModifiedByAvatarId;
            this.ModifiedDate = holon.ModifiedDate;
            this.DeletedByAvatar = holon.DeletedByAvatar;
            this.DeletedByAvatarId = holon.DeletedByAvatarId;
            this.DeletedDate = holon.DeletedDate;
            this.Version = holon.Version;
            this.IsActive = holon.IsActive;
            this.IsChanged = holon.IsChanged;
            this.IsNewHolon = holon.IsNewHolon;
            this.MetaData = holon.MetaData;
            this.ProviderMetaData = holon.ProviderMetaData;
            this.Original = holon.Original;
        }


        // TODO: COME BACK TO SETTING THESE PARENTID'S, NEED TO NOT BE TIRED TO WORK IT ALL OUT! ;-) LOL
        // PLUS NOT EVEN SURE WE NEED TO DO THIS BECAUSE ALL THE ADD METHODS ALREADY SET THE PARENTID'S?!
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
                planet.ParentOmiverse = greatGrandSuperStar.ParentOmiverse;
                planet.ParentOmiverseId = greatGrandSuperStar.ParentOmiverseId;
                planet.ParentGreatGrandSuperStar = greatGrandSuperStar;
                planet.ParentGreatGrandSuperStarId = greatGrandSuperStar.Id;
            }

            if (grandSuperStar != null)
            {
                planet.ParentUniverse = grandSuperStar.ParentUniverse;
                planet.ParentUniverseId = grandSuperStar.ParentUniverseId;
                planet.ParentGrandSuperStar = grandSuperStar;
                planet.ParentGrandSuperStarId = grandSuperStar.Id;
                planet.ParentGalaxy = grandSuperStar.ParentGalaxy;
                planet.ParentGalaxyId = grandSuperStar.ParentGalaxy.Id;
            }

            if (superStar != null)
            {
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
            star.ParentOmiverse = greatGrandSuperStar.ParentOmiverse;
            star.ParentOmiverseId = greatGrandSuperStar.ParentOmiverseId;
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
                solarSystem.ParentOmiverse = greatGrandSuperStar.ParentOmiverse;
                solarSystem.ParentOmiverseId = greatGrandSuperStar.ParentOmiverseId;
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
            grandSuperStar.ParentOmiverse = greatGrandSuperStar.ParentOmiverse;
            grandSuperStar.ParentOmiverseId = greatGrandSuperStar.ParentOmiverseId;
            grandSuperStar.ParentGreatGrandSuperStar = greatGrandSuperStar;
            grandSuperStar.ParentGreatGrandSuperStarId = greatGrandSuperStar.Id;

            grandSuperStar.ParentUniverse = greatGrandSuperStar.ParentUniverse;
            grandSuperStar.ParentUniverseId = grandSuperStar.ParentUniverseId;
            grandSuperStar.ParentGrandSuperStar = grandSuperStar;
            grandSuperStar.ParentGrandSuperStarId = grandSuperStar.Id;

            foreach (IStar star in grandSuperStar.ParentUniverse.Stars)
            {
                // Stars that are outside of a Galaxy (do not have a superstar).
                star.ParentOmiverse = greatGrandSuperStar.ParentOmiverse;
                star.ParentOmiverseId = greatGrandSuperStar.ParentOmiverseId;
                star.ParentGreatGrandSuperStar = greatGrandSuperStar;
                star.ParentGreatGrandSuperStarId = greatGrandSuperStar.Id;

                star.ParentUniverse = grandSuperStar.ParentUniverse;
                star.ParentUniverseId = grandSuperStar.ParentUniverseId;
                star.ParentGrandSuperStar = grandSuperStar;
                star.ParentGrandSuperStarId = grandSuperStar.Id;

                SetParentIdsForStar(greatGrandSuperStar, grandSuperStar, null, star); //Stars outside of a Galaxy do not have a parent SuperStar (at the centre of each Galaxy).
            }

            //foreach (IGalaxy galaxy in grandSuperStar.ParentUniverse.Galaxies)
            foreach (IGalaxyCluster galaxyCluster in grandSuperStar.ParentUniverse.GalaxyClusters)
            {
                //TODO: Come back to this... finish this bit later..
                galaxyCluster.ParentOmiverse = greatGrandSuperStar.ParentOmiverse;
                galaxyCluster.ParentOmiverseId = greatGrandSuperStar.ParentOmiverseId;
                galaxyCluster.ParentGreatGrandSuperStar = greatGrandSuperStar;
                galaxyCluster.ParentGreatGrandSuperStarId = greatGrandSuperStar.Id;

                galaxyCluster.ParentUniverse = grandSuperStar.ParentUniverse;
                galaxyCluster.ParentUniverseId = grandSuperStar.ParentUniverseId;
                galaxyCluster.ParentGrandSuperStar = grandSuperStar;
                galaxyCluster.ParentGrandSuperStarId = grandSuperStar.Id;

                foreach (IGalaxy galaxy in galaxyCluster.Galaxies)
                {
                    galaxy.ParentOmiverse = greatGrandSuperStar.ParentOmiverse;
                    galaxy.ParentOmiverseId = greatGrandSuperStar.ParentOmiverseId;
                    galaxy.ParentGreatGrandSuperStar = greatGrandSuperStar;
                    galaxy.ParentGreatGrandSuperStarId = greatGrandSuperStar.Id;

                    galaxy.ParentUniverse = grandSuperStar.ParentUniverse;
                    galaxy.ParentUniverseId = grandSuperStar.ParentUniverseId;
                    galaxy.ParentGrandSuperStar = grandSuperStar;
                    galaxy.ParentGrandSuperStarId = grandSuperStar.Id;

                    SetParentIdsForSuperStar(greatGrandSuperStar, grandSuperStar, galaxy.SuperStar);
                }
            }
        }

        private void SetParentIdsForGreatGrandSuperStar(IGreatGrandSuperStar greatGrandSuperStar)
        {
            foreach (IMultiverse multiverse in greatGrandSuperStar.ParentOmiverse.Multiverses)
            {
                multiverse.ParentOmiverse = greatGrandSuperStar.ParentOmiverse;
                multiverse.ParentOmiverseId = greatGrandSuperStar.ParentOmiverseId;
                multiverse.ParentGreatGrandSuperStar = greatGrandSuperStar;
                multiverse.ParentGreatGrandSuperStarId = greatGrandSuperStar.Id;

                //TODO: Come back to this tomorrow...
                //Mapper<IGreatGrandSuperStar, Multiverse>.MapParentCelestialBodyProperties(greatGrandSuperStar, (Multiverse)multiverse);

                foreach (IUniverse universe in multiverse.Dimensions.ThirdDimension.ParallelUniverses)
                {
                    universe.ParentOmiverse = greatGrandSuperStar.ParentOmiverse;
                    universe.ParentOmiverseId = greatGrandSuperStar.ParentOmiverseId;
                    universe.ParentGreatGrandSuperStar = greatGrandSuperStar;
                    universe.ParentGreatGrandSuperStarId = greatGrandSuperStar.Id;

                    //SetParentIdsForGrandSuperStar(greatGrandSuperStar, universe.GrandSuperStar);
                    SetParentIdsForGrandSuperStar(greatGrandSuperStar, universe.ParentGrandSuperStar);
                }
            }
        }



        /*
        private async Task<bool> SaveZomesAndHolons()
        {
            foreach (ZomeBase zome in Zomes)
            {
                //TODO: Need to check if any state has changed and only save if it has...
                //await zome.SaveHolonAsync(zome);
                await zome.SaveHolonAsync(this.RustHolonType, zome); //TODO: FIX ASAP!

                foreach (Holon holon in zome.Holons)
                {
                    //TODO: Need to check if any state has changed and only save if it has...
                    await zome.SaveHolonAsync(this.RustHolonType, holon);
                }
            }

            return true;
        }
        */

        // Build
        public CoronalEjection Flare()
        {
            return new CoronalEjection();
           // return Star.Flare(this);
        }

        // Activate & Launch - Launch & activate the planet (OAPP) by shining the star's light upon it...
        public void Shine()
        {
           // Star.Shine(this);
        }

        // Deactivate the planet (OAPP)
        public void Dim()
        {
            //Star.Dim(this);
        }

        // Deploy the planet (OAPP)
        public void Seed()
        {
            //Star.Seed(this);
        }

        // Run Tests
        public void Twinkle()
        {
            //Star.Twinkle(this);
        }

        // Highlight the Planet (OAPP) in the OAPP Store (StarNET). *Admin Only*
        public void Radiate()
        {
            //Star.Radiate(this);
        }

        // Show how much light the planet (OAPP) is emitting into the solar system (StarNET/HoloNET)
        public void Emit()
        {
           // Star.Emit(this);
        }

        // Show stats of the Planet (OAPP).
        public void Reflect()
        {
           // Star.Reflect(this);
        }

        // Upgrade/update a Planet (OAPP).
        public void Evolve()
        {
            //Star.Evolve(this);
        }

        // Import/Export hApp, dApp & others.
        public void Mutate()
        {
           // Star.Mutate(this);
        }

        // Send/Receive Love
        public void Love()
        {
           // Star.Love(this);
        }

        // Reserved For Future Use...
        public void Super()
        {
            //Star.Super(this);
        }

        private void CelestialBodyCore_OnZomeError(object sender, ZomeErrorEventArgs e)
        {
            OnZomeError?.Invoke(sender, e);
        }

        private async void CelestialBodyCore_OnZomesLoaded(object sender, ZomesLoadedEventArgs e)
        {
            OnZomesLoaded?.Invoke(sender, e);

            // TODO: Dont think this is needed now?
            // This was going to load each of the zomes holons once the zomes were loaded for this Planet. 
            // But maybe it is better to allow them be lazy loaded as and when they are needed rather than pulling them all back in one go?
            // Trade offs between the 2 approaches... for now we leave as lazy loading so will only load when they are needed...

            /*
            foreach (ZomeBase zome in CelestialBodyCore.Zomes)
            {
                await zome.Initialize(zome.Name, this.HoloNETClient);
                zome.OnHolonLoaded += Zome_OnHolonLoaded;
                zome.OnHolonSaved += Zome_OnHolonSaved;
            }*/

            //TODO: Not sure whether to delegate holons being loaded by zomes if can just load direct from PlanetCore?
            //Nice for Zomes to manage their own collections of holons (good practice) so will see... :)
        }

        private void CelestialBodyCore_OnHolonsLoaded(object sender, HolonsLoadedEventArgs e)
        {
            OnHolonsLoaded?.Invoke(sender, e);
        }

        public async Task<OASISResult<IEnumerable<IZome>>> LoadZomesAsync()
        {
            return await CelestialBodyCore.LoadZomesAsync(); 
        }

        public OASISResult<IEnumerable<IZome>> LoadZomes()
        {
            return CelestialBodyCore.LoadZomes();
        }

        //public async Task<OASISResult<IHolon>> LoadCelestialBodyAsync()
        //{
        //    return await CelestialBodyCore.LoadCelestialBodyAsync();
        //}

        //public OASISResult<IHolon> LoadCelestialBody()
        //{
        //    return CelestialBodyCore.LoadCelestialBody();
        //}

        //public override async Task<OASISResult<ICelestialBody>> LoadAsync(bool loadZomes = true, bool continueOnError = true)
        public async Task<OASISResult<ICelestialBody>> LoadAsync(bool loadZomes = true, bool continueOnError = true)
        {
            OASISResult<ICelestialBody> result = await CelestialBodyCore.LoadCelestialBodyAsync();

            if ((result != null && !result.IsError && result.Result != null)
                || ((result == null || result.IsError || result.Result == null) && continueOnError))
            {
                if (loadZomes)
                {
                    OASISResult<IEnumerable<IZome>> zomeResult = await LoadZomesAsync();

                    if (!(zomeResult != null && !zomeResult.IsError && zomeResult.Result != null))
                        ErrorHandling.HandleWarning(ref result, $"The CelestialBody {this.Name} with type {Enum.GetName(typeof(HolonType), this.HolonType)} loaded fine but it's zomes failed to load. Reason: {zomeResult.Message}");
                }
            }

            return result;
            //return await CelestialBodyCore.LoadCelestialBodyAsync();
        }

        //public override OASISResult<ICelestialBody> Load(bool loadZomes = true, bool continueOnError = true)
        public OASISResult<ICelestialBody> Load(bool loadZomes = true, bool continueOnError = true)
        {
            return LoadAsync(loadZomes, continueOnError).Result;
            //return CelestialBodyCore.LoadCelestialBody();
        }

        protected override async Task InitializeAsync()
        {
            InitCelestialBodyCore();
            WireUpEvents();

            if (Id != Guid.Empty || (ProviderKey != null && ProviderKey.Keys.Count > 0))
            {
                OASISResult<IHolon> celestialBodyResult = await LoadAsync();

                if (celestialBodyResult != null && !celestialBodyResult.IsError && celestialBodyResult.Result != null)
                    Mapper.MapBaseHolonProperties(celestialBodyResult.Result, this);
                else
                    throw new Exception($"ERROR: Error loading CelesitalBody, reason: {celestialBodyResult.Message}"); //TODO: Replace exception with bubbling up OASISResult (needs to be OASIS wide ASAP).

                //await LoadCelestialBodyAsync();
                await LoadZomesAsync();

               
                //OASISResult<IEnumerable<IZome>> zomesResult = await LoadZomesAsync();

                //if (zomesResult != null && !zomesResult.IsError && zomesResult.Result != null)
                //    this.CelestialBodyCore.Zomes = (List<IZome>)zomesResult.Result;
                //else
                //    throw new Exception($"ERROR: Error loading CelesitalBody Zomes, reason: {zomesResult.Message}"); //TODO: Replace exception with bubbling up OASISResult (needs to be OASIS wide ASAP).
            }

            await base.InitializeAsync();
        }

        protected override void Initialize()
        {
            InitCelestialBodyCore();
            WireUpEvents();

            if (Id != Guid.Empty || (ProviderKey != null && ProviderKey.Keys.Count > 0))
            {
                //LoadCelestialBody();

                OASISResult<IHolon> celestialBodyResult = Load();

                if (celestialBodyResult != null && !celestialBodyResult.IsError && celestialBodyResult.Result != null)
                    Mapper.MapBaseHolonProperties(celestialBodyResult.Result, this);
                else
                    throw new Exception($"ERROR: Error loading CelesitalBody, reason: {celestialBodyResult.Message}"); //TODO: Replace exception with bubbling up OASISResult (needs to be OASIS wide ASAP).

                LoadZomes();

                
                // OASISResult<IEnumerable<IZome>> zomesResult = LoadZomes();

                //if (zomesResult != null && !zomesResult.IsError && zomesResult.Result != null)
                //    this.CelestialBodyCore.Zomes = (List<IZome>)zomesResult.Result;
                //else
                //    throw new Exception($"ERROR: Error loading CelesitalBody Zomes, reason: {zomesResult.Message}"); //TODO: Replace exception with bubbling up OASISResult (needs to be OASIS wide ASAP).
            }

            base.Initialize();
        }

        private void InitCelestialBodyCore()
        {
            // The Id/ProviderKey will be set from LoadCelestialBody/SetProperties.
            switch (this.HolonType)
            {
                case HolonType.Moon:
                    CelestialBodyCore = new MoonCore((IMoon)this);
                    break;

                case HolonType.Planet:
                    CelestialBodyCore = new PlanetCore((IPlanet)this);
                    break;

                case HolonType.Star:
                    CelestialBodyCore = new StarCore((IStar)this);
                    break;

                case HolonType.SuperStar:
                    CelestialBodyCore = new SuperStarCore((ISuperStar)this);
                    break;

                case HolonType.GrandSuperStar:
                    CelestialBodyCore = new GrandSuperStarCore((IGrandSuperStar)this);
                    break;

                case HolonType.GreatGrandSuperStar:
                    CelestialBodyCore = new GreatGrandSuperStarCore((IGreatGrandSuperStar)this);
                    break;
            }

            CelestialBodyCore.Id = this.Id;
            CelestialBodyCore.ProviderKey = this.ProviderKey;
        }

        private void WireUpEvents()
        {
            if (CelestialBodyCore != null)
            {
                ((CelestialBodyCore)CelestialBodyCore).OnHolonsLoaded += CelestialBodyCore_OnHolonsLoaded;
                ((CelestialBodyCore)CelestialBodyCore).OnZomesLoaded += CelestialBodyCore_OnZomesLoaded;
                ((CelestialBodyCore)CelestialBodyCore).OnHolonSaved += CelestialBodyCore_OnHolonSaved;
                ((CelestialBodyCore)CelestialBodyCore).OnZomeError += CelestialBodyCore_OnZomeError;
            }
        }

        private async void CelestialBodyCore_OnHolonSaved(object sender, HolonSavedEventArgs e)
        {
            OnHolonSaved?.Invoke(sender, e);

            //TODO: Come back to this...
            return;

            //TODO: Handle error.
            if (!e.Result.IsError)
            {
                if (e.Result.Result.HolonType == HolonType.Planet)
                {
                    // This is the hc Address of the planet (we can use this as the anchor/coreProviderKey to load all future zomes/holons belonging to this planet).
                    this.ProviderKey = e.Result.Result.ProviderKey;

                    //Just in case the zomes/holons have been added since the planet was last saved.
                    foreach (Zome zome in CelestialBodyCore.Zomes)
                    {
                        switch (HolonType)
                        {
                            case HolonType.Star:
                                zome.ParentStar = (IStar)this;
                                zome.ParentStarId = this.Id;
                                break;

                            case HolonType.Planet:
                                zome.ParentPlanet = (IPlanet)this;
                                zome.ParentPlanetId = this.Id;
                                break;

                            case HolonType.Moon:
                                zome.ParentMoon = (IMoon)this;
                                zome.ParentMoonId = this.Id;
                                break;
                        }

                        zome.ParentHolonId = this.Id;
                        zome.ParentHolon = this;

                        // TODO: Need to sort this.Holons collection too (this is a list of ALL holons that belong to ALL zomes for this planet.
                        // So the same holon will be in both collections, just that this.Holons has been flatterned. Why it's Fractal Holonic! ;-)
                        foreach (Holon holon in zome.Holons)
                        {
                            holon.ParentHolon = zome;
                            holon.ParentHolonId = zome.Id;

                            switch (HolonType)
                            {
                                case HolonType.Star:
                                    zome.ParentStar = (IStar)this;
                                    zome.ParentStarId = this.Id;
                                    break;

                                case HolonType.Planet:
                                    zome.ParentPlanet = (IPlanet)this;
                                    zome.ParentPlanetId = this.Id;
                                    break;

                                case HolonType.Moon:
                                    zome.ParentMoon = (IMoon)this;
                                    zome.ParentMoonId = this.Id;
                                    break;
                            }
                        }

                        await zome.SaveHolonsAsync(zome.Holons);
                    }
                }
            }
        }

        //TODO: Come back to this, this is what is fired when each zome is loaded once the celestialbody is loaded but I think for now we will lazy load them later...
        private void Zome_OnHolonLoaded(object sender, HolonLoadedEventArgs e)
        {
            /*
            bool holonFound = false;

            foreach (ZomeBase zome in CelestialBodyCore.Zomes)
            {
                foreach (Holon holon in zome.Holons)
                {
                    if (holon.Id == e.Holon.Id)
                    {
                        holonFound = true;
                        break;
                    }
                }
            }

            // If the zome or holon is not stored in the cache yet then add it now...
            // Currently the collection will fill up as the individual zome loads each holon.
            // They can call the LoadAll function to load all Holons and Zomes linked to this Planet (OAPP).

            //TODO: Now all zomes and holons belonging to a planet (OAPP) are loaded in init method using hc anchor pattern.
            //Maybe it can be a setting to choose between lazy loading (loading only as needed) or to prefetch and load everything up front.
            //Pros and Cons to both methods, Lazy loading = quicker init load time and less memory but then if you start loading lots of zomes/holons after, that's a lot more network traffic, etc.
            //Loading up front- Longer init load time and uses more memory but then all data cached so no more loading or network traffic needed.

            if (!holonFound)
            {
                //IZome zome = CelestialBodyCore.Zomes.FirstOrDefault(x => x.Parent.Name == e.Holon.Parent.Name);
                IZome zome = CelestialBodyCore.Zomes.FirstOrDefault(x => x.Parent.Id == e.Holon.Parent.Id);

                if (zome == null)
                {
                    zome = new Zome(e.Holon.Parent.Id);
                    zome.Holons.Add(e.Holon);
                    CelestialBodyCore.Zomes.Add(zome);
                    //CelestialBodyCore.Zomes.Add(new Zome(HoloNETClient, e.Holon.Parent.Name));
                }

                ((ZomeBase)zome).Holons.Add((Holon)e.Holon);
            }

            OnHolonLoaded?.Invoke(this, e);
            */
        }

        private void Zome_OnHolonSaved(object sender, HolonSavedEventArgs e)
        {
            //TODO: Handle Error.
            if (!e.Result.IsError)
            {
                if (e.Result.Result.HolonType == HolonType.Zome)
                {
                    IZome zome = GetZomeById(e.Result.Result.Id);

                    if (zome != null)
                    {
                        //If the ProviderKey is empty then this is the first time the zome has been saved so we now need to save the zomes holons.
                        //if (string.IsNullOrEmpty(zome.ProviderKey))
                        // {
                        zome.ProviderKey = e.Result.Result.ProviderKey;
                        zome.ParentHolon = e.Result.Result;

                        switch (HolonType)
                        {
                            case HolonType.Star:
                                zome.ParentStar = (IStar)this;
                                zome.ParentStarId = this.Id;
                                break;

                            case HolonType.Planet:
                                zome.ParentPlanet = (IPlanet)this;
                                zome.ParentPlanetId = this.Id;
                                break;

                            case HolonType.Moon:
                                zome.ParentMoon = (IMoon)this;
                                zome.ParentMoonId = this.Id;
                                break;
                        }

                        foreach (Holon holon in GetHolonsThatBelongToZome(zome))
                            zome.SaveHolonAsync(holon);
                    }
                }
                else
                {
                    IHolon holon = CelestialBodyCore.Holons.FirstOrDefault(x => x.Id == e.Result.Result.Id);

                    //TODO: Come back to this... Wouldn't parent already be set? Same for zomes? Need to check...
                    if (holon != null)
                    {
                        holon.ProviderKey = e.Result.Result.ProviderKey;
                        //holon.Parent = e.Holon;
                        //holon.ParentCelestialBody = this;

                        switch (HolonType)
                        {
                            case HolonType.Star:
                                holon.ParentStar = (IStar)this;
                                holon.ParentStarId = this.Id;
                                break;

                            case HolonType.Planet:
                                holon.ParentPlanet = (IPlanet)this;
                                holon.ParentPlanetId = this.Id;
                                break;

                            case HolonType.Moon:
                                holon.ParentMoon = (IMoon)this;
                                holon.ParentMoonId = this.Id;
                                break;
                        }
                    }
                }

                OnHolonSaved?.Invoke(this, e);
            }
        }

        private IZome GetZomeThatHolonBelongsTo(IHolon holon)
        {
            return (IZome)CelestialBodyCore.Holons.FirstOrDefault(x => x.Id == holon.Id).ParentHolon;
        }

        private List<IHolon> GetHolonsThatBelongToZome(IZome zome)
        {
            return CelestialBodyCore.Holons.Where(x => x.ParentHolon.Id == zome.Id).ToList();
        }

        private IZome GetZomeByName(string name)
        {
            return CelestialBodyCore.Zomes.FirstOrDefault(x => x.Name == name);
        }

        private IZome GetZomeById(Guid id)
        {
            return CelestialBodyCore.Zomes.FirstOrDefault(x => x.Id == id);
        }

        //private void PlanetBase_OnHolonSaved(object sender, HolonLoadedEventArgs e)
        //{

        //}

        //private void PlanetBase_OnHolonLoaded(object sender, HolonLoadedEventArgs e)
        //{

        //}

        /*
        private void HoloNETClient_OnZomeFunctionCallBack(object sender, ZomeFunctionCallBackEventArgs e)
        {
            //if (!e.IsCallSuccessful)
            //    HandleError(string.Concat("Zome function ", e.ZomeFunction, " on zome ", e.Zome, " returned an error. Error Details: ", e.ZomeReturnData), null, null);
            //else
            //{
            //    for (int i = 0; i < _loadFuncNames.Count; i++)
            //    {
            //        if (e.ZomeFunction == _loadFuncNames[i])
            //        {
            //            IHolon holon = (IHolon)JsonConvert.DeserializeObject<IHolon>(string.Concat("{", e.ZomeReturnData, "}"));
            //            OnHolonLoaded?.Invoke(this, new HolonLoadedEventArgs { Holon = holon });
            //            _taskCompletionSourceLoadHolon.SetResult(holon);
            //        }
            //        else if (e.ZomeFunction == _saveFuncNames[i])
            //        {
            //            _savingHolons[e.Id].HcAddressHash = e.ZomeReturnData;

            //            OnHolonSaved?.Invoke(this, new HolonLoadedEventArgs { Holon = _savingHolons[e.Id] });
            //            _taskCompletionSourceSaveHolon.SetResult(_savingHolons[e.Id]);
            //            _savingHolons.Remove(e.Id);
            //        }
            //    }
            //}
        }*/

        /*
        public virtual async Task<IHolon> LoadHolonAsync(string hcEntryAddressHash)
        {
            return await LoadHolonAsync(this.RustHolonType, hcEntryAddressHash);

            // Find the zome that the holon belongs to and then load it...
            //TODO: May be more efficient way of doing this by loading it directly? But nice each zome manages its own collection of holons...
            //foreach (ZomeBase zome in Zomes)
            //{
            //    foreach (Holon holon in zome.Holons)
            //    {
            //        //if (holon.Name == holonName)
            //        if (holon.RustHolonType == this.RustHolonType)
            //            return await zome.LoadHolonAsync(holon.RustHolonType, hcEntryAddressHash);
            //    }
            //}

            //return null;
        }
        */

        //TODO: Should this be in PlanetCore?
        /*
          public virtual async Task<IHolon> LoadHolonAsync(string rustHolonType, string hcEntryAddressHash)
          {
              // Find the zome that the holon belongs to and then load it...
              //TODO: May be more efficient way of doing this by loading it directly? But nice each zome manages its own collection of holons...
              foreach (ZomeBase zome in Zomes)
              {
                  foreach (Holon holon in zome.Holons)
                  {
                      //if (holon.Name == holonName)
                      if (holon.RustHolonType == rustHolonType)
                          return await zome.LoadHolonAsync(rustHolonType, hcEntryAddressHash);
                  }
              }

              return null;
          }*/

        /*
        //TODO: Should this be in PlanetCore?
        public virtual async Task<IHolon> SaveHolonAsync(string rustHolonType, IHolon savingHolon)
        {
            // Find the zome that the holon belongs to and then save it...
            foreach (ZomeBase zome in Zomes)
            {
                foreach (Holon holon in zome.Holons)
                {
                    //if (holon.Name == savingHolon.Name)
                    if (holon.RustHolonType == rustHolonType)
                        return await zome.SaveHolonAsync(rustHolonType, savingHolon);
                    //return await zome.SaveHolonAsync(this.RustHolonType, savingHolon);
                }
            }

            return null;
        }*/

        /*
        public virtual async Task<IHolon> SaveHolonAsync(IHolon savingHolon)
        {
            // Find the zome that the holon belongs to and then save it...
            foreach (ZomeBase zome in Zomes)
            {
                foreach (Holon holon in zome.Holons)
                {
                    //if (holon.Name == savingHolon.Name)
                    if (holon.RustHolonType == this.RustHolonType)
                        return await zome.SaveHolonAsync(this.RustHolonType, savingHolon);
                    //return await zome.SaveHolonAsync(this.RustHolonType, savingHolon);
                }
            }

            return null;
        }
        */


        /*
        private void HoloNETClient_OnSignalsCallBack(object sender, SignalsCallBackEventArgs e)
        {

        }

        private void HoloNETClient_OnGetInstancesCallBack(object sender, GetInstancesCallBackEventArgs e)
        {
            _hcinstance = e.Instances[0];
            OnInitialized?.Invoke(this, new EventArgs());
            _taskCompletionSourceGetInstance.SetResult(_hcinstance);
        }

        private void HoloNETClient_OnDataReceived(object sender, DataReceivedEventArgs e)
        {
            OnDataReceived?.Invoke(this, e);
        }

        private void HoloNETClient_OnDisconnected(object sender, DisconnectedEventArgs e)
        {
            OnDisconnected?.Invoke(this, e);
        }

        private void HoloNETClient_OnConnected(object sender, ConnectedEventArgs e)
        {
            HoloNETClient.GetHolochainInstancesAsync();
        }

        private void HoloNETClient_OnError(object sender, HoloNETErrorEventArgs e)
        {
            HandleError("Error occured in HoloNET. See ErrorDetial for reason.", null, e);
        }


        /// <summary>
        /// Handles any errors thrown by HoloNET or HolochainBaseZome. It fires the OnZomeError error handler if there are any 
        /// subscriptions.
        /// </summary>
        /// <param name="reason"></param>
        /// <param name="errorDetails"></param>
        /// <param name="holoNETEventArgs"></param>
        protected void HandleError(string reason, Exception errorDetails, HoloNETErrorEventArgs holoNETEventArgs)
        {
            OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { EndPoint = HoloNETClient.EndPoint, Reason = reason, ErrorDetails = errorDetails, HoloNETErrorDetails = holoNETEventArgs });
        }*/
    }
}
