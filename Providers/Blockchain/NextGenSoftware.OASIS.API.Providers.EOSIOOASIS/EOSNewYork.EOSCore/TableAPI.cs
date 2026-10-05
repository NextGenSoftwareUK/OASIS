using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EOSNewYork.EOSCore.Response.Table;
using EOSNewYork.EOSCore.Lib;

namespace EOSNewYork.EOSCore
{
    public class TableAPI : BaseAPI
    {
        public TableAPI() {}

        public TableAPI(string host) : base(host) {}

        public async Task<List<GlobalRow>> GetGlobalRowsAsync()
        {
            return await GetTableRowsAsync<GlobalRow>();
        }
        public List<GlobalRow> GetGlobalRows()
        {
            return GetGlobalRowsAsync().Result;
        }
        public async Task<List<NameBidsRow>> GetNameBidRowsAsync()
        {
            return await GetTableRowsAsync<NameBidsRow>();
        }
        public List<NameBidsRow> GetNameBidRows()
        {
            return GetNameBidRowsAsync().Result;
        }
        public async Task<List<ProducerRow>> GetProducerRowsAsync()
        {
            return await GetTableRowsAsync<ProducerRow>();
        }
        public List<ProducerRow> GetProducerRows()
        {
            return GetProducerRowsAsync().Result;
        }
        public async Task<List<VoterRow>> GetVoterRowsAsync()
        {
            return await GetTableRowsAsync<VoterRow>();
        }
        public async Task<List<UnusedaccntsWhitelistRow>> GetUnusedaccntsWhitelistRowsAsync()
        {
            return await GetTableRowsAsync<UnusedaccntsWhitelistRow>();
        }

        public async Task<List<BancorConnectorSettingsRow>> GetBancorConnectorSettingsAsync(BancorConnectorSettingsConstructorSettings settings)
        {
            return await GetTableRowsAsync<BancorConnectorSettingsRow>(settings);
        }

        public async Task<List<BancorConnectorReservesRow>> GetBancorConnectorReservesAsync(BancorConnectorReservesConstructorSettings settings)
        {
            return await GetTableRowsAsync<BancorConnectorReservesRow>(settings);
        }

        public async Task<List<GetTokenAccountBalanceRow>> GetTokenAccountBalanceAsync(GetTokenAccountBalanceConstructorSettings settings)
        {
            return await GetTableRowsAsync<GetTokenAccountBalanceRow>(settings);
        }

        public async Task<List<GetTokenStatsRow>> GetTokenStatsAsync(GetTokenStatsConstructorSettings settings)
        {
            return await GetTableRowsAsync<GetTokenStatsRow>(settings);
        }

        public List<VoterRow> GetVoterRows()
        {
            return GetVoterRowsAsync().Result;
        }
        public List<UnusedaccntsWhitelistRow> GetUnusedaccntsWhitelistRows()
        {
            return GetUnusedaccntsWhitelistRowsAsync().Result;
        }

        public List<BancorConnectorSettingsRow> GetBancorConnectorSettings(BancorConnectorSettingsConstructorSettings settings)
        {
            return GetBancorConnectorSettingsAsync(settings).Result;
        }

        public List<BancorConnectorReservesRow> GetBancorConnectorReserves(BancorConnectorReservesConstructorSettings settings)
        {
            return GetBancorConnectorReservesAsync(settings).Result;
        }

        public List<GetTokenAccountBalanceRow> GetTokenAccountBalance(GetTokenAccountBalanceConstructorSettings settings)
        {
            return GetTokenAccountBalanceAsync(settings).Result;
        }

        public List<GetTokenStatsRow> GetTokenStats(GetTokenStatsConstructorSettings settings)
        {
            return GetTokenStatsAsync(settings).Result;
        }

        public async Task<List<T>> GetTableRowsAsync<T>(object settings = null) where T : IEOSTable
        {
            return await new EOS_Table<T>(HOST).GetRowsFromAPIAsync(settings);
        }
        public List<T> GetTableRows<T>(object settings = null) where T : IEOSTable
        {
            return GetTableRowsAsync<T>(settings).Result;
        }

        public async Task<List<RammarketRow>> GetRammarketRowsAsync()
        {
            return await GetTableRowsAsync<RammarketRow>();
        }
        public List<RammarketRow> GetRammarketRows()
        {
            return GetRammarketRowsAsync().Result;
        }

    }
}
