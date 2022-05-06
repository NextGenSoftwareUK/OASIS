using NextGenSoftware.OASIS.API.Providers.SQLLiteOASIS.Exceptions;
using NextGenSoftware.OASIS.API.Providers.SQLLiteOASIS.Interfaces;
using NextGenSoftware.OASIS.API.Providers.SQLLiteOASIS.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.SQLLiteOASIS.Entities;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteOASIS.Persistence.Repositories
{
    public class AvtarDetailRepository : IAvtarDetailRepository
    {
        private readonly DataContext eFContext;

        public AvtarDetailRepository(DataContext eFContext)
        {
            this.eFContext = eFContext;
        }

        public OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
        {
            try
            {
                var obj = this.eFContext.AvatarDetailEntities.Where(x => x.Id == id && x.Version == version).FirstOrDefault();
                if(obj == null)
                {
                    return new OASISResult<IAvatarDetail>
                    {
                        IsLoaded = false,
                        IsError = false,
                        Message = "No Avatar Detail Found",
                    };
                }
                else
                {
                    return new OASISResult<IAvatarDetail>
                    {
                        IsLoaded = true,
                        IsError = false,
                        Message = "Avatar Detail Loaded successfully",
                        Result = (IAvatarDetail)obj,
                    };
                }
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatarDetail>
                {
                    IsLoaded = false,
                    IsError = true,
                    Message = ex.ToString(),
                };
            }
        }

        public OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0)
        {
            try
            {
                var obj = this.eFContext.AvatarDetailEntities.Where(x => x.Email == avatarEmail && x.Version == version).FirstOrDefault();
                if (obj == null)
                {
                    return new OASISResult<IAvatarDetail>
                    {
                        IsLoaded = false,
                        IsError = false,
                        Message = "No Avatar Detail Found",
                    };
                }
                else
                {
                    return new OASISResult<IAvatarDetail>
                    {
                        IsLoaded = true,
                        IsError = false,
                        Message = "Avatar Detail Loaded successfully",
                        Result = (IAvatarDetail)obj,
                    };
                }
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatarDetail>
                {
                    IsLoaded = false,
                    IsError = true,
                    Message = ex.ToString(),
                };
            }
        }

        public OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0)
        {
            try
            {
                var obj= this.eFContext.AvatarDetailEntities.Where(x => x.Username == avatarUsername && x.Version == version).FirstOrDefault();
                if (obj == null)
                {
                    return new OASISResult<IAvatarDetail>
                    {
                        IsLoaded = false,
                        IsError = false,
                        Message = "No Avatar Detail Found",
                    };
                }
                else
                {
                    return new OASISResult<IAvatarDetail>
                    {
                        IsLoaded = true,
                        IsError = false,
                        Message = "Avatar Detail Loaded successfully",
                        Result = (IAvatarDetail)obj,
                    };
                }
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatarDetail>
                {
                    IsLoaded = false,
                    IsError = true,
                    Message = ex.ToString(),
                };
            }
        }

        public async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            try
            {
                var obj= await this.eFContext.AvatarDetailEntities.Where(x => x.Id == id && x.Version == version).FirstOrDefaultAsync();
                if (obj == null)
                {
                    return new OASISResult<IAvatarDetail>
                    {
                        IsLoaded = false,
                        IsError = false,
                        Message = "No Avatar Detail Found",
                    };
                }
                else
                {
                    return new OASISResult<IAvatarDetail>
                    {
                        IsLoaded = true,
                        IsError = false,
                        Message = "Avatar Detail Loaded successfully",
                        Result = (IAvatarDetail)obj,
                    };
                }
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatarDetail>
                {
                    IsLoaded = false,
                    IsError = true,
                    Message = ex.ToString(),
                };
            }
        }

        public async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
        {
            try
            {
                var obj= await this.eFContext.AvatarDetailEntities.Where(x => x.Username == avatarUsername && x.Version == version).FirstOrDefaultAsync();
                if (obj == null)
                {
                    return new OASISResult<IAvatarDetail>
                    {
                        IsLoaded = false,
                        IsError = false,
                        Message = "No Avatar Detail Found",
                    };
                }
                else
                {
                    return new OASISResult<IAvatarDetail>
                    {
                        IsLoaded = true,
                        IsError = false,
                        Message = "Avatar Detail Loaded successfully",
                        Result = (IAvatarDetail)obj,
                    };
                }
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatarDetail>
                {
                    IsLoaded = false,
                    IsError = true,
                    Message = ex.ToString(),
                };
            }
        }

        public async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
        {
            try
            {
                var obj= await this.eFContext.AvatarDetailEntities.Where(x => x.Email == avatarEmail && x.Version == version).FirstOrDefaultAsync();
                if (obj == null)
                {
                    return new OASISResult<IAvatarDetail>
                    {
                        IsLoaded = false,
                        IsError = false,
                        Message = "No Avatar Detail Found",
                    };
                }
                else
                {
                    return new OASISResult<IAvatarDetail>
                    {
                        IsLoaded = true,
                        IsError = false,
                        Message = "Avatar Detail Loaded successfully",
                        Result = (IAvatarDetail)obj,
                    };
                }
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatarDetail>
                {
                    IsLoaded = false,
                    IsError = true,
                    Message = ex.ToString(),
                };
            }
        }

        public OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
        {
            try
            {
                var obj = this.eFContext.AvatarDetailEntities.Where(x => x.Version == version).ToList();
                return new OASISResult<IEnumerable<IAvatarDetail>>
                {
                    IsLoaded = true,
                    IsError = false,
                    Message = "Avatar Detail Loaded successfully",
                    Result = (IEnumerable<IAvatarDetail>)obj,
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<IEnumerable<IAvatarDetail>>
                {
                    IsLoaded = false,
                    IsError = true,
                    Message = ex.ToString(),
                };
            }
        }

        public async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            try
            {
                var obj= await this.eFContext.AvatarDetailEntities.Where(x => x.Version == version).ToListAsync();
                return new OASISResult<IEnumerable<IAvatarDetail>>
                {
                    IsLoaded = true,
                    IsError = false,
                    Message = "Avatar Detail Loaded successfully",
                    Result = (IEnumerable<IAvatarDetail>)obj,
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<IEnumerable<IAvatarDetail>>
                {
                    IsLoaded = false,
                    IsError = true,
                    Message = ex.ToString(),
                };
            }
        }

        public OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail AvatarDetailEntity)
        {
            try
            {
                AvatarDetailEntity avatarDetail = CreateAvatarDetailModel(AvatarDetailEntity);
                this.eFContext.AvatarDetailEntities.Add(avatarDetail);
                this.eFContext.SaveChanges();
                return new OASISResult<IAvatarDetail>
                {
                    IsSaved = true,
                    IsError = false,
                    Message = avatarDetail.Username + " Record saved successfully",
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatarDetail>
                {
                    IsSaved = false,
                    IsError = true,
                    Message = ex.ToString(),
                };
            }
        }

        public async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail AvatarDetailEntity)
        {
            try
            {
                AvatarDetailEntity avatarDetail = CreateAvatarDetailModel(AvatarDetailEntity);
                this.eFContext.AvatarDetailEntities.Add(avatarDetail);
                await this.eFContext.SaveChangesAsync();
                return new OASISResult<IAvatarDetail>
                {
                    IsSaved = true,
                    IsError = false,
                    Message = avatarDetail.Username + " Record saved successfully",
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatarDetail>
                {
                    IsSaved = false,
                    IsError = true,
                    Message = ex.ToString(),
                };
            }
        }

        public AvatarDetailEntity CreateAvatarDetailModel(IAvatarDetail AvatarDetail)
        {
            AvatarDetailEntity avatarDetail = new AvatarDetailEntity();
            avatarDetail.Address = AvatarDetail.Address == null ? "": AvatarDetail.Address;
            avatarDetail.Country = AvatarDetail.Country == null ? "" : AvatarDetail.Country;
            //avatarDetail.CreatedByAvatarId = AvatarDetail.CreatedByAvatarId;
            //avatarDetail.CreatedDate = AvatarDetail.CreatedDate;
            //avatarDetail.DeletedByAvatarId = AvatarDetail.DeletedByAvatarId;
            //avatarDetail.DeletedDate = AvatarDetail.DeletedDate;
            avatarDetail.Description = "";//AvatarDetail.Description == null ?"":AvatarDetail.Description;
            avatarDetail.DOB = AvatarDetail.DOB;
            avatarDetail.Email = AvatarDetail.Email;
            //avatarDetail.FavouriteColour = AvatarDetail.FavouriteColour;
            //avatarDetail.HolonId = AvatarDetail.HolonId;
            avatarDetail.Id = AvatarDetail.Id;
            avatarDetail.Portrait = AvatarDetail.Portrait == null ?"": AvatarDetail.Portrait;
            //avatarDetail.IsActive = AvatarDetail.IsActive;
            //avatarDetail.IsChanged = AvatarDetail.IsChanged;
            avatarDetail.Karma = AvatarDetail.Karma;
            avatarDetail.Landline = AvatarDetail.Landline == null ?"": AvatarDetail.Landline;
            avatarDetail.Level = 1;
            avatarDetail.Mobile = AvatarDetail.Mobile == null ? "": AvatarDetail.Mobile;
            //avatarDetail.ModifiedByAvatarId = AvatarDetail.ModifiedByAvatarId;
            //avatarDetail.ModifiedDate = AvatarDetail.ModifiedDate;
            avatarDetail.Name = AvatarDetail.Name == null ? "": AvatarDetail.Name;
            //avatarDetail.ParentCelestialBodyId = AvatarDetail.ParentCelestialBodyId;
            //avatarDetail.ParentCelestialSpaceId = AvatarDetail.ParentCelestialSpaceId;
            //avatarDetail.ParentDimensionId = AvatarDetail.ParentDimensionId;
            //avatarDetail.ParentGalaxyClusterId = AvatarDetail.ParentGalaxyClusterId;
            //avatarDetail.ParentGalaxyId = AvatarDetail.ParentGalaxyId;
            //avatarDetail.ParentGrandSuperStarId = AvatarDetail.ParentGrandSuperStarId;
            //avatarDetail.ParentGreatGrandSuperStarId = AvatarDetail.ParentGreatGrandSuperStarId;
            //avatarDetail.ParentHolonId = AvatarDetail.ParentHolonId;
            //avatarDetail.ParentMoonId = AvatarDetail.ParentMoonId;
            //avatarDetail.ParentMultiverseId = AvatarDetail.ParentMultiverseId;
            //avatarDetail.ParentOmniverseId = AvatarDetail.ParentOmniverseId;
            //avatarDetail.ParentPlanetId = AvatarDetail.ParentPlanetId;
            //avatarDetail.ParentSolarSystemId = AvatarDetail.ParentSolarSystemId;
            //avatarDetail.ParentStarId = AvatarDetail.ParentStarId;
            //avatarDetail.ParentSuperStarId = AvatarDetail.ParentSuperStarId;
            //avatarDetail.ParentUniverseId = AvatarDetail.ParentUniverseId;
            //avatarDetail.ParentZomeId = AvatarDetail.ParentZomeId;
            avatarDetail.Postcode = AvatarDetail.Postcode == null ?"": AvatarDetail.Postcode;
            //avatarDetail.PreviousVersionId = AvatarDetail.PreviousVersionId;
            //avatarDetail.STARCLIColour = AvatarDetail.STARCLIColour;
            avatarDetail.Town = AvatarDetail.Town == null ? "": AvatarDetail.Town;
            avatarDetail.UmaJson = AvatarDetail.UmaJson == null ? "" : AvatarDetail.UmaJson;
            avatarDetail.Username = AvatarDetail.Username;
            //avatarDetail.Version = AvatarDetail.Version;
            avatarDetail.XP = AvatarDetail.XP;
            return avatarDetail;
        }
    }
}