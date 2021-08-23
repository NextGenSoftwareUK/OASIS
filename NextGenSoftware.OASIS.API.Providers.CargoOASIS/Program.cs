using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Enum;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Handlers.Queries;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Interfaces;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Common;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS
{
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            IHandle<Response<GetTokenDetailsResponseModel>, GetTokenDetailsRequestModel> 
                tokenDetailsHandler = new GetTokenDetailsHandler();

            var details = await tokenDetailsHandler.Handle(new GetTokenDetailsRequestModel()
            {
                CollectibleId = "0xC7C6bEF6d146acF326A3a5f1d3CC9E920711906D",
                ProjectId = "dolli-comics"
            });

            if (details.ResponseStatus == ResponseStatus.Success)
            {
                Console.WriteLine(details.Payload.Data.TokenUrl);
            }
        }
    }
}