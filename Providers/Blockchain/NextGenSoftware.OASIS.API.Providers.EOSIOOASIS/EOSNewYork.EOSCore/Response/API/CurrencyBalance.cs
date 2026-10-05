using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EOSNewYork.EOSCore.Lib;

namespace EOSNewYork.EOSCore.Response.API
{
    public class CurrencyBalance : IEOAPI, IEOStringArray
    {
        public List<String> balances { get; set; }
        
        public EOSAPIMetadata GetMetaData()
        {
            var meta = new EOSAPIMetadata
            {
                uri = "/v1/chain/get_currency_balance"
            };

            return meta;
        }

        public void SetStringArray(List<String> array)
        {
            balances = array;
        }
    }
}
