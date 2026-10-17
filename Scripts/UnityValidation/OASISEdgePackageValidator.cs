#if UNITY_EDITOR
using System;
using System.IO;
using System.Security.Cryptography;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.Edge.Unity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NextGenSoftware.OASIS.Edge.Unity.Editor
{
    public static class OASISEdgePackageValidator
    {
        public static void Validate()
        {
#if UNITY_EDITOR_WIN || UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX
            // Previously Windows-only: every desktop editor must prove its selected protected-store round trip.
            Guid avatarId = Guid.NewGuid();
            Guid deviceId = Guid.NewGuid();
            var store = new UnityPlatformSecureSessionStore(avatarId, deviceId);
            var grant = new HyperDriveOfflineSessionGrant
            {
                GrantId = Guid.NewGuid().ToString("N"), AvatarId = avatarId, DeviceId = deviceId,
                IssuedUtc = DateTime.UtcNow, ExpiresUtc = DateTime.UtcNow.AddMinutes(5),
                Scopes = new[] { "edge:play" }, Signature = "package-validation"
            };
            try
            {
                var saved = store.SaveAsync(grant, default).GetAwaiter().GetResult();
                if (saved.IsError || !saved.Result) throw new InvalidOperationException(saved.Message);
                var loaded = store.LoadAsync(default).GetAwaiter().GetResult();
                if (loaded.IsError || loaded.Result == null || loaded.Result.GrantId != grant.GrantId ||
                    loaded.Result.AvatarId != avatarId || loaded.Result.DeviceId != deviceId)
                    throw new InvalidOperationException(loaded.Message ?? "Desktop secure-session round trip failed.");
            }
            finally
            {
                var deleted = store.DeleteAsync(default).GetAwaiter().GetResult();
                if (deleted.IsError) Debug.LogError(deleted.Message);
            }
#endif
            Debug.Log($"OASIS_EDGE_UNITY_PACKAGE_VALIDATION_PASSED manifest={PackageManifestHash()}");
            EditorApplication.Exit(0);
        }

        public static void ValidateAndroidBuild()
        {
            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
                throw new InvalidOperationException("Unity could not activate the Android build target.");
            string packageManifest = PackageManifestPath();
            string packageManifestJson = File.ReadAllText(packageManifest);
            // All Edge profiles must prove the shipped Android ARM64 IL2CPP contract, not Unity's Mono/ARMv7 defaults.
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            if (packageManifestJson.IndexOf("\"profile\": \"HoloEnabled\"", StringComparison.Ordinal) >= 0)
            {
                PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel27;
                // Retired Holo-only backend/architecture assignments: the common profile boundary now owns them above.
                // Retired: AssetDatabase.SaveAssets(); common save below persists settings for every profile.
                Debug.Log($"OASIS_HOLO_ANDROID_SETTINGS minSdk={PlayerSettings.Android.minSdkVersion} " +
                    $"backend={PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android)} " +
                    $"architectures={PlayerSettings.Android.targetArchitectures}");
            }
            AssetDatabase.SaveAssets();
            const string scenePath = "Assets/OASISEdgeValidation.unity";
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            if (!EditorSceneManager.SaveScene(scene, scenePath))
                throw new InvalidOperationException($"Unity could not save the Android validation scene at '{scenePath}'.");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
            if (sceneAsset == null)
                throw new InvalidOperationException($"Unity did not import the Android validation scene at '{scenePath}'.");
            string importedScenePath = AssetDatabase.GetAssetPath(sceneAsset);
            Directory.CreateDirectory("Build");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { importedScenePath },
                locationPathName = "Build/OASISEdgeValidation.apk",
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.CleanBuildCache
            });
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new InvalidOperationException($"Android Edge package build failed: {report.summary.result} ({report.summary.totalErrors} errors).");
            Debug.Log($"OASIS_EDGE_ANDROID_BUILD_VALIDATION_PASSED manifest={PackageManifestHash()}");
            EditorApplication.Exit(0);
        }

        private static string PackageManifestHash()
        {
            string path = PackageManifestPath();
            if (!File.Exists(path)) throw new FileNotFoundException("The OASIS Edge package manifest is missing.", path);
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", string.Empty);
        }

        private static string PackageManifestPath()
        {
            var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(OASISEdgeUnityHost).Assembly);
            if (package == null || string.IsNullOrWhiteSpace(package.resolvedPath))
                throw new InvalidOperationException("Unity could not resolve the OASIS Edge package path.");
            return Path.Combine(package.resolvedPath, "build-manifest.json");
        }
    }
}
#endif
