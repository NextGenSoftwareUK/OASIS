using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests;

internal sealed class IndependentAvatarChakras:IAvatarChakras
{
    public CrownChakra Crown {get;set;}=new();
    public HeartChakra Heart {get;set;}=new();
    public RootChakra Root {get;set;}=new();
    public SacralChakra Sacral {get;set;}=new();
    public SolarPlexusChakra SolarPlexus {get;set;}=new();
    public ThirdEyeChakra ThirdEye {get;set;}=new();
    public ThroatChakra Throat {get;set;}=new();

    internal static IndependentAvatarChakras Create(int revision)
    {
        var result=new IndependentAvatarChakras();
        foreach(var slot in typeof(IAvatarChakras).GetProperties()){
            var chakra=(Chakra)slot.GetValue(result)!;
            foreach(var property in typeof(IChakra).GetProperties()){
                var type=property.PropertyType;
                if(type==typeof(string))property.SetValue(chakra,$"{slot.Name}-{property.Name}-{revision}");
                else if(type==typeof(int))property.SetValue(chakra,100*revision+property.Name.Length);
                else if(type.IsGenericType && type.GetGenericTypeDefinition()==typeof(EnumValue<>)){
                    var values=Enum.GetValues(type.GetGenericArguments()[0]);
                    property.SetValue(chakra,Activator.CreateInstance(type,values.GetValue(revision%values.Length)));
                }
            }
            chakra.Crystal=new Crystal{Description=$"crystal-{slot.Name}-{revision}",
                Name=new EnumValue<CrystalName>((CrystalName)Enum.GetValues(typeof(CrystalName)).GetValue(revision%Enum.GetValues(typeof(CrystalName)).Length)!),
                Type=new EnumValue<CrystalType>((CrystalType)Enum.GetValues(typeof(CrystalType)).GetValue(revision%Enum.GetValues(typeof(CrystalType)).Length)!),
                ProtectionLevel=revision+1,EnergisingLevel=revision+2,GroundingLevel=revision+3,
                CleansingLevel=revision+4,AmplifyicationLevel=revision+5};
            var gift=(IAvatarGift)IndependentAvatarCollections.Create("Gifts",revision);
            chakra.GiftsUnlocked=new List<AvatarGift>{new(){AvatarId=gift.AvatarId,GiftEarnt=gift.GiftEarnt,
                GiftType=gift.GiftType,KarmaSource=gift.KarmaSource,KarmaSourceDesc=gift.KarmaSourceDesc,
                KarmaSourceTitle=gift.KarmaSourceTitle,Provider=gift.Provider,WebLink=gift.WebLink}};
        }
        return result;
    }
}

internal sealed class IndependentSpell:ISpell
{
    public string Description {get;set;}=""; public string Name {get;set;}="";
    public int FireDamage {get;set;} public int HealingPower {get;set;} public int IceDamage {get;set;}
    public int LightningDamage {get;set;} public int PoisonDamage {get;set;} public int WaterDamage {get;set;} public int WindDamage {get;set;}
}
internal sealed class IndependentGeneKey:IGeneKey
{
    public string Description {get;set;}=""; public string Gift {get;set;}="";
    public string Name {get;set;}=""; public string Shadow {get;set;}=""; public string Sidhi {get;set;}="";
}
internal sealed class IndependentGift:IAvatarGift
{
    public Guid AvatarId {get;set;} public DateTime GiftEarnt {get;set;} public KarmaTypePositive GiftType {get;set;}
    public EnumValue<KarmaSourceType> KarmaSource {get;set;}=null!;
    public string KarmaSourceDesc {get;set;}=""; public string KarmaSourceTitle {get;set;}="";
    public EnumValue<ProviderType> Provider {get;set;}=null!; public string WebLink {get;set;}="";
}
internal sealed class IndependentAchievement:IAchievement
{
    public Guid AvatarId {get;set;} public DateTime AchievementEarnt {get;set;} public KarmaTypePositive AchievementType {get;set;}
    public string Description {get;set;}=""; public string Name {get;set;}="";
    public EnumValue<KarmaSourceType> KarmaSource {get;set;}=null!;
    public string KarmaSourceDesc {get;set;}=""; public string KarmaSourceTitle {get;set;}="";
    public EnumValue<ProviderType> Provider {get;set;}=null!; public string WebLink {get;set;}="";
}
internal sealed class IndependentKarmaRecord:IKarmaAkashicRecord
{
    public Guid AvatarId {get;set;} public DateTime Date {get;set;} public int Karma {get;set;} public long TotalKarma {get;set;}
    public EnumValue<KarmaEarntOrLost> KarmaEarntOrLost {get;set;}=null!;
    public EnumValue<KarmaSourceType> KarmaSource {get;set;}=null!;
    public string KarmaSourceDesc {get;set;}=""; public string KarmaSourceTitle {get;set;}="";
    public EnumValue<KarmaTypeNegative> KarmaTypeNegative {get;set;}=null!;
    public EnumValue<KarmaTypePositive> KarmaTypePositive {get;set;}=null!;
    public EnumValue<ProviderType> Provider {get;set;}=null!; public string WebLink {get;set;}="";
}
internal static class IndependentAvatarCollections
{
    internal static readonly string[] Fields={"Spells","GeneKeys","Gifts","Achievements","KarmaAkashicRecords"};
    internal static PropertyInfo Slot(string field)=>typeof(IAvatarDetail).GetProperty(field)!;
    internal static object Create(string field,int revision)
    {
        object entry=field switch{"Spells"=>new IndependentSpell(),"GeneKeys"=>new IndependentGeneKey(),"Gifts"=>new IndependentGift(),"Achievements"=>new IndependentAchievement(),_=>new IndependentKarmaRecord()};
        var contract=Slot(field).PropertyType.GetGenericArguments()[0];var number=revision*100;
        foreach(var property in contract.GetProperties()){
            var type=property.PropertyType;object value;
            if(type==typeof(string))value=$"revision-{revision}-{property.Name}";
            else if(type==typeof(int))value=number++;
            else if(type==typeof(long))value=revision==1?(long)int.MaxValue+number++:(long)int.MinValue-number++;
            else if(type==typeof(Guid))value=Guid.NewGuid();
            else if(type==typeof(DateTime))value=new DateTime(2000+revision,2,3,4,5,6,DateTimeKind.Utc);
            else if(type.IsEnum)value=Enum.GetValues(type).GetValue(revision%Enum.GetValues(type).Length)!;
            else if(type.IsGenericType && type.GetGenericTypeDefinition()==typeof(EnumValue<>)){
                var choices=Enum.GetValues(type.GetGenericArguments()[0]);value=Activator.CreateInstance(type,choices.GetValue(revision%choices.Length))!;
            }else throw new InvalidOperationException("Unhandled collection contract property: "+property);
            property.SetValue(entry,value);
        }
        return entry;
    }
    internal static void Assign(IAvatarDetail detail,string field,object entry)
    {
        var slot=Slot(field);var list=(IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(slot.PropertyType.GetGenericArguments()[0]))!;
        list.Add(entry);slot.SetValue(detail,list);
    }
    internal static object? Scalar(PropertyInfo property,object entry)
    {
        var value=property.GetValue(entry);
        return property.PropertyType.IsGenericType?property.PropertyType.GetProperty("Value")!.GetValue(value):value;
    }
}
