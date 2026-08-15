using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models;
using NextGenSoftware.OASIS.API.Core.Objects.Avatar;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Avatar;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Security;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    public partial class AvatarProfileController
    {
        /// <summary>
        /// Search avatars for the given search term. Coming soon...
        /// </summary>
        /// <param name="searchParams"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("search")]
        public async Task<OASISHttpResponseMessage<ISearchResults>> SearchAvatar(SearchParams searchParams)
        {
            return HttpResponseHelper.FormatResponse(await SearchManager.Instance.SearchAsync(searchParams));
        }

        /// <summary>
        /// Search avatars for the given search term. Coming soon...
        /// Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="searchParams"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("search/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<ISearchResults>> SearchAvatar(SearchParams searchParams, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await SearchAvatar(searchParams);
        }

        /// <summary>
        ///     Add positive karma to the given avatar. karmaType = The type of positive karma, karmaSourceType = Where the karma
        ///     was earnt (App, dApp, hApp, Website, Game, karmaSourceTitle/karamSourceDesc = The name/desc of the app/website/game
        ///     where the karma was earnt. 
        ///     Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// </summary>
        /// <param name="avatarId">The avatar ID to add the karma to.</param>
        /// <param name="karmaType">The type of positive karma.</param>
        /// <param name="karmaSourceType">Where the karma was earnt (App, dApp, hApp, Website, Game.</param>
        /// <param name="karmaSourceTitle">The name of the app/website/game where the karma was earnt.</param>
        /// <param name="karmaSourceDesc">The description of the app/website/game where the karma was earnt.</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("add-karma-to-avatar/{avatarId}")]
        public async Task<OASISHttpResponseMessage<KarmaAkashicRecord>> AddKarmaToAvatar(Guid avatarId,
            AddRemoveKarmaToAvatarRequest addKarmaToAvatarRequest)
        {
            try
            {
                var result = await AvatarManager.AddKarmaToAvatarAsync(
                    avatarId, 
                    (KarmaTypePositive)Enum.Parse(typeof(KarmaTypePositive), addKarmaToAvatarRequest.KarmaType), 
                    (KarmaSourceType)Enum.Parse(typeof(KarmaSourceType), addKarmaToAvatarRequest.karmaSourceType), 
                    addKarmaToAvatarRequest.KaramSourceTitle, 
                    addKarmaToAvatarRequest.KarmaSourceDesc, 
                    null); // KarmaSourceWebLink not available in request
                return HttpResponseHelper.FormatResponse(new OASISResult<KarmaAkashicRecord> { Result = result });
            }
            catch (Exception ex)
            {
                return HttpResponseHelper.FormatResponse(new OASISResult<KarmaAkashicRecord> { IsError = true, Message = ex.Message, Exception = ex });
            }
        }

        /// <summary>
        ///     Add positive karma to the given avatar. karmaType = The type of positive karma, karmaSourceType = Where the karma
        ///     was earnt (App, dApp, hApp, Website, Game, karmaSourceTitle/karamSourceDesc = The name/desc of the app/website/game
        ///     where the karma was earnt.
        ///     Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        ///     Pass in the provider you wish to use.Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="avatarId">The avatar ID to add the karma to.</param>
        /// <param name="karmaType">The type of positive karma.</param>
        /// <param name="karmaSourceType">Where the karma was earnt (App, dApp, hApp, Website, Game.</param>
        /// <param name="karmaSourceTitle">The name of the app/website/game where the karma was earnt.</param>
        /// <param name="karmaSourceDesc">The description of the app/website/game where the karma was earnt.</param>
        /// <param name="providerType">Pass in the provider you wish to use.</param>
        /// <param name="setGlobally">
        ///     Set this to false for this provider to be used only for this request or true for it to be
        ///     used for all future requests too.
        /// </param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("add-karma-to-avatar/{avatarId}/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<KarmaAkashicRecord>> AddKarmaToAvatar(
            AddRemoveKarmaToAvatarRequest addKarmaToAvatarRequest, Guid avatarId, ProviderType providerType,
            bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await AddKarmaToAvatar(avatarId, addKarmaToAvatarRequest);
        }

        /// <summary>
        ///     Remove karma from the given avatar. karmaType = The type of negative karma, karmaSourceType = Where the karma was lost (App, dApp, hApp, Website, Game,
        ///     karmaSourceTitle/karamSourceDesc = The name/desc of the app/website/game where the karma was lost.
        ///     Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// </summary>
        /// <param name="avatarId">The avatar ID to remove the karma from.</param>
        /// <param name="karmaType">The type of negative karma.</param>
        /// <param name="karmaSourceType">Where the karma was lost (App, dApp, hApp, Website, Game.</param>
        /// <param name="karmaSourceTitle">The name of the app/website/game where the karma was lost.</param>
        /// <param name="karmaSourceDesc">The description of the app/website/game where the karma was lost.</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("remove-karma-from-avatar/{avatarId}")]
        public async Task<OASISHttpResponseMessage<KarmaAkashicRecord>> RemoveKarmaFromAvatar(Guid avatarId,
            AddRemoveKarmaToAvatarRequest addKarmaToAvatarRequest)
        {
            try
            {
                var result = await AvatarManager.RemoveKarmaFromAvatarAsync(
                    avatarId, 
                    (KarmaTypeNegative)Enum.Parse(typeof(KarmaTypeNegative), addKarmaToAvatarRequest.KarmaType), 
                    (KarmaSourceType)Enum.Parse(typeof(KarmaSourceType), addKarmaToAvatarRequest.karmaSourceType), 
                    addKarmaToAvatarRequest.KaramSourceTitle, 
                    addKarmaToAvatarRequest.KarmaSourceDesc, 
                    null); // KarmaSourceWebLink not available in request
                return HttpResponseHelper.FormatResponse(new OASISResult<KarmaAkashicRecord> { Result = result });
            }
            catch (Exception ex)
            {
                return HttpResponseHelper.FormatResponse(new OASISResult<KarmaAkashicRecord> { IsError = true, Message = ex.Message, Exception = ex });
            }
        }

        /// <summary>
        ///     Remove karma from the given avatar. karmaType = The type of negative karma, karmaSourceType = Where the karma was lost (App, dApp, hApp, Website, Game,
        ///     karmaSourceTitle/karamSourceDesc = The name/desc of the app/website/game where the karma was lost. 
        ///     Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        ///     Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or
        ///     true for it to be used for all future requests too.
        /// </summary>
        /// <param name="avatarId">The avatar ID to remove the karma from.</param>
        /// <param name="karmaType">The type of negative karma.</param>
        /// <param name="karmaSourceType">Where the karma was lost (App, dApp, hApp, Website, Game.</param>
        /// <param name="karmaSourceTitle">The name of the app/website/game where the karma was lost.</param>
        /// <param name="karmaSourceDesc">The description of the app/website/game where the karma was lost.</param>
        /// <param name="providerType">Pass in the provider you wish to use.</param>
        /// <param name="setGlobally">
        ///     Set this to false for this provider to be used only for this request or true for it to be
        ///     used for all future requests too.
        /// </param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("remove-karma-from-avatar/{avatarId}/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<KarmaAkashicRecord>> RemoveKarmaFromAvatar(
            AddRemoveKarmaToAvatarRequest addKarmaToAvatarRequest, Guid avatarId, ProviderType providerType,
            bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await RemoveKarmaFromAvatar(avatarId, addKarmaToAvatarRequest);
        }

        /// <summary>
        ///     Update the given avatar using their id.
        ///     Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// </summary>
        /// <param name="avatar"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("update-by-id/{id}")]
        public async Task<OASISHttpResponseMessage<IAvatar>> Update(UpdateRequest avatar, Guid id)
        {
            // users can update their own account and admins can update any account
            if (id != Avatar.Id && Avatar.AvatarType.Value != AvatarType.Wizard)
                return HttpResponseHelper.FormatResponse(new OASISResult<IAvatar>() { Result = null, IsError = true, Message = "Unauthorized" }, HttpStatusCode.Unauthorized);

            // Load existing avatar and update with new data
            var existingAvatarResult = await Program.AvatarManager.LoadAvatarAsync(id);
            if (existingAvatarResult.IsError || existingAvatarResult.Result == null)
                return HttpResponseHelper.FormatResponse(existingAvatarResult, HttpStatusCode.NotFound);

            var existingAvatar = existingAvatarResult.Result;
            
            // Enforce uniqueness before applying username/email changes
            if (!string.IsNullOrEmpty(avatar.Username) && !string.Equals(avatar.Username, existingAvatar.Username, StringComparison.OrdinalIgnoreCase))
            {
                var usernameCheck = Program.AvatarManager.CheckIfUsernameIsAlreadyInUse(avatar.Username);
                if (usernameCheck.Result)
                    return HttpResponseHelper.FormatResponse(new OASISResult<IAvatar>() { IsError = true, Message = $"Username '{avatar.Username}' is already taken." }, HttpStatusCode.Conflict);
            }
            if (!string.IsNullOrEmpty(avatar.Email) && !string.Equals(avatar.Email, existingAvatar.Email, StringComparison.OrdinalIgnoreCase))
            {
                var emailCheck = Program.AvatarManager.CheckIfEmailIsAlreadyInUse(avatar.Email, false);
                if (emailCheck.Result)
                    return HttpResponseHelper.FormatResponse(new OASISResult<IAvatar>() { IsError = true, Message = $"Email '{avatar.Email}' is already registered to another account." }, HttpStatusCode.Conflict);
            }

            // Update avatar properties from UpdateRequest
            if (!string.IsNullOrEmpty(avatar.Title)) existingAvatar.Title = avatar.Title;
            if (!string.IsNullOrEmpty(avatar.FirstName)) existingAvatar.FirstName = avatar.FirstName;
            if (!string.IsNullOrEmpty(avatar.LastName)) existingAvatar.LastName = avatar.LastName;
            if (!string.IsNullOrEmpty(avatar.Username)) existingAvatar.Username = avatar.Username;
            if (!string.IsNullOrEmpty(avatar.Email)) existingAvatar.Email = avatar.Email;
            if (!string.IsNullOrEmpty(avatar.Password)) existingAvatar.Password = avatar.Password;
            if (!string.IsNullOrEmpty(avatar.AvatarType) && Avatar.AvatarType.Value == AvatarType.Wizard)
            {
                if (Enum.TryParse<AvatarType>(avatar.AvatarType, out var avatarType))
                    existingAvatar.AvatarType = new EnumValue<AvatarType>(avatarType);
            }

            // Use AvatarManager for business logic
            return HttpResponseHelper.FormatResponse(await Program.AvatarManager.SaveAvatarAsync(existingAvatar));
        }

        /// <summary>
        ///     Update the given avatar using their id.
        ///     Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        ///     Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for
        ///     it to be used for all future requests too.
        /// </summary>
        /// <param name="id">The id of the avatar.</param>
        /// <param name="avatar">The avatar to update.</param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("update-by-id/{id}/{providerType}/{setGlobally}")]
        //public ActionResult<IAvatar> Update(Guid id, Core.Avatar avatar, ProviderType providerType, bool setGlobally = false)
        public async Task<OASISHttpResponseMessage<IAvatar>> Update(Guid id, UpdateRequest avatar, ProviderType providerType,
            bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await Update(avatar, id);
        }

        /// <summary>
        /// Update the given avatar using their email address.
        /// Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// </summary>
        /// <param name="avatar"></param>
        /// <param name="email"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("update-by-email/{email}")]
        public async Task<OASISHttpResponseMessage<IAvatar>> UpdateByEmail(UpdateRequest avatar, string email)
        {
            // users can update their own account and admins can update any account
            if (email != Avatar.Email && Avatar.AvatarType.Value != AvatarType.Wizard)
                return HttpResponseHelper.FormatResponse(new OASISResult<IAvatar>() { Result = null, IsError = true, Message = "Unauthorized" }, HttpStatusCode.Unauthorized);

            // Load existing avatar by email and update with new data
            var existingAvatarResult = await Program.AvatarManager.LoadAvatarByEmailAsync(email);
            if (existingAvatarResult.IsError || existingAvatarResult.Result == null)
                return HttpResponseHelper.FormatResponse(existingAvatarResult, HttpStatusCode.NotFound);

            var existingAvatar = existingAvatarResult.Result;
            
            // Enforce uniqueness before applying username/email changes
            if (!string.IsNullOrEmpty(avatar.Username) && !string.Equals(avatar.Username, existingAvatar.Username, StringComparison.OrdinalIgnoreCase))
            {
                var usernameCheck = Program.AvatarManager.CheckIfUsernameIsAlreadyInUse(avatar.Username);
                if (usernameCheck.Result)
                    return HttpResponseHelper.FormatResponse(new OASISResult<IAvatar>() { IsError = true, Message = $"Username '{avatar.Username}' is already taken." }, HttpStatusCode.Conflict);
            }
            if (!string.IsNullOrEmpty(avatar.Email) && !string.Equals(avatar.Email, existingAvatar.Email, StringComparison.OrdinalIgnoreCase))
            {
                var emailCheck = Program.AvatarManager.CheckIfEmailIsAlreadyInUse(avatar.Email, false);
                if (emailCheck.Result)
                    return HttpResponseHelper.FormatResponse(new OASISResult<IAvatar>() { IsError = true, Message = $"Email '{avatar.Email}' is already registered to another account." }, HttpStatusCode.Conflict);
            }

            // Update avatar properties from UpdateRequest
            if (!string.IsNullOrEmpty(avatar.Title)) existingAvatar.Title = avatar.Title;
            if (!string.IsNullOrEmpty(avatar.FirstName)) existingAvatar.FirstName = avatar.FirstName;
            if (!string.IsNullOrEmpty(avatar.LastName)) existingAvatar.LastName = avatar.LastName;
            if (!string.IsNullOrEmpty(avatar.Username)) existingAvatar.Username = avatar.Username;
            if (!string.IsNullOrEmpty(avatar.Email)) existingAvatar.Email = avatar.Email;
            if (!string.IsNullOrEmpty(avatar.Password)) existingAvatar.Password = avatar.Password;
            if (!string.IsNullOrEmpty(avatar.AvatarType) && Avatar.AvatarType.Value == AvatarType.Wizard)
            {
                if (Enum.TryParse<AvatarType>(avatar.AvatarType, out var avatarType))
                    existingAvatar.AvatarType = new EnumValue<AvatarType>(avatarType);
            }

            // Use AvatarManager for business logic
            return HttpResponseHelper.FormatResponse(await Program.AvatarManager.SaveAvatarAsync(existingAvatar));
        }

        /// <summary>
        /// Update the given avatar using their email address.
        /// Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="avatar"></param>
        /// <param name="email"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("update-by-email/{email}/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<IAvatar>> UpdateByEmail(UpdateRequest avatar, string email, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await UpdateByEmail(avatar, email);
        }

        /// <summary>
        /// Update the given avatar using their username.
        /// Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// </summary>
        /// <param name="avatar"></param>
        /// <param name="username"></param>
        [Authorize]
        [HttpPost("update-by-username/{username}")]
        public async Task<OASISHttpResponseMessage<IAvatar>> UpdateByUsername(UpdateRequest avatar, string username)
        {
            // users can update their own account and admins can update any account
            if (username != Avatar.Username && Avatar.AvatarType.Value != AvatarType.Wizard)
                return HttpResponseHelper.FormatResponse(new OASISResult<IAvatar>() { Result = null, IsError = true, Message = "Unauthorized" }, HttpStatusCode.Unauthorized);

            // Load existing avatar by username and update with new data
            var existingAvatarResult = await Program.AvatarManager.LoadAvatarAsync(username);
            if (existingAvatarResult.IsError || existingAvatarResult.Result == null)
                return HttpResponseHelper.FormatResponse(existingAvatarResult, HttpStatusCode.NotFound);

            var existingAvatar = existingAvatarResult.Result;
            
            // Enforce uniqueness before applying username/email changes
            if (!string.IsNullOrEmpty(avatar.Username) && !string.Equals(avatar.Username, existingAvatar.Username, StringComparison.OrdinalIgnoreCase))
            {
                var usernameCheck = Program.AvatarManager.CheckIfUsernameIsAlreadyInUse(avatar.Username);
                if (usernameCheck.Result)
                    return HttpResponseHelper.FormatResponse(new OASISResult<IAvatar>() { IsError = true, Message = $"Username '{avatar.Username}' is already taken." }, HttpStatusCode.Conflict);
            }
            if (!string.IsNullOrEmpty(avatar.Email) && !string.Equals(avatar.Email, existingAvatar.Email, StringComparison.OrdinalIgnoreCase))
            {
                var emailCheck = Program.AvatarManager.CheckIfEmailIsAlreadyInUse(avatar.Email, false);
                if (emailCheck.Result)
                    return HttpResponseHelper.FormatResponse(new OASISResult<IAvatar>() { IsError = true, Message = $"Email '{avatar.Email}' is already registered to another account." }, HttpStatusCode.Conflict);
            }

            // Update avatar properties from UpdateRequest
            if (!string.IsNullOrEmpty(avatar.Title)) existingAvatar.Title = avatar.Title;
            if (!string.IsNullOrEmpty(avatar.FirstName)) existingAvatar.FirstName = avatar.FirstName;
            if (!string.IsNullOrEmpty(avatar.LastName)) existingAvatar.LastName = avatar.LastName;
            if (!string.IsNullOrEmpty(avatar.Username)) existingAvatar.Username = avatar.Username;
            if (!string.IsNullOrEmpty(avatar.Email)) existingAvatar.Email = avatar.Email;
            if (!string.IsNullOrEmpty(avatar.Password)) existingAvatar.Password = avatar.Password;
            if (!string.IsNullOrEmpty(avatar.AvatarType) && Avatar.AvatarType.Value == AvatarType.Wizard)
            {
                if (Enum.TryParse<AvatarType>(avatar.AvatarType, out var avatarType))
                    existingAvatar.AvatarType = new EnumValue<AvatarType>(avatarType);
            }

            // Use AvatarManager for business logic
            return HttpResponseHelper.FormatResponse(await Program.AvatarManager.SaveAvatarAsync(existingAvatar));
        }

        /// <summary>
        /// Update the given avatar using their username.
        /// Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="avatar"></param>
        /// <param name="username"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        [Authorize]
        [HttpPost("update-by-username/{username}/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<IAvatar>> UpdateByUsername(UpdateRequest avatar, string username, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await UpdateByUsername(avatar, username);
        }

        /// <summary>
        ///     Update the given avatar detail with their avatar id.
        ///     Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// </summary>
        /// <param name="avatarDetail"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("update-avatar-detail-by-id/{id}")]
        public async Task<OASISHttpResponseMessage<IAvatarDetail>> UpdateAvatarDetail(AvatarDetail avatarDetail, Guid id)
        {
            // users can update their own account and admins can update any account
            if (id != Avatar.Id && Avatar.AvatarType.Value != AvatarType.Wizard)
                return HttpResponseHelper.FormatResponse(new OASISResult<IAvatarDetail>() { Result = null, IsError = true, Message = "Unauthorized" }, HttpStatusCode.Unauthorized);

            return HttpResponseHelper.FormatResponse(await Program.AvatarManager.UpdateAvatarDetailAsync(id, avatarDetail));
        }

        /// <summary>
        ///     Update the given avatar detail by the avatar's id. 
        ///     Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        ///     Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="id">The id of the avatar.</param>
        /// <param name="avatarDetail">The avatar detail to update.</param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("update-avatar-detail-by-id/{id}/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<IAvatarDetail>> UpdateAvatarDetail(Guid id, AvatarDetail avatarDetail, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await UpdateAvatarDetail(avatarDetail, id);
        }

        /// <summary>
        ///     Update the given avatar detail with their avatar email address. 
        ///     Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// </summary>
        /// <param name="avatarDetail"></param>
        /// <param name="email"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("update-avatar-detail-by-email/{email}")]
        public async Task<OASISHttpResponseMessage<IAvatarDetail>> UpdateAvatarDetailByEmail(AvatarDetail avatarDetail, string email)
        {
            // users can update their own account and admins can update any account
            if (email != Avatar.Email && Avatar.AvatarType.Value != AvatarType.Wizard)
                return HttpResponseHelper.FormatResponse(new OASISResult<IAvatarDetail>() { Result = null, IsError = true, Message = "Unauthorized" }, HttpStatusCode.Unauthorized);

            return HttpResponseHelper.FormatResponse(await Program.AvatarManager.UpdateAvatarDetailByEmailAsync(email, avatarDetail));
        }

        /// <summary>
        ///     Update the given avatar detail with their avatar email address. 
        ///     Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        ///     Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="avatarDetail"></param>
        /// <param name="email"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("update-avatar-detail-by-email/{email}/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<IAvatarDetail>> UpdateAvatarDetailByEmail(AvatarDetail avatarDetail, string email, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await UpdateAvatarDetailByEmail(avatarDetail, email);
        }

        /// <summary>
        ///     Update the given avatar detail with their avatar username. 
        ///     Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// </summary>
        /// <param name="avatarDetail"></param>
        /// <param name="username"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("update-avatar-detail-by-username/{username}")]
        public async Task<OASISHttpResponseMessage<IAvatarDetail>> UpdateAvatarDetailByUsername(AvatarDetail avatarDetail, string username)
        {
            // users can update their own account and admins can update any account
            if (username != Avatar.Username && Avatar.AvatarType.Value != AvatarType.Wizard)
                return HttpResponseHelper.FormatResponse(new OASISResult<IAvatarDetail>() { Result = null, IsError = true, Message = "Unauthorized" }, HttpStatusCode.Unauthorized);

            return HttpResponseHelper.FormatResponse(await Program.AvatarManager.UpdateAvatarDetailByUsernameAsync(username, avatarDetail));
        }

        /// <summary>
        ///     Update the given avatar detail with their avatar username. 
        ///     Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        ///     Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="avatarDetail"></param>
        /// <param name="username"></param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("update-avatar-detail-by-username/{username}/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<IAvatarDetail>> UpdateAvatarDetailByUsername(AvatarDetail avatarDetail, string username, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await UpdateAvatarDetailByUsername(avatarDetail, username);
        }

        /// <summary>
        ///     Delete the given avatar using their id.
        ///     Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// </summary>
        /// <param name="id">The id of the avatar.</param>
        /// <returns></returns>
        [Authorize]
        [HttpDelete("{id:Guid}")]
        public async Task<OASISHttpResponseMessage<bool>> Delete(Guid id)
        {
            // users can delete their own account and admins can delete any account
            if (id != Avatar.Id && Avatar.AvatarType.Value != AvatarType.Wizard)
                return HttpResponseHelper.FormatResponse(new OASISResult<bool>() { IsError = true, Message = "Unauthorized" }, HttpStatusCode.Unauthorized);

            return HttpResponseHelper.FormatResponse(await Program.AvatarManager.DeleteAvatarAsync(id));
        }

        /// <summary>
        ///     Delete the given avatar using their id.
        ///     Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        ///     Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="id">The id of the avatar.</param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize]
        [HttpDelete("{id:Guid}/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<bool>> Delete(Guid id, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await Delete(id);
        }

        /// <summary>
        ///     Delete the given avatar using their username.
        ///     Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// </summary>
        /// <param name="username">The id of the avatar.</param>
        /// <returns></returns>
        [Authorize]
        [HttpDelete("delete-by-username/{username}")]
        public async Task<OASISHttpResponseMessage<bool>> DeleteByUsername(string username)
        {
            // users can delete their own account and admins can delete any account
            if (username != Avatar.Username && Avatar.AvatarType.Value != AvatarType.Wizard)
                return HttpResponseHelper.FormatResponse(new OASISResult<bool>() { IsError = true, Message = "Unauthorized" }, HttpStatusCode.Unauthorized);

            return HttpResponseHelper.FormatResponse(await Program.AvatarManager.DeleteAvatarByUsernameAsync(username));
        }

        /// <summary>
        ///     Delete the given avatar using their username.
        ///     Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        ///     Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="username">The id of the avatar.</param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize]
        [HttpDelete("delete-by-username/{username}/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<bool>> DeleteByUsername(string username, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await DeleteByUsername(username);
        }

        /// <summary>
        ///     Delete the given avatar using their email.
        ///     Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        /// </summary>
        /// <param name="email">The id of the avatar.</param>
        /// <returns></returns>
        [Authorize]
        [HttpDelete("delete-by-email/{email}")]
        public async Task<OASISHttpResponseMessage<bool>> DeleteByEmail(string email)
        {
            // users can delete their own account and admins can delete any account
            if (email != Avatar.Email && Avatar.AvatarType.Value != AvatarType.Wizard)
                return HttpResponseHelper.FormatResponse(new OASISResult<bool>() { IsError = true, Message = "Unauthorized" }, HttpStatusCode.Unauthorized);

            return HttpResponseHelper.FormatResponse(await Program.AvatarManager.DeleteAvatarByEmailAsync(email));
        }

        /// <summary>
        ///     Delete the given avatar using their email.
        ///     Only works for logged in users. Use Authenticate endpoint first to obtain a JWT Token.
        ///     Pass in the provider you wish to use. Set the setglobally flag to false for this provider to be used only for this request or true for it to be used for all future requests too.
        /// </summary>
        /// <param name="email">The id of the avatar.</param>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize]
        [HttpDelete("delete-by-email/{email}/{providerType}/{setGlobally}")]
        public async Task<OASISHttpResponseMessage<bool>> DeleteByEmail(string email, ProviderType providerType, bool setGlobally = false)
        {
            await GetAndActivateProviderAsync(providerType, setGlobally);
            return await DeleteByUsername(email);
        }

    }
}
