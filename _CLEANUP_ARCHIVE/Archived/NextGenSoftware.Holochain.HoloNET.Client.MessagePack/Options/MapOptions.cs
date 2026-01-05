using System.Reflection;

namespace NextGenSoftware.Holochain.HoloNET.Client.MessagePack
{
	public class MapOptions
	{
		/// <summary>
		/// This makes it so that classes/structs will be serialized only if
		/// they have the `System.Serializable` attribute defined.
		/// If it is not defined on the Serializing/Deseraializing class, the
		/// formatter will throw an error. If it's not defined on member fields,
		/// that member field will be ignored on serialization.
		/// Defaults to true.
		/// </summary>
		public bool RequireSerializableAttribute = true;

		/// <summary>
		/// Setting this to false will force auto property values to be serialized as
		/// something like "<{property_name}>k__BackingField: {value}"
		/// Defaults to true.
		/// </summary>
		public bool IgnoreAutoPropertyValues = true;

		/// <summary>
		/// This will skip all the field values which are null to reduce the size
		/// of the resulting bytes.
		/// Defaults to true.
		/// </summary>
		public bool IgnoreNullOnPack = true;

		/// <summary>
		/// Ignore Field names that is not defined on the Map.
		/// Settings this to false will raise an Exception instead.
		/// Defaults to true.
		/// </summary>
		public bool IgnoreUnknownFieldOnUnpack = true;

		/// <summary>
		/// This is for compatibility with msgpack-php since php cannot 
		/// distinguish between ordered array and hashed array.
		/// Defaults to true.
		/// </summary>
		public bool AllowEmptyArrayOnUnpack = true;

		/// <summary>
		/// The naming strategy used when Packing/Unpacking map field names.
		/// Defaults to a strategy that does nothing.
		/// This library also includes one that convert to and from camel case 
		/// which is used for PHP and one that convert to snake case for Ruby.
		/// </summary>
		public IMapNamingStrategy NamingStrategy = new DefaultNamingStrategy();

		/// <summary>
		/// BindingFlags for Field
		/// Defaults include Instance/Public/NonPublic/GetField/SetField
		/// </summary>
		public BindingFlags FieldFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.SetField;
	}
}
