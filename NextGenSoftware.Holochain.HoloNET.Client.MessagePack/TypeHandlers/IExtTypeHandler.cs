namespace NextGenSoftware.Holochain.HoloNET.Client.MessagePack
{
	public interface IExtTypeHandler : ITypeHandler
	{
		sbyte ExtType { get; }

		object ReadExt(uint length, FormatReader reader);
	}
}
