using NextGenSoftware.OASIS.API.Providers.SQLLiteOASIS.Exceptions;
using NextGenSoftware.OASIS.API.Providers.SQLLiteOASIS.Interfaces;
using NextGenSoftware.OASIS.API.Providers.SQLLiteOASIS.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.SQLLiteOASIS.Entities;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteOASIS.Persistence.Repositories
{
    public class AvtarDetailRepository : IAvtarDetailRepository
    {
        private readonly DataContext eFContext;

        public AvtarDetailRepository(DataContext eFContext)
        {
            this.eFContext = eFContext;
        }

        //public async Task<List<AvatarDetailEntity>> GetAvtarDetails()
        //{
        //    try
        //    {
        //        return await this.eFContext.AvatarDetailEntities.ToListAsync();
        //    }
        //    catch (Exception)
        //    {
        //        throw;
        //    }
        //}

        //public async Task<AvatarDetailEntity> GetAvtarDetailById(Guid avtarDetailId)
        //{
        //    try
        //    {
        //        var avtarDetails = await this.eFContext.AvatarDetailEntities.Where(e => e.Id.Equals(avtarDetailId)).FirstOrDefaultAsync();
        //        if (avtarDetails != null)
        //        {
        //            return avtarDetails;
        //        }
        //    }
        //    catch (Exception)
        //    {
        //        throw;
        //    }
        //    throw new NotFoundException();
        //}

        //public async Task<AvatarDetailEntity> CreateAvtarDetail(AvatarDetailEntity request)
        //{
        //    try
        //    {
        //        var avtarDetail = request;
        //        this.eFContext.AvatarDetailEntities.Add(avtarDetail);
        //        await this.eFContext.SaveChangesAsync();
        //        return avtarDetail;
        //    }
        //    catch (Exception)
        //    {
        //        throw;
        //    }
        //}

        //public async Task<AvatarDetailEntity> UpdateAvtarDetail(AvatarDetailEntity request)
        //{
        //    try
        //    {
        //        var avtarDetail = this.eFContext.AvatarDetailEntities.Find(request.Id);
        //        if (avtarDetail != null)
        //        {
        //            avtarDetail.Name = request.Name;
        //            this.eFContext.AvatarDetailEntities.Update(avtarDetail);
        //            await this.eFContext.SaveChangesAsync();
        //            return avtarDetail;
        //        }
        //    }
        //    catch (Exception)
        //    {
        //        throw;
        //    }
        //    throw new NotFoundException();
        //}

        //public async Task<bool> DeleteAvtarDetailById(Guid avtarDetailID)
        //{
        //    try
        //    {
        //        var avtarDetail = await this.eFContext.AvatarDetailEntities.Where(p => p.Id == avtarDetailID).FirstOrDefaultAsync();
        //        if (avtarDetail != null)
        //        {
        //            this.eFContext.AvatarDetailEntities.Remove(avtarDetail);
        //            await this.eFContext.SaveChangesAsync();
        //            return true;
        //        }
        //        else { return false; }
        //    }
        //    catch (Exception)
        //    {
        //        throw;
        //    }
        //}

        public List<AvatarDetailEntity> LoadAvatarDetail(Guid id, int version = 0)
        {
            try
            {
                return this.eFContext.AvatarDetailEntities.Where(x => x.Id == id && x.Version == version).ToList();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public List<AvatarDetailEntity> LoadAvatarDetailByEmail(string avatarEmail, int version = 0)
        {
            try
            {
                return this.eFContext.AvatarDetailEntities.Where(x => x.Email == avatarEmail && x.Version == version).ToList();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public List<AvatarDetailEntity> LoadAvatarDetailByUsername(string avatarUsername, int version = 0)
        {
            try
            {
                return this.eFContext.AvatarDetailEntities.Where(x => x.Username == avatarUsername && x.Version == version).ToList();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<List<AvatarDetailEntity>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            try
            {
                return await this.eFContext.AvatarDetailEntities.Where(x => x.Id == id && x.Version == version).ToListAsync();
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<List<AvatarDetailEntity>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
        {
            try
            {
                return await this.eFContext.AvatarDetailEntities.Where(x => x.Username == avatarUsername && x.Version == version).ToListAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<List<AvatarDetailEntity>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
        {
            try
            {
                return await this.eFContext.AvatarDetailEntities.Where(x => x.Email == avatarEmail && x.Version == version).ToListAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public List<AvatarDetailEntity> LoadAllAvatarDetails(int version = 0)
        {
            try
            {
                return this.eFContext.AvatarDetailEntities.Where(x => x.Version == version).ToList();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<List<AvatarDetailEntity>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            try
            {
                return await this.eFContext.AvatarDetailEntities.Where(x => x.Version == version).ToListAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public AvatarDetailEntity SaveAvatarDetail(AvatarDetailEntity AvatarDetailEntity)
        {
            try
            {
                AvatarDetailEntity avatarDetail = CreateAvatarDetailModel(AvatarDetailEntity);
                this.eFContext.AvatarDetailEntities.Add(avatarDetail);
                this.eFContext.SaveChanges();
                return avatarDetail;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<AvatarDetailEntity> SaveAvatarDetailAsync(AvatarDetailEntity AvatarDetailEntity)
        {
            try
            {
                AvatarDetailEntity avatarDetail = CreateAvatarDetailModel(AvatarDetailEntity);
                this.eFContext.AvatarDetailEntities.Add(avatarDetail);
                await this.eFContext.SaveChangesAsync();
                return avatarDetail;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public AvatarDetailEntity CreateAvatarDetailModel(AvatarDetailEntity AvatarDetail)
        {
            AvatarDetailEntity avatarDetail = new AvatarDetailEntity();
            avatarDetail.Address = AvatarDetail.Address;
            avatarDetail.Country = AvatarDetail.Country;
            avatarDetail.CreatedByAvatarId = Convert.ToString(AvatarDetail.CreatedByAvatarId);
            avatarDetail.CreatedDate = AvatarDetail.CreatedDate;
            avatarDetail.DeletedByAvatarId = Convert.ToString(AvatarDetail.DeletedByAvatarId);
            avatarDetail.DeletedDate = AvatarDetail.DeletedDate;
            avatarDetail.Description = AvatarDetail.Description;
            avatarDetail.DOB = AvatarDetail.DOB;
            avatarDetail.Email = AvatarDetail.Email;
            //avatarDetail.FavouriteColour = AvatarDetail.FavouriteColour;
            avatarDetail.HolonId = AvatarDetail.HolonId;
            avatarDetail.Id = AvatarDetail.Id;
            avatarDetail.Image2D = AvatarDetail.Image2D;
            avatarDetail.IsActive = AvatarDetail.IsActive;
            avatarDetail.IsChanged = AvatarDetail.IsChanged;
            avatarDetail.Karma = AvatarDetail.Karma;
            avatarDetail.Landline = AvatarDetail.Landline;
            avatarDetail.Level = AvatarDetail.Level;
            avatarDetail.Mobile = AvatarDetail.Mobile;
            avatarDetail.ModifiedByAvatarId = Convert.ToString(AvatarDetail.ModifiedByAvatarId);
            avatarDetail.ModifiedDate = AvatarDetail.ModifiedDate;
            avatarDetail.Name = AvatarDetail.Name;
            avatarDetail.ParentCelestialBodyId = AvatarDetail.ParentCelestialBodyId;
            avatarDetail.ParentCelestialSpaceId = AvatarDetail.ParentCelestialSpaceId;
            avatarDetail.ParentDimensionId = AvatarDetail.ParentDimensionId;
            avatarDetail.ParentGalaxyClusterId = AvatarDetail.ParentGalaxyClusterId;
            avatarDetail.ParentGalaxyId = AvatarDetail.ParentGalaxyId;
            avatarDetail.ParentGrandSuperStarId = AvatarDetail.ParentGrandSuperStarId;
            avatarDetail.ParentGreatGrandSuperStarId = AvatarDetail.ParentGreatGrandSuperStarId;
            avatarDetail.ParentHolonId = AvatarDetail.ParentHolonId;
            avatarDetail.ParentMoonId = AvatarDetail.ParentMoonId;
            avatarDetail.ParentMultiverseId = AvatarDetail.ParentMultiverseId;
            avatarDetail.ParentOmniverseId = AvatarDetail.ParentOmniverseId;
            avatarDetail.ParentPlanetId = AvatarDetail.ParentPlanetId;
            avatarDetail.ParentSolarSystemId = AvatarDetail.ParentSolarSystemId;
            avatarDetail.ParentStarId = AvatarDetail.ParentStarId;
            avatarDetail.ParentSuperStarId = AvatarDetail.ParentSuperStarId;
            avatarDetail.ParentUniverseId = AvatarDetail.ParentUniverseId;
            avatarDetail.ParentZomeId = AvatarDetail.ParentZomeId;
            avatarDetail.Postcode = AvatarDetail.Postcode;
            avatarDetail.PreviousVersionId = AvatarDetail.PreviousVersionId;
            //avatarDetail.STARCLIColour = AvatarDetail.STARCLIColour;
            avatarDetail.Town = AvatarDetail.Town;
            avatarDetail.UmaJson = AvatarDetail.UmaJson;
            avatarDetail.Username = AvatarDetail.Username;
            avatarDetail.Version = AvatarDetail.Version;
            avatarDetail.XP = AvatarDetail.XP;
            return avatarDetail;
        }
    }
}