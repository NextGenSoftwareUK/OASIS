using System;

namespace EOSNewYork.EOSCore
{
    public class BaseAPI
    {
        protected Uri HOST = new Uri("https://api.eosnewyork.io");

        public BaseAPI(){}

        public BaseAPI(string host)
        {
            HOST = new Uri(host);
        }
    }
}
