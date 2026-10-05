using System;


namespace NextGenSoftware.Holochain.HoloNET.Client.MessagePack
{
	public class IntHandler : ITypeHandler
	{
		public object Read(Format format, FormatReader reader)
		{
			if(format.IsPositiveFixInt) return (int)reader.ReadPositiveFixInt(format);
			if(format.IsUInt8) return (int)reader.ReadUInt8();
			if(format.IsUInt16) return (int)reader.ReadUInt16();
			if(format.IsUInt32) return Convert.ToInt32(reader.ReadUInt32());
			if(format.IsNegativeFixInt) return (int)reader.ReadNegativeFixInt(format);
			if(format.IsInt8) return (int)reader.ReadInt8();
			if(format.IsInt16) return (int)reader.ReadInt16();
			if(format.IsInt32) return reader.ReadInt32();
			if(format.IsNil) return default(int);
			throw new FormatException(this, format, reader);
		}

		public void Write(object obj, FormatWriter writer)
		{
			writer.Write(Convert.ToInt32(obj));
		}
	}
}
