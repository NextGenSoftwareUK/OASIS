using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Neo4jOgm.Attribute;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.DataBaseModels{

    [NeoNodeEntity("AvatarChakras", "AvatarChakras")]

    public class AvatarChakraModel{

        [NeoNodeId]
        public long? Id { get; set; }

        public String Type { get; set; }
        public string Name { get; set; }

        public string SanskritName { get; set; }
        public string Description { get; set; }
        public string WhatItControls { get; set; }
        public String YogaPose { get; set; } 
        public string WhenItDevelops { get; set; }
        public String Element { get; set; }


        [NeoRelationship("Crystal_FOR")]

        public CrystalModel Crystal { get; set; }
        //public Color Color { get; set; } //TODO: Put back in later when have time to fix deserialization issue with MongoDB...
        public int Level { get; set; }
        public int Progress { get; set; }
        public int XP { get; set; }
        public string AvatarId { get; set; }
        [NeoRelationship("HAS_AvatarGifts")]

        public List<AvatarGiftModel> GiftsUnlocked { get; set; } = new List<AvatarGiftModel>();

        public AvatarChakraModel(){}
        public AvatarChakraModel(Chakra source){

            this.Type=source.Type.Name;
            this.Name=source.Name;

            this.SanskritName=source.SanskritName;
            this.Description=source.Description;
            this.WhatItControls=source.WhatItControls;
            this.YogaPose=source.YogaPose.Name;
            this.WhenItDevelops=source.WhenItDevelops;
            this.Element=source.Element.Name;

            this.Crystal=new CrystalModel(source.Crystal);

            this.Level=source.Level;
            this.Progress=source.Progress;
            this.XP=source.XP;

            foreach(AvatarGift gift in source.GiftsUnlocked){

                this.GiftsUnlocked.Add(new AvatarGiftModel(gift));
            }
        }

        public Chakra GetAvatarChakras(){

            Chakra item = null;

            ChakraType chakraType=(ChakraType)Enum.Parse<ChakraType>(this.Type);
            switch(chakraType){

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

            item.Type=new EnumValue<ChakraType>(chakraType);
            item.Name=this.Name;

            item.SanskritName=this.SanskritName;
            item.Description=this.Description;
            item.WhatItControls=this.WhatItControls;

            YogaPoseType poseType=Enum.Parse<YogaPoseType>(this.YogaPose);
            item.YogaPose=new EnumValue<YogaPoseType>(poseType);

            item.WhenItDevelops=this.WhenItDevelops;

            ElementType elementType=Enum.Parse<ElementType>(this.Element);
            item.Element=new EnumValue<ElementType>(elementType);

            item.Level=this.Level;
            item.Progress=this.Progress;
            item.XP=this.XP;

            return(item);
        }

    }
}