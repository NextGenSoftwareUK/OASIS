using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
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
    /// Service for handling Telegram bot commands and user interactions
    /// Integrates with TelegramOASIS provider for data persistence
    /// </summary>
    public class TelegramBotService
    {
        private readonly TelegramBotClient _botClient;
        private readonly TelegramOASIS _telegramProvider;
        private readonly AvatarManager _avatarManager;
        private readonly ILogger<TelegramBotService> _logger;
        private readonly NFTService _nftService;
        private readonly PinataService _pinataService;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly System.Net.Http.HttpClient _httpClient;
        
        // TimoRides state tracking
        private readonly Dictionary<long, string> _userBookingState = new();
        private readonly Dictionary<long, (double lat, double lon, string addr)> _userPickupLocation = new();
        private readonly Dictionary<long, (double lat, double lon, string addr)> _userDropoffLocation = new();
        private readonly Dictionary<long, string> _selectedDriver = new();
        private readonly Dictionary<long, DateTime> _userScheduledTime = new();
        private readonly Dictionary<long, string> _userVehiclePreference = new();
        private readonly Dictionary<long, List<string>> _userMultiStops = new();
        
        // AI Configuration
        private const string OPENAI_API_KEY = "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";
        private const bool AI_ENABLED = true; // Toggle AI processing

        public TelegramBotService(
            string botToken,
            TelegramOASIS telegramProvider,
            AvatarManager avatarManager,
            ILogger<TelegramBotService> logger,
            NFTService nftService,
            PinataService pinataService)
        {
            // Create HttpClient with SSL bypass for local development
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
            _cancellationTokenSource = new CancellationTokenSource();
            
            // Initialize HttpClient for AI API calls
            _httpClient = new System.Net.Http.HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        /// <summary>
        /// Start receiving messages from Telegram
        /// </summary>
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

        /// <summary>
        /// Stop receiving messages
        /// </summary>
        public void StopReceiving()
        {
            _cancellationTokenSource.Cancel();
            _logger.LogInformation("Telegram bot stopped receiving messages");
        }

        /// <summary>
        /// Handle incoming Telegram updates
        /// </summary>
        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            try
            {
                if (update.Type == UpdateType.Message)
                {
                    if (update.Message?.Text != null)
                    {
                        await HandleMessageAsync(update.Message, cancellationToken);
                    }
                    else if (update.Message?.Location != null)
                    {
                        await HandleLocationAsync(update.Message, cancellationToken);
                    }
                    else if (update.Message?.Photo != null && update.Message.Photo.Length > 0)
                    {
                        await HandlePhotoMessageAsync(update.Message, cancellationToken);
                    }
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

        /// <summary>
        /// Handle text messages and commands
        /// </summary>
        private async Task HandleMessageAsync(Telegram.Bot.Types.Message message, CancellationToken cancellationToken)
        {
            var chatId = message.Chat.Id;
            var text = message.Text;
            var user = message.From;

            _logger.LogInformation($"Received message from {user?.Username}: {text}");

            // Parse command
            if (text.StartsWith("/"))
            {
                var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var command = parts[0].ToLower();
                var args = parts.Skip(1).ToArray();

                await HandleCommandAsync(chatId, user, command, args, cancellationToken);
            }
            else
            {
                // Check if user is in address input mode
                if (_userBookingState.TryGetValue(user.Id, out var state))
                {
                    if (state == "WAITING_PICKUP" || state == "WAITING_DROPOFF")
                    {
                        await HandleAddressTextAsync(message, cancellationToken);
                        return;
                    }
                    
                    // Handle additional details while awaiting confirmation
                    if (state == "AWAITING_CONFIRMATION")
                    {
                        await HandleConfirmationUpdatesAsync(chatId, user.Id, text, cancellationToken);
                        return;
                    }
                }
                
                // AI-powered natural language processing
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

        /// <summary>
        /// Route commands to appropriate handlers
        /// </summary>
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

                    // TimoRides commands
                    case "/bookride":
                        await HandleBookRideCommand(chatId, user, cancellationToken);
                        break;
                        
                    case "/myrides":
                        await HandleMyRidesCommand(chatId, user, cancellationToken);
                        break;
                        
                    case "/track":
                        await HandleTrackRideCommand(chatId, user, args, cancellationToken);
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

        #region Command Handlers

        /// <summary>
        /// /start - Link Telegram account to OASIS avatar
        /// </summary>
        private async Task HandleStartCommand(long chatId, User user, CancellationToken cancellationToken)
        {
            // Check if user is already linked
            var existingAvatar = await _telegramProvider.GetTelegramAvatarByTelegramIdAsync(user.Id);

            if (!existingAvatar.IsError && existingAvatar.Result != null)
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    $"Welcome back, @{existingAvatar.Result.TelegramUsername}\n\n" +
                    $"Where are you trying to get to today?\n\n" +
                    $"Just tell me naturally:\n" +
                    $"• 'I need to get to King Shaka Airport'\n" +
                    $"• 'From Gateway Mall to uShaka Beach'\n" +
                    $"• Or use /bookride",
                    parseMode: ParseMode.Markdown,
                    cancellationToken: cancellationToken
                );
                return;
            }

            // Create new OASIS avatar for this Telegram user
            var linkResult = await _telegramProvider.LinkTelegramToAvatarAsync(
                user.Id,
                user.Username ?? $"user_{user.Id}",
                user.FirstName ?? "User",
                user.LastName ?? "",
                Guid.NewGuid() // Create new avatar
            );

            if (linkResult.IsError)
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    "❌ Failed to create your account. Please try again later.",
                    cancellationToken: cancellationToken
                );
                return;
            }

            await _botClient.SendTextMessageAsync(
                chatId,
                $"Welcome to TimoRides, @{user.Username}\n\n" +
                $"Your account is ready!\n\n" +
                $"Where do you need to go today?\n\n" +
                $"Just tell me:\n" +
                $"• 'I need to get to King Shaka Airport'\n" +
                $"• 'From Gateway Mall to uShaka Beach tomorrow at 3pm'\n" +
                $"• 'Book me a luxury ride to Umhlanga'\n\n" +
                $"Or use /bookride",
                parseMode: ParseMode.Markdown,
                cancellationToken: cancellationToken
            );
        }

        /// <summary>
        /// /help - Show available commands
        /// </summary>
        private async Task HandleHelpCommand(long chatId, CancellationToken cancellationToken)
        {
            var helpText = "*TimoRides*\n" +
                          "Premium ride-hailing on Telegram\n\n" +
                          "*Commands:*\n" +
                          "/bookride - Book a ride\n" +
                          "/myrides - View ride history\n" +
                          "/track - Track active ride\n" +
                          "/mystats - View your karma score\n\n" +
                          "*Features:*\n" +
                          "• Real-time driver tracking\n" +
                          "• Karma-based trust scores\n" +
                          "• Transparent pricing\n" +
                          "• Driver ratings & stats";

            await _botClient.SendTextMessageAsync(
                chatId,
                helpText,
                parseMode: ParseMode.Markdown,
                cancellationToken: cancellationToken
            );
        }

        /// <summary>
        /// /creategroup <name> - Create accountability group
        /// </summary>
        private async Task HandleCreateGroupCommand(long chatId, User user, string[] args, CancellationToken cancellationToken)
        {
            if (args.Length == 0)
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    "❌ Usage: /creategroup <group name>\n\nExample: /creategroup Fitness Warriors",
                    cancellationToken: cancellationToken
                );
                return;
            }

            // Get user's OASIS avatar
            var avatarResult = await _telegramProvider.GetTelegramAvatarByTelegramIdAsync(user.Id);
            if (avatarResult.IsError || avatarResult.Result == null)
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    "❌ Please use /start first to link your account",
                    cancellationToken: cancellationToken
                );
                return;
            }

            var groupName = string.Join(" ", args);
            var description = $"Accountability group created by @{user.Username}";

            // Create group
            var groupResult = await _telegramProvider.CreateGroupAsync(
                groupName,
                description,
                avatarResult.Result.OasisAvatarId,
                chatId
            );

            if (groupResult.IsError)
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    $"❌ Failed to create group: {groupResult.Message}",
                    cancellationToken: cancellationToken
                );
                return;
            }

            await _botClient.SendTextMessageAsync(
                chatId,
                $"✅ Group created: **{groupName}**\n\n" +
                $"Group ID: `{groupResult.Result.Id}`\n\n" +
                $"Share this ID with friends so they can join using:\n" +
                $"/joingroup {groupResult.Result.Id}",
                parseMode: ParseMode.Markdown,
                cancellationToken: cancellationToken
            );
        }

        /// <summary>
        /// /joingroup <groupId> - Join existing group
        /// </summary>
        private async Task HandleJoinGroupCommand(long chatId, User user, string[] args, CancellationToken cancellationToken)
        {
            if (args.Length == 0)
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    "❌ Usage: /joingroup <groupId>\n\nExample: /joingroup abc123",
                    cancellationToken: cancellationToken
                );
                return;
            }

            var groupId = args[0];

            // Get group details
            var groupResult = await _telegramProvider.GetGroupAsync(groupId);
            if (groupResult.IsError || groupResult.Result == null)
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    "❌ Group not found. Check the group ID and try again.",
                    cancellationToken: cancellationToken
                );
                return;
            }

            // Add user to group
            var joinResult = await _telegramProvider.AddMemberToGroupAsync(groupId, user.Id);
            if (joinResult.IsError)
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    $"❌ Failed to join group: {joinResult.Message}",
                    cancellationToken: cancellationToken
                );
                return;
            }

            await _botClient.SendTextMessageAsync(
                chatId,
                $"✅ You joined **{groupResult.Result.Name}**!\n\n" +
                $"Start checking in with /checkin to track your progress.",
                parseMode: ParseMode.Markdown,
                cancellationToken: cancellationToken
            );
        }

        /// <summary>
        /// /mygroups - List user's groups
        /// </summary>
        private async Task HandleMyGroupsCommand(long chatId, User user, CancellationToken cancellationToken)
        {
            var groupsResult = await _telegramProvider.GetUserGroupsAsync(user.Id);

            if (groupsResult.IsError || groupsResult.Result == null || !groupsResult.Result.Any())
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    "You're not in any groups yet.\n\n" +
                    "• Use /creategroup to start one\n" +
                    "• Use /joingroup to join an existing group",
                    cancellationToken: cancellationToken
                );
                return;
            }

            var groupsList = string.Join("\n", groupsResult.Result.Select((g, i) => 
                $"{i + 1}. **{g.Name}** - `{g.Id}`"
            ));

            await _botClient.SendTextMessageAsync(
                chatId,
                $"📋 **Your Groups:**\n\n{groupsList}",
                parseMode: ParseMode.Markdown,
                cancellationToken: cancellationToken
            );
        }

        /// <summary>
        /// /checkin <message> - Log progress and earn karma
        /// </summary>
        private async Task HandleCheckinCommand(long chatId, User user, string[] args, CancellationToken cancellationToken)
        {
            if (args.Length == 0)
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    "❌ Usage: /checkin <your update>\n\nExample: /checkin Completed 30-min workout! 💪",
                    cancellationToken: cancellationToken
                );
                return;
            }

            var message = string.Join(" ", args);
            int karmaAwarded = 10; // Base karma for check-in

            // Get user's avatar
            var avatarResult = await _telegramProvider.GetTelegramAvatarByTelegramIdAsync(user.Id);
            if (avatarResult.IsError || avatarResult.Result == null)
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    "❌ Please use /start first to link your account",
                    cancellationToken: cancellationToken
                );
                return;
            }

            // Get user's groups to log check-in
            var groupsResult = await _telegramProvider.GetUserGroupsAsync(user.Id);
            if (groupsResult.IsError || !groupsResult.Result.Any())
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    "❌ You need to join a group first. Use /joingroup or /creategroup",
                    cancellationToken: cancellationToken
                );
                return;
            }

            // Create achievement/check-in for first group (could enhance to select group)
            var group = groupsResult.Result.First();
            
            var achievement = new Achievement
            {
                GroupId = group.Id.ToString(),
                UserId = avatarResult.Result.OasisAvatarId,
                TelegramUserId = user.Id,
                Description = message,
                Type = AchievementType.Manual,
                Status = AchievementStatus.Completed,
                KarmaReward = karmaAwarded,
                TokenReward = 0, // Tokens awarded for milestones
                CompletedAt = DateTime.UtcNow,
                Checkins = new List<CheckIn>
                {
                    new CheckIn
                    {
                        Message = message,
                        KarmaAwarded = karmaAwarded,
                        Timestamp = DateTime.UtcNow
                    }
                }
            };

            var createResult = await _telegramProvider.CreateAchievementAsync(achievement);
            if (createResult.IsError)
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    $"❌ Failed to record check-in: {createResult.Message}",
                    cancellationToken: cancellationToken
                );
                return;
            }

            // Get user's total achievements to show progress
            var achievementsResult = await _telegramProvider.GetUserAchievementsAsync(avatarResult.Result.OasisAvatarId);
            var totalKarma = achievementsResult.Result?.Sum(a => a.KarmaReward) ?? karmaAwarded;
            var checkinCount = achievementsResult.Result?.Count ?? 1;

            await _botClient.SendTextMessageAsync(
                chatId,
                $"✅ **Check-in recorded!**\n\n" +
                $"_{message}_\n\n" +
                $"🎯 +{karmaAwarded} karma\n" +
                $"⭐ Total: {totalKarma} karma\n" +
                $"📊 Check-ins: {checkinCount}\n\n" +
                $"Keep it up! 🚀",
                parseMode: ParseMode.Markdown,
                cancellationToken: cancellationToken
            );
        }

        /// <summary>
        /// /milestone <@user> - Mark user milestone complete (admin only)
        /// </summary>
        private async Task HandleMilestoneCommand(long chatId, User user, string[] args, CancellationToken cancellationToken)
        {
            // For now, simplified - would add admin checking in production
            await _botClient.SendTextMessageAsync(
                chatId,
                "🏆 Milestone feature coming soon!\n\n" +
                "Admins will be able to mark major achievements and trigger token rewards.",
                cancellationToken: cancellationToken
            );
        }

        /// <summary>
        /// /mystats - View karma, tokens, and achievements
        /// </summary>
        private async Task HandleMyStatsCommand(long chatId, User user, CancellationToken cancellationToken)
        {
            var avatarResult = await _telegramProvider.GetTelegramAvatarByTelegramIdAsync(user.Id);
            if (avatarResult.IsError || avatarResult.Result == null)
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    "❌ Please use /start first to link your account",
                    cancellationToken: cancellationToken
                );
                return;
            }

            var achievementsResult = await _telegramProvider.GetUserAchievementsAsync(avatarResult.Result.OasisAvatarId);
            var achievements = achievementsResult.Result ?? new List<Achievement>();

            var totalKarma = achievements.Sum(a => a.KarmaReward);
            var totalTokens = achievements.Sum(a => a.TokenReward);
            var completedCount = achievements.Count(a => a.Status == AchievementStatus.Completed);
            var activeCount = achievements.Count(a => a.Status == AchievementStatus.Active);

            var statsText = $@"
