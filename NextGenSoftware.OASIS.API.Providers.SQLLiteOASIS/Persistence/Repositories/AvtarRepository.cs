using NextGenSoftware.OASIS.API.Providers.SQLLiteOASIS.Exceptions;
using NextGenSoftware.OASIS.API.Providers.SQLLiteOASIS.Interfaces;
using NextGenSoftware.OASIS.API.Providers.SQLLiteOASIS.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.SQLLiteOASIS.Entities;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteOASIS.Persistence.Repositories
{
    public class AvtarRepository : IAvtarRepository
    {
        private readonly DataContext eFContext;

        public AvtarRepository(DataContext eFContext)
        {
            this.eFContext = eFContext;
        }

        public OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
        {
            try
            {
                var avatar = this.eFContext.AvatarEntities.Where(p => p.Id == id).FirstOrDefault();
                if (avatar != null)
                {
                    if (softDelete)
                    {
                        avatar.IsActive = false;
                        this.eFContext.AvatarEntities.Update(avatar);
                    }
                    else
                    {
                        this.eFContext.AvatarEntities.Remove(avatar);
                    }
                    this.eFContext.SaveChangesAsync();
                    return new OASISResult<bool>
                    {
                        IsError = false,
                        Message = "Avatar Deleted Successfully",
                        Result = true
                    };
                }
                else
                {
                    return new OASISResult<bool>
                    {
                        IsError = true,
                        Message = "Something went wrong! please try again later",
                        Result = false
                    };
                }

            }
            catch (Exception ex)
            {
                return new OASISResult<bool>
                {
                    IsError = true,
                    Message = ex.ToString(),
                    Result = false
                };
            }
        }

        public OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
        {
            try
            {
                var avatar = this.eFContext.AvatarEntities.Where(p => p.Email == avatarEmail).FirstOrDefault();
                if (avatar != null)
                {
                    if (softDelete)
                    {
                        avatar.IsActive = false;
                        this.eFContext.AvatarEntities.Update(avatar);
                    }
                    else
                    {
                        this.eFContext.AvatarEntities.Remove(avatar);
                    }
                    this.eFContext.SaveChangesAsync();
                    return new OASISResult<bool>
                    {
                        IsError = false,
                        Message = "Avatar Deleted Successfully",
                        Result = true
                    };
                }
                else
                {
                    return new OASISResult<bool>
                    {
                        IsError = true,
                        Message = "Something went wrong! please try again later",
                        Result = false
                    };
                }

            }
            catch (Exception ex)
            {
                return new OASISResult<bool>
                {
                    IsError = true,
                    Message = ex.ToString(),
                    Result = false
                };
            }
        }

        public OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
        {
            try
            {
                var avatar = this.eFContext.AvatarEntities.Where(p => p.Username == avatarUsername).FirstOrDefault();
                if (avatar != null)
                {
                    if (softDelete)
                    {
                        avatar.IsActive = false;
                        this.eFContext.AvatarEntities.Update(avatar);
                    }
                    else
                    {
                        this.eFContext.AvatarEntities.Remove(avatar);
                    }
                    this.eFContext.SaveChangesAsync();
                    return new OASISResult<bool>
                    {
                        IsError = false,
                        Message = "Avatar Deleted Successfully",
                        Result = true
                    };
                }
                else
                {
                    return new OASISResult<bool>
                    {
                        IsError = true,
                        Message = "Something went wrong! please try again later",
                        Result = false
                    };
                }
            }
            catch (Exception ex)
            {
                return new OASISResult<bool>
                {
                    IsError = true,
                    Message = ex.ToString(),
                    Result = false
                };
            }
        }
        public async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
        {
            try
            {
                var avatar = await this.eFContext.AvatarEntities.Where(p => p.Username == avatarUsername).FirstOrDefaultAsync();
                if (avatar != null)
                {
                    if (softDelete)
                    {
                        avatar.IsActive = false;
                        this.eFContext.AvatarEntities.Update(avatar);
                    }
                    else
                    {
                        this.eFContext.AvatarEntities.Remove(avatar);
                    }
                    await this.eFContext.SaveChangesAsync();
                    return new OASISResult<bool>
                    {
                        IsError = false,
                        Message = "Avatar Deleted Successfully",
                        Result = true
                    };
                }
                else
                {
                    return new OASISResult<bool>
                    {
                        IsError = true,
                        Message = "Something went wrong! please try again later",
                        Result = false
                    };
                }

            }
            catch (Exception ex)
            {
                return new OASISResult<bool>
                {
                    IsError = true,
                    Message = ex.ToString(),
                    Result = false
                };
            }
        }

        public OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
        {
            //try
            //{
            //    var avatar = this.eFContext.AvatarEntities.Where(p => p.providerKey == providerKey).FirstOrDefault();
            //    if (avatar != null)
            //    {
            //        if (softDelete)
            //        {
            //            avatar.IsActive = false;
            //            this.eFContext.AvatarEntities.Update(avatar);
            //        }
            //        else
            //        {
            //            this.eFContext.AvatarEntities.Remove(avatar);
            //        }
            //        this.eFContext.SaveChangesAsync();
            //        return true;
            //    }
            //    else { return false; }
            //}
            //catch (Exception)
            //{
            //    throw;
            //}
            throw new NotImplementedException();
        }

        public async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            try
            {
                var avatar = await this.eFContext.AvatarEntities.Where(p => p.Id == id).FirstOrDefaultAsync();
                if (avatar != null)
                {
                    if (softDelete)
                    {
                        avatar.IsActive = false;
                        this.eFContext.AvatarEntities.Update(avatar);
                    }
                    else
                    {
                        this.eFContext.AvatarEntities.Remove(avatar);
                    }
                    await this.eFContext.SaveChangesAsync();
                    return new OASISResult<bool>
                    {
                        IsError = false,
                        Message = "Avatar Deleted Successfully",
                        Result = true
                    };
                }
                else
                {
                    return new OASISResult<bool>
                    {
                        IsError = true,
                        Message = "Something went wrong! please try again later",
                        Result = false
                    };
                }

            }
            catch (Exception ex)
            {
                return new OASISResult<bool>
                {
                    IsError = true,
                    Message = ex.ToString(),
                    Result = false
                };
            }
        }

        public async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        {
            try
            {
                var avatar = await this.eFContext.AvatarEntities.Where(p => p.Email == avatarEmail).FirstOrDefaultAsync();
                if (avatar != null)
                {
                    if (softDelete)
                    {
                        avatar.IsActive = false;
                        this.eFContext.AvatarEntities.Update(avatar);
                    }
                    else
                    {
                        this.eFContext.AvatarEntities.Remove(avatar);
                    }
                    await this.eFContext.SaveChangesAsync();
                    return new OASISResult<bool>
                    {
                        IsError = false,
                        Message = "Avatar Deleted Successfully",
                        Result = true
                    };
                }
                else
                {
                    return new OASISResult<bool>
                    {
                        IsError = true,
                        Message = "Something went wrong! please try again later",
                        Result = false
                    };
                }

            }
            catch (Exception ex)
            {
                return new OASISResult<bool>
                {
                    IsError = true,
                    Message = ex.ToString(),
                    Result = false
                };
            }
        }

        public async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            //try
            //{
            //    var avatar = await this.eFContext.AvatarEntities.Where(p => p.providerKey == providerKey).FirstOrDefaultAsync();
            //    if (avatar != null)
            //    {
            //        if (softDelete)
            //        {
            //            avatar.IsActive = false;
            //            this.eFContext.AvatarEntities.Update(avatar);
            //        }
            //        else
            //        {
            //            this.eFContext.AvatarEntities.Remove(avatar);
            //        }
            //        await this.eFContext.SaveChangesAsync();
            //        return true;
            //    }
            //    else { return false; }
            //}
            //catch (Exception)
            //{
            //    throw;
            //}
            throw new NotImplementedException();
        }

        public OASISResult<IAvatar> LoadAvatar(string username, string password, int version = 0)
        {
            try
            {
                var obj = this.eFContext.AvatarEntities.Where(p => p.Username == username && p.Password == password && p.Version == version).ToList();
                return new OASISResult<IAvatar>
                {
                    IsLoaded = true,
                    IsError = false,
                    Message = "Avatar Loaded Successfully",
                    Result = (IAvatar)obj
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar>
                {
                    IsLoaded = false,
                    IsError = true,
                    Message = ex.ToString(),
                };
            }
        }

        public OASISResult<IAvatar> LoadAvatar(string username, int version = 0)
        {
            try
            {
                var obj = this.eFContext.AvatarEntities.Where(p => p.Username == username && p.Version == version).ToList();
                return new OASISResult<IAvatar>
                {
                    IsLoaded = true,
                    IsError = false,
                    Message = "Avatar Loaded Successfully",
                    Result = (IAvatar)obj
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar>
                {
                    IsLoaded = false,
                    IsError = true,
                    Message = ex.ToString(),
                };
            }
        }

        public async Task<OASISResult<IAvatar>> LoadAvatarAsync(string username, string password, int version = 0)
        {
            try
            {
                var obj = await this.eFContext.AvatarEntities.Where(p => p.Username == username && p.Version == version).FirstOrDefaultAsync();
                if(obj == null)
                {

                    return new OASISResult<IAvatar>
                    {
                        IsLoaded = false,
                        IsError = false,
                        Message = "No Avatar Found",
                    };
                }
                else
                {

                    return new OASISResult<IAvatar>
                    {
                        IsLoaded = true,
                        IsError = false,
                        Message = "Avatar Loaded Successfully",
                        Result = (IAvatar)obj
                    };
                }
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar>
                {
                    IsLoaded = false,
                    IsError = true,
                    Message = ex.ToString(),
                };
            }
        }

        public async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            try
            {
                var obj= await this.eFContext.AvatarEntities.Where(p => p.Version == version).ToListAsync();
                return new OASISResult<IEnumerable<IAvatar>>
                {
                    IsLoaded = true,
                    IsError = false,
                    Message = "Avatar Loaded Successfully",
                    Result = (IEnumerable<IAvatar>)obj
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<IEnumerable<IAvatar>>
                {
                    IsLoaded = false,
                    IsError = true,
                    Message = ex.ToString(),
                };
            }
        }

        public OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
        {
            try
            {
                var obj= this.eFContext.AvatarEntities.Where(p => p.Version == version).ToList();
                return new OASISResult<IEnumerable<IAvatar>>
                {
                    IsLoaded = true,
                    IsError = false,
                    Message = "Avatar Loaded Successfully",
                    Result = (IEnumerable<IAvatar>)obj
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<IEnumerable<IAvatar>>
                {
                    IsLoaded = false,
                    IsError = true,
                    Message = ex.ToString(),
                };
            }
        }

        public OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
        {
            try
            {
                var obj= this.eFContext.AvatarEntities.Where(p => p.Username == avatarUsername).FirstOrDefault();
                if (obj == null)
                {
                    return new OASISResult<IAvatar>
                    {
                        IsLoaded = false,
                        IsError = false,
                        Message = "No Avatar found",
                    };
                }
                else
                {
                    return new OASISResult<IAvatar>
                    {
                        IsLoaded = true,
                        IsError = false,
                        Message = "Avatar Loaded Successfully",
                        Result = (IAvatar)obj
                    };
                }
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar>
                {
                    IsLoaded = false,
                    IsError = true,
                    Message = ex.ToString(),
                };
            }
        }

        public async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid Id, int version = 0)
        {
            try
            {
                var obj = await this.eFContext.AvatarEntities.Where(p => p.Id == Id && p.Version == version).ToListAsync();
                if (obj == null)
                {

                    return new OASISResult<IAvatar>
                    {
                        IsLoaded = true,
                        IsError = false,
                        Message = "No Avatar Found",
                    };
                }
                else
                {

                    return new OASISResult<IAvatar>
                    {
                        IsLoaded = true,
                        IsError = false,
                        Message = "Avatar Loaded Successfully",
                        Result = (IAvatar)obj
                    };
                }
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar>
                {
                    IsLoaded = false,
                    IsError = true,
                    Message = ex.ToString(),
                };
            }
        }

        public async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            try
            {
                var obj = await this.eFContext.AvatarEntities.Where(p => p.Email == avatarEmail && p.Version == version).FirstOrDefaultAsync();
                if (obj == null)
                {
                    return new OASISResult<IAvatar>
                    {
                        IsLoaded = false,
                        IsError = false,
                        Message = "No Avatar found",
                    };
                }
                else
                {
                    return new OASISResult<IAvatar>
                    {
                        IsLoaded = true,
                        IsError = false,
                        Message = "Avatar Loaded Successfully",
                        Result = (IAvatar)obj
                    };
                }
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar>
                {
                    IsLoaded = false,
                    IsError = true,
                    Message = ex.ToString(),
                };
            }
        }

        public async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            try
            {
                var obj= await this.eFContext.AvatarEntities.Where(p => p.Username == avatarUsername && p.Version == version).FirstOrDefaultAsync();
                if (obj == null)
                {
                    return new OASISResult<IAvatar>
                    {
                        IsLoaded = false,
                        IsError = false,
                        Message = "No Avatar found",
                    };
                }
                else
                {
                    return new OASISResult<IAvatar>
                    {
                        IsLoaded = true,
                        IsError = false,
                        Message = "Avatar Loaded Successfully",
                        Result = (IAvatar)obj
                    };
                }
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar>
                {
                    IsLoaded = false,
                    IsError = true,
                    Message = ex.ToString(),
                };
            }
        }

        public OASISResult<IAvatar> LoadAvatar(Guid Id, int version = 0)
        {
            try
            {
                var obj= this.eFContext.AvatarEntities.Where(p => p.Id == Id && p.Version == version).FirstOrDefault();
                if (obj == null)
                {
                    return new OASISResult<IAvatar>
                    {
                        IsLoaded = false,
                        IsError = false,
                        Message = "No Avatar found",
                    };
                }
                else
                {
                    return new OASISResult<IAvatar>
                    {
                        IsLoaded = true,
                        IsError = false,
                        Message = "Avatar Loaded Successfully",
                        Result = (IAvatar)obj
                    };
                }
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar>
                {
                    IsLoaded = false,
                    IsError = true,
                    Message = ex.ToString(),
                };
            }
        }

        public OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
        {
            try
            {
                var obj = this.eFContext.AvatarEntities.Where(p => p.Email == avatarEmail && p.Version == version).FirstOrDefault();
                if (obj == null)
                {
                    return new OASISResult<IAvatar>
                    {
                        IsLoaded = false,
                        IsError = false,
                        Message = "No Avatar found",
                    };
                }
                else
                {
                    return new OASISResult<IAvatar>
                    {
                        IsLoaded = true,
                        IsError = false,
                        Message = "Avatar Loaded Successfully",
                        Result = (IAvatar)obj
                    };
                }
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar>
                {
                    IsLoaded = false,
                    IsError = true,
                    Message = ex.ToString(),
                };
            }
        }

        public async Task<OASISResult<IAvatar>> LoadAvatarAsync(string username, int version = 0)
        {
            try
            {
                var obj = await this.eFContext.AvatarEntities.Where(p => p.Username == username && p.Version == version).FirstOrDefaultAsync();
                if (obj == null)
                {
                    return new OASISResult<IAvatar>
                    {
                        IsLoaded = false,
                        IsError = false,
                        Message = "No Avatar found",
                    };
                }
                else
                {
                    return new OASISResult<IAvatar>
                    {
                        IsLoaded = true,
                        IsError = false,
                        Message = "Avatar Loaded Successfully",
                        Result = (IAvatar)obj
                    };
                }
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar>
                {
                    IsLoaded = false,
                    IsError = true,
                    Message = ex.ToString(),
                };
            }
        }

        public async Task<OASISResult<IAvatar>> LoadAvatarForProviderKeyAsync(string providerKey, int version = 0)
        {
            //try
            //{
            //    return await this.eFContext.AvatarEntities.Where(p => p.providerKey == providerKey && p.Version == version).ToListAsync();
            //}
            //catch (Exception)
            //{
            //    throw;
            //}
            throw new NotImplementedException();
        }

        public OASISResult<IAvatar> LoadAvatarForProviderKey(string providerKey, int version = 0)
        {
            //try
            //{
            //    return this.eFContext.AvatarEntities.Where(p => p.providerKey == providerKey && p.Version == version).ToList();
            //}
            //catch (Exception)
            //{
            //    throw;
            //}
            throw new NotImplementedException();
        }

        public OASISResult<IAvatar> SaveAvatar(IAvatar avatar)
        {
            try
            {
                AvatarEntity avatarEntity = CreateAvatarModel(avatar);
                this.eFContext.AvatarEntities.Add(avatarEntity);
                this.eFContext.SaveChangesAsync();
                return new OASISResult<IAvatar>
                {
                    IsSaved = true,
                    IsError = false,
                    Message = avatarEntity.FirstName + " Record saved successfully",
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar>
                {
                    IsSaved = false,
                    IsError = true,
                    Message = ex.ToString(),
                };
            }
        }

        public async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar Avatar)
        {
            try
            {
                AvatarEntity avatarEntity = CreateAvatarModel(Avatar);
                this.eFContext.AvatarEntities.Add(avatarEntity);
                await this.eFContext.SaveChangesAsync();
                //return Avatar;
                return new OASISResult<IAvatar>
                {
                    IsSaved = true,
                    IsError = false,
                    Message = Avatar.FirstName + " Record saved successfully",
                    Result = Avatar
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar>
                {
                    IsSaved = false,
                    IsError = true,
                    Message = ex.ToString(),
                };
            }
        }

        public AvatarEntity CreateAvatarModel(IAvatar Avatar)
        {
            AvatarEntity avatar = new AvatarEntity();
            avatar.AcceptTerms = Avatar.AcceptTerms;
            //avatar.CreatedByAvatarId = Convert.ToString(Avatar.CreatedByAvatarId);
            avatar.CreatedDate = Avatar.CreatedDate;
            //avatar.DeletedByAvatarId = Convert.ToString(Avatar.DeletedByAvatarId);
            avatar.DeletedDate = Avatar.DeletedDate;
            avatar.Description = Avatar.Description == null ? "" : Avatar.Description;
            avatar.Email = Avatar.Email == null ? "" : Avatar.Email;
            avatar.FirstName = Avatar.FirstName == null ? "" : Avatar.FirstName;
            //avatar.FullName = Avatar.FullName;
            //avatar.HolonId = Avatar.HolonId;
            avatar.Id = Avatar.Id;
            avatar.IsActive = Avatar.IsActive;
            avatar.IsBeamedIn = Avatar.IsBeamedIn;
            avatar.IsChanged = Avatar.IsChanged;
            //avatar.IsVerified = Avatar.IsVerified;
            avatar.JwtToken = Avatar.JwtToken == null ? "": Avatar.JwtToken;
            avatar.LastBeamedIn = Avatar.LastBeamedIn;
            avatar.LastBeamedOut = Avatar.LastBeamedOut;
            avatar.LastName = Avatar.LastName;
            //avatar.ModifiedByAvatarId = Convert.ToString(Avatar.ModifiedByAvatarId);
            //avatar.ModifiedDate = Avatar.ModifiedDate;
            avatar.Name = Avatar.FullName;
            avatar.Password = Avatar.Password == null ? "" : Avatar.Password;
            avatar.PasswordReset = Avatar.PasswordReset;
            avatar.PreviousVersionId = Avatar.PreviousVersionId == Guid.Empty ? Guid.NewGuid() : Avatar.PreviousVersionId;
            avatar.RefreshToken = Avatar.RefreshToken == null ? "" : Avatar.RefreshToken;
            avatar.ResetToken = Avatar.ResetToken == null ? "" : Avatar.ResetToken;
            avatar.ResetTokenExpires = Avatar.ResetTokenExpires;
            avatar.Title = Avatar.Title == null ? "": Avatar.Title;
            avatar.Username = Avatar.Username == null ? "" : Avatar.Username;
            avatar.VerificationToken = Avatar.VerificationToken == null ? "": Avatar.VerificationToken;
            avatar.Verified = Avatar.Verified;
            avatar.Version = Avatar.Version;
            return avatar;
        }
    }
}
