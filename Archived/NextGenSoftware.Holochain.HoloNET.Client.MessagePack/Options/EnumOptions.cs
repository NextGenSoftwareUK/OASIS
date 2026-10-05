namespace NextGenSoftware.Holochain.HoloNET.Client.MessagePack
{
	public class EnumOptions
	{
		/// <summary>
		/// The packing format for Enum types. Available formats are...
		/// Integer: packs using the underlying integer value.
		/// String: packs using the name of the enum value.
		/// Defaults to Integer.
		/// </summary>
		public EnumPackingFormat PackingFormat = EnumPackingFormat.Integer;
	}
}
