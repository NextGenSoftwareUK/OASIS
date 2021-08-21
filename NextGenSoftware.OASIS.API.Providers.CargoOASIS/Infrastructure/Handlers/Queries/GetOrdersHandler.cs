using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Interfaces;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Cargo;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Common;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Handlers.Queries
{
    public class GetOrdersHandler : IHandle<Response<PaginationResponseWithResults<IEnumerable<Order>>>, OrderParams>
    {
        public Task<Response<PaginationResponseWithResults<IEnumerable<Order>>>> Handle(OrderParams request)
        {
            throw new System.NotImplementedException();
        }
    }
}