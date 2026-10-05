namespace Infrastructure.ImplementationContract;

using Application.Contracts;
using Application.DTOs.Account.OASIS;
using Application.DTOs.Account.Requests;
using Application.DTOs.FractionalNFT;
using BuildingBlocks.Extensions.Http;
using BuildingBlocks.Extensions.Logger;
using BuildingBlocks.Extensions.ResultPattern;
using Domain.Constants;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using System.Text.Json;

/// <summary>
/// Service for minting fractional ownership NFTs for real-world assets
/// </summary>
public sealed class FractionalNFTService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<FractionalNFTService> logger,
    DataContext dbContext,
    IHttpContextAccessor accessor,
    IOASISAuthService oasisAuthService,
    IIpfsService ipfsService) : IFractionalNFTService
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient(HttpClientNames.OASISClient);
    private readonly string _oasisApiUrl = configuration["OASIS:ApiUrl"] ?? "https://api.oasisplatform.world";
    private readonly string _siteAvatarUsername = configuration["OASIS:SiteAvatar:Username"] ?? string.Empty;
    private readonly string _siteAvatarPassword = configuration["OASIS:SiteAvatar:Password"] ?? string.Empty;
    private readonly string _siteAvatarId = configuration["OASIS:SiteAvatar:AvatarId"] ?? string.Empty;

    public int CalculateTokenCount(decimal fractionAmount, int totalSupply)
    {
        if (fractionAmount <= 0 || fractionAmount > 1.0m)
            throw new ArgumentException("Fraction amount must be between 0.01 and 1.0", nameof(fractionAmount));

        if (totalSupply <= 0)
            throw new ArgumentException("Total supply must be greater than 0", nameof(totalSupply));

        // Calculate token count: fraction * total supply, rounded to nearest integer
        decimal tokenCount = fractionAmount * totalSupply;
        return (int)Math.Round(tokenCount, MidpointRounding.AwayFromZero);
    }

    public async Task<Result<NFTMetadata>> ConvertUATToNFTMetadataAsync(
        string assetId,
        decimal fractionAmount,
        CancellationToken token = default)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(ConvertUATToNFTMetadataAsync), date);

        try
        {
            // Get RWA token details
            RwaToken? rwaToken = await dbContext.RwaTokens
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == Guid.Parse(assetId), token);

            if (rwaToken is null)
            {
                logger.OperationCompleted(nameof(ConvertUATToNFTMetadataAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
                return Result<NFTMetadata>.Failure(ResultPatternError.NotFound("RWA token not found"));
            }

            // Calculate token count (assuming total supply from metadata or default)
            // For now, we'll use a default total supply - this should come from UAT metadata
            int totalSupply = 3500; // TODO: Extract from UAT metadata
            int tokenCount = CalculateTokenCount(fractionAmount, totalSupply);
            decimal percentage = fractionAmount * 100;

            // Build NFT metadata
            var metadata = new NFTMetadata
            {
                Name = $"{rwaToken.Title} - {percentage:F2}% Fraction",
                Description = $"Fractional ownership representing {percentage:F2}% of {rwaToken.Title}. {rwaToken.AssetDescription}",
                Image = rwaToken.Image,
                ExternalUrl = $"https://exchange.quantumstreet.com/rwa/{assetId}",
                Attributes = new List<NFTAttribute>
                {
                    new() { TraitType = "Fraction", Value = fractionAmount.ToString("F8") },
                    new() { TraitType = "Percentage", Value = $"{percentage:F2}%" },
                    new() { TraitType = "Total Supply", Value = totalSupply.ToString() },
                    new() { TraitType = "Tokens Owned", Value = tokenCount.ToString() },
                    new() { TraitType = "Asset Class", Value = rwaToken.AssetType.ToString() },
                    new() { TraitType = "Location", Value = rwaToken.Geolocation != null 
                        ? $"{rwaToken.Geolocation.Latitude}, {rwaToken.Geolocation.Longitude}" 
                        : "N/A" }
                },
                Properties = new NFTProperties
                {
                    AssetId = assetId,
                    MintDate = DateTimeOffset.UtcNow.ToString("O"),
                    UatMetadata = new Dictionary<string, object>
                    {
                        { "title", rwaToken.Title },
                        { "description", rwaToken.AssetDescription },
                        { "assetType", rwaToken.AssetType.ToString() },
                        { "propertyType", rwaToken.PropertyType.ToString() }
                    }
                }
            };

            logger.OperationCompleted(nameof(ConvertUATToNFTMetadataAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return Result<NFTMetadata>.Success(metadata);
        }
        catch (Exception ex)
        {
            logger.OperationException(nameof(ConvertUATToNFTMetadataAsync), ex.Message);
            logger.OperationCompleted(nameof(ConvertUATToNFTMetadataAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return Result<NFTMetadata>.Failure(ResultPatternError.InternalServerError(ex.Message));
        }
    }

    public async Task<Result<MintFractionalNFTResponse>> MintFractionalNFTAsync(
        MintFractionalNFTRequest request,
        CancellationToken cancellationToken = default)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(MintFractionalNFTAsync), date);

        try
        {
            // Validate request
            if (request.FractionAmount <= 0 || request.FractionAmount > 1.0m)
            {
                return Result<MintFractionalNFTResponse>.Failure(
                    ResultPatternError.BadRequest("Fraction amount must be between 0.01 and 1.0"));
            }

            // Get RWA token
            RwaToken? rwaToken = await dbContext.RwaTokens
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == request.AssetId, cancellationToken);

            if (rwaToken is null)
            {
                return Result<MintFractionalNFTResponse>.Failure(
                    ResultPatternError.NotFound("RWA token not found"));
            }

            // Get buyer wallet linked account
            WalletLinkedAccount? buyerWallet = await dbContext.WalletLinkedAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.PublicKey == request.BuyerWallet, cancellationToken);

            if (buyerWallet is null)
            {
                return Result<MintFractionalNFTResponse>.Failure(
                    ResultPatternError.NotFound("Buyer wallet not found"));
            }

            // Convert UAT to NFT metadata
            Result<NFTMetadata> metadataResult = await ConvertUATToNFTMetadataAsync(
                request.AssetId.ToString(),
                request.FractionAmount,
                cancellationToken);

            if (!metadataResult.IsSuccess || metadataResult.Value is null)
            {
                return Result<MintFractionalNFTResponse>.Failure(metadataResult.Error);
            }

            NFTMetadata metadata = metadataResult.Value;

            // Upload metadata to IPFS
            // Create a JSON file from metadata
            string metadataJson = JsonSerializer.Serialize(new
            {
                name = metadata.Name,
                description = metadata.Description,
                image = metadata.Image,
                external_url = metadata.ExternalUrl,
                attributes = metadata.Attributes.Select(a => new
                {
                    trait_type = a.TraitType,
                    value = a.Value
                }),
                properties = new
                {
                    uat_metadata = metadata.Properties.UatMetadata,
                    asset_id = metadata.Properties.AssetId,
                    mint_date = metadata.Properties.MintDate
                }
            }, new JsonSerializerOptions { WriteIndented = true });

            // Upload to IPFS (we'll need to create a file from the JSON string)
            // For now, we'll use a placeholder - in production, upload actual JSON
            string metadataUrl = $"ipfs://placeholder/{request.AssetId}"; // TODO: Upload to IPFS

            // Get OASIS JWT token (authenticate with site avatar)
            string jwtToken = await GetOASISTokenAsync(cancellationToken);

            // Calculate token count
            int totalSupply = 3500; // TODO: Get from UAT metadata
            int tokenCount = CalculateTokenCount(request.FractionAmount, totalSupply);

            // Prepare mint request
            var mintRequest = new
            {
                JSONMetaDataURL = metadataUrl,
                Title = metadata.Name,
                Symbol = $"{rwaToken.Title.Substring(0, Math.Min(4, rwaToken.Title.Length))}FRAC",
                MintedByAvatarId = request.BuyerAvatarId,
                SendToAddressAfterMinting = request.BuyerWallet
            };

            // Call OASIS API to mint NFT
            string mintUrl = $"{_oasisApiUrl}/api/Solana/Mint";
            using var mintRequestMessage = new HttpRequestMessage(HttpMethod.Post, mintUrl);
            mintRequestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);
            mintRequestMessage.Content = JsonContent.Create(mintRequest);

            using var mintResponse = await _httpClient.SendAsync(mintRequestMessage, cancellationToken);
            string mintResponseContent = await mintResponse.Content.ReadAsStringAsync(cancellationToken);

            if (!mintResponse.IsSuccessStatusCode)
            {
                logger.LogWarning($"OASIS mint failed: {mintResponse.StatusCode} - {mintResponseContent}");
                return Result<MintFractionalNFTResponse>.Failure(
                    ResultPatternError.InternalServerError($"Minting failed: {mintResponse.StatusCode}"));
            }

            // Parse OASIS response
            var jsonDoc = JsonDocument.Parse(mintResponseContent);
            var root = jsonDoc.RootElement;

            // Handle nested result structure
            JsonElement resultElement = root;
            if (root.TryGetProperty("result", out var resultProp))
            {
                resultElement = resultProp;
            }

            bool isError = resultElement.TryGetProperty("isError", out var isErrorProp) && isErrorProp.GetBoolean() ||
                          resultElement.TryGetProperty("IsError", out var isErrorProp2) && isErrorProp2.GetBoolean();

            if (isError)
            {
                string errorMsg = resultElement.TryGetProperty("message", out var msgProp) ? msgProp.GetString() ?? "Unknown error" :
                                 resultElement.TryGetProperty("Message", out var msgProp2) ? msgProp2.GetString() ?? "Unknown error" :
                                 "Minting failed";
                return Result<MintFractionalNFTResponse>.Failure(
                    ResultPatternError.InternalServerError(errorMsg));
            }

            // Extract mint account and transaction hash
            JsonElement dataElement = resultElement;
            if (resultElement.TryGetProperty("Result", out var resultData))
            {
                dataElement = resultData;
            }
            else if (resultElement.TryGetProperty("result", out var resultData2))
            {
                dataElement = resultData2;
            }

            string mintAddress = dataElement.TryGetProperty("mintAccount", out var mintProp) ? mintProp.GetString() ?? "" :
                               dataElement.TryGetProperty("MintAccount", out var mintProp2) ? mintProp2.GetString() ?? "" : "";

            string transactionHash = dataElement.TryGetProperty("transactionHash", out var txProp) ? txProp.GetString() ?? "" :
                                    dataElement.TryGetProperty("TransactionHash", out var txProp2) ? txProp2.GetString() ?? "" : "";

            if (string.IsNullOrEmpty(mintAddress))
            {
                return Result<MintFractionalNFTResponse>.Failure(
                    ResultPatternError.InternalServerError("Mint address not found in response"));
            }

            // Try to transfer NFT to buyer (if not already sent)
            bool transferSuccessful = false;
            string? transferTxHash = null;

            if (!string.IsNullOrEmpty(request.BuyerWallet))
            {
                Result<TransferNFTResponse> transferResult = await TransferNFTToBuyerAsync(
                    mintAddress,
                    request.BuyerWallet,
                    cancellationToken);

                if (transferResult.IsSuccess && transferResult.Value is not null)
                {
                    transferSuccessful = transferResult.Value.Success;
                    transferTxHash = transferResult.Value.TransactionHash;
                }
            }

            // Save fractional ownership to database
            var fractionalOwnership = new FractionalOwnership
            {
                RwaTokenId = request.AssetId,
                BuyerWalletLinkedAccountId = buyerWallet.Id,
                FractionAmount = request.FractionAmount,
                TokenCount = tokenCount,
                MintAddress = mintAddress,
                MintTransactionHash = transactionHash,
                TransferTransactionHash = transferTxHash,
                TransferSuccessful = transferSuccessful,
                MetadataUrl = metadataUrl,
                CreatedBy = accessor.GetId(),
                CreatedByIp = accessor.GetRemoteIpAddress()
            };

            await dbContext.FractionalOwnerships.AddAsync(fractionalOwnership, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            logger.OperationCompleted(nameof(MintFractionalNFTAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);

            return Result<MintFractionalNFTResponse>.Success(new MintFractionalNFTResponse
            {
                MintAddress = mintAddress,
                TransactionHash = transactionHash,
                TokenCount = tokenCount,
                FractionAmount = request.FractionAmount,
                FractionalOwnershipId = fractionalOwnership.Id,
                TransferSuccessful = transferSuccessful
            });
        }
        catch (Exception ex)
        {
            logger.OperationException(nameof(MintFractionalNFTAsync), ex.Message);
            logger.OperationCompleted(nameof(MintFractionalNFTAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return Result<MintFractionalNFTResponse>.Failure(
                ResultPatternError.InternalServerError(ex.Message));
        }
    }

    public async Task<Result<TransferNFTResponse>> TransferNFTToBuyerAsync(
        string mintAddress,
        string buyerWallet,
        CancellationToken cancellationToken = default)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(TransferNFTToBuyerAsync), date);

        try
        {
            // Wait for NFT to be fully processed (as per OASIS pattern)
            await Task.Delay(5000, cancellationToken);

            // Get OASIS JWT token
            string jwtToken = await GetOASISTokenAsync(cancellationToken);

            // Get OASIS wallet address from configuration
            string oasisWallet = configuration["OASIS:WalletAddress"] ?? "AfpSpMjNyoHTZWMWkog6Znf57KV82MGzkpDUUjLtmHwG";

            // Prepare transfer request
            var transferRequest = new
            {
                FromWalletAddress = oasisWallet,
                ToWalletAddress = buyerWallet,
                NFTId = mintAddress,
                FromProviderType = "SolanaOASIS",
                ToProviderType = "SolanaOASIS",
                Amount = 1
            };

            // Call OASIS API to transfer NFT
            string transferUrl = $"{_oasisApiUrl}/api/Nft/send-nft";
            using var transferRequestMessage = new HttpRequestMessage(HttpMethod.Post, transferUrl);
            transferRequestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);
            transferRequestMessage.Content = JsonContent.Create(transferRequest);

            using var transferResponse = await _httpClient.SendAsync(transferRequestMessage, cancellationToken);
            string transferResponseContent = await transferResponse.Content.ReadAsStringAsync(cancellationToken);

            if (!transferResponse.IsSuccessStatusCode)
            {
                logger.LogWarning($"OASIS transfer failed: {transferResponse.StatusCode} - {transferResponseContent}");
                return Result<TransferNFTResponse>.Failure(
                    ResultPatternError.InternalServerError($"Transfer failed: {transferResponse.StatusCode}"));
            }

            // Parse response
            var jsonDoc = JsonDocument.Parse(transferResponseContent);
            var root = jsonDoc.RootElement;

            JsonElement resultElement = root;
            if (root.TryGetProperty("result", out var resultProp))
            {
                resultElement = resultProp;
            }

            bool isError = resultElement.TryGetProperty("isError", out var isErrorProp) && isErrorProp.GetBoolean() ||
                          resultElement.TryGetProperty("IsError", out var isErrorProp2) && isErrorProp2.GetBoolean();

            if (isError)
            {
                string errorMsg = resultElement.TryGetProperty("message", out var msgProp) ? msgProp.GetString() ?? "Transfer failed" :
                                 resultElement.TryGetProperty("Message", out var msgProp2) ? msgProp2.GetString() ?? "Transfer failed" :
                                 "Transfer failed";
                return Result<TransferNFTResponse>.Failure(
                    ResultPatternError.InternalServerError(errorMsg));
            }

            // Extract transaction hash
            JsonElement dataElement = resultElement;
            if (resultElement.TryGetProperty("Result", out var resultData))
            {
                dataElement = resultData;
            }

            string transactionHash = dataElement.TryGetProperty("transactionResult", out var txProp) ? txProp.GetString() ?? "" :
                                    dataElement.TryGetProperty("TransactionResult", out var txProp2) ? txProp2.GetString() ?? "" : "";

            logger.OperationCompleted(nameof(TransferNFTToBuyerAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);

            return Result<TransferNFTResponse>.Success(new TransferNFTResponse
            {
                TransactionHash = transactionHash,
                Success = true
            });
        }
        catch (Exception ex)
        {
            logger.OperationException(nameof(TransferNFTToBuyerAsync), ex.Message);
            logger.OperationCompleted(nameof(TransferNFTToBuyerAsync), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow - date);
            return Result<TransferNFTResponse>.Failure(
                ResultPatternError.InternalServerError(ex.Message));
        }
    }

    /// <summary>
    /// Get OASIS JWT token by authenticating with site avatar credentials
    /// </summary>
    private async Task<string> GetOASISTokenAsync(CancellationToken cancellationToken)
    {
            // Try to authenticate with site avatar credentials
            var loginRequest = new LoginRequest(_siteAvatarUsername, _siteAvatarPassword);

        OASISResult<Application.DTOs.Account.OASIS.AvatarAuthResponse> authResult = 
            await oasisAuthService.LoginAsync(loginRequest);

        if (authResult.IsError || authResult.Result is null || string.IsNullOrEmpty(authResult.Result.JwtToken))
        {
            throw new InvalidOperationException("Failed to authenticate with OASIS API");
        }

        return authResult.Result.JwtToken;
    }
}

