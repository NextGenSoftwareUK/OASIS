using System.Collections.Generic;
using Models.Cargo;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Cargo
{
    public class SaveNft
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Amount { get; set; }
        public IEnumerable<ToDelete> ToDelete { get; set; }
        public string Metadata { get; set; }
        public IEnumerable<byte[]> Files { get; set; }
        public byte[] PreviewImage { get; set; }
        public DisplayContent DisplayContent { get; set; }
    }
}