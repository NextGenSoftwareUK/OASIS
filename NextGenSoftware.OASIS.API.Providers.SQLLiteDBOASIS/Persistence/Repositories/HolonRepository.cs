using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Interfaces;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories
{
    public class HolonRepository : IHolonRepository
    {
        private readonly DataContext _dbContext;

        public HolonRepository(DataContext dbContext)
        {
            _dbContext = dbContext;
        }

        public OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true,
            int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            try
            {
                var holonEntity = _dbContext.HolonEntities.FirstOrDefault(p => p.Id == id && p.Version == version);
                if (holonEntity == null)
                    return new OASISResult<IHolon>
                    {
                        IsLoaded = false,
                        IsError = false,
                        Message = "No Holon Found"
                    };
                return new OASISResult<IHolon>
                {
                    IsLoaded = true,
                    IsError = false,
                    Message = "Holon Loaded successfully",
                    Result = holonEntity
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<IHolon>
                {
                    IsLoaded = false,
                    IsError = true,
                    Message = ex.ToString()
                };
            }
        }

        public async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true,
            int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            try
            {
                var holonEntity = await _dbContext.HolonEntities.Where(p => p.Id == id && p.Version == version)
                    .FirstOrDefaultAsync();
                if (holonEntity == null)
                    return new OASISResult<IHolon>
                    {
                        IsLoaded = false,
                        IsError = false,
                        Message = "No Holon Found"
                    };
                return new OASISResult<IHolon>
                {
                    IsLoaded = true,
                    IsError = false,
                    Message = "Holon Loaded successfully",
                    Result = holonEntity
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<IHolon>
                {
                    IsLoaded = true,
                    IsError = false,
                    Message = ex.ToString()
                };
            }
        }

        public OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true,
            int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            try
            {
                var holonEntities = _dbContext.HolonEntities.FirstOrDefault(p => p.Version == version);
                return new OASISResult<IHolon>
                {
                    IsLoaded = true,
                    IsError = false,
                    Message = "Holon Loaded successfully",
                    Result = holonEntities
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<IHolon>
                {
                    IsLoaded = true,
                    IsError = false,
                    Message = ex.ToString()
                };
            }
        }

        public async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true,
            bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            try
            {
                var holonEntity = _dbContext.HolonEntities.FirstOrDefault(p => p.Version == version);
                return new OASISResult<IHolon>
                {
                    IsLoaded = true,
                    IsError = false,
                    Message = "Holon Loaded successfully",
                    Result = holonEntity
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<IHolon>
                {
                    IsLoaded = true,
                    IsError = false,
                    Message = ex.ToString()
                };
            }
        }

        public OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All,
            bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0,
            bool continueOnError = true, int version = 0)
        {
            try
            {
                List<HolonEntity> holonEntities =
                    _dbContext.HolonEntities.Where(p => p.Id == id && p.Version == version).ToList();
                return new OASISResult<IEnumerable<IHolon>>
                {
                    IsLoaded = true,
                    IsError = false,
                    Message = "Holon Loaded successfully",
                    Result = holonEntities
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<IEnumerable<IHolon>>
                {
                    IsLoaded = true,
                    IsError = false,
                    Message = ex.ToString()
                };
            }
        }

        public async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id,
            HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0,
            int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            try
            {
                var holonEntities = await _dbContext.HolonEntities.Where(p => p.Id == id && p.Version == version)
                    .ToListAsync();
                return new OASISResult<IEnumerable<IHolon>>
                {
                    IsLoaded = true,
                    IsError = false,
                    Message = "Holon Loaded successfully",
                    Result = holonEntities
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<IEnumerable<IHolon>>
                {
                    IsLoaded = true,
                    IsError = false,
                    Message = ex.ToString()
                };
            }
        }

        public OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All,
            bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0,
            bool continueOnError = true, int version = 0)
        {
            try
            {
                var holonEntities = _dbContext.HolonEntities.Where(p => p.Version == version).ToList();
                return new OASISResult<IEnumerable<IHolon>>
                {
                    IsLoaded = true,
                    IsError = false,
                    Message = "Holon Loaded successfully",
                    Result = holonEntities
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<IEnumerable<IHolon>>
                {
                    IsLoaded = true,
                    IsError = false,
                    Message = ex.ToString()
                };
            }
        }

        public async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey,
            HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0,
            int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            try
            {
                var holonEntities = await _dbContext.HolonEntities.Where(p => p.Version == version).ToListAsync();
                return new OASISResult<IEnumerable<IHolon>>
                {
                    IsLoaded = true,
                    IsError = false,
                    Message = "Holon Loaded successfully",
                    Result = holonEntities
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<IEnumerable<IHolon>>
                {
                    IsLoaded = true,
                    IsError = false,
                    Message = ex.ToString()
                };
            }
        }

        public OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true,
            bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true,
            int version = 0)
        {
            try
            {
                var holonEntities = _dbContext.HolonEntities.Where(p => p.Version == version).ToList();
                return new OASISResult<IEnumerable<IHolon>>
                {
                    IsLoaded = true,
                    IsError = false,
                    Message = "Holon Loaded successfully",
                    Result = holonEntities
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<IEnumerable<IHolon>>
                {
                    IsLoaded = true,
                    IsError = false,
                    Message = ex.ToString()
                };
            }
        }

        public async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All,
            bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0,
            bool continueOnError = true, int version = 0)
        {
            try
            {
                var holonEntities = await _dbContext.HolonEntities.Where(p => p.Version == version).ToListAsync();
                return new OASISResult<IEnumerable<IHolon>>
                {
                    IsLoaded = true,
                    IsError = false,
                    Message = "Holon Loaded successfully",
                    Result = holonEntities
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<IEnumerable<IHolon>>
                {
                    IsLoaded = true,
                    IsError = false,
                    Message = ex.ToString()
                };
            }
        }

        public OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true,
            int maxChildDepth = 0, bool continueOnError = true)
        {
            try
            {
                HolonEntity holonEntity = CreateHolonModel(holon);
                _dbContext.HolonEntities.Add(holonEntity);
                _dbContext.SaveChangesAsync();
                return new OASISResult<IHolon>
                    {IsError = false, Result = holonEntity, IsSaved = true};
            }
            catch (Exception ex)
            {
                return new OASISResult<IHolon>
                {
                    IsError = true,
                    IsSaved = false,
                    Message = ex.ToString()
                };
            }
        }

        public async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true,
            bool recursive = true, int maxChildDepth = 0, bool continueOnError = true)
        {
            try
            {
                HolonEntity holonEntity = CreateHolonModel(holon);
                _dbContext.HolonEntities.Add(holonEntity);
                await _dbContext.SaveChangesAsync();
                return new OASISResult<IHolon>
                    {IsError = false, Result = holonEntity, IsSaved = true};
            }
            catch (Exception ex)
            {
                return new OASISResult<IHolon>
                {
                    IsError = true,
                    IsSaved = false,
                    Message = ex.ToString()
                };
            }
        }

        public OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true,
            bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true)
        {
            try
            {
                HolonEntity holonEntity = new();
                foreach (var holonModel in holons)
                {
                    holonEntity = CreateHolonModel(holonModel);
                    _dbContext.HolonEntities.Add(holonEntity);
                    _dbContext.SaveChangesAsync();
                }

                return new OASISResult<IEnumerable<IHolon>>
                    {IsError = false, Result = (IEnumerable<IHolon>) holonEntity, IsSaved = true};
            }
            catch (Exception ex)
            {
                return new OASISResult<IEnumerable<IHolon>>
                {
                    IsError = true,
                    IsSaved = false,
                    Message = ex.ToString()
                };
            }
        }

        public async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons,
            bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0,
            bool continueOnError = true)
        {
            try
            {
                HolonEntity holonEntity = new();
                foreach (var holonModel in holons)
                {
                    holonEntity = CreateHolonModel(holonModel);
                    _dbContext.HolonEntities.Add(holonEntity);
                    await _dbContext.SaveChangesAsync();
                }

                return new OASISResult<IEnumerable<IHolon>>
                    {IsError = false, Result = (IEnumerable<IHolon>) holonEntity, IsSaved = true};
            }
            catch (Exception ex)
            {
                return new OASISResult<IEnumerable<IHolon>>
                {
                    IsError = true,
                    IsSaved = false,
                    Message = ex.ToString()
                };
            }
        }

        public OASISResult<bool> DeleteHolon(Guid id, bool softDelete = true)
        {
            try
            {
                var holon = _dbContext.HolonEntities.FirstOrDefault(p => p.Id == id);
                if (holon != null)
                {
                    if (softDelete)
                    {
                        holon.IsActive = false;
                        _dbContext.HolonEntities.Update(holon);
                    }
                    else
                    {
                        _dbContext.HolonEntities.Remove(holon);
                    }

                    _dbContext.SaveChangesAsync();
                    return new OASISResult<bool>
                    {
                        IsError = false,
                        Message = "Holon Deleted Successfully",
                        Result = true
                    };
                }

                return new OASISResult<bool>
                {
                    IsError = true,
                    Message = "Something went wrong! please try again later",
                    Result = false
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<bool>
                {
                    IsError = true,
                    Message = ex.ToString(),
                    Result = false
                };
            }
        }

        public async Task<OASISResult<bool>> DeleteHolonAsync(Guid id, bool softDelete = true)
        {
            try
            {
                var holon = await _dbContext.HolonEntities.Where(p => p.Id == id).FirstOrDefaultAsync();
                if (holon != null)
                {
                    if (softDelete)
                    {
                        holon.IsActive = false;
                        _dbContext.HolonEntities.Update(holon);
                    }
                    else
                    {
                        _dbContext.HolonEntities.Remove(holon);
                    }

                    await _dbContext.SaveChangesAsync();
                    return new OASISResult<bool>
                    {
                        IsError = false,
                        Message = "Holon Deleted Successfully",
                        Result = true
                    };
                }

                return new OASISResult<bool>
                {
                    IsError = true,
                    Message = "Something went wrong! please try again later",
                    Result = false
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<bool>
                {
                    IsError = true,
                    Message = ex.ToString(),
                    Result = false
                };
            }
        }

        public OASISResult<bool> DeleteHolon(string providerKey, bool softDelete = true)
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

        public async Task<OASISResult<bool>> DeleteHolonAsync(string providerKey, bool softDelete = true)
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

        public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(HolonType Type)
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

        private HolonEntity CreateHolonModel(IHolon holon)
        {
            return new()
            {
                CreatedDate = holon.CreatedDate,
                DeletedDate = holon.DeletedDate,
                Description = holon.Description,
                ParentHolonId = holon.ParentHolonId,
                Id = holon.Id,
                HolonId = Guid.NewGuid(),
                IsActive = holon.IsActive,
                IsChanged = holon.IsChanged,
                ModifiedDate = holon.ModifiedDate,
                Name = holon.Name,
                ParentCelestialBodyId = holon.ParentCelestialBodyId,
                ParentCelestialSpaceId = holon.ParentCelestialSpaceId,
                ParentDimensionId = holon.ParentDimensionId,
                ParentGalaxyClusterId = holon.ParentGalaxyClusterId,
                ParentGalaxyId = holon.ParentGalaxyId,
                ParentGrandSuperStarId = holon.ParentGreatGrandSuperStarId,
                ParentMoonId = holon.ParentMoonId,
                ParentMultiverseId = holon.ParentMultiverseId,
                ParentOmniverseId = holon.ParentOmniverseId,
                ParentPlanetId = holon.ParentPlanetId,
                ParentSolarSystemId = holon.ParentSolarSystemId,
                ParentStarId = holon.ParentStarId,
                ParentSuperStarId = holon.ParentSuperStarId,
                ParentUniverseId = holon.ParentUniverseId,
                ParentZomeId = holon.ParentZomeId,
                PreviousVersionId = holon.PreviousVersionId,
                Version = holon.Version
            };
        }
    }
}