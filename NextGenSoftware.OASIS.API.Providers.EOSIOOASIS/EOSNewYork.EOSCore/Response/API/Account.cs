using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EOSNewYork.EOSCore.Lib;

namespace EOSNewYork.EOSCore.Response.API
{
    public class Account : IEOAPI
    {
        public string account_name { get; set; }
        public Int64 head_block_num { get; set; }
        public String head_block_time { get; set; }
        public DateTime head_block_time_datetime
        {
            get
            {
                return DateTime.SpecifyKind((DateTime.Parse(head_block_time)), DateTimeKind.Utc);
            }
        }

        public bool privileged { get; set; }
        public String last_code_update { get; set; }
        public DateTime last_code_update_datetime { get
            {
                return DateTime.SpecifyKind((DateTime.Parse(last_code_update)), DateTimeKind.Utc);
            }
        }

        public String created { get; set; }
        public DateTime created_datetime
        {
            get
            {
                return DateTime.SpecifyKind((DateTime.Parse(created)), DateTimeKind.Utc);
            }
        }

        public String core_liquid_balance { get; set; }
        public decimal core_liquid_balance_ulong { get {
                var clean_core_liquid_balance = string.Empty;
                if (core_liquid_balance == null)
                    clean_core_liquid_balance = "0.0";
                else
                    clean_core_liquid_balance = core_liquid_balance.Trim().Replace(" EOS", "");

                return decimal.Parse(clean_core_liquid_balance, System.Globalization.CultureInfo.InvariantCulture);
            }
        }
        public Int64 ram_quota { get; set; }
        public Int64 ram_usage { get; set; }
        public Int64 net_weight { get; set; }
        public Int64 cpu_weight { get; set; }
        public AccountLimit net_limit { get; set; }
        public AccountLimit cpu_limit { get; set; }
        //permissions here
        public AccountTotalResources total_resources { get; set; }
        public AccountSelfDelegatedBandwidth self_delegated_bandwidth { get; set; }
        public AccountRefundRequest refund_request { get; set; }
        public AccountVoterInfo voter_info { get; set; }
        public List<AccountPermission> permissions { get; set; }

        public EOSAPIMetadata GetMetaData()
        {
            var meta = new EOSAPIMetadata
            {
                uri = "v1/chain/get_account"
            };

            return meta;
        }
    }

    public class AccountLimit
    {
        public Int64 used { get; set; }
        public Int64 available { get; set; }
        public Int64 max { get; set; }
    }

    public class AccountTotalResources
    {
        public string owner { get; set; }
        public string net_weight { get; set; }
        public string cpu_weight { get; set; }
        public Int64 ram_bytes { get; set; }
    }

    public class AccountSelfDelegatedBandwidth
    {
        public string from { get; set; }
        public string to { get; set; }
        public string net_weight { get; set; }
        public string cpu_weight { get; set; }

        public decimal net_weight_decimal
        {
            get
            {
                string net_weight_clean = string.Empty;
                if (net_weight == null)
                    net_weight_clean = "0.0";
                else
                    net_weight_clean = net_weight.Trim().Replace(" EOS", "");

                return decimal.Parse(net_weight_clean, System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        public decimal cpu_weight_decimal
        {
            get
            {
                string cpu_weight_clean = string.Empty;
                if (cpu_weight == null)
                    cpu_weight_clean = "0.0";
                else
                    cpu_weight_clean = cpu_weight.Trim().Replace(" EOS", "");

                return decimal.Parse(cpu_weight_clean, System.Globalization.CultureInfo.InvariantCulture);
            }
        }
    }

    public class AccountVoterInfo
    {
        public string owner { get; set; }
        public string proxy { get; set; }
        public List<string> producers { get; set; }
        public Int64 staked { get; set; }
        public String last_vote_weight { get; set; }
        public String proxied_vote_weight { get; set; }
        public bool is_proxy { get; set; }
    }

    public class AccountPermission
    {
        public string parent { get; set; }
        public string perm_name { get; set; }
        public AccountPermissionRequiredAuth required_auth { get; set; }
    }

    public class AccountPermissionRequiredAuth
    {
        public List<AccountKey> keys { get; set; }
        public int threshold { get; set; }
    }

    public class AccountKey
    {
        public string key { get; set; }
        public int weight { get; set; }
    }

    public class AccountRefundRequest
    {
        public string owner { get; set; }
        public string request_time { get; set; }
        public string net_amount { get; set; }
        public string cpu_amount { get; set; }

        public decimal net_amount_decimal
        {
            get
            {
                string net_amount_clean = string.Empty;
                if (net_amount == null)
                    net_amount_clean = "0.0";
                else
                    net_amount_clean = net_amount.Trim().Replace(" EOS", "");

                return decimal.Parse(net_amount_clean, System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        public decimal cpu_amount_decimal
        {
            get
            {
                string cpu_amount_clean = string.Empty;
                if (cpu_amount == null)
                    cpu_amount_clean = "0.0";
                else
                    cpu_amount_clean = cpu_amount.Trim().Replace(" EOS", "");

                return decimal.Parse(cpu_amount_clean, System.Globalization.CultureInfo.InvariantCulture);
            }
        }
    }
}