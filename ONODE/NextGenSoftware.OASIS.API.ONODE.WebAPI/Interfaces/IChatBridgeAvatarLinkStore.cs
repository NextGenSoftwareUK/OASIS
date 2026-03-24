using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.ChatBridge;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Interfaces
{
    public interface IChatBridgeAvatarLinkStore
    {
        Task<BridgedAvatarLinkDocument> GetByDiscordUserIdAsync(ulong userId);
        Task<BridgedAvatarLinkDocument> GetByTelegramUserIdAsync(long userId);
        Task UpsertAsync(BridgedAvatarLinkDocument doc);
        Task<bool> RemoveByDiscordUserIdAsync(ulong userId);
        Task<bool> RemoveByTelegramUserIdAsync(long userId);
    }
}
