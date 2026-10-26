using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BreakDrop.Editor
{
    // Explicit file-requested editor diagnostics. Never builds a player or modifies authored scenes.
    [InitializeOnLoad]
    public static class CandyVisualAudit
    {
        private const string Root = "docs/validation/candy-fidelity";
        private static double next;
        private static readonly string[] Pages = { "home", "select", "settings", "energy", "supplies", "daily", "game", "pause", "restart", "noad", "win", "fail", "message" };
        private static double captureAt;
        private static string pendingCapture;
        static CandyVisualAudit() { EditorApplication.update += Poll; }
        private static void Progress(string message)
        {
            Directory.CreateDirectory(Root);
            File.WriteAllText(Root + "/progress.txt", DateTime.Now.ToString("O") + "\n" + message);
        }
        private static void ResetDeadline(int seconds) =>
            SessionState.SetString("CandyAudit.Deadline", DateTime.UtcNow.AddSeconds(seconds).Ticks.ToString());
        [MenuItem("Tools/Break & Drop/Capture Candy UI Audit")]
        public static void StartAudit()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play mode before starting the visual audit.");
            for (int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save scene changes before running the visual audit.");
            EditorSceneManager.OpenScene("Assets/BreakDrop/Scenes/Prototype.unity");
            if (!UnityEngine.Object.FindObjectOfType<BreakDropBootstrap>())
                throw new InvalidOperationException("Prototype scene has no BreakDropBootstrap.");
            var view=EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));
            view.Show(); view.Focus(); view.Repaint();
            pendingCapture=null;
            SessionState.SetInt("CandyAudit.Index",0);
            SessionState.SetBool("CandyAudit.Running",true);
            ResetDeadline(60);
            Progress("Starting Play in Prototype scene; waiting for HUD (60s timeout).");
            EditorApplication.isPlaying=true;
        }
        private static void Poll()
        {
            if (EditorApplication.timeSinceStartup < next || EditorApplication.isCompiling) return;
            next = EditorApplication.timeSinceStartup + 1;
            if (SessionState.GetBool("CandyAudit.Running", false))
            {
                try
                {
                if (long.TryParse(SessionState.GetString("CandyAudit.Deadline","0"),out long deadline) &&
                    DateTime.UtcNow.Ticks>deadline)
                    throw new TimeoutException("Visual audit timed out: playing="+EditorApplication.isPlaying+
                        ", scene="+UnityEngine.SceneManagement.SceneManager.GetActiveScene().path+
                        ", page="+SessionState.GetInt("CandyAudit.Index",0));
                if (!EditorApplication.isPlaying || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
                var hud = UnityEngine.Object.FindObjectOfType<BreakDropHud>();
                if (!hud) return;
                Application.runInBackground = true;
                EditorApplication.isPaused = false;
                hud.SendMessage("LateUpdate"); Canvas.ForceUpdateCanvases();
                if (pendingCapture != null)
                {
                    if (EditorApplication.timeSinceStartup < captureAt) return;
                    Capture(pendingCapture + "-after");
                    Progress("Captured "+pendingCapture);
                    pendingCapture = null;
                    SessionState.SetInt("CandyAudit.Index", SessionState.GetInt("CandyAudit.Index", 0) + 1);
                }
                int index = SessionState.GetInt("CandyAudit.Index", 0);
                if (index >= Pages.Length)
                {
                    SessionState.SetBool("CandyAudit.Running", false);
                    File.WriteAllText(Root + "/visual-audit.txt", DateTime.Now.ToString("O") + "\nCaptured " + Pages.Length + " actual GameView render textures. Unity " + Application.unityVersion + "\nFixtures use a disposable save; result pages are visual fixtures, not physics outcome proofs.");
                    EditorApplication.isPlaying = false;
                    Progress("Completed all "+Pages.Length+" pages.");
                    return;
                }
                hud.PreviewForAudit(Pages[index]);
                pendingCapture=Pages[index];captureAt=EditorApplication.timeSinceStartup+3;
                ResetDeadline(30);
                Progress("Rendering "+pendingCapture+" (30s timeout).");
                var view=Resources.FindObjectsOfTypeAll(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).FirstOrDefault() as EditorWindow;
                if(view) {view.Show();view.Focus();view.Repaint();}
                EditorApplication.QueuePlayerLoopUpdate();
                }
                catch (Exception ex)
                {
                    SessionState.SetBool("CandyAudit.Running", false);
                    pendingCapture = null;
                    File.WriteAllText(Root + "/error.txt", ex.ToString());
                    Progress("FAILED: "+ex.Message);
                    Debug.LogException(ex);
                    EditorApplication.isPlaying = false;
                }
                return;
            }
            string request = Root + "/request.txt";
            if (!File.Exists(request)) return;
            string action = File.ReadAllText(request).Trim();
            File.Move(request, Root + "/request-" + DateTime.Now.Ticks + ".done");
            try
            {
                if (action == "refresh") AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                if (action == "shutdown")
                {
                    if (EditorApplication.isPlayingOrWillChangePlaymode)
                        throw new InvalidOperationException("Stop the audit before shutdown.");
                    for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
                        if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                            throw new InvalidOperationException("Unsaved scene; shutdown refused.");
                    EditorApplication.Exit(0);
                }
                if (action == "inspect")
                {
                    Type t = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
                    string info = string.Join("\n", t.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static).Select(m => m.ToString()));
                    File.WriteAllText(Root + "/editor-state.txt", "playing=" + EditorApplication.isPlaying + "\nscene=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().path + "\n" + info);
                }
                if (action == "play") EditorApplication.isPlaying = true;
                if (action == "audit")
                {
                    StartAudit();
                }
                if (action == "stop") EditorApplication.isPlaying = false;
                if (action == "import")
                {
                    foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/BreakDrop/Resources/Candy/Approved" }))
                    {
                        var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));
                        importer.isReadable = true; importer.mipmapEnabled = false; importer.alphaIsTransparency = true;
                        importer.npotScale = TextureImporterNPOTScale.None; importer.maxTextureSize = 2048;
                        importer.textureCompression = TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
                    }
                }
                if (action.StartsWith("page "))
                {
                    var hud = UnityEngine.Object.FindObjectOfType<BreakDropHud>();
                    if (!hud) throw new Exception("Enter Play before preview.");
                    hud.PreviewForAudit(action.Substring(5));
                    EditorApplication.QueuePlayerLoopUpdate();
                }
                if (action == "tests") CandyAcceptance.Run();
                if (action == "physics") TowerAcceptance.Run();
                if (action.StartsWith("capture")) Capture(action.Replace("capture", "").Trim());
            }
            catch (Exception ex) { File.WriteAllText(Root + "/error.txt", ex.ToString()); Debug.LogException(ex); }
        }
        private static void Capture(string name)
        {
            Type t = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            var window = Resources.FindObjectsOfTypeAll(t).FirstOrDefault() as EditorWindow;
            if (window == null) throw new Exception("Open the Game tab before capture.");
            window.Repaint();
            var field = t.GetField("m_RenderTexture", BindingFlags.NonPublic | BindingFlags.Instance);
            var texture = field?.GetValue(window) as RenderTexture;
            if (texture == null) throw new Exception("GameView targetTexture unavailable.");
            var prior = RenderTexture.active; RenderTexture.active = texture;
            var copy = new Texture2D(texture.width, texture.height, TextureFormat.RGB24, false);
            copy.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0); copy.Apply();
            if (SystemInfo.graphicsUVStartsAtTop)
            {
                Color[] pixels=copy.GetPixels(); int w=copy.width,h=copy.height;
                var flipped=new Color[pixels.Length];
                for(int y=0;y<h;y++) Array.Copy(pixels,y*w,flipped,(h-y-1)*w,w);
                copy.SetPixels(flipped); copy.Apply();
            }
            File.WriteAllBytes(Root + "/" + (string.IsNullOrEmpty(name) ? "game" : name) + ".png", copy.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(copy); RenderTexture.active = prior;
        }
    }
}

