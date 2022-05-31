using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Exceptions;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Interfaces;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories
{
    public class HolonRepository : IHolonRepository
    {
        private readonly DataContext eFContext;

        public HolonRepository(DataContext eFContext)
        {
            this.eFContext = eFContext;
        }

        public OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            try
            {
                var obj = this.eFContext.HolonEntities.Where(p => p.Id == id && p.Version == version).FirstOrDefault();
                if (obj == null)
                {
                    return new OASISResult<IHolon>
                    {
                        IsLoaded = false,
                        IsError = false,
                        Message = "No Holon Found",
                    };
                }
                else
                {
                    return new OASISResult<IHolon>
                    {
                        IsLoaded = true,
                        IsError = false,
                        Message = "Holon Loaded successfully",
                        Result = (IHolon)obj,
                    };
                }
            }
            catch (Exception ex)
            {
                return new OASISResult<IHolon>
                {
                    IsLoaded = false,
                    IsError = true,
                    Message = ex.ToString(),
                };
            }
        }

        public async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            try
            {
                var obj = await this.eFContext.HolonEntities.Where(p => p.Id == id && p.Version == version).FirstOrDefaultAsync();
                if (obj == null)
                {
                    return new OASISResult<IHolon>
                    {
                        IsLoaded = false,
                        IsError = false,
                        Message = "No Holon Found",
                    };
                }
                else
                {
                    return new OASISResult<IHolon>
                    {
                        IsLoaded = true,
                        IsError = false,
                        Message = "Holon Loaded successfully",
                        Result = (IHolon)obj,
                    };
                }
            }
            catch (Exception ex)
            {
                return new OASISResult<IHolon>
                {
                    IsLoaded = true,
                    IsError = false,
                    Message = ex.ToString(),
                };
            }
        }

        public OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            try
            {
                var obj = this.eFContext.HolonEntities.Where(p => p.Version == version).ToList();
                return new OASISResult<IHolon>
                {
                    IsLoaded = true,
                    IsError = false,
                    Message = "Holon Loaded successfully",
                    Result = (IHolon)obj,
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<IHolon>
                {
                    IsLoaded = true,
                    IsError = false,
                    Message = ex.ToString(),
                };
            }
        }

        public async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            try
            {
                var obj = await this.eFContext.HolonEntities.Where(p => p.Version == version).ToListAsync();
                return new OASISResult<IHolon>
                {
                    IsLoaded = true,
                    IsError = false,
                    Message = "Holon Loaded successfully",
                    Result = (IHolon)obj,
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<IHolon>
                {
                    IsLoaded = true,
                    IsError = false,
                    Message = ex.ToString(),
                };
            }
        }

        public OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            try
            {
                List<HolonEntity> obj = this.eFContext.HolonEntities.Where(p => p.Id == id && p.Version == version).ToList();
                return new OASISResult<IEnumerable<IHolon>>
                {
                    IsLoaded = true,
                    IsError = false,
                    Message = "Holon Loaded successfully",
                    Result = (IEnumerable<IHolon>)obj,
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<IEnumerable<IHolon>>
                {
                    IsLoaded = true,
                    IsError = false,
                    Message = ex.ToString(),
                };
            }
        }

        public async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            try
            {
                var obj = await this.eFContext.HolonEntities.Where(p => p.Id == id && p.Version == version).ToListAsync();
                return new OASISResult<IEnumerable<IHolon>>
                {
                    IsLoaded = true,
                    IsError = false,
                    Message = "Holon Loaded successfully",
                    Result = (IEnumerable<IHolon>)obj,
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<IEnumerable<IHolon>>
                {
                    IsLoaded = true,
                    IsError = false,
                    Message = ex.ToString(),
                };
            }
        }

        public OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            try
            {
                var obj = this.eFContext.HolonEntities.Where(p => p.Version == version).ToList();
                return new OASISResult<IEnumerable<IHolon>>
                {
                    IsLoaded = true,
                    IsError = false,
                    Message = "Holon Loaded successfully",
                    Result = (IEnumerable<IHolon>)obj,
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<IEnumerable<IHolon>>
                {
                    IsLoaded = true,
                    IsError = false,
                    Message = ex.ToString(),
                };
            }
        }

        public async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            try
            {
                var obj = await this.eFContext.HolonEntities.Where(p => p.Version == version).ToListAsync();
                return new OASISResult<IEnumerable<IHolon>>
                {
                    IsLoaded = true,
                    IsError = false,
                    Message = "Holon Loaded successfully",
                    Result = (IEnumerable<IHolon>)obj,
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<IEnumerable<IHolon>>
                {
                    IsLoaded = true,
                    IsError = false,
                    Message = ex.ToString(),
                };
            }
        }

        public OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            try
            {
                var obj = this.eFContext.HolonEntities.Where(p => p.Version == version).ToList();
                return new OASISResult<IEnumerable<IHolon>>
                {
                    IsLoaded = true,
                    IsError = false,
                    Message = "Holon Loaded successfully",
                    Result = (IEnumerable<IHolon>)obj,
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<IEnumerable<IHolon>>
                {
                    IsLoaded = true,
                    IsError = false,
                    Message = ex.ToString(),
                };
            }
        }

        public async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            try
            {
                var obj = await this.eFContext.HolonEntities.Where(p => p.Version == version).ToListAsync();
                return new OASISResult<IEnumerable<IHolon>>
                {
                    IsLoaded = true,
                    IsError = false,
                    Message = "Holon Loaded successfully",
                    Result = (IEnumerable<IHolon>)obj,
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<IEnumerable<IHolon>>
                {
                    IsLoaded = true,
                    IsError = false,
                    Message = ex.ToString(),
                };
            }
        }

        public OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true)
        {
            try
            {
                HolonEntity holonEntity = CreateHolonModel(holon);
                this.eFContext.HolonEntities.Add(holonEntity);
                this.eFContext.SaveChangesAsync();
                //return holonEntity;
                return new OASISResult<IHolon>
                { IsError = false, Result = holonEntity, IsSaved = true };
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

        public async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true)
        {
            try
            {
                HolonEntity holonEntity = CreateHolonModel(holon);
                this.eFContext.HolonEntities.Add(holonEntity);
                await this.eFContext.SaveChangesAsync();
                return new OASISResult<IHolon>
                { IsError = false, Result = holonEntity, IsSaved = true };
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

        public OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true)
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
                return new OASISResult<IEnumerable<IHolon>>
                { IsError = false, Result = (IEnumerable<IHolon>)holonEntity, IsSaved = true };
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

        public async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true)
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
                return new OASISResult<IEnumerable<IHolon>>
                { IsError = false, Result = (IEnumerable<IHolon>)holonEntity, IsSaved = true };
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
                    return new OASISResult<bool>
                    {
                        IsError = false,
                        Message = "Holon Deleted Successfully",
                        Result = true
                    };
                }
                else
                {
                    return new OASISResult<bool>
                    {
                        IsError = true,
                        Message = "Something went wrong! please try again later",
                        Result = false
                    };
                }
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
                    return new OASISResult<bool>
                    {
                        IsError = false,
                        Message = "Holon Deleted Successfully",
                        Result = true
                    };
                }
                else
                {
                    return new OASISResult<bool>
                    {
                        IsError = true,
                        Message = "Something went wrong! please try again later",
                        Result = false
                    };
                }

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

        public HolonEntity CreateHolonModel(IHolon Holon)
        {
            HolonEntity holon = new HolonEntity();
            //holon.CreatedByAvatarId = Holon.CreatedByAvatarId;
            holon.CreatedDate = Holon.CreatedDate;
            //holon.DeletedByAvatarId = Holon.DeletedByAvatarId;
            holon.DeletedDate = Holon.DeletedDate;
            holon.Description = Holon.Description;
            holon.ParentHolonId = Holon.ParentHolonId;
            holon.Id = Holon.Id;
            holon.HolonId = Guid.NewGuid();
            holon.IsActive = Holon.IsActive;
            holon.IsChanged = Holon.IsChanged;
            //holon.ModifiedByAvatarId = Holon.ModifiedByAvatarId;
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