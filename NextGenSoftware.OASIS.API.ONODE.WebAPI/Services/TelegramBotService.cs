using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.TelegramOASIS;
using NextGenSoftware.OASIS.API.Providers.TelegramOASIS.Models;
using NextGenSoftware.OASIS.API.Providers.TelegramOASIS.Models.TimoRides;
using NextGenSoftware.OASIS.API.Providers.TelegramOASIS.Services;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services
{
    /// <summary>
    /// Service for handling Telegram bot commands and interactions
    /// </summary>
    public partial class TelegramBotService
    {
        private readonly TelegramBotClient _botClient;
        private readonly TelegramOASIS _telegramProvider;
        private readonly AvatarManager _avatarManager;
        private readonly AchievementManager _achievementManager;
        private readonly TimoRidesApiService _timoRidesService;
        private readonly RideBookingStateManager _rideStateManager;
        private readonly GoogleMapsService _mapsService;
        private CancellationTokenSource _cts;

        public TelegramBotService(
            string botToken,
            TelegramOASIS telegramProvider,
            AvatarManager avatarManager,
            AchievementManager achievementManager,
            TimoRidesApiService timoRidesService,
            RideBookingStateManager rideStateManager,
            GoogleMapsService mapsService)
        {
            _botClient = new TelegramBotClient(botToken);
            _telegramProvider = telegramProvider;
            _avatarManager = avatarManager;
            _achievementManager = achievementManager;
            _timoRidesService = timoRidesService;
            _rideStateManager = rideStateManager;
            _mapsService = mapsService;
        }

        /// <summary>
        /// Start receiving bot updates
        /// </summary>
        public void StartReceiving()
        {
            _cts = new CancellationTokenSource();

            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>() // receive all update types
            };

            _botClient.StartReceiving(
                updateHandler: HandleUpdateAsync,
                pollingErrorHandler: HandlePollingErrorAsync,
                receiverOptions: receiverOptions,
                cancellationToken: _cts.Token
            );
        }

        /// <summary>
        /// Stop receiving bot updates
        /// </summary>
        public void StopReceiving()
        {
            _cts?.Cancel();
        }

        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Type == UpdateType.Message && update.Message?.Location != null)
            {
                await HandleLocationMessageAsync(update.Message, cancellationToken);
                return;
            }

            if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery != null)
            {
                await HandleRideCallbackQueryAsync(update.CallbackQuery, cancellationToken);
                return;
            }

            if (update.Message is not { } message)
                return;
            
            if (message.Text is not { } messageText)
                return;

            var chatId = message.Chat.Id;
            var userId = message.From.Id;
            var username = message.From.Username ?? "";
            var firstName = message.From.FirstName ?? "";
            var lastName = message.From.LastName ?? "";

            Console.WriteLine($"Received message '{messageText}' from {username} in chat {chatId}");

            try
            {
                // Handle commands
                if (messageText.StartsWith("/"))
                {
                    await HandleCommandAsync(message, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling update: {ex.Message}");
                await botClient.SendTextMessageAsync(chatId, "❌ An error occurred processing your request.", cancellationToken: cancellationToken);
            }
        }

        private async Task HandleCommandAsync(Message message, CancellationToken cancellationToken)
        {
            var messageText = message.Text;
            var chatId = message.Chat.Id;
            var userId = message.From.Id;
            var username = message.From.Username ?? "";
            var firstName = message.From.FirstName ?? "";
            var lastName = message.From.LastName ?? "";

            var parts = messageText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var command = parts[0].ToLower();

            switch (command)
            {
                case "/start":
                    await HandleStartCommandAsync(chatId, userId, username, firstName, lastName, cancellationToken);
                    break;

                case "/creategroup":
                    if (parts.Length < 2)
                    {
                        await _botClient.SendTextMessageAsync(chatId, "Usage: /creategroup <group_name>", cancellationToken: cancellationToken);
                        return;
                    }
                    var groupName = string.Join(" ", parts, 1, parts.Length - 1);
                    await HandleCreateGroupCommandAsync(chatId, userId, groupName, cancellationToken);
                    break;

                case "/joingroup":
                    if (parts.Length < 2)
                    {
                        await _botClient.SendTextMessageAsync(chatId, "Usage: /joingroup <group_id>", cancellationToken: cancellationToken);
                        return;
                    }
                    await HandleJoinGroupCommandAsync(chatId, userId, parts[1], cancellationToken);
                    break;

                case "/setgoal":
                    if (parts.Length < 2)
                    {
                        await _botClient.SendTextMessageAsync(chatId, "Usage: /setgoal <description>", cancellationToken: cancellationToken);
                        return;
                    }
                    var goalDescription = string.Join(" ", parts, 1, parts.Length - 1);
                    await HandleSetGoalCommandAsync(chatId, userId, goalDescription, cancellationToken);
                    break;

                case "/checkin":
                    if (parts.Length < 2)
                    {
                        await _botClient.SendTextMessageAsync(chatId, "Usage: /checkin <update_message>", cancellationToken: cancellationToken);
                        return;
                    }
                    var checkInMessage = string.Join(" ", parts, 1, parts.Length - 1);
                    await HandleCheckInCommandAsync(chatId, userId, checkInMessage, cancellationToken);
                    break;

                case "/mystats":
                    await HandleMyStatsCommandAsync(chatId, userId, cancellationToken);
                    break;

                case "/mygroups":
                    await HandleMyGroupsCommandAsync(chatId, userId, cancellationToken);
                    break;

                case "/leaderboard":
                    await HandleLeaderboardCommandAsync(chatId, userId, cancellationToken);
                    break;

                case "/bookride":
                case "/myrides":
                case "/track":
                case "/rate":
                case "/cancel":
                    await HandleRideCommandAsync(message, cancellationToken);
                    break;

                case "/driveraction":
                    await HandleDriverActionCommandAsync(message, cancellationToken);
                    break;

                case "/driverloc":
                    await HandleDriverLocationCommandAsync(message, cancellationToken);
                    break;

                case "/help":
                    await HandleHelpCommandAsync(chatId, cancellationToken);
                    break;

                default:
                    await _botClient.SendTextMessageAsync(chatId, "Unknown command. Type /help for available commands.", cancellationToken: cancellationToken);
                    break;
            }
        }

        private async Task HandleStartCommandAsync(long chatId, long userId, string username, string firstName, string lastName, CancellationToken cancellationToken)
        {
            // Check if user already linked
            var existingLink = await _telegramProvider.GetTelegramAvatarByTelegramIdAsync(userId);
            
            if (existingLink.Result != null)
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    $"✅ Welcome back {firstName}! Your Telegram account is already linked to OASIS.\n\n" +
                    "Use /help to see available commands.",
                    cancellationToken: cancellationToken
                );
                return;
            }

            // Create new OASIS avatar
            var avatarUsername = $"tg_{username}";
            var avatarResult = await _avatarManager.CreateAvatarAsync(
                avatarUsername,
                $"{firstName} {lastName}".Trim(),
                firstName,
                lastName,
                $"{avatarUsername}@telegram.oasis",
                Guid.NewGuid().ToString() // Generate random password
            );

            if (avatarResult.IsError || avatarResult.Result == null)
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    "❌ Error creating OASIS avatar. Please try again later.",
                    cancellationToken: cancellationToken
                );
                return;
            }

            // Link Telegram to OASIS
            var linkResult = await _telegramProvider.LinkTelegramToAvatarAsync(
                userId,
                username,
                firstName,
                lastName,
                avatarResult.Result.Id
            );

            if (linkResult.IsError)
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    "❌ Error linking Telegram to OASIS. Please try again later.",
                    cancellationToken: cancellationToken
                );
                return;
            }

            await _botClient.SendTextMessageAsync(
                chatId,
                $"🎉 Welcome to OASIS, {firstName}!\n\n" +
                $"Your Telegram account has been linked to OASIS avatar: {avatarUsername}\n\n" +
                "You can now:\n" +
                "• Create accountability groups (/creategroup)\n" +
                "• Join existing groups (/joingroup)\n" +
                "• Set goals (/setgoal)\n" +
                "• Check in with progress (/checkin)\n" +
                "• View your stats (/mystats)\n\n" +
                "Type /help for full command list.",
                cancellationToken: cancellationToken
            );
        }

        private async Task HandleCreateGroupCommandAsync(long chatId, long userId, string groupName, CancellationToken cancellationToken)
        {
            // Get user's OASIS avatar
            var userLink = await _telegramProvider.GetTelegramAvatarByTelegramIdAsync(userId);
            
            if (userLink.Result == null)
            {
                await _botClient.SendTextMessageAsync(chatId, "❌ Please use /start first to link your account.", cancellationToken: cancellationToken);
                return;
            }

            // Create group
            var groupResult = await _telegramProvider.CreateGroupAsync(
                groupName,
                $"Accountability group: {groupName}",
                userLink.Result.OasisAvatarId,
                chatId
            );

            if (groupResult.IsError)
            {
                await _botClient.SendTextMessageAsync(chatId, "❌ Error creating group. Please try again.", cancellationToken: cancellationToken);
                return;
            }

            // Add creator as first member and admin
            await _telegramProvider.AddMemberToGroupAsync(groupResult.Result.Id, userId);

            await _botClient.SendTextMessageAsync(
                chatId,
                $"✅ Group '{groupName}' created successfully!\n\n" +
                $"Group ID: {groupResult.Result.Id}\n" +
                $"Share this ID with others so they can join using:\n" +
                $"/joingroup {groupResult.Result.Id}",
                cancellationToken: cancellationToken
            );
        }

        private async Task HandleJoinGroupCommandAsync(long chatId, long userId, string groupId, CancellationToken cancellationToken)
        {
            // Get user's OASIS avatar
            var userLink = await _telegramProvider.GetTelegramAvatarByTelegramIdAsync(userId);
            
            if (userLink.Result == null)
            {
                await _botClient.SendTextMessageAsync(chatId, "❌ Please use /start first to link your account.", cancellationToken: cancellationToken);
                return;
            }

            // Get group
            var groupResult = await _telegramProvider.GetGroupAsync(groupId);
            
            if (groupResult.IsError || groupResult.Result == null)
            {
                await _botClient.SendTextMessageAsync(chatId, "❌ Group not found. Please check the Group ID.", cancellationToken: cancellationToken);
                return;
            }

            // Add member to group
            var addResult = await _telegramProvider.AddMemberToGroupAsync(groupId, userId);
            
            if (addResult.IsError)
            {
                await _botClient.SendTextMessageAsync(chatId, "❌ Error joining group. Please try again.", cancellationToken: cancellationToken);
                return;
            }

            await _botClient.SendTextMessageAsync(
                chatId,
                $"✅ You've joined the group '{groupResult.Result.Name}'!\n\n" +
                $"Rules:\n" +
                $"• {groupResult.Result.Rules.KarmaPerCheckin} karma per check-in\n" +
                $"• {groupResult.Result.Rules.TokenPerMilestone} tokens per milestone\n" +
                $"• {groupResult.Result.Rules.RequiredCheckinsPerWeek} check-ins required per week\n\n" +
                $"Set your first goal with /setgoal",
                cancellationToken: cancellationToken
            );
        }

        private async Task HandleSetGoalCommandAsync(long chatId, long userId, string description, CancellationToken cancellationToken)
        {
            // Get user's OASIS avatar
            var userLink = await _telegramProvider.GetTelegramAvatarByTelegramIdAsync(userId);
            
            if (userLink.Result == null)
            {
                await _botClient.SendTextMessageAsync(chatId, "❌ Please use /start first to link your account.", cancellationToken: cancellationToken);
                return;
            }

            // Get user's groups
            var groupsResult = await _telegramProvider.GetUserGroupsAsync(userId);
            
            if (groupsResult.Result == null || groupsResult.Result.Count == 0)
            {
                await _botClient.SendTextMessageAsync(chatId, "❌ Please join a group first with /joingroup", cancellationToken: cancellationToken);
                return;
            }

            // Create achievement in first group (in future, let user choose)
            var group = groupsResult.Result[0];
            var achievement = new Achievement
            {
                GroupId = group.Id,
                UserId = userLink.Result.OasisAvatarId,
                TelegramUserId = userId,
                Description = description,
                Type = AchievementType.Automated,
                Status = AchievementStatus.Active,
                KarmaReward = 100,
                TokenReward = 5.0m,
                Deadline = DateTime.UtcNow.AddDays(7)
            };

            var achievementResult = await _telegramProvider.CreateAchievementAsync(achievement);
            
            if (achievementResult.IsError)
            {
                await _botClient.SendTextMessageAsync(chatId, "❌ Error creating goal. Please try again.", cancellationToken: cancellationToken);
                return;
            }

            await _botClient.SendTextMessageAsync(
                chatId,
                $"🎯 Goal set successfully!\n\n" +
                $"Description: {description}\n" +
                $"Deadline: {achievement.Deadline:yyyy-MM-dd}\n" +
                $"Rewards:\n" +
                $"• {achievement.KarmaReward} karma points\n" +
                $"• {achievement.TokenReward} Solana tokens\n\n" +
                $"Use /checkin to update your progress!",
                cancellationToken: cancellationToken
            );
        }

        private async Task HandleCheckInCommandAsync(long chatId, long userId, string message, CancellationToken cancellationToken)
        {
            // Get user's OASIS avatar
            var userLink = await _telegramProvider.GetTelegramAvatarByTelegramIdAsync(userId);
            
            if (userLink.Result == null)
            {
                await _botClient.SendTextMessageAsync(chatId, "❌ Please use /start first to link your account.", cancellationToken: cancellationToken);
                return;
            }

            // Get user's active achievements
            var achievementsResult = await _telegramProvider.GetUserAchievementsAsync(userLink.Result.OasisAvatarId);
            
            if (achievementsResult.Result == null || achievementsResult.Result.Count == 0)
            {
                await _botClient.SendTextMessageAsync(chatId, "❌ You don't have any active goals. Use /setgoal to create one.", cancellationToken: cancellationToken);
                return;
            }

            var activeAchievements = achievementsResult.Result.FindAll(a => a.Status == AchievementStatus.Active);
            
            if (activeAchievements.Count == 0)
            {
                await _botClient.SendTextMessageAsync(chatId, "❌ You don't have any active goals.", cancellationToken: cancellationToken);
                return;
            }

            // Add check-in to first active achievement (in future, let user choose)
            var achievement = activeAchievements[0];
            var karmaAwarded = 10; // Get from group rules

            var checkInResult = await _telegramProvider.AddCheckInAsync(achievement.Id, message, karmaAwarded);
            
            if (checkInResult.IsError)
            {
                await _botClient.SendTextMessageAsync(chatId, "❌ Error recording check-in. Please try again.", cancellationToken: cancellationToken);
                return;
            }

            // Award karma
            await _achievementManager.AwardKarmaAsync(userLink.Result.OasisAvatarId, karmaAwarded);

            await _botClient.SendTextMessageAsync(
                chatId,
                $"✅ Check-in recorded!\n\n" +
                $"Progress: {message}\n" +
                $"Karma awarded: +{karmaAwarded}\n\n" +
                $"Keep it up! 🚀",
                cancellationToken: cancellationToken
            );
        }

        private async Task HandleMyStatsCommandAsync(long chatId, long userId, CancellationToken cancellationToken)
        {
            // Get user's OASIS avatar
            var userLink = await _telegramProvider.GetTelegramAvatarByTelegramIdAsync(userId);
            
            if (userLink.Result == null)
            {
                await _botClient.SendTextMessageAsync(chatId, "❌ Please use /start first to link your account.", cancellationToken: cancellationToken);
                return;
            }

            // Get avatar details
            var avatarResult = await _avatarManager.LoadAvatarAsync(userLink.Result.OasisAvatarId);
            
            if (avatarResult.IsError || avatarResult.Result == null)
            {
                await _botClient.SendTextMessageAsync(chatId, "❌ Error loading stats. Please try again.", cancellationToken: cancellationToken);
                return;
            }

            // Get achievements
            var achievementsResult = await _telegramProvider.GetUserAchievementsAsync(userLink.Result.OasisAvatarId);
            var achievements = achievementsResult.Result ?? new List<Achievement>();
            var completed = achievements.FindAll(a => a.Status == AchievementStatus.Completed).Count;
            var active = achievements.FindAll(a => a.Status == AchievementStatus.Active).Count;

            await _botClient.SendTextMessageAsync(
                chatId,
                $"📊 Your OASIS Stats\n\n" +
                $"Username: {avatarResult.Result.Username}\n" +
                $"Karma: {avatarResult.Result.Karma}\n\n" +
                $"Achievements:\n" +
                $"• Active: {active}\n" +
                $"• Completed: {completed}\n" +
                $"• Total: {achievements.Count}\n\n" +
                $"Keep grinding! 💪",
                cancellationToken: cancellationToken
            );
        }

        private async Task HandleMyGroupsCommandAsync(long chatId, long userId, CancellationToken cancellationToken)
        {
            var groupsResult = await _telegramProvider.GetUserGroupsAsync(userId);
            
            if (groupsResult.Result == null || groupsResult.Result.Count == 0)
            {
                await _botClient.SendTextMessageAsync(chatId, "You're not in any groups yet. Use /creategroup or /joingroup to get started!", cancellationToken: cancellationToken);
                return;
            }

            var message = "📋 Your Groups:\n\n";
            foreach (var group in groupsResult.Result)
            {
                message += $"• {group.Name}\n";
                message += $"  ID: {group.Id}\n";
                message += $"  Members: {group.MemberIds.Count}\n\n";
            }

            await _botClient.SendTextMessageAsync(chatId, message, cancellationToken: cancellationToken);
        }

        private async Task HandleLeaderboardCommandAsync(long chatId, long userId, CancellationToken cancellationToken)
        {
            await _botClient.SendTextMessageAsync(
                chatId,
                "🏆 Leaderboard\n\nComing soon! This will show top performers in your groups.",
                cancellationToken: cancellationToken
            );
        }

        private async Task HandleDriverActionCommandAsync(Message message, CancellationToken cancellationToken)
        {
            var chatId = message.Chat.Id;
            var parts = message.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 4)
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    "Usage: /driveraction <bookingId> <driverId> <action> [reason]",
                    cancellationToken: cancellationToken);
                return;
            }

            var bookingId = parts[1];
            var driverId = parts[2];
            var action = parts[3];
            var reason = parts.Length > 4 ? string.Join(' ', parts.Skip(4)) : null;

            await SendDriverActionAsync(
                chatId,
                message.From,
                bookingId,
                driverId,
                action,
                reason,
                cancellationToken);
        }

        private async Task HandleDriverLocationCommandAsync(Message message, CancellationToken cancellationToken)
        {
            var chatId = message.Chat.Id;
            var parts = message.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 3)
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    "Usage: /driverloc <bookingId> <driverId>",
                    cancellationToken: cancellationToken);
                return;
            }

            var bookingId = parts[1];
            var driverId = parts[2];

            await _rideStateManager.SetPendingDriverLocationAsync(message.From.Id, driverId, bookingId);

            await _botClient.SendTextMessageAsync(
                chatId,
                "📍 Ready to capture your live location.\n\n" +
                "Please share your location within the next 5 minutes using Telegram's attachment button.",
                cancellationToken: cancellationToken);
        }

        private async Task SendDriverActionAsync(
            long chatId,
            Telegram.Bot.Types.User actor,
            string bookingId,
            string driverId,
            string action,
            string reason,
            CancellationToken cancellationToken)
        {
            try
            {
                var payload = new DriverActionPayload
                {
                    BookingId = bookingId,
                    DriverId = driverId,
                    Action = action,
                    ChatId = chatId.ToString(),
                    Meta = new DriverActionMeta
                    {
                        Reason = reason,
                        ActorTelegramId = actor.Id,
                        ActorUsername = actor.Username
                    }
                };

                var response = await _timoRidesService.NotifyDriverActionAsync(payload);

                await _rideStateManager.RecordDriverSignalAsync(
                    actor.Id,
                    new DriverSignalAuditEntry
                    {
                        Action = action,
                        BookingId = bookingId,
                        PayloadJson = JsonSerializer.Serialize(payload),
                        Timestamp = DateTime.UtcNow,
                        TraceId = response?.TraceId ?? payload.TraceId
                    });

                await _botClient.SendTextMessageAsync(
                    chatId,
                    $"✅ Sent *{action}* for booking `{bookingId}`\nTrace ID: `{response?.TraceId ?? payload.TraceId}`",
                    parseMode: ParseMode.Markdown,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    $"❌ Driver action failed: {ex.Message}",
                    cancellationToken: cancellationToken);
            }
        }

        private async Task HandleHelpCommandAsync(long chatId, CancellationToken cancellationToken)
        {
            var helpText = @"
🤖 OASIS Accountability Bot Commands

/start - Link your Telegram account to OASIS
/creategroup <name> - Create a new accountability group
/joingroup <id> - Join an existing group
/setgoal <description> - Set a new goal
/checkin <message> - Check in with progress update
/mystats - View your karma and achievements
/mygroups - View your groups
/leaderboard - View group leaderboard
/bookride - Book a Timo ride
/myrides - View your ride history
/track <id> - Track an active ride
/rate <id> - Rate a completed ride
/cancel <id> - Cancel a booking
/driveraction <bookingId> <driverId> <action> - Send driver action
/driverloc <bookingId> <driverId> - Share driver location
/help - Show this help message

Questions? Join our support group!
            ";

            await _botClient.SendTextMessageAsync(chatId, helpText, cancellationToken: cancellationToken);
        }

        private Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var errorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(errorMessage);
            return Task.CompletedTask;
        }
    }
}





