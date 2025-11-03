using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.Models;

public class SolanaAvatarDetailDto : SolanaBaseDto
{
    public string Username { get; set; }
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
    public string AvatarType { get; set; }
    public string Description { get; set; }

    public string Address { get; set; }
    public string Country { get; set; }
    public string Postcode { get; set; }
    public string Mobile { get; set; }
    public string Landline { get; set; }
    public string Title { get; set; }
    public DateTime DOB { get; set; }

    public IList<IKarmaAkashicRecord> KarmaAkashicRecords { get; set; } = new List<IKarmaAkashicRecord>();
    public int Level { get; set; }
    public long Karma { get; set; }
    public int XP { get; set; }
    public int Xp { get; set; }

    public string Website { get; set; }
    public string Language { get; set; }
    public Dictionary<string, object> MetaData { get; set; }

    public new Guid Id
    {
        get
        {
            return base.Id;
        }
        set
        {
            base.Id = value;
        }
    }

    public ConsoleColor FavouriteColour { get; set; }
    public ConsoleColor STARCLIColour { get; set; }
    public string Town { get; set; }
    public string County { get; set; }
    public string UmaJson { get; set; }
    public string Portrait { get; set; }
    public string Model3D { get; set; }
    public IList<IHeartRateEntry> HeartRateData { get; set; }

    public IOmiverse Omniverse { get; set; } //We have all of creation inside of us... ;-)
    public IList<IAvatarGift> Gifts { get; set; } = new List<IAvatarGift>();
    //public List<Chakra> Chakras { get; set; }
    public IDictionary<DimensionLevel, Guid> DimensionLevelIds { get; set; } = new Dictionary<DimensionLevel, Guid>();
    public IDictionary<DimensionLevel, IHolon> DimensionLevels { get; set; } = new Dictionary<DimensionLevel, IHolon>();
    public IAvatarChakras Chakras { get; set; } = new AvatarChakras();
    public IAvatarAura Aura { get; set; } = new AvatarAura();
    public IAvatarStats Stats { get; set; } = new AvatarStats();
    public IList<IGeneKey> GeneKeys { get; set; } = new List<IGeneKey>();
    public IHumanDesign HumanDesign { get; set; } = new HumanDesign();
    public IAvatarSkills Skills { get; set; } = new AvatarSkills();
    public IAvatarAttributes Attributes { get; set; } = new AvatarAttributes();
    public IAvatarSuperPowers SuperPowers { get; set; } = new AvatarSuperPowers();
    public IList<ISpell> Spells { get; set; } = new List<ISpell>();
    public IList<IAchievement> Achievements { get; set; } = new List<IAchievement>();
    public IList<IInventoryItem> Inventory { get; set; } = new List<IInventoryItem>();
}