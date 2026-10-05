using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    public class SampleManager : OASISManager, ISampleManager
    {
        public SampleManager(Guid avatarId, OASISDNA OASISDNA = null) : base(avatarId, OASISDNA)
        {

        }

        public SampleManager(IOASISStorageProvider OASISStorageProvider, Guid avatarId, OASISDNA OASISDNA = null) : base(OASISStorageProvider, avatarId, OASISDNA)
        {

        }

        public OASISResult<SampleHolon> SaveSampleHolon(string customPropety, string customPropety2, Guid avatarId, DateTime customDate, int customNumber, long customLongNumber)
        {
            SampleHolon sampleHolon = new SampleHolon();
            sampleHolon.CustomProperty = customPropety;
            sampleHolon.CustomProperty2 = customPropety2;
            sampleHolon.AvatarId = avatarId;
            sampleHolon.CustomDate = customDate;
            sampleHolon.CustomNumber = customNumber;
            sampleHolon.CustomLongNumber = customLongNumber;

            return Data.SaveHolon<SampleHolon>(sampleHolon, avatarId);
        }

        public async Task<OASISResult<SampleHolon>> SaveSampleHolonAsync(string customPropety, string customPropety2, Guid avatarId, DateTime customDate, int customNumber, long customLongNumber)
        {
            SampleHolon sampleHolon = new SampleHolon();
            sampleHolon.CustomProperty = customPropety;
            sampleHolon.CustomProperty2 = customPropety2;
            sampleHolon.AvatarId = avatarId;
            sampleHolon.CustomDate = customDate;
            sampleHolon.CustomNumber = customNumber;
            sampleHolon.CustomLongNumber = customLongNumber;

            return await Data.SaveHolonAsync<SampleHolon>(sampleHolon, avatarId);
        }

        public OASISResult<SampleHolon> LoadSampleHolon(Guid holonId)
        {
            return Data.LoadHolon<SampleHolon>(holonId);
        }

        public async Task<OASISResult<SampleHolon>> LoadSampleHolonAsync(Guid holonId)
        {
            return await Data.LoadHolonAsync<SampleHolon>(holonId);
        }
    }
}