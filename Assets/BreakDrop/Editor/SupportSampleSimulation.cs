using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BreakDrop.Editor
{
    internal sealed class SupportSampleSimulation : IDisposable
    {
        [Serializable] internal sealed class Frame
        {
            public int removed; public string state; public Vector2 target; public float angle;
            public int[] supports; public Vector2[] blocks; public float[] rotations; public bool[] broken;
        }
        [Serializable] internal sealed class Result
        {
            public bool win; public string reason; public int[] route;
            public List<Frame> frames=new List<Frame>();
        }
        private Scene scene;
        private readonly Scene previous;
        private readonly SimulationMode2D mode;
        private readonly Vector2 gravity;
        private readonly int velocity, position;
        private readonly float scale;
        private readonly Rigidbody2D[] others;
        private readonly bool[] simulated;
        private GameObject host;
        private readonly Camera[] cameras;
        private readonly bool[] cameraEnabled;
        internal BreakDropGameManager Manager { get; private set; }
        internal SupportSampleSimulation()
        {
            if(Application.isPlaying) throw new InvalidOperationException("Stop Play before validation.");
            previous=SceneManager.GetActiveScene();
            if(previous.isDirty) throw new InvalidOperationException("Save the active scene before validation.");
            cameras=UnityEngine.Object.FindObjectsOfType<Camera>();cameraEnabled=cameras.Select(c=>c.enabled).ToArray();
            foreach(var c in cameras)c.enabled=false;
            mode=Physics2D.simulationMode; gravity=Physics2D.gravity;
            velocity=Physics2D.velocityIterations;position=Physics2D.positionIterations;scale=Time.timeScale;
            others=Resources.FindObjectsOfTypeAll<Rigidbody2D>().Where(b=>b.gameObject.scene.IsValid()).ToArray();
            simulated=others.Select(b=>b.simulated).ToArray();
            foreach(var b in others)b.simulated=false;

            Physics2D.simulationMode=SimulationMode2D.Script;Physics2D.gravity=new Vector2(0,-14);
            Physics2D.velocityIterations=12;Physics2D.positionIterations=8;
            try { CreateWorld(); } catch { Dispose(); throw; }
        }
        private void CreateWorld()
        {
            scene=EditorSceneManager.NewPreviewScene();
            host=new GameObject("Support Sample Validation");
            SceneManager.MoveGameObjectToScene(host,scene);
            Manager=host.AddComponent<BreakDropGameManager>();
        }
        internal void Configure(LevelDefinition level)
        {
            UnityEngine.Object.DestroyImmediate(host);
            SceneManager.SetActiveScene(previous);if(scene.IsValid())EditorSceneManager.ClosePreviewScene(scene);
            CreateWorld();Manager.SetEditorCatalog(new[]{level});Manager.LoadLevel(1);
        }
        internal void Step(int count)
        {
            for(int i=0;i<count;i++) { scene.GetPhysicsScene2D().Simulate(1f/60);Manager.TickPhysics(1f/60); }
        }
        internal void Resolve()
        {
            for(int i=0;i<1200&&(Manager.State==BreakDropGameState.Ready||Manager.State==BreakDropGameState.Resolving);i++)Step(1);
            Step(40);
        }
        internal static int[] Supports(BreakDropGameManager m)
        {
            var contacts=new ContactPoint2D[32];int count=m.Target.Collider.GetContacts(contacts);
            var ids=new HashSet<int>();
            for(int i=0;i<count;i++)
            {
                if(Mathf.Abs(contacts[i].normal.y)<.5f||contacts[i].point.y>m.Target.Body.position.y)continue;
                foreach(var c in new[]{contacts[i].collider,contacts[i].otherCollider})
                {
                    var b=c?c.GetComponentInParent<BreakableBlock>():null;
                    if(b&&!b.IsBroken)ids.Add(b.Id);
                }
            }
            return ids.OrderBy(x=>x).ToArray();
        }
        internal static Frame Snapshot(BreakDropGameManager m,int removed=0) => new Frame {
            removed=removed,state=m.State.ToString(),target=m.Target.Body.position,angle=m.Target.Body.rotation,
            supports=Supports(m),blocks=m.Blocks.Select(b=>b.Body.position).ToArray(),
            rotations=m.Blocks.Select(b=>b.Body.rotation).ToArray(),broken=m.Blocks.Select(b=>b.IsBroken).ToArray() };
        internal string InitialCheck(int sample)
        {
            Manager.Restart();
            var colliders=Manager.Blocks.SelectMany(b=>b.Colliders).Concat(new[]{Manager.Target.Collider}).ToArray();
            for(int i=0;i<colliders.Length;i++)for(int j=i+1;j<colliders.Length;j++)
            {
                if(colliders[i].attachedRigidbody==colliders[j].attachedRigidbody)continue;
                var d=colliders[i].Distance(colliders[j]);if(d.isOverlapped&&d.distance<-.006f)return "initial collider overlap "+colliders[i].name+" / "+colliders[j].name+" = "+d.distance;
            }
            var before=Snapshot(Manager);
            foreach(var b in Manager.Blocks)b.Body.WakeUp();Manager.Target.Body.WakeUp();Step(180);
            var after=Snapshot(Manager);
            if(Manager.State!=BreakDropGameState.Playing)return "not playable after initial settling";
            if(Vector2.Distance(before.target,after.target)>.15f||before.blocks.Where((v,i)=>Vector2.Distance(v,after.blocks[i])>.15f).Any())return "initial stack moved >0.15";
            if(after.supports.Length!=(sample==2?2:1))return "wrong initial support count: "+after.supports.Length;
            return null;
        }
        internal Result Run(int[] route)
        {
            var definition=Manager.Level;Configure(definition);Step(180);
            var result=new Result{route=route};result.frames.Add(Snapshot(Manager));
            foreach(int id in route)
            {
                if(Manager.State==BreakDropGameState.Won) {result.reason="redundant move after Win";return result;}
                var block=Manager.Blocks.First(b=>b.Id==id);int moves=Manager.Moves;
                Manager.TryBreak(block);
                if(Manager.Moves!=moves+1){result.reason="move rejected: "+id;return result;}
                Resolve();result.frames.Add(Snapshot(Manager,id));
                if(Manager.State==BreakDropGameState.Failed){result.reason="Fail";return result;}
                if(Manager.State==BreakDropGameState.Resolving){result.reason="settle timeout";return result;}
            }
            result.win=Manager.State==BreakDropGameState.Won&&Manager.HasGoalSupport();
            result.reason=result.win?"Win":Manager.State.ToString();return result;
        }
        internal int[] Search(int sample,Action<string> progress)
        {
            foreach(var authored in SupportSampleRecipes.ReferenceCandidates(sample))
            {
                bool reliable=true;
                for(int run=0;run<3;run++)if(!Mechanism(sample,Run(authored))){reliable=false;break;}
                if(reliable)return authored;
            }
            var starts=sample==1?new[]{6}:sample==2?new[]{9,10}:sample==3?new[]{6,7}:sample==4?new[]{9,10,12}:new[]{12,13};
            var beam=starts.Select(id=>new[]{id}).ToList();int tested=0;
            for(int depth=1;depth<=7;depth++)
            {
                var candidates=new List<Tuple<int[],float>>();
                foreach(var route in beam)
                {
                    Result r=Run(route);tested++;
                    if(r.win&&route.Length>=3&&Mechanism(sample,r))return route;
                    if(r.reason!="Playing")continue;
                    var frame=r.frames.Last();
                    float score=frame.target.y+Mathf.Abs(frame.target.x-Manager.Level.goal.x)*.45f;
                    // Keep routes that expose the intended structure rather than clear peripheral columns.
                    score-=route.Contains(sample==1?6:sample==2?8:sample==3?9:sample==4?11:14)?.5f:0;
                    foreach(var b in Manager.Blocks.Where(b=>!b.IsBroken))
                        candidates.Add(Tuple.Create(route.Concat(new[]{b.Id}).ToArray(),score+(frame.supports.Contains(b.Id)?-.3f:0)));
                }
                progress("S"+sample+" depth "+depth+" tested "+tested+" next "+candidates.Count);
                beam=candidates.OrderBy(x=>x.Item2).ThenBy(x=>string.Join(",",x.Item1),StringComparer.Ordinal).Take(48).Select(x=>x.Item1).ToList();
                if(beam.Count==0)break;
            }
            return null;
        }
        internal static bool Meaningful(int sample,int[] r) => sample==1?r.Contains(6):sample==2?r.Contains(8)&&r.Contains(3):sample==3?r.Contains(9)&&r.Length>0&&(r[0]==6||r[0]==7):sample==4?r.Contains(6)&&r.Contains(11):r.Contains(14)&&(r.Contains(6)||r.Contains(9));
        internal static bool Mechanism(int sample, Result result)
        {
            if (!result.win || !Meaningful(sample,result.route)) return false;
            if (sample==3)
            {
                var tilt=result.frames[1];
                if(!tilt.supports.Contains(9)||Mathf.Abs(Mathf.DeltaAngle(0,tilt.rotations[8]))<8)return false;
            }
            if(sample==5)
            {
                int changes=0;
                for(int i=1;i<result.frames.Count;i++)
                    if(!result.frames[i-1].supports.SequenceEqual(result.frames[i].supports))changes++;
                if(changes<2)return false;
            }
            return true;
        }
        public void Dispose()
        {
            UnityEngine.Object.DestroyImmediate(host);
            SceneManager.SetActiveScene(previous);if(scene.IsValid())EditorSceneManager.ClosePreviewScene(scene);
            for(int i=0;i<others.Length;i++)if(others[i])others[i].simulated=simulated[i];
            for(int i=0;i<cameras.Length;i++)if(cameras[i])cameras[i].enabled=cameraEnabled[i];
            Physics2D.simulationMode=mode;Physics2D.gravity=gravity;Physics2D.velocityIterations=velocity;Physics2D.positionIterations=position;Time.timeScale=scale;
        }
    }
}
