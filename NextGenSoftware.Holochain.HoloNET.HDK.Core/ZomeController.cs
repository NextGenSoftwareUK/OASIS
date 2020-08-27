
namespace NextGenSoftware.Holochain.HoloNET.HDK.Core
{
    public class ZomeController
    {
        public IPlanet Planet { get; set; }

        public ZomeController(IPlanet planet)
        {
            this.Planet = planet;
        }


    }
}
