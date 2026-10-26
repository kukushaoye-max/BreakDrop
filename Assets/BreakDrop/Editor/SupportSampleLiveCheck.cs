using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BreakDrop.Editor
{
    [InitializeOnLoad]
    public static class SupportSampleLiveCheck
    {
        private const string Key = "BreakDrop.SampleLiveCheck";
        [Serializable] private sealed class Report
        {
            public string unityVersion = Application.unityVersion;
            public List<SupportSampleSimulation.Result> runs = new List<SupportSampleSimulation.Result>();
            public List<string> failures = new List<string>();
        }
        static SupportSampleLiveCheck()
        {
            EditorApplication.update += Poll;
            EditorApplication.playModeStateChanged += state => {
                if(state==PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Key+".Restore",false))
                {
                    Physics2D.simulationMode=(SimulationMode2D)SessionState.GetInt(Key+".PhysicsMode",(int)SimulationMode2D.FixedUpdate);
                    SessionState.SetBool(Key+".Restore",false);
                }
            };
        }
        [MenuItem("Tools/Break & Drop/Support Samples/Validate Play Mode Ten Times")]
        public static void Start()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play first.");
            SessionState.SetInt(Key+".PhysicsMode",(int)Physics2D.simulationMode);
            SessionState.SetBool(Key+".Restore",true);
            SessionState.SetBool(Key, true);
            File.WriteAllText("Temp/breakdrop-support-samples.request", "preview 1");
        }
        private static void Poll()
        {
            const string request = "Temp/breakdrop-sample-live.request";
            if (File.Exists(request) && !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling)
            { SessionState.SetBool(Key+".Search",File.ReadAllText(request).Trim()=="deletions"); File.Delete(request); Start(); return; }
            if (!SessionState.GetBool(Key, false) || !EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            var manager = UnityEngine.Object.FindObjectOfType<BreakDropGameManager>();
            if (!manager || manager.Target == null) return;
            SessionState.SetBool(Key, false);
            var report = new Report();
            try
            {
                manager.enabled = false;
                Physics2D.simulationMode = SimulationMode2D.Script;
                var hud = manager.GetComponent<BreakDropHud>();
                hud.enabled = false; hud.CloseDialog(); manager.SetPaused(false);
                if(SessionState.GetBool(Key+".Search",false)) { CheckDeletions(manager); return; }
                for (int sample = 1; sample <= 5; sample++)
                for (int repeat = 0; repeat < 10; repeat++)
                {
                    // Alternate reconstructed worlds and actual Restart to cover both contact histories.
                    if (repeat % 2 == 0) manager.ShowHome();
                    manager.LoadLevel(sample); manager.InputLocked = false; Tick(manager, 180);
                    var result = new SupportSampleSimulation.Result { route = manager.Level.hintOrder };
                    result.frames.Add(SupportSampleSimulation.Snapshot(manager));
                    foreach (int id in result.route)
                    {
                        if (manager.State != BreakDropGameState.Playing) break;
                        manager.TryBreak(manager.Blocks.First(b => b.Id == id));
                        for (int t = 0; t < 1200 && manager.State == BreakDropGameState.Resolving; t++) Tick(manager, 1);
                        Tick(manager, 40);
                        result.frames.Add(SupportSampleSimulation.Snapshot(manager, id));
                    }
                    result.win = manager.State == BreakDropGameState.Won && manager.HasGoalSupport();
                    result.reason = "Sample " + sample + " repeat " + repeat + ": " + manager.State;
                    report.runs.Add(result);
                    if (!result.win) report.failures.Add(result.reason);
                }
            }
            catch (Exception ex) { report.failures.Add(ex.ToString()); Debug.LogException(ex); }
            finally
            {
                if(!SessionState.GetBool(Key+".Search",false)) File.WriteAllText(SupportSampleAuthoring.ReportRoot + "/live-validation.json", JsonUtility.ToJson(report, true));
                EditorApplication.isPlaying = false;
            }
        }
        private static void CheckDeletions(BreakDropGameManager m)
        {
            var output=new List<string>();
            for(int sample=1;sample<=5;sample++)
            {
                m.LoadLevel(sample);var original=m.Level.hintOrder;
                for(int omit=0;omit<original.Length;omit++)
                {
                    var route=original.Where((id,index)=>index!=omit).ToArray();
                    bool pass=true;int completed=0;
                    for(int repeat=0;repeat<20;repeat++)
                    {
                        if(repeat%2==0)m.ShowHome();m.LoadLevel(sample);m.InputLocked=false;Tick(m,180);
                        foreach(int id in route){if(m.State!=BreakDropGameState.Playing){pass=false;break;}m.TryBreak(m.Blocks.First(b=>b.Id==id));for(int t=0;t<1200&&m.State==BreakDropGameState.Resolving;t++)Tick(m,1);Tick(m,40);}
                        if(m.State!=BreakDropGameState.Won||!m.HasGoalSupport())pass=false;
                        if(!pass)break;completed++;
                    }
                    output.Add("S"+sample+" omit "+original[omit]+" "+(pass?"PASS":"FAIL")+" completed="+completed+" route="+string.Join(",",route));
                    File.WriteAllLines(SupportSampleAuthoring.ReportRoot+"/reference-deletions.txt",output);
                }
            }
        }
        private static void Tick(BreakDropGameManager manager, int count)
        {
            for (int i = 0; i < count; i++) { Physics2D.Simulate(1f / 60); manager.TickPhysics(1f / 60); }
        }
    }
}