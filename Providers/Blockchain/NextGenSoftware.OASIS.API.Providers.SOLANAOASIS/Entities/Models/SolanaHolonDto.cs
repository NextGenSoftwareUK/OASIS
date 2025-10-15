using Solnet.Rpc.Models;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.Models;

public class SolanaHolonDto : SolanaBaseDto
{
    public string Name { get; set; }
    public string Description { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
    public bool IsActive { get; set; }

    public string PublicKey { get; set; }
    public AccountInfo AccountInfo { get; set; }
    public ulong Lamports { get; set; }
    public string Owner { get; set; }
    public bool Executable { get; set; }
    public ulong RentEpoch { get; set; }
    public List<string> Data { get; set; }
    public Dictionary<string, object> MetaData { get; set; }

    public Guid ParentOmniverseId { get; set; }
    public Guid ParentMultiverseId { get; set; }
    public Guid ParentUniverseId { get; set; }
}