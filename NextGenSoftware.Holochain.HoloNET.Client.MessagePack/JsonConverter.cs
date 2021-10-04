using System;
using System.IO;
using System.Text;

namespace NextGenSoftware.Holochain.HoloNET.Client.MessagePack
{
	public class JsonConverter
	{
		readonly SerializationContext context;
		readonly FormatReader reader;
		readonly StringBuilder builder;
		int indentationSize;

		public static string Encode(Stream stream, SerializationContext context = null)
		{
			JsonConverter converter = new JsonConverter(stream, context);
			try {
				return converter.AppendStream().ToString();
			}
			catch(Exception e) {
				e.Source = converter.builder.ToString();
				throw;
			}
		}

		JsonConverter(Stream stream, SerializationContext context = null)
		{
			this.context = context ?? SerializationContext.Default;
			this.reader = new FormatReader(stream);
			this.builder = new StringBuilder();
			this.indentationSize = 0;
		}

		JsonConverter AppendStream()
		{
			Format format = reader.ReadFormat();

			if(format.IsNil) Append("null");
			else if(format.IsFalse) Append("false");
			else if(format.IsTrue) Append("true");
			else if(format.IsPositiveFixInt) Append(reader.ReadPositiveFixInt(format).ToString());
			else if(format.IsUInt8) Append(reader.ReadUInt8().ToString());
			else if(format.IsUInt16) Append(reader.ReadUInt16().ToString());
			else if(format.IsUInt32) Append(reader.ReadUInt32().ToString());
			else if(format.IsUInt64) Append(reader.ReadUInt64().ToString());
			else if(format.IsNegativeFixInt) Append(reader.ReadNegativeFixInt(format).ToString());
			else if(format.IsInt8) Append(reader.ReadInt8().ToString());
			else if(format.IsInt16) Append(reader.ReadInt16().ToString());
			else if(format.IsInt32) Append(reader.ReadInt32().ToString());
			else if(format.IsInt64) Append(reader.ReadInt64().ToString());
			else if(format.IsFloat32) Append(reader.ReadFloat32().ToString());
			else if(format.IsFloat64) Append(reader.ReadFloat64().ToString());
			else if(format.IsFixStr) AppendQuotedString(reader.ReadFixStr(format));
			else if(format.IsStr8) AppendQuotedString(reader.ReadStr8());
			else if(format.IsStr16) AppendQuotedString(reader.ReadStr16());
			else if(format.IsStr32) AppendQuotedString(reader.ReadStr32());
			else if(format.IsBin8) StringifyBinary(reader.ReadBin8());
			else if(format.IsBin16) StringifyBinary(reader.ReadBin16());
			else if(format.IsBin32) StringifyBinary(reader.ReadBin32());
			else if(format.IsArrayFamily) ReadArray(format);
			else if(format.IsMapFamily) ReadMap(format);
			else if(format.IsExtFamily) ReadExt(format);
			else throw new FormatException(format, reader);

			return this;
		}

		public override string ToString()
		{
			return builder.ToString();
		}

		JsonConverter Indent()
		{
			if(context.JsonOptions.PrettyPrint) {
				for(int i = 0; i < indentationSize; i++) {
					Append(context.JsonOptions.IndentationString);
				}
			}
			return this;
		}

		JsonConverter Append(string str)
		{
			builder.Append(str);
			return this;
		}

		JsonConverter AppendIfPretty(string str)
		{
			if(context.JsonOptions.PrettyPrint) {
				Append(str);
			}
			return this;
		}

		JsonConverter ValueSeparator()
		{
			AppendIfPretty(context.JsonOptions.ValueSeparator);
			return this;
		}

		JsonConverter AppendQuotedString(string str)
		{
			return Append("\"").Append(str).Append("\"");
		}

		void StringifyBinary(byte[] bytes)
		{
			Append("[");
			foreach(byte b in bytes) {
				Append("0x").Append(b.ToString("X2")).Append(",");
			}
			Append("]");
		}

		void ReadArray(Format format)
		{
			int size = reader.ReadArrayLength(format);
			if(size == 0) {
				Append("[]");
				return;
			}
			Append("[").ValueSeparator();
			indentationSize += 1;
			for(int i = 0; i < size; i++) {
				Indent().AppendStream();
				if(i < size - 1) {
					Append(",");
				}
				ValueSeparator();
			}
			indentationSize -= 1;
			Indent().Append("]");
		}

		void ReadMap(Format format)
		{
			int size = reader.ReadMapLength(format);
			if(size == 0) {
				Append("{}");
				return;
			}
			Append("{").ValueSeparator();
			indentationSize += 1;
			for(int i = 0; i < size; i++) {
				Indent().AppendStream().Append(":").AppendIfPretty(" ").AppendStream();
				if(i < size - 1) {
					Append(",");
				}
				ValueSeparator();
			}
			indentationSize -= 1;
			Indent().Append("}");
		}

		void ReadExt(Format format)
		{
			uint length = reader.ReadExtLength(format);
			sbyte extType = reader.ReadExtType(format);
			object value = context.TypeHandlers.GetExt(extType).ReadExt(length, reader);
			Append(value.ToString());
		}
	}
}
