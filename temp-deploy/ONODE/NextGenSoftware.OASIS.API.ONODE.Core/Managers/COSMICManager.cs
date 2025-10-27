//using System;
//using System.Drawing;
//using NextGenSoftware.OASIS.API.DNA;
//using NextGenSoftware.OASIS.API.Core.Enums;
//using NextGenSoftware.OASIS.API.Core.Objects;
//using NextGenSoftware.OASIS.API.Core.Interfaces;
//using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
//using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
//using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
//using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers;
//using NextGenSoftware.OASIS.API.ONODE.Core.Managers.Base;
//using NextGenSoftware.OASIS.API.Core.Helpers;
//using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
//using NextGenSoftware.OASIS.Common;
//using NextGenSoftware.Utilities;
//using System.Threading.Tasks;

//namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
//{
//    public class COSMICManager : OASISManager
//    {
//        IOmiverse _omiverse = null;

//        public COSMICManager(Guid avatarId, OASISDNA OASISDNA = null) : base(avatarId, OASISDNA)
//        {

//        }

//        public COSMICManager(IOASISStorageProvider OASISStorageProvider, Guid avatarId, OASISDNA OASISDNA = null) : base(OASISStorageProvider, avatarId, OASISDNA)
//        {

//        }

//        public IOmiverse Omiverse
//        {
//            get
//            {
//                //if (_omiverse == null)
//                //{
//                //    OASISResult<IOmiverse> result = new OASISResult<IOmiverse>();

//                //    if (_starId == Guid.Empty)
//                //        result = await OASISOmniverseGenesisAsync();
//                //    else
//                //        result = await InitDefaultCelestialBodiesAsync(result);
//                //}

//                if (_omiverse == null)
//                {
//                    _omiverse = GetOmniverseAsync().ConfigureAwait(false).GetAwaiter().GetResult();
//                }

//                return _omiverse;
//            }
//        }
        

//        public IMultiverse AddMultiverse()
//        {

//        }

//        public IMultiverse AddUniverse()
//        {

//        }

//        private async Task<OASISResult<IOmiverse>> GetOmniverseAsync()
//        {
//            OASISResult<IOmiverse> result = new OASISResult<IOmiverse>();

//            if (_omiverse == null)
//            {
//                if (_starId == Guid.Empty)
//                    result = await OASISOmniverseGenesisAsync();
//                else
//                    result = await InitDefaultCelestialBodiesAsync(result);
//            }

//            return result;
//        }


//        /// <summary>
//        /// Create's the OASIS Omniverse along with a new default Multiverse (with it's GrandSuperStar) containing the ThirdDimension containing UniversePrime (simulation) and the MagicVerse (contains OApp's), which itself contains a default GalaxyCluster containing a default Galaxy (along with it's SuperStar) containing a default SolarSystem (along wth it's Star) containing a default planet (Our World).
//        /// </summary>
//        /// <param name="result"></param>
//        /// <returns></returns>
//        private static async Task<OASISResult<IOmiverse>> OASISOmniverseGenesisAsync()
//        {
//            OASISResult<IOmiverse> result = new OASISResult<IOmiverse>();
//            OASISResult<ICelestialSpace> celestialSpaceResult = new OASISResult<ICelestialSpace>();
//            ShowStatusMessage(StarStatusMessageType.Processing, "OASIS Omniverse not found. Initiating Omniverse Genesis Process...");

//            //OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Processing, Message = "Creating Omniverse..." });

//            //Will create the Omniverse with all the omniverse dimensions (8 - 12) along with one default Multiverse and it's dimensions (1-7), each containing a Universe. 
//            //The 3rd Dimension will contain the UniversePrime and MagicVerse.
//            //It will also create the GreatGrandCentralStar in the centre of the Omniverse and also a GrandCentralStar at the centre of the Multiverse.
//            Omniverse omniverse = new Omniverse();
//            celestialSpaceResult = await omniverse.SaveAsync();
//            OASISResultHelper.CopyResult(celestialSpaceResult, result);
//            result.Result = (IOmiverse)celestialSpaceResult.Result;

