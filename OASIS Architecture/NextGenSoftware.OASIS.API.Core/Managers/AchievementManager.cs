using System;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    /// <summary>
    /// Manages achievement tracking and reward distribution for accountability platforms
    /// Integrates with Karma system and token distribution (Solana, etc.)
    /// </summary>
    public class AchievementManager : OASISManager
    {
        private readonly AvatarManager _avatarManager;

        // Singleton instance
        private static AchievementManager _instance;
        private static readonly object _lockObject = new object();

        public static AchievementManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lockObject)
                    {
                        if (_instance == null)
                        {
                            _instance = new AchievementManager(null, null, null);
                        }
                    }
                }
                return _instance;
            }
        }

        public AchievementManager(IOASISStorageProvider storageProvider, OASISDNA dna = null, AvatarManager avatarManager = null) : base(storageProvider, dna)
        {
            _avatarManager = avatarManager ?? AvatarManager.Instance;
        }

        #region Karma Rewards

        /// <summary>
        /// Award karma points to avatar for achievement completion
        /// </summary>
        /// <param name="avatarId">OASIS Avatar ID</param>
        /// <param name="karmaAmount">Amount of karma to award</param>
        /// <param name="achievementTitle">Title of achievement</param>
        /// <param name="achievementDescription">Description of achievement</param>
        /// <param name="sourceLink">Optional link to achievement details</param>
        /// <param name="providerType">Storage provider to use</param>
        /// <returns>Karma record with updated total</returns>
        public async Task<OASISResult<KarmaAkashicRecord>> AwardKarmaAsync(
            Guid avatarId,
            int karmaAmount,
            string achievementTitle,
            string achievementDescription,
            string sourceLink = null,
            ProviderType providerType = ProviderType.Default)
        {
            try
            {
                if (karmaAmount <= 0)
                {
                    return new OASISResult<KarmaAkashicRecord>
                    {
                        IsError = true,
                        Message = "Karma amount must be positive"
                    };
                }

                // Map karma amount to KarmaTypePositive enum
                // For simplicity, using BeHelpful for all positive karma
                // Could be extended to map specific achievement types to karma types
                var karmaType = GetKarmaTypeForAmount(karmaAmount);

                var karmaRecord = await _avatarManager.AddKarmaToAvatarAsync(
                    avatarId,
                    karmaType,
                    KarmaSourceType.dApp, // Telegram bot is a dApp
                    achievementTitle,
                    achievementDescription,
                    sourceLink,
                    providerType
                );

                if (karmaRecord != null)
                {
                    // Log karma award for tracking
                    System.Console.WriteLine($"[AchievementManager] Awarded {karmaAmount} karma to avatar {avatarId} for: {achievementTitle}");

                    return new OASISResult<KarmaAkashicRecord>
                    {
                        Result = karmaRecord,
                        Message = $"Successfully awarded {karmaAmount} karma"
                    };
                }

                return new OASISResult<KarmaAkashicRecord>
                {
                    IsError = true,
                    Message = "Failed to award karma"
                };
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[AchievementManager] Error awarding karma to avatar {avatarId}: {ex.Message}");
                return new OASISResult<KarmaAkashicRecord>
                {
                    IsError = true,
                    Message = ex.Message
                };
            }
        }

        /// <summary>
        /// Synchronous version of AwardKarmaAsync
        /// </summary>
        public OASISResult<KarmaAkashicRecord> AwardKarma(
            Guid avatarId,
            int karmaAmount,
            string achievementTitle,
            string achievementDescription,
            string sourceLink = null,
            ProviderType providerType = ProviderType.Default)
        {
            try
            {
                if (karmaAmount <= 0)
                {
                    return new OASISResult<KarmaAkashicRecord>
                    {
                        IsError = true,
                        Message = "Karma amount must be positive"
                    };
                }

                var karmaType = GetKarmaTypeForAmount(karmaAmount);

                var karmaRecord = _avatarManager.AddKarmaToAvatar(
                    avatarId,
                    karmaType,
                    KarmaSourceType.dApp,
                    achievementTitle,
                    achievementDescription,
                    sourceLink,
                    providerType
                );

                if (karmaRecord != null && !karmaRecord.IsError)
                {
                    System.Console.WriteLine($"[AchievementManager] Awarded {karmaAmount} karma to avatar {avatarId} for: {achievementTitle}");

                    return new OASISResult<KarmaAkashicRecord>
                    {
                        Result = karmaRecord.Result,
                        Message = $"Successfully awarded {karmaAmount} karma"
                    };
                }

                return new OASISResult<KarmaAkashicRecord>
                {
                    IsError = true,
                    Message = karmaRecord?.Message ?? "Failed to award karma"
                };
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[AchievementManager] Error awarding karma to avatar {avatarId}: {ex.Message}");
                return new OASISResult<KarmaAkashicRecord>
                {
                    IsError = true,
                    Message = ex.Message
                };
            }
        }

        /// <summary>
        /// Map karma amount to appropriate KarmaTypePositive enum
        /// </summary>
        private KarmaTypePositive GetKarmaTypeForAmount(int amount)
        {
            // Map karma amounts to predefined karma types
            // This is a simplified mapping - could be more sophisticated
            if (amount >= 100)
                return KarmaTypePositive.OurWorldBeASuperHero; // Large achievements
            else if (amount >= 50)
                return KarmaTypePositive.OurWorldBeAHero; // Medium achievements
            else if (amount >= 20)
                return KarmaTypePositive.OurWorldHelpOtherPlayer; // Small achievements
            else
                return KarmaTypePositive.OurWorldPickupLitter; // Check-ins and small tasks
        }

        #endregion

        #region Token Rewards

        /// <summary>
        /// Award tokens (Solana, etc.) to avatar for milestone completion
        /// </summary>
        /// <param name="avatarId">OASIS Avatar ID</param>
        /// <param name="tokenAmount">Amount of tokens to award</param>
        /// <param name="tokenSymbol">Token symbol (e.g., "EXP")</param>
        /// <param name="achievementTitle">Title of achievement</param>
        /// <param name="providerType">Blockchain provider (e.g., SolanaOASIS)</param>
        /// <returns>Transaction result</returns>
        public async Task<OASISResult<string>> AwardTokensAsync(
            Guid avatarId,
            decimal tokenAmount,
            string tokenSymbol,
            string achievementTitle,
            ProviderType providerType = ProviderType.SolanaOASIS)
        {
            try
            {
                if (tokenAmount <= 0)
                {
                    return new OASISResult<string>
                    {
                        IsError = true,
                        Message = "Token amount must be positive"
                    };
                }

                // Load avatar to get wallet address
                var avatarResult = await _avatarManager.LoadAvatarDetailAsync(avatarId);
                if (avatarResult.IsError || avatarResult.Result == null)
                {
                    return new OASISResult<string>
                    {
                        IsError = true,
                        Message = $"Avatar not found: {avatarResult.Message}"
                    };
                }

                // Get the avatar's Solana wallet address
                // For now, return placeholder. In production, retrieve from avatar's wallet collection
                string recipientWallet = "PLACEHOLDER_SOLANA_WALLET";
                
                // TODO: Implement proper wallet retrieval from IAvatarDetail
                // Cast to AvatarDetail to access Wallets property if needed:
                // var avatarDetail = avatarResult.Result as AvatarDetail;
                // if (avatarDetail?.Wallets != null && avatarDetail.Wallets.Count > 0) { ... }
                
                if (string.IsNullOrEmpty(recipientWallet))
                {
                    return new OASISResult<string>
                    {
                        IsError = true,
                        Message = "No wallet found for avatar"
                    };
                }

                // TODO: Implement actual token transfer via WalletManager or provider
                // This would integrate with SolanaOASIS provider's token transfer functionality
                
                System.Console.WriteLine(
                    $"[AchievementManager] Token award initiated: {tokenAmount} {tokenSymbol} to avatar {avatarId} " +
                    $"at wallet {recipientWallet} for: {achievementTitle}"
                );

                // For now, return success with transaction placeholder
                // In production, this would call:
                // await SolanaProvider.TransferTokensAsync(treasuryWallet, recipientWallet, tokenAmount, tokenMintAddress);
                
                return new OASISResult<string>
                {
                    Result = $"TOKEN_TX_{Guid.NewGuid()}", // Placeholder transaction ID
                    Message = $"Successfully awarded {tokenAmount} {tokenSymbol} tokens"
                };
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[AchievementManager] Error awarding tokens to avatar {avatarId}: {ex.Message}");
                return new OASISResult<string>
                {
                    IsError = true,
                    Message = ex.Message
                };
            }
        }

        #endregion

        #region Complete Achievement With Rewards

        /// <summary>
        /// Complete achievement and distribute all rewards (karma + tokens)
        /// </summary>
        /// <param name="avatarId">OASIS Avatar ID</param>
        /// <param name="karmaReward">Karma points to award</param>
        /// <param name="tokenReward">Token amount to award</param>
        /// <param name="tokenSymbol">Token symbol</param>
        /// <param name="achievementTitle">Achievement title</param>
        /// <param name="achievementDescription">Achievement description</param>
        /// <param name="sourceLink">Optional link</param>
        /// <returns>Combined result of both rewards</returns>
        public async Task<OASISResult<AchievementRewardResult>> CompleteAchievementAsync(
            Guid avatarId,
            int karmaReward,
            decimal tokenReward,
            string tokenSymbol,
            string achievementTitle,
            string achievementDescription,
            string sourceLink = null)
        {
            var result = new AchievementRewardResult
            {
                AvatarId = avatarId,
                AchievementTitle = achievementTitle
            };

            try
            {
                // Award karma
                if (karmaReward > 0)
                {
                    var karmaResult = await AwardKarmaAsync(
                        avatarId,
                        karmaReward,
                        achievementTitle,
                        achievementDescription,
                        sourceLink
                    );

                    if (!karmaResult.IsError)
                    {
                        result.KarmaAwarded = karmaReward;
                        result.KarmaSuccess = true;
                    }
                    else
                    {
                        result.KarmaError = karmaResult.Message;
                    }
                }

                // Award tokens
                if (tokenReward > 0)
                {
                    var tokenResult = await AwardTokensAsync(
                        avatarId,
                        tokenReward,
                        tokenSymbol,
                        achievementTitle
                    );

                    if (!tokenResult.IsError)
                    {
                        result.TokensAwarded = tokenReward;
                        result.TokenSymbol = tokenSymbol;
                        result.TokenSuccess = true;
                        result.TransactionId = tokenResult.Result;
                    }
                    else
                    {
                        result.TokenError = tokenResult.Message;
                    }
                }

                result.Success = result.KarmaSuccess || result.TokenSuccess;
                result.Message = BuildResultMessage(result);

                return new OASISResult<AchievementRewardResult>
                {
                    Result = result,
                    Message = result.Message
                };
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[AchievementManager] Error completing achievement for avatar {avatarId}: {ex.Message}");
                return new OASISResult<AchievementRewardResult>
                {
                    IsError = true,
                    Message = ex.Message
                };
            }
        }

        private string BuildResultMessage(AchievementRewardResult result)
        {
            var messages = new System.Collections.Generic.List<string>();

            if (result.KarmaSuccess)
                messages.Add($"+{result.KarmaAwarded} karma");
            else if (!string.IsNullOrEmpty(result.KarmaError))
                messages.Add($"Karma failed: {result.KarmaError}");

            if (result.TokenSuccess)
                messages.Add($"+{result.TokensAwarded} {result.TokenSymbol}");
            else if (!string.IsNullOrEmpty(result.TokenError))
                messages.Add($"Tokens failed: {result.TokenError}");

            return messages.Count > 0 ? string.Join(", ", messages) : "No rewards";
        }

        #endregion
    }

    /// <summary>
    /// Result of achievement completion with reward distribution
    /// </summary>
    public class AchievementRewardResult
    {
        public Guid AvatarId { get; set; }
        public string AchievementTitle { get; set; }
        
        // Karma
        public bool KarmaSuccess { get; set; }
        public int KarmaAwarded { get; set; }
        public string KarmaError { get; set; }
        
        // Tokens
        public bool TokenSuccess { get; set; }
        public decimal TokensAwarded { get; set; }
        public string TokenSymbol { get; set; }
        public string TransactionId { get; set; }
        public string TokenError { get; set; }
        
        // Overall
        public bool Success { get; set; }
        public string Message { get; set; }
    }
}

