//using System.Threading.Tasks;
//using NextGenSoftware.OASIS.API.Core.Enums;
//using NextGenSoftware.OASIS.API.Core.Helpers;
//using NextGenSoftware.OASIS.API.Core.Interfaces;
//using NextGenSoftware.OASIS.API.Core.Managers;
//using NextGenSoftware.OASIS.API.DNA;
//using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces;

//namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
//{
//    public class SearchManager : OASISManager, ISearchManager
//    {
//        public SearchManager(OASISDNA OASISDNA = null) : base(OASISDNA)
//        {

//        }

//        public SearchManager(IOASISStorageProvider OASISStorageProvider, OASISDNA OASISDNA = null) : base(OASISStorageProvider, OASISDNA)
//        {

//        }

//        public async Task<ISearchResults> SearchAsync(ISearchParams searchParams, ProviderType provider = ProviderType.Default)
//        {
//            OASISResult<ISearchResults> result = await ((IOASISStorageProvider)ProviderManager.SetAndActivateCurrentStorageProvider(provider)).SearchAsync(searchParams);
//            return result.Result;
//        }
//    }
//}