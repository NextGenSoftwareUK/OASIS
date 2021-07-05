using NLog;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EOSNewYork.EOSCore.Response.Table;
using EOSNewYork.EOSCore.Serialization;
using EOSNewYork.EOSCore.Lib;
using Newtonsoft.Json;

namespace EOSNewYork.EOSCore.Utilities
{
    public static class EOSUtility
    {
        static Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private static readonly DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static async Task<string> GetValidatedAPIResponse(HttpResponseMessage response)
        {
            var responseString = await response.Content.ReadAsStringAsync();
            
            if(response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.Accepted)
            {
                InternalServiceError error = JsonConvert.DeserializeObject<InternalServiceError>(responseString);
                if(error.code == 500)
                {
                    throw new Exception(string.Format("Error thrown from node. Code: {0}, Mesage: {1}", error.error.code, error.error.name));
                }
                else
                {
                    throw new Exception("API Call did not respond with 200 OK");
                }
            }
            return responseString;
        }
        public static DateTime EOSTimeToUTC(Int64 slotTime)
        {
            Int64 interval = 500;
            Int64 epoch = 946684800000;

            var unixEpochTime = (slotTime * interval + epoch) / 1000;
            var UTCTime = FromUnixTime(unixEpochTime);
            return UTCTime;
        }

        public static DateTime FromUnixTime(long unixTime, bool microseconds = false)
        {
            if(microseconds)
            {
                unixTime = unixTime / 1000000;
            }

            return epoch.AddSeconds(unixTime);
        }

        public static void UpdateProxyVotersWithProducerInfo(ref List<VoterRow> voters)
        {

            //logger.Debug("Correting proxy voter data");
            int proxyVoterCount = 0;
            // Loop through the full resultset and correct producer list on those that chose to vote via proxy
            foreach (var row in voters)
            {
                //If this voter voted by proxy
                if (!string.IsNullOrEmpty(row.proxy))
                {
                    proxyVoterCount++;
                    //Find the proxy that voted for this voter and link the same producers to this account. 
                    var proxyWhoVoted = voters.Find(x => x.owner.Equals(row.proxy));
                    row.producers = proxyWhoVoted.producers;
                }

            }

            logger.Debug("{0} proxy votes updated", proxyVoterCount);

        }

        public static decimal EosToDecimal(string eosString)
        {

                var clean_core_liquid_balance = string.Empty;
                if (eosString == null)
                    clean_core_liquid_balance = "0.0";
                else
                    clean_core_liquid_balance = eosString.Trim().Replace(" EOS", "");

                return decimal.Parse(clean_core_liquid_balance, System.Globalization.CultureInfo.InvariantCulture);
         
        }

        public static List<dynamic> FilterFields<T>(List<string> properties, List<T> data) where T: IEOSTable
        {
            List<dynamic> objList = new List<dynamic>();

            foreach (var item in data)
            {
                dynamic cleanObj = new ExpandoObject();
                var obj = (IDictionary<string, object>)cleanObj;

                foreach (var property in properties)
                {
                    var value = item.GetType().GetProperty(property).GetValue(item).ToString();
                    obj.Add(property, value);
                }
                objList.Add(obj);
            }

            return objList;
        }
    }

    public static class StringUtil
    {
        public static string ToCsv<T>(string separator, IEnumerable<T> objectlist)
        {
            Type t = typeof(T);
            FieldInfo[] fields = t.GetFields();

            string header = String.Join(separator, fields.Select(f => f.Name).ToArray());

            StringBuilder csvdata = new StringBuilder();
            csvdata.AppendLine(header);

            foreach (var o in objectlist)
                csvdata.AppendLine(ToCsvFields(separator, fields, o));

            return csvdata.ToString();
        }

        public static string ToCsvFields(string separator, FieldInfo[] fields, object o)
        {
            StringBuilder linie = new StringBuilder();

            foreach (var f in fields)
            {
                if (linie.Length > 0)
                    linie.Append(separator);

                var x = f.GetValue(o);

                if (x != null)
                    linie.Append(x.ToString());
            }

            return linie.ToString();
        }
    }
}
