using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SEEDSOASIS
{
    public partial class SEEDSOASIS
    {
        public OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0) =>
            LoadAvatarAsync(id, version).GetAwaiter().GetResult();

        public Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0) =>
            TelosOASIS.LoadAvatarAsync(id, version);

        public OASISResult<IAvatar> LoadAvatarByEmail(string email, int version = 0) =>
            LoadAvatarByEmailAsync(email, version).GetAwaiter().GetResult();

        public Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string email, int version = 0) =>
            TelosOASIS.LoadAvatarByEmailAsync(email, version);

        public OASISResult<IAvatar> LoadAvatarByUsername(string username, int version = 0) =>
            LoadAvatarByUsernameAsync(username, version).GetAwaiter().GetResult();

        public Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string username, int version = 0) =>
            TelosOASIS.LoadAvatarByUsernameAsync(username, version);

        public OASISResult<IAvatar> LoadAvatarByVerificationToken(string verificationToken, int version = 0) =>
            LoadAvatarByVerificationTokenAsync(verificationToken, version).GetAwaiter().GetResult();

        public Task<OASISResult<IAvatar>> LoadAvatarByVerificationTokenAsync(string verificationToken, int version = 0) =>
            TelosOASIS.LoadAvatarByVerificationTokenAsync(verificationToken, version);

        public OASISResult<IAvatar> LoadAvatarByResetToken(string resetToken, int version = 0) =>
            LoadAvatarByResetTokenAsync(resetToken, version).GetAwaiter().GetResult();

        public Task<OASISResult<IAvatar>> LoadAvatarByResetTokenAsync(string resetToken, int version = 0) =>
            TelosOASIS.LoadAvatarByResetTokenAsync(resetToken, version);

        public OASISResult<IAvatar> LoadAvatarByRefreshToken(string refreshToken, int version = 0) =>
            LoadAvatarByRefreshTokenAsync(refreshToken, version).GetAwaiter().GetResult();

        public Task<OASISResult<IAvatar>> LoadAvatarByRefreshTokenAsync(string refreshToken, int version = 0) =>
            TelosOASIS.LoadAvatarByRefreshTokenAsync(refreshToken, version);

        public OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0) =>
            LoadAllAvatarsAsync(version).GetAwaiter().GetResult();

        public Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0) =>
            TelosOASIS.LoadAllAvatarsAsync(version);

        public OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0) =>
            LoadAvatarDetailAsync(id, version).GetAwaiter().GetResult();

        public Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0) =>
            TelosOASIS.LoadAvatarDetailAsync(id, version);

        public OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string email, int version = 0) =>
            LoadAvatarDetailByEmailAsync(email, version).GetAwaiter().GetResult();

        public Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string email, int version = 0) =>
            TelosOASIS.LoadAvatarDetailByEmailAsync(email, version);

        public OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string username, int version = 0) =>
            LoadAvatarDetailByUsernameAsync(username, version).GetAwaiter().GetResult();

        public Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string username, int version = 0) =>
            TelosOASIS.LoadAvatarDetailByUsernameAsync(username, version);

        public OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0) =>
            LoadAllAvatarDetailsAsync(version).GetAwaiter().GetResult();

        public Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0) =>
            TelosOASIS.LoadAllAvatarDetailsAsync(version);
    }
}
