using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BreakDrop.Editor
{
    [InitializeOnLoad]
    public static class SupportSampleAuthoring
    {
        internal const string ReportRoot="docs/validation/support-samples";
        private const string Request="Temp/breakdrop-support-samples.request";
        private const string ManifestPath = SupportSampleSession.DirectoryPath + "/generation-manifest.json";
        private const int RecipeRevision=2;
        [Serializable] private sealed class Selection { public int sample,attempt,seed; public int[] route; }
        [Serializable] private sealed class Manifest { public int revision=RecipeRevision;public Selection[] selections; }
        [Serializable] private sealed class Report
        {
            public string unityVersion=Application.unityVersion;
            public int sample,seed,attempt;public string name;
            public LevelDefinition level;
            public List<string> checks=new List<string>();
            public List<SupportSampleSimulation.Result> runs=new List<SupportSampleSimulation.Result>();
            public SupportSampleSimulation.Result branch;
        }
        static SupportSampleAuthoring()
        {
            EditorApplication.update+=Poll;
            EditorApplication.playModeStateChanged+=s=>{ if(s==PlayModeStateChange.EnteredEditMode)SupportSampleSession.Clear(); };
        }
        internal static void Progress(string text)
        {
            Directory.CreateDirectory(ReportRoot);File.WriteAllText(ReportRoot+"/status.txt",DateTime.Now.ToString("O")+"\n"+text);
        }
        private static void Poll()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(Request))return;
            string action=File.ReadAllText(Request).Trim();File.Delete(Request);
            try
            {
                if(action=="generate")Generate();
                else if(action.StartsWith("preview "))Preview(int.Parse(action.Substring(8)));
            }
            catch(Exception ex){ Progress("FAILED "+ex);Debug.LogException(ex); }
        }
        [MenuItem("Tools/Break & Drop/Support Samples/Generate and Validate Five Samples")]
        public static void Generate()
        {
            Directory.CreateDirectory(ReportRoot);
            var manifest=File.Exists(ManifestPath)?JsonUtility.FromJson<Manifest>(File.ReadAllText(ManifestPath)):null;
            if(manifest!=null && (manifest.revision!=RecipeRevision||manifest.selections==null||manifest.selections.Length!=5))
                throw new InvalidOperationException("Recipe manifest version mismatch. Keep existing samples; review the recipe revision before regeneration.");
            var accepted=new List<Report>();var signatures=new HashSet<string>();
            using(var sim=new SupportSampleSimulation())
            for(int sample=1;sample<=5;sample++)
            {
                Report report=null;var seen=new HashSet<string>();
                var pinned=manifest?.selections.SingleOrDefault(x=>x.sample==sample);
                foreach(int attempt in pinned==null?Enumerable.Range(0,100):new[]{pinned.attempt})
                {
                    var level=SupportSampleRecipes.Create(sample,attempt);
                    string signature=JsonUtility.ToJson(level);
                    if(!seen.Add(signature))continue;
                    sim.Configure(level);string error=StaticCheck(level)??sim.InitialCheck(sample);
                    if(error!=null){Reject(sample,attempt,error);continue;}
                    var route=pinned!=null?(int[])pinned.route.Clone():sim.Search(sample, message=> { Progress(message); File.AppendAllText(ReportRoot+"/search.txt",message+"\n"); });
                    if(route==null){Reject(sample,attempt,"no intended route <=7");continue;}
                    // Greedy single-deletion minimization. Minimal under this operation, not a global optimum.
                    bool changed=pinned==null;
                    while(changed)
                    {
                        changed=false;
                        for(int i=0;i<route.Length;i++)
                        {
                            var shorter=route.Where((_,index)=>i!=index).ToArray();
                            bool repeatable=true;
                            for(int repeat=0;repeat<3;repeat++)if(!sim.Run(shorter).win){repeatable=false;break;}
                            if(repeatable){route=shorter;changed=true;break;}
                        }
                    }
                    if(route.Length<3||!SupportSampleSimulation.Meaningful(sample,route)){Reject(sample,attempt,"route lost defining mechanism after minimization");continue;}
                    level.hintOrder=route;level.bestMoves=route.Length;
                    report=new Report{sample=sample,seed=20261010+sample*7919+attempt*104729,attempt=attempt,name=SupportSampleRecipes.Names[sample-1],level=level};
                    for(int run=0;run<10;run++)
                    {
                        // Recreate bodies between repeats, not only reuse Restart contacts.
                        sim.Configure(level);var result=sim.Run(route);report.runs.Add(result);
                        if(!SupportSampleSimulation.Mechanism(sample,result)){error="reference route "+string.Join(",",route)+" failed repeat "+run+" "+result.reason; File.WriteAllText(ReportRoot+"/repeat-failure-"+sample+".json",JsonUtility.ToJson(result,true));break;}
                    }
                    if(error!=null){Reject(sample,attempt,error);report=null;continue;}
                    var reference=report.runs[0];
                    if(sample>=2)
                    {
                        for(int i=0;i<route.Length-1;i++)
                        {
                            var swapped=(int[])route.Clone();int temp=swapped[i];swapped[i]=swapped[i+1];swapped[i+1]=temp;
                            var branch=sim.Run(swapped);
                            int index=Math.Min(i+2,branch.frames.Count-1);
                            var a=reference.frames[Math.Min(index,reference.frames.Count-1)];var b=branch.frames[index];
                            if(branch.reason=="Fail"||Vector2.Distance(a.target,b.target)>.25f||Mathf.Abs(Mathf.DeltaAngle(a.angle,b.angle))>10||!a.supports.SequenceEqual(b.supports))
                            { report.branch=branch;break; }
                        }
                        if(report.branch==null){Reject(sample,attempt,"no consequential swapped-order branch");report=null;continue;}
                    }
                    if(!signatures.Add(PrototypeLevelBatchGenerator.LayoutSignature(level))){report=null;continue;}
                    report.checks.Add("PASS initial contacts, static geometry, stable stack, ten supported wins and intended mechanism");
                    Interactions(sim,level,report.checks);
                    var regenerated=SupportSampleRecipes.Create(sample,attempt);regenerated.hintOrder=route;regenerated.bestMoves=route.Length;
                    if(JsonUtility.ToJson(level)!=JsonUtility.ToJson(regenerated))throw new Exception("Nondeterministic generation");
                    report.checks.Add("PASS byte-identical regeneration with accepted seed");break;
                }
                if(report==null)throw new Exception("Sample "+sample+" exhausted candidates; existing samples retained.");
                accepted.Add(report);File.WriteAllText(ReportRoot+"/validated-"+sample+".json",JsonUtility.ToJson(report,true));Progress("Validated sample "+sample);
            }
            // Only publish once all five passed; roll back bytes if a write fails.
            Directory.CreateDirectory(SupportSampleSession.DirectoryPath);
            var prior=new Dictionary<string,string>();
            try
            {
                foreach(var report in accepted)
                {
                    string path=SupportSampleSession.DirectoryPath+"/sample_"+report.sample.ToString("00")+".json";
                    prior[path]=File.Exists(path)?File.ReadAllText(path):null;
                    File.WriteAllText(path,JsonUtility.ToJson(report.level,true));
                    string reportPath=ReportRoot+"/sample_"+report.sample.ToString("00")+"-report.json";
                    prior[reportPath]=File.Exists(reportPath)?File.ReadAllText(reportPath):null;
                    File.WriteAllText(reportPath,JsonUtility.ToJson(report,true));
                }
                prior[ManifestPath]=File.Exists(ManifestPath)?File.ReadAllText(ManifestPath):null;
                File.WriteAllText(ManifestPath,JsonUtility.ToJson(new Manifest { selections=accepted.Select(r=>new Selection{sample=r.sample,attempt=r.attempt,seed=r.seed,route=r.level.hintOrder}).ToArray() },true));
            }
            catch { foreach(var pair in prior) {if(pair.Value==null)File.Delete(pair.Key);else File.WriteAllText(pair.Key,pair.Value);}throw; }
            AssetDatabase.Refresh();Progress("PASS five independent samples; 50 supported reference runs. Production levels unchanged.");
            Debug.Log("[Support Samples] Five samples generated and validated.");
        }
        private static void Reject(int sample,int attempt,string reason) => File.AppendAllText(ReportRoot+"/rejections.txt","S"+sample+" attempt "+attempt+": "+reason+"\n");
        private static string StaticCheck(LevelDefinition l)
        {
            if(l.blocks.Length<8||l.blocks.Length>16)return "block count";
            if(l.blocks.Any(b=>b.fixedBlock||!b.breakable))return "non-dynamic piece";
            int horizontal=l.blocks.Count(b=>b.shape=="Rectangle" && b.width>b.height);
            int nonHorizontal=l.blocks.Length-horizontal;
            int categories=l.blocks.Select(b=>b.shape.StartsWith("L")?"L":b.width>b.height?"Horizontal":b.height>b.width?"Vertical":"Square").Distinct().Count();
            if(nonHorizontal<2 || horizontal>(l.blocks.Length*4)/5 || categories<3)
                return "insufficient structural variety: need >=3 shape/orientation categories, >=2 non-horizontal pieces and <=80% horizontal bars";
            foreach(var group in l.blocks.Where(b=>b.width==2&&b.height==1&&b.rotation==0).GroupBy(b=>b.x))
            {
                var ys=group.Select(b=>b.y).OrderBy(y=>y).ToArray();
                for(int i=2;i<ys.Length;i++)if(Mathf.Abs(ys[i]-ys[i-1]-1)<.01f&&Mathf.Abs(ys[i-1]-ys[i-2]-1)<.01f)return "three aligned short bars";
            }
            return null;
        }
        private static void Interactions(SupportSampleSimulation sim,LevelDefinition l,List<string> checks)
        {
            sim.Configure(l);sim.Step(180);var m=sim.Manager;var initial=SupportSampleSimulation.Snapshot(m);
            m.Hint();if(m.Moves!=0||m.Blocks.Any(b=>b.IsBroken)||Vector2.Distance(initial.target,m.Target.Body.position)>.0001f)throw new Exception("Hint changed physics");
            var first=m.Blocks.First(b=>b.Id==l.hintOrder[0]);m.TryBreak(first);
            m.TryBreak(m.Blocks.First(b=>b.Id!=first.Id));if(m.Moves!=1)throw new Exception("Resolving input not locked");
            if(first.Colliders.Any(c=>c.enabled))throw new Exception("Collider not removed");
            sim.Step(10);m.Undo();var undo=SupportSampleSimulation.Snapshot(m);
            if(undo.broken.Any(x=>x)||Vector2.Distance(initial.target,undo.target)>.0001f||initial.blocks.Where((p,i)=>Vector2.Distance(p,undo.blocks[i])>.0001f).Any())throw new Exception("Undo mismatch");
            m.Restart();sim.Step(180);if(m.Moves!=0||m.Blocks.Any(b=>b.IsBroken)||Vector2.Distance(initial.target,m.Target.Body.position)>.02f)throw new Exception("Restart mismatch");
            checks.Add("PASS Hint does not move bodies; one-click collider removal; resolving rejects input; Undo and Restart restore state");
        }
        [MenuItem("Tools/Break & Drop/Support Samples/Play Sample 1")] private static void Play1()=>Preview(1);
        [MenuItem("Tools/Break & Drop/Support Samples/Play Sample 2")] private static void Play2()=>Preview(2);
        [MenuItem("Tools/Break & Drop/Support Samples/Play Sample 3")] private static void Play3()=>Preview(3);
        [MenuItem("Tools/Break & Drop/Support Samples/Play Sample 4")] private static void Play4()=>Preview(4);
        [MenuItem("Tools/Break & Drop/Support Samples/Play Sample 5")] private static void Play5()=>Preview(5);
        private static void Preview(int number)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play first.");
            if(SupportSampleSession.Catalog().Length!=5)throw new InvalidOperationException("Generate the five samples first.");
            if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Save the scene first.");
            EditorSceneManager.OpenScene("Assets/BreakDrop/Scenes/Prototype.unity");
            SessionState.SetInt(SupportSampleSession.SessionKey+".Selected",number);
            SessionState.SetBool(SupportSampleSession.SessionKey,true);
            EditorApplication.isPlaying=true;
        }
    }
}
