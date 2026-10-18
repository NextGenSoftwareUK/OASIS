using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests;

internal sealed class IndependentAvatarStats:IAvatarStats
{
    public AvatarStat Energy {get;set;}=new();
    public AvatarStat HP {get;set;}=new();
    public AvatarStat Mana {get;set;}=new();
    public AvatarStat Stamina {get;set;}=new();
}
internal sealed class IndependentAvatarPowers:IAvatarSuperPowers
{
    public int AstralProjection {get;set;}
    public int BioLocatation {get;set;}
    public int Flight {get;set;}
    public int FreezeBreath {get;set;}
    public int HeatVision {get;set;}
    public int Invulnerability {get;set;}
    public int RemoteViewing {get;set;}
    public int SuperSpeed {get;set;}
    public int SuperStrength {get;set;}
    public int Telekinesis {get;set;}
    public int Telepathy {get;set;}
    public int Teleportation {get;set;}
    public int XRayVision {get;set;}
}
internal sealed class IndependentAvatarAura:IAvatarAura
{
    public int Progress {get;set;}
    public int Brightness {get;set;}
    public int Size {get;set;}
    public int ColourBlue {get;set;}
    public int ColourGreen {get;set;}
    public int ColourRed {get;set;}
    public int Level {get;set;}
    public int Value {get;set;}
}
internal sealed class IndependentHumanDesign:IHumanDesign
{
    public string Type {get;set;}="";
}
