using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Neo4jOgm.Attribute;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.DataBaseModels{

    [NeoNodeEntity("AvatarGift", "AvatarGift")]

    public class AvatarGiftModel{

        [NeoNodeId]
        public long? Id { get; set; }


        public KarmaTypePositive GiftType { get; set; }
        public DateTime GiftEarnt { get; set; }
        public string KarmaSourceTitle { get; set; }
        public string KarmaSourceDesc { get; set; }
        public string WebLink { get; set; }
        public string KarmaSource { get; set; }
        public string Provider { get; set; }

        public string AvatarId { get; set; }
        public long AvatarChakraId { set; get; } = 0;

        public AvatarGiftModel(){}
        public AvatarGiftModel(AvatarGift source){

            this.AvatarId=source.AvatarId.ToString();
            this.GiftType=source.GiftType;
            this.GiftEarnt=source.GiftEarnt;
            this.KarmaSourceTitle=source.KarmaSourceTitle;
            this.KarmaSourceDesc=source.KarmaSourceDesc;
            this.WebLink=source.WebLink;
            this.KarmaSource=source.KarmaSource.Name;
            this.Provider=source.Provider.Name;

        }

        public AvatarGift GetAvatarGift(){
            AvatarGift item=new AvatarGift();

            item.AvatarId= new Guid(this.AvatarId);
            item.GiftType=this.GiftType;
            
            item.GiftEarnt=this.GiftEarnt;
            item.KarmaSourceTitle=this.KarmaSourceTitle;
            item.KarmaSourceDesc=this.KarmaSourceDesc;
            item.WebLink=this.WebLink;
            
            KarmaSourceType karmaSource=Enum.Parse<KarmaSourceType>(this.KarmaSource);
            item.KarmaSource=new EnumValue<KarmaSourceType>(karmaSource);
            
            ProviderType provideType=Enum.Parse<ProviderType>(this.Provider);
            item.Provider=new EnumValue<ProviderType>(provideType);

            return(item);
        }

    }
}