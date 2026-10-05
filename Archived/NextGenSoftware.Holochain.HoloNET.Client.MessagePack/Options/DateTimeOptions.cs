namespace NextGenSoftware.Holochain.HoloNET.Client.MessagePack
{
	public class DateTimeOptions
	{
		/// <summary>
		/// The packing format for DateTime format. Available formats are...
		/// Extension: packs using extension format defined in spec.
		/// String: packs in ISO8601 formatted string.
		/// Epoch: packs as Unix Epoch Time using double.
		/// Defaults to `Extension` format.
		/// </summary>
		public DateTimePackingFormat PackingFormat = DateTimePackingFormat.Extension;
	}
}
