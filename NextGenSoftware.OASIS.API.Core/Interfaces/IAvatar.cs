using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Core
{
    public interface IAvatar : IHolon
    {
        string Username { get; set; }
        string Password { get; set; }
        string Email { get; set; }
        string Title { get; set; }
        string FirstName { get; set; }
        string LastName { get; set; }
        string FullName { get; }
        string DOB { get; set; }
        string Address { get; set; }
        int Karma { get; }
        int Level { get; }
        string Town { get; set; }
        string County { get; set; }
        string Country { get; set; }
        string Postcode { get; set; }
        string Mobile { get; set; }
        string Landline { get; set; }
        AvatarType AvatarType { get; set; }

        Task<KarmaAkashicRecord> KarmaEarnt(KarmaTypePositive karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, bool autoSave = true);
        Task<KarmaAkashicRecord> KarmaLost(KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, bool autoSave = true);
        List<KarmaAkashicRecord> KarmaAkashicRecords { get; set; }
        Task<bool> Save();
    }
}
