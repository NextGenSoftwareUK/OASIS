using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Avatar;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Security;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Interfaces
{
    [Obsolete("IAvatarService is being phased out. Controllers should call AvatarManager directly.")]
    public interface IAvatarService
    {
        //TODO: Want to phase this out, not needed, moving more and more code into AvatarManager.
        Task<OASISResult<string>> GetTerms();
        Task<OASISResult<string>> ValidateAccountToken(string accountToken);
        //Task<OASISResult<AuthenticateResponse>> Authenticate(AuthenticateRequest model, string ipAddress);
        //Task<OASISResult<IAvatar>> Authenticate(AuthenticateRequest model, string ipAddress);
        Task<OASISResult<IAvatar>> RefreshToken(string token, string ipAddress);
        Task<OASISResult<string>> RevokeToken(string token, string ipAddress);
        Task<OASISResult<IAvatar>> RegisterAsync(RegisterRequest model, string origin);
        OASISResult<IAvatar> Register(RegisterRequest model, string origin);
        Task<OASISResult<bool>> VerifyEmail(string token);
        //Task<OASISResult<string>> ForgotPassword(ForgotPasswordRequest model, string origin);
        Task<OASISResult<string>> ValidateResetToken(ValidateResetTokenRequest model);
        //Task<OASISResult<string>> ResetPassword(ResetPasswordRequest model);
        //Task<OASISResult<IEnumerable<IAvatar>>> GetAll();
        Task<OASISResult<AvatarPortrait>> GetAvatarPortraitById(Guid id);
        Task<OASISResult<AvatarPortrait>> GetAvatarPortraitByUsername(string userName);
        Task<OASISResult<AvatarPortrait>> GetAvatarPortraitByEmail(string email);
        Task<OASISResult<bool>> UploadAvatarPortrait(AvatarPortrait avatarImage);
        //Task<OASISResult<IAvatar>> GetById(Guid id);
        //Task<OASISResult<IAvatar>> GetByUsername(string userName);
        //Task<OASISResult<IAvatar>> GetByEmail(string email);
        Task<OASISResult<IAvatar>> Create(CreateRequest model);
        Task<OASISResult<IAvatar>> Update(Guid id, UpdateRequest avatar);
        Task<OASISResult<IAvatar>> UpdateByEmail(string email, UpdateRequest avatar);
        Task<OASISResult<IAvatar>> UpdateByUsername(string username, UpdateRequest avatar);
        //Task<OASISResult<bool>> Delete(Guid id);
        //Task<OASISResult<bool>> DeleteByUsername(string username);
        //Task<OASISResult<bool>> DeleteByEmail(string email);
        //Task<OASISResult<IAvatarDetail>> GetAvatarDetail(Guid id);
        //Task<OASISResult<IAvatarDetail>> GetAvatarDetailByUsername(string username);
        //Task<OASISResult<IAvatarDetail>> GetAvatarDetailByEmail(string email);
        //Task<OASISResult<IEnumerable<IAvatarDetail>>> GetAllAvatarDetails();
        Task<OASISResult<string>> GetAvatarUmaJsonById(Guid id);
        Task<OASISResult<string>> GetAvatarUmaJsonByUsername(string username);
        Task<OASISResult<string>> GetAvatarUmaJsonByEmail(string mail);
        Task<OASISResult<IAvatar>> GetLoggedInAvatar();
        //Task<OASISResult<ISearchResults>> Search(ISearchParams searchParams);
        //Task<OASISResult<bool>> LinkProviderKeyToAvatar(Guid avatarId, ProviderType providerType, string key);
        //Task<OASISResult<bool>> LinkPrivateProviderKeyToAvatar(Guid avatarId, ProviderType providerType, string key);
        //Task<OASISResult<string>> GetProviderKeyForAvatar(string avatarUsername, ProviderType providerType);
        //Task<OASISResult<string>> GetPrivateProviderKeyForAvatar(Guid avatarId, ProviderType providerType);
        Task<OASISResult<KarmaAkashicRecord>> AddKarmaToAvatar(Guid avatarId, AddRemoveKarmaToAvatarRequest addRemoveKarmaToAvatarRequest);
        Task<OASISResult<KarmaAkashicRecord>> RemoveKarmaFromAvatar(Guid avatarId, AddRemoveKarmaToAvatarRequest addKarmaToAvatarRequest);
        
        // Avatar Session Management
        Task<OASISResult<NextGenSoftware.OASIS.API.Core.Objects.Avatar.AvatarSessionManagement>> GetAvatarSessionsAsync(Guid avatarId);
        Task<OASISResult<bool>> LogoutAvatarSessionsAsync(Guid avatarId, System.Collections.Generic.List<string> sessionIds);
        Task<OASISResult<bool>> LogoutAllAvatarSessionsAsync(Guid avatarId);
        Task<OASISResult<NextGenSoftware.OASIS.API.Core.Objects.Avatar.AvatarSession>> CreateAvatarSessionAsync(Guid avatarId, NextGenSoftware.OASIS.API.Core.Objects.Avatar.CreateSessionRequest request);
        Task<OASISResult<NextGenSoftware.OASIS.API.Core.Objects.Avatar.AvatarSession>> UpdateAvatarSessionAsync(Guid avatarId, string sessionId, NextGenSoftware.OASIS.API.Core.Objects.Avatar.UpdateSessionRequest request);
        Task<OASISResult<NextGenSoftware.OASIS.API.Core.Objects.Avatar.AvatarSessionStats>> GetAvatarSessionStatsAsync(Guid avatarId);
    }
}
