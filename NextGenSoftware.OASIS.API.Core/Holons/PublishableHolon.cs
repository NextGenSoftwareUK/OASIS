//using System;
//using NextGenSoftware.OASIS.Common;
//using NextGenSoftware.OASIS.API.Core.Enums;
//using NextGenSoftware.OASIS.API.Core.Managers;
//using NextGenSoftware.OASIS.API.Core.Interfaces;
//using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;

//namespace NextGenSoftware.OASIS.API.Core.Holons
//{
//    public class PublishableHolon : Holon, IPublishableHolon
//    {
//        private IAvatar _publishedByAvatar = null;

//        public PublishableHolon()
//        {
//            HolonType = HolonType.Mission;
//        }

//        [CustomOASISProperty]
//        public DateTime PublishedOn { get; set; }

//        [CustomOASISProperty]
//        public Guid PublishedByAvatarId { get; set; }

//        public IAvatar PublishedByAvatar
//        {
//            get
//            {
//                if (_publishedByAvatar == null && PublishedByAvatarId != Guid.Empty)
//                {
//                    OASISResult<IAvatar> avatarResult = AvatarManager.Instance.LoadAvatar(PublishedByAvatarId);

//                    if (avatarResult != null && avatarResult.Result != null && !avatarResult.IsError)
//                        _publishedByAvatar = avatarResult.Result;
//                }

//                return _publishedByAvatar;
//            }
//        }
//    }
//}