//            if (!result.IsError && result.Result != null)
//            {
//                //OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Success, Message = "CelestialSpace Omniverse Created." });
//                STARDNA.DefaultGreatGrandSuperStarId = omniverse.GreatGrandSuperStar.Id.ToString();
//                STARDNA.DefaultGrandSuperStarId = omniverse.Multiverses[0].GrandSuperStar.Id.ToString();


//                //TODO: May not need any of the code below because the Omniverse Save method will recursively save all it's child CelestialBodies & CelesitalSpaces...
//                //OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Processing, Message = "Creating Default Multiverse..." });
//                //Multiverse multiverse = new Multiverse();
//                //celestialSpaceResult = await multiverse.SaveAsync(); //TODO: Check tomorrow if this is better way than using old below method (On the STAR Core).
//                ////OASISResult<IMultiverse> multiverseResult = await ((GreatGrandSuperStarCore)result.Result.GreatGrandSuperStar.CelestialBodyCore).AddMultiverseAsync(multiverse);

//                //if (!celestialSpaceResult.IsError && celestialSpaceResult.Result != null)
//                //{
//                //    OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Success, Message = "Multiverse Created." });
//                //    multiverse = (Multiverse)celestialSpaceResult.Result;
//                //    STARDNA.DefaultGrandSuperStarId = multiverse.GrandSuperStar.Id.ToString();

//                //GalaxyCluster galaxyCluster = new GalaxyCluster();
//                //galaxyCluster.CreatedOASISType = new EnumValue<OASISType>(OASISType.STARCLI);
//                //galaxyCluster.Name = "Our Milky Way Galaxy Cluster.";
//                //galaxyCluster.Description = "Our Galaxy Cluster that our Milky Way Galaxy belongs to, the default Galaxy Cluster.";
//                //Mapper<IMultiverse, GalaxyCluster>.MapParentCelestialBodyProperties(multiverse, galaxyCluster);
//                //galaxyCluster.ParentMultiverse = multiverse;
//                //galaxyCluster.ParentMultiverseId = multiverse.Id;
//                //galaxyCluster.ParentDimension = multiverse.Dimensions.ThirdDimension;
//                //galaxyCluster.ParentDimensionId = multiverse.Dimensions.ThirdDimension.Id;
//                //galaxyCluster.ParentUniverseId = multiverse.Dimensions.ThirdDimension.MagicVerse.Id;
//                //galaxyCluster.ParentUniverse = multiverse.Dimensions.ThirdDimension.MagicVerse;

//                //OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Processing, Message = "Creating Default Galaxy Cluster..." });
//                //OASISResult<IGalaxyCluster> galaxyClusterResult = await ((GrandSuperStarCore)multiverse.GrandSuperStar.CelestialBodyCore).AddGalaxyClusterToUniverseAsync(multiverse.Dimensions.ThirdDimension.MagicVerse, galaxyCluster);

//                GalaxyCluster galaxyCluster = new GalaxyCluster();
//                galaxyCluster.CreatedOASISType = new EnumValue<OASISType>(OASISType.STARCLI);
//                galaxyCluster.Name = "Our Milky Way Galaxy Cluster (Default Galaxy Cluster).";
//                galaxyCluster.Description = "Our Galaxy Cluster that our Milky Way Galaxy belongs to, the default Galaxy Cluster.";
//                Mapper<IMultiverse, GalaxyCluster>.MapParentCelestialBodyProperties(omniverse.Multiverses[0], galaxyCluster);
//                galaxyCluster.ParentMultiverse = omniverse.Multiverses[0];
//                galaxyCluster.ParentMultiverseId = omniverse.Multiverses[0].Id;
//                galaxyCluster.ParentHolon = omniverse.Multiverses[0];
//                galaxyCluster.ParentHolonId = omniverse.Multiverses[0].Id;
//                galaxyCluster.ParentCelestialSpace = omniverse.Multiverses[0];
//                galaxyCluster.ParentCelestialSpaceId = omniverse.Multiverses[0].Id;
//                galaxyCluster.ParentDimension = omniverse.Multiverses[0].Dimensions.ThirdDimension;
//                galaxyCluster.ParentDimensionId = omniverse.Multiverses[0].Dimensions.ThirdDimension.Id;
//                galaxyCluster.ParentUniverseId = omniverse.Multiverses[0].Dimensions.ThirdDimension.MagicVerse.Id;
//                galaxyCluster.ParentUniverse = omniverse.Multiverses[0].Dimensions.ThirdDimension.MagicVerse;

