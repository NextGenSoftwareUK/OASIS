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
                SqliteReadVersion.Validate(version);
                var row = await ReadUniqueAvatarAsync(_dbContext.Avatars.Where(p => p.Email == avatarEmail && (version == 0 || p.Version == version)));
                var avatarEntity = row == null ? null : GetAvatarFromEntity(row);
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
                    Exception = ex,
                    ErrorCode = ex is StorageIdentityAmbiguousException ? OASISResultErrorCodes.AmbiguousIdentity : null,
                    Message = ex.ToString()
                };
            }
        }

        public async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            try
            {
                SqliteReadVersion.Validate(version);
                var row = await ReadUniqueAvatarAsync(_dbContext.Avatars.Where(p => p.Username == avatarUsername && (version == 0 || p.Version == version)));
                var avatarEntity = row == null ? null : GetAvatarFromEntity(row);
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
                    Exception = ex,
                    ErrorCode = ex is StorageIdentityAmbiguousException ? OASISResultErrorCodes.AmbiguousIdentity : null,
                    Message = ex.ToString()
                };
            }
        }

        public OASISResult<IAvatar> LoadAvatar(Guid Id, int version = 0)
        {
            try
            {
                SqliteReadVersion.Validate(version);
                var row = _dbContext.Avatars.AsNoTracking().FirstOrDefault(p => p.Id == Id.ToString() && (version == 0 || p.Version == version));
                var avatarEntity = row == null ? null : GetAvatarFromEntity(row);
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
                    Exception = ex,
                    ErrorCode = ex is StorageIdentityAmbiguousException ? OASISResultErrorCodes.AmbiguousIdentity : null,
                    Message = ex.ToString()
                };
            }
        }

        public OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
        {
            try
            {
                SqliteReadVersion.Validate(version);
                var row = ReadUniqueAvatar(_dbContext.Avatars.Where(p => p.Email == avatarEmail && (version == 0 || p.Version == version)));
                var avatarEntity = row == null ? null : GetAvatarFromEntity(row);
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
                    Exception = ex,
                    ErrorCode = ex is StorageIdentityAmbiguousException ? OASISResultErrorCodes.AmbiguousIdentity : null,
                    Message = ex.ToString()
                };
            }
        }

        public async Task<OASISResult<IAvatar>> LoadAvatarAsync(string username, int version = 0)
        {
            try
            {
                SqliteReadVersion.Validate(version);
                var entity = await ReadUniqueAvatarAsync(_dbContext.Avatars.Where(p => p.Username == username && (version == 0 || p.Version == version)));
                var avatarEntity = entity == null ? null : GetAvatarFromEntity(entity);
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
                    Exception = ex,
                    ErrorCode = ex is StorageIdentityAmbiguousException ? OASISResultErrorCodes.AmbiguousIdentity : null,
                    Message = ex.ToString()
                };
            }
        }

        public async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            try
            {
                SqliteReadVersion.Validate(version);
                var avatarEntity = await _dbContext.Avatars
                    .SingleOrDefaultAsync(p => p.LocalProviderKey == providerKey && (version == 0 || p.Version == version));
                
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
                    Exception = ex,
                    ErrorCode = ex is StorageIdentityAmbiguousException ? OASISResultErrorCodes.AmbiguousIdentity : null,
                    Message = ex.ToString()
                };
            }
        }

        public OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
        {
            try
            {
                SqliteReadVersion.Validate(version);
                var avatarEntity = _dbContext.Avatars
                    .SingleOrDefault(p => p.LocalProviderKey == providerKey && (version == 0 || p.Version == version));
                
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
                    Exception = ex,
                    ErrorCode = ex is StorageIdentityAmbiguousException ? OASISResultErrorCodes.AmbiguousIdentity : null,
                    Message = ex.ToString()
                };
            }
        }

        public async Task<OASISResult<IAvatar>> LoadAvatarByVerificationTokenAsync(string verificationToken, int version = 0)
        {
            try
            {
                SqliteReadVersion.Validate(version);
                ArgumentException.ThrowIfNullOrWhiteSpace(verificationToken);
                var avatarEntity = await ReadUniqueAvatarAsync(_dbContext.Avatars.Where(p => p.VerificationToken == verificationToken && p.DeletedDate == DateTime.MinValue && (version == 0 || p.Version == version)));
                if (avatarEntity != null)
                    return new OASISResult<IAvatar> { IsLoaded = true, IsError = false, Message = "Avatar loaded successfully", Result = GetAvatarFromEntity(avatarEntity) };
                return new OASISResult<IAvatar> { IsLoaded = false, IsError = false, ErrorCode = OASISResultErrorCodes.RecordNotFound, Message = "Avatar not found" };
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar> { IsLoaded = false, IsError = true, Exception = ex, Message = ex.ToString(),
                    ErrorCode = ex is StorageIdentityAmbiguousException ? OASISResultErrorCodes.AmbiguousIdentity : ex is ArgumentException ? "HYPERDRIVE_PAYLOAD_INVALID" : null };
            }
        }

        public OASISResult<IAvatar> LoadAvatarByVerificationToken(string verificationToken, int version = 0)
        {
            try
            {
                SqliteReadVersion.Validate(version);
                ArgumentException.ThrowIfNullOrWhiteSpace(verificationToken);
                var avatarEntity = ReadUniqueAvatar(_dbContext.Avatars.Where(p => p.VerificationToken == verificationToken && p.DeletedDate == DateTime.MinValue && (version == 0 || p.Version == version)));
                if (avatarEntity != null)
                    return new OASISResult<IAvatar> { IsLoaded = true, IsError = false, Message = "Avatar loaded successfully", Result = GetAvatarFromEntity(avatarEntity) };
                return new OASISResult<IAvatar> { IsLoaded = false, IsError = false, ErrorCode = OASISResultErrorCodes.RecordNotFound, Message = "Avatar not found" };
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar> { IsLoaded = false, IsError = true, Exception = ex, Message = ex.ToString(),
                    ErrorCode = ex is StorageIdentityAmbiguousException ? OASISResultErrorCodes.AmbiguousIdentity : ex is ArgumentException ? "HYPERDRIVE_PAYLOAD_INVALID" : null };
            }
        }

        public async Task<OASISResult<IAvatar>> LoadAvatarByResetTokenAsync(string resetToken, int version = 0)
        {
            try
            {
                SqliteReadVersion.Validate(version);
                ArgumentException.ThrowIfNullOrWhiteSpace(resetToken);
                var avatarEntity = await ReadUniqueAvatarAsync(_dbContext.Avatars.Where(p => p.ResetToken == resetToken && p.DeletedDate == DateTime.MinValue && (version == 0 || p.Version == version)));
                if (avatarEntity != null)
                    return new OASISResult<IAvatar> { IsLoaded = true, IsError = false, Message = "Avatar loaded successfully", Result = GetAvatarFromEntity(avatarEntity) };
                return new OASISResult<IAvatar> { IsLoaded = false, IsError = false, ErrorCode = OASISResultErrorCodes.RecordNotFound, Message = "Avatar not found" };
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar> { IsLoaded = false, IsError = true, Exception = ex, Message = ex.ToString(),
                    ErrorCode = ex is StorageIdentityAmbiguousException ? OASISResultErrorCodes.AmbiguousIdentity : ex is ArgumentException ? "HYPERDRIVE_PAYLOAD_INVALID" : null };
            }
        }

        public OASISResult<IAvatar> LoadAvatarByResetToken(string resetToken, int version = 0)
        {
            try
            {
                SqliteReadVersion.Validate(version);
                ArgumentException.ThrowIfNullOrWhiteSpace(resetToken);
                var avatarEntity = ReadUniqueAvatar(_dbContext.Avatars.Where(p => p.ResetToken == resetToken && p.DeletedDate == DateTime.MinValue && (version == 0 || p.Version == version)));
                if (avatarEntity != null)
                    return new OASISResult<IAvatar> { IsLoaded = true, IsError = false, Message = "Avatar loaded successfully", Result = GetAvatarFromEntity(avatarEntity) };
                return new OASISResult<IAvatar> { IsLoaded = false, IsError = false, ErrorCode = OASISResultErrorCodes.RecordNotFound, Message = "Avatar not found" };
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar> { IsLoaded = false, IsError = true, Exception = ex, Message = ex.ToString(),
                    ErrorCode = ex is StorageIdentityAmbiguousException ? OASISResultErrorCodes.AmbiguousIdentity : ex is ArgumentException ? "HYPERDRIVE_PAYLOAD_INVALID" : null };
            }
        }

        public async Task<OASISResult<IAvatar>> LoadAvatarByRefreshTokenAsync(string refreshToken, int version = 0)
        {
            try
            {
                SqliteReadVersion.Validate(version);
                ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);
                var avatarEntity = await ReadUniqueAvatarAsync(_dbContext.Avatars.Where(p => (p.RefreshToken == refreshToken || p.RefreshTokens.Any(t => t.Token == refreshToken)) && p.DeletedDate == DateTime.MinValue && (version == 0 || p.Version == version)));
                if (avatarEntity != null)
                    return new OASISResult<IAvatar> { IsLoaded = true, IsError = false, Message = "Avatar loaded successfully", Result = GetAvatarFromEntity(avatarEntity) };
                return new OASISResult<IAvatar> { IsLoaded = false, IsError = false, ErrorCode = OASISResultErrorCodes.RecordNotFound, Message = "Avatar not found" };
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar> { IsLoaded = false, IsError = true, Exception = ex, Message = ex.ToString(),
                    ErrorCode = ex is StorageIdentityAmbiguousException ? OASISResultErrorCodes.AmbiguousIdentity : ex is ArgumentException ? "HYPERDRIVE_PAYLOAD_INVALID" : null };
            }
        }

        public OASISResult<IAvatar> LoadAvatarByRefreshToken(string refreshToken, int version = 0)
        {
            try
            {
                SqliteReadVersion.Validate(version);
                ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);
                var avatarEntity = ReadUniqueAvatar(_dbContext.Avatars.Where(p => (p.RefreshToken == refreshToken || p.RefreshTokens.Any(t => t.Token == refreshToken)) && p.DeletedDate == DateTime.MinValue && (version == 0 || p.Version == version)));
                if (avatarEntity != null)
                    return new OASISResult<IAvatar> { IsLoaded = true, IsError = false, Message = "Avatar loaded successfully", Result = GetAvatarFromEntity(avatarEntity) };
                return new OASISResult<IAvatar> { IsLoaded = false, IsError = false, ErrorCode = OASISResultErrorCodes.RecordNotFound, Message = "Avatar not found" };
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar> { IsLoaded = false, IsError = true, Exception = ex, Message = ex.ToString(),
                    ErrorCode = ex is StorageIdentityAmbiguousException ? OASISResultErrorCodes.AmbiguousIdentity : ex is ArgumentException ? "HYPERDRIVE_PAYLOAD_INVALID" : null };
            }
        }

        public OASISResult<IAvatar> SaveAvatar(IAvatar avatar)
        {
            AvatarModel avatarEntity = null;
            var persistenceEntered = false;
            try
            {
                avatarEntity = AddOrUpdateAvatar(CreateAvatarModel(avatar));
                persistenceEntered = true;
                _dbContext.SaveChanges();
                avatar.Id=Guid.Parse(avatarEntity.Id);
                return new OASISResult<IAvatar>
                {
                    IsSaved = true,
                    IsError = false,
                    Result = GetAvatarFromEntity(avatarEntity),
                    Message = avatarEntity.FirstName + " Record saved successfully"
                };
            }
            catch (Exception ex)
            {
                ex = AvatarSaveFailure(avatarEntity, avatar, persistenceEntered, ex);
                return new OASISResult<IAvatar>
                {
                    IsSaved = false,
                    IsError = true,
                    Exception = ex,
                    ErrorCode = ex is StorageIdentityAmbiguousException ? OASISResultErrorCodes.AmbiguousIdentity : null,
                    Message = ex.ToString()
                };
            }
        }

        public async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            AvatarModel avatarEntity = null;
            var persistenceEntered = false;
            try
            {
                avatarEntity = AddOrUpdateAvatar(CreateAvatarModel(avatar));
                persistenceEntered = true;
                await _dbContext.SaveChangesAsync();
                avatar.Id=Guid.Parse(avatarEntity.Id);
                //return Avatar;
                return new OASISResult<IAvatar>
                {
                    IsSaved = true,
                    IsError = false,
                    Message = avatar.FirstName + " Record saved successfully",
                    Result = GetAvatarFromEntity(avatarEntity)
                };
            }
            catch (Exception ex)
            {
                ex = AvatarSaveFailure(avatarEntity, avatar, persistenceEntered, ex);
                return new OASISResult<IAvatar>
                {
                    IsSaved = false,
                    IsError = true,
                    Exception = ex,
                    ErrorCode = ex is StorageIdentityAmbiguousException ? OASISResultErrorCodes.AmbiguousIdentity : null,
                    Message = ex.ToString()
                };
            }
        }

        private Exception AvatarSaveFailure(AvatarModel model, IAvatar source, bool persistenceEntered, Exception cause)
        {
            // Model identity is the actual attempted write, even if the caller changes its object later.
            var id = model?.Id ?? source?.Id.ToString();
            try { if (id != null) DetachFailedAvatarSave(id); }
            catch (Exception cleanup) { cause = new AggregateException("SQLite failure cleanup also failed.", cause, cleanup); }
            return persistenceEntered ? new SqliteMutationReconciliationException(Guid.Parse(id), cause) : cause;
        }

        private static AvatarModel RequireUniqueAvatar(List<AvatarModel> rows)
        {
            if (rows.Count > 1) throw new StorageIdentityAmbiguousException();
            return rows.SingleOrDefault();
        }

        private AvatarModel ReadUniqueAvatar(IQueryable<AvatarModel> query) => RequireUniqueAvatar(query.AsNoTracking().Take(2).ToList());
        private async Task<AvatarModel> ReadUniqueAvatarAsync(IQueryable<AvatarModel> query) => RequireUniqueAvatar(await query.AsNoTracking().Take(2).ToListAsync());

        private void DetachFailedAvatarSave(string avatarId)
        {
            foreach (var entry in _dbContext.ChangeTracker.Entries().ToList())
                if (entry.Entity is AvatarModel avatar && avatar.Id == avatarId ||
                    entry.Entity is RefreshTokenModel token && token.AvatarId == avatarId)
                    entry.State = EntityState.Detached;
        }

        private AvatarModel AddOrUpdateAvatar(AvatarModel incoming)
        {
            var existing = _dbContext.Avatars.Local.SingleOrDefault(row => row.Id == incoming.Id)
                ?? _dbContext.Avatars.SingleOrDefault(row => row.Id == incoming.Id);
            if (existing == null)
            {
                _dbContext.Avatars.Add(incoming);
                return incoming;
            }
            incoming.CreatedProviderType = existing.CreatedProviderType;
            incoming.HasCreatedProviderType = existing.HasCreatedProviderType;
            incoming.CreatedOASISType = existing.CreatedOASISType;
            incoming.HasCreatedOASISType = existing.HasCreatedOASISType;
            _dbContext.Entry(existing).CurrentValues.SetValues(incoming);
            foreach (var token in existing.RefreshTokens.ToList())
            {
                var replacement = incoming.RefreshTokens.SingleOrDefault(t => t.Token == token.Token);
                if (replacement == null) { _dbContext.Remove(token); existing.RefreshTokens.Remove(token); }
                else
                {
                    replacement.StorageId = token.StorageId;
                    _dbContext.Entry(token).CurrentValues.SetValues(replacement);
                }
            }
            foreach (var token in incoming.RefreshTokens.Where(t => !existing.RefreshTokens.Any(old => old.Token == t.Token)))
                existing.RefreshTokens.Add(token);
            return existing;
        }

        public AvatarModel CreateAvatarModel(IAvatar avatar)
        {
            ArgumentNullException.ThrowIfNull(avatar);
            var id=avatar.Id==Guid.Empty ? Guid.NewGuid():avatar.Id;
            var keys = avatar.ProviderUniqueStorageKey == null ? new Dictionary<ProviderType,string>() : new Dictionary<ProviderType,string>(avatar.ProviderUniqueStorageKey);
            if (!keys.ContainsKey(ProviderType.SQLLiteDBOASIS)) keys[ProviderType.SQLLiteDBOASIS] = id.ToString();
            ArgumentException.ThrowIfNullOrWhiteSpace(keys[ProviderType.SQLLiteDBOASIS]);
            return new()
            {
                AcceptTerms = avatar.AcceptTerms,
                Email = avatar.Email,
                FirstName = avatar.FirstName,
                Id = id.ToString(),
                LocalProviderKey = keys[ProviderType.SQLLiteDBOASIS],
                ProviderKeysJson = System.Text.Json.JsonSerializer.Serialize(keys),
                ProviderWalletsJson = NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.OasisJson.Serialize(avatar.ProviderWallets ?? new Dictionary<ProviderType,List<IProviderWallet>>()),
                ParentHolonId = avatar.ParentHolonId,
                MetaDataJson = System.Text.Json.JsonSerializer.Serialize(avatar.MetaData ?? new Dictionary<string,object>()),
                Version = avatar.Version,
                IsActive = avatar.IsActive,
                IsPublic = avatar.IsPublic,
                VersionId = avatar.VersionId,
                PreviousVersionId = avatar.PreviousVersionId,
                CustomHolonType = avatar.CustomHolonType,
                HasCreatedProviderType = avatar.CreatedProviderType != null,
                CreatedProviderType = avatar.CreatedProviderType?.Value ?? default,
                HasCreatedOASISType = avatar.CreatedOASISType != null,
                CreatedOASISType = avatar.CreatedOASISType?.Value ?? default,
                IsVerified = avatar.IsVerified,
                RefreshTokens = (avatar.RefreshTokens ?? new List<NextGenSoftware.OASIS.API.Core.Objects.RefreshToken>())
                    .Select(token => new RefreshTokenModel(token) { AvatarId = id.ToString() }).ToList(),
                CreatedByAvatarId = avatar.CreatedByAvatarId.ToString(),
                ModifiedByAvatarId = avatar.ModifiedByAvatarId.ToString(),
                DeletedByAvatarId = avatar.DeletedByAvatarId.ToString(),
                CreatedDate = avatar.CreatedDate,
                ModifiedDate = avatar.ModifiedDate,
                DeletedDate = avatar.DeletedDate,
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
                HasAvatarType = avatar.AvatarType != null,
                AvatarType = avatar.AvatarType?.Value ?? default,
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
                ParentHolonId = avatar.ParentHolonId,
                ProviderWallets = NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.OasisJson.DeserializeStored<Dictionary<ProviderType,List<IProviderWallet>>>(avatar.ProviderWalletsJson)
                    ?? throw new System.IO.InvalidDataException("Stored avatar wallets must be a JSON object."),
                MetaData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string,object>>(avatar.MetaDataJson)
                    ?? throw new System.IO.InvalidDataException("Stored avatar metadata must be a JSON object."),
                ProviderUniqueStorageKey = System.Text.Json.JsonSerializer.Deserialize<Dictionary<ProviderType,string>>(avatar.ProviderKeysJson)
                    ?? throw new System.IO.InvalidDataException("Stored avatar provider keys must be a JSON object."),
                Version = avatar.Version,
                IsActive = avatar.IsActive,
                IsPublic = avatar.IsPublic,
                VersionId = avatar.VersionId,
                PreviousVersionId = avatar.PreviousVersionId,
                CustomHolonType = avatar.CustomHolonType,
                CreatedProviderType = avatar.HasCreatedProviderType ? new NextGenSoftware.Utilities.EnumValue<ProviderType>(avatar.CreatedProviderType) : null,
                CreatedOASISType = avatar.HasCreatedOASISType ? new NextGenSoftware.Utilities.EnumValue<OASISType>(avatar.CreatedOASISType) : null,
                RefreshTokens = avatar.RefreshTokens.Select(token => token.GetRefreshToken()).ToList(),
                CreatedByAvatarId = avatar.CreatedByAvatarId == null ? Guid.Empty : Guid.Parse(avatar.CreatedByAvatarId),
                ModifiedByAvatarId = avatar.ModifiedByAvatarId == null ? Guid.Empty : Guid.Parse(avatar.ModifiedByAvatarId),
                DeletedByAvatarId = avatar.DeletedByAvatarId == null ? Guid.Empty : Guid.Parse(avatar.DeletedByAvatarId),
                CreatedDate = avatar.CreatedDate,
                ModifiedDate = avatar.ModifiedDate,
                DeletedDate = avatar.DeletedDate,
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
                AvatarType = avatar.HasAvatarType ? new EnumValue<AvatarType>(avatar.AvatarType) : null,
                Description = avatar.Description
            };
        }
    }
}
