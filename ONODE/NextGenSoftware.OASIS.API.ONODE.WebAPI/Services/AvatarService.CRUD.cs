using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Avatar;
using NextGenSoftware.OASIS.API.Core.Objects.Avatar;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Security;
using BC = BCrypt.Net.BCrypt;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Interfaces;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services
{
    public partial class AvatarService
    {
        /*
        public async Task<OASISResult<IAvatar>> Create(CreateRequest model)
        {
            var result = new OASISResult<IAvatar>();
            //TODO: PERFORMANCE} Implement in Providers so more efficient and do not need to return whole list!

            OASISResult<IEnumerable<IAvatar>> avatarsResult = await AvatarManager.LoadAllAvatarsAsync();

            if (!avatarsResult.IsError && avatarsResult.Result != null)
            {
                if (avatarsResult.Result.Any(x => x.Email == model.Email))
                    OASISErrorHandling.HandleError(ref result, $"Email '{model.Email}' is already registered");
                else
                {
                    // map model to new account object
                    var avatar = _mapper.Map<IAvatar>(model);
                    avatar.CreatedDate = DateTime.UtcNow;
                    avatar.Verified = DateTime.UtcNow;

                    // hash password
                    avatar.Password = BC.HashPassword(model.Password);
                    var saveAvatarResult = await AvatarManager.SaveAvatarAsync(avatar);

                    if (saveAvatarResult.IsError || saveAvatarResult.Result == null)
                        OASISErrorHandling.HandleError(ref result, $"Error occured in Create method on AvatarService saving the avatar. Reason: {saveAvatarResult.Message}", saveAvatarResult.DetailedMessage);
                    else
                    {
                        result.Result = AvatarManager.HideAuthDetails(avatar);
                        result.Message = "Avatar Created Successfully";
                    }
                }
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in Create method on AvatarService loading all avatars. Reason: {avatarsResult.Message}", avatarsResult.DetailedMessage);

            return result;
        }
        */

        // MIGRATED — AvatarController handles Update/UpdateByEmail/UpdateByUsername directly via AvatarManager
        /*
        public async Task<OASISResult<IAvatar>> Update(Guid id, UpdateRequest avatar)
        {
            var response = new OASISResult<IAvatar>();
            string errorMessage = "Error in Update method in Avatar Service. Reason: ";

            try
            {
                response = await AvatarManager.LoadAvatarAsync(id, false, false);

                if (response.IsError || response.Result == null)
                    OASISErrorHandling.HandleError(ref response, $"{errorMessage}{response.Message}", response.DetailedMessage);
                else
                    response = await Update(response.Result, avatar);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref response, $"{errorMessage}Unknown Error Occured. See DetailedMessage for more info.", ex.Message, ex);
            }

            return response;
        }

        public async Task<OASISResult<IAvatar>> UpdateByEmail(string email, UpdateRequest avatar)
        {
            var response = new OASISResult<IAvatar>();
            string errorMessage = "Error in UpdateByEmail method in Avatar Service. Reason: ";

            try
            {
                response = await AvatarManager.LoadAvatarByEmailAsync(email, false, false);

                if (response.IsError || response.Result == null)
                    OASISErrorHandling.HandleError(ref response, $"{errorMessage}{response.Message}", response.DetailedMessage);
                else
                    response = await Update(response.Result, avatar);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref response, $"{errorMessage}Unknown Error Occured. See DetailedMessage for more info.", ex.Message, ex);
            }

            return response;
        }

        public async Task<OASISResult<IAvatar>> UpdateByUsername(string username, UpdateRequest avatar)
        {
            var response = new OASISResult<IAvatar>();
            string errorMessage = "Error in UpdateByUsername method in Avatar Service. Reason: ";

            try
            {
                response = await AvatarManager.LoadAvatarAsync(username, false, false);

                if (response.IsError || response.Result == null)
                    OASISErrorHandling.HandleError(ref response, $"{errorMessage}{response.Message}", response.DetailedMessage);
                else
                    response = await Update(response.Result, avatar);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref response, $"{errorMessage}Unknown Error Occured. See DetailedMessage for more info.", ex.Message, ex);
            }

            return response;
        }
        */

        /*
        public async Task<OASISResult<bool>> Delete(Guid id)
        {
            var response = new OASISResult<bool>();
            try
            {
                // Default to soft delete.
                response = await AvatarManager.DeleteAvatarAsync(id);
            }
            catch (Exception e)
            {
                response.IsError = true;
                response.IsSaved = false;
                response.Message = e.Message;
                OASISErrorHandling.HandleError(ref response, e.Message);
            }

            return response;
        }

        public async Task<OASISResult<bool>> DeleteByUsername(string username)
        {
            var response = new OASISResult<bool>();
            try
            {
                // Default to soft delete.
                response = await AvatarManager.DeleteAvatarByUsernameAsync(username);
            }
            catch (Exception e)
            {
                response.IsError = true;
                response.IsSaved = false;
                response.Message = e.Message;
                OASISErrorHandling.HandleError(ref response, e.Message);
            }

            return response;
        }

        public async Task<OASISResult<bool>> DeleteByEmail(string email)
        {
            var response = new OASISResult<bool>();
            try
            {
                // Default to soft delete.
                response = await AvatarManager.DeleteAvatarByEmailAsync(email);
            }
            catch (Exception e)
            {
                response.IsError = true;
                response.IsSaved = false;
                response.Message = e.Message;
                OASISErrorHandling.HandleError(ref response, e.Message);
            }

            return response;
        }*/

        // MIGRATED — AvatarManager.ValidateAccountToken — AvatarController calls AvatarManager.ValidateAccountToken directly
        /*
        public async Task<OASISResult<string>> ValidateAccountToken(string accountToken)
        {
            return await Task.Run(() =>
            {
                var response = new OASISResult<string>();
                try
                {
                    var key = Encoding.ASCII.GetBytes(OASISBootLoader.OASISBootLoader.OASISDNA.OASIS.Security.SecretKey);
                    var tokenHandler = new JwtSecurityTokenHandler();
                    tokenHandler.ValidateToken(accountToken, new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ValidateIssuer = true,
                        ValidIssuer = "OASIS",
                        ValidateAudience = true,
                        ValidAudience = "OASIS",
                        ClockSkew = TimeSpan.Zero
                    }, out _);
                    response.IsError = false;
                    response.Result = "Token is Valid!";
                }
                catch (Exception e)
                {
                    response.IsError = true;
                    response.Exception = e;
                    response.Message = e.Message;
                    response.Result = "Token Validating Failed: Invalid Token";
                    OASISErrorHandling.HandleError(ref response, e.Message);
                }

                return response;
            });
        }
        */

        //public async Task<OASISResult<IAvatarDetail>> GetAvatarDetail(Guid id)
        //{
        //    var result = new OASISResult<IAvatarDetail>();
        //    var avatar = await AvatarManager.LoadAvatarDetailAsync(id);

        //    if (avatar != null) return result;
        //    result.Message = "AvatarDetail not found";
        //    result.IsError = true;
        //    OASISErrorHandling.HandleError(ref result, result.Message);
        //    return result;
        //}

        //public async Task<OASISResult<IAvatarDetail>> GetAvatarDetailByUsername(string username)
        //{
        //    var response = new OASISResult<IAvatarDetail>();
        //    try
        //    {
        //        var entity = await AvatarManager.LoadAvatarDetailByUsernameAsync(username);
        //        response.Result = entity;
        //    }
        //    catch (Exception e)
        //    {
        //        response.IsError = true;
        //        response.Exception = e;
        //        response.Message = e.Message;
        //        OASISErrorHandling.HandleError(ref response, response.Message);
        //    }

        //    return response;
        //}

        //public async Task<OASISResult<IAvatarDetail>> GetAvatarDetailByEmail(string email)
        //{
        //    var response = new OASISResult<IAvatarDetail>();
        //    try
        //    {
        //        var entity = await AvatarManager.LoadAvatarDetailByEmailAsync(email);
        //        response.Result = entity;
        //    }
        //    catch (Exception e)
        //    {
        //        response.IsError = true;
        //        response.Exception = e;
        //        response.Message = e.Message;
        //        OASISErrorHandling.HandleError(ref response, e.Message);
        //    }

        //    return response;
        //}

    }
}
