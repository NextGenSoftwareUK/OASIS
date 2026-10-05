using System;
using System.IO;
using System.Text;
using Cryptography.ECDSA;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using EOSNewYork.EOSCore.Response.API;

namespace EOSNewYork.EOSCore.Serialization
{
    public class TransactionReceiptTrxArray : BaseCustomType
    {
        public TransactionReceiptTrx value { get; set; }

        public TransactionReceiptTrxArray() { }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JArray array = JArray.Load(reader);
            value = new TransactionReceiptTrx();
            value.index = array[0].Value<uint>();
            value.trx = array[1].ToObject<TransactionReceiptTrxInner>();
            return value;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            TransactionReceiptTrx transactionReceiptTrx = (TransactionReceiptTrx) value;
            JArray array = new JArray();
            array.Add(JToken.Parse(transactionReceiptTrx.index.ToString()));
            array.Add(JToken.Parse(JsonConvert.SerializeObject(transactionReceiptTrx.trx)));
            writer.WriteToken(array.CreateReader());
        }

        public override void WriteToStream(Stream stream)
        {
            throw new NotImplementedException(); 
        }
    }
}