📊 **Your Stats**

👤 @{user.Username}
⭐ Karma: {totalKarma} points
🪙 Tokens: {totalTokens} EXP
✅ Completed: {completedCount}
🎯 Active Goals: {activeCount}

Keep crushing it! 🚀
";

            await _botClient.SendTextMessageAsync(
                chatId,
                statsText,
                parseMode: ParseMode.Markdown,
                cancellationToken: cancellationToken
            );
        }

        /// <summary>
        /// /leaderboard [groupId] - View group rankings
        /// </summary>
        private async Task HandleLeaderboardCommand(long chatId, User user, string[] args, CancellationToken cancellationToken)
        {
            // Simplified for now - would aggregate karma by group
            await _botClient.SendTextMessageAsync(
                chatId,
                "🏆 Leaderboard feature coming soon!\n\n" +
                "You'll be able to see:\n" +
                "• Top contributors in your groups\n" +
                "• Karma rankings\n" +
                "• Achievement streaks",
                cancellationToken: cancellationToken
            );
        }

        /// <summary>
        /// /setgoal <description> - Create personal goal
        /// </summary>
        private async Task HandleSetGoalCommand(long chatId, User user, string[] args, CancellationToken cancellationToken)
        {
            if (args.Length == 0)
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    "❌ Usage: /setgoal <description>\n\nExample: /setgoal Complete 5 workouts this week",
                    cancellationToken: cancellationToken
                );
                return;
            }

            var goalDescription = string.Join(" ", args);

            // Get user's avatar
            var avatarResult = await _telegramProvider.GetTelegramAvatarByTelegramIdAsync(user.Id);
            if (avatarResult.IsError || avatarResult.Result == null)
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    "❌ Please use /start first to link your account",
                    cancellationToken: cancellationToken
                );
                return;
            }

            // Create achievement as goal
            var achievement = new Achievement
            {
                UserId = avatarResult.Result.OasisAvatarId,
                TelegramUserId = user.Id,
                Description = goalDescription,
                Type = AchievementType.Manual,
                Status = AchievementStatus.Active,
                KarmaReward = 50, // Reward when completed
                TokenReward = 5.0m,
                Deadline = DateTime.UtcNow.AddDays(7) // Default 1 week
            };

            var createResult = await _telegramProvider.CreateAchievementAsync(achievement);
            if (createResult.IsError)
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    $"❌ Failed to set goal: {createResult.Message}",
                    cancellationToken: cancellationToken
                );
                return;
            }

            await _botClient.SendTextMessageAsync(
                chatId,
                $"🎯 **Goal Set!**\n\n" +
                $"_{goalDescription}_\n\n" +
                $"📅 Deadline: {achievement.Deadline:MMM dd, yyyy}\n" +
                $"🎁 Reward: {achievement.KarmaReward} karma + {achievement.TokenReward} EXP\n\n" +
                $"Use /checkin to track your progress!",
                parseMode: ParseMode.Markdown,
                cancellationToken: cancellationToken
            );
        }

        #endregion

        /// <summary>
        /// Handle callback queries from inline keyboards
        /// </summary>
        private async Task HandleCallbackQueryAsync(CallbackQuery callbackQuery, CancellationToken cancellationToken)
        {
            var data = callbackQuery.Data;
            var userId = callbackQuery.From.Id;
            var chatId = callbackQuery.Message.Chat.Id;
            
            // Handle pickup location confirmation
            if (data.StartsWith("pickup_"))
            {
                var confirmedPickup = data.Replace("pickup_", "");
                _userPickupLocation[userId] = (0, 0, confirmedPickup);
                
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "✅", cancellationToken: cancellationToken);
                
                // Check if destination is waiting
                if (_userDropoffLocation.TryGetValue(userId, out var dest))
                {
                    var destOptions = GetLocationOptions(dest.addr);
                    if (destOptions.Count > 1)
                    {
                        // Destination also ambiguous
                        var keyboard = new InlineKeyboardMarkup(
                            destOptions.Select(loc => new[] { 
                                InlineKeyboardButton.WithCallbackData(loc, $"dest_{loc}") 
                            })
                        );
                        
                        await _botClient.SendTextMessageAsync(
                            chatId,
                            $"*Pickup confirmed*\n{confirmedPickup}\n\n" +
                            $"*Which destination?*\nI found multiple matches for '{dest.addr}':",
                            parseMode: ParseMode.Markdown,
                            replyMarkup: keyboard,
                            cancellationToken: cancellationToken
                        );
                        _userBookingState[userId] = "CONFIRMING_DESTINATION";
                        return;
                    }
                    else
                    {
                        // All locations confirmed, show final confirmation
                        _userDropoffLocation[userId] = (0, 0, destOptions[0]);
                        await ConfirmBookingDetailsAsync(chatId, userId, confirmedPickup, destOptions[0], cancellationToken);
                        return;
                    }
                }
                
                return;
            }
            
            // Handle destination location confirmation
            if (data.StartsWith("dest_"))
            {
                var confirmedDest = data.Replace("dest_", "");
                _userDropoffLocation[userId] = (0, 0, confirmedDest);
                
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "✅", cancellationToken: cancellationToken);
                
                // Get pickup and show FINAL CONFIRMATION before showing drivers
                if (_userPickupLocation.TryGetValue(userId, out var pickup))
                {
                    await ConfirmBookingDetailsAsync(chatId, userId, pickup.addr, confirmedDest, cancellationToken);
                    return;
                }
                
                return;
            }
            
            // Handle driver selection
            if (data.StartsWith("select_"))
            {
                var driverId = data.Replace("select_", "");
                _selectedDriver[userId] = driverId;
                
                var driverDetails = new Dictionary<string, (string name, string car, string plate)>
                {
                    ["driver1"] = ("Jonathan", "Renault Kwid", "ND 862-688"),
                    ["driver2"] = ("Eddison", "VW Polo", "ND 923-856"),
                    ["driver3"] = ("Sipho Mkhize", "Toyota Corolla", "NKZ 234 GP")
                };
                
                var driver = driverDetails[driverId];
                var bookingId = $"BK-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 4).ToUpper()}";
                
                await _botClient.AnswerCallbackQueryAsync(
                    callbackQuery.Id,
                    "✅ Booking confirmed!",
                    cancellationToken: cancellationToken
                );
                
                await _botClient.SendTextMessageAsync(
                    chatId,
                    $"*Booking Confirmed*\n\n" +
                    $"Booking ID: `{bookingId}`\n" +
                    $"Driver: {driver.name}\n" +
                    $"Vehicle: {driver.car} • {driver.plate}\n" +
                    $"Fare: R 420\n\n" +
                    $"Your driver is on the way.",
                    parseMode: ParseMode.Markdown,
                    cancellationToken: cancellationToken
                );
                
                // Start live tracking updates in the background
                _ = Task.Run(async () => await SimulateDriverTrackingAsync(chatId, driver.name, bookingId));
                
                _userBookingState.Remove(userId);
                return;
            }
            
            // Handle booking confirmation
            if (data == "confirm_yes")
            {
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "✅ Confirmed", cancellationToken: cancellationToken);
                
                // Get stored locations and proceed
                if (_userPickupLocation.TryGetValue(userId, out var pickup) &&
                    _userDropoffLocation.TryGetValue(userId, out var dest))
                {
                    await ProceedWithConfirmedBooking(chatId, userId, pickup.addr, dest.addr, cancellationToken);
                }
                return;
            }
            
            if (data == "confirm_cancel")
            {
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "Cancelled", cancellationToken: cancellationToken);
                
                // Clear booking state
                _userBookingState.Remove(userId);
                _userPickupLocation.Remove(userId);
                _userDropoffLocation.Remove(userId);
                _userScheduledTime.Remove(userId);
                _userVehiclePreference.Remove(userId);
                _userMultiStops.Remove(userId);
                
                await _botClient.SendTextMessageAsync(
                    chatId,
                    "Booking cancelled.\n\nUse /bookride when you're ready to try again.",
                    cancellationToken: cancellationToken
                );
                return;
            }
            
            // Handle trip safety confirmation
            if (data == "trip_confirm")
            {
                await _botClient.AnswerCallbackQueryAsync(
                    callbackQuery.Id,
                    "✅ Have a safe trip!",
                    cancellationToken: cancellationToken
                );
                
                await _botClient.SendTextMessageAsync(
                    chatId,
                    "*Safety confirmed*\n\nEnjoy your ride!",
                    parseMode: ParseMode.Markdown,
                    cancellationToken: cancellationToken
                );
                return;
            }
            
            if (data == "trip_cancel")
            {
                await _botClient.AnswerCallbackQueryAsync(
                    callbackQuery.Id,
                    "Contact support immediately",
                    cancellationToken: cancellationToken
                );
                
                await _botClient.SendTextMessageAsync(
                    chatId,
                    "*⚠️ Safety Alert*\n\n" +
                    "Do NOT enter the vehicle.\n\n" +
                    "If this is not your driver:\n" +
                    "• Contact TimoRides support immediately\n" +
                    "• Stay in a safe location\n" +
                    "• Report the incorrect vehicle\n\n" +
                    "Support: +27 XX XXX XXXX",
                    parseMode: ParseMode.Markdown,
                    cancellationToken: cancellationToken
                );
                return;
            }
            
            // Handle driver rating
            if (data.StartsWith("rate_"))
            {
                var rating = data.Replace("rate_", "");
                var stars = new string('⭐', int.Parse(rating));
                
                await _botClient.AnswerCallbackQueryAsync(
                    callbackQuery.Id,
                    "✅ Thank you for your feedback!",
                    cancellationToken: cancellationToken
                );
                
                await _botClient.SendTextMessageAsync(
                    chatId,
                    $"*Rating submitted*\n\n" +
                    $"You rated: {stars}\n" +
                    $"+10 Karma points earned\n\n" +
                    $"Thank you for riding with TimoRides.\n" +
                    $"Use /bookride for your next trip.",
                    parseMode: ParseMode.Markdown,
                    cancellationToken: cancellationToken
                );
                
                return;
            }
            
            // Default callback handling
            await _botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "Processing...",
                cancellationToken: cancellationToken
            );
        }
        
        /// <summary>
        /// Simulate real-time driver tracking updates
        /// </summary>
        private async Task SimulateDriverTrackingAsync(long chatId, string driverName, string bookingId)
        {
            try
            {
                var firstName = driverName.Split(' ')[0];
                
                // Update 1: Driver accepted (5 seconds)
                await Task.Delay(TimeSpan.FromSeconds(5));
                await _botClient.SendTextMessageAsync(
                    chatId,
                    $"*{firstName} accepted your ride*\n\n" +
                    $"4.2 km away • ETA 5 minutes",
                    parseMode: ParseMode.Markdown
                );
                
                // Update 2: Driver en route (8 seconds)
                await Task.Delay(TimeSpan.FromSeconds(8));
                await _botClient.SendTextMessageAsync(
                    chatId,
                    $"*{firstName} is on the way*\n\n" +
                    $"2.8 km away • ETA 3 minutes",
                    parseMode: ParseMode.Markdown
                );
                
                // Update 3: Driver nearby (8 seconds)
                await Task.Delay(TimeSpan.FromSeconds(8));
                await _botClient.SendTextMessageAsync(
                    chatId,
                    $"*{firstName} is nearby*\n\n" +
                    $"800 meters away • ETA 1 minute",
                    parseMode: ParseMode.Markdown
                );
                
                // Update 4: Driver arrived (10 seconds)
                await Task.Delay(TimeSpan.FromSeconds(10));
                await _botClient.SendAnimationAsync(
                    chatId,
                    animation: InputFile.FromUri("https://media.giphy.com/media/l0HlBO7eyXzSZkJri/giphy.gif"),
                    caption: $"*{firstName} has arrived*\n\n" +
                    $"Your driver is waiting at the pickup location.",
                    parseMode: ParseMode.Markdown
                );
                
                // Safety confirmation - verify vehicle before entering
                await Task.Delay(TimeSpan.FromSeconds(3));
                
                var vehicleDetails = GetVehicleDetails(driverName);
                var safetyKeyboard = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("✅ Confirmed, get in", "trip_confirm"),
                        InlineKeyboardButton.WithCallbackData("❌ Not my ride", "trip_cancel")
                    }
                });
                
                await _botClient.SendTextMessageAsync(
                    chatId,
                    $"*Safety Check*\n\n" +
                    $"Please confirm the vehicle details:\n\n" +
                    $"Driver: {driverName}\n" +
                    $"Vehicle: {vehicleDetails.car} ({vehicleDetails.color})\n" +
                    $"Plate: {vehicleDetails.plate}\n\n" +
                    $"Does everything match?",
                    parseMode: ParseMode.Markdown,
                    replyMarkup: safetyKeyboard
                );
                
                // Wait for user confirmation (they'll click the button)
                // In real implementation, would wait for callback. For demo, proceed after delay
                await Task.Delay(TimeSpan.FromSeconds(10));
                
                // Update 5: Trip started (after confirmation)
                await _botClient.SendTextMessageAsync(
                    chatId,
                    $"*Trip started*\n\n" +
                    $"Tracking: `{bookingId}`",
                    parseMode: ParseMode.Markdown
                );
                
                // Update 6: Approaching destination (15 seconds)
                await Task.Delay(TimeSpan.FromSeconds(15));
                await _botClient.SendTextMessageAsync(
                    chatId,
                    $"*Approaching destination*\n\n" +
                    $"500 meters • Arriving in 1 minute",
                    parseMode: ParseMode.Markdown
                );
                
                // Update 7: Trip completed (10 seconds)
                await Task.Delay(TimeSpan.FromSeconds(10));
                await _botClient.SendAnimationAsync(
                    chatId,
                    animation: InputFile.FromUri("https://media.giphy.com/media/l0MYt5jPR6QX5pnqM/giphy.gif"),
                    caption: $"*Trip completed*\n\n" +
                    $"Fare: R 420\n" +
                    $"Duration: 18 minutes\n" +
                    $"Distance: 12.5 km\n\n" +
                    $"Rate your experience with {firstName}:",
                    parseMode: ParseMode.Markdown,
                    replyMarkup: new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("⭐⭐⭐⭐⭐", "rate_5"),
                            InlineKeyboardButton.WithCallbackData("⭐⭐⭐⭐", "rate_4"),
                            InlineKeyboardButton.WithCallbackData("⭐⭐⭐", "rate_3")
                        }
                    })
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in driver tracking simulation");
            }
        }

        /// <summary>
        /// Handle errors during message processing
        /// </summary>
        private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            _logger.LogError(exception, "Telegram bot error occurred");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Handle /mintnft command - Mint an NFT badge
        /// Format: /mintnft <wallet> | <title> | <description>
        /// Example: /mintnft 7vX... | My Achievement | Completed 30-day challenge
        /// </summary>
        private async Task HandleMintNFTCommand(long chatId, User user, string[] args, CancellationToken cancellationToken)
        {
            try
            {
                _logger?.LogInformation($"[TelegramBot] User {user.Id} requested NFT mint");

                // Check if user has an OASIS avatar
                var avatarResult = await _telegramProvider.GetTelegramAvatarByTelegramIdAsync(user.Id);
                if (avatarResult == null || avatarResult.IsError || avatarResult.Result == null)
                {
                    await _botClient.SendTextMessageAsync(
                        chatId,
                        "❌ You need to /start first to create your OASIS avatar!",
                        cancellationToken: cancellationToken
                    );
                    return;
                }

                var avatar = avatarResult.Result;

                // Parse command: /mintnft wallet | title | description
                var fullMessage = string.Join(" ", args);
                var parts = fullMessage.Split('|');
                
                if (parts.Length < 3)
                {
                    await _botClient.SendTextMessageAsync(
                        chatId,
                        "❌ Usage: /mintnft <wallet> | <title> | <description>\n\n" +
                        "Example:\n" +
                        "/mintnft 7vX1234...abcd | Achievement Badge | Completed my first challenge!\n\n" +
                        "📝 You need:\n" +
                        "• Solana wallet address\n" +
                        "• NFT title\n" +
                        "• NFT description\n\n" +
                        "Separate each part with the | character",
                        cancellationToken: cancellationToken
                    );
                    return;
                }

                var wallet = parts[0].Trim();
                var title = parts[1].Trim();
                var description = parts[2].Trim();

                // Validate wallet address (basic check)
                if (wallet.Length < 32)
                {
                    await _botClient.SendTextMessageAsync(
                        chatId,
                        "❌ Invalid Solana wallet address. It should be 32-44 characters long.",
                        cancellationToken: cancellationToken
                    );
                    return;
                }

                // Validate title and description
                if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(description))
                {
                    await _botClient.SendTextMessageAsync(
                        chatId,
                        "❌ Title and description cannot be empty!",
                        cancellationToken: cancellationToken
                    );
                    return;
                }

                // Send "minting..." message
                await _botClient.SendTextMessageAsync(
                    chatId,
                    "🎨 Minting your NFT...\n\n" +
                    $"📝 Title: {title}\n" +
                    $"💭 Description: {description}\n" +
                    $"💰 Wallet: {wallet.Substring(0, 8)}...{wallet.Substring(wallet.Length - 4)}\n\n" +
                    "⏳ This may take 30-90 seconds...",
                    cancellationToken: cancellationToken
                );

                _logger?.LogInformation($"[TelegramBot] Calling NFT service to mint: {title}");

                // Call NFT service to mint
                var mintResult = await _nftService.MintTestNFTAsync(
                    title: title,
                    description: description,
                    recipientWallet: wallet,
                    mintedByAvatarId: avatar.Id
                );

                if (mintResult.IsError)
                {
                    _logger?.LogError($"[TelegramBot] NFT minting failed: {mintResult.Message}");
                    
                    await _botClient.SendTextMessageAsync(
                        chatId,
                        $"❌ NFT Minting Failed!\n\n" +
                        $"Error: {mintResult.Message}\n\n" +
                        $"💡 Tips:\n" +
                        $"• Make sure your wallet address is correct\n" +
                        $"• Check that the OASIS API is running\n" +
                        $"• Try again in a moment",
                        cancellationToken: cancellationToken
                    );
                }
                else
                {
                    _logger?.LogInformation($"[TelegramBot] NFT minted successfully!");
                    
                    await _botClient.SendTextMessageAsync(
                        chatId,
                        $"✅ NFT Minted Successfully! 🎉\n\n" +
                        $"🎨 Title: {title}\n" +
                        $"📝 Description: {description}\n" +
                        $"💰 Sent to: {wallet.Substring(0, 8)}...{wallet.Substring(wallet.Length - 4)}\n\n" +
                        $"🔍 Check your Solana wallet!\n" +
                        $"(Phantom, Solflare, or any SPL-compatible wallet)\n\n" +
                        $"🎊 Your achievement is now on-chain!",
                        cancellationToken: cancellationToken
                    );

                    // Award karma for minting an NFT
                    try
                    {
                        var karmaResult = await _avatarManager.AddKarmaToAvatarAsync(
                            avatar.Id,
                            NextGenSoftware.OASIS.API.Core.Enums.KarmaTypePositive.OurWorldBeAHero,
                            NextGenSoftware.OASIS.API.Core.Enums.KarmaSourceType.dApp,
                            "NFT Minted",
                            $"Minted NFT: {title}"
                        );

                        if (karmaResult != null)
                        {
                            await _botClient.SendTextMessageAsync(
                                chatId,
                                $"✨ Bonus: +50 Karma for minting an NFT!",
                                cancellationToken: cancellationToken
                            );
                        }
                    }
                    catch (Exception karmaEx)
                    {
                        _logger?.LogWarning(karmaEx, "Failed to award karma for NFT mint");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"[TelegramBot] Error in HandleMintNFTCommand");
                
                await _botClient.SendTextMessageAsync(
                    chatId,
                    $"❌ An unexpected error occurred while minting your NFT.\n\n" +
                    $"Error: {ex.Message}\n\n" +
                    $"Please try again later.",
                    cancellationToken: cancellationToken
                );
            }
        }

        /// <summary>
        /// Handle photo messages - upload to Pinata and mint NFT
        /// Caption format: wallet | title | description
        /// Example: 7vXZK6... | Achievement Badge | Completed my challenge!
        /// </summary>
        private async Task HandlePhotoMessageAsync(Telegram.Bot.Types.Message message, CancellationToken cancellationToken)
        {
            try
            {
                var chatId = message.Chat.Id;
                var user = message.From;
                var caption = message.Caption ?? "";
                
                _logger?.LogInformation($"[TelegramBot] User {user.Id} sent photo with caption: {caption}");

                // Check if user has an OASIS avatar
                var avatarResult = await _telegramProvider.GetTelegramAvatarByTelegramIdAsync(user.Id);
                if (avatarResult == null || avatarResult.IsError || avatarResult.Result == null)
                {
                    await _botClient.SendTextMessageAsync(
                        chatId,
                        "❌ You need to /start first to create your OASIS avatar!",
                        cancellationToken: cancellationToken
                    );
                    return;
                }

                // Parse caption: wallet | title | description
                var parts = caption.Split('|');
                if (parts.Length < 3)
                {
                    await _botClient.SendTextMessageAsync(
                        chatId,
                        "📸 Image received! To mint an NFT with this image, use this format:\n\n" +
                        "Send a photo with caption:\n" +
                        "<wallet> | <title> | <description>\n\n" +
                        "Example:\n" +
                        "7vXZK6SQ... | My Achievement | I completed something amazing!\n\n" +
                        "Or use /mintnft for text-only NFTs with placeholder images.",
                        cancellationToken: cancellationToken
                    );
                    return;
                }

                var wallet = parts[0].Trim();
                var title = parts[1].Trim();
                var description = parts[2].Trim();

                // Validate wallet
                if (wallet.Length < 32)
                {
                    await _botClient.SendTextMessageAsync(
                        chatId,
                        "❌ Invalid Solana wallet address. It should be 32-44 characters long.",
                        cancellationToken: cancellationToken
                    );
                    return;
                }

                await _botClient.SendTextMessageAsync(
                    chatId,
                    "🎨 Processing your image...\n" +
                    "1️⃣ Uploading to IPFS via Pinata...\n" +
                    "2️⃣ Minting your NFT...\n\n" +
                    "⏳ This may take 1-2 minutes...",
                    cancellationToken: cancellationToken
                );

                // Get the largest photo size
                var photo = message.Photo[message.Photo.Length - 1];
                
                // Download photo from Telegram
                var fileInfo = await _botClient.GetFileAsync(photo.FileId, cancellationToken);
                
                using var memoryStream = new System.IO.MemoryStream();
                await _botClient.DownloadFileAsync(fileInfo.FilePath, memoryStream, cancellationToken);
                var imageBytes = memoryStream.ToArray();

                _logger?.LogInformation($"[TelegramBot] Downloaded image: {imageBytes.Length} bytes");

                // Upload to Pinata
                var fileName = $"badge_{user.Id}_{DateTime.UtcNow.Ticks}.png";
                var uploadResult = await _pinataService.UploadImageAsync(imageBytes, fileName);

                if (uploadResult.IsError)
                {
                    await _botClient.SendTextMessageAsync(
                        chatId,
                        $"❌ Failed to upload image to IPFS: {uploadResult.Message}",
                        cancellationToken: cancellationToken
                    );
                    return;
                }

                var ipfsImageUrl = uploadResult.Result;
                _logger?.LogInformation($"[TelegramBot] Image uploaded to IPFS: {ipfsImageUrl}");

                await _botClient.SendTextMessageAsync(
                    chatId,
                    $"✅ Image uploaded to IPFS!\n" +
                    $"🔗 {ipfsImageUrl}\n\n" +
                    $"🎨 Now minting your NFT...",
                    cancellationToken: cancellationToken
                );

                // Mint NFT with the IPFS image
                var mintResult = await _nftService.MintAchievementNFTAsync(
                    title: title,
                    description: description,
                    recipientWallet: wallet,
                    mintedByAvatarId: avatarResult.Result.Id,
                    symbol: "BADGE",
                    imageUrl: ipfsImageUrl
                );

                if (mintResult.IsError)
                {
                    await _botClient.SendTextMessageAsync(
                        chatId,
                        $"❌ NFT Minting Failed!\n\n" +
                        $"Error: {mintResult.Message}\n\n" +
                        $"Your image is still on IPFS: {ipfsImageUrl}",
                        cancellationToken: cancellationToken
                    );
                }
                else
                {
                    await _botClient.SendTextMessageAsync(
                        chatId,
                        $"✅ NFT Minted Successfully! 🎉\n\n" +
                        $"🎨 Title: {title}\n" +
                        $"📝 Description: {description}\n" +
                        $"🖼️ Image: {ipfsImageUrl}\n" +
                        $"💰 Sent to: {wallet.Substring(0, 8)}...{wallet.Substring(wallet.Length - 4)}\n\n" +
                        $"🔍 Check your Solana wallet!\n" +
                        $"Your custom badge NFT is now on-chain! 🎊",
                        cancellationToken: cancellationToken
                    );

                    // Award extra karma for custom badge
                    try
                    {
                        var karmaResult = await _avatarManager.AddKarmaToAvatarAsync(
                            avatarResult.Result.Id,
                            NextGenSoftware.OASIS.API.Core.Enums.KarmaTypePositive.OurWorldBeASuperHero,
                            NextGenSoftware.OASIS.API.Core.Enums.KarmaSourceType.dApp,
                            "Custom Badge NFT",
                            $"Minted custom badge NFT: {title}"
                        );

                        if (karmaResult != null)
                        {
                            await _botClient.SendTextMessageAsync(
                                chatId,
                                $"✨ Bonus: +100 Karma for creating a custom badge NFT!",
                                cancellationToken: cancellationToken
                            );
                        }
                    }
                    catch (Exception karmaEx)
                    {
                        _logger?.LogWarning(karmaEx, "Failed to award karma for custom badge");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[TelegramBot] Error handling photo message");
                
                await _botClient.SendTextMessageAsync(
                    message.Chat.Id,
                    $"❌ An error occurred while processing your image.\n\n" +
                    $"Error: {ex.Message}",
                    cancellationToken: cancellationToken
                );
            }
        }

        /// <summary>
        /// Send message to a specific chat
        /// </summary>
        public async Task<OASISResult<bool>> SendMessageAsync(long chatId, string message)
        {
            try
            {
                await _botClient.SendTextMessageAsync(chatId, message);
                return new OASISResult<bool> { Result = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send message to chat {chatId}");
                return new OASISResult<bool> 
                { 
                    IsError = true, 
                    Message = ex.Message 
                };
            }
        }

        #region TimoRides Ride Booking Handlers

        /// <summary>
        /// /bookride - Start ride booking flow
        /// </summary>
        private async Task HandleBookRideCommand(long chatId, User user, CancellationToken cancellationToken)
        {
            var userId = user.Id;
            
            // Clear any old state
            _userBookingState.Remove(userId);
            _userPickupLocation.Remove(userId);
            _userDropoffLocation.Remove(userId);
            _selectedDriver.Remove(userId);
            
            // Request pickup location
            var keyboard = new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton[]
                {
                    KeyboardButton.WithRequestLocation("📍 Share My Location")
                }
            })
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = true
            };
            
            await _botClient.SendTextMessageAsync(
                chatId,
                "*Book a ride*\n\n" +
                "Share your pickup location:\n" +
                "• Tap the button below, or\n" +
                "• Type an address (e.g., 'uShaka Beach, Durban')",
                parseMode: ParseMode.Markdown,
                replyMarkup: keyboard,
                cancellationToken: cancellationToken
            );
            
            _userBookingState[userId] = "WAITING_PICKUP";
        }

        /// <summary>
        /// /myrides - Show ride history
        /// </summary>
        private async Task HandleMyRidesCommand(long chatId, User user, CancellationToken cancellationToken)
        {
            await _botClient.SendTextMessageAsync(
                chatId,
                "📋 <b>Your Ride History</b>\n\n" +
                "You haven't taken any rides yet.\n\n" +
                "Use /bookride to book your first ride!",
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken
            );
        }

        /// <summary>
        /// /track - Track active ride
        /// </summary>
        private async Task HandleTrackRideCommand(long chatId, User user, string[] args, CancellationToken cancellationToken)
        {
            if (args.Length == 0)
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    "❓ Please provide a booking ID\n\n" +
                    "Example: /track BK-20251105-A7X9",
                    cancellationToken: cancellationToken
                );
                return;
            }

            var bookingId = args[0];
            
            await _botClient.SendTextMessageAsync(
                chatId,
                $"🚖 *Tracking Ride*\n\n" +
                $"Booking ID: `{bookingId}`\n\n" +
                $"_Real-time tracking will be available in the next update!_",
                parseMode: ParseMode.Markdown,
                cancellationToken: cancellationToken
            );
        }
        
        private async Task HandleLocationAsync(Telegram.Bot.Types.Message message, CancellationToken cancellationToken)
        {
            var userId = message.From.Id;
            var chatId = message.Chat.Id;
            
            if (!_userBookingState.TryGetValue(userId, out var state))
                return;
                
            var location = message.Location;
            var address = $"{location.Latitude:F4}, {location.Longitude:F4}"; // Simplified for demo
            
            if (state == "WAITING_PICKUP")
            {
                _userPickupLocation[userId] = (location.Latitude, location.Longitude, address);
                
                var keyboard = new ReplyKeyboardMarkup(new[]
                {
                    new KeyboardButton[] { KeyboardButton.WithRequestLocation("📍 Share Destination") }
                })
                {
                    ResizeKeyboard = true,
                    OneTimeKeyboard = true
                };
                
                await _botClient.SendTextMessageAsync(
                    chatId,
                    $"*Pickup set*\n{address}\n\n" +
                    "Now share your destination:",
                    parseMode: ParseMode.Markdown,
                    replyMarkup: keyboard,
                    cancellationToken: cancellationToken
                );
                
                _userBookingState[userId] = "WAITING_DROPOFF";
            }
            else if (state == "WAITING_DROPOFF")
            {
                _userDropoffLocation[userId] = (location.Latitude, location.Longitude, address);
                
                await _botClient.SendTextMessageAsync(
                    chatId,
                    $"*Destination set*\n{address}\n\n" +
                    "Finding drivers...",
                    parseMode: ParseMode.Markdown,
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken
                );
                
                try
                {
                    await ShowMockDriversAsync(chatId, cancellationToken);
                    _userBookingState[userId] = "SELECTING_DRIVER";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error showing drivers");
                    await _botClient.SendTextMessageAsync(
                        chatId,
                        $"❌ Error loading drivers: {ex.Message}",
                        cancellationToken: cancellationToken
                    );
                }
            }
        }
        
        private async Task HandleAddressTextAsync(Telegram.Bot.Types.Message message, CancellationToken cancellationToken)
        {
            var userId = message.From.Id;
            var chatId = message.Chat.Id;
            var address = message.Text;
            
            var state = _userBookingState[userId];
            
            // Check if address is vague (my hotel, home, work, etc.) - ask for clarification
            var vaguePhrases = new[] { "my hotel", "the hotel", "my place", "home", "work", "office", "my house" };
            if (vaguePhrases.Any(p => address.ToLower().Contains(p)))
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    $"*Which {address.ToLower().Replace("my ", "").Replace("the ", "")}?*\n\n" +
                    "Please provide the full name or address.\n\n" +
                    "Example:\n" +
                    "• 'Hilton Durban Hotel'\n" +
                    "• '123 Marine Parade, Durban'",
                    parseMode: ParseMode.Markdown,
                    cancellationToken: cancellationToken
                );
                return;
            }
            
            // Use fake coordinates for demo (in real version, would geocode the address)
            var lat = -29.8587;
            var lon = 31.0218;
            
            if (state == "WAITING_PICKUP")
            {
                _userPickupLocation[userId] = (lat, lon, address);
                
                await _botClient.SendTextMessageAsync(
                    chatId,
                    $"*Pickup set*\n{address}\n\n" +
                    "Now, where would you like to go?",
                    parseMode: ParseMode.Markdown,
                    replyMarkup: new ReplyKeyboardMarkup(new[]
                    {
                        new KeyboardButton[] { KeyboardButton.WithRequestLocation("Share Destination") }
                    })
                    {
                        ResizeKeyboard = true,
                        OneTimeKeyboard = true
                    },
                    cancellationToken: cancellationToken
                );
                
                _userBookingState[userId] = "WAITING_DROPOFF";
            }
            else if (state == "WAITING_DROPOFF")
            {
                _userDropoffLocation[userId] = (lat, lon, address);
                
                await _botClient.SendTextMessageAsync(
                    chatId,
                    $"*Destination set*\n{address}\n\n" +
                    "Finding drivers...",
                    parseMode: ParseMode.Markdown,
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken
                );
                
                try
                {
                    await ShowMockDriversAsync(chatId, cancellationToken);
                    _userBookingState[userId] = "SELECTING_DRIVER";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error showing drivers");
                    await _botClient.SendTextMessageAsync(
                        chatId,
                        $"❌ Error: {ex.Message}",
                        cancellationToken: cancellationToken
                    );
                }
            }
        }
        
        private async Task ShowMockDriversAsync(long chatId, CancellationToken cancellationToken)
        {
            // Enhanced driver data with TimoRides branded photos: (id, name, car, color, rating, fare, eta, karma, trips, acceptance, onTime, photoUrl)
            // Note: Replace URLs below with actual uploaded TimoRides driver photos
            var drivers = new[]
            {
                ("driver1", "Jonathan", "Renault Kwid", "Red", "4.7", "R 320", "7 min", "680", "945", "96%", "93%", 
                 "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=800&h=800&fit=crop"),
                ("driver2", "Eddison", "VW Polo", "Silver", "4.9", "R 380", "4 min", "920", "1,534", "99%", "97%",
                 "https://images.unsplash.com/photo-1531123897727-8f129e1688ce?w=800&h=800&fit=crop"),
                ("driver3", "Sipho Mkhize", "Toyota Corolla", "Black", "4.8", "R 420", "6 min", "850", "1,247", "98%", "96%",
                 "https://images.unsplash.com/photo-1506794778202-cad84cf45f1d?w=800&h=800&fit=crop")
            };
            
            _logger.LogInformation($"Showing {drivers.Length} drivers to chat {chatId}");
            
            // Send searching animation
            await _botClient.SendAnimationAsync(
                chatId,
                animation: InputFile.FromUri("https://media.giphy.com/media/3oEjI6SIIHBdRxXI40/giphy.gif"),
                caption: "*3 drivers available*\n\nSelect your driver:",
                parseMode: ParseMode.Markdown,
                cancellationToken: cancellationToken
            );
            
            await Task.Delay(500, cancellationToken);
            
            foreach (var (id, name, car, color, rating, fare, eta, karma, trips, acceptance, onTime, photoUrl) in drivers)
            {
                _logger.LogInformation($"Sending driver card with photo: {name}");
                
                var keyboard = new InlineKeyboardMarkup(new[]
                {
                    InlineKeyboardButton.WithCallbackData($"✅ Select {name.Split(' ')[0]}", $"select_{id}")
                });
                
                // Clean, professional caption
                var caption = 
                    $"*{name}*\n" +
                    $"Rating: {rating} ⭐ • {trips} trips\n\n" +
                    $"{car} ({color})\n" +
                    $"Fare: {fare} • Arrives in {eta}\n\n" +
                    $"Karma: {karma} • Acceptance: {acceptance} • On-time: {onTime}";
                
                try
                {
                    await _botClient.SendPhotoAsync(
                        chatId,
                        photo: InputFile.FromUri(photoUrl),
                        caption: caption,
                        parseMode: ParseMode.Markdown,
                        replyMarkup: keyboard,
                        cancellationToken: cancellationToken
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Failed to send photo for {name}, sending text instead");
                    // Fallback to text if photo fails
                    await _botClient.SendTextMessageAsync(
                        chatId,
                        $"👤 {caption}",
                        parseMode: ParseMode.Markdown,
                        replyMarkup: keyboard,
                        cancellationToken: cancellationToken
                    );
                }
                
                await Task.Delay(400, cancellationToken);
            }
            
            _logger.LogInformation("All driver cards sent successfully");
        }
        
        /// <summary>
        /// Process natural language with AI to understand ride booking intent
        /// </summary>
        private async Task ProcessNaturalLanguageAsync(long chatId, User user, string userMessage, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation($"Processing natural language: '{userMessage}'");
                
                // Extract passenger info first
                var passengerInfo = ExtractPassengerInfo(userMessage);
                if (passengerInfo.recommendation != null)
                {
                    await _botClient.SendTextMessageAsync(
                        chatId,
                        $"*{passengerInfo.recommendation}*\n\n" +
                        (passengerInfo.passengers.HasValue && passengerInfo.passengers.Value >= 6 
                            ? "I'll show you SUV and larger vehicle options.\n\n" 
                            : "") +
                        "Now, where are you going?",
                        parseMode: ParseMode.Markdown,
                        cancellationToken: cancellationToken
                    );
                    
                    if (passengerInfo.passengers.HasValue && passengerInfo.passengers.Value >= 6)
                        _userVehiclePreference[user.Id] = "SUV";
                }
                
                // Try OpenAI first if API key is available
                if (!string.IsNullOrEmpty(OPENAI_API_KEY))
                {
                    var aiResult = await ProcessWithOpenAI(userMessage);
                    if (aiResult != null)
                    {
                        await HandleAIExtractedBooking(chatId, user, aiResult, cancellationToken);
                        return;
                    }
                }
                
                // Fallback to rule-based extraction
                var lowerMessage = userMessage.ToLower();
                
                // Extract AI features
                var scheduledTime = ExtractScheduledTime(userMessage);
                var vehiclePreference = ExtractVehiclePreference(userMessage);
                var multiStops = ExtractMultiStops(userMessage);
                
                // Detect ride booking intent
                if (lowerMessage.Contains("ride") || lowerMessage.Contains("book") || 
                    lowerMessage.Contains("driver") || lowerMessage.Contains("taxi") ||
                    lowerMessage.Contains("take me") || lowerMessage.Contains("go to") ||
                    lowerMessage.Contains("need to get to"))
                {
                    var responseText = "*I can help you book a ride*\n\n";
                    
                    // Show what we extracted
                    if (scheduledTime.HasValue)
                    {
                        _userScheduledTime[user.Id] = scheduledTime.Value;
                        responseText += $"Scheduled for: {scheduledTime.Value:MMM dd, h:mm tt}\n";
                    }
                    if (!string.IsNullOrEmpty(vehiclePreference))
                    {
                        _userVehiclePreference[user.Id] = vehiclePreference;
                        responseText += $"Vehicle preference: {vehiclePreference}\n";
                    }
                    if (multiStops.Any())
                    {
                        _userMultiStops[user.Id] = multiStops;
                        responseText += $"Stops: {string.Join(" → ", multiStops)}\n";
                    }
                    
                    responseText += "\nUse /bookride to get started, or tell me:\n" +
                                   "• Where you're going (e.g., 'Gateway Mall to King Shaka Airport')\n" +
                                   "• Just your destination (e.g., 'Take me to uShaka Beach')";
                    
                    await _botClient.SendTextMessageAsync(
                        chatId,
                        responseText,
                        parseMode: ParseMode.Markdown,
                        cancellationToken: cancellationToken
                    );
                    return;
                }
                
                // Detect location mentions - auto-extract pickup and destination
                if (ContainsLocationKeywords(lowerMessage))
                {
                    var locations = ExtractLocations(userMessage);
                    if (locations.pickup != null && locations.destination != null)
                    {
                        var confirmationText = "*Got it!*\n\n" +
                            $"Pickup: {locations.pickup}\n" +
                            $"Destination: {locations.destination}\n";
                        
                        // Add extracted preferences
                        if (scheduledTime.HasValue)
                        {
                            _userScheduledTime[user.Id] = scheduledTime.Value;
                            confirmationText += $"Time: {scheduledTime.Value:h:mm tt on MMM dd}\n";
                        }
                        if (!string.IsNullOrEmpty(vehiclePreference))
                        {
                            _userVehiclePreference[user.Id] = vehiclePreference;
                            confirmationText += $"Preference: {vehiclePreference}\n";
                        }
                        if (multiStops.Any())
                        {
                            _userMultiStops[user.Id] = multiStops;
                            confirmationText += $"Via: {string.Join(", ", multiStops)}\n";
                        }
                        
                        confirmationText += "\nFinding drivers...";
                        
                        await _botClient.SendTextMessageAsync(
                            chatId,
                            confirmationText,
                            parseMode: ParseMode.Markdown,
                            cancellationToken: cancellationToken
                        );
                        
                        // Store locations and show drivers
                        _userPickupLocation[user.Id] = (0, 0, locations.pickup);
                        _userDropoffLocation[user.Id] = (0, 0, locations.destination);
                        await ShowMockDriversAsync(chatId, cancellationToken);
                        _userBookingState[user.Id] = "SELECTING_DRIVER";
                        return;
                    }
                }
                
                // General conversational AI
                await _botClient.SendTextMessageAsync(
                    chatId,
                    "I'm TimoRides AI assistant. How can I help you today?\n\n" +
                    "Try saying:\n" +
                    "• 'I need a ride to the airport tomorrow at 3pm'\n" +
                    "• 'Book me a luxury car from Gateway to King Shaka'\n" +
                    "• 'Take me from Durban to Ballito via uShaka Beach'\n" +
                    "• Or use /bookride",
                    parseMode: ParseMode.Markdown,
                    cancellationToken: cancellationToken
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing natural language");
                await _botClient.SendTextMessageAsync(
                    chatId,
                    "I didn't quite catch that. Use /help to see available commands.",
                    cancellationToken: cancellationToken
                );
            }
        }
        
        /// <summary>
        /// Process message with OpenAI to extract booking details
        /// </summary>
        private async Task<AIBookingResult> ProcessWithOpenAI(string userMessage)
        {
            try
            {
                var systemPrompt = @"You are a ride-booking assistant. Extract booking details from user messages.
Respond ONLY with a JSON object in this exact format:
{
  ""intent"": ""book_ride"" or ""general"",
  ""pickup"": ""location or null"",
  ""destination"": ""location or null"",
  ""scheduledTime"": ""ISO datetime or null"",
  ""vehicleType"": ""luxury/economy/suv/sedan or null"",
  ""multiStops"": [""location1"", ""location2""] or []
}";

                var requestBody = new
                {
                    model = "gpt-4o-mini",
                    messages = new[]
                    {
                        new { role = "system", content = systemPrompt },
                        new { role = "user", content = userMessage }
                    },
                    temperature = 0.3,
                    max_tokens = 200
                };
                
                var json = System.Text.Json.JsonSerializer.Serialize(requestBody);
                var content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");
                
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {OPENAI_API_KEY}");
                
                var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"OpenAI API failed: {response.StatusCode}");
                    return null;
                }
                
                var responseText = await response.Content.ReadAsStringAsync();
                var responseJson = System.Text.Json.JsonDocument.Parse(responseText);
                var aiResponse = responseJson.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();
                
                _logger.LogInformation($"OpenAI response: {aiResponse}");
                
                // Parse AI JSON response
                var result = System.Text.Json.JsonSerializer.Deserialize<AIBookingResult>(aiResponse);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OpenAI processing failed");
                return null;
            }
        }
        
        /// <summary>
        /// Handle booking extracted by AI - Step-by-step confirmation flow
        /// </summary>
        private async Task HandleAIExtractedBooking(long chatId, User user, AIBookingResult aiResult, CancellationToken cancellationToken)
        {
            if (aiResult.intent != "book_ride")
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    "I'm here to help you book rides. Try saying:\n" +
                    "• 'I need a ride to the airport tomorrow at 3pm'\n" +
                    "• 'Book me a luxury car from Gateway to King Shaka'",
                    cancellationToken: cancellationToken
                );
                return;
            }
            
            // Store extracted preferences for later
            if (!string.IsNullOrEmpty(aiResult.scheduledTime) && aiResult.scheduledTime != "null")
            {
                if (DateTime.TryParse(aiResult.scheduledTime, out var scheduledDt))
                    _userScheduledTime[user.Id] = scheduledDt;
            }
            
            if (!string.IsNullOrEmpty(aiResult.vehicleType) && aiResult.vehicleType != "null")
                _userVehiclePreference[user.Id] = aiResult.vehicleType;
                
            if (aiResult.multiStops != null && aiResult.multiStops.Any())
                _userMultiStops[user.Id] = aiResult.multiStops.ToList();
            
            // Store destination for later steps
            if (!string.IsNullOrEmpty(aiResult.destination) && aiResult.destination != "null")
                _userDropoffLocation[user.Id] = (0, 0, aiResult.destination);
            
            // STEP 1: Validate and confirm PICKUP location ONLY
            var hasPickup = !string.IsNullOrEmpty(aiResult.pickup) && aiResult.pickup != "null";
            
            if (!hasPickup)
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    "*Let's start with your pickup location*\n\n" +
                    "Where should I pick you up?\n\n" +
                    "Example: 'Gateway Mall' or 'King Shaka Airport'",
                    parseMode: ParseMode.Markdown,
                    cancellationToken: cancellationToken
                );
                _userBookingState[user.Id] = "WAITING_PICKUP";
                return;
            }
            
            // Validate pickup
            var pickupOptions = GetLocationOptions(aiResult.pickup);
            
            // Pickup too vague
            if (pickupOptions.Count == 0)
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    $"*I need more details about '{aiResult.pickup}'*\n\n" +
                    "Please provide a more specific pickup location.\n\n" +
                    "Example: 'Gateway Mall, Umhlanga'",
                    parseMode: ParseMode.Markdown,
                    cancellationToken: cancellationToken
                );
                _userBookingState[user.Id] = "WAITING_PICKUP";
                return;
            }
            
            // Multiple pickup options - ask user to choose
            if (pickupOptions.Count > 1)
            {
                var keyboard = new InlineKeyboardMarkup(
                    pickupOptions.Select(loc => new[] { 
                        InlineKeyboardButton.WithCallbackData(loc, $"pickup_{loc}") 
                    })
                );
                
                await _botClient.SendTextMessageAsync(
                    chatId,
                    $"*Confirm your pickup location*\n\nWhich one?",
                    parseMode: ParseMode.Markdown,
                    replyMarkup: keyboard,
                    cancellationToken: cancellationToken
                );
                
                _userBookingState[user.Id] = "CONFIRMING_PICKUP";
                return;
            }
            
            // Pickup is clear - store it and move to STEP 2: DESTINATION
            _userPickupLocation[user.Id] = (0, 0, pickupOptions[0]);
            
            // STEP 2: Now validate DESTINATION
            var hasDestination = !string.IsNullOrEmpty(aiResult.destination) && aiResult.destination != "null";
            
            if (!hasDestination)
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    $"*Pickup confirmed: {pickupOptions[0]}*\n\n" +
                    "Where are you going?",
                    parseMode: ParseMode.Markdown,
                    cancellationToken: cancellationToken
                );
                _userBookingState[user.Id] = "WAITING_DROPOFF";
                return;
            }
            
            // Validate destination
            var destOptions = GetLocationOptions(aiResult.destination);
            
            // Destination too vague
            if (destOptions.Count == 0)
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    $"*Pickup confirmed: {pickupOptions[0]}*\n\n" +
                    $"I need more details about '{aiResult.destination}'.\n\n" +
                    "Please provide the full name or address.\n\n" +
                    "Example: 'Sheraton Hotel, Marine Parade'",
                    parseMode: ParseMode.Markdown,
                    cancellationToken: cancellationToken
                );
                _userBookingState[user.Id] = "WAITING_DROPOFF";
                return;
            }
            
            // Multiple destination options - ask user to choose  
            if (destOptions.Count > 1)
            {
                var keyboard = new InlineKeyboardMarkup(
                    destOptions.Select(loc => new[] { 
                        InlineKeyboardButton.WithCallbackData(loc, $"dest_{loc}") 
                    })
                );
                
                await _botClient.SendTextMessageAsync(
                    chatId,
                    $"*Pickup confirmed: {pickupOptions[0]}*\n\n" +
                    $"*Which destination?*\n\nI found multiple options:",
                    parseMode: ParseMode.Markdown,
                    replyMarkup: keyboard,
                    cancellationToken: cancellationToken
                );
                
                _userBookingState[user.Id] = "CONFIRMING_DESTINATION";
                return;
            }
            
            // Both locations are clear! Now confirm everything BEFORE showing drivers
            _userDropoffLocation[user.Id] = (0, 0, destOptions[0]);
            
            await ConfirmBookingDetailsAsync(chatId, user.Id, pickupOptions[0], destOptions[0], cancellationToken);
        }
        
        /// <summary>
        /// Check if message contains location keywords
        /// </summary>
        private bool ContainsLocationKeywords(string message)
        {
            var keywords = new[] { "from", "to", "at", "near", "beach", "mall", "airport", "street", "road", "avenue" };
            return keywords.Any(k => message.Contains(k));
        }
        
        /// <summary>
        /// AI booking result structure
        /// </summary>
        private class AIBookingResult
        {
            public string intent { get; set; }
            public string pickup { get; set; }
            public string destination { get; set; }
            public string scheduledTime { get; set; }
            public string vehicleType { get; set; }
            public string[] multiStops { get; set; }
        }
        
        /// <summary>
        /// Extract pickup and destination from natural language
        /// </summary>
        private (string pickup, string destination) ExtractLocations(string message)
        {
            // Simple pattern matching for "from X to Y" or "X to Y"
            var patterns = new[]
            {
                @"from\s+(.+?)\s+to\s+(.+)",
                @"(.+?)\s+to\s+(.+)",
                @"take me to\s+(.+)",
                @"go to\s+(.+)",
                @"going to\s+(.+)"
            };
            
            foreach (var pattern in patterns)
            {
                var match = System.Text.RegularExpressions.Regex.Match(
                    message, 
                    pattern, 
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase
                );
                
                if (match.Success)
                {
                    if (match.Groups.Count == 3)
                    {
                        // "from X to Y" format
                        return (match.Groups[1].Value.Trim(), match.Groups[2].Value.Trim());
                    }
                    else if (match.Groups.Count == 2)
                    {
                        // "to X" format - destination only
                        return (null, match.Groups[1].Value.Trim());
                    }
                }
            }
            
            return (null, null);
        }
        
        /// <summary>
        /// Extract scheduled time from natural language (e.g., "tomorrow at 3pm", "in 2 hours")
        /// </summary>
        private DateTime? ExtractScheduledTime(string message)
        {
            var lowerMessage = message.ToLower();
            var now = DateTime.Now;
            
            // Tomorrow
            if (lowerMessage.Contains("tomorrow"))
            {
                var time = ExtractTimeOfDay(message) ?? new TimeSpan(9, 0, 0); // Default 9am
                return now.Date.AddDays(1).Add(time);
            }
            
            // Today
            if (lowerMessage.Contains("today") || lowerMessage.Contains("this evening") || lowerMessage.Contains("tonight"))
            {
                var time = ExtractTimeOfDay(message) ?? new TimeSpan(18, 0, 0); // Default 6pm
                return now.Date.Add(time);
            }
            
            // Specific time patterns (e.g., "at 3pm", "at 15:00")
            var timeMatch = System.Text.RegularExpressions.Regex.Match(
                message,
                @"at\s+(\d{1,2})(?::(\d{2}))?\s*(am|pm)?",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );
            
            if (timeMatch.Success)
            {
                var hour = int.Parse(timeMatch.Groups[1].Value);
                var minute = timeMatch.Groups[2].Success ? int.Parse(timeMatch.Groups[2].Value) : 0;
                var isPm = timeMatch.Groups[3].Value.ToLower() == "pm";
                
                if (isPm && hour < 12) hour += 12;
                if (!isPm && hour == 12) hour = 0;
                
                var scheduledTime = now.Date.Add(new TimeSpan(hour, minute, 0));
                if (scheduledTime < now) scheduledTime = scheduledTime.AddDays(1);
                
                return scheduledTime;
            }
            
            // Relative time (e.g., "in 2 hours", "in 30 minutes")
            var relativeMatch = System.Text.RegularExpressions.Regex.Match(
                message,
                @"in\s+(\d+)\s*(hour|minute|min)s?",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );
            
            if (relativeMatch.Success)
            {
                var amount = int.Parse(relativeMatch.Groups[1].Value);
                var unit = relativeMatch.Groups[2].Value.ToLower();
                
                if (unit.StartsWith("hour"))
                    return now.AddHours(amount);
                else
                    return now.AddMinutes(amount);
            }
            
            return null;
        }
        
        /// <summary>
        /// Extract time of day from message
        /// </summary>
        private TimeSpan? ExtractTimeOfDay(string message)
        {
            var match = System.Text.RegularExpressions.Regex.Match(
                message,
                @"(\d{1,2})(?::(\d{2}))?\s*(am|pm)",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );
            
            if (match.Success)
            {
                var hour = int.Parse(match.Groups[1].Value);
                var minute = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : 0;
                var isPm = match.Groups[3].Value.ToLower() == "pm";
                
                if (isPm && hour < 12) hour += 12;
                if (!isPm && hour == 12) hour = 0;
                
                return new TimeSpan(hour, minute, 0);
            }
            
            return null;
        }
        
        /// <summary>
        /// Extract vehicle preference (luxury, economy, SUV, etc.)
        /// </summary>
        private string ExtractVehiclePreference(string message)
        {
            var lowerMessage = message.ToLower();
            
            if (lowerMessage.Contains("luxury") || lowerMessage.Contains("premium") || lowerMessage.Contains("merc"))
                return "Luxury";
            if (lowerMessage.Contains("economy") || lowerMessage.Contains("cheap") || lowerMessage.Contains("budget"))
                return "Economy";
            if (lowerMessage.Contains("suv") || lowerMessage.Contains("big car") || lowerMessage.Contains("large"))
                return "SUV";
            if (lowerMessage.Contains("sedan") || lowerMessage.Contains("standard"))
                return "Sedan";
                
            return null;
        }
        
        /// <summary>
        /// Extract multi-stop locations (e.g., "via Gateway Mall", "stop at uShaka")
        /// </summary>
        private List<string> ExtractMultiStops(string message)
        {
            var stops = new List<string>();
            var lowerMessage = message.ToLower();
            
            // Pattern: "via X" or "stop at X"
            var viaMatches = System.Text.RegularExpressions.Regex.Matches(
                message,
                @"(?:via|stop at|stopping at)\s+([^,\.]+)",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );
            
            foreach (System.Text.RegularExpressions.Match match in viaMatches)
            {
                if (match.Success)
                {
                    stops.Add(match.Groups[1].Value.Trim());
                }
            }
            
            return stops;
        }
        
        /// <summary>
        /// Handle user updates while in confirmation phase (passenger count, preferences)
        /// </summary>
        private async Task HandleConfirmationUpdatesAsync(long chatId, long userId, string message, CancellationToken cancellationToken)
        {
            var lowerMessage = message.ToLower();
            
            // Extract passenger count
            var passengerInfo = ExtractPassengerInfo(message);
            if (passengerInfo.passengers.HasValue)
            {
                var vehicleRecommendation = passengerInfo.passengers.Value >= 6 ? "SUV" :
                                           passengerInfo.passengers.Value >= 4 ? "Sedan" : "Standard";
                _userVehiclePreference[userId] = vehicleRecommendation;
                
                await _botClient.SendTextMessageAsync(
                    chatId,
                    $"*Updated: {passengerInfo.passengers.Value} passengers*\n\n" +
                    $"Recommended vehicle: {vehicleRecommendation}",
                    parseMode: ParseMode.Markdown,
                    cancellationToken: cancellationToken
                );
            }
            
            // Extract vehicle preference
            var vehiclePref = ExtractVehiclePreference(message);
            if (!string.IsNullOrEmpty(vehiclePref))
            {
                _userVehiclePreference[userId] = vehiclePref;
                
                await _botClient.SendTextMessageAsync(
                    chatId,
                    $"*Updated: {vehiclePref} vehicle*",
                    parseMode: ParseMode.Markdown,
                    cancellationToken: cancellationToken
                );
            }
            
            // Re-show confirmation with updated details
            if (_userPickupLocation.TryGetValue(userId, out var pickup) &&
                _userDropoffLocation.TryGetValue(userId, out var dest))
            {
                await Task.Delay(500);
                await ConfirmBookingDetailsAsync(chatId, userId, pickup.addr, dest.addr, cancellationToken);
            }
        }
        
        /// <summary>
        /// Show booking summary and ask for final confirmation
        /// </summary>
        private async Task ConfirmBookingDetailsAsync(long chatId, long userId, string pickup, string destination, CancellationToken cancellationToken)
        {
            var summaryText = "*Please confirm your booking*\n\n" +
                $"Pickup: {pickup}\n" +
                $"Destination: {destination}\n";
            
            // Add any stored preferences
            if (_userScheduledTime.TryGetValue(userId, out var scheduledTime))
            {
                summaryText += $"Time: {scheduledTime:h:mm tt on MMM dd}\n";
            }
            
            if (_userVehiclePreference.TryGetValue(userId, out var vehiclePref))
            {
                summaryText += $"Vehicle preference: {vehiclePref}\n";
            }
            
            if (_userMultiStops.TryGetValue(userId, out var stops))
            {
                summaryText += $"Via: {string.Join(", ", stops)}\n";
            }
            
            summaryText += "\n*Additional options:*\n";
            summaryText += "• Reply with passenger count (e.g., '4 people')\n";
            summaryText += "• Request vehicle type (e.g., 'luxury car')\n";
            summaryText += "• Or click 'Find drivers' to continue\n";
            
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("✅ Find drivers", "confirm_yes"),
                    InlineKeyboardButton.WithCallbackData("❌ Cancel", "confirm_cancel")
                }
            });
            
            await _botClient.SendTextMessageAsync(
                chatId,
                summaryText,
                parseMode: ParseMode.Markdown,
                replyMarkup: keyboard,
                cancellationToken: cancellationToken
            );
            
            _userBookingState[userId] = "AWAITING_CONFIRMATION";
        }
        
        /// <summary>
        /// Proceed with booking after locations are confirmed
        /// </summary>
        private async Task ProceedWithConfirmedBooking(long chatId, long userId, string pickup, string destination, CancellationToken cancellationToken)
        {
            var confirmText = $"*Searching for drivers...*\n\n" +
                $"Pickup: {pickup}\n" +
                $"Destination: {destination}";
            
            await _botClient.SendTextMessageAsync(
                chatId,
                confirmText,
                parseMode: ParseMode.Markdown,
                cancellationToken: cancellationToken
            );
            
            await ShowMockDriversAsync(chatId, cancellationToken);
            _userBookingState[userId] = "SELECTING_DRIVER";
        }
        
        /// <summary>
        /// Get vehicle details for safety check
        /// </summary>
        private (string car, string color, string plate) GetVehicleDetails(string driverName)
        {
            var firstName = driverName.Split(' ')[0].ToLower();
            
            return firstName switch
            {
                "jonathan" => ("Renault Kwid", "Red", "ND 862-688"),
                "eddison" => ("VW Polo", "Silver", "ND 923-856"),
                "sipho" => ("Toyota Corolla", "Black", "NKZ 234 GP"),
                _ => ("Unknown Vehicle", "Unknown", "XXX XXX")
            };
        }
        
        /// <summary>
        /// Extract passenger count and recommend appropriate vehicle
        /// </summary>
        private (int? passengers, string recommendation) ExtractPassengerInfo(string message)
        {
            var lowerMessage = message.ToLower();
            
            // Pattern: "family of X", "X people", "X passengers"
            var countMatch = System.Text.RegularExpressions.Regex.Match(
                message,
                @"(?:family of|group of|with|party of)?\s*(\d+)\s*(?:people|passengers|person|pax)?",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );
            
            if (countMatch.Success)
            {
                var count = int.Parse(countMatch.Groups[1].Value);
                
                if (count >= 6)
                    return (count, "You'll need an SUV or XL vehicle for 6+ passengers");
                else if (count >= 4)
                    return (count, "Standard sedans fit up to 4 passengers comfortably");
            }
            
            // Keywords
            if (lowerMessage.Contains("family") || lowerMessage.Contains("group"))
                return (null, "How many passengers? This helps me find the right vehicle size.");
            
            return (null, null);
        }
        
        /// <summary>
        /// Get possible location matches for ambiguous names
        /// </summary>
        private List<string> GetLocationOptions(string location)
        {
            var lowerLoc = location.ToLower();
            
            // Airport variations
            if (lowerLoc.Contains("airport"))
            {
                if (lowerLoc.Contains("king shaka") || lowerLoc.Contains("durban"))
                    return new List<string> { "King Shaka International Airport" };
                else
                    return new List<string>
                    {
                        "King Shaka International Airport (Durban)",
                        "Virginia Airport (Durban North)"
                    };
            }
            
            // Hotel chains - ALWAYS show options with addresses
            var hotelChains = new Dictionary<string, List<string>>
            {
                ["hilton"] = new List<string>
                {
                    "Hilton Durban (12-14 Walnut Rd, Durban)",
                    "DoubleTree by Hilton (Umhlanga)",
                    "Hilton Garden Inn (Umhlanga Ridge)"
                },
                ["sheraton"] = new List<string>
                {
                    "Sheraton Durban (Marine Parade)",
                    "Four Points by Sheraton (Durban)"
                },
                ["marriott"] = new List<string>
                {
                    "Marriott Hotel Durban",
                    "Protea Hotel by Marriott (Umhlanga)"
                },
                ["southern sun"] = new List<string>
                {
                    "Southern Sun Elangeni & Maharani (Marine Parade)",
                    "Southern Sun North Beach (Durban)"
                },
                ["protea"] = new List<string>
                {
                    "Protea Hotel Edward (Marine Parade)",
                    "Protea Hotel Umhlanga"
                }
            };
            
            foreach (var chain in hotelChains)
            {
                if (lowerLoc.Contains(chain.Key))
                    return chain.Value;
            }
            
            // Generic "hotel" without specific name
            if (lowerLoc.Contains("hotel") && lowerLoc.Split(' ').Length <= 3)
            {
                return new List<string>
                {
                    "Hilton Durban (Marine Parade)",
                    "Sheraton Durban (Marine Parade)",
                    "Southern Sun Elangeni (Marine Parade)",
                    "Beverly Hills Hotel (Umhlanga)"
                };
            }
            
            // Mall variations
            if (lowerLoc.Contains("mall"))
            {
                if (lowerLoc.Contains("gateway"))
                    return new List<string> { "Gateway Theatre of Shopping (Umhlanga)" };
                else
                    return new List<string>
                    {
                        "Gateway Theatre of Shopping (Umhlanga)",
                        "Pavilion Shopping Centre (Westville)",
                        "The Workshop Shopping Centre (City Centre)"
                    };
            }
            
            // Beach variations
            if (lowerLoc.Contains("beach"))
            {
                if (lowerLoc.Contains("ushaka"))
                    return new List<string> { "uShaka Beach (Marine Parade)" };
                else
                    return new List<string>
                    {
                        "uShaka Beach (Marine Parade)",
                        "Durban Beachfront (Golden Mile)",
                        "Umhlanga Beach (Umhlanga Rocks)"
                    };
            }
            
            // If no specific address given, it's ambiguous
            var hasStreetNumber = System.Text.RegularExpressions.Regex.IsMatch(location, @"\d+");
            if (!hasStreetNumber && lowerLoc.Split(' ').Length <= 3)
            {
                // Too vague - needs clarification
                return new List<string>(); // Will trigger different flow
            }
            
            // Specific enough - return single option
            return new List<string> { location };
        }

        #endregion
    }
}

