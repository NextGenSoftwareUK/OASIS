using System;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
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
    }
}

