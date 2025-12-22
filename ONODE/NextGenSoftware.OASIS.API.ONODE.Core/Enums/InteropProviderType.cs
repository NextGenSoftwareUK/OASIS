namespace NextGenSoftware.OASIS.API.ONODE.Core.Enums
{
    /// <summary>
    /// Types of interop providers for different languages/frameworks
    /// </summary>
    public enum InteropProviderType
    {
        /// <summary>
        /// Native C/C++/Rust libraries via P/Invoke
        /// Best performance for native code
        /// </summary>
        NativePInvoke,

        /// <summary>
        /// Python libraries via Python.NET
        /// </summary>
        Python,

        /// <summary>
        /// JavaScript/Node.js libraries via ClearScript or Edge.js
        /// </summary>
        JavaScript,

        /// <summary>
        /// WebAssembly (WASM) modules
        /// Cross-platform, many languages compile to WASM
        /// </summary>
        WebAssembly,

        /// <summary>
        /// Java libraries via JNI or IKVM.NET
        /// </summary>
        Java,

        /// <summary>
        /// Go libraries via CGO (C interop)
        /// </summary>
        Go,

        /// <summary>
        /// Rust libraries (native or via C interop)
        /// </summary>
        Rust,

        /// <summary>
        /// .NET assemblies (direct)
        /// </summary>
        DotNet,

        /// <summary>
        /// REST API wrapper for remote libraries
        /// </summary>
        RestApi,

        /// <summary>
        /// gRPC wrapper for remote libraries
        /// High performance RPC
        /// </summary>
        Grpc,

        /// <summary>
        /// COM interop (Windows)
        /// </summary>
        Com,

        /// <summary>
        /// Auto-detect based on file extension
        /// </summary>
        Auto
    }
}

