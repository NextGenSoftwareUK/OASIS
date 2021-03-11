using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Security;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.WebAPI
{
    public interface IAvatarService
    {
        //AuthenticateResponse RefreshToken(string token, string ipAddress);
        AuthenticateResponse Authenticate(AuthenticateRequest model, string ipAddress);
        IAvatar RefreshToken(string token, string ipAddress);
        void RevokeToken(string token, string ipAddress);
        IAvatar Register(RegisterRequest model, string origin);
        void VerifyEmail(string token);
        void ForgotPassword(ForgotPasswordRequest model, string origin);
        void ValidateResetToken(ValidateResetTokenRequest model);
        void ResetPassword(ResetPasswordRequest model);
        //IEnumerable<AccountResponse> GetAll();
        //AccountResponse GetById(Guid id);
        IEnumerable<IAvatar> GetAll();
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