//                OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Processing, Message = $"Creating CelestialSpace {galaxyCluster.Name}..." });
//                OASISResult<IGalaxyCluster> galaxyClusterResult = await ((GrandSuperStarCore)omniverse.Multiverses[0].GrandSuperStar.CelestialBodyCore).AddGalaxyClusterToUniverseAsync(omniverse.Multiverses[0].Dimensions.ThirdDimension.MagicVerse, galaxyCluster);

//                if (!galaxyClusterResult.IsError && galaxyClusterResult.Result != null)
//                {
//                    OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Success, Message = $"CelestialSpace {galaxyCluster.Name} Created." }); ;
//                    galaxyCluster = (GalaxyCluster)galaxyClusterResult.Result;

//                    Galaxy galaxy = new Galaxy();
//                    galaxy.CreatedOASISType = new EnumValue<OASISType>(OASISType.STARCLI);
//                    galaxy.Name = "Our Milky Way Galaxy (Default Galaxy)";
//                    galaxy.Description = "Our Milky Way Galaxy, which is the default Galaxy.";
//                    Mapper<IGalaxyCluster, Galaxy>.MapParentCelestialBodyProperties(galaxyCluster, galaxy);
//                    galaxy.ParentGalaxyCluster = galaxyCluster;
//                    galaxy.ParentGalaxyClusterId = galaxyCluster.Id;
//                    galaxy.ParentHolon = galaxyCluster;
//                    galaxy.ParentHolonId = galaxyCluster.Id;
//                    galaxy.ParentCelestialSpace = galaxyCluster;
//                    galaxy.ParentCelestialSpaceId = galaxyCluster.Id;

//                    OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Processing, Message = $"Creating CelestialSpace {galaxy.Name}..." });
//                    //OASISResult<IGalaxy> galaxyResult = await ((GrandSuperStarCore)multiverse.GrandSuperStar.CelestialBodyCore).AddGalaxyToGalaxyClusterAsync(galaxyCluster, galaxy);
//                    OASISResult<IGalaxy> galaxyResult = await ((GrandSuperStarCore)omniverse.Multiverses[0].GrandSuperStar.CelestialBodyCore).AddGalaxyToGalaxyClusterAsync(galaxyCluster, galaxy);

//                    if (!galaxyResult.IsError && galaxyResult.Result != null)
//                    {
//                        OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Success, Message = $"CelestialSpace {galaxy.Name} Created." });
//                        galaxy = (Galaxy)galaxyResult.Result;
//                        STARDNA.DefaultSuperStarId = galaxy.SuperStar.Id.ToString();

//                        SolarSystem solarSystem = new SolarSystem();
//                        solarSystem.CreatedOASISType = new EnumValue<OASISType>(OASISType.STARCLI);
//                        solarSystem.Name = "Our Solar System (Default Solar System)";
//                        solarSystem.Description = "Our Solar System, which is the default Solar System.";
//                        solarSystem.Id = Guid.NewGuid();
//                        solarSystem.IsNewHolon = true;

//                        Mapper<IGalaxy, Star>.MapParentCelestialBodyProperties(galaxy, (Star)solarSystem.Star);
//                        solarSystem.Star.Name = "Our Sun (Sol) (Default Star)";
//                        solarSystem.Star.Description = "The Sun at the centre of our Solar System";
//                        solarSystem.Star.ParentGalaxy = galaxy;
//                        solarSystem.Star.ParentGalaxyId = galaxy.Id;
//                        solarSystem.Star.ParentHolon = galaxy;
//                        solarSystem.Star.ParentHolonId = galaxy.Id;
//                        solarSystem.Star.ParentCelestialSpace = galaxy;
//                        solarSystem.Star.ParentCelestialSpaceId = galaxy.Id;
//                        solarSystem.Star.ParentSolarSystem = solarSystem;
//                        solarSystem.Star.ParentSolarSystemId = solarSystem.Id;

