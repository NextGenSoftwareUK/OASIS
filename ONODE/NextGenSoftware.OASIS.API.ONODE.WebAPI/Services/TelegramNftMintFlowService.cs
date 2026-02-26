using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Saints;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Telegram;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.OASISBootLoader;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services
{
    /// <summary>
    /// Handles the NFT mint wizard flow inside Telegram: /mint → image → title → symbol → description → wallet → confirm → mint.
    /// State is keyed by (chatId, userId) so multiple users in a group can each run their own flow. In groups we never ask for password (DM only).
    /// </summary>
    public class TelegramNftMintFlowService
    {
        private readonly ConcurrentDictionary<(long ChatId, long UserId), NftMintFlowState> _state = new ConcurrentDictionary<(long, long), NftMintFlowState>();
        /// <summary>Telegram user id → OASIS avatar id. Set when user logs in via DM; used so they can mint as their avatar in groups without typing password.</summary>
        private readonly ConcurrentDictionary<long, Guid> _userIdToAvatarId = new ConcurrentDictionary<long, Guid>();
        private readonly HttpClient _httpClient;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITokenMetadataByMintService _tokenMetadataByMintService;
        private readonly TelegramNftMintOptions _options;
        private readonly ILogger<TelegramNftMintFlowService> _logger;
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        private const int TelegramTimeoutSeconds = 20;
        private const int MaxRetries = 3;
        private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(1);
        private const string BuiltInMintingVideoPath = "/minting/witness-the-jpeg-miracle.mp4";
        private const string SaintsWelcomePath = "/saints/welcome.mp4";
        private const string SaintsConfirmPath = "/saints/confirm.png";
        private const string SaintsJoinSaintsPath = "/saints/ascended-halls.mp4";
        // Ascension-themed fallback when built-in video unreachable (e.g. localhost). To use your own GIF (e.g. one you found in Telegram): upload it to a public host (e.g. Pinata), then set MintingGifUrl in TelegramNftMint config.
        private static readonly string FallbackMintingGifUrl = "https://media.tenor.com/2q7RCZAr-4gAAAAM/ascending-energy.gif";

        private readonly ISolanaSplTokenBalanceService _splTokenBalanceService;
        private readonly ISaintMintRecordService _saintMintRecordService;
        private readonly ITopTokenHoldersService _topTokenHoldersService;
        private readonly IServiceProvider _serviceProvider;
        private readonly ITelegramFlowStateStore _flowStateStore;
        private readonly IHallVerificationNftSentStore _hallVerificationNftSentStore;
        private readonly IHallVerificationNftSenderService _hallVerificationNftSender;
        private readonly HallVerificationNftOptions _hallVerificationNftOptions;
        private string _dropSaintMetadataUrlCache;
        private readonly ConcurrentDictionary<(long ChatId, long UserId), DropHoldersFlowState> _dropState = new ConcurrentDictionary<(long, long), DropHoldersFlowState>();

        public TelegramNftMintFlowService(
            IHttpClientFactory httpClientFactory,
            IOptions<TelegramNftMintOptions> options,
            ILogger<TelegramNftMintFlowService> logger,
            ITokenMetadataByMintService tokenMetadataByMintService = null,
            ISolanaSplTokenBalanceService splTokenBalanceService = null,
            ISaintMintRecordService saintMintRecordService = null,
            ITopTokenHoldersService topTokenHoldersService = null,
            IServiceProvider serviceProvider = null,
            ITelegramFlowStateStore flowStateStore = null,
            IHallVerificationNftSentStore hallVerificationNftSentStore = null,
            IHallVerificationNftSenderService hallVerificationNftSender = null,
            IOptions<HallVerificationNftOptions> hallVerificationNftOptions = null)
        {
            _httpClientFactory = httpClientFactory;
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(TelegramTimeoutSeconds);
            _options = options?.Value ?? new TelegramNftMintOptions();
            _logger = logger;
            _tokenMetadataByMintService = tokenMetadataByMintService;
            _splTokenBalanceService = splTokenBalanceService;
            _saintMintRecordService = saintMintRecordService;
            _topTokenHoldersService = topTokenHoldersService;
            _serviceProvider = serviceProvider;
            _flowStateStore = flowStateStore;
            _hallVerificationNftSentStore = hallVerificationNftSentStore;
            _hallVerificationNftSender = hallVerificationNftSender;
            _hallVerificationNftOptions = hallVerificationNftOptions?.Value;
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
            var chat = update.Message.Chat;
            var text = (update.Message.Text ?? "").Trim();
            var hasPhoto = update.Message.Photo != null && update.Message.Photo.Count > 0;
            var hasDocument = update.Message.Document != null && IsImageOrGifDocument(update.Message.Document);
            var hasAnimation = update.Message.Animation != null;

            var stateKey = StateKey(chatId, user?.Id);
            var isGroup = IsGroupChat(chat);

            // When someone sends a message in the SAINTS group: if they're in top 20 and we haven't sent the Hall verification NFT yet, send it and DM them.
            if (isGroup && user?.Id != null && _options.SaintsGroupChatId.HasValue && chatId == _options.SaintsGroupChatId.Value)
            {
                var groupReply = await TrySendHallVerificationNftToGroupMemberAsync(user.Id).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(groupReply))
                    return groupReply;
            }

            // Load persisted state from store so multi-instance (e.g. ECS) can resume the same conversation
            if (_flowStateStore != null && _flowStateStore.IsConfigured)
            {
                if (!_dropState.ContainsKey(stateKey))
                {
                    var storedDrop = await _flowStateStore.GetDropStateAsync(chatId, user?.Id).ConfigureAwait(false);
                    if (storedDrop != null)
                        _dropState[stateKey] = storedDrop;
                }
                if (!_state.ContainsKey(stateKey))
                {
                    var storedMint = await _flowStateStore.GetMintStateAsync(chatId, user?.Id).ConfigureAwait(false);
                    if (storedMint != null)
                        _state[stateKey] = storedMint;
                }
            }

            // Normalize command: Telegram may send /command@BotUsername (e.g. /join_saints@SAINT_Bot) when used from menu or in some chats
            var commandText = NormalizeCommand(text);

            // Commands
            if (commandText.Equals("/mint", StringComparison.OrdinalIgnoreCase) || commandText.Equals("/start mint", StringComparison.OrdinalIgnoreCase))
            {
                await StartFlowAsync(chatId, user?.Id).ConfigureAwait(false);
                if (_state.TryGetValue(stateKey, out var newState))
                {
                    newState.UserAvatarId = null;
                    newState.Step = NftMintFlowStep.Image;
                }
                var welcomeLine = _options?.SaintsMintWelcomeLine?.Trim();
                var welcomeMediaUrl = GetWelcomeMediaUrl(_options);
                if (!string.IsNullOrEmpty(welcomeMediaUrl))
                    await SendMediaAsync(chatId, welcomeMediaUrl, welcomeLine).ConfigureAwait(false);
                var imagePrompt = ImageStepPrompt(isGroup) + "\n\nType `skip` for a placeholder. Type /cancel anytime to cancel.";
                if (!string.IsNullOrEmpty(welcomeLine) && string.IsNullOrEmpty(welcomeMediaUrl))
                    imagePrompt = welcomeLine + "\n\n" + imagePrompt;
                return imagePrompt;
            }

            if (commandText.Equals("/logout", StringComparison.OrdinalIgnoreCase))
            {
                if (user?.Id != null)
                    _userIdToAvatarId.TryRemove(user.Id, out _);
                await ClearFlowAsync(chatId, user?.Id).ConfigureAwait(false);
                return "Logged out. Use /mint to start again.";
            }

            // /join_saints: send secret group invite link only to users who have minted (and only in private chat)
            // Match /join_saints, /join_saint (typo), /joinsaints, ／join_saints (fullwidth slash), /join_saints@BotName
            var isJoinSaintsCommand = commandText.StartsWith("/join_saints", StringComparison.OrdinalIgnoreCase)
                || commandText.StartsWith("/join_saint", StringComparison.OrdinalIgnoreCase)
                || commandText.StartsWith("/joinsaints", StringComparison.OrdinalIgnoreCase);
            if (isJoinSaintsCommand)
            {
                try
                {
                    var link = _options?.SaintsSecretGroupInviteLink?.Trim();
                    if (string.IsNullOrEmpty(link))
                        return "The SAINTS secret group is not configured yet. Stay tuned.";
                    if (isGroup)
                        return "To get your invite link, open me in a **private chat** (tap my name → Start) and send /join_saints again.";
                    if (user?.Id == null)
                        return "I couldn't identify you. Try again in a private chat.";
                    var hasMinted = _saintMintRecordService != null && await _saintMintRecordService.HasMintedAsync(user.Id).ConfigureAwait(false);
                    if (!hasMinted)
                        return "🔒 Mint a SAINT NFT first with /mint to unlock the secret group. Once you've minted, send /join_saints here to get your link.";
                    var joinSuccessMediaUrl = GetJoinSaintsSuccessMediaUrl(_options);
                    if (!string.IsNullOrEmpty(joinSuccessMediaUrl))
                        await SendMediaAsync(chatId, joinSuccessMediaUrl, _options?.SaintsJoinSaintsLine?.Trim()).ConfigureAwait(false);
                    return "🔐 **You're in.** Use this link to join the SAINTS secret group. Don't share it.\n\n" + link;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "[TelegramNftMint] /join_saints failed for user {UserId}", user?.Id);
                    return "Could not check mint status right now. Please try again in a moment.";
                }
            }

            // --- /ordain = top holders; /baptise = newest investors; /dropholders = alias for ordain ---
            if (commandText.Equals("/ordain", StringComparison.OrdinalIgnoreCase) || commandText.Equals("/dropholders", StringComparison.OrdinalIgnoreCase))
            {
                await StartDropFlowAsync(chatId, user?.Id, recipientMode: 1).ConfigureAwait(false);
                return "📋 **Ordain**\n\nI'll send **1 NFT** per wallet to the **top holders** of the token you paste.\n\nDrop a link to the token mint (e.g. pump.fun/coin/… or Solscan token URL). Paste the token whose top holders you want to bless.\n\nExample: `9VTxmdpCD9dKsJZaBccHsBwYcebGV6iCZtKA8et5pump` or https://pump.fun/coin/…\n\n/cancel to cancel.";
            }
            if (commandText.Equals("/baptise", StringComparison.OrdinalIgnoreCase))
            {
                await StartDropFlowAsync(chatId, user?.Id, recipientMode: 2).ConfigureAwait(false);
                return "📋 **Baptise**\n\nI'll send **1 NFT** per wallet to the **newest investors** of the token you paste.\n\nDrop a link to the token mint (e.g. pump.fun/coin/… or Solscan token URL). Paste the token whose newest investors you want to bless.\n\nExample: `9VTxmdpCD9dKsJZaBccHsBwYcebGV6iCZtKA8et5pump` or https://pump.fun/coin/…\n\n/cancel to cancel.";
            }

            if (commandText.Equals("/cancel", StringComparison.OrdinalIgnoreCase))
            {
                if (_dropState.TryGetValue(stateKey, out _))
                {
                    await ClearDropFlowAsync(chatId, user?.Id).ConfigureAwait(false);
                    return "Cancelled.";
                }
                await ClearFlowAsync(chatId, user?.Id).ConfigureAwait(false);
                return "Cancelled.";
            }

            if (_dropState.TryGetValue(stateKey, out var dropState))
            {
                if (dropState.Step == DropHoldersFlowStep.CustomImage && (hasPhoto || hasDocument || hasAnimation))
                {
                    await SendMessageAsync(chatId, "📤 **Uploading image to IPFS...**").ConfigureAwait(false);
                    string imageUrl = null;
                    string imageContentType = "image/png";
                    if (hasPhoto)
                    {
                        var (url, ct) = await DownloadPhotoAndUploadToPinataAsync(update.Message.Photo, chatId).ConfigureAwait(false);
                        imageUrl = url;
                        imageContentType = ct ?? "image/png";
                    }
                    else
                    {
                        var fileId = hasAnimation ? update.Message.Animation.FileId : update.Message.Document.FileId;
                        var (url, ct) = await DownloadFileByFileIdAndUploadToPinataAsync(chatId, fileId).ConfigureAwait(false);
                        imageUrl = url;
                        imageContentType = ct ?? "image/gif";
                    }
                    if (string.IsNullOrEmpty(imageUrl))
                        return "❌ Failed to upload image. Try another photo or GIF. Or /cancel.";
                    var groupLink = dropState.UserProvidedGroupLink ?? _options?.OrdainNftTelegramGroupLink;
                    var jsonUrl = await UploadMetadataJsonToPinataAsync("Saint", "SAINT", "Custom ordained.", imageUrl, imageContentType, groupLink, GetOrdainSecretMessage(), GetBaptiseX402PaymentEndpoint(), _options?.X402RevenueModel, _options?.X402TreasuryWallet).ConfigureAwait(false);
                    if (string.IsNullOrEmpty(jsonUrl))
                        return "❌ Failed to create metadata. Try again or /cancel.";
                    dropState.CustomMetadataUrl = jsonUrl;
                    dropState.Step = DropHoldersFlowStep.Confirm;
                    await PersistDropStateAsync(chatId, user?.Id, dropState).ConfigureAwait(false);
                    var recipientLinePhoto = dropState.RecipientMode == 2
                        ? $"**newest {dropState.TopN} investors**"
                        : $"**top {dropState.TopN} holders**";
                    return $"📋 **Confirm**\n\nSplendid. I will mint **1 NFT** per wallet and send to {recipientLinePhoto} of {dropState.CommunityTokenName}.\n\nTreasury will pay the mint fee 🙏\n\nType **YES** to start, or /cancel to cancel.";
                }
                var dropReply = await HandleDropFlowStepAsync(dropState, text, chatId, user?.Id, stateKey).ConfigureAwait(false);
                if (dropReply != null)
                    return dropReply;
            }

            if (!_state.TryGetValue(stateKey, out var state))
                return "Use /mint to create an NFT, /ordain to bless top token holders, or /baptise to bless newest investors.";

            // Step: OASIS login (username)
            if (state.Step == NftMintFlowStep.OasisLogin)
            {
                if (text.Equals("skip", StringComparison.OrdinalIgnoreCase))
                {
                    state.UserAvatarId = null;
                    state.Step = NftMintFlowStep.Image;
                    return ImageStepPrompt(isGroup) + "\n\nType `skip` for a placeholder.";
                }
                if (string.IsNullOrWhiteSpace(text))
                    return "Send your OASIS **username**, or type `skip` to mint as the bot.";
                // In groups we never ask for password (everyone can see messages)
                if (isGroup)
                    return "To mint as your OASIS avatar, open me in a **private chat** (tap my name → Start) and log in there. Here, type `skip` to mint as the bot.";
                state.OasisUsername = text.Trim();
                state.Step = NftMintFlowStep.OasisPassword;
                return "Send your OASIS **password** (you can type `skip` in the previous step next time to mint as the bot).";
            }

            // Step: OASIS password (only in private chat; in group we should not be here)
            if (state.Step == NftMintFlowStep.OasisPassword)
            {
                if (isGroup)
                    return "For security, don't send your password here. Open me in a **private chat** to log in. Here, type /cancel then `skip` to mint as the bot.";
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
                // Cache so they can /mint in groups as their avatar without typing password again
                if (user?.Id != null)
                    _userIdToAvatarId[user.Id] = authResult.Result.Id;
                return "✅ Logged in as your OASIS avatar. You can now use /mint in groups as your avatar too (no password needed there).\n\n" + ImageStepPrompt(isGroup) + "\n\nType `skip` for a placeholder.";
            }

            // Step: Image
            if (state.Step == NftMintFlowStep.Image)
            {
                if (text.Equals("skip", StringComparison.OrdinalIgnoreCase))
                {
                    state.ImageUrl = "https://via.placeholder.com/512/000000/FFFFFF?text=OASIS+NFT";
                    state.ImageContentType = "image/png";
                    state.Step = NftMintFlowStep.Title;
                    var doneLine = _options?.SaintsMintImageDoneLine?.Trim();
                    if (!string.IsNullOrEmpty(_options?.SaintsMintImageDoneGifUrl))
                        await SendAnimationAsync(chatId, _options.SaintsMintImageDoneGifUrl, doneLine).ConfigureAwait(false);
                    var titlePrompt = !string.IsNullOrWhiteSpace(_options?.SaintsMintTitlePrompt) ? _options.SaintsMintTitlePrompt.Trim() : "**Title** for your NFT?";
                    return string.IsNullOrEmpty(doneLine) ? "✅ Using placeholder image.\n\n" + titlePrompt : doneLine + "\n\n" + titlePrompt;
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
                    var doneLine = _options?.SaintsMintImageDoneLine?.Trim();
                    if (!string.IsNullOrEmpty(_options?.SaintsMintImageDoneGifUrl))
                        await SendAnimationAsync(chatId, _options.SaintsMintImageDoneGifUrl, doneLine).ConfigureAwait(false);
                    var titlePrompt = !string.IsNullOrWhiteSpace(_options?.SaintsMintTitlePrompt) ? _options.SaintsMintTitlePrompt.Trim() : "**Title** for your NFT?";
                    return string.IsNullOrEmpty(doneLine) ? "✅ Image uploaded.\n\n" + titlePrompt : doneLine + "\n\n" + titlePrompt;
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
                    var doneLine = _options?.SaintsMintImageDoneLine?.Trim();
                    if (!string.IsNullOrEmpty(_options?.SaintsMintImageDoneGifUrl))
                        await SendAnimationAsync(chatId, _options.SaintsMintImageDoneGifUrl, doneLine).ConfigureAwait(false);
                    var titlePrompt = !string.IsNullOrWhiteSpace(_options?.SaintsMintTitlePrompt) ? _options.SaintsMintTitlePrompt.Trim() : "**Title** for your NFT?";
                    return string.IsNullOrEmpty(doneLine) ? "✅ GIF/image uploaded.\n\n" + titlePrompt : doneLine + "\n\n" + titlePrompt;
                }

                return ImageStepPrompt(isGroup) + "\n\nOr type `skip` for a placeholder.";
            }

            // Step: Title
            if (state.Step == NftMintFlowStep.Title)
            {
                if (string.IsNullOrWhiteSpace(text))
                    return "Please send the **title** for your NFT.";
                state.Title = text.Length > 32 ? text.Substring(0, 32) : text;
                state.Step = NftMintFlowStep.Symbol;
                return !string.IsNullOrWhiteSpace(_options?.SaintsMintSymbolPrompt) ? _options.SaintsMintSymbolPrompt.Trim() : "**Symbol** (e.g. MYNFT, max 10 chars)?";
            }

            // Step: Symbol
            if (state.Step == NftMintFlowStep.Symbol)
            {
                if (string.IsNullOrWhiteSpace(text))
                    return "Please send the **symbol** (e.g. MYNFT).";
                state.Symbol = (text.Length > 10 ? text.Substring(0, 10) : text).ToUpperInvariant();
                state.Step = NftMintFlowStep.Description;
                return !string.IsNullOrWhiteSpace(_options?.SaintsMintDescriptionPrompt) ? _options.SaintsMintDescriptionPrompt.Trim() : "**Description**? (optional – type `skip` to leave empty)";
            }

            // Step: Description
            if (state.Step == NftMintFlowStep.Description)
            {
                state.Description = text.Equals("skip", StringComparison.OrdinalIgnoreCase) ? "" : (text ?? "");
                state.Step = NftMintFlowStep.Wallet;
                return !string.IsNullOrWhiteSpace(_options?.SaintsMintWalletPrompt) ? _options.SaintsMintWalletPrompt.Trim() : "Send your **Solana wallet address** to receive the NFT (e.g. 8bFhmkao9SJ6axVNcNRoeo85aNG45HP94oMtnQKGSUuz).";
            }

            // Step: Wallet
            if (state.Step == NftMintFlowStep.Wallet)
            {
                if (string.IsNullOrWhiteSpace(text) || text.Length < 32)
                    return "❌ Please send a valid Solana wallet address (32–44 characters).";
                var walletAddress = text.Trim();

                // Token gate: require minimum balance of gating token in the receive wallet
                if (!string.IsNullOrWhiteSpace(_options.SaintTokenMint) && _options.SaintTokenRequiredBalance > 0 && _splTokenBalanceService != null)
                {
                    // Cluster is determined by OASIS_DNA / provider config; balance check uses mainnet RPC when configured.
                    var balanceResult = await _splTokenBalanceService.GetBalanceAsync(walletAddress, _options.SaintTokenMint.Trim()).ConfigureAwait(false);
                    var balance = balanceResult.Result;
                    var required = _options.SaintTokenRequiredBalance;
                    var passed = !balanceResult.IsError && balance >= required;
                    _logger?.LogInformation("[TelegramNftMint] Token gate: wallet {Wallet}, balance {Balance}, required {Required}, passed {Passed}, IsError {IsError}", walletAddress, balance, required, passed, balanceResult.IsError);

                    if (balanceResult.IsError)
                    {
                        _logger?.LogWarning("[TelegramNftMint] Token balance check failed for wallet {Wallet}: {Message}", walletAddress, balanceResult.Message);
                        return "❌ Could not verify token balance. Please try again or use another wallet.";
                    }
                    if (balance < required)
                    {
                        const string buySaintUrl = "https://pump.fun/coin/9VTxmdpCD9dKsJZaBccHsBwYcebGV6iCZtKA8et5pump";
                        var msg = !string.IsNullOrWhiteSpace(_options.SaintTokenInsufficientMessage)
                            ? _options.SaintTokenInsufficientMessage
                            : $"🙇 You need at least **{required:N0}** SAINT in this wallet to mint. Your balance: **{balance:N0}**. [Buy SAINT on pump.fun ✨]({buySaintUrl})";
                        return msg;
                    }
                }

                state.SendToWalletAddress = walletAddress;
                state.Step = NftMintFlowStep.Confirm;
                var confirmLine = _options?.SaintsMintConfirmLine?.Trim();
                var confirmMediaUrl = GetConfirmMediaUrl(_options);
                if (!string.IsNullOrEmpty(confirmMediaUrl))
                    await SendMediaAsync(chatId, confirmMediaUrl, confirmLine).ConfigureAwait(false);
                await SendChatActionAsync(chatId, "typing").ConfigureAwait(false);
                var confirmBlock = $"📋 **Confirm**\n\nTitle: {state.Title}\nSymbol: {state.Symbol}\nDescription: {(string.IsNullOrEmpty(state.Description) ? "(none)" : state.Description)}\nSend to: {state.SendToWalletAddress.Substring(0, 8)}...{state.SendToWalletAddress.Substring(state.SendToWalletAddress.Length - 4)}\n\nType **YES** to mint, or /cancel to cancel.";
                return !string.IsNullOrEmpty(confirmMediaUrl) ? confirmBlock : (string.IsNullOrEmpty(confirmLine) ? confirmBlock : confirmLine + "\n\n" + confirmBlock);
            }

            // Step: Confirm
            if (state.Step == NftMintFlowStep.Confirm)
            {
                if (!text.Equals("YES", StringComparison.OrdinalIgnoreCase))
                    return "Type **YES** to mint or /cancel to cancel.";

                await SendMessageAsync(chatId, "📤 **Uploading metadata to IPFS...**").ConfigureAwait(false);
                await SendMintingMessageAsync(chatId).ConfigureAwait(false);
                var result = await MintNftAsync(state, user?.Id).ConfigureAwait(false);
                await ClearFlowAsync(chatId, user?.Id).ConfigureAwait(false);
                return result;
            }

            return null;
        }

        /// <summary>Extract IPFS hash from a URL like https://gateway.pinata.cloud/ipfs/Qm... or https://ipfs.io/ipfs/Qm...</summary>
        private static bool TryGetIpfsHash(string url, out string hash)
        {
            hash = null;
            if (string.IsNullOrWhiteSpace(url)) return false;
            var match = Regex.Match(url, @"/ipfs/(Qm[A-HJ-NP-Za-km-z1-9]+)");
            if (match.Success)
            {
                hash = match.Groups[1].Value;
                return true;
            }
            return false;
        }

        /// <summary>Extract Solana mint from Solscan, pump.fun, or raw mint address (base58, 32-44 chars).</summary>
        private static string TryExtractMintFromText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;
            text = text.Trim();
            // Solscan: https://solscan.io/token/MINT or https://solscan.io/#/token/MINT or with ?cluster=...
            var solscanMatch = Regex.Match(text, @"solscan\.io(?:/#?)?/token/([A-HJ-NP-Za-km-z1-9]{32,44})", RegexOptions.IgnoreCase);
            if (solscanMatch.Success)
                return solscanMatch.Groups[1].Value;
            // pump.fun: https://pump.fun/coin/MINT (e.g. ...et5pump)
            var pumpMatch = Regex.Match(text, @"pump\.fun/coin/([A-HJ-NP-Za-km-z1-9]{32,44})", RegexOptions.IgnoreCase);
            if (pumpMatch.Success)
                return pumpMatch.Groups[1].Value;
            // Any URL with /token/MINT or /coin/MINT (explorers, pump.fun, etc.)
            var tokenPathMatch = Regex.Match(text, @"/token/([A-HJ-NP-Za-km-z1-9]{32,44})", RegexOptions.IgnoreCase);
            if (tokenPathMatch.Success)
                return tokenPathMatch.Groups[1].Value;
            var coinPathMatch = Regex.Match(text, @"/coin/([A-HJ-NP-Za-km-z1-9]{32,44})", RegexOptions.IgnoreCase);
            if (coinPathMatch.Success)
                return coinPathMatch.Groups[1].Value;
            // Raw mint: 32-44 base58 chars only (no spaces, no URL)
            if (text.Length >= 32 && text.Length <= 44 && Regex.IsMatch(text, @"^[A-HJ-NP-Za-km-z1-9]+$") && !text.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                return text;
            return null;
        }

        /// <summary>Strip @BotUsername suffix and normalize slash so we recognise /join_saints, /join_saints@Bot, ／join_saints (fullwidth slash), etc.</summary>
        private static string NormalizeCommand(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return text?.Trim() ?? "";
            var t = text.Trim();
            // Some Telegram clients send fullwidth slash (U+FF0F) instead of ASCII /
            if (t.Length > 0 && t[0] == '\uFF0F')
                t = '/' + t.Substring(1);
            var at = t.IndexOf('@');
            if (at > 0) return t.Substring(0, at).Trim();
            return t;
        }

        private static (long, long) StateKey(long chatId, long? userId)
        {
            // Private chat: chatId equals user id. Group: chatId is group id, userId is sender.
            return (chatId, userId ?? chatId);
        }

        private static bool IsGroupChat(TelegramChat chat)
        {
            return chat?.Type == "group" || chat?.Type == "supergroup";
        }

        /// <summary>When a user sends a message in the SAINTS group: if they have a linked wallet (from mint), are in top 20 $SAINT holders, and we haven't sent the Hall verification NFT yet, send the NFT and DM them. Returns reply to send in the group (or null).</summary>
        private async Task<string> TrySendHallVerificationNftToGroupMemberAsync(long telegramUserId)
        {
            if (_saintMintRecordService == null || _topTokenHoldersService == null || _hallVerificationNftSentStore == null || _hallVerificationNftSender == null)
                return null;
            var saintMint = _options?.SaintTokenMint?.Trim();
            if (string.IsNullOrEmpty(saintMint))
                return null;

            var wallet = await _saintMintRecordService.GetLatestWalletForTelegramUserAsync(telegramUserId).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(wallet))
                return null;

            if (await _hallVerificationNftSentStore.ContainsAsync(wallet).ConfigureAwait(false))
                return null;

            var topN = _hallVerificationNftOptions?.TopN > 0 ? _hallVerificationNftOptions.TopN : 20;
            IReadOnlyList<(string WalletAddress, decimal Balance)> holders;
            try
            {
                holders = await _topTokenHoldersService.GetTopHoldersAsync(saintMint, topN).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "[TelegramNftMint] SAINTS group: failed to get top holders for {Mint}", saintMint);
                return null;
            }
            if (holders == null || holders.Count == 0)
                return null;
            var inTop = holders.Any(h => string.Equals(h.WalletAddress, wallet, StringComparison.OrdinalIgnoreCase));
            if (!inTop)
                return null;

            var baseUrl = _hallVerificationNftOptions?.BaseUrl?.Trim() ?? _options?.OasisApiBaseUrl?.Trim();
            if (string.IsNullOrEmpty(baseUrl))
            {
                _logger?.LogWarning("[TelegramNftMint] SAINTS group: no BaseUrl configured for Hall verification NFT.");
                return null;
            }

            var (sent, error) = await _hallVerificationNftSender.SendToWalletAsync(wallet, baseUrl).ConfigureAwait(false);
            if (!sent)
            {
                _logger?.LogWarning("[TelegramNftMint] SAINTS group: failed to send verification NFT to {Wallet}: {Error}", wallet, error);
                return null;
            }

            await _hallVerificationNftSentStore.AddAsync(wallet).ConfigureAwait(false);
            var dmMessage = "🪙 **Hall verification NFT sent.** Check your wallet — the NFT's description has a one-time code. Use it at **saints.fun** to claim your saint name in the Hall of Saints.";
            await SendMessageAsync(telegramUserId, dmMessage).ConfigureAwait(false);
            _logger?.LogInformation("[TelegramNftMint] SAINTS group: sent Hall verification NFT to user {UserId} (wallet {Wallet}).", telegramUserId, wallet);
            return "✅ Check your DMs for next steps.";
        }

        /// <summary>Prompt for the Image step. In groups, add hint to reply to the bot so Telegram delivers the update (Group Privacy).</summary>
        private static string ImageStepPrompt(bool isGroup)
        {
            var baseText = "🎨 Send a **photo** or **GIF**, or paste a **Solscan token URL** (e.g. https://solscan.io/token/...) or **mint address** to use that token's image and metadata.";
            if (isGroup)
                return baseText + "\n\n💡 **In groups:** send your photo or GIF as a **reply to this message** so the bot can see it (or disable Group Privacy in @BotFather).";
            return baseText;
        }

        private async Task StartFlowAsync(long chatId, long? userId)
        {
            var key = StateKey(chatId, userId);
            var state = new NftMintFlowState { ChatId = chatId, UserId = userId, Step = NftMintFlowStep.OasisLogin };
            _state[key] = state;
            if (_flowStateStore != null && _flowStateStore.IsConfigured)
                await _flowStateStore.SetMintStateAsync(chatId, userId, state).ConfigureAwait(false);
        }

        private async Task ClearFlowAsync(long chatId, long? userId)
        {
            _state.TryRemove(StateKey(chatId, userId), out _);
            if (_flowStateStore != null && _flowStateStore.IsConfigured)
                await _flowStateStore.RemoveMintStateAsync(chatId, userId).ConfigureAwait(false);
        }

        /// <param name="recipientMode">1 = top holders (/ordain); 2 = newest investors (/baptise).</param>
        private async Task StartDropFlowAsync(long chatId, long? userId, int recipientMode = 0)
        {
            var key = StateKey(chatId, userId);
            var state = new DropHoldersFlowState { ChatId = chatId, UserId = userId, Step = DropHoldersFlowStep.TokenInput, RecipientMode = recipientMode };
            _dropState[key] = state;
            if (_flowStateStore != null && _flowStateStore.IsConfigured)
                await _flowStateStore.SetDropStateAsync(chatId, userId, state).ConfigureAwait(false);
        }

        private async Task ClearDropFlowAsync(long chatId, long? userId)
        {
            _dropState.TryRemove(StateKey(chatId, userId), out _);
            if (_flowStateStore != null && _flowStateStore.IsConfigured)
                await _flowStateStore.RemoveDropStateAsync(chatId, userId).ConfigureAwait(false);
        }

        private async Task PersistDropStateAsync(long chatId, long? userId, DropHoldersFlowState state)
        {
            if (_flowStateStore != null && _flowStateStore.IsConfigured && state != null)
                await _flowStateStore.SetDropStateAsync(chatId, userId, state).ConfigureAwait(false);
        }

        private async Task PersistMintStateAsync(long chatId, long? userId, NftMintFlowState state)
        {
            if (_flowStateStore != null && _flowStateStore.IsConfigured && state != null)
                await _flowStateStore.SetMintStateAsync(chatId, userId, state).ConfigureAwait(false);
        }

        private async Task<string> HandleDropFlowStepAsync(DropHoldersFlowState dropState, string text, long chatId, long? userId, (long, long) stateKey)
        {
            var maxHolders = _options?.DropMaxHolders > 0 ? _options.DropMaxHolders : 20;

            if (dropState.Step == DropHoldersFlowStep.TokenInput)
            {
                var mint = TryExtractMintFromText(text) ?? text?.Trim();
                if (string.IsNullOrEmpty(mint) || mint.Length < 32)
                    return "Send a valid **token mint address** (Solana SPL, 32–44 chars). Or /cancel.";
                dropState.CommunityTokenMint = mint;
                var displayName = mint.Length > 12 ? mint.Substring(0, 8) + "…" + mint.Substring(mint.Length - 4) : mint;
                if (_tokenMetadataByMintService != null)
                {
                    try
                    {
                        var meta = await _tokenMetadataByMintService.GetMetadataAsync(mint).ConfigureAwait(false);
                        if (meta != null && (!string.IsNullOrWhiteSpace(meta.Name) || !string.IsNullOrWhiteSpace(meta.Symbol)))
                            displayName = (meta.Name ?? meta.Symbol ?? "").Trim();
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogDebug(ex, "[TelegramNftMint] Token metadata lookup failed for ordain mint {Mint}", mint);
                    }
                }
                dropState.CommunityTokenName = displayName;
                dropState.Step = DropHoldersFlowStep.TopN;
                await PersistDropStateAsync(chatId, userId, dropState).ConfigureAwait(false);
                var howManyLine = dropState.RecipientMode == 2 ? $"How many do you wish to baptise? (Max {maxHolders})" : dropState.RecipientMode == 1 ? $"How many do you wish to ordain? (Max {maxHolders})" : $"How many to send to? (Max {maxHolders})";
                return $"Token acquired. **{displayName}**\n\n{howManyLine}";
            }

            if (dropState.Step == DropHoldersFlowStep.TopN)
            {
                if (!int.TryParse(text?.Trim(), out var n) || n <= 0 || n > maxHolders)
                    return $"Send a number between 1 and {maxHolders}. Or /cancel.";
                dropState.TopN = n;
                await PersistDropStateAsync(chatId, userId, dropState).ConfigureAwait(false);
                // If command already set RecipientMode (/ordain=1, /baptise=2), skip choice and go to NftChoice
                if (dropState.RecipientMode == 1 || dropState.RecipientMode == 2)
                {
                    dropState.Step = DropHoldersFlowStep.NftChoice;
                    await PersistDropStateAsync(chatId, userId, dropState).ConfigureAwait(false);
                    return "What would thy send?\n\n**1** – pump.fun PFP (default)\n**2** – custom image\n\n---\n\nReply 1 or 2.";
                }
                dropState.Step = DropHoldersFlowStep.RecipientChoice;
                await PersistDropStateAsync(chatId, userId, dropState).ConfigureAwait(false);
                return "Who to send to?\n\n**1** – Top holders by balance\n**2** – Newest investors (recent buyers)\n\n---\n\nReply 1 or 2.";
            }

            if (dropState.Step == DropHoldersFlowStep.RecipientChoice)
            {
                var modeChoice = text?.Trim();
                if (modeChoice == "1")
                {
                    dropState.RecipientMode = 1;
                    dropState.Step = DropHoldersFlowStep.NftChoice;
                    await PersistDropStateAsync(chatId, userId, dropState).ConfigureAwait(false);
                    return "What would thy send?\n\n**1** – pump.fun PFP (default)\n**2** – custom image\n\n---\n\nReply 1 or 2.";
                }
                if (modeChoice == "2")
                {
                    dropState.RecipientMode = 2;
                    dropState.Step = DropHoldersFlowStep.NftChoice;
                    await PersistDropStateAsync(chatId, userId, dropState).ConfigureAwait(false);
                    return "What would thy send?\n\n**1** – pump.fun PFP (default)\n**2** – custom image\n\n---\n\nReply 1 or 2.";
                }
                return "Reply **1** (top holders) or **2** (newest investors). Or /cancel.";
            }

            if (dropState.Step == DropHoldersFlowStep.CustomImage)
                return "Send a **photo** or **GIF** for your custom baptise. Or /cancel.";

            if (dropState.Step == DropHoldersFlowStep.NftChoice)
            {
                var choice = text?.Trim();
                if (choice == "2")
                {
                    dropState.NftChoice = 2;
                    dropState.Step = DropHoldersFlowStep.OptionalGroupLink;
                    await PersistDropStateAsync(chatId, userId, dropState).ConfigureAwait(false);
                    return "Optional: Send your **private Telegram group invite link** to embed in the NFT (e.g. https://t.me/joinchat/...). Or type **skip**.";
                }
                if (choice != "1")
                    return "Reply **1** (pump.fun PFP) or **2** (custom image). Or /cancel.";
                dropState.NftChoice = 1;
                dropState.Step = DropHoldersFlowStep.OptionalGroupLink;
                await PersistDropStateAsync(chatId, userId, dropState).ConfigureAwait(false);
                return "Optional: Send your **private Telegram group invite link** to embed in the NFT (e.g. https://t.me/joinchat/...). Or type **skip**.";
            }

            if (dropState.Step == DropHoldersFlowStep.OptionalGroupLink)
            {
                var link = text?.Trim();
                if (string.IsNullOrEmpty(link) || link.Equals("skip", StringComparison.OrdinalIgnoreCase))
                    dropState.UserProvidedGroupLink = null;
                else
                {
                    if (!link.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !link.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                        link = "https://" + link;
                    if (link.IndexOf("t.me", StringComparison.OrdinalIgnoreCase) < 0 && link.IndexOf("telegram", StringComparison.OrdinalIgnoreCase) < 0)
                        return "Send a valid Telegram invite link (e.g. https://t.me/joinchat/...) or type **skip**.";
                    dropState.UserProvidedGroupLink = link;
                }
                if (dropState.NftChoice == 1)
                {
                    dropState.Step = DropHoldersFlowStep.Confirm;
                    await PersistDropStateAsync(chatId, userId, dropState).ConfigureAwait(false);
                    var recipientLine = dropState.RecipientMode == 2
                        ? $"**newest {dropState.TopN} investors**"
                        : $"**top {dropState.TopN} holders**";
                    return $"📋 **Confirm**\n\nSplendid. I will mint **1 NFT** per wallet and send to {recipientLine} of {dropState.CommunityTokenName}.\n\nTreasury will pay the mint fee 🙏\n\nType **YES** to start, or /cancel to cancel.";
                }
                dropState.Step = DropHoldersFlowStep.CustomImage;
                await PersistDropStateAsync(chatId, userId, dropState).ConfigureAwait(false);
                return "Send the **image** (photo or GIF) for your custom baptise. /cancel to cancel.";
            }

            if (dropState.Step == DropHoldersFlowStep.Confirm)
            {
                if (!text.Equals("YES", StringComparison.OrdinalIgnoreCase))
                    return "Type **YES** to baptise them, or /cancel to cancel.";
                if (_topTokenHoldersService == null)
                {
                    await ClearDropFlowAsync(chatId, userId).ConfigureAwait(false);
                    return "❌ Top holders service not configured. Contact support.";
                }
                var dropExecution = _serviceProvider?.GetService<DropToHoldersExecutionService>();
                if (dropExecution == null)
                {
                    await ClearDropFlowAsync(chatId, userId).ConfigureAwait(false);
                    return "❌ Drop execution not configured. Contact support.";
                }
                var customMetadataUrl = dropState.NftChoice == 2 ? dropState.CustomMetadataUrl : null;
                if (dropState.NftChoice == 1 && !string.IsNullOrWhiteSpace(dropState.UserProvidedGroupLink))
                {
                    var imageUrl = GetConfirmMediaUrl(_options) ?? GetWelcomeMediaUrl(_options) ?? "https://via.placeholder.com/512/1a1a2e/eee?text=SAINT";
                    customMetadataUrl = await UploadMetadataJsonToPinataAsync("Saint", "SAINT", "A Saint NFT. Ordained to top token holders.", imageUrl, "image/png", dropState.UserProvidedGroupLink, GetOrdainSecretMessage(), GetBaptiseX402PaymentEndpoint(), _options?.X402RevenueModel, _options?.X402TreasuryWallet).ConfigureAwait(false);
                }
                dropExecution.RunDropInBackground(dropState.CommunityTokenMint, dropState.TopN, chatId, customMetadataUrl, useNewestRecipients: dropState.RecipientMode == 2);
                await ClearDropFlowAsync(chatId, userId).ConfigureAwait(false);
                return dropState.RecipientMode == 2 ? "⏳ **Baptising.** I'll message you here when it's done." : "⏳ **Ordaining.** I'll message you here when it's done.";
            }

            return null;
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
        /// Upload Metaplex-style NFT metadata JSON to Pinata for marketplace/Phantom compatibility.
        /// Follows Metaplex standard: name, description, image, category (required); symbol, external_url, attributes, properties, seller_fee_basis_points (optional).
        /// optionalExternalUrl: e.g. Telegram group link for ordained NFTs (utility). optionalSecretMessage: "Message from SAINT" attribute for ordained NFTs.
        /// When x402PaymentEndpoint is set, adds x402Config so the NFT can participate in revenue sharing (holders earn when payments are received).
        /// </summary>
        private async Task<string> UploadMetadataJsonToPinataAsync(string name, string symbol, string description, string imageUrl, string imageContentType = "image/png", string optionalExternalUrl = null, string optionalSecretMessage = null, string x402PaymentEndpoint = null, string x402RevenueModel = null, string x402TreasuryWallet = null)
        {
            var rawImage = imageUrl ?? "https://via.placeholder.com/512/000000/FFFFFF?text=OASIS+NFT";
            // Prefer ipfs.io for the image field so Solscan/explorers that use it can display the image (same content, different gateway)
            var image = TryGetIpfsHash(rawImage, out var hash) ? $"https://ipfs.io/ipfs/{hash}" : rawImage;
            var externalUrl = optionalExternalUrl?.Trim();
            if (string.IsNullOrEmpty(externalUrl))
                externalUrl = null;
            var attrs = new List<object>();
            if (!string.IsNullOrWhiteSpace(optionalSecretMessage))
                attrs.Add(new { trait_type = "Message from SAINT", value = optionalSecretMessage.Trim() });
            var metadata = new Dictionary<string, object>
            {
                ["name"] = name ?? "Telegram NFT",
                ["symbol"] = symbol ?? "TNFT",
                ["description"] = (description ?? "").Trim().Length > 0 ? description : "Minted via OASIS",
                ["image"] = image,
                ["external_url"] = externalUrl,
                ["category"] = "image",
                ["seller_fee_basis_points"] = 500,
                ["attributes"] = attrs,
                ["properties"] = new Dictionary<string, object>
                {
                    ["category"] = "image",
                    ["files"] = new[] { new { uri = rawImage, type = imageContentType ?? "image/png" } }
                }
            };
            if (!string.IsNullOrWhiteSpace(x402PaymentEndpoint))
            {
                metadata["x402Config"] = new Dictionary<string, object>
                {
                    ["enabled"] = true,
                    ["paymentEndpoint"] = x402PaymentEndpoint.Trim(),
                    ["revenueModel"] = (x402RevenueModel ?? "equal").Trim(),
                    ["treasuryWallet"] = (x402TreasuryWallet ?? "").Trim(),
                    ["metadata"] = new Dictionary<string, object>
                    {
                        ["distributionFrequency"] = "realtime",
                        ["revenueSharePercentage"] = 100
                    }
                };
            }
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

        /// <summary>
        /// Web3-only mint + send: calls Solana provider directly (no NFTManager, no Web4/MongoDB).
        /// Provider mints with OASIS wallet and sends to state.SendToWalletAddress in one flow.
        /// When telegramUserId is set and mint succeeds, records the mint for /join_saints gating.
        /// </summary>
        private async Task<string> MintNftAsync(NftMintFlowState state, long? telegramUserId = null)
        {
            var title = state.Title ?? "Telegram NFT";
            var symbol = state.Symbol ?? "TNFT";
            var description = state.Description ?? "";
            var imageUrl = state.ImageUrl ?? "https://via.placeholder.com/512/000000/FFFFFF?text=OASIS+NFT";
            var imageContentType = state.ImageContentType ?? "image/png";

            string jsonMetaDataUrl;
            if (!string.IsNullOrEmpty(state.JsonMetaDataUrl))
                jsonMetaDataUrl = state.JsonMetaDataUrl;
            else
            {
                jsonMetaDataUrl = await UploadMetadataJsonToPinataAsync(title, symbol, description, imageUrl, imageContentType).ConfigureAwait(false);
                if (string.IsNullOrEmpty(jsonMetaDataUrl))
                {
                    _logger?.LogWarning("[TelegramNftMint] Metadata JSON upload to Pinata failed");
                    return "❌ Mint failed: could not upload metadata to IPFS. Try again.";
                }
            }

            // Cluster (devnet/mainnet) is determined by OASIS_DNA / provider config.
            var providerResult = new NFTManager(Guid.Empty).GetNFTProvider(ProviderType.SolanaOASIS);
            if (providerResult?.Result == null || providerResult.IsError)
            {
                _logger?.LogWarning("[TelegramNftMint] Solana provider not available: {Message}", providerResult?.Message);
                return "❌ Mint failed: Solana provider not available. Try again later.";
            }

            var mintRequest = new MintWeb3NFTRequest
            {
                Title = title,
                Symbol = symbol,
                JSONMetaDataURL = jsonMetaDataUrl,
                SendToAddressAfterMinting = state.SendToWalletAddress
            };

            try
            {
                var result = await providerResult.Result.MintNFTAsync(mintRequest).ConfigureAwait(false);

                if (result.IsError)
                {
                    _logger?.LogWarning("[TelegramNftMint] Mint failed: {Message}", result.Message);
                    return $"❌ Mint failed: {result.Message}";
                }

                var txHash = result.Result?.TransactionResult ?? result.Result?.Web3NFT?.MintTransactionHash ?? "";
                var nftAddress = result.Result?.Web3NFT?.NFTTokenAddress ?? "";
                if (telegramUserId.HasValue && _saintMintRecordService != null)
                {
                    try
                    {
                        await _saintMintRecordService.RecordMintAsync(telegramUserId.Value, state.SendToWalletAddress ?? "", nftAddress, txHash).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "[TelegramNftMint] RecordMintAsync failed for user {UserId}", telegramUserId.Value);
                    }
                }
                var clusterForLink = string.IsNullOrWhiteSpace(_options?.SolanaCluster) ? "devnet" : _options.SolanaCluster.Trim();
                var txLink = $"https://solscan.io/tx/{txHash}?cluster={clusterForLink}";
                var nftLink = string.IsNullOrEmpty(nftAddress) ? txLink : $"https://solscan.io/account/{nftAddress}?cluster={clusterForLink}";
                var successMsg = $"✅ **NFT minted!**\n\n🔗 [View transaction]({txLink})\n📍 [View NFT]({nftLink})\n\nCheck your Solana wallet. Mint a SAINT? Use /join_saints in a private chat to get the secret group link.";
                if (!string.IsNullOrWhiteSpace(_options?.SaintsMintSuccessLine))
                    successMsg += "\n\n" + _options.SaintsMintSuccessLine.Trim();
                return successMsg;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[TelegramNftMint] Mint exception");
                return $"❌ Mint failed: {ex.Message}";
            }
        }

        /// <summary>
        /// Get or create a single metadata URL for "drop Saint" NFTs (cached so we upload once per process).
        /// Used by drop-to-holders flow to mint the same Saint NFT to many wallets.
        /// </summary>
        public async Task<string> GetOrCreateDropSaintMetadataUrlAsync()
        {
            if (!string.IsNullOrEmpty(_dropSaintMetadataUrlCache))
                return _dropSaintMetadataUrlCache;
            var title = "Saint";
            var symbol = "SAINT";
            var description = "A Saint NFT. Dropped to top token holders.";
            var imageUrl = GetConfirmMediaUrl(_options) ?? GetWelcomeMediaUrl(_options) ?? "https://via.placeholder.com/512/1a1a2e/eee?text=SAINT";
            var jsonUrl = await UploadMetadataJsonToPinataAsync(title, symbol, description, imageUrl, "image/png", _options?.OrdainNftTelegramGroupLink, GetOrdainSecretMessage(), GetBaptiseX402PaymentEndpoint(), _options?.X402RevenueModel, _options?.X402TreasuryWallet).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(jsonUrl))
                _dropSaintMetadataUrlCache = jsonUrl;
            return jsonUrl;
        }

        /// <summary>
        /// Mint one Saint NFT (using drop metadata) and send to the given wallet. Used by drop-to-holders.
        /// Returns (Success, Error, TransactionSignature, NftMintAddress). NftMintAddress is set on success for x402/drip eligibility.
        /// </summary>
        public async Task<(bool Success, string Error, string TransactionSignature, string NftMintAddress)> MintNftToWalletAsync(string jsonMetaDataUrl, string walletAddress)
        {
            if (string.IsNullOrWhiteSpace(jsonMetaDataUrl) || string.IsNullOrWhiteSpace(walletAddress))
                return (false, "Missing metadata URL or wallet.", null, null);
            // Cluster is determined by OASIS_DNA / provider config.
            var providerResult = new NFTManager(Guid.Empty).GetNFTProvider(ProviderType.SolanaOASIS);
            if (providerResult?.Result == null || providerResult.IsError)
                return (false, "Solana provider not available.", null, null);
            var treasuryAvatarId = Guid.Empty;
            var treasuryIdStr = !string.IsNullOrWhiteSpace(_options.DropTreasuryAvatarId) ? _options.DropTreasuryAvatarId.Trim() : _options.BotAvatarId?.Trim();
            if (!string.IsNullOrEmpty(treasuryIdStr) && Guid.TryParse(treasuryIdStr, out var parsed))
                treasuryAvatarId = parsed;
            var mintRequest = new MintWeb3NFTRequest
            {
                Title = "Saint",
                Symbol = "SAINT",
                JSONMetaDataURL = jsonMetaDataUrl,
                SendToAddressAfterMinting = walletAddress.Trim(),
                MintedByAvatarId = treasuryAvatarId
            };
            try
            {
                var result = await providerResult.Result.MintNFTAsync(mintRequest).ConfigureAwait(false);
                if (result.IsError)
                    return (false, result.Message ?? "Mint failed.", null, null);
                var txHash = result.Result?.TransactionResult ?? result.Result?.Web3NFT?.MintTransactionHash ?? "";
                var nftMintAddress = result.Result?.Web3NFT?.NFTTokenAddress?.Trim();
                return (true, null, string.IsNullOrWhiteSpace(txHash) ? null : txHash.Trim(), string.IsNullOrWhiteSpace(nftMintAddress) ? null : nftMintAddress);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "[TelegramNftMint] MintNftToWallet failed for {Wallet}", walletAddress);
                return (false, ex.Message, null, null);
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

        /// <summary>Welcome media URL: SaintsMintWelcomeGifUrl if set, else {OasisApiBaseUrl}/saints/welcome.mp4 when base is public (Telegram must be able to fetch it).</summary>
        private static string GetWelcomeMediaUrl(TelegramNftMintOptions options)
        {
            if (!string.IsNullOrWhiteSpace(options?.SaintsMintWelcomeGifUrl))
                return options.SaintsMintWelcomeGifUrl.Trim();
            var baseUrl = options?.OasisApiBaseUrl?.Trim();
            if (string.IsNullOrEmpty(baseUrl)) return null;
            if (baseUrl.IndexOf("localhost", StringComparison.OrdinalIgnoreCase) >= 0 || baseUrl.IndexOf("127.0.0.1", StringComparison.OrdinalIgnoreCase) >= 0)
                return null;
            return baseUrl.TrimEnd('/') + SaintsWelcomePath;
        }

        /// <summary>Confirm media URL: SaintsMintConfirmGifUrl if set, else {OasisApiBaseUrl}/saints/confirm.png when base is public.</summary>
        private static string GetConfirmMediaUrl(TelegramNftMintOptions options)
        {
            if (!string.IsNullOrWhiteSpace(options?.SaintsMintConfirmGifUrl))
                return options.SaintsMintConfirmGifUrl.Trim();
            var baseUrl = options?.OasisApiBaseUrl?.Trim();
            if (string.IsNullOrEmpty(baseUrl)) return null;
            if (baseUrl.IndexOf("localhost", StringComparison.OrdinalIgnoreCase) >= 0 || baseUrl.IndexOf("127.0.0.1", StringComparison.OrdinalIgnoreCase) >= 0)
                return null;
            return baseUrl.TrimEnd('/') + SaintsConfirmPath;
        }

        /// <summary>Secret message for ordained NFT attribute "Message from SAINT". Default: "Sent with blessings from $SAINT".</summary>
        private string GetOrdainSecretMessage()
        {
            return !string.IsNullOrWhiteSpace(_options?.OrdainNftSecretMessage) ? _options.OrdainNftSecretMessage.Trim() : "Sent with blessings from $SAINT";
        }

        /// <summary>When x402 is enabled for baptise, returns the payment endpoint URL (from X402PaymentEndpoint or OasisApiBaseUrl + /api/x402/revenue/saint). Otherwise null.</summary>
        private string GetBaptiseX402PaymentEndpoint()
        {
            if (_options == null || !_options.X402Enabled)
                return null;
            var endpoint = _options.X402PaymentEndpoint?.Trim();
            if (!string.IsNullOrEmpty(endpoint))
                return endpoint;
            var baseUrl = _options.OasisApiBaseUrl?.Trim();
            if (string.IsNullOrEmpty(baseUrl))
                return null;
            return baseUrl.TrimEnd('/') + "/api/x402/revenue/saint";
        }

        /// <summary>Join Saints success media URL: SaintsJoinSaintsGifUrl if set, else {OasisApiBaseUrl}/saints/ascended-halls.mp4 when base is public.</summary>
        private static string GetJoinSaintsSuccessMediaUrl(TelegramNftMintOptions options)
        {
            if (!string.IsNullOrWhiteSpace(options?.SaintsJoinSaintsGifUrl))
                return options.SaintsJoinSaintsGifUrl.Trim();
            var baseUrl = options?.OasisApiBaseUrl?.Trim();
            if (string.IsNullOrEmpty(baseUrl)) return null;
            if (baseUrl.IndexOf("localhost", StringComparison.OrdinalIgnoreCase) >= 0 || baseUrl.IndexOf("127.0.0.1", StringComparison.OrdinalIgnoreCase) >= 0)
                return null;
            return baseUrl.TrimEnd('/') + SaintsJoinSaintsPath;
        }

        /// <summary>Pick animation URL: random from MintingGifUrls if set, else MintingGifUrl, else built-in video (when OasisApiBaseUrl set), else fallback GIF.</summary>
        private static string GetMintingGifUrl(TelegramNftMintOptions options)
        {
            var urls = options?.MintingGifUrls;
            if (urls != null && urls.Length > 0)
            {
                var nonEmpty = urls.Where(u => !string.IsNullOrWhiteSpace(u)).ToArray();
                if (nonEmpty.Length > 0)
                    return nonEmpty[System.Random.Shared.Next(nonEmpty.Length)];
            }
            if (!string.IsNullOrEmpty(options?.MintingGifUrl))
                return options.MintingGifUrl;
            var baseUrl = options?.OasisApiBaseUrl?.Trim();
            if (!string.IsNullOrEmpty(baseUrl))
            {
                // Telegram's servers cannot fetch localhost; use fallback GIF so the animation loads
                if (baseUrl.IndexOf("localhost", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    baseUrl.IndexOf("127.0.0.1", StringComparison.OrdinalIgnoreCase) >= 0)
                    return FallbackMintingGifUrl;
                return baseUrl.TrimEnd('/') + BuiltInMintingVideoPath;
            }
            return FallbackMintingGifUrl;
        }

        /// <summary>
        /// Send an animation (GIF/MP4) to the chat with optional caption. Used for welcome, image-done, confirm, and minting steps.
        /// </summary>
        private async Task SendAnimationAsync(long chatId, string animationUrl, string caption = null)
        {
            if (string.IsNullOrEmpty(_options.BotToken) || string.IsNullOrWhiteSpace(animationUrl))
                return;
            var url = $"https://api.telegram.org/bot{_options.BotToken}/sendAnimation";
            var payload = string.IsNullOrWhiteSpace(caption)
                ? (object)new { chat_id = chatId, animation = animationUrl }
                : new { chat_id = chatId, animation = animationUrl, caption = caption, parse_mode = "Markdown" };
            var json = JsonSerializer.Serialize(payload);
            try
            {
                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                await _httpClient.PostAsync(url, content).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "[TelegramNftMint] SendAnimation failed");
            }
        }

        /// <summary>
        /// Send a photo (PNG/JPEG) to the chat with optional caption. Used when confirm media is an image (e.g. /saints/confirm.png).
        /// </summary>
        private async Task SendPhotoAsync(long chatId, string photoUrl, string caption = null)
        {
            if (string.IsNullOrEmpty(_options.BotToken) || string.IsNullOrWhiteSpace(photoUrl))
                return;
            var url = $"https://api.telegram.org/bot{_options.BotToken}/sendPhoto";
            var payload = string.IsNullOrWhiteSpace(caption)
                ? (object)new { chat_id = chatId, photo = photoUrl }
                : new { chat_id = chatId, photo = photoUrl, caption = caption, parse_mode = "Markdown" };
            var json = JsonSerializer.Serialize(payload);
            try
            {
                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                await _httpClient.PostAsync(url, content).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "[TelegramNftMint] SendPhoto failed");
            }
        }

        /// <summary>
        /// Send media by URL: photo for .png/.jpg/.jpeg (Telegram sendPhoto), animation for .gif/.mp4 etc. (sendAnimation).
        /// </summary>
        private async Task SendMediaAsync(long chatId, string mediaUrl, string caption = null)
        {
            if (string.IsNullOrWhiteSpace(mediaUrl)) return;
            var u = mediaUrl.Trim();
            var isPhoto = u.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                || u.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                || u.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase);
            if (isPhoto)
                await SendPhotoAsync(chatId, u, caption).ConfigureAwait(false);
            else
                await SendAnimationAsync(chatId, u, caption).ConfigureAwait(false);
        }

        /// <summary>
        /// Send "Minting..." message with configured GIF (or default) before the actual mint.
        /// Uses MintingGifUrls (random one) if set, else MintingGifUrl, else default.
        /// Caption can be overridden via SaintsMintMintingCaption.
        /// </summary>
        private async Task SendMintingMessageAsync(long chatId)
        {
            if (string.IsNullOrEmpty(_options.BotToken))
                return;
            var gifUrl = GetMintingGifUrl(_options);
            var caption = !string.IsNullOrWhiteSpace(_options?.SaintsMintMintingCaption)
                ? _options.SaintsMintMintingCaption
                : "🪙 **Minting your NFT...**\n\nOne moment please.";
            await SendAnimationAsync(chatId, gifUrl, caption).ConfigureAwait(false);
        }

        /// <summary>
        /// Send the ordain success video (Ascended Halls) to the chat. Call before sending the ordain complete text message.
        /// </summary>
        public async Task SendOrdainSuccessMediaAsync(long chatId)
        {
            var url = GetJoinSaintsSuccessMediaUrl(_options);
            if (!string.IsNullOrWhiteSpace(url))
                await SendMediaAsync(chatId, url, null).ConfigureAwait(false);
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
