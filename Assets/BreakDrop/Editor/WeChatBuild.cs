using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using WeChatWASM;

namespace BreakDrop.Editor
{
    [InitializeOnLoad]
    public static class WeChatBuild
    {
        private const string Request = "Temp/breakdrop-wechat-build.request";
        private const string Status = "Builds/wechat-build-status.txt";
        static WeChatBuild() { EditorApplication.update += Poll; }

        [MenuItem("Tools/Break & Drop/Build WeChat Mini Game")]
        public static void RequestBuild()
        {
            Directory.CreateDirectory("Temp");
            File.WriteAllText(Request, "build");
        }

        private static void Poll()
        {
            if (!File.Exists(Request) || EditorApplication.isCompiling ||
                EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode ||
                BuildPipeline.isBuildingPlayer) return;
            Directory.CreateDirectory("Builds");
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
            {
                File.WriteAllText(Status, "Switching to WebGL");
                if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL))
                {
                    File.Delete(Request);
                    File.WriteAllText(Status, "FAILED: WebGL target switch failed");
                }
                return;
            }
            File.Delete(Request);
            Build();
        }

        public static void Build()
        {
            Directory.CreateDirectory("Builds");
            File.WriteAllText(Status, "Building");
            try
            {
                ViewportValidation.Run();
                var config = WXConvertCore.config;
                config.ProjectConf.Appid = "wx49aa8a94c17a8cca";
                config.ProjectConf.projectName = "BreakDrop";
                config.ProjectConf.relativeDST = "Builds/WeChat";
                config.ProjectConf.DST = Path.GetFullPath("Builds/WeChat");
                // Preserve the CDN configured in the WeChat SDK export settings.
                config.ProjectConf.assetLoadType = string.IsNullOrWhiteSpace(config.ProjectConf.CDN) ? 1 : 0;
                config.ProjectConf.compressDataPackage = true;
                config.ProjectConf.preloadFiles = "";
                config.ProjectConf.Orientation = (WXScreenOritation)0;
                PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
                PlayerSettings.defaultWebScreenWidth = 390;
                PlayerSettings.defaultWebScreenHeight = 844;
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
                var result = WXConvertCore.DoExport();
                if (result != WXConvertCore.WXExportError.SUCCEED)
                    throw new Exception("WeChat SDK export failed: " + result);
                if (config.ProjectConf.assetLoadType == 0 && string.IsNullOrWhiteSpace(config.ProjectConf.CDN))
                    throw new Exception("Resource package requires a CDN URL; configure the WeChat SDK export settings.");
                File.WriteAllText(Status, "SDK export returned success; verify generated packages and Console");
                Debug.Log("[BreakDrop WeChat] Export returned success: " + config.ProjectConf.DST);
            }
            catch (Exception ex)
            {
                File.WriteAllText(Status, "FAILED: " + ex);
                Debug.LogException(ex);
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }
    }
}




