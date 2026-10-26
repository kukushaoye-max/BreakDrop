using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BreakDrop.Editor
{
    [InitializeOnLoad]
    public static class UguiAuthoring
    {
        public const string PrefabPath = "Assets/BreakDrop/Resources/UI/CandyCanvas.prefab";
        private const string Root = "docs/validation/ugui";
        private static readonly string[] Pages = { "home","select","game","settings","energy","supplies","daily","pause","restart","noad","win","fail","message" };
        private static double next;
        static UguiAuthoring() { EditorApplication.update += Poll; }

        [MenuItem("Tools/Break & Drop/UI/Build Canvas Prefab and Scene")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play before building UI.");
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != "Assets/BreakDrop/Scenes/Prototype.unity" || scene.isDirty)
                throw new InvalidOperationException("Open and save Prototype scene before building UI.");
            SessionState.SetInt("BreakDrop.UI.Build",1);
            Directory.CreateDirectory(Root);
            File.WriteAllText(Root+"/build-status.txt","Entering Play to materialize all UI pages.");
            EditorApplication.isPlaying = true;
        }
        private static void Export(BreakDropHud hud)
        {
            foreach (string name in Pages) { hud.PreviewForAudit(name); hud.SendMessage("LateUpdate"); }
            hud.PreviewForAudit("select");
            var manager=hud.GetComponent<BreakDropGameManager>();
            for(int i=1;i<=manager.LevelCount;i++) { manager.LoadLevel(i); hud.SendMessage("LateUpdate"); }
            // Materialize conditional states without writing the player's save.
            hud.PreviewForAudit("daily"); hud.Economy.ClaimDaily(); hud.SendMessage("LateUpdate");
            hud.PreviewForAudit("energy"); hud.Economy.Data.energy=10; hud.SendMessage("LateUpdate");
            hud.PreviewForAudit("home"); hud.SendMessage("LateUpdate");
            Canvas.ForceUpdateCanvases();
            var view = hud.GetComponentInChildren<CandyUguiView>(true);
            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
            PrefabUtility.SaveAsPrefabAsset(view.gameObject,PrefabPath);
            AssetDatabase.SaveAssets();
            File.WriteAllText(Root+"/build-status.txt","Exported all 13 pages; attaching prefab after leaving Play.");
            SessionState.SetInt("BreakDrop.UI.Build",2);
            EditorApplication.isPlaying=false;
        }
        public static void AttachToScene()
        {
            var host=UnityEngine.Object.FindObjectOfType<BreakDropBootstrap>();
            if(!host) throw new Exception("Bootstrap missing.");
            if(!host.GetComponentInChildren<CandyUguiView>(true))
            {
                var asset=AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                if(!asset) throw new Exception("Canvas prefab missing.");
                var canvas=(GameObject)PrefabUtility.InstantiatePrefab(asset,host.transform);
                canvas.name="Canvas";
            }
            if(!UnityEngine.Object.FindObjectOfType<EventSystem>()) new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
            EditorSceneManager.MarkSceneDirty(host.gameObject.scene);
            EditorSceneManager.SaveScene(host.gameObject.scene);
            Selection.activeGameObject=host.GetComponentInChildren<CandyUguiView>(true).gameObject;
            EditorGUIUtility.PingObject(Selection.activeGameObject);
            File.WriteAllText(Root+"/hierarchy.txt",string.Join("\n",host.GetComponentsInChildren<Transform>(true).Select(t=>PathOf(t))));
            File.WriteAllText(Root+"/build-status.txt","PASS Canvas prefab and Prototype scene saved. UI exists outside Play mode.");
            Debug.Log("[UGUI Authoring] Saved Canvas prefab and scene hierarchy.");
        }
        private static string PathOf(Transform t) => t.parent ? PathOf(t.parent)+"/"+t.name : t.name;
        private static void Inspect()
        {
            var type=typeof(EditorWindow).Assembly.GetType("UnityEditor.LogEntries");
            var args=new object[]{0,0,0};
            type.GetMethod("GetCountsByType",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic).Invoke(null,args);
            File.WriteAllText(Root+"/editor-status.txt","Unity "+Application.unityVersion+"\nplaying="+EditorApplication.isPlaying+"\ncompiling="+EditorApplication.isCompiling+"\nconsole errors="+args[0]+" warnings="+args[1]+" logs="+args[2]);
        }
        private static void Poll()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.timeSinceStartup<next)return;
            next=EditorApplication.timeSinceStartup+.3;
            try
            {
                int state=SessionState.GetInt("BreakDrop.UI.Build",0);
                if(state==1 && EditorApplication.isPlaying)
                {
                    var hud=UnityEngine.Object.FindObjectOfType<BreakDropHud>();
                    if(hud) Export(hud);
                    return;
                }
                if(state==2 && !EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    SessionState.SetInt("BreakDrop.UI.Build",0); AttachToScene(); return;
                }
                string request=Root+"/authoring-request.txt";
                if(!File.Exists(request))return;
                string action=File.ReadAllText(request).Trim(); File.Delete(request);
                if(action=="build")Build();
                else if(action=="inspect")Inspect();
                else if(action=="clear-console") typeof(EditorWindow).Assembly.GetType("UnityEditor.LogEntries").GetMethod("Clear",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic).Invoke(null,null);
                else if(action=="viewport")ViewportValidation.Run();
            }
            catch(Exception ex)
            {
                SessionState.SetInt("BreakDrop.UI.Build",0);
                Directory.CreateDirectory(Root); File.WriteAllText(Root+"/build-error.txt",ex.ToString()); Debug.LogException(ex);
            }
        }
    }
}
