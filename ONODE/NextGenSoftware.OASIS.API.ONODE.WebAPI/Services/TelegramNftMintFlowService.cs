using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.ONODE.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Telegram;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services
{
    /// <summary>
    /// Handles the NFT mint wizard flow inside Telegram: /mint → image → title → symbol → description → wallet → confirm → mint.
    /// </summary>
    public class TelegramNftMintFlowService
    {
        private readonly ConcurrentDictionary<long, NftMintFlowState> _state = new ConcurrentDictionary<long, NftMintFlowState>();
        private readonly HttpClient _httpClient;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITokenMetadataByMintService _tokenMetadataByMintService;
        private readonly TelegramNftMintOptions _options;
        private readonly ILogger<TelegramNftMintFlowService> _logger;
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        private const int TelegramTimeoutSeconds = 20;
        private const int MaxRetries = 3;
        private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(1);
        private static readonly string DefaultMintingGifUrl = "https://media.tenor.com/sbH2izFboncAAAAM/pink-panther-walk.gif";

        public TelegramNftMintFlowService(
            IHttpClientFactory httpClientFactory,
            IOptions<TelegramNftMintOptions> options,
            ILogger<TelegramNftMintFlowService> logger,
            ITokenMetadataByMintService tokenMetadataByMintService = null)
        {
            _httpClientFactory = httpClientFactory;
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(TelegramTimeoutSeconds);
            _options = options?.Value ?? new TelegramNftMintOptions();
            _logger = logger;
            _tokenMetadataByMintService = tokenMetadataByMintService;
        }

        /// <summary>
        /// Process incoming Telegram update (message or photo). Updates state and returns the reply text to send.
        /// </summary>
        public async Task<string> HandleUpdateAsync(TelegramWebhookUpdate update)
        {
            if (update?.Message == null)
                return null;

            var chatId = update.Message.Chat.Id;
            var user = update.Message.From;
            var text = (update.Message.Text ?? "").Trim();
            var hasPhoto = update.Message.Photo != null && update.Message.Photo.Count > 0;
            var hasDocument = update.Message.Document != null && IsImageOrGifDocument(update.Message.Document);
            var hasAnimation = update.Message.Animation != null;

            // Commands
            if (text.Equals("/mint", StringComparison.OrdinalIgnoreCase) || text.Equals("/start mint", StringComparison.OrdinalIgnoreCase))
            {
                StartFlow(chatId, user?.Id);
                return "🔐 **OASIS login** (optional)\n\nSend your OASIS **username** to mint as your avatar, or type `skip` to mint as the bot.\n\nType /cancel anytime to cancel.";
            }

            if (text.Equals("/cancel", StringComparison.OrdinalIgnoreCase))
            {
                ClearFlow(chatId);
                return "Cancelled.";
            }

            if (!_state.TryGetValue(chatId, out var state))
                return "Use /mint to start creating an NFT.";

            // Step: OASIS login (username)
            if (state.Step == NftMintFlowStep.OasisLogin)
            {
                if (text.Equals("skip", StringComparison.OrdinalIgnoreCase))
                {
                    state.UserAvatarId = null;
                state.Step = NftMintFlowStep.Image;
                return "🎨 **Create NFT**\n\nSend a **photo** or **GIF**, or paste a **Solscan token URL** (e.g. https://solscan.io/token/...) or **mint address** to use that token's image and metadata.\n\nType `skip` for a placeholder.";
                }
                if (string.IsNullOrWhiteSpace(text))
                    return "Send your OASIS **username**, or type `skip` to mint as the bot.";
                state.OasisUsername = text.Trim();
                state.Step = NftMintFlowStep.OasisPassword;
                return "Send your OASIS **password** (you can type `skip` in the previous step next time to mint as the bot).";
            }

            // Step: OASIS password
            if (state.Step == NftMintFlowStep.OasisPassword)
            {
                if (string.IsNullOrWhiteSpace(text))
                    return "Please send your OASIS **password**.";
                var authResult = await Program.AvatarManager.AuthenticateAsync(
                    state.OasisUsername,
                    text,
                    "telegram-webhook",
                    AutoReplicationMode.UseGlobalDefaultInOASISDNA,
                    AutoFailOverMode.UseGlobalDefaultInOASISDNA,
                    AutoLoadBalanceMode.UseGlobalDefaultInOASISDNA,
                    false).ConfigureAwait(false);
                if (authResult.IsError || authResult.Result == null)
                {
                    _logger?.LogWarning("[TelegramNftMint] Auth failed for username {Username}", state.OasisUsername);
                    state.OasisUsername = null;
                    state.Step = NftMintFlowStep.OasisLogin;
                    return "❌ Invalid credentials. Send your OASIS **username** again, or type `skip` to mint as the bot.";
                }
                state.UserAvatarId = authResult.Result.Id;
                state.OasisUsername = null; // clear from state after use
                state.Step = NftMintFlowStep.Image;
                return "✅ Logged in as your OASIS avatar.\n\n🎨 Send a **photo** or **GIF**, or paste a **Solscan token URL** or **mint address** to use that token's image and metadata. Type `skip` for a placeholder.";
            }

            // Step: Image
            if (state.Step == NftMintFlowStep.Image)
            {
                if (text.Equals("skip", StringComparison.OrdinalIgnoreCase))
                {
                    state.ImageUrl = "https://via.placeholder.com/512/000000/FFFFFF?text=OASIS+NFT";
                    state.ImageContentType = "image/png";
                    state.Step = NftMintFlowStep.Title;
                    return "✅ Using placeholder image.\n\n**Title** for your NFT?";
                }

                // Memecoin import: Solscan token URL or paste mint address (in-process, no HTTP/base URL needed)
                var mint = TryExtractMintFromText(text);
                if (!string.IsNullOrEmpty(mint))
                {
                    _logger?.LogInformation("[TelegramNftMint] Memecoin import: extracted mint {Mint}", mint);
                    await SendMessageAsync(chatId, "🔍 **Looking up token metadata...**").ConfigureAwait(false);
                    if (_tokenMetadataByMintService == null)
                    {
                        _logger?.LogWarning("[TelegramNftMint] TokenMetadataByMintService not registered");
                        return "❌ Memecoin import not configured. Try a photo/GIF or type `skip`.";
                    }
                    var meta = await _tokenMetadataByMintService.GetMetadataAsync(mint).ConfigureAwait(false);
                    if (meta == null)
                    {
                        _logger?.LogWarning("[TelegramNftMint] GetMetadataAsync returned null for mint {Mint}", mint);
                        return "❌ Could not load token metadata. Check that the token has Metaplex metadata (e.g. pump.fun tokens). Try a photo/GIF or type `skip`.";
                    }
                    state.ImageUrl = string.IsNullOrEmpty(meta.Image) ? "https://via.placeholder.com/512/000000/FFFFFF?text=Token" : meta.Image;
                    state.ImageContentType = "image/png";
                    state.Title = (meta.Name?.Length > 32 ? meta.Name.Substring(0, 32) : meta.Name) ?? "Token NFT";
                    state.Symbol = (meta.Symbol?.Length > 10 ? meta.Symbol.Substring(0, 10) : meta.Symbol)?.ToUpperInvariant() ?? "TNFT";
                    state.Description = meta.Description ?? "";
                    state.JsonMetaDataUrl = meta.Uri;
                    state.Step = NftMintFlowStep.Wallet;
                    return $"✅ **Token loaded:** {state.Title} ({state.Symbol})\n\nSend your **Solana wallet address** to receive the NFT.";
                }

                if (hasPhoto)
                {
                    await SendMessageAsync(chatId, "📤 **Uploading image to IPFS...**").ConfigureAwait(false);
                    var (imageUrl, imageContentType) = await DownloadPhotoAndUploadToPinataAsync(update.Message.Photo, chatId).ConfigureAwait(false);
                    if (imageUrl == null)
                        return "❌ Failed to upload image. Try another photo or type `skip`.";
                    state.ImageUrl = imageUrl;
                    state.ImageContentType = imageContentType ?? "image/png";
                    state.Step = NftMintFlowStep.Title;
                    return "✅ Image uploaded.\n\n**Title** for your NFT?";
                }

                if (hasDocument || hasAnimation)
                {
                    var fileId = hasAnimation ? update.Message.Animation.FileId : update.Message.Document.FileId;
                    await SendMessageAsync(chatId, "📤 **Uploading GIF/image to IPFS...**").ConfigureAwait(false);
                    var (imageUrl, imageContentType) = await DownloadFileByFileIdAndUploadToPinataAsync(chatId, fileId).ConfigureAwait(false);
                    if (imageUrl == null)
                        return "❌ Failed to upload file. Try a photo/GIF or type `skip`.";
                    state.ImageUrl = imageUrl;
                    state.ImageContentType = imageContentType ?? "image/gif";
                    state.Step = NftMintFlowStep.Title;
                    return "✅ GIF/image uploaded.\n\n**Title** for your NFT?";
                }

                return "Send a **photo** or **GIF**, paste a **Solscan token URL** or **mint address**, or type `skip` for a placeholder.";
            }

            // Step: Title
            if (state.Step == NftMintFlowStep.Title)
            {
                if (string.IsNullOrWhiteSpace(text))
                    return "Please send the **title** for your NFT.";
                state.Title = text.Length > 32 ? text.Substring(0, 32) : text;
                state.Step = NftMintFlowStep.Symbol;
                return "**Symbol** (e.g. MYNFT, max 10 chars)?";
            }

            // Step: Symbol
            if (state.Step == NftMintFlowStep.Symbol)
            {
                if (string.IsNullOrWhiteSpace(text))
                    return "Please send the **symbol** (e.g. MYNFT).";
                state.Symbol = (text.Length > 10 ? text.Substring(0, 10) : text).ToUpperInvariant();
                state.Step = NftMintFlowStep.Description;
                return "**Description**? (optional – type `skip` to leave empty)";
            }

            // Step: Description
            if (state.Step == NftMintFlowStep.Description)
            {
                state.Description = text.Equals("skip", StringComparison.OrdinalIgnoreCase) ? "" : (text ?? "");
                state.Step = NftMintFlowStep.Wallet;
                return "Send your **Solana wallet address** to receive the NFT (e.g. 8bFhmkao9SJ6axVNcNRoeo85aNG45HP94oMtnQKGSUuz).";
            }

            // Step: Wallet
            if (state.Step == NftMintFlowStep.Wallet)
            {
                if (string.IsNullOrWhiteSpace(text) || text.Length < 32)
                    return "❌ Please send a valid Solana wallet address (32–44 characters).";
                state.SendToWalletAddress = text.Trim();
                state.Step = NftMintFlowStep.Confirm;
                await SendChatActionAsync(chatId, "typing").ConfigureAwait(false);
                return $"📋 **Confirm**\n\nTitle: {state.Title}\nSymbol: {state.Symbol}\nDescription: {(string.IsNullOrEmpty(state.Description) ? "(none)" : state.Description)}\nSend to: {state.SendToWalletAddress.Substring(0, 8)}...{state.SendToWalletAddress.Substring(state.SendToWalletAddress.Length - 4)}\n\nType **YES** to mint, or /cancel to cancel.";
            }

            // Step: Confirm
            if (state.Step == NftMintFlowStep.Confirm)
            {
                if (!text.Equals("YES", StringComparison.OrdinalIgnoreCase))
                    return "Type **YES** to mint or /cancel to cancel.";

                await SendMessageAsync(chatId, "📤 **Uploading metadata to IPFS...**").ConfigureAwait(false);
                await SendMintingMessageAsync(chatId).ConfigureAwait(false);
                var result = await MintNftAsync(state).ConfigureAwait(false);
                ClearFlow(chatId);
                return result;
            }

            return null;
        }

        /// <summary>Extract Solana mint from Solscan URL or raw mint address (base58, 32-44 chars).</summary>
        private static string TryExtractMintFromText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;
            text = text.Trim();
            // Solscan: https://solscan.io/token/MINT or https://solscan.io/#/token/MINT or with ?cluster=...
            var solscanMatch = Regex.Match(text, @"solscan\.io(?:/#?)?/token/([A-HJ-NP-Za-km-z1-9]{32,44})", RegexOptions.IgnoreCase);
            if (solscanMatch.Success)
                return solscanMatch.Groups[1].Value;
            // Any URL with /token/MINT (e.g. solscan or other explorers)
            var tokenPathMatch = Regex.Match(text, @"/token/([A-HJ-NP-Za-km-z1-9]{32,44})", RegexOptions.IgnoreCase);
            if (tokenPathMatch.Success)
                return tokenPathMatch.Groups[1].Value;
            // Raw mint: 32-44 base58 chars only (no spaces, no URL)
            if (text.Length >= 32 && text.Length <= 44 && Regex.IsMatch(text, @"^[A-HJ-NP-Za-km-z1-9]+$") && !text.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                return text;
            return null;
        }

        private void StartFlow(long chatId, long? userId)
        {
            _state[chatId] = new NftMintFlowState { ChatId = chatId, UserId = userId, Step = NftMintFlowStep.OasisLogin };
        }

        private void ClearFlow(long chatId)
        {
            _state.TryRemove(chatId, out _);
        }

        private async Task<(string url, string contentType)> DownloadPhotoAndUploadToPinataAsync(System.Collections.Generic.List<TelegramPhotoSize> photoSizes, long chatId)
        {
            if (string.IsNullOrEmpty(_options.BotToken))
            {
                _logger?.LogWarning("[TelegramNftMint] BotToken not configured");
                return (null, null);
            }

            // Largest photo
            var photo = photoSizes[photoSizes.Count - 1];
            var getFileUrl = $"https://api.telegram.org/bot{_options.BotToken}/getFile?file_id={Uri.EscapeDataString(photo.FileId)}";
            string filePath = null;
            for (var attempt = 1; attempt <= MaxRetries && filePath == null; attempt++)
            {
                try
                {
                    var getFileResp = await _httpClient.GetStringAsync(getFileUrl).ConfigureAwait(false);
                    var fileResp = JsonSerializer.Deserialize<TelegramFileResponse>(getFileResp, JsonOptions);
                    if (fileResp?.Result?.FilePath != null)
                        filePath = fileResp.Result.FilePath;
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "[TelegramNftMint] getFile attempt {Attempt} failed", attempt);
                    if (attempt < MaxRetries)
                        await Task.Delay(RetryDelay).ConfigureAwait(false);
                }
            }
            if (string.IsNullOrEmpty(filePath))
                return (null, null);

            var downloadUrl = $"https://api.telegram.org/file/bot{_options.BotToken}/{filePath}";
            byte[] imageBytes = null;
            for (var attempt = 1; attempt <= MaxRetries && imageBytes == null; attempt++)
            {
                try
                {
                    imageBytes = await _httpClient.GetByteArrayAsync(downloadUrl).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "[TelegramNftMint] download file attempt {Attempt} failed", attempt);
                    if (attempt < MaxRetries)
                        await Task.Delay(RetryDelay).ConfigureAwait(false);
                }
            }
            if (imageBytes == null || imageBytes.Length == 0)
                return (null, null);

            if (string.IsNullOrEmpty(_options.PinataJwt) && (string.IsNullOrEmpty(_options.PinataApiKey) || string.IsNullOrEmpty(_options.PinataSecretKey)))
            {
                _logger?.LogWarning("[TelegramNftMint] Pinata not configured (need PinataJwt or PinataApiKey+PinataSecretKey)");
                return (null, null);
            }

            var (contentType, ext) = GetImageContentTypeAndExtension(imageBytes);
            var fileName = $"nft_telegram_{chatId}_{DateTime.UtcNow.Ticks}.{ext}";
            var ipfsUrl = await UploadToPinataAsync(imageBytes, fileName, contentType).ConfigureAwait(false);
            return (ipfsUrl, contentType);
        }

        /// <summary>
        /// Detect image type from magic bytes so we upload with correct content-type and extension (phones often send JPEG).
        /// </summary>
        private static (string contentType, string ext) GetImageContentTypeAndExtension(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 12)
                return ("image/png", "png");
            if (bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
                return ("image/jpeg", "jpg");
            if (bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)
                return ("image/png", "png");
            if (bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46 &&
                bytes.Length >= 12 && bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50)
                return ("image/webp", "webp");
            if (bytes[0] == 0x47 && bytes[1] == 0x49 && bytes[2] == 0x46)
                return ("image/gif", "gif");
            return ("image/png", "png");
        }

        /// <summary>
        /// True if the document is an image or GIF we accept for NFT (by mime_type or file extension).
        /// </summary>
        private static bool IsImageOrGifDocument(TelegramDocument doc)
        {
            if (doc == null || string.IsNullOrEmpty(doc.FileId))
                return false;
            var mime = (doc.MimeType ?? "").ToLowerInvariant();
            var name = (doc.FileName ?? "").ToLowerInvariant();
            if (mime.StartsWith("image/"))
                return true;
            if (name.EndsWith(".gif") || name.EndsWith(".png") || name.EndsWith(".jpg") || name.EndsWith(".jpeg") || name.EndsWith(".webp"))
                return true;
            return false;
        }

        /// <summary>
        /// Download a file by Telegram file_id (e.g. document or animation/GIF) and upload to Pinata.
        /// </summary>
        private async Task<(string url, string contentType)> DownloadFileByFileIdAndUploadToPinataAsync(long chatId, string fileId)
        {
            if (string.IsNullOrEmpty(_options.BotToken))
                return (null, null);
            var getFileUrl = $"https://api.telegram.org/bot{_options.BotToken}/getFile?file_id={Uri.EscapeDataString(fileId)}";
            string filePath = null;
            for (var attempt = 1; attempt <= MaxRetries && filePath == null; attempt++)
            {
                try
                {
                    var getFileResp = await _httpClient.GetStringAsync(getFileUrl).ConfigureAwait(false);
                    var fileResp = JsonSerializer.Deserialize<TelegramFileResponse>(getFileResp, JsonOptions);
                    if (fileResp?.Result?.FilePath != null)
                        filePath = fileResp.Result.FilePath;
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "[TelegramNftMint] getFile (document/animation) attempt {Attempt} failed", attempt);
                    if (attempt < MaxRetries)
                        await Task.Delay(RetryDelay).ConfigureAwait(false);
                }
            }
            if (string.IsNullOrEmpty(filePath))
                return (null, null);

            var downloadUrl = $"https://api.telegram.org/file/bot{_options.BotToken}/{filePath}";
            byte[] fileBytes = null;
            for (var attempt = 1; attempt <= MaxRetries && fileBytes == null; attempt++)
            {
                try
                {
                    fileBytes = await _httpClient.GetByteArrayAsync(downloadUrl).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "[TelegramNftMint] download (document/animation) attempt {Attempt} failed", attempt);
                    if (attempt < MaxRetries)
                        await Task.Delay(RetryDelay).ConfigureAwait(false);
                }
            }
            if (fileBytes == null || fileBytes.Length == 0)
                return (null, null);

            if (string.IsNullOrEmpty(_options.PinataJwt) && (string.IsNullOrEmpty(_options.PinataApiKey) || string.IsNullOrEmpty(_options.PinataSecretKey)))
            {
                _logger?.LogWarning("[TelegramNftMint] Pinata not configured");
                return (null, null);
            }

            var (contentType, ext) = GetImageContentTypeAndExtension(fileBytes);
            var fileName = $"nft_telegram_{chatId}_{DateTime.UtcNow.Ticks}.{ext}";
            var ipfsUrl = await UploadToPinataAsync(fileBytes, fileName, contentType).ConfigureAwait(false);
            return (ipfsUrl, contentType);
        }

        /// <summary>
        /// Upload Metaplex-style NFT metadata JSON to Pinata so Solana metadata URI points to name, symbol, description, image.
        /// Uses full Metaplex schema (name, symbol, description, image, attributes, properties with category) so Solscan/wallets display correctly.
        /// </summary>
        private async Task<string> UploadMetadataJsonToPinataAsync(string name, string symbol, string description, string imageUrl, string imageContentType = "image/png")
        {
            var image = imageUrl ?? "https://via.placeholder.com/512/000000/FFFFFF?text=OASIS+NFT";
            var metadata = new
            {
                name = name ?? "Telegram NFT",
                symbol = symbol ?? "TNFT",
                description = description ?? "",
                image = image,
                external_url = "",
                attributes = Array.Empty<object>(),
                properties = new
                {
                    category = "image",
                    files = new[]
                    {
                        new { uri = image, type = imageContentType ?? "image/png" }
                    }
                }
            };
            var json = JsonSerializer.Serialize(metadata);
            var bytes = Encoding.UTF8.GetBytes(json);
            return await UploadToPinataAsync(bytes, "metadata.json", "application/json").ConfigureAwait(false);
        }

        private async Task<string> UploadToPinataAsync(byte[] fileBytes, string fileName, string contentType = "image/png")
        {
            using var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            content.Add(fileContent, "file", fileName);

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.pinata.cloud/pinning/pinFileToIPFS") { Content = content };
            if (!string.IsNullOrEmpty(_options.PinataJwt))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.PinataJwt);
            else
            {
                request.Headers.Add("pinata_api_key", _options.PinataApiKey ?? "");
                request.Headers.Add("pinata_secret_api_key", _options.PinataSecretKey ?? "");
            }

            using var pinataClient = _httpClientFactory.CreateClient("Pinata");
            var response = await pinataClient.SendAsync(request).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                _logger?.LogWarning("[TelegramNftMint] Pinata upload failed: {Body}", body);
                return null;
            }

            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("IpfsHash", out var hashEl))
            {
                var hash = hashEl.GetString();
                return $"https://gateway.pinata.cloud/ipfs/{hash}";
            }
            return null;
        }

        private async Task<string> MintNftAsync(NftMintFlowState state)
        {
            Guid mintingAvatarId;
            if (state.UserAvatarId.HasValue && state.UserAvatarId.Value != Guid.Empty)
                mintingAvatarId = state.UserAvatarId.Value;
            else if (!string.IsNullOrEmpty(_options.BotAvatarId) && Guid.TryParse(_options.BotAvatarId, out var botAvatarId))
                mintingAvatarId = botAvatarId;
            else
            {
                _logger?.LogWarning("[TelegramNftMint] No user avatar and BotAvatarId not configured or invalid");
                return "❌ Mint failed: no avatar configured (log in with OASIS or set BotAvatarId).";
            }

            var title = state.Title ?? "Telegram NFT";
            var symbol = state.Symbol ?? "TNFT";
            var description = state.Description ?? "";
            var imageUrl = state.ImageUrl ?? "https://via.placeholder.com/512/000000/FFFFFF?text=OASIS+NFT";

            var imageContentType = state.ImageContentType ?? "image/png";
            string jsonMetaDataUrl;
            if (!string.IsNullOrEmpty(state.JsonMetaDataUrl))
            {
                jsonMetaDataUrl = state.JsonMetaDataUrl;
            }
            else
            {
                jsonMetaDataUrl = await UploadMetadataJsonToPinataAsync(title, symbol, description, imageUrl, imageContentType).ConfigureAwait(false);
                if (string.IsNullOrEmpty(jsonMetaDataUrl))
                {
                    _logger?.LogWarning("[TelegramNftMint] Metadata JSON upload to Pinata failed");
                    return "❌ Mint failed: could not upload metadata to IPFS. Try again.";
                }
            }

            var request = new MintWeb4NFTRequest
            {
                MintedByAvatarId = mintingAvatarId,
                Title = title,
                Symbol = symbol,
                Description = description,
                ImageUrl = imageUrl,
                JSONMetaDataURL = jsonMetaDataUrl,
                SendToAddressAfterMinting = state.SendToWalletAddress,
                NumberToMint = 1,
                OnChainProvider = new EnumValue<ProviderType>(ProviderType.SolanaOASIS),
                OffChainProvider = new EnumValue<ProviderType>(ProviderType.MongoDBOASIS),
                NFTOffChainMetaType = new EnumValue<NFTOffChainMetaType>(NFTOffChainMetaType.ExternalJSONURL),
                NFTStandardType = new EnumValue<NFTStandardType>(NFTStandardType.SPL),
                StoreNFTMetaDataOnChain = false,
                WaitTillNFTMinted = true,
                WaitTillNFTSent = true,
                MetaData = state.MetaData
            };

            try
            {
                var nftManager = new NFTManager(mintingAvatarId);
                var result = await nftManager.MintNftAsync(request, false, ResponseFormatType.SimpleText).ConfigureAwait(false);

                if (result.IsError)
                {
                    _logger?.LogWarning("[TelegramNftMint] Mint failed: {Message}", result.Message);
                    return $"❌ Mint failed: {result.Message}";
                }

                var web3 = result.Result?.Web3NFTs?.Count > 0 ? result.Result.Web3NFTs[0] : null;
                var txHash = web3?.MintTransactionHash ?? "";
                var nftAddress = web3?.NFTTokenAddress ?? "";
                var cluster = string.IsNullOrEmpty(_options.SolanaCluster) ? "devnet" : _options.SolanaCluster;
                var txLink = $"https://solscan.io/tx/{txHash}?cluster={cluster}";
                var nftLink = $"https://solscan.io/account/{nftAddress}?cluster={cluster}";
                return $"✅ **NFT minted!**\n\n🔗 [View transaction]({txLink})\n📍 [View NFT]({nftLink})\n\nCheck your Solana wallet.";
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[TelegramNftMint] Mint exception");
                return $"❌ Mint failed: {ex.Message}";
            }
        }

        /// <summary>
        /// Show "typing..." in the chat so the user sees loading feedback.
        /// </summary>
        private async Task SendChatActionAsync(long chatId, string action)
        {
            if (string.IsNullOrEmpty(_options.BotToken))
                return;
            var url = $"https://api.telegram.org/bot{_options.BotToken}/sendChatAction";
            var payload = new { chat_id = chatId, action };
            var json = JsonSerializer.Serialize(payload);
            try
            {
                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                await _httpClient.PostAsync(url, content).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger?.LogDebug(ex, "[TelegramNftMint] SendChatAction failed");
            }
        }

        /// <summary>
        /// Send "Minting..." message with Pink Panther (or configured) GIF before the actual mint.
        /// </summary>
        private async Task SendMintingMessageAsync(long chatId)
        {
            if (string.IsNullOrEmpty(_options.BotToken))
                return;
            var gifUrl = !string.IsNullOrEmpty(_options.MintingGifUrl) ? _options.MintingGifUrl : DefaultMintingGifUrl;
            var url = $"https://api.telegram.org/bot{_options.BotToken}/sendAnimation";
            var payload = new { chat_id = chatId, animation = gifUrl, caption = "🪙 **Minting your NFT...**\n\nOne moment please.", parse_mode = "Markdown" };
            var json = JsonSerializer.Serialize(payload);
            try
            {
                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                await _httpClient.PostAsync(url, content).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "[TelegramNftMint] SendMintingMessage failed");
            }
        }

        /// <summary>
        /// Send a text message to a Telegram chat (used by webhook controller after processing).
        /// Retries up to MaxRetries on timeout/network failure so transient slowness doesn't drop replies.
        /// </summary>
        public async Task<bool> SendMessageAsync(long chatId, string text)
        {
            if (string.IsNullOrEmpty(_options.BotToken))
                return false;
            var url = $"https://api.telegram.org/bot{_options.BotToken}/sendMessage";
            var payload = new { chat_id = chatId, text, parse_mode = "Markdown" };
            var json = JsonSerializer.Serialize(payload);
            for (var attempt = 1; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    using var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var response = await _httpClient.PostAsync(url, content).ConfigureAwait(false);
                    if (response.IsSuccessStatusCode)
                        return true;
                    _logger?.LogWarning("[TelegramNftMint] SendMessage attempt {Attempt} returned {StatusCode}", attempt, response.StatusCode);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "[TelegramNftMint] SendMessage attempt {Attempt} failed", attempt);
                    if (attempt == MaxRetries)
                        return false;
                }
                await Task.Delay(RetryDelay).ConfigureAwait(false);
            }
            return false;
        }
    }
}
