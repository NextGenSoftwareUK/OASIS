using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using NextGenSoftware.OASIS.API.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
            this.ProviderType = ProviderType.SQLLiteDBOASIS;
            this.ProviderCategory = ProviderCategory.StorageAndNetwork;
        }

        public Task<bool> AddKarmaToAvatarAsync(IAvatar Avatar, int karma)
        {
            throw new NotImplementedException();
        }

        public List<IHolon> GetHolonsNearMe(HolonType Type)
        {
            throw new NotImplementedException();
        }

        public List<IPlayer> GetPlayersNearMe()
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
            return new Task<IAvatar>(() =>  _context.Avatars.FirstOrDefault(x => x.ProviderKey == providerKey));
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
        public override async Task<ISearchResults> SearchAsync(string searchTerm)
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
    }
}
