using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EOSNewYork.EOSCore.Utilities;

namespace EOSNewYork.EOSCore.Lib
{
    public class EOS_Table<T> where T : IEOSTable
    {
        public List<T> rows = new List<T>();
        public bool more;
        
        public List<T> GetRows()
        {
            return rows;
        }

        System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
        Logger logger = NLog.LogManager.GetCurrentClassLogger();
        List<EOS_Table<T>> subsets = new List<EOS_Table<T>>();
        Uri _uri;
        EOSTableMetadata _metadata;
        Type t = typeof(T);

        public EOS_Table()
        {
        }

        public EOS_Table(Uri host)
        {
            _uri = new Uri(host, "v1/chain/get_table_rows");
        }

        public EOS_Table(Uri host, EOSTableMetadata metaData)
        {
            _uri = new Uri(host, "v1/chain/get_table_rows");
            _metadata = metaData;
        }

        //This method takes all the subsets that were collected and merges them into a single table. 
        public List<T> Merge(String keyName)
        {
            Dictionary<String, bool> keysInUse = new Dictionary<string, bool>();
            PropertyInfo propertyInfo = typeof(T).GetProperty(keyName);

            foreach (var subset in subsets)
            {
                foreach (var item in subset.rows)
                {
                    
                    //Some results are tables but only have one entry and there's no point in defining a key. If it's blank, then just skip the duplicate check and add the record.
                    if(keyName != string.Empty)
                    {
                        var keyValue = propertyInfo.GetValue(item).ToString();

                        if (!keysInUse.ContainsKey(keyValue))
                        {
                            keysInUse.Add(keyValue, true);
                            rows.Add(item);
                        }
                        else
                        {
                            logger.Debug("Not adding duplicate key {0}", keyValue);
                        }
                    }
                    else
                    {
                        rows.Add(item);
                    }
                 }
            }

            return rows;
        }

        //This calls getDataSubset until there is no more data to fetch in the table. 
        //The first record of the next subset fetched is the same as the last recod of the previous subset so we need to trim that. 
        public async Task<List<T>> GetRowsFromAPIAsync(object settings = null)
        {
            watch.Start();

            int limit = 1000; // The max # of records to get with each request. 
            int maxRequests = 100000; // The max number of HTTP API requests to make .. 
            int requestCount = 0;

            bool more = true;
            var lower_bound = "";

            //We need to know what the name of the primary key propery is, as we'll use this field value and then use it as the lower_bound in future requests.
            T rowObjType;
            //If a settings object is supplied then we'll want to construct that object using this object
            if (settings != null)
            {
                rowObjType = (T)Activator.CreateInstance(typeof(T), settings);
            } else
            {
                rowObjType = (T)Activator.CreateInstance(typeof(T));
            }
            
            string keyName = rowObjType.GetMetaData().primaryKey;


            while (more)
            {
                logger.Debug("Get {0} records for data for Type {1}. Request {2}, lower_bound = {3} (API: {4}) - {5}", limit, t.ToString(), requestCount, lower_bound, _uri, watch.Elapsed);

                string startkey = string.Empty;
                var result = await GetDataSubset(lower_bound, limit, rowObjType);

                more = result.more;
                subsets.Add(result);

                // We only need to worry about setting the lower_bound if the table has more data. 
                if (more)
                {
                    var lastRecord = result.rows[result.rows.Count - 1];
                    PropertyInfo propertyInfo = typeof(T).GetProperty(keyName);
                    lower_bound = propertyInfo.GetValue(lastRecord).ToString();
                }

                requestCount++;
                if (requestCount > maxRequests)
                    more = false;

            }

            watch.Stop();
            logger.Debug("Done fetching ALL results. Total duration = {0}", watch.Elapsed);

            //Now that we've fetched all the subsets, merge then into one lage resultset and return to the user. 
            return Merge(keyName);

        }

        //This method fetches a specific subset of data. 
        async Task<EOS_Table<T>> GetDataSubset(string lower_bound, int limit, T rowObject)
        {
            if(_metadata == null)
            {
                //_metadata = ((T)Activator.CreateInstance(typeof(T))).GetMetaData();
                _metadata = rowObject.GetMetaData();
            }
            string key_type = _metadata.key_type;
            var content = string.Empty;
            if (String.IsNullOrEmpty(lower_bound))
            {
                content = string.Format("{{\"scope\":\"{0}\", \"code\":\"{1}\", \"table\":\"{2}\", \"json\": true, \"limit\":{5}}}", _metadata.scope, _metadata.contract, _metadata.table, lower_bound, "", limit);
            }
            else
            {
                if(string.IsNullOrEmpty(key_type))
                {
                    content = string.Format("{{\"scope\":\"{0}\", \"code\":\"{1}\", \"table\":\"{2}\", \"json\": true, \"lower_bound\":\"{3}\", \"upper_bound\":\"{4}\", \"limit\":{5}}}", _metadata.scope, _metadata.contract, _metadata.table, lower_bound, "", limit);
                }
                else
                {
                    content = string.Format("{{\"scope\":\"{0}\", \"code\":\"{1}\", \"table\":\"{2}\", \"json\": true, \"lower_bound\":\"{3}\", \"upper_bound\":\"{4}\", \"limit\":{5}, \"key_type\":\"{6}\"}}", _metadata.scope, _metadata.contract, _metadata.table, lower_bound, "", limit, key_type);
                }           
            }

            var responseString = await HttpUtility.GetValidatedAPIResponse(_uri, new StringContent(content));
            return JsonConvert.DeserializeObject<EOS_Table<T>>(responseString);
        }
    }
}
