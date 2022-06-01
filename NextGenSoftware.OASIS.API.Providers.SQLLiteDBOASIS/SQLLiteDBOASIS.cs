using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Interfaces;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS
{
    public class SQLLiteDBOASIS : OASISStorageProviderBase, IOASISDBStorageProvider, IOASISLocalStorageProvider, IOASISNETProvider, IOASISSuperStar
    {
        private DataContext appDataContext;

        public IAvatarDetailRepository? avatarDetailRepository;
        public IAvatarRepository? avatarRepository;
        public IHolonRepository? holonRepository;

        //TODO: Set ConnectionString in DBContext
        public SQLLiteDBOASIS(string connectionString)
        {
            this.ProviderName = "SQLLiteDBOASIS";
            this.ProviderDescription = "SQLLiteDBOASIS Provider";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.SQLLiteDBOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageLocalAndNetwork);

            appDataContext = new DataContext(connectionString);
            //appDataContext = new DataContext();

            avatarRepository = new AvatarRepository(appDataContext);
            holonRepository = new HolonRepository(appDataContext);

            //var serviceProvider = new ServiceCollection()
            //    .AddDbContext<DataContext>()
            //    .AddSingleton<IAvtarDetailRepository, AvtarDetailRepository>()
            //    .AddSingleton<IAvtarRepository, AvtarRepository>()
            //    .AddSingleton<IHolonRepository, HolonRepository>()
            //    .BuildServiceProvider();

            //avatarDetailRepository = serviceProvider.GetService<IAvtarDetailRepository>();
            //avatarRepository = serviceProvider.GetService<IAvtarRepository>();
            //holonRepository = serviceProvider.GetService<IHolonRepository>();
        }
        public bool IsVersionControlEnabled { get; set; } = false;

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
        {
            var result = avatarRepository.DeleteAvatar(id, softDelete);
            return result;
        }

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
        {
            var result = avatarRepository.DeleteAvatar(providerKey, softDelete);
            return result;
        }

        public override Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var result = avatarRepository.DeleteAvatarAsync(id, softDelete);
            return result;
        }

        public override Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            var result = avatarRepository.DeleteAvatarAsync(providerKey, softDelete);
            return result;
        }

        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
        {
            var result = avatarRepository.DeleteAvatarByEmail(avatarEmail, softDelete);
            return result;
        }

        public override Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        {
            var result = avatarRepository.DeleteAvatarByEmailAsync(avatarEmail, softDelete);
            return result;
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
        {
            var result = avatarRepository.DeleteAvatarByUsername(avatarUsername, softDelete);
            return result;
        }

        public override Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
        {
            var result = avatarRepository.DeleteAvatarByUsernameAsync(avatarUsername, softDelete);
            return result;
        }

        public override OASISResult<bool> DeleteHolon(Guid id, bool softDelete = true)
        {
            var result = holonRepository.DeleteHolon(id, softDelete);
            return result;
        }

        public override OASISResult<bool> DeleteHolon(string providerKey, bool softDelete = true)
        {
            var result = holonRepository.DeleteHolon(providerKey, softDelete);
            return result;
        }

        public override Task<OASISResult<bool>> DeleteHolonAsync(Guid id, bool softDelete = true)
        {
            var result = holonRepository.DeleteHolonAsync(id, softDelete);
            return result;
        }

        public override Task<OASISResult<bool>> DeleteHolonAsync(string providerKey, bool softDelete = true)
        {
            var result = holonRepository.DeleteHolonAsync(providerKey, softDelete);
            return result;
        }

        public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(HolonType Type)
        {
            var result = holonRepository.GetHolonsNearMe(Type);
            return result;
        }

        public OASISResult<IEnumerable<IPlayer>> GetPlayersNearMe()
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
        {
            var result = avatarDetailRepository.LoadAllAvatarDetails(version);
            return result;
        }

        public override Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = avatarDetailRepository.LoadAllAvatarDetailsAsync(version);
            return result;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
        {
            var result = avatarRepository.LoadAllAvatars(version);
            return result;
        }

        public override Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = avatarRepository.LoadAllAvatarsAsync(version);
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = holonRepository.LoadAllHolons(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, version);
            return result;
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = holonRepository.LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, version);
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid Id, int version = 0)
        {
            var result = avatarRepository.LoadAvatar(Id, version);
            return result;
        }

        //public override OASISResult<IAvatar> LoadAvatar(string username, string password, int version = 0)
        //{
        //    var result = avatarRepository.LoadAvatar(username, password, version);
        //    return result;
        //}

        public override OASISResult<IAvatar> LoadAvatar(string username, int version = 0)
        {
            var result = avatarRepository.LoadAvatar(username, version);
            return result;
        }

        public OASISResult<IAvatar> LoadAvatar(string username, string password, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid Id, int version = 0)
        {
            var result = avatarRepository.LoadAvatarAsync(Id, version);
            return result;
        }

        //public override Task<OASISResult<IAvatar>> LoadAvatarAsync(string username, string password, int version = 0)
        //{
        //    var result = avatarRepository.LoadAvatarAsync(username, password, version);
        //    return result;
        //}

        public override Task<OASISResult<IAvatar>> LoadAvatarAsync(string username, int version = 0)
        {
            var result = avatarRepository.LoadAvatarAsync(username, version);
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
        {
            var result = avatarRepository.LoadAvatarByEmail(avatarEmail, version);
            return result;
        }

        public override Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            var result = avatarRepository.LoadAvatarByEmailAsync(avatarEmail, version);
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
        {
            var result = avatarRepository.LoadAvatarByUsername(avatarUsername, version);
            return result;
        }

        public override Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var result = avatarRepository.LoadAvatarByUsernameAsync(avatarUsername, version);
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
        {
            var result = avatarDetailRepository.LoadAvatarDetail(id, version);
            return result;
        }

        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var result = avatarDetailRepository.LoadAvatarDetailAsync(id, version);
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0)
        {
            var result = avatarDetailRepository.LoadAvatarDetailByEmail(avatarEmail, version);
            return result;
        }

        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
        {
            var result = avatarDetailRepository.LoadAvatarDetailByEmailAsync(avatarEmail, version);
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0)
        {
            var result = avatarDetailRepository.LoadAvatarDetailByUsername(avatarUsername, version);
            return result;
        }

        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
        {
            var result = avatarDetailRepository.LoadAvatarDetailByUsernameAsync(avatarUsername, version);
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarForProviderKey(string providerKey, int version = 0)
        {
            var result = avatarRepository.LoadAvatarForProviderKey(providerKey, version);
            return result;
        }

        public override Task<OASISResult<IAvatar>> LoadAvatarForProviderKeyAsync(string providerKey, int version = 0)
        {
            var result = avatarRepository.LoadAvatarForProviderKeyAsync(providerKey, version);
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = holonRepository.LoadHolon(id, loadChildren, recursive, maxChildDepth, continueOnError, version);
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = holonRepository.LoadHolon(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, version);
            return result;
        }

        public override Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = holonRepository.LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, version);
            return result;
        }

        public override Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = holonRepository.LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, version);
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = holonRepository.LoadHolonsForParent(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, version);
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = holonRepository.LoadHolonsForParent(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, version);
            return result;
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = holonRepository.LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, version);
            return result;
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = holonRepository.LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, version);
            return result;
        }

        public bool NativeCodeGenesis(ICelestialBody celestialBody)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar Avatar)
        {
            var result = avatarRepository.SaveAvatar(Avatar);
            return result;
        }

        public override Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar Avatar)
        {
            var result = avatarRepository.SaveAvatarAsync(Avatar);
            return result;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail Avatar)
        {
            var result = avatarDetailRepository.SaveAvatarDetail(Avatar);
            return result;
        }

        public override Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail Avatar)
        {
            var result = avatarDetailRepository.SaveAvatarDetailAsync(Avatar);
            return result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true)
        {
            var result = holonRepository.SaveHolon(holon, saveChildren, recursive, maxChildDepth, continueOnError);
            return result;
        }

        public override Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true)
        {
            var result = holonRepository.SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError);
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true)
        {
            var result = holonRepository.SaveHolons(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError);
            return result;
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true)
        {
            var result = holonRepository.SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError);
            return result;
        }

        public override Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }

        public OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> LoadProviderWallets()
        {
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();

            //TODO: Finish Implementing.

            return result;
        }

        public async Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> LoadProviderWalletsAsync()
        {
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();

            //TODO: Finish Implementing.

            return result;
        }

        public OASISResult<bool> SaveProviderWallets(Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            //TODO: Finish Implementing.

            return result;
        }

        public async Task<OASISResult<bool>> SaveProviderWalletsAsync(Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            //TODO: Finish Implementing.

            return result;
        }

        public override Task<OASISResult<bool>> Import(IEnumerable<IHolon> holons)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAll(int version = 0)
        {
            throw new NotImplementedException();
        }
    }
}
