using NextGenSoftware.Holochain.HoloNET.Client.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.Holochain.HoloNET.Client.Desktop;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.HoloOASIS.Core;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS.Desktop
{
    public class HoloOASIS : HoloOASISBase
    {
        public HoloOASIS(string holochainURI, HolochainVersion version) : base(new HoloNETClient(holochainURI, version))
        {
            
        }

        //public HoloOASIS(string holochainURI, string version) : base(new HoloNETClient(holochainURI, (HolochainVersion)Enum.Parse(typeof(HolochainVersion), version)))
        //{

        //}
        public override async Task<IEnumerable<IOland>> LoadAllOlandsAsync()
        {
            throw new NotImplementedException();
        }

        public override async Task<IOland> LoadOlandAsync(int id)
        {
            throw new NotImplementedException();
        }

        public override async Task<bool> DeleteOlandAsync(int id)
        {
            throw new NotImplementedException();
        }

        public override async Task<bool> DeleteOlandAsync(int id, bool safeDelete)
        {
            throw new NotImplementedException();
        }

        public override async Task<int> CreateOlandAsync(IOland oland)
        {
            throw new NotImplementedException();
        }

        public override async Task<bool> UpdateOlandAsync(IOland oland)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<IOland> LoadAllOlands()
        {
            throw new NotImplementedException();
        }

        public override IOland LoadOland(int id)
        {
            throw new NotImplementedException();
        }

        public override bool DeleteOland(int id)
        {
            throw new NotImplementedException();
        }

        public override bool DeleteOland(int id, bool safeDelete)
        {
            throw new NotImplementedException();
        }

        public override int CreateOland(IOland oland)
        {
            throw new NotImplementedException();
        }

        public override bool UpdateOland(IOland oland)
        {
            throw new NotImplementedException();
        }
    }
}
