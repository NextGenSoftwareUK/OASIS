using NextGenSoftware.OASIS.API.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using Neo4j.Driver;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS
{
    public partial class Neo4jOASIS : OASISStorageProviderBase, IOASISDBStorageProvider, IOASISNETProvider
    {
        public string Host { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool IsVersionControlEnabled { get; set; }
        private readonly IDriver _driver;

        public Neo4jOASIS(string host, string username, string password)
        {
            this.ProviderName = "Neo4jOASIS";
            this.ProviderDescription = "Neo4j Provider";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.Neo4jOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);

            Host = host;
            Username = username;
            Password = password;
            
            // Initialize Neo4j driver for REAL database operations
            _driver = GraphDatabase.Driver($"bolt://{host}:7687", AuthTokens.Basic(username, password));
        }

    }
}
