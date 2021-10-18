using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Providers.OnionProtocol.DbModel.Models;
using NextGenSoftware.OASIS.API.Providers.OnionProtocol.Services.Interface;
using OASIS_Onion.WebApi.V1.Controller.Entity;
using OASIS_Onion.WebApi.V1.ViewModel.TestViewData;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace OASIS_Onion.Http.WebApi.V1.Controller
{
    [Route("api/v1/test")]
    public class TestController : EntityController<Test, ITestService>
    {
        private readonly ITestService _testService;
        private readonly IHttpContextAccessor _accessor;

        public TestController(ITestService testService, IHttpContextAccessor accessor) : base(testService, accessor)
        {
            this._testService = testService;
            this._accessor = accessor;
        }

        [HttpGet]
        public async Task<IEnumerable<TestGetViewModel>> GetAll()
        {
            var list = await _testService.GetAll();
            var view = new HashSet<TestGetViewModel>();
            foreach (var entity in list)
            {
                var viewItem = new TestGetViewModel(entity);
                view.Add(viewItem);
            }
            return view;
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<TestGetViewModel> Get(Guid id)
        {
            var entity = await _testService.Get(id);
            var viewItem = new TestGetViewModel();
            if (entity != null)
            {
                viewItem = new TestGetViewModel(entity);
            }
            return viewItem;
        }

        [HttpPost]
        public async Task<TestGetViewModel> Post([FromBody] TestCreateViewModel testModel)
        {
            var entity = new Test();
            SetClientIp(entity);
            testModel.Hydrate(entity);
            var view = new TestGetViewModel(entity);
            await _testService.Create(entity);
            return view;
        }

        [HttpPut]
        [Route("{id}")]
        public async Task<TestGetViewModel> Put([FromBody] TestGetViewModel testModel, Guid id)
        {
            if (id.Equals(null))
            {
                Response.StatusCode = 402;
                return new TestGetViewModel();
            }
            var record = await _testService.Get(id);
            if (record != null)
            {
                var entity = new Test();
                testModel.Hydrate(entity);
                entity.Id = id;
                entity = await _testService.Update(entity);
                var view = new TestGetViewModel(entity);
                return view;
            }
            Response.StatusCode = (int)HttpStatusCode.OK;
            return new TestGetViewModel();
        }

        [HttpDelete("{id}")]
        public virtual async Task<HttpStatusCode> Delete(Guid id)
        {
            var instance = await _testService.Get(id);
            if (instance == null)
            {
                Response.StatusCode = (int)HttpStatusCode.NotFound;
                return HttpStatusCode.NotFound;
            }
            _testService.Delete(instance);
            return HttpStatusCode.OK;
        }
    }
}