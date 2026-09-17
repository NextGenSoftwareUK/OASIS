using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using NextGenSoftware.OASIS.API.Providers.TelegramOASIS.Models;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services
{
    public partial class TelegramBotService
    {
        #region Command Handlers

        private async Task HandleStartCommand(long chatId, User user, CancellationToken cancellationToken)
        {
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

            var linkResult = await _telegramProvider.LinkTelegramToAvatarAsync(
                user.Id,
                user.Username ?? $"user_{user.Id}",
                user.FirstName ?? "User",
                user.LastName ?? "",
                Guid.NewGuid()
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

            var avatarResult = await _telegramProvider.GetTelegramAvatarByTelegramIdAsync(user.Id);
            if (avatarResult.IsError || avatarResult.Result == null)
            {
                await _botClient.SendTextMessageAsync(chatId, "❌ Please use /start first to link your account", cancellationToken: cancellationToken);
                return;
            }

            var groupName = string.Join(" ", args);
            var description = $"Accountability group created by @{user.Username}";

            var groupResult = await _telegramProvider.CreateGroupAsync(
                groupName,
                description,
                avatarResult.Result.OasisAvatarId,
                chatId
            );

            if (groupResult.IsError)
            {
                await _botClient.SendTextMessageAsync(chatId, $"❌ Failed to create group: {groupResult.Message}", cancellationToken: cancellationToken);
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

        private async Task HandleJoinGroupCommand(long chatId, User user, string[] args, CancellationToken cancellationToken)
        {
            if (args.Length == 0)
            {
                await _botClient.SendTextMessageAsync(chatId, "❌ Usage: /joingroup <groupId>\n\nExample: /joingroup abc123", cancellationToken: cancellationToken);
                return;
            }

            var groupId = args[0];
            var groupResult = await _telegramProvider.GetGroupAsync(groupId);
            if (groupResult.IsError || groupResult.Result == null)
            {
                await _botClient.SendTextMessageAsync(chatId, "❌ Group not found. Check the group ID and try again.", cancellationToken: cancellationToken);
                return;
            }

            var joinResult = await _telegramProvider.AddMemberToGroupAsync(groupId, user.Id);
            if (joinResult.IsError)
            {
                await _botClient.SendTextMessageAsync(chatId, $"❌ Failed to join group: {joinResult.Message}", cancellationToken: cancellationToken);
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

        private async Task HandleCheckinCommand(long chatId, User user, string[] args, CancellationToken cancellationToken)
        {
            if (args.Length == 0)
            {
                await _botClient.SendTextMessageAsync(chatId, "❌ Usage: /checkin <your update>\n\nExample: /checkin Completed 30-min workout! 💪", cancellationToken: cancellationToken);
                return;
            }

            var message = string.Join(" ", args);
            int karmaAwarded = 10;

            var avatarResult = await _telegramProvider.GetTelegramAvatarByTelegramIdAsync(user.Id);
            if (avatarResult.IsError || avatarResult.Result == null)
            {
                await _botClient.SendTextMessageAsync(chatId, "❌ Please use /start first to link your account", cancellationToken: cancellationToken);
                return;
            }

            var groupsResult = await _telegramProvider.GetUserGroupsAsync(user.Id);
            if (groupsResult.IsError || !groupsResult.Result.Any())
            {
                await _botClient.SendTextMessageAsync(chatId, "❌ You need to join a group first. Use /joingroup or /creategroup", cancellationToken: cancellationToken);
                return;
            }

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
                TokenReward = 0,
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
                await _botClient.SendTextMessageAsync(chatId, $"❌ Failed to record check-in: {createResult.Message}", cancellationToken: cancellationToken);
                return;
            }

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

        private async Task HandleMilestoneCommand(long chatId, User user, string[] args, CancellationToken cancellationToken)
        {
            await _botClient.SendTextMessageAsync(
                chatId,
                "🏆 Milestone feature coming soon!\n\n" +
                "Admins will be able to mark major achievements and trigger token rewards.",
                cancellationToken: cancellationToken
            );
        }

        private async Task HandleMyStatsCommand(long chatId, User user, CancellationToken cancellationToken)
        {
            var avatarResult = await _telegramProvider.GetTelegramAvatarByTelegramIdAsync(user.Id);
            if (avatarResult.IsError || avatarResult.Result == null)
            {
                await _botClient.SendTextMessageAsync(chatId, "❌ Please use /start first to link your account", cancellationToken: cancellationToken);
                return;
            }

            var achievementsResult = await _telegramProvider.GetUserAchievementsAsync(avatarResult.Result.OasisAvatarId);
            var achievements = achievementsResult.Result ?? new List<Achievement>();

            var totalKarma = achievements.Sum(a => a.KarmaReward);
            var totalTokens = achievements.Sum(a => a.TokenReward);
            var completedCount = achievements.Count(a => a.Status == AchievementStatus.Completed);
            var activeCount = achievements.Count(a => a.Status == AchievementStatus.Active);

            var statsText =
$@"📊 **Your Stats**

👤 @{user.Username}
⭐ Karma: {totalKarma} points
🪙 Tokens: {totalTokens} EXP
✅ Completed: {completedCount}
🎯 Active Goals: {activeCount}

Keep crushing it! 🚀";

            await _botClient.SendTextMessageAsync(chatId, statsText, parseMode: ParseMode.Markdown, cancellationToken: cancellationToken);
        }

        private async Task HandleLeaderboardCommand(long chatId, User user, string[] args, CancellationToken cancellationToken)
        {
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

        private async Task HandleSetGoalCommand(long chatId, User user, string[] args, CancellationToken cancellationToken)
        {
            if (args.Length == 0)
            {
                await _botClient.SendTextMessageAsync(chatId, "❌ Usage: /setgoal <description>\n\nExample: /setgoal Complete 5 workouts this week", cancellationToken: cancellationToken);
                return;
            }

            var goalDescription = string.Join(" ", args);

            var avatarResult = await _telegramProvider.GetTelegramAvatarByTelegramIdAsync(user.Id);
            if (avatarResult.IsError || avatarResult.Result == null)
            {
                await _botClient.SendTextMessageAsync(chatId, "❌ Please use /start first to link your account", cancellationToken: cancellationToken);
                return;
            }

            var achievement = new Achievement
            {
                UserId = avatarResult.Result.OasisAvatarId,
                TelegramUserId = user.Id,
                Description = goalDescription,
                Type = AchievementType.Manual,
                Status = AchievementStatus.Active,
                KarmaReward = 50,
                TokenReward = 5.0m,
                Deadline = DateTime.UtcNow.AddDays(7)
            };

            var createResult = await _telegramProvider.CreateAchievementAsync(achievement);
            if (createResult.IsError)
            {
                await _botClient.SendTextMessageAsync(chatId, $"❌ Failed to set goal: {createResult.Message}", cancellationToken: cancellationToken);
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
    }
}
