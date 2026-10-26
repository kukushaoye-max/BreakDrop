using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace BreakDrop.Editor
{
    [InitializeOnLoad]
    public static class SupportSampleCapture
    {
        private const string Key="BreakDrop.SampleCapture";
        private static double due;
        private static int sample=1,step;
        private static bool prepared;
        private static string SaveSnapshot() => string.Join("\n",new[]{"breakdrop.candy.save.v1","breakdrop.tower.level"}
            .Concat(Enumerable.Range(1,23).Select(n=>"breakdrop.tower.best."+n)).Select(k=>k+":"+PlayerPrefs.HasKey(k)+":"+PlayerPrefs.GetString(k,"<int>")+":"+PlayerPrefs.GetInt(k,-1)));
        static SupportSampleCapture()
        {
            EditorApplication.update+=Poll;
            EditorApplication.playModeStateChanged+=state=> {
                if(state==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool(Key,false))
                {
                    bool same=SessionState.GetString(Key+".Save","")==SaveSnapshot();
                    File.WriteAllText(SupportSampleAuthoring.ReportRoot+"/capture-status.txt",(same && sample>5?"PASS":"FAIL")+" production PlayerPrefs unchanged; captured "+(sample-1)+" sample routes.");
                    Physics2D.simulationMode=(SimulationMode2D)SessionState.GetInt(Key+".PhysicsMode",(int)SimulationMode2D.FixedUpdate);
                    SessionState.SetBool(Key,false);
                }
            };
        }
        [MenuItem("Tools/Break & Drop/Support Samples/Capture All Reference Routes")]
        public static void Start()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play first.");
            sample=1;step=0;prepared=false;
            if(File.Exists(SupportSampleAuthoring.ReportRoot+"/capture-error.txt"))File.Delete(SupportSampleAuthoring.ReportRoot+"/capture-error.txt");
            Directory.CreateDirectory(SupportSampleAuthoring.ReportRoot+"/screenshots");
            SessionState.SetInt(Key+".PhysicsMode",(int)Physics2D.simulationMode);
            SessionState.SetString(Key+".Save",SaveSnapshot());SessionState.SetBool(Key,true);
            File.WriteAllText("Temp/breakdrop-support-samples.request","preview 1");
        }
        private static void Poll()
        {
            string request="Temp/breakdrop-sample-capture.request";
            if(File.Exists(request)&&!EditorApplication.isPlayingOrWillChangePlaymode&&!EditorApplication.isCompiling)
            { File.Delete(request);Start(); }
            if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||EditorApplication.isCompiling||EditorApplication.timeSinceStartup<due)return;
            try
            {
                var hud=UnityEngine.Object.FindObjectOfType<BreakDropHud>();if(!hud)return;
                var m=hud.GetComponent<BreakDropGameManager>();
                Application.runInBackground=true;
                if(!GameObject.Find("Support Sample Capture Background"))
                {
                    var backdrop=new GameObject("Support Sample Capture Background").AddComponent<Camera>();
                    backdrop.depth=-100;backdrop.cullingMask=0;backdrop.clearFlags=CameraClearFlags.SolidColor;
                    backdrop.backgroundColor=Color.black;
                }
                Physics2D.simulationMode=SimulationMode2D.Script;
                m.enabled=false;hud.enabled=false;
                foreach(var input in UnityEngine.Object.FindObjectsOfType<UnityEngine.EventSystems.BaseInputModule>())input.enabled=false;
                hud.CloseDialog();m.SetPaused(false);
                var window=EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));window.Show();window.Focus();
                if(!prepared)
                {
                    m.InputLocked=false;m.LoadLevel(sample);Tick(m,180);m.InputLocked=true;
                    step=0;prepared=true;hud.SendMessage("LateUpdate");Canvas.ForceUpdateCanvases();
                    due=EditorApplication.timeSinceStartup+1.5;window.Repaint();EditorApplication.QueuePlayerLoopUpdate();return;
                }
                hud.SendMessage("LateUpdate");Canvas.ForceUpdateCanvases();
                File.WriteAllText(SupportSampleAuthoring.ReportRoot+"/screenshots/sample-"+sample.ToString("00")+"-step-"+step.ToString("00")+".json",JsonUtility.ToJson(SupportSampleSimulation.Snapshot(m),true));
                Capture(window,SupportSampleAuthoring.ReportRoot+"/screenshots/sample-"+sample.ToString("00")+"-step-"+step.ToString("00")+".png");
                if(step>=m.Level.hintOrder.Length)
                {
                    if(m.State!=BreakDropGameState.Won)throw new Exception("Live capture route did not win sample "+sample);
                    sample++;prepared=false;
                    if(sample>5){EditorApplication.isPlaying=false;return;}
                }
                else
                {
                    m.InputLocked=false;m.TryBreak(m.Blocks.First(b=>b.Id==m.Level.hintOrder[step]));m.InputLocked=true;
                    for(int i=0;i<1200&&m.State==BreakDropGameState.Resolving;i++)Tick(m,1);
                    Tick(m,40);step++;
                }
                hud.SendMessage("LateUpdate");Canvas.ForceUpdateCanvases();window.Repaint();EditorApplication.QueuePlayerLoopUpdate();
                due=EditorApplication.timeSinceStartup+1.5;
            }
            catch(Exception ex)
            {
                File.WriteAllText(SupportSampleAuthoring.ReportRoot+"/capture-error.txt",ex.ToString());
                Debug.LogException(ex);EditorApplication.isPlaying=false;
            }
        }
        private static void Tick(BreakDropGameManager m,int ticks)
        { for(int i=0;i<ticks;i++){Physics2D.Simulate(1f/60);m.TickPhysics(1f/60);} }
        private static void Capture(EditorWindow window,string path)
        {
            var texture=window.GetType().GetField("m_RenderTexture",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(window) as RenderTexture;
            if(!texture)throw new Exception("No GameView render target");
            var previous=RenderTexture.active;var copy=new Texture2D(texture.width,texture.height,TextureFormat.RGB24,false);
            try
            {
                RenderTexture.active=texture;copy.ReadPixels(new Rect(0,0,texture.width,texture.height),0,0);copy.Apply();
                if(SystemInfo.graphicsUVStartsAtTop)
                {
                    var pixels=copy.GetPixels();var flipped=new Color[pixels.Length];
                    for(int y=0;y<texture.height;y++)Array.Copy(pixels,y*texture.width,flipped,(texture.height-y-1)*texture.width,texture.width);
                    copy.SetPixels(flipped);copy.Apply();
                }
                File.WriteAllBytes(path,copy.EncodeToPNG());
            }
            finally {RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(copy);}
        }
    }
}
