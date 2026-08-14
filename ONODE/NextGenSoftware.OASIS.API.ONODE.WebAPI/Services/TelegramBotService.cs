using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Configuration;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Polling;
using NextGenSoftware.OASIS.API.Providers.TelegramOASIS;
using NextGenSoftware.OASIS.API.Providers.TelegramOASIS.Models;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services
{
    /// <summary>
    /// Service for handling Telegram bot commands and user interactions.
    /// Integrates with TelegramOASIS provider for data persistence and TimoRides for ride-hailing.
    /// </summary>
    public partial class TelegramBotService
    {
        private readonly TelegramBotClient _botClient;
        private readonly TelegramOASISProvider _telegramProvider;
        private readonly AvatarManager _avatarManager;
        private readonly ILogger<TelegramBotService> _logger;
        private readonly NFTService _nftService;
        private readonly PinataService _pinataService;
        private readonly TimoRidesApiService _timoRidesApiService;
        private readonly RideBookingStateManager _rideState;
        private readonly GoogleMapsService _mapsService;
        private readonly TimoRidesOptions _timoOptions;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly System.Net.Http.HttpClient _httpClient;

        private readonly string _openAiApiKey;
        private const bool AI_ENABLED = true;

        public TelegramBotService(
            string botToken,
            TelegramOASISProvider telegramProvider,
            AvatarManager avatarManager,
            ILogger<TelegramBotService> logger,
            NFTService nftService,
            PinataService pinataService,
            TimoRidesApiService timoRidesApiService,
            RideBookingStateManager rideBookingStateManager,
            GoogleMapsService googleMapsService,
            IOptions<TimoRidesOptions> timoOptions)
        {
            var handler = new System.Net.Http.HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };
            var telegramHttpClient = new System.Net.Http.HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(10)
            };

            _botClient = new TelegramBotClient(botToken, telegramHttpClient);
            _telegramProvider = telegramProvider;
            _avatarManager = avatarManager;
            _logger = logger;
            _nftService = nftService;
            _pinataService = pinataService;
            _timoRidesApiService = timoRidesApiService;
            _rideState = rideBookingStateManager;
            _mapsService = googleMapsService;
            _timoOptions = timoOptions?.Value ?? new TimoRidesOptions();
            _openAiApiKey = _timoOptions.OpenAiApiKey ?? "";
            _cancellationTokenSource = new CancellationTokenSource();

            _httpClient = new System.Net.Http.HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        public void StartReceiving()
        {
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery }
            };

            _botClient.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                _cancellationTokenSource.Token
            );

            _logger.LogInformation("Telegram bot started receiving messages");
        }

        public void StopReceiving()
        {
            _cancellationTokenSource.Cancel();
            _logger.LogInformation("Telegram bot stopped receiving messages");
        }

        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            try
            {
                if (update.Type == UpdateType.Message)
                {
                    if (update.Message?.Text != null)
                        await HandleMessageAsync(update.Message, cancellationToken);
                    else if (update.Message?.Location != null)
                        await HandleLocationAsync(update.Message, cancellationToken);
                    else if (update.Message?.Photo != null && update.Message.Photo.Length > 0)
                        await HandlePhotoMessageAsync(update.Message, cancellationToken);
                }
                else if (update.Type == UpdateType.CallbackQuery)
                {
                    await HandleCallbackQueryAsync(update.CallbackQuery, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling Telegram update");
            }
        }

        private async Task HandleMessageAsync(Telegram.Bot.Types.Message message, CancellationToken cancellationToken)
        {
            var chatId = message.Chat.Id;
            var text = message.Text;
            var user = message.From;

            _logger.LogInformation($"Received message from {user?.Username}: {text}");

            if (text.StartsWith("/"))
            {
                var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var command = parts[0].ToLower();
                var args = parts.Skip(1).ToArray();
                await HandleCommandAsync(chatId, user, command, args, cancellationToken);
            }
            else
            {
                if (TryGetRideConversationState(user.Id, out var state))
                {
                    if (state == RideConversationStates.WaitingPickup || state == RideConversationStates.WaitingDropoff)
                    {
                        await HandleAddressTextAsync(message, cancellationToken);
                        return;
                    }

                    if (state == RideConversationStates.AwaitingConfirmation)
                    {
                        await HandleConfirmationUpdatesAsync(chatId, user.Id, text, cancellationToken);
                        return;
                    }
                }

                if (AI_ENABLED && !string.IsNullOrWhiteSpace(text))
                {
                    await ProcessNaturalLanguageAsync(chatId, user, text, cancellationToken);
                }
                else
                {
                    await _botClient.SendTextMessageAsync(
                        chatId,
                        "Use /help to see available commands",
                        cancellationToken: cancellationToken
                    );
                }
            }
        }

        private async Task HandleCommandAsync(
            long chatId,
            User user,
            string command,
            string[] args,
            CancellationToken cancellationToken)
        {
            try
            {
                switch (command)
                {
                    case "/start":
                        await HandleStartCommand(chatId, user, cancellationToken);
                        break;
                    case "/help":
                        await HandleHelpCommand(chatId, cancellationToken);
                        break;
                    case "/creategroup":
                        await HandleCreateGroupCommand(chatId, user, args, cancellationToken);
                        break;
                    case "/joingroup":
                        await HandleJoinGroupCommand(chatId, user, args, cancellationToken);
                        break;
                    case "/mygroups":
                        await HandleMyGroupsCommand(chatId, user, cancellationToken);
                        break;
                    case "/checkin":
                        await HandleCheckinCommand(chatId, user, args, cancellationToken);
                        break;
                    case "/milestone":
                        await HandleMilestoneCommand(chatId, user, args, cancellationToken);
                        break;
                    case "/mystats":
                        await HandleMyStatsCommand(chatId, user, cancellationToken);
                        break;
                    case "/leaderboard":
                        await HandleLeaderboardCommand(chatId, user, args, cancellationToken);
                        break;
                    case "/setgoal":
                        await HandleSetGoalCommand(chatId, user, args, cancellationToken);
                        break;
                    case "/mintnft":
                        await HandleMintNFTCommand(chatId, user, args, cancellationToken);
                        break;
                    case "/bookride":
                        await HandleBookRideCommand(chatId, user, cancellationToken);
                        break;
                    case "/myrides":
                        await HandleMyRidesCommand(chatId, user, cancellationToken);
                        break;
                    case "/track":
                        await HandleTrackRideCommand(chatId, user, args, cancellationToken);
                        break;
                    case "/cancel":
                        await HandleCancelRideCommand(chatId, user, args, cancellationToken);
                        break;
                    default:
                        await _botClient.SendTextMessageAsync(
                            chatId,
                            $"❓ Unknown command: {command}\nUse /help to see available commands",
                            cancellationToken: cancellationToken
                        );
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error handling command: {command}");
                await _botClient.SendTextMessageAsync(
                    chatId,
                    "❌ An error occurred processing your command. Please try again.",
                    cancellationToken: cancellationToken
                );
            }
        }

        private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            _logger.LogError(exception, "Telegram bot error occurred");
            return Task.CompletedTask;
        }
    }
}
