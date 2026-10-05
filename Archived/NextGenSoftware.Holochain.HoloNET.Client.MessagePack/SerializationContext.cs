namespace NextGenSoftware.Holochain.HoloNET.Client.MessagePack
{
	public class SerializationContext
	{
		static SerializationContext defaultContext;

		public static SerializationContext Default
		{
			get { return defaultContext = defaultContext ?? new SerializationContext(); }
		}

		public readonly DateTimeOptions DateTimeOptions;

		public readonly EnumOptions EnumOptions;

		public readonly ArrayOptions ArrayOptions;

		public readonly MapOptions MapOptions;

		public readonly JsonOptions JsonOptions;

		public readonly TypeHandlers TypeHandlers;

		public SerializationContext()
		{
			DateTimeOptions = new DateTimeOptions();
			EnumOptions = new EnumOptions();
			ArrayOptions = new ArrayOptions();
			MapOptions = new MapOptions();
			JsonOptions = new JsonOptions();
			TypeHandlers = new TypeHandlers(this);
		}
	}
}
