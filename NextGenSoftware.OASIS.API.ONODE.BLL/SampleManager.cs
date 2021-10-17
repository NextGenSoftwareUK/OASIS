using System;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;

namespace NextGenSoftware.OASIS.API.ONODE.BLL
{
    public class SampleManager
    {
        private HolonManager _holonManager = null;

        public SampleManager()
        {
            OASISResult<IOASISStorage> result = OASISBootLoader.OASISBootLoader.GetAndActivateDefaultProvider();

            //TODO: Eventually want to replace all exceptions with OASISResult throughout the OASIS because then it makes sure errors are handled properly and friendly messages are shown (plus less overhead of throwing an entire stack trace!)
            if (result.IsError)
            {
                string errorMessage = string.Concat("Error calling OASISDNAManager.GetAndActivateDefaultProvider(). Error details: ", result.Message);
                ErrorHandling.HandleError(ref result, errorMessage, true, false, true);
            }
            else
                _holonManager = new HolonManager(result.Result);
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

            return _holonManager.SaveHolon<SampleHolon>(sampleHolon);
        }

        public OASISResult<SampleHolon> LoadSampleHolon(Guid holonId)
        {
            return _holonManager.LoadHolon<SampleHolon>(holonId);
        }
    }
}