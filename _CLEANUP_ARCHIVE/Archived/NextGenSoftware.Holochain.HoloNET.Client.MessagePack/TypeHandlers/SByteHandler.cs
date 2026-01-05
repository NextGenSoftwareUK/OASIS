using System;


namespace NextGenSoftware.Holochain.HoloNET.Client.MessagePack
{
	public class SByteHandler : ITypeHandler
	{
		public object Read(Format format, FormatReader reader)
		{
			if(format.IsPositiveFixInt) return (sbyte)reader.ReadPositiveFixInt(format);
			if(format.IsUInt8) return Convert.ToSByte(reader.ReadUInt8());
			if(format.IsNegativeFixInt) return reader.ReadNegativeFixInt(format);
			if(format.IsInt8) return reader.ReadInt8();
			if(format.IsNil) return default(sbyte);
			throw new FormatException(this, format, reader);
		}

		public void Write(object obj, FormatWriter writer)
		{
			writer.Write(Convert.ToSByte(obj));
		}
	}
}
