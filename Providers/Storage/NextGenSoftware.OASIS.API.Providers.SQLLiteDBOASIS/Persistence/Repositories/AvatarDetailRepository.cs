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
    public class AvatarDetailRepository : IAvatarDetailRepository
    {
        private readonly DataContext _dbContext;

        public AvatarDetailRepository(DataContext dbContext)
        {
            _dbContext = dbContext;
        }

        public OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
        {
            try
            {
                var avatarDetailEntity =
                    _dbContext.AvatarDetails
                        .Include(a => a.InventoryItems)
                        .ToList()
                        .Select(GetAvatarDetailFromEntity)
                        .FirstOrDefault(x => x.Id == id && x.Version == version);
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
                    Message = ex.ToString()
                };
            }
        }

        public OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0)
        {
            try
            {
                var avatarDetailEntity =
                    _dbContext.AvatarDetails
                        .Include(a => a.InventoryItems)
                        .ToList()
                        .Select(GetAvatarDetailFromEntity)
                        .FirstOrDefault(x => x.Email == avatarEmail && x.Version == version);
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
                    Message = ex.ToString()
                };
            }
        }

        public OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0)
        {
            try
            {
                var avatarDetailEntity =
                    _dbContext.AvatarDetails
                        .Include(a => a.InventoryItems)
                        .ToList()
                        .Select(GetAvatarDetailFromEntity)
                        .FirstOrDefault(x => x.Username == avatarUsername && x.Version == version);
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
                    Message = ex.ToString()
                };
            }
        }

        public async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            try
            {
                var obj = await _dbContext.AvatarDetails
                    .Include(a => a.InventoryItems)
                    .Where(x => x.Id == id.ToString() && x.Version == version)
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
                    Message = ex.ToString()
                };
            }
        }

        public async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername,
            int version = 0)
        {
            try
            {
                var avatarDetailEntity = _dbContext.AvatarDetails
                    .Include(a => a.InventoryItems)
                    .ToList()
                    .Where(x => x.Username == avatarUsername && x.Version == version)
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
                    Message = ex.ToString()
                };
            }
        }

        public async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
        {
            try
            {
                var avatarDetailEntity = _dbContext.AvatarDetails
                    .Include(a => a.InventoryItems)
                    .ToList()
                    .Where(x => x.Email == avatarEmail && x.Version == version)
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
                    Message = ex.ToString()
                };
            }
        }

        public OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
        {
            try
            {
                var avatarDetailEntities = 
                    _dbContext.AvatarDetails
                    .Include(a => a.InventoryItems)
                    .ToList()
                    .Where(x => x.Version == version)
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
                    Message = ex.ToString()
                };
            }
        }

        public async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            try
            {
                var avatarDetailEntities =
                    _dbContext.AvatarDetails
                        .Include(a => a.InventoryItems)
                        .ToList()
                        .Where(x => x.Version == version)
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
                    Message = ex.ToString()
                };
            }
        }

        public OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetailEntity)
        {
            try
            {
                var idStr = avatarDetailEntity.Id.ToString();
                var existing = _dbContext.AvatarDetails
                    .Include(a => a.InventoryItems)
                    .FirstOrDefault(x => x.Id == idStr && x.Version == avatarDetailEntity.Version);

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
                    existing.CreatedByAvatarId = avatarDetailEntity.CreatedByAvatarId.ToString();
                    existing.CreatedDate = avatarDetailEntity.CreatedDate;
                    existing.DeletedByAvatarId = avatarDetailEntity.DeletedByAvatarId.ToString();
                    existing.DeletedDate = avatarDetailEntity.DeletedDate;
                    existing.FavouriteColour = avatarDetailEntity.FavouriteColour;
                    existing.IsActive = avatarDetailEntity.IsActive;
                    existing.IsChanged = avatarDetailEntity.IsChanged;
                    existing.ModifiedByAvatarId = avatarDetailEntity.ModifiedByAvatarId.ToString();
                    existing.ModifiedDate = avatarDetailEntity.ModifiedDate;
                    existing.STARCLIColour = avatarDetailEntity.STARCLIColour;
                    existing.Version = avatarDetailEntity.Version;
                    existing.County = avatarDetailEntity.County;

                    var toRemoveSync = existing.InventoryItems.ToList();
                    _dbContext.RemoveRange(toRemoveSync);
                    existing.InventoryItems.Clear();
                    if (avatarDetailEntity.Inventory != null)
                    {
                        foreach (var item in avatarDetailEntity.Inventory)
                        {
                            var model = new InventoryItemModel((InventoryItem)item) { AvatarId = existing.Id };
                            existing.InventoryItems.Add(model);
                        }
                    }

                    _dbContext.SaveChanges();
                    return new OASISResult<IAvatarDetail>
                    {
                        IsSaved = true,
                        IsError = false,
                        Message = existing.Username + " Record updated successfully"
                    };
                }

                var newModel = new AvatarDetailModel(avatarDetailEntity);
                _dbContext.AvatarDetails.Add(newModel);
                _dbContext.SaveChanges();
                return new OASISResult<IAvatarDetail>
                {
                    IsSaved = true,
                    IsError = false,
                    Message = newModel.Username + " Record saved successfully"
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatarDetail>
                {
                    IsSaved = false,
                    IsError = true,
                    Message = ex.ToString()
                };
            }
        }

        public async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail AvatarDetailEntity)
        {
            try
            {
                var idStr = AvatarDetailEntity.Id.ToString();
                var existing = await _dbContext.AvatarDetails
                    .Include(a => a.InventoryItems)
                    .FirstOrDefaultAsync(x => x.Id == idStr && x.Version == AvatarDetailEntity.Version);

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
                    existing.CreatedByAvatarId = AvatarDetailEntity.CreatedByAvatarId.ToString();
                    existing.CreatedDate = AvatarDetailEntity.CreatedDate;
                    existing.DeletedByAvatarId = AvatarDetailEntity.DeletedByAvatarId.ToString();
                    existing.DeletedDate = AvatarDetailEntity.DeletedDate;
                    existing.FavouriteColour = AvatarDetailEntity.FavouriteColour;
                    existing.IsActive = AvatarDetailEntity.IsActive;
                    existing.IsChanged = AvatarDetailEntity.IsChanged;
                    existing.ModifiedByAvatarId = AvatarDetailEntity.ModifiedByAvatarId.ToString();
                    existing.ModifiedDate = AvatarDetailEntity.ModifiedDate;
                    existing.STARCLIColour = AvatarDetailEntity.STARCLIColour;
                    existing.Version = AvatarDetailEntity.Version;
                    existing.County = AvatarDetailEntity.County;

                    var toRemove = existing.InventoryItems.ToList();
                    _dbContext.RemoveRange(toRemove);
                    existing.InventoryItems.Clear();
                    if (AvatarDetailEntity.Inventory != null)
                    {
                        foreach (var item in AvatarDetailEntity.Inventory)
                        {
                            var model = new InventoryItemModel((InventoryItem)item) { AvatarId = existing.Id };
                            existing.InventoryItems.Add(model);
                        }
                    }

                    await _dbContext.SaveChangesAsync();
                    return new OASISResult<IAvatarDetail>
                    {
                        IsSaved = true,
                        IsError = false,
                        Message = existing.Username + " Record updated successfully"
                    };
                }

                var newModel = new AvatarDetailModel(AvatarDetailEntity);
                _dbContext.AvatarDetails.Add(newModel);
                await _dbContext.SaveChangesAsync();
                return new OASISResult<IAvatarDetail>
                {
                    IsSaved = true,
                    IsError = false,
                    Message = newModel.Username + " Record saved successfully"
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatarDetail>
                {
                    IsSaved = false,
                    IsError = true,
                    Message = ex.ToString()
                };
            }
        }

        private IAvatarDetail GetAvatarDetailFromEntity(AvatarDetailModel avatarDetailEntity) =>
            new AvatarDetail()
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
                CreatedByAvatarId = Guid.Parse(avatarDetailEntity.CreatedByAvatarId),
                CreatedDate = avatarDetailEntity.CreatedDate,
                DeletedByAvatarId = Guid.Parse(avatarDetailEntity.DeletedByAvatarId),
                DeletedDate = avatarDetailEntity.DeletedDate,
                FavouriteColour = avatarDetailEntity.FavouriteColour,
                IsActive = avatarDetailEntity.IsActive,
                IsChanged = avatarDetailEntity.IsChanged,
                ModifiedByAvatarId = Guid.Parse(avatarDetailEntity.ModifiedByAvatarId),
                ModifiedDate = avatarDetailEntity.ModifiedDate,
                STARCLIColour = avatarDetailEntity.STARCLIColour,
                Version = avatarDetailEntity.Version,
                Attributes = avatarDetailEntity.Attributes,
                Aura = avatarDetailEntity.Aura,
                County = avatarDetailEntity.County,
                Inventory = avatarDetailEntity.InventoryItems == null
                    ? new List<IInventoryItem>()
                    : avatarDetailEntity.InventoryItems.Select(m => (IInventoryItem)m.GetInventoryItem()).ToList(),
            };

        private AvatarDetailModel CreateAvatarDetailModel(IAvatarDetail avatarDetail)
        {
            var model = new AvatarDetailModel()
            {
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
                CreatedByAvatarId = avatarDetail.CreatedByAvatarId.ToString(),
                CreatedDate = avatarDetail.CreatedDate,
                DeletedByAvatarId = avatarDetail.DeletedByAvatarId.ToString(),
                DeletedDate = avatarDetail.DeletedDate,
                FavouriteColour = avatarDetail.FavouriteColour,
                IsActive = avatarDetail.IsActive,
                IsChanged = avatarDetail.IsChanged,
                ModifiedByAvatarId = avatarDetail.ModifiedByAvatarId.ToString(),
                ModifiedDate = avatarDetail.ModifiedDate,
                STARCLIColour = avatarDetail.STARCLIColour,
                Version = avatarDetail.Version,
                County = avatarDetail.County
            };
            if (avatarDetail.Inventory != null)
            {
                foreach (var item in avatarDetail.Inventory)
                {
                    var invModel = new InventoryItemModel((InventoryItem)item) { AvatarId = model.Id };
                    model.InventoryItems.Add(invModel);
                }
            }
            return model;
        }
    }
}