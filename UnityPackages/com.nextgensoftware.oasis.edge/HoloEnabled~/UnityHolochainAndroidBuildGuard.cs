#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace NextGenSoftware.OASIS.Edge.Unity.Holochain.Editor
{
    /// <summary>Fails the player build when its Android contract cannot run the bundled conductor.</summary>
    public sealed class UnityHolochainAndroidBuildGuard : IPreprocessBuildWithReport
    {
        public int callbackOrder => -1000;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.Android) return;
            if (PlayerSettings.Android.minSdkVersion < AndroidSdkVersions.AndroidApiLevel27)
                throw new BuildFailedException(
                    "The HoloEnabled OASIS Edge profile requires Android API level 27 or newer.");
            if (PlayerSettings.Android.targetArchitectures != AndroidArchitecture.ARM64)
                throw new BuildFailedException(
                    "The pinned Holochain mobile runtime is ARM64-only; select only ARM64 in Android Player Settings.");
        }
    }
}
#endif
