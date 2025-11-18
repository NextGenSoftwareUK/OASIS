using System;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegcrm.Services;

namespace Telegcrm.Integration
{
    /// <summary>
    /// Integration helper to connect Telegram bot with CRM service
    /// This should be called from your TelegramBotService when messages are received
    /// </summary>
    public class TelegramCrmIntegration
    {
        private readonly TelegramCrmService _crmService;
        private readonly TelegramBotClient _botClient;
        private readonly Guid _oasisAvatarId; // Your friend's OASIS avatar ID

        public TelegramCrmIntegration(
            TelegramCrmService crmService,
            TelegramBotClient botClient,
            Guid oasisAvatarId)
        {
            _crmService = crmService;
            _botClient = botClient;
            _oasisAvatarId = oasisAvatarId;
        }

        /// <summary>
        /// Call this method whenever a message is received in Telegram
        /// This will automatically store it in the CRM system
        /// </summary>
        public async Task HandleIncomingMessageAsync(Message message)
        {
            try
            {
                // Determine if message is from us
                // Note: For incoming messages, isFromMe will be false
                // For outgoing messages sent via bot, we'd need to track those separately
                // For now, assume all messages handled here are incoming
                bool isFromMe = false;

                // Store message in CRM
                await _crmService.StoreMessageAsync(message, _oasisAvatarId, isFromMe);
            }
            catch (Exception ex)
            {
                // Log error but don't break the bot
                Console.WriteLine($"Error storing message in CRM: {ex.Message}");
            }
        }

        /// <summary>
        /// Call this method when a message is sent
        /// </summary>
        public async Task HandleOutgoingMessageAsync(Message message)
        {
            try
            {
                // Store outgoing message
                await _crmService.StoreMessageAsync(message, _oasisAvatarId, isFromMe: true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error storing outgoing message in CRM: {ex.Message}");
            }
        }
    }
}

