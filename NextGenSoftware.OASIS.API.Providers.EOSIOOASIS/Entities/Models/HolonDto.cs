using System;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.Models
{
    public class HolonDto
    {
        public int EntityId { get; set; }
        public string HolonId { get; set; }
        public string Info { get; set; }
        public bool IsDeleted { get; set; }
        
        public IHolon GetBaseHolon()
        {
            if (string.IsNullOrEmpty(Info))
                throw new ArgumentNullException(nameof(Info));
            return JsonConvert.DeserializeObject<IHolon>(Info);
        }
    }
}