// NativeExports.cs — [UnmanagedCallersOnly] exports for OGEditorClient.dll
//
// Each export exactly mirrors the C signature declared in OGEditorClient.h.
// Compiled with NativeAOT (dotnet publish --configuration Release) to produce
// a self-contained OGEditorClient.dll (Windows) / libOGEditorClient.so (Linux)
// with no .NET runtime dependency — C++ editors load it like any Win32 DLL.
//
// Build:
//   dotnet publish OGEditorSDK.Native.csproj -r win-x64 -c Release
//   → bin\Release\net8.0\win-x64\publish\OGEditorClient.dll + OGEditorClient.h

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace OGEditorSDK.Native
{
    internal static unsafe class NativeExports
    {
        // ── Handle table ──────────────────────────────────────────────────────────

        private sealed class Context
        {
            public OGStarApiClient StarApi;
            public string          LastError = string.Empty;
        }

        private static readonly Dictionary<IntPtr, Context> Handles = new Dictionary<IntPtr, Context>();
        private static int _nextHandle = 1;

        private static IntPtr NewHandle(Context ctx)
        {
            var ptr = new IntPtr(_nextHandle++);
            Handles[ptr] = ctx;
            return ptr;
        }

        private static bool TryGet(IntPtr h, out Context ctx) => Handles.TryGetValue(h, out ctx);

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        [UnmanagedCallersOnly(EntryPoint = "ogeditor_init")]
        public static IntPtr Init(byte* starApiBaseUrl, byte* avatarId)
        {
            try
            {
                var ctx = new Context
                {
                    StarApi = new OGStarApiClient(
                        starApiBaseUrl != null ? Marshal.PtrToStringUTF8((IntPtr)starApiBaseUrl) : "http://localhost:5000",
                        avatarId       != null ? Marshal.PtrToStringUTF8((IntPtr)avatarId)       : string.Empty)
                };
                return NewHandle(ctx);
            }
            catch { return IntPtr.Zero; }
        }

        [UnmanagedCallersOnly(EntryPoint = "ogeditor_dispose")]
        public static void Dispose(IntPtr handle)
        {
            if (Handles.TryGetValue(handle, out var ctx))
            {
                ctx.StarApi?.Dispose();
                Handles.Remove(handle);
            }
        }

        // ── Asset catalog ─────────────────────────────────────────────────────────

        [UnmanagedCallersOnly(EntryPoint = "ogeditor_get_assets_json")]
        public static int GetAssetsJson(IntPtr handle, byte* gameId, byte* outBuf, int bufLen)
        {
            if (!TryGet(handle, out var ctx)) return -1;
            try
            {
                string gid    = gameId != null ? Marshal.PtrToStringUTF8((IntPtr)gameId) : "*";
                var    assets = gid == "*" ? OGAssetCatalog.All : OGAssetCatalog.ForGame(gid);
                var    sb     = new System.Text.StringBuilder("[");
                bool   first  = true;
                foreach (var a in assets)
                {
                    if (!first) sb.Append(',');
                    first = false;
                    sb.Append("{\"gameId\":\"").Append(a.GameId).Append("\"")
                      .Append(",\"thingType\":").Append(a.ThingType)
                      .Append(",\"displayName\":\"").Append(Esc(a.DisplayName)).Append("\"")
                      .Append(",\"nativeClassname\":\"").Append(Esc(a.NativeClassname)).Append("\"")
                      .Append(",\"category\":\"").Append(a.Category).Append("\"")
                      .Append("}");
                }
                sb.Append("]");
                return WriteUtf8(sb.ToString(), outBuf, bufLen);
            }
            catch (Exception ex) { return SetErr(ctx, ex, -3); }
        }

        [UnmanagedCallersOnly(EntryPoint = "ogeditor_get_asset_by_type")]
        public static int GetAssetByType(IntPtr handle, int thingType, byte* outBuf, int bufLen)
        {
            if (!TryGet(handle, out var ctx)) return -1;
            var a = OGAssetCatalog.ByThingType(thingType);
            if (a == null) return SetErr(ctx, "not found: " + thingType, -4);
            string json = "{\"gameId\":\"" + a.GameId + "\",\"thingType\":" + a.ThingType +
                          ",\"displayName\":\"" + Esc(a.DisplayName) + "\",\"nativeClassname\":\"" +
                          Esc(a.NativeClassname) + "\",\"category\":\"" + a.Category + "\"}";
            return WriteUtf8(json, outBuf, bufLen);
        }

        // ── Portals / sidecar ─────────────────────────────────────────────────────

        [UnmanagedCallersOnly(EntryPoint = "ogeditor_get_portals_json")]
        public static int GetPortalsJson(IntPtr handle, byte* mapFilePath, byte* outBuf, int bufLen)
        {
            if (!TryGet(handle, out var ctx)) return -1;
            try
            {
                string path    = Marshal.PtrToStringUTF8((IntPtr)mapFilePath);
                var    sidecar = OGMapSidecar.Load(path);
                // Simple JSON: just return the raw sidecar file content (OGMapSidecar.Save format)
                // Re-serialise via Save→read pattern
                string tmp = System.IO.Path.GetTempFileName();
                OGMapSidecar.Save(tmp, sidecar);
                string json = System.IO.File.ReadAllText(tmp);
                System.IO.File.Delete(tmp);
                return WriteUtf8(json, outBuf, bufLen);
            }
            catch (Exception ex) { return SetErr(ctx, ex, -6); }
        }

        [UnmanagedCallersOnly(EntryPoint = "ogeditor_append_portal")]
        public static int AppendPortal(IntPtr handle, byte* mapFilePath, byte* portalJson)
        {
            if (!TryGet(handle, out var ctx)) return -1;
            try
            {
                string path = Marshal.PtrToStringUTF8((IntPtr)mapFilePath);
                string json = Marshal.PtrToStringUTF8((IntPtr)portalJson);
                var    e    = ParsePortalJson(json);
                OGMapSidecar.AppendPortal(path, e);
                // Also push to STAR API
                try { ctx.StarApi?.PostPortal(json); } catch { /* non-fatal — sidecar already written */ }
                return 0;
            }
            catch (Exception ex) { return SetErr(ctx, ex, -6); }
        }

        // ── Quests ────────────────────────────────────────────────────────────────

        [UnmanagedCallersOnly(EntryPoint = "ogeditor_get_quests_json")]
        public static int GetQuestsJson(IntPtr handle, byte* gameId, byte* outBuf, int bufLen)
        {
            if (!TryGet(handle, out var ctx)) return -1;
            try
            {
                string gid  = gameId != null ? Marshal.PtrToStringUTF8((IntPtr)gameId) : null;
                string json = ctx.StarApi.GetQuests(gid);
                return WriteUtf8(json, outBuf, bufLen);
            }
            catch (Exception ex) { return SetErr(ctx, ex, -3); }
        }

        [UnmanagedCallersOnly(EntryPoint = "ogeditor_create_quest")]
        public static int CreateQuest(IntPtr handle, byte* questJson, byte* outBuf, int bufLen)
        {
            if (!TryGet(handle, out var ctx)) return -1;
            try
            {
                string body   = Marshal.PtrToStringUTF8((IntPtr)questJson);
                string result = ctx.StarApi.CreateQuest(body);
                return WriteUtf8(result, outBuf, bufLen);
            }
            catch (Exception ex) { return SetErr(ctx, ex, -3); }
        }

        [UnmanagedCallersOnly(EntryPoint = "ogeditor_bind_objective")]
        public static int BindObjective(IntPtr handle, byte* questId, byte* bindingJson)
        {
            if (!TryGet(handle, out var ctx)) return -1;
            try
            {
                string qid  = Marshal.PtrToStringUTF8((IntPtr)questId);
                string body = Marshal.PtrToStringUTF8((IntPtr)bindingJson);
                ctx.StarApi.BindObjective(qid, body);
                return 0;
            }
            catch (Exception ex) { return SetErr(ctx, ex, -3); }
        }

        // ── Missions ──────────────────────────────────────────────────────────────

        [UnmanagedCallersOnly(EntryPoint = "ogeditor_get_missions_json")]
        public static int GetMissionsJson(IntPtr handle, byte* questId, byte* outBuf, int bufLen)
        {
            if (!TryGet(handle, out var ctx)) return -1;
            try
            {
                string qid  = questId != null ? Marshal.PtrToStringUTF8((IntPtr)questId) : null;
                string json = ctx.StarApi.GetMissions(qid);
                return WriteUtf8(json, outBuf, bufLen);
            }
            catch (Exception ex) { return SetErr(ctx, ex, -3); }
        }

        // ── Entity mappings ───────────────────────────────────────────────────────

        [UnmanagedCallersOnly(EntryPoint = "ogeditor_classname_to_thing_type")]
        public static int ClassnameToThingType(IntPtr handle, byte* sourceGame, byte* classname, int* outThingType)
        {
            if (!TryGet(handle, out var ctx)) return -1;
            string game = Marshal.PtrToStringUTF8((IntPtr)sourceGame) ?? "";
            string cls  = Marshal.PtrToStringUTF8((IntPtr)classname)  ?? "";
            IReadOnlyDictionary<string, int> map = GetMappingForGame(game);
            int val;
            if (map != null && map.TryGetValue(cls, out val)) { *outThingType = val; return 0; }
            return SetErr(ctx, "unmapped: " + game + "/" + cls, -4);
        }

        [UnmanagedCallersOnly(EntryPoint = "ogeditor_thing_type_to_classname")]
        public static int ThingTypeToClassname(IntPtr handle, byte* targetGame, int thingType, byte* outBuf, int bufLen)
        {
            if (!TryGet(handle, out _)) return -1;
            string cls = OGEntityMappings.NativeClassnameForThingType(thingType);
            if (cls == null) return -4;
            return WriteUtf8(cls, outBuf, bufLen);
        }

        // ── Map format conversion (OGMapFormat SDK) ───────────────────────────────

        [UnmanagedCallersOnly(EntryPoint = "ogeditor_list_adapters")]
        public static int ListAdapters(byte* outBuf, int bufLen)
        {
            try
            {
                var sb = new System.Text.StringBuilder("[");
                bool first = true;
                foreach (var a in OGEditorSDK.MapFormat.OGMapFormatRegistry.Instance.All)
                {
                    if (!first) sb.Append(',');
                    first = false;
                    sb.Append("{\"formatId\":\"").Append(Esc(a.FormatId)).Append("\"")
                      .Append(",\"displayName\":\"").Append(Esc(a.DisplayName)).Append("\"")
                      .Append(",\"family\":").Append((int)a.Family)
                      .Append(",\"extensions\":[");
                    bool fe = true;
                    foreach (var ext in a.FileExtensions)
                    {
                        if (!fe) sb.Append(',');
                        fe = false;
                        sb.Append("\"").Append(Esc(ext)).Append("\"");
                    }
                    sb.Append("]}");
                }
                sb.Append("]");
                return WriteUtf8(sb.ToString(), outBuf, bufLen);
            }
            catch { return -3; }
        }

        [UnmanagedCallersOnly(EntryPoint = "ogeditor_conversion_fidelity")]
        public static float ConversionFidelity(byte* srcFormatId, byte* dstFormatId)
        {
            try
            {
                string src = Marshal.PtrToStringUTF8((IntPtr)srcFormatId) ?? "";
                string dst = Marshal.PtrToStringUTF8((IntPtr)dstFormatId) ?? "";
                return new OGEditorSDK.MapFormat.OGMapConverter().GetFidelity(src, dst);
            }
            catch { return -1f; }
        }

        [UnmanagedCallersOnly(EntryPoint = "ogeditor_convert_map")]
        public static int ConvertMap(byte* srcPath, byte* srcFormatId,
                                     byte* dstPath, byte* dstFormatId,
                                     byte* outResultJson, int bufLen)
        {
            try
            {
                string sp  = Marshal.PtrToStringUTF8((IntPtr)srcPath)     ?? "";
                string sf  = Marshal.PtrToStringUTF8((IntPtr)srcFormatId) ?? "";
                string dp  = Marshal.PtrToStringUTF8((IntPtr)dstPath)     ?? "";
                string df  = Marshal.PtrToStringUTF8((IntPtr)dstFormatId) ?? "";

                var result = new OGEditorSDK.MapFormat.OGMapConverter().Convert(sp, sf, dp, df);

                var sb = new System.Text.StringBuilder();
                sb.Append("{\"success\":").Append(result.Success ? "true" : "false");
                sb.Append(",\"fidelity\":").Append(result.Fidelity.ToString("F3",
                    System.Globalization.CultureInfo.InvariantCulture));
                sb.Append(",\"diagnostics\":[");
                bool first = true;
                foreach (var d in result.Diagnostics)
                {
                    if (!first) sb.Append(',');
                    first = false;
                    sb.Append("{\"severity\":\"").Append(d.Severity).Append("\"")
                      .Append(",\"message\":\"").Append(Esc(d.Message)).Append("\"")
                      .Append("}");
                }
                sb.Append("]}");
                return WriteUtf8(sb.ToString(), outResultJson, bufLen);
            }
            catch (Exception ex)
            {
                string err = "{\"success\":false,\"fidelity\":0,\"diagnostics\":[{\"severity\":\"Error\",\"message\":\"" +
                             Esc(ex.Message) + "\"}]}";
                return WriteUtf8(err, outResultJson, bufLen);
            }
        }

        // ── Utility ───────────────────────────────────────────────────────────────

        [UnmanagedCallersOnly(EntryPoint = "ogeditor_version")]
        public static int Version(byte* outBuf, int bufLen) => WriteUtf8("1.0.0", outBuf, bufLen);

        [UnmanagedCallersOnly(EntryPoint = "ogeditor_last_error")]
        public static int LastError(IntPtr handle, byte* outBuf, int bufLen)
        {
            if (!TryGet(handle, out var ctx)) return WriteUtf8("invalid handle", outBuf, bufLen);
            return WriteUtf8(ctx.LastError, outBuf, bufLen);
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static int WriteUtf8(string s, byte* buf, int bufLen)
        {
            if (buf == null || bufLen <= 0) return -2;
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(s ?? "");
            if (bytes.Length >= bufLen) return -2;
            Marshal.Copy(bytes, 0, (IntPtr)buf, bytes.Length);
            buf[bytes.Length] = 0;
            return 0;
        }

        private static string Esc(string s) =>
            (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"");

        private static int SetErr(Context ctx, Exception ex, int code)
        {
            ctx.LastError = ex.Message;
            return code;
        }

        private static int SetErr(Context ctx, string msg, int code)
        {
            ctx.LastError = msg;
            return code;
        }

        private static IReadOnlyDictionary<string, int> GetMappingForGame(string gameId)
        {
            if (string.Equals(gameId, OGameId.OQUAKE,  StringComparison.OrdinalIgnoreCase)) return OGEntityMappings.QuakeClassToDoom;
            if (string.Equals(gameId, OGameId.OQUAKE2, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(gameId, OGameId.OQUAKE2_RTX, StringComparison.OrdinalIgnoreCase)) return OGEntityMappings.Quake2ClassToDoom;
            if (string.Equals(gameId, OGameId.OQUAKE3, StringComparison.OrdinalIgnoreCase)) return OGEntityMappings.Quake3ClassToDoom;
            if (string.Equals(gameId, OGameId.ODUKE3D, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(gameId, OGameId.ODUKE3D_RT, StringComparison.OrdinalIgnoreCase)) return OGEntityMappings.DukeActorToDoom;
            if (string.Equals(gameId, OGameId.OWOLF3D, StringComparison.OrdinalIgnoreCase)) return OGEntityMappings.WolfActorToDoom;
            return null;
        }

        private static PortalEntry ParsePortalJson(string json)
        {
            // Minimal parse — the JSON shape matches what we write via ogeditor_append_portal
            static int    I(string k, string s) { var m = System.Text.RegularExpressions.Regex.Match(s, "\"" + k + "\":\\s*(-?\\d+)"); return m.Success ? int.Parse(m.Groups[1].Value) : 0; }
            static double D(string k, string s) { var m = System.Text.RegularExpressions.Regex.Match(s, "\"" + k + "\":\\s*(-?[\\d.]+)"); return m.Success ? double.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) : 0; }
            static string S(string k, string s) { var m = System.Text.RegularExpressions.Regex.Match(s, "\"" + k + "\":\\s*\"([^\"]*)\""); return m.Success ? m.Groups[1].Value : ""; }
            return new PortalEntry
            {
                ThingId         = I("thingId",         json),
                X               = D("x",               json),
                Y               = D("y",               json),
                DestinationGame = S("destinationGame",  json),
                DestinationMap  = S("destinationMap",   json),
                DestinationX    = D("destinationX",     json),
                DestinationY    = D("destinationY",     json),
                DestinationZ    = D("destinationZ",     json),
            };
        }
    }
}
