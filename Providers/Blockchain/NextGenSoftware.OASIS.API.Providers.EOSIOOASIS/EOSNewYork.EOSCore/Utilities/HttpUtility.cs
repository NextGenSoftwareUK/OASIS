using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using EOSNewYork.EOSCore.Serialization;
using Newtonsoft.Json;

namespace EOSNewYork.EOSCore.Utilities
{
    public static class HttpUtility
    {
        // Best to use a global HTTP Client
        // https://aspnetmonsters.com/2016/08/2016-08-27-httpclientwrong/
        //private static HttpClient Client = new HttpClient();
        private static readonly HttpClient httpClient;
        static HttpUtility()
        {
            httpClient = new HttpClient();
        }
        public static async Task<string> GetValidatedAPIResponse(Uri host, StringContent postContent = null)
        {
            HttpResponseMessage response = null;
            if(postContent == null)
            {
                response = await httpClient.GetAsync(host);
            }
            else
            {
                response = await httpClient.PostAsync(host, postContent);
            }            
            var responseString = await response.Content.ReadAsStringAsync();
            if(response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.Accepted)
            {
                InternalServiceError error = JsonConvert.DeserializeObject<InternalServiceError>(responseString);
                if(error.code == 500)
                {
                    throw new Exception(string.Format("Error thrown from node. Code: {0}, Mesage: {1}. Detail: {2}", error.error.code, error.error.name, responseString));
                }
                else
                {
                    throw new Exception(string.Format("API Call did not respond with 200 OK. Detail {0}", responseString));
                }
            }
            return responseString;
        }
    }
}
