using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EOSNewYork.EOSCore.Serialization;
using Newtonsoft.Json;
using EOSNewYork.EOSCore.Lib;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace EOSNewYork.EOSCore.Response.API
{
    public class Block : IEOAPI
    {
        public string timestamp { get; set; }
        public DateTime timestamp_datetime
        {
            get
            {
                return DateTime.SpecifyKind((DateTime.Parse(timestamp)), DateTimeKind.Utc);
            }
        }
        public string producer { get; set; }
        public ushort confirmed { get; set; }
        public string previous { get; set; }
        public string transaction_mroot { get; set; }
        public string action_mroot { get; set; }
        public uint schedule_version { get; set; }
        public string producer_signature { get; set; }
        public List<Transaction> transactions { get; set; }
        public string id { get; set; }
        public uint block_num { get; set; }
        public uint ref_block_prefix { get; set; }
        public EOSAPIMetadata GetMetaData()
        {
            var meta = new EOSAPIMetadata
            {
                uri = "/v1/chain/get_block"
            };

            return meta;
        }
    }

    public class Transaction
    {
        public string status { get; set; }
        public uint cpu_usage_us { get; set; }
        public uint net_usage_words { get; set; }
        public Trx trx { get; set; }
        
    }

    [JsonConverter(typeof(TxnConverter))]
    public class Trx
    {
        public TrnType TransactionType { get; set; }
        public string id { get; set; }
        public List<string> signatures { get; set; }
        public string compression { get; set; }
        public string packed_context_free_data { get; set; }
        public List<string> context_free_data{ get; set; }
        public string packed_trx { get; set; }
        public TransactionInner transaction { get; set; }
    }

    public enum TrnType { Inline, Deferred };

    public class TransactionInner
    {
        public string expiration { get; set; }
        public ushort ref_block_num { get; set; }
        public uint ref_block_prefix { get; set; }
        public uint max_net_usage_words { get; set; }
        public byte max_cpu_usage_ms { get; set; }
        public uint delay_sec { get; set; }
        public List<Action> context_free_actions { get; set; }
        public List<Action> actions { get; set; }
        public Tuple<ushort, char[]>[] transaction_extensions { get; set; } = new Tuple<ushort, char[]>[0];
        public List<string> signatures { get; set; }
        public List<string> context_free_data { get; set; }
    }

    public class Authorization
    {
        public string actor { get; set; }
        public string permission { get; set; }
    }

    public class Action
    {
        public string account { get; set; }
        public string name { get; set; }
        public Authorization[] authorization { get; set; }
        [JsonConverter(typeof(JsonString))]
        public string data { get; set; }

        public string hex_data { get; set; }
    }


    public class TxnConverter : JsonConverter
    {

        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType.GetTypeInfo().IsClass;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            object instance = Activator.CreateInstance(objectType);
            var props = objectType.GetTypeInfo().DeclaredProperties.ToList();

            PropertyInfo type_prop = props.FirstOrDefault(pi => pi.CanWrite && pi.PropertyType == typeof(TrnType));
            var tokenType = reader.TokenType;
            if (tokenType != JsonToken.String)
            {
                //instance = serializer.Deserialize(reader, objectType);
                type_prop.SetValue(instance, TrnType.Inline);
                JObject jo = JObject.Load(reader);

                foreach (JProperty jp in jo.Properties())
                {
                    var name = jp.Name;
                    //PropertyInfo prop = props.FirstOrDefault(pi => pi.CanWrite && pi.GetCustomAttribute<JsonPropertyAttribute>().PropertyName == name);
                    PropertyInfo prop = props.FirstOrDefault(pi => pi.CanWrite && pi.Name == name);
                    prop?.SetValue(instance, jp.Value.ToObject(prop.PropertyType, serializer));
                }
            }
            else
            {
                type_prop.SetValue(instance, TrnType.Deferred);
                PropertyInfo id_prop = props.FirstOrDefault(pi => pi.CanWrite && pi.Name == "id");
                id_prop.SetValue(instance, reader.Value.ToString());
            }


       

            return instance;
            
//            return serializer.Deserialize(reader, objectType);
        }
    }


}