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
using System.Threading;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests")]

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS
{
    public partial class Neo4jOASIS : OASISStorageProviderBase, IOASISDBStorageProvider, IOASISNETProvider, IProviderTransportFailureClassifier
    {
        // Native operations do not pass caller cancellation tokens to the driver. Its typed
        // ServiceUnavailableException owns nested connect-deadline cancellations; a raw OCE does not.
        public bool IsProviderTransportFailure(Exception failure)=>failure is ServiceUnavailableException;
        public string Host { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool IsVersionControlEnabled { get; set; }
        private IDriver _driver;
        private readonly Func<IDriver> _driverFactory;
        private readonly SemaphoreSlim _lifecycleGate = new(1, 1);

        public Neo4jOASIS(string host, string username, string password)
            : this(host, username, password, null) { }

        internal Neo4jOASIS(string host, string username, string password, Func<IDriver> driverFactory)
        {
            this.ProviderName = "Neo4jOASIS";
            this.ProviderDescription = "Neo4j Provider";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.Neo4jOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);

            Host = host;
            Username = username;
            Password = password;
            
            _driverFactory = driverFactory ?? CreateDriver;
            _driver = _driverFactory();
        }

        private IDriver CreateDriver()
        {
            return GraphDatabase.Driver(ConnectionUri(Host), AuthTokens.Basic(Username, Password));
        }

        internal static Uri ConnectionUri(string host)
        {
            if (string.IsNullOrWhiteSpace(host)) throw new ArgumentException("An explicit Neo4j host is required.", nameof(host));
            var explicitScheme = host.Contains("://", StringComparison.Ordinal);
            if (!Uri.TryCreate(explicitScheme ? host : "bolt://" + host, UriKind.Absolute, out var uri)
                || string.IsNullOrEmpty(uri.Host) || !string.IsNullOrEmpty(uri.UserInfo)
                || uri.AbsolutePath != "/" && uri.AbsolutePath != ""
                || !(new[] { "bolt", "bolt+s", "bolt+ssc", "neo4j", "neo4j+s", "neo4j+ssc" }).Contains(uri.Scheme, StringComparer.Ordinal))
                throw new ArgumentException("A valid Neo4j connection URI or hostname is required.", nameof(host));
            return !explicitScheme && uri.Port == -1 ? new UriBuilder(uri) { Port = 7687 }.Uri : uri;
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().GetAwaiter().GetResult();

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            await _lifecycleGate.WaitAsync();
            try
            {
                _driver ??= _driverFactory();
                await _driver.VerifyConnectivityAsync().ConfigureAwait(false);
                await EnsureHolonWriteSchemaAsync().ConfigureAwait(false);
                IsProviderActivated = true;
                return new OASISResult<bool>(true);
            }
            catch (Exception ex)
            {
                IsProviderActivated = false;
                return new OASISResult<bool>(false) { IsError = true, Message = $"Neo4j activation failed: {ex.Message}", Exception = ex };
            }
            finally { _lifecycleGate.Release(); }
        }

        public override OASISResult<bool> DeActivateProvider()
            => DeActivateProviderAsync().GetAwaiter().GetResult();

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            await _lifecycleGate.WaitAsync();
            try
            {
                if (_driver != null) await _driver.CloseAsync();
                _driver = null;
                IsProviderActivated = false;
                return new OASISResult<bool>(true);
            }
            catch (Exception ex)
            {
                return new OASISResult<bool>(false) { IsError = true, Message = $"Neo4j deactivation failed: {ex.Message}", Exception = ex };
            }
            finally { _lifecycleGate.Release(); }
        }

    }
}
