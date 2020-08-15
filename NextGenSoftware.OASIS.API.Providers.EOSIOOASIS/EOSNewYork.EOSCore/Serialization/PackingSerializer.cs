using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;

namespace EOSNewYork.EOSCore.Serialization
{
    public class PackingSerializer 
    {
        public byte[] Serialize<T>(object obj)
        {
            using (var stream = new MemoryStream())
            {
                Serialize<T>(stream, obj);
                return stream.ToArray();
            }
        }
        public void Serialize<T>(Stream stream, object obj)
        {
            var properties = typeof(T).GetRuntimeProperties();
            foreach (var property in properties)
            {
                WriteToStream(stream, property, obj);
            }
        }
        protected void WriteToStream(Stream stream, PropertyInfo propertyInfo, object val)
        {
            Type propertyType = propertyInfo.PropertyType;
            object propertyValue = propertyInfo.GetValue(val);
            if(typeof(string) == propertyType && propertyValue == null)
            {
                propertyValue = string.Empty;
            }
            WritePropertyToStream(stream, propertyType, propertyValue);
        }
        public virtual void WritePropertyToStream(Stream stream, Type type, object value)
        {
            switch (value)
            {
                case bool item:
                {
                    stream.WriteByte((byte)(item ? 1 : 0));
                    return;
                }
                case byte item:
                {
                    stream.WriteByte(item);
                    return;
                }
                case short item:
                {
                    var bytes = BitConverter.GetBytes(item);
                    stream.Write(bytes, 0, bytes.Length);
                    return;
                }
                case ushort item:
                {
                    var bytes = BitConverter.GetBytes(item);
                    stream.Write(bytes, 0, bytes.Length);
                    return;
                }
                case int item:
                {
                    var bytes = BitConverter.GetBytes(item);
                    stream.Write(bytes, 0, bytes.Length);
                    return;
                }
                case uint item:
                {
                    var bytes = BitConverter.GetBytes(item);
                    stream.Write(bytes, 0, bytes.Length);
                    return;
                }
                case long item:
                {
                    var bytes = BitConverter.GetBytes(item);
                    stream.Write(bytes, 0, bytes.Length);
                    return;
                }
                case ulong item:
                {
                    var bytes = BitConverter.GetBytes(item);
                    stream.Write(bytes, 0, bytes.Length);
                    return;
                }
                case float item:
                {
                    var bytes = BitConverter.GetBytes(item);
                    stream.Write(bytes, 0, bytes.Length);
                    return;
                }
                case double item:
                {
                    var bytes = BitConverter.GetBytes(item);
                    stream.Write(bytes, 0, bytes.Length);
                    return;
                }
                case byte[] bytes:
                {
                    stream.Write(bytes, 0, bytes.Length);
                    return;
                }
                case string item:
                {
                    if (string.IsNullOrEmpty(item))
                    {
                        stream.WriteByte(0);
                        return;
                    }
                    var bytes = Encoding.UTF8.GetBytes(item);

                    var length = new UnsignedInt((uint)bytes.Length);
                    length.WriteToStream(stream);

                    stream.Write(bytes, 0, bytes.Length);
                    return;
                }
                case BaseCustomType item:
                {
                    item.WriteToStream(stream);
                    return;
                }
                default:
                {
                    if (type.IsArray)
                    {
                        var item = (ICollection)value;
                        if (item == null)
                            return;

                        var bytes = new UnsignedInt((uint)item.Count);
                        bytes.WriteToStream(stream);

                        foreach (var property in item)
                        {
                            WritePropertyToStream(stream, property.GetType(), property);
                        }
                        return;
                    }
                    if (type.IsClass)
                    {
                        Type classType = value.GetType();
                        var properties = classType.GetRuntimeProperties();
                        foreach (var property in properties)
                        {
                            WriteToStream(stream, property, value);
                        }
                        return;
                    }
                    throw new NotImplementedException();
                }
            }
        }
    }
}