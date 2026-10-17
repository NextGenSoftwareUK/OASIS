using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services
{
    public partial class TelegramBotService
    {
        private async Task HandleMintNFTCommand(long chatId, User user, string[] args, CancellationToken cancellationToken)
        {
            try
            {
                _logger?.LogInformation($"[TelegramBot] User {user.Id} requested NFT mint");

                var avatarResult = await _telegramProvider.GetTelegramAvatarByTelegramIdAsync(user.Id);
                if (avatarResult == null || avatarResult.IsError || avatarResult.Result == null)
                {
                    await _botClient.SendTextMessageAsync(chatId, "❌ You need to /start first to create your OASIS avatar!", cancellationToken: cancellationToken);
                    return;
                }

                var avatar = avatarResult.Result;
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

                if (wallet.Length < 32)
                {
                    await _botClient.SendTextMessageAsync(chatId, "❌ Invalid Solana wallet address. It should be 32-44 characters long.", cancellationToken: cancellationToken);
                    return;
                }

                if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(description))
                {
                    await _botClient.SendTextMessageAsync(chatId, "❌ Title and description cannot be empty!", cancellationToken: cancellationToken);
                    return;
                }

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
                            await _botClient.SendTextMessageAsync(chatId, $"✨ Bonus: +50 Karma for minting an NFT!", cancellationToken: cancellationToken);
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
                    $"❌ An unexpected error occurred while minting your NFT.\n\nError: {ex.Message}\n\nPlease try again later.",
                    cancellationToken: cancellationToken
                );
            }
        }

        private async Task HandlePhotoMessageAsync(Telegram.Bot.Types.Message message, CancellationToken cancellationToken)
        {
            try
            {
                var chatId = message.Chat.Id;
                var user = message.From;
                var caption = message.Caption ?? "";

                _logger?.LogInformation($"[TelegramBot] User {user.Id} sent photo with caption: {caption}");

                var avatarResult = await _telegramProvider.GetTelegramAvatarByTelegramIdAsync(user.Id);
                if (avatarResult == null || avatarResult.IsError || avatarResult.Result == null)
                {
                    await _botClient.SendTextMessageAsync(chatId, "❌ You need to /start first to create your OASIS avatar!", cancellationToken: cancellationToken);
                    return;
                }

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

                if (wallet.Length < 32)
                {
                    await _botClient.SendTextMessageAsync(chatId, "❌ Invalid Solana wallet address. It should be 32-44 characters long.", cancellationToken: cancellationToken);
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

                var photo = message.Photo[message.Photo.Length - 1];
                var fileInfo = await _botClient.GetFileAsync(photo.FileId, cancellationToken);

                using var memoryStream = new System.IO.MemoryStream();
                await _botClient.DownloadFile(fileInfo.FilePath, memoryStream, cancellationToken);
                var imageBytes = memoryStream.ToArray();

                _logger?.LogInformation($"[TelegramBot] Downloaded image: {imageBytes.Length} bytes");

                var fileName = $"badge_{user.Id}_{DateTime.UtcNow.Ticks}.png";
                var uploadResult = await _pinataService.UploadImageAsync(imageBytes, fileName);

                if (uploadResult.IsError)
                {
                    await _botClient.SendTextMessageAsync(chatId, $"❌ Failed to upload image to IPFS: {uploadResult.Message}", cancellationToken: cancellationToken);
                    return;
                }

                var ipfsImageUrl = uploadResult.Result;
                _logger?.LogInformation($"[TelegramBot] Image uploaded to IPFS: {ipfsImageUrl}");

                await _botClient.SendTextMessageAsync(chatId, $"✅ Image uploaded to IPFS!\n🔗 {ipfsImageUrl}\n\n🎨 Now minting your NFT...", cancellationToken: cancellationToken);

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
                    await _botClient.SendTextMessageAsync(chatId, $"❌ NFT Minting Failed!\n\nError: {mintResult.Message}\n\nYour image is still on IPFS: {ipfsImageUrl}", cancellationToken: cancellationToken);
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

                    try
                    {
                        await _avatarManager.AddKarmaToAvatarAsync(
                            avatarResult.Result.Id,
                            NextGenSoftware.OASIS.API.Core.Enums.KarmaTypePositive.OurWorldBeASuperHero,
                            NextGenSoftware.OASIS.API.Core.Enums.KarmaSourceType.dApp,
                            "Custom Badge NFT",
                            $"Minted custom badge NFT: {title}"
                        );
                        await _botClient.SendTextMessageAsync(chatId, $"✨ Bonus: +100 Karma for creating a custom badge NFT!", cancellationToken: cancellationToken);
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
                await _botClient.SendTextMessageAsync(message.Chat.Id, $"❌ An error occurred while processing your image.\n\nError: {ex.Message}", cancellationToken: cancellationToken);
            }
        }

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
                return new OASISResult<bool> { IsError = true, Message = ex.Message };
            }
        }
    }
}
