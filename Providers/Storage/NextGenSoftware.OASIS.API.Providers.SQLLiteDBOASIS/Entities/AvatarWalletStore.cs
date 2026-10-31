using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities
{
    // Separate from replicated Avatar state: ciphertext is supplied by WalletManager.
    [Table("AvatarWalletStore")]
    public class AvatarWalletStore
    {
        [Key] public string AvatarId { get; set; }
        [Required] public string WalletsJson { get; set; }
    }
}
