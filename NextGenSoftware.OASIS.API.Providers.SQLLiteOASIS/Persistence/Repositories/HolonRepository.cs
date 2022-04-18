using NextGenSoftware.OASIS.API.Providers.SQLLiteOASIS.Exceptions;
using NextGenSoftware.OASIS.API.Providers.SQLLiteOASIS.Interfaces;
using NextGenSoftware.OASIS.API.Providers.SQLLiteOASIS.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.SQLLiteOASIS.Entities;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteOASIS.Persistence.Repositories
{
    public class HolonRepository : IHolonRepository
    {
        private readonly DataContext eFContext;

        public HolonRepository(DataContext eFContext)
        {
            this.eFContext = eFContext;
        }

        //public async Task<List<HolonEntity>> GetHolons()
        //{
        //    return await this.eFContext.HolonEntities.ToListAsync();
        //}

        //public async Task<HolonEntity> GetHolonById(Guid holonId)
        //{
        //    var holons = await this.eFContext.HolonEntities.Where(e => e.Id.Equals(holonId)).FirstOrDefaultAsync();
        //    if (holons != null)
        //    {
        //        return holons;
        //    }
        //    throw new NotFoundException();
        //}

        //public async Task<HolonEntity> CreateHolon(HolonEntity request)
        //{
        //    var holon = request;
        //    this.eFContext.HolonEntities.Add(holon);
        //    await this.eFContext.SaveChangesAsync();
        //    return holon;
        //}

        //public async Task<HolonEntity> UpdateHolon(HolonEntity request)
        //{
        //    var holon = this.eFContext.HolonEntities.Find(request.Id);
        //    if (holon != null)
        //    {
        //        holon.Name = request.Name;
        //        this.eFContext.HolonEntities.Update(holon);
        //        await this.eFContext.SaveChangesAsync();
        //        return holon;
        //    }
        //    throw new NotFoundException();
        //}

        //public async Task<bool> DeleteHolonById(Guid holonId)
        //{
        //    var holon = await this.eFContext.HolonEntities.Where(p => p.Id == holonId).FirstOrDefaultAsync();
        //    if (holon != null)
        //    {
        //        this.eFContext.HolonEntities.Remove(holon);
        //        this.eFContext.SaveChanges();
        //        return true;
        //    }
        //    else { return false; }
        //}

        public List<HolonEntity> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            try
            {
                return this.eFContext.HolonEntities.Where(p => p.Id == id && p.Version == version).ToList();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<List<HolonEntity>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            try
            {
                return await this.eFContext.HolonEntities.Where(p => p.Id == id && p.Version == version).ToListAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public List<HolonEntity> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            try
            {
                return this.eFContext.HolonEntities.Where(p => p.Version == version).ToList();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<List<HolonEntity>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            try
            {
                return await this.eFContext.HolonEntities.Where(p => p.Version == version).ToListAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public List<HolonEntity> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            try
            {
                return this.eFContext.HolonEntities.Where(p => p.Id == id && p.Version == version).ToList();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<List<HolonEntity>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            try
            {
                return await this.eFContext.HolonEntities.Where(p => p.Id == id && p.Version == version).ToListAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public List<HolonEntity> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            try
            {
                return this.eFContext.HolonEntities.Where(p => p.Version == version).ToList();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<List<HolonEntity>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            try
            {
                return await this.eFContext.HolonEntities.Where(p=>p.Version == version).ToListAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public List<HolonEntity> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            try
            {
                return this.eFContext.HolonEntities.Where(p=> p.Version == version).ToList();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<List<HolonEntity>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            try
            {
                return await this.eFContext.HolonEntities.Where(p => p.Version == version).ToListAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public HolonEntity SaveHolon(HolonEntity holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true)
        {
            try
            {
                HolonEntity holonEntity = CreateHolonModel(holon);
                this.eFContext.HolonEntities.Add(holonEntity);
                this.eFContext.SaveChangesAsync();
                return holonEntity;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<HolonEntity> SaveHolonAsync(HolonEntity holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true)
        {
            try
            {
                HolonEntity holonEntity = CreateHolonModel(holon);
                this.eFContext.HolonEntities.Add(holonEntity);
                await this.eFContext.SaveChangesAsync();
                return holonEntity;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public HolonEntity SaveHolons(IEnumerable<HolonEntity> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true)
        {
            try
            {
                HolonEntity holonEntity = new HolonEntity();
                foreach (var holonModel in holons)
                {
                    holonEntity = CreateHolonModel(holonModel);
                    this.eFContext.HolonEntities.Add(holonEntity);
                    this.eFContext.SaveChangesAsync();
                }
                return holonEntity;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<HolonEntity> SaveHolonsAsync(IEnumerable<HolonEntity> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true)
        {
            try
            {
                HolonEntity holonEntity = new HolonEntity();
                foreach (var holonModel in holons)
                {
                    holonEntity = CreateHolonModel(holonModel);
                    this.eFContext.HolonEntities.Add(holonEntity);
                    await this.eFContext.SaveChangesAsync();
                }
                return holonEntity;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public bool DeleteHolon(Guid id, bool softDelete = true)
        {
            try
            {
                var holon = this.eFContext.HolonEntities.Where(p => p.Id == id).FirstOrDefault();
                if (holon != null)
                {
                    if (softDelete)
                    {
                        holon.IsActive = false;
                        this.eFContext.HolonEntities.Update(holon);
                    }
                    else
                    {
                        this.eFContext.HolonEntities.Remove(holon);
                    }
                    this.eFContext.SaveChangesAsync();
                    return true;
                }
                else { return false; }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<bool> DeleteHolonAsync(Guid id, bool softDelete = true)
        {
            try
            {
                var holon = await this.eFContext.HolonEntities.Where(p => p.Id == id).FirstOrDefaultAsync();
                if (holon != null)
                {
                    if (softDelete)
                    {
                        holon.IsActive = false;
                        this.eFContext.HolonEntities.Update(holon);
                    }
                    else
                    {
                        this.eFContext.HolonEntities.Remove(holon);
                    }
                    await this.eFContext.SaveChangesAsync();
                    return true;
                }
                else { return false; }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public bool DeleteHolon(string providerKey, bool softDelete = true)
        {
            //try
            //{
            //    var holon = this.eFContext.HolonEntities.Where(p => p.ProvideKey == providerKey).FirstOrDefault();
            //    if (holon != null)
            //    {
            //        if (softDelete)
            //        {
            //            holon.IsActive = false;
            //            this.eFContext.HolonEntities.Update(holon);
            //        }
            //        else
            //        {
            //            this.eFContext.HolonEntities.Remove(holon);
            //        }
            //        this.eFContext.SaveChangesAsync();
            //        return true;
            //    }
            //    else { return false; }
            //}
            //catch (Exception)
            //{
            //    throw;
            //}
            throw new NotImplementedException();
        }

        public async Task<bool> DeleteHolonAsync(string providerKey, bool softDelete = true)
        {
            //try
            //{
            //    var holon = await this.eFContext.HolonEntities.Where(p => p.ProvideKey == providerKey).FirstOrDefaultAsync();
            //    if (holon != null)
            //    {
            //        if (softDelete)
            //        {
            //            holon.IsActive = false;
            //            this.eFContext.HolonEntities.Update(holon);
            //        }
            //        else
            //        {
            //            this.eFContext.HolonEntities.Remove(holon);
            //        }
            //        await this.eFContext.SaveChangesAsync();
            //        return true;
            //    }
            //    else { return false; }
            //}
            //catch (Exception)
            //{
            //    throw;
            //}
            throw new NotImplementedException();
        }

        public List<HolonEntity> GetHolonsNearMe(HolonType Type)
        {
            //try
            //{
            //    return this.eFContext.HolonEntities.Where(p => p.Type == Type).ToList();
            //}
            //catch (Exception)
            //{
            //    throw;
            //}
            throw new NotImplementedException();
        }

        public HolonEntity CreateHolonModel(HolonEntity Holon)
        {
            HolonEntity holon = new HolonEntity();
            holon.CreatedByAvatarId = Convert.ToString(Holon.CreatedByAvatarId);
            holon.CreatedDate = Holon.CreatedDate;
            holon.DeletedByAvatarId = Convert.ToString(Holon.DeletedByAvatarId);
            holon.DeletedDate = Holon.DeletedDate;
            holon.Description = Holon.Description;
            holon.HolonId = Holon.Id;
            holon.Id = Holon.Id;
            holon.IsActive = Holon.IsActive;
            holon.IsChanged = Holon.IsChanged;
            holon.ModifiedByAvatarId = Convert.ToString(Holon.ModifiedByAvatarId);
            holon.ModifiedDate = Holon.ModifiedDate;
            holon.Name = Holon.Name;
            holon.ParentCelestialBodyId = Holon.ParentCelestialBodyId;
            holon.ParentCelestialSpaceId = Holon.ParentCelestialSpaceId;
            holon.ParentDimensionId = Holon.ParentDimensionId;
            holon.ParentGalaxyClusterId = Holon.ParentGalaxyClusterId;
            holon.ParentGalaxyId = Holon.ParentGalaxyId;
            holon.ParentGrandSuperStarId = Holon.ParentGreatGrandSuperStarId;
            holon.ParentHolonId = Holon.ParentHolonId;
            holon.ParentMoonId = Holon.ParentMoonId;
            holon.ParentMultiverseId = Holon.ParentMultiverseId;
            holon.ParentOmniverseId = Holon.ParentOmniverseId;
            holon.ParentPlanetId = Holon.ParentPlanetId;
            holon.ParentSolarSystemId = Holon.ParentSolarSystemId;
            holon.ParentStarId = Holon.ParentStarId;
            holon.ParentSuperStarId = Holon.ParentSuperStarId;
            holon.ParentUniverseId = Holon.ParentUniverseId;
            holon.ParentZomeId = Holon.ParentZomeId;
            holon.PreviousVersionId = Holon.PreviousVersionId;
            holon.Version = Holon.Version;
            return holon;
        }
    }
}