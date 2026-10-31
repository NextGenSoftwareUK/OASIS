using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests;

// Interface-only input, deliberately not derived from the framework DTO.
internal sealed class IndependentAvatarSkills:IAvatarSkills
{
    public int Computers {get;set;}
    public int Engineering {get;set;}
    public int Farming {get;set;}
    public int FireStarting {get;set;}
    public int Fishing {get;set;}
    public int Languages {get;set;}
    public int Meditation {get;set;}
    public int MeleeCombat {get;set;}
    public int Mindfulness {get;set;}
    public int Negotiating {get;set;}
    public int RangedCombat {get;set;}
    public int Research {get;set;}
    public int Science {get;set;}
    public int SpellCasting {get;set;}
    public int Translating {get;set;}
    public int Yoga {get;set;}
}
