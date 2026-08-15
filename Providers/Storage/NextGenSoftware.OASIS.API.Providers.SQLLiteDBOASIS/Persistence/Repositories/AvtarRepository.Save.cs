using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Interfaces;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories
{
    public partial class AvatarRepository : IAvatarRepository
    {

        public async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            try
            {
                var avatarEntity = _dbContext.Avatars
                    .ToList()
                    .Where(p => p.Email == avatarEmail && p.Version == version)
                    .Select(GetAvatarFromEntity)
                    .FirstOrDefault();
                if (avatarEntity == null)
                    return new OASISResult<IAvatar>
                    {
                        IsLoaded = false,
                        IsError = false,
                        Message = "No Avatar found"
                    };

                return new OASISResult<IAvatar>
                {
                    IsLoaded = true,
                    IsError = false,
                    Message = "Avatar Loaded Successfully",
                    Result = avatarEntity
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar>
                {
                    IsLoaded = false,
                    IsError = true,
                    Message = ex.ToString()
                };
            }
        }

        public async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            try
            {
                var avatarEntity = _dbContext.Avatars
                    .ToList()
                    .Where(p => p.Username == avatarUsername && p.Version == version)
                    .Select(GetAvatarFromEntity)
                    .FirstOrDefault();
                if (avatarEntity == null)
                    return new OASISResult<IAvatar>
                    {
                        IsLoaded = false,
                        IsError = false,
                        Message = "No Avatar found"
                    };

                return new OASISResult<IAvatar>
                {
                    IsLoaded = true,
                    IsError = false,
                    Message = "Avatar Loaded Successfully",
                    Result = avatarEntity
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar>
                {
                    IsLoaded = false,
                    IsError = true,
                    Message = ex.ToString()
                };
            }
        }

        public OASISResult<IAvatar> LoadAvatar(Guid Id, int version = 0)
        {
            try
            {
                var avatarEntity = _dbContext.Avatars
                    .ToList()
                    .Select(GetAvatarFromEntity)
                    .FirstOrDefault(p => p.Id == Id && p.Version == version);
                if (avatarEntity == null)
                    return new OASISResult<IAvatar>
                    {
                        IsLoaded = false,
                        IsError = false,
                        Message = "No Avatar found"
                    };

                return new OASISResult<IAvatar>
                {
                    IsLoaded = true,
                    IsError = false,
                    Message = "Avatar Loaded Successfully",
                    Result = avatarEntity
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar>
                {
                    IsLoaded = false,
                    IsError = true,
                    Message = ex.ToString()
                };
            }
        }

        public OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
        {
            try
            {
                var avatarEntity =
                    _dbContext.Avatars
                        .ToList()
                        .Select(GetAvatarFromEntity)
                        .FirstOrDefault(p => p.Email == avatarEmail && p.Version == version);
                if (avatarEntity == null)
                    return new OASISResult<IAvatar>
                    {
                        IsLoaded = false,
                        IsError = false,
                        Message = "No Avatar found"
                    };

                return new OASISResult<IAvatar>
                {
                    IsLoaded = true,
                    IsError = false,
                    Message = "Avatar Loaded Successfully",
                    Result = avatarEntity
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar>
                {
                    IsLoaded = false,
                    IsError = true,
                    Message = ex.ToString()
                };
            }
        }

        public async Task<OASISResult<IAvatar>> LoadAvatarAsync(string username, int version = 0)
        {
            try
            {
                var avatarEntity = _dbContext.Avatars
                    .ToList()
                    .Where(p => p.Username == username && p.Version == version)
                    .Select(GetAvatarFromEntity)
                    .FirstOrDefault();
                if (avatarEntity == null)
                    return new OASISResult<IAvatar>
                    {
                        IsLoaded = false,
                        IsError = false,
                        Message = "No Avatar found"
                    };

                return new OASISResult<IAvatar>
                {
                    IsLoaded = true,
                    IsError = false,
                    Message = "Avatar Loaded Successfully",
                    Result = avatarEntity
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar>
                {
                    IsLoaded = false,
                    IsError = true,
                    Message = ex.ToString()
                };
            }
        }

        public async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            try
            {
                var avatarEntity = await _dbContext.Avatars
                    .FirstOrDefaultAsync(p => p.ProviderKey.Any(pk => pk.Value == providerKey) && p.Version == version);
                
                if (avatarEntity != null)
                {
                    var avatar = GetAvatarFromEntity(avatarEntity);
                    return new OASISResult<IAvatar>
                    {
                        IsLoaded = true,
                        IsError = false,
                        Message = "Avatar loaded successfully",
                        Result = avatar
                    };
                }
                else
                {
                    return new OASISResult<IAvatar>
                    {
                        IsLoaded = false,
                        IsError = false,
                        Message = "Avatar not found"
                    };
                }
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar>
                {
                    IsLoaded = false,
                    IsError = true,
                    Message = ex.ToString()
                };
            }
        }

        public OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
        {
            try
            {
                var avatarEntity = _dbContext.Avatars
                    .FirstOrDefault(p => p.ProviderKey.Any(pk => pk.Value == providerKey) && p.Version == version);
                
                if (avatarEntity != null)
                {
                    var avatar = GetAvatarFromEntity(avatarEntity);
                    return new OASISResult<IAvatar>
                    {
                        IsLoaded = true,
                        IsError = false,
                        Message = "Avatar loaded successfully",
                        Result = avatar
                    };
                }
                else
                {
                    return new OASISResult<IAvatar>
                    {
                        IsLoaded = false,
                        IsError = false,
                        Message = "Avatar not found"
                    };
                }
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar>
                {
                    IsLoaded = false,
                    IsError = true,
                    Message = ex.ToString()
                };
            }
        }

        public async Task<OASISResult<IAvatar>> LoadAvatarByVerificationTokenAsync(string verificationToken, int version = 0)
        {
            try
            {
                var avatarEntity = await _dbContext.Avatars
                    .FirstOrDefaultAsync(p => p.VerificationToken == verificationToken);
                if (avatarEntity != null)
                    return new OASISResult<IAvatar> { IsLoaded = true, IsError = false, Message = "Avatar loaded successfully", Result = GetAvatarFromEntity(avatarEntity) };
                return new OASISResult<IAvatar> { IsLoaded = false, IsError = false, Message = "Avatar not found" };
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar> { IsLoaded = false, IsError = true, Message = ex.ToString() };
            }
        }

        public OASISResult<IAvatar> LoadAvatarByVerificationToken(string verificationToken, int version = 0)
        {
            try
            {
                var avatarEntity = _dbContext.Avatars
                    .FirstOrDefault(p => p.VerificationToken == verificationToken);
                if (avatarEntity != null)
                    return new OASISResult<IAvatar> { IsLoaded = true, IsError = false, Message = "Avatar loaded successfully", Result = GetAvatarFromEntity(avatarEntity) };
                return new OASISResult<IAvatar> { IsLoaded = false, IsError = false, Message = "Avatar not found" };
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar> { IsLoaded = false, IsError = true, Message = ex.ToString() };
            }
        }

        public async Task<OASISResult<IAvatar>> LoadAvatarByResetTokenAsync(string resetToken, int version = 0)
        {
            try
            {
                var avatarEntity = await _dbContext.Avatars
                    .FirstOrDefaultAsync(p => p.ResetToken == resetToken);
                if (avatarEntity != null)
                    return new OASISResult<IAvatar> { IsLoaded = true, IsError = false, Message = "Avatar loaded successfully", Result = GetAvatarFromEntity(avatarEntity) };
                return new OASISResult<IAvatar> { IsLoaded = false, IsError = false, Message = "Avatar not found" };
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar> { IsLoaded = false, IsError = true, Message = ex.ToString() };
            }
        }

        public OASISResult<IAvatar> LoadAvatarByResetToken(string resetToken, int version = 0)
        {
            try
            {
                var avatarEntity = _dbContext.Avatars
                    .FirstOrDefault(p => p.ResetToken == resetToken);
                if (avatarEntity != null)
                    return new OASISResult<IAvatar> { IsLoaded = true, IsError = false, Message = "Avatar loaded successfully", Result = GetAvatarFromEntity(avatarEntity) };
                return new OASISResult<IAvatar> { IsLoaded = false, IsError = false, Message = "Avatar not found" };
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar> { IsLoaded = false, IsError = true, Message = ex.ToString() };
            }
        }

        public async Task<OASISResult<IAvatar>> LoadAvatarByRefreshTokenAsync(string refreshToken, int version = 0)
        {
            try
            {
                var avatarEntity = await _dbContext.Avatars
                    .FirstOrDefaultAsync(p => p.RefreshToken == refreshToken);
                if (avatarEntity != null)
                    return new OASISResult<IAvatar> { IsLoaded = true, IsError = false, Message = "Avatar loaded successfully", Result = GetAvatarFromEntity(avatarEntity) };
                return new OASISResult<IAvatar> { IsLoaded = false, IsError = false, Message = "Avatar not found" };
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar> { IsLoaded = false, IsError = true, Message = ex.ToString() };
            }
        }

        public OASISResult<IAvatar> LoadAvatarByRefreshToken(string refreshToken, int version = 0)
        {
            try
            {
                var avatarEntity = _dbContext.Avatars
                    .FirstOrDefault(p => p.RefreshToken == refreshToken);
                if (avatarEntity != null)
                    return new OASISResult<IAvatar> { IsLoaded = true, IsError = false, Message = "Avatar loaded successfully", Result = GetAvatarFromEntity(avatarEntity) };
                return new OASISResult<IAvatar> { IsLoaded = false, IsError = false, Message = "Avatar not found" };
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar> { IsLoaded = false, IsError = true, Message = ex.ToString() };
            }
        }

        public OASISResult<IAvatar> SaveAvatar(IAvatar avatar)
        {
            try
            {
                AvatarModel avatarEntity = CreateAvatarModel(avatar);
                _dbContext.Avatars.Add(avatarEntity);
                _dbContext.SaveChangesAsync();
                return new OASISResult<IAvatar>
                {
                    IsSaved = true,
                    IsError = false,
                    Message = avatarEntity.FirstName + " Record saved successfully"
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar>
                {
                    IsSaved = false,
                    IsError = true,
                    Message = ex.ToString()
                };
            }
        }

        public async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            try
            {
                AvatarModel avatarEntity = CreateAvatarModel(avatar);
                _dbContext.Avatars.Add(avatarEntity);
                await _dbContext.SaveChangesAsync();
                //return Avatar;
                return new OASISResult<IAvatar>
                {
                    IsSaved = true,
                    IsError = false,
                    Message = avatar.FirstName + " Record saved successfully",
                    Result = avatar
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar>
                {
                    IsSaved = false,
                    IsError = true,
                    Message = ex.ToString()
                };
            }
        }

        public AvatarModel CreateAvatarModel(IAvatar avatar)
        {
            return new()
            {
                AcceptTerms = avatar.AcceptTerms,
                Email = avatar.Email,
                FirstName = avatar.FirstName,
                Id = avatar.Id.ToString(),
                JwtToken = avatar.JwtToken,
                LastName = avatar.LastName,
                Name = avatar.FullName,
                Password = avatar.Password,
                PasswordReset = avatar.PasswordReset,
                RefreshToken = avatar.RefreshToken,
                ResetToken = avatar.ResetToken,
                ResetTokenExpires = avatar.ResetTokenExpires,
                Title = avatar.Title,
                Username = avatar.Username,
                VerificationToken = avatar.VerificationToken,
                Verified = avatar.Verified,
                AvatarType = avatar.AvatarType.Value,
                Description = avatar.Description
            };
        }

        private IAvatar GetAvatarFromEntity(AvatarModel avatar)
        {
            return new Avatar
            {
                AcceptTerms = avatar.AcceptTerms,
                Email = avatar.Email,
                FirstName = avatar.FirstName,
                Id = Guid.Parse(avatar.Id),
                JwtToken = avatar.JwtToken,
                LastName = avatar.LastName,
                Password = avatar.Password,
                PasswordReset = avatar.PasswordReset,
                RefreshToken = avatar.RefreshToken,
                ResetToken = avatar.ResetToken,
                ResetTokenExpires = avatar.ResetTokenExpires,
                Title = avatar.Title,
                Username = avatar.Username,
                VerificationToken = avatar.VerificationToken,
                Verified = avatar.Verified,
                AvatarId = Guid.Parse(avatar.Id),
                AvatarType = new EnumValue<AvatarType>(avatar.AvatarType),
                Description = avatar.Description
            };
        }
    }
}
