using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;

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
    public int XP { get; set; }
    public int Xp { get; set; }

    public string Website { get; set; }
    public string Language { get; set; }
    public Dictionary<string, object> MetaData { get; set; }
}