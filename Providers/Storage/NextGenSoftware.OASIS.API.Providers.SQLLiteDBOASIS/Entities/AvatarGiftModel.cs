using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities{

    [Table("AvatarGifts")]
    public class AvatarGiftModel{

        [Required, Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id{ set; get;}
        
        public KarmaTypePositive GiftType { get; set; }
        public DateTime GiftEarnt { get; set; }
        public string KarmaSourceTitle { get; set; }
        public string KarmaSourceDesc { get; set; }
        public string WebLink { get; set; }
        public KarmaSourceType KarmaSource { get; set; }
        public ProviderType Provider { get; set; }
        
        [ForeignKey("AvatarId")]
        public AvatarDetailModel AvatarDetail { get; set; }
        public string AvatarId { get; set; }
        
        public long? AvatarChakraId { set; get; }
        public int SequenceIndex { get; set; }

        public AvatarGiftModel(){}
        public AvatarGiftModel(AvatarGift source):this((IAvatarGift)source) { }
        public AvatarGiftModel(IAvatarGift source){
            ArgumentNullException.ThrowIfNull(source);

            this.AvatarId=source.AvatarId.ToString();
            this.GiftType=source.GiftType;
            this.GiftEarnt=source.GiftEarnt;
            this.KarmaSourceTitle=source.KarmaSourceTitle;
            this.KarmaSourceDesc=source.KarmaSourceDesc;
            this.WebLink=source.WebLink;
            this.KarmaSource=source.KarmaSource.Value;
            this.Provider=source.Provider.Value;

        }

        internal static AvatarGiftModel ForAvatar(IAvatarGift source,string owner,int sequence)
        {
            ArgumentNullException.ThrowIfNull(source);
            var identity=Guid.Parse(owner);
            if(source.AvatarId!=Guid.Empty && source.AvatarId!=identity)
                throw new ArgumentException("Gift owner differs from the avatar detail.");
            if(source.KarmaSource==null || source.Provider==null
                ||!Enum.IsDefined(typeof(KarmaTypePositive),source.GiftType)
                ||!Enum.IsDefined(typeof(KarmaSourceType),source.KarmaSource.Value)
                ||!Enum.IsDefined(typeof(ProviderType),source.Provider.Value))
                throw new ArgumentException("Gift requires valid source, provider and gift type.");
            return new AvatarGiftModel(source){AvatarId=owner,SequenceIndex=sequence};
        }

        public AvatarGift GetAvatarGift(){
            AvatarGift item=new AvatarGift();

            item.AvatarId= new Guid(this.AvatarId);
            item.GiftType=this.GiftType;
            
            item.GiftEarnt=this.GiftEarnt;
            item.KarmaSourceTitle=this.KarmaSourceTitle;
            item.KarmaSourceDesc=this.KarmaSourceDesc;
            item.WebLink=this.WebLink;
            
            item.KarmaSource=new EnumValue<KarmaSourceType>(this.KarmaSource);
            item.Provider=new EnumValue<ProviderType>(this.Provider);

            return(item);
        }

    }
}
