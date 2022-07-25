using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.BLL.Holons;
using NextGenSoftware.OASIS.API.ONODE.BLL.Interfaces;

namespace NextGenSoftware.OASIS.API.ONODE.BLL.Managers
{
    public class SampleManager : OASISManager, ISampleManager
    {
        public SampleManager(OASISDNA OASISDNA = null) : base(OASISDNA)
        {

        }

        public SampleManager(IOASISStorageProvider OASISStorageProvider, OASISDNA OASISDNA = null) : base(OASISStorageProvider, OASISDNA)
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

            return Data.SaveHolon<SampleHolon>(sampleHolon);
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

            return await Data.SaveHolonAsync<SampleHolon>(sampleHolon);
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