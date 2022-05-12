using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.TestHarness
{
    internal static class Program
    {
        public static async Task Main()
        {
            EOSIOOASIS eosProvider = new EOSIOOASIS("https://api.eosnewyork.io");
            eosProvider.ActivateProvider();
            await eosProvider.SaveAvatarAsync(new Avatar()
            {
                FirstName = "Pop",
                LastName = "Nod",
                Id = Guid.NewGuid(),
                AvatarId = Guid.NewGuid(),
                Password = "Oops..."
            });
        }
    }
}