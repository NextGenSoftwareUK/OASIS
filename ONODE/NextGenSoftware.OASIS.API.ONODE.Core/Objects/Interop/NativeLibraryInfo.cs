using System;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Objects.Interop
{
    /// <summary>
    /// Information about a loaded native library
    /// </summary>
    public class NativeLibraryInfo
    {
        /// <summary>
        /// Handle to the loaded native library
        /// </summary>
        public IntPtr Handle { get; set; }

        /// <summary>
        /// Path to the library file
        /// </summary>
        public string LibraryPath { get; set; }
    }
}

