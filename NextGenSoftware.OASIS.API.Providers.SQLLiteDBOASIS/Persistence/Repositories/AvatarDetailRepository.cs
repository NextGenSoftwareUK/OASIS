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
                        .Select(x => GetAvatarDetailFromEntity(x))
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
                        .Select(x => GetAvatarDetailFromEntity(x))
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
                        .Select(x => GetAvatarDetailFromEntity(x))
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
                var obj = await _dbContext.AvatarDetails.Where(x => x.Id == id && x.Version == version)
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
                    Result = (IAvatarDetail) obj
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
                var avatarDetailEntity = await _dbContext.AvatarDetails
                    .Where(x => x.Username == avatarUsername && x.Version == version)
                    .Select(x => GetAvatarDetailFromEntity(x))
                    .FirstOrDefaultAsync();
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
                var avatarDetailEntity = await _dbContext.AvatarDetails
                    .Where(x => x.Email == avatarEmail && x.Version == version)
                    .Select(x => GetAvatarDetailFromEntity(x))
                    .FirstOrDefaultAsync();
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
                var avatarDetailEntities = _dbContext.AvatarDetails
                    .Where(x => x.Version == version)
                    .Select(x => GetAvatarDetailFromEntity(x))
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
                    await _dbContext.AvatarDetails
                        .Where(x => x.Version == version)
                        .Select(x => GetAvatarDetailFromEntity(x))
                        .ToListAsync();
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
                AvatarDetailEntity avatarDetail = CreateAvatarDetailModel(avatarDetailEntity);
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
                AvatarDetailEntity avatarDetail = CreateAvatarDetailModel(AvatarDetailEntity);
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

        private IAvatarDetail GetAvatarDetailFromEntity(AvatarDetailEntity avatarDetailEntity) =>
            new AvatarDetail()
            {
                Address = avatarDetailEntity.Address,
                Country = avatarDetailEntity.Country,
                Description = "",
                DOB = avatarDetailEntity.DOB,
                Email = avatarDetailEntity.Email,
                Id = avatarDetailEntity.Id,
                Portrait = avatarDetailEntity.Portrait,
                Karma = avatarDetailEntity.Karma,
                Landline = avatarDetailEntity.Landline,
                DimensionLevel = avatarDetailEntity.DimensionLevel,
                Mobile = avatarDetailEntity.Mobile,
                Postcode = avatarDetailEntity.Postcode,
                Town = avatarDetailEntity.Town,
                UmaJson = avatarDetailEntity.UmaJson,
                Username = avatarDetailEntity.Username,
                XP = avatarDetailEntity.XP,
                CreatedByAvatarId = avatarDetailEntity.CreatedByAvatarId,
                CreatedDate = avatarDetailEntity.CreatedDate,
                DeletedByAvatarId = avatarDetailEntity.DeletedByAvatarId,
                DeletedDate = avatarDetailEntity.DeletedDate,
                FavouriteColour = avatarDetailEntity.FavouriteColour,
                IsActive = avatarDetailEntity.IsActive,
                IsChanged = avatarDetailEntity.IsChanged,
                ModifiedByAvatarId = avatarDetailEntity.ModifiedByAvatarId,
                ModifiedDate = avatarDetailEntity.ModifiedDate,
                ParentCelestialBodyId = avatarDetailEntity.ParentCelestialBodyId,
                ParentCelestialSpaceId = avatarDetailEntity.ParentCelestialSpaceId,
                ParentDimensionId = avatarDetailEntity.ParentDimensionId,
                ParentGalaxyClusterId = avatarDetailEntity.ParentGalaxyClusterId,
                ParentGalaxyId = avatarDetailEntity.ParentGalaxyId,
                ParentGrandSuperStarId = avatarDetailEntity.ParentGrandSuperStarId,
                ParentGreatGrandSuperStarId = avatarDetailEntity.ParentGreatGrandSuperStarId,
                ParentHolonId = avatarDetailEntity.ParentHolonId,
                ParentMoonId = avatarDetailEntity.ParentMoonId,
                ParentMultiverseId = avatarDetailEntity.ParentMultiverseId,
                ParentOmniverseId = avatarDetailEntity.ParentOmniverseId,
                ParentPlanetId = avatarDetailEntity.ParentPlanetId,
                ParentSolarSystemId = avatarDetailEntity.ParentSolarSystemId,
                ParentStarId = avatarDetailEntity.ParentStarId,
                ParentSuperStarId = avatarDetailEntity.ParentSuperStarId,
                ParentUniverseId = avatarDetailEntity.ParentUniverseId,
                ParentZomeId = avatarDetailEntity.ParentZomeId,
                PreviousVersionId = avatarDetailEntity.PreviousVersionId,
                STARCLIColour = avatarDetailEntity.STARCLIColour,
                Version = avatarDetailEntity.Version,
                Achievements = avatarDetailEntity.Achievements,
                Attributes = avatarDetailEntity.Attributes,
                Aura = avatarDetailEntity.Aura,
                Chakras = avatarDetailEntity.Chakras,
                Children = avatarDetailEntity.Children,
                County = avatarDetailEntity.County,
                Gifts = avatarDetailEntity.Gifts,
                Inventory = avatarDetailEntity.Inventory,
                Nodes = avatarDetailEntity.Nodes,
                Omniverse = avatarDetailEntity.Omniverse
            };

        private AvatarDetailEntity CreateAvatarDetailModel(IAvatarDetail avatarDetail)
        {
            return new()
            {
                Address = avatarDetail.Address,
                Country = avatarDetail.Country,
                Description = "",
                DOB = avatarDetail.DOB,
                Email = avatarDetail.Email,
                Id = avatarDetail.Id,
                Portrait = avatarDetail.Portrait,
                Karma = avatarDetail.Karma,
                Landline = avatarDetail.Landline,
                Level = 1,
                Mobile = avatarDetail.Mobile,
                Postcode = avatarDetail.Postcode,
                Town = avatarDetail.Town,
                UmaJson = avatarDetail.UmaJson,
                Username = avatarDetail.Username,
                XP = avatarDetail.XP,
                CreatedByAvatarId = avatarDetail.CreatedByAvatarId,
                CreatedDate = avatarDetail.CreatedDate,
                DeletedByAvatarId = avatarDetail.DeletedByAvatarId,
                DeletedDate = avatarDetail.DeletedDate,
                FavouriteColour = avatarDetail.FavouriteColour,
                IsActive = avatarDetail.IsActive,
                IsChanged = avatarDetail.IsChanged,
                ModifiedByAvatarId = avatarDetail.ModifiedByAvatarId,
                ModifiedDate = avatarDetail.ModifiedDate,
                ParentCelestialBodyId = avatarDetail.ParentCelestialBodyId,
                ParentCelestialSpaceId = avatarDetail.ParentCelestialSpaceId,
                ParentDimensionId = avatarDetail.ParentDimensionId,
                ParentGalaxyClusterId = avatarDetail.ParentGalaxyClusterId,
                ParentGalaxyId = avatarDetail.ParentGalaxyId,
                ParentGrandSuperStarId = avatarDetail.ParentGrandSuperStarId,
                ParentGreatGrandSuperStarId = avatarDetail.ParentGreatGrandSuperStarId,
                ParentHolonId = avatarDetail.ParentHolonId,
                ParentMoonId = avatarDetail.ParentMoonId,
                ParentMultiverseId = avatarDetail.ParentMultiverseId,
                ParentOmniverseId = avatarDetail.ParentOmniverseId,
                ParentPlanetId = avatarDetail.ParentPlanetId,
                ParentSolarSystemId = avatarDetail.ParentSolarSystemId,
                ParentStarId = avatarDetail.ParentStarId,
                ParentSuperStarId = avatarDetail.ParentSuperStarId,
                ParentUniverseId = avatarDetail.ParentUniverseId,
                ParentZomeId = avatarDetail.ParentZomeId,
                PreviousVersionId = avatarDetail.PreviousVersionId,
                STARCLIColour = avatarDetail.STARCLIColour,
                Version = avatarDetail.Version,
                Achievements = avatarDetail.Achievements,
                Attributes = avatarDetail.Attributes,
                Aura = avatarDetail.Aura,
                Chakras = avatarDetail.Chakras,
                Children = avatarDetail.Children,
                County = avatarDetail.County,
                Gifts = avatarDetail.Gifts,
                Inventory = avatarDetail.Inventory,
                Nodes = avatarDetail.Nodes,
                Omniverse = avatarDetail.Omniverse
            };
        }
    }
}