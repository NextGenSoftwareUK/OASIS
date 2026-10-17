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

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS
{
    public partial class SQLLiteDBOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISDBStorageProvider, IOASISLocalStorageProvider, IOASISNETProvider, IOASISSuperStar
    {
        private readonly DataContext _appDataContext;

        private readonly IAvatarDetailRepository _avatarDetailRepository;
        private readonly IAvatarRepository _avatarRepository;
        private readonly IHolonRepository _holonRepository;

        public SQLLiteDBOASIS(string connectionString)
        {
            this.ProviderName = "SQLLiteDBOASIS";
            this.ProviderDescription = "SQLLiteDBOASIS Provider";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.SQLLiteDBOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageLocalAndNetwork);

            _appDataContext = new DataContext(connectionString);
            _avatarDetailRepository = new AvatarDetailRepository(_appDataContext);
            _avatarRepository = new AvatarRepository(_appDataContext);
            _holonRepository = new HolonRepository(_appDataContext);
        }
    }
}
