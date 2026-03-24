using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.ChatBridge;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services
{
    /// <summary>
    /// Long-running background service that connects to Discord via gateway (WebSocket).
    /// Listens for messages in the configured bridge channel and routes them to Telegram.
    /// Also handles /link and /unlink slash-style text commands in the bridge channel or DMs.
    /// 
    /// Setup in Discord Developer Portal:
    ///   1. Create an Application → Bot
    ///   2. Enable "Message Content Intent" under Bot → Privileged Gateway Intents
    ///   3. Copy the bot token → ChatBridge:DiscordBotToken in appsettings
    ///   4. Invite bot with scopes: bot, applications.commands and permissions: Read Messages, Send Messages
    /// </summary>
    public class DiscordBridgeService : IHostedService
    {
        private readonly ChatBridgeOptions _options;
        private readonly ChatBridgeRouter _router;
        private readonly ILogger<DiscordBridgeService> _logger;
        private DiscordSocketClient _client;

        public DiscordBridgeService(
            IOptions<ChatBridgeOptions> options,
            ChatBridgeRouter router,
            ILogger<DiscordBridgeService> logger)
        {
            _options = options.Value;
            _router = router;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (!_options.Enabled)
            {
                _logger.LogInformation("[DiscordBridge] Bridge disabled via ChatBridge:Enabled=false; skipping Discord bot startup.");
                return;
            }

            if (string.IsNullOrEmpty(_options.DiscordBotToken))
            {
                _logger.LogWarning("[DiscordBridge] ChatBridge:DiscordBotToken not configured; Discord bridge will not start.");
                return;
            }

            var config = new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.Guilds
                    | GatewayIntents.GuildMessages
                    | GatewayIntents.MessageContent
                    | GatewayIntents.DirectMessages,
                LogLevel = LogSeverity.Warning
            };

            _client = new DiscordSocketClient(config);
            _client.Log += OnLogAsync;
            _client.MessageReceived += OnMessageReceivedAsync;
            _client.Ready += OnReadyAsync;

            await _client.LoginAsync(TokenType.Bot, _options.DiscordBotToken).ConfigureAwait(false);
            await _client.StartAsync().ConfigureAwait(false);

            _logger.LogInformation("[DiscordBridge] Discord gateway bot started.");
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_client != null)
            {
                await _client.StopAsync().ConfigureAwait(false);
                _client.Dispose();
                _logger.LogInformation("[DiscordBridge] Discord gateway bot stopped.");
            }
        }

        private Task OnReadyAsync()
        {
            _logger.LogInformation("[DiscordBridge] Connected to Discord as {User}. Watching channel {ChannelId}.",
                _client.CurrentUser?.Username, _options.DiscordBridgeChannelId);
            return Task.CompletedTask;
        }

        private async Task OnMessageReceivedAsync(SocketMessage socketMessage)
        {
            // Ignore bots and system messages (prevent bridge loops)
            if (socketMessage.Author.IsBot || socketMessage is not SocketUserMessage message)
                return;

            var text = message.Content?.Trim() ?? "";

            // Handle /link command from any DM or from the bridged channel
            if (text.StartsWith("/link ", StringComparison.OrdinalIgnoreCase))
            {
                var oasisUsername = text.Substring("/link ".Length).Trim();
                var reply = await _router.HandleDiscordLinkCommandAsync(message.Author.Id, message.Author.Username, oasisUsername).ConfigureAwait(false);
                await message.Channel.SendMessageAsync(reply).ConfigureAwait(false);
                return;
            }

            if (text.Equals("/unlink", StringComparison.OrdinalIgnoreCase))
            {
                var reply = await _router.HandleDiscordUnlinkCommandAsync(message.Author.Id).ConfigureAwait(false);
                await message.Channel.SendMessageAsync(reply).ConfigureAwait(false);
                return;
            }

            if (text.Equals("/bridge", StringComparison.OrdinalIgnoreCase))
            {
                await message.Channel.SendMessageAsync("🌉 Bridge is active. Use `/link <oasis-username>` to link your OASIS avatar.").ConfigureAwait(false);
                return;
            }

            // Only relay messages from the configured bridge channel
            if (_options.DiscordBridgeChannelId == 0 || message.Channel.Id != _options.DiscordBridgeChannelId)
                return;

            // Skip empty messages and attachments-only for now (v1: text only)
            if (string.IsNullOrWhiteSpace(text))
                return;

            await _router.RouteFromDiscordAsync(message.Author.Id, message.Author.Username, text).ConfigureAwait(false);
        }

        private Task OnLogAsync(LogMessage log)
        {
            switch (log.Severity)
            {
                case LogSeverity.Critical:
                case LogSeverity.Error:
                    _logger.LogError(log.Exception, "[DiscordGateway] {Message}", log.Message);
                    break;
                case LogSeverity.Warning:
                    _logger.LogWarning("[DiscordGateway] {Message}", log.Message);
                    break;
                default:
                    _logger.LogDebug("[DiscordGateway] {Message}", log.Message);
                    break;
            }
            return Task.CompletedTask;
        }
    }
}
