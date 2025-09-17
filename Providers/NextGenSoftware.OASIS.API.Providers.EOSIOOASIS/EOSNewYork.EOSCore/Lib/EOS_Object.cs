using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using EOSNewYork.EOSCore.Serialization;
using EOSNewYork.EOSCore.Utilities;

namespace EOSNewYork.EOSCore.Lib
{
    public class EOS_Object<T> where T : IEOAPI
    {
        Uri _host;
        Logger logger = NLog.LogManager.GetCurrentClassLogger();
        
        public EOS_Object(Uri host)
        {
            var ObjType = (T)Activator.CreateInstance(typeof(T));
            var meta = ObjType.GetMetaData();
            _host = new Uri(host, meta.uri);
        }
        
        public async Task<T> GetObjectsFromAPIAsync()
        {
            logger.Debug("HTTP GET: {0}", _host);

            var responseString = await HttpUtility.GetValidatedAPIResponse(_host);
            return JsonConvert.DeserializeObject<T>(responseString);
        }

        public async Task<T> GetObjectsFromAPIAsync(object postData)
        {
            string json = JsonConvert.SerializeObject(postData);
            logger.Debug("HTTP POST: {0}, DATA: {1}", _host, json);

            var responseString = await HttpUtility.GetValidatedAPIResponse(_host, new StringContent(json));
            return JsonConvert.DeserializeObject<T>(responseString);
        }
    }

}
