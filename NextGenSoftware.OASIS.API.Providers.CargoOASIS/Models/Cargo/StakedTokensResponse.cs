using System.Collections.Generic;
using Models.Cargo;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Cargo
{
    public class StakedTokensResponse
    {
        public string TotalStakedAmount { get; set; }
        public string TotalAvailableRewards { get; set; }
        public IEnumerable<Token> Tokens { get; set; }
    }
}