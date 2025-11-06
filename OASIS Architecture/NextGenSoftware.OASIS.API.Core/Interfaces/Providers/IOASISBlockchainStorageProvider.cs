using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallets.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallets.Response;
using NextGenSoftware.OASIS.Common;

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

        public OASISResult<ITransactionRespone> SendTransaction(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText);
        public Task<OASISResult<ITransactionRespone>> SendTransactionAsync(string fromWalletAddress, string toWalletAddres, decimal amount, string memoText);

        //TODO: Implement ASAP!
        //public OASISResult<IWeb4NFTTransactionRespone> MintToken(IMintNFTTransactionRequest transation);
        //public Task<OASISResult<IWeb4NFTTransactionRespone>> MintTokenAsync(IMintNFTTransactionRequest transation);

        //public OASISResult<ITransactionRespone> BurnToken(string tokenAddress);
        //public Task<OASISResult<ITransactionRespone>> BurnTokenAsync(string tokenAddress);

        //public OASISResult<ITransactionRespone> GetBalance(string walletAddress);
        //public Task<OASISResult<ITransactionRespone>> GetBalanceAsync(string walletAddress);


        //OBSOLETE
        /*
        public OASISResult<ITransactionRespone> SendTransaction(IWalletTransactionRequest transaction);
        public Task<OASISResult<ITransactionRespone>> SendTransactionAsync(IWalletTransactionRequest transaction);
        
        public OASISResult<ITransactionRespone> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount);
        public Task<OASISResult<ITransactionRespone>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount);

        public OASISResult<ITransactionRespone> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token);
        public Task<OASISResult<ITransactionRespone>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token);

        public Task<OASISResult<ITransactionRespone>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount);
        public OASISResult<ITransactionRespone> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount);

        public Task<OASISResult<ITransactionRespone>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token);
        public OASISResult<ITransactionRespone> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token);


        public Task<OASISResult<ITransactionRespone>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount);
        public OASISResult<ITransactionRespone> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount);

        public Task<OASISResult<ITransactionRespone>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token);
        public OASISResult<ITransactionRespone> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token);

        public OASISResult<ITransactionRespone> SendTransactionByDefaultWallet(Guid fromAvatarId, Guid toAvatarId, decimal amount);
        public Task<OASISResult<ITransactionRespone>> SendTransactionByDefaultWalletAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount);
        */
    }
}