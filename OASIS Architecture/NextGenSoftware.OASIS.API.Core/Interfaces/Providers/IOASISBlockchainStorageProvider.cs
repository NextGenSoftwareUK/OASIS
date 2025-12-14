using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
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

        // Bridge methods ported from IOASISBridge
        /// <summary>
        /// Creates a new account and returns the public key, private key, and seed phrase.
        /// </summary>
        /// <param name="token">A cancellation token to support cancellation of the operation.</param>
        /// <returns>A tuple containing the public key, private key, and seed phrase of the created account.</returns>
        Task<OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>> CreateAccountAsync(CancellationToken token = default);

        /// <summary>
        /// Restores an existing account using a seed phrase, returning the public and private keys.
        /// </summary>
        /// <param name="seedPhrase">The seed phrase used to restore the account.</param>
        /// <param name="token">A cancellation token to support cancellation of the operation.</param>
        /// <returns>A tuple containing the public key and private key of the restored account.</returns>
        Task<OASISResult<(string PublicKey, string PrivateKey)>> RestoreAccountAsync(string seedPhrase, CancellationToken token = default);

        /// <summary>
        /// Initiates a withdrawal of a specified amount from an account.
        /// </summary>
        /// <param name="amount">The amount to withdraw.</param>
        /// <param name="senderAccountAddress">The address of the account to withdraw from.</param>
        /// <param name="senderPrivateKey">The private key of the client initiating the withdrawal.</param>
        /// <returns>A response containing the transaction details of the withdrawal.</returns>
        Task<OASISResult<BridgeTransactionResponse>> WithdrawAsync(decimal amount, string senderAccountAddress, string senderPrivateKey);

        /// <summary>
        /// Initiates a deposit of a specified amount into an account.
        /// </summary>
        /// <param name="amount">The amount to deposit.</param>
        /// <param name="receiverAccountAddress">The address of the account to deposit to.</param>
        /// <returns>A response containing the transaction details of the deposit.</returns>
        Task<OASISResult<BridgeTransactionResponse>> DepositAsync(decimal amount, string receiverAccountAddress);

        /// <summary>
        /// Retrieves the status of a transaction using its transaction hash.
        /// </summary>
        /// <param name="transactionHash">The hash of the transaction to check the status of.</param>
        /// <param name="token">A cancellation token to support cancellation of the operation.</param>
        /// <returns>The status of the transaction.</returns>
        Task<OASISResult<BridgeTransactionStatus>> GetTransactionStatusAsync(string transactionHash, CancellationToken token = default);
    }
}