//                        //Star star = new Star();
//                        //star.CreatedOASISType = new EnumValue<OASISType>(OASISType.STARCLI);
//                        //Mapper<IGalaxy, Star>.MapParentCelestialBodyProperties(galaxy, star);
//                        //star.Name = "Our Sun (Sol)";
//                        //star.Description = "The Sun at the centre of our Solar System";
//                        //star.ParentGalaxy = galaxy;
//                        //star.ParentGalaxyId = galaxy.Id;
//                        //star.ParentSolarSystem = solarSystem;
//                        //star.ParentSolarSystemId = solarSystem.Id;

//                        OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Processing, Message = $"Creating CelestialBody {solarSystem.Star.Name}..." });
//                        OASISResult<IStar> starResult = await ((SuperStarCore)galaxy.SuperStar.CelestialBodyCore).AddStarAsync(solarSystem.Star);

//                        if (!starResult.IsError && starResult.Result != null)
//                        {
//                            OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Success, Message = $"CelestialBody {solarSystem.Star.Name} Created." });
//                            solarSystem.Star = (Star)starResult.Result;
//                            DefaultStar = solarSystem.Star; //TODO: TEMP: For now the default Star in STAR ODK will be our Sun (this will be more dynamic later on).
//                            STARDNA.DefaultStarId = DefaultStar.Id.ToString();

//                            Mapper<IStar, SolarSystem>.MapParentCelestialBodyProperties(solarSystem.Star, solarSystem);
//                            solarSystem.ParentStar = solarSystem.Star;
//                            solarSystem.ParentStarId = solarSystem.Star.Id;
//                            solarSystem.ParentHolon = solarSystem;
//                            solarSystem.ParentHolonId = solarSystem.Id;
//                            solarSystem.ParentCelestialSpace = solarSystem;
//                            solarSystem.ParentCelestialSpaceId = solarSystem.Id;
//                            solarSystem.ParentSolarSystem = null;
//                            solarSystem.ParentSolarSystemId = Guid.Empty;

//                            //TODO: Not sure if this method should also automatically create a Star inside it like the methods above do for Galaxy, Universe etc?
//                            // I like how a Star creates its own Solar System from its StarDust, which is how it works in real life I am pretty sure? So I think this is best... :)
//                            //TODO: For some reason I could not get Galaxy and Universe to work the same way? Need to come back to this so they all work in the same consistent manner...

//                            OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Processing, Message = $"Creating CelestialSpace {solarSystem.Name}..." });
//                            OASISResult<ISolarSystem> solarSystemResult = await ((StarCore)solarSystem.Star.CelestialBodyCore).AddSolarSystemAsync(solarSystem);

//                            if (!solarSystemResult.IsError && solarSystemResult.Result != null)
//                            {
//                                OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Success, Message = $"CelestialSpace {solarSystem.Name} Created." });
//                                solarSystem = (SolarSystem)solarSystemResult.Result;

//                                Planet ourWorld = new Planet();
//                                ourWorld.CreatedOASISType = new EnumValue<OASISType>(OASISType.STARCLI);
//                                ourWorld.Name = "Our World (Default Planet)";
//                                ourWorld.Description = "The digital twin of our planet and the default planet.";
//                                Mapper<ISolarSystem, Planet>.MapParentCelestialBodyProperties(solarSystem, ourWorld);
//                                ourWorld.ParentSolarSystem = solarSystem;
//                                ourWorld.ParentSolarSystemId = solarSystem.Id;
//                                ourWorld.ParentHolon = solarSystem;
//                                ourWorld.ParentHolonId = solarSystem.Id;
//                                ourWorld.ParentCelestialSpace = solarSystem;
//                                ourWorld.ParentCelestialSpaceId = solarSystem.Id;
//                                // await ourWorld.InitializeAsync();

