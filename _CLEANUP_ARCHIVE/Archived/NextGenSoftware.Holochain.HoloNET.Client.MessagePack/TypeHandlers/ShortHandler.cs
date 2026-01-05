using System;


namespace NextGenSoftware.Holochain.HoloNET.Client.MessagePack
{
	public class ShortHandler : ITypeHandler
	{
		public object Read(Format format, FormatReader reader)
		{
			if(format.IsPositiveFixInt) return (short)reader.ReadPositiveFixInt(format);
			if(format.IsUInt8) return (short)reader.ReadUInt8();
			if(format.IsUInt16) return Convert.ToInt16(reader.ReadUInt16());
			if(format.IsNegativeFixInt) return(short) reader.ReadNegativeFixInt(format);
			if(format.IsInt8) return (short)reader.ReadInt8();
			if(format.IsInt16) return reader.ReadInt16();
			if(format.IsNil) return default(short);
			throw new FormatException(this, format, reader);
		}

		public void Write(object obj, FormatWriter writer)
		{
			writer.Write(Convert.ToInt16(obj));
		}
	}
}
