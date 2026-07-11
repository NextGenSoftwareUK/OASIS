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
            if (value == null) return;

            // Write index as VarUint32 (EOS packed varint encoding)
            var v = value.index;
            while (v >= 0x80)
            {
                stream.WriteByte((byte)(0x80 | (v & 0x7f)));
                v >>= 7;
            }
            stream.WriteByte((byte)v);

            // Write packed transaction binary data if present
            if (value.trx?.packed_trx != null)
            {
                var packedHex = value.trx.packed_trx;
                var bytes = new byte[packedHex.Length / 2];
                for (int i = 0; i < bytes.Length; i++)
                    bytes[i] = Convert.ToByte(packedHex.Substring(i * 2, 2), 16);
                stream.Write(bytes, 0, bytes.Length);
            }
        }
    }
}
