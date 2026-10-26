using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace BreakDrop.Editor
{
    public sealed class WorkshopWindow : EditorWindow
    {
        public static readonly string[] Presets={"L形承托","双块共托","桥梁换支点","错层承接","组合支撑","固定支点承接","跷跷板配重转移"};
        [SerializeField] private WorkshopDraft draft;
        [SerializeField] private int preset,seed=1001,batchSize=100;
        private Vector2 libraryScroll,inspectorScroll;
        private string filter="",exportPath="";
        private double nextHashCheck;
        private bool currentValidation;
        private WorkshopDraft hashDraft;
        private List<WorkshopDraft> library;
        private List<WorkshopDraft> visibleDrafts=new List<WorkshopDraft>();
        private readonly HashSet<string> checkedDrafts=new HashSet<string>();
        private double nextLibrary;
        private readonly HashSet<int> selected=new HashSet<int>();
        private WorkshopSimulation preview;
        private double lastStep;
        private bool previewPaused;
        private bool showRoute;
        [SerializeField] private int blockPreset,blockRotation;
        private List<int> playedRoute=new List<int>();
        private Vector2 dragPoint;
        private bool draggingTarget;
        [MenuItem("Tools/Break & Drop/Level Workshop")]
        public static void Open()=>GetWindow<WorkshopWindow>("关卡工作台");
        void OnEnable(){minSize=new Vector2(1050,650);EditorApplication.update+=UpdatePreview;Undo.undoRedoPerformed+=AfterUndo;AssemblyReloadEvents.beforeAssemblyReload+=StopPreview;}
        void OnDisable(){StopPreview();EditorApplication.update-=UpdatePreview;Undo.undoRedoPerformed-=AfterUndo;AssemblyReloadEvents.beforeAssemblyReload-=StopPreview;}
        void AfterUndo(){if(draft!=null)WorkshopCore.Changed(draft);Repaint();}
        void Act(Action action)
        {
            // Mutations run after the current IMGUI event, never halfway through its layout tree.
            EditorApplication.delayCall+=()=>{try{action();}catch(Exception e){EditorUtility.DisplayDialog("关卡工作台",e.Message,"确定");}Repaint();};
        }
        void Edit(Action action){if(draft==null||preview!=null||WorkshopRunner.Busy)return;Undo.RecordObject(draft,"编辑关卡");action();WorkshopCore.Changed(draft);}
        void StopPreview(){preview?.Dispose();preview=null;}
        void StartPreview()
        {
            if(draft==null)return;var error=WorkshopCore.StaticError(draft.level);if(error!=null)throw new Exception(error);
            StopPreview();preview=new WorkshopSimulation(draft.level);
            try
            {
                var initial=WorkshopValidation.InitialGeometryError(preview.Manager);
                if(initial==null)
                {
                    for(int i=0;i<180;i++)preview.Step();
                    initial=WorkshopValidation.InitialStateError(preview.Manager);
                }
                if(initial!=null)
                {
                    draft.status="结构检查失败";draft.report=initial;draft.validatedHash="";draft.selectedForRelease=false;
                    EditorUtility.SetDirty(draft);AssetDatabase.SaveAssets();throw new Exception(initial);
                }
            }
            catch{StopPreview();throw;}
            playedRoute.Clear();previewPaused=false;lastStep=EditorApplication.timeSinceStartup;
        }
        void UpdatePreview()
        {
            if(preview!=null)
            {
                if(EditorApplication.isPlayingOrWillChangePlaymode){StopPreview();return;}
                var now=EditorApplication.timeSinceStartup;
                if(!previewPaused){int count=Mathf.Min(6,(int)((now-lastStep)*60));for(int i=0;i<count;i++)preview.Step();if(count>0)lastStep=now;}
                else lastStep=now;
            }
            if(preview!=null||WorkshopRunner.Busy)Repaint();
        }
        void OnGUI()
        {
            using(new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                using(new EditorGUI.DisabledScope(WorkshopRunner.Busy||preview!=null))
                {
                    if(GUILayout.Button("导入JSON",EditorStyles.toolbarButton))Act(WorkshopCore.Import);
                    preset=EditorGUILayout.Popup(preset,Presets,GUILayout.Width(140));
                    if(GUILayout.Button("从模板新建",EditorStyles.toolbarButton))Act(()=>{draft=WorkshopCore.SaveNew(WorkshopCore.Preset(preset,0),Presets[preset],Presets[preset]);selected.Clear();});
                    if(GUILayout.Button("保存草稿",EditorStyles.toolbarButton))AssetDatabase.SaveAssets();
                }
                if(GUILayout.Button("停止任务",EditorStyles.toolbarButton,GUILayout.Width(75)))Act(WorkshopRunner.Cancel);
                GUILayout.FlexibleSpace();GUILayout.Label("草稿与正式关卡隔离 · 未验证不能发布");
            }
            using(new EditorGUILayout.HorizontalScope())
            {
                DrawLibrary();
                using(new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true)))
                {
                    DrawActions();
                    var canvas=GUILayoutUtility.GetRect(400,400,GUILayout.ExpandWidth(true),GUILayout.ExpandHeight(true));DrawCanvas(canvas);
                    if(draft!=null){GUILayout.Label(draft.title+" · "+draft.status+" · 难度建议："+draft.difficulty);EditorGUILayout.HelpBox(draft.report.Length==0?"拖动积木；Ctrl多选；右侧编辑属性。试玩时点击积木拆除。":draft.report,MessageType.Info);}
                    DrawJobs();
                }
                DrawInspector();
            }
            EditorGUILayout.HelpBox(string.IsNullOrEmpty(WorkshopRunner.Error)?WorkshopRunner.Status:WorkshopRunner.Error,string.IsNullOrEmpty(WorkshopRunner.Error)?MessageType.Info:MessageType.Error);
        }
        void DrawLibrary()
        {
            using(new EditorGUILayout.VerticalScope(GUILayout.Width(230)))
            {
                filter=EditorGUILayout.TextField("筛选",filter);
                if(Event.current.type==EventType.Layout)
                {
                    if(library==null||EditorApplication.timeSinceStartup>nextLibrary)
                    {
                        library=WorkshopCore.All();nextLibrary=EditorApplication.timeSinceStartup+1;
                        var available=new HashSet<string>(library.Select(AssetDatabase.GetAssetPath));
                        checkedDrafts.IntersectWith(available);
                    }
                    visibleDrafts=library.Where(x=>(x.title+x.theme+x.status).IndexOf(filter,StringComparison.OrdinalIgnoreCase)>=0).ToList();
                }
                using(new EditorGUI.DisabledScope(preview!=null||WorkshopRunner.Busy))
                {
                    using(new EditorGUILayout.HorizontalScope())
                    {
                        if(GUILayout.Button("全选筛选结果"))foreach(var item in visibleDrafts)checkedDrafts.Add(AssetDatabase.GetAssetPath(item));
                        if(GUILayout.Button("清空勾选"))checkedDrafts.Clear();
                    }
                    GUILayout.Label("已勾选 "+checkedDrafts.Count+" 项（含筛选隐藏项）");
                    using(new EditorGUI.DisabledScope(checkedDrafts.Count==0))
                        if(GUILayout.Button("删除勾选草稿")){var paths=checkedDrafts.ToArray();Act(()=>DeleteDrafts(paths));}
                    using(new EditorGUI.DisabledScope(draft==null))
                        if(GUILayout.Button("删除当前草稿")){var path=AssetDatabase.GetAssetPath(draft);Act(()=>DeleteDrafts(new[]{path}));}
                }
                libraryScroll=EditorGUILayout.BeginScrollView(libraryScroll);
                foreach(var item in visibleDrafts)
                {
                    using(new EditorGUILayout.HorizontalScope())
                    {
                        using(new EditorGUI.DisabledScope(preview!=null||WorkshopRunner.Busy))
                        {
                            string path=AssetDatabase.GetAssetPath(item);
                            bool check=EditorGUILayout.Toggle(checkedDrafts.Contains(path),GUILayout.Width(18),GUILayout.Height(44));
                            if(check)checkedDrafts.Add(path);else checkedDrafts.Remove(path);
                            if(GUILayout.Toggle(item==draft,"       "+(item.selectedForRelease?"★ ":"")+item.title+"\n"+item.status,"Button",GUILayout.Height(44))&&item!=draft)
                            {var chosen=item;Act(()=>{draft=chosen;selected.Clear();});}
                        }
                        var row=GUILayoutUtility.GetLastRect();var thumb=new Rect(row.x+3,row.y+4,36,36);
                        foreach(var block in item.level.blocks)
                        {
                            var rect=new Rect(thumb.center.x+block.x*2-block.width,thumb.yMax-8-block.y*2-block.height,Mathf.Max(1,block.width*2),Mathf.Max(1,block.height*2));
                            EditorGUI.DrawRect(rect,block.fixedBlock?Color.gray:Color.cyan);
                        }
                    }
                }
                EditorGUILayout.EndScrollView();
            }
        }
        void DeleteDrafts(string[] paths)
        {
            if(preview!=null||WorkshopRunner.Busy)throw new Exception("请先停止试玩或生成任务");
            var targets=paths.Where(x=>!string.IsNullOrEmpty(x)).Select(x=>x.Replace('\\','/')).Distinct().ToArray();
            if(targets.Length==0)return;
            foreach(var path in targets)
                if(!path.StartsWith(WorkshopCore.Root+"/",StringComparison.Ordinal)||!path.EndsWith(".asset",StringComparison.OrdinalIgnoreCase)||AssetDatabase.LoadAssetAtPath<WorkshopDraft>(path)==null)
                    throw new Exception("只能删除工作台草稿目录中存在的关卡："+path);
            string names=string.Join("\n",targets.Take(10).Select(x=>AssetDatabase.LoadAssetAtPath<WorkshopDraft>(x).title));
            if(targets.Length>10)names+="\n……共 "+targets.Length+" 项";
            if(!EditorUtility.DisplayDialog("删除关卡草稿","将以下 "+targets.Length+" 个草稿移入回收站？\n\n"+names+"\n\n已发布的正式关卡不受影响。","删除草稿","取消"))return;
            string activePath=draft==null?null:AssetDatabase.GetAssetPath(draft);
            var failures=new List<string>();int removed=0;
            try
            {
                foreach(var path in targets)
                {
                    try
                    {
                        if(!AssetDatabase.MoveAssetToTrash(path)){failures.Add(path);continue;}
                        removed++;checkedDrafts.Remove(path);
                        if(path==activePath){draft=null;selected.Clear();}
                    }
                    catch(Exception e){failures.Add(path+": "+e.Message);}
                }
            }
            finally
            {
                library=null;nextLibrary=0;hashDraft=null;currentValidation=false;
                WorkshopRunner.Status="已删除 "+removed+" 个草稿"+(failures.Count>0?"，失败 "+failures.Count+" 个":"");
            }
            if(failures.Count>0)throw new Exception("以下草稿删除失败，已保留勾选：\n"+string.Join("\n",failures));
        }
        void DrawActions()
        {
            if(draft==null)return;
            showRoute=EditorGUILayout.Toggle("显示参考落点轨迹",showRoute);
            using(new EditorGUI.DisabledScope(preview!=null||WorkshopRunner.Busy))
            using(new EditorGUILayout.HorizontalScope())
            {
                blockPreset=EditorGUILayout.Popup(blockPreset,WorkshopBlockCatalog.Entries.Select(x=>x.label).ToArray(),GUILayout.MinWidth(160));
                blockRotation=EditorGUILayout.Popup(blockRotation,new[]{"0°","90°","180°","270°"},GUILayout.Width(60));
                if(GUILayout.Button("添加积木",GUILayout.Width(80)))AddCatalogBlock();
                GUILayout.Label("11 款积木预设",GUILayout.Width(105));
            }
            using(new EditorGUILayout.HorizontalScope())
            {
                var entry=WorkshopBlockCatalog.Entries[blockPreset];
                var texture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/BreakDrop/Resources/Art/"+entry.art+".png");
                var thumb=GUILayoutUtility.GetRect(60,36,GUILayout.Width(60));
                if(texture!=null)GUI.DrawTexture(thumb,texture,ScaleMode.ScaleToFit,true);
                GUILayout.Label(entry.label+" · 当前关卡 "+draft.level.blocks.Length+" 块",GUILayout.Height(36));
            }
            using(new EditorGUILayout.HorizontalScope())
            {
                using(new EditorGUI.DisabledScope(WorkshopRunner.Busy))
                {
                    if(GUILayout.Button(preview==null?"试玩":"停止"))Act(()=>{if(preview==null)StartPreview();else StopPreview();});
                    if(preview!=null)
                    {
                        if(GUILayout.Button(previewPaused?"继续":"暂停"))previewPaused=!previewPaused;
                        if(GUILayout.Button("回退")){if(preview.Manager.CanUndo){preview.Manager.Undo();if(playedRoute.Count>0)playedRoute.RemoveAt(playedRoute.Count-1);}}
                        if(GUILayout.Button("重开"))Act(StartPreview);
                        if(GUILayout.Button("保存本次路线")&&preview.Manager.State==BreakDropGameState.Won)
                        {var route=playedRoute.ToArray();StopPreview();Edit(()=>{draft.level.hintOrder=route;draft.level.bestMoves=route.Length;});AssetDatabase.SaveAssets();}
                    }
                    else
                    {

                        if(GUILayout.Button("固定支点"))Add("Rectangle",true,false);
                        if(GUILayout.Button("跷跷板"))Add("Rectangle",false,true);
                        if(GUILayout.Button("复制"))Duplicate();
                        if(GUILayout.Button("删除积木"))Edit(()=>{draft.level.blocks=draft.level.blocks.Where(b=>!selected.Contains(b.id)).ToArray();draft.level.mechanisms=(draft.level.mechanisms??Array.Empty<MechanismDefinition>()).Where(m=>!selected.Contains(m.blockId)).ToArray();draft.level.hintOrder=Array.Empty<int>();selected.Clear();});
                    }
                }
            }
        }
        void AddCatalogBlock()
        {
            Edit(()=>
            {
                int id=draft.level.blocks.Select(b=>b.id).DefaultIfEmpty(0).Max()+1;
                var block=WorkshopBlockCatalog.Entries[blockPreset].Create(id,blockRotation*90);
                draft.level.blocks=draft.level.blocks.Concat(new[]{block}).ToArray();
                selected.Clear();selected.Add(id);
            });
        }
        void Add(string shape,bool fixedBlock,bool hinge)
        {
            Edit(()=>{int id=draft.level.blocks.Select(b=>b.id).DefaultIfEmpty(0).Max()+1;
                var b=new BlockDefinition{id=id,y=3,shape=shape,width=hinge?3:shape=="L3"?3:1,height=hinge?.5f:shape=="L3"?3:1,breakable=!fixedBlock&&!hinge,fixedBlock=fixedBlock};
                draft.level.blocks=draft.level.blocks.Concat(new[]{b}).ToArray();
                if(hinge)draft.level.mechanisms=(draft.level.mechanisms??Array.Empty<MechanismDefinition>()).Concat(new[]{new MechanismDefinition{blockId=id}}).ToArray();selected.Clear();selected.Add(id);});
        }
        void Duplicate()
        {
            Edit(()=>{int next=draft.level.blocks.Max(b=>b.id)+1;var copies=new List<BlockDefinition>();var joints=new List<MechanismDefinition>(draft.level.mechanisms??Array.Empty<MechanismDefinition>());
                foreach(var b in draft.level.blocks.Where(b=>selected.Contains(b.id))){var copy=WorkshopCore.Clone(b);copy.id=next++;copy.x+=.25f;copy.y+=.25f;copies.Add(copy);foreach(var m in (draft.level.mechanisms??Array.Empty<MechanismDefinition>()).Where(m=>m.blockId==b.id)){var j=WorkshopCore.Clone(m);j.blockId=copy.id;joints.Add(j);}}
                draft.level.blocks=draft.level.blocks.Concat(copies).ToArray();draft.level.mechanisms=joints.ToArray();selected.Clear();foreach(var b in copies)selected.Add(b.id);});
        }
        void DrawInspector()
        {
            using(new EditorGUILayout.VerticalScope(GUILayout.Width(285)))
            {
                if(draft==null)return;
                inspectorScroll=EditorGUILayout.BeginScrollView(inspectorScroll);
                using(new EditorGUI.DisabledScope(WorkshopRunner.Busy||preview!=null))
                {
                    var so=new SerializedObject(draft);so.Update();EditorGUI.BeginChangeCheck();
                    var level=so.FindProperty("level");
                    EditorGUILayout.LabelField("六边形初始位置",EditorStyles.boldLabel);
                    var targetProperty=level.FindPropertyRelative("target");
                    EditorGUILayout.PropertyField(targetProperty.FindPropertyRelative("x"),new GUIContent("水平位置 X"));
                    EditorGUILayout.PropertyField(targetProperty.FindPropertyRelative("y"),new GUIContent("初始高度 Y"));
                    EditorGUILayout.LabelField("三星高度线",EditorStyles.boldLabel);
                    var stars=level.FindPropertyRelative("starHeights");
                    for(int star=0;star<stars.arraySize;star++)
                        EditorGUILayout.PropertyField(stars.GetArrayElementAtIndex(star),new GUIContent((star+1)+" 星高度 Y"));
                    EditorGUILayout.HelpBox("拖动六边形调整起点，或输入精确高度。1、2、3 星线由高到低设置。试玩时请先停止再编辑。",MessageType.None);
                    foreach(var name in new[]{"title","theme","template","positionVariation","ranges","lockedBlocks","ordinal"})EditorGUILayout.PropertyField(so.FindProperty(name),true);
                    foreach(var name in new[]{"goal","goalWidth","failX","failY","timeLimitSeconds","hintOrder"})EditorGUILayout.PropertyField(level.FindPropertyRelative(name),true);
                    var blocks=level.FindPropertyRelative("blocks");
                    for(int i=0;i<blocks.arraySize;i++)if(selected.Contains(draft.level.blocks[i].id))EditorGUILayout.PropertyField(blocks.GetArrayElementAtIndex(i),new GUIContent("积木 "+draft.level.blocks[i].id),true);
                    EditorGUILayout.PropertyField(level.FindPropertyRelative("mechanisms"),true);
                    if(EditorGUI.EndChangeCheck()){so.ApplyModifiedProperties();WorkshopCore.Changed(draft);}
                    if(GUILayout.Button("所选旋转15°"))Edit(()=>{foreach(var b in draft.level.blocks.Where(b=>selected.Contains(b.id)))b.rotation+=15;});
                    if(GUILayout.Button("验证 / 搜索参考路线"))Act(()=>WorkshopRunner.Start(WorkshopValidation.Validate(draft)));
                    if(Event.current.type==EventType.Layout&&(hashDraft!=draft||EditorApplication.timeSinceStartup>=nextHashCheck)){hashDraft=draft;nextHashCheck=EditorApplication.timeSinceStartup+1;currentValidation=draft.validatedHash.Length>0&&draft.validatedHash==WorkshopCore.ValidationKey(draft.level);}
                    bool current=currentValidation&&draft.validatedHash.Length>0;
                    using(new EditorGUI.DisabledScope(!current))
                    {bool selectedValue=EditorGUILayout.Toggle("人工精选加入关卡集",draft.selectedForRelease);if(selectedValue!=draft.selectedForRelease){draft.selectedForRelease=selectedValue;EditorUtility.SetDirty(draft);}}
                    var similar=(library??WorkshopCore.All()).FirstOrDefault(x=>x!=draft&&WorkshopCore.Similarity(x.level,draft.level)>.6f);
                    if(similar!=null)EditorGUILayout.HelpBox("结构相近或镜像候选："+similar.title,MessageType.Info);
                    if(!current&&draft.validatedHash.Length>0)EditorGUILayout.HelpBox("内容或验证代码变化，需要重新验证",MessageType.Warning);
                    if(GUILayout.Button("保存参考轨迹报告")&&!string.IsNullOrEmpty(draft.replayJson))Act(()=>{var f=EditorUtility.SaveFilePanel("保存验证轨迹","","route.json","json");if(!string.IsNullOrEmpty(f))File.WriteAllText(f,draft.replayJson);});
                    if(GUILayout.Button("标为淘汰")){Edit(()=>draft.report="人工淘汰");draft.status="淘汰";}
                }
                EditorGUILayout.EndScrollView();
            }
        }
        void DrawJobs()
        {
            using(new EditorGUILayout.HorizontalScope())
            {
                using(new EditorGUI.DisabledScope(WorkshopRunner.Busy||preview!=null))
                {
                    batchSize=EditorGUILayout.IntField("候选数",batchSize,GUILayout.Width(180));seed=EditorGUILayout.IntField("种子",seed,GUILayout.Width(180));
                    if(GUILayout.Button("批量生成"))Act(()=>WorkshopBatch.Begin(preset,draft!=null&&draft.template?draft:null,seed,batchSize));
                    if(GUILayout.Button("续跑"))Act(()=>{if(WorkshopBatch.HasCheckpoint)WorkshopBatch.Resume();else throw new Exception("没有可恢复的批次");});
                    if(GUILayout.Button("发布候选包"))Act(()=>{var folder=EditorUtility.OpenFolderPanel("选择已验证的候选发布目录","LevelWorkshopReleases","");if(!string.IsNullOrEmpty(folder))WorkshopPublish.Install(folder);});
                    if(GUILayout.Button("导出精选集"))Act(()=>{exportPath=WorkshopCore.ExportSelected();EditorUtility.RevealInFinder(exportPath);});
                }
                if(GUILayout.Button("停止任务"))Act(WorkshopRunner.Cancel);
            }
        }
        Vector2 ToScreen(Rect r,Vector2 p){float scale=Mathf.Min(r.width/12,r.height/16);return new Vector2(r.center.x+p.x*scale,r.yMax-40-(p.y+6)*scale);}
        Vector2 ToWorld(Rect r,Vector2 p){float scale=Mathf.Min(r.width/12,r.height/16);return new Vector2((p.x-r.center.x)/scale,(r.yMax-40-p.y)/scale-6);}
        void DrawCanvas(Rect rect)
        {
            EditorGUI.DrawRect(rect,new Color(.13f,.15f,.18f));if(draft==null)return;
            Handles.BeginGUI();
            for(int star=0;star<(draft.level.starHeights?.Length??0);star++)
            {
                float height=draft.level.starHeights[star];
                float y=ToScreen(rect,new Vector2(0,height)).y;
                if(y<rect.yMin||y>rect.yMax)continue;
                Handles.color=new Color(1f,.78f,.25f,.8f);
                Handles.DrawDottedLine(new Vector3(rect.xMin+8,y),new Vector3(rect.xMax-8,y),5);
                GUI.Label(new Rect(rect.xMin+10,y-21,150,20),(star+1)+" 星  Y="+height.ToString("0.##"),EditorStyles.whiteLabel);
            }
            foreach(var b in draft.level.blocks)
            {
                Vector2 pos=new Vector2(b.x,b.y);float angle=b.rotation;
                var actual=preview?.Manager.Blocks.FirstOrDefault(x=>x.Id==b.id);if(actual!=null){if(actual.IsBroken)continue;pos=actual.Body.position;angle=actual.Body.rotation;}
                var points=b.shape.StartsWith("L")?new[]{new Vector2(-b.width/2,-b.height/2),new Vector2(1-b.width/2,-b.height/2),new Vector2(1-b.width/2,b.height/2-1),new Vector2(b.width/2,b.height/2-1),new Vector2(b.width/2,b.height/2),new Vector2(-b.width/2,b.height/2)}:new[]{new Vector2(-b.width/2,-b.height/2),new Vector2(b.width/2,-b.height/2),new Vector2(b.width/2,b.height/2),new Vector2(-b.width/2,b.height/2)};
                var corners=points.Select(p=>(Vector3)ToScreen(rect,pos+(Vector2)(Quaternion.Euler(0,0,angle)*p))).ToList();corners.Add(corners[0]);
                Handles.color=selected.Contains(b.id)?Color.yellow:b.fixedBlock?Color.gray:Color.cyan;Handles.DrawAAPolyLine(3,corners.ToArray());GUI.Label(new Rect(ToScreen(rect,pos)-new Vector2(14,10),new Vector2(50,22)),b.id.ToString());
                foreach(var j in (draft.level.mechanisms??Array.Empty<MechanismDefinition>()).Where(m=>m.blockId==b.id))
                {var anchor=ToScreen(rect,pos+(Vector2)(Quaternion.Euler(0,0,angle)*new Vector2(j.anchorX,j.anchorY)));Handles.color=Color.white;Handles.DrawWireDisc(anchor,Vector3.forward,6);
                    var from=Quaternion.Euler(0,0,-(b.rotation+j.lowerAngle))*Vector3.right;
                    Handles.DrawWireArc(anchor,Vector3.forward,from,-(j.upperAngle-j.lowerAngle),24);}
            }
            var target=preview==null?draft.level.target.Value:preview.Manager.Target.Body.position;
            Handles.color=draggingTarget?Color.yellow:Color.magenta;
            float targetAngle=preview==null?0:preview.Manager.Target.Body.rotation;
            var hex=Enumerable.Range(0,7).Select(i=>
                (Vector3)ToScreen(rect,target+(Vector2)(Quaternion.Euler(0,0,targetAngle+i*60)*Vector3.right)*(42f/DesignArt.PixelsPerUnit))).ToArray();
            Handles.DrawAAPolyLine(3,hex);
            GUI.Label(new Rect(ToScreen(rect,target)+new Vector2(8,-30),new Vector2(160,22)),"六边形 Y="+target.y.ToString("0.###"),EditorStyles.whiteLabel);
            Handles.color=Color.green;Handles.DrawAAPolyLine(5,ToScreen(rect,draft.level.goal.Value+new Vector2(-draft.level.goalWidth/2,.4f)),ToScreen(rect,draft.level.goal.Value+new Vector2(draft.level.goalWidth/2,.4f)));
            if(showRoute&&!string.IsNullOrEmpty(draft.replayJson))
            {
                var replay=JsonUtility.FromJson<WorkshopValidation.Result>(draft.replayJson);
                if(replay?.frames!=null&&replay.frames.Count>0)
                {
                    Handles.color=new Color(1,.6f,.2f);
                    Handles.DrawAAPolyLine(2,replay.frames.Select(f=>(Vector3)ToScreen(rect,f.target)).ToArray());
                    foreach(var frame in replay.frames)
                    {
                        var point=ToScreen(rect,frame.target);Handles.DrawWireDisc(point,Vector3.forward,4);
                        GUI.Label(new Rect(point+new Vector2(6,0),new Vector2(160,35)),"拆除 "+frame.removed+" / 承托 "+string.Join(",",frame.supports));
                    }
                }
            }
            Handles.EndGUI();
            var e=Event.current;
            if(e.type==EventType.MouseUp&&draggingTarget){draggingTarget=false;GUIUtility.hotControl=0;e.Use();Repaint();return;}
            if(!rect.Contains(e.mousePosition)&&!draggingTarget)return;
            if(e.type==EventType.MouseDown&&e.button==0)
            {
                Vector2 world=ToWorld(rect,e.mousePosition);
                if(preview==null&&!WorkshopRunner.Busy&&Vector2.Distance(world,draft.level.target.Value)<=42f/DesignArt.PixelsPerUnit)
                {
                    selected.Clear();draggingTarget=true;dragPoint=world;
                    GUIUtility.hotControl=GUIUtility.GetControlID(FocusType.Passive);
                    Undo.RecordObject(draft,"移动六边形");e.Use();Repaint();return;
                }
                var hit=draft.level.blocks.Reverse().FirstOrDefault(b=>{var actual=preview?.Manager.Blocks.FirstOrDefault(x=>x.Id==b.id);if(actual!=null&&actual.IsBroken)return false;var pos=actual==null?new Vector2(b.x,b.y):actual.Body.position;float a=actual==null?b.rotation:actual.Body.rotation;var local=(Vector2)(Quaternion.Euler(0,0,-a)*(world-pos));return Mathf.Abs(local.x)<=b.width/2&&Mathf.Abs(local.y)<=b.height/2&&(!b.shape.StartsWith("L")||local.x<=1-b.width/2||local.y>=b.height/2-1);});
                if(preview!=null){if(hit!=null&&!previewPaused){int moves=preview.Manager.Moves;preview.Manager.TryBreak(preview.Manager.Blocks.First(b=>b.Id==hit.id));if(preview.Manager.Moves>moves)playedRoute.Add(hit.id);}}
                else if(!WorkshopRunner.Busy){if(!e.control)selected.Clear();if(hit!=null){if(!selected.Add(hit.id)&&e.control)selected.Remove(hit.id);}dragPoint=world;Undo.RecordObject(draft,"移动积木");}
                e.Use();Repaint();
            }
            if(e.type==EventType.MouseDrag&&e.button==0&&preview==null&&!WorkshopRunner.Busy)
            {
                Vector2 p=ToWorld(rect,e.mousePosition);
                if(draggingTarget)
                {
                    var delta=p-dragPoint;draft.level.target.x+=delta.x;draft.level.target.y+=delta.y;
                    dragPoint=p;WorkshopCore.Changed(draft);e.Use();Repaint();return;
                }
                Vector2 d=p-dragPoint;d=new Vector2(Mathf.Round(d.x/.25f)*.25f,Mathf.Round(d.y/.25f)*.25f);
                if(d!=Vector2.zero){foreach(var b in draft.level.blocks.Where(b=>selected.Contains(b.id))){b.x+=d.x;b.y+=d.y;}dragPoint+=d;WorkshopCore.Changed(draft);}e.Use();Repaint();
            }
        }
    }
}
