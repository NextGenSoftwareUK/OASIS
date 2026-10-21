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
        public int SequenceIndex { get; set; }
        public int DateKind { get; set; }
        public bool HasKarmaSource { get; set; } = true;
        public bool HasKarmaEarntOrLost { get; set; } = true;
        public bool HasKarmaTypePositive { get; set; } = true;
        public bool HasKarmaTypeNegative { get; set; } = true;
        public bool HasProvider { get; set; } = true;

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
        public KarmaAkashicRecordModel(NextGenSoftware.OASIS.API.Core.Interfaces.Avatar.IKarmaAkashicRecord source){

            this.AvatarId=source.AvatarId.ToString();
            this.Date = source.Date;
            this.DateKind = (int)source.Date.Kind;
            this.HasKarmaSource=source.KarmaSource!=null;
            this.HasKarmaEarntOrLost=source.KarmaEarntOrLost!=null;
            this.HasKarmaTypePositive=source.KarmaTypePositive!=null;
            this.HasKarmaTypeNegative=source.KarmaTypeNegative!=null;
            this.HasProvider=source.Provider!=null;

            this.Karma = source.Karma;
            this.TotalKarma = source.TotalKarma;

            this.KarmaSourceTitle=source.KarmaSourceTitle;
            this.KarmaSourceDesc=source.KarmaSourceDesc;
            this.WebLink=source.WebLink;
            this.KarmaSource=source.KarmaSource?.Value ?? default;

            this.KarmaEarntOrLost = source.KarmaEarntOrLost?.Value ?? default;
            this.KarmaTypePositive=source.KarmaTypePositive?.Value ?? default;
            this.KarmaTypeNegative=source.KarmaTypeNegative?.Value ?? default;
            this.Provider=source.Provider?.Value ?? default;
        }

        public KarmaAkashicRecord GetKarmaAkashicRecord(){

            KarmaAkashicRecord item=new KarmaAkashicRecord();

            item.AvatarId=new Guid(this.AvatarId);
            if(DateKind<0 || DateKind>2)throw new System.IO.InvalidDataException("Stored karma history date kind is invalid.");
            item.Date = DateTime.SpecifyKind(this.Date,(DateTimeKind)DateKind);

            item.Karma = this.Karma;
            item.TotalKarma = this.TotalKarma;

            item.KarmaSourceTitle=this.KarmaSourceTitle;
            item.KarmaSourceDesc=this.KarmaSourceDesc;
            item.WebLink=this.WebLink;

            item.KarmaSource=HasKarmaSource ? new EnumValue<KarmaSourceType>(this.KarmaSource):null;
            item.KarmaEarntOrLost = HasKarmaEarntOrLost ? new EnumValue<KarmaEarntOrLost>(this.KarmaEarntOrLost):null;
            item.KarmaTypePositive=HasKarmaTypePositive ? new EnumValue<KarmaTypePositive>(this.KarmaTypePositive):null;

            item.KarmaTypeNegative=HasKarmaTypeNegative ? new EnumValue<KarmaTypeNegative>(this.KarmaTypeNegative):null;
            item.Provider=HasProvider ? new EnumValue<ProviderType>(this.Provider):null;

            return(item);
        }
    }
}
