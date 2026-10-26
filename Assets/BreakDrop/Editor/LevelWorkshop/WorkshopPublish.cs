using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace BreakDrop.Editor
{
    [Serializable] internal sealed class WorkshopRelease
    {
        public string unityVersion,baseCatalogHash;
        public WorkshopReleaseItem[] items;
    }
    [Serializable] internal sealed class WorkshopReleaseItem { public string file,hash,draftPath,validationKey; }
    internal static class WorkshopPublish
    {
        const string Production="Assets/BreakDrop/Resources/Levels";
        internal static string BaseHash()=>WorkshopCore.Fingerprint(new LevelDefinition{blocks=Array.Empty<BlockDefinition>(),hintOrder=Directory.GetFiles(Production,"*.json").OrderBy(x=>x).SelectMany(f=>System.Text.Encoding.UTF8.GetBytes(Path.GetFileName(f)+File.ReadAllText(f)).Select(x=>(int)x)).ToArray()});
        internal static void Seal(string directory,WorkshopDraft[] drafts)
        {
            var files=Directory.GetFiles(directory,"level_*.json").OrderBy(x=>x).ToArray();
            var manifest=new WorkshopRelease{unityVersion=Application.unityVersion,baseCatalogHash=BaseHash(),items=files.Select((file,i)=>new WorkshopReleaseItem{file=Path.GetFileName(file),hash=WorkshopCore.Fingerprint(JsonUtility.FromJson<LevelDefinition>(File.ReadAllText(file))),draftPath=AssetDatabase.GetAssetPath(drafts[i]),validationKey=drafts[i].validatedHash}).ToArray()};
            File.WriteAllText(directory+"/manifest.json",JsonUtility.ToJson(manifest,true));
        }
        internal static void Install(string directory)
        {
            var manifest=JsonUtility.FromJson<WorkshopRelease>(File.ReadAllText(Path.Combine(directory,"manifest.json")));
            if(manifest.baseCatalogHash!=BaseHash())throw new Exception("正式关卡已变化，请重新导出候选发布包");
            if(manifest.items==null||manifest.items.Length==0)throw new Exception("发布包为空");
            foreach(var item in manifest.items)
            {
                if(item.file!=Path.GetFileName(item.file)||!item.file.StartsWith("level_")||!item.file.EndsWith(".json"))throw new Exception("非法发布文件名");
                var level=JsonUtility.FromJson<LevelDefinition>(File.ReadAllText(Path.Combine(directory,item.file)));
                var draft=AssetDatabase.LoadAssetAtPath<WorkshopDraft>(item.draftPath);
                if(WorkshopCore.StaticError(level)!=null||WorkshopCore.Fingerprint(level)!=item.hash||draft==null||!draft.selectedForRelease||WorkshopCore.ValidationKey(draft.level)!=item.validationKey)throw new Exception("发布内容已修改或验证失效");
                if(File.Exists(Path.Combine(Production,item.file)))throw new Exception("禁止覆盖已有正式关卡");
            }
            if(!EditorUtility.DisplayDialog("追加正式关卡", "将追加 "+manifest.items.Length+" 关。现有关卡顺序和ID保持不变。是否发布？","追加发布","取消"))return;
            string backup=Path.Combine(directory,"production-backup");Directory.CreateDirectory(backup);
            foreach(var file in Directory.GetFiles(Production,"*.json"))File.Copy(file,Path.Combine(backup,Path.GetFileName(file)),true);
            var created=new List<string>();
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach(var item in manifest.items)
                {
                    string path=Path.Combine(Production,item.file);
                    using(var target=new FileStream(path,FileMode.CreateNew,FileAccess.Write))using(var source=File.OpenRead(Path.Combine(directory,item.file)))
                    {created.Add(path);source.CopyTo(target);}
                }
                File.WriteAllText(Path.Combine(directory,"published.txt"),DateTime.UtcNow.ToString("O"));
            }
            catch{foreach(var file in created)File.Delete(file);throw;}
            finally{AssetDatabase.StopAssetEditing();AssetDatabase.Refresh();}
        }
    }
}
