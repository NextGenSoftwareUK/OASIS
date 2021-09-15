using NextGenSoftware.OASIS.API.Core.Holons;
using System.Threading.Tasks;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.DataBaseModels;
using NextGenSoftware.OASIS.API.Core.Managers;
using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Repositories{

    public class HolonRepository : IHolonRepository
    {
        private readonly DataContext dataBase;

        public HolonRepository(DataContext dataBase){

            this.dataBase=dataBase;
        }

        public Holon Add(Holon holon)
        {
            try
            {
                HolonModel holonModel=new HolonModel(holon);
                holonModel.CreatedProviderType = ProviderType.SQLLiteDBOASIS;

                dataBase.Holons.Add(holonModel);
                dataBase.SaveChanges();

                holonModel.ProviderKey.Add(new ProviderKeyModel(ProviderType.SQLLiteDBOASIS, holonModel.Id));

                dataBase.SaveChanges();
                
                holon.ProviderKey.Add(ProviderType.SQLLiteDBOASIS, holonModel.Id);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return(holon);
        }

        public Task<Holon> AddAsync(Holon holon)
        {
            try
            {
                return new Task<Holon>(()=>{

                    HolonModel holonModel=new HolonModel(holon);
                    holonModel.CreatedProviderType = ProviderType.SQLLiteDBOASIS;
                    
                    dataBase.Holons.AddAsync(holonModel);
                    dataBase.SaveChanges();
                    
                    holonModel.ProviderKey.Add(new ProviderKeyModel(ProviderType.SQLLiteDBOASIS, holonModel.Id));
                    dataBase.SaveChanges();
                    
                    holon.ProviderKey.Add(ProviderType.SQLLiteDBOASIS, holonModel.Id);

                    return(holon);

                });
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public bool Delete(Guid id, bool softDelete = true)
        {
            bool delete_complete = false;
            try
            {
                String convertedId = id.ToString();
                HolonModel deletingModel = dataBase.Holons.FirstOrDefault(x => x.Id.Equals(convertedId));

                if(deletingModel == null){
                    return(true);
                }

                if (softDelete)
                {
                    LoadHolonReferences(deletingModel);

                    if (AvatarManager.LoggedInAvatar != null)
                        deletingModel.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id.ToString();

                    deletingModel.DeletedDate = DateTime.Now;                    
                    dataBase.Holons.Update(deletingModel);
                }
                else
                {
                    dataBase.Holons.Remove(deletingModel);
                }

                dataBase.SaveChanges();
                delete_complete=true;
            }
            catch(Exception ex)
            {
                throw ex;
            }
            return(delete_complete);
        }

        public bool Delete(string providerKey, bool softDelete = true)
        {
            bool delete_complete = false;
            try
            {
                HolonModel deletingModel = dataBase.Holons
                                            .FirstOrDefault(h => h.ProviderKey
                                            .Any(pk => pk.KeyId == ProviderType.SQLLiteDBOASIS
                                                        &&  pk.Value.Equals(providerKey)));

                if(deletingModel == null){
                    return(true);
                }

                if (softDelete)
                {
                    LoadHolonReferences(deletingModel);
                    
                    if (AvatarManager.LoggedInAvatar != null)
                        deletingModel.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id.ToString();

                    deletingModel.DeletedDate = DateTime.Now;                    
                    dataBase.Holons.Update(deletingModel);
                }
                else
                {
                    dataBase.Holons.Remove(deletingModel);
                }

                dataBase.SaveChanges();
                delete_complete=true;
            }
            catch(Exception ex)
            {
                throw ex;
            }
            return(delete_complete);
        }

        public Task<bool> DeleteAsync(Guid id, bool softDelete = true)
        {
            try
            {
                return new Task<bool>(()=>{

                    String convertedId = id.ToString();
                    HolonModel deletingModel = dataBase.Holons.FirstOrDefault(x => x.Id.Equals(convertedId));

                    if(deletingModel == null){
                        return(true);
                    }

                    if (softDelete)
                    {
                        LoadHolonReferences(deletingModel);

                        if (AvatarManager.LoggedInAvatar != null)
                            deletingModel.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id.ToString();

                        deletingModel.DeletedDate = DateTime.Now;                    
                        dataBase.Holons.Update(deletingModel);
                    }
                    else
                    {
                        dataBase.Holons.Remove(deletingModel);
                    }

                    dataBase.SaveChanges();
                    return(true);

                });
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public Task<bool> DeleteAsync(string providerKey, bool softDelete = true)
        {
            try
            {
                return new Task<bool>(()=>{

                    HolonModel deletingModel = dataBase.Holons
                                            .FirstOrDefault(h => h.ProviderKey
                                            .Any(pk => pk.KeyId == ProviderType.SQLLiteDBOASIS
                                                        &&  pk.Value.Equals(providerKey)));

                    if(deletingModel == null){
                        return(true);
                    }

                    if (softDelete)
                    {
                        LoadHolonReferences(deletingModel);

                        if (AvatarManager.LoggedInAvatar != null)
                            deletingModel.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id.ToString();

                        deletingModel.DeletedDate = DateTime.Now;                    
                        dataBase.Holons.Update(deletingModel);
                    }
                    else
                    {
                        dataBase.Holons.Remove(deletingModel);
                    }

                    dataBase.SaveChanges();
                    return(true);

                });
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public IEnumerable<Holon> GetAllHolons(HolonType holonType = HolonType.All)
        {
            List<Holon> holonsList=new List<Holon>();
            try{

                List<HolonModel> holonsModels=dataBase.Holons.Where<HolonModel>(h => h.HolonType == holonType).ToList();
                foreach (HolonModel model in holonsModels)
                {
                    LoadHolonReferences(model);
                    holonsList.Add(model.GetHolon());
                }

            }
            catch(Exception ex){
                throw ex;
            }
            return(holonsList);
        }

        public Task<IEnumerable<Holon>> GetAllHolonsAsync(HolonType holonType = HolonType.All)
        {
            try
            {
                return new Task<IEnumerable<Holon>>(()=>{

                    List<Holon> holonsList=new List<Holon>();

                    List<HolonModel> holonsModels=dataBase.Holons.Where<HolonModel>(h => h.HolonType == holonType).ToList();
                    foreach (HolonModel model in holonsModels)
                    {
                        LoadHolonReferences(model);
                        holonsList.Add(model.GetHolon());
                    }
                    
                    return(holonsList);
                });
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public IEnumerable<Holon> GetAllHolonsForParent(Guid id, HolonType holonType)
        {
            List<Holon> holonsList=new List<Holon>();
            try{

                string convertedId = id.ToString();

                List<HolonModel> holonsModels=dataBase.Holons.Where<HolonModel>(h => h.HolonType == holonType && h.ParentHolonId.Equals(convertedId)).ToList();
                foreach (HolonModel model in holonsModels)
                {
                    LoadHolonReferences(model);
                    holonsList.Add(model.GetHolon());
                }

            }
            catch(Exception ex){
                throw ex;
            }
            return(holonsList);
        }

        public IEnumerable<Holon> GetAllHolonsForParent(string providerKey, HolonType holonType)
        {
            List<Holon> holonsList=new List<Holon>();
            try{



                HolonModel holonModel = dataBase.Holons
                                            .FirstOrDefault(h => h.ProviderKey
                                                .Any(pk => pk.KeyId == ProviderType.SQLLiteDBOASIS
                                                    &&  pk.Value.Equals(providerKey)));
                
                if(holonModel != null){

                    List<HolonModel> holonsModels=dataBase.Holons.Where<HolonModel>(h => h.HolonType == holonType && h.ParentHolonId.Equals(holonModel.Id)).ToList();
                    foreach (HolonModel model in holonsModels)
                    {
                        LoadHolonReferences(model);
                        holonsList.Add(model.GetHolon());
                    }

                }

            }
            catch(Exception ex){
                throw ex;
            }
            return(holonsList);
        }

        public Task<IEnumerable<Holon>> GetAllHolonsForParentAsync(Guid id, HolonType holonType)
        {
            try
            {
                return new Task<IEnumerable<Holon>>(()=>{

                    string convertedId = id.ToString();
                    List<Holon> holonsList=new List<Holon>();

                    List<HolonModel> holonsModels=dataBase.Holons.Where<HolonModel>(h => h.HolonType == holonType && h.ParentHolonId.Equals(convertedId)).ToList();
                    foreach (HolonModel model in holonsModels)
                    {
                        LoadHolonReferences(model);
                        holonsList.Add(model.GetHolon());
                    }
                    
                    return(holonsList);
                });
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public Task<OASISResult<IEnumerable<Holon>>> GetAllHolonsForParentAsync(string providerKey, HolonType holonType)
        {
            return new Task<OASISResult<IEnumerable<Holon>>>(()=>{

                OASISResult<IEnumerable<Holon>> result = new OASISResult<IEnumerable<Holon>>();
                List<Holon> holons = new List<Holon>();
                try
                {
                    HolonModel holonModel = dataBase.Holons
                                            .FirstOrDefault(h => h.ProviderKey
                                                .Any(pk => pk.KeyId == ProviderType.SQLLiteDBOASIS
                                                    &&  pk.Value.Equals(providerKey)));
                
                    if(holonModel != null){

                        List<HolonModel> holonsModels=dataBase.Holons.Where<HolonModel>(h => h.HolonType == holonType && h.ParentHolonId.Equals(holonModel.Id)).ToList();
                        foreach (HolonModel model in holonsModels)
                        {
                            LoadHolonReferences(model);
                            holons.Add(model.GetHolon());
                        }

                    }

                    result.Result = holons;
                }
                catch (Exception ex)
                {
                    result.IsError = true;
                    result.Message = string.Concat("Unknown error occured in GetAllHolonsForParentAsync method. providerKey: ", providerKey, ", holonType: ", Enum.GetName(typeof(HolonType), holonType), ". Error details: ", ex.ToString());;
                    LoggingManager.Log(result.Message, LogType.Error);
                    result.Exception = ex;
                }

                return result;
            });
        }

        public Holon GetHolon(Guid id)
        {
            Holon holon = null;
            String convertedId = id.ToString();
            try
            {
                HolonModel holonModel = dataBase.Holons.FirstOrDefault(x => x.Id.Equals(convertedId));

                if (holonModel == null){
                    return(holon);
                }

                LoadHolonReferences(holonModel);
                holon=holonModel.GetHolon();

            }
            catch(Exception ex)
            {
                throw ex;
            }
            return(holon);
        }

        public Holon GetHolon(string providerKey)
        {
            Holon holon = null;
            try
            {
                HolonModel holonModel = dataBase.Holons
                                            .FirstOrDefault(h => h.ProviderKey
                                            .Any(pk => pk.KeyId == ProviderType.SQLLiteDBOASIS
                                                        &&  pk.Value.Equals(providerKey)));

                if (holonModel == null){
                    return(holon);
                }

                LoadHolonReferences(holonModel);
                holon=holonModel.GetHolon();

            }
            catch(Exception ex)
            {
                throw ex;
            }
            return(holon);
        }

        public Task<Holon> GetHolonAsync(Guid id)
        {
            try
            {
                return new Task<Holon>(()=>{

                    Holon holon = null;
                    String convertedId = id.ToString();

                    HolonModel holonModel = dataBase.Holons.FirstOrDefault(x => x.Id.Equals(convertedId));

                    if (holonModel == null){
                        return(holon);
                    }

                    LoadHolonReferences(holonModel);
                    holon=holonModel.GetHolon();

                    return(holon);

                });
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public Task<Holon> GetHolonAsync(string providerKey)
        {
            try
            {
                return new Task<Holon>(()=>{

                    Holon holon = null;

                    HolonModel holonModel = dataBase.Holons
                                            .FirstOrDefault(h => h.ProviderKey
                                            .Any(pk => pk.KeyId == ProviderType.SQLLiteDBOASIS
                                                        &&  pk.Value.Equals(providerKey)));

                    if (holonModel == null){
                        return(holon);
                    }

                    LoadHolonReferences(holonModel);
                    holon=holonModel.GetHolon();

                    return(holon);

                });
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public Holon Update(Holon holon)
        {
            try
            {
                HolonModel holonModel=new HolonModel(holon);

                dataBase.Holons.Update(holonModel);
                dataBase.SaveChanges();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return holon;
        }

        public Task<Holon> UpdateAsync(Holon holon)
        {
            try
            {
                return new Task<Holon>(()=>{

                    HolonModel holonModel=new HolonModel(holon);

                    dataBase.Holons.Update(holonModel);
                    dataBase.SaveChangesAsync();
                    
                    return(holon);

                });
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }



        //Load all holon object related data
        private void LoadHolonReferences(HolonModel holonModel){

            dataBase.Entry(holonModel)
                    .Reference<DimensionModel>(holon => holon.ParentDimension)
                    .Load();

            dataBase.Entry(holonModel)
                    .Reference<StarModel>(holon => holon.ParentStar)
                    .Load();

            dataBase.Entry(holonModel)
                    .Reference<SuperStarModel>(holon => holon.ParentSuperStar)
                    .Load();

            dataBase.Entry(holonModel)
                    .Reference<GrandSuperStarModel>(holon => holon.ParentGrandSuperStar)
                    .Load();

            dataBase.Entry(holonModel)
                    .Reference<GreatGrandSuperStarModel>(holon => holon.ParentGreatGrandSuperStar)
                    .Load();

            dataBase.Entry(holonModel)
                    .Reference<MoonModel>(holon => holon.ParentMoon)
                    .Load();
            
            dataBase.Entry(holonModel)
                    .Reference<PlanetModel>(holon => holon.ParentPlanet)
                    .Load();
            
            dataBase.Entry(holonModel)
                    .Reference<SolarSystemModel>(holon => holon.ParentSolarSystem)
                    .Load();
            
            dataBase.Entry(holonModel)
                    .Reference<GalaxyModel>(holon => holon.ParentGalaxy)
                    .Load();
            
            dataBase.Entry(holonModel)
                    .Reference<GalaxyClusterModel>(holon => holon.ParentGalaxyCluster)
                    .Load();
            
            dataBase.Entry(holonModel)
                    .Reference<UniverseModel>(holon => holon.ParentUniverse)
                    .Load();
            
            
            dataBase.Entry(holonModel)
                    .Collection<HolonModel>(holon => holon.Childrens)
                    .Load();
            
            dataBase.Entry(holonModel)
                    .Collection<ProviderKeyModel>(holon => holon.ProviderKey)
                    .Load();

            
            dataBase.Entry(holonModel.ParentPlanet)
                    .Collection<MoonModel>(planet => planet.Moons)
                    .Load();
            
            
            dataBase.Entry(holonModel.ParentSolarSystem)
                    .Reference<StarModel>(solar => solar.Star)
                    .Load();
            
            dataBase.Entry(holonModel.ParentSolarSystem)
                    .Collection<PlanetModel>(solar => solar.Planets)
                    .Load();
            
            dataBase.Entry(holonModel.ParentSolarSystem)
                    .Collection<AsteroidModel>(solar => solar.Asteroids)
                    .Load();
            
            dataBase.Entry(holonModel.ParentSolarSystem)
                    .Collection<CometModel>(solar => solar.Comets)
                    .Load();
            
            dataBase.Entry(holonModel.ParentSolarSystem)
                    .Collection<MeteroidModel>(solar => solar.Meteroids)
                    .Load();
            
            

            dataBase.Entry(holonModel.ParentGalaxy)
                    .Reference<SuperStarModel>(galaxy => galaxy.SuperStar)
                    .Load();
            
            dataBase.Entry(holonModel.ParentGalaxy)
                    .Collection<PlanetModel>(galaxy => galaxy.Planets)
                    .Load();
            
            dataBase.Entry(holonModel.ParentGalaxy)
                    .Collection<AsteroidModel>(galaxy => galaxy.Asteroids)
                    .Load();
            
            dataBase.Entry(holonModel.ParentGalaxy)
                    .Collection<CometModel>(galaxy => galaxy.Comets)
                    .Load();
            
            dataBase.Entry(holonModel.ParentGalaxy)
                    .Collection<MeteroidModel>(galaxy => galaxy.Meteroids)
                    .Load();
            
            dataBase.Entry(holonModel.ParentGalaxy)
                    .Collection<SolarSystemModel>(galaxy => galaxy.SolarSystems)
                    .Load();

            dataBase.Entry(holonModel.ParentGalaxy)
                    .Collection<NebulaModel>(galaxy => galaxy.Nebulas)
                    .Load();
            
            dataBase.Entry(holonModel.ParentGalaxy)
                    .Collection<StarModel>(galaxy => galaxy.Stars)
                    .Load();
            


            dataBase.Entry(holonModel.ParentGalaxyCluster)
                    .Collection<GalaxyModel>(galaxy => galaxy.Galaxies)
                    .Load();
            
            dataBase.Entry(holonModel.ParentGalaxyCluster)
                    .Collection<PlanetModel>(galaxy => galaxy.Planets)
                    .Load();
            
            dataBase.Entry(holonModel.ParentGalaxyCluster)
                    .Collection<AsteroidModel>(galaxy => galaxy.Asteroids)
                    .Load();
            
            dataBase.Entry(holonModel.ParentGalaxyCluster)
                    .Collection<CometModel>(galaxy => galaxy.Comets)
                    .Load();
            
            dataBase.Entry(holonModel.ParentGalaxyCluster)
                    .Collection<MeteroidModel>(galaxy => galaxy.Meteroids)
                    .Load();
            
            dataBase.Entry(holonModel.ParentGalaxyCluster)
                    .Collection<SolarSystemModel>(galaxy => galaxy.SolarSystems)
                    .Load();
            
            dataBase.Entry(holonModel.ParentGalaxyCluster)
                    .Collection<StarModel>(galaxy => galaxy.Stars)
                    .Load();
            


            dataBase.Entry(holonModel.ParentUniverse)
                    .Collection<GalaxyClusterModel>(universe => universe.GalaxyClusters)
                    .Load();
            
            dataBase.Entry(holonModel.ParentUniverse)
                    .Collection<PlanetModel>(universe => universe.Planets)
                    .Load();
            
            dataBase.Entry(holonModel.ParentUniverse)
                    .Collection<AsteroidModel>(universe => universe.Asteroids)
                    .Load();
            
            dataBase.Entry(holonModel.ParentUniverse)
                    .Collection<CometModel>(universe => universe.Comets)
                    .Load();
            
            dataBase.Entry(holonModel.ParentUniverse)
                    .Collection<MeteroidModel>(universe => universe.Meteroids)
                    .Load();
            
            dataBase.Entry(holonModel.ParentUniverse)
                    .Collection<SolarSystemModel>(universe => universe.SolarSystems)
                    .Load();
            
            dataBase.Entry(holonModel.ParentUniverse)
                    .Collection<StarModel>(universe => universe.Stars)
                    .Load();
            
            dataBase.Entry(holonModel.ParentUniverse)
                    .Collection<NebulaModel>(universe => universe.Nebulas)
                    .Load();
        }
    }
}