//using MongoDB.Driver;
//using OASIS_Onion.MongoDb.Interface;
//using OASIS_Onion.Repository.Entity;
//using OASIS_Onion.Repository.Interface;
//using System;
//using System.Collections.Generic;
//using System.Linq.Expressions;
//using System.Threading.Tasks;
//using AvatarDetail = OASIS_Onion.Model.Models.AvatarDetail;
//using Core = NextGenSoftware.OASIS.API.Core;

//namespace OASIS_Onion.Repository.Repository
//{
//    public class AvatarDetailRepository : EntityRepository<AvatarDetail, IEntityProvider<AvatarDetail>>, IAvatarDetailRepository
//    {
//        private IEntityProvider<AvatarDetail> _testProvider;

//        public AvatarDetailRepository(IEntityProvider<AvatarDetail> testProvider) : base(testProvider)
//        {
//            this._testProvider = testProvider;
//        }

//        public async Task<AvatarDetail> AddAsync(AvatarDetail avatar)
//        {
//            try
//            {
//                avatar.HolonId = Guid.NewGuid();
//                avatar.CreatedProviderType = new Core.Helpers.EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.MongoDBOASIS);

//                await this._testProvider.InsertAsync(avatar);
//                avatar.ProviderKey[Core.Enums.ProviderType.MongoDBOASIS] = avatar.Id.ToString();

//                await UpdateAsync(avatar);
//                return avatar;
//            }
//            catch (Exception ex)
//            {
//                throw;
//            }
//        }

//        public AvatarDetail Add(AvatarDetail avatar)
//        {
//            try
//            {
//                avatar.HolonId = Guid.NewGuid();
//                avatar.CreatedProviderType = new Core.Helpers.EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.MongoDBOASIS);

//                this._testProvider.Insert(avatar);
//                avatar.ProviderKey[Core.Enums.ProviderType.MongoDBOASIS] = avatar.Id.ToString();
//                Update(avatar);
//                return avatar;
//            }
//            catch (Exception ex)
//            {
//                throw;
//            }
//        }

//        public AvatarDetail GetAvatarDetail(Expression<Func<AvatarDetail, bool>> expression)
//        {
//            return this._testProvider.Get(expression);
//        }

//        public async Task<AvatarDetail> GetAvatarDetailAsync(Expression<Func<AvatarDetail, bool>> expression)
//        {
//            return await this._testProvider.GetAsync(expression);
//        }

//        public async Task<AvatarDetail> UpdateAsync(AvatarDetail avatar)
//        {
//            try
//            {
//                await this._testProvider.UpdateAsync(avatar);
//                return avatar;
//            }
//            catch
//            {
//                throw;
//            }
//        }

//        public async Task<AvatarDetail> GetAvatarDetailAsync(Guid id)
//        {
//            return await this._testProvider.GetAsync(id);
//        }

//        public AvatarDetail GetAvatarDetail(Guid id)
//        {
//            return this._testProvider.Get(id);
//        }

//        public async Task<AvatarDetail> GetAvatarDetailAsync(string username)
//        {
//            var filter = Builders<AvatarDetail>.Filter.Where(x => x.Username == username);
//            return await this._testProvider.GetAsync(filter);
//        }

//        public AvatarDetail GetAvatarDetail(string username)
//        {
//            var filter = Builders<AvatarDetail>.Filter.Where(x => x.Username == username);
//            return this._testProvider.Get(filter);
//        }

//        public async Task<IEnumerable<AvatarDetail>> GetAvatarDetailsAsync()
//        {
//            var cursor = await this._testProvider.GetAllAsync();
//            return cursor;
//        }

//        public IEnumerable<AvatarDetail> GetAvatarDetails()
//        {
//            return this._testProvider.GetAll();
//        }

//        public AvatarDetail Update(AvatarDetail avatar)
//        {
//            try
//            {
//                avatar = this._testProvider.Update(avatar);
//                return avatar;
//            }
//            catch
//            {
//                throw;
//            }
//        }
//    }
//}