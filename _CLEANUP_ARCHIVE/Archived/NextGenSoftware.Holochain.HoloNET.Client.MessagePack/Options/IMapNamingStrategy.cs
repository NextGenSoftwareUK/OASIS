

namespace NextGenSoftware.Holochain.HoloNET.Client.MessagePack
{
	public interface IMapNamingStrategy
	{
		string OnPack(string name, MapDefinition definition);
		string OnUnpack(string name, MapDefinition definition);
	}
}
