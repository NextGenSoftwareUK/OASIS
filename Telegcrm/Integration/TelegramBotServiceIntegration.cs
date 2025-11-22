using System;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegcrm.Services;

namespace Telegcrm.Integration
{
    /// <summary>
    /// Helper class to integrate CRM with existing TelegramBotService
    /// Copy the HandleUpdateAsync modification from this file
    /// </summary>
    public static class TelegramBotServiceIntegration
    {
        /// <summary>
        /// Call this method in your TelegramBotService.HandleUpdateAsync method
        /// Add this right after you receive a message
        /// </summary>
        public static async Task StoreMessageInCrmAsync(
            TelegramCrmService crmService,
            Message message,
            Guid oasisAvatarId,
            Telegram.Bot.ITelegramBotClient botClient)
        {
            try
            {
                // Determine if message is from the bot owner
                // Note: We'll determine isFromMe based on whether message is sent by the bot
                // For now, assume messages are incoming (not from bot)
                bool isFromMe = false; // Can be enhanced later to check bot ID

                // Store message in CRM
                await crmService.StoreMessageAsync(message, oasisAvatarId, isFromMe);
            }
            catch (Exception ex)
            {
                // Log but don't break the bot
                Console.WriteLine($"[CRM] Error storing message: {ex.Message}");
            }
        }

        /// <summary>
        /// Example of how to modify HandleUpdateAsync in TelegramBotService.cs
        /// 
        /// BEFORE:
        /// if (update.Message is not { } message)
        ///     return;
        /// 
        /// AFTER:
        /// if (update.Message is not { } message)
        ///     return;
        /// 
        /// // Store in CRM
        /// await TelegramBotServiceIntegration.StoreMessageInCrmAsync(
        ///     _crmService,
        ///     message,
        ///     _oasisAvatarId,
        ///     _botClient
        /// );
        /// </summary>
    }
}

