using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace BreakDrop.Editor
{
    public static class WorkshopAcceptance
    {
        [Serializable] class Report { public List<string> passed=new List<string>();public List<string> failed=new List<string>(); }
        public static void Run()
        {
            var report=new Report();
            Action<string,Action> check=(name,action)=>{try{action();report.passed.Add(name);}catch(Exception e){report.failed.Add(name+": "+e.Message);}};
            report.passed.Add("physics settings: "+Physics2D.gravity+" / "+Physics2D.velocityIterations+" / "+Physics2D.positionIterations);
            check("block palette matches all production art and dimensions",()=>
            {
                var expected=new Dictionary<string,string>{
                    {"BlockA","Rectangle:2:1"},{"BlockB","Rectangle:2:1"},{"BlockC","Rectangle:1:1"},
                    {"BlockD","L3:3:3"},{"BlockE","Rectangle:1:1"},{"BlockG","Rectangle:3:1"},
                    {"BlockI","Rectangle:2:1"},{"BlockJ","L2:2:3"},{"BlockK","Rectangle:1:1"},
                    {"BlockL","Rectangle:1:1"},{"BlockR","Rectangle:5:1"}};
                var art=Directory.GetFiles("Assets/BreakDrop/Resources/Art","Block*.png").Select(Path.GetFileNameWithoutExtension).OrderBy(x=>x);
                if(!art.SequenceEqual(WorkshopBlockCatalog.Entries.Select(x=>x.art).OrderBy(x=>x)))throw new Exception("missing or duplicate palette art");
                int id=1;
                foreach(var entry in WorkshopBlockCatalog.Entries)
                {
                    var block=entry.Create(id++,90);
                    if(expected[block.art]!=block.shape+":"+block.width+":"+block.height||block.rotation!=90||!block.breakable||block.fixedBlock||block.mass<=0)throw new Exception(entry.label);
                    var again=entry.Create(id,0);block.width=99;if(again.width==99)throw new Exception("shared mutable preset");
                }
            });
            check("old simulator baseline",()=>{using(var sim=new SupportSampleSimulation()){var l=WorkshopCore.Preset(0,0);sim.Configure(l);var result=sim.Run(l.hintOrder);if(!result.win)throw new Exception(result.reason);}});
            var original=Directory.GetFiles("Assets/BreakDrop/Resources/Levels","*.json").ToDictionary(x=>x,File.ReadAllText);
            check("geometry rejects overlap",()=>{var l=WorkshopCore.Preset(5,0);l.blocks[1].x=l.blocks[0].x;l.blocks[1].y=l.blocks[0].y;WorkshopValidation.Result result=null;var run=WorkshopValidation.Route(l,l.hintOrder,x=>result=x);try{while(run.MoveNext()){} }finally{(run as IDisposable)?.Dispose();}if(result.won||!result.initialError||!result.error.StartsWith("初始碰撞体穿插"))throw new Exception("not rejected");});
            check("cancel disposes physics scene",()=>{int before=UnityEngine.SceneManagement.SceneManager.sceneCount;var mode=Physics2D.simulationMode;var run=WorkshopValidation.Route(WorkshopCore.Preset(0,0),new[]{6},_=>{});run.MoveNext();(run as IDisposable).Dispose();if(before!=UnityEngine.SceneManagement.SceneManager.sceneCount||mode!=Physics2D.simulationMode)throw new Exception("scene or simulation state leaked");});
            check("legacy JSON roundtrip",()=>{foreach(var f in original){var l=JsonUtility.FromJson<LevelDefinition>(f.Value);if(WorkshopCore.Fingerprint(l)!=WorkshopCore.Fingerprint(WorkshopCore.Clone(l)))throw new Exception(f.Key);}});
            check("unknown mechanism rejected",()=>{var l=WorkshopCore.Preset(6,0);l.mechanisms[0].type="spring";if(WorkshopCore.StaticError(l)==null)throw new Exception("not rejected");});
            check("invalid hinge rejected",()=>{var l=WorkshopCore.Preset(6,0);l.mechanisms[0].anchorX=500;if(WorkshopCore.StaticError(l)==null)throw new Exception("not rejected");});
            check("NaN hinge rejected",()=>{var l=WorkshopCore.Preset(6,0);l.mechanisms[0].lowerAngle=float.NaN;if(WorkshopCore.StaticError(l)==null)throw new Exception("not rejected");});
            check("deterministic layout",()=>{for(int i=0;i<7;i++)if(WorkshopCore.Fingerprint(WorkshopCore.Preset(i,0))!=WorkshopCore.Fingerprint(WorkshopCore.Preset(i,0)))throw new Exception("preset "+i);});
            check("local physics isolation and hinge reset",()=>
            {
                var l=WorkshopCore.Preset(6,0);var gravity=Physics2D.gravity;float time=Time.timeScale;
                using(var sim=new WorkshopSimulation(l))
                {
                    var block=sim.Manager.Blocks.First(b=>b.Id==1);var hinge=block.GetComponent<HingeJoint2D>();
                    if(hinge==null||block.CanRemove||!hinge.useLimits)throw new Exception("hinge config");
                    for(int i=0;i<180;i++)sim.Step();sim.Manager.Restart();
                    if(Vector2.Distance(block.Body.position,new Vector2(l.blocks[0].x,l.blocks[0].y))>.001f)throw new Exception("reset");
                }
                if(Physics2D.gravity!=gravity||Time.timeScale!=time)throw new Exception("global state changed");
            });
            for(int i=0;i<7;i++)
            {int index=i;check("preset route "+i,()=>{var l=WorkshopCore.Preset(index,0);WorkshopValidation.Result result=null;var run=WorkshopValidation.Route(l,l.hintOrder,r=>result=r);try{while(run.MoveNext()){} }finally{(run as IDisposable)?.Dispose();}if(!result.won)throw new Exception(result.error);});}
            foreach(var file in original.Keys)
            {
                var level=JsonUtility.FromJson<LevelDefinition>(original[file]);if(!level.towerLevel)continue;
                check("production reference x10 "+Path.GetFileName(file),()=>
                {
                    for(int repeat=0;repeat<10;repeat++)
                    {
                        WorkshopValidation.Result result=null;var run=WorkshopValidation.Route(level,level.hintOrder,x=>result=x);
                        try{while(run.MoveNext()){} }finally{(run as IDisposable)?.Dispose();}
                        if(!result.won)throw new Exception("repeat "+repeat+": "+result.error);
                    }
                });
            }
            for(int kind=5;kind<=6;kind++)for(int variant=0;variant<3;variant++)
            {
                int k=kind,v=variant;
                check("mechanism example x10 "+kind+"/"+variant,()=>
                {
                    var l=WorkshopCore.Preset(k,0);l.blocks[0].width+=v*.25f;
                    for(int repeat=0;repeat<10;repeat++)
                    {
                        WorkshopValidation.Result result=null;var run=WorkshopValidation.Route(l,l.hintOrder,x=>result=x);
                        try{while(run.MoveNext()){} }finally{(run as IDisposable)?.Dispose();}
                        if(!result.won)throw new Exception("repeat "+repeat+": "+result.error);
                    }
                    string title=WorkshopWindow.Presets[k]+" 示例"+(v+1);
                    var existing=WorkshopCore.All().FirstOrDefault(d=>d.title==title);
                    // Do not overwrite an example that a designer has edited.
                    if(existing==null||WorkshopCore.Fingerprint(existing.level)==WorkshopCore.Fingerprint(l))
                    {
                        var d=existing??WorkshopCore.SaveNew(l,title,WorkshopWindow.Presets[k]);d.validatedHash=WorkshopCore.ValidationKey(l);d.status="已验证";d.report="10/10 重建通关；待人工精选与真机验收";EditorUtility.SetDirty(d);AssetDatabase.SaveAssets();
                    }
                });
            }
            for(int sample=0;sample<5;sample++)
            {
                string title=WorkshopWindow.Presets[sample]+" 基础模板";
                if(!WorkshopCore.All().Any(d=>d.title==title))
                {var d=WorkshopCore.SaveNew(WorkshopCore.Preset(sample,0),title,WorkshopWindow.Presets[sample]);d.template=true;EditorUtility.SetDirty(d);AssetDatabase.SaveAssets();}
            }
            check("validator search and repeat workflow",()=>
            {
                var draft=ScriptableObject.CreateInstance<WorkshopDraft>();
                try
                {
                    draft.level=WorkshopCore.Preset(5,0);draft.level.hintOrder=Array.Empty<int>();
                    var validation=WorkshopValidation.Validate(draft);
                    try{while(validation.MoveNext()){} }finally{(validation as IDisposable)?.Dispose();}
                    if(draft.status!="已验证"||draft.level.hintOrder.Length==0||draft.validatedHash!=WorkshopCore.ValidationKey(draft.level)||string.IsNullOrEmpty(draft.replayJson))throw new Exception(draft.status+" "+draft.report);
                }
                finally{UnityEngine.Object.DestroyImmediate(draft);}
            });
            check("production files preserved",()=>{foreach(var f in original)if(File.ReadAllText(f.Key)!=f.Value)throw new Exception(f.Key);});
            Directory.CreateDirectory("docs/validation/level-workshop");File.WriteAllText("docs/validation/level-workshop/acceptance.json",JsonUtility.ToJson(report,true));
            Debug.Log("Workshop acceptance: "+report.passed.Count+" passed / "+report.failed.Count+" failed");
            if(report.failed.Count>0)throw new Exception("Workshop acceptance failed; inspect acceptance.json");
        }
    }
}
