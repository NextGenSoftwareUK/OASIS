//using System;
//using Microsoft.AspNetCore.Mvc;
//using NextGenSoftware.OASIS.API.Core.Enums;
//using NextGenSoftware.OASIS.API.Core.Helpers;
//using NextGenSoftware.OASIS.API.Core.Interfaces;
//using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models;

//namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers
//{
//    public class ValidationHelpers
//    {
//        public ActionResult<IAvatar> ValidateAddKarmaToAvatar(Guid avatarId, AddRemoveKarmaToAvatarRequest addKarmaToAvatarRequest)
//        {
//            object karmaTypePositiveObject = null;
//            object karmaSourceTypeObject = null;

//            if (!Enum.TryParse(typeof(KarmaTypePositive), addKarmaToAvatarRequest.KarmaType, out karmaTypePositiveObject))
//                return Ok(string.Concat("ERROR: KarmaType needs to be one of the values found in KarmaTypePositive enumeration. Possible value can be: ", EnumHelper.GetEnumValues(typeof(KarmaTypePositive))));

//            if (!Enum.TryParse(typeof(KarmaSourceType), addKarmaToAvatarRequest.karmaSourceType, out karmaSourceTypeObject))
//                return Ok(string.Concat("ERROR: KarmaSourceType needs to be one of the values found in KarmaSourceType enumeration. Possible value can be: ", EnumHelper.GetEnumValues(typeof(KarmaSourceType))));

//            return new ActionResult<IAvatar>(new ActionResult() Program.AvatarManager.AddKarmaToAvatar(avatarId, (KarmaTypePositive)karmaTypePositiveObject, (KarmaSourceType)karmaSourceTypeObject, addKarmaToAvatarRequest.KaramSourceTitle, addKarmaToAvatarRequest.KarmaSourceDesc)));
//        }
//    }
//}
