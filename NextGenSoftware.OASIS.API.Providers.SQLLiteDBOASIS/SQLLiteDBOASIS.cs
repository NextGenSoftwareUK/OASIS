using System.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS
{
    public class SQLLiteDBOASIS : OASISStorageBase, IOASISStorage, IOASISNET
    {
        public string ConnectionString { get; set; }
        public DataContext _context;

        public SQLLiteDBOASIS(string connectionString)
        {
            _context = new DataContext(connectionString);

           ConnectionString = connectionString;

       //     _db = new MongoDbContext(connectionString, dbName);
      //      _avatarRepository = new AvatarRepository(_db);

            this.ProviderName = "SQLLiteDBOASIS";
            this.ProviderDescription = "SQLLiteDB Provider";
            this.ProviderType = new Core.Helpers.EnumValue<ProviderType>(Core.Enums.ProviderType.SQLLiteDBOASIS);
            this.ProviderCategory = new Core.Helpers.EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
        }

        public Task<bool> AddKarmaToAvatarAsync(IAvatar Avatar, int karma)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IHolon> GetHolonsNearMe(HolonType Type)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IPlayer> GetPlayersNearMe()
        {
            throw new NotImplementedException();
        }
        public override Task<IEnumerable<IAvatar>> LoadAllAvatarsAsync()
        {
            return new Task<IEnumerable<IAvatar>>(() => _context.Avatars.ToList());
        }

        public override IEnumerable<IAvatar> LoadAllAvatars()
        {
            return _context.Avatars.ToList();
        }

        public override Task<IAvatar> LoadAvatarAsync(string providerKey)
        {
            return new Task<IAvatar>(() =>  _context.Avatars.FirstOrDefault(x => x.ProviderKey[Core.Enums.ProviderType.SQLLiteDBOASIS] == providerKey));
        }

        public override Task<IAvatar> LoadAvatarAsync(Guid id)
        {
            return new Task<IAvatar>(() => _context.Avatars.FirstOrDefault(x => x.Id == id));
        }

        public override IAvatar LoadAvatar(Guid id)
        {
            return  _context.Avatars.FirstOrDefault(x => x.Id == id);
        }

        public override Task<IAvatar> LoadAvatarAsync(string username, string password)
        {
            return new Task<IAvatar>(() => _context.Avatars.FirstOrDefault(x => x.Username == username && x.Password == password));
        }

        public override IAvatar LoadAvatar(string username, string password)
        {
            return _context.Avatars.FirstOrDefault(x => x.Username == username && x.Password == password);
        }

        public override Task<IAvatar> SaveAvatarAsync(IAvatar avatar)
        {
            //TODO: Check this works...
            EntityEntry<Avatar> savedAvatar = _context.Avatars.Update((Avatar)avatar);
            return new Task<IAvatar>(() => savedAvatar.Entity);
        }

        //TODO: Move this into Search Reposirary like Avatar is...
        public override async Task<ISearchResults> SearchAsync(ISearchParams searchTerm)
        {
            //TODO: Implement.
            return new SearchResults() { SearchResultHolons = new List<Holon>() };
        }

        public override void ActivateProvider()
        {
            if (_context == null)
                _context = new DataContext(ConnectionString);

            base.ActivateProvider();
        }

        public override void DeActivateProvider()
        {
            _context.Database.CloseConnection();
            _context.Dispose();
            _context = null;

            base.DeActivateProvider();
        }

        public override IAvatar SaveAvatar(IAvatar Avatar)
        {
            throw new NotImplementedException();
        }

        public override IAvatar LoadAvatar(string username)
        {
            throw new NotImplementedException();
        }

        public override bool DeleteAvatar(Guid id, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override Task<bool> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override IHolon LoadHolon(Guid id, HolonType type = HolonType.Holon)
        {
            throw new NotImplementedException();
        }

        public override IHolon LoadHolon(string providerKey, HolonType type = HolonType.Holon)
        {
            throw new NotImplementedException();
        }
        public override IHolon SaveHolon(IHolon holon)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<IHolon> SaveHolons(IEnumerable<IHolon> holons)
        {
            throw new NotImplementedException();
        }

        public override Task<IHolon> LoadHolonAsync(Guid id, HolonType type = HolonType.Holon)
        {
            throw new NotImplementedException();
        }

        public override Task<IHolon> LoadHolonAsync(string providerKey, HolonType type = HolonType.Holon)
        {
            throw new NotImplementedException();
        }

        public override Task<IHolon> SaveHolonAsync(IHolon holon)
        {
            throw new NotImplementedException();
        }

        public override Task<IEnumerable<IHolon>> SaveHolonsAsync(IEnumerable<IHolon> holons)
        {
            throw new NotImplementedException();
        }

        public override Task<IAvatar> LoadAvatarForProviderKeyAsync(string providerKey)
        {
            throw new NotImplementedException();
        }

        public override IAvatar LoadAvatarForProviderKey(string providerKey)
        {
            throw new NotImplementedException();
        }

        public override bool DeleteAvatar(string providerKey, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override Task<bool> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override bool DeleteHolon(Guid id, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override Task<bool> DeleteHolonAsync(Guid id, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override bool DeleteHolon(string providerKey, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override Task<bool> DeleteHolonAsync(string providerKey, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<IHolon> LoadHolonsForParent(Guid id, HolonType type = HolonType.Holon)
        {
            throw new NotImplementedException();
        }

        public override Task<IEnumerable<IHolon>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.Holon)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<IHolon> LoadHolonsForParent(string providerKey, HolonType type = HolonType.Holon)
        {
            throw new NotImplementedException();
        }

        public override Task<IEnumerable<IHolon>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.Holon)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<IHolon> LoadAllHolons(HolonType type = HolonType.Holon)
        {
            throw new NotImplementedException();
        }

        public override Task<IEnumerable<IHolon>> LoadAllHolonsAsync(HolonType type = HolonType.Holon)
        {
            throw new NotImplementedException();
        }
    }
}
