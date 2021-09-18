using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Avatar;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Security;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Interfaces
{
    public interface IAvatarService
    {
        string GetTerms();
        AuthenticateResponse Authenticate(AuthenticateRequest model, string ipAddress);
        IAvatar RefreshToken(string token, string ipAddress);
        void RevokeToken(string token, string ipAddress);
        IAvatar Register(RegisterRequest model, string origin);
        OASISResult<bool> VerifyEmail(string token);
        void ForgotPassword(ForgotPasswordRequest model, string origin);
        void ValidateResetToken(ValidateResetTokenRequest model);
        void ResetPassword(ResetPasswordRequest model);
        IEnumerable<IAvatar> GetAll();
        AvatarImage GetAvatarImageById(Guid id);
        void Upload2DAvatarImage(Guid id, byte[] image);
        IAvatar GetById(Guid id);
        IAvatar Create(CreateRequest model);
        IAvatar Update(Guid id, UpdateRequest avatar);
        bool Delete(Guid id);
        IAvatarDetail GetAvatarDetail(Guid id);
        // Task<ApiResponse<IAvatarThumbnail>> GetAvatarThumbnail(Guid id);
        //Task<ApiResponse<IAvatarDetail>> GetAvatarDetail(Guid id);
        //Task<ApiResponse<IEnumerable<IAvatarDetail>>> GetAllAvatarDetails();
    }
}
