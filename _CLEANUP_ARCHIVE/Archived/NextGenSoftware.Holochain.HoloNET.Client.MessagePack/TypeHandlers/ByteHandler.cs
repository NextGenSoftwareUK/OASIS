using System;

namespace NextGenSoftware.Holochain.HoloNET.Client.MessagePack
{
	public class ByteHandler : ITypeHandler
	{
		public object Read(Format format, FormatReader reader)
		{
			if(format.IsPositiveFixInt) return reader.ReadPositiveFixInt(format);
			if(format.IsUInt8) return reader.ReadUInt8();
			if(format.IsNil) return default(byte);
			throw new FormatException(this, format, reader);
		}

		public void Write(object obj, FormatWriter writer)
		{
			writer.Write(Convert.ToByte(obj));
		}
	}
}
