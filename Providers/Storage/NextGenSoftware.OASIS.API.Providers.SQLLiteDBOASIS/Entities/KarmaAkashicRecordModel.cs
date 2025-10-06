using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities{

    [Table("KarmaAkashicRecord")]
    public class KarmaAkashicRecordModel{

        [Required, Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id{ set; get; }

        public String AvatarId { get; set; }

        public DateTime Date { get; set; }
        public int Karma { get; set; }
        public long TotalKarma { get; set; }
        public string KarmaSourceTitle { get; set; }
        public string KarmaSourceDesc { get; set; }
        public string WebLink { get; set; }
        public KarmaSourceType KarmaSource { get; set; }
        public KarmaEarntOrLost KarmaEarntOrLost { get; set; }
        public KarmaTypePositive KarmaTypePositive { get; set; }
        public KarmaTypeNegative KarmaTypeNegative { get; set; }
        public ProviderType Provider { get; set; } 

        public KarmaAkashicRecordModel(){}
        public KarmaAkashicRecordModel(KarmaAkashicRecord source){

            this.AvatarId=source.AvatarId.ToString();
            this.Date = source.Date;

            this.Karma = source.Karma;
            this.TotalKarma = source.TotalKarma;

            this.KarmaSourceTitle=source.KarmaSourceTitle;
            this.KarmaSourceDesc=source.KarmaSourceDesc;
            this.WebLink=source.WebLink;
            this.KarmaSource=source.KarmaSource.Value;

            this.KarmaEarntOrLost = source.KarmaEarntOrLost.Value;
            this.KarmaTypePositive=source.KarmaTypePositive.Value;
            this.KarmaTypeNegative=source.KarmaTypeNegative.Value;
            this.Provider=source.Provider.Value;
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

            item.KarmaSource=new EnumValue<KarmaSourceType>(this.KarmaSource);
            item.KarmaEarntOrLost = new EnumValue<KarmaEarntOrLost>(this.KarmaEarntOrLost);
            item.KarmaTypePositive=new EnumValue<KarmaTypePositive>(this.KarmaTypePositive);

            item.KarmaTypeNegative=new EnumValue<KarmaTypeNegative>(this.KarmaTypeNegative);
            item.Provider=new EnumValue<ProviderType>(this.Provider);

            return(item);
        }
    }
}