namespace NextGenSoftware.OASIS.STARAPI.Client;

/// <summary>How the game connects to STAR: HTTP to hosted WEB5/WEB4 APIs (default), or in-process OASIS (requires a native host build that embeds HyperDrive — not the default NativeAOT ogengine).</summary>
public enum OGEngineTransport
{
    Remote = 0,
    Native = 1
}
