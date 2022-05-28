using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Infrastructure.EOSClient;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.TestHarness
{
    internal static class Program
    {
        private static string _chainUrl = "http://localhost:8888";
        private static string _oasisEosAccount = "berd22";
        private static string _avatarTable = "avatar";
        private static string _holonTable = "holon";
        private static string _avatarDetailTable = "avatardetail";

        #region Avatar Examples

        private static async Task Run_GetAvatarTableRows()
        {
            IEosClient eosClient = new EosClient(new Uri(_chainUrl));

            Console.WriteLine("Requesting avatar table rows...");
            
            var avatarsTableDto = await eosClient.GetTableRows<AvatarDto>(new GetTableRowsRequestDto()
            {
                Code = _oasisEosAccount,
                Table = _avatarTable,
                Scope = _oasisEosAccount
            });
            
            foreach (var avatarItem in avatarsTableDto.Rows)
            {
                Console.WriteLine(new string('-', 10));
                Console.WriteLine("Info: " + avatarItem.Info);
                Console.WriteLine("AvatarId: " + avatarItem.AvatarId);
                Console.WriteLine("EntityId: " + avatarItem.EntityId);
                Console.WriteLine("IsDeleted: " + avatarItem.IsDeleted);
                Console.WriteLine(new string('-', 10));
            }
            
            Console.WriteLine("Requesting avatar table rows completed successfully...");

            eosClient.Dispose();
        }

        #endregion

        #region Holon Example

        private static async Task Run_GetHolonTableRows()
        {
            IEosClient eosClient = new EosClient(new Uri(_chainUrl));

            Console.WriteLine("Requesting holon table rows...");
            
            var holonTableRows = await eosClient.GetTableRows<HolonDto>(new GetTableRowsRequestDto()
            {
                Code = _oasisEosAccount,
                Table = _holonTable,
                Scope = _oasisEosAccount
            });
            
            foreach (var holonDto in holonTableRows.Rows)
            {
                Console.WriteLine(new string('-', 10));
                Console.WriteLine("Info: " + holonDto.Info);
                Console.WriteLine("HolonId: " + holonDto.HolonId);
                Console.WriteLine("EntityId: " + holonDto.EntityId);
                Console.WriteLine("IsDeleted: " + holonDto.IsDeleted);
                Console.WriteLine(new string('-', 10));
            }
            
            Console.WriteLine("Requesting holon table rows completed successfully...");

            eosClient.Dispose();
        }

        private static async Task Run_GetAllHolon()
        {
            EOSIOOASIS eosioOasis = new EOSIOOASIS(_chainUrl, _oasisEosAccount);
            
            Console.WriteLine("Run_GetAllHolon-->ActivateProvider()");
            eosioOasis.ActivateProvider();

            Console.WriteLine("Run_GetAllHolon-->LoadAllHolonsAsync()");
            var allHolonsResult = await eosioOasis.LoadAllHolonsAsync();
            if (!allHolonsResult.IsLoaded)
            {
                Console.WriteLine(allHolonsResult.Message);
                return;
            }

            foreach (var holon in allHolonsResult.Result)
            {
                Console.WriteLine(new string('-', 10));
                Console.WriteLine("Holon Id: " + holon.Id);
                Console.WriteLine("Name: " + holon.Name);
                Console.WriteLine(new string('-', 10));
            }

            Console.WriteLine("Run_GetAllHolon-->DeActivateProvider()");
            eosioOasis.DeActivateProvider();
        }
        
        private static async Task Run_SaveAndGetHolonById()
        {
            EOSIOOASIS eosioOasis = new EOSIOOASIS(_chainUrl, _oasisEosAccount);
            
            Console.WriteLine("Run_SaveAndGetHolonById-->ActivateProvider()");
            eosioOasis.ActivateProvider();

            var entityId = Guid.NewGuid();
            Holon entity = new Holon()
            {
                Name = "Bob",
                Id = entityId
            };
            Console.WriteLine("Run_SaveAndGetHolonById-->SaveHolonAsync()");
            var saveResult = await eosioOasis.SaveHolonAsync(entity);
            if (!saveResult.IsSaved)
            {
                Console.WriteLine(saveResult.Message);
                return;
            }

            Console.WriteLine("Run_SaveAndGetHolonById-->LoadHolonAsync()");
            var loadedEntityResult = await eosioOasis.LoadHolonAsync(entityId);
            if (!loadedEntityResult.IsLoaded)
            {
                Console.WriteLine(saveResult.Message);
                return;
            }
            
            Console.WriteLine("Holon Id: " + loadedEntityResult.Result.Id);
            Console.WriteLine("Holon Name: " + loadedEntityResult.Result.Name);

            Console.WriteLine("Run_SaveAndGetHolonById-->DeActivateProvider()");
            eosioOasis.DeActivateProvider();
        }
        
        private static async Task Run_SoftAndHardDeleteHolonById()
        {
            EOSIOOASIS eosioOasis = new EOSIOOASIS(_chainUrl, _oasisEosAccount);
            
            Console.WriteLine("Run_SoftAndHardDeleteHolonById-->ActivateProvider()");
            eosioOasis.ActivateProvider();
            
            var entityId = Guid.NewGuid();
            Holon entity = new Holon()
            {
                Name = "Bob",
                Id = entityId
            };
            Console.WriteLine("Run_SoftAndHardDeleteHolonById-->SaveHolonAsync()");
            var saveResult = await eosioOasis.SaveHolonAsync(entity);
            if (!saveResult.IsSaved)
            {
                Console.WriteLine(saveResult.Message);
                return;
            }

            Console.WriteLine("Run_SoftAndHardDeleteHolonById-->DeleteHolonAsync()");
            var softDeleteResult = await eosioOasis.DeleteHolonAsync(entityId, softDelete: true);
            if (!softDeleteResult.IsSaved)
            {
                Console.WriteLine(softDeleteResult.Message);
                return;
            }
            
            var entityId2 = Guid.NewGuid();
            Holon entity2 = new Holon()
            {
                Name = "Bob",
                Id = entityId2
            };
            Console.WriteLine("Run_SoftAndHardDeleteHolonById-->SaveHolonAsync()");
            var saveResult2 = await eosioOasis.SaveHolonAsync(entity2);
            if (!saveResult2.IsSaved)
            {
                Console.WriteLine(saveResult.Message);
                return;
            }
            
            Console.WriteLine("Run_SoftAndHardDeleteHolonById-->DeleteHolonAsync()");
            var hardDeleteResult = await eosioOasis.DeleteHolonAsync(entityId2, softDelete: false);
            if (!hardDeleteResult.IsSaved)
            {
                Console.WriteLine(hardDeleteResult.Message);
                return;
            }

            Console.WriteLine("Run_SoftAndHardDeleteHolonById-->DeActivateProvider()");
            eosioOasis.DeActivateProvider();
        }

        #endregion

        #region Avatar Detail

        private static async Task Run_GetAvatarDetailsTableRows()
        {
            IEosClient eosClient = new EosClient(new Uri(_chainUrl));

            Console.WriteLine("Requesting avatar detail table rows...");
            
            var avatarDetailsTableDto = await eosClient.GetTableRows<AvatarDetailDto>(new GetTableRowsRequestDto()
            {
                Code = _oasisEosAccount,
                Table = _avatarDetailTable,
                Scope = _oasisEosAccount
            });
            
            foreach (var avatarDetailItem in avatarDetailsTableDto.Rows)
            {
                Console.WriteLine(new string('-', 10));
                Console.WriteLine("Info: " + avatarDetailItem.Info);
                Console.WriteLine("AvatarId: " + avatarDetailItem.AvatarId);
                Console.WriteLine("EntityId: " + avatarDetailItem.EntityId);
                Console.WriteLine(new string('-', 10));
            }
            
            Console.WriteLine("Requesting avatar detail table rows completed successfully...");

            eosClient.Dispose();
        }

        #endregion

        public static async Task Main()
        {
            // Avatar Examples
            await Run_GetAvatarTableRows();
            
            // Avatar Detail Examples
            await Run_GetAvatarDetailsTableRows();
            
            // Holon Examples
            await Run_GetHolonTableRows();
            await Run_GetAllHolon();
            await Run_SaveAndGetHolonById();
            await Run_SoftAndHardDeleteHolonById();
        }
    }
}