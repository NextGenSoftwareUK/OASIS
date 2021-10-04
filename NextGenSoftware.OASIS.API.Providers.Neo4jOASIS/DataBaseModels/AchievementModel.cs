using System;
using Neo4jOgm.Attribute;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.DataBaseModels{

    [NeoNodeEntity("AvatarAchievements", "AvatarAchievements")]

    public class AchievementModel{

        [NeoNodeId]
        public long? Id { get; set; }

        public string AvatarId{ set; get; }

        public string Name { get; set; }
        public string Description { get; set; }
        public KarmaTypePositive AchievementType { get; set; }
        public DateTime AchievementEarnt { get; set; }
        public string KarmaSourceTitle { get; set; } //Name of the app/website/game etc.
        public string KarmaSourceDesc { get; set; }
        public string WebLink { get; set; }
        public string KarmaSource { get; set; } //App, dApp, hApp, Website or Game.
        public String Provider { get; set; }

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
            this.KarmaSource=source.KarmaSource.Name;
            this.Provider=source.Provider.Name;
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

            KarmaSourceType karmaSource=(KarmaSourceType)Enum.Parse<KarmaSourceType>(this.KarmaSource);
            item.KarmaSource=new EnumValue<KarmaSourceType>(karmaSource);

            ProviderType provideType=(ProviderType)Enum.Parse<ProviderType>(this.Provider);
            item.Provider=new EnumValue<ProviderType>(provideType);

            return(item);
        }
    }
}