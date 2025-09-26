using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;

namespace NextGenSoftware.OASIS.STAR.WebUI.Services
{
    public interface ISTARService
    {
        Task<OASISResult<IOmiverse>> IgniteSTARAsync();
        Task<OASISResult<bool>> ExtinguishStarAsync();
        Task<OASISResult<bool>> IsSTARIgnitedAsync();
        Task<OASISResult<IAvatar>> GetBeamedInAvatarAsync();
        Task<OASISResult<IAvatar>> BeamInAvatarAsync();
        Task<OASISResult<IAvatar>> CreateAvatarAsync(string username, string email, string password);
        Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id);
        Task<OASISResult<IAvatar>> LoadAvatarAsync(string username);
        Task<OASISResult<IAvatar>> LoadAvatarAsync(string username, string password);
        Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar);
        Task<OASISResult<bool>> DeleteAvatarAsync(Guid id);
        Task<OASISResult<List<IAvatar>>> LoadAllAvatarsAsync();
        Task<OASISResult<List<IAvatar>>> SearchAvatarsAsync(string searchTerm);
        Task<OASISResult<IKarmaAkashicRecord>> GetKarmaAsync(Guid avatarId);
        Task<OASISResult<IKarmaAkashicRecord>> AddKarmaAsync(Guid avatarId, int karma);
        Task<OASISResult<IKarmaAkashicRecord>> RemoveKarmaAsync(Guid avatarId, int karma);
        Task<OASISResult<IKarmaAkashicRecord>> SetKarmaAsync(Guid avatarId, int karma);
        Task<OASISResult<List<IKarmaAkashicRecord>>> GetAllKarmaAsync();
        Task<OASISResult<List<IKarmaAkashicRecord>>> GetKarmaBetweenAsync(DateTime fromDate, DateTime toDate);
        Task<OASISResult<List<IKarmaAkashicRecord>>> GetKarmaAboveAsync(int karmaLevel);
        Task<OASISResult<List<IKarmaAkashicRecord>>> GetKarmaBelowAsync(int karmaLevel);
    }
}
