using System;
using System.Collections.Generic;
using System.Linq;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Gardens;

public sealed class SeedSpecies
{
    public string Id {get;set;}
    public string Name {get;set;}
    public string Shape {get;set;}
    public int GrowthDays {get;set;}
    public int CareRequired {get;set;}
    public int MatureKarma {get;set;}=100;
    public string QuestId {get;set;}
}

/// <summary>Gameplay policy shared by all gardening interfaces. Time and rewards are never supplied by clients.</summary>
public static class GardenRules
{
    public const int PlantKarma=500, CareKarma=50, WaterCapacity=5;
    public static SeedSpecies[] Species {get;private set;}=new SeedSpecies[]{
        new(){Id="wildflower",Name="Wildflower",Shape="flower",GrowthDays=7,CareRequired=10},
        new(){Id="rose",Name="Rose bush",Shape="bush",GrowthDays=14,CareRequired=20},
        new(){Id="oak",Name="Oak tree",Shape="tree",GrowthDays=30,CareRequired=40},
        new(){Id="apple",Name="Apple tree",Shape="fruit-tree",GrowthDays=21,CareRequired=30}};
    public static void Configure(SeedSpecies[] species)
    {
        if(species==null)return;
        if(species.Length!=4||species.Select(s=>s.Id).Distinct().Count()!=4||
            species.Any(s=>!new[]{"wildflower","rose","oak","apple"}.Contains(s.Id)||string.IsNullOrWhiteSpace(s.Name)||s.GrowthDays<1||s.CareRequired<1||s.MatureKarma<1||s.MatureKarma>500||
                (!string.IsNullOrWhiteSpace(s.QuestId)&&!Guid.TryParse(s.QuestId,out _))))
            throw new ArgumentException("Gardens:Species must author the four supported species, positive growth/rewards, and valid optional quest IDs.");
        Species=species;
    }
    public static SeedSpecies Get(string id)=>Species.SingleOrDefault(s=>s.Id==id)
        ??throw new ArgumentException("Unknown seed species.");
    public static string Day(DateTime utc)=>utc.ToUniversalTime().ToString("yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture);
    public static double Progress(SeedSpecies species,DateTime plantedUtc,DateTime now,int water,int love)
        =>Math.Clamp(Math.Min((now-plantedUtc).TotalDays/species.GrowthDays,(water+love)/(double)species.CareRequired),0,1);
    public static string Stage(double progress)=>progress>=1?"Alive":progress>=.7?"Growing":progress>=.35?"Sprout":"Seed";
    public static void ValidateAction(string action)
    {if(action!="water"&&action!="love")throw new ArgumentException("Choose Water or Give love.");}
    public static void ValidateObservation(DateTime observedUtc,DateTime now)
    {if(observedUtc.Kind!=DateTimeKind.Utc||Math.Abs((now-observedUtc).TotalMinutes)>2)throw new ArgumentException("A fresh location observation is required.");}
}
