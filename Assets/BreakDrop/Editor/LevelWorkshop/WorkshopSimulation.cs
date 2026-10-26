using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace BreakDrop.Editor
{
    internal sealed class WorkshopSimulation : IDisposable
    {
        private Scene scene;
        private readonly SimulationMode2D previousMode;
        public BreakDropGameManager Manager { get; private set; }
        public WorkshopSimulation(LevelDefinition level)
        {
            previousMode=Physics2D.simulationMode;Physics2D.simulationMode=SimulationMode2D.Script;
            try
            {
                scene=EditorSceneManager.NewPreviewScene();
                if(scene.GetPhysicsScene2D()==Physics2D.defaultPhysicsScene) throw new Exception("Preview scene does not provide isolated 2D physics");
                var host=new GameObject("Workshop Simulation");SceneManager.MoveGameObjectToScene(host,scene);
                Manager=host.AddComponent<BreakDropGameManager>();Manager.WorkshopSimulation=true;
                Manager.SetEditorCatalog(new[]{WorkshopCore.Clone(level)});Manager.LoadLevel(1);
            }
            catch{Dispose();throw;}
        }
        public void Step() { scene.GetPhysicsScene2D().Simulate(1f/60);Manager.TickPhysics(1f/60); }
        public void Dispose() { if(scene.IsValid())EditorSceneManager.ClosePreviewScene(scene);Physics2D.simulationMode=previousMode; }
    }
    internal static class WorkshopValidation
    {
        [Serializable] internal sealed class Frame { public Vector2 target;public float angle;public int removed;public int[] supports; }
        [Serializable] internal sealed class Result { public bool won;public bool initialError;public string error;public float score;public int[] remaining;public List<Frame> frames=new List<Frame>(); }
        static Frame Snapshot(BreakDropGameManager m,int removed=0) => new Frame{target=m.Target.Body.position,angle=m.Target.Body.rotation,removed=removed,supports=SupportSampleSimulation.Supports(m)};
        internal static string InitialGeometryError(BreakDropGameManager manager)
        {
            Physics2D.SyncTransforms();
            var colliders=manager.gameObject.scene.GetRootGameObjects().SelectMany(go=>go.GetComponentsInChildren<Collider2D>()).Where(c=>c.enabled&&!c.isTrigger).ToArray();
            for(int a=0;a<colliders.Length;a++)for(int b=a+1;b<colliders.Length;b++)
                if(colliders[a].attachedRigidbody!=colliders[b].attachedRigidbody&&colliders[a].Distance(colliders[b]).distance<-.04f)
                    return "初始碰撞体穿插："+ColliderLabel(colliders[a])+" 与 "+ColliderLabel(colliders[b])+"；请先调整位置，再试玩和验证";
            return null;
        }
        static string ColliderLabel(Collider2D collider)
        {
            var block=collider.GetComponentInParent<BreakableBlock>();
            return block!=null?"积木 "+block.Id:collider.gameObject.name;
        }
        internal static string InitialStateError(BreakDropGameManager manager)
        {
            if(manager.State!=BreakDropGameState.Playing)return "初始结构不稳定或自动过关："+manager.State;
            if(SupportSampleSimulation.Supports(manager).Length==0)return "目标初始没有实际承托，请调整六边形起点或支撑积木";
            return null;
        }
        internal static IEnumerator Route(LevelDefinition level,int[] route,Action<Result> done)
        {
            var result=new Result();
            using(var world=new WorkshopSimulation(level))
            {
                var m=world.Manager;
                string initial=InitialGeometryError(m);
                if(initial!=null){result.initialError=true;result.error=initial;done(result);yield break;}
                for(int i=0;i<180;i++){world.Step();if(i%30==0)yield return null;}
                initial=InitialStateError(m);
                if(initial!=null){result.initialError=true;result.error=initial;done(result);yield break;}
                result.frames.Add(Snapshot(m));
                foreach(int id in route)
                {
                    var b=m.Blocks.FirstOrDefault(x=>x.Id==id&&x.CanRemove&&!x.IsBroken);
                    if(b==null||m.State!=BreakDropGameState.Playing){result.error="路线引用无效或多余步骤";done(result);yield break;}
                    m.TryBreak(b);
                    for(int frame=0;frame<1200 && m.State==BreakDropGameState.Resolving;frame++)
                    {world.Step();if(frame%30==0)yield return null;}
                    for(int frame=0;frame<40;frame++){world.Step();if(frame%30==0)yield return null;}
                    result.frames.Add(Snapshot(m,id));
                    if(m.State==BreakDropGameState.Failed||m.State==BreakDropGameState.Resolving){result.error="掉出边界或稳定超时: "+m.State+" target="+m.Target.Body.position+" time="+m.TimeRemaining;done(result);yield break;}
                }
                result.won=m.State==BreakDropGameState.Won&&m.HasGoalSupport();
                result.score=m.Target.Body.position.y+Mathf.Abs(m.Target.Body.position.x-level.goal.x)*.45f;
                result.remaining=m.Blocks.Where(b=>b.CanRemove&&!b.IsBroken).Select(b=>b.Id).ToArray();
                result.error=result.won?null:"路线尚未到达终点";done(result);
            }
        }
        internal static IEnumerator Validate(WorkshopDraft draft)
        {
            var level=WorkshopCore.Clone(draft.level);var key=WorkshopCore.ValidationKey(level);
            string error=WorkshopCore.StaticError(level);
            if(error!=null){Finish(draft,"拒绝",error,"");yield break;}
            var route=level.hintOrder??Array.Empty<int>();Result result=null;
            var run=Route(level,route,r=>result=r);try{while(run.MoveNext())yield return run.Current;}finally{(run as IDisposable)?.Dispose();}
            if(result.initialError){Finish(draft,"结构检查失败",result.error,"");yield break;}
            string referenceFailure=route.Length>0?"已保存路线复现失败："+result.error+"。\n":"";
            int tested=1;
            if(!result.won)
            {
                var beam=new List<int[]>{Array.Empty<int>()};bool found=false;
                for(int depth=0;depth<=12&&!found&&tested<2000;depth++)
                {
                    var next=new List<Tuple<int[],float>>();
                    foreach(var candidate in beam)
                    {
                        if(tested++>=2000)break;
                        run=Route(level,candidate,r=>result=r);try{while(run.MoveNext())yield return run.Current;}finally{(run as IDisposable)?.Dispose();}
                        draft.report="搜索深度 "+depth+"，评估 "+tested+" / 2000";
                        if(result.won){route=candidate;found=true;break;}
                        if(result.remaining!=null && depth<12)
                            foreach(var id in result.remaining)next.Add(Tuple.Create(candidate.Concat(new[]{id}).ToArray(),result.score));
                    }
                    beam=next.OrderBy(x=>x.Item2).ThenBy(x=>string.Join(",",x.Item1)).Take(48).Select(x=>x.Item1).ToList();
                    if(beam.Count==0)break;
                }
                if(!found){Finish(draft,"未找到解",referenceFailure+"达到搜索预算或无可继续路线；不等于无解","");yield break;}
            }
            // Greedy deletion gives an irreducible reference route, not a proof of optimality.
            for(int index=0;index<route.Length;)
            {
                var shorter=route.Where((id,i)=>i!=index).ToArray();
                run=Route(level,shorter,r=>result=r);try{while(run.MoveNext())yield return run.Current;}finally{(run as IDisposable)?.Dispose();}
                if(result.won){route=shorter;index=0;}else index++;
            }
            for(int repeat=0;repeat<10;repeat++)
            {
                run=Route(level,route,r=>result=r);try{while(run.MoveNext())yield return run.Current;}finally{(run as IDisposable)?.Dispose();}
                if(!result.won){Finish(draft,"不稳定","重建复测失败 "+(repeat+1),"");yield break;}
                draft.report="物理重建验证 "+(repeat+1)+" / 10";
            }
            if(key!=WorkshopCore.ValidationKey(draft.level)){Finish(draft,"待验证","验证期间内容已变化","");yield break;}
            draft.replayJson=JsonUtility.ToJson(result,true);
            int transitions=0;for(int i=1;i<result.frames.Count;i++)if(!result.frames[i-1].supports.SequenceEqual(result.frames[i].supports))transitions++;
            draft.difficulty=route.Length+transitions+(level.mechanisms?.Length??0)*2>=10?"进阶":route.Length<=3?"入门":"普通";
            draft.level.hintOrder=route;draft.level.bestMoves=route.Length;
            Finish(draft,"已验证","10/10 重建通关；参考步数 "+route.Length+"；待人工试玩及真机验收",WorkshopCore.ValidationKey(draft.level));
        }
        static void Finish(WorkshopDraft d,string status,string report,string hash)
        {d.status=status;d.report=report;d.validatedHash=hash;EditorUtility.SetDirty(d);AssetDatabase.SaveAssets();}
    }
    [InitializeOnLoad] internal static class WorkshopRunner
    {
        private static IEnumerator job;
        public static bool Busy=>job!=null;
        public static string Error="";
        public static string Status="空闲";
        static WorkshopRunner(){EditorApplication.update+=Tick;AssemblyReloadEvents.beforeAssemblyReload+=Cancel;EditorApplication.quitting+=Cancel;}
        public static void Start(IEnumerator next)
        {
            if(Busy)throw new Exception("已有验证任务");
            if(next==null)throw new ArgumentNullException(nameof(next));
            Error="";Status="正在验证";job=next;
        }
        [MenuItem("Tools/Break & Drop/Stop Workshop Task")]
        public static void Cancel()
        {
            var previous=job;job=null; // Clear first: a Dispose failure must never leave the task busy.
            if(previous==null)return;
            Status="任务已停止，批量任务可续跑";
            try{(previous as IDisposable)?.Dispose();}
            catch(Exception e){Error="停止时清理失败："+e.Message;Debug.LogException(e);}
        }
        private static void Tick()
        {
            if(job==null)return;
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling){Cancel();return;}
            var watch=System.Diagnostics.Stopwatch.StartNew();
            try
            {
                // Consume simulation yields within a bounded editor frame, rather than one yield per update.
                do
                {
                    if(!job.MoveNext()){Cancel();Status="任务已完成";break;}
                }while(job!=null&&watch.ElapsedMilliseconds<8);
            }
            catch(Exception e){Cancel();Error=e.ToString();Status="任务失败，已停止";Debug.LogException(e);}
        }
    }
}
