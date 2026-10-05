namespace NextGenSoftware.Holochain.HoloNET.Client.MessagePack
{
	public class ArrayOptions
	{
		/// <summary>
		/// If the array is not defined or is null, setting this to true will
		/// cause the formatter to assign an empty array instead of null.
		/// Defaults to true.
		/// </summary>
		public bool NullAsEmptyOnUnpack = true;
	}
}
