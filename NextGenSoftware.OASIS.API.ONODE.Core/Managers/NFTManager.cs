using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.ONode.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.ONode.Core.Interfaces.Managers;

namespace NextGenSoftware.OASIS.API.ONode.Core.Managers
{
    public class NFTManager : OASISManager, INFTManager
    {
        private static NFTManager _instance = null;

        public static NFTManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new NFTManager(ProviderManager.CurrentStorageProvider);

                return _instance;
            }
        }

        public NFTManager(IOASISStorageProvider OASISStorageProvider, OASISDNA OASISDNA = null) : base(OASISStorageProvider, OASISDNA)
        {

        }

        public NFTManager(OASISDNA OASISDNA = null) : base(OASISDNA)
        {

        }

        //TODO: This method may become obsolete if ProviderType changes to NFTProviderType on INFTWalletTransaction
        public async Task<OASISResult<TransactionRespone>> CreateNftTransactionAsync(CreateNftTransactionRequest request)
        {
            return await CreateNftTransactionAsync(new NFTWalletTransaction()
            {
                Amount = request.Amount,
                //Date = DateTime.Now,
                FromWalletAddress = request.FromWalletAddress,
                MemoText = request.MemoText,
                MintWalletAddress = request.MintWalletAddress,
                ToWalletAddress = request.ToWalletAddress,
                Token = request.Token,
                ProviderType = GetProviderTypeFromNFTProviderType(request.NFTProviderType)
            });
        }

        //TODO: This method may become obsolete if ProviderType changes to NFTProviderType on INFTWalletTransaction
        public OASISResult<TransactionRespone> CreateNftTransaction(CreateNftTransactionRequest request)
        {
            return CreateNftTransaction(new NFTWalletTransaction()
            {
                Amount = request.Amount,
                //Date = DateTime.Now,
                FromWalletAddress = request.FromWalletAddress,
                MemoText = request.MemoText,
                MintWalletAddress = request.MintWalletAddress,
                ToWalletAddress = request.ToWalletAddress,
                Token = request.Token,
                ProviderType = GetProviderTypeFromNFTProviderType(request.NFTProviderType)
            });
        }

        public async Task<OASISResult<TransactionRespone>> CreateNftTransactionAsync(INFTWalletTransaction request)
        {
            OASISResult<TransactionRespone> result = new OASISResult<TransactionRespone>();
            string errorMessage = "Error occured in CreateNftTransactionAsync in NFTManager. Reason:";

            //if (request.Date == DateTime.MinValue)
            //    request.Date = DateTime.Now;

            try
            {
                IOASISNFTProvider nftProvider = GetNFTProvider(request.ProviderType, ref result, errorMessage);

                if (nftProvider == null)
                    result = await nftProvider.SendNFTAsync(request);
            }
            catch (Exception e)
            {
                ErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public OASISResult<TransactionRespone> CreateNftTransaction(INFTWalletTransaction request)
        {
            OASISResult<TransactionRespone> result = new OASISResult<TransactionRespone>();
            string errorMessage = "Error occured in CreateNftTransactionAsync in NFTManager. Reason:";

            //if (request.Date == DateTime.MinValue)
            //    request.Date = DateTime.Now;

            try
            {
                IOASISNFTProvider nftProvider = GetNFTProvider(request.ProviderType, ref result, errorMessage);

                if (nftProvider == null)
                    result = nftProvider.SendNFT(request);
            }
            catch (Exception e)
            {
                ErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<TransactionRespone>> MintNftAsync(IMintNFTTransaction request)
        {
            OASISResult<TransactionRespone> result = new OASISResult<TransactionRespone>();
            string errorMessage = "Error occured in MintNftAsync in NFTManager. Reason:";

            try
            {
                IOASISNFTProvider nftProvider = GetNFTProvider(request.OnChainProvider, ref result, errorMessage);

                if (nftProvider == null)
                    result = await nftProvider.MintNFTAsync(request);
            }
            catch (Exception e)
            {
                ErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public OASISResult<TransactionRespone> MintNft(IMintNFTTransaction request)
        {
            OASISResult<TransactionRespone> result = new OASISResult<TransactionRespone>();
            string errorMessage = "Error occured in MintNftAsync in NFTManager. Reason:";

            try
            {
                IOASISNFTProvider nftProvider = GetNFTProvider(request.OnChainProvider, ref result, errorMessage);

                if (nftProvider == null)
                    result = nftProvider.MintNFT(request);
            }
            catch (Exception e)
            {
                ErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IOASISNFT>> LoadNftAsync(Guid id, NFTProviderType NFTProviderType)
        {
            OASISResult<IOASISNFT> result = new OASISResult<IOASISNFT>();
            string errorMessage = "Error occured in LoadNftAsync in NFTManager. Reason:";

            try
            {
                IOASISNFTProvider nftProvider = GetNFTProvider(NFTProviderType, ref result, errorMessage);

                if (nftProvider == null)
                    result = await nftProvider.LoadNFTAsync(id);
            }
            catch (Exception e)
            {
                ErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public OASISResult<IOASISNFT> LoadNft(Guid id, NFTProviderType NFTProviderType)
        {
            OASISResult<IOASISNFT> result = new OASISResult<IOASISNFT>();
            string errorMessage = "Error occured in LoadNft in NFTManager. Reason:";

            try
            {
                IOASISNFTProvider nftProvider = GetNFTProvider(NFTProviderType, ref result, errorMessage);

                if (nftProvider == null)
                    result = nftProvider.LoadNFT(id);
            }
            catch (Exception e)
            {
                ErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public ProviderType GetProviderTypeFromNFTProviderType(NFTProviderType nftProviderType)
        {
            ProviderType providerType = ProviderType.None;

            switch (nftProviderType)
            {
                case NFTProviderType.Solana:
                    providerType = ProviderType.SolanaOASIS;
                    break;

                case NFTProviderType.EOS:
                    providerType = ProviderType.EOSIOOASIS;
                    break;

                case NFTProviderType.Ethereum:
                    providerType = ProviderType.EthereumOASIS;
                    break;
            }

            return providerType;
        }

        public NFTProviderType GetNFTProviderTypeFromProviderType(ProviderType providerType)
        {
            NFTProviderType nftProviderType = NFTProviderType.None;

            switch (providerType)
            {
                case ProviderType.SolanaOASIS:
                    nftProviderType = NFTProviderType.Solana;
                    break;

                case ProviderType.EOSIOOASIS:
                    nftProviderType = NFTProviderType.EOS;
                    break;

                case ProviderType.EthereumOASIS:
                    nftProviderType = NFTProviderType.Ethereum;
                    break;
            }

            return nftProviderType;
        }

        public IOASISNFTProvider GetNFTProvider<T>(NFTProviderType NFTProviderType, ref OASISResult<T> result, string errorMessage)
        {
            return GetNFTProvider(GetProviderTypeFromNFTProviderType(NFTProviderType), ref result, errorMessage);
        }

        public IOASISNFTProvider GetNFTProvider<T>(ProviderType providerType, ref OASISResult<T> result, string errorMessage)
        {
            IOASISNFTProvider nftProvider = null;
            IOASISProvider OASISProvider = ProviderManager.GetProvider(providerType);

            if (OASISProvider != null)
            {
                if (!OASISProvider.IsProviderActivated)
                {
                    OASISResult<bool> activateProviderResult = OASISProvider.ActivateProvider();

                    if (activateProviderResult.IsError)
                        ErrorHandling.HandleError(ref result, $"{errorMessage} Error occured activating provider. Reason: {activateProviderResult.Message}");
                }
            }
            else
                ErrorHandling.HandleError(ref result, $"{errorMessage} The {Enum.GetName(typeof(ProviderType), providerType)} provider was not found.");

            if (!result.IsError)
            {
                nftProvider = OASISProvider as IOASISNFTProvider;

                if (nftProvider == null)
                    ErrorHandling.HandleError(ref result, $"{errorMessage} The {Enum.GetName(typeof(ProviderType), providerType)} provider is not a valid OASISNFTProvider.");
            }

            return nftProvider;
        }

        //TODO: Lots more coming soon! ;-)
    }
}