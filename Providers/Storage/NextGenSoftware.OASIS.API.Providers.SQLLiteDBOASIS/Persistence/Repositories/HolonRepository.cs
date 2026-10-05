using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
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
                var holonEntity = _dbContext.Holons
                    .ToList()
                    .Select(GetHolonFromEntity)
                    .FirstOrDefault(p => p.Id == id && p.Version == version);
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
                var holonEntity = _dbContext.Holons
                    .ToList()
                    .Where(p => p.Id == id.ToString())
                    .Select(GetHolonFromEntity)
                    .FirstOrDefault();
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

        public OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true,
            int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            try
            {
                var holonEntities = _dbContext.Holons
                    .ToList()
                    .Select(GetHolonFromEntity)
                    .FirstOrDefault(p => p.Version == version);
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
                var holonEntity = _dbContext.Holons
                    .ToList()
                    .Select(GetHolonFromEntity)
                    .FirstOrDefault(p => p.Version == version);
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
                var holonEntities =
                    _dbContext.Holons
                        .ToList()
                        .Where(p => p.Id == id.ToString() && p.Version == version)
                        .Select(GetHolonFromEntity)
                        .ToList();
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
                var holonEntities = _dbContext.Holons
                    .ToList()
                    .Where(p => p.Id == id.ToString() && p.Version == version)
                    .Select(GetHolonFromEntity)
                    .ToList();
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
                var holonEntities = _dbContext.Holons
                    .Where(p => p.Version == version)
                    .Select(x => GetHolonFromEntity(x))
                    .ToList();
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
                    IsLoaded = false,
                    IsError = true,
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
                var holonEntities = await _dbContext.Holons
                    .Where(p => p.Version == version)
                    .Select(x => GetHolonFromEntity(x))
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

        public OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true,
            bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true,
            int version = 0)
        {
            try
            {
                var holonEntities = _dbContext.Holons
                    .Where(p => p.Version == version)
                    .ToList()
                    .Select(GetHolonFromEntity)
                    .ToList();
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
                    IsLoaded = false,
                    IsError = true,
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
                var holonEntities = _dbContext.Holons
                    .Where(p => p.Version == version)
                    .ToList()
                    .Select(GetHolonFromEntity)
                    .ToList();
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
                    IsLoaded = false,
                    IsError = true,
                    Message = ex.ToString()
                };
            }
        }

        public OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true,
            int maxChildDepth = 0, bool continueOnError = true)
        {
            try
            {
                HolonModel holonEntity = CreateHolonModel(holon);
                _dbContext.Holons.Add(holonEntity);
                _dbContext.SaveChangesAsync();
                return new OASISResult<IHolon>
                    {IsError = false, Result = GetHolonFromEntity(holonEntity), IsSaved = true};
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
                HolonModel holonEntity = CreateHolonModel(holon);
                _dbContext.Holons.Add(holonEntity);
                await _dbContext.SaveChangesAsync();
                return new OASISResult<IHolon>
                    {IsError = false, Result = GetHolonFromEntity(holonEntity), IsSaved = true};
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
                HolonModel holonEntity = new();
                foreach (var holonModel in holons)
                {
                    holonEntity = CreateHolonModel(holonModel);
                    _dbContext.Holons.Add(holonEntity);
                    _dbContext.SaveChangesAsync();
                }

                return new OASISResult<IEnumerable<IHolon>>
                    {IsError = false, Result = holons, IsSaved = true};
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
                HolonModel holonEntity = new();
                foreach (var holonModel in holons)
                {
                    holonEntity = CreateHolonModel(holonModel);
                    _dbContext.Holons.Add(holonEntity);
                    await _dbContext.SaveChangesAsync();
                }

                return new OASISResult<IEnumerable<IHolon>>
                    {IsError = false, Result = holons, IsSaved = true};
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

        public OASISResult<IHolon> DeleteHolon(Guid id, bool softDelete = true)
        {
            try
            {
                var holon = _dbContext.Holons.FirstOrDefault(p => p.Id == id.ToString());
                if (holon != null)
                {
                    if (softDelete)
                    {
                        holon.IsActive = false;
                        _dbContext.Holons.Update(holon);
                    }
                    else
                    {
                        _dbContext.Holons.Remove(holon);
                    }

                    _dbContext.SaveChangesAsync();
                    return new OASISResult<IHolon>
                    {
                        IsDeleted = true,
                        DeletedCount = 1,
                        Message = "Holon Deleted Successfully"
                    };
                }

                return new OASISResult<IHolon>
                {
                    IsError = true,
                    Message = "Something went wrong! please try again later"
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<IHolon>
                {
                    IsError = true,
                    Message = ex.ToString()
                };
            }
        }

        public async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id, bool softDelete = true)
        {
            try
            {
                var holon = await _dbContext.Holons.Where(p => p.Id == id.ToString()).FirstOrDefaultAsync();
                if (holon != null)
                {
                    if (softDelete)
                    {
                        holon.IsActive = false;
                        _dbContext.Holons.Update(holon);
                    }
                    else
                    {
                        _dbContext.Holons.Remove(holon);
                    }

                    await _dbContext.SaveChangesAsync();
                    return new OASISResult<IHolon>
                    {
                        IsError = false,
                        Message = "Holon Deleted Successfully",
                        IsDeleted = true,
                        DeletedCount = 1
                        //Result = holon
                    };
                }

                return new OASISResult<IHolon>
                {
                    IsError = true,
                    Message = "Something went wrong! please try again later"
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<IHolon>
                {
                    IsError = true,
                    Message = ex.ToString()
                };
            }
        }

        public OASISResult<IHolon> DeleteHolon(string providerKey, bool softDelete = true)
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

        public async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey, bool softDelete = true)
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

        private IHolon GetHolonFromEntity(HolonModel holonEntity) =>
            new Holon()
            {
                CreatedDate = holonEntity.CreatedDate,
                DeletedDate = holonEntity.DeletedDate,
                Description = holonEntity.Description,
                ParentHolonId = Guid.Parse(holonEntity.ParentHolonId),
                Id = Guid.Parse(holonEntity.Id),
                IsActive = holonEntity.IsActive,
                IsChanged = holonEntity.IsChanged,
                ModifiedDate = holonEntity.ModifiedDate,
                Name = holonEntity.Name,
                Version = holonEntity.Version
            };

        private HolonModel CreateHolonModel(IHolon holon)
        {
            return new()
            {
                CreatedDate = holon.CreatedDate,
                DeletedDate = holon.DeletedDate,
                Description = holon.Description,
                ParentHolonId = holon.ParentHolonId.ToString(),
                Id = holon.Id.ToString(),
                IsActive = holon.IsActive,
                IsChanged = holon.IsChanged,
                ModifiedDate = holon.ModifiedDate,
                Name = holon.Name,
                Version = holon.Version
            };
        }
    }
}