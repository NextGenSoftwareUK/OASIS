using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Interfaces;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using System.Linq;
using Microsoft.Data.Sqlite;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS
{
    public partial class SQLLiteDBOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISDBStorageProvider, IOASISLocalStorageProvider, IOASISNETProvider, IOASISSuperStar, IDisposable
    {
        private readonly DataContext _appDataContext;

        private readonly IAvatarDetailRepository _avatarDetailRepository;
        private readonly IAvatarRepository _avatarRepository;
        private readonly IHolonRepository _holonRepository;
        private readonly SqliteConnection _memoryLifetimeConnection;

        public SQLLiteDBOASIS(string connectionString)
        {
            this.ProviderName = "SQLLiteDBOASIS";
            this.ProviderDescription = "SQLLiteDBOASIS Provider";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.SQLLiteDBOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageLocalAndNetwork);

            _appDataContext = new DataContext(connectionString);
            // Resolve the actual default once instead of opening another unnamed
            // temporary database when a native operation receives an empty DSN.
            var settings = new SqliteConnectionStringBuilder(_appDataContext.Database.GetConnectionString());
            if (settings.Mode == SqliteOpenMode.Memory || settings.DataSource == ":memory:")
            {
                // A private :memory: connection is otherwise a different store on
                // every EF/native open. Own one named memory store for this provider;
                // explicitly shared named stores retain their supplied identity.
                if (settings.DataSource == ":memory:" || string.IsNullOrEmpty(settings.DataSource)
                    || settings.Cache != SqliteCacheMode.Shared)
                    settings.DataSource = "oasis-memory-" + Guid.NewGuid().ToString("N");
                settings.Mode = SqliteOpenMode.Memory;
                settings.Cache = SqliteCacheMode.Shared;
                settings.Pooling = false;
                _appDataContext.Dispose();
                _appDataContext = new DataContext(settings.ToString());
                _memoryLifetimeConnection = new SqliteConnection(settings.ToString());
                try { _memoryLifetimeConnection.Open(); }
                catch { _memoryLifetimeConnection.Dispose(); _appDataContext.Dispose(); throw; }
            }
            _ownConnectionString = settings.ToString();
            _avatarDetailRepository = new AvatarDetailRepository(_appDataContext);
            _avatarRepository = new AvatarRepository(_appDataContext);
            _holonRepository = new HolonRepository(_appDataContext,()=>IsVersionControlEnabled);
        }

        public void Dispose()
        {
            IsProviderActivated = false;
            _appDataContext.Dispose();
            _memoryLifetimeConnection?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
