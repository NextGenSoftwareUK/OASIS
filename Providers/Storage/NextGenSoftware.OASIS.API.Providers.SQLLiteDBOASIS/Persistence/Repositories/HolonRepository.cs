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
        private readonly Func<bool> _versionControlEnabled;

        public HolonRepository(DataContext dbContext)
            :this(dbContext,()=>false) { }

        public HolonRepository(DataContext dbContext,Func<bool> versionControlEnabled)
        {
            _dbContext = dbContext;
            _versionControlEnabled=versionControlEnabled ?? throw new ArgumentNullException(nameof(versionControlEnabled));
        }

        public OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true,
            int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            try
            {
                SqliteReadVersion.Validate(version);
                ValidateReadTraversal(maxChildDepth, 0);
                var row = version==0 ? _dbContext.Holons.AsNoTracking().FirstOrDefault(p => p.Id == id.ToString()) : null;
                var holonEntity = version>0 ? ReadHolonRevision(id,version) : row == null ? null : GetHolonFromEntity(row);
                if (holonEntity != null && loadChildren)
                    AttachLoadedChildren(holonEntity, _dbContext.Holons.AsNoTracking().ToList(), recursive, maxChildDepth);
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
                SqliteReadVersion.Validate(version);
                ValidateReadTraversal(maxChildDepth, 0);
                var row = version==0 ? await _dbContext.Holons.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id.ToString()) : null;
                var holonEntity = version>0 ? ReadHolonRevision(id,version) : row == null ? null : GetHolonFromEntity(row);
                if (holonEntity != null && loadChildren)
                    AttachLoadedChildren(holonEntity, await _dbContext.Holons.AsNoTracking().ToListAsync(), recursive, maxChildDepth);
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
                SqliteReadVersion.Validate(version);
                ValidateReadTraversal(maxChildDepth, 0);
                ArgumentException.ThrowIfNullOrWhiteSpace(providerKey);
                var holonEntities = version>0 ? ReadHolonRevision(providerKey,version) : _dbContext.Holons.AsNoTracking()
                    .Where(p => p.LocalProviderKey == providerKey)
                    .ToList()
                    .Select(GetHolonFromEntity)
                    .SingleOrDefault();
                if (holonEntities != null && loadChildren)
                    AttachLoadedChildren(holonEntities, _dbContext.Holons.AsNoTracking().ToList(), recursive, maxChildDepth);
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
                SqliteReadVersion.Validate(version);
                ValidateReadTraversal(maxChildDepth, 0);
                ArgumentException.ThrowIfNullOrWhiteSpace(providerKey);
                var stored = version==0 ? await _dbContext.Holons.AsNoTracking().SingleOrDefaultAsync(p => p.LocalProviderKey == providerKey) : null;
                var holonEntity = version>0 ? ReadHolonRevision(providerKey,version) : stored == null ? null : GetHolonFromEntity(stored);
                if (holonEntity != null && loadChildren)
                    AttachLoadedChildren(holonEntity, await _dbContext.Holons.AsNoTracking().ToListAsync(), recursive, maxChildDepth);
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
                SqliteReadVersion.Validate(version);
                ValidateReadTraversal(maxChildDepth, curentChildDepth);
                var holonEntities = version>0 ? ReadHolonRevisions(version,type,id) :
                    _dbContext.Holons.AsNoTracking()
                        .Where(p => p.ParentHolonId == id.ToString() && (version == 0 || p.Version == version) && (type == HolonType.All || p.HolonType == type))
                        .ToList()
                        .Select(GetHolonFromEntity)
                        .ToList();
                if (loadChildren && recursive)
                {
                    var graphRows = _dbContext.Holons.AsNoTracking().ToList();
                    foreach (var root in holonEntities) AttachLoadedChildren(root, graphRows, recursive, maxChildDepth, (long)curentChildDepth + 1);
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
                SqliteReadVersion.Validate(version);
                ValidateReadTraversal(maxChildDepth, curentChildDepth);
                var rows = version==0 ? await _dbContext.Holons.AsNoTracking()
                    .Where(p => p.ParentHolonId == id.ToString() && (version == 0 || p.Version == version) && (type == HolonType.All || p.HolonType == type))
                    .ToListAsync() : new List<HolonModel>();
                var holonEntities = version>0 ? ReadHolonRevisions(version,type,id) : rows
                    .Select(GetHolonFromEntity)
                    .ToList();
                if (loadChildren && recursive)
                {
                    var graphRows = await _dbContext.Holons.AsNoTracking().ToListAsync();
                    foreach (var root in holonEntities) AttachLoadedChildren(root, graphRows, recursive, maxChildDepth, (long)curentChildDepth + 1);
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
                SqliteReadVersion.Validate(version);
                ValidateReadTraversal(maxChildDepth, curentChildDepth);
                var holonEntities = version>0 ? ReadHistoricalChildren(providerKey,version,type) : _dbContext.Holons.AsNoTracking()
                    .Where(p => _dbContext.Holons.AsNoTracking().Any(parent => parent.LocalProviderKey == providerKey && parent.Id == p.ParentHolonId)
                        && (version == 0 || p.Version == version) && (type == HolonType.All || p.HolonType == type))
                    .ToList()
                    .Select(GetHolonFromEntity)
                    .ToList();
                if (loadChildren && recursive)
                {
                    var graphRows = _dbContext.Holons.AsNoTracking().ToList();
                    foreach (var root in holonEntities) AttachLoadedChildren(root, graphRows, recursive, maxChildDepth, (long)curentChildDepth + 1);
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
                SqliteReadVersion.Validate(version);
                ValidateReadTraversal(maxChildDepth, curentChildDepth);
                var rows = version==0 ? await _dbContext.Holons.AsNoTracking()
                    .Where(p => _dbContext.Holons.AsNoTracking().Any(parent => parent.LocalProviderKey == providerKey && parent.Id == p.ParentHolonId)
                        && (version == 0 || p.Version == version) && (type == HolonType.All || p.HolonType == type))
                    .ToListAsync() : new List<HolonModel>();
                var holonEntities = version>0 ? ReadHistoricalChildren(providerKey,version,type) : rows.Select(GetHolonFromEntity).ToList();
                if (loadChildren && recursive)
                {
                    var graphRows = await _dbContext.Holons.AsNoTracking().ToListAsync();
                    foreach (var root in holonEntities) AttachLoadedChildren(root, graphRows, recursive, maxChildDepth, (long)curentChildDepth + 1);
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
                SqliteReadVersion.Validate(version);
                ValidateReadTraversal(maxChildDepth, curentChildDepth);
                var holonEntities = version>0 ? ReadHolonRevisions(version,type) : _dbContext.Holons.AsNoTracking()
                    .Where(p => type == HolonType.All || p.HolonType == type)
                    .ToList()
                    .Select(GetHolonFromEntity)
                    .ToList();
                if (loadChildren)
                {
                    var graphRows = _dbContext.Holons.AsNoTracking().ToList();
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
                SqliteReadVersion.Validate(version);
                ValidateReadTraversal(maxChildDepth, curentChildDepth);
                var rows = version==0 ? await _dbContext.Holons.AsNoTracking()
                    .Where(p => type == HolonType.All || p.HolonType == type).ToListAsync() : new List<HolonModel>();
                var holonEntities = version>0 ? ReadHolonRevisions(version,type) : rows.Select(GetHolonFromEntity).ToList();
                if (loadChildren)
                {
                    var graphRows = await _dbContext.Holons.AsNoTracking().ToListAsync();
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
            var batch=SaveOrdinaryGraph(new[]{holon},saveChildren,recursive,maxChildDepth);
            var result=NextGenSoftware.OASIS.API.Core.Helpers.OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<IHolon>,IHolon>(batch);
            if(!batch.IsError && batch.Result!=null)result.Result=batch.Result.Single();
            return result;
        }

        // Microsoft.Data.Sqlite native I/O is synchronous; both surfaces share one transaction owner.
        public Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true,
            bool recursive = true, int maxChildDepth = 0, bool continueOnError = true)
            =>Task.FromResult(SaveHolon(holon,saveChildren,recursive,maxChildDepth,continueOnError));

        public OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true,
            bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true)
            =>SaveOrdinaryGraph(holons,saveChildren,recursive,maxChildDepth,curentChildDepth);

        public Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons,
            bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0,
            bool continueOnError = true)
            =>Task.FromResult(SaveHolons(holons,saveChildren,recursive,maxChildDepth,curentChildDepth,continueOnError));
        private Exception HolonSaveFailure(List<HolonModel> models, bool persistenceEntered, Exception cause,bool includeUnchanged=false)
        {
            try { DetachOwnedFailedWrites(models.Select(model => model.Id),includeUnchanged); }
            catch (Exception cleanup) { cause = new AggregateException("SQLite failure cleanup also failed.", cause, cleanup); }
            return persistenceEntered
                ? new SqliteMutationReconciliationException(models.Select(model => Guid.Parse(model.Id)), cause)
                : cause;
        }

        public OASISResult<IHolon> DeleteHolon(Guid id, bool softDelete = true)
            =>DeleteOrdinaryHolon(id,null,softDelete);

        public Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id, bool softDelete = true)
            =>Task.FromResult(DeleteHolon(id,softDelete));

        public OASISResult<IHolon> DeleteHolon(string providerKey, bool softDelete = true)
            =>DeleteOrdinaryHolon(null,providerKey,softDelete);

        public Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey, bool softDelete = true)
            =>Task.FromResult(DeleteHolon(providerKey,softDelete));
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

        private IHolon GetHolonFromEntity(HolonModel holonEntity)
        {
            var mapped = new Holon()
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
                IsPublic = holonEntity.IsPublic,
                VersionId = holonEntity.VersionId,
                ProviderMetaData = new Dictionary<ProviderType, Dictionary<string, string>> {
                    [ProviderType.SQLLiteDBOASIS] = new Dictionary<string, string> {
                        [CosmicConcurrency.ProviderRevisionMetadataKey] = holonEntity.ProviderRevisionId.ToString("D") } },
                PreviousVersionId = holonEntity.PreviousVersionId,
                CustomHolonType = holonEntity.CustomHolonType,
                CreatedProviderType = holonEntity.HasCreatedProviderType ? new NextGenSoftware.Utilities.EnumValue<ProviderType>(holonEntity.CreatedProviderType):null,
                CreatedOASISType = holonEntity.HasCreatedOASISType ? new NextGenSoftware.Utilities.EnumValue<OASISType>(holonEntity.CreatedOASISType):null,
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
            return RestoreHolonPersistentValues(holonEntity,mapped);
        }

        private HolonModel CreateHolonModel(IHolon holon,Guid? plannedId=null)
        {
            var id = plannedId ?? (holon.Id == Guid.Empty ? Guid.NewGuid() : holon.Id);
            var keys = holon.ProviderUniqueStorageKey == null ? new Dictionary<ProviderType,string>() : new Dictionary<ProviderType,string>(holon.ProviderUniqueStorageKey);
            if (!keys.ContainsKey(ProviderType.SQLLiteDBOASIS)) keys[ProviderType.SQLLiteDBOASIS] = id.ToString();
            ArgumentException.ThrowIfNullOrWhiteSpace(keys[ProviderType.SQLLiteDBOASIS]);
            return new()
            {
                PersistentValueJson = CaptureHolonPersistentValues(holon),
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
                IsPublic = holon.IsPublic,
                VersionId = holon.VersionId,
                PreviousVersionId = holon.PreviousVersionId,
                CustomHolonType = holon.CustomHolonType,
                CreatedProviderType = holon.CreatedProviderType?.Value ?? default,
                HasCreatedProviderType = holon.CreatedProviderType!=null,
                CreatedOASISType = holon.CreatedOASISType?.Value ?? default,
                HasCreatedOASISType = holon.CreatedOASISType!=null,
                IsChanged = holon.IsChanged,
                ModifiedDate = holon.ModifiedDate,
                Name = holon.Name,
                Version = holon.Version,
                HolonType = holon.HolonType,
                MetaDataJson = NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.Serialize(holon.MetaData ?? new Dictionary<string,object>()),
                LocalProviderKey = keys[ProviderType.SQLLiteDBOASIS],
                ProviderKeysJson = System.Text.Json.JsonSerializer.Serialize(keys)
            };
        }

        // Public OASIS Id is the persistence identity. Provider keys and lifecycle/audit
        // hints are not available to stateless REST or JavaScript callers.
        private HolonModel AddOrUpdateHolon(HolonModel holon,bool committedReplica=false,Action<HolonModel> capturePrevious=null)
        {
            // Local concurrency state is never supplied by callers or copied from another authority.
            holon.ProviderRevisionId = Guid.NewGuid();
            if (holon.VersionId == Guid.Empty) holon.VersionId = Guid.NewGuid();
            var tracked = _dbContext.Holons.Local.SingleOrDefault(existing => existing.Id == holon.Id);
            HolonModel persistedHolon = _dbContext.Holons
                .AsNoTracking()
                .FirstOrDefault(existingHolon => existingHolon.Id == holon.Id);

            if (persistedHolon == null)
            {
                if (tracked != null)
                {
                    _dbContext.Entry(tracked).CurrentValues.SetValues(holon);
                    _dbContext.Entry(tracked).State = EntityState.Added;
                    return tracked;
                }
                _dbContext.Holons.Add(holon);
                return holon;
            }

            // A full replacement may omit immutable creation audit data. Preserve the
            // persisted value rather than treating its presence as a create signal.
            capturePrevious?.Invoke(persistedHolon);
            if(!committedReplica)
            {
                holon.CreatedDate = persistedHolon.CreatedDate;
                holon.CreatedByAvatarId = persistedHolon.CreatedByAvatarId;
                holon.CreatedProviderType = persistedHolon.CreatedProviderType;
                holon.HasCreatedProviderType = persistedHolon.HasCreatedProviderType;
                holon.CreatedOASISType = persistedHolon.CreatedOASISType;
                holon.HasCreatedOASISType = persistedHolon.HasCreatedOASISType;
            }
            // Logical version/lineage belongs to the committed content. Preserve it on replication.
            if (tracked != null)
            {
                _dbContext.Entry(tracked).CurrentValues.SetValues(holon);
                return tracked;
            }
            _dbContext.Holons.Update(holon);
            return holon;
        }

        private void DetachOwnedFailedWrites(IEnumerable<string> ids,bool includeUnchanged=false)
        {
            var owned = ids.ToHashSet(StringComparer.Ordinal);
            foreach (var entry in _dbContext.ChangeTracker.Entries<HolonModel>()
                .Where(entry => owned.Contains(entry.Entity.Id) && (includeUnchanged || entry.State == EntityState.Added || entry.State == EntityState.Modified || entry.State == EntityState.Deleted)).ToList())
                entry.State = EntityState.Detached;
        }
    }
}
