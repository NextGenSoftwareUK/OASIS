//using System;
//using NextGenSoftware.OASIS.API.DNA;
//using NextGenSoftware.OASIS.API.Core.Interfaces;
//using NextGenSoftware.OASIS.Common;
//using NextGenSoftware.OASIS.API.Core.Managers;
//using NextGenSoftware.OASIS.API.Core.Enums;
//using NextGenSoftware.OASIS.API.Providers.IPFSOASIS;


//namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
//{
//    public class FileManager : OASISManager//, INFTManager
//    {
//        public FileManager(IOASISStorageProvider OASISStorageProvider, Guid avatarId, OASISDNA OASISDNA = null) : base(OASISStorageProvider, avatarId, OASISDNA)
//        {

//        }

//        public FileManager(Guid avatarId, OASISDNA OASISDNA = null) : base(avatarId, OASISDNA)
//        {

//        }

//        public OASISResult<Guid> SaveIPFSFile(byte[] data)
//        {
//            OASISResult<Guid> result = new OASISResult<Guid>();
//            string errorMessage = "An error occured in FileManager.SaveIPFSFile. Reason: ";

//            IPFSOASIS IPFSProvider = ProviderManager.Instance.GetProvider(ProviderType.IPFSOASIS) as IPFSOASIS;

//            if (IPFSProvider != null)
//            {
//                IPFSProvider.Sa
//            }

//            if (holonResult != null && holonResult.Result != null && !holonResult.IsError)
//            {
//                result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(holonResult, result);
//                result.Result = holonResult.Result.Id;
//                result.Message = "File Saved";
//            }
//            else
//                OASISErrorHandling.HandleError(ref result, $"{errorMessage} There was an error saving the holon contianing the file. Reason: {holonResult.Message}");

//            return result;
//        }
//    }
//}