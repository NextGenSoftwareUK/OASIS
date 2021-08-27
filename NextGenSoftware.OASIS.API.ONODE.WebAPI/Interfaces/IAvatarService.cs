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
        //AuthenticateResponse RefreshToken(string token, string ipAddress);
        string GetTerms();
        AuthenticateResponse Authenticate(AuthenticateRequest model, string ipAddress);
        IAvatar RefreshToken(string token, string ipAddress);
        void RevokeToken(string token, string ipAddress);
        IAvatar Register(RegisterRequest model, string origin);
        OASISResult<bool> VerifyEmail(string token);
        void ForgotPassword(ForgotPasswordRequest model, string origin);
        void ValidateResetToken(ValidateResetTokenRequest model);
        void ResetPassword(ResetPasswordRequest model);
        //IEnumerable<AccountResponse> GetAll();
        //AccountResponse GetById(Guid id);
        IEnumerable<IAvatar> GetAll();
        AvatarImage GetAvatarImageById(Guid id);
        void Upload2DAvatarImage(Guid id, byte[] image);
        IAvatar GetById(Guid id);
        //AccountResponse Create(CreateRequest model);
        IAvatar Create(CreateRequest model);
        //AccountResponse Update(Guid id, UpdateRequest model);
        //IAvatar Update(Guid id, UpdateRequest model);
        //IAvatar Update(Guid id, IAvatar avatar);
        IAvatar Update(Guid id, UpdateRequest avatar);
        bool Delete(Guid id);
    }
}
