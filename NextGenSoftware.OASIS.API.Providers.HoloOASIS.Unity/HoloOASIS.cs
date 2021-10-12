using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.Holochain.HoloNET.Client.Core;
using NextGenSoftware.Holochain.HoloNET.Client.Unity;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.HoloOASIS.Core;


namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS.Unity
{
    public class HoloOASIS : HoloOASISBase
    {
        public HoloOASIS(string holochainURI, HolochainVersion version) : base(new HoloNETClient(holochainURI, version))
        {
            
        }

        public override async Task<IEnumerable<IOland>> LoadAllOlandsAsync()
        {
            throw new System.NotImplementedException();
        }

        public override async Task<IOland> LoadOlandAsync(int id)
        {
            throw new System.NotImplementedException();
        }

        public override async Task<bool> DeleteOlandAsync(int id)
        {
            throw new System.NotImplementedException();
        }

        public override async Task<bool> DeleteOlandAsync(int id, bool safeDelete)
        {
            throw new System.NotImplementedException();
        }

        public override async Task<int> CreateOlandAsync(IOland oland)
        {
            throw new System.NotImplementedException();
        }

        public override async Task<bool> UpdateOlandAsync(IOland oland)
        {
            throw new System.NotImplementedException();
        }

        public override IEnumerable<IOland> LoadAllOlands()
        {
            throw new System.NotImplementedException();
        }

        public override IOland LoadOland(int id)
        {
            throw new System.NotImplementedException();
        }

        public override bool DeleteOland(int id)
        {
            throw new System.NotImplementedException();
        }

        public override bool DeleteOland(int id, bool safeDelete)
        {
            throw new System.NotImplementedException();
        }

        public override int CreateOland(IOland oland)
        {
            throw new System.NotImplementedException();
        }

        public override bool UpdateOland(IOland oland)
        {
            throw new System.NotImplementedException();
        }
    }
}
