using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BreakDrop.Editor
{
    [InitializeOnLoad]
    public static class BreakDropProjectBuilder
    {
        private const string SceneDirectory = "Assets/BreakDrop/Scenes";
        private const string ScenePath = SceneDirectory + "/Prototype.unity";

        static BreakDropProjectBuilder()
        {
            EditorApplication.delayCall += EnsurePrototypeScene;
        }

        [MenuItem("Tools/Break & Drop/Build Prototype Scene")]
        public static void BuildPrototypeScene()
        {
            if (!Directory.Exists(SceneDirectory))
            {
                Directory.CreateDirectory(SceneDirectory);
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 6.2f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.035f, 0.045f, 0.12f);
            cameraObject.AddComponent<AudioListener>();

            GameObject bootstrap = new GameObject("Break Drop Bootstrap");
            bootstrap.AddComponent<BreakDropBootstrap>();
            var canvasPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(UguiAuthoring.PrefabPath);
            if (canvasPrefab) PrefabUtility.InstantiatePrefab(canvasPrefab, bootstrap.transform);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

            PlayerSettings.productName = "Break & Drop";
            PlayerSettings.companyName = "Break Drop Prototype";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeGameObject = bootstrap;
            Debug.Log($"Break & Drop prototype scene created at {ScenePath}");
        }

        public static void BuildForBatchMode()
        {
            BuildPrototypeScene();
            EditorApplication.Exit(0);
        }

        private static void EnsurePrototypeScene()
        {
            if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                BuildPrototypeScene();
            }
        }
    }
}
