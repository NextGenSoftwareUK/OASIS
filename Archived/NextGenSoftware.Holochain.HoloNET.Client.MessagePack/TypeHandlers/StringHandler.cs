

namespace NextGenSoftware.Holochain.HoloNET.Client.MessagePack
{
	public class StringHandler : ITypeHandler
	{
		public object Read(Format format, FormatReader reader)
		{
			if(format.IsFixStr) return reader.ReadFixStr(format);
			if(format.IsStr8) return reader.ReadStr8();
			if(format.IsStr16) return reader.ReadStr16();
			if(format.IsStr32) return reader.ReadStr32();
			if(format.IsNil) return null;
			throw new FormatException(this, format, reader);
		}

		public void Write(object obj, FormatWriter writer)
		{
			writer.Write((string)obj);
		}
	}
}
