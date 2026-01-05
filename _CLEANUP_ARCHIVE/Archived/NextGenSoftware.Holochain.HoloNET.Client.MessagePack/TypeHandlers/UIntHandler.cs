using System;


namespace NextGenSoftware.Holochain.HoloNET.Client.MessagePack
{
	public class UIntHandler : ITypeHandler
	{
		public object Read(Format format, FormatReader reader)
		{
			if(format.IsPositiveFixInt) return (uint)reader.ReadPositiveFixInt(format);
			if(format.IsUInt8) return (uint)reader.ReadUInt8();
			if(format.IsUInt16) return (uint)reader.ReadUInt16();
			if(format.IsUInt32) return reader.ReadUInt32();
			if(format.IsNil) return default(uint);
			throw new FormatException(this, format, reader);
		}

		public void Write(object obj, FormatWriter writer)
		{
			writer.Write(Convert.ToUInt32(obj));
		}
	}
}
