using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Neo4jOgm.Attribute;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.DataBaseModels{

    [NeoNodeEntity("KarmaAkashicRecord", "KarmaAkashicRecord")]

    public class KarmaAkashicRecordModel{
        [NeoNodeId]
        public long? Id { get; set; }
  
        public String AvatarId { get; set; }

        public DateTime Date { get; set; }
        public int Karma { get; set; }
        public int TotalKarma { get; set; }
        public string KarmaSourceTitle { get; set; }
        public string KarmaSourceDesc { get; set; }
        public string WebLink { get; set; }
        public string KarmaSource { get; set; }
        public string KarmaEarntOrLost { get; set; }
        public string KarmaTypePositive { get; set; }
        public string KarmaTypeNegative { get; set; }
        public string Provider { get; set; } 

        public KarmaAkashicRecordModel(){}
        public KarmaAkashicRecordModel(KarmaAkashicRecord source){

            this.AvatarId=source.AvatarId.ToString();
            this.Date = source.Date;

            this.Karma = source.Karma;
            this.TotalKarma = (int)source.TotalKarma;

            this.KarmaSourceTitle=source.KarmaSourceTitle;
            this.KarmaSourceDesc=source.KarmaSourceDesc;
            this.WebLink=source.WebLink;
            this.KarmaSource=source.KarmaSource.Name;

            this.KarmaEarntOrLost = source.KarmaEarntOrLost.Name;
            this.KarmaTypePositive=source.KarmaTypePositive.Name;
            this.KarmaTypeNegative=source.KarmaTypeNegative.Name;
            this.Provider=source.Provider.Name;
        }

        public KarmaAkashicRecord GetKarmaAkashicRecord(){

            KarmaAkashicRecord item=new KarmaAkashicRecord();

            item.AvatarId=new Guid(this.AvatarId);
            item.Date = this.Date;

            item.Karma = this.Karma;
            item.TotalKarma = this.TotalKarma;

            item.KarmaSourceTitle=this.KarmaSourceTitle;
            item.KarmaSourceDesc=this.KarmaSourceDesc;
            item.WebLink=this.WebLink;

            KarmaSourceType karmaSource=(KarmaSourceType)Enum.Parse<KarmaSourceType>(this.KarmaSource);
            item.KarmaSource=new EnumValue<KarmaSourceType>(karmaSource);

            KarmaEarntOrLost karmaEarntOrLost=(KarmaEarntOrLost)Enum.Parse<KarmaEarntOrLost>(this.KarmaEarntOrLost);
            item.KarmaEarntOrLost = new EnumValue<KarmaEarntOrLost>(karmaEarntOrLost);

            KarmaTypePositive karmaTypePositive=(KarmaTypePositive)Enum.Parse<KarmaTypePositive>(this.KarmaTypePositive);
            item.KarmaTypePositive=new EnumValue<KarmaTypePositive>(karmaTypePositive);

            KarmaTypeNegative karmaTypeNegative=(KarmaTypeNegative)Enum.Parse<KarmaTypeNegative>(this.KarmaTypeNegative);
            item.KarmaTypeNegative=new EnumValue<KarmaTypeNegative>(karmaTypeNegative);

            ProviderType provideType=(ProviderType)Enum.Parse<ProviderType>(this.Provider);
            item.Provider=new EnumValue<ProviderType>(provideType);

            return(item);
        }
    }
}