//                                //OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Processing, Message = "Creating Default Planet (Our World)..." });
//                                OASISResult<IPlanet> ourWorldResult = await ((StarCore)solarSystem.Star.CelestialBodyCore).AddPlanetAsync(ourWorld);

//                                if (!ourWorldResult.IsError && ourWorldResult.Result != null)
//                                {
//                                    //OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Success, Message = "Our World Created." });
//                                    ourWorld = (Planet)ourWorldResult.Result;
//                                    STARDNA.DefaultPlanetId = ourWorld.Id.ToString();
//                                }
//                                else
//                                {
//                                    OASISResultHelper.CopyResult(ourWorldResult, result);
//                                    OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Error, Message = $"Error Creating Our World. Reason: {ourWorldResult.Message}." });
//                                }
//                            }
//                            else
//                                OASISResultHelper.CopyResult(solarSystemResult, result);
//                        }
//                        else
//                        {
//                            OASISResultHelper.CopyResult(starResult, result);
//                            OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Error, Message = $"Error Creating Star. Reason: {starResult.Message}." });
//                        }
//                    }
//                    else
//                    {
//                        OASISResultHelper.CopyResult(galaxyResult, result);
//                        OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Error, Message = $"Error Creating Galaxy. Reason: {galaxyResult.Message}." });
//                    }
//                }
//                else
//                {
//                    OASISResultHelper.CopyResult(galaxyClusterResult, result);
//                    OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Error, Message = $"Error Creating Galaxy Cluster. Reason: {galaxyClusterResult.Message}." });
//                }
//                //}
//                //else
//                //{
//                //    OASISResultHelper<IMultiverse, ICelestialBody>.CopyResult(multiverseResult, result);
//                //    OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Error, Message = $"Error Creating Multiverse. Reason: {multiverseResult.Message}." });
//                //}
//            }
//            else
//                OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Error, Message = $"Error Creating Omniverse. Reason: {result.Message}." });

//            SaveDNA();

//            if (!result.IsError)
//            {
//                result.Message = "STAR Ignited and The OASIS Omniverse Created.";
//                OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Success, Message = "Omniverse Genesis Process Complete." });
//            }

//            return result;
//        }

//        private static OASISResult<IOmiverse> InitDefaultCelestialBodies(OASISResult<IOmiverse> result)
//        {
//            ShowStatusMessage(StarStatusMessageType.Success, "OASIS Omniverse Already Created.");
//            ShowStatusMessage(StarStatusMessageType.Processing, "Initializing Default Celestial Bodies...");

//            (result, DefaultPlanet) = InitCelestialBody<Planet>(STARDNA.DefaultPlanetId, "Default Planet", result);

//            if (result.IsError || DefaultPlanet == null)
//                return result;

//            (result, DefaultStar) = InitCelestialBody<Star>(STARDNA.DefaultStarId, "Default Star", result);

//            if (result.IsError || DefaultStar == null)
//                return result;

//            (result, DefaultSuperStar) = InitCelestialBody<SuperStar>(STARDNA.DefaultSuperStarId, "Default Super Star", result);

//            if (result.IsError || DefaultSuperStar == null)
//                return result;

//            (result, DefaultGrandSuperStar) = InitCelestialBody<GrandSuperStar>(STARDNA.DefaultGrandSuperStarId, "Default Grand Super Star", result);

//            if (result.IsError || DefaultGrandSuperStar == null)
//                return result;

//            (result, DefaultGreatGrandSuperStar) = InitCelestialBody<GreatGrandSuperStar>(STARDNA.DefaultGreatGrandSuperStarId, "Default Great Grand Super Star", result);

//            if (result.IsError || DefaultGreatGrandSuperStar == null)
//                return result;

//            ShowStatusMessage(StarStatusMessageType.Success, "Default Celestial Bodies Initialized.");
//            return result;
//        }

//        private static async Task<OASISResult<IOmiverse>> InitDefaultCelestialBodiesAsync(OASISResult<IOmiverse> result)
//        {
//            ShowStatusMessage(StarStatusMessageType.Success, "OASIS Omniverse Already Created.");
//            ShowStatusMessage(StarStatusMessageType.Processing, "Initializing Default Celestial Bodies...");

