using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Providers.TelegramOASIS;
using NextGenSoftware.OASIS.API.Providers.TelegramOASIS.Models;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    /// <summary>
    /// Manager for tracking achievements and distributing rewards (karma + tokens)
    /// </summary>
    public class AchievementManager
    {
        private readonly TelegramOASIS _telegramProvider;
        private readonly AvatarManager _avatarManager;
        private readonly ISolanaProvider _solanaProvider;
        private readonly string _treasuryWalletAddress;

        public AchievementManager(
            TelegramOASIS telegramProvider,
            AvatarManager avatarManager,
            IOASISStorageProvider solanaProvider = null,
            string treasuryWalletAddress = null)
        {
            _telegramProvider = telegramProvider;
            _avatarManager = avatarManager;
            _solanaProvider = solanaProvider as ISolanaProvider;
            _treasuryWalletAddress = treasuryWalletAddress;
        }

        /// <summary>
        /// Award karma points to an avatar
        /// </summary>
        public async Task<OASISResult<int>> AwardKarmaAsync(Guid avatarId, int karmaPoints)
        {
            var result = new OASISResult<int>();

            try
            {
                // Load avatar
                var avatarResult = await _avatarManager.LoadAvatarAsync(avatarId);
                
                if (avatarResult.IsError || avatarResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found");
                    return result;
                }

                var avatar = avatarResult.Result;
                
                // Update karma
                avatar.Karma += karmaPoints;
                
                // Save avatar
                var saveResult = await _avatarManager.SaveAvatarAsync(avatar);
                
                if (saveResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, "Error saving avatar karma");
                    return result;
                }

                result.Result = avatar.Karma;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error awarding karma: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Award Solana tokens to an avatar's wallet
        /// </summary>
        public async Task<OASISResult<string>> AwardTokensAsync(Guid avatarId, decimal tokenAmount)
        {
            var result = new OASISResult<string>();

            try
            {
                if (_solanaProvider == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Solana provider not configured");
                    return result;
                }

                if (string.IsNullOrEmpty(_treasuryWalletAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Treasury wallet not configured");
                    return result;
                }

                // Load avatar to get wallet address
                var avatarResult = await _avatarManager.LoadAvatarAsync(avatarId);
                
                if (avatarResult.IsError || avatarResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found");
                    return result;
                }

                // Get user's Solana wallet (stored in avatar)
                // Note: In production, you'd get the actual wallet from avatar.Wallets or similar
                var userWalletAddress = GetAvatarSolanaWallet(avatarResult.Result);
                
                if (string.IsNullOrEmpty(userWalletAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "User Solana wallet not found");
                    return result;
                }

                // Transfer tokens using Solana provider
                // Note: This is a simplified implementation - in production you'd use the actual SolanaOASIS provider methods
                var transferResult = await TransferSolanaTokensAsync(_treasuryWalletAddress, userWalletAddress, tokenAmount);
                
                if (transferResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Token transfer failed: {transferResult.Message}");
                    return result;
                }

                result.Result = transferResult.Result; // Transaction signature
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error awarding tokens: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Complete an achievement and award rewards
        /// </summary>
        public async Task<OASISResult<AchievementReward>> CompleteAchievementAsync(string achievementId, long? verifiedBy = null)
        {
            var result = new OASISResult<AchievementReward>();

            try
            {
                // Load achievement
                var achievementsResult = await _telegramProvider.GetUserAchievementsAsync(Guid.Empty); // This needs to be improved
                
                // Update achievement status
                var updateResult = await _telegramProvider.UpdateAchievementStatusAsync(
                    achievementId,
                    AchievementStatus.Completed,
                    verifiedBy
                );
                
                if (updateResult.IsError || updateResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Error updating achievement");
                    return result;
                }

                var achievement = updateResult.Result;
                
                // Award karma
                var karmaResult = await AwardKarmaAsync(achievement.UserId, achievement.KarmaReward);
                
                // Award tokens
                OASISResult<string> tokenResult = null;
                if (achievement.TokenReward > 0)
                {
                    tokenResult = await AwardTokensAsync(achievement.UserId, achievement.TokenReward);
                }

                // Send notification via Telegram
                var telegramAvatar = await _telegramProvider.GetTelegramAvatarByOASISIdAsync(achievement.UserId);
                if (telegramAvatar.Result != null)
                {
                    var message = $"🎉 Achievement Completed!\n\n" +
                                $"Goal: {achievement.Description}\n\n" +
                                $"Rewards:\n" +
                                $"• {achievement.KarmaReward} karma points ✅\n";
                    
                    if (achievement.TokenReward > 0)
                    {
                        message += $"• {achievement.TokenReward} Solana tokens ✅\n";
                    }

                    await _telegramProvider.SendMessageAsync(telegramAvatar.Result.TelegramId, message);
                }

                result.Result = new AchievementReward
                {
                    AchievementId = achievementId,
                    KarmaAwarded = achievement.KarmaReward,
                    TokensAwarded = achievement.TokenReward,
                    TransactionSignature = tokenResult?.Result
                };
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error completing achievement: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Process a check-in and award karma
        /// </summary>
        public async Task<OASISResult<int>> ProcessCheckInAsync(string achievementId, string message, int karmaAmount, Guid userId)
        {
            var result = new OASISResult<int>();

            try
            {
                // Add check-in to achievement
                var checkInResult = await _telegramProvider.AddCheckInAsync(achievementId, message, karmaAmount);
                
                if (checkInResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, "Error adding check-in");
                    return result;
                }

                // Award karma
                var karmaResult = await AwardKarmaAsync(userId, karmaAmount);
                
                if (karmaResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, "Error awarding karma");
                    return result;
                }

                result.Result = karmaResult.Result;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error processing check-in: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Helper to get avatar's Solana wallet address
        /// Note: This is a placeholder - implement based on your avatar wallet structure
        /// </summary>
        private string GetAvatarSolanaWallet(IAvatar avatar)
        {
            // In production, this would retrieve the actual Solana wallet from avatar.Wallets
            // For now, return a placeholder
            return avatar.Id.ToString(); // This should be replaced with actual wallet retrieval
        }

        /// <summary>
        /// Helper to transfer Solana tokens
        /// Note: This is a placeholder - implement using actual SolanaOASIS provider
        /// </summary>
        private async Task<OASISResult<string>> TransferSolanaTokensAsync(string fromWallet, string toWallet, decimal amount)
        {
            var result = new OASISResult<string>();

            try
            {
                if (_solanaProvider == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Solana provider not available");
                    return result;
                }

                // In production, use actual SolanaOASIS methods like:
                // var transferResult = await _solanaProvider.TransferTokensAsync(fromWallet, toWallet, amount);
                
                // For now, return a mock transaction signature
                result.Result = $"mock_tx_{Guid.NewGuid().ToString().Substring(0, 8)}";
                
                await Task.CompletedTask; // Placeholder for async operation
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error transferring tokens: {ex.Message}");
            }

            return result;
        }
    }

    /// <summary>
    /// Interface for Solana provider (to avoid circular dependencies)
    /// </summary>
    public interface ISolanaProvider
    {
        // Define Solana-specific methods here
    }

    /// <summary>
    /// Result of achievement reward distribution
    /// </summary>
    public class AchievementReward
    {
        public string AchievementId { get; set; }
        public int KarmaAwarded { get; set; }
        public decimal TokensAwarded { get; set; }
        public string TransactionSignature { get; set; }
    }
}





