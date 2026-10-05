using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EOSNewYork.EOSCore.Lib
{

    //This interface must be implemented by any class wishing to be treated as a row of an EOS table. 
    public interface IEOSTable
    {
        EOSTableMetadata GetMetaData();
    }

    //Defines the properties of a table. Any table implementing IEOSTable will need to return on of these in order to provide details of where table exists on the EOS chain. 
    public class EOSTableMetadata
    {
        public string primaryKey;
        public string contract;
        public string scope;
        public string table;
        public string key_type = string.Empty;

    }


    public interface IEOAPI
    {
        EOSAPIMetadata GetMetaData();
    }

    public class EOSAPIMetadata
    {
        public string uri;
    }

    //////////// Interface used for StringArray /////////////

    public interface IEOStringArray
    {
        void SetStringArray(List<String> array);
    }


}

