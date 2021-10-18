using NextGenSoftware.OASIS.API.Providers.OnionProtocol.DbModel.Models;
using OASIS_Onion.WebApi.V1.ViewModel.Entity;

namespace OASIS_Onion.WebApi.V1.ViewModel.TestViewData
{
    public class TestCreateViewModel : EntityConverter<Test>
    {
        public TestCreateViewModel()
        {
        }

        public TestCreateViewModel(Test test) : base(test)
        {
        }

        public string TestName { get; set; }
    }
}