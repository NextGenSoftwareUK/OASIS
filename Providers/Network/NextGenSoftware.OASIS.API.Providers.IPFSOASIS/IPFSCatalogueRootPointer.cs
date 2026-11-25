using System;
using System.IO;
using System.Text;
using Ipfs;
using NextGenSoftware.OASIS.API.DNA;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("NextGenSoftware.OASIS.API.Providers.IPFSOASIS.CatalogueTests")]

namespace NextGenSoftware.OASIS.API.Providers.IPFSOASIS;

/// <summary>
/// Durable holder of the IPFS catalogue root CID (<see cref="IPFSOASISSettings.CatalogueRootPointerPath"/>). The catalogue
/// root is mutable provider state; on hosts that regenerate the DNA file at boot it must live outside that file or every
/// restart silently rolls the catalogue back to the configured seed.
/// </summary>
internal static class IPFSCatalogueRootPointer
{
    /// <summary>Overlays the durable root onto <paramref name="settings"/> when a pointer file exists.</summary>
    public static void Apply(IPFSOASISSettings settings)
    {
        var path = settings?.CatalogueRootPointerPath;
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return;
        settings.LookUpIPFSAddress = Read(path);
    }

    /// <summary>Atomically replaces the durable root (write-through temp file, flush, then rename).</summary>
    public static void Write(IPFSOASISSettings settings, string cid)
    {
        var path = settings?.CatalogueRootPointerPath;
        if (string.IsNullOrWhiteSpace(path)) return;
        Validate(cid);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var temporary = path + ".next";
        using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            var bytes = Encoding.ASCII.GetBytes(cid);
            stream.Write(bytes, 0, bytes.Length);
            stream.Flush(flushToDisk: true);
        }
        File.Move(temporary, path, overwrite: true);
    }

    private static string Read(string path)
    {
        var cid = File.ReadAllText(path, Encoding.ASCII).Trim();
        Validate(cid);
        return cid;
    }

    private static void Validate(string cid)
    {
        try { _ = (Cid)cid; }
        catch (Exception cause) { throw new InvalidDataException("The IPFS catalogue root pointer does not hold a valid CID.", cause); }
    }
}
