using Ipfs;
using Newtonsoft.Json;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Request;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers.Base;
using NextGenSoftware.OASIS.API.Providers.IPFSOASIS;
using NextGenSoftware.OASIS.API.Providers.PinataOASIS;
using NextGenSoftware.Logging;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    public partial class NFTManager
    {
        private string FormatSuccessMessage(IMintWeb4NFTRequest request, OASISResult<IWeb4NFT> response, EnumValue<ProviderType> metaDataProviderType, IList<IWeb3NFT> newlyMintedWeb3Nfts, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText, int colWidth = FORMAT_SUCCESS_MESSAGE_COL_WIDTH)
        {
            string lineBreak = "\n";
            string message = "";
            //string summary = $"Successfully minted the OASIS NFT containing {response.SavedCount} Web NFT(s) & {response.ErrorCount} errored!";
            string summary = response.SavedCount > 0 ? $"Successfully minted the Web4 NFT containing {newlyMintedWeb3Nfts.Count} Web3 NFT(s)" : "No Web4 NFT's were minted!";

            if (responseFormatType == ResponseFormatType.SimpleText)
            {
                lineBreak = "|";
                message = string.Concat(summary, lineBreak);

                foreach (IWeb3NFT web3NFT in newlyMintedWeb3Nfts)
                {
                    string sendNFTMessage = GenerateSendMessage(response.Result, request, web3NFT.SendNFTTransactionHash, "", 2);
                    bool web3MintOk = !string.IsNullOrEmpty(web3NFT.MintTransactionHash) && !web3NFT.MintTransactionHash.StartsWith("Error", StringComparison.OrdinalIgnoreCase);
                    string web3Status = web3MintOk ? "Successfully minted" : "Failed to mint";
                    message = string.Concat(message, $"{web3Status} the Web3 NFT on the {web3NFT.OnChainProvider.Name} provider with hash {web3NFT.MintTransactionHash} and title '{web3NFT.Title}' by AvatarId {request.MintedByAvatarId} using OASIS Minting Account {web3NFT.OASISMintWalletAddress} for price {web3NFT.Price}. NFT Address: {web3NFT.NFTTokenAddress}. The OASIS metadata is stored on the {web3NFT.OffChainProvider.Name} provider with the id {web3NFT.Id} and JSON URL {web3NFT.JSONMetaDataURL}. JSON MetaData URI Holon Id: ", response.Result.JSONMetaDataURLHolonId, ", Image URL: ", web3NFT.ImageUrl, ", Mint Date: ", web3NFT.MintedOn, ". ", sendNFTMessage, lineBreak);
                }

                return message;
            }

            if (responseFormatType == ResponseFormatType.HTML)
                lineBreak = "<br>";

            message = string.Concat(message, summary, lineBreak, lineBreak);

            foreach (IWeb3NFT web3NFT in newlyMintedWeb3Nfts)
                message = string.Concat(message, GenerateWeb3NFTSummary(web3NFT, request, lineBreak, colWidth));

            if (response.IsWarning || response.IsError)
                message = string.Concat(message, " Errors Occured:\n", OASISResultHelper.BuildInnerMessageError(response.InnerMessages));

            return message;
        }

        private string FormatSuccessMessage(IMintWeb4NFTRequest request, OASISResult<IWeb4GeoSpatialNFT> response, IList<IWeb3NFT> newlyMintedWeb3Nfts, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText, int colWidth = 40)
        {
            string lineBreak = "\n";
            string message = "";
            //string summary = $"Successfully minted & placed the OASIS Geo-NFT containing {response.SavedCount} Web3 NFT(s) & {response.ErrorCount} errored!";
            //string summary = $"Successfully minted & placed the OASIS Geo-NFT containing {response.SavedCount} Web3 NFT(s)";
            string summary = response.SavedCount > 0 ? $"Successfully minted the Web4 Geo-FT containing {newlyMintedWeb3Nfts.Count} Web3 NFT(s)" : "No Web4 Geo-NFT's were minted!";
            //string summary = mintWeb4NFResult.SavedCount > 0 ? $"Successfully minted the OASIS Geo-FT containing {mintWeb4NFResult.SavedCount} Web NFT(s)" : "No OASIS Geo-NFT's were minted!";

            if (responseFormatType == ResponseFormatType.SimpleText)
            {
                lineBreak = "|";
                message = string.Concat(summary, lineBreak);

                foreach (IWeb3NFT web3NFT in newlyMintedWeb3Nfts)
                {
                    string sendNFTMessage = GenerateSendMessage(response.Result, request, web3NFT.SendNFTTransactionHash, "", 2);
                    bool web3MintOk = !string.IsNullOrEmpty(web3NFT.MintTransactionHash) && !web3NFT.MintTransactionHash.StartsWith("Error", StringComparison.OrdinalIgnoreCase);
                    string web3Status = web3MintOk ? "Successfully minted" : "Failed to mint";
                    message = string.Concat(message, $"{web3Status} the Web3 NFT on the {web3NFT.OnChainProvider.Name} provider with hash {web3NFT.MintTransactionHash} and title '{web3NFT.Title}' by AvatarId {request.MintedByAvatarId} using OASIS Minting Account {web3NFT.OASISMintWalletAddress} for price {web3NFT.Price}. NFT Address: {web3NFT.NFTTokenAddress}. The OASIS metadata is stored on the {web3NFT.OffChainProvider.Name} provider with the id {web3NFT.Id} and JSON URL {web3NFT.JSONMetaDataURL}. JSON MetaData URI Holon Id: ", response.Result.JSONMetaDataURLHolonId, ", Image URL: ", web3NFT.ImageUrl, ", Mint Date: ", web3NFT.MintedOn, ". The GeoNFT meta data is stored on the GeoNFTMetaDataProvider ", response.Result.GeoNFTMetaDataProvider.Name, " with id ", response.Result.Id, " and was placed by the avatar with id ", response.Result.PlacedByAvatarId, sendNFTMessage, lineBreak);
                }

                //return string.Concat(mintWeb4NFResult.Message, lineBreak, message);
                return message;
            }

            if (responseFormatType == ResponseFormatType.HTML)
                lineBreak = "<br>";

            //message = string.Concat(mintWeb4NFResult.Message, lineBreak, lineBreak, summary, lineBreak, lineBreak);
            message = string.Concat(summary, lineBreak, lineBreak);

            foreach (IWeb3NFT web3NFT in newlyMintedWeb3Nfts)
                message = string.Concat(message, GenerateWeb3NFTSummary(web3NFT, request, lineBreak, colWidth));

            message = string.Concat(message, GenerateWeb4GeoNFTSummary(response.Result, lineBreak, colWidth));

            if (response.IsWarning || response.IsError)
                message = string.Concat(message, " Errors Occured:\n", OASISResultHelper.BuildInnerMessageError(response.InnerMessages));

            return message;
        }

        private string FormatSuccessMessage(OASISResult<IWeb4GeoSpatialNFT> response, IList<IWeb3NFT> newlyMintedWeb3Nfts, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText, int colWidth = 40)
        {
            string lineBreak = "\n";
            string message = "";
            //string summary = $"Successfully created & placed OASIS Geo-NFT containing {response.SavedCount} Web3 NFT(s) & {response.ErrorCount} errored!";
            //string summary = $"Successfully created & placed OASIS Geo-NFT containing {response.SavedCount} Web3 NFT(s)";
            string summary = response.SavedCount > 0 ? $"Successfully created & placed Web4 Geo-NFT containing {newlyMintedWeb3Nfts.Count} Web3 NFT(s)" : "No Web4 Geo-NFT's were placed!";

            if (responseFormatType == ResponseFormatType.SimpleText)
            {
                lineBreak = "|";
                message = string.Concat(summary, lineBreak);

                foreach (IWeb3NFT web3NFT in newlyMintedWeb3Nfts)
                    message = string.Concat(message, $"{summary} The meta data is stored on the GeoNFTMetaDataProvider {response.Result.GeoNFTMetaDataProvider.Name} with id {response.Result.Id} and was placed by the avatar with id {response.Result.PlacedByAvatarId}. The NFT was originally minted on the {web3NFT.OnChainProvider.Name} onchain provider with hash {web3NFT.MintTransactionHash} and title '{web3NFT.Title}' by the avatar with id {web3NFT.MintedByAvatarId} for the price of {web3NFT.Price} using OASIS Minting Account {web3NFT.OASISMintWalletAddress}. NFT Address: {web3NFT.NFTTokenAddress}. The OASIS metadata for the original NFT is stored on the {web3NFT.OffChainProvider.Name} offchain provider with the id {response.Result.OriginalWeb4OASISNFTId} and JSON URL {web3NFT.JSONMetaDataURL}. JSON MetaData URL Holon Id: ", web3NFT.JSONMetaDataURLHolonId, ", Image URL: {web3NFT.ImageUrl}, Mint Date: {web3NFT.MintedOn}.");

                return message;
            }

            if (responseFormatType == ResponseFormatType.HTML)
                lineBreak = "<br>";

            message = string.Concat(message, summary, lineBreak, lineBreak);
            message = string.Concat(message, $"ORIGINAL NFT INFO:{lineBreak}");

            foreach (IWeb3NFT web3NFT in newlyMintedWeb3Nfts)
                message = string.Concat(message, GenerateWeb3NFTSummary(web3NFT, null, lineBreak, colWidth));

            message = string.Concat(message, lineBreak);
            message = string.Concat(message, GenerateWeb4GeoNFTSummary(response.Result, lineBreak, colWidth));

            if (response.IsWarning || response.IsError)
                message = string.Concat(message, " Errors Occured:\n", OASISResultHelper.BuildInnerMessageError(response.InnerMessages));

            return message;
        }

        private string FormatSuccessMessage(IImportWeb3NFTRequest request, OASISResult<IWeb4NFT> response, IList<IWeb3NFT> importedWeb3NFTs, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText, int colWidth = FORMAT_SUCCESS_MESSAGE_COL_WIDTH)
        {
            string lineBreak = "\n";
            string message = "";
            //string summary = $"Successfully imported {response.SavedCount} Web3 NFT(s) & {response.ErrorCount} errored!";
            string summary = response.SavedCount > 0 ? $"Successfully imported {importedWeb3NFTs.Count} Web3 NFT(s)" : "No Web3 NFT's were imported!";
            //string summary = $"Successfully imported {response.SavedCount} Web3 NFT(s)";

            if (responseFormatType == ResponseFormatType.SimpleText)
            {
                lineBreak = "|";
                message = string.Concat(summary, lineBreak);

                foreach (IWeb3NFT web3NFT in importedWeb3NFTs)
                    message = string.Concat(message, $"Web3 NFT OnChain Provider: {web3NFT.OnChainProvider.Name}, NFTTokenAddress {web3NFT.NFTTokenAddress}, title '{web3NFT.Title}', Imported By Avatar Id: {web3NFT.ImportedByAvatarId}. NFT minted using wallet address: {web3NFT.NFTMintedUsingWalletAddress}. Price: {web3NFT.Price}. The OASIS metadata is stored on the {web3NFT.OnChainProvider.Name} provider with the id {web3NFT.Id} and JSON URL {web3NFT.JSONMetaDataURL}. JSON MetaData URI Holon Id: ", response.Result.JSONMetaDataURLHolonId, ", Image URL: ", web3NFT.ImageUrl, ", Imported Date: ", web3NFT.ImportedOn, lineBreak);
            }

            if (responseFormatType == ResponseFormatType.HTML)
                lineBreak = "<br>";

            message = string.Concat(message, summary, lineBreak, lineBreak);

            foreach (IWeb3NFT web3NFT in importedWeb3NFTs)
                message = string.Concat(message, GenerateWeb3NFTSummary(web3NFT, lineBreak, colWidth));

            if (response.IsWarning || response.IsError)
                message = string.Concat(message, " Errors Occured:\n", OASISResultHelper.BuildInnerMessageError(response.InnerMessages));

            return message;
        }

        private string FormatSuccessMessage(OASISResult<IWeb4NFT> response, Guid importedByByAvatarId, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText, int colWidth = FORMAT_SUCCESS_MESSAGE_COL_WIDTH)
        {
            string lineBreak = "\n";
            string message = "";
            //string summary = $"Successfully imported the OASIS NFT containing {response.SavedCount} Web3 NFT(s) & {response.ErrorCount} errored!";
            //string summary = $"Successfully imported the OASIS NFT containing {response.SavedCount} Web3 NFT(s)";
            string summary = response.SavedCount > 0 ? $"Successfully imported the Web4 NFT containing {response.Result.Web3NFTs.Count} Web3 NFT(s)" : "No Web4 NFT's were imported!";

            if (responseFormatType == ResponseFormatType.SimpleText)
            {
                string JSONMetaDataURIHolonId = "";
                lineBreak = "|";

                message = string.Concat(summary, lineBreak);

                foreach (IWeb3NFT web3NFT in response.Result.Web3NFTs)
                    message = string.Concat(message, $"Web3 NFT OnChain Provider: {web3NFT.OnChainProvider.Name}, NFTTokenAddress {web3NFT.NFTTokenAddress}, title '{web3NFT.Title}', Imported By Avatar Id: {web3NFT.ImportedByAvatarId}. NFT minted using wallet address: {web3NFT.NFTMintedUsingWalletAddress}. Price: {web3NFT.Price}. The OASIS metadata is stored on the {web3NFT.OnChainProvider.Name} provider with the id {web3NFT.Id} and JSON URL {web3NFT.JSONMetaDataURL}. JSON MetaData URI Holon Id: ", response.Result.JSONMetaDataURLHolonId, ", Image URL: ", web3NFT.ImageUrl, ", Imported Date: ", web3NFT.ImportedOn, lineBreak);

                return message;
            }

            if (responseFormatType == ResponseFormatType.HTML)
                lineBreak = "<br>";

            message = string.Concat(message, summary, lineBreak, lineBreak);

            foreach (IWeb3NFT web3NFT in response.Result.Web3NFTs)
                message = string.Concat(message, GenerateWeb3NFTSummary(web3NFT, lineBreak, colWidth));

            if (response.IsWarning || response.IsError)
                message = string.Concat(message, " Errors Occured:\n", OASISResultHelper.BuildInnerMessageError(response.InnerMessages));

            return message;
        }

        private string FormatSuccessMessage(OASISResult<IWeb4GeoSpatialNFT> response, Guid importedByByAvatarId, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText, int colWidth = FORMAT_SUCCESS_MESSAGE_COL_WIDTH)
        {
            string lineBreak = "\n";
            string message = "";
            //string summary = $"Successfully imported the GeoNFT containing {response.SavedCount} Web3 NFT(s) & {response.ErrorCount} errored!";
            //string summary = $"Successfully imported the GeoNFT containing {response.SavedCount} Web3 NFT(s)";
            string summary = response.SavedCount > 0 ? $"Successfully imported the Web4 GeoNFT containing {response.Result.Web3NFTs.Count} Web3 NFT(s)" : "No Web4 GeoNFT's were imported!";

            if (responseFormatType == ResponseFormatType.SimpleText)
            {
                lineBreak = "|";
                message = string.Concat(summary, lineBreak);

                foreach (IWeb3NFT web3NFT in response.Result.Web3NFTs)
                    message = string.Concat(message, $"Web3 NFT OnChain Provider: {web3NFT.OnChainProvider.Name}, NFTTokenAddress {web3NFT.NFTTokenAddress}, title '{web3NFT.Title}', Imported By Avatar Id: {web3NFT.ImportedByAvatarId}. NFT minted using wallet address: {web3NFT.NFTMintedUsingWalletAddress}. Price: {web3NFT.Price}. The OASIS metadata is stored on the {web3NFT.OnChainProvider.Name} provider with the id {web3NFT.Id} and JSON URL {web3NFT.JSONMetaDataURL}. JSON MetaData URI Holon Id: ", response.Result.JSONMetaDataURLHolonId, ", Image URL: ", web3NFT.ImageUrl, ", Imported Date: ", web3NFT.ImportedOn, lineBreak);

                return message;
            }

            if (responseFormatType == ResponseFormatType.HTML)
                lineBreak = "<br>";


            message = string.Concat(message, summary, lineBreak, lineBreak);

            foreach (IWeb3NFT web3NFT in response.Result.Web3NFTs)
                message = string.Concat(message, GenerateWeb3NFTSummary(web3NFT, lineBreak, colWidth));

            if (response.IsWarning || response.IsError)
                message = string.Concat(message, " Errors Occured:\n", OASISResultHelper.BuildInnerMessageError(response.InnerMessages));

            return message;
        }

        private string FormatSuccessMessage(ISendWeb4NFTRequest request, OASISResult<IWeb3NFTTransactionResponse> response, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText, int colWidth = FORMAT_SUCCESS_MESSAGE_COL_WIDTH)
        {
            string lineBreak = "\n";

            if (responseFormatType == ResponseFormatType.SimpleText)
                return $"Successfully sent the NFT from wallet {request.FromWalletAddress} to wallet {request.ToWalletAddress}. Transaction Hash: {response.Result.TransactionResult}, From Provider: {request.FromProvider.Name}, To Provider: {request.ToProvider.Name}, Amount: {request.Amount}, Memo: {request.MemoText}.";

            if (responseFormatType == ResponseFormatType.HTML)
                lineBreak = "<br>";

            string message = "";
            message = string.Concat(message, $" NFT Successfully Sent!{lineBreak}");
            message = string.Concat(message, lineBreak);
            message = string.Concat(message, " From Wallet:".PadRight(colWidth), request.FromWalletAddress, lineBreak);
            message = string.Concat(message, " To Wallet:".PadRight(colWidth), request.ToWalletAddress, lineBreak);
            message = string.Concat(message, " From Provider:".PadRight(colWidth), request.FromProvider.Name, lineBreak);
            message = string.Concat(message, " To Provider:".PadRight(colWidth), request.ToProvider.Name, lineBreak);
            message = string.Concat(message, " Amount:".PadRight(colWidth), request.Amount, lineBreak);
            message = string.Concat(message, " Memo:".PadRight(colWidth), request.MemoText, lineBreak);
            message = string.Concat(message, " Transaction Hash:".PadRight(colWidth), response.Result.TransactionResult, lineBreak);

            return message;
        }


        private string GenerateWeb3NFTSummary(IWeb3NFT web3NFT, IMintWeb4NFTRequest request, string lineBreak, int colWidth)
        {
            string message = GenerateWeb3NFTSummary(web3NFT, lineBreak, colWidth);

            if (request != null)
                message = string.Concat(message, " Number To Mint:".PadRight(colWidth), request.NumberToMint, lineBreak);

            message = string.Concat(message, GenerateSendMessage(web3NFT, request, web3NFT.SendNFTTransactionHash, lineBreak, colWidth), lineBreak);
            return message;
        }

        private string GenerateWeb3NFTSummary(IWeb3NFT web3NFT, string lineBreak, int colWidth)
        {
            string message = "";
            message = string.Concat(message, " OASIS NFT Id:".PadRight(colWidth), web3NFT.Id, lineBreak);
            message = string.Concat(message, " Onchain Provider:".PadRight(colWidth), web3NFT.OnChainProvider.Name, lineBreak);
            message = string.Concat(message, " Offchain Provider:".PadRight(colWidth), web3NFT.OffChainProvider.Name, lineBreak);
            message = string.Concat(message, " Mint Transaction Hash:".PadRight(colWidth), web3NFT.MintTransactionHash, lineBreak);
            message = string.Concat(message, " Title:".PadRight(colWidth), web3NFT.Title, lineBreak);
            message = string.Concat(message, " Description:".PadRight(colWidth), web3NFT.Description, lineBreak);
            message = string.Concat(message, " Price:".PadRight(colWidth), web3NFT.Price, lineBreak);
            message = string.Concat(message, " Symbol:".PadRight(colWidth), web3NFT.Symbol, lineBreak);
            message = string.Concat(message, " NFT Standard Type:".PadRight(colWidth), web3NFT.NFTStandardType.Name, lineBreak);
            message = string.Concat(message, " Minted By Avatar Id:".PadRight(colWidth), web3NFT.MintedByAvatarId, lineBreak);
            message = string.Concat(message, " Minted Date:".PadRight(colWidth), web3NFT.MintedOn, lineBreak);
            message = string.Concat(message, " OASIS Minting Account:".PadRight(colWidth), web3NFT.OASISMintWalletAddress, lineBreak);
            message = string.Concat(message, " NFT Address:".PadRight(colWidth), web3NFT.NFTTokenAddress, lineBreak);
            message = string.Concat(message, " Store NFT MetaData OnChain:".PadRight(colWidth), web3NFT.StoreNFTMetaDataOnChain, lineBreak);
            message = string.Concat(message, " NFT OffChain MetaType:".PadRight(colWidth), web3NFT.NFTOffChainMetaType.Name, lineBreak);
            message = string.Concat(message, " JSON MetaData URL:".PadRight(colWidth), web3NFT.JSONMetaDataURL, lineBreak);
            //TODO: Add rest of properties.

            if (web3NFT.JSONMetaDataURLHolonId != Guid.Empty)
                message = string.Concat(message, " JSON MetaData URL Holon Id:".PadRight(colWidth), web3NFT.JSONMetaDataURLHolonId, lineBreak);

            message = string.Concat(message, " Image URL:".PadRight(colWidth), web3NFT.ImageUrl, lineBreak);
            message = string.Concat(message, " Thumbnail URL:".PadRight(colWidth), web3NFT.ThumbnailUrl, lineBreak);

            return message;
        }

        private string GenerateWeb4GeoNFTSummary(IWeb4GeoSpatialNFT OASISNFT, string lineBreak, int colWidth)
        {
            string message = "";
            message = string.Concat(message, " Lat/Long:".PadRight(colWidth), OASISNFT.Lat, "/", OASISNFT.Long, lineBreak);
            message = string.Concat(message, " Perm Spawn:".PadRight(colWidth), OASISNFT.PermSpawn, lineBreak);

            if (!OASISNFT.PermSpawn)
            {
                message = string.Concat(message, " Allow Other Players To Also Collect:".PadRight(colWidth), OASISNFT.AllowOtherPlayersToAlsoCollect, lineBreak);

                if (OASISNFT.AllowOtherPlayersToAlsoCollect)
                {
                    message = string.Concat(message, " Global Spawn Quantity:".PadRight(colWidth), OASISNFT.GlobalSpawnQuantity, lineBreak);
                    message = string.Concat(message, " Player Spawn Quantity:".PadRight(colWidth), OASISNFT.PlayerSpawnQuantity, lineBreak);
                    message = string.Concat(message, " Respawn Duration In Seconds:".PadRight(colWidth), OASISNFT.RespawnDurationInSeconds, lineBreak);
                }
                else
                {
                    message = string.Concat(message, " Global Spawn Quantity:".PadRight(colWidth), "N/A", lineBreak);
                    message = string.Concat(message, " Player Spawn Quantity:".PadRight(colWidth), "N/A", lineBreak);
                    message = string.Concat(message, " Respawn Duration In Seconds:".PadRight(colWidth), "N/A", lineBreak);
                }
            }
            else
            {
                message = string.Concat(message, " Allow Other Players To Also Collect:".PadRight(colWidth), "N/A", lineBreak);
                message = string.Concat(message, " Global Spawn Quantity:".PadRight(colWidth), "N/A", lineBreak);
                message = string.Concat(message, " Player Spawn Quantity:".PadRight(colWidth), "N/A", lineBreak);
                message = string.Concat(message, " Respawn Duration In Seconds:".PadRight(colWidth), "N/A", lineBreak);
            }

            message = string.Concat(message, " 2D Sprite URI:".PadRight(colWidth), !string.IsNullOrEmpty(OASISNFT.Nft2DSpriteURI) ? OASISNFT.Nft2DSpriteURI : "None", lineBreak);
            message = string.Concat(message, " 2D Sprite:".PadRight(colWidth), OASISNFT.Nft2DSprite != null ? "Yes" : "None", lineBreak);
            message = string.Concat(message, " 3D Object URI:".PadRight(colWidth), !string.IsNullOrEmpty(OASISNFT.Nft3DObjectURI) ? OASISNFT.Nft3DObjectURI : "None", lineBreak);
            message = string.Concat(message, " 3D Object:".PadRight(colWidth), OASISNFT.Nft3DObject != null ? "Yes" : "None", lineBreak);
            message = string.Concat(message, " GeoNFT MetaData Provider:".PadRight(colWidth), OASISNFT.GeoNFTMetaDataProvider.Name, lineBreak);

            return message;
        }

        private string GenerateSendMessage(INFTBase nft, IMintWeb4NFTRequest request, string sendNFTTransactionHash = "", string lineBreak = "", int colWidth = 20)
        {
            string sendNFTMessage = "";

            if (!string.IsNullOrEmpty(nft.SendToAddressAfterMinting))
                sendNFTMessage = string.Concat(" Send To Address After Minting: ".PadRight(colWidth), nft.SendToAddressAfterMinting, $". {lineBreak}");

            if (!string.IsNullOrEmpty(nft.SendToAvatarAfterMintingId.ToString()) && nft.SendToAvatarAfterMintingId.ToString() != Guid.Empty.ToString())
                sendNFTMessage = string.Concat(sendNFTMessage, " Send To Avatar After Minting Id: ".PadRight(colWidth), nft.SendToAvatarAfterMintingId, $". {lineBreak}");

            if (!string.IsNullOrEmpty(nft.SendToAvatarAfterMintingUsername))
                sendNFTMessage = string.Concat(sendNFTMessage, " Send To Avatar After Minting Username: ".PadRight(colWidth), nft.SendToAvatarAfterMintingUsername, $". {lineBreak}");

            if (request != null)
            {
                if (!string.IsNullOrEmpty(request.SendToAvatarAfterMintingEmail))
                    sendNFTMessage = string.Concat(sendNFTMessage, " Send To Avatar After Minting Email: ".PadRight(colWidth), request.SendToAvatarAfterMintingEmail, $". {lineBreak}");
            }

            if (!string.IsNullOrEmpty(sendNFTTransactionHash))
                sendNFTMessage = string.Concat(sendNFTMessage, " Send NFT Hash: ".PadRight(colWidth), sendNFTTransactionHash, $". {lineBreak}");

            return sendNFTMessage;
        }

        private OASISResult<IHolon> SaveJSONMetaDataToOASIS(IMintWeb4NFTRequest request, EnumValue<ProviderType> metaDataProviderType, string json)
        {
            return Data.SaveHolon(new Holon()
            {
                MetaData = new Dictionary<string, object>()
                            {
                                { "data",  json }
                            }
            }, request.MintedByAvatarId, true, true, 0, true, false, metaDataProviderType.Value);
        }

        private async Task<OASISResult<IHolon>> SaveJSONMetaDataToOASISAsync(IMintWeb4NFTRequest request, EnumValue<ProviderType> metaDataProviderType, string json)
        {
            return await Data.SaveHolonAsync(new Holon()
            {
                MetaData = new Dictionary<string, object>()
                            {
                                { "data",  json }
                            }
            }, request.MintedByAvatarId, true, true, 0, true, false, metaDataProviderType.Value);
        }

        public string CreateMetaDataJson(IMintWeb4NFTRequest request, NFTStandardType NFTStandardType)
        {
            if (request.OnChainProvider.Value == ProviderType.SolanaOASIS)
                return CreateMetaplexJson(request);
            else
            {
                switch (NFTStandardType)
                {
                    case NFTStandardType.ERC721:
                        return CreateERC721Json(request);

                    case NFTStandardType.ERC1155:
                        return CreateERC1155Json(request);
                }
            }

            return "";
        }

        private string CreateMetaplexJson(IMintWeb4NFTRequest request)
        {
            var metadata = new
            {
                name = request.Title,
                symbol = request.Symbol,
                description = request.Description,
                seller_fee_basis_points = 500,
                image = request.ImageUrl,
                thumbnail = request.ThumbnailUrl,
                attributes = request.MetaData != null ? request.MetaData : new Dictionary<string, string>(),
                price = request.Price,
                discount = request.Discount,
                memo = request.MemoText
            };

            return System.Text.Json.JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
        }

        private string CreateERC721Json(IMintWeb4NFTRequest request)
        {
            var metadata = new
            {
                title = request.Title,
                description = request.Description,
                image = request.ImageUrl,
                thumbnail = request.ThumbnailUrl,
                attributes = request.MetaData != null ? request.MetaData : new Dictionary<string, string>(),
                price = request.Price,
                discount = request.Discount,
                memo = request.MemoText
            };

            return System.Text.Json.JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
        }

        private string CreateERC1155Json(IMintWeb4NFTRequest request)
        {
            var metadata = new
            {
                title = request.Title,
                description = request.Description,
                image = request.ImageUrl,
                thumbnail = request.ThumbnailUrl,
                copies = request.NumberToMint,
                attributes = request.MetaData != null ? request.MetaData : new Dictionary<string, string>(),
                price = request.Price,
                discount = request.Discount,
                memo = request.MemoText
            };

            return System.Text.Json.JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
        }

        private IWeb3NFT UpdateWeb3NFT(IWeb3NFT web3NFT, IMintWeb3NFTRequest request)
        {
            if (web3NFT.Id == Guid.Empty)
                web3NFT.Id = Guid.NewGuid();

            web3NFT.MintedByAvatarId = request.MintedByAvatarId;
            web3NFT.SendToAddressAfterMinting = request.SendToAddressAfterMinting;
            web3NFT.SendToAvatarAfterMintingId = request.SendToAvatarAfterMintingId;
            web3NFT.SendToAvatarAfterMintingUsername = request.SendToAvatarAfterMintingUsername;
            web3NFT.Title = request.Title;
            web3NFT.Description = request.Description;
            web3NFT.Price = request.Price.Value;
            web3NFT.Discount = request.Discount.Value;
            web3NFT.RoyaltyPercentage = request.RoyaltyPercentage.HasValue ? request.RoyaltyPercentage.Value : 0;
            web3NFT.Image = request.Image;
            web3NFT.ImageUrl = request.ImageUrl;
            web3NFT.Thumbnail = request.Thumbnail;
            web3NFT.ThumbnailUrl = request.ThumbnailUrl;
            web3NFT.OnChainProvider = new EnumValue<ProviderType>(request.OnChainProvider.Value);
            web3NFT.OffChainProvider = new EnumValue<ProviderType>(request.OffChainProvider.Value);
            web3NFT.StoreNFTMetaDataOnChain = request.StoreNFTMetaDataOnChain.HasValue ? request.StoreNFTMetaDataOnChain.Value : false;
            web3NFT.NFTOffChainMetaType = new EnumValue<NFTOffChainMetaType>(request.NFTOffChainMetaType.Value);
            web3NFT.NFTStandardType = new EnumValue<NFTStandardType>(request.NFTStandardType.Value);
            web3NFT.Symbol = request.Symbol;
            web3NFT.MintedOn = DateTime.Now;
            web3NFT.MemoText = request.MemoText;
            web3NFT.JSONMetaDataURL = request.JSONMetaDataURL;
            web3NFT.IsForSale = request.IsForSale.HasValue ? request.IsForSale.Value : false;
            web3NFT.SaleStartDate = request.SaleStartDate;
            web3NFT.SaleEndDate = request.SaleEndDate;
            web3NFT.Tags = request.Tags;
            web3NFT.MetaData = request.MetaData;
            //web3NFT.MetaData["{{{newnft}}}"] = "true";

            return web3NFT;
        }

        private Web4NFT CreateWeb4NFT(IMintWeb4NFTRequest request)
        {
            return new Web4NFT()
            {
                Id = Guid.NewGuid(),
                MetaData = request.MetaData,
                Tags = request.Tags,
                CollectionPublicKey = request.CollectionPublicKey,
                MintedByAvatarId = request.MintedByAvatarId,
                SendToAddressAfterMinting = request.SendToAddressAfterMinting,
                SendToAvatarAfterMintingId = request.SendToAvatarAfterMintingId,
                SendToAvatarAfterMintingUsername = request.SendToAvatarAfterMintingUsername,
                Title = request.Title,
                Description = request.Description,
                Price = request.Price,
                Discount = request.Discount,
                RoyaltyPercentage = request.RoyaltyPercentage.HasValue ? request.RoyaltyPercentage.Value : 0,
                Image = request.Image,
                ImageUrl = request.ImageUrl,
                Thumbnail = request.Thumbnail,
                ThumbnailUrl = request.ThumbnailUrl,
                OnChainProvider = request.OnChainProvider,
                OffChainProvider = request.OffChainProvider,
                StoreNFTMetaDataOnChain = request.StoreNFTMetaDataOnChain,
                NFTOffChainMetaType = request.NFTOffChainMetaType,
                NFTStandardType = request.NFTStandardType,
                Symbol = request.Symbol,
                MintedOn = DateTime.Now,
                MemoText = request.MemoText,
                JSONMetaDataURL = request.JSONMetaDataURL,
                IsForSale = request.IsForSale.HasValue ? request.IsForSale.Value : false,
                SaleStartDate = request.SaleStartDate,
                SaleEndDate = request.SaleEndDate
            };
        }

        private Web4NFT CreateWeb4NFT(IImportWeb3NFTRequest request)
        {
            return new Web4NFT()
            {
                Id = Guid.NewGuid(),
                MetaData = request.MetaData,
                Tags = request.Tags,
                ImportedByAvatarId = request.ImportedByAvatarId,
                ImportedOn = DateTime.Now,
                Title = request.Title,
                Description = request.Description,
                Price = request.Price,
                Discount = request.Discount,
                RoyaltyPercentage = request.RoyaltyPercentage,
                Image = request.Image,
                ImageUrl = request.ImageUrl,
                Thumbnail = request.Thumbnail,
                ThumbnailUrl = request.ThumbnailUrl,
                OnChainProvider = request.OnChainProvider,
                OffChainProvider = request.OffChainProvider,
                StoreNFTMetaDataOnChain = request.StoreNFTMetaDataOnChain,
                NFTOffChainMetaType = request.NFTOffChainMetaType,
                NFTStandardType = request.NFTStandardType,
                Symbol = request.Symbol,
                MemoText = request.MemoText,
                JSONMetaDataURL = request.JSONMetaDataURL,
                IsForSale = request.IsForSale.Value,
                SaleStartDate = request.SaleStartDate,
                SaleEndDate = request.SaleEndDate,
                Web3NFTs = new List<Web3NFT>() { new Web3NFT()
                {
                    MintTransactionHash = request.MintTransactionHash,
                    NFTMintedUsingWalletAddress = request.NFTMintedUsingWalletAddress,
                    UpdateAuthority = request.UpdateAuthority,
                    NFTTokenAddress = request.NFTTokenAddress
                } }
            };
        }

        private Web4OASISGeoSpatialNFT CreateWeb4GeoSpatialNFT(IPlaceWeb4GeoSpatialNFTRequest request, IWeb4NFT originalNftMetaData)
        {
            return new Web4OASISGeoSpatialNFT()
            {
                Id = Guid.NewGuid(),  //The NFT could be placed many times so we need a new ID for each time
                OriginalWeb4OASISNFTId = request.OriginalWeb4OASISNFTId, //We need to link back to the orignal NFT (but we copy across the NFT properties making it quicker and easier to get at the data). TODO: Do we want to copy the data across? Pros and Cons? Need to think about this... for now it's fine... ;-)
                GeoNFTMetaDataProvider = request.GeoNFTMetaDataProvider,
                JSONMetaDataURL = originalNftMetaData.JSONMetaDataURL,
                MintedByAvatarId = originalNftMetaData.MintedByAvatarId,
                SendToAddressAfterMinting = originalNftMetaData.SendToAddressAfterMinting,
                SendToAvatarAfterMintingId = originalNftMetaData.SendToAvatarAfterMintingId,
                SendToAvatarAfterMintingUsername = originalNftMetaData.SendToAvatarAfterMintingUsername,
                SellerFeeBasisPoints = originalNftMetaData.SellerFeeBasisPoints,
                Title = originalNftMetaData.Title,
                Description = originalNftMetaData.Description,
                Price = originalNftMetaData.Price,
                Discount = originalNftMetaData.Discount,
                RoyaltyPercentage = originalNftMetaData.RoyaltyPercentage,
                IsForSale = originalNftMetaData.IsForSale,
                SaleStartDate = originalNftMetaData.SaleStartDate,
                SaleEndDate = originalNftMetaData.SaleEndDate,
                Image = originalNftMetaData.Image,
                ImageUrl = originalNftMetaData.ImageUrl,
                Thumbnail = originalNftMetaData.Thumbnail,
                ThumbnailUrl = originalNftMetaData.ThumbnailUrl,
                MetaData = originalNftMetaData.MetaData,
                Tags = originalNftMetaData.Tags,
                OnChainProvider = originalNftMetaData.OnChainProvider,
                OffChainProvider = originalNftMetaData.OffChainProvider,
                StoreNFTMetaDataOnChain = originalNftMetaData.StoreNFTMetaDataOnChain,
                NFTOffChainMetaType = originalNftMetaData.NFTOffChainMetaType,
                NFTStandardType = originalNftMetaData.NFTStandardType,
                Symbol = originalNftMetaData.Symbol,
                MintedOn = originalNftMetaData.MintedOn,
                MemoText = originalNftMetaData.MemoText,
                PlacedByAvatarId = request.PlacedByAvatarId,
                Lat = request.Lat,
                Long = request.Long,
                PermSpawn = request.PermSpawn,
                PlayerSpawnQuantity = request.PlayerSpawnQuantity,
                AllowOtherPlayersToAlsoCollect = request.AllowOtherPlayersToAlsoCollect,
                GlobalSpawnQuantity = request.GlobalSpawnQuantity,
                RespawnDurationInSeconds = request.RespawnDurationInSeconds,
                PlacedOn = DateTime.Now,
                Nft2DSprite = request.Nft2DSprite,
                Nft3DObject = request.Nft3DObject,
                Nft3DObjectURI = request.Nft3DObjectURI,
                Nft2DSpriteURI = request.Nft2DSpriteURI,
                Web3NFTs = originalNftMetaData.Web3NFTs
            };
        }

        private IHolon CreateWeb4NFTMetaDataHolon(IWeb4NFT nftMetaData, IImportWeb3NFTRequest request)
        {
            IHolon holonNFT = new Holon(HolonType.Web4NFT);
            holonNFT.Id = nftMetaData.Id;
            holonNFT.Name = $"{nftMetaData.OnChainProvider.Name} WEB3 NFT Imported OnTo The OASIS with title {nftMetaData.Title}";
            holonNFT.Description = request.Description;
            holonNFT.MetaData["NFT.OASISNFT"] = System.Text.Json.JsonSerializer.Serialize(nftMetaData); //TODO: May remove this because its duplicated data. BUT we may need this for other purposes later such as exporting it to a file etc (but then we could just serialaize it there and then).
            holonNFT.MetaData["NFT.MintTransactionHash"] = request.MintTransactionHash;
            holonNFT.MetaData["NFT.Id"] = nftMetaData.Id;
            holonNFT.MetaData["NFT.MemoText"] = nftMetaData.MemoText;
            holonNFT.MetaData["NFT.Title"] = nftMetaData.Title;
            holonNFT.MetaData["NFT.Description"] = nftMetaData.Description;
            holonNFT.MetaData["NFT.Price"] = request.Price.ToString();
            holonNFT.MetaData["NFT.RoyaltyPercentage"] = nftMetaData.RoyaltyPercentage.ToString();
            holonNFT.MetaData["NFT.IsForSale"] = nftMetaData.IsForSale == true ? "Yes" : "No";
            holonNFT.MetaData["NFT.SaleStartDate"] = nftMetaData.SaleStartDate.HasValue ? nftMetaData.SaleStartDate.Value.ToShortDateString() : null;
            holonNFT.MetaData["NFT.SaleEndDate"] = nftMetaData.SaleEndDate.HasValue ? nftMetaData.SaleEndDate.Value.ToShortDateString() : null;
            holonNFT.MetaData["NFT.Discount"] = request.Discount.ToString();
            holonNFT.MetaData["NFT.OnChainProvider"] = nftMetaData.OnChainProvider.Name;
            holonNFT.MetaData["NFT.OffChainProvider"] = nftMetaData.OffChainProvider.Name;
            holonNFT.MetaData["NFT.StoreNFTMetaDataOnChain"] = request.StoreNFTMetaDataOnChain ? "True" : "False";
            holonNFT.MetaData["NFT.NFTOffChainMetaType"] = nftMetaData.NFTOffChainMetaType.Name;
            holonNFT.MetaData["NFT.NFTStandardType"] = request.NFTStandardType.Name;
            holonNFT.MetaData["NFT.Symbol"] = request.Symbol;
            holonNFT.MetaData["NFT.Image"] = request.Image;
            holonNFT.MetaData["NFT.ImageUrl"] = request.ImageUrl;
            holonNFT.MetaData["NFT.Thumbnail"] = request.Thumbnail;
            holonNFT.MetaData["NFT.ThumbnailUrl"] = request.ThumbnailUrl;
            holonNFT.MetaData["NFT.JSONMetaDataURL"] = request.JSONMetaDataURL;
            holonNFT.MetaData["NFT.SellerFeeBasisPoints"] = nftMetaData.SellerFeeBasisPoints;
            holonNFT.MetaData["NFT.MetaData"] = System.Text.Json.JsonSerializer.Serialize(request.MetaData);
            holonNFT.MetaData["NFT.Tags"] = System.Text.Json.JsonSerializer.Serialize(request.Tags);
            holonNFT.MetaData["NFT.ImportedByAvatarId"] = request.ImportedByAvatarId.ToString();
            holonNFT.MetaData["NFT.ImportedOn"] = DateTime.Now;
            holonNFT.ParentHolonId = nftMetaData.ImportedByAvatarId;

            if (nftMetaData.Web3NFTs.Count > 0)
            {
                holonNFT.MetaData["NFT.UpdateAuthority"] = nftMetaData.Web3NFTs[0].UpdateAuthority;
                holonNFT.MetaData["NFT.NFTMintedUsingWalletAddress"] = nftMetaData.Web3NFTs[0].NFTMintedUsingWalletAddress;
                holonNFT.MetaData["NFT.NFTTokenAddress"] = nftMetaData.Web3NFTs[0].NFTTokenAddress;
            }

            return holonNFT;
        }

        private IHolon CreateWeb4NFTMetaDataHolon(IWeb4NFT nftMetaData, IMintWeb4NFTRequest request = null)
        {
            return UpdateWeb4NFTMetaDataHolon(new Holon(HolonType.Web4NFT), nftMetaData, request);
        }

        private IHolon UpdateWeb4NFTMetaDataHolon(IHolon holonNFT, IWeb4NFT nftMetaData, IMintWeb4NFTRequest request = null)
        {
            holonNFT.Id = nftMetaData.Id;
            holonNFT.Name = $"{nftMetaData.OnChainProvider.Name} WEB4 NFT Minted On The OASIS with title {nftMetaData.Title}";
            holonNFT.Description = nftMetaData.MemoText;
            holonNFT.MetaData["NFT.WEB4NFT"] = System.Text.Json.JsonSerializer.Serialize(nftMetaData);
            holonNFT.MetaData["NFT.CollectionPublicKey"] = nftMetaData.CollectionPublicKey;
            //holonNFT.MetaData["NFT.VerifyCollectionTransactionHash"] = nftMetaData.VerifyCollectionTransactionHash;
            holonNFT.MetaData["NFT.Id"] = nftMetaData.Id;
            holonNFT.MetaData["NFT.MintedByAvatarId"] = nftMetaData.MintedByAvatarId.ToString();
            holonNFT.MetaData["NFT.SendToAvatarAfterMintingId"] = nftMetaData.SendToAvatarAfterMintingId.ToString();
            holonNFT.MetaData["NFT.SendToAvatarAfterMintingUsername"] = nftMetaData.SendToAvatarAfterMintingUsername;
            holonNFT.MetaData["NFT.SendToAddressAfterMinting"] = nftMetaData.SendToAddressAfterMinting;
            holonNFT.MetaData["NFT.MemoText"] = nftMetaData.MemoText;
            holonNFT.MetaData["NFT.Title"] = nftMetaData.Title;
            holonNFT.MetaData["NFT.Description"] = nftMetaData.Description;
            holonNFT.MetaData["NFT.Price"] = nftMetaData.Price.ToString();
            holonNFT.MetaData["NFT.Discount"] = nftMetaData.Discount.ToString();
            holonNFT.MetaData["NFT.RoyaltyPercentage"] = nftMetaData.RoyaltyPercentage.ToString();
            holonNFT.MetaData["NFT.IsForSale"] = nftMetaData.IsForSale == true ? "Yes" : "No";
            holonNFT.MetaData["NFT.SaleStartDate"] = nftMetaData.SaleStartDate.HasValue ? nftMetaData.SaleStartDate.Value.ToShortDateString() : null;
            holonNFT.MetaData["NFT.SaleEndDate"] = nftMetaData.SaleEndDate.HasValue ? nftMetaData.SaleEndDate.Value.ToShortDateString() : null;
            holonNFT.MetaData["NFT.NumberToMint"] = request != null ? request.NumberToMint.ToString() : "";
            holonNFT.MetaData["NFT.OnChainProvider"] = nftMetaData.OnChainProvider.Name;
            holonNFT.MetaData["NFT.OffChainProvider"] = nftMetaData.OffChainProvider.Name;
            holonNFT.MetaData["NFT.StoreNFTMetaDataOnChain"] = nftMetaData.StoreNFTMetaDataOnChain ? "True" : "False";
            holonNFT.MetaData["NFT.NFTOffChainMetaType"] = nftMetaData.NFTOffChainMetaType.Name;
            holonNFT.MetaData["NFT.NFTStandardType"] = nftMetaData.NFTStandardType.Name;
            holonNFT.MetaData["NFT.Symbol"] = nftMetaData.Symbol;
            holonNFT.MetaData["NFT.Image"] = nftMetaData.Image;
            holonNFT.MetaData["NFT.ImageUrl"] = nftMetaData.ImageUrl;
            holonNFT.MetaData["NFT.Thumbnail"] = nftMetaData.Thumbnail;
            holonNFT.MetaData["NFT.ThumbnailUrl"] = nftMetaData.ThumbnailUrl;
            holonNFT.MetaData["NFT.JSONMetaDataURL"] = nftMetaData.JSONMetaDataURL;
            holonNFT.MetaData["NFT.JSONMetaDataURLHolonId"] = nftMetaData.JSONMetaDataURLHolonId;
            holonNFT.MetaData["NFT.MintedOn"] = nftMetaData.MintedOn.ToShortDateString();
            holonNFT.MetaData["NFT.SellerFeeBasisPoints"] = nftMetaData.SellerFeeBasisPoints;
            holonNFT.MetaData["NFT.Tags"] = System.Text.Json.JsonSerializer.Serialize(nftMetaData.Tags);
            holonNFT.MetaData["NFT.MetaData"] = System.Text.Json.JsonSerializer.Serialize(nftMetaData.MetaData);
            //holonNFT.MetaData["NFT.MetaData"] = nftMetaData.MetaData; //TODO: Currently the line above works fine for normal metaData but for objects such as file uploads then it causes issues displaying the meta because it is displayed/stored as a string so there is no way to know if its a binary file.
            holonNFT.ParentHolonId = nftMetaData.MintedByAvatarId;

            //TODO: Not even sure if we need to record this anymore? Because these are not stored at the web4 level anymore, only at the web3 level.
            //if (nftMetaData.Web3NFTs.Count > 0)
            //{
            //    holonNFT.MetaData["NFT.MintTransactionHash"] = nftMetaData.Web3NFTs[0].MintTransactionHash;
            //    holonNFT.MetaData["NFT.OASISMintWalletAddress"] = nftMetaData.Web3NFTs[0].OASISMintWalletAddress;
            //    holonNFT.MetaData["NFT.SendNFTTransactionHash"] = nftMetaData.Web3NFTs[0].SendNFTTransactionHash;
            //    holonNFT.MetaData["NFT.NFTTokenAddress"] = nftMetaData.Web3NFTs[0].NFTTokenAddress;
            //    holonNFT.MetaData["NFT.UpdateAuthority"] = nftMetaData.Web3NFTs[0].UpdateAuthority;
            //}

            return holonNFT;
        }

        private IHolon CreateWeb3NFTMetaDataHolon(IWeb3NFT nftMetaData, Guid parentWeb4NFTId, IMintWeb3NFTRequest request = null)
        {
            return UpdateWeb3NFTMetaDataHolon(new Holon(HolonType.Web3NFT), nftMetaData, parentWeb4NFTId, request);
        }

        private IHolon UpdateWeb3NFTMetaDataHolon(IHolon holonNFT, IWeb3NFT nftMetaData, Guid parentWeb4NFTId, IMintWeb3NFTRequest request = null)
        {
            holonNFT.Id = nftMetaData.Id;
            holonNFT.Name = $"{nftMetaData.OnChainProvider.Name} WEB3 NFT Minted On The OASIS with title {nftMetaData.Title}";
            holonNFT.Description = nftMetaData.MemoText;
            holonNFT.MetaData["NFT.WEB3NFT"] = System.Text.Json.JsonSerializer.Serialize(nftMetaData);
            holonNFT.MetaData["NFT.Id"] = nftMetaData.Id;
            holonNFT.MetaData["NFT.CollectionPublicKey"] = nftMetaData.CollectionPublicKey;
            holonNFT.MetaData["NFT.VerifyCollectionTransactionHash"] = nftMetaData.VerifyCollectionTransactionHash;
            holonNFT.MetaData["NFT.ParentWeb4NFTId"] = parentWeb4NFTId.ToString();
            holonNFT.MetaData["NFT.MintedByAvatarId"] = nftMetaData.MintedByAvatarId.ToString();
            holonNFT.MetaData["NFT.SendToAvatarAfterMintingId"] = nftMetaData.SendToAvatarAfterMintingId.ToString();
            holonNFT.MetaData["NFT.SendToAvatarAfterMintingUsername"] = nftMetaData.SendToAvatarAfterMintingUsername;
            holonNFT.MetaData["NFT.SendToAddressAfterMinting"] = nftMetaData.SendToAddressAfterMinting;
            holonNFT.MetaData["NFT.MemoText"] = nftMetaData.MemoText;
            holonNFT.MetaData["NFT.Title"] = nftMetaData.Title;
            holonNFT.MetaData["NFT.Description"] = nftMetaData.Description;
            holonNFT.MetaData["NFT.Price"] = nftMetaData.Price.ToString();
            holonNFT.MetaData["NFT.Discount"] = nftMetaData.Discount.ToString();
            holonNFT.MetaData["NFT.RoyaltyPercentage"] = nftMetaData.RoyaltyPercentage.ToString();
            holonNFT.MetaData["NFT.IsForSale"] = nftMetaData.IsForSale == true ? "Yes" : "No";
            holonNFT.MetaData["NFT.SaleStartDate"] = nftMetaData.SaleStartDate.HasValue ? nftMetaData.SaleStartDate.Value.ToShortDateString() : null;
            holonNFT.MetaData["NFT.SaleEndDate"] = nftMetaData.SaleEndDate.HasValue ? nftMetaData.SaleEndDate.Value.ToShortDateString() : null;
            holonNFT.MetaData["NFT.NumberToMint"] = request != null ? request.NumberToMint.ToString() : "";
            holonNFT.MetaData["NFT.OnChainProvider"] = nftMetaData.OnChainProvider.Name;
            holonNFT.MetaData["NFT.OffChainProvider"] = nftMetaData.OffChainProvider.Name;
            holonNFT.MetaData["NFT.StoreNFTMetaDataOnChain"] = nftMetaData.StoreNFTMetaDataOnChain ? "True" : "False";
            holonNFT.MetaData["NFT.NFTOffChainMetaType"] = nftMetaData.NFTOffChainMetaType.Name;
            holonNFT.MetaData["NFT.NFTStandardType"] = nftMetaData.NFTStandardType.Name;
            holonNFT.MetaData["NFT.Symbol"] = nftMetaData.Symbol;
            holonNFT.MetaData["NFT.Image"] = nftMetaData.Image;
            holonNFT.MetaData["NFT.ImageUrl"] = nftMetaData.ImageUrl;
            holonNFT.MetaData["NFT.Thumbnail"] = nftMetaData.Thumbnail;
            holonNFT.MetaData["NFT.ThumbnailUrl"] = nftMetaData.ThumbnailUrl;
            holonNFT.MetaData["NFT.JSONMetaDataURL"] = nftMetaData.JSONMetaDataURL;
            holonNFT.MetaData["NFT.JSONMetaDataURLHolonId"] = nftMetaData.JSONMetaDataURLHolonId;
            holonNFT.MetaData["NFT.MintedOn"] = nftMetaData.MintedOn.ToShortDateString();
            holonNFT.MetaData["NFT.SellerFeeBasisPoints"] = nftMetaData.SellerFeeBasisPoints;
            holonNFT.MetaData["NFT.Tags"] = System.Text.Json.JsonSerializer.Serialize(nftMetaData.Tags);
            holonNFT.MetaData["NFT.MetaData"] = System.Text.Json.JsonSerializer.Serialize(nftMetaData.MetaData);
            //holonNFT.MetaData["NFT.MetaData"] = nftMetaData.MetaData; //TODO: Currently the line above works fine for normal metaData but for objects such as file uploads then it causes issues displaying the meta because it is displayed/stored as a string so there is no way to know if its a binary file.
            holonNFT.ParentHolonId = nftMetaData.MintedByAvatarId;

            return holonNFT;
        }

        private IHolon CreateWeb4GeoSpatialNFTMetaDataHolon(IWeb4GeoSpatialNFT geoNFTMetaData, IMintWeb4NFTRequest request = null)
        {
            return UpdateWeb4GeoNFTMetaDataHolon(new Holon(HolonType.Web4GeoNFT), geoNFTMetaData, request);
        }

        private IHolon UpdateWeb4GeoNFTMetaDataHolon(IHolon holonNFT, IWeb4GeoSpatialNFT geoNFTMetaData, IMintWeb4NFTRequest request = null)
        {
            holonNFT.Id = geoNFTMetaData.Id;
            holonNFT.Name = "WEB4 OASIS GEO NFT";
            holonNFT.Description = "WEB4 OASIS GEO NFT";
            holonNFT.MetaData["GEONFT.WEB4GEONFT"] = System.Text.Json.JsonSerializer.Serialize(geoNFTMetaData); //TODO: May remove this because its duplicated data.
            holonNFT.MetaData["GEONFT.Id"] = geoNFTMetaData.Id;
            holonNFT.MetaData["GEONFT.GeoNFTMetaDataProvider"] = geoNFTMetaData.GeoNFTMetaDataProvider.Name;
            holonNFT.MetaData["GEONFT.PlacedByAvatarId"] = geoNFTMetaData.PlacedByAvatarId.ToString();
            holonNFT.MetaData["GEONFT.PlacedOn"] = geoNFTMetaData.PlacedOn.ToShortDateString();
            holonNFT.MetaData["GEONFT.Lat"] = geoNFTMetaData.Lat;
            holonNFT.MetaData["GEONFT.Long"] = geoNFTMetaData.Long;
            holonNFT.MetaData["GEONFT.LatLong"] = string.Concat(geoNFTMetaData.Lat, ":", geoNFTMetaData.Long);
            holonNFT.MetaData["GEONFT.PermSpawn"] = geoNFTMetaData.PermSpawn;
            holonNFT.MetaData["GEONFT.PlayerSpawnQuantity"] = geoNFTMetaData.PlayerSpawnQuantity;
            holonNFT.MetaData["GEONFT.AllowOtherPlayersToAlsoCollect"] = geoNFTMetaData.AllowOtherPlayersToAlsoCollect;
            holonNFT.MetaData["GEONFT.GlobalSpawnQuantity"] = geoNFTMetaData.GlobalSpawnQuantity;
            holonNFT.MetaData["GEONFT.RespawnDurationInSeconds"] = geoNFTMetaData.RespawnDurationInSeconds;
            holonNFT.MetaData["GEONFT.Nft2DSprite"] = geoNFTMetaData.Nft2DSprite;
            holonNFT.MetaData["GEONFT.Nft2DSpriteURI"] = geoNFTMetaData.Nft2DSpriteURI;
            holonNFT.MetaData["GEONFT.Nft3DObject"] = geoNFTMetaData.Nft3DObject;
            holonNFT.MetaData["GEONFT.Nft3DObjectURI"] = geoNFTMetaData.Nft3DObjectURI;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.Id"] = geoNFTMetaData.OriginalWeb4OASISNFTId;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.MemoText"] = geoNFTMetaData.MemoText;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.Title"] = geoNFTMetaData.Title;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.Description"] = geoNFTMetaData.Description;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.MintedByAvatarId"] = geoNFTMetaData.MintedByAvatarId.ToString();
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.SendToAvatarAfterMintingId"] = geoNFTMetaData.SendToAvatarAfterMintingId.ToString();
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.SendToAvatarAfterMintingUsername"] = geoNFTMetaData.SendToAvatarAfterMintingUsername;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.SendToAddressAfterMinting"] = geoNFTMetaData.SendToAddressAfterMinting;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.Price"] = geoNFTMetaData.Price.ToString();
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.Discount"] = geoNFTMetaData.Discount.ToString();
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.RoyaltyPercentage"] = geoNFTMetaData.RoyaltyPercentage.ToString();
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.IsForSale"] = geoNFTMetaData.IsForSale == true ? "Yes" : "No";
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.SaleStartDate"] = geoNFTMetaData.SaleStartDate.HasValue ? geoNFTMetaData.SaleStartDate.Value.ToShortDateString() : null;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.SaleEndDate"] = geoNFTMetaData.SaleEndDate.HasValue ? geoNFTMetaData.SaleEndDate.Value.ToShortDateString() : null;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.OnChainProvider"] = geoNFTMetaData.OnChainProvider.Name;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.OffChainProvider"] = geoNFTMetaData.OffChainProvider.Name;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.StoreNFTMetaDataOnChain"] = geoNFTMetaData.StoreNFTMetaDataOnChain ? "True" : "False";
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.NFTOffChainMetaType"] = geoNFTMetaData.NFTOffChainMetaType.Name;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.NFTStandardType"] = geoNFTMetaData.NFTStandardType.Name;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.Symbol"] = geoNFTMetaData.Symbol;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.Image"] = geoNFTMetaData.Image;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.ImageUrl"] = geoNFTMetaData.ImageUrl;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.Thumbnail"] = geoNFTMetaData.Thumbnail;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.ThumbnailUrl"] = geoNFTMetaData.ThumbnailUrl;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.JSONMetaDataURL"] = geoNFTMetaData.JSONMetaDataURL;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.JSONMetaDataURLHolonId"] = geoNFTMetaData.JSONMetaDataURLHolonId;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.MintedOn"] = geoNFTMetaData.MintedOn.ToShortDateString();
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.SellerFeeBasisPoints"] = geoNFTMetaData.SellerFeeBasisPoints;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.MetaData"] = geoNFTMetaData.MetaData;
            holonNFT.MetaData["GEONFT.OriginalOASISNFT.Tags"] = geoNFTMetaData.Tags;

            //TODO: Not even sure if we need to record this anymore? Because these are not stored at the web4 level anymore, only at the web3 level.
            //if (geoNFTMetaData.Web3NFTs.Count > 0)
            //{
            //    holonNFT.MetaData["NFT.MintTransactionHash"] = geoNFTMetaData.Web3NFTs[0].MintTransactionHash;
            //    holonNFT.MetaData["NFT.OASISMintWalletAddress"] = geoNFTMetaData.Web3NFTs[0].OASISMintWalletAddress;
            //    holonNFT.MetaData["NFT.SendNFTTransactionHash"] = geoNFTMetaData.Web3NFTs[0].SendNFTTransactionHash;
            //    holonNFT.MetaData["NFT.NFTTokenAddress"] = geoNFTMetaData.Web3NFTs[0].NFTTokenAddress;
            //    holonNFT.MetaData["NFT.UpdateAuthority"] = geoNFTMetaData.Web3NFTs[0].UpdateAuthority;
            //}

            return holonNFT;
        }

        private IMintWeb4NFTRequest CreateMintWeb4NFTTransactionRequest(IMintAndPlaceWeb4GeoSpatialNFTRequest mintAndPlaceGeoSpatialNFTRequest)
        {
            return new MintWeb4NFTRequest()
            {
                //MintWalletAddress = mintAndPlaceGeoSpatialNFTRequest.MintWalletAddress,
                MintedByAvatarId = mintAndPlaceGeoSpatialNFTRequest.MintedByAvatarId,
                Title = mintAndPlaceGeoSpatialNFTRequest.Title,
                Description = mintAndPlaceGeoSpatialNFTRequest.Description,
                Image = mintAndPlaceGeoSpatialNFTRequest.Image,
                ImageUrl = mintAndPlaceGeoSpatialNFTRequest.ImageUrl,
                Thumbnail = mintAndPlaceGeoSpatialNFTRequest.Thumbnail,
                ThumbnailUrl = mintAndPlaceGeoSpatialNFTRequest.ThumbnailUrl,
                Price = mintAndPlaceGeoSpatialNFTRequest.Price,
                Discount = mintAndPlaceGeoSpatialNFTRequest.Discount,
                RoyaltyPercentage = mintAndPlaceGeoSpatialNFTRequest.RoyaltyPercentage,
                IsForSale = mintAndPlaceGeoSpatialNFTRequest.IsForSale,
                SaleStartDate = mintAndPlaceGeoSpatialNFTRequest.SaleStartDate,
                SaleEndDate = mintAndPlaceGeoSpatialNFTRequest.SaleEndDate,
                MemoText = mintAndPlaceGeoSpatialNFTRequest.MemoText,
                NumberToMint = mintAndPlaceGeoSpatialNFTRequest.NumberToMint,
                MetaData = mintAndPlaceGeoSpatialNFTRequest.MetaData,
                Tags = mintAndPlaceGeoSpatialNFTRequest.Tags,
                OffChainProvider = mintAndPlaceGeoSpatialNFTRequest.OffChainProvider,
                OnChainProvider = mintAndPlaceGeoSpatialNFTRequest.OnChainProvider,
                JSONMetaDataURL = mintAndPlaceGeoSpatialNFTRequest.JSONMetaDataURL,
                NFTOffChainMetaType = mintAndPlaceGeoSpatialNFTRequest.NFTOffChainMetaType,
                NFTStandardType = mintAndPlaceGeoSpatialNFTRequest.NFTStandardType,
                SendToAddressAfterMinting = mintAndPlaceGeoSpatialNFTRequest.SendToAddressAfterMinting,
                SendToAvatarAfterMintingId = mintAndPlaceGeoSpatialNFTRequest.SendToAvatarAfterMintingId,
                SendToAvatarAfterMintingUsername = mintAndPlaceGeoSpatialNFTRequest.SendToAvatarAfterMintingUsername,
                StoreNFTMetaDataOnChain = mintAndPlaceGeoSpatialNFTRequest.StoreNFTMetaDataOnChain,
                Symbol = mintAndPlaceGeoSpatialNFTRequest.Symbol,
                AttemptToMintEveryXSeconds = mintAndPlaceGeoSpatialNFTRequest.AttemptToMintEveryXSeconds,
                AttemptToSendEveryXSeconds = mintAndPlaceGeoSpatialNFTRequest.AttemptToSendEveryXSeconds,
                JSONMetaData = mintAndPlaceGeoSpatialNFTRequest.JSONMetaData,
                SendToAvatarAfterMintingEmail = mintAndPlaceGeoSpatialNFTRequest.SendToAvatarAfterMintingEmail,
                WaitForNFTToMintInSeconds = mintAndPlaceGeoSpatialNFTRequest.WaitForNFTToMintInSeconds,
                WaitForNFTToSendInSeconds = mintAndPlaceGeoSpatialNFTRequest.WaitForNFTToSendInSeconds,
                WaitTillNFTMinted = mintAndPlaceGeoSpatialNFTRequest.WaitTillNFTMinted,
                WaitTillNFTSent = mintAndPlaceGeoSpatialNFTRequest.WaitTillNFTSent,
                Web3NFTs = mintAndPlaceGeoSpatialNFTRequest.Web3NFTs
            };
        }

    }
}