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
            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage);
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Network));

            Host = host;
            Username = username;
            Password = password;
            
            // Initialize Neo4j driver for REAL database operations
            var uri = Uri.TryCreate(host, UriKind.Absolute, out var configuredUri) &&
                      (configuredUri.Scheme == "bolt" || configuredUri.Scheme == "neo4j")
                ? configuredUri.AbsoluteUri
                : $"bolt://{host}:7687";
            _driver = GraphDatabase.Driver(uri, AuthTokens.Basic(username, password));
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().GetAwaiter().GetResult();

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            try
            {
                await _driver.VerifyConnectivityAsync();
                IsProviderActivated = true;
                return new OASISResult<bool>(true);
            }
            catch (Exception ex)
            {
                IsProviderActivated = false;
                return new OASISResult<bool>(false) { IsError = true, Message = $"Neo4j activation failed: {ex.Message}", Exception = ex };
            }
        }

        public override OASISResult<bool> DeActivateProvider()
            => DeActivateProviderAsync().GetAwaiter().GetResult();

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            try
            {
                await _driver.CloseAsync();
                IsProviderActivated = false;
                return new OASISResult<bool>(true);
            }
            catch (Exception ex)
            {
                return new OASISResult<bool>(false) { IsError = true, Message = $"Neo4j deactivation failed: {ex.Message}", Exception = ex };
            }
        }

    }
}
