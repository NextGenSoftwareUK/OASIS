using System;

namespace NextGenSoftware.Holochain.HoloNET.Client.MessagePack
{
	[AttributeUsage(AttributeTargets.Field, Inherited = false)]
	public sealed class NonSerializedAttribute : Attribute
	{
		
	}
}
