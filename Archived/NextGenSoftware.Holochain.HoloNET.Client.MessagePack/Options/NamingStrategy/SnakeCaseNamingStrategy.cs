using System.Text;


namespace NextGenSoftware.Holochain.HoloNET.Client.MessagePack
{
	public class SnakeCaseNamingStrategy : IMapNamingStrategy
	{
		public string OnPack(string name, MapDefinition definition)
		{
			StringBuilder sb = new StringBuilder();

			char previous = default(char);
			for(int i = 0; i < name.Length; i++) {
				char current = name[i];
				if(char.IsUpper(current)) {
					if(char.IsLower(previous)) {
						sb.Append('_');
					}
					sb.Append(char.ToLower(current));
				}
				else {
					sb.Append(current);
				}
				previous = current;
			}
			return sb.ToString();
		}

		public string OnUnpack(string name, MapDefinition definition)
		{
			StringBuilder sb = new StringBuilder();

			bool capitalizeNext = true;
			for(int i = 0; i < name.Length; i++) {
				if(name[i] == '_') {
					capitalizeNext = true;
				}
				else if(capitalizeNext) {
					sb.Append(char.ToUpper(name[i]));
					capitalizeNext = false;
				}
				else {
					sb.Append(name[i]);
				}
			}

			return sb.ToString();
		}
	}
}
