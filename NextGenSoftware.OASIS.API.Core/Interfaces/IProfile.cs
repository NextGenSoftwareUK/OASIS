using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Core
{
    public interface IProfile : IHolon
    {
        Guid UserId { get; set; } //TODO: Remember to add this to the HC Rust code...
        string Username { get; set; }
        string Password { get; set; }
        string Email { get; set; }
        string Title { get; set; }
        string FirstName { get; set; }
        string LastName { get; set; }
        string DOB { get; set; }
        string PlayerAddress { get; set; }
        int Karma { get; }
        int Level { get; }

        //bool AddKarma(int karmaToAdd);
        //bool SubstractKarma(int karmaToRemove);

        Task<KarmaAkashicRecord> KarmaEarnt(KarmaTypePositive karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, bool autoSave = true);
        Task<KarmaAkashicRecord> KarmaLost(KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, bool autoSave = true);
        List<KarmaAkashicRecord> KarmaAkashicRecords { get; set; }
        Task<bool> Save();
    }
}
