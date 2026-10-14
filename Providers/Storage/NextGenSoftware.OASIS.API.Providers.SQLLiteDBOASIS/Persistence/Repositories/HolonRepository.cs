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
    public partial class HolonRepository : IHolonRepository
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
                var row = _dbContext.Holons.FirstOrDefault(p => p.Id == id.ToString() && (version == 0 || p.Version == version));
                var holonEntity = row == null ? null : GetHolonFromEntity(row);
                if (holonEntity != null && loadChildren)
                    AttachLoadedChildren(holonEntity, _dbContext.Holons.AsNoTracking().Where(item => version == 0 || item.Version == version).ToList(), recursive, maxChildDepth);
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
                    Exception = ex,
                    Message = ex.ToString()
                };
            }
        }

        public async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true,
            int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            try
            {
                var row = await _dbContext.Holons.FirstOrDefaultAsync(p => p.Id == id.ToString() && (version == 0 || p.Version == version));
                var holonEntity = row == null ? null : GetHolonFromEntity(row);
                if (holonEntity != null && loadChildren)
                    AttachLoadedChildren(holonEntity, await _dbContext.Holons.AsNoTracking().Where(item => version == 0 || item.Version == version).ToListAsync(), recursive, maxChildDepth);
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
                    Exception = ex,
                    Message = ex.ToString()
                };
            }
        }

        public OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true,
            int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            try
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(providerKey);
                var holonEntities = _dbContext.Holons
                    .Where(p => p.LocalProviderKey == providerKey && (version == 0 || p.Version == version))
                    .ToList()
                    .Select(GetHolonFromEntity)
                    .SingleOrDefault();
                if (holonEntities != null && loadChildren)
                    AttachLoadedChildren(holonEntities, _dbContext.Holons.AsNoTracking().Where(item => version == 0 || item.Version == version).ToList(), recursive, maxChildDepth);
                return new OASISResult<IHolon>
                {
                    IsLoaded = holonEntities != null,
                    IsError = false,
                    Message = "Holon Loaded successfully",
                    Result = holonEntities
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<IHolon>
                {
                    IsLoaded = false,
                    IsError = true,
                    Exception = ex, Message = ex.Message
                };
            }
        }

        public async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true,
            bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            try
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(providerKey);
                var stored = await _dbContext.Holons.SingleOrDefaultAsync(p => p.LocalProviderKey == providerKey && (version == 0 || p.Version == version));
                var holonEntity = stored == null ? null : GetHolonFromEntity(stored);
                if (holonEntity != null && loadChildren)
                    AttachLoadedChildren(holonEntity, await _dbContext.Holons.AsNoTracking().Where(item => version == 0 || item.Version == version).ToListAsync(), recursive, maxChildDepth);
                return new OASISResult<IHolon>
                {
                    IsLoaded = holonEntity != null,
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
                    Exception = ex, Message = ex.Message
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
                        .Where(p => p.ParentHolonId == id.ToString() && (version == 0 || p.Version == version) && (type == HolonType.All || p.HolonType == type))
                        .ToList()
                        .Select(GetHolonFromEntity)
                        .ToList();
                if (loadChildren)
                {
                    var graphRows = _dbContext.Holons.AsNoTracking().Where(item => version == 0 || item.Version == version).ToList();
                    foreach (var root in holonEntities) AttachLoadedChildren(root, graphRows, recursive, maxChildDepth, curentChildDepth);
                }
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
                    Exception = ex,
                    Message = ex.Message
                };
            }
        }

        public async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id,
            HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0,
            int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            try
            {
                var rows = await _dbContext.Holons
                    .Where(p => p.ParentHolonId == id.ToString() && (version == 0 || p.Version == version) && (type == HolonType.All || p.HolonType == type))
                    .ToListAsync();
                var holonEntities = rows
                    .Select(GetHolonFromEntity)
                    .ToList();
                if (loadChildren)
                {
                    var graphRows = await _dbContext.Holons.AsNoTracking().Where(item => version == 0 || item.Version == version).ToListAsync();
                    foreach (var root in holonEntities) AttachLoadedChildren(root, graphRows, recursive, maxChildDepth, curentChildDepth);
                }
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
                    Exception = ex,
                    Message = ex.Message
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
                    .Where(p => _dbContext.Holons.Any(parent => parent.LocalProviderKey == providerKey && parent.Id == p.ParentHolonId)
                        && (version == 0 || p.Version == version) && (type == HolonType.All || p.HolonType == type))
                    .ToList()
                    .Select(GetHolonFromEntity)
                    .ToList();
                if (loadChildren)
                {
                    var graphRows = _dbContext.Holons.AsNoTracking().Where(item => version == 0 || item.Version == version).ToList();
                    foreach (var root in holonEntities) AttachLoadedChildren(root, graphRows, recursive, maxChildDepth, curentChildDepth);
                }
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
                    Exception = ex,
                    Message = ex.Message
                };
            }
        }

        public async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey,
            HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0,
            int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            try
            {
                var rows = await _dbContext.Holons
                    .Where(p => _dbContext.Holons.Any(parent => parent.LocalProviderKey == providerKey && parent.Id == p.ParentHolonId)
                        && (version == 0 || p.Version == version) && (type == HolonType.All || p.HolonType == type))
                    .ToListAsync();
                var holonEntities = rows.Select(GetHolonFromEntity).ToList();
                if (loadChildren)
                {
                    var graphRows = await _dbContext.Holons.AsNoTracking().Where(item => version == 0 || item.Version == version).ToListAsync();
                    foreach (var root in holonEntities) AttachLoadedChildren(root, graphRows, recursive, maxChildDepth, curentChildDepth);
                }
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
                    Exception = ex,
                    Message = ex.Message
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
                    .Where(p => (version == 0 || p.Version == version) && (type == HolonType.All || p.HolonType == type))
                    .ToList()
                    .Select(GetHolonFromEntity)
                    .ToList();
                if (loadChildren)
                {
                    var graphRows = _dbContext.Holons.AsNoTracking().Where(item => version == 0 || item.Version == version).ToList();
                    foreach (var root in holonEntities) AttachLoadedChildren(root, graphRows, recursive, maxChildDepth, curentChildDepth);
                }
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
                    Exception = ex,
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
                var rows = await _dbContext.Holons
                    .Where(p => (version == 0 || p.Version == version) && (type == HolonType.All || p.HolonType == type))
                    .ToListAsync();
                var holonEntities = rows
                    .Select(GetHolonFromEntity)
                    .ToList();
                if (loadChildren)
                {
                    var graphRows = await _dbContext.Holons.AsNoTracking().Where(item => version == 0 || item.Version == version).ToListAsync();
                    foreach (var root in holonEntities) AttachLoadedChildren(root, graphRows, recursive, maxChildDepth, curentChildDepth);
                }
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
                    Exception = ex,
                    Message = ex.ToString()
                };
            }
        }

        public OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true,
            int maxChildDepth = 0, bool continueOnError = true)
        {
            HolonModel holonEntity = null;
            var graphModels = new List<HolonModel>();
            try
            {
                graphModels = PrepareGraph(new[] { holon }, saveChildren, recursive, maxChildDepth);
                for (var index = 0; index < graphModels.Count; index++) graphModels[index] = AddOrUpdateHolon(graphModels[index]);
                holonEntity = graphModels[0];
                _dbContext.SaveChanges();
                return new OASISResult<IHolon>
                    {IsError = false, Result = GetHolonFromEntity(holonEntity), IsSaved = true};
            }
            catch (Exception ex)
            {
                DetachOwnedFailedWrites(graphModels.Select(model => model.Id));
                return new OASISResult<IHolon>
                {
                    IsError = true,
                    IsSaved = false,
                    Exception = ex, Message = ex.Message
                };
            }
        }

        public async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true,
            bool recursive = true, int maxChildDepth = 0, bool continueOnError = true)
        {
            HolonModel holonEntity = null;
            var graphModels = new List<HolonModel>();
            try
            {
                graphModels = PrepareGraph(new[] { holon }, saveChildren, recursive, maxChildDepth);
                for (var index = 0; index < graphModels.Count; index++) graphModels[index] = AddOrUpdateHolon(graphModels[index]);
                holonEntity = graphModels[0];
                await _dbContext.SaveChangesAsync();
                return new OASISResult<IHolon>
                    {IsError = false, Result = GetHolonFromEntity(holonEntity), IsSaved = true};
            }
            catch (Exception ex)
            {
                DetachOwnedFailedWrites(graphModels.Select(model => model.Id));
                return new OASISResult<IHolon>
                {
                    IsError = true,
                    IsSaved = false,
                    Exception = ex, Message = ex.Message
                };
            }
        }

        public OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true,
            bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true)
        {
            var models = new List<HolonModel>();
            try
            {
                var roots = holons.ToList();
                models = PrepareGraph(roots, saveChildren, recursive, maxChildDepth, curentChildDepth);
                var rootIds = new HashSet<string>(roots.Select(root => root.Id.ToString()));
                for (var index = 0; index < models.Count; index++) models[index] = AddOrUpdateHolon(models[index]);

                _dbContext.SaveChanges();

                return new OASISResult<IEnumerable<IHolon>>
                    {IsError = false, Result = models.Where(model => rootIds.Contains(model.Id)).Select(GetHolonFromEntity).ToList(), IsSaved = true};
            }
            catch (Exception ex)
            {
                DetachOwnedFailedWrites(models.Select(model => model.Id));
                return new OASISResult<IEnumerable<IHolon>>
                {
                    IsError = true,
                    IsSaved = false,
                    Exception = ex, Message = ex.Message
                };
            }
        }

        public async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons,
            bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0,
            bool continueOnError = true)
        {
            var models = new List<HolonModel>();
            try
            {
                var roots = holons.ToList();
                models = PrepareGraph(roots, saveChildren, recursive, maxChildDepth, curentChildDepth);
                var rootIds = new HashSet<string>(roots.Select(root => root.Id.ToString()));
                for (var index = 0; index < models.Count; index++) models[index] = AddOrUpdateHolon(models[index]);

                await _dbContext.SaveChangesAsync();

                return new OASISResult<IEnumerable<IHolon>>
                    {IsError = false, Result = models.Where(model => rootIds.Contains(model.Id)).Select(GetHolonFromEntity).ToList(), IsSaved = true};
            }
            catch (Exception ex)
            {
                DetachOwnedFailedWrites(models.Select(model => model.Id));
                return new OASISResult<IEnumerable<IHolon>>
                {
                    IsError = true,
                    IsSaved = false,
                    Exception = ex, Message = ex.Message
                };
            }
        }

        public OASISResult<IHolon> DeleteHolon(Guid id, bool softDelete = true)
        {
            HolonModel holon = null;
            try
            {
                holon = _dbContext.Holons.FirstOrDefault(p => p.Id == id.ToString());
                if (holon != null)
                {
                    if (softDelete)
                    {
                        holon.IsActive = false;
                        holon.DeletedDate = DateTime.UtcNow;
                        _dbContext.Holons.Update(holon);
                    }
                    else
                    {
                        _dbContext.Holons.Remove(holon);
                    }

                    _dbContext.SaveChanges();
                    return new OASISResult<IHolon>
                    {
                        IsDeleted = true,
                        DeletedCount = 1,
                        Result = GetHolonFromEntity(holon),
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
                if (holon != null) _dbContext.Entry(holon).State = EntityState.Detached;
                return new OASISResult<IHolon>
                {
                    IsError = true,
                    Exception = ex,
                    Message = ex.Message
                };
            }
        }

        public async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id, bool softDelete = true)
        {
            HolonModel holon = null;
            try
            {
                holon = await _dbContext.Holons.Where(p => p.Id == id.ToString()).FirstOrDefaultAsync();
                if (holon != null)
                {
                    if (softDelete)
                    {
                        holon.IsActive = false;
                        holon.DeletedDate = DateTime.UtcNow;
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
                        DeletedCount = 1,
                        Result = GetHolonFromEntity(holon)
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
                if (holon != null) _dbContext.Entry(holon).State = EntityState.Detached;
                return new OASISResult<IHolon>
                {
                    IsError = true,
                    Exception = ex,
                    Message = ex.Message
                };
            }
        }

        public OASISResult<IHolon> DeleteHolon(string providerKey, bool softDelete = true)
        {
            HolonModel holonEntity = null;
            try
            {
                holonEntity = _dbContext.Holons
                    .FirstOrDefault(p => p.LocalProviderKey == providerKey);
                
                if (holonEntity != null)
                {
                    if (softDelete)
                    {
                        holonEntity.IsActive = false;
                        holonEntity.DeletedDate = DateTime.UtcNow;
                        _dbContext.Holons.Update(holonEntity);
                    }
                    else
                    {
                        _dbContext.Holons.Remove(holonEntity);
                    }
                    _dbContext.SaveChanges();
                    
                    var holon = GetHolonFromEntity(holonEntity);
                    return new OASISResult<IHolon>
                    {
                        IsError = false,
                        IsDeleted = true,
                        DeletedCount = 1,
                        Message = softDelete ? "Holon soft deleted successfully" : "Holon deleted successfully",
                        Result = holon
                    };
                }
                else
                {
                    return new OASISResult<IHolon>
                    {
                        IsError = true,
                        Message = "Holon not found"
                    };
                }
            }
            catch (Exception ex)
            {
                if (holonEntity != null) _dbContext.Entry(holonEntity).State = EntityState.Detached;
                return new OASISResult<IHolon>
                {
                    IsError = true,
                    Exception = ex,
                    Message = ex.Message
                };
            }
        }

        public async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey, bool softDelete = true)
        {
            HolonModel holonEntity = null;
            try
            {
                holonEntity = await _dbContext.Holons
                    .FirstOrDefaultAsync(p => p.LocalProviderKey == providerKey);
                
                if (holonEntity != null)
                {
                    if (softDelete)
                    {
                        holonEntity.IsActive = false;
                        holonEntity.DeletedDate = DateTime.UtcNow;
                        _dbContext.Holons.Update(holonEntity);
                    }
                    else
                    {
                        _dbContext.Holons.Remove(holonEntity);
                    }
                    await _dbContext.SaveChangesAsync();
                    
                    var holon = GetHolonFromEntity(holonEntity);
                    return new OASISResult<IHolon>
                    {
                        IsError = false,
                        IsDeleted = true,
                        DeletedCount = 1,
                        Message = softDelete ? "Holon soft deleted successfully" : "Holon deleted successfully",
                        Result = holon
                    };
                }
                else
                {
                    return new OASISResult<IHolon>
                    {
                        IsError = true,
                        Message = "Holon not found"
                    };
                }
            }
            catch (Exception ex)
            {
                if (holonEntity != null) _dbContext.Entry(holonEntity).State = EntityState.Detached;
                return new OASISResult<IHolon>
                {
                    IsError = true,
                    Exception = ex,
                    Message = ex.Message
                };
            }
        }

        public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(HolonType Type)
        {
            try
            {
                var holonEntities = _dbContext.Holons
                    .Where(p => p.HolonType == Type)
                    .ToList()
                    .Select(GetHolonFromEntity)
                    .ToList();
                
                return new OASISResult<IEnumerable<IHolon>>
                {
                    IsLoaded = true,
                    IsError = false,
                    Message = $"Loaded {holonEntities.Count} holons of type {Type}",
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

        private IHolon GetHolonFromEntity(HolonModel holonEntity) =>
            new Holon()
            {
                CreatedDate = holonEntity.CreatedDate,
                CreatedByAvatarId = holonEntity.CreatedByAvatarId == null ? Guid.Empty : Guid.Parse(holonEntity.CreatedByAvatarId),
                ModifiedByAvatarId = holonEntity.ModifiedByAvatarId == null ? Guid.Empty : Guid.Parse(holonEntity.ModifiedByAvatarId),
                DeletedByAvatarId = holonEntity.DeletedByAvatarId == null ? Guid.Empty : Guid.Parse(holonEntity.DeletedByAvatarId),
                DimensionLevel = holonEntity.DimensionLevel,
                SubDimensionLevel = holonEntity.SubDimensionLevel,
                DeletedDate = holonEntity.DeletedDate,
                Description = holonEntity.Description,
                ParentHolonId = Guid.Parse(holonEntity.ParentHolonId),
                Id = Guid.Parse(holonEntity.Id),
                IsActive = holonEntity.IsActive,
                IsChanged = holonEntity.IsChanged,
                ModifiedDate = holonEntity.ModifiedDate,
                Name = holonEntity.Name,
                Version = holonEntity.Version,
                HolonType = holonEntity.HolonType,
                MetaData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string,object>>(holonEntity.MetaDataJson)
                    ?? throw new System.IO.InvalidDataException("Stored holon metadata must be a JSON object."),
                ProviderUniqueStorageKey = System.Text.Json.JsonSerializer.Deserialize<Dictionary<ProviderType,string>>(holonEntity.ProviderKeysJson)
                    ?? throw new System.IO.InvalidDataException("Stored provider keys must be a JSON object.")
            };

        private HolonModel CreateHolonModel(IHolon holon)
        {
            var id = holon.Id == Guid.Empty ? Guid.NewGuid() : holon.Id;
            var keys = holon.ProviderUniqueStorageKey == null ? new Dictionary<ProviderType,string>() : new Dictionary<ProviderType,string>(holon.ProviderUniqueStorageKey);
            if (!keys.ContainsKey(ProviderType.SQLLiteDBOASIS)) keys[ProviderType.SQLLiteDBOASIS] = id.ToString();
            ArgumentException.ThrowIfNullOrWhiteSpace(keys[ProviderType.SQLLiteDBOASIS]);
            return new()
            {
                CreatedDate = holon.CreatedDate,
                CreatedByAvatarId = holon.CreatedByAvatarId.ToString(),
                ModifiedByAvatarId = holon.ModifiedByAvatarId.ToString(),
                DeletedByAvatarId = holon.DeletedByAvatarId.ToString(),
                DimensionLevel = holon.DimensionLevel,
                SubDimensionLevel = holon.SubDimensionLevel,
                DeletedDate = holon.DeletedDate,
                Description = holon.Description,
                ParentHolonId = holon.ParentHolonId.ToString(),
                Id = id.ToString(),
                IsActive = holon.IsActive,
                IsChanged = holon.IsChanged,
                ModifiedDate = holon.ModifiedDate,
                Name = holon.Name,
                Version = holon.Version,
                HolonType = holon.HolonType,
                MetaDataJson = System.Text.Json.JsonSerializer.Serialize(holon.MetaData ?? new Dictionary<string,object>()),
                LocalProviderKey = keys[ProviderType.SQLLiteDBOASIS],
                ProviderKeysJson = System.Text.Json.JsonSerializer.Serialize(keys)
            };
        }

        // Public OASIS Id is the persistence identity. Provider keys and lifecycle/audit
        // hints are not available to stateless REST or JavaScript callers.
        private HolonModel AddOrUpdateHolon(HolonModel holon)
        {
            var tracked = _dbContext.Holons.Local.SingleOrDefault(existing => existing.Id == holon.Id);
            if (tracked != null)
            {
                holon.CreatedDate = tracked.CreatedDate;
                holon.CreatedByAvatarId = tracked.CreatedByAvatarId;
                _dbContext.Entry(tracked).CurrentValues.SetValues(holon);
                return tracked;
            }
            HolonModel persistedHolon = _dbContext.Holons
                .AsNoTracking()
                .FirstOrDefault(existingHolon => existingHolon.Id == holon.Id);

            if (persistedHolon == null)
            {
                _dbContext.Holons.Add(holon);
                return holon;
            }

            // A full replacement may omit immutable creation audit data. Preserve the
            // persisted value rather than treating its presence as a create signal.
            holon.CreatedDate = persistedHolon.CreatedDate;
            holon.CreatedByAvatarId = persistedHolon.CreatedByAvatarId;
            _dbContext.Holons.Update(holon);
            return holon;
        }

        private void DetachOwnedFailedWrites(IEnumerable<string> ids)
        {
            var owned = ids.ToHashSet(StringComparer.Ordinal);
            foreach (var entry in _dbContext.ChangeTracker.Entries<HolonModel>()
                .Where(entry => owned.Contains(entry.Entity.Id) && (entry.State == EntityState.Added || entry.State == EntityState.Modified)).ToList())
                entry.State = EntityState.Detached;
        }
    }
}
