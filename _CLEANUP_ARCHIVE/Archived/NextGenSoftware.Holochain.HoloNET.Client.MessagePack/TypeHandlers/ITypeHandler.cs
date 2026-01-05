namespace NextGenSoftware.Holochain.HoloNET.Client.MessagePack
{
	public interface ITypeHandler
	{
		object Read(Format format, FormatReader reader);

		void Write(object obj, FormatWriter writer);
	}
}
