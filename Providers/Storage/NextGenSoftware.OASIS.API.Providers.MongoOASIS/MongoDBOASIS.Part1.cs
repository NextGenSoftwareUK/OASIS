using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Options;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Infrastructure.Singleton;
using DataHelper = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Helpers.DataHelper;
using Holon = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Holon;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS
{
    public partial class MongoDBOASIS
    {
        public MongoDbContext Database { get; set; }
        private AvatarRepository _avatarRepository = null;
        private HolonRepository _holonRepository = null;
        private SearchRepository _searchRepository = null;

        public string ConnectionString { get; set; }
        public string DBName { get; set; }
        public bool IsVersionControlEnabled { get; set; }

        public MongoDBOASIS(string connectionString, string dbName) : base()
        {
            Init(connectionString, dbName);
        }

        public MongoDBOASIS(string connectionString, string dbName, OASISDNA OASISDNA, string OASISDNAPath = "OASIS_DNA.json") : base(OASISDNA, OASISDNAPath)
        {
            Init(connectionString, dbName);
        }

        public MongoDBOASIS(string connectionString, string dbName, OASISDNA OASISDNA) : base(OASISDNA)
        {
            Init(connectionString, dbName);
        }
        
        
        public MongoDBOASIS(string connectionString, string dbName, string OASISDNAPath = "OASIS_DNA.json") : base (OASISDNAPath)
        {
            Init(connectionString, dbName);
        }

        private void Init(string connectionString, string dbName)
        {
            ConnectionString = connectionString;
            DBName = dbName;

            this.ProviderName = "MongoDBOASIS";
            this.ProviderDescription = "MongoDB Atlas Provider";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.MongoDBOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);

            var objectSerializer = new ObjectSerializer(type => ObjectSerializer.DefaultAllowedTypes(type) || type.FullName.StartsWith("NextGenSoftware") || type.FullName.StartsWith("System")); 
            BsonSerializer.RegisterSerializer(objectSerializer);
            //BsonClassMap.RegisterClassMap<OAPPDNA>();

            try
            {
                ConventionRegistry.Register(
                    "DictionaryRepresentationConvention",
                    new ConventionPack { new DictionaryRepresentationConvention(DictionaryRepresentation.ArrayOfArrays) },
                    _ => true);
            }
            catch
            {
                // Convention may already be registered in long-lived host processes.
            }
        }

        public override OASISResult<bool> ActivateProvider()
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                if (Database == null)
                {
                    Database = new MongoDbContext(ConnectionString, DBName);
                    SerializerRegister.GetInstance().RegisterGuidBsonSerializer();
                    SerializerRegister.GetInstance().RegisterMetaDataDictionarySerializer();
                    SerializerRegister.GetInstance().RegisterSTARNETDNADiscriminator();
                    _avatarRepository = new AvatarRepository(Database);
                    _holonRepository = new HolonRepository(Database);
                    _searchRepository = new SearchRepository(Database);
                }

                IsProviderActivated = true;
                result.Result = true;
            }
            catch (Exception ex) 
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown Error Occured In MongoDBOASISProvider.ActivateProvider. Reason: {ex}");
            }
            
            return result;
            //return base.ActivateProvider();
        }

        public override OASISResult<bool> DeActivateProvider()
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                if (Database != null)
                {
                    Database.MongoDB = null;
                    Database.MongoClient = null;
                    Database = null;
                }

                _avatarRepository = null;
                _holonRepository = null;
                _searchRepository = null;

                IsProviderActivated = false;
                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown Error Occured In MongoDBOASISProvider.DeActivateProvider. Reason: {ex}");
            }

            return result;
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                if (Database == null)
                {
                    Database = new MongoDbContext(ConnectionString, DBName);
                    SerializerRegister.GetInstance().RegisterGuidBsonSerializer();
                    SerializerRegister.GetInstance().RegisterMetaDataDictionarySerializer();
                    SerializerRegister.GetInstance().RegisterSTARNETDNADiscriminator();
                    _avatarRepository = new AvatarRepository(Database);
                    _holonRepository = new HolonRepository(Database);
                    _searchRepository = new SearchRepository(Database);
                }

                IsProviderActivated = true;
                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown Error Occured In MongoDBOASISProvider.ActivateProviderAsync. Reason: {ex}");
            }

            return result;
            //return await base.ActivateProviderAsync();
        }

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                if (Database != null)
                {
                    Database.MongoDB = null;
                    Database.MongoClient = null;
                    Database = null;
                }

                _avatarRepository = null;
                _holonRepository = null;
                _searchRepository = null;

                IsProviderActivated = false;
                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown Error Occured In MongoDBOASISProvider.DeActivateProviderAsync. Reason: {ex}");
            }

            return result;
            //return await base.DeActivateProviderAsync();
        }

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            return DataHelper.ConvertMongoEntitysToOASISAvatars(await _avatarRepository.GetAvatarsAsync());
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
        {
            return DataHelper.ConvertMongoEntitysToOASISAvatars(_avatarRepository.GetAvatars());
        }

        public override OASISResult<IAvatar> LoadAvatarByVerificationToken(string verificationToken, int version = 0)
            => DataHelper.ConvertMongoEntityToOASISAvatar(_avatarRepository.GetAvatar(x => x.VerificationToken == verificationToken));

        public override async Task<OASISResult<IAvatar>> LoadAvatarByVerificationTokenAsync(string verificationToken, int version = 0)
            => DataHelper.ConvertMongoEntityToOASISAvatar(await _avatarRepository.GetAvatarAsync(x => x.VerificationToken == verificationToken));

        public override OASISResult<IAvatar> LoadAvatarByResetToken(string resetToken, int version = 0)
            => DataHelper.ConvertMongoEntityToOASISAvatar(_avatarRepository.GetAvatar(x => x.ResetToken == resetToken));

        public override async Task<OASISResult<IAvatar>> LoadAvatarByResetTokenAsync(string resetToken, int version = 0)
            => DataHelper.ConvertMongoEntityToOASISAvatar(await _avatarRepository.GetAvatarAsync(x => x.ResetToken == resetToken));

        public override OASISResult<IAvatar> LoadAvatarByRefreshToken(string refreshToken, int version = 0)
            => DataHelper.ConvertMongoEntityToOASISAvatar(_avatarRepository.GetAvatar(x => x.RefreshTokens.Any(r => r.Token == refreshToken)));

        public override async Task<OASISResult<IAvatar>> LoadAvatarByRefreshTokenAsync(string refreshToken, int version = 0)
            => DataHelper.ConvertMongoEntityToOASISAvatar(await _avatarRepository.GetAvatarAsync(x => x.RefreshTokens.Any(r => r.Token == refreshToken)));

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
        {
            return DataHelper.ConvertMongoEntityToOASISAvatar(_avatarRepository.GetAvatar(x => x.Email == avatarEmail));
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
        {
            return DataHelper.ConvertMongoEntityToOASISAvatar(_avatarRepository.GetAvatar(x => x.Username == avatarUsername));
        }

        //public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(string username, int version = 0)
        //{
        //    return ConvertMongoEntityToOASISAvatar(await _avatarRepository.GetAvatarAsync(username));
        //}

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            return DataHelper.ConvertMongoEntityToOASISAvatar(await _avatarRepository.GetAvatarAsync(x => x.Username == avatarUsername));
        }

        //public override OASISResult<IAvatar> LoadAvatar(string username, int version = 0)
        //{
        //    return ConvertMongoEntityToOASISAvatar(_avatarRepository.GetAvatar(username));
        //}

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid Id, int version = 0)
        {
            return DataHelper.ConvertMongoEntityToOASISAvatar(await _avatarRepository.GetAvatarAsync(Id));
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            return DataHelper.ConvertMongoEntityToOASISAvatar(await _avatarRepository.GetAvatarAsync(x => x.Email == avatarEmail));
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid Id, int version = 0)
        {
            return DataHelper.ConvertMongoEntityToOASISAvatar(_avatarRepository.GetAvatar(Id));
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
            return DataHelper.ConvertMongoEntityToOASISAvatar(avatar.IsNewHolon || avatar.CreatedDate == DateTime.MinValue ?
               await _avatarRepository.AddAsync(DataHelper.ConvertOASISAvatarToMongoEntity(avatar)) :
               await _avatarRepository.UpdateAsync(DataHelper.ConvertOASISAvatarToMongoEntity(avatar)));
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatar)
        {
            return DataHelper.ConvertMongoEntityToOASISAvatarDetail(avatar.IsNewHolon || avatar.CreatedDate == DateTime.MinValue ?
               _avatarRepository.Add(DataHelper.ConvertOASISAvatarDetailToMongoEntity(avatar)) :
               _avatarRepository.Update(DataHelper.ConvertOASISAvatarDetailToMongoEntity(avatar)));
        }

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatar)
        {
            return DataHelper.ConvertMongoEntityToOASISAvatarDetail(avatar.IsNewHolon || avatar.CreatedDate == DateTime.MinValue ?
               await _avatarRepository.AddAsync(DataHelper.ConvertOASISAvatarDetailToMongoEntity(avatar)) :
               await _avatarRepository.UpdateAsync(DataHelper.ConvertOASISAvatarDetailToMongoEntity(avatar)));
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar)
        {
            return DataHelper.ConvertMongoEntityToOASISAvatar(avatar.IsNewHolon || avatar.CreatedDate == DateTime.MinValue ?
                _avatarRepository.Add(DataHelper.ConvertOASISAvatarToMongoEntity(avatar)) :
                _avatarRepository.Update(DataHelper.ConvertOASISAvatarToMongoEntity(avatar)));
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
        {
            return _avatarRepository.Delete(x => x.Username == avatarUsername, softDelete);
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            return await _avatarRepository.DeleteAsync(id);
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

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            return DataHelper.ConvertMongoEntityToOASISAvatar(await _avatarRepository.GetAvatarAsync(providerKey));
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
        {
            return DataHelper.ConvertMongoEntityToOASISAvatar(_avatarRepository.GetAvatar(providerKey));
        }

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
        {
            return _avatarRepository.Delete(providerKey, softDelete);
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            return await _avatarRepository.DeleteAsync(providerKey, softDelete);
        }

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return await _searchRepository.SearchAsync(searchParams);
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return _searchRepository.Search(searchParams);
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0)
        {
            return DataHelper.ConvertMongoEntityToOASISAvatarDetail(_avatarRepository.GetAvatarDetail(x => x.Username == avatarUsername));
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            if (_avatarRepository == null)
                return new OASISResult<IAvatarDetail> { IsError = true, Message = "MongoDBOASIS provider is not fully initialised (avatarRepository is null). Provider may be activating or was concurrently deactivated — OASIS Hyperdrive will retry." };

            return DataHelper.ConvertMongoEntityToOASISAvatarDetail(await _avatarRepository.GetAvatarDetailAsync(id));
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
        {
            return DataHelper.ConvertMongoEntityToOASISAvatarDetail(await _avatarRepository.GetAvatarDetailAsync(x => x.Username == avatarUsername));
        }
    }
}
