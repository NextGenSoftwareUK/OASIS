using System.Collections.Generic;

namespace Models.Cargo
{
    public class StakedTokensResponse
    {
        public string TotalStakedAmount { get; set; }
        public string TotalAvailableRewards { get; set; }
        public IEnumerable<Token> Tokens { get; set; }
    }
}