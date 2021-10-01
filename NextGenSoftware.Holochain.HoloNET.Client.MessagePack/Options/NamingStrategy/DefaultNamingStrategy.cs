using System.Reflection;


namespace NextGenSoftware.Holochain.HoloNET.Client.MessagePack
{
	public class DefaultNamingStrategy : IMapNamingStrategy
	{
		public string OnPack(string name, MapDefinition definition)
		{
			return name;
		}

		public string OnUnpack(string name, MapDefinition definition)
		{
			return name;
		}
	}
}
