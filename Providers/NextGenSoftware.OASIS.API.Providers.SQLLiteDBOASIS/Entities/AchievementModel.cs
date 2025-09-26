using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities{

    [Table("AvatarAchievements")]
    public class AchievementModel{

        [Required, Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id{ set; get; }

        public string AvatarId{ set; get; }

        public string Name { get; set; }
        public string Description { get; set; }
        public KarmaTypePositive AchievementType { get; set; }
        public DateTime AchievementEarnt { get; set; }
        public string KarmaSourceTitle { get; set; } //Name of the app/website/game etc.
        public string KarmaSourceDesc { get; set; }
        public string WebLink { get; set; }
        public KarmaSourceType KarmaSource { get; set; } //App, dApp, hApp, Website or Game.
        public ProviderType Provider { get; set; }

        public AchievementModel(){}
        public AchievementModel(Achievement source){

            this.AvatarId=source.AvatarId.ToString();
            this.Name = source.Name;
            this.Description = source.Description;

            this.AchievementType = source.AchievementType;
            this.AchievementEarnt = source.AchievementEarnt;

            this.KarmaSourceTitle=source.KarmaSourceTitle;
            this.KarmaSourceDesc=source.KarmaSourceDesc;
            this.WebLink=source.WebLink;
            this.KarmaSource=source.KarmaSource.Value;
            this.Provider=source.Provider.Value;
        }

        public Achievement GetAchievement(){
            Achievement item=new Achievement();

            item.AvatarId=new Guid(this.AvatarId);
            item.Name = this.Name;
            item.Description = this.Description;

            item.AchievementType = this.AchievementType;
            item.AchievementEarnt = this.AchievementEarnt;

            item.KarmaSourceTitle=this.KarmaSourceTitle;
            item.KarmaSourceDesc=this.KarmaSourceDesc;
            item.WebLink=this.WebLink;

            item.KarmaSource=new EnumValue<KarmaSourceType>(this.KarmaSource);
            item.Provider=new EnumValue<ProviderType>(this.Provider);

            return(item);
        }
    }
}