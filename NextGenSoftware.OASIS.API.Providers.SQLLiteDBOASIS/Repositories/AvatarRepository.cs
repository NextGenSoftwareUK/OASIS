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

                // avatarModel.ProviderKey.Add(new ProviderKeyModel(ProviderType.SQLLiteDBOASIS, avatarModel.Id));
                avatar.ProviderKey[ProviderType.SQLLiteDBOASIS] = avatarModel.Id;
                dataBase.SaveChanges();
            }
            catch (Exception ex)
            {
                throw;
            }
            return avatar;
        }

        public AvatarDetail Add(AvatarDetail avatar)
        {
            try
            {
                avatar.Id = Guid.NewGuid();
                avatar.CreatedProviderType = new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS);

                AvatarDetailModel avatarModel=new AvatarDetailModel(avatar);
                dataBase.AvatarDetails.Add(avatarModel);
                dataBase.SaveChanges();

                // avatarModel.ProviderKey.Add(new ProviderKeyModel(ProviderType.SQLLiteDBOASIS, avatarModel.Id));
                avatar.ProviderKey[ProviderType.SQLLiteDBOASIS] = avatarModel.Id;
                dataBase.SaveChanges();
            }
            catch (Exception ex)
            {
                throw;
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
                    dataBase.Avatars.AddAsync(avatarModel);
                    dataBase.SaveChangesAsync();

                    //avatarModel.ProviderKey.Add(new ProviderKeyModel(ProviderType.SQLLiteDBOASIS, avatarModel.Id));
                    avatar.ProviderKey[ProviderType.SQLLiteDBOASIS] = avatarModel.Id;
                    dataBase.SaveChangesAsync();
                    return(avatar);

                });
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public Task<AvatarDetail> AddAsync(AvatarDetail avatar)
        {
            try
            {
                return new Task<AvatarDetail>(()=>{

                    avatar.Id = Guid.NewGuid();
                    avatar.CreatedProviderType = new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS);

                    AvatarDetailModel avatarModel=new AvatarDetailModel(avatar);
                    dataBase.AvatarDetails.AddAsync(avatarModel);
                    dataBase.SaveChangesAsync();
                    
                    //avatarModel.ProviderKey.Add(new ProviderKeyModel(ProviderType.SQLLiteDBOASIS, avatarModel.Id));
                    avatar.ProviderKey[ProviderType.SQLLiteDBOASIS] = avatarModel.Id;
                    dataBase.SaveChangesAsync();
                    return(avatar);

                });
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public bool Delete(Guid id, bool softDelete = true)
        {
            bool delete_complete = false;
            try
            {
                String convertedId = id.ToString();
                AvatarModel deletingModel = dataBase.Avatars.FirstOrDefault(x => x.Id.Equals(convertedId));

                if(deletingModel == null){
                    return(true);
                }

                if (softDelete)
                {
                    LoadAvatarReferences(deletingModel);

                    if (AvatarManager.LoggedInAvatar != null)
                        deletingModel.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id.ToString();

                    deletingModel.DeletedDate = DateTime.Now;                    
                    dataBase.Avatars.Update(deletingModel);
                }
                else
                {
                    dataBase.Avatars.Remove(deletingModel);
                }

                dataBase.SaveChanges();
                delete_complete=true;
            }
            catch(Exception ex)
            {
                throw;
            }
            return(delete_complete);
        }

        public bool Delete(string userName, bool softDelete = true)
        {
            bool delete_complete = false;
            try
            {
                AvatarModel deletingModel = dataBase.Avatars.FirstOrDefault(x => x.Username.Equals(userName));

                if(deletingModel == null){
                    return(true);
                }

                if (softDelete)
                {
                    LoadAvatarReferences(deletingModel);

                    if (AvatarManager.LoggedInAvatar != null)
                        deletingModel.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id.ToString();

                    deletingModel.DeletedDate = DateTime.Now;                    
                    dataBase.Avatars.Update(deletingModel);
                }
                else
                {
                    dataBase.Avatars.Remove(deletingModel);
                }

                dataBase.SaveChanges();
                delete_complete=true;
            }
            catch(Exception ex)
            {
                throw ex;
            }
            return(delete_complete);
        }

        public Task<bool> DeleteAsync(Guid id, bool softDelete = true)
        {
            try
            {
                return new Task<bool>(()=>{

                    String convertedId = id.ToString();
                    AvatarModel deletingModel = dataBase.Avatars.FirstOrDefault(x => x.Id.Equals(convertedId));

                    if(deletingModel == null){
                        return(true);
                    }

                    if (softDelete)
                    {
                        LoadAvatarReferences(deletingModel);

                        if (AvatarManager.LoggedInAvatar != null)
                            deletingModel.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id.ToString();

                        deletingModel.DeletedDate = DateTime.Now;                    
                        dataBase.Avatars.Update(deletingModel);
                    }
                    else
                    {
                        dataBase.Avatars.Remove(deletingModel);
                    }

                    dataBase.SaveChanges();
                    return(true);

                });
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public Task<bool> DeleteAsync(string userName, bool softDelete = true)
        {
            try
            {
                return new Task<bool>(()=>{

                    AvatarModel deletingModel = dataBase.Avatars.FirstOrDefault(x => x.Username.Equals(userName));

                    if(deletingModel == null){
                        return(true);
                    }

                    if (softDelete)
                    {
                        LoadAvatarReferences(deletingModel);

                        if (AvatarManager.LoggedInAvatar != null)
                            deletingModel.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id.ToString();

                        deletingModel.DeletedDate = DateTime.Now;                    
                        dataBase.Avatars.Update(deletingModel);
                    }
                    else
                    {
                        dataBase.Avatars.Remove(deletingModel);
                    }

                    dataBase.SaveChanges();
                    return(true);

                });
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public bool DeleteByEmail(string avatarEmail, bool softDelete = true)
        {
            bool delete_complete = false;
            try
            {
                AvatarModel deletingModel = dataBase.Avatars.FirstOrDefault(x => x.Email.Equals(avatarEmail));

                if(deletingModel == null){
                    return(true);
                }

                if (softDelete)
                {
                    LoadAvatarReferences(deletingModel);

                    if (AvatarManager.LoggedInAvatar != null)
                        deletingModel.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id.ToString();

                    deletingModel.DeletedDate = DateTime.Now;                    
                    dataBase.Avatars.Update(deletingModel);
                }
                else
                {
                    dataBase.Avatars.Remove(deletingModel);
                }

                dataBase.SaveChanges();
                delete_complete=true;
            }
            catch(Exception ex)
            {
                throw ex;
            }
            return(delete_complete);
        }

        public Task<bool> DeleteByEmailAsync(string avatarEmail, bool softDelete = true)
        {
            try
            {
                return new Task<bool>(()=>{

                    AvatarModel deletingModel = dataBase.Avatars.FirstOrDefault(x => x.Email.Equals(avatarEmail));

                    if(deletingModel == null){
                        return(true);
                    }

                    if (softDelete)
                    {
                        LoadAvatarReferences(deletingModel);

                        if (AvatarManager.LoggedInAvatar != null)
                            deletingModel.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id.ToString();

                        deletingModel.DeletedDate = DateTime.Now;                    
                        dataBase.Avatars.Update(deletingModel);
                    }
                    else
                    {
                        dataBase.Avatars.Remove(deletingModel);
                    }

                    dataBase.SaveChanges();
                    return(true);

                });
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public Avatar GetAvatar(Guid id)
        {
            Avatar avatar = null;
            String convertedId = id.ToString();
            try
            {
                AvatarModel avatarModel = dataBase.Avatars.FirstOrDefault(x => x.Id.Equals(convertedId));

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
                AvatarModel avatarModel = dataBase.Avatars.FirstOrDefault(x => x.Username.Equals(username));

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
                AvatarModel avatarModel = dataBase.Avatars.FirstOrDefault(x => x.Username.Equals(username) && x.Password.Equals(password));

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

                    AvatarModel avatarModel = dataBase.Avatars.FirstOrDefault(x => x.Id.Equals(convertedId));

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
                    AvatarModel avatarModel = dataBase.Avatars.FirstOrDefault(x => x.Username.Equals(username));

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
                    AvatarModel avatarModel = dataBase.Avatars.FirstOrDefault(x => x.Username.Equals(username) && x.Password.Equals(password));

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

        public Task<Avatar> GetAvatarByEmailAsync(string avatarEmail)
        {
            try
            {
                return new Task<Avatar>(()=>{

                    Avatar avatar = null;
                    AvatarModel avatarModel = dataBase.Avatars.FirstOrDefault(x => x.Email.Equals(avatarEmail));

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

        public AvatarDetail GetAvatarDetail(Guid id)
        {
            AvatarDetail avatar = null;
            String convertedId = id.ToString();
            try
            {
                AvatarDetailModel avatarModel = dataBase.AvatarDetails.FirstOrDefault(x => x.Id.Equals(convertedId));

                if (avatarModel == null){
                    return(avatar);
                }

                LoadAvatarDetailReferences(avatarModel);
                avatar=avatarModel.GetAvatar();

            }
            catch(Exception ex)
            {
                throw ex;
            }
            return(avatar);
        }

        public AvatarDetail GetAvatarDetail(string username)
        {
            AvatarDetail avatar = null;
            try
            {
                AvatarDetailModel avatarModel = dataBase.AvatarDetails.FirstOrDefault(x => x.Username.Equals(username));

                if (avatarModel == null){
                    return(avatar);
                }

                LoadAvatarDetailReferences(avatarModel);
                avatar=avatarModel.GetAvatar();

            }
            catch(Exception ex)
            {
                throw ex;
            }
            return(avatar);
        }

        public Task<AvatarDetail> GetAvatarDetailAsync(Guid id)
        {
            try
            {
                return new Task<AvatarDetail>(()=>{

                    AvatarDetail avatar = null;
                    String convertedId = id.ToString();

                    AvatarDetailModel avatarModel = dataBase.AvatarDetails.FirstOrDefault(x => x.Id.Equals(convertedId));

                    if (avatarModel == null){
                        return(avatar);
                    }

                    LoadAvatarDetailReferences(avatarModel);
                    avatar=avatarModel.GetAvatar();

                    return(avatar);

                });
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public Task<AvatarDetail> GetAvatarDetailAsync(string username)
        {
            try
            {
                return new Task<AvatarDetail>(()=>{

                    AvatarDetail avatar = null;
                    AvatarDetailModel avatarModel = dataBase.AvatarDetails.FirstOrDefault(x => x.Username.Equals(username));

                    if (avatarModel == null){
                        return(avatar);
                    }

                    LoadAvatarDetailReferences(avatarModel);
                    avatar=avatarModel.GetAvatar();

                    return(avatar);

                });
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public AvatarDetail GetAvatarDetailByEmail(string avatarEmail)
        {
            AvatarDetail avatar = null;
            try
            {
                AvatarDetailModel avatarModel = dataBase.AvatarDetails.FirstOrDefault(x => x.Email.Equals(avatarEmail));

                if (avatarModel == null){
                    return(avatar);
                }

                LoadAvatarDetailReferences(avatarModel);
                avatar=avatarModel.GetAvatar();

            }
            catch(Exception ex)
            {
                throw ex;
            }
            return(avatar);
        }

        public Task<AvatarDetail> GetAvatarDetailByEmailAsync(string avatarEmail)
        {
            try
            {
                return new Task<AvatarDetail>(()=>{

                    AvatarDetail avatar = null;
                    AvatarDetailModel avatarModel = dataBase.AvatarDetails.FirstOrDefault(x => x.Email.Equals(avatarEmail));

                    if (avatarModel == null){
                        return(avatar);
                    }

                    LoadAvatarDetailReferences(avatarModel);
                    avatar=avatarModel.GetAvatar();

                    return(avatar);

                });
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public IEnumerable<AvatarDetail> GetAvatarDetails()
        {
            List<AvatarDetail> detailsList=new List<AvatarDetail>();
            try{

                List<AvatarDetailModel> detailModels=dataBase.AvatarDetails.ToList<AvatarDetailModel>();
                foreach (AvatarDetailModel model in detailModels)
                {
                    LoadAvatarDetailReferences(model);
                    detailsList.Add(model.GetAvatar());
                }

            }
            catch(Exception ex){
                throw ex;
            }
            return(detailsList);
        }

        public Task<IEnumerable<AvatarDetail>> GetAvatarDetailsAsync()
        {
            try
            {
                return new Task<IEnumerable<AvatarDetail>>(()=>{

                    List<AvatarDetail> avatarsList=new List<AvatarDetail>();

                    List<AvatarDetailModel> detailModels=dataBase.AvatarDetails.ToList<AvatarDetailModel>();
                    foreach (AvatarDetailModel model in detailModels)
                    {
                        LoadAvatarDetailReferences(model);
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

        public AvatarDetail Update(AvatarDetail avatar)
        {
            try
            {
                AvatarDetailModel avatarModel=new AvatarDetailModel(avatar);

                dataBase.AvatarDetails.Update(avatarModel);
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

                    dataBase.Avatars.Update(avatarModel);
                    dataBase.SaveChangesAsync();
                    
                    return(avatar);

                });
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public Task<AvatarDetail> UpdateAsync(AvatarDetail avatar)
        {
            try
            {
                return new Task<AvatarDetail>(()=>{

                    AvatarDetailModel avatarModel=new AvatarDetailModel(avatar);

                    dataBase.AvatarDetails.Update(avatarModel);
                    dataBase.SaveChangesAsync();
                    
                    return(avatar);

                });
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void LoadAvatarDetailReferences(AvatarDetailModel avatarModel){

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
                    .Collection<ProviderKeyModel>(a => a.ProviderKey)
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
                    
            
            dataBase.Entry(avatarModel)
                    .Collection<MetaDataModel>(a => a.MetaData)
                    .Load();

        }

        private void LoadAvatarReferences(AvatarModel avatarModel){

            dataBase.Entry(avatarModel)
                    .Collection<RefreshTokenModel>(a => a.RefreshTokens)
                    .Load();
            
            //TODO: Check is this needed?
            //dataBase.Entry(avatarModel)
            //        .Collection<ProviderKeyModel>(a => a.ProviderKey)
            //        .Load();
            
            dataBase.Entry(avatarModel)
                    .Collection<MetaDataModel>(a => a.MetaData)
                    .Load();
        }
    }
}