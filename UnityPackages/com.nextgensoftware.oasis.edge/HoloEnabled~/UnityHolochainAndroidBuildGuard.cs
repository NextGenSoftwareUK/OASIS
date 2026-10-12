#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace NextGenSoftware.OASIS.Edge.Unity.Holochain.Editor
{
    /// <summary>Fails the player build when its Android or iOS contract cannot run the bundled conductor.</summary>
    public sealed class UnityHolochainAndroidBuildGuard : IPreprocessBuildWithReport
    {
        public int callbackOrder => -1000;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform == BuildTarget.iOS)
            {
                CheckIos();
                return;
            }
            if (report.summary.platform != BuildTarget.Android) return;
            if (PlayerSettings.Android.minSdkVersion < AndroidSdkVersions.AndroidApiLevel27)
                throw new BuildFailedException(
                    "The HoloEnabled OASIS Edge profile requires Android API level 27 or newer.");
            if (PlayerSettings.Android.targetArchitectures != AndroidArchitecture.ARM64)
                throw new BuildFailedException(
                    "The pinned Holochain mobile runtime is ARM64-only; select only ARM64 in Android Player Settings.");
        }

        /// <summary>The in-process iOS conductor needs iOS 15+ and the packaged xcframework.</summary>
        private static void CheckIos()
        {
            if (!Version.TryParse(PlayerSettings.iOS.targetOSVersionString, out var minimum) || minimum < new Version(15, 0))
                throw new BuildFailedException(
                    "The HoloEnabled OASIS Edge profile requires a minimum iOS version of 15.0 (Player Settings > Target minimum iOS Version).");
            if (AssetDatabase.FindAssets("HolochainConductor").Length == 0 ||
                !Array.Exists(AssetDatabase.FindAssets("HolochainConductor"),
                    guid => AssetDatabase.GUIDToAssetPath(guid).EndsWith("HolochainConductor.xcframework", StringComparison.Ordinal)))
                throw new BuildFailedException(
                    "HolochainConductor.xcframework is missing. Package the HoloEnabled profile with the iOS conductor " +
                    "(Scripts/fetch_holochain_ios_runtime.ps1, then Scripts/build_edge_unity_package.ps1).");
        }
    }
}
#endif
