using System;


namespace NextGenSoftware.Holochain.HoloNET.Client.MessagePack
{
	public class LongHandler : ITypeHandler
	{
		public object Read(Format format, FormatReader reader)
		{
			if(format.IsPositiveFixInt) return (long)reader.ReadPositiveFixInt(format);
			if(format.IsUInt8) return (long)reader.ReadUInt8();
			if(format.IsUInt16) return (long)reader.ReadUInt16();
			if(format.IsUInt32) return (long)reader.ReadUInt32();
			if(format.IsUInt64) return Convert.ToInt64(reader.ReadUInt64());
			if(format.IsNegativeFixInt) return (long)reader.ReadNegativeFixInt(format);
			if(format.IsInt8) return (long)reader.ReadInt8();
			if(format.IsInt16) return (long)reader.ReadInt16();
			if(format.IsInt32) return (long)reader.ReadInt32();
			if(format.IsInt64) return reader.ReadInt64();
			if(format.IsNil) return default(long);
			throw new FormatException(this, format, reader);
		}

		public void Write(object obj, FormatWriter writer)
		{
			writer.Write(Convert.ToInt64(obj));
		}
	}
}
