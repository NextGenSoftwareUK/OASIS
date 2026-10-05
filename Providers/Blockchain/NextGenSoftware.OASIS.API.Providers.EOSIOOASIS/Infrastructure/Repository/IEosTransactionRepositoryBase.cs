// using System;
// using System.Collections;
// using System.Collections.Generic;
// using System.Globalization;
// using System.IO;
// using System.Linq;
// using System.Reflection;
// using System.Runtime.Serialization.Formatters.Binary;
// using System.Security.Cryptography;
// using System.Text;
// using System.Text.RegularExpressions;
// using System.Threading.Tasks;
// using Cryptography.ECDSA;
// using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.GetBlock;
// using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.GetInfo;
// using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.GetRawAbi;
// using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.GetRequiredKeys;
// using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Infrastructure.EOSClient;
//
// namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Infrastructure.Repository
// {
//     /// <summary>
//     /// Data Attribute to map how the field is represented in the abi
//     /// </summary>
//     public class AbiFieldTypeAttribute : Attribute
//     {
//         public string AbiType { get; set; }
//
//         public AbiFieldTypeAttribute(string abiType)
//         {
//             AbiType = abiType;
//         }
//     }
//     
//     public class EosAbiType
//     {
//         public string NewTypeName;
//         public string Type;
//     }
//     
//     public class EosAbiField
//     {
//         public string Name;
//         public string Type;
//     }
//     [Serializable]
//     public class EosAbiStruct
//     {
//         public string Name;
//         public string Base;
//         public List<EosAbiField> Fields;
//     }
//     public class EosAbiAction
//     {
//         [AbiFieldType("name")]
//         public string Name;
//         public string Type;
//         public string RicardianContract;
//     }
//     public class EosAbiTable
//     {
//         [AbiFieldType("name")]
//         public string Name;
//         public string IndexType;
//         public List<string> KeyNames;
//         public List<string> KeyTypes;
//         public string Type;
//     }
//     
//     public class EosAbiRicardianClause
//     {
//         public string Id;
//         public string Body;
//     }
//     public class EosExtendedAsset
//     {
//         public string Quantity;
//         public string Contract;
//     }
//     public class EosSymbol
//     {
//         public string Name;
//         public byte Precision;
//     }
//
//     public class EosVariant
//     {
//         public string Name;
//         public List<string> Types;
//     }
//     
//     public class EosAbi
//     {
//         public string Version;
// 		
//         public List<EosAbiType> Types;
// 		
//         public List<EosAbiStruct> Structs;
// 		
//         public List<EosAbiAction> Actions;
// 		
//         public List<EosAbiTable> Tables;
// 		
//         public List<EosAbiRicardianClause> RicardianClauses;
// 		
//         public List<string> ErrorMessages;
// 		
//         public List<EosExtension> AbiExtensions;
// 		
//         public List<EosVariant> Variants;
//     }
//     
//     public class SignedEosTransaction
//     {
//         public IEnumerable<string> Signatures { get; set; }
//         public byte[] PackedTransaction { get; set; }
//     }
//     
//     public class EosExtension
//     {
// 	    public UInt16 Type { get; set; }
// 	    public byte[] Data { get; set; }
//     }
//     
//     public class EosPermissionLevel
//     {
// 	    public string Actor { get; set; }
// 	    public string Permission { get; set; }
//     }
//     
//     public class EosAction
//     {
// 	    public string Account { get; set; }
// 	    public string Name { get; set; }
// 	    public List<EosPermissionLevel> Authorization { get; set; }
// 	    public object Data { get; set; }
// 	    public string HexData { get; set; }
//     }
//     
//     public class EosTransaction
//     {
// 	    public DateTime Expiration { get; set; }
// 	    public UInt16 RefBlockNum { get; set; }
// 	    public UInt32 RefBlockPrefix { get; set; }
// 	    public UInt32 MaxNetUsageWords { get; set; }
// 	    public byte MaxCpuUsageMs { get; set; }
// 	    public UInt32 DelaySec { get; set; }
// 	    public List<EosAction> ContextFreeActions { get; set; }
// 	    public List<EosAction> Actions { get; set; }
// 	    public List<EosExtension> TransactionExtensions { get; set; }
//     }
//
//     public interface IEosTransactionRepositoryBase
//     {
//         public Task<SignedEosTransaction> SignTransaction(EosTransaction eosTransaction, List<string> requiredKeys = null);
//     }
//     
//     /// <summary>
//     /// Signature provider Interface to delegate multiple signing implementations
//     /// </summary>
//     public interface ISignProvider
//     {
//         /// <summary>
//         /// Get available public keys from signature provider
//         /// </summary>
//         /// <returns>List of public keys</returns>
//         Task<IEnumerable<string>> GetAvailableKeys();
//
//         /// <summary>
//         /// Sign bytes using the signature provider
//         /// </summary>
//         /// <param name="chainId">EOSIO Chain id</param>
//         /// <param name="requiredKeys">required public keys for signing this bytes</param>
//         /// <param name="signBytes">signature bytes</param>
//         /// <param name="abiNames">abi contract names to get abi information from</param>
//         /// <returns>List of signatures per required keys</returns>
//         Task<IEnumerable<string>> Sign(string chainId, IEnumerable<string> requiredKeys, byte[] signBytes, IEnumerable<string> abiNames = null);
//     }
//     
//     public class EosConfigurator
//     {
//         /// <summary>
//         /// http or https location of a nodeosd server providing a chain API.
//         /// </summary>
//         public string HttpEndpoint { get; set; } = "http://127.0.0.1:8888";
//         /// <summary>
//         /// unique ID for the blockchain you're connecting to. If no ChainId is provided it will get from the get_info API call.
//         /// </summary>
//         public string ChainId { get; set; }
//         /// <summary>
//         /// number of seconds before the transaction will expire. The time is based on the nodeosd's clock. 
//         /// An unexpired transaction that may have had an error is a liability until the expiration is reached, this time should be brief.
//         /// </summary>
//         public double ExpireSeconds { get; set; } = 60;
//
//         /// <summary>
//         /// How many blocks behind to use for TAPoS reference block
//         /// </summary>
//         public UInt32 BlocksBehind { get; set; } = 3;
//         /// <summary>
//         /// signature implementation to handle available keys and signing transactions. Use the DefaultSignProvider with a privateKey to sign transactions inside the lib.
//         /// </summary>
//         public ISignProvider SignProvider { get; set; }
//     }
//
//     public class SerializationHelper
//     {
//         /// <summary>
//         /// Is a big Number negative
//         /// </summary>
//         /// <param name="bin">big number in byte array</param>
//         /// <returns></returns>
//         public static bool IsNegative(byte[] bin)
//         {
//             return (bin[bin.Length - 1] & 0x80) != 0;
//         }
//
//         /// <summary>
//         /// Negate a big number
//         /// </summary>
//         /// <param name="bin">big number in byte array</param>
//         public static void Negate(byte[] bin)
//         {
//             int carry = 1;
//             for (int i = 0; i < bin.Length; ++i)
//             {
//                 int x = (~bin[i] & 0xff) + carry;
//                 bin[i] = (byte)x;
//                 carry = x >> 8;
//             }
//         }
//
//         /// <summary>
//         /// Convert an unsigned decimal number as string to a big number
//         /// </summary>
//         /// <param name="size">Size in bytes of the big number</param>
//         /// <param name="s">decimal encoded as string</param>
//         /// <returns></returns>
//         public static byte[] DecimalToBinary(uint size, string s)
//         {
//             byte[] result = new byte[size];
//             for (int i = 0; i < s.Length; ++i)
//             {
//                 char srcDigit = s[i];
//                 if (srcDigit < '0' || srcDigit > '9')
//                     throw new Exception("invalid number");
//                 int carry = srcDigit - '0';
//                 for (int j = 0; j < size; ++j)
//                 {
//                     int x = result[j] * 10 + carry;
//                     result[j] = (byte)x;
//                     carry = x >> 8;
//                 }
//                 if (carry != 0)
//                     throw new Exception("number is out of range");
//             }
//             return result;
//         }
//
//         /// <summary>
//         /// Convert an signed decimal number as string to a big number
//         /// </summary>
//         /// <param name="size">Size in bytes of the big number</param>
//         /// <param name="s">decimal encoded as string</param>
//         /// <returns></returns>
//         public static byte[] SignedDecimalToBinary(uint size, string s)
//         {
//             bool negative = s[0] == '-';
//             if (negative)
//                 s = s.Substring(0, 1);
//             byte[] result = DecimalToBinary(size, s);
//             if (negative)
//                 Negate(result);
//             return result;
//         }
//
//         /// <summary>
//         /// Convert big number to an unsigned decimal number
//         /// </summary>
//         /// <param name="bin">big number as byte array</param>
//         /// <param name="minDigits">0-pad result to this many digits</param>
//         /// <returns></returns>
//         public static string BinaryToDecimal(byte[] bin, int minDigits = 1)
//         {
//             var result = new List<char>(minDigits);
//
//             for (int i = 0; i < minDigits; i++)
//             {
//                 result.Add('0');
//             }
//
//             for (int i = bin.Length - 1; i >= 0; --i)
//             {
//                 int carry = bin[i];
//                 for (int j = 0; j < result.Count; ++j)
//                 {
//                     int x = ((result[j] - '0') << 8) + carry;
//                     result[j] = (char)('0' + (x % 10));
//                     carry = (x / 10) | 0;
//                 }
//                 while (carry != 0)
//                 {
//                     result.Add((char)('0' + carry % 10));
//                     carry = (carry / 10) | 0;
//                 }
//             }
//             result.Reverse();
//             return string.Join("", result);
//         }
//
//         /// <summary>
//         /// Convert big number to an signed decimal number
//         /// </summary>
//         /// <param name="bin">big number as byte array</param>
//         /// <param name="minDigits">0-pad result to this many digits</param>
//         /// <returns></returns>
//         public static string SignedBinaryToDecimal(byte[] bin, int minDigits = 1)
//         {
//             if (IsNegative(bin))
//             {
//                 Negate(bin);
//                 return '-' + BinaryToDecimal(bin, minDigits);
//             }
//             return BinaryToDecimal(bin, minDigits);
//         }
//
//         /// <summary>
//         /// Convert base64 with fc prefix to byte array
//         /// </summary>
//         /// <param name="s">string to convert</param>
//         /// <returns></returns>
//         public static byte[] Base64FcStringToByteArray(string s)
//         {
//             //fc adds extra '='
//             if((s.Length & 3) == 1 && s[s.Length - 1] == '=')
//             {
//                 return Convert.FromBase64String(s.Substring(0, s.Length - 1));
//             }
//
//             return Convert.FromBase64String(s);
//         }
//
//         /// <summary>
//         /// Convert ascii char to symbol value
//         /// </summary>
//         /// <param name="c"></param>
//         /// <returns></returns>
//         public static byte CharToSymbol(char c)
//         {
//             if (c >= 'a' && c <= 'z')
//                 return (byte)(c - 'a' + 6);
//             if (c >= '1' && c <= '5')
//                 return (byte)(c - '1' + 1);
//             return 0;
//         }
//
//         /// <summary>
//         /// Convert snake case string to pascal case
//         /// </summary>
//         /// <param name="s">string to convert</param>
//         /// <returns></returns>
//         public static string SnakeCaseToPascalCase(string s)
//         {
//             var result = s.ToLower().Replace("_", " ");
//             TextInfo info = CultureInfo.CurrentCulture.TextInfo;
//             result = info.ToTitleCase(result).Replace(" ", string.Empty);
//             return result;
//         }
//
//         /// <summary>
//         /// Convert pascal case string to snake case
//         /// </summary>
//         /// <param name="s">string to convert</param>
//         /// <returns></returns>
//         public static string PascalCaseToSnakeCase(string s)
//         {
//             if (string.IsNullOrEmpty(s))
//             {
//                 return s;
//             }
//
//             var builder = new StringBuilder();
//             bool first = true;
//             foreach(var c in s)
//             {
//                 if(char.IsUpper(c))
//                 {
//                     if (!first)
//                         builder.Append('_');
//                     builder.Append(char.ToLower(c));
//                 }
//                 else
//                 {
//                     builder.Append(c);
//                 }
//
//                 if (first)
//                     first = false;
//             }
//             return builder.ToString();
//         }
//
//         /// <summary>
//         /// Serialize object to byte array
//         /// </summary>
//         /// <param name="obj">object to serialize</param>
//         /// <returns></returns>
//         public static byte[] ObjectToByteArray(object obj)
//         {
//             if (obj == null)
//                 return null;
//
//             BinaryFormatter bf = new BinaryFormatter();
//             using (MemoryStream ms = new MemoryStream())
//             {
//                 bf.Serialize(ms, obj);
//                 return ms.ToArray();
//             }
//         }
//
//         /// <summary>
//         /// Encode byte array to hexadecimal string
//         /// </summary>
//         /// <param name="ba">byte array to convert</param>
//         /// <returns></returns>
//         public static string ByteArrayToHexString(byte[] ba)
//         {
//             StringBuilder hex = new StringBuilder(ba.Length * 2);
//             foreach (byte b in ba)
//                 hex.AppendFormat("{0:x2}", b);
//
//             return hex.ToString();
//         }
//
//         /// <summary>
//         /// Decode hexadecimal string to byte array
//         /// </summary>
//         /// <param name="hex"></param>
//         /// <returns></returns>
//         public static byte[] HexStringToByteArray(string hex)
//         {
//             var l = hex.Length / 2;
//             var result = new byte[l];
//             for (var i = 0; i < l; ++i)
//                 result[i] = (byte)Convert.ToInt32(hex.Substring(i * 2, 2), 16);
//             return result;
//         }
//
//         /// <summary>
//         /// Serialize object to hexadecimal encoded string
//         /// </summary>
//         /// <param name="obj"></param>
//         /// <returns></returns>
//         public static string ObjectToHexString(object obj)
//         {
//             return ByteArrayToHexString(ObjectToByteArray(obj));
//         }
//
//         /// <summary>
//         /// Combina multiple arrays into one
//         /// </summary>
//         /// <param name="arrays"></param>
//         /// <returns></returns>
//         public static byte[] Combine(IEnumerable<byte[]> arrays)
//         {
//             byte[] ret = new byte[arrays.Sum(x => x != null ? x.Length : 0)];
//             int offset = 0;
//             foreach (byte[] data in arrays)
//             {
//                 if (data == null) continue;
//
//                 Buffer.BlockCopy(data, 0, ret, offset, data.Length);
//                 offset += data.Length;
//             }
//             return ret;
//         }
//
//         /// <summary>
//         /// Convert DateTime to `time_point` (miliseconds since epoch)
//         /// </summary>
//         /// <param name="value">date to convert</param>
//         /// <returns></returns>
//         public static UInt64 DateToTimePoint(DateTime value)
//         {
//             var span = (value - new DateTime(1970, 1, 1));
//             return (UInt64)(span.Ticks / TimeSpan.TicksPerMillisecond);
//         }
//
//         /// <summary>
//         /// Convert `time_point` (miliseconds since epoch) to DateTime
//         /// </summary>
//         /// <param name="ticks">time_point ticks to convert</param>
//         /// <returns></returns>
//         public static DateTime TimePointToDate(long ticks)
//         {
//             return new DateTime(ticks + new DateTime(1970, 1, 1).Ticks);
//         }
//
//         /// <summary>
//         /// Convert DateTime to `time_point_sec` (seconds since epoch)
//         /// </summary>
//         /// <param name="value">date to convert</param>
//         /// <returns></returns>
//         public static UInt32 DateToTimePointSec(DateTime value)
//         {
//             var span = (value - new DateTime(1970, 1, 1));
//             return (UInt32)((span.Ticks / TimeSpan.TicksPerSecond) & 0xffffffff);
//         }
//
//         /// <summary>
//         /// Convert `time_point_sec` (seconds since epoch) to DateTime
//         /// </summary>
//         /// <param name="secs">time_point_sec to convert</param>
//         /// <returns></returns>
//         public static DateTime TimePointSecToDate(UInt32 secs)
//         {
//             return new DateTime(secs * TimeSpan.TicksPerSecond + new DateTime(1970, 1, 1).Ticks);
//         }
//
//         /// <summary>
//         /// Convert DateTime to `block_timestamp_type` (half-seconds since a different epoch)
//         /// </summary>
//         /// <param name="value">date to convert</param>
//         /// <returns></returns>
//         public static UInt32 DateToBlockTimestamp(DateTime value)
//         { 
//             var span = (value - new DateTime(1970, 1, 1));
//             return (UInt32)((UInt64)Math.Round((double)(span.Ticks / TimeSpan.TicksPerMillisecond - 946684800000) / 500) & 0xffffffff);
//         }
//
//         /// <summary>
//         /// Convert `block_timestamp_type` (half-seconds since a different epoch) to DateTime
//         /// </summary>
//         /// <param name="slot">block_timestamp slot to convert</param>
//         /// <returns></returns>
//         public static DateTime BlockTimestampToDate(UInt32 slot)
//         {
//             return new DateTime(slot * TimeSpan.TicksPerMillisecond * 500 + 946684800000 + new DateTime(1970, 1, 1).Ticks);
//         }
//
//         /// <summary>
//         /// Convert Name into unsigned long
//         /// </summary>
//         /// <param name="name"></param>
//         /// <returns>Converted value</returns>
//         public static UInt64 ConvertNameToLong(string name)
//         {
//             return BitConverter.ToUInt64(ConvertNameToBytes(name), 0);
//         }
//
//         /// <summary>
//         /// Convert Name into bytes
//         /// </summary>
//         /// <param name="name"></param>
//         /// <returns>Converted value bytes</returns>
//         public static byte[] ConvertNameToBytes(string name)
//         {
//             var a = new byte[8];
//             Int32 bit = 63;
//             for (int i = 0; i < name.Length; ++i)
//             {
//                 var c = SerializationHelper.CharToSymbol(name[i]);
//                 if (bit < 5)
//                     c = (byte)(c << 1);
//                 for (int j = 4; j >= 0; --j)
//                 {
//                     if (bit >= 0)
//                     {
//                         a[(int)Math.Floor((decimal)(bit / 8))] |= (byte)(((c >> j) & 1) << (bit % 8));
//                         --bit;
//                     }
//                 }
//             }
//             return a;
//         }
//
//
//         public static string ReverseHex(string h)
//         {
//             return h.Substring(6, 2) + h.Substring(4, 2) + h.Substring(2, 2) + h.Substring(0, 2);
//         }
//     }
//     
//     /// <summary>
//     /// Serialize / deserialize transaction and fields using a Abi schema
//     /// https://developers.eos.io/eosio-home/docs/the-abi
//     /// </summary>
//     public class AbiSerializationProvider
//     {
//         private enum KeyType
//         {
//             K1 = 0,
//             R1 = 1,
//         };
//
//         private delegate object ReaderDelegate(byte[] data, ref int readIndex);
//
//         private IEosClient _eosClient { get; set; }
//         private Dictionary<string, Action<MemoryStream, object>> TypeWriters { get; set; }
//         private Dictionary<string, ReaderDelegate> TypeReaders { get; set; }
//
//         /// <summary>
//         /// Construct abi serialization provided using EOS api
//         /// </summary>
//         /// <param name="eosClient"></param>
//         public AbiSerializationProvider(IEosClient eosClient)
//         {
//             this._eosClient = eosClient;
//
//             TypeWriters = new Dictionary<string, Action<MemoryStream, object>>()
//             {     
//                 {"int8",                 WriteByte               },
//                 {"uint8",                WriteByte               },
//                 {"int16",                WriteUint16             },
//                 {"uint16",               WriteUint16             },
//                 {"int32",                WriteUint32             },
//                 {"uint32",               WriteUint32             },
//                 {"int64",                WriteInt64              },
//                 {"uint64",               WriteUint64             },
//                 {"int128",               WriteInt128             },
//                 {"uint128",              WriteUInt128            },
//                 {"varuint32",            WriteVarUint32          },
//                 {"varint32",             WriteVarInt32           },
//                 {"float32",              WriteFloat32            },
//                 {"float64",              WriteFloat64            },
//                 {"float128",             WriteFloat128           },
//                 {"bytes",                WriteBytes              },
//                 {"bool",                 WriteBool               },
//                 {"string",               WriteString             },
//                 {"name",                 WriteName               },
//                 {"asset",                WriteAsset              },
//                 {"time_point",           WriteTimePoint          },
//                 {"time_point_sec",       WriteTimePointSec       },
//                 {"block_timestamp_type", WriteBlockTimestampType },
//                 {"symbol_code",          WriteSymbolCode         },
//                 {"symbol",               WriteSymbolString       },
//                 {"checksum160",          WriteChecksum160        },
//                 {"checksum256",          WriteChecksum256        },
//                 {"checksum512",          WriteChecksum512        },
//                 {"public_key",           WritePublicKey          },
//                 {"private_key",          WritePrivateKey         },
//                 {"signature",            WriteSignature          },
//                 {"extended_asset",       WriteExtendedAsset      }
//             };
//
//             TypeReaders = new Dictionary<string, ReaderDelegate>()
//             {
//                 {"int8",                 ReadByte               },
//                 {"uint8",                ReadByte               },
//                 {"int16",                ReadUint16             },
//                 {"uint16",               ReadUint16             },
//                 {"int32",                ReadUint32             },
//                 {"uint32",               ReadUint32             },
//                 {"int64",                ReadInt64              },
//                 {"uint64",               ReadUint64             },
//                 {"int128",               ReadInt128             },
//                 {"uint128",              ReadUInt128            },
//                 {"varuint32",            ReadVarUint32          },
//                 {"varint32",             ReadVarInt32           },
//                 {"float32",              ReadFloat32            },
//                 {"float64",              ReadFloat64            },
//                 {"float128",             ReadFloat128           },
//                 {"bytes",                ReadBytes              },
//                 {"bool",                 ReadBool               },
//                 {"string",               ReadString             },
//                 {"name",                 ReadName               },
//                 {"asset",                ReadAsset              },
//                 {"time_point",           ReadTimePoint          },
//                 {"time_point_sec",       ReadTimePointSec       },
//                 {"block_timestamp_type", ReadBlockTimestampType },
//                 {"symbol_code",          ReadSymbolCode         },
//                 {"symbol",               ReadSymbolString       },
//                 {"checksum160",          ReadChecksum160        },
//                 {"checksum256",          ReadChecksum256        },
//                 {"checksum512",          ReadChecksum512        },
//                 {"public_key",           ReadPublicKey          },
//                 {"private_key",          ReadPrivateKey         },
//                 {"signature",            ReadSignature          },
//                 {"extended_asset",       ReadExtendedAsset      }
//             };
//         }
//
//         /// <summary>
//         /// Serialize transaction to packed asynchronously
//         /// </summary>
//         /// <param name="trx">transaction to pack</param>
//         /// <returns></returns>
//         public async Task<byte[]> SerializePackedTransaction(EosTransaction eosTransaction)
//         {
//             int actionIndex = 0;
//             var abiResponses = await GetTransactionAbis(eosTransaction);
//
//             using (MemoryStream ms = new MemoryStream())
//             {
//                 //trx headers
//                 WriteUint32(ms, SerializationHelper.DateToTimePointSec(eosTransaction.Expiration));
//                 WriteUint16(ms, eosTransaction.RefBlockNum);
//                 WriteUint32(ms, eosTransaction.RefBlockPrefix);
//
//                 //trx info
//                 WriteVarUint32(ms, eosTransaction.MaxNetUsageWords);
//                 WriteByte(ms, eosTransaction.MaxCpuUsageMs);
//                 WriteVarUint32(ms, eosTransaction.DelaySec);
//
//                 WriteVarUint32(ms, (UInt32)eosTransaction.ContextFreeActions.Count);
//                 foreach (var action in eosTransaction.ContextFreeActions)
//                 {
//                     WriteAction(ms, action, abiResponses[actionIndex++]);
//                 }
//
//                 WriteVarUint32(ms, (UInt32)eosTransaction.Actions.Count);
//                 foreach (var action in eosTransaction.Actions)
//                 {
//                     WriteAction(ms, action, abiResponses[actionIndex++]);
//                 }
//
//                 WriteVarUint32(ms, (UInt32)eosTransaction.TransactionExtensions.Count);
//                 foreach (var extension in eosTransaction.TransactionExtensions)
//                 {
//                     WriteExtension(ms, extension);
//                 }
//
//                 return ms.ToArray();
//             }
//         }
//
//         /// <summary>
//         /// Deserialize packed transaction asynchronously
//         /// </summary>
//         /// <param name="packtrx">hex encoded strinh with packed transaction</param>
//         /// <returns></returns>
//         public async Task<EosTransaction> DeserializePackedTransaction(string packtrx)
//         {
//             var data = SerializationHelper.HexStringToByteArray(packtrx);
//             int readIndex = 0;
//
//             var trx = new EosTransaction()
//             {
//                 Expiration = (DateTime)ReadTimePointSec(data, ref readIndex),
//                 RefBlockNum = (UInt16)ReadUint16(data, ref readIndex),
//                 RefBlockPrefix = (UInt32)ReadUint32(data, ref readIndex),
//                 MaxNetUsageWords = (UInt32)ReadVarUint32(data, ref readIndex),
//                 MaxCpuUsageMs = (byte)ReadByte(data, ref readIndex),
//                 DelaySec = (UInt32)ReadVarUint32(data, ref readIndex),
//             };
//
//             var contextFreeActionsSize = Convert.ToInt32(ReadVarUint32(data, ref readIndex));
//             trx.ContextFreeActions = new List<EosAction>(contextFreeActionsSize);
//
//             for (int i = 0; i < contextFreeActionsSize; i++)
//             {
//                 var action = (EosAction)ReadActionHeader(data, ref readIndex);
//                 EosAbi eosAbi = await GetAbi(action.Account);
//
//                 trx.ContextFreeActions.Add((EosAction)ReadAction(data, action, eosAbi, ref readIndex));
//             }
//
//             var actionsSize = Convert.ToInt32(ReadVarUint32(data, ref readIndex));
//             trx.Actions = new List<EosAction>(actionsSize);
//
//             for (int i = 0; i < actionsSize; i++)
//             {
//                 var action = (EosAction)ReadActionHeader(data, ref readIndex);
//                 EosAbi eosAbi = await GetAbi(action.Account);
//
//                 trx.Actions.Add((EosAction)ReadAction(data, action, eosAbi, ref readIndex));
//             }
//
//             return trx;
//         }
//
//         /// <summary>
//         /// Deserialize packed abi
//         /// </summary>
//         /// <param name="packabi">string encoded abi</param>
//         /// <returns></returns>
//         public EosAbi DeserializePackedAbi(string packabi)
//         {
//             var data = SerializationHelper.Base64FcStringToByteArray(packabi);
//             int readIndex = 0;
//
//             return new EosAbi()
//             {
//                 Version = (string)ReadString(data, ref readIndex),
//                 Types = ReadType<List<EosAbiType>>(data, ref readIndex),
//                 Structs = ReadType<List<EosAbiStruct>>(data, ref readIndex),
//                 Actions = ReadAbiActionList(data, ref readIndex),
//                 Tables = ReadAbiTableList(data, ref readIndex),
//                 RicardianClauses = ReadType<List<EosAbiRicardianClause>>(data, ref readIndex),
//                 ErrorMessages = ReadType<List<string>>(data, ref readIndex),
//                 AbiExtensions = ReadType<List<EosExtension>>(data, ref readIndex),
//                 Variants = ReadType<List<EosVariant>>(data, ref readIndex)
//             };
//         }
//
//         /// <summary>
//         /// Serialize action to packed action data
//         /// </summary>
//         /// <param name="action">action to pack</param>
//         /// <param name="eosAbi">abi schema to look action structure</param>
//         /// <returns></returns>
//         public byte[] SerializeActionData(EosAction action, EosAbi eosAbi)
//         {
//             var abiAction = eosAbi.Actions.FirstOrDefault(aa => aa.Name == action.Name);
//             
//             if (abiAction == null)
//                 throw new ArgumentException(string.Format("action name {0} not found on abi.", action.Name));
//
//             var abiStruct = eosAbi.Structs.FirstOrDefault(s => s.Name == abiAction.Type);
//
//             if (abiStruct == null)
//                 throw new ArgumentException(string.Format("struct type {0} not found on abi.", abiAction.Type));
//
//             using (MemoryStream ms = new MemoryStream())
//             {
//                 WriteAbiStruct(ms, action.Data, abiStruct, eosAbi);
//                 return ms.ToArray();
//             }
//         }
//
//         /// <summary>
//         /// Deserialize structure data as "Dictionary<string, object>"
//         /// </summary>
//         /// <param name="structType">struct type in abi</param>
//         /// <param name="dataHex">data to deserialize</param>
//         /// <param name="eosAbi">abi schema to look for struct type</param>
//         /// <returns></returns>
//         public Dictionary<string, object> DeserializeStructData(string structType, string dataHex, EosAbi eosAbi)
//         {
//             return DeserializeStructData<Dictionary<string, object>>(structType, dataHex, eosAbi);
//         }
//
//         /// <summary>
//         /// Deserialize structure data with generic TStructData type
//         /// </summary>
//         /// <typeparam name="TStructData">deserialization struct data type</typeparam>
//         /// <param name="structType">struct type in abi</param>
//         /// <param name="dataHex">data to deserialize</param>
//         /// <param name="eosAbi">abi schema to look for struct type</param>
//         /// <returns></returns>
//         public TStructData DeserializeStructData<TStructData>(string structType, string dataHex, EosAbi eosAbi)
//         {
//             var data = SerializationHelper.HexStringToByteArray(dataHex);
//             var abiStruct = eosAbi.Structs.First(s => s.Name == structType);
//             int readIndex = 0;
//             return ReadAbiStruct<TStructData>(data, abiStruct, eosAbi, ref readIndex);
//         }
//
//         /// <summary>
//         /// Get abi schemas used in transaction
//         /// </summary>
//         /// <param name="eosTransaction"></param>
//         /// <returns></returns>
//         public Task<EosAbi[]> GetTransactionAbis(EosTransaction eosTransaction)
//         {
//             var abiTasks = new List<Task<EosAbi>>();
//
//             foreach (var action in eosTransaction.ContextFreeActions)
//             {
//                 abiTasks.Add(GetAbi(action.Account));
//             }
//
//             foreach (var action in eosTransaction.Actions)
//             {
//                 abiTasks.Add(GetAbi(action.Account));
//             }
//
//             return Task.WhenAll(abiTasks);
//         }
//
//         /// <summary>
//         /// Get abi schema by contract account name
//         /// </summary>
//         /// <param name="accountName">account name</param>
//         /// <returns></returns>
//         public async Task<EosAbi> GetAbi(string accountName)
//         {
//             var result = await _eosClient.GetRawAbi(new GetRawAbiRequestDto()
//             {
//                  AccountName = accountName
//             });
//
//             return DeserializePackedAbi(result.Abi);
//         }
//
//         /// <summary>
//         /// Deserialize type by encoded string data
//         /// </summary>
//         /// <typeparam name="T"></typeparam>
//         /// <param name="dataHex"></param>
//         /// <returns></returns>
//         public T DeserializeType<T>(string dataHex)
//         {
//             return DeserializeType<T>(SerializationHelper.HexStringToByteArray(dataHex));
//         }
//
//         /// <summary>
//         /// Deserialize type by binary data
//         /// </summary>
//         /// <typeparam name="T"></typeparam>
//         /// <param name="data"></param>
//         /// <returns></returns>
//         public T DeserializeType<T>(byte[] data)
//         {
//             int readIndex = 0;
//             return ReadType<T>(data, ref readIndex);
//         }
//
//         #region Writer Functions
//
//         private static void WriteByte(MemoryStream ms, object value)
//         {
//             ms.Write(new byte[] { Convert.ToByte(value) }, 0, 1);
//         }
//         
//         private static void WriteUint16(MemoryStream ms, object value)
//         {
//             ms.Write(BitConverter.GetBytes(Convert.ToUInt16(value)), 0, 2);
//         }
//
//         private static void WriteUint32(MemoryStream ms, object value)
//         {
//
//             ms.Write(BitConverter.GetBytes(Convert.ToUInt32(value)), 0, 4);
//         }
//
//         private static void WriteInt64(MemoryStream ms, object value)
//         {
//             var decimalBytes = SerializationHelper.SignedDecimalToBinary(8, value.ToString());
//             ms.Write(decimalBytes, 0, decimalBytes.Length);
//         }
//
//         private static void WriteUint64(MemoryStream ms, object value)
//         {
//             var decimalBytes = SerializationHelper.DecimalToBinary(8, value.ToString());
//             ms.Write(decimalBytes, 0, decimalBytes.Length);
//         }
//
//         private static void WriteInt128(MemoryStream ms, object value)
//         {
//             var decimalBytes = SerializationHelper.SignedDecimalToBinary(16, value.ToString());
//             ms.Write(decimalBytes, 0, decimalBytes.Length);
//         }
//
//         private static void WriteUInt128(MemoryStream ms, object value)
//         {
//             var decimalBytes = SerializationHelper.DecimalToBinary(16, value.ToString());
//             ms.Write(decimalBytes, 0, decimalBytes.Length);
//         }
//
//         private static void WriteVarUint32(MemoryStream ms, object value)
//         {
//             var v = Convert.ToUInt32(value);
//             while (true)
//             {
//                 if ((v >> 7) != 0)
//                 {
//                     ms.Write(new byte[] { (byte)(0x80 | (v & 0x7f)) }, 0, 1);
//                     v >>= 7;
//                 }
//                 else
//                 {
//                     ms.Write(new byte[] { (byte)(v) }, 0, 1);
//                     break;
//                 }
//             }
//         }
//
//         private static void WriteVarInt32(MemoryStream ms, object value)
//         {
//             var n = Convert.ToInt32(value);
//             WriteVarUint32(ms, (UInt32)((n << 1) ^ (n >> 31)));
//         }
//
//         private static void WriteFloat32(MemoryStream ms, object value)
//         {
//             ms.Write(BitConverter.GetBytes(Convert.ToSingle(value)), 0, 4);
//         }
//
//         private static void WriteFloat64(MemoryStream ms, object value)
//         {
//             ms.Write(BitConverter.GetBytes(Convert.ToDouble(value)), 0, 8);
//         }
//
//         private static void WriteFloat128(MemoryStream ms, object value)
//         {
//             Int32[] bits = decimal.GetBits(Convert.ToDecimal(value));
//             List<byte> bytes = new List<byte>();
//             foreach (Int32 i in bits)
//             {
//                 bytes.AddRange(BitConverter.GetBytes(i));
//             }
//
//             ms.Write(bytes.ToArray(), 0, 16);
//         }
//
//         private static void WriteBytes(MemoryStream ms, object value)
//         {
//             var bytes = (byte[])value;
//
//             WriteVarUint32(ms, (UInt32)bytes.Length);
//             ms.Write(bytes, 0, bytes.Length);
//         }
//
//         private static void WriteBool(MemoryStream ms, object value)
//         {
//             WriteByte(ms, (bool)value ? 1 : 0);
//         }
//
//         private static void WriteString(MemoryStream ms, object value)
//         {
//             var strBytes = Encoding.UTF8.GetBytes((string)value);
//             WriteVarUint32(ms, (UInt32)strBytes.Length);
//             if (strBytes.Length > 0)
//                 ms.Write(strBytes, 0, strBytes.Length); 
//         }
//
//         private static void WriteName(MemoryStream ms, object value)
//         {
//             var a = SerializationHelper.ConvertNameToBytes((string)value);
//             ms.Write(a, 0, 8);
//         }
//
//         private static void WriteAsset(MemoryStream ms, object value)
//         {
//             var s = ((string)value).Trim();
//             Int32 pos = 0;
//             string amount = "";
//             byte precision = 0;
//
//             if (s[pos] == '-')
//             {
//                 amount += '-';
//                 ++pos;
//             }
//
//             bool foundDigit = false;
//             while (pos < s.Length && s[pos] >= '0' && s[pos] <= '9')
//             {
//                 foundDigit = true;
//                 amount += s[pos];
//                 ++pos;
//             }
//
//             if (!foundDigit)
//                 throw new Exception("Asset must begin with a number");
//
//             if (s[pos] == '.')
//             {
//                 ++pos;
//                 while (pos < s.Length && s[pos] >= '0' && s[pos] <= '9')
//                 {
//                     amount += s[pos];
//                     ++precision;
//                     ++pos;
//                 }
//             }
//
//             string name = s.Substring(pos).Trim();
//
//             var decimalBytes = SerializationHelper.SignedDecimalToBinary(8, amount);
//             ms.Write(decimalBytes, 0, decimalBytes.Length);
//             WriteSymbol(ms, new EosSymbol() { Name = name, Precision = precision });
//         }
//
//         private static void WriteTimePoint(MemoryStream ms, object value)
//         {
//             var ticks = SerializationHelper.DateToTimePoint((DateTime)value);
//             WriteUint32(ms, (UInt32)(ticks & 0xffffffff));
//             WriteUint32(ms, (UInt32)Math.Floor((double)ticks / 0x100000000));
//         }
//
//         private static void WriteTimePointSec(MemoryStream ms, object value)
//         {
//             WriteUint32(ms, SerializationHelper.DateToTimePointSec((DateTime)value));
//         }
//
//         private static void WriteBlockTimestampType(MemoryStream ms, object value)
//         {
//             WriteUint32(ms, SerializationHelper.DateToBlockTimestamp((DateTime)value));
//         }
//
//         private static void WriteSymbolString(MemoryStream ms, object value)
//         {
//             Regex r = new Regex("^([0-9]+),([A-Z]+)$", RegexOptions.IgnoreCase);
//             Match m = r.Match((string)value);
//
//             if (!m.Success)
//                 throw new Exception("Invalid symbol.");
//
//             WriteSymbol(ms, new EosSymbol() { Name = m.Groups[2].ToString(), Precision = byte.Parse(m.Groups[1].ToString()) });
//         }
//
//         private static void WriteSymbolCode(MemoryStream ms, object value)
//         {
//             var name = (string)value;
//
//             if (name.Length > 8)
//                 ms.Write(Encoding.UTF8.GetBytes(name.Substring(0, 8)), 0, 8);
//             else
//             {
//                 ms.Write(Encoding.UTF8.GetBytes(name), 0, name.Length);
//
//                 if (name.Length < 8)
//                 {
//                     var fill = new byte[8 - name.Length];
//                     for (int i = 0; i < fill.Length; i++)
//                         fill[i] = 0;
//                     ms.Write(fill, 0, fill.Length);
//                 }
//             }
//         }
//
//         private static void WriteChecksum160(MemoryStream ms, object value)
//         {
//             var bytes = SerializationHelper.HexStringToByteArray((string)value);
//
//             if (bytes.Length != 20)
//                 throw new Exception("Binary data has incorrect size");
//
//             ms.Write(bytes, 0, bytes.Length);
//         }
//
//         private static void WriteChecksum256(MemoryStream ms, object value)
//         {
//             var bytes = SerializationHelper.HexStringToByteArray((string)value);
//
//             if (bytes.Length != 32)
//                 throw new Exception("Binary data has incorrect size");
//
//             ms.Write(bytes, 0, bytes.Length);
//         }
//
//         private static void WriteChecksum512(MemoryStream ms, object value)
//         {
//             var bytes = SerializationHelper.HexStringToByteArray((string)value);
//
//             if (bytes.Length != 64)
//                 throw new Exception("Binary data has incorrect size");
//
//             ms.Write(bytes, 0, bytes.Length);
//         }
//         
//         private static void WritePublicKey(MemoryStream ms, object value)
//         {
//             var s = (string)value;
//             var keyBytes = CryptoHelper.PubKeyStringToBytes(s);
//
//             WriteByte(ms, s.StartsWith("PUB_R1_") ? KeyType.R1 : KeyType.K1);
//             ms.Write(keyBytes, 0, CryptoHelper.PUB_KEY_DATA_SIZE);
//         }
//
//         private static void WritePrivateKey(MemoryStream ms, object value)
//         {
//             var s = (string)value;
//             var keyBytes = CryptoHelper.PrivKeyStringToBytes(s);
//             WriteByte(ms, KeyType.R1);
//             ms.Write(keyBytes, 0, CryptoHelper.PRIV_KEY_DATA_SIZE);
//         }
//
//         private static void WriteSignature(MemoryStream ms, object value)
//         {
//             var s = (string)value;
//             var signBytes = CryptoHelper.SignStringToBytes(s);
//             
//             if (s.StartsWith("SIG_K1_"))
//                 WriteByte(ms, KeyType.K1);
//             else if (s.StartsWith("SIG_R1_"))
//                 WriteByte(ms, KeyType.R1);
//
//             ms.Write(signBytes, 0, CryptoHelper.SIGN_KEY_DATA_SIZE);
//         }
//
//         private static void WriteExtendedAsset(MemoryStream ms, object value)
//         {
//             var extAsset = (EosExtendedAsset)value;
//             WriteAsset(ms, extAsset.Quantity);
//             WriteName(ms, extAsset.Contract);
//         }
//
//         private static void WriteSymbol(MemoryStream ms, object value)
//         {
//             var symbol = (EosSymbol)value;
//
//             WriteByte(ms, symbol.Precision);
//
//             if (symbol.Name.Length > 7)
//                 ms.Write(Encoding.UTF8.GetBytes(symbol.Name.Substring(0, 7)), 0, 7);
//             else
//             {
//                 ms.Write(Encoding.UTF8.GetBytes(symbol.Name), 0, symbol.Name.Length);
//
//                 if (symbol.Name.Length < 7)
//                 {
//                     var fill = new byte[7 - symbol.Name.Length];
//                     for (int i = 0; i < fill.Length; i++)
//                         fill[i] = 0;
//                     ms.Write(fill, 0, fill.Length);
//                 }
//             }
//         }
//
//         private static void WriteExtension(MemoryStream ms, EosExtension extension)
//         {
//             if (extension.Data == null)
//                 return;
//
//             WriteUint16(ms, extension.Type);
//             WriteBytes(ms, extension.Data);
//         }
//
//         private static void WritePermissionLevel(MemoryStream ms, EosPermissionLevel perm)
//         {
//             WriteName(ms, perm.Actor);
//             WriteName(ms, perm.Permission);
//         }
//
//         private void WriteAction(MemoryStream ms, EosAction action, EosAbi eosAbi)
//         {
//             WriteName(ms, action.Account);
//             WriteName(ms, action.Name);
//
//             WriteVarUint32(ms, (UInt32)action.Authorization.Count);
//             foreach (var perm in action.Authorization)
//             {
//                 WritePermissionLevel(ms, perm);
//             }
//
//             WriteBytes(ms, SerializeActionData(action, eosAbi));
//         }
//
//         private void WriteAbiType(MemoryStream ms, object value, string type, EosAbi eosAbi, bool isBinaryExtensionAllowed)
//         {
//             var uwtype = UnwrapTypeDef(eosAbi, type);
//
//             // binary extension type
//             if(uwtype.EndsWith("$"))
//             {
//                 if (!isBinaryExtensionAllowed) throw new Exception("Binary Extension type not allowed.");
//                 WriteAbiType(ms, value, uwtype.Substring(0, uwtype.Length - 1), eosAbi, isBinaryExtensionAllowed);
//
//                 return;
//             }
//
//             //optional type
//             if (uwtype.EndsWith("?"))
//             {
//                 if(value != null)
//                 {
//                     WriteByte(ms, 1);
//                     type = uwtype.Substring(0, uwtype.Length - 1);
//                 }
//                 else
//                 {
//                     WriteByte(ms, 0);
//                     return;
//                 }
//             }
//
//             // array type
//             if(uwtype.EndsWith("[]"))
//             {
//                 var items = (ICollection)value;
//                 var arrayType = uwtype.Substring(0, uwtype.Length - 2);
//
//                 WriteVarUint32(ms, items.Count);
//                 foreach (var item in items)
//                     WriteAbiType(ms, item, arrayType, eosAbi, false);
//
//                 return;
//             }
//
//             var writer = GetTypeSerializerAndCache(type, TypeWriters, eosAbi);
//             if (writer != null)
//             {
//                 writer(ms, value);
//                 return;
//             }
//
//             var abiStruct = eosAbi.Structs.FirstOrDefault(s => s.Name == uwtype);
//             if (abiStruct != null)
//             {
//                 WriteAbiStruct(ms, value, abiStruct, eosAbi);
//                 return;
//             }
//
//             var abiVariant = eosAbi.Variants.FirstOrDefault(v => v.Name == uwtype);
//             if (abiVariant != null)
//             {
//                 WriteAbiVariant(ms, value, abiVariant, eosAbi, isBinaryExtensionAllowed);
//             }
//             else
//             {
//                 throw new Exception("Type supported writer not found.");
//             }
//         }
//
//         private void WriteAbiStruct(MemoryStream ms, object value, EosAbiStruct eosAbiStruct, EosAbi eosAbi)
//         {
//             if (value == null)
//                 return;
//
//             if(!string.IsNullOrWhiteSpace(eosAbiStruct.Base))
//             {
//                 WriteAbiType(ms, value, eosAbiStruct.Base, eosAbi, true);
//             }
//
//             if(value is System.Collections.IDictionary)
//             {
//                 var skippedBinaryExtension = false;
//                 var valueDict = value as System.Collections.IDictionary;
//                 foreach (var field in eosAbiStruct.Fields)
//                 {
//                     var fieldName = FindObjectFieldName(field.Name, valueDict);
//
//                     if (string.IsNullOrWhiteSpace(fieldName))
//                     {
//                         if (field.Type.EndsWith("$"))
//                         {
//                             skippedBinaryExtension = true;
//                             continue;
//                         }
//
//                         throw new Exception("Missing " + eosAbiStruct.Name + "." + field.Name + " (type=" + field.Type + ")");
//                     }
//                     else if (skippedBinaryExtension)
//                     {
//                         throw new Exception("Unexpected " + eosAbiStruct.Name + "." + field.Name + " (type=" + field.Type + ")");
//                     }
//
//                     WriteAbiType(ms, valueDict[fieldName], field.Type, eosAbi, true);
//                 }
//             }
//             else
//             {
//                 var valueType = value.GetType();
//                 foreach (var field in eosAbiStruct.Fields)
//                 {
//                     var fieldInfo = valueType.GetField(field.Name);
//
//                     if(fieldInfo != null)
//                         WriteAbiType(ms, fieldInfo.GetValue(value), field.Type, eosAbi, true);
//                     else
//                     {
//                         var propInfo = valueType.GetProperty(field.Name);
//
//                         if(propInfo != null)
//                             WriteAbiType(ms, propInfo.GetValue(value), field.Type, eosAbi, true);
//                         else
//                             throw new Exception("Missing " + eosAbiStruct.Name + "." + field.Name + " (type=" + field.Type + ")");
//
//                     }
//                 }
//             }
//         }
//
//         private void WriteAbiVariant(MemoryStream ms, object value, EosVariant abiEosVariant, EosAbi eosAbi, bool isBinaryExtensionAllowed)
//         {
//             var variantValue = (KeyValuePair<string, object>)value;
//             var i = abiEosVariant.Types.IndexOf(variantValue.Key);
//             if (i < 0)
//             {
//                 throw new Exception("type " + variantValue.Key + " is not valid for variant");
//             }
//             WriteVarUint32(ms, i);
//             WriteAbiType(ms, variantValue.Value, variantValue.Key, eosAbi, isBinaryExtensionAllowed);
//         }
//
//         private string UnwrapTypeDef(EosAbi eosAbi, string type)
//         {
//             var wtype = eosAbi.Types.FirstOrDefault(t => t.NewTypeName == type);
//             if(wtype != null && wtype.Type != type)
//             {
//                 return UnwrapTypeDef(eosAbi, wtype.Type);
//             }
//
//             return type;
//         }
//
//         private TSerializer GetTypeSerializerAndCache<TSerializer>(string type, Dictionary<string, TSerializer> typeSerializers, EosAbi eosAbi)
//         {
//             TSerializer nativeSerializer;
//             if (typeSerializers.TryGetValue(type, out nativeSerializer))
//             {
//                 return nativeSerializer;
//             }
//
//             var abiTypeDef = eosAbi.Types.FirstOrDefault(t => t.NewTypeName == type);
//
//             if(abiTypeDef != null)
//             {
//                 var serializer = GetTypeSerializerAndCache(abiTypeDef.Type, typeSerializers, eosAbi);
//
//                 if(serializer != null)
//                 {
//                     typeSerializers.Add(type, serializer);
//                     return serializer;
//                 }
//             }
//
//             return default(TSerializer);
//         }
//     #endregion
//
//     #region Reader Functions
//     private object ReadByte(byte[] data, ref int readIndex)
//         {
//             return data[readIndex++];
//         }
//
//         private object ReadUint16(byte[] data, ref int readIndex)
//         {
//             var value = BitConverter.ToUInt16(data, readIndex);
//             readIndex += 2;
//             return value;
//         }
//
//         private object ReadUint32(byte[] data, ref int readIndex)
//         {
//             var value = BitConverter.ToUInt32(data, readIndex);
//             readIndex += 4;
//             return value;
//         }
//
//         private object ReadInt64(byte[] data, ref int readIndex)
//         {
//             var value = (Int64)BitConverter.ToUInt64(data, readIndex);
//             readIndex += 8;
//             return value;
//         }
//
//         private object ReadUint64(byte[] data, ref int readIndex)
//         {
//             var value = BitConverter.ToUInt64(data, readIndex);
//             readIndex += 8;
//             return value;
//         }
//
//         private object ReadInt128(byte[] data, ref int readIndex)
//         {
//             byte[] amount = data.Skip(readIndex).Take(16).ToArray();
//             readIndex += 16;
//             return SerializationHelper.SignedBinaryToDecimal(amount);
//         }
//
//         private object ReadUInt128(byte[] data, ref int readIndex)
//         {
//             byte[] amount = data.Skip(readIndex).Take(16).ToArray();
//             readIndex += 16;
//             return SerializationHelper.BinaryToDecimal(amount);
//         }
//
//         private object ReadVarUint32(byte[] data, ref int readIndex)
//         {
//             uint v = 0;
//             int bit = 0;
//             while (true)
//             {
//                 byte b = data[readIndex++];
//                 v |= (uint)((b & 0x7f) << bit);
//                 bit += 7;
//                 if ((b & 0x80) == 0)
//                     break;
//             }
//             return v >> 0;
//         }
//
//         private object ReadVarInt32(byte[] data, ref int readIndex)
//         {
//             var v = (UInt32)ReadVarUint32(data, ref readIndex);
//
//             if ((v & 1) != 0)
//                 return ((~v) >> 1) | 0x80000000;
//             else
//                 return v >> 1;
//         }
//
//         private object ReadFloat32(byte[] data, ref int readIndex)
//         {
//             var value = BitConverter.ToSingle(data, readIndex);
//             readIndex += 4;
//             return value;
//         }
//
//         private object ReadFloat64(byte[] data, ref int readIndex)
//         {
//             var value = BitConverter.ToDouble(data, readIndex);
//             readIndex += 8;
//             return value;
//         }
//
//         private object ReadFloat128(byte[] data, ref int readIndex)
//         {
//             var a = data.Skip(readIndex).Take(16).ToArray();
//             var value = SerializationHelper.ByteArrayToHexString(a);
//             readIndex += 16;
//             return value;
//         }
//
//         private object ReadBytes(byte[] data, ref int readIndex)
//         {
//             var size = Convert.ToInt32(ReadVarUint32(data, ref readIndex));
//             var value = data.Skip(readIndex).Take(size).ToArray();
//             readIndex += size;
//             return value;
//         }
//
//         private object ReadBool(byte[] data, ref int readIndex)
//         {
//             return (byte)ReadByte(data, ref readIndex) == 1;
//         }
//
//         private object ReadString(byte[] data, ref int readIndex)
//         {
//             var size = Convert.ToInt32(ReadVarUint32(data, ref readIndex));
//             string value = null;
//             if (size > 0)
//             {
//                 value = Encoding.UTF8.GetString(data.Skip(readIndex).Take(size).ToArray());
//                 readIndex += size;
//             }
//             return value;
//         }
//
//         private object ReadName(byte[] data, ref int readIndex)
//         {
//             byte[] a = data.Skip(readIndex).Take(8).ToArray();
//             string result = "";
//
//             readIndex += 8;
//
//             for (int bit = 63; bit >= 0;)
//             {
//                 int c = 0;
//                 for (int i = 0; i < 5; ++i)
//                 {
//                     if (bit >= 0)
//                     {
//                         c = (c << 1) | ((a[(int)Math.Floor((double)bit / 8)] >> (bit % 8)) & 1);
//                         --bit;
//                     }
//                 }
//                 if (c >= 6)
//                     result += (char)(c + 'a' - 6);
//                 else if (c >= 1)
//                     result += (char)(c + '1' - 1);
//                 else
//                     result += '.';
//             }
//
//             if (result == ".............")
//                 return result;
//
//             while (result.EndsWith("."))
//                 result = result.Substring(0, result.Length - 1);
//
//             return result;
//         }
//
//         private object ReadAsset(byte[] data, ref int readIndex)
//         {
//             byte[] amount = data.Skip(readIndex).Take(8).ToArray();
//
//             readIndex += 8;
//
//             var symbol = (EosSymbol)ReadSymbol(data, ref readIndex);
//             string s = SerializationHelper.SignedBinaryToDecimal(amount, symbol.Precision + 1);
//
//             if (symbol.Precision > 0)
//                 s = s.Substring(0, s.Length - symbol.Precision) + '.' + s.Substring(s.Length - symbol.Precision);
//
//             return s + ' ' + symbol.Name;
//         }
//
//         private object ReadTimePoint(byte[] data, ref int readIndex)
//         {
//             var low = (UInt32)ReadUint32(data, ref readIndex);
//             var high = (UInt32)ReadUint32(data, ref readIndex);
//             return SerializationHelper.TimePointToDate((high >> 0) * 0x100000000 + (low >> 0));
//         }
//
//         private object ReadTimePointSec(byte[] data, ref int readIndex)
//         {
//             var secs = (UInt32)ReadUint32(data, ref readIndex);
//             return SerializationHelper.TimePointSecToDate(secs);
//         }
//
//         private object ReadBlockTimestampType(byte[] data, ref int readIndex)
//         {
//             var slot = (UInt32)ReadUint32(data, ref readIndex);
//             return SerializationHelper.BlockTimestampToDate(slot);
//         }
//
//         private object ReadSymbolString(byte[] data, ref int readIndex)
//         {
//             var value = (EosSymbol)ReadSymbol(data, ref readIndex);
//             return value.Precision + ',' + value.Name;
//         }
//
//         private object ReadSymbolCode(byte[] data, ref int readIndex)
//         {
//             byte[] a = data.Skip(readIndex).Take(8).ToArray();
//
//             readIndex += 8;
//
//             int len;
//             for (len = 0; len < a.Length; ++len)
//                 if (a[len] == 0)
//                     break;
//
//             return string.Join("", a.Take(len));
//         }
//
//         private object ReadChecksum160(byte[] data, ref int readIndex)
//         {
//             var a = data.Skip(readIndex).Take(20).ToArray();
//             var value = SerializationHelper.ByteArrayToHexString(a);
//             readIndex += 20;
//             return value;
//         }
//
//         private object ReadChecksum256(byte[] data, ref int readIndex)
//         {
//             var a = data.Skip(readIndex).Take(32).ToArray();
//             var value = SerializationHelper.ByteArrayToHexString(a);
//             readIndex += 32;
//             return value;
//         }
//
//         private object ReadChecksum512(byte[] data, ref int readIndex)
//         {
//             var a = data.Skip(readIndex).Take(64).ToArray();
//             var value = SerializationHelper.ByteArrayToHexString(a);
//             readIndex += 64;
//             return value;
//         }
//
//         private object ReadPublicKey(byte[] data, ref int readIndex)
//         {
//             var type = (byte)ReadByte(data, ref readIndex);
//             var keyBytes = data.Skip(readIndex).Take(CryptoHelper.PUB_KEY_DATA_SIZE).ToArray();
//
//             readIndex += CryptoHelper.PUB_KEY_DATA_SIZE;
//
//             if(type == (int)KeyType.K1)
//             {
//                 return CryptoHelper.PubKeyBytesToString(keyBytes, "K1");
//             }
//             if (type == (int)KeyType.R1)
//             {
//                 return CryptoHelper.PubKeyBytesToString(keyBytes, "R1", "PUB_R1_");
//             }
//             else
//             {
//                 throw new Exception("public key type not supported.");
//             }
//         }
//
//         private object ReadPrivateKey(byte[] data, ref int readIndex)
//         {
//             var type = (byte)ReadByte(data, ref readIndex);
//             var keyBytes = data.Skip(readIndex).Take(CryptoHelper.PRIV_KEY_DATA_SIZE).ToArray();
//
//             readIndex += CryptoHelper.PRIV_KEY_DATA_SIZE;
//
//             if (type == (int)KeyType.R1)
//             {
//                 return CryptoHelper.PrivKeyBytesToString(keyBytes, "R1", "PVT_R1_");
//             }
//             else
//             {
//                 throw new Exception("private key type not supported.");
//             }
//         }
//
//         private object ReadSignature(byte[] data, ref int readIndex)
//         {
//             var type = (byte)ReadByte(data, ref readIndex);
//             var signBytes = data.Skip(readIndex).Take(CryptoHelper.SIGN_KEY_DATA_SIZE).ToArray();
//
//             readIndex += CryptoHelper.SIGN_KEY_DATA_SIZE;
//
//             if (type == (int)KeyType.R1)
//             {
//                 return CryptoHelper.SignBytesToString(signBytes, "R1", "SIG_R1_");
//             }
//             else if (type == (int)KeyType.K1)
//             {
//                 return CryptoHelper.SignBytesToString(signBytes, "K1", "SIG_K1_");
//             }
//             else
//             {
//                 throw new Exception("signature type not supported.");
//             }
//         }
//
//         private object ReadExtendedAsset(byte[] data, ref int readIndex)
//         {
//             return new EosExtendedAsset()
//             {
//                 Quantity = (string)ReadAsset(data, ref readIndex),
//                 Contract = (string)ReadName(data, ref readIndex)
//             };
//         }
//
//         private object ReadSymbol(byte[] data, ref int readIndex)
//         {
//             var value = new EosSymbol
//             {
//                 Precision = (byte)ReadByte(data, ref readIndex)
//             };
//
//             byte[] a = data.Skip(readIndex).Take(7).ToArray();
//
//             readIndex += 7;
//
//             int len;
//             for (len = 0; len < a.Length; ++len)
//                 if (a[len] == 0)
//                     break;
//
//             value.Name = string.Join("", a.Take(len).Select(b => (char)b));
//
//             return value;
//         }
//
//         private object ReadPermissionLevel(byte[] data, ref int readIndex)
//         {
//             var value = new EosPermissionLevel()
//             {
//                 Actor = (string)ReadName(data, ref readIndex),
//                 Permission = (string)ReadName(data, ref readIndex),
//             };
//             return value;
//         }
//
//         private object ReadActionHeader(byte[] data, ref int readIndex)
//         {
//             return new EosAction()
//             {
//                 Account = (string)ReadName(data, ref readIndex),
//                 Name = (string)ReadName(data, ref readIndex)
//             };
//         }
//
//         private object ReadAction(byte[] data, EosAction action, EosAbi eosAbi, ref int readIndex)
//         {
//             if (action == null)
//                 throw new ArgumentNullException("action");
//
//             var size = Convert.ToInt32(ReadVarUint32(data, ref readIndex));
//
//             action.Authorization = new List<EosPermissionLevel>(size);
//             for (var i = 0; i < size ; i++)
//             {
//                 action.Authorization.Add((EosPermissionLevel)ReadPermissionLevel(data, ref readIndex));
//             }
//
//             var abiAction = eosAbi.Actions.First(aa => aa.Name == action.Name);
//             var abiStruct = eosAbi.Structs.First(s => s.Name == abiAction.Type);
//
//             var dataSize = Convert.ToInt32(ReadVarUint32(data, ref readIndex));
//
//             action.Data = ReadAbiStruct(data, abiStruct, eosAbi, ref readIndex);
//
//             return action;
//         }
//
//         private List<EosAbiAction> ReadAbiActionList(byte[] data, ref int readIndex)
//         {
//             var size = Convert.ToInt32(ReadVarUint32(data, ref readIndex));
//             List<EosAbiAction> items = new List<EosAbiAction>();
//
//             for (int i = 0; i < size; i++)
//             {
//                 items.Add(new EosAbiAction() {
//                     Name = (string)TypeReaders["name"](data, ref readIndex),
//                     Type = (string)TypeReaders["string"](data, ref readIndex),
//                     RicardianContract = (string)TypeReaders["string"](data, ref readIndex)
//                 });
//             }
//
//             return items;
//         }
//
//         private List<EosAbiTable> ReadAbiTableList(byte[] data, ref int readIndex)
//         {
//             var size = Convert.ToInt32(ReadVarUint32(data, ref readIndex));
//             List<EosAbiTable> items = new List<EosAbiTable>();
//
//             for (int i = 0; i < size; i++)
//             {
//                 items.Add(new EosAbiTable()
//                 {
//                     Name = (string)TypeReaders["name"](data, ref readIndex),
//                     IndexType = (string)TypeReaders["string"](data, ref readIndex),
//                     KeyNames = ReadType<List<string>>(data, ref readIndex),
//                     KeyTypes = ReadType<List<string>>(data, ref readIndex),
//                     Type = (string)TypeReaders["string"](data, ref readIndex)
//                 });
//             }
//
//             return items;
//         }
//
//         private object ReadAbiType(byte[] data, string type, EosAbi eosAbi, ref int readIndex, bool isBinaryExtensionAllowed)
//         {
//             var uwtype = UnwrapTypeDef(eosAbi, type);
//
//             // binary extension type
//             if(uwtype.EndsWith("$"))
//             {
//                 if (!isBinaryExtensionAllowed) throw new Exception("Binary Extension type not allowed.");
//                 return ReadAbiType(data, uwtype.Substring(0, uwtype.Length - 1), eosAbi, ref readIndex, isBinaryExtensionAllowed);
//             }
//
//             //optional type
//             if (uwtype.EndsWith("?"))
//             {
//                 var opt = (byte)ReadByte(data, ref readIndex);
//
//                 if (opt == 0)
//                 {
//                     return null;
//                 }
//             }
//
//             // array type
//             if (uwtype.EndsWith("[]"))
//             {
//                 var arrayType = uwtype.Substring(0, uwtype.Length - 2);
//                 var size = Convert.ToInt32(ReadVarUint32(data, ref readIndex));
//                 var items = new List<object>(size);
//
//                 for (int i = 0; i < size; i++)
//                 {
//                     items.Add(ReadAbiType(data, arrayType, eosAbi, ref readIndex, false));
//                 }
//
//                 return items;
//             }
//
//             var reader = GetTypeSerializerAndCache(type, TypeReaders, eosAbi);
//             if (reader != null)
//             {
//                 return reader(data, ref readIndex);
//             }
//
//             var abiStruct = eosAbi.Structs.FirstOrDefault(s => s.Name == uwtype);
//             if (abiStruct != null)
//             {
//                 return ReadAbiStruct(data, abiStruct, eosAbi, ref readIndex);
//             }
//
//             var abiVariant = eosAbi.Variants.FirstOrDefault(v => v.Name == uwtype);
//             if(abiVariant != null)
//             {
//                 return ReadAbiVariant(data, abiVariant, eosAbi, ref readIndex, isBinaryExtensionAllowed);
//             }
//             else
//             {
//                 throw new Exception("Type supported writer not found.");
//             }
//         }
//
//         private object ReadAbiStruct(byte[] data, EosAbiStruct eosAbiStruct, EosAbi eosAbi, ref int readIndex)
//         {
//             return ReadAbiStruct<Dictionary<string, object>>(data, eosAbiStruct, eosAbi, ref readIndex);
//         }
//
//         private T ReadAbiStruct<T>(byte[] data, EosAbiStruct eosAbiStruct, EosAbi eosAbi, ref int readIndex)
//         {
//             object value = default(T);
//
//             if (!string.IsNullOrWhiteSpace(eosAbiStruct.Base))
//             {
//                 value = (T)ReadAbiType(data, eosAbiStruct.Base, eosAbi, ref readIndex, true);
//             }
//             else
//             {
//                 value = Activator.CreateInstance(typeof(T));
//             }
//
//             if (value is IDictionary<string, object>)
//             {
//                 var valueDict = value as IDictionary<string, object>;
//                 foreach (var field in eosAbiStruct.Fields)
//                 {
//                     var abiValue = ReadAbiType(data, field.Type, eosAbi, ref readIndex, true);
//                     if (field.Type.EndsWith("$") && abiValue == null) break;
//                     valueDict.Add(field.Name, abiValue);
//                 }
//             }
//             else
//             {
//                 var valueType = value.GetType();
//                 foreach (var field in eosAbiStruct.Fields)
//                 {
//                     var abiValue = ReadAbiType(data, field.Type, eosAbi, ref readIndex, true);
//                     if (field.Type.EndsWith("$") && abiValue == null) break;
//                     var fieldName = FindObjectFieldName(field.Name, value.GetType());
//                     valueType.GetField(fieldName).SetValue(value, abiValue);
//                 }
//             }
//
//             return (T)value;
//         }
//
//         private object ReadAbiVariant(byte[] data, EosVariant abiEosVariant, EosAbi eosAbi, ref int readIndex, bool isBinaryExtensionAllowed)
//         {
//             var i = (Int32)ReadVarUint32(data, ref readIndex);
//             if (i >= abiEosVariant.Types.Count)
//             {
//                 throw new Exception("type index " + i + " is not valid for variant");
//             }
//             var type = abiEosVariant.Types[i];
//             return new KeyValuePair<string, object>(abiEosVariant.Name, ReadAbiType(data, type, eosAbi, ref readIndex, isBinaryExtensionAllowed));
//         }
//
//         private T ReadType<T>(byte[] data, ref int readIndex)
//         {
//             return (T)ReadType(data, typeof(T), ref readIndex);
//         }
//
//         private object ReadType(byte[] data, Type objectType, ref int readIndex)
//         {
//             if (IsCollection(objectType))
//             {
//                 return ReadCollectionType(data, objectType, ref readIndex);
//             }
//             else if (IsOptional(objectType))
//             {
//                 var opt = (byte)ReadByte(data, ref readIndex);
//                 if (opt == 1)
//                 {
//                     var optionalType = GetFirstGenericType(objectType);
//                     return ReadType(data, optionalType, ref readIndex);
//                 }
//             }
//             else if (IsPrimitive(objectType))
//             {
//                 var readerName = GetNormalizedReaderName(objectType);
//                 return TypeReaders[readerName](data, ref readIndex);
//             }
//
//             var value = Activator.CreateInstance(objectType);
//
//             foreach (var member in objectType.GetFields())
//             {
//                 if (IsCollection(member.FieldType))
//                 {
//                     objectType.GetField(member.Name).SetValue(value, ReadCollectionType(data, member.FieldType, ref readIndex));
//                 }
//                 else if(IsOptional(member.FieldType))
//                 {
//                     var opt = (byte)ReadByte(data, ref readIndex);
//                     if (opt == 1)
//                     {
//                         var optionalType = GetFirstGenericType(member.FieldType);
//                         objectType.GetField(member.Name).SetValue(value, ReadType(data, optionalType, ref readIndex));
//                     }
//                 }
//                 else if (IsPrimitive(member.FieldType))
//                 {
//                     var readerName = GetNormalizedReaderName(member.FieldType, member.GetCustomAttributes());
//                     objectType.GetField(member.Name).SetValue(value, TypeReaders[readerName](data, ref readIndex));
//                 }
//                 else
//                 {
//                     objectType.GetField(member.Name).SetValue(value, ReadType(data, member.FieldType, ref readIndex));
//                 }
//             }
//
//             return value;
//         }
//
//         private IList ReadCollectionType(byte[] data, Type objectType, ref int readIndex)
//         {
//             var collectionType = GetFirstGenericType(objectType);
//             var size = Convert.ToInt32(ReadVarUint32(data, ref readIndex));
//             IList items = (IList)Activator.CreateInstance(objectType);
//
//             for (int i = 0; i < size; i++)
//             {
//                 items.Add(ReadType(data, collectionType, ref readIndex));
//             }
//
//             return items;
//         }
//
//         private static bool IsCollection(Type type)
//         {
//             return type.Name.StartsWith("List");
//         }
//
//         private static bool IsOptional(Type type)
//         {
//             return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
//         }
//
//         private static Type GetFirstGenericType(Type type)
//         {
//             return type.GetGenericArguments().First();
//         }
//
//         private static bool IsPrimitive(Type type)
//         {
//             return type.IsPrimitive ||                   
//                    type.Name.ToLower() == "string" ||
//                    type.Name.ToLower() == "byte[]";
//         }
//
//         private static string GetNormalizedReaderName(Type type, IEnumerable<Attribute> customAttributes = null)
//         {
//             if(customAttributes != null)
//             {
//                 var abiFieldAttr = (AbiFieldTypeAttribute)customAttributes.FirstOrDefault(attr => attr.GetType() == typeof(AbiFieldTypeAttribute));
//                 if (abiFieldAttr != null)
//                 {
//                     return abiFieldAttr.AbiType;
//                 }
//                     
//             }
//
//             var typeName = type.Name.ToLower();
//
//             if (typeName == "byte[]")
//                 return "bytes";
//             else if (typeName == "boolean")
//                 return "bool";
//
//             return typeName;
//         }
//
//         private string FindObjectFieldName(string name, System.Collections.IDictionary value)
//         {
//             if (value.Contains(name))
//                 return name;
//
//             name = SerializationHelper.SnakeCaseToPascalCase(name);
//
//             if (value.Contains(name))
//                 return name;
//
//             name = SerializationHelper.PascalCaseToSnakeCase(name);
//
//             if (value.Contains(name))
//                 return name;
//
//             return null;
//         }
//
//         private string FindObjectFieldName(string name, Type objectType)
//         {
//             if (objectType.GetFields().Any(p => p.Name == name))
//                 return name;
//
//             name = SerializationHelper.SnakeCaseToPascalCase(name);
//
//             if (objectType.GetFields().Any(p => p.Name == name))
//                 return name;
//
//             name = SerializationHelper.PascalCaseToSnakeCase(name);
//
//             if (objectType.GetFields().Any(p => p.Name == name))
//                 return name;
//
//             return null;
//         }
//         #endregion
//     }
//     
//     /// <summary>
//     /// Signature provider that combine multiple signature providers to complete all the signatures for a transaction
//     /// </summary>
//     public class CombinedSignersProvider : ISignProvider
//     {
//         private List<ISignProvider> Signers { get; set; }
//         
//         /// <summary>
//         /// Creates the provider with a list of signature providers
//         /// </summary>
//         /// <param name="signers"></param>
//         public CombinedSignersProvider(List<ISignProvider> signers)
//         {
//             if (signers == null || signers.Count == 0)
//                 throw new ArgumentNullException("Required atleast one signer.");
//
//             Signers = signers;
//         }
//
//         /// <summary>
//         /// Get available public keys from the list of signature providers
//         /// </summary>
//         /// <returns>List of public keys</returns>
//         public async Task<IEnumerable<string>> GetAvailableKeys()
//         {
//             var availableKeysListTasks = Signers.Select(s => s.GetAvailableKeys());
//             var availableKeysList = await Task.WhenAll(availableKeysListTasks);
//             return availableKeysList.SelectMany(k => k).Distinct();
//         }
//
//         /// <summary>
//         /// Sign bytes using the list of signature providers
//         /// </summary>
//         /// <param name="chainId">EOSIO Chain id</param>
//         /// <param name="requiredKeys">required public keys for signing this bytes</param>
//         /// <param name="signBytes">signature bytes</param>
//         /// <param name="abiNames">abi contract names to get abi information from</param>
//         /// <returns>List of signatures per required keys</returns>
//         public async Task<IEnumerable<string>> Sign(string chainId, IEnumerable<string> requiredKeys, byte[] signBytes, IEnumerable<string> abiNames = null)
//         {
//             var signatureTasks = Signers.Select(s => s.Sign(chainId, requiredKeys, signBytes, abiNames));
//             var signatures = await Task.WhenAll(signatureTasks);
//             return signatures.SelectMany(k => k).Distinct();
//         }
//     }
//     
//     /// <summary>
//     /// Helper class with crypto functions
//     /// </summary>
//     public class CryptoHelper
//     {
//         public static readonly int PUB_KEY_DATA_SIZE = 33;
//         public static readonly int PRIV_KEY_DATA_SIZE = 32;
//         public static readonly int SIGN_KEY_DATA_SIZE = 64;
//
//         /// <summary>
//         /// KeyPair with a private and public key
//         /// </summary>
//         public class KeyPair
//         {
//             public string PrivateKey { get; set; }
//             public string PublicKey { get; set; }
//         }
//
//         /// <summary>
//         /// Generate a new key pair based on a key type
//         /// </summary>
//         /// <param name="keyType">Optional key type. (sha256x2, R1)</param>
//         /// <returns>key pair</returns>
//         public static KeyPair GenerateKeyPair(string keyType = "sha256x2")
//         {
//             var key = Secp256K1Manager.GenerateRandomKey();
//             var pubKey = Secp256K1Manager.GetPublicKey(key, true);
//
//             if (keyType != "sha256x2" && keyType != "R1")
//                 throw new Exception("invalid key type.");
//
//             return new KeyPair()
//             {
//                 PrivateKey = KeyToString(key, keyType, keyType == "R1" ? "PVT_R1_" : null),
//                 PublicKey = KeyToString(pubKey, keyType != "sha256x2" ? keyType : null, keyType == "R1" ? "PUB_R1_" : "EOS")
//             };
//         }
//
//         /// <summary>
//         /// Get private key bytes without is checksum
//         /// </summary>
//         /// <param name="privateKey">private key</param>
//         /// <returns>byte array</returns>
//         public static byte[] GetPrivateKeyBytesWithoutCheckSum(string privateKey)
//         {
//             if (privateKey.StartsWith("PVT_R1_"))
//                 return PrivKeyStringToBytes(privateKey).Take(PRIV_KEY_DATA_SIZE).ToArray();
//             else
//                 return PrivKeyStringToBytes(privateKey).Skip(1).Take(PRIV_KEY_DATA_SIZE).ToArray();
//         }
//
//         /// <summary>
//         /// Convert byte array to encoded public key string
//         /// </summary>
//         /// <param name="keyBytes">public key bytes</param>
//         /// <param name="keyType">Optional key type. (sha256x2, R1, K1)</param>
//         /// <param name="prefix">Optional prefix to public key</param>
//         /// <returns>encoded public key</returns>
//         public static string PubKeyBytesToString(byte[] keyBytes, string keyType = null, string prefix = "EOS")
//         {
//             return KeyToString(keyBytes, keyType, prefix);
//         }
//
//         /// <summary>
//         /// Convert byte array to encoded private key string
//         /// </summary>
//         /// <param name="keyBytes">public key bytes</param>
//         /// <param name="keyType">Optional key type. (sha256x2, R1, K1)</param>
//         /// <param name="prefix">Optional prefix to public key</param>
//         /// <returns>encoded private key</returns>
//         public static string PrivKeyBytesToString(byte[] keyBytes, string keyType = "R1", string prefix = "PVT_R1_")
//         {
//             return KeyToString(keyBytes, keyType, prefix);
//         }
//
//         /// <summary>
//         /// Convert byte array to encoded signature string
//         /// </summary>
//         /// <param name="signBytes">signature bytes</param>
//         /// <param name="keyType">Optional key type. (sha256x2, R1, K1)</param>
//         /// <param name="prefix">Optional prefix to public key</param>
//         /// <returns>encoded signature</returns>
//         public static string SignBytesToString(byte[] signBytes, string keyType = "K1", string prefix = "SIG_K1_")
//         {
//             return KeyToString(signBytes, keyType, prefix);
//         }
//
//         /// <summary>
//         /// Convert encoded public key to byte array
//         /// </summary>
//         /// <param name="key">encoded public key</param>
//         /// <param name="prefix">Optional prefix on key</param>
//         /// <returns>public key bytes</returns>
//         public static byte[] PubKeyStringToBytes(string key, string prefix = "EOS")
//         {
//             if(key.StartsWith("PUB_R1_"))
//             {
//                 return StringToKey(key.Substring(7), PUB_KEY_DATA_SIZE, "R1");
//             }
//             else if(key.StartsWith(prefix))
//             {
//                 return StringToKey(key.Substring(prefix.Length), PUB_KEY_DATA_SIZE);
//             }
//             else
//             {
//                 throw new Exception("unrecognized public key format.");
//             }
//         }
//
//         /// <summary>
//         /// Convert encoded public key to byte array
//         /// </summary>
//         /// <param name="key">encoded public key</param>
//         /// <param name="prefix">Optional prefix on key</param>
//         /// <returns>public key bytes</returns>
//         public static byte[] PrivKeyStringToBytes(string key)
//         {
//             if (key.StartsWith("PVT_R1_"))
//                 return StringToKey(key.Substring(7), PRIV_KEY_DATA_SIZE, "R1");
//             else
//                 return StringToKey(key, PRIV_KEY_DATA_SIZE, "sha256x2");
//         }
//
//         /// <summary>
//         /// Convert encoded signature to byte array
//         /// </summary>
//         /// <param name="sign">encoded signature</param>
//         /// <returns>signature bytes</returns>
//         public static byte[] SignStringToBytes(string sign)
//         {
//             if (sign.StartsWith("SIG_K1_"))
//                 return StringToKey(sign.Substring(7), SIGN_KEY_DATA_SIZE, "K1");
//             if (sign.StartsWith("SIG_R1_"))
//                 return StringToKey(sign.Substring(7), SIGN_KEY_DATA_SIZE, "R1");
//             else
//                 throw new Exception("unrecognized signature format.");
//         }
//
//         /// <summary>
//         /// Convert Pub/Priv key or signature to byte array
//         /// </summary>
//         /// <param name="key">generic key</param>
//         /// <param name="size">Key size</param>
//         /// <param name="keyType">Optional key type. (sha256x2, R1, K1)</param>
//         /// <returns>key bytes</returns>
//         public static byte[] StringToKey(string key, int size, string keyType = null) 
//         {
//             var keyBytes = Base58.Decode(key);
//             byte[] digest = null;
//             int skipSize = 0;
//
//             if(keyType == "sha256x2")
//             {
//                 // skip version
//                 skipSize = 1;
//                 digest = Sha256Manager.GetHash(Sha256Manager.GetHash(keyBytes.Take(size + skipSize).ToArray()));
//             }
//             else if(!string.IsNullOrWhiteSpace(keyType))
//             {
//                 // skip K1 recovery param
//                 if(keyType == "K1")
//                     skipSize = 1;
//
//                 digest = Ripemd160Manager.GetHash(SerializationHelper.Combine(new List<byte[]>() {
//                     keyBytes.Take(size + skipSize).ToArray(),
//                     Encoding.UTF8.GetBytes(keyType)
//                 }));
//             }
//             else
//             {
//                 digest = Ripemd160Manager.GetHash(keyBytes.Take(size).ToArray());
//             }
//
//             if (!keyBytes.Skip(size + skipSize).SequenceEqual(digest.Take(4)))
//             {
//                 throw new Exception("checksum doesn't match.");
//             }
//             return keyBytes;
//         }
//
//         /// <summary>
//         /// Convert key byte array to encoded generic key
//         /// </summary>
//         /// <param name="key">key byte array</param>
//         /// <param name="keyType">Key type. (sha256x2, R1, K1)</param>
//         /// <param name="prefix">Optional prefix</param>
//         /// <returns></returns>
//         public static string KeyToString(byte[] key, string keyType, string prefix = null)
//         {
//             byte[] digest = null;
//
//             if (keyType == "sha256x2")
//             {
//                 digest = Sha256Manager.GetHash(Sha256Manager.GetHash(SerializationHelper.Combine(new List<byte[]>() {
//                     new byte[] { 128 },
//                     key
//                 })));
//             }
//             else if (!string.IsNullOrWhiteSpace(keyType))
//             {
//                 digest = Ripemd160Manager.GetHash(SerializationHelper.Combine(new List<byte[]>() {
//                     key,
//                     Encoding.UTF8.GetBytes(keyType)
//                 }));
//             }
//             else
//             {
//                 digest = Ripemd160Manager.GetHash(key);
//             }
//
//             if(keyType == "sha256x2")
//             {
//                 return (prefix ?? "") + Base58.Encode(SerializationHelper.Combine(new List<byte[]>() {
//                     new byte[] { 128 },
//                     key,
//                     digest.Take(4).ToArray()
//                 }));
//             }
//             else
//             {
//                 return (prefix ?? "") + Base58.Encode(SerializationHelper.Combine(new List<byte[]>() {
//                     key,
//                     digest.Take(4).ToArray()
//                 }));
//             }
//         }
//
//
//         public static byte[] AesEncrypt(byte[] keyBytes, string plainText)
//         {
//             using (Aes aes = Aes.Create())
//             {
//                 aes.IV = keyBytes.Skip(32).Take(16).ToArray();
//                 aes.Key = keyBytes.Take(32).ToArray();
//                 return EncryptStringToBytes_Aes(plainText, aes.Key, aes.IV);
//             }
//         }
//
//         public static string AesDecrypt(byte[] keyBytes, byte[] cipherText)
//         {
//             using (Aes aes = Aes.Create())
//             {
//                 aes.IV = keyBytes.Skip(32).Take(16).ToArray();
//                 aes.Key = keyBytes.Take(32).ToArray();
//
//                 // Decrypt the bytes to a string.
//                 return DecryptStringFromBytes_Aes(cipherText, aes.Key, aes.IV);
//             }
//         }
//
//         public static byte[] EncryptStringToBytes_Aes(string plainText, byte[] Key, byte[] IV)
//         {
//             // Check arguments.
//             if (plainText == null || plainText.Length <= 0)
//                 throw new ArgumentNullException("plainText");
//             if (Key == null || Key.Length <= 0)
//                 throw new ArgumentNullException("Key");
//             if (IV == null || IV.Length <= 0)
//                 throw new ArgumentNullException("IV");
//             byte[] encrypted;
//
//             // Create an Aes object
//             // with the specified key and IV.
//             using (Aes aesAlg = Aes.Create())
//             {
//                 aesAlg.Key = Key;
//                 aesAlg.IV = IV;
//
//                 // Create an encryptor to perform the stream transform.
//                 ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
//
//                 // Create the streams used for encryption.
//                 using (MemoryStream msEncrypt = new MemoryStream())
//                 {
//                     using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
//                     {
//                         using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
//                         {
//                             //Write all data to the stream.
//                             swEncrypt.Write(plainText);
//                         }
//                         encrypted = msEncrypt.ToArray();
//                     }
//                 }
//             }
//
//             // Return the encrypted bytes from the memory stream.
//             return encrypted;
//         }
//
//         public static string DecryptStringFromBytes_Aes(byte[] cipherText, byte[] Key, byte[] IV)
//         {
//             // Check arguments.
//             if (cipherText == null || cipherText.Length <= 0)
//                 throw new ArgumentNullException("cipherText");
//             if (Key == null || Key.Length <= 0)
//                 throw new ArgumentNullException("Key");
//             if (IV == null || IV.Length <= 0)
//                 throw new ArgumentNullException("IV");
//
//             // Declare the string used to hold
//             // the decrypted text.
//             string plaintext = null;
//
//             // Create an Aes object
//             // with the specified key and IV.
//             using (Aes aesAlg = Aes.Create())
//             {
//                 aesAlg.Key = Key;
//                 aesAlg.IV = IV;
//
//                 // Create a decryptor to perform the stream transform.
//                 ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
//
//                 // Create the streams used for decryption.
//                 using (MemoryStream msDecrypt = new MemoryStream(cipherText))
//                 {
//                     using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
//                     {
//                         using (StreamReader srDecrypt = new StreamReader(csDecrypt))
//                         {
//
//                             // Read the decrypted bytes from the decrypting stream
//                             // and place them in a string.
//                             plaintext = srDecrypt.ReadToEnd();
//                         }
//                     }
//                 }
//
//             }
//
//             return plaintext;
//         }
//     }
//     
//     /// <summary>
//     /// Signature provider default implementation that stores private keys in memory
//     /// </summary>
//     public class DefaultSignProvider : ISignProvider
//     {
//         private readonly byte[] KeyTypeBytes = Encoding.UTF8.GetBytes("K1");
//         private readonly Dictionary<string, byte[]> Keys = new Dictionary<string, byte[]>();
//
//         /// <summary>
//         /// Create provider with single private key
//         /// </summary>
//         /// <param name="privateKey"></param>
//         public DefaultSignProvider(string privateKey)
//         {
//             var privKeyBytes = CryptoHelper.GetPrivateKeyBytesWithoutCheckSum(privateKey);
//             var pubKey = CryptoHelper.PubKeyBytesToString(Secp256K1Manager.GetPublicKey(privKeyBytes, true));
//             Keys.Add(pubKey, privKeyBytes);
//         }
//
//         /// <summary>
//         /// Create provider with list of private keys
//         /// </summary>
//         /// <param name="privateKeys"></param>
//         public DefaultSignProvider(List<string> privateKeys)
//         {
//             if (privateKeys == null || privateKeys.Count == 0)
//                 throw new ArgumentNullException("privateKeys");
//
//             foreach(var key in privateKeys)
//             {
//                 var privKeyBytes = CryptoHelper.GetPrivateKeyBytesWithoutCheckSum(key);
//                 var pubKey = CryptoHelper.PubKeyBytesToString(Secp256K1Manager.GetPublicKey(privKeyBytes, true));
//                 Keys.Add(pubKey, privKeyBytes);
//             }
//         }
//
//         /// <summary>
//         /// Create provider with dictionary of encoded key pairs
//         /// </summary>
//         /// <param name="encodedKeys"></param>
//         public DefaultSignProvider(Dictionary<string, string> encodedKeys)
//         {
//             if (encodedKeys == null || encodedKeys.Count == 0)
//                 throw new ArgumentNullException("encodedKeys");
//
//             foreach (var keyPair in encodedKeys)
//             {
//                 var privKeyBytes = CryptoHelper.GetPrivateKeyBytesWithoutCheckSum(keyPair.Value);
//                 Keys.Add(keyPair.Key, privKeyBytes);
//             }
//         }
//
//         /// <summary>
//         /// Create provider with dictionary of  key pair with private key as byte array
//         /// </summary>
//         /// <param name="keys"></param>
//         public DefaultSignProvider(Dictionary<string, byte[]> keys)
//         {
//             if (keys == null || keys.Count == 0)
//                 throw new ArgumentNullException("encodedKeys");
//
//             Keys = keys;
//         }
//
//         /// <summary>
//         /// Get available public keys from signature provider
//         /// </summary>
//         /// <returns>List of public keys</returns>
//         public Task<IEnumerable<string>> GetAvailableKeys()
//         {
//             return Task.FromResult(Keys.Keys.AsEnumerable());
//         }
//
//         /// <summary>
//         /// Sign bytes using the signature provider
//         /// </summary>
//         /// <param name="chainId">EOSIO Chain id</param>
//         /// <param name="requiredKeys">required public keys for signing this bytes</param>
//         /// <param name="signBytes">signature bytes</param>
//         /// <param name="abiNames">abi contract names to get abi information from</param>
//         /// <returns>List of signatures per required keys</returns>
//         public Task<IEnumerable<string>> Sign(string chainId, IEnumerable<string> requiredKeys, byte[] signBytes, IEnumerable<string> abiNames = null)
//         {
//             if (requiredKeys == null)
//                 return Task.FromResult(new List<string>().AsEnumerable());
//
//             var availableAndReqKeys = requiredKeys.Intersect(Keys.Keys);
//
//             var data = new List<byte[]>()
//             {
//                 Hex.HexToBytes(chainId),
//                 signBytes,
//                 new byte[32]
//             };
//
//             var hash = Sha256Manager.GetHash(SerializationHelper.Combine(data));
//
//             return Task.FromResult(availableAndReqKeys.Select(key =>
//             {
//                 var sign = Secp256K1Manager.SignCompressedCompact(hash, Keys[key]);
//                 var check = new List<byte[]>() { sign, KeyTypeBytes };
//                 var checksum = Ripemd160Manager.GetHash(SerializationHelper.Combine(check)).Take(4).ToArray();
//                 var signAndChecksum = new List<byte[]>() { sign, checksum };
//
//                 return "SIG_K1_" + Base58.Encode(SerializationHelper.Combine(signAndChecksum));
//             }));
//         }
//     }
//     
//     public class EosTransactionRepositoryBase : IEosTransactionRepositoryBase
//     {
//         private readonly IEosClient _eosClient;
//         private readonly EosConfigurator _eosConfigurator;
//         private readonly AbiSerializationProvider _serializationProvider;
//
//         public EosTransactionRepositoryBase()
//         {
//             _eosClient = new EosClient(new Uri("http://localhost:8888"));
//             _eosConfigurator = new EosConfigurator();
//             _serializationProvider = new AbiSerializationProvider(_eosClient);
//         }
//         
//         /// <summary>
//         /// Calculate required keys to sign the given transaction
//         /// </summary>
//         /// <param name="availableKeys">available public keys list</param>
//         /// <param name="eosTransaction">transaction requiring signatures</param>
//         /// <returns>required public keys</returns>
//         public async Task<List<string>> GetRequiredKeys(List<string> availableKeys, EosTransaction eosTransaction)
//         {
//             int actionIndex = 0;
//             var abiSerializer = new AbiSerializationProvider(_eosClient);
//             var abiResponses = await abiSerializer.GetTransactionAbis(eosTransaction);
//
//             foreach (var action in eosTransaction.ContextFreeActions)
//             {
//                 action.Data = SerializationHelper.ByteArrayToHexString(abiSerializer.SerializeActionData(action, abiResponses[actionIndex++]));
//             }
//
//             foreach (var action in eosTransaction.Actions)
//             {
//                 action.Data = SerializationHelper.ByteArrayToHexString(abiSerializer.SerializeActionData(action, abiResponses[actionIndex++]));
//             }
//             //
//             // return (await _eosClient.GetRequiredKeys(new GetRequiredKeysRequestDto()
//             // {
//             //     AvailableKeys = availableKeys,
//             //     Transaction = new()
//             // }));
//             return new List<string>();
//         }
//         
// 	    public async Task<SignedEosTransaction> SignTransaction(EosTransaction eosTransaction, List<string> requiredKeys = null)
// 	    {
//             if (eosTransaction == null)
//                 throw new ArgumentNullException(nameof(eosTransaction));
//
//             if (_eosConfigurator.SignProvider == null)
//                 throw new ArgumentNullException(nameof(_eosConfigurator));
//
//             GetNodeInfoResponseDto getInfoResult = null;
//             string chainId = _eosConfigurator.ChainId;
//
//             if (string.IsNullOrWhiteSpace(chainId))
//             {
//                 getInfoResult = await _eosClient.GetNodeInfo();
//                 chainId = getInfoResult.ChainId;
//             }
//
//             if (eosTransaction.Expiration == DateTime.MinValue ||
//                eosTransaction.RefBlockNum == 0 ||
//                eosTransaction.RefBlockPrefix == 0)
//             {
//                 if (getInfoResult == null)
//                     getInfoResult = await _eosClient.GetNodeInfo();
//
//                 var taposBlockNum = getInfoResult.HeadBlockNum - (int)_eosConfigurator.BlocksBehind;
//
//                 if ((taposBlockNum - getInfoResult.LastIrreversibleBlockNum) < 2)
//                 {
//                     var getBlockResult = await _eosClient.GetBlock(new GetBlockRequestDto()
//                     {
//                         BlockNumOrId = taposBlockNum.ToString()
//                     });
//                     eosTransaction.Expiration = getBlockResult.Timestamp.AddSeconds(_eosConfigurator.ExpireSeconds);
//                     eosTransaction.RefBlockNum = (UInt16)(getBlockResult.BlockNum & 0xFFFF);
//                     eosTransaction.RefBlockPrefix = getBlockResult.RefBlockPrefix;
//                 }
//                 else
//                 {
//                     var getBlockHeaderState = await _eosClient.GetBlockHeaderState(new GetBlockRequestDto()
//                     {
//                         BlockNumOrId = taposBlockNum.ToString()
//                     });
//                     eosTransaction.Expiration = getBlockHeaderState.Header.Timestamp.AddSeconds(_eosConfigurator.ExpireSeconds);
//                     eosTransaction.RefBlockNum = (UInt16)(getBlockHeaderState.BlockNum & 0xFFFF);
//                     eosTransaction.RefBlockPrefix = Convert.ToUInt32(SerializationHelper.ReverseHex(getBlockHeaderState.Id.Substring(16, 8)), 16);
//                 }
//             }
//
//             var packedTrx = await _serializationProvider.SerializePackedTransaction(eosTransaction);
//             
//             if(requiredKeys == null || requiredKeys.Count == 0)
//             {
//                 var availableKeys = await _eosConfigurator.SignProvider.GetAvailableKeys();
//                 requiredKeys = await GetRequiredKeys(availableKeys.ToList(), eosTransaction);
//             }
//             
//             IEnumerable<string> abis = null;
//
//             if (eosTransaction.Actions != null)
//                 abis = eosTransaction.Actions.Select(a => a.Account);
//
//             var signatures = await _eosConfigurator.SignProvider.Sign(chainId, requiredKeys, packedTrx, abis);
//
//             return new SignedEosTransaction()
//             {
//                 Signatures = signatures,
//                 PackedTransaction = packedTrx
//             };
//         }
//     }
// }