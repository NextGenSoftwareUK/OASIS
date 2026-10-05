using System;

namespace NextGenSoftware.Holochain.HoloNET.Client.MessagePack
{
	static class ArrayHelper
	{
		internal static void AdjustSize(ref byte[] bytes, int length)
		{
			if(bytes.Length < length) {
				int newSize = bytes.Length;
				while(newSize < length) {
					newSize *= 2;
				}
				Array.Resize(ref bytes, newSize);
			}
		}
	}
}
