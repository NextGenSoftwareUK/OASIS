using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Managers;
using OASIS_Onion.MongoDb.Interface;
using OASIS_Onion.Repository.Entity;
using OASIS_Onion.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Avatar = OASIS_Onion.Model.Models.Avatar;
using Core = NextGenSoftware.OASIS.API.Core;

namespace OASIS_Onion.Repository.Repository
{
    public class AvatarRepository : EntityRepository<Avatar, IEntityProvider<Avatar>>, IAvatarRepository
    {
        private IEntityProvider<Avatar> _testProvider;

        public AvatarRepository(IEntityProvider<Avatar> AvatarProvider) : base(AvatarProvider)
        {
            this._testProvider = AvatarProvider;
        }

        public async Task<Avatar> AddAsync(Avatar avatar)
        {
            try
            {
                avatar.HolonId = Guid.NewGuid();
                avatar.CreatedProviderType = new Core.Helpers.EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.MongoDBOASIS);
                await this._testProvider.InsertAsync(avatar);
                avatar.ProviderKey[Core.Enums.ProviderType.MongoDBOASIS] = avatar.Id.ToString();

                await UpdateAsync(avatar);
                return avatar;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public Avatar Add(Avatar avatar)
        {
            try
            {
                avatar.HolonId = Guid.NewGuid();
                avatar.CreatedProviderType = new Core.Helpers.EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.MongoDBOASIS);

                this._testProvider.Insert(avatar);
                avatar.ProviderKey[Core.Enums.ProviderType.MongoDBOASIS] = avatar.Id.ToString();

                Update(avatar);
                return avatar;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<Avatar> GetAvatarAsync(Guid id)
        {
            try
            {
                return await this._testProvider.GetAsync(id);
            }
            catch
            {
                throw;
            }
        }

        public Avatar GetAvatar(Guid id)
        {
            try
            {
                return this._testProvider.Get(id);
            }
            catch
            {
                throw;
            }
        }

        public Avatar GetAvatar(Expression<Func<Avatar, bool>> expression)
        {
            return this._testProvider.Get(expression);
        }

        public async Task<Avatar> GetAvatarAsync(Expression<Func<Avatar, bool>> expression)
        {
            return await this._testProvider.GetAsync(expression);
        }

        public async Task<Avatar> GetAvatarAsync(string username)
        {
            try
            {
                FilterDefinition<Avatar> filter = Builders<Avatar>.Filter.Where(x => x.Username == username);
                return await this._testProvider.GetAsync(filter);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public Avatar GetAvatar(string username)
        {
            try
            {
                FilterDefinition<Avatar> filter = Builders<Avatar>.Filter.Where(x => x.Username == username);
                return this._testProvider.Get(filter);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<Avatar> GetAvatarAsync(string username, string password)
        {
            try
            {
                FilterDefinition<Avatar> filter = Builders<Avatar>.Filter.Where(x => x.Username == username);
                Avatar avatar = await this._testProvider.GetAsync(filter);

                if (avatar != null && password != avatar.Password)
                    return null;

                return avatar;
            }
            catch
            {
                throw;
            }
        }

        public Avatar GetAvatar(string username, string password)
        {
            try
            {
                FilterDefinition<Avatar> filter = Builders<Avatar>.Filter.Where(x => x.Username == username);
                Avatar avatar = this._testProvider.Get(filter);

                if (avatar != null && password != avatar.Password)
                    return null;

                return avatar;
            }
            catch
            {
                throw;
            }
        }

        public async Task<List<Avatar>> GetAvatarsAsync()
        {
            try
            {
                return (List<Avatar>)await this._testProvider.GetAllAsync();
            }
            catch
            {
                throw;
            }
        }

        public List<Avatar> GetAvatars()
        {
            try
            {
                return (List<Avatar>)this._testProvider.GetAll();
            }
            catch
            {
                throw;
            }
        }

        public async Task<Avatar> UpdateAsync(Avatar avatar)
        {
            try
            {
                await this._testProvider.UpdateAsync(avatar);
                return avatar;
            }
            catch
            {
                throw;
            }
        }

        //public async Task<AvatarThumbnail> GetAvatarThumbnailByIdAsync(Guid id)
        //{
        //    var filter = Builders<AvatarThumbnail>.Filter.Where(x => x.Id == id);
        //    var findResult = await _dbContext.AvatarThumbnail.FindAsync(filter);
        //    var detailEntity = await findResult.FirstOrDefaultAsync();
        //    return detailEntity;
        //}

        //public AvatarThumbnail GetAvatarThumbnailById(Guid id)
        //{
        //    var filter = Builders<AvatarThumbnail>.Filter.Where(x => x.Id == id);
        //    return _dbContext.AvatarThumbnail.Find(filter).FirstOrDefault();
        //}

        public Avatar Update(Avatar avatar)
        {
            try
            {
                this._testProvider.Update(avatar);
                return avatar;
            }
            catch
            {
                throw;
            }
        }

        public async Task<bool> DeleteAsync(Guid id, bool softDelete = true)
        {
            try
            {
                if (softDelete)
                {
                    Avatar avatar = await GetAvatarAsync(id);

                    if (AvatarManager.LoggedInAvatar != null)
                        avatar.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id.ToString();

                    avatar.DeletedDate = DateTime.Now;
                    await this._testProvider.UpdateAsync(avatar);
                    return true;
                }
                else
                {
                    await this._testProvider.RemoveAsync(id);
                    return true;
                }
            }
            catch
            {
                throw;
            }
        }

        public bool Delete(Expression<Func<Avatar, bool>> expression, bool softDelete = true)
        {
            try
            {
                if (softDelete)
                {
                    Avatar avatar = GetAvatar(expression);

                    if (AvatarManager.LoggedInAvatar != null)
                        avatar.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id.ToString();

                    avatar.DeletedDate = DateTime.Now;
                    this._testProvider.Update(avatar);
                    return true;
                }
                else
                {
                    this._testProvider.RemoveAsync(expression);
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteAsync(Expression<Func<Avatar, bool>> expression, bool softDelete = true)
        {
            try
            {
                if (softDelete)
                {
                    var avatar = await GetAvatarAsync(expression);

                    if (AvatarManager.LoggedInAvatar != null)
                        avatar.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id.ToString();
                    avatar.DeletedDate = DateTime.Now;
                    await this._testProvider.UpdateAsync(avatar);
                    return true;
                }
                else
                {
                    await this._testProvider.RemoveAsync(expression);
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public bool Delete(Guid id, bool softDelete = true)
        {
            try
            {
                if (softDelete)
                {
                    Avatar avatar = GetAvatar(id);

                    if (AvatarManager.LoggedInAvatar != null)
                        avatar.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id.ToString();

                    avatar.DeletedDate = DateTime.Now;
                    this._testProvider.Update(avatar);
                    return true;
                }
                else
                {
                    this._testProvider.Remove(id);
                    return true;
                }
            }
            catch
            {
                throw;
            }
        }

        public async Task<bool> DeleteAsync(string providerKey, bool softDelete = true)
        {
            try
            {
                if (softDelete)
                {
                    Avatar avatar = await GetAvatarAsync(providerKey);

                    if (AvatarManager.LoggedInAvatar != null)
                        avatar.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id.ToString();

                    avatar.DeletedDate = DateTime.Now;
                    await this._testProvider.UpdateAsync(avatar);
                    return true;
                }
                else
                {
                    FilterDefinition<Avatar> data = Builders<Avatar>.Filter.Where(x => x.ProviderKey[Core.Enums.ProviderType.MongoDBOASIS] == providerKey);
                    await this._testProvider.RemoveAsync(data);
                    return true;
                }
            }
            catch
            {
                throw;
            }
        }

        public bool Delete(string providerKey, bool softDelete = true)
        {
            try
            {
                if (softDelete)
                {
                    Avatar avatar = GetAvatar(providerKey);

                    if (AvatarManager.LoggedInAvatar != null)
                        avatar.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id.ToString();

                    avatar.DeletedDate = DateTime.Now;
                    this._testProvider.Update(avatar);
                    return true;
                }
                else
                {
                    FilterDefinition<Avatar> data = Builders<Avatar>.Filter.Where(x => x.ProviderKey[Core.Enums.ProviderType.MongoDBOASIS] == providerKey);
                    this._testProvider.Remove(data);
                    return true;
                }
            }
            catch
            {
                throw;
            }
        }
    }
}