using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Repositories;

namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS
{
    /// <summary>
    /// Shipex Pro OASIS Provider - Logistics Middleware Provider
    /// Integrates shipping services through a unified API, connecting merchants with
    /// Shipox (order management), iShip (carrier services), and QuickBooks (accounting).
    /// </summary>
    public class ShipexProOASIS : OASISStorageProviderBase, IOASISDBStorageProvider, IOASISNETProvider
    {
        public ShipexProMongoDbContext Database { get; set; }
        private IShipexProRepository _repository = null;

        public string ConnectionString { get; set; }
        public string DBName { get; set; }

        public ShipexProOASIS(string connectionString, string dbName) : base()
        {
            Init(connectionString, dbName);
        }

        public ShipexProOASIS(string connectionString, string dbName, OASISDNA OASISDNA, string OASISDNAPath = "OASIS_DNA.json") : base(OASISDNA, OASISDNAPath)
        {
            Init(connectionString, dbName);
        }

        public ShipexProOASIS(string connectionString, string dbName, OASISDNA OASISDNA) : base(OASISDNA)
        {
            Init(connectionString, dbName);
        }

        public ShipexProOASIS(string connectionString, string dbName, string OASISDNAPath = "OASIS_DNA.json") : base(OASISDNAPath)
        {
            Init(connectionString, dbName);
        }

        private void Init(string connectionString, string dbName)
        {
            ConnectionString = connectionString;
            DBName = dbName;

            this.ProviderName = "ShipexProOASIS";
            this.ProviderDescription = "Shipex Pro Logistics Middleware Provider";
            // TODO: Add ShipexProOASIS to ProviderType enum in Core.Enums
            // For now, using MongoDBOASIS as placeholder - this will need to be updated
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.MongoDBOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);

            // Register MongoDB serializers for OASIS types
            var objectSerializer = new ObjectSerializer(type => 
                ObjectSerializer.DefaultAllowedTypes(type) || 
                type.FullName.StartsWith("NextGenSoftware") || 
                type.FullName.StartsWith("System"));
            BsonSerializer.RegisterSerializer(objectSerializer);
        }

        public override OASISResult<bool> ActivateProvider()
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                if (Database == null)
                {
                    Database = new ShipexProMongoDbContext(ConnectionString, DBName);
                    _repository = new ShipexProMongoRepository(Database);
                }

                IsProviderActivated = true;
                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown Error Occurred In ShipexProOASISProvider.ActivateProvider. Reason: {ex}");
            }

            return result;
        }

        public override OASISResult<bool> DeActivateProvider()
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                // Properly dispose MongoDB resources
                if (Database != null)
                {
                    Database.MongoDB = null;
                    Database.MongoClient = null;
                    Database = null;
                }

                // Dispose repository
                _repository = null;

                IsProviderActivated = false;
                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown Error Occurred In ShipexProOASISProvider.DeActivateProvider. Reason: {ex}");
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
                    Database = new ShipexProMongoDbContext(ConnectionString, DBName);
                    _repository = new ShipexProMongoRepository(Database);
                }

                IsProviderActivated = true;
                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown Error Occurred In ShipexProOASISProvider.ActivateProviderAsync. Reason: {ex}");
            }

            return result;
        }

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                // Properly dispose MongoDB resources
                if (Database != null)
                {
                    Database.MongoDB = null;
                    Database.MongoClient = null;
                    Database = null;
                }

                // Dispose repository
                _repository = null;

                IsProviderActivated = false;
                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown Error Occurred In ShipexProOASISProvider.DeActivateProviderAsync. Reason: {ex}");
            }

            return result;
        }

        // Repository accessor for services
        public IShipexProRepository Repository => _repository;

        #region OASIS Storage Provider Base - Stub Implementations
        // Note: ShipexProOASIS is a specialized logistics provider and doesn't implement
        // general OASIS avatar/holon operations. These stubs allow compilation while
        // indicating that these operations are not supported by this provider.

        private OASISResult<T> NotSupportedResult<T>(string operation)
        {
            return new OASISResult<T>
            {
                IsError = true,
                Message = $"{operation} is not supported by ShipexProOASIS provider. This provider is specialized for logistics operations."
            };
        }

        // Avatar Operations
        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0) => NotSupportedResult<IAvatar>("LoadAvatar");
        public override Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0) => Task.FromResult(NotSupportedResult<IAvatar>("LoadAvatarAsync"));
        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0) => NotSupportedResult<IAvatar>("LoadAvatarByUsername");
        public override Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0) => Task.FromResult(NotSupportedResult<IAvatar>("LoadAvatarByUsernameAsync"));
        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0) => NotSupportedResult<IAvatar>("LoadAvatarByEmail");
        public override Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0) => Task.FromResult(NotSupportedResult<IAvatar>("LoadAvatarByEmailAsync"));
        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0) => NotSupportedResult<IAvatar>("LoadAvatarByProviderKey");
        public override Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0) => Task.FromResult(NotSupportedResult<IAvatar>("LoadAvatarByProviderKeyAsync"));
        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0) => NotSupportedResult<IEnumerable<IAvatar>>("LoadAllAvatars");
        public override Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0) => Task.FromResult(NotSupportedResult<IEnumerable<IAvatar>>("LoadAllAvatarsAsync"));
        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar) => NotSupportedResult<IAvatar>("SaveAvatar");
        public override Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar) => Task.FromResult(NotSupportedResult<IAvatar>("SaveAvatarAsync"));
        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true) => NotSupportedResult<bool>("DeleteAvatar");
        public override Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true) => Task.FromResult(NotSupportedResult<bool>("DeleteAvatarAsync"));
        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true) => NotSupportedResult<bool>("DeleteAvatarByEmail");
        public override Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true) => Task.FromResult(NotSupportedResult<bool>("DeleteAvatarByEmailAsync"));
        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true) => NotSupportedResult<bool>("DeleteAvatar");
        public override Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true) => Task.FromResult(NotSupportedResult<bool>("DeleteAvatarAsync"));
        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true) => NotSupportedResult<bool>("DeleteAvatarByUsername");
        public override Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true) => Task.FromResult(NotSupportedResult<bool>("DeleteAvatarByUsernameAsync"));

        // Avatar Detail Operations
        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0) => NotSupportedResult<IAvatarDetail>("LoadAvatarDetail");
        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0) => Task.FromResult(NotSupportedResult<IAvatarDetail>("LoadAvatarDetailAsync"));
        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0) => NotSupportedResult<IAvatarDetail>("LoadAvatarDetailByUsername");
        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0) => Task.FromResult(NotSupportedResult<IAvatarDetail>("LoadAvatarDetailByUsernameAsync"));
        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0) => NotSupportedResult<IAvatarDetail>("LoadAvatarDetailByEmail");
        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0) => Task.FromResult(NotSupportedResult<IAvatarDetail>("LoadAvatarDetailByEmailAsync"));
        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0) => NotSupportedResult<IEnumerable<IAvatarDetail>>("LoadAllAvatarDetails");
        public override Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0) => Task.FromResult(NotSupportedResult<IEnumerable<IAvatarDetail>>("LoadAllAvatarDetailsAsync"));
        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatar) => NotSupportedResult<IAvatarDetail>("SaveAvatarDetail");
        public override Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatar) => Task.FromResult(NotSupportedResult<IAvatarDetail>("SaveAvatarDetailAsync"));

        // Holon Operations
        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => NotSupportedResult<IHolon>("LoadHolon");
        public override Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => Task.FromResult(NotSupportedResult<IHolon>("LoadHolonAsync"));
        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => NotSupportedResult<IHolon>("LoadHolon");
        public override Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => Task.FromResult(NotSupportedResult<IHolon>("LoadHolonAsync"));
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => NotSupportedResult<IEnumerable<IHolon>>("LoadHolonsForParent");
        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => Task.FromResult(NotSupportedResult<IEnumerable<IHolon>>("LoadHolonsForParentAsync"));
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => NotSupportedResult<IEnumerable<IHolon>>("LoadHolonsForParent");
        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => Task.FromResult(NotSupportedResult<IEnumerable<IHolon>>("LoadHolonsForParentAsync"));
        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => NotSupportedResult<IEnumerable<IHolon>>("LoadAllHolons");
        public override Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => Task.FromResult(NotSupportedResult<IEnumerable<IHolon>>("LoadAllHolonsAsync"));
        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) => NotSupportedResult<IHolon>("SaveHolon");
        public override Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) => Task.FromResult(NotSupportedResult<IHolon>("SaveHolonAsync"));
        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) => NotSupportedResult<IEnumerable<IHolon>>("SaveHolons");
        public override Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) => Task.FromResult(NotSupportedResult<IEnumerable<IHolon>>("SaveHolonsAsync"));
        public override OASISResult<IHolon> DeleteHolon(string providerKey) => NotSupportedResult<IHolon>("DeleteHolon");
        public override Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey) => Task.FromResult(NotSupportedResult<IHolon>("DeleteHolonAsync"));
        public override OASISResult<IHolon> DeleteHolon(Guid id) => NotSupportedResult<IHolon>("DeleteHolon");
        public override Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id) => Task.FromResult(NotSupportedResult<IHolon>("DeleteHolonAsync"));
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => NotSupportedResult<IEnumerable<IHolon>>("LoadHolonsByMetaData");
        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => Task.FromResult(NotSupportedResult<IEnumerable<IHolon>>("LoadHolonsByMetaDataAsync"));
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => NotSupportedResult<IEnumerable<IHolon>>("LoadHolonsByMetaData");
        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => Task.FromResult(NotSupportedResult<IEnumerable<IHolon>>("LoadHolonsByMetaDataAsync"));

        // Import/Export Operations
        public override OASISResult<bool> Import(IEnumerable<IHolon> holons) => NotSupportedResult<bool>("Import");
        public override Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons) => Task.FromResult(NotSupportedResult<bool>("ImportAsync"));
        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0) => NotSupportedResult<IEnumerable<IHolon>>("ExportAll");
        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0) => Task.FromResult(NotSupportedResult<IEnumerable<IHolon>>("ExportAllAsync"));
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid id, int version = 0) => NotSupportedResult<IEnumerable<IHolon>>("ExportAllDataForAvatarById");
        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid id, int version = 0) => Task.FromResult(NotSupportedResult<IEnumerable<IHolon>>("ExportAllDataForAvatarByIdAsync"));
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string email, int version = 0) => NotSupportedResult<IEnumerable<IHolon>>("ExportAllDataForAvatarByEmail");
        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string email, int version = 0) => Task.FromResult(NotSupportedResult<IEnumerable<IHolon>>("ExportAllDataForAvatarByEmailAsync"));
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string username, int version = 0) => NotSupportedResult<IEnumerable<IHolon>>("ExportAllDataForAvatarByUsername");
        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string username, int version = 0) => Task.FromResult(NotSupportedResult<IEnumerable<IHolon>>("ExportAllDataForAvatarByUsernameAsync"));

        // Search Operations
        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) => new OASISResult<ISearchResults> { IsError = true, Message = "Search is not supported by ShipexProOASIS provider" };
        public override Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) => Task.FromResult(new OASISResult<ISearchResults> { IsError = true, Message = "Search is not supported by ShipexProOASIS provider" });

        // Interface Properties
        public bool IsVersionControlEnabled 
        { 
            get => false; 
            set { /* Not supported */ } 
        }

        // Network Provider Methods
        public OASISResult<IEnumerable<IAvatar>> GetAvatarsNearMe(long x, long y, int radius) => NotSupportedResult<IEnumerable<IAvatar>>("GetAvatarsNearMe");
        public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(long x, long y, int radius, HolonType holonType) => NotSupportedResult<IEnumerable<IHolon>>("GetHolonsNearMe");

        #endregion
    }
}




