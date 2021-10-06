using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Avatar;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Security;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Interfaces
{
    public interface IAvatarService
    {
        string GetTerms();
        OASISResult<string> ValidateAccountToken(string accountToken);
        AuthenticateResponse Authenticate(AuthenticateRequest model, string ipAddress);
        IAvatar RefreshToken(string token, string ipAddress);
        void RevokeToken(string token, string ipAddress);
        IAvatar Register(RegisterRequest model, string origin);
        OASISResult<bool> VerifyEmail(string token);
        void ForgotPassword(ForgotPasswordRequest model, string origin);
        void ValidateResetToken(ValidateResetTokenRequest model);
        void ResetPassword(ResetPasswordRequest model);
        IEnumerable<IAvatar> GetAll();
        OASISResult<AvatarImage> GetAvatarImageById(Guid id);
        Task<OASISResult<AvatarImage>> GetAvatarImageByUsername(string userName);
        Task<OASISResult<AvatarImage>> GetAvatarImageByEmail(string email);
        void Upload2DAvatarImage(AvatarImage avatarImage);
        IAvatar GetById(Guid id);
        Task<IAvatar> GetByUsername(string userName);
        Task<IAvatar> GetByEmail(string email);
        IAvatar Create(CreateRequest model);
        Task<IAvatar> Update(Guid id, UpdateRequest avatar);
        Task<IAvatar> UpdateByEmail(string email, UpdateRequest avatar);
        Task<IAvatar> UpdateByUsername(string username, UpdateRequest avatar);
        bool Delete(Guid id);
        Task<bool> DeleteByUsername(string username);
        Task<bool> DeleteByEmail(string email);
        OASISResult<IAvatarDetail> GetAvatarDetail(Guid id);
        Task<OASISResult<IAvatarDetail>> GetAvatarDetailByUsername(string username);
        Task<OASISResult<IAvatarDetail>> GetAvatarDetailByEmail(string email);
        OASISResult<IEnumerable<IAvatarDetail>> GetAllAvatarDetails();

        // Task<ApiResponse<IAvatarThumbnail>> GetAvatarThumbnail(Guid id);
        //Task<ApiResponse<IAvatarDetail>> GetAvatarDetail(Guid id);
        //Task<ApiResponse<IEnumerable<IAvatarDetail>>> GetAllAvatarDetails();
    }
}
