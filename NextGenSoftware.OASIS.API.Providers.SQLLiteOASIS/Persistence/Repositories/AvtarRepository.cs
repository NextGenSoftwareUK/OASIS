using NextGenSoftware.OASIS.API.Providers.SQLLiteOASIS.Exceptions;
using NextGenSoftware.OASIS.API.Providers.SQLLiteOASIS.Interfaces;
using NextGenSoftware.OASIS.API.Providers.SQLLiteOASIS.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.SQLLiteOASIS.Entities;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteOASIS.Persistence.Repositories
{
    public class AvtarRepository : IAvtarRepository
    {
        private readonly DataContext eFContext;

        public AvtarRepository(DataContext eFContext)
        {
            this.eFContext = eFContext;
        }

        //public async Task<List<AvatarEntity>> GetAvtars()
        //{
        //    try
        //    {
        //        return await this.eFContext.AvatarEntities.ToListAsync();
        //    }
        //    catch (Exception)
        //    {
        //        throw;
        //    }
        //}

        //public async Task<AvatarEntity> GetAvtarById(Guid avtarId)
        //{
        //    try
        //    {
        //        var avtars = await this.eFContext.AvatarEntities.Where(e => e.Id.Equals(avtarId.ToString())).FirstOrDefaultAsync();
        //        if (avtars != null)
        //        {
        //            return avtars;
        //        }
        //    }
        //    catch (Exception)
        //    {
        //        throw;
        //    }
        //    throw new NotFoundException();
        //}

        //public async Task<AvatarEntity> CreateAvtar(AvatarEntity request)
        //{
        //    try
        //    {
        //        var avtar = request;
        //        this.eFContext.AvatarEntities.Add(avtar);
        //        await this.eFContext.SaveChangesAsync();
        //        return avtar;
        //    }
        //    catch (Exception)
        //    {
        //        throw;
        //    }
        //}

        //public async Task<AvatarEntity> UpdateAvtar(AvatarEntity request)
        //{
        //    try
        //    {
        //        var avtar = this.eFContext.AvatarEntities.Find(request.Id);
        //        if (avtar != null)
        //        {
        //            //avtar.Name = request.Name;
        //            this.eFContext.AvatarEntities.Update(avtar);
        //            await this.eFContext.SaveChangesAsync();
        //            return avtar;
        //        }
        //        throw new NotFoundException();
        //    }
        //    catch (Exception)
        //    {
        //        throw;
        //    }
        //}

        //public async Task<bool> DeleteAvtrarById(Guid avtarId)
        //{
        //    try
        //    {
        //        var avatar = await this.eFContext.AvatarEntities.Where(p => p.Id == avtarId).FirstOrDefaultAsync();
        //        if (avatar != null)
        //        {
        //            this.eFContext.AvatarEntities.Remove(avatar);
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

        public bool DeleteAvatar(Guid id, bool softDelete = true)
        {
            try
            {
                var avatar = this.eFContext.AvatarEntities.Where(p => p.Id == id).FirstOrDefault();
                if (avatar != null)
                {
                    if (softDelete)
                    {
                        avatar.IsActive = false;
                        this.eFContext.AvatarEntities.Update(avatar);
                    }
                    else
                    {
                        this.eFContext.AvatarEntities.Remove(avatar);
                    }
                    this.eFContext.SaveChangesAsync();
                    return true;
                }
                else { return false; }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public bool DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
        {
            try
            {
                var avatar = this.eFContext.AvatarEntities.Where(p => p.Email == avatarEmail).FirstOrDefault();
                if (avatar != null)
                {
                    if (softDelete)
                    {
                        avatar.IsActive = false;
                        this.eFContext.AvatarEntities.Update(avatar);
                    }
                    else
                    {
                        this.eFContext.AvatarEntities.Remove(avatar);
                    }
                    this.eFContext.SaveChangesAsync();
                    return true;
                }
                else { return false; }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public bool DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
        {
            try
            {
                var avatar = this.eFContext.AvatarEntities.Where(p => p.Username == avatarUsername).FirstOrDefault();
                if (avatar != null)
                {
                    if (softDelete)
                    {
                        avatar.IsActive = false;
                        this.eFContext.AvatarEntities.Update(avatar);
                    }
                    else
                    {
                        this.eFContext.AvatarEntities.Remove(avatar);
                    }
                    this.eFContext.SaveChangesAsync();
                    return true;
                }
                else { return false; }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<bool> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
        {
            try
            {
                var avatar = await this.eFContext.AvatarEntities.Where(p => p.Username == avatarUsername).FirstOrDefaultAsync();
                if (avatar != null)
                {
                    if (softDelete)
                    {
                        avatar.IsActive = false;
                        this.eFContext.AvatarEntities.Update(avatar);
                    }
                    else
                    {
                        this.eFContext.AvatarEntities.Remove(avatar);
                    }
                    await this.eFContext.SaveChangesAsync();
                    return true;
                }
                else { return false; }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public bool DeleteAvatar(string providerKey, bool softDelete = true)
        {
            //try
            //{
            //    var avatar = this.eFContext.AvatarEntities.Where(p => p.providerKey == providerKey).FirstOrDefault();
            //    if (avatar != null)
            //    {
            //        if (softDelete)
            //        {
            //            avatar.IsActive = false;
            //            this.eFContext.AvatarEntities.Update(avatar);
            //        }
            //        else
            //        {
            //            this.eFContext.AvatarEntities.Remove(avatar);
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

        public async Task<bool> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            try
            {
                var avatar = await this.eFContext.AvatarEntities.Where(p => p.Id == id).FirstOrDefaultAsync();
                if (avatar != null)
                {
                    if (softDelete)
                    {
                        avatar.IsActive = false;
                        this.eFContext.AvatarEntities.Update(avatar);
                    }
                    else
                    {
                        this.eFContext.AvatarEntities.Remove(avatar);
                    }
                    await this.eFContext.SaveChangesAsync();
                    return true;
                }
                else { return false; }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<bool> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        {
            try
            {
                var avatar = await this.eFContext.AvatarEntities.Where(p => p.Email == avatarEmail).FirstOrDefaultAsync();
                if (avatar != null)
                {
                    if (softDelete)
                    {
                        avatar.IsActive = false;
                        this.eFContext.AvatarEntities.Update(avatar);
                    }
                    else
                    {
                        this.eFContext.AvatarEntities.Remove(avatar);
                    }
                    await this.eFContext.SaveChangesAsync();
                    return true;
                }
                else { return false; }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<bool> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            //try
            //{
            //    var avatar = await this.eFContext.AvatarEntities.Where(p => p.providerKey == providerKey).FirstOrDefaultAsync();
            //    if (avatar != null)
            //    {
            //        if (softDelete)
            //        {
            //            avatar.IsActive = false;
            //            this.eFContext.AvatarEntities.Update(avatar);
            //        }
            //        else
            //        {
            //            this.eFContext.AvatarEntities.Remove(avatar);
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

        public List<AvatarEntity> LoadAvatar(string username, string password, int version = 0)
        {
            try
            {
                return this.eFContext.AvatarEntities.Where(p => p.Username == username && p.Password == password && p.Version == version).ToList();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public List<AvatarEntity> LoadAvatar(string username, int version = 0)
        {
            try
            {
                return this.eFContext.AvatarEntities.Where(p => p.Username == username && p.Version == version).ToList();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<List<AvatarEntity>> LoadAvatarAsync(string username, string password, int version = 0)
        {
            try
            {
                return await this.eFContext.AvatarEntities.Where(p => p.Username == username && p.Version == version).ToListAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<List<AvatarEntity>> LoadAllAvatarsAsync(int version = 0)
        {
            try
            {
                return await this.eFContext.AvatarEntities.Where(p => p.Version == version).ToListAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public List<AvatarEntity> LoadAllAvatars(int version = 0)
        {
            try
            {
                return this.eFContext.AvatarEntities.Where(p => p.Version == version).ToList();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public AvatarEntity LoadAvatarByUsername(string avatarUsername, int version = 0)
        {
            try
            {
                return this.eFContext.AvatarEntities.Where(p => p.Username == avatarUsername).FirstOrDefault();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<List<AvatarEntity>> LoadAvatarAsync(Guid Id, int version = 0)
        {
            try
            {
                return await this.eFContext.AvatarEntities.Where(p => p.Id == Id && p.Version == version).ToListAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<List<AvatarEntity>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            try
            {
                return await this.eFContext.AvatarEntities.Where(p => p.Email == avatarEmail && p.Version == version).ToListAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<List<AvatarEntity>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            try
            {
                return await this.eFContext.AvatarEntities.Where(p => p.Username == avatarUsername && p.Version == version).ToListAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public List<AvatarEntity> LoadAvatar(Guid Id, int version = 0)
        {
            try
            {
                return this.eFContext.AvatarEntities.Where(p => p.Id == Id && p.Version == version).ToList();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public List<AvatarEntity> LoadAvatarByEmail(string avatarEmail, int version = 0)
        {
            try
            {
                return this.eFContext.AvatarEntities.Where(p => p.Email == avatarEmail && p.Version == version).ToList();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<List<AvatarEntity>> LoadAvatarAsync(string username, int version = 0)
        {
            try
            {
                return await this.eFContext.AvatarEntities.Where(p => p.Username == username && p.Version == version).ToListAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<List<AvatarEntity>> LoadAvatarForProviderKeyAsync(string providerKey, int version = 0)
        {
            //try
            //{
            //    return await this.eFContext.AvatarEntities.Where(p => p.providerKey == providerKey && p.Version == version).ToListAsync();
            //}
            //catch (Exception)
            //{
            //    throw;
            //}
            throw new NotImplementedException();
        }

        public List<AvatarEntity> LoadAvatarForProviderKey(string providerKey, int version = 0)
        {
            //try
            //{
            //    return this.eFContext.AvatarEntities.Where(p => p.providerKey == providerKey && p.Version == version).ToList();
            //}
            //catch (Exception)
            //{
            //    throw;
            //}
            throw new NotImplementedException();
        }

        public AvatarEntity SaveAvatar(AvatarEntity avatar)
        {
            try
            {
                AvatarEntity avatarEntity = CreateAvatarModel(avatar);
                this.eFContext.AvatarEntities.Add(avatarEntity);
                this.eFContext.SaveChangesAsync();
                return avatarEntity;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<AvatarEntity> SaveAvatarAsync(AvatarEntity Avatar)
        {
            try
            {
                AvatarEntity avatarEntity = CreateAvatarModel(Avatar);
                this.eFContext.AvatarEntities.Add(Avatar);
                await this.eFContext.SaveChangesAsync();
                return Avatar;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public AvatarEntity CreateAvatarModel(AvatarEntity Avatar)
        {
            AvatarEntity avatar = new AvatarEntity();
            avatar.AcceptTerms = Avatar.AcceptTerms;
            avatar.CreatedByAvatarId = Convert.ToString(Avatar.CreatedByAvatarId);
            avatar.CreatedDate = Avatar.CreatedDate;
            avatar.DeletedByAvatarId = Convert.ToString(Avatar.DeletedByAvatarId);
            avatar.DeletedDate = Avatar.DeletedDate;
            avatar.Description = Avatar.Description;
            avatar.Email = Avatar.Email;
            avatar.FirstName = Avatar.FirstName;
            //avatar.FullName = Avatar.FullName;
            avatar.HolonId = Avatar.HolonId;
            avatar.Id = Avatar.Id;
            avatar.IsActive = Avatar.IsActive;
            avatar.IsBeamedIn = Avatar.IsBeamedIn;
            avatar.IsChanged = Avatar.IsChanged;
            //avatar.IsVerified = Avatar.IsVerified;
            avatar.JwtToken = Avatar.JwtToken;
            avatar.LastBeamedIn = Avatar.LastBeamedIn;
            avatar.LastBeamedOut = Avatar.LastBeamedOut;
            avatar.LastName = Avatar.LastName;
            avatar.ModifiedByAvatarId = Convert.ToString(Avatar.ModifiedByAvatarId);
            avatar.ModifiedDate = Avatar.ModifiedDate;
            avatar.Name = Avatar.Name;
            avatar.Password = Avatar.Password;
            avatar.PasswordReset = Avatar.PasswordReset;
            avatar.PreviousVersionId = Avatar.PreviousVersionId;
            avatar.RefreshToken = Avatar.RefreshToken;
            avatar.ResetToken = Avatar.ResetToken;
            avatar.ResetTokenExpires = Avatar.ResetTokenExpires;
            avatar.Title = Avatar.Title;
            avatar.Username = Avatar.Username;
            avatar.VerificationToken = Avatar.VerificationToken;
            avatar.Verified = Avatar.Verified;
            avatar.Version = Avatar.Version;
            return avatar;
        }
    }
}
