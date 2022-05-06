using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;
using Avatar = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Avatar;
using AvatarDetail = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.AvatarDetail;
using Holon = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Holon;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS
{
    //TODO: Implement OASISResult properly on below methods! :)
    public class MongoDBOASIS : OASISStorageProviderBase, IOASISDBStorageProvider, IOASISNETProvider, IOASISSuperStar
    {
        public MongoDbContext Database { get; set; }
        private AvatarRepository _avatarRepository = null;
        private HolonRepository _holonRepository = null;
        private SearchRepository _searchRepository = null;

        public string ConnectionString { get; set; }
        public string DBName { get; set; }
        public bool IsVersionControlEnabled { get; set; }

        public MongoDBOASIS(string connectionString, string dbName)
        {
            ConnectionString = connectionString;
            DBName = dbName;

            this.ProviderName = "MongoDBOASIS";
            this.ProviderDescription = "MongoDB Atlas Provider";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.MongoDBOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);

            /*
            ConventionRegistry.Register(
                   "DictionaryRepresentationConvention",
                   new ConventionPack { new DictionaryRepresentationConvention(DictionaryRepresentation.ArrayOfArrays) },
                   _ => true);*/
        }

        public override OASISResult<bool> ActivateProvider()
        {
            if (Database == null)
            {
                Database = new MongoDbContext(ConnectionString, DBName);
                _avatarRepository = new AvatarRepository(Database);
                _holonRepository = new HolonRepository(Database);
                _searchRepository = new SearchRepository(Database);
            }

            return base.ActivateProvider();
        }

        public override OASISResult<bool> DeActivateProvider()
        {
            //TODO: {URGENT} Disconnect, Dispose and release resources here.
            if (Database != null)
            {
                Database.MongoDB = null;
                Database.MongoClient = null;
                Database = null;
            }

            return base.DeActivateProvider();
        }

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            return ConvertMongoEntitysToOASISAvatars(await _avatarRepository.GetAvatarsAsync());
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
        {
            return ConvertMongoEntitysToOASISAvatars(_avatarRepository.GetAvatars());
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
        {
            return ConvertMongoEntityToOASISAvatar(_avatarRepository.GetAvatar(x => x.Email == avatarEmail));
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
        {
            return ConvertMongoEntityToOASISAvatar(_avatarRepository.GetAvatar(x => x.Username == avatarUsername));
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(string username, int version = 0)
        {
            return ConvertMongoEntityToOASISAvatar(await _avatarRepository.GetAvatarAsync(username));
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            return ConvertMongoEntityToOASISAvatar(await _avatarRepository.GetAvatarAsync(x => x.Username == avatarUsername));
        }

        public override OASISResult<IAvatar> LoadAvatar(string username, int version = 0)
        {
            return ConvertMongoEntityToOASISAvatar(_avatarRepository.GetAvatar(username));
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid Id, int version = 0)
        {
            return ConvertMongoEntityToOASISAvatar(await _avatarRepository.GetAvatarAsync(Id));
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            return ConvertMongoEntityToOASISAvatar(await _avatarRepository.GetAvatarAsync(x => x.Email == avatarEmail));
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid Id, int version = 0)
        {
            return ConvertMongoEntityToOASISAvatar(_avatarRepository.GetAvatar(Id));
        }

        //public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(string username, string password, int version = 0)
        //{
        //    return ConvertMongoEntityToOASISAvatar(await _avatarRepository.GetAvatarAsync(username, password));
        //}

        //public override OASISResult<IAvatar> LoadAvatar(string username, string password, int version = 0)
        //{
        //    return new OASISResult<IAvatar>(ConvertMongoEntityToOASISAvatar(_avatarRepository.GetAvatar(username, password)));
        //}

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            return ConvertMongoEntityToOASISAvatar(avatar.IsNewHolon ?
               await _avatarRepository.AddAsync(ConvertOASISAvatarToMongoEntity(avatar)) :
               await _avatarRepository.UpdateAsync(ConvertOASISAvatarToMongoEntity(avatar)));
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatar)
        {
            return ConvertMongoEntityToOASISAvatarDetail(avatar.IsNewHolon ?
               _avatarRepository.Add(ConvertOASISAvatarDetailToMongoEntity(avatar)) :
               _avatarRepository.Update(ConvertOASISAvatarDetailToMongoEntity(avatar)));
        }

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatar)
        {
            return ConvertMongoEntityToOASISAvatarDetail(avatar.IsNewHolon ?
               await _avatarRepository.AddAsync(ConvertOASISAvatarDetailToMongoEntity(avatar)) :
               await _avatarRepository.UpdateAsync(ConvertOASISAvatarDetailToMongoEntity(avatar)));
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar)
        {
            return ConvertMongoEntityToOASISAvatar(avatar.IsNewHolon ?
                _avatarRepository.Add(ConvertOASISAvatarToMongoEntity(avatar)) :
                _avatarRepository.Update(ConvertOASISAvatarToMongoEntity(avatar)));
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
        {
            return _avatarRepository.Delete(x => x.Username == avatarUsername, softDelete);
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            return await _avatarRepository.DeleteAsync(id, softDelete);
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        {
            return await _avatarRepository.DeleteAsync(x => x.Email == avatarEmail, softDelete);
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
        {
            return await _avatarRepository.DeleteAsync(x => x.Username == avatarUsername, softDelete);
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
        {
            return _avatarRepository.Delete(id, softDelete);
        }

        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
        {
            return _avatarRepository.Delete(x => x.Email == avatarEmail, softDelete);
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarForProviderKeyAsync(string providerKey, int version = 0)
        {
            return ConvertMongoEntityToOASISAvatar(await _avatarRepository.GetAvatarAsync(providerKey));
        }

        public override OASISResult<IAvatar> LoadAvatarForProviderKey(string providerKey, int version = 0)
        {
            return ConvertMongoEntityToOASISAvatar(_avatarRepository.GetAvatar(providerKey));
        }

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
        {
            return _avatarRepository.Delete(providerKey, softDelete);
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            return await _avatarRepository.DeleteAsync(providerKey, softDelete);
        }


        //TODO: {URGENT} FIX BEB SEARCH TO WORK WITH ISearchParams instead of string as it use to be!
        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchTerm, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return new OASISResult<ISearchResults>(await _searchRepository.SearchAsync(searchTerm));
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0)
        {
            return ConvertMongoEntityToOASISAvatarDetail(_avatarRepository.GetAvatarDetail(x => x.Username == avatarUsername));
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            return ConvertMongoEntityToOASISAvatarDetail(await _avatarRepository.GetAvatarDetailAsync(id));
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
        {
            return ConvertMongoEntityToOASISAvatarDetail(await _avatarRepository.GetAvatarDetailAsync(x => x.Username == avatarUsername));
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
        {
            return ConvertMongoEntityToOASISAvatarDetail(await _avatarRepository.GetAvatarDetailAsync(x => x.Email == avatarEmail));
        }

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            return ConvertMongoEntitysToOASISAvatarDetails(await _avatarRepository.GetAvatarDetailsAsync());
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
        {
            return ConvertMongoEntityToOASISAvatarDetail(_avatarRepository.GetAvatarDetail(id));
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0)
        {
            return ConvertMongoEntityToOASISAvatarDetail(_avatarRepository.GetAvatarDetail(x => x.Email == avatarEmail));
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
        {
            return ConvertMongoEntitysToOASISAvatarDetails(_avatarRepository.GetAvatarDetails());
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            //TODO: Finish implementing OASISResult properly...
            return new OASISResult<IHolon>(ConvertMongoEntityToOASISHolon(new OASISResult<Holon>(await _holonRepository.GetHolonAsync(id))).Result);
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            //TODO: Finish implementing OASISResult properly...
            return new OASISResult<IHolon>(ConvertMongoEntityToOASISHolon(new OASISResult<Holon>(_holonRepository.GetHolon(id))).Result);
        }


        //public override T LoadHolon<T>(Guid id)
        //{
        //    return ConvertMongoEntityToOASISHolon(new OASISResult<Holon>(_holonRepository.GetHolon(id))).Result;
        //}

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            //TODO: Finish implementing OASISResult properly...
            return new OASISResult<IHolon>(ConvertMongoEntityToOASISHolon(new OASISResult<Holon>(await _holonRepository.GetHolonAsync(providerKey))).Result);
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            //TODO: Finish implementing OASISResult properly...
            return new OASISResult<IHolon>(ConvertMongoEntityToOASISHolon(new OASISResult<Holon>(_holonRepository.GetHolon(providerKey))).Result);
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return new OASISResult<IEnumerable<IHolon>>(ConvertMongoEntitysToOASISHolons(await _holonRepository.GetAllHolonsForParentAsync(id, type)));
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return new OASISResult<IEnumerable<IHolon>>(ConvertMongoEntitysToOASISHolons(_holonRepository.GetAllHolonsForParent(id, type)));
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();
            OASISResult<IEnumerable<Holon>> repoResult = await _holonRepository.GetAllHolonsForParentAsync(providerKey, type);

            if (repoResult.IsError)
            {
                result.IsError = true;
                result.Message = repoResult.Message;
            }
            else
                result.Result = ConvertMongoEntitysToOASISHolons(repoResult.Result);

            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return new OASISResult<IEnumerable<IHolon>>(ConvertMongoEntitysToOASISHolons(_holonRepository.GetAllHolonsForParent(providerKey, type)));
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return new OASISResult<IEnumerable<IHolon>>(ConvertMongoEntitysToOASISHolons(await _holonRepository.GetAllHolonsAsync(type)));
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return new OASISResult<IEnumerable<IHolon>>(ConvertMongoEntitysToOASISHolons(_holonRepository.GetAllHolons(type)));
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true)
        {
            OASISResult<IHolon> result =  holon.IsNewHolon
                ? ConvertMongoEntityToOASISHolon(await _holonRepository.AddAsync(ConvertOASISHolonToMongoEntity(holon)))
                : ConvertMongoEntityToOASISHolon(await _holonRepository.UpdateAsync(ConvertOASISHolonToMongoEntity(holon)));

            if (!result.IsError && result.Result != null && saveChildren && result.Result.Children != null && result.Result.Children.Count() > 0)
            {
                OASISResult<IEnumerable<IHolon>> saveChildrenResult = SaveHolons(result.Result.Children);

                if (!saveChildrenResult.IsError && saveChildrenResult.Result != null)
                    result.Result.Children = saveChildrenResult.Result;
                else
                {
                    result.IsError = true;
                    result.Message = $"Holon with id {holon.Id} and name {holon.Name} saved but it's children failed to save. Reason: {saveChildrenResult.Message}";
                }
            }

            return result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true)
        {
            OASISResult<IHolon> result = holon.IsNewHolon
                ? ConvertMongoEntityToOASISHolon(_holonRepository.Add(ConvertOASISHolonToMongoEntity(holon)))
                : ConvertMongoEntityToOASISHolon(_holonRepository.Update(ConvertOASISHolonToMongoEntity(holon)));

            if (!result.IsError && result.Result != null && saveChildren && result.Result.Children != null && result.Result.Children.Count() > 0)
            {
                OASISResult<IEnumerable<IHolon>> saveChildrenResult = SaveHolons(result.Result.Children);

                if (!saveChildrenResult.IsError && saveChildrenResult.Result != null)
                    result.Result.Children = saveChildrenResult.Result;
                else
                {
                    result.IsError = true;
                    result.Message = $"Holon with id {holon.Id} and name {holon.Name} saved but it's children failed to save. Reason: {saveChildrenResult.Message}";
                }
            }

            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true)
        {
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();
            List<IHolon> savedHolons = new List<IHolon>();

            if (holons == null)
            {
                result.Message = "Holons is null";
                result.IsWarning = true;
                result.IsSaved = false;
                return result;
            }

            if (holons.Count() == 0)
            {
                result.Message = "Holons collection is empty.";
                result.IsWarning = true;
                result.IsSaved = false;
                return result;
            }

            // Recursively save all child holons.
            foreach (IHolon holon in holons)
            {
                OASISResult<IHolon> holonResult = SaveHolon(holon);

                if (!holonResult.IsError && holonResult.Result != null)
                {
                    if (saveChildren && holonResult.Result.Children != null && holonResult.Result.Children.Count() > 0)
                    {
                        OASISResult<IEnumerable<IHolon>> saveChildrenResult = SaveHolons(holonResult.Result.Children);

                        if (!saveChildrenResult.IsError && saveChildrenResult.Result != null)
                            holonResult.Result.Children = saveChildrenResult.Result;
                        else
                        {
                            result.IsError = true;
                            result.InnerMessages.Add($"Holon with id {holon.Id} and name {holon.Name} saved but it's children failed to save. Reason: {saveChildrenResult.Message}");
                        }
                    }

                    savedHolons.Add(holonResult.Result);
                }
                else
                {
                    result.IsError = true;
                    result.InnerMessages.Add($"Holon with id {holon.Id} and name {holon.Name} faild to save. Reason: {holonResult.Message}");
                }
            }

            result.Result = savedHolons.ToList();

            if (result.IsError)
                result.Message = "One or more errors occured saving the holons in the SQLLiteOASIS Provider. Please check the InnerMessages property for more infomration.";

            return result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true)
        {
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();
            List<IHolon> savedHolons = new List<IHolon>();

            if (holons == null)
            {
                result.Message = "Holons is null";
                result.IsWarning = true;
                result.IsSaved = false;
                return result;
            }

            if (holons.Count() == 0)
            {
                result.Message = "Holons collection is empty.";
                result.IsWarning = true;
                result.IsSaved = false;
                return result;
            }

            // Recursively save all child holons.
            foreach (IHolon holon in holons)
            {
                OASISResult<IHolon> holonResult = await SaveHolonAsync(holon);

                if (!holonResult.IsError && holonResult.Result != null)
                {
                    if (saveChildren && holonResult.Result.Children != null && holonResult.Result.Children.Count() > 0)
                    {
                        OASISResult<IEnumerable<IHolon>> saveChildrenResult = await SaveHolonsAsync(holonResult.Result.Children);

                        if (!saveChildrenResult.IsError && saveChildrenResult.Result != null)
                            holonResult.Result.Children = saveChildrenResult.Result;
                        else
                        {
                            result.IsError = true;
                            result.InnerMessages.Add($"Holon with id {holon.Id} and name {holon.Name} saved but it's children failed to save. Reason: {saveChildrenResult.Message}");
                        }
                    }

                    savedHolons.Add(holonResult.Result);
                }
                else
                {
                    result.IsError = true;
                    result.InnerMessages.Add($"Holon with id {holon.Id} and name {holon.Name} faild to save. Reason: {holonResult.Message}");
                }
            }

            result.Result = savedHolons.ToList();

            if (result.IsError)
                result.Message = "One or more errors occured saving the holons in the SQLLiteOASIS Provider. Please check the InnerMessages property for more infomration.";

            return result;
        }

        public override OASISResult<bool> DeleteHolon(Guid id, bool softDelete = true)
        {
            return new OASISResult<bool>(_holonRepository.Delete(id, softDelete));
        }

        public override async Task<OASISResult<bool>> DeleteHolonAsync(Guid id, bool softDelete = true)
        {
            return new OASISResult<bool>(await _holonRepository.DeleteAsync(id, softDelete));
        }

        public override OASISResult<bool> DeleteHolon(string providerKey, bool softDelete = true)
        {
            return new OASISResult<bool>(_holonRepository.Delete(providerKey, softDelete));
        }

        public override async Task<OASISResult<bool>> DeleteHolonAsync(string providerKey, bool softDelete = true)
        {
            return new OASISResult<bool>(await _holonRepository.DeleteAsync(providerKey, softDelete));
        }

        public IEnumerable<IHolon> GetHolonsNearMe(HolonType Type)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IPlayer> GetPlayersNearMe()
        {
            throw new NotImplementedException();
        }


        //IOASISSuperStar Interface Implementation

        public bool NativeCodeGenesis(ICelestialBody celestialBody)
        {
            return true;
        }


        private OASISResult<IEnumerable<IAvatar>> ConvertMongoEntitysToOASISAvatars(OASISResult<IEnumerable<Avatar>> avatars)
        {
            OASISResult<IEnumerable<IAvatar>> result = new OASISResult<IEnumerable<IAvatar>>();
            List<IAvatar> oasisAvatars = new List<IAvatar>();

            OASISResultCollectionToCollectionHelper<IEnumerable<Avatar>, IEnumerable<IAvatar>>.CopyResult(avatars, result);

            foreach (Avatar avatar in avatars.Result)
                oasisAvatars.Add(ConvertMongoEntityToOASISAvatar(new OASISResult<Avatar>(avatar)).Result);

            result.Result = oasisAvatars;
            return result;
        }

        private OASISResult<IEnumerable<IAvatarDetail>> ConvertMongoEntitysToOASISAvatarDetails(OASISResult<IEnumerable<AvatarDetail>> avatars)
        {
            OASISResult<IEnumerable<IAvatarDetail>> result = new OASISResult<IEnumerable<IAvatarDetail>>();
            List<IAvatarDetail> oasisAvatars = new List<IAvatarDetail>();

            OASISResultCollectionToCollectionHelper<IEnumerable<AvatarDetail>, IEnumerable<IAvatarDetail>>.CopyResult(avatars, result);

            foreach (AvatarDetail avatar in avatars.Result)
                oasisAvatars.Add(ConvertMongoEntityToOASISAvatarDetail(new OASISResult<AvatarDetail>(avatar)).Result);

            result.Result = oasisAvatars;
            return result;
        }

        private IEnumerable<IHolon> ConvertMongoEntitysToOASISHolons(IEnumerable<Holon> holons)
        {
            List<IHolon> oasisHolons = new List<IHolon>();

            foreach (Holon holon in holons)
            {
                OASISResult<IHolon> convertedResult = ConvertMongoEntityToOASISHolon(new OASISResult<Holon>(holon));
                
                if (!convertedResult.IsError && convertedResult.Result != null)
                    oasisHolons.Add(convertedResult.Result);
            }

            return oasisHolons;
        }

        private OASISResult<IAvatar> ConvertMongoEntityToOASISAvatar(OASISResult<Avatar> avatarResult)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();

            if (avatarResult.IsError || avatarResult.Result == null)
                return result;

            OASISResultHolonToHolonHelper<Avatar, IAvatar>.CopyResult(avatarResult, result);
            result.Result = new Core.Holons.Avatar();

            result.Result.IsNewHolon = false;
            result.Result.Id = avatarResult.Result.HolonId;
            result.Result.ProviderUniqueStorageKey = avatarResult.Result.ProviderUniqueStorageKey;
            result.Result.ProviderPrivateKey = avatarResult.Result.ProviderPrivateKey;
            result.Result.ProviderPublicKey = avatarResult.Result.ProviderPublicKey;
            result.Result.ProviderUsername = avatarResult.Result.ProviderUsername;
            result.Result.ProviderWalletAddress = avatarResult.Result.ProviderWalletAddress;
            result.Result.PreviousVersionId = avatarResult.Result.PreviousVersionId;
            result.Result.PreviousVersionProviderUniqueStorageKey = avatarResult.Result.PreviousVersionProviderUniqueStorageKey;
            result.Result.ProviderMetaData = avatarResult.Result.ProviderMetaData;
            result.Result.Description = avatarResult.Result.Description;
            result.Result.Title = avatarResult.Result.Title;
            result.Result.FirstName = avatarResult.Result.FirstName;
            result.Result.LastName = avatarResult.Result.LastName;
            result.Result.Email = avatarResult.Result.Email;
            result.Result.Password = avatarResult.Result.Password;
            result.Result.Username = avatarResult.Result.Username;
            result.Result.CreatedOASISType = avatarResult.Result.CreatedOASISType;
            //oasisAvatar.CreatedProviderType = new EnumValue<ProviderType>(avatarResult.Result.CreatedProviderType);
            result.Result.CreatedProviderType = avatarResult.Result.CreatedProviderType;
            result.Result.AvatarType = avatarResult.Result.AvatarType;
            result.Result.HolonType = avatarResult.Result.HolonType;
            // oasisAvatar.Image2D = avatarResult.Result.Image2D;
            //oasisAvatar.UmaJson = avatarResult.Result.UmaJson; //TODO: Not sure whether to include UmaJson or not? I think Unity guys said is it pretty large?
            //oasisAvatar.Karma = avatarResult.Result.Karma;
            //oasisAvatar.XP = avatarResult.Result.XP;
            result.Result.IsChanged = avatarResult.Result.IsChanged;
            result.Result.AcceptTerms = avatarResult.Result.AcceptTerms;
            result.Result.JwtToken = avatarResult.Result.JwtToken;
            result.Result.PasswordReset = avatarResult.Result.PasswordReset;
            result.Result.RefreshToken = avatarResult.Result.RefreshToken;
            result.Result.RefreshTokens = avatarResult.Result.RefreshTokens;
            result.Result.ResetToken = avatarResult.Result.ResetToken;
            result.Result.ResetTokenExpires = avatarResult.Result.ResetTokenExpires;
            result.Result.VerificationToken = avatarResult.Result.VerificationToken;
            result.Result.Verified = avatarResult.Result.Verified;
            result.Result.CreatedByAvatarId = Guid.Parse(avatarResult.Result.CreatedByAvatarId);
            result.Result.CreatedDate = avatarResult.Result.CreatedDate;
            result.Result.DeletedByAvatarId = Guid.Parse(avatarResult.Result.DeletedByAvatarId);
            result.Result.DeletedDate = avatarResult.Result.DeletedDate;
            result.Result.ModifiedByAvatarId = Guid.Parse(avatarResult.Result.ModifiedByAvatarId);
            result.Result.ModifiedDate = avatarResult.Result.ModifiedDate;
            result.Result.DeletedDate = avatarResult.Result.DeletedDate;
            result.Result.LastBeamedIn = avatarResult.Result.LastBeamedIn;
            result.Result.LastBeamedOut = avatarResult.Result.LastBeamedOut;
            result.Result.IsBeamedIn = avatarResult.Result.IsBeamedIn;
            result.Result.Version = avatarResult.Result.Version;
            result.Result.IsActive = avatarResult.Result.IsActive;

            return result;
        }

        private OASISResult<IAvatarDetail> ConvertMongoEntityToOASISAvatarDetail(OASISResult<AvatarDetail> avatar)
        {
            OASISResult<IAvatarDetail> result = new OASISResult<IAvatarDetail>();

            if (avatar == null || (avatar != null && avatar.Result == null))
                return new OASISResult<IAvatarDetail>() { Message = avatar.Message, IsError = true, IsLoaded = false};

            Core.Holons.AvatarDetail oasisAvatar = new Core.Holons.AvatarDetail();
            oasisAvatar.IsNewHolon = false;
            oasisAvatar.Id = avatar.Result.HolonId;
            oasisAvatar.ProviderUniqueStorageKey = avatar.Result.ProviderUniqueStorageKey;
            oasisAvatar.ProviderMetaData = avatar.Result.ProviderMetaData;
            oasisAvatar.PreviousVersionId = avatar.Result.PreviousVersionId;
            oasisAvatar.PreviousVersionProviderUniqueStorageKey = avatar.Result.PreviousVersionProviderUniqueStorageKey;
           // oasisAvatar.Title = avatar.Result.Title;
            oasisAvatar.Description = avatar.Result.Description;
            //oasisAvatar.FirstName = avatar.Result.FirstName;
            //oasisAvatar.LastName = avatar.Result.LastName;
            oasisAvatar.Email = avatar.Result.Email;
            oasisAvatar.Username = avatar.Result.Username;
            oasisAvatar.CreatedOASISType = avatar.Result.CreatedOASISType;
            oasisAvatar.CreatedProviderType = avatar.Result.CreatedProviderType;
           // oasisAvatar.AvatarType = avatar.Result.AvatarType;
            oasisAvatar.HolonType = avatar.Result.HolonType;
            oasisAvatar.IsChanged = avatar.Result.IsChanged;
            oasisAvatar.CreatedByAvatarId = Guid.Parse(avatar.Result.CreatedByAvatarId);
            oasisAvatar.CreatedDate = avatar.Result.CreatedDate;
            oasisAvatar.DeletedByAvatarId = Guid.Parse(avatar.Result.DeletedByAvatarId);
            oasisAvatar.DeletedDate = avatar.Result.DeletedDate;
            oasisAvatar.ModifiedByAvatarId = Guid.Parse(avatar.Result.ModifiedByAvatarId);
            oasisAvatar.ModifiedDate = avatar.Result.ModifiedDate;
            oasisAvatar.DeletedDate = avatar.Result.DeletedDate;
            oasisAvatar.Version = avatar.Result.Version;
            oasisAvatar.IsActive = avatar.Result.IsActive;
            oasisAvatar.Portrait = avatar.Result.Portrait;
            oasisAvatar.UmaJson = avatar.Result.UmaJson;
            //oasisAvatar.ProviderPrivateKey = avatar.Result.ProviderPrivateKey;
            //oasisAvatar.ProviderPublicKey = avatar.Result.ProviderPublicKey;
            //oasisAvatar.ProviderUsername = avatar.Result.ProviderUsername;
            //oasisAvatar.ProviderWalletAddress = avatar.Result.ProviderWalletAddress;
            oasisAvatar.XP = avatar.Result.XP;
            oasisAvatar.FavouriteColour = avatar.Result.FavouriteColour;
            oasisAvatar.STARCLIColour = avatar.Result.STARCLIColour;
            oasisAvatar.Skills = avatar.Result.Skills;
            oasisAvatar.Spells = avatar.Result.Spells;
            oasisAvatar.Stats = avatar.Result.Stats;
            oasisAvatar.SuperPowers = avatar.Result.SuperPowers;
            oasisAvatar.GeneKeys = avatar.Result.GeneKeys;
            oasisAvatar.HumanDesign = avatar.Result.HumanDesign;
            oasisAvatar.Gifts = avatar.Result.Gifts;
            oasisAvatar.Chakras = avatar.Result.Chakras;
            oasisAvatar.Aura = avatar.Result.Aura;
            oasisAvatar.Achievements = avatar.Result.Achievements;
            oasisAvatar.Inventory = avatar.Result.Inventory;
            oasisAvatar.Address = avatar.Result.Address;
           // oasisAvatar.AvatarType = avatar.Result.AvatarType;
            oasisAvatar.Country = avatar.Result.Country;
            oasisAvatar.County = avatar.Result.County;
            oasisAvatar.Address = avatar.Result.Address;
            oasisAvatar.Country = avatar.Result.Country;
            oasisAvatar.County = avatar.Result.County;
            oasisAvatar.DOB = avatar.Result.DOB;
            oasisAvatar.Landline = avatar.Result.Landline;
            oasisAvatar.Mobile = avatar.Result.Mobile;
            oasisAvatar.Postcode = avatar.Result.Postcode;
            oasisAvatar.Town = avatar.Result.Town;
            oasisAvatar.Karma = avatar.Result.Karma;
            oasisAvatar.KarmaAkashicRecords = avatar.Result.KarmaAkashicRecords;
            oasisAvatar.ParentHolonId = avatar.Result.ParentHolonId;
            oasisAvatar.ParentHolon = avatar.Result.ParentHolon;
            oasisAvatar.ParentZomeId = avatar.Result.ParentZomeId;
            oasisAvatar.ParentZome = avatar.Result.ParentZome;
            oasisAvatar.ParentCelestialBody = avatar.Result.ParentCelestialBody;
            oasisAvatar.ParentCelestialBodyId = avatar.Result.ParentCelestialBodyId;
            oasisAvatar.ParentCelestialSpace = avatar.Result.ParentCelestialSpace;
            oasisAvatar.ParentCelestialSpaceId = avatar.Result.ParentCelestialSpaceId;
            oasisAvatar.ParentOmniverse = avatar.Result.ParentOmniverse;
            oasisAvatar.ParentOmniverseId = avatar.Result.ParentOmniverseId;
            oasisAvatar.ParentDimension = avatar.Result.ParentDimension;
            oasisAvatar.ParentDimensionId = avatar.Result.ParentDimensionId;
            oasisAvatar.ParentMultiverse = avatar.Result.ParentMultiverse;
            oasisAvatar.ParentMultiverseId = avatar.Result.ParentMultiverseId;
            oasisAvatar.ParentUniverse = avatar.Result.ParentUniverse;
            oasisAvatar.ParentUniverseId = avatar.Result.ParentUniverseId;
            oasisAvatar.ParentGalaxyCluster = avatar.Result.ParentGalaxyCluster;
            oasisAvatar.ParentGalaxyClusterId = avatar.Result.ParentGalaxyClusterId;
            oasisAvatar.ParentGalaxy = avatar.Result.ParentGalaxy;
            oasisAvatar.ParentGalaxyId = avatar.Result.ParentGalaxyId;
            oasisAvatar.ParentSolarSystem = avatar.Result.ParentSolarSystem;
            oasisAvatar.ParentSolarSystemId = avatar.Result.ParentSolarSystemId;
            oasisAvatar.ParentGreatGrandSuperStar = avatar.Result.ParentGreatGrandSuperStar;
            oasisAvatar.ParentGreatGrandSuperStarId = avatar.Result.ParentGreatGrandSuperStarId;
            oasisAvatar.ParentGreatGrandSuperStar = avatar.Result.ParentGreatGrandSuperStar;
            oasisAvatar.ParentGrandSuperStarId = avatar.Result.ParentGrandSuperStarId;
            oasisAvatar.ParentGrandSuperStar = avatar.Result.ParentGrandSuperStar;
            oasisAvatar.ParentSuperStarId = avatar.Result.ParentSuperStarId;
            oasisAvatar.ParentSuperStar = avatar.Result.ParentSuperStar;
            oasisAvatar.ParentStarId = avatar.Result.ParentStarId;
            oasisAvatar.ParentStar = avatar.Result.ParentStar;
            oasisAvatar.ParentPlanetId = avatar.Result.ParentPlanetId;
            oasisAvatar.ParentPlanet = avatar.Result.ParentPlanet;
            oasisAvatar.ParentMoonId = avatar.Result.ParentMoonId;
            oasisAvatar.ParentMoon = avatar.Result.ParentMoon;
            oasisAvatar.Children = avatar.Result.Children;
            oasisAvatar.Nodes = avatar.Result.Nodes;

            result.Result = oasisAvatar;
            return result;
        }

        private Avatar ConvertOASISAvatarToMongoEntity(IAvatar avatar)
        {
            if (avatar == null)
                return null;

            Avatar mongoAvatar = new Avatar();

            if (avatar.ProviderUniqueStorageKey != null && avatar.ProviderUniqueStorageKey.ContainsKey(Core.Enums.ProviderType.MongoDBOASIS))
                mongoAvatar.Id = avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.MongoDBOASIS];

            //if (avatar.CreatedProviderType != null)
            //    mongoAvatar.CreatedProviderType = avatar.CreatedProviderType.Value;

            mongoAvatar.HolonId = avatar.Id;
            // mongoAvatar.AvatarId = avatar.Id;
            mongoAvatar.ProviderUniqueStorageKey = avatar.ProviderUniqueStorageKey;
            mongoAvatar.ProviderPrivateKey = avatar.ProviderPrivateKey;
            mongoAvatar.ProviderPublicKey = avatar.ProviderPublicKey;
            mongoAvatar.ProviderUsername = avatar.ProviderUsername;
            mongoAvatar.ProviderWalletAddress = avatar.ProviderWalletAddress;
            mongoAvatar.ProviderMetaData = avatar.ProviderMetaData;
            mongoAvatar.PreviousVersionId = avatar.PreviousVersionId;
            mongoAvatar.PreviousVersionProviderUniqueStorageKey = avatar.PreviousVersionProviderUniqueStorageKey;
            mongoAvatar.Name = avatar.Name;
            mongoAvatar.Description = avatar.Description;
            mongoAvatar.FirstName = avatar.FirstName;
            mongoAvatar.LastName = avatar.LastName;
            mongoAvatar.Email = avatar.Email;
            mongoAvatar.Password = avatar.Password;
            mongoAvatar.Title = avatar.Title;
            mongoAvatar.Username = avatar.Username;
            mongoAvatar.HolonType = avatar.HolonType;
            mongoAvatar.AvatarType = avatar.AvatarType;
            mongoAvatar.CreatedProviderType = avatar.CreatedProviderType;
            mongoAvatar.CreatedOASISType = avatar.CreatedOASISType;
            mongoAvatar.MetaData = avatar.MetaData;
           // mongoAvatar.Image2D = avatar.Image2D;
            mongoAvatar.AcceptTerms = avatar.AcceptTerms;
            mongoAvatar.JwtToken = avatar.JwtToken;
            mongoAvatar.PasswordReset = avatar.PasswordReset;
            mongoAvatar.RefreshToken = avatar.RefreshToken;
            mongoAvatar.RefreshTokens = avatar.RefreshTokens;
            mongoAvatar.ResetToken = avatar.ResetToken;
            mongoAvatar.ResetTokenExpires = avatar.ResetTokenExpires;
            mongoAvatar.VerificationToken = avatar.VerificationToken;
            mongoAvatar.Verified = avatar.Verified;
            //mongoAvatar.Karma = avatar.Karma;
           // mongoAvatar.XP = avatar.XP;
           // mongoAvatar.Image2D = avatar.Image2D;
            mongoAvatar.IsChanged = avatar.IsChanged;
            mongoAvatar.CreatedByAvatarId = avatar.CreatedByAvatarId.ToString();
            mongoAvatar.CreatedDate = avatar.CreatedDate;
            mongoAvatar.DeletedByAvatarId = avatar.DeletedByAvatarId.ToString();
            mongoAvatar.DeletedDate = avatar.DeletedDate;
            mongoAvatar.ModifiedByAvatarId = avatar.ModifiedByAvatarId.ToString();
            mongoAvatar.ModifiedDate = avatar.ModifiedDate;
            mongoAvatar.DeletedDate = avatar.DeletedDate;
            mongoAvatar.LastBeamedIn = avatar.LastBeamedIn;
            mongoAvatar.LastBeamedOut = avatar.LastBeamedOut;
            mongoAvatar.IsBeamedIn = avatar.IsBeamedIn;
            mongoAvatar.Version = avatar.Version;
            mongoAvatar.IsActive = avatar.IsActive;

            return mongoAvatar;
        }

        /*
        private AvatarDetail ConvertOASISAvatarToMongoEntity(IAvatarDetail avatarDetail)
        {
            if (avatarDetail == null)
                return null;

            AvatarDetail mongoAvatar = new AvatarDetail();

            if (avatarDetail.ProviderUniqueStorageKey != null && avatarDetail.ProviderUniqueStorageKey.ContainsKey(Core.Enums.ProviderType.MongoDBOASIS))
                mongoAvatar.Id = avatarDetail.ProviderUniqueStorageKey[Core.Enums.ProviderType.MongoDBOASIS];

            //if (avatar.CreatedProviderType != null)
            //    mongoAvatar.CreatedProviderType = avatar.CreatedProviderType.Value;

            mongoAvatar.HolonId = avatarDetail.Id;
            // mongoAvatar.AvatarId = avatarDetail.Id;
            mongoAvatar.ProviderUniqueStorageKey = avatarDetail.ProviderUniqueStorageKey;
            mongoAvatar.ProviderMetaData = avatarDetail.ProviderMetaData;
            mongoAvatar.PreviousVersionId = avatarDetail.PreviousVersionId;
            mongoAvatar.PreviousVersionProviderKey = avatarDetail.PreviousVersionProviderKey;
            mongoAvatar.Name = avatarDetail.Name;
            mongoAvatar.Description = avatarDetail.Description;
            mongoAvatar.FirstName = avatarDetail.FirstName;
            mongoAvatar.LastName = avatarDetail.LastName;
            mongoAvatar.Email = avatarDetail.Email;
            mongoAvatar.Title = avatarDetail.Title;
            mongoAvatar.Username = avatarDetail.Username;
            mongoAvatar.HolonType = avatarDetail.HolonType;
            mongoAvatar.AvatarType = avatarDetail.AvatarType;
            mongoAvatar.CreatedProviderType = avatarDetail.CreatedProviderType;
            mongoAvatar.CreatedOASISType = avatarDetail.CreatedOASISType;
            mongoAvatar.MetaData = avatarDetail.MetaData;
            mongoAvatar.Image2D = avatarDetail.Image2D;
            mongoAvatar.Karma = avatarDetail.Karma;
            mongoAvatar.XP = avatarDetail.XP;
            mongoAvatar.Image2D = avatarDetail.Image2D;
            mongoAvatar.IsChanged = avatarDetail.IsChanged;
            mongoAvatar.CreatedByAvatarId = avatarDetail.CreatedByAvatarId.ToString();
            mongoAvatar.CreatedDate = avatarDetail.CreatedDate;
            mongoAvatar.DeletedByAvatarId = avatarDetail.DeletedByAvatarId.ToString();
            mongoAvatar.DeletedDate = avatarDetail.DeletedDate;
            mongoAvatar.ModifiedByAvatarId = avatarDetail.ModifiedByAvatarId.ToString();
            mongoAvatar.ModifiedDate = avatarDetail.ModifiedDate;
            mongoAvatar.DeletedDate = avatarDetail.DeletedDate;
            mongoAvatar.Version = avatarDetail.Version;
            mongoAvatar.IsActive = avatarDetail.IsActive;


            return mongoAvatar;
        }*/

        private AvatarDetail ConvertOASISAvatarDetailToMongoEntity(IAvatarDetail avatar)
        {
            if (avatar == null)
                return null;

            AvatarDetail mongoAvatar = new AvatarDetail();

            if (avatar.ProviderUniqueStorageKey != null && avatar.ProviderUniqueStorageKey.ContainsKey(Core.Enums.ProviderType.MongoDBOASIS))
                mongoAvatar.Id = avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.MongoDBOASIS];

            // if (avatar.CreatedProviderType != null)
            //     mongoAvatar.CreatedProviderType = avatar.CreatedProviderType.Value;

            //Avatar Properties
            mongoAvatar.HolonId = avatar.Id;
            mongoAvatar.ProviderUniqueStorageKey = avatar.ProviderUniqueStorageKey;
            mongoAvatar.ProviderMetaData = avatar.ProviderMetaData;
            mongoAvatar.PreviousVersionId = avatar.PreviousVersionId;
            mongoAvatar.PreviousVersionProviderUniqueStorageKey = avatar.PreviousVersionProviderUniqueStorageKey;
            mongoAvatar.Name = avatar.Name;
            mongoAvatar.Description = avatar.Description;
            //mongoAvatar.FirstName = avatar.FirstName;
            //mongoAvatar.LastName = avatar.LastName;
            mongoAvatar.Email = avatar.Email;
            //mongoAvatar.Title = avatar.Title;
            mongoAvatar.Username = avatar.Username;
            mongoAvatar.HolonType = avatar.HolonType;
           // mongoAvatar.AvatarType = avatar.AvatarType;
            mongoAvatar.CreatedProviderType = avatar.CreatedProviderType;
            mongoAvatar.CreatedOASISType = avatar.CreatedOASISType;
            mongoAvatar.MetaData = avatar.MetaData;
            mongoAvatar.Karma = avatar.Karma;
            mongoAvatar.XP = avatar.XP;
            mongoAvatar.Portrait = avatar.Portrait;
            mongoAvatar.IsChanged = avatar.IsChanged;
            mongoAvatar.CreatedByAvatarId = avatar.CreatedByAvatarId.ToString();
            mongoAvatar.CreatedDate = avatar.CreatedDate;
            mongoAvatar.DeletedByAvatarId = avatar.DeletedByAvatarId.ToString();
            mongoAvatar.DeletedDate = avatar.DeletedDate;
            mongoAvatar.ModifiedByAvatarId = avatar.ModifiedByAvatarId.ToString();
            mongoAvatar.ModifiedDate = avatar.ModifiedDate;
            mongoAvatar.DeletedDate = avatar.DeletedDate;
            mongoAvatar.Version = avatar.Version;
            mongoAvatar.IsActive = avatar.IsActive;

            //AvatarDetail Properties
            mongoAvatar.UmaJson = avatar.UmaJson;
            //mongoAvatar.ProviderPrivateKey = avatar.ProviderPrivateKey;
            //mongoAvatar.ProviderPublicKey = avatar.ProviderPublicKey;
            //mongoAvatar.ProviderUsername = avatar.ProviderUsername;
            //mongoAvatar.ProviderWalletAddress = avatar.ProviderWalletAddress;
            mongoAvatar.FavouriteColour = avatar.FavouriteColour;
            mongoAvatar.STARCLIColour = avatar.STARCLIColour;
            mongoAvatar.Skills = avatar.Skills;
            mongoAvatar.Spells = avatar.Spells;
            mongoAvatar.Stats = avatar.Stats;
            mongoAvatar.SuperPowers = avatar.SuperPowers;
            mongoAvatar.GeneKeys = avatar.GeneKeys;
            mongoAvatar.HumanDesign = avatar.HumanDesign;
            mongoAvatar.Gifts = avatar.Gifts;
            mongoAvatar.Chakras = avatar.Chakras;
            mongoAvatar.Aura = avatar.Aura;
            mongoAvatar.Achievements = avatar.Achievements;
            mongoAvatar.Inventory = avatar.Inventory;
            mongoAvatar.Address = avatar.Address;
            mongoAvatar.Country = avatar.Country;
            mongoAvatar.County = avatar.County;
            mongoAvatar.Address = avatar.Address;
            mongoAvatar.Country = avatar.Country;
            mongoAvatar.County = avatar.County;
            mongoAvatar.DOB = avatar.DOB;
            mongoAvatar.Landline = avatar.Landline;
            mongoAvatar.Mobile = avatar.Mobile;
            mongoAvatar.Postcode = avatar.Postcode;
            mongoAvatar.Town = avatar.Town;
            mongoAvatar.KarmaAkashicRecords = avatar.KarmaAkashicRecords;
            mongoAvatar.MetaData = avatar.MetaData;
            mongoAvatar.ParentHolonId = avatar.ParentHolonId;
            mongoAvatar.ParentHolon = avatar.ParentHolon;
            mongoAvatar.ParentZomeId = avatar.ParentZomeId;
            mongoAvatar.ParentZome = avatar.ParentZome;
            mongoAvatar.ParentCelestialBody = avatar.ParentCelestialBody;
            mongoAvatar.ParentCelestialBodyId = avatar.ParentCelestialBodyId;
            mongoAvatar.ParentCelestialSpace = avatar.ParentCelestialSpace;
            mongoAvatar.ParentCelestialSpaceId = avatar.ParentCelestialSpaceId;
            mongoAvatar.ParentOmniverse = avatar.ParentOmniverse;
            mongoAvatar.ParentOmniverseId = avatar.ParentOmniverseId;
            mongoAvatar.ParentDimension = avatar.ParentDimension;
            mongoAvatar.ParentDimensionId = avatar.ParentDimensionId;
            mongoAvatar.ParentMultiverse = avatar.ParentMultiverse;
            mongoAvatar.ParentMultiverseId = avatar.ParentMultiverseId;
            mongoAvatar.ParentUniverse = avatar.ParentUniverse;
            mongoAvatar.ParentUniverseId = avatar.ParentUniverseId;
            mongoAvatar.ParentGalaxyCluster = avatar.ParentGalaxyCluster;
            mongoAvatar.ParentGalaxyClusterId = avatar.ParentGalaxyClusterId;
            mongoAvatar.ParentGalaxy = avatar.ParentGalaxy;
            mongoAvatar.ParentGalaxyId = avatar.ParentGalaxyId;
            mongoAvatar.ParentSolarSystem = avatar.ParentSolarSystem;
            mongoAvatar.ParentSolarSystemId = avatar.ParentSolarSystemId;
            mongoAvatar.ParentGreatGrandSuperStar = avatar.ParentGreatGrandSuperStar;
            mongoAvatar.ParentGreatGrandSuperStarId = avatar.ParentGreatGrandSuperStarId;
            mongoAvatar.ParentGreatGrandSuperStar = avatar.ParentGreatGrandSuperStar;
            mongoAvatar.ParentGrandSuperStarId = avatar.ParentGrandSuperStarId;
            mongoAvatar.ParentGrandSuperStar = avatar.ParentGrandSuperStar;
            mongoAvatar.ParentSuperStarId = avatar.ParentSuperStarId;
            mongoAvatar.ParentSuperStar = avatar.ParentSuperStar;
            mongoAvatar.ParentStarId = avatar.ParentStarId;
            mongoAvatar.ParentStar = avatar.ParentStar;
            mongoAvatar.ParentPlanetId = avatar.ParentPlanetId;
            mongoAvatar.ParentPlanet = avatar.ParentPlanet;
            mongoAvatar.ParentMoonId = avatar.ParentMoonId;
            mongoAvatar.ParentMoon = avatar.ParentMoon;
            mongoAvatar.Children = avatar.Children;
            mongoAvatar.Nodes = avatar.Nodes;

            return mongoAvatar;
        }

        private OASISResult<IHolon> ConvertMongoEntityToOASISHolon(OASISResult<Holon> holon)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>(new Core.Holons.Holon());

            if (holon.Result == null || holon.IsError)
            {
                result.IsError = true;
                result.Message = holon.Message;
                return result;
            }

            result.Result.IsNewHolon = false; //TODO: Not sure if best to default all new Holons to have this set to true or not?
            result.Result.Id = holon.Result.HolonId;
            result.Result.ProviderUniqueStorageKey = holon.Result.ProviderUniqueStorageKey;
            result.Result.PreviousVersionId = holon.Result.PreviousVersionId;
            result.Result.PreviousVersionProviderUniqueStorageKey = holon.Result.PreviousVersionProviderUniqueStorageKey;
            result.Result.MetaData = holon.Result.MetaData;
            result.Result.ProviderMetaData = holon.Result.ProviderMetaData;
            result.Result.Name = holon.Result.Name;
            result.Result.Description = holon.Result.Description;
            result.Result.HolonType = holon.Result.HolonType;
            result.Result.CreatedOASISType = holon.Result.CreatedOASISType;
            // oasisHolon.CreatedProviderType = new EnumValue<ProviderType>(holon.CreatedProviderType);
            result.Result.CreatedProviderType = holon.Result.CreatedProviderType;
            //oasisHolon.CreatedProviderType.Value = Core.Enums.ProviderType.MongoDBOASIS;
            result.Result.CreatedProviderType = holon.Result.CreatedProviderType;
            result.Result.IsChanged = holon.Result.IsChanged;
            result.Result.ParentHolonId = holon.Result.ParentHolonId;
            result.Result.ParentHolon = holon.Result.ParentHolon;
            result.Result.ParentZomeId = holon.Result.ParentZomeId;
            result.Result.ParentZome = holon.Result.ParentZome;
            result.Result.ParentCelestialBody = holon.Result.ParentCelestialBody;
            result.Result.ParentCelestialBodyId = holon.Result.ParentCelestialBodyId;
            result.Result.ParentCelestialSpace = holon.Result.ParentCelestialSpace;
            result.Result.ParentCelestialSpaceId = holon.Result.ParentCelestialSpaceId;
            result.Result.ParentOmniverse = holon.Result.ParentOmniverse;
            result.Result.ParentOmniverseId = holon.Result.ParentOmniverseId;
            result.Result.ParentDimension = holon.Result.ParentDimension;
            result.Result.ParentDimensionId = holon.Result.ParentDimensionId;
            result.Result.ParentMultiverse = holon.Result.ParentMultiverse;
            result.Result.ParentMultiverseId = holon.Result.ParentMultiverseId;
            result.Result.ParentUniverse = holon.Result.ParentUniverse;
            result.Result.ParentUniverseId = holon.Result.ParentUniverseId;
            result.Result.ParentGalaxyCluster = holon.Result.ParentGalaxyCluster;
            result.Result.ParentGalaxyClusterId = holon.Result.ParentGalaxyClusterId;
            result.Result.ParentGalaxy = holon.Result.ParentGalaxy;
            result.Result.ParentGalaxyId = holon.Result.ParentGalaxyId;
            result.Result.ParentSolarSystem = holon.Result.ParentSolarSystem;
            result.Result.ParentSolarSystemId = holon.Result.ParentSolarSystemId;
            result.Result.ParentGreatGrandSuperStar = holon.Result.ParentGreatGrandSuperStar;
            result.Result.ParentGreatGrandSuperStarId = holon.Result.ParentGreatGrandSuperStarId;
            result.Result.ParentGreatGrandSuperStar = holon.Result.ParentGreatGrandSuperStar;
            result.Result.ParentGrandSuperStarId = holon.Result.ParentGrandSuperStarId;
            result.Result.ParentGrandSuperStar = holon.Result.ParentGrandSuperStar;
            result.Result.ParentSuperStarId = holon.Result.ParentSuperStarId;
            result.Result.ParentSuperStar = holon.Result.ParentSuperStar;
            result.Result.ParentStarId = holon.Result.ParentStarId;
            result.Result.ParentStar = holon.Result.ParentStar;
            result.Result.ParentPlanetId = holon.Result.ParentPlanetId;
            result.Result.ParentPlanet = holon.Result.ParentPlanet;
            result.Result.ParentMoonId = holon.Result.ParentMoonId;
            result.Result.ParentMoon = holon.Result.ParentMoon;
            result.Result.Children = holon.Result.Children;
            result.Result.Nodes = holon.Result.Nodes;
            result.Result.CreatedByAvatarId = Guid.Parse(holon.Result.CreatedByAvatarId);
            result.Result.CreatedDate = holon.Result.CreatedDate;
            result.Result.DeletedByAvatarId = Guid.Parse(holon.Result.DeletedByAvatarId);
            result.Result.DeletedDate = holon.Result.DeletedDate;
            result.Result.ModifiedByAvatarId = Guid.Parse(holon.Result.ModifiedByAvatarId);
            result.Result.ModifiedDate = holon.Result.ModifiedDate;
            result.Result.DeletedDate = holon.Result.DeletedDate;
            result.Result.Version = holon.Result.Version;
            result.Result.IsActive = holon.Result.IsActive;
            return result;
        }

        private Holon ConvertOASISHolonToMongoEntity(IHolon holon)
        {
            if (holon == null)
                return null;

            Holon mongoHolon = new Holon();

           // if (holon.CreatedProviderType != null)
           //     mongoHolon.CreatedProviderType = holon.CreatedProviderType.Value;

            if (holon.ProviderUniqueStorageKey != null && holon.ProviderUniqueStorageKey.ContainsKey(Core.Enums.ProviderType.MongoDBOASIS))
                mongoHolon.Id = holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.MongoDBOASIS];

            mongoHolon.HolonId = holon.Id;
            mongoHolon.ProviderUniqueStorageKey = holon.ProviderUniqueStorageKey;
            mongoHolon.PreviousVersionId = holon.PreviousVersionId;
            mongoHolon.PreviousVersionProviderUniqueStorageKey = holon.PreviousVersionProviderUniqueStorageKey;
            mongoHolon.ProviderMetaData = holon.ProviderMetaData;
            mongoHolon.MetaData = holon.MetaData;
            mongoHolon.CreatedOASISType = holon.CreatedOASISType;
            mongoHolon.CreatedProviderType = holon.CreatedProviderType;
            mongoHolon.HolonType = holon.HolonType;
            mongoHolon.Name = holon.Name;
            mongoHolon.Description = holon.Description;
            mongoHolon.IsChanged = holon.IsChanged;
            mongoHolon.ParentHolonId = holon.ParentHolonId;
            mongoHolon.ParentHolon = holon.ParentHolon;
            mongoHolon.ParentZomeId = holon.ParentZomeId;
            mongoHolon.ParentZome = holon.ParentZome;
            mongoHolon.ParentCelestialBody = holon.ParentCelestialBody;
            mongoHolon.ParentCelestialBodyId = holon.ParentCelestialBodyId;
            mongoHolon.ParentCelestialSpace = holon.ParentCelestialSpace;
            mongoHolon.ParentCelestialSpaceId = holon.ParentCelestialSpaceId;
            mongoHolon.ParentOmniverse = holon.ParentOmniverse;
            mongoHolon.ParentOmniverseId = holon.ParentOmniverseId;
            mongoHolon.ParentDimension = holon.ParentDimension;
            mongoHolon.ParentDimensionId = holon.ParentDimensionId;
            mongoHolon.ParentMultiverse = holon.ParentMultiverse;
            mongoHolon.ParentMultiverseId = holon.ParentMultiverseId;
            mongoHolon.ParentUniverse = holon.ParentUniverse;
            mongoHolon.ParentUniverseId = holon.ParentUniverseId;
            mongoHolon.ParentGalaxyCluster = holon.ParentGalaxyCluster;
            mongoHolon.ParentGalaxyClusterId = holon.ParentGalaxyClusterId;
            mongoHolon.ParentGalaxy = holon.ParentGalaxy;
            mongoHolon.ParentGalaxyId = holon.ParentGalaxyId;
            mongoHolon.ParentSolarSystem = holon.ParentSolarSystem;
            mongoHolon.ParentSolarSystemId = holon.ParentSolarSystemId;
            mongoHolon.ParentGreatGrandSuperStar = holon.ParentGreatGrandSuperStar;
            mongoHolon.ParentGreatGrandSuperStarId = holon.ParentGreatGrandSuperStarId;
            mongoHolon.ParentGreatGrandSuperStar = holon.ParentGreatGrandSuperStar;
            mongoHolon.ParentGrandSuperStarId = holon.ParentGrandSuperStarId;
            mongoHolon.ParentGrandSuperStar = holon.ParentGrandSuperStar;
            mongoHolon.ParentSuperStarId = holon.ParentSuperStarId;
            mongoHolon.ParentSuperStar = holon.ParentSuperStar;
            mongoHolon.ParentStarId = holon.ParentStarId;
            mongoHolon.ParentStar = holon.ParentStar;
            mongoHolon.ParentPlanetId = holon.ParentPlanetId;
            mongoHolon.ParentPlanet = holon.ParentPlanet;
            mongoHolon.ParentMoonId = holon.ParentMoonId;
            mongoHolon.ParentMoon = holon.ParentMoon;
            mongoHolon.Children = holon.Children;
            mongoHolon.Nodes = holon.Nodes;
            mongoHolon.CreatedByAvatarId = holon.CreatedByAvatarId.ToString();
            mongoHolon.CreatedDate = holon.CreatedDate;
            mongoHolon.DeletedByAvatarId = holon.DeletedByAvatarId.ToString();
            mongoHolon.DeletedDate = holon.DeletedDate;
            mongoHolon.ModifiedByAvatarId = holon.ModifiedByAvatarId.ToString();
            mongoHolon.ModifiedDate = holon.ModifiedDate;
            mongoHolon.DeletedDate = holon.DeletedDate;
            mongoHolon.Version = holon.Version;
            mongoHolon.IsActive = holon.IsActive;
            
            return mongoHolon;
        }

        OASISResult<IEnumerable<IPlayer>> IOASISNETProvider.GetPlayersNearMe()
        {
            throw new NotImplementedException();
        }

        OASISResult<IEnumerable<IHolon>> IOASISNETProvider.GetHolonsNearMe(HolonType Type)
        {
            throw new NotImplementedException();
        }
    }
}