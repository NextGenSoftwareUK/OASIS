using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
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
                var avatarDetail = CreateAvatarDetailModel(avatarDetailEntity);
                _dbContext.AvatarDetails.Add(avatarDetail);
                _dbContext.SaveChanges();
                return new OASISResult<IAvatarDetail>
                {
                    IsSaved = true,
                    IsError = false,
                    Message = avatarDetail.Username + " Record saved successfully"
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
                var avatarDetail = CreateAvatarDetailModel(AvatarDetailEntity);
                _dbContext.AvatarDetails.Add(avatarDetail);
                await _dbContext.SaveChangesAsync();
                return new OASISResult<IAvatarDetail>
                {
                    IsSaved = true,
                    IsError = false,
                    Message = avatarDetail.Username + " Record saved successfully"
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
            };

        private AvatarDetailModel CreateAvatarDetailModel(IAvatarDetail avatarDetail)
        {
            return new()
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
        }
    }
}