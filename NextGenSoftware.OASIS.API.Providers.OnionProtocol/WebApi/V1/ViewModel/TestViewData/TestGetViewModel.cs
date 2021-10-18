using NextGenSoftware.OASIS.API.Providers.OnionProtocol.DbModel.Models;
using OASIS_Onion.WebApi.V1.ViewModel.Entity;
using System;

namespace OASIS_Onion.WebApi.V1.ViewModel.TestViewData
{
    public class TestGetViewModel : EntityConverter<Test>
    {
        public TestGetViewModel()
        {
        }

        public TestGetViewModel(Test test) : base(test)
        {
        }

        public Guid Id { get; set; }

        public string TestName { get; set; }
    }
}