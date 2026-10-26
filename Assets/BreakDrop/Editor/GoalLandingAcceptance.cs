using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace BreakDrop.Editor
{
    [InitializeOnLoad]
    public static class GoalLandingAcceptance
    {
        static GoalLandingAcceptance() { EditorApplication.update += Poll; }
        private static void Poll()
        {
            const string request="Temp/breakdrop-goal-landing.request";
            if(EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(request))return;
            string name=File.ReadAllText(request).Trim();File.Delete(request);
            Run(name);
        }
        [Serializable] private sealed class Case
        {
            public string name,state; public bool expected,actual,supported,passed;
            public Vector2 target; public float targetMinX,targetMaxX,goalMinX,goalMaxX;
        }
        [Serializable] private sealed class Report { public string unityVersion=Application.unityVersion;public List<Case> cases=new List<Case>(); }
        private static void Run(string suffix)
        {
            var report=new Report();
            using(var sim=new SupportSampleSimulation())
            {
                foreach(string name in new[]{"center","left-stable-overhang","right-stable-overhang","outside-left","below-platform","air-above-platform"})
                {
                    var level=SupportSampleRecipes.Create(1,0);sim.Configure(level);var m=sim.Manager;
                    foreach(var b in m.Blocks)b.SetBroken(true);
                    float left=level.goal.x-level.goalWidth/2,right=level.goal.x+level.goalWidth/2,top=level.goal.y+.4f;
                    float x=name=="left-stable-overhang"?left+.55f:name=="right-stable-overhang"?right-.55f:name=="outside-left"?left-.3f:level.goal.x;
                    float y=name=="below-platform"?level.goal.y-1.2f:top+1.3f;
                    m.Target.Body.position=new Vector2(x,y);m.Target.Body.rotation=0;
                    m.Target.Body.velocity=Vector2.zero;m.Target.Body.angularVelocity=0;m.Target.Body.WakeUp();Physics2D.SyncTransforms();
                    sim.Step(name=="air-above-platform"?1:240);
                    bool expected=name=="center"||name.EndsWith("stable-overhang");
                    bool actual=m.State==BreakDropGameState.Won;
                    report.cases.Add(new Case{name=name,state=m.State.ToString(),expected=expected,actual=actual,passed=expected==actual,
                        supported=m.HasGoalSupport(),target=m.Target.Body.position,targetMinX=m.Target.Collider.bounds.min.x,targetMaxX=m.Target.Collider.bounds.max.x,goalMinX=left,goalMaxX=right});
                }
            }
            Directory.CreateDirectory("docs/validation/goal-landing");
            File.WriteAllText("docs/validation/goal-landing/"+suffix+".json",JsonUtility.ToJson(report,true));
            Debug.Log("[Goal Landing] Recorded "+report.cases.Count+" contact fixtures: "+suffix);
        }
    }
}