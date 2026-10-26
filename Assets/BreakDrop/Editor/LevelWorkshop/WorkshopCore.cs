using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;
namespace BreakDrop.Editor
{
    public static class WorkshopCore
    {
        public const string Root = "Assets/BreakDrop/Editor/LevelWorkshop/Drafts";
        public static T Clone<T>(T value) => JsonUtility.FromJson<T>(JsonUtility.ToJson(value));
        public static string Fingerprint(LevelDefinition level)
        {
            using(var sha=SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(JsonUtility.ToJson(level)))).Replace("-", "");
        }
        public static float Similarity(LevelDefinition a,LevelDefinition b)
        {
            Func<LevelDefinition,bool,HashSet<string>> cells=(l,mirror)=>new HashSet<string>(l.blocks.Select(block=>
                (Mathf.Round((mirror?-block.x:block.x)*4)/4)+":"+(Mathf.Round(block.y*4)/4)+":"+block.width+":"+block.height+":"+block.shape));
            var left=cells(a,false);var normal=cells(b,false);var reflected=cells(b,true);
            float denominator=Mathf.Max(1,Mathf.Max(left.Count,normal.Count));
            return Mathf.Max(left.Intersect(normal).Count(),left.Intersect(reflected).Count())/denominator;
        }
        public static string ValidationKey(LevelDefinition level)
        {
            using(var sha=SHA256.Create())
            {
                var source = new StringBuilder(Fingerprint(level)).Append(Application.unityVersion).Append(Time.fixedDeltaTime);
                foreach(var file in Directory.GetFiles("Assets/BreakDrop/Scripts/Runtime", "*.cs").Concat(Directory.GetFiles("Assets/BreakDrop/Editor/LevelWorkshop", "*.cs")).OrderBy(x=>x))
                    source.Append(File.ReadAllText(file));
                foreach(var file in new[]{"Assets/BreakDrop/Editor/SupportSampleRecipes.cs","ProjectSettings/Physics2DSettings.asset"})
                    if(File.Exists(file))source.Append(File.ReadAllText(file));
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(source.ToString()))).Replace("-", "");
            }
        }
        public static List<WorkshopDraft> All() => AssetDatabase.FindAssets("t:WorkshopDraft", new[]{"Assets/BreakDrop/Editor/LevelWorkshop"})
            .Select(id=>AssetDatabase.LoadAssetAtPath<WorkshopDraft>(AssetDatabase.GUIDToAssetPath(id))).OrderBy(x=>x.ordinal).ThenBy(x=>x.title).ToList();
        public static WorkshopDraft SaveNew(LevelDefinition level, string title, string theme="自定义", int seed=0)
        {
            Directory.CreateDirectory(Root);AssetDatabase.Refresh();
            var d=ScriptableObject.CreateInstance<WorkshopDraft>();d.level=Clone(level);d.title=title;d.theme=theme;d.seed=seed;d.ordinal=All().Count;
            AssetDatabase.CreateAsset(d, AssetDatabase.GenerateUniqueAssetPath(Root+"/Draft.asset"));return d;
        }
        public static void Changed(WorkshopDraft d)
        { d.status="待验证";d.validatedHash="";d.selectedForRelease=false;EditorUtility.SetDirty(d); }
        public static string StaticError(LevelDefinition l)
        {
            if(l==null || l.blocks==null || l.blocks.Length==0 || l.target==null || l.goal==null)return "缺少目标、终点或积木";
            if(l.blocks.Select(b=>b.id).Distinct().Count()!=l.blocks.Length)return "积木ID重复";
            if(l.blocks.Any(b=>b.id<=0 || !Finite(b.x) || !Finite(b.y) || !Finite(b.rotation) || !Finite(b.width) || !Finite(b.height) || !Finite(b.mass) || b.width<=.02f || b.height<=.02f || b.mass<=0 || !(new[]{"Rectangle","L2","L3"}.Contains(b.shape))))return "积木尺寸、形状或质量无效";
            if(!Finite(l.target.x)||!Finite(l.target.y)||!Finite(l.goal.x)||!Finite(l.goal.y)||!Finite(l.goalWidth)||!Finite(l.timeLimitSeconds)||!Finite(l.failX)||!Finite(l.failY)||l.goalWidth<=0 ||l.failX<=0|| l.timeLimitSeconds<=0)return "边界、目标或时限无效";
            if(l.starHeights==null||l.starHeights.Length!=3||l.starHeights.Any(x=>!Finite(x)))return "需要三个有效星级高度";
            try{MechanismRegistry.Validate(l);}catch(Exception e){return e.Message;}
            if((l.hintOrder??Array.Empty<int>()).Distinct().Count()!=(l.hintOrder??Array.Empty<int>()).Length)return "路线包含重复步骤";
            if((l.hintOrder??Array.Empty<int>()).Any(id=>!l.blocks.Any(b=>b.id==id&&b.breakable&&!b.fixedBlock)))return "路线引用不存在或不可拆积木";
            return null;
        }
        private static bool Finite(float v) => !float.IsNaN(v)&&!float.IsInfinity(v);
        public static void Import()
        {
            var file=EditorUtility.OpenFilePanel("导入关卡JSON", "Assets/BreakDrop/Resources/Levels", "json");
            if(string.IsNullOrEmpty(file))return;
            var l=JsonUtility.FromJson<LevelDefinition>(File.ReadAllText(file));
            string error=StaticError(l);if(error!=null)throw new Exception(error);
            Selection.activeObject=SaveNew(l,Path.GetFileNameWithoutExtension(file));
        }
        public static LevelDefinition Preset(int index, int attempt)
        {
            if(index<5)
            {
                var l=SupportSampleRecipes.Create(index+1,attempt);
                var saved="Assets/BreakDrop/Editor/LevelSamples/sample_"+(index+1).ToString("00")+".json";
                l.hintOrder=File.Exists(saved)?JsonUtility.FromJson<LevelDefinition>(File.ReadAllText(saved)).hintOrder:SupportSampleRecipes.ReferenceCandidates(index+1)[0];l.bestMoves=l.hintOrder.Length;return l;
            }
            var result=new LevelDefinition{towerLevel=true,goal=new VectorData{x=0,y=-3.6f},goalWidth=5.8f,failX=5,failY=-6,
                target=new VectorData{x=0,y=1.459482f},timeLimitSeconds=90,starHeights=new[]{1f,-.5f,-2.2f}};
            if(index==5)
            {
                result.blocks=new[]{
                    new BlockDefinition{id=1,x=0,y=-.5f,width=4,height=.5f,art="BlockA"},
                    new BlockDefinition{id=2,x=0,y=.25f,width=1,height=1,art="BlockB"},
                    new BlockDefinition{id=3,x=-1.8f,y=-1.25f,width=1,height=1,art="BlockG",breakable=false,fixedBlock=true},
                    new BlockDefinition{id=4,x=1.8f,y=-1.25f,width=1,height=1,art="BlockG",breakable=false,fixedBlock=true}};
                result.hintOrder=new[]{2,1};
            }
            else
            {
                result.goalWidth=7.2f;
                result.target=new VectorData{x=-.8f,y=.959482f};
                result.blocks=new[]{
                    new BlockDefinition{id=1,x=0,y=0,width=4,height=.5f,art="BlockB",breakable=false,mass=2},
                    new BlockDefinition{id=2,x=.8f,y=.75f,width=1,height=1,art="BlockG",mass=1.2f},
                    new BlockDefinition{id=3,x=-3.7f,y=0,width=.5f,height=6,art="BlockG",breakable=false,fixedBlock=true},
                    new BlockDefinition{id=4,x=3.7f,y=0,width=.5f,height=6,art="BlockG",breakable=false,fixedBlock=true}};
                result.mechanisms=new[]{new MechanismDefinition{blockId=1,lowerAngle=-50,upperAngle=50}};
                result.hintOrder=new[]{2};
            }
            result.bestMoves=result.hintOrder.Length;
            return result;
        }
        public static string ExportSelected()
        {
            var selected=All().Where(d=>d.selectedForRelease).ToArray();
            if(selected.Length==0)throw new Exception("先选择已验证关卡加入关卡集");
            foreach(var d in selected)if(StaticError(d.level)!=null || d.validatedHash!=ValidationKey(d.level))throw new Exception(d.title+" 已修改或未通过当前版本验证");
            string production="Assets/BreakDrop/Resources/Levels";
            var existing=Directory.GetFiles(production,"*.json").Select(f=>JsonUtility.FromJson<LevelDefinition>(File.ReadAllText(f))).ToArray();
            int next=existing.Max(l=>l.levelId)+1;
            string path="LevelWorkshopReleases/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff");Directory.CreateDirectory(path);
            var rows=new List<string>{"仅追加发布；正式关卡未修改。Unity "+Application.unityVersion};
            for(int i=1;i<selected.Length;i++)if(Similarity(selected[i-1].level,selected[i].level)>.6f)
                rows.Add("相邻结构相似度超过60%，建议人工复核："+selected[i-1].title+" / "+selected[i].title);
            foreach(var d in selected)
            {
                var l=Clone(d.level);l.levelId=next++;File.WriteAllText(path+"/level_"+l.levelId.ToString("000")+".json",JsonUtility.ToJson(l,true));
                rows.Add(d.title+" | "+l.levelId+" | "+Fingerprint(l)+" | "+d.report);
            }
            File.WriteAllLines(path+"/report.txt",rows);WorkshopPublish.Seal(path,selected);return Path.GetFullPath(path);
        }
    }
}
