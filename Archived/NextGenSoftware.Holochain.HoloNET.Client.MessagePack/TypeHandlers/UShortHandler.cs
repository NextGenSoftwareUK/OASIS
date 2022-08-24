using System;


namespace NextGenSoftware.Holochain.HoloNET.Client.MessagePack
{
	public class UShortHandler : ITypeHandler
	{
		public object Read(Format format, FormatReader reader)
		{
			if(format.IsPositiveFixInt) return (ushort)reader.ReadPositiveFixInt(format);
			if(format.IsUInt8) return (ushort)reader.ReadUInt8();
			if(format.IsUInt16) return reader.ReadUInt16();
			if(format.IsNil) return default(ushort);
			throw new FormatException(this, format, reader);
		}

		public void Write(object obj, FormatWriter writer)
		{
			writer.Write(Convert.ToUInt16(obj));
		}
	}
}
