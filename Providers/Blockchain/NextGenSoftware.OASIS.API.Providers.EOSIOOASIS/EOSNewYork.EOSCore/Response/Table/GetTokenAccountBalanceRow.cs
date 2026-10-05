using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EOSNewYork.EOSCore.Lib;

namespace EOSNewYork.EOSCore.Response.Table
{

    public class GetTokenAccountBalanceConstructorSettings
    {
        public string tokenContract;
        public string accountName;
    }




    public class GetTokenAccountBalanceRow : IEOSTable
    {
        GetTokenAccountBalanceConstructorSettings settings;

        public GetTokenAccountBalanceRow(GetTokenAccountBalanceConstructorSettings settings)
        {
            this.settings = settings;
        }

        public decimal balance_decimal
        {
            get
            {
                return decimal.Parse(balance.Split(' ')[0], System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        public string symbol
        {
            get
            {
                return balance.Split(' ')[1];
            }
        }

        public string balance { get; set; }


        public EOSTableMetadata GetMetaData()
        {
            var meta = new EOSTableMetadata
            {
                primaryKey = "",
                contract = this.settings.tokenContract,
                scope = this.settings.accountName,
                table = "accounts",
                key_type = "name"
            };
            return meta;
        }
    }
}
