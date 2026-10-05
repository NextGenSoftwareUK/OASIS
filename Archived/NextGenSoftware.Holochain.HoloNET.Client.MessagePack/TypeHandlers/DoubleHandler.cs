using System;


namespace NextGenSoftware.Holochain.HoloNET.Client.MessagePack
{
	public class DoubleHandler : ITypeHandler
	{
		public object Read(Format format, FormatReader reader)
		{
			if(format.IsFloat32) return (double)reader.ReadFloat32();
			if(format.IsFloat64) return reader.ReadFloat64();
			if(format.IsPositiveFixInt) return (double)reader.ReadPositiveFixInt(format);
			if(format.IsUInt8) return (double)reader.ReadUInt8();
			if(format.IsUInt16) return (double)reader.ReadUInt16();
			if(format.IsUInt32) return (double)reader.ReadUInt32();
			if(format.IsUInt64) return (double)reader.ReadUInt64();
			if(format.IsNegativeFixInt) return (double)reader.ReadNegativeFixInt(format);
			if(format.IsInt8) return (double)reader.ReadInt8();
			if(format.IsInt16) return (double)reader.ReadInt16();
			if(format.IsInt32) return (double)reader.ReadInt32();
			if(format.IsInt64) return (double)reader.ReadInt64();
			if(format.IsNil) return default(double);
			throw new FormatException(this, format, reader);
		}

		public void Write(object obj, FormatWriter writer)
		{
			writer.Write((double)obj);
		}
	}
}
