using System;
using System.Linq;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Holons;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
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
        
        public OASISResult<Avatar> Add(Avatar avatar)
        {
            OASISResult<Avatar> result = new OASISResult<Avatar>();
            try
            {
                avatar.Id = Guid.NewGuid();
                avatar.CreatedProviderType = new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS);

                if (AvatarManager.LoggedInAvatar != null)
                    avatar.CreatedByAvatarId = AvatarManager.LoggedInAvatar.Id;

                avatar.CreatedDate = DateTime.Now;

                AvatarModel avatarModel=new AvatarModel(avatar);
                dataBase.Avatars.Add(avatarModel);
                
                dataBase.SaveChanges();

                // avatarModel.ProviderKey.Add(new ProviderKeyModel(ProviderType.SQLLiteDBOASIS, avatarModel.Id));
                avatar.ProviderKey[ProviderType.SQLLiteDBOASIS] = avatarModel.Id;
                dataBase.SaveChanges();

                result.Result = avatar;
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error occurred adding avatar in SQLLiteOASIS Provider.";
                result.Exception = ex;
            }
            return result;
        }

        public OASISResult<AvatarDetail> Add(AvatarDetail avatar)
        {
            OASISResult<AvatarDetail> result = new OASISResult<AvatarDetail>();
            try
            {
                avatar.Id = Guid.NewGuid();
                avatar.CreatedProviderType = new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS);

                if (AvatarManager.LoggedInAvatar != null)
                    avatar.CreatedByAvatarId = AvatarManager.LoggedInAvatar.Id;

                avatar.CreatedDate = DateTime.Now;

                AvatarDetailModel avatarModel=new AvatarDetailModel(avatar);
                dataBase.AvatarDetails.Add(avatarModel);

                dataBase.SaveChanges();

                // avatarModel.ProviderKey.Add(new ProviderKeyModel(ProviderType.SQLLiteDBOASIS, avatarModel.Id));
                avatar.ProviderKey[ProviderType.SQLLiteDBOASIS] = avatarModel.Id;
                dataBase.SaveChanges();

                result.Result = avatar;
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error occurred adding avatarDetail in SQLLiteOASIS Provider.";
                result.Exception = ex;
            }
            return result;
        }

        public async Task<OASISResult<Avatar>> AddAsync(Avatar avatar)
        {
            OASISResult<Avatar> result = new OASISResult<Avatar>();
            try
            {
                avatar.Id = Guid.NewGuid();
                avatar.CreatedProviderType = new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS);

                if (AvatarManager.LoggedInAvatar != null)
                    avatar.CreatedByAvatarId = AvatarManager.LoggedInAvatar.Id;

                avatar.CreatedDate = DateTime.Now;

                AvatarModel avatarModel=new AvatarModel(avatar);
                await dataBase.Avatars.AddAsync(avatarModel);
                
                await dataBase.SaveChangesAsync();

                // avatarModel.ProviderKey.Add(new ProviderKeyModel(ProviderType.SQLLiteDBOASIS, avatarModel.Id));
                avatar.ProviderKey[ProviderType.SQLLiteDBOASIS] = avatarModel.Id;
                await dataBase.SaveChangesAsync();

                result.Result = avatar;
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error occurred adding avatar in SQLLiteOASIS Provider.";
                result.Exception = ex;
            }
            return result;
        }

        public async Task<OASISResult<AvatarDetail>> AddAsync(AvatarDetail avatar)
        {
            OASISResult<AvatarDetail> result = new OASISResult<AvatarDetail>();
            try
            {
                avatar.Id = Guid.NewGuid();
                avatar.CreatedProviderType = new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS);

                if (AvatarManager.LoggedInAvatar != null)
                    avatar.CreatedByAvatarId = AvatarManager.LoggedInAvatar.Id;

                avatar.CreatedDate = DateTime.Now;

                AvatarDetailModel avatarModel=new AvatarDetailModel(avatar);
                await dataBase.AvatarDetails.AddAsync(avatarModel);
                await dataBase.SaveChangesAsync();

                // avatarModel.ProviderKey.Add(new ProviderKeyModel(ProviderType.SQLLiteDBOASIS, avatarModel.Id));
                avatar.ProviderKey[ProviderType.SQLLiteDBOASIS] = avatarModel.Id;
                await dataBase.SaveChangesAsync();

                result.Result = avatar;
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error occurred adding avatarDetail in SQLLiteOASIS Provider.";
                result.Exception = ex;
            }
            return result;
        }

        public OASISResult<bool> Delete(Guid id, bool softDelete = true)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            try
            {
                String convertedId = id.ToString();
                AvatarModel deletingModel = dataBase.Avatars.FirstOrDefault(x => x.Id.Equals(convertedId));

                if(deletingModel != null){

                    if (softDelete)
                    {
                        //LoadAvatarReferences(deletingModel);

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
                }
                result.Result = true;
            }
            catch(Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error occurred deleting avatar in SQLLiteOASIS Provider.";
                result.Exception = ex;
            }
            return(result);
        }

        public OASISResult<bool> Delete(string userName, bool softDelete = true)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            try
            {
                AvatarModel deletingModel = dataBase.Avatars.FirstOrDefault(x => x.Username.Equals(userName));

                if(deletingModel != null){

                    if (softDelete)
                    {
                        //LoadAvatarReferences(deletingModel);

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
                }
                result.Result = true;
            }
            catch(Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error occurred deleting avatar in SQLLiteOASIS Provider.";
                result.Exception = ex;
            }
            return(result);
        }

        public async Task<OASISResult<bool>> DeleteAsync(Guid id, bool softDelete = true)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            try{

                String convertedId = id.ToString();
                AvatarModel deletingModel = dataBase.Avatars.FirstOrDefault(x => x.Id.Equals(convertedId));

                if(deletingModel != null){

                    if (softDelete)
                    {
                        //LoadAvatarReferences(deletingModel);

                        if (AvatarManager.LoggedInAvatar != null)
                            deletingModel.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id.ToString();

                        deletingModel.DeletedDate = DateTime.Now;                    
                        dataBase.Avatars.Update(deletingModel);
                    }
                    else
                    {
                        dataBase.Avatars.Remove(deletingModel);
                    }

                    await dataBase.SaveChangesAsync();
                    
                }

            }
            catch(Exception ex){

                result.IsError = true;
                result.Message = $"Error occurred deleting avatar in SQLLiteOASIS Provider.";
                result.Exception = ex;
            }
            return(result);
        }

        public async Task<OASISResult<bool>> DeleteAsync(string userName, bool softDelete = true)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            try{

                AvatarModel deletingModel = dataBase.Avatars.FirstOrDefault(x => x.Username.Equals(userName));

                if(deletingModel != null){

                    if (softDelete)
                    {
                        //LoadAvatarReferences(deletingModel);

                        if (AvatarManager.LoggedInAvatar != null)
                            deletingModel.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id.ToString();

                        deletingModel.DeletedDate = DateTime.Now;                    
                        dataBase.Avatars.Update(deletingModel);
                    }
                    else
                    {
                        dataBase.Avatars.Remove(deletingModel);
                    }

                    await dataBase.SaveChangesAsync();
                    
                }
                result.Result = true;

            }
            catch(Exception ex){

                result.IsError = true;
                result.Message = $"Error occurred deleting avatar in SQLLiteOASIS Provider.";
                result.Exception = ex;
            }
            return(result);
        }

        public OASISResult<bool> DeleteByEmail(string avatarEmail, bool softDelete = true)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            try
            {
                AvatarModel deletingModel = dataBase.Avatars.FirstOrDefault(x => x.Email.Equals(avatarEmail));

                if(deletingModel != null){

                    if (softDelete)
                    {
                        //LoadAvatarReferences(deletingModel);

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
                    
                }
                result.Result = true;

            }
            catch(Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error occurred deleting avatar in SQLLiteOASIS Provider.";
                result.Exception = ex;
            }
            return(result);
        }

        public async Task<OASISResult<bool>> DeleteByEmailAsync(string avatarEmail, bool softDelete = true)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            try
            {
                AvatarModel deletingModel = dataBase.Avatars.FirstOrDefault(x => x.Email.Equals(avatarEmail));

                if(deletingModel != null){

                    if (softDelete)
                    {
                        //LoadAvatarReferences(deletingModel);

                        if (AvatarManager.LoggedInAvatar != null)
                            deletingModel.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id.ToString();

                        deletingModel.DeletedDate = DateTime.Now;                    
                        dataBase.Avatars.Update(deletingModel);
                    }
                    else
                    {
                        dataBase.Avatars.Remove(deletingModel);
                    }

                    await dataBase.SaveChangesAsync();
                    
                }
            }
            catch(Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error occurred deleting avatar in SQLLiteOASIS Provider.";
                result.Exception = ex;
            }
            return(result);
        }

        public OASISResult<Avatar> GetAvatar(Guid id)
        {
            OASISResult<Avatar> result=new OASISResult<Avatar>();
            String convertedId = id.ToString();
            try
            {
                AvatarModel avatarModel = dataBase.Avatars.AsNoTracking().FirstOrDefault(x => x.Id.Equals(convertedId));

                if (avatarModel != null){
                    
                    LoadAvatarReferences(avatarModel);
                    result.Result=avatarModel.GetAvatar();
                }

            }
            catch(Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error occurred getting avatar in SQLLiteOASIS Provider.";
                result.Exception = ex;
            }
            return(result);
        }

        public OASISResult<Avatar> GetAvatar(string username)
        {
            OASISResult<Avatar> result=new OASISResult<Avatar>();
            try
            {
                AvatarModel avatarModel = dataBase.Avatars.AsNoTracking().FirstOrDefault(x => x.Username.Equals(username));

                if (avatarModel != null){

                    LoadAvatarReferences(avatarModel);
                    result.Result=avatarModel.GetAvatar();
                }

            }
            catch(Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error occurred getting avatar in SQLLiteOASIS Provider.";
                result.Exception = ex;
            }
            return(result);
        }

        public OASISResult<Avatar> GetAvatar(string username, string password)
        {
            OASISResult<Avatar> result=new OASISResult<Avatar>();
            try
            {
                AvatarModel avatarModel = dataBase.Avatars.AsNoTracking().FirstOrDefault(x => x.Username.Equals(username) && x.Password.Equals(password));

                if (avatarModel != null){

                    LoadAvatarReferences(avatarModel);
                    result.Result=avatarModel.GetAvatar();
                }

            }
            catch(Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error occurred getting avatar in SQLLiteOASIS Provider.";
                result.Exception = ex;
            }
            return(result);
        }

        public async Task<OASISResult<Avatar>> GetAvatarAsync(Guid id)
        {
            OASISResult<Avatar> result=new OASISResult<Avatar>();
            try
            {
                String convertedId = id.ToString();
                AvatarModel avatarModel = dataBase.Avatars.AsNoTracking().FirstOrDefault(x => x.Id.Equals(convertedId));

                if (avatarModel != null){

                    Action loadAction = delegate(){LoadAvatarReferences(avatarModel);};
                    Task loadReferencesTask = new Task(loadAction);

                    await loadReferencesTask;

                    result.Result=avatarModel.GetAvatar();
                }
            }
            catch(Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error occurred getting avatar in SQLLiteOASIS Provider.";
                result.Exception = ex;
            }
            return(result);
        }

        public async Task<OASISResult<Avatar>> GetAvatarAsync(string username)
        {
            OASISResult<Avatar> result=new OASISResult<Avatar>();
            try
            {
                AvatarModel avatarModel = dataBase.Avatars.AsNoTracking().FirstOrDefault(x => x.Username.Equals(username));

                if (avatarModel != null){
                    
                    Action loadAction = delegate(){LoadAvatarReferences(avatarModel);};
                    Task loadReferencesTask = new Task(loadAction);

                    await loadReferencesTask;

                    result.Result=avatarModel.GetAvatar();
                }
            }
            catch(Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error occurred getting avatar in SQLLiteOASIS Provider.";
                result.Exception = ex;
            }
            return(result);
        }

        public async Task<OASISResult<Avatar>> GetAvatarAsync(string username, string password)
        {
            OASISResult<Avatar> result=new OASISResult<Avatar>();
            try
            {
                AvatarModel avatarModel = dataBase.Avatars.AsNoTracking().FirstOrDefault(x => x.Username.Equals(username) && x.Password.Equals(password));

                if (avatarModel != null){
                    
                    Action loadAction = delegate(){LoadAvatarReferences(avatarModel);};
                    Task loadReferencesTask = new Task(loadAction);

                    await loadReferencesTask;

                    result.Result=avatarModel.GetAvatar();
                }
            }
            catch(Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error occurred getting avatar in SQLLiteOASIS Provider.";
                result.Exception = ex;
            }
            return(result);
        }

        public async Task<OASISResult<Avatar>> GetAvatarByEmailAsync(string avatarEmail)
        {
            OASISResult<Avatar> result=new OASISResult<Avatar>();
            try
            {
                AvatarModel avatarModel = dataBase.Avatars.AsNoTracking().FirstOrDefault(x => x.Email.Equals(avatarEmail));
                if (avatarModel != null){

                    Action loadAction = delegate(){LoadAvatarReferences(avatarModel);};
                    Task loadReferencesTask = new Task(loadAction);

                    await loadReferencesTask;

                    result.Result=avatarModel.GetAvatar();
                }
            }
            catch(Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error occurred getting avatar in SQLLiteOASIS Provider.";
                result.Exception = ex;
            }
            return(result);
        }

        public OASISResult<Avatar> GetAvatarByEmail(string avatarEmail)
        {
            OASISResult<Avatar> result=new OASISResult<Avatar>();
            try
            {
                AvatarModel avatarModel = dataBase.Avatars.AsNoTracking().FirstOrDefault(x => x.Email.Equals(avatarEmail));

                if (avatarModel != null){
                    
                    LoadAvatarReferences(avatarModel);
                    result.Result=avatarModel.GetAvatar();
                }
            }
            catch(Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error occurred getting avatar in SQLLiteOASIS Provider.";
                result.Exception = ex;
            }
            return(result);
        }

        public OASISResult<AvatarDetail> GetAvatarDetail(Guid id)
        {
            OASISResult<AvatarDetail> result=new OASISResult<AvatarDetail>();
            try
            {
                String convertedId = id.ToString();
                AvatarDetailModel avatarModel = dataBase.AvatarDetails.AsNoTracking().FirstOrDefault(x => x.Id.Equals(convertedId));

                if (avatarModel != null){

                    LoadAvatarDetailReferences(avatarModel);
                    result.Result=avatarModel.GetAvatar();
                }

            }
            catch(Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error occurred getting AvatarDetail in SQLLiteOASIS Provider.";
                result.Exception = ex;
            }
            return(result);
        }

        public OASISResult<AvatarDetail> GetAvatarDetail(string username)
        {
            OASISResult<AvatarDetail> result=new OASISResult<AvatarDetail>();
            try
            {
                AvatarDetailModel avatarModel = dataBase.AvatarDetails.AsNoTracking().FirstOrDefault(x => x.Username.Equals(username));

                if (avatarModel != null){
                    
                    LoadAvatarDetailReferences(avatarModel);
                    result.Result=avatarModel.GetAvatar();
                }

            }
            catch(Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error occurred getting AvatarDetail in SQLLiteOASIS Provider.";
                result.Exception = ex;
            }
            return(result);
        }

        public async Task<OASISResult<AvatarDetail>> GetAvatarDetailAsync(Guid id)
        {
            OASISResult<AvatarDetail> result=new OASISResult<AvatarDetail>();
            try
            {
                String convertedId = id.ToString();
                AvatarDetailModel avatarModel = dataBase.AvatarDetails.AsNoTracking().FirstOrDefault(x => x.Id.Equals(convertedId));

                if (avatarModel != null){

                    Action loadAction = delegate(){LoadAvatarDetailReferences(avatarModel);};
                    Task loadReferencesTask = new Task(loadAction);

                    await loadReferencesTask;

                    result.Result=avatarModel.GetAvatar();
                }
            }
            catch(Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error occurred getting AvatarDetail in SQLLiteOASIS Provider.";
                result.Exception = ex;
            }
            return(result);
        }

        public async Task<OASISResult<AvatarDetail>> GetAvatarDetailAsync(string username)
        {
            OASISResult<AvatarDetail> result=new OASISResult<AvatarDetail>();
            try
            {
                AvatarDetailModel avatarModel = dataBase.AvatarDetails.AsNoTracking().FirstOrDefault(x => x.Username.Equals(username));
                if (avatarModel != null){
                    
                    Action loadAction = delegate(){LoadAvatarDetailReferences(avatarModel);};
                    Task loadReferencesTask = new Task(loadAction);

                    await loadReferencesTask;

                    result.Result=avatarModel.GetAvatar();
                }
            }
            catch(Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error occurred getting AvatarDetail in SQLLiteOASIS Provider.";
                result.Exception = ex;
            }
            return(result);
        }

        public OASISResult<AvatarDetail> GetAvatarDetailByEmail(string avatarEmail)
        {
            OASISResult<AvatarDetail> result=new OASISResult<AvatarDetail>();
            try
            {
                AvatarDetailModel avatarModel = dataBase.AvatarDetails.AsNoTracking().FirstOrDefault(x => x.Email.Equals(avatarEmail));

                if (avatarModel != null){
                    
                    LoadAvatarDetailReferences(avatarModel);
                    result.Result=avatarModel.GetAvatar();
                }

            }
            catch(Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error occurred getting AvatarDetail in SQLLiteOASIS Provider.";
                result.Exception = ex;
            }
            return(result);
        }

        public async Task<OASISResult<AvatarDetail>> GetAvatarDetailByEmailAsync(string avatarEmail)
        {
            OASISResult<AvatarDetail> result=new OASISResult<AvatarDetail>();
            try
            {
                AvatarDetailModel avatarModel = dataBase.AvatarDetails.AsNoTracking().FirstOrDefault(x => x.Email.Equals(avatarEmail));
                if (avatarModel != null){
                    
                    Action loadAction = delegate(){LoadAvatarDetailReferences(avatarModel);};
                    Task loadReferencesTask = new Task(loadAction);

                    await loadReferencesTask;

                    result.Result=avatarModel.GetAvatar();
                }

            }
            catch(Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error occurred getting AvatarDetail in SQLLiteOASIS Provider.";
                result.Exception = ex;
            }
            return(result);
        }

        public OASISResult<IEnumerable<AvatarDetail>> GetAvatarDetails()
        {
            OASISResult<IEnumerable<AvatarDetail>> result=new OASISResult<IEnumerable<AvatarDetail>>();
            try{
                List<AvatarDetail> details = new List<AvatarDetail>();

                List<AvatarDetailModel> detailModels=dataBase.AvatarDetails.AsNoTracking().ToList<AvatarDetailModel>();
                foreach (AvatarDetailModel model in detailModels)
                {
                    LoadAvatarDetailReferences(model);
                    details.Add(model.GetAvatar());
                }
                result.Result = details;

            }
            catch(Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error occurred getting AvatarDetail in SQLLiteOASIS Provider.";
                result.Exception = ex;
            }
            return(result);
        }

        public async Task<OASISResult<IEnumerable<AvatarDetail>>> GetAvatarDetailsAsync()
        {
            OASISResult<IEnumerable<AvatarDetail>> result=new OASISResult<IEnumerable<AvatarDetail>>();
            try
            {
                List<AvatarDetail> details=new List<AvatarDetail>();

                List<AvatarDetailModel> detailModels=dataBase.AvatarDetails.AsNoTracking().ToList<AvatarDetailModel>();
                foreach (AvatarDetailModel model in detailModels)
                {
                    Action loadAction = delegate(){LoadAvatarDetailReferences(model);};
                    Task loadReferencesTask = new Task(loadAction);

                    await loadReferencesTask;
                    
                    details.Add(model.GetAvatar());
                }

                result.Result = details;
            }
            catch(Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error occurred getting AvatarDetail in SQLLiteOASIS Provider.";
                result.Exception = ex;
            }
            return(result);
        }

        public OASISResult<List<Avatar>> GetAvatars()
        {
            OASISResult<List<Avatar>> result=new OASISResult<List<Avatar>>();

            try{

                List<Avatar> avatarsList=new List<Avatar>();

                List<AvatarModel> avatarModels=dataBase.Avatars.ToList<AvatarModel>();
                foreach (AvatarModel model in avatarModels)
                {
                    LoadAvatarReferences(model);
                    avatarsList.Add(model.GetAvatar());
                }
                result.Result = avatarsList;

            }
            catch(Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error occurred getting AvatarDetail in SQLLiteOASIS Provider.";
                result.Exception = ex;
            }
            return(result);
        }

        public async Task<OASISResult<List<Avatar>>> GetAvatarsAsync()
        {
            OASISResult<List<Avatar>> result=new OASISResult<List<Avatar>>();
            try
            {
                List<Avatar> avatarsList=new List<Avatar>();

                List<AvatarModel> avatarModels=dataBase.Avatars.ToList<AvatarModel>();
                foreach (AvatarModel model in avatarModels)
                {
                    Action loadAction = delegate(){LoadAvatarReferences(model);};
                    Task loadReferencesTask = new Task(loadAction);

                    await loadReferencesTask;
                    
                    avatarsList.Add(model.GetAvatar());
                }
                result.Result = avatarsList;
            }
            catch(Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error occurred getting AvatarDetail in SQLLiteOASIS Provider.";
                result.Exception = ex;
            }
            return(result);
        }

        public OASISResult<Avatar> Update(Avatar avatar)
        {
            OASISResult<Avatar> result=new OASISResult<Avatar>();
            try
            {
                if (AvatarManager.LoggedInAvatar != null)
                    avatar.ModifiedByAvatarId = AvatarManager.LoggedInAvatar.Id;

                avatar.ModifiedDate = DateTime.Now;  
                
                AvatarModel avatarModel=new AvatarModel(avatar);

                dataBase.Avatars.Update(avatarModel);
                dataBase.SaveChanges();

                result.Result = avatar;
            }
            catch(Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error occurred updating AvatarDetail in SQLLiteOASIS Provider.";
                result.Exception = ex;
            }
            return(result);
        }

        public OASISResult<AvatarDetail> Update(AvatarDetail avatar)
        {
            OASISResult<AvatarDetail> result=new OASISResult<AvatarDetail>();
            try
            {
                if (AvatarManager.LoggedInAvatar != null)
                    avatar.ModifiedByAvatarId = AvatarManager.LoggedInAvatar.Id;

                avatar.ModifiedDate = DateTime.Now;  

                AvatarDetailModel avatarModel=new AvatarDetailModel(avatar);

                dataBase.AvatarDetails.Update(avatarModel);
                dataBase.SaveChanges();

                result.Result = avatar;
            }
            catch(Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error occurred updating AvatarDetail in SQLLiteOASIS Provider.";
                result.Exception = ex;
            }
            return(result);
        }

        public async Task<OASISResult<Avatar>> UpdateAsync(Avatar avatar)
        {
            OASISResult<Avatar> result=new OASISResult<Avatar>();
            try
            {
                if (AvatarManager.LoggedInAvatar != null)
                    avatar.ModifiedByAvatarId = AvatarManager.LoggedInAvatar.Id;

                avatar.ModifiedDate = DateTime.Now;  

                AvatarModel avatarModel=new AvatarModel(avatar);

                dataBase.Avatars.Update(avatarModel);
                await dataBase.SaveChangesAsync();

                result.Result = avatar;
            }
            catch(Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error occurred updating AvatarDetail in SQLLiteOASIS Provider.";
                result.Exception = ex;
            }
            return(result);
        }

        public async Task<OASISResult<AvatarDetail>> UpdateAsync(AvatarDetail avatar)
        {
            OASISResult<AvatarDetail> result=new OASISResult<AvatarDetail>();
            try
            {
                if (AvatarManager.LoggedInAvatar != null)
                    avatar.ModifiedByAvatarId = AvatarManager.LoggedInAvatar.Id;

                avatar.ModifiedDate = DateTime.Now;  

                AvatarDetailModel avatarModel=new AvatarDetailModel(avatar);

                dataBase.AvatarDetails.Update(avatarModel);
                await dataBase.SaveChangesAsync();

                result.Result = avatar;
            }
            catch(Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error occurred updating AvatarDetail in SQLLiteOASIS Provider.";
                result.Exception = ex;
            }
            return(result);
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