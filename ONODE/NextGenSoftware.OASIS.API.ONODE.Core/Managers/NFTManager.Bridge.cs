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

    }
}
