using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.ONode.Core.Objects;
using NextGenSoftware.OASIS.API.ONode.Core.Interfaces.Managers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search.Holon;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;

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
        public async Task<OASISResult<NFTTransactionRespone>> CreateNftTransactionAsync(CreateNftTransactionRequest request)
        {
            return await CreateNftTransactionAsync(new NFTWalletTransaction()
            {
                Amount = request.Amount,
                //Date = DateTime.Now,
                FromWalletAddress = request.FromWalletAddress,
                MemoText = request.MemoText,
                MintWalletAddress = request.MintWalletAddress,
                ToWalletAddress = request.ToWalletAddress,
                //Token = request.Token,
                ProviderType = GetProviderTypeFromNFTProviderType(request.NFTProviderType)
            });
        }

        //TODO: This method may become obsolete if ProviderType changes to NFTProviderType on INFTWalletTransaction
        public OASISResult<NFTTransactionRespone> CreateNftTransaction(CreateNftTransactionRequest request)
        {
            return CreateNftTransaction(new NFTWalletTransaction()
            {
                Amount = request.Amount,
                //Date = DateTime.Now,
                FromWalletAddress = request.FromWalletAddress,
                MemoText = request.MemoText,
                MintWalletAddress = request.MintWalletAddress,
                ToWalletAddress = request.ToWalletAddress,
                //Token = request.Token,
                ProviderType = GetProviderTypeFromNFTProviderType(request.NFTProviderType)
            });
        }

        public async Task<OASISResult<NFTTransactionRespone>> CreateNftTransactionAsync(INFTWalletTransaction request)
        {
            OASISResult<NFTTransactionRespone> result = new OASISResult<NFTTransactionRespone>();
            string errorMessage = "Error occured in CreateNftTransactionAsync in NFTManager. Reason:";

            //if (request.Date == DateTime.MinValue)
            //    request.Date = DateTime.Now;

            try
            {
                OASISResult<IOASISNFTProvider> nftProviderResult = GetNFTProvider(request.ProviderType, errorMessage);

                if (nftProviderResult != null && nftProviderResult.Result != null && !nftProviderResult.IsError)
                    result = await nftProviderResult.Result.SendNFTAsync(request);
                else
                {
                    result.Message = nftProviderResult.Message;
                    result.IsError = true;
                }
            }
            catch (Exception e)
            {
                ErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public OASISResult<NFTTransactionRespone> CreateNftTransaction(INFTWalletTransaction request)
        {
            OASISResult<NFTTransactionRespone> result = new OASISResult<NFTTransactionRespone>();
            string errorMessage = "Error occured in CreateNftTransactionAsync in NFTManager. Reason:";

            //if (request.Date == DateTime.MinValue)
            //    request.Date = DateTime.Now;

            try
            {
                OASISResult<IOASISNFTProvider> nftProviderResult = GetNFTProvider(request.ProviderType, errorMessage);

                if (nftProviderResult != null && nftProviderResult.Result != null && !nftProviderResult.IsError)
                    result = nftProviderResult.Result.SendNFT(request);
                else
                {
                    result.Message = nftProviderResult.Message;
                    result.IsError = true;
                }
            }
            catch (Exception e)
            {
                ErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        //public async Task<OASISResult<NFTTransactionRespone>> MintNftAsync(IMintNFTTransaction request)
        //{
        //    OASISResult<NFTTransactionRespone> result = new OASISResult<NFTTransactionRespone>();
        //    string errorMessage = "Error occured in MintNftAsync in NFTManager. Reason:";

        //    try
        //    {
        //        OASISResult<IOASISNFTProvider> nftProviderResult = GetNFTProvider(request.OnChainProvider, errorMessage);

        //        if (nftProviderResult != null && nftProviderResult.Result != null && !nftProviderResult.IsError)
        //        {
        //            request.MemoText = $"{Enum.GetName(typeof(ProviderType), request.OnChainProvider)} NFT minted on The OASIS with title {request.Title} by AvatarId {request.MintedByAvatarId} for price {request.Price}. {request.MemoText}";

        //            result = await nftProviderResult.Result.MintNFTAsync(request);

        //            if (result != null && !result.IsError)
        //            {
        //                IOASISProvider offChainProvider = ProviderManager.GetProvider(request.OffChainProvider);

        //                if (offChainProvider != null)
        //                {
        //                    if (!offChainProvider.IsProviderActivated)
        //                    {
        //                        OASISResult<bool> activateProviderResult = await offChainProvider.ActivateProviderAsync();

        //                        if (activateProviderResult.IsError)
        //                            ErrorHandling.HandleError(ref result, $"{errorMessage} Error occured activating OffChainProvider. Reason: {activateProviderResult.Message}");
        //                    }

        //                    if (!result.IsError)
        //                    {
        //                        IOASISStorageProvider storageProvider = offChainProvider as IOASISStorageProvider;

        //                        if (storageProvider != null)
        //                        {
        //                            result.Result.OASISNFT = new OASISNFT()
        //                            {
        //                                Id = Guid.NewGuid(),
        //                                Hash = result.Result.TransactionResult,
        //                                MintedByAddress = request.MintWalletAddress,
        //                                MintedByAvatarId = request.MintedByAvatarId,
        //                                Price = request.Price,
        //                                Discount = request.Discount,
        //                                Thumbnail = request.Thumbnail,
        //                                ThumbnailUrl = request.ThumbnailUrl,
        //                                OnChainProvider = request.OnChainProvider,
        //                                OffChainProvider = request.OffChainProvider,
        //                                OffChainProviderHolonId = Guid.NewGuid(),
        //                                //Token= request.Token
        //                            };

        //                            HolonManager holonManager = new HolonManager(storageProvider);

        //                            Holon holonNFT = new Holon(HolonType.NFT);
        //                            holonNFT.Id = result.Result.OASISNFT.OffChainProviderHolonId;
        //                            holonNFT.IsNewHolon = true;
        //                            holonNFT.Name = $"{Enum.GetName(typeof(ProviderType), request.OnChainProvider)} NFT Minted On The OASIS with title {request.Title}";
        //                            holonNFT.Description = request.MemoText;
        //                            holonNFT.MetaData["NFT.OASISNFT"] = JsonSerializer.Serialize(result.Result.OASISNFT);
        //                            holonNFT.MetaData["NFT.Hash"] = result.Result.TransactionResult;
        //                            holonNFT.MetaData["NFT.Id"] = result.Result.OASISNFT.Id;
        //                            holonNFT.MetaData["NFT.MintedByAvatarId"] = request.MintedByAvatarId.ToString();
        //                            holonNFT.MetaData["NFT.MintWalletAddress"] = request.MintWalletAddress;
        //                            holonNFT.MetaData["NFT.MemoText"] = request.MemoText;
        //                            holonNFT.MetaData["NFT.Title"] = request.Title;
        //                            holonNFT.MetaData["NFT.Description"] = request.Description;
        //                            holonNFT.MetaData["NFT.Price"] = request.Price.ToString();
        //                            holonNFT.MetaData["NFT.NumberToMint"] = request.NumberToMint.ToString();
        //                            holonNFT.MetaData["NFT.OnChainProvider"] = Enum.GetName(typeof(ProviderType), request.OnChainProvider);
        //                            holonNFT.MetaData["NFT.OffChainProvider"] = Enum.GetName(typeof(ProviderType), request.OffChainProvider);
        //                            holonNFT.MetaData["NFT.Thumbnail"] = request.Thumbnail;
        //                            holonNFT.MetaData["NFT.ThumbnailUrl"] = request.ThumbnailUrl;

        //                            OASISResult<IHolon> saveHolonResult = await holonManager.SaveHolonAsync(holonNFT);

        //                             if (saveHolonResult != null && (saveHolonResult.IsError || saveHolonResult.Result != null) || saveHolonResult == null)
        //                                ErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving metadata holon to the OffChainProvider {Enum.GetName(typeof(ProviderType), request.OffChainProvider)}. Reason: {saveHolonResult.Message}");
        //                        }
        //                        else
        //                            ErrorHandling.HandleError(ref result, $"{errorMessage} The {Enum.GetName(typeof(ProviderType), request.OffChainProvider)} OffChainProvider is not a valid IOASISStorageProvider.");
        //                    }
        //                }
        //                else
        //                    ErrorHandling.HandleError(ref result, $"{errorMessage} The {Enum.GetName(typeof(ProviderType), request.OffChainProvider)} OffChainProvider was not found.");
        //            }
        //        }
        //        else
        //        {
        //            result.Message = nftProviderResult.Message;
        //            result.IsError = true;
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        ErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
        //    }

        //    return result;
        //}

        public async Task<OASISResult<NFTTransactionRespone>> MintNftAsync(IMintNFTTransaction request)
        {
            OASISResult<NFTTransactionRespone> result = new OASISResult<NFTTransactionRespone>();
            string errorMessage = "Error occured in MintNftAsync in NFTManager. Reason:";

            try
            {
                OASISResult<IOASISNFTProvider> nftProviderResult = GetNFTProvider(request.OnChainProvider, errorMessage);

                if (nftProviderResult != null && nftProviderResult.Result != null && !nftProviderResult.IsError)
                {
                    request.MemoText = $"{Enum.GetName(typeof(ProviderType), request.OnChainProvider)} NFT minted on The OASIS with title {request.Title} by AvatarId {request.MintedByAvatarId} for price {request.Price}. {request.MemoText}";
                    result = await nftProviderResult.Result.MintNFTAsync(request);

                    if (result != null && !result.IsError)
                    {
                        //IOASISProvider offChainProvider = ProviderManager.GetProvider(request.OffChainProvider);

                        //if (offChainProvider != null)
                        //{
                        //    if (!offChainProvider.IsProviderActivated)
                        //    {
                        //        OASISResult<bool> activateProviderResult = await offChainProvider.ActivateProviderAsync();

                        //        if (activateProviderResult.IsError)
                        //            ErrorHandling.HandleError(ref result, $"{errorMessage} Error occured activating OffChainProvider. Reason: {activateProviderResult.Message}");
                        //    }

                        //    if (!result.IsError)
                        //    {
                        //        IOASISStorageProvider storageProvider = offChainProvider as IOASISStorageProvider;

                        //        if (storageProvider != null)
                        //        {
                                    result.Result.OASISNFT = new OASISNFT()
                                    {
                                        Id = Guid.NewGuid(),
                                        Hash = result.Result.TransactionResult,
                                        MintedByAddress = request.MintWalletAddress,
                                        MintedByAvatarId = request.MintedByAvatarId,
                                        Price = request.Price,
                                        Discount = request.Discount,
                                        Thumbnail = request.Thumbnail,
                                        ThumbnailUrl = request.ThumbnailUrl,
                                        OnChainProvider = request.OnChainProvider,
                                        OffChainProvider = request.OffChainProvider,
                                        //OffChainProviderHolonId = Guid.NewGuid(),
                                        //Token= request.Token
                                    };

                                    //HolonManager holonManager = new HolonManager(storageProvider);

                                    Holon holonNFT = new Holon(HolonType.NFT);
                                    holonNFT.Id = result.Result.OASISNFT.Id;
                                    //holonNFT.ParentHolonId = AvatarManager.LoggedInAvatar.Id; //This is now done automatically in HolonManger if the ParentHolonId is left empty.
                                    //holonNFT.Id = result.Result.OASISNFT.OffChainProviderHolonId;
                                    holonNFT.CustomKey = result.Result.TransactionResult;
                                    holonNFT.Name = $"{Enum.GetName(typeof(ProviderType), request.OnChainProvider)} NFT Minted On The OASIS with title {request.Title}";
                                    holonNFT.Description = request.MemoText;
                                    holonNFT.MetaData["NFT.OASISNFT"] = JsonSerializer.Serialize(result.Result.OASISNFT);
                                    holonNFT.MetaData["NFT.Hash"] = result.Result.TransactionResult;
                                    holonNFT.MetaData["NFT.Id"] = result.Result.OASISNFT.Id;
                                    holonNFT.MetaData["NFT.MintedByAvatarId"] = request.MintedByAvatarId.ToString();
                                    holonNFT.MetaData["NFT.MintWalletAddress"] = request.MintWalletAddress;
                                    holonNFT.MetaData["NFT.MemoText"] = request.MemoText;
                                    holonNFT.MetaData["NFT.Title"] = request.Title;
                                    holonNFT.MetaData["NFT.Description"] = request.Description;
                                    holonNFT.MetaData["NFT.Price"] = request.Price.ToString();
                                    holonNFT.MetaData["NFT.NumberToMint"] = request.NumberToMint.ToString();
                                    holonNFT.MetaData["NFT.OnChainProvider"] = Enum.GetName(typeof(ProviderType), request.OnChainProvider);
                                    holonNFT.MetaData["NFT.OffChainProvider"] = Enum.GetName(typeof(ProviderType), request.OffChainProvider);
                                    holonNFT.MetaData["NFT.Thumbnail"] = request.Thumbnail;
                                    holonNFT.MetaData["NFT.ThumbnailUrl"] = request.ThumbnailUrl;

                                    OASISResult<IHolon> saveHolonResult = await Data.SaveHolonAsync(holonNFT, true, true, 0, true, request.OffChainProvider);

                                    if (saveHolonResult != null && (saveHolonResult.IsError || saveHolonResult.Result != null) || saveHolonResult == null)
                                        ErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving metadata holon to the OffChainProvider {Enum.GetName(typeof(ProviderType), request.OffChainProvider)}. Reason: {saveHolonResult.Message}");
                                //}
                        //        else
                        //            ErrorHandling.HandleError(ref result, $"{errorMessage} The {Enum.GetName(typeof(ProviderType), request.OffChainProvider)} OffChainProvider is not a valid IOASISStorageProvider.");
                        //    }
                        //}
                        //else
                        //    ErrorHandling.HandleError(ref result, $"{errorMessage} The {Enum.GetName(typeof(ProviderType), request.OffChainProvider)} OffChainProvider was not found.");
                    }
                }
                else
                {
                    result.Message = nftProviderResult.Message;
                    result.IsError = true;
                }
            }
            catch (Exception e)
            {
                ErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public OASISResult<NFTTransactionRespone> MintNft(IMintNFTTransaction request)
        {
            OASISResult<NFTTransactionRespone> result = new OASISResult<NFTTransactionRespone>();
            string errorMessage = "Error occured in MintNft in NFTManager. Reason:";

            try
            {
                OASISResult<IOASISNFTProvider> nftProviderResult = GetNFTProvider(request.OnChainProvider, errorMessage);

                if (nftProviderResult != null && nftProviderResult.Result != null && !nftProviderResult.IsError)
                {
                    request.MemoText = $"{Enum.GetName(typeof(ProviderType), request.OnChainProvider)} NFT minted on The OASIS with title {request.Title} by AvatarId {request.MintedByAvatarId} for price {request.Price}. {request.MemoText}";
                    result = nftProviderResult.Result.MintNFT(request);

                    if (result != null && !result.IsError)
                    {
                        result.Result.OASISNFT = new OASISNFT()
                        {
                            Id = Guid.NewGuid(),
                            Hash = result.Result.TransactionResult,
                            MintedByAddress = request.MintWalletAddress,
                            MintedByAvatarId = request.MintedByAvatarId,
                            Price = request.Price,
                            Discount = request.Discount,
                            Thumbnail = request.Thumbnail,
                            ThumbnailUrl = request.ThumbnailUrl,
                            OnChainProvider = request.OnChainProvider,
                            OffChainProvider = request.OffChainProvider,
                           //OffChainProviderHolonId = Guid.NewGuid(),
                            //Token= request.Token
                        };

                        Holon holonNFT = new Holon(HolonType.NFT);
                        //holonNFT.Id = result.Result.OASISNFT.OffChainProviderHolonId;
                        holonNFT.Id = result.Result.OASISNFT.Id;
                        holonNFT.CustomKey = result.Result.TransactionResult;
                        holonNFT.Name = $"{Enum.GetName(typeof(ProviderType), request.OnChainProvider)} NFT Minted On The OASIS with title {request.Title}";
                        holonNFT.Description = request.MemoText;
                        holonNFT.MetaData["NFT.OASISNFT"] = JsonSerializer.Serialize(result.Result.OASISNFT);
                        holonNFT.MetaData["NFT.Hash"] = result.Result.TransactionResult;
                        holonNFT.MetaData["NFT.Id"] = result.Result.OASISNFT.Id;
                        holonNFT.MetaData["NFT.MintedByAvatarId"] = request.MintedByAvatarId.ToString();
                        holonNFT.MetaData["NFT.MintWalletAddress"] = request.MintWalletAddress;
                        holonNFT.MetaData["NFT.MemoText"] = request.MemoText;
                        holonNFT.MetaData["NFT.Title"] = request.Title;
                        holonNFT.MetaData["NFT.Description"] = request.Description;
                        holonNFT.MetaData["NFT.Price"] = request.Price.ToString();
                        holonNFT.MetaData["NFT.NumberToMint"] = request.NumberToMint.ToString();
                        holonNFT.MetaData["NFT.OnChainProvider"] = Enum.GetName(typeof(ProviderType), request.OnChainProvider);
                        holonNFT.MetaData["NFT.OffChainProvider"] = Enum.GetName(typeof(ProviderType), request.OffChainProvider);
                        holonNFT.MetaData["NFT.Thumbnail"] = request.Thumbnail;
                        holonNFT.MetaData["NFT.ThumbnailUrl"] = request.ThumbnailUrl;

                        OASISResult<IHolon> saveHolonResult = Data.SaveHolon(holonNFT, true, true, 0, true, request.OffChainProvider);

                        if (saveHolonResult != null && (saveHolonResult.IsError || saveHolonResult.Result != null) || saveHolonResult == null)
                            ErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving metadata holon to the OffChainProvider {Enum.GetName(typeof(ProviderType), request.OffChainProvider)}. Reason: {saveHolonResult.Message}");
                    }
                }
                else
                {
                    result.Message = nftProviderResult.Message;
                    result.IsError = true;
                }
            }
            catch (Exception e)
            {
                ErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        //public async Task<OASISResult<IOASISNFT>> LoadNftAsync(Guid id, NFTProviderType NFTProviderType)
        //{
        //    OASISResult<IOASISNFT> result = new OASISResult<IOASISNFT>();
        //    string errorMessage = "Error occured in LoadNftAsync in NFTManager. Reason:";

        //    try
        //    {
        //        OASISResult<IOASISNFTProvider> nftProviderResult = GetNFTProvider(NFTProviderType, errorMessage);

        //        if (nftProviderResult != null && nftProviderResult.Result != null && !nftProviderResult.IsError)
        //            result = await nftProviderResult.Result.LoadNFTAsync(id);
        //        else
        //        {
        //            result.Message = nftProviderResult.Message;
        //            result.IsError = true;
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        ErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
        //    }

        //    return result;
        //}

        public async Task<OASISResult<IOASISNFT>> LoadNftAsync(Guid id, ProviderType providerType)
        {
            OASISResult<IOASISNFT> result = new OASISResult<IOASISNFT>();
            string errorMessage = "Error occured in LoadNftAsync in NFTManager. Reason:";

            try
            {
                OASISResult<IHolon> oasisHolonResult = await Data.LoadHolonAsync(id, true, true, 0, true, 0, providerType);

                if (oasisHolonResult != null && !oasisHolonResult.IsError && oasisHolonResult.Result != null)
                    result.Result = (IOASISNFT)JsonSerializer.Deserialize(oasisHolonResult.Result.MetaData["OASISNFT"].ToString(), typeof(IOASISNFT));
                else
                    ErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading holon metadata. Reason: {oasisHolonResult.Message}");
            }
            catch (Exception e)
            {
                ErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public OASISResult<IOASISNFT> LoadNft(Guid id, ProviderType providerType)
        {
            OASISResult<IOASISNFT> result = new OASISResult<IOASISNFT>();
            string errorMessage = "Error occured in LoadNft in NFTManager. Reason:";

            try
            {
                OASISResult<IHolon> oasisHolonResult = Data.LoadHolon(id, true, true, 0, true, 0, providerType);

                if (oasisHolonResult != null && !oasisHolonResult.IsError && oasisHolonResult.Result != null)
                    result.Result = (IOASISNFT)JsonSerializer.Deserialize(oasisHolonResult.Result.MetaData["OASISNFT"].ToString(), typeof(IOASISNFT));
                else
                    ErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading holon metadata. Reason: {oasisHolonResult.Message}");
            }
            catch (Exception e)
            {
                ErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IOASISNFT>> LoadNftAsync(string onChainNftHash, ProviderType providerType)
        {
            OASISResult<IOASISNFT> result = new OASISResult<IOASISNFT>();
            string errorMessage = "Error occured in LoadNftAsync in NFTManager. Reason:";

            try
            {
                OASISResult<IHolon> oasisHolonResult = await Data.LoadHolonByCustomKeyAsync(onChainNftHash, true, true, 0, true, 0, providerType);

                if (oasisHolonResult != null && !oasisHolonResult.IsError && oasisHolonResult.Result != null)
                    result.Result = (IOASISNFT)JsonSerializer.Deserialize(oasisHolonResult.Result.MetaData["OASISNFT"].ToString(), typeof(IOASISNFT));
                else
                    ErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading holon metadata. Reason: {oasisHolonResult.Message}");


                //TODO: It may be more efficient and faster to add a custom/metadata field to IHolonBase that can used to Load holons by? Just means having to add additional LoadHolon methods...
                //OASISResult<ISearchResults> searchResult = await SearchManager.Instance.SearchAsync(new SearchParams()
                //{
                //    SearchGroups = new List<ISearchGroupBase>() 
                //    { 
                //        new SearchTextGroup() 
                //        {
                //            SearchQuery = hash, 
                //            SearchHolons = true,
                //            HolonSearchParams = new SearchHolonParams()
                //            {
                //                 MetaData = true
                //            }
                //        },
                //        new SearchTextGroup()
                //        {
                //            PreviousSearchGroupOperator = SearchParamGroupOperator.And, //This wll currently not work in MongoDBOASIS Provider because it currently only supports OR and not AND...
                //            SearchQuery = "NFT",
                //            SearchHolons = true,
                //            HolonSearchParams = new SearchHolonParams()
                //            {
                //                Name = true
                //            }
                //        }
                //    }
                //});

                //if (searchResult != null && !searchResult.IsError && searchResult.Result != null && searchResult.Result.SearchResultHolons.Count > 0)
                //    result.Result = (IOASISNFT)JsonSerializer.Deserialize(searchResult.Result.SearchResultHolons[0].MetaData["OASISNFT"].ToString(), typeof(IOASISNFT));
                //else
                //    ErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading/searching for the holon metadata. Reason: {searchResult.Message}");
            }
            catch (Exception e)
            {
                ErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public OASISResult<IOASISNFT> LoadNft(string onChainNftHash, ProviderType providerType)
        {
            OASISResult<IOASISNFT> result = new OASISResult<IOASISNFT>();
            string errorMessage = "Error occured in LoadNft in NFTManager. Reason:";

            try
            {
                OASISResult<IHolon> oasisHolonResult = Data.LoadHolonByCustomKey(onChainNftHash, true, true, 0, true, 0, providerType);

                if (oasisHolonResult != null && !oasisHolonResult.IsError && oasisHolonResult.Result != null)
                    result.Result = (IOASISNFT)JsonSerializer.Deserialize(oasisHolonResult.Result.MetaData["OASISNFT"].ToString(), typeof(IOASISNFT));
                else
                    ErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading holon metadata. Reason: {oasisHolonResult.Message}");
            }
            catch (Exception e)
            {
                ErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<List<IOASISNFT>>> LoadAllNFTsForAvatarAsync(Guid avatarId, ProviderType providerType)
        {
            OASISResult<List<IOASISNFT>> result = new OASISResult<List<IOASISNFT>>();
            string errorMessage = "Error occured in LoadAllNFTsForAvatarAsync in NFTManager. Reason:";

            try
            {
                //TODO: Want to add new LoadHolonsForAvatar methods to HolonManager eventually, which we would use here instead. It would load all Holons that had CreatedByAvatarId = avatarId. But for now we can just set the ParentId on the holons to the AvatarId.
                OASISResult<IEnumerable<IHolon>> holonsResult = await Data.LoadHolonsForParentAsync(avatarId, HolonType.NFT, true, true, 0, true, 0, providerType);

                if (holonsResult != null && !holonsResult.IsError && holonsResult.Result != null)
                {
                    foreach (IHolon holon in holonsResult.Result)
                        result.Result.Add((IOASISNFT)JsonSerializer.Deserialize(holon.MetaData["OASISNFT"].ToString(), typeof(IOASISNFT)));
                }
                else
                    ErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading holon metadata. Reason: {holonsResult.Message}");
            }
            catch (Exception e)
            {
                ErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public OASISResult<List<IOASISNFT>> LoadAllNFTsForAvatar(Guid avatarId, ProviderType providerType)
        {
            OASISResult<List<IOASISNFT>> result = new OASISResult<List<IOASISNFT>>();
            string errorMessage = "Error occured in LoadAllNFTsForAvatar in NFTManager. Reason:";

            try
            {
                OASISResult<IEnumerable<IHolon>> holonsResult = Data.LoadHolonsForParent(avatarId, HolonType.NFT, true, true, 0, true, 0, providerType);

                if (holonsResult != null && !holonsResult.IsError && holonsResult.Result != null)
                {
                    foreach (IHolon holon in holonsResult.Result)
                        result.Result.Add((IOASISNFT)JsonSerializer.Deserialize(holon.MetaData["OASISNFT"].ToString(), typeof(IOASISNFT)));
                }
                else
                    ErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading holon metadata. Reason: {holonsResult.Message}");
            }
            catch (Exception e)
            {
                ErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<List<IOASISNFT>>> LoadAllNFTsForMintAddressAsync(string mintWalletAddress, ProviderType providerType)
        {
            OASISResult<List<IOASISNFT>> result = new OASISResult<List<IOASISNFT>>();
            string errorMessage = "Error occured in LoadAllNFTsForMintAddressAsync in NFTManager. Reason:";

            try
            {
                //OASISResult<IEnumerable<IHolon>> holonsResult = await Data.LoadHolonsForParentAsync(avatarId, HolonType.NFT, true, true, 0, true, 0, providerType);
                
                //TODO: We could possibly add a CustomKey2 property to Holons to load by but not sure how far we go with this? I think eventually we may have 3 custom keys you can load by but for now Search will do... ;-)
                //TODO: Alternatively we may be able to load the nfts directly from the provider blockchains using the mintWalletAddress? Need to look into this...
                OASISResult<ISearchResults> searchResult = await SearchManager.Instance.SearchAsync(new SearchParams()
                {
                    SearchGroups = new List<ISearchGroupBase>()
                    {
                        new SearchTextGroup()
                        {
                            SearchQuery = mintWalletAddress,
                            SearchHolons = true,
                            HolonSearchParams = new SearchHolonParams()
                            {
                                 MetaData = true,
                                 MetaDataKey = "NFT.MintWalletAddress"
                            }
                        },
                        new SearchTextGroup()
                        {
                            PreviousSearchGroupOperator = SearchParamGroupOperator.And, //This wll currently not work in MongoDBOASIS Provider because it currently only supports OR and not AND...
                            SearchQuery = "NFT",
                            SearchHolons = true,
                            HolonSearchParams = new SearchHolonParams()
                            {
                                Name = true
                            }
                        }
                    }
                });

                if (searchResult != null && !searchResult.IsError && searchResult.Result != null)
                {
                    foreach (IHolon holon in searchResult.Result.SearchResultHolons)
                        result.Result.Add((IOASISNFT)JsonSerializer.Deserialize(holon.MetaData["OASISNFT"].ToString(), typeof(IOASISNFT)));
                }
                else
                    ErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading/searching the holon metadata. Reason: {searchResult.Message}");

                //OASISResult<IOASISNFTProvider> nftProviderResult = GetNFTProvider(NFTProviderType, errorMessage);

                //if (nftProviderResult != null && nftProviderResult.Result != null && !nftProviderResult.IsError)
                //    result = await nftProviderResult.Result.LoadAllNFTsForMintAddressAsync(mintWalletAddress);
                //else
                //{
                //    result.Message = nftProviderResult.Message;
                //    result.IsError = true;
                //}
            }
            catch (Exception e)
            {
                ErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public OASISResult<List<IOASISNFT>> LoadAllNFTsForMintAddress(string mintWalletAddress, NFTProviderType NFTProviderType)
        {
            OASISResult<List<IOASISNFT>> result = new OASISResult<List<IOASISNFT>>();
            string errorMessage = "Error occured in LoadAllNFTsForMintAddress in NFTManager. Reason:";

            try
            {
                OASISResult<IOASISNFTProvider> nftProviderResult = GetNFTProvider(NFTProviderType, errorMessage);

                if (nftProviderResult != null && nftProviderResult.Result != null && !nftProviderResult.IsError)
                    result = nftProviderResult.Result.LoadAllNFTsForMintAddress(mintWalletAddress);
                else
                {
                    result.Message = nftProviderResult.Message;
                    result.IsError = true;
                }
            }
            catch (Exception e)
            {
                ErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<List<IOASISGeoSpatialNFT>>> LoadAllGeoNFTsForAvatarAsync(Guid avatarId, NFTProviderType NFTProviderType)
        {
            OASISResult<List<IOASISGeoSpatialNFT>> result = new OASISResult<List<IOASISGeoSpatialNFT>>();
            string errorMessage = "Error occured in LoadAllGeoNFTsForAvatarAsync in NFTManager. Reason:";

            try
            {
                OASISResult<IOASISNFTProvider> nftProviderResult = GetNFTProvider(NFTProviderType, errorMessage);

                if (nftProviderResult != null && nftProviderResult.Result != null && !nftProviderResult.IsError)
                    result = await nftProviderResult.Result.LoadAllGeoNFTsForAvatarAsync(avatarId);
                else
                {
                    result.Message = nftProviderResult.Message;
                    result.IsError = true;
                }
            }
            catch (Exception e)
            {
                ErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public OASISResult<List<IOASISGeoSpatialNFT>> LoadAllGeoNFTsForAvatar(Guid avatarId, NFTProviderType NFTProviderType)
        {
            OASISResult<List<IOASISGeoSpatialNFT>> result = new OASISResult<List<IOASISGeoSpatialNFT>>();
            string errorMessage = "Error occured in LoadAllGeoNFTsForAvatar in NFTManager. Reason:";

            try
            {
                OASISResult<IOASISNFTProvider> nftProviderResult = GetNFTProvider(NFTProviderType, errorMessage);

                if (nftProviderResult != null && nftProviderResult.Result != null && !nftProviderResult.IsError)
                    result = nftProviderResult.Result.LoadAllGeoNFTsForAvatar(avatarId);
                else
                {
                    result.Message = nftProviderResult.Message;
                    result.IsError = true;
                }
            }
            catch (Exception e)
            {
                ErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<List<IOASISGeoSpatialNFT>>> LoadAllGeoNFTsForMintAddressAsync(string mintWalletAddress, NFTProviderType NFTProviderType)
        {
            OASISResult<List<IOASISGeoSpatialNFT>> result = new OASISResult<List<IOASISGeoSpatialNFT>>();
            string errorMessage = "Error occured in LoadAllGeoNFTsForMintAddressAsync in NFTManager. Reason:";

            try
            {
                OASISResult<IOASISNFTProvider> nftProviderResult = GetNFTProvider(NFTProviderType, errorMessage);

                if (nftProviderResult != null && nftProviderResult.Result != null && !nftProviderResult.IsError)
                    result = await nftProviderResult.Result.LoadAllGeoNFTsForMintAddressAsync(mintWalletAddress);
                else
                {
                    result.Message = nftProviderResult.Message;
                    result.IsError = true;
                }
            }
            catch (Exception e)
            {
                ErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public OASISResult<List<IOASISGeoSpatialNFT>> LoadAllGeoNFTsForMintAddress(string mintWalletAddress, NFTProviderType NFTProviderType)
        {
            OASISResult<List<IOASISGeoSpatialNFT>> result = new OASISResult<List<IOASISGeoSpatialNFT>>();
            string errorMessage = "Error occured in LoadAllGeoNFTsForMintAddress in NFTManager. Reason:";

            try
            {
                OASISResult<IOASISNFTProvider> nftProviderResult = GetNFTProvider(NFTProviderType, errorMessage);

                if (nftProviderResult != null && nftProviderResult.Result != null && !nftProviderResult.IsError)
                    result = nftProviderResult.Result.LoadAllGeoNFTsForMintAddress(mintWalletAddress);
                else
                {
                    result.Message = nftProviderResult.Message;
                    result.IsError = true;
                }
            }
            catch (Exception e)
            {
                ErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IOASISGeoSpatialNFT>> PlaceGeoNFTAsync(IPlaceGeoSpatialNFTRequest request)
        {
            OASISResult<IOASISGeoSpatialNFT> result = new OASISResult<IOASISGeoSpatialNFT>();
            string errorMessage = "Error occured in PlaceGeoNFTAsync in NFTManager. Reason:";

            try
            {
                OASISResult<IOASISNFTProvider> nftProviderResult = GetNFTProvider(request.ProviderType, errorMessage);

                if (nftProviderResult != null && nftProviderResult.Result != null && !nftProviderResult.IsError)
                    result = await nftProviderResult.Result.PlaceGeoNFTAsync(request);
                else
                {
                    result.Message = nftProviderResult.Message;
                    result.IsError = true;
                }
            }
            catch (Exception e)
            {
                ErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public OASISResult<IOASISGeoSpatialNFT> PlaceGeoNFT(IPlaceGeoSpatialNFTRequest request)
        {
            OASISResult<IOASISGeoSpatialNFT> result = new OASISResult<IOASISGeoSpatialNFT>();
            string errorMessage = "Error occured in PlaceGeoNFT in NFTManager. Reason:";

            try
            {
                OASISResult<IOASISNFTProvider> nftProviderResult = GetNFTProvider(request.ProviderType, errorMessage);

                if (nftProviderResult != null && nftProviderResult.Result != null && !nftProviderResult.IsError)
                    result = nftProviderResult.Result.PlaceGeoNFT(request);
                else
                {
                    result.Message = nftProviderResult.Message;
                    result.IsError = true;
                }
            }
            catch (Exception e)
            {
                ErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IOASISGeoSpatialNFT>> MintAndPlaceGeoNFTAsync(IMintAndPlaceGeoSpatialNFTRequest request)
        {
            OASISResult<IOASISGeoSpatialNFT> result = new OASISResult<IOASISGeoSpatialNFT>();
            string errorMessage = "Error occured in MintAndPlaceGeoNFTAsync in NFTManager. Reason:";

            try
            {
                OASISResult<IOASISNFTProvider> nftProviderResult = GetNFTProvider(request.OnChainProvider, errorMessage);

                if (nftProviderResult != null && nftProviderResult.Result != null && !nftProviderResult.IsError)
                    result = await nftProviderResult.Result.MintAndPlaceGeoNFTAsync(request);
                else
                {
                    result.Message = nftProviderResult.Message;
                    result.IsError = true;
                }
            }
            catch (Exception e)
            {
                ErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public OASISResult<IOASISGeoSpatialNFT> MintAndPlaceGeoNFT(IMintAndPlaceGeoSpatialNFTRequest request)
        {
            OASISResult<IOASISGeoSpatialNFT> result = new OASISResult<IOASISGeoSpatialNFT>();
            string errorMessage = "Error occured in MintAndPlaceGeoNFT in NFTManager. Reason:";

            try
            {
                OASISResult<IOASISNFTProvider> nftProviderResult = GetNFTProvider(request.OnChainProvider, errorMessage);

                if (nftProviderResult != null && nftProviderResult.Result != null && !nftProviderResult.IsError)
                    result = nftProviderResult.Result.MintAndPlaceGeoNFT(request);
                else
                {
                    result.Message = nftProviderResult.Message;
                    result.IsError = true;
                }
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

        //public IOASISNFTProvider GetNFTProvider<T>(NFTProviderType NFTProviderType, ref OASISResult<T> result, string errorMessage)
        //{
        //    return GetNFTProvider(GetProviderTypeFromNFTProviderType(NFTProviderType), ref result, errorMessage);
        //}

        public OASISResult<IOASISNFTProvider> GetNFTProvider(NFTProviderType NFTProviderType, string errorMessage = "")
        {
            return GetNFTProvider(GetProviderTypeFromNFTProviderType(NFTProviderType), errorMessage);
        }

        public OASISResult<IOASISNFTProvider> GetNFTProvider(ProviderType providerType, string errorMessage = "")
        {
            OASISResult<IOASISNFTProvider> result = new OASISResult<IOASISNFTProvider>();
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
                result.Result = OASISProvider as IOASISNFTProvider;

                if (result.Result == null)
                    ErrorHandling.HandleError(ref result, $"{errorMessage} The {Enum.GetName(typeof(ProviderType), providerType)} provider is not a valid OASISNFTProvider.");
            }

            return result;
        }

        //public IOASISNFTProvider GetNFTProvider<T>(ProviderType providerType, ref OASISResult<T> result, string errorMessage)
        //{
        //    OASISResult<IOASISNFTProvider> getNFTProviderResult = GetNFTProvider(providerType);

        //    if (getNFTProviderResult == null || getNFTProviderResult != null && getNFTProviderResult.IsError ) 
        //    { 
                
        //    }


        //    //IOASISNFTProvider nftProvider = null;
        //    //IOASISProvider OASISProvider = ProviderManager.GetProvider(providerType);

        //    //if (OASISProvider != null)
        //    //{
        //    //    if (!OASISProvider.IsProviderActivated)
        //    //    {
        //    //        OASISResult<bool> activateProviderResult = OASISProvider.ActivateProvider();

        //    //        if (activateProviderResult.IsError)
        //    //            ErrorHandling.HandleError(ref result, $"{errorMessage} Error occured activating provider. Reason: {activateProviderResult.Message}");
        //    //    }
        //    //}
        //    //else
        //    //    ErrorHandling.HandleError(ref result, $"{errorMessage} The {Enum.GetName(typeof(ProviderType), providerType)} provider was not found.");

        //    //if (!result.IsError)
        //    //{
        //    //    nftProvider = OASISProvider as IOASISNFTProvider;

        //    //    if (nftProvider == null)
        //    //        ErrorHandling.HandleError(ref result, $"{errorMessage} The {Enum.GetName(typeof(ProviderType), providerType)} provider is not a valid OASISNFTProvider.");
        //    //}

        //    //return nftProvider;
        //}

        //TODO: Lots more coming soon! ;-)
    }
}