//            (result, DefaultPlanet) = await InitCelestialBodyAsync<Planet>(STARDNA.DefaultPlanetId, "Default Planet", result);

//            if (result.IsError || DefaultPlanet == null)
//                return result;

//            (result, DefaultStar) = await InitCelestialBodyAsync<Star>(STARDNA.DefaultStarId, "Default Star", result);

//            if (result.IsError || DefaultStar == null)
//                return result;

//            (result, DefaultSuperStar) = await InitCelestialBodyAsync<SuperStar>(STARDNA.DefaultSuperStarId, "Default Super Star", result);

//            if (result.IsError || DefaultSuperStar == null)
//                return result;

//            (result, DefaultGrandSuperStar) = await InitCelestialBodyAsync<GrandSuperStar>(STARDNA.DefaultGrandSuperStarId, "Default Grand Super Star", result);

//            if (result.IsError || DefaultGrandSuperStar == null)
//                return result;

//            (result, DefaultGreatGrandSuperStar) = await InitCelestialBodyAsync<GreatGrandSuperStar>(STARDNA.DefaultGreatGrandSuperStarId, "Default Great Grand Super Star", result);

//            if (result.IsError || DefaultGreatGrandSuperStar == null)
//                return result;

//            ShowStatusMessage(StarStatusMessageType.Success, "Default Celestial Bodies Initialized.");

//            return result;
//        }

//        private static (OASISResult<IOmiverse>, T) InitCelestialBody<T>(string id, string longName, OASISResult<IOmiverse> result) where T : ICelestialBody, new()
//        {
//            Guid guidId;
//            ICelestialBody celestialBody = null;
//            string name = longName.Replace(" ", "");

//            ShowStatusMessage(StarStatusMessageType.Processing, $"Initializing {longName}...");

//            if (!string.IsNullOrEmpty(id))
//            {
//                if (Guid.TryParse(id, out guidId))
//                {
//                    //Normally you would leave autoLoad set to true but if you need to process the result in-line then you need to manually call Load as we do here (otherwise you would process the result from the OnCelestialBodyLoaded or OnCelestialBodyError event handlers).
//                    //ICelestialBody celestialBody = new T(guidId, false);
//                    celestialBody = new T() { Id = guidId };
//                    OASISResult<T> celestialBodyResult = celestialBody.Load<T>();

//                    if (celestialBodyResult.IsError || celestialBodyResult.Result == null)
//                    {
//                        ShowStatusMessage(StarStatusMessageType.Error, $"Error Initializing {longName}.");
//                        HandleCelesitalBodyInitError(result, name, id, celestialBodyResult);
//                    }
//                    else
//                    {
//                        ShowStatusMessage(StarStatusMessageType.Success, $"{longName} Initialized.");
//                        OnDefaultCeletialBodyInit?.Invoke(null, new DefaultCelestialBodyInitEventArgs() { Result = OASISResultHelper.CopyResultToICelestialBody(celestialBodyResult) });
//                    }
//                }
//                else
//                    HandleCelesitalBodyInitError<T>(result, name, id, $"The {name}Id value in STARDNA.json is not a valid Guid.");
//            }
//            else
//                HandleCelesitalBodyInitError<T>(result, name, id, $"The {name}Id value in STARDNA.json is missing.");

//            return (result, (T)celestialBody);
//        }

//        private static async Task<(OASISResult<IOmiverse>, T)> InitCelestialBodyAsync<T>(string id, string longName, OASISResult<IOmiverse> result) where T : ICelestialBody, new()
//        {
//            Guid guidId;
//            ICelestialBody celestialBody = null;
//            string name = longName.Replace(" ", "");

//            ShowStatusMessage(StarStatusMessageType.Processing, $"Initializing {longName}..");

//            if (!string.IsNullOrEmpty(id))
//            {
//                if (Guid.TryParse(id, out guidId))
//                {
//                    //Normally you would leave autoLoad set to true but if you need to process the result in-line then you need to manually call Load as we do here (otherwise you would process the result from the OnCelestialBodyLoaded or OnCelestialBodyError event handlers).
//                    //ICelestialBody celestialBody = new T(guidId, false);
//                    celestialBody = new T() { Id = guidId };
//                    OASISResult<T> celestialBodyResult = await celestialBody.LoadAsync<T>();

//                    if (celestialBodyResult.IsError || celestialBodyResult.Result == null)
//                    {
//                        ShowStatusMessage(StarStatusMessageType.Error, $"Error Initializing {longName}.");
//                        HandleCelesitalBodyInitError(result, name, id, celestialBodyResult);
//                    }
//                    else
//                    {
//                        ShowStatusMessage(StarStatusMessageType.Success, $"{longName} Initialized.");
//                        OnDefaultCeletialBodyInit?.Invoke(null, new DefaultCelestialBodyInitEventArgs() { Result = OASISResultHelper.CopyResultToICelestialBody(celestialBodyResult) });
//                    }
//                }
//                else
//                    HandleCelesitalBodyInitError<T>(result, name, id, $"The {name}Id value in STARDNA.json is not a valid Guid.");
//            }
//            else
//                HandleCelesitalBodyInitError<T>(result, name, id, $"The {name}Id value in STARDNA.json is missing.");

//            return (result, (T)celestialBody);
//        }

//        //private static void HandleCelesitalBodyInitError(OASISResult<IOmiverse> result, string name, string id, string errorMessage, OASISResult<ICelestialBody> celstialBodyResult = null)
//        //{
//        //    string msg = $"Error occured in IgniteInnerStar initializing {name} with Id {id}. {errorMessage} Please correct or delete STARDNA to reset STAR ODK to then auto-generate new defaults.";

//        //    if (celstialBodyResult != null)
//        //        msg = string.Concat(msg, " Reason: ", celstialBodyResult.Message);

//        //    OASISErrorHandling.HandleError(ref result, msg, celstialBodyResult != null ? celstialBodyResult.DetailedMessage : null);
//        //}

//        //private static void HandleCelesitalBodyInitError(OASISResult<IOmiverse> result, string name, string id, OASISResult<ICelestialBody> celstialBodyResult)
//        //{
//        //    HandleCelesitalBodyInitError(result, name, id, "Likely reason is that the id does not exist.", celstialBodyResult);
//        //    //OASISErrorHandling.HandleError(ref result, $"Error occured in IgniteInnerStar initializing {name} with Id {id}. Likely reason is that the id does not exist. Please correct or delete STARDNA to reset STAR ODK to then auto-generate new defaults. Reason: {celstialBodyResult.Message}", celstialBodyResult.DetailedMessage);
//        //    //OASISErrorHandling.HandleError(ref result, $"Error occured in IgniteInnerStar initializing {name} with Id {id}. Likely reason is that the id does not exist, in this case remove the {name}Id from STARDNA.json and then try again. Reason: {celstialBodyResult.Message}", celstialBodyResult.DetailedMessage);
//        //}

//        private static void HandleCelesitalBodyInitError<T>(OASISResult<IOmiverse> result, string name, string id, string errorMessage, OASISResult<T> celstialBodyResult = null) where T : ICelestialBody
//        {
//            string msg = $"Error occured in IgniteInnerStar initializing {name} with Id {id}. {errorMessage} Please correct or delete STARDNA to reset STAR ODK to then auto-generate new defaults.";

//            if (celstialBodyResult != null)
//                msg = string.Concat(msg, " Reason: ", celstialBodyResult.Message);

//            OASISErrorHandling.HandleError(ref result, msg, celstialBodyResult != null ? celstialBodyResult.DetailedMessage : null);
//        }

//        private static void HandleCelesitalBodyInitError<T>(OASISResult<IOmiverse> result, string name, string id, OASISResult<T> celstialBodyResult) where T : ICelestialBody
//        {
//            HandleCelesitalBodyInitError(result, name, id, "Likely reason is that the id does not exist.", celstialBodyResult);
//        }


//    }
//}
