//using System;
//using System.Linq;
//using System.Collections.Generic;
//using NextGenSoftware.OASIS.API.Core.Holons;
//using System.Threading.Tasks;
//using NextGenSoftware.OASIS.API.Core.Enums;
//using NextGenSoftware.OASIS.API.Core.Helpers;
//using NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.DataBaseModels;
//using NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Interfaces;
//using Neo4jOgm.Repository;
//using Neo4jOgm.Domain;

//namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Repositories
//{

//    public class AvatarRepository : IAvatarRepository
//    {
//        private readonly NeoRepository dataBase;

//        public AvatarRepository(NeoRepository dataBase)
//        {

//            this.dataBase = dataBase;
//        }

//        public Avatar Add(Avatar avatar)
//        {
//            try
//            {
//                AvatarModel avatarModel = new AvatarModel(avatar);

//                avatarModel.CreatedOASISType = OASISType.OASISWeb.ToString();
//                dataBase.Create(avatarModel);

//            }
//            catch (Exception ex)
//            {
//                throw ex;
//            }
//            return avatar;
//        }


//        public bool Delete(long id, bool softDelete = true)
//        {
//            throw new NotImplementedException();
//        }

//        public bool Delete(string providerKey, bool softDelete = true)
//        {
//            throw new NotImplementedException();
//        }

//        public Task<bool> DeleteAsync(Guid id, bool softDelete = true)
//        {
//            throw new NotImplementedException();
//        }

//        public Task<bool> DeleteAsync(string providerKey, bool softDelete = true)
//        {
//            throw new NotImplementedException();
//        }




//        public Avatar GetAvatar(string username)
//        {
//            var crteria = new Criteria("Username", Operator.Equal, username);

//            Avatar avatar = null;
//            try
//            {
//                AvatarModel avatarModel = dataBase.FindAll<AvatarModel>(new PageRequest(1, 5), crteria, new RelationshipOption { Load = true }).Items.SingleOrDefault();

//                if (avatarModel == null)
//                {
//                    return (avatar);
//                }
//                avatar = avatarModel.GetAvatar();
//            }
//            catch (Exception ex)
//            {
//                throw ex;
//            }
//            return (avatar);
//        }


//        public Avatar GetAvatar(string username, string password)
//        {
//            var crteria = new Criteria("Username", Operator.Equal, username)
//        .And(new Criteria("Password", Operator.Equal, password));

//            Avatar avatar = null;
//            try
//            {
//                AvatarModel avatarModel = dataBase.FindAll<AvatarModel>(new PageRequest(1, 5), crteria, new RelationshipOption { Load = true }).Items.FirstOrDefault();

//                if (avatarModel == null)
//                {
//                    return (avatar);
//                }
//                avatar = avatarModel.GetAvatar();
//            }
//            catch (Exception ex)
//            {
//                throw ex;
//            }
//            return (avatar);
//        }




//        public Task<Avatar> GetAvatarAsync(string username)
//        {
//            throw new NotImplementedException();

//        }

//        public Task<Avatar> GetAvatarAsync(string username, string password)
//        {
//            throw new NotImplementedException();
//        }

//        public List<Avatar> GetAvatars()
//        {
//            List<Avatar> avatarsList = new List<Avatar>();
//            try
//            {
//                List<AvatarModel> avatarModels = dataBase.FindAll<AvatarModel>(new PageRequest(1, 100)).Items.ToList();

//                foreach (AvatarModel model in avatarModels)
//                {
//                    avatarsList.Add(model.GetAvatar());
//                }

//            }
//            catch (Exception ex)
//            {
//                throw ex;
//            }
//            return (avatarsList);
//        }

//        public Task<List<Avatar>> GetAvatarsAsync()
//        {
//            throw new NotImplementedException();

//        }

//        public Avatar Update(Avatar avatar)
//        {
//            try
//            {
//                AvatarModel avatarModel = new AvatarModel(avatar);
//                dataBase.Update(avatarModel);

//            }
//            catch (Exception ex)
//            {
//                throw ex;
//            }
//            return avatar;
//        }

//        public Task<Avatar> UpdateAsync(Avatar avatar)
//        {
//            throw new NotImplementedException();

//        }

//        Avatar IAvatarRepository.Add(AvatarModel avatar)
//        {
//            throw new NotImplementedException();
//        }

//        Task<bool> IAvatarRepository.DeleteAsync(long id, bool softDelete)
//        {
//            throw new NotImplementedException();
//        }

//        Avatar IAvatarRepository.GetAvatar(long id)
//        {
//            throw new NotImplementedException();
//        }

//        Task<Avatar> IAvatarRepository.GetAvatarAsync(long id)
//        {
//            throw new NotImplementedException();
//        }

//        Avatar IAvatarRepository.Update(AvatarModel avatar)
//        {
//            throw new NotImplementedException();
//        }
//    }
//}