using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Interfaces;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories
{
    public partial class AvatarDetailRepository : IAvatarDetailRepository
    {
        private readonly DataContext _dbContext;

        public AvatarDetailRepository(DataContext dbContext)
        {
            _dbContext = dbContext;
        }

        // Load the two independent collections without a cartesian inventory ×
        // heart-rate join. One-to-one components are centrally AutoIncluded.
        private IQueryable<AvatarDetailModel> DetailState() => _dbContext.AvatarDetails.AsSplitQuery()
            .Include(a=>a.InventoryItems).Include(a=>a.HeartRates);

        private IAvatarDetail MapUniqueAvatarDetail(List<AvatarDetailModel> rows)
        {
            if (rows.Count > 1) throw new StorageIdentityAmbiguousException();
            return rows.Count == 0 ? null : GetAvatarDetailFromEntity(rows[0]);
        }

        public OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
        {
            try
            {
                SqliteReadVersion.Validate(version);
                var avatarDetailEntity =
                    DetailState().AsNoTracking()
                        .Where(x => x.Id == id.ToString() && (version == 0 || x.Version == version))
                        .ToList()
                        .Select(GetAvatarDetailFromEntity)
                        .FirstOrDefault();
                if (avatarDetailEntity == null)
                    return new OASISResult<IAvatarDetail>
                    {
                        IsLoaded = false,
                        IsError = false,
                        Message = "No Avatar Detail Found"
                    };
                return new OASISResult<IAvatarDetail>
                {
                    IsLoaded = true,
                    IsError = false,
                    Message = "Avatar Detail Loaded successfully",
                    Result = avatarDetailEntity
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatarDetail>
                {
                    IsLoaded = false,
                    IsError = true,
                    Exception = ex,
                    ErrorCode = ex is StorageIdentityAmbiguousException ? OASISResultErrorCodes.AmbiguousIdentity : null,
                    Message = ex.ToString()
                };
            }
        }

        public OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0)
        {
            try
            {
                SqliteReadVersion.Validate(version);
                var rows = DetailState().AsNoTracking()
                        .Where(x => x.Email == avatarEmail && (version == 0 || x.Version == version))
                        .Take(2).ToList();
                var avatarDetailEntity = MapUniqueAvatarDetail(rows);
                if (avatarDetailEntity == null)
                    return new OASISResult<IAvatarDetail>
                    {
                        IsLoaded = false,
                        IsError = false,
                        Message = "No Avatar Detail Found"
                    };
                return new OASISResult<IAvatarDetail>
                {
                    IsLoaded = true,
                    IsError = false,
                    Message = "Avatar Detail Loaded successfully",
                    Result = avatarDetailEntity
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatarDetail>
                {
                    IsLoaded = false,
                    IsError = true,
                    Exception = ex,
                    ErrorCode = ex is StorageIdentityAmbiguousException ? OASISResultErrorCodes.AmbiguousIdentity : null,
                    Message = ex.ToString()
                };
            }
        }

        public OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0)
        {
            try
            {
                SqliteReadVersion.Validate(version);
                var rows = DetailState().AsNoTracking()
                        .Where(x => x.Username == avatarUsername && (version == 0 || x.Version == version))
                        .Take(2).ToList();
                var avatarDetailEntity = MapUniqueAvatarDetail(rows);
                if (avatarDetailEntity == null)
                    return new OASISResult<IAvatarDetail>
                    {
                        IsLoaded = false,
                        IsError = false,
                        Message = "No Avatar Detail Found"
                    };
                return new OASISResult<IAvatarDetail>
                {
                    IsLoaded = true,
                    IsError = false,
                    Message = "Avatar Detail Loaded successfully",
                    Result = avatarDetailEntity
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatarDetail>
                {
                    IsLoaded = false,
                    IsError = true,
                    Exception = ex,
                    ErrorCode = ex is StorageIdentityAmbiguousException ? OASISResultErrorCodes.AmbiguousIdentity : null,
                    Message = ex.ToString()
                };
            }
        }

        public async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            try
            {
                SqliteReadVersion.Validate(version);
                var obj = await DetailState().AsNoTracking()
                    .Where(x => x.Id == id.ToString() && (version == 0 || x.Version == version))
                    .FirstOrDefaultAsync();
                if (obj == null)
                    return new OASISResult<IAvatarDetail>
                    {
                        IsLoaded = false,
                        IsError = false,
                        Message = "No Avatar Detail Found"
                    };
                return new OASISResult<IAvatarDetail>
                {
                    IsLoaded = true,
                    IsError = false,
                    Message = "Avatar Detail Loaded successfully",
                    Result = GetAvatarDetailFromEntity(obj)
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatarDetail>
                {
                    IsLoaded = false,
                    IsError = true,
                    Exception = ex,
                    ErrorCode = ex is StorageIdentityAmbiguousException ? OASISResultErrorCodes.AmbiguousIdentity : null,
                    Message = ex.ToString()
                };
            }
        }

        public async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername,
            int version = 0)
        {
            try
            {
                SqliteReadVersion.Validate(version);
                var rows = await DetailState().AsNoTracking()
                    .Where(x => x.Username == avatarUsername && (version == 0 || x.Version == version))
                    .Take(2).ToListAsync();
                var avatarDetailEntity = MapUniqueAvatarDetail(rows);
                if (avatarDetailEntity == null)
                    return new OASISResult<IAvatarDetail>
                    {
                        IsLoaded = false,
                        IsError = false,
                        Message = "No Avatar Detail Found"
                    };
                return new OASISResult<IAvatarDetail>
                {
                    IsLoaded = true,
                    IsError = false,
                    Message = "Avatar Detail Loaded successfully",
                    Result = avatarDetailEntity
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatarDetail>
                {
                    IsLoaded = false,
                    IsError = true,
                    Exception = ex,
                    ErrorCode = ex is StorageIdentityAmbiguousException ? OASISResultErrorCodes.AmbiguousIdentity : null,
                    Message = ex.ToString()
                };
            }
        }

        public async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
        {
            try
            {
                SqliteReadVersion.Validate(version);
                var rows = await DetailState().AsNoTracking()
                    .Where(x => x.Email == avatarEmail && (version == 0 || x.Version == version))
                    .Take(2).ToListAsync();
                var avatarDetailEntity = MapUniqueAvatarDetail(rows);
                if (avatarDetailEntity == null)
                    return new OASISResult<IAvatarDetail>
                    {
                        IsLoaded = false,
                        IsError = false,
                        Message = "No Avatar Detail Found"
                    };
                return new OASISResult<IAvatarDetail>
                {
                    IsLoaded = true,
                    IsError = false,
                    Message = "Avatar Detail Loaded successfully",
                    Result = avatarDetailEntity
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatarDetail>
                {
                    IsLoaded = false,
                    IsError = true,
                    Exception = ex,
                    ErrorCode = ex is StorageIdentityAmbiguousException ? OASISResultErrorCodes.AmbiguousIdentity : null,
                    Message = ex.ToString()
                };
            }
        }

        public OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
        {
            try
            {
                SqliteReadVersion.Validate(version);
                var avatarDetailEntities = 
                    DetailState().AsNoTracking()
                    .Where(x => (version == 0 || x.Version == version))
                    .ToList()
                    .Select(GetAvatarDetailFromEntity)
                    .ToList();
                return new OASISResult<IEnumerable<IAvatarDetail>>
                {
                    IsLoaded = true,
                    IsError = false,
                    Message = "Avatar Detail Loaded successfully",
                    Result = avatarDetailEntities
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<IEnumerable<IAvatarDetail>>
                {
                    IsLoaded = false,
                    IsError = true,
                    Exception = ex,
                    ErrorCode = ex is StorageIdentityAmbiguousException ? OASISResultErrorCodes.AmbiguousIdentity : null,
                    Message = ex.ToString()
                };
            }
        }

        public async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            try
            {
                SqliteReadVersion.Validate(version);
                var rows = await DetailState().AsNoTracking()
                    .Where(x => version == 0 || x.Version == version)
                    .ToListAsync();
                var avatarDetailEntities = rows.Select(GetAvatarDetailFromEntity).ToList();
                return new OASISResult<IEnumerable<IAvatarDetail>>
                {
                    IsLoaded = true,
                    IsError = false,
                    Message = "Avatar Detail Loaded successfully",
                    Result = avatarDetailEntities
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<IEnumerable<IAvatarDetail>>
                {
                    IsLoaded = false,
                    IsError = true,
                    Exception = ex,
                    ErrorCode = ex is StorageIdentityAmbiguousException ? OASISResultErrorCodes.AmbiguousIdentity : null,
                    Message = ex.ToString()
                };
            }
        }

        public OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetailEntity)
        {
            var persistenceEntered=false;
            var recordId=Guid.Empty;
            try
            {
                ArgumentNullException.ThrowIfNull(avatarDetailEntity);
                recordId=avatarDetailEntity.Id==Guid.Empty ? Guid.NewGuid():avatarDetailEntity.Id;
                var idStr = recordId.ToString();
                var existing = DetailState()
                    .FirstOrDefault(x => x.Id == idStr);

                if (existing != null)
                {
                    existing.Address = avatarDetailEntity.Address;
                    existing.Country = avatarDetailEntity.Country;
                    existing.Description = avatarDetailEntity.Description;
                    existing.DOB = avatarDetailEntity.DOB;
                    existing.Email = avatarDetailEntity.Email;
                    existing.Karma = avatarDetailEntity.Karma;
                    existing.Landline = avatarDetailEntity.Landline;
                    existing.Mobile = avatarDetailEntity.Mobile;
                    existing.Postcode = avatarDetailEntity.Postcode;
                    existing.Town = avatarDetailEntity.Town;
                    existing.Username = avatarDetailEntity.Username;
                    existing.XP = avatarDetailEntity.XP;
                    existing.MetaDataJson = System.Text.Json.JsonSerializer.Serialize(avatarDetailEntity.MetaData ?? new Dictionary<string,object>());
                    existing.ProviderLinksJson = AvatarDetailProviderLinks.Encode(avatarDetailEntity);
                    existing.DimensionLevelIdsJson = AvatarDetailDimensionLinks.Encode(avatarDetailEntity);
                    SetComponents(existing, avatarDetailEntity);
                    SetHeartRates(existing, avatarDetailEntity);
                    existing.Model3D = avatarDetailEntity.Model3D;
                    existing.Portrait = avatarDetailEntity.Portrait;
                    existing.UmaJson = avatarDetailEntity.UmaJson;
                    existing.DeletedByAvatarId = avatarDetailEntity.DeletedByAvatarId.ToString();
                    existing.DeletedDate = avatarDetailEntity.DeletedDate;
                    existing.FavouriteColour = avatarDetailEntity.FavouriteColour;
                    existing.IsActive = avatarDetailEntity.IsActive;
                    existing.IsPublic = avatarDetailEntity.IsPublic;
                    existing.VersionId = avatarDetailEntity.VersionId;
                    existing.PreviousVersionId = avatarDetailEntity.PreviousVersionId;
                    existing.ParentHolonId = avatarDetailEntity.ParentHolonId;
                    existing.ParentHolonId2 = avatarDetailEntity.ParentHolonId2;
                    existing.ParentHolonId3 = avatarDetailEntity.ParentHolonId3;
                    existing.CustomHolonType = avatarDetailEntity.CustomHolonType;
                    existing.IsChanged = avatarDetailEntity.IsChanged;
                    existing.ModifiedByAvatarId = avatarDetailEntity.ModifiedByAvatarId.ToString();
                    existing.ModifiedDate = avatarDetailEntity.ModifiedDate;
                    existing.STARCLIColour = avatarDetailEntity.STARCLIColour;
                    existing.Version = avatarDetailEntity.Version;
                    existing.County = avatarDetailEntity.County;
                    existing.ActiveQuestId = avatarDetailEntity.ActiveQuestId;
                    existing.ActiveObjectiveId = avatarDetailEntity.ActiveObjectiveId;

                    var toRemoveSync = existing.InventoryItems.ToList();
                    _dbContext.RemoveRange(toRemoveSync);
                    existing.InventoryItems.Clear();
                    if (avatarDetailEntity.Inventory != null)
                    {
                        foreach (var item in avatarDetailEntity.Inventory)
                        {
                            var model = new InventoryItemModel(item) { AvatarId = existing.Id };
                            existing.InventoryItems.Add(model);
                        }
                    }

                    persistenceEntered=true;
                    _dbContext.SaveChanges();
                avatarDetailEntity.Id=recordId;
                    return new OASISResult<IAvatarDetail>
                    {
                        IsSaved = true,
                        IsError = false,
                        Result = GetAvatarDetailFromEntity(existing),
                        Message = existing.Username + " Record updated successfully"
                    };
                }

                var newModel = new AvatarDetailModel(avatarDetailEntity,recordId);
                _dbContext.AvatarDetails.Add(newModel);
                persistenceEntered=true;
                _dbContext.SaveChanges();
                avatarDetailEntity.Id=recordId;
                return new OASISResult<IAvatarDetail>
                {
                    IsSaved = true,
                    IsError = false,
                    Result = GetAvatarDetailFromEntity(newModel),
                    Message = newModel.Username + " Record saved successfully"
                };
            }
            catch (Exception ex)
            {
                var cause=DetailSaveFailure(recordId,persistenceEntered,ex);
                return new OASISResult<IAvatarDetail>
                {
                    IsSaved = false,
                    IsError = true,
                    Exception = cause,
                    Message = cause.Message
                };
            }
        }

        public async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail AvatarDetailEntity)
        {
            var persistenceEntered=false;
            var recordId=Guid.Empty;
            try
            {
                ArgumentNullException.ThrowIfNull(AvatarDetailEntity);
                recordId=AvatarDetailEntity.Id==Guid.Empty ? Guid.NewGuid():AvatarDetailEntity.Id;
                var idStr = recordId.ToString();
                var existing = await DetailState()
                    .FirstOrDefaultAsync(x => x.Id == idStr);

                if (existing != null)
                {
                    // Update existing: persist scalars and replace inventory so Quantity updates are saved
                    existing.Address = AvatarDetailEntity.Address;
                    existing.Country = AvatarDetailEntity.Country;
                    existing.Description = AvatarDetailEntity.Description;
                    existing.DOB = AvatarDetailEntity.DOB;
                    existing.Email = AvatarDetailEntity.Email;
                    existing.Karma = AvatarDetailEntity.Karma;
                    existing.Landline = AvatarDetailEntity.Landline;
                    existing.Mobile = AvatarDetailEntity.Mobile;
                    existing.Postcode = AvatarDetailEntity.Postcode;
                    existing.Town = AvatarDetailEntity.Town;
                    existing.Username = AvatarDetailEntity.Username;
                    existing.XP = AvatarDetailEntity.XP;
                    existing.MetaDataJson = System.Text.Json.JsonSerializer.Serialize(AvatarDetailEntity.MetaData ?? new Dictionary<string,object>());
                    existing.ProviderLinksJson = AvatarDetailProviderLinks.Encode(AvatarDetailEntity);
                    existing.DimensionLevelIdsJson = AvatarDetailDimensionLinks.Encode(AvatarDetailEntity);
                    SetComponents(existing, AvatarDetailEntity);
                    SetHeartRates(existing, AvatarDetailEntity);
                    existing.Model3D = AvatarDetailEntity.Model3D;
                    existing.Portrait = AvatarDetailEntity.Portrait;
                    existing.UmaJson = AvatarDetailEntity.UmaJson;
                    existing.DeletedByAvatarId = AvatarDetailEntity.DeletedByAvatarId.ToString();
                    existing.DeletedDate = AvatarDetailEntity.DeletedDate;
                    existing.FavouriteColour = AvatarDetailEntity.FavouriteColour;
                    existing.IsActive = AvatarDetailEntity.IsActive;
                    existing.IsPublic = AvatarDetailEntity.IsPublic;
                    existing.VersionId = AvatarDetailEntity.VersionId;
                    existing.PreviousVersionId = AvatarDetailEntity.PreviousVersionId;
                    existing.ParentHolonId = AvatarDetailEntity.ParentHolonId;
                    existing.ParentHolonId2 = AvatarDetailEntity.ParentHolonId2;
                    existing.ParentHolonId3 = AvatarDetailEntity.ParentHolonId3;
                    existing.CustomHolonType = AvatarDetailEntity.CustomHolonType;
                    existing.IsChanged = AvatarDetailEntity.IsChanged;
                    existing.ModifiedByAvatarId = AvatarDetailEntity.ModifiedByAvatarId.ToString();
                    existing.ModifiedDate = AvatarDetailEntity.ModifiedDate;
                    existing.STARCLIColour = AvatarDetailEntity.STARCLIColour;
                    existing.Version = AvatarDetailEntity.Version;
                    existing.County = AvatarDetailEntity.County;
                    existing.ActiveQuestId = AvatarDetailEntity.ActiveQuestId;
                    existing.ActiveObjectiveId = AvatarDetailEntity.ActiveObjectiveId;

                    var toRemove = existing.InventoryItems.ToList();
                    _dbContext.RemoveRange(toRemove);
                    existing.InventoryItems.Clear();
                    if (AvatarDetailEntity.Inventory != null)
                    {
                        foreach (var item in AvatarDetailEntity.Inventory)
                        {
                            var model = new InventoryItemModel(item) { AvatarId = existing.Id };
                            existing.InventoryItems.Add(model);
                        }
                    }

                    persistenceEntered=true;
                    await _dbContext.SaveChangesAsync();
                AvatarDetailEntity.Id=recordId;
                    return new OASISResult<IAvatarDetail>
                    {
                        IsSaved = true,
                        IsError = false,
                        Result = GetAvatarDetailFromEntity(existing),
                        Message = existing.Username + " Record updated successfully"
                    };
                }

                var newModel = new AvatarDetailModel(AvatarDetailEntity,recordId);
                _dbContext.AvatarDetails.Add(newModel);
                persistenceEntered=true;
                await _dbContext.SaveChangesAsync();
                AvatarDetailEntity.Id=recordId;
                return new OASISResult<IAvatarDetail>
                {
                    IsSaved = true,
                    IsError = false,
                    Result = GetAvatarDetailFromEntity(newModel),
                    Message = newModel.Username + " Record saved successfully"
                };
            }
            catch (Exception ex)
            {
                var cause=DetailSaveFailure(recordId,persistenceEntered,ex);
                return new OASISResult<IAvatarDetail>
                {
                    IsSaved = false,
                    IsError = true,
                    Exception = cause,
                    Message = cause.Message
                };
            }
        }

        private Exception DetailSaveFailure(Guid id,bool persistenceEntered,Exception cause)
        {
            try {DetachFailedDetailWrites(id.ToString());}
            catch(Exception cleanup) {cause=new AggregateException("SQLite failure cleanup also failed.",cause,cleanup);}
            return persistenceEntered ? new SqliteMutationReconciliationException(id,cause):cause;
        }

        private void DetachFailedDetailWrites(string id)
        {
            foreach (var entry in _dbContext.ChangeTracker.Entries().ToList())
            {
                var belongsToAvatar = entry.Entity is AvatarDetailModel detail && detail.Id == id;
                var avatarKey = entry.Metadata.FindProperty("AvatarId");
                if (avatarKey != null && avatarKey.ClrType == typeof(string))
                    belongsToAvatar |= (string)entry.Property("AvatarId").CurrentValue == id
                        || (string)entry.Property("AvatarId").OriginalValue == id;
                if (belongsToAvatar && (entry.State == EntityState.Added || entry.State == EntityState.Modified || entry.State == EntityState.Deleted))
                    entry.State = EntityState.Detached;
            }
        }

        private void SetStats(AvatarDetailModel existing, IAvatarDetail source)
        {
            if (source.Stats == null)
            {
                if (existing.Stats != null) _dbContext.Remove(existing.Stats);
                existing.Stats = null;
                return;
            }
            var incoming = new AvatarStatsModel(source.Stats) { Id = existing.Id, AvatarId = existing.Id };
            if (existing.Stats == null) existing.Stats = incoming;
            else _dbContext.Entry(existing.Stats).CurrentValues.SetValues(incoming);
        }

        private void SetHeartRates(AvatarDetailModel existing,IAvatarDetail source)
        {
            // Stage the complete replacement before mutating tracked rows.
            var incoming=(source.HeartRateData ?? new List<Core.Interfaces.Avatar.IHeartRateEntry>())
                .Select((entry,index)=>new HeartRateEntryModel(entry) {AvatarId=existing.Id,SequenceIndex=index}).ToList();
            _dbContext.RemoveRange(existing.HeartRates);
            existing.HeartRates.Clear();
            existing.HeartRates.AddRange(incoming);
        }

        private void SetComponent<T>(T current,T incoming,Action<T> assign) where T:class
        {
            if(incoming==null)
            {
                if(current!=null) _dbContext.Remove(current);
                assign(null);
            }
            else if(current==null) assign(incoming);
            else _dbContext.Entry(current).CurrentValues.SetValues(incoming);
        }

        private void SetComponents(AvatarDetailModel existing,IAvatarDetail source)
        {
            SetStats(existing,source);
            SetComponent(existing.Attributes,source.Attributes==null ? null:new AvatarAttributesModel(source.Attributes) {AvatarId=existing.Id},value=>existing.Attributes=value);
            SetComponent(existing.Aura,source.Aura==null ? null:new AvatarAuraModel(source.Aura) {AvatarId=existing.Id},value=>existing.Aura=value);
            SetComponent(existing.Skills,source.Skills==null ? null:new AvatarSkillsModel(source.Skills) {AvatarId=existing.Id},value=>existing.Skills=value);
            SetComponent(existing.SuperPowers,source.SuperPowers==null ? null:new AvatarSuperPowersModel(source.SuperPowers) {AvatarId=existing.Id},value=>existing.SuperPowers=value);
            SetComponent(existing.HumanDesign,source.HumanDesign==null ? null:new AvatarHumanDesignModel(source.HumanDesign) {Id=existing.Id,AvatarId=existing.Id},value=>existing.HumanDesign=value);
        }

        private IAvatarDetail GetAvatarDetailFromEntity(AvatarDetailModel avatarDetailEntity)
        {
            var result=new AvatarDetail()
            {
                Address = avatarDetailEntity.Address,
                Country = avatarDetailEntity.Country,
                Description = avatarDetailEntity.Description,
                DOB = avatarDetailEntity.DOB,
                Email = avatarDetailEntity.Email,
                Id = Guid.Parse(avatarDetailEntity.Id),
                Karma = avatarDetailEntity.Karma,
                Landline = avatarDetailEntity.Landline,
                Mobile = avatarDetailEntity.Mobile,
                Postcode = avatarDetailEntity.Postcode,
                Town = avatarDetailEntity.Town,
                Username = avatarDetailEntity.Username,
                XP = avatarDetailEntity.XP,
                Model3D = avatarDetailEntity.Model3D,
                Portrait = avatarDetailEntity.Portrait,
                UmaJson = avatarDetailEntity.UmaJson,
                HeartRateData = avatarDetailEntity.HeartRates.OrderBy(row=>row.SequenceIndex).ThenBy(row=>row.Id)
                    .Select(row=>(Core.Interfaces.Avatar.IHeartRateEntry)row.GetHeartRateEntry()).ToList(),
                MetaData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string,object>>(avatarDetailEntity.MetaDataJson)
                    ?? throw new System.IO.InvalidDataException("Stored avatar detail metadata must be a JSON object."),
                Stats = avatarDetailEntity.Stats?.GetAvatarStats(),
                CreatedByAvatarId = Guid.Parse(avatarDetailEntity.CreatedByAvatarId),
                CreatedDate = avatarDetailEntity.CreatedDate,
                CreatedProviderType = avatarDetailEntity.CreatedProviderType.HasValue
                    ? new NextGenSoftware.Utilities.EnumValue<NextGenSoftware.OASIS.API.Core.Enums.ProviderType>(avatarDetailEntity.CreatedProviderType.Value) : null,
                CreatedOASISType = avatarDetailEntity.CreatedOASISType.HasValue
                    ? new NextGenSoftware.Utilities.EnumValue<NextGenSoftware.OASIS.API.Core.Enums.OASISType>(avatarDetailEntity.CreatedOASISType.Value) : null,
                DeletedByAvatarId = Guid.Parse(avatarDetailEntity.DeletedByAvatarId),
                DeletedDate = avatarDetailEntity.DeletedDate,
                FavouriteColour = avatarDetailEntity.FavouriteColour,
                IsActive = avatarDetailEntity.IsActive,
                IsPublic = avatarDetailEntity.IsPublic,
                VersionId = avatarDetailEntity.VersionId,
                PreviousVersionId = avatarDetailEntity.PreviousVersionId,
                ParentHolonId = avatarDetailEntity.ParentHolonId,
                ParentHolonId2 = avatarDetailEntity.ParentHolonId2,
                ParentHolonId3 = avatarDetailEntity.ParentHolonId3,
                CustomHolonType = avatarDetailEntity.CustomHolonType,
                IsChanged = avatarDetailEntity.IsChanged,
                ModifiedByAvatarId = Guid.Parse(avatarDetailEntity.ModifiedByAvatarId),
                ModifiedDate = avatarDetailEntity.ModifiedDate,
                STARCLIColour = avatarDetailEntity.STARCLIColour,
                Version = avatarDetailEntity.Version,
                Attributes = avatarDetailEntity.Attributes?.GetAvatarAttributes(),
                Aura = avatarDetailEntity.Aura?.GetAvatarAura(),
                Skills = avatarDetailEntity.Skills?.GetAvatarSkills(),
                SuperPowers = avatarDetailEntity.SuperPowers?.GetAvatarSuperPowers(),
                HumanDesign = avatarDetailEntity.HumanDesign?.GetHumanDesign(),
                County = avatarDetailEntity.County,
                ActiveQuestId = avatarDetailEntity.ActiveQuestId,
                ActiveObjectiveId = avatarDetailEntity.ActiveObjectiveId,
                Inventory = avatarDetailEntity.InventoryItems == null
                    ? new List<IInventoryItem>()
                    : avatarDetailEntity.InventoryItems.Select(m => (IInventoryItem)m.GetInventoryItem()).ToList(),
            };

            AvatarDetailProviderLinks.Apply(avatarDetailEntity.ProviderLinksJson,result);
            result.DimensionLevelIds=AvatarDetailDimensionLinks.Decode(avatarDetailEntity.DimensionLevelIdsJson);
            return result;
        }

        private AvatarDetailModel CreateAvatarDetailModel(IAvatarDetail avatarDetail)
        {
            var model = new AvatarDetailModel()
            {
                ProviderLinksJson = AvatarDetailProviderLinks.Encode(avatarDetail),
                DimensionLevelIdsJson = AvatarDetailDimensionLinks.Encode(avatarDetail),
                Address = avatarDetail.Address,
                Country = avatarDetail.Country,
                Description = avatarDetail.Description,
                DOB = avatarDetail.DOB,
                Email = avatarDetail.Email,
                Id = avatarDetail.Id.ToString(),
                Karma = avatarDetail.Karma,
                Landline = avatarDetail.Landline,
                Level = 1,
                Mobile = avatarDetail.Mobile,
                Postcode = avatarDetail.Postcode,
                Town = avatarDetail.Town,
                Username = avatarDetail.Username,
                XP = avatarDetail.XP,
                Portrait = avatarDetail.Portrait,
                UmaJson = avatarDetail.UmaJson,
                CreatedByAvatarId = avatarDetail.CreatedByAvatarId.ToString(),
                CreatedDate = avatarDetail.CreatedDate,
                DeletedByAvatarId = avatarDetail.DeletedByAvatarId.ToString(),
                DeletedDate = avatarDetail.DeletedDate,
                FavouriteColour = avatarDetail.FavouriteColour,
                IsActive = avatarDetail.IsActive,
                IsPublic = avatarDetail.IsPublic,
                VersionId = avatarDetail.VersionId,
                PreviousVersionId = avatarDetail.PreviousVersionId,
                ParentHolonId = avatarDetail.ParentHolonId,
                ParentHolonId2 = avatarDetail.ParentHolonId2,
                ParentHolonId3 = avatarDetail.ParentHolonId3,
                CustomHolonType = avatarDetail.CustomHolonType,
                CreatedProviderType = avatarDetail.CreatedProviderType?.Value,
                CreatedOASISType = avatarDetail.CreatedOASISType?.Value,
                IsChanged = avatarDetail.IsChanged,
                ModifiedByAvatarId = avatarDetail.ModifiedByAvatarId.ToString(),
                ModifiedDate = avatarDetail.ModifiedDate,
                STARCLIColour = avatarDetail.STARCLIColour,
                Version = avatarDetail.Version,
                County = avatarDetail.County,
                ActiveQuestId = avatarDetail.ActiveQuestId,
                ActiveObjectiveId = avatarDetail.ActiveObjectiveId
            };
            if (avatarDetail.Inventory != null)
            {
                foreach (var item in avatarDetail.Inventory)
                {
                    var invModel = new InventoryItemModel(item) { AvatarId = model.Id };
                    model.InventoryItems.Add(invModel);
                }
            }
            return model;
        }
    }
}
