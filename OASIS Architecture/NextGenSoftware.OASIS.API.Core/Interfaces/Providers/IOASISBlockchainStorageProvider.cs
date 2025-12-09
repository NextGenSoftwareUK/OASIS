using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    /// <summary>
    /// This interface is responsbile for persisting data/state to blockchain storage providers.
    /// </summary>
    public interface IOASISBlockchainStorageProvider : IOASISStorageProvider
    {
        // Blockchain providers have version control built in because they always store a new record rather than updating an existing.
        // Central Storage/DB's by default can update the same record, if this flag is set below then they will act more like a Blockchain and store a new copy of the record and link to the previous version.
        // You cannot turn VersionControl off for Blockchains because it is built in.
        // public bool IsVersionControlEnabled { get; set; }

        public OASISResult<ITransactionResponse> SendToken(ISendWeb3TokenRequest request);
        public Task<OASISResult<ITransactionResponse>> SendTokenAsync(ISendWeb3TokenRequest request);
        public OASISResult<ITransactionResponse> MintToken(IMintWeb3TokenRequest request);
        public Task<OASISResult<ITransactionResponse>> MintTokenAsync(IMintWeb3TokenRequest request);
        public OASISResult<ITransactionResponse> BurnToken(IBurnWeb3TokenRequest request);
        public Task<OASISResult<ITransactionResponse>> BurnTokenAsync(IBurnWeb3TokenRequest request);
        public OASISResult<ITransactionResponse> LockToken(ILockWeb3TokenRequest request);
        public Task<OASISResult<ITransactionResponse>> LockTokenAsync(ILockWeb3TokenRequest request);
        public OASISResult<ITransactionResponse> UnlockToken(IUnlockWeb3TokenRequest request);
        public Task<OASISResult<ITransactionResponse>> UnlockTokenAsync(IUnlockWeb3TokenRequest request);
        public OASISResult<double> GetBalance(IGetWeb3WalletBalanceRequest request);
        public Task<OASISResult<double>> GetBalanceAsync(IGetWeb3WalletBalanceRequest request);
        public OASISResult<IList<IWalletTransaction>> GetTransactions(IGetWeb3TransactionsRequest request);
        public Task<OASISResult<IList<IWalletTransaction>>> GetTransactionsAsync(IGetWeb3TransactionsRequest request);
        public OASISResult<IKeyPairAndWallet> GenerateKeyPair(IGetWeb3WalletBalanceRequest request);
        public Task<OASISResult<IKeyPairAndWallet>> GenerateKeyPairAsync(IGetWeb3WalletBalanceRequest request);
    }
}