using NextGenSoftware.OASIS.API.Core.Holons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.DataBaseModels;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Repositories{

    public class HolonRepository : IHolonRepository
    {
        private readonly DataContext dataBase;

        public HolonRepository(DataContext dataBase){

            this.dataBase=dataBase;
        }

        public OASISResult<IHolon> Add(IHolon holon)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();

            try
            {
                holon.Id = Guid.NewGuid();
                
                if (AvatarManager.LoggedInAvatar != null)
                    holon.CreatedByAvatarId = AvatarManager.LoggedInAvatar.Id;

                holon.CreatedDate = DateTime.Now;

                HolonModel holonModel = new HolonModel(holon);
                holonModel.CreatedProviderType = ProviderType.SQLLiteDBOASIS;

                dataBase.Holons.Add(holonModel);
                dataBase.SaveChanges();

                // holonModel.ProviderKey.Add(new ProviderKeyModel(ProviderType.SQLLiteDBOASIS, holonModel.Id));
                holon.ProviderKey[ProviderType.SQLLiteDBOASIS] = holonModel.Id;
                dataBase.SaveChanges();

                result.Result = holon;
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error occurred adding holon in SQLLiteOASIS Provider.";
                result.Exception = ex;
            }

            return (result);
        }

        public async Task<OASISResult<IHolon>> AddAsync(IHolon holon)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();

            try
            {
                holon.Id = Guid.NewGuid();
                
                if (AvatarManager.LoggedInAvatar != null)
                    holon.CreatedByAvatarId = AvatarManager.LoggedInAvatar.Id;

                holon.CreatedDate = DateTime.Now;

                HolonModel holonModel = new HolonModel(holon);
                holonModel.CreatedProviderType = ProviderType.SQLLiteDBOASIS;

                await dataBase.Holons.AddAsync(holonModel);
                await dataBase.SaveChangesAsync();

                //holonModel.ProviderKey.Add(new ProviderKeyModel(ProviderType.SQLLiteDBOASIS, holonModel.Id));
                holon.ProviderKey[ProviderType.SQLLiteDBOASIS] = holonModel.Id;
                await dataBase.SaveChangesAsync();

                result.Result = holon;
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error occurred adding holon in SQLLiteOASIS Provider.";
                result.Exception = ex;
            }

            return (result);
        }

        public OASISResult<bool> Delete(Guid id, bool softDelete = true)
        {
            OASISResult<bool> result=new OASISResult<bool>();

            try
            {
                HolonModel deletingModel = dataBase.Holons.FirstOrDefault(x => x.Id.Equals(id.ToString()));

                if (deletingModel != null){

                    if (softDelete)
                    {
                        //LoadHolonReferences(deletingModel);

                        if (AvatarManager.LoggedInAvatar != null)
                            deletingModel.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id.ToString();

                        deletingModel.DeletedDate = DateTime.Now;                    
                        dataBase.Holons.Update(deletingModel);
                    }
                    else
                        dataBase.Holons.Remove(deletingModel);

                    dataBase.SaveChanges();
                }
                result.Result = true;
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error occurred deleting holon in SQLLiteOASIS Provider.";
                result.Exception = ex;
            }

            return (result);
        }

        public OASISResult<bool> Delete(string providerKey, bool softDelete = true)
        {
            OASISResult<bool> result=new OASISResult<bool>();
            try
            {
                HolonModel deletingModel = dataBase.Holons
                                            .FirstOrDefault(h => h.ProviderKey
                                            .Any(pk => pk.ProviderId == ProviderType.SQLLiteDBOASIS
                                                        &&  pk.Value.Equals(providerKey)));

                if(deletingModel != null){
                    
                    if (softDelete)
                    {
                        //LoadHolonReferences(deletingModel);
                        
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
                }
                result.Result = true;
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error occurred deleting holon in SQLLiteOASIS Provider.";
                result.Exception = ex;
            }
            return (result);
        }

        public async Task<OASISResult<bool>> DeleteAsync(Guid id, bool softDelete = true)
        {
            OASISResult<bool> result=new OASISResult<bool>();
            try
            {
                String convertedId = id.ToString();
                HolonModel deletingModel = dataBase.Holons.FirstOrDefault(x => x.Id.Equals(convertedId));

                if(deletingModel == null){
                    
                    if (softDelete)
                    {
                        //LoadHolonReferences(deletingModel);

                        if (AvatarManager.LoggedInAvatar != null)
                            deletingModel.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id.ToString();

                        deletingModel.DeletedDate = DateTime.Now;                    
                        dataBase.Holons.Update(deletingModel);
                    }
                    else
                    {
                        dataBase.Holons.Remove(deletingModel);
                    }

                    await dataBase.SaveChangesAsync();
                }
                result.Result = true;
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error occurred deleting holon in SQLLiteOASIS Provider.";
                result.Exception = ex;
            }
            return (result);
        }

        public async Task<OASISResult<bool>> DeleteAsync(string providerKey, bool softDelete = true)
        {
            OASISResult<bool> result=new OASISResult<bool>();
            try
            {
                HolonModel deletingModel = dataBase.Holons
                                            .FirstOrDefault(h => h.ProviderKey
                                            .Any(pk => pk.ProviderId == ProviderType.SQLLiteDBOASIS
                                                        &&  pk.Value.Equals(providerKey)));

                if(deletingModel != null){
                    
                    if (softDelete)
                    {
                        //LoadHolonReferences(deletingModel);

                        if (AvatarManager.LoggedInAvatar != null)
                            deletingModel.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id.ToString();

                        deletingModel.DeletedDate = DateTime.Now;                    
                        dataBase.Holons.Update(deletingModel);
                    }
                    else
                    {
                        dataBase.Holons.Remove(deletingModel);
                    }

                    await dataBase.SaveChangesAsync();
                }
                result.Result=true;
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error occurred deleting holon in SQLLiteOASIS Provider.";
                result.Exception = ex;
            }
            return (result);
        }

        public OASISResult<IEnumerable<Holon>> GetAllHolons(HolonType holonType = HolonType.All)
        {
            OASISResult<IEnumerable<Holon>> result=new OASISResult<IEnumerable<Holon>>();
            try{

                List<Holon> holonsList=new List<Holon>();

                List<HolonModel> holonsModels=dataBase.Holons.AsNoTracking().Where<HolonModel>(h => h.HolonType == holonType).ToList();
                foreach (HolonModel model in holonsModels)
                {
                    LoadHolonReferences(model);
                    holonsList.Add(model.GetHolon());
                }
                result.Result = holonsList;

            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error occurred reading holon in SQLLiteOASIS Provider.";
                result.Exception = ex;
            }
            return (result);
        }

        public async Task<OASISResult<IEnumerable<Holon>>> GetAllHolonsAsync(HolonType holonType = HolonType.All)
        {
            OASISResult<IEnumerable<Holon>> result=new OASISResult<IEnumerable<Holon>>();
            try
            {
                List<Holon> holonsList=new List<Holon>();

                List<HolonModel> holonsModels=dataBase.Holons.AsNoTracking().Where<HolonModel>(h => h.HolonType == holonType).ToList();
                foreach (HolonModel model in holonsModels)
                {
                    Action loadAction = delegate(){LoadHolonReferences(model);};
                    Task loadReferencesTask = new Task(loadAction);

                    await loadReferencesTask;

                    holonsList.Add(model.GetHolon());
                }
                result.Result = holonsList;
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error occurred reading holon in SQLLiteOASIS Provider.";
                result.Exception = ex;
            }
            return (result);
        }

        public OASISResult<IEnumerable<Holon>> GetAllHolonsForParent(Guid id, HolonType holonType)
        {
            OASISResult<IEnumerable<Holon>> result=new OASISResult<IEnumerable<Holon>>();
            try{

                List<Holon> holonsList=new List<Holon>();

                string convertedId = id.ToString();
                List<HolonModel> holonsModels=dataBase.Holons.AsNoTracking().Where<HolonModel>(h => h.HolonType == holonType && h.ParentHolonId.Equals(convertedId)).ToList();

                foreach (HolonModel model in holonsModels)
                {
                    LoadHolonReferences(model);
                    holonsList.Add(model.GetHolon());
                }
                result.Result = holonsList;
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error occurred reading holon in SQLLiteOASIS Provider.";
                result.Exception = ex;
            }
            return (result);
        }

        public OASISResult<IEnumerable<Holon>> GetAllHolonsForParent(string providerKey, HolonType holonType)
        {
            OASISResult<IEnumerable<Holon>> result=new OASISResult<IEnumerable<Holon>>();
            try{

                HolonModel holonModel = dataBase.Holons
                                            .FirstOrDefault(h => h.ProviderKey
                                                .Any(pk => pk.ProviderId == ProviderType.SQLLiteDBOASIS
                                                    &&  pk.Value.Equals(providerKey)));
                
                List<Holon> holonsList=new List<Holon>();
                if(holonModel != null){

                    List<HolonModel> holonsModels=dataBase.Holons.AsNoTracking().Where<HolonModel>(h => h.HolonType == holonType && h.ParentHolonId.Equals(holonModel.Id)).ToList();
                    foreach (HolonModel model in holonsModels)
                    {
                        LoadHolonReferences(model);
                        holonsList.Add(model.GetHolon());
                    }
                }
                result.Result = holonsList;

            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error occurred reading holon in SQLLiteOASIS Provider.";
                result.Exception = ex;
            }
            return (result);
        }

        public async Task<OASISResult<IEnumerable<Holon>>> GetAllHolonsForParentAsync(Guid id, HolonType holonType)
        {
            OASISResult<IEnumerable<Holon>> result=new OASISResult<IEnumerable<Holon>>();
            try
            {
                string convertedId = id.ToString();
                List<Holon> holonsList=new List<Holon>();

                List<HolonModel> holonsModels=dataBase.Holons.AsNoTracking().Where<HolonModel>(h => h.HolonType == holonType && h.ParentHolonId.Equals(convertedId)).ToList();
                foreach (HolonModel model in holonsModels)
                {
                    Action loadAction = delegate(){LoadHolonReferences(model);};
                    Task loadReferencesTask = new Task(loadAction);

                    await loadReferencesTask;

                    holonsList.Add(model.GetHolon());
                }
                result.Result = holonsList;
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error occurred reading holon in SQLLiteOASIS Provider.";
                result.Exception = ex;
            }
            return (result);
        }

        public async Task<OASISResult<IEnumerable<Holon>>> GetAllHolonsForParentAsync(string providerKey, HolonType holonType)
        {
            OASISResult<IEnumerable<Holon>> result = new OASISResult<IEnumerable<Holon>>();
            try
            {

                HolonModel holonModel = dataBase.Holons
                                        .FirstOrDefault(h => h.ProviderKey
                                            .Any(pk => pk.ProviderId == ProviderType.SQLLiteDBOASIS
                                                &&  pk.Value.Equals(providerKey)));
                
                List<Holon> holonsList = new List<Holon>();
                if(holonModel != null){

                    List<HolonModel> holonsModels=dataBase.Holons.AsNoTracking().Where<HolonModel>(h => h.HolonType == holonType && h.ParentHolonId.Equals(holonModel.Id)).ToList();
                    foreach (HolonModel model in holonsModels)
                    {
                        Action loadAction = delegate(){LoadHolonReferences(model);};
                        Task loadReferencesTask = new Task(loadAction);

                        await loadReferencesTask;

                        holonsList.Add(model.GetHolon());
                    }

                }
                result.Result = holonsList;
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error occurred reading holon in SQLLiteOASIS Provider.";
                result.Exception = ex;
            }
            return result;
        }

        public OASISResult<Holon> GetHolon(Guid id)
        {
            OASISResult<Holon> result = new OASISResult<Holon>();
            try
            {
                String convertedId = id.ToString();
                HolonModel holonModel = dataBase.Holons.AsNoTracking().FirstOrDefault(x => x.Id.Equals(convertedId));

                if (holonModel != null){
                    
                    LoadHolonReferences(holonModel);
                    result.Result=holonModel.GetHolon();
                }

            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error occurred getting holon in SQLLiteOASIS Provider.";
                result.Exception = ex;
            }
            return result;
        }

        public OASISResult<Holon> GetHolon(string providerKey)
        {
            OASISResult<Holon> result = new OASISResult<Holon>();
            try
            {
                HolonModel holonModel = dataBase.Holons
                                        .AsNoTracking()
                                            .FirstOrDefault(h => h.ProviderKey
                                                .Any(pk => pk.ProviderId == ProviderType.SQLLiteDBOASIS
                                                        &&  pk.Value.Equals(providerKey)));

                if (holonModel != null){
                    
                    LoadHolonReferences(holonModel);
                    result.Result=holonModel.GetHolon();
                }

            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error occurred getting holon in SQLLiteOASIS Provider.";
                result.Exception = ex;
            }
            return result;
        }

        public async Task<OASISResult<Holon>> GetHolonAsync(Guid id)
        {
            OASISResult<Holon> result = new OASISResult<Holon>();
            try
            {
                String convertedId = id.ToString();

                HolonModel holonModel = dataBase.Holons.AsNoTracking().FirstOrDefault(x => x.Id.Equals(convertedId));

                if (holonModel == null){
                    
                    Action loadAction = delegate(){LoadHolonReferences(holonModel);};
                    Task loadReferencesTask = new Task(loadAction);

                    await loadReferencesTask;
                    result.Result=holonModel.GetHolon();
                }
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error occurred getting holon in SQLLiteOASIS Provider.";
                result.Exception = ex;
            }
            return result;
        }

        public async Task<OASISResult<Holon>> GetHolonAsync(string providerKey)
        {
            OASISResult<Holon> result = new OASISResult<Holon>();
            try
            {
                HolonModel holonModel = dataBase.Holons
                                        .AsNoTracking()
                                            .FirstOrDefault(h => h.ProviderKey
                                                .Any(pk => pk.ProviderId == ProviderType.SQLLiteDBOASIS
                                                    &&  pk.Value.Equals(providerKey)));

                if (holonModel != null){
                    
                    Action loadAction = delegate(){LoadHolonReferences(holonModel);};
                    Task loadReferencesTask = new Task(loadAction);

                    await loadReferencesTask;

                    result.Result=holonModel.GetHolon();
                }
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error occurred getting holon in SQLLiteOASIS Provider.";
                result.Exception = ex;
            }
            return result;
        }

        public OASISResult<IHolon> Update(IHolon holon)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();

            try
            {
                if (AvatarManager.LoggedInAvatar != null)
                    holon.ModifiedByAvatarId = AvatarManager.LoggedInAvatar.Id;

                holon.ModifiedDate = DateTime.Now;  

                HolonModel holonModel = new HolonModel(holon);
                dataBase.Holons.Update(holonModel);

                dataBase.SaveChanges();
                result.Result = holon;
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error occurred updating holon in SQLLiteOASIS Provider.";
                result.Exception = ex;
            }
            return result;
        }

        public OASISResult<IHolon> Update(Holon holon)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();
            try
            {
                if (AvatarManager.LoggedInAvatar != null)
                    holon.ModifiedByAvatarId = AvatarManager.LoggedInAvatar.Id;

                holon.ModifiedDate = DateTime.Now; 

                HolonModel holonModel = new HolonModel(holon);
                dataBase.Holons.Update(holonModel);

                dataBase.SaveChanges();
                result.Result = holon;
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error occurred updating holon in SQLLiteOASIS Provider.";
                result.Exception = ex;
            }
            return result;
        }

        public async Task<OASISResult<IHolon>> UpdateAsync(IHolon holon)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();
            try
            {
                if (AvatarManager.LoggedInAvatar != null)
                    holon.ModifiedByAvatarId = AvatarManager.LoggedInAvatar.Id;

                holon.ModifiedDate = DateTime.Now; 

                HolonModel holonModel=new HolonModel(holon);
                dataBase.Holons.Update(holonModel);

                await dataBase.SaveChangesAsync();
                result.Result = holon;
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error occurred updating holon in SQLLiteOASIS Provider.";
                result.Exception = ex;
            }
            return result;
        }

        public async Task<OASISResult<IHolon>> UpdateAsync(Holon holon)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();
            try
            {
                if (AvatarManager.LoggedInAvatar != null)
                    holon.ModifiedByAvatarId = AvatarManager.LoggedInAvatar.Id;

                holon.ModifiedDate = DateTime.Now; 

                HolonModel holonModel = new HolonModel(holon);
                dataBase.Holons.Update(holonModel);

                await dataBase.SaveChangesAsync();
                result.Result = holon;
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error occurred updating holon in SQLLiteOASIS Provider.";
                result.Exception = ex;
            }
            return result;
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
            
            dataBase.Entry(holonModel)
                    .Collection<MetaDataModel>(holon => holon.MetaData)
                    .Load();
            
            dataBase.Entry(holonModel)
                    .Collection<ProviderMetaData>(holon => holon.ProviderMetaData)
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