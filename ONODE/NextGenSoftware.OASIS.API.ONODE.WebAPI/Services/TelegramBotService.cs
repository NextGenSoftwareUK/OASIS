using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
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

        public TelegramBotService(
            string botToken,
            TelegramOASIS telegramProvider,
            AvatarManager avatarManager,
            ILogger<TelegramBotService> logger,
            NFTService nftService,
            PinataService pinataService)
        {
            _botClient = new TelegramBotClient(botToken);
            _telegramProvider = telegramProvider;
            _avatarManager = avatarManager;
            _logger = logger;
            _nftService = nftService;
            _pinataService = pinataService;
            _cancellationTokenSource = new CancellationTokenSource();
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
        private async Task HandleMessageAsync(Message message, CancellationToken cancellationToken)
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
                // Non-command message - could be used for natural language processing
                await _botClient.SendTextMessageAsync(
                    chatId,
                    "💡 Tip: Use /help to see available commands",
                    cancellationToken: cancellationToken
                );
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
                    $"👋 Welcome back, @{existingAvatar.Result.TelegramUsername}!\n\n" +
                    $"Your account is already linked to OASIS.\n" +
                    $"Use /help to see what you can do!",
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
                $"🎉 Welcome to Experiences.fun, @{user.Username}!\n\n" +
                $"Your OASIS avatar has been created and linked to your Telegram account.\n\n" +
                $"**Getting Started:**\n" +
                $"• Use /creategroup to start an accountability group\n" +
                $"• Use /joingroup to join an existing group\n" +
                $"• Use /checkin to log your progress and earn karma\n" +
                $"• Use /mystats to see your achievements\n\n" +
                $"Type /help anytime to see all commands.",
                parseMode: ParseMode.Markdown,
                cancellationToken: cancellationToken
            );
        }

        /// <summary>
        /// /help - Show available commands
        /// </summary>
        private async Task HandleHelpCommand(long chatId, CancellationToken cancellationToken)
        {
            var helpText = @"
🤖 **Experiences.fun Accountability Bot**

**Account Commands:**
/start - Link your Telegram to OASIS
/mystats - View your karma, tokens & achievements

**Group Commands:**
/creategroup <name> - Create accountability group
/joingroup <groupId> - Join a group
/mygroups - List your groups
/leaderboard [groupId] - View group rankings

**Achievement Commands:**
/checkin <message> - Log progress (earn karma)
/setgoal <description> - Create personal goal
/milestone <@user> - Mark user milestone (admins)

**Info:**
• Earn karma by checking in regularly
• Complete milestones to earn EXP tokens
• Stay accountable with your crew!

Need help? Contact @experiences_support
";

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
            // Handle button clicks from inline keyboards
            await _botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "Processing...",
                cancellationToken: cancellationToken
            );

            // Could add interactive features here
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
        private async Task HandlePhotoMessageAsync(Message message, CancellationToken cancellationToken)
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
    }
}

