using System.Collections.Generic;


namespace NextGenSoftware.Holochain.HoloNET.Client.MessagePack
{
	public class CamelCaseNamingStrategy : IMapNamingStrategy
	{
		public string OnPack(string name, MapDefinition definition)
		{
			return char.ToLowerInvariant(name[0]) + name.Substring(1); 
		}

		public string OnUnpack(string name, MapDefinition definition)
		{
			return char.ToUpperInvariant(name[0]) + name.Substring(1);
		}
	}
}
