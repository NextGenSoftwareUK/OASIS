using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;

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
        
        public OASISResult<TransactionRespone> SendTransaction(IWalletTransaction transaction);
        public Task<OASISResult<TransactionRespone>> SendTransactionAsync(IWalletTransaction transaction);
        
        public OASISResult<TransactionRespone> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount);
        public Task<OASISResult<TransactionRespone>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount);

        public OASISResult<TransactionRespone> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token);
        public Task<OASISResult<TransactionRespone>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token);

        public Task<OASISResult<TransactionRespone>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount);
        public OASISResult<TransactionRespone> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount);

        public Task<OASISResult<TransactionRespone>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token);
        public OASISResult<TransactionRespone> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token);


        public Task<OASISResult<TransactionRespone>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount);
        public OASISResult<TransactionRespone> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount);

        public Task<OASISResult<TransactionRespone>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token);
        public OASISResult<TransactionRespone> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token);

        public OASISResult<TransactionRespone> SendTransactionByDefaultWallet(Guid fromAvatarId, Guid toAvatarId, decimal amount);
        public Task<OASISResult<TransactionRespone>> SendTransactionByDefaultWalletAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount);
    }
}