using System.Runtime.InteropServices;

namespace NextGenSoftware.OASIS.STARAPI.Client;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void StarSyncDoneCallback(IntPtr userData);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void StarSyncAddItemLogCallback(IntPtr itemName, int success, IntPtr errorMessage, IntPtr userData);
