using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities{

    [Table("AvatarChakras")]
    public class AvatarChakraModel{

        [Required, Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id{ set; get;}

        public ChakraType Type { get; set; }
        public string Name { get; set; }

        public string SanskritName { get; set; }
        public string Description { get; set; }
        public string WhatItControls { get; set; }
        public YogaPoseType YogaPose { get; set; } 
        public string WhenItDevelops { get; set; }
        public ElementType Element { get; set; } 
        public CrystalModel Crystal { get; set; }
        public int Level { get; set; }
        public int Progress { get; set; }
        public int XP { get; set; }

        [ForeignKey("AvatarId")] 
        public AvatarDetailModel AvatarDetail { get; set; }
        public string AvatarId { get; set; }

        public List<AvatarGiftModel> GiftsUnlocked { get; set; } = new List<AvatarGiftModel>();

        public AvatarChakraModel(){}
        public AvatarChakraModel(Chakra source){

            this.Type=source.Type.Value;
            this.Name=source.Name;

            this.SanskritName=source.SanskritName;
            this.Description=source.Description;
            this.WhatItControls=source.WhatItControls;
            this.YogaPose=source.YogaPose.Value;
            this.WhenItDevelops=source.WhenItDevelops;
            this.Element=source.Element.Value;

            this.Crystal=new CrystalModel(source.Crystal);
            this.Crystal.AvatarChakraId=this.Id;

            this.Level=source.Level;
            this.Progress=source.Progress;
            this.XP=source.XP;

            foreach(AvatarGift gift in source.GiftsUnlocked){

                this.GiftsUnlocked.Add(new AvatarGiftModel(gift));
            }
        }

        public Chakra GetAvatarChakra(){

            Chakra item = null;

            switch(this.Type){

                case ChakraType.Sacral:
                    item=new SacralChakra();
                    break;
                case ChakraType.Crown:
                    item=new CrownChakra();
                    break;
                case ChakraType.Heart:
                    item=new HeartChakra();
                    break;
                case ChakraType.SolarPlexus:
                    item=new SolarPlexusChakra();
                    break;
                case ChakraType.Throat:
                    item=new ThroatChakra();
                    break;
                case ChakraType.ThirdEye:
                    item=new ThirdEyeChakra();
                    break;
                default:
                    item=new RootChakra();
                    break;
            }

            item.Type=new EnumValue<ChakraType>(this.Type);
            item.Name=this.Name;

            item.SanskritName=this.SanskritName;
            item.Description=this.Description;
            item.WhatItControls=this.WhatItControls;

            item.YogaPose=new EnumValue<YogaPoseType>(this.YogaPose);
            item.Element=new EnumValue<ElementType>(this.Element);

            item.WhenItDevelops=this.WhenItDevelops;

            item.Level=this.Level;
            item.Progress=this.Progress;
            item.XP=this.XP;

            return(item);
        }

    }
}