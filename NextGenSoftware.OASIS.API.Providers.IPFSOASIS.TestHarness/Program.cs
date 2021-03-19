using NextGenSoftware.OASIS.API.Core.Holons;
using System;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.IPFSOASIS.TestHarness
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("NEXTGEN SOFTWARE IPFSOASIS TEST HARNESS V1.O");
            Console.WriteLine("");

            IPFSOASIS ipfs = new IPFSOASIS();
            Avatar avatar = (Avatar)await ipfs.LoadAvatarAsync("david", "ellams");
        }
    }
}
