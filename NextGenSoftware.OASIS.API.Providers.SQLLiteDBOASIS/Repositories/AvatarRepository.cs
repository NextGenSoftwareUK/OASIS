using System;
using System.Linq;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Holons;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.DataBaseModels;
using NextGenSoftware.OASIS.API.Core.Managers;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Repositories{

    public class AvatarRepository : IAvatarRepository
    {
        private readonly DataContext dataBase;

        public AvatarRepository(DataContext dataBase){

            this.dataBase=dataBase;
        }
        
        public Avatar Add(Avatar avatar)
        {
            try
            {
                avatar.Id = Guid.NewGuid();
                avatar.CreatedProviderType = new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS);

                AvatarModel avatarModel=new AvatarModel(avatar);

                dataBase.Avatars.Add(avatarModel);
                dataBase.SaveChanges();

                avatar.ProviderKey[ProviderType.SQLLiteDBOASIS] = avatarModel.Id;

                dataBase.SaveChanges();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return avatar;
        }

        public Task<Avatar> AddAsync(Avatar avatar)
        {
            try
            {
                return new Task<Avatar>(()=>{

                    avatar.Id = Guid.NewGuid();
                    avatar.CreatedProviderType = new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS);

                    AvatarModel avatarModel=new AvatarModel(avatar);

                    dataBase.AddAsync(avatarModel);
                    dataBase.SaveChangesAsync();
                    
                    avatar.ProviderKey[ProviderType.SQLLiteDBOASIS] = avatarModel.Id;
                
                    dataBase.SaveChangesAsync();
                    return(avatar);

                });
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public bool Delete(Guid id, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public bool Delete(string providerKey, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteAsync(Guid id, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteAsync(string providerKey, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public Avatar GetAvatar(Guid id)
        {
            Avatar avatar = null;
            String convertedId = id.ToString();
            try
            {
                AvatarModel avatarModel = dataBase.Avatars.FirstOrDefault(x => x.Id == convertedId);

                if (avatarModel == null){
                    return(avatar);
                }

                LoadAvatarReferences(avatarModel);
                avatar=avatarModel.GetAvatar();

            }
            catch(Exception ex)
            {
                throw ex;
            }
            return(avatar);
        }

        public Avatar GetAvatar(string username)
        {
            Avatar avatar = null;
            try
            {
                AvatarModel avatarModel = dataBase.Avatars.FirstOrDefault(x => x.Username == username);

                if (avatarModel == null){
                    return(avatar);
                }

                LoadAvatarReferences(avatarModel);
                avatar=avatarModel.GetAvatar();

            }
            catch(Exception ex)
            {
                throw ex;
            }
            return(avatar);
        }

        public Avatar GetAvatar(string username, string password)
        {
            Avatar avatar = null;
            try
            {
                AvatarModel avatarModel = dataBase.Avatars.FirstOrDefault(x => x.Username == username && x.Password == password);

                if (avatarModel == null){
                    return(avatar);
                }

                LoadAvatarReferences(avatarModel);
                avatar=avatarModel.GetAvatar();

            }
            catch(Exception ex)
            {
                throw ex;
            }
            return(avatar);
        }

        public Task<Avatar> GetAvatarAsync(Guid id)
        {
            try
            {
                return new Task<Avatar>(()=>{

                    Avatar avatar = null;
                    String convertedId = id.ToString();

                    AvatarModel avatarModel = dataBase.Avatars.FirstOrDefault(x => x.Id == convertedId);

                    if (avatarModel == null){
                        return(avatar);
                    }

                    LoadAvatarReferences(avatarModel);
                    avatar=avatarModel.GetAvatar();

                    return(avatar);

                });
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public Task<Avatar> GetAvatarAsync(string username)
        {
            try
            {
                return new Task<Avatar>(()=>{

                    Avatar avatar = null;
                    AvatarModel avatarModel = dataBase.Avatars.FirstOrDefault(x => x.Username == username);

                    if (avatarModel == null){
                        return(avatar);
                    }

                    LoadAvatarReferences(avatarModel);
                    avatar=avatarModel.GetAvatar();

                    return(avatar);

                });
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public Task<Avatar> GetAvatarAsync(string username, string password)
        {
            try
            {
                return new Task<Avatar>(()=>{

                    Avatar avatar = null;
                    AvatarModel avatarModel = dataBase.Avatars.FirstOrDefault(x => x.Username == username && x.Password == password);

                    if (avatarModel == null){
                        return(avatar);
                    }

                    LoadAvatarReferences(avatarModel);
                    avatar=avatarModel.GetAvatar();

                    return(avatar);

                });
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public List<Avatar> GetAvatars()
        {
            List<Avatar> avatarsList=new List<Avatar>();
            try{

                List<AvatarModel> avatarModels=dataBase.Avatars.ToList<AvatarModel>();
                foreach (AvatarModel model in avatarModels)
                {
                    LoadAvatarReferences(model);
                    avatarsList.Add(model.GetAvatar());
                }

            }
            catch(Exception ex){
                throw ex;
            }
            return(avatarsList);
        }

        public Task<List<Avatar>> GetAvatarsAsync()
        {
            try
            {
                return new Task<List<Avatar>>(()=>{

                    List<Avatar> avatarsList=new List<Avatar>();

                    List<AvatarModel> avatarModels=dataBase.Avatars.ToList<AvatarModel>();
                    foreach (AvatarModel model in avatarModels)
                    {
                        LoadAvatarReferences(model);
                        avatarsList.Add(model.GetAvatar());
                    }

                    return(avatarsList);

                });
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public Avatar Update(Avatar avatar)
        {
            try
            {
                AvatarModel avatarModel=new AvatarModel(avatar);

                dataBase.Avatars.Update(avatarModel);
                dataBase.SaveChanges();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return avatar;
        }

        public Task<Avatar> UpdateAsync(Avatar avatar)
        {
            try
            {
                return new Task<Avatar>(()=>{

                    AvatarModel avatarModel=new AvatarModel(avatar);

                    dataBase.Update(avatarModel);
                    dataBase.SaveChangesAsync();
                    
                    return(avatar);

                });
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void LoadAvatarReferences(AvatarModel avatarModel){

            dataBase.Entry(avatarModel)
                    .Reference<AvatarAttributesModel>(a => a.Attributes)
                    .Load();

            dataBase.Entry(avatarModel)
                    .Reference<AvatarAuraModel>(a => a.Aura)
                    .Load();

            dataBase.Entry(avatarModel)
                    .Reference<AvatarHumanDesignModel>(a => a.HumanDesign)
                    .Load();

            dataBase.Entry(avatarModel)
                    .Reference<AvatarSkillsModel>(a => a.Skills)
                    .Load();

            dataBase.Entry(avatarModel)
                    .Reference<AvatarStatsModel>(a => a.Stats)
                    .Load();

            dataBase.Entry(avatarModel)
                    .Reference<AvatarSuperPowersModel>(a => a.SuperPowers)
                    .Load();
            
            

            dataBase.Entry(avatarModel)
                    .Collection<AvatarChakraModel>(a => a.AvatarChakras)
                    .Load();
            
            foreach (AvatarChakraModel chakraModel in avatarModel.AvatarChakras)
            {
                dataBase.Entry(chakraModel)
                        .Reference<CrystalModel>(c => c.Crystal)
                        .Load();
                
                dataBase.Entry(chakraModel)
                    .Collection<AvatarGiftModel>(c => c.GiftsUnlocked)
                    .Query()
                    .Where<AvatarGiftModel>(g => g.AvatarId==avatarModel.Id && g.AvatarChakraId!=0)
                    .ToList();
                
            }

            dataBase.Entry(avatarModel)
                    .Collection<AvatarGiftModel>(a => a.Gifts)
                    .Query()
                    .Where<AvatarGiftModel>(g => g.AvatarId==avatarModel.Id && g.AvatarChakraId==0)
                    .ToList();

            //dataBase.Entry(avatarModel).Collection<AvatarGiftModel>(e => e.Gifts).Load();


            dataBase.Entry(avatarModel)
                    .Collection<HeartRateEntryModel>(a => a.HeartRates)
                    .Load();

            dataBase.Entry(avatarModel)
                    .Collection<RefreshTokenModel>(a => a.RefreshTokens)
                    .Load();

            dataBase.Entry(avatarModel)
                    .Collection<InventoryItemModel>(a => a.InventoryItems)
                    .Load();

            dataBase.Entry(avatarModel)
                    .Collection<GeneKeyModel>(a => a.GeneKeys)
                    .Load();

            dataBase.Entry(avatarModel)
                    .Collection<SpellModel>(a => a.Spells)
                    .Load();

            dataBase.Entry(avatarModel)
                    .Collection<AchievementModel>(a => a.Achievements)
                    .Load();

            dataBase.Entry(avatarModel)
                    .Collection<KarmaAkashicRecordModel>(a => a.KarmaAkashicRecords)
                    .Load();


            dataBase.Entry(avatarModel)
                    .Collection<ProviderPrivateKeyModel>(a => a.ProviderPrivateKey)
                    .Load();

            dataBase.Entry(avatarModel)
                    .Collection<ProviderPublicKeyModel>(a => a.ProviderPublicKey)
                    .Load();

            dataBase.Entry(avatarModel)
                    .Collection<ProviderWalletAddressModel>(a => a.ProviderWalletAddress)
                    .Load();
        }
    }
}