using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace BreakDrop.Editor
{
    [Serializable] internal sealed class WorkshopBatchState
    {
        public int next,total=100,seed=1001,preset;
        public string templateJson,templateTitle,variationJson,version="workshop-v2";
        public List<string> created=new List<string>();
        public List<string> rejected=new List<string>();
    }
    internal static class WorkshopBatch
    {
        const string PathName="Library/LevelWorkshop/batch.json";
        public static bool HasCheckpoint=>File.Exists(PathName);
        public static void Begin(int preset,WorkshopDraft template,int seed,int total)
        {
            if(WorkshopRunner.Busy)throw new Exception("请先停止当前任务");
            var s=new WorkshopBatchState{preset=preset,seed=seed,total=Mathf.Clamp(total,1,1000),
                templateJson=template==null?null:JsonUtility.ToJson(template.level),templateTitle=template?.title,variationJson=template==null?null:JsonUtility.ToJson(template)};
            Save(s);WorkshopRunner.Start(Run(s));
        }
        public static void Resume()
        {
            var state=JsonUtility.FromJson<WorkshopBatchState>(File.ReadAllText(PathName));
            if(state==null||state.version!="workshop-v2")throw new Exception("生成器版本已变化，请开始新的批次");
            WorkshopRunner.Start(Run(state));
        }
        static void Save(WorkshopBatchState s){Directory.CreateDirectory(System.IO.Path.GetDirectoryName(PathName));File.WriteAllText(PathName,JsonUtility.ToJson(s,true));}
        static IEnumerator Run(WorkshopBatchState s)
        {
            var signatures=new HashSet<string>(WorkshopCore.All().Select(d=>WorkshopCore.Fingerprint(d.level)));
            for(;s.next<s.total;s.next++)
            {
                WorkshopRunner.Status="批量候选 "+(s.next+1)+" / "+s.total;
                int candidateSeed=unchecked(s.seed+s.next*104729);WorkshopDraft d=null;
                // A cancelled validation resumes the same saved candidate instead of regenerating it.
                if(s.created.Count>s.next&&string.IsNullOrEmpty(s.created[s.next]))continue;
                if(s.created.Count>s.next)
                {
                    d=AssetDatabase.LoadAssetAtPath<WorkshopDraft>(s.created[s.next]);
                    if(d==null)
                    {
                        // An intentionally deleted candidate must not reappear on resume.
                        s.created[s.next]="";s.rejected.Add(s.next+": draft removed");Save(s);
                        yield return null;continue;
                    }
                }
                if(d==null)
                {
                    var level=string.IsNullOrEmpty(s.templateJson)?WorkshopCore.Preset(s.preset,s.next):JsonUtility.FromJson<LevelDefinition>(s.templateJson);
                    if(!string.IsNullOrEmpty(s.variationJson))
                    {
                        var t=ScriptableObject.CreateInstance<WorkshopDraft>();
                        try
                        {
                        JsonUtility.FromJsonOverwrite(s.variationJson,t);
                        var random=new System.Random(candidateSeed);
                        foreach(var b in level.blocks.Where(b=>!t.lockedBlocks.Contains(b.id)&&t.ranges.Length==0))
                        {b.x+=Mathf.Round(((float)random.NextDouble()*2-1)*t.positionVariation/.25f)*.25f;}
                        foreach(var range in t.ranges)
                        {
                            if(range.step<=0||float.IsNaN(range.step)||float.IsInfinity(range.step)||(double)(range.max-range.min)/range.step>1000000||range.min>range.max||float.IsNaN(range.min)||float.IsNaN(range.max)||float.IsInfinity(range.min)||float.IsInfinity(range.max))throw new Exception("Invalid template range");
                            if(range.blockId!=0&&t.lockedBlocks.Contains(range.blockId))continue;
                            float value=range.min+random.Next(1+Mathf.FloorToInt((range.max-range.min)/range.step))*range.step;
                            if(range.blockId==0)
                            {
                                if(range.property=="x")level.target.x=value;else if(range.property=="y")level.target.y=value;else throw new Exception("Target supports x/y ranges only");
                            }
                            else
                            {
                                var b=level.blocks.FirstOrDefault(x=>x.id==range.blockId);if(b==null)throw new Exception("Range references missing block");
                                if(new[]{"x","y","width","height","mass","rotation"}.Contains(range.property))typeof(BlockDefinition).GetField(range.property).SetValue(b,value);
                                else if(new[]{"anchorX","anchorY","lowerAngle","upperAngle"}.Contains(range.property))
                                {var m=level.mechanisms.FirstOrDefault(x=>x.blockId==b.id);if(m==null)throw new Exception("Range references missing hinge");typeof(MechanismDefinition).GetField(range.property).SetValue(m,value);}
                                else throw new Exception("Unsupported range property "+range.property);
                            }
                        }
                        }
                        finally{UnityEngine.Object.DestroyImmediate(t);}
                    }
                    else
                    {
                        // Mass variations retain authored bearing geometry while changing balance.
                        var random=new System.Random(candidateSeed);
                        foreach(var b in level.blocks.Where(b=>!b.fixedBlock))b.mass*=new[]{.8f,1f,1.2f}[random.Next(3)];
                    }
                    if(!signatures.Add(WorkshopCore.Fingerprint(level)))
                    {s.rejected.Add(s.next+": duplicate");s.created.Add("");Save(s);yield return null;continue;}
                    d=WorkshopCore.SaveNew(level,(s.templateTitle??WorkshopWindow.Presets[s.preset])+" #"+(s.next+1),s.templateTitle??WorkshopWindow.Presets[s.preset],candidateSeed);
                    if(s.created.Count>s.next)s.created[s.next]=AssetDatabase.GetAssetPath(d);else s.created.Add(AssetDatabase.GetAssetPath(d));Save(s);
                }
                var validation=WorkshopValidation.Validate(d);
                var deadline=EditorApplication.timeSinceStartup+30;
                bool timedOut=false;
                try
                {
                    while(validation.MoveNext())
                    {
                        WorkshopRunner.Status="批量候选 "+(s.next+1)+" / "+s.total+" · "+d.title+" · "+d.report;
                        if(EditorApplication.timeSinceStartup>=deadline){timedOut=true;break;}
                        yield return validation.Current;
                    }
                }
                finally{(validation as IDisposable)?.Dispose();}
                if(timedOut)
                {
                    d.status="验证超时";d.report="单候选达到 30 秒预算，已跳过；可单独验证";
                    d.validatedHash="";d.selectedForRelease=false;EditorUtility.SetDirty(d);AssetDatabase.SaveAssets();
                }
                Save(s);yield return null;
            }
            Save(s);
        }
    }
}
