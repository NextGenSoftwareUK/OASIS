using System;


namespace NextGenSoftware.Holochain.HoloNET.Client.MessagePack
{
	public class FloatHandler : ITypeHandler
	{
		public object Read(Format format, FormatReader reader)
		{
			if(format.IsFloat32) return reader.ReadFloat32();
			if(format.IsFloat64) {
				double value = reader.ReadFloat64();
				if(value > float.MaxValue) {
					throw new InvalidCastException(string.Format("{0} is too big for a float", value));
				}
				if(value < float.MinValue) {
					throw new InvalidCastException(string.Format("{0} is too small for a float", value));
				}
				return (float)value;
			}
			if(format.IsPositiveFixInt) return (float)reader.ReadPositiveFixInt(format);
			if(format.IsUInt8) return (float)reader.ReadUInt8();
			if(format.IsUInt16) return (float)reader.ReadUInt16();
			if(format.IsUInt32) return (float)reader.ReadUInt32();
			if(format.IsUInt64) return (float)reader.ReadUInt64();
			if(format.IsNegativeFixInt) return (float)reader.ReadNegativeFixInt(format);
			if(format.IsInt8) return (float)reader.ReadInt8();
			if(format.IsInt16) return (float)reader.ReadInt16();
			if(format.IsInt32) return (float)reader.ReadInt32();
			if(format.IsInt64) return (float)reader.ReadInt64();
			if(format.IsNil) return default(float);
			throw new FormatException(this, format, reader);
		}

		public void Write(object obj, FormatWriter writer)
		{
			writer.Write((float)obj);
		}
	}
}
