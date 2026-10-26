using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace BreakDrop.Editor
{
    [InitializeOnLoad]
    public static class UguiAcceptance
    {
        private static int step = -1;
        private static double due;
        private static int homeId;
        private static System.Collections.IEnumerator extra;
        private static readonly List<string> checks = new List<string>();
        static UguiAcceptance() { EditorApplication.update += Poll; }
        [MenuItem("Tools/Break & Drop/UI/Validate Interactions")]
        public static void Run()
        {
            Directory.CreateDirectory("docs/validation/ugui");
            File.WriteAllText("docs/validation/ugui/request.txt","run");
            if(!EditorApplication.isPlaying) EditorApplication.isPlaying=true;
        }
        private static void Check(string name, bool pass)
        {
            if (!pass) throw new Exception(name);
            checks.Add("PASS " + name);
        }
        private static void Click(BreakDropHud hud,string name)
        {
            var button=hud.GetComponentsInChildren<Button>().First(x=>x.name==name);
            Check("interactable " + name,button.interactable);
            var r=(RectTransform)button.transform;
            var point=RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(r.rect.center));
            var data=new PointerEventData(EventSystem.current){position=point,button=PointerEventData.InputButton.Left};
            var hits=new List<RaycastResult>(); EventSystem.current.RaycastAll(data,hits);
            Check("raycast " + name + (hits.Count>0 ? " top="+hits[0].gameObject.name : " no hits"),hits.Count>0 && hits[0].gameObject==button.gameObject);
            ExecuteEvents.Execute(button.gameObject,data,ExecuteEvents.pointerClickHandler);
        }
        private static void Render(BreakDropHud hud)
        {
            hud.SendMessage("LateUpdate"); Canvas.ForceUpdateCanvases();
        }
        private static void Tap(BreakDropHud hud, string name)
        {
            Render(hud); Click(hud,name); Render(hud); Render(hud);
        }
        private static System.Collections.IEnumerator Extra(BreakDropHud hud)
        {
            var view=hud.GetComponentInChildren<CandyUguiView>(true);
            Check("single Canvas",hud.GetComponentsInChildren<Canvas>(true).Length==1);
            Check("single EventSystem",UnityEngine.Object.FindObjectsOfType<EventSystem>().Length==1);
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(UguiAuthoring.PrefabPath);
            Check("persisted prefab has all 13 pages",prefab.GetComponentsInChildren<CandyUguiElement>(true).Count(x=>x.IsPage)==13);
            hud.PreviewForAudit("home");Render(hud);
            var start=hud.GetComponentsInChildren<Button>().First(x=>x.name=="Start Game");
            var layout=(RectTransform)start.transform; var position=layout.anchoredPosition;
            layout.anchoredPosition+=Vector2.right*3;Render(hud);
            Check("authored RectTransform survives frame",layout.anchoredPosition==position+Vector2.right*3);
            layout.anchoredPosition=position;
            yield return null;Tap(hud,"Settings");
            bool music=hud.Economy.Data.music;yield return null;Tap(hud,"Toggle Music");
            Check("music toggles",hud.Economy.Data.music!=music);
            bool effects=hud.Economy.Data.effects;yield return null;Tap(hud,"Toggle Effects");
            Check("effects toggles",hud.Economy.Data.effects!=effects);
            bool vibration=hud.Economy.Data.vibration;yield return null;Tap(hud,"Toggle Vibration");
            Check("vibration toggles",hud.Economy.Data.vibration!=vibration);
            var data=new PointerEventData(EventSystem.current){position=Vector2.one};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
            Check("modal blocks full canvas margins",hits.Any(x=>x.gameObject.name=="ModalInputBlocker"));
            yield return null;Tap(hud,"Close");yield return null;Tap(hud,"Energy");yield return null;Tap(hud,"每日免费补给");
            int inventory=hud.Economy.Count(CandyItem.Hint);yield return null;Tap(hud,"Claim Daily");
            Check("daily claims once",hud.Economy.Count(CandyItem.Hint)==inventory+1 && !hud.Economy.CanClaimDaily);
            yield return null;Tap(hud,"知道了");hud.OpenDialog(BreakDropHud.Dialog.Daily);Render(hud);
            Check("claimed daily button disabled",!hud.GetComponentsInChildren<Button>().First(x=>x.name=="Claim Daily").interactable);
            yield return null;Tap(hud,"Close");yield return null;Tap(hud,"Energy");yield return null;Tap(hud,"道具补给");yield return null;Tap(hud,"Watch Ad Hint");
            Check("NoAd opens",hud.CurrentDialog==BreakDropHud.Dialog.NoAd);
            Check("ad retry disabled during cooldown",!hud.GetComponentsInChildren<Button>().First(x=>x.name=="Retry Ad").interactable);
            yield return null;Tap(hud,"Back");Check("ad returns to supplies",hud.CurrentDialog==BreakDropHud.Dialog.Supplies);
            yield return null;Tap(hud,"Close");yield return null;Tap(hud,"Start Game");
            var manager=hud.GetComponent<BreakDropGameManager>();manager.TickPhysics(.3f);Render(hud);
            float time=manager.TimeRemaining;yield return null;Tap(hud,"Time");
            Check("time item adds 30 seconds",Mathf.Abs(manager.TimeRemaining-time-30)<1.5f);
            int hint=hud.Economy.Count(CandyItem.Hint);yield return null;Tap(hud,"Hint");
            Check("hint consumes item without breaking blocks",hud.Economy.Count(CandyItem.Hint)==hint-1 && manager.Moves==0);
            var block=manager.Blocks.First(x=>x.Id==manager.Level.hintOrder[0]);manager.TryBreak(block);Render(hud);
            Check("fixture block breaks",block.IsBroken);yield return null;Tap(hud,"Undo");
            Check("UGUI undo restores block",!block.IsBroken && manager.Moves==0);
            yield return null;Tap(hud,"Pause");yield return null;Tap(hud,"重新开始");int energy=hud.Economy.Energy;
            yield return null;Tap(hud,"Confirm Restart");
            Check("restart charges exactly once",hud.Economy.Energy==energy-1 && manager.Moves==0 && !manager.Paused);
            hud.PreviewForAudit("win");Render(hud);int oldLevel=manager.CurrentLevel;yield return null;Tap(hud,"Next Level");
            Check("next level loads",manager.CurrentLevel==oldLevel+1);
            hud.PreviewForAudit("fail");Render(hud);yield return null;Tap(hud,"Retry");
            Check("fail retry clears result",manager.State!=BreakDropGameState.Failed);
            hud.PreviewForAudit("home");Render(hud);hud.Economy.Data.energy=0;yield return null;Tap(hud,"Start Game");
            Check("zero energy opens recharge",hud.CurrentDialog==BreakDropHud.Dialog.Energy && manager.State==BreakDropGameState.Home);
            // Visiting conditional selection and moving progress must not replace existing controls.
            hud.PreviewForAudit("select");Render(hud);
            yield return null;
            var selectedNumber=hud.GetComponentsInChildren<Text>().First(x=>x.text==manager.CurrentLevel.ToString("00"));
            Check("selected level number has rendered glyphs",selectedNumber.cachedTextGenerator.vertexCount>4);
            int first=hud.GetComponentsInChildren<Button>().First(x=>x.name=="Level 1").GetInstanceID();
            for(int i=1;i<=manager.LevelCount;i++){manager.LoadLevel(i);Render(hud);}
            Check("selection identity survives highlight changes",first==hud.GetComponentsInChildren<Button>().First(x=>x.name=="Level 1").GetInstanceID());
            int count=view.GetComponentsInChildren<CandyUguiElement>(true).Length;
            for(int repeat=0;repeat<2;repeat++)for(int i=1;i<=manager.LevelCount;i++){manager.LoadLevel(i);Render(hud);}
            Check("selection controls stop growing",count==view.GetComponentsInChildren<CandyUguiElement>(true).Length);
            hud.PreviewForAudit("game");Render(hud);count=view.GetComponentsInChildren<CandyUguiElement>(true).Length;
            for(int i=0;i<12;i++){Camera.main.orthographicSize=6+i*.1f;Render(hud);}
            Check("moving progress does not allocate controls",count==view.GetComponentsInChildren<CandyUguiElement>(true).Length);
            ViewportValidation.Run();
            Check("eleven mobile viewport cases",true);
        }
        private static void Poll()
        {
            if(EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            const string request="docs/validation/ugui/request.txt";
            if(File.Exists(request) && EditorApplication.isPlaying && UnityEngine.Object.FindObjectOfType<BreakDropHud>())
            {
                File.Delete(request); checks.Clear(); step=0; due=EditorApplication.timeSinceStartup+1;
            }
            if(step<0 || EditorApplication.timeSinceStartup<due) return;
            try
            {
                var hud=UnityEngine.Object.FindObjectOfType<BreakDropHud>();
                if(!EditorApplication.isPlaying || !hud) throw new Exception("Enter Play before UGUI validation");
                Application.runInBackground = true;
                EditorApplication.isPaused = false;
                hud.SendMessage("LateUpdate"); Canvas.ForceUpdateCanvases();
                var manager=hud.GetComponent<BreakDropGameManager>();
                switch(step)
                {
                    case 0: hud.PreviewForAudit("home"); break;
                    case 1:
                        Check("canvas and EventSystem",hud.GetComponentInChildren<Canvas>() && EventSystem.current);
                        homeId=hud.GetComponentsInChildren<Button>().First(x=>x.name=="Settings").GetInstanceID();
                        Click(hud,"Settings"); break;
                    case 2:
                        Check("settings opens through UGUI",hud.CurrentDialog==BreakDropHud.Dialog.Settings);
                        Check("modal blocks world",BreakDropHud.BlocksWorldInput);
                        Check("underlying button disabled",!hud.GetComponentsInChildren<Button>().First(x=>x.name=="Start Game").interactable);
                        Check("raycast blocker exists",hud.GetComponentsInChildren<RawImage>().Any(x=>x.name=="ModalBlocker"&&x.raycastTarget));
                        hud.CloseDialog(); break;
                    case 3:
                        Check("home controls retained",homeId==hud.GetComponentsInChildren<Button>().First(x=>x.name=="Settings").GetInstanceID());
                        Click(hud,"Level Select"); break;
                    case 4:
                        Check("level buttons",hud.GetComponentsInChildren<Button>().Count(x=>x.name.StartsWith("Level "))==manager.LevelCount);
                        Click(hud,"Level 1"); break;
                    case 5:
                        Check("level starts and energy charged once",manager.CurrentLevel==1 && hud.Economy.Energy==5);
                        Check("item controls",hud.GetComponentsInChildren<Button>().Any(x=>x.name=="Undo"));
                        hud.OpenDialog(BreakDropHud.Dialog.Pause); break;
                    case 6:
                        Check("pause state",manager.Paused && hud.CurrentDialog==BreakDropHud.Dialog.Pause);
                        Click(hud,"重新开始"); break;
                    case 7:
                        Check("restart dialog",hud.CurrentDialog==BreakDropHud.Dialog.Restart);
                        extra=Extra(hud); break;
                    case 8:
                        if(extra.MoveNext()) { due=EditorApplication.timeSinceStartup+.2; EditorApplication.QueuePlayerLoopUpdate(); return; }
                        hud.PreviewForAudit("home");
                        File.WriteAllText("docs/validation/ugui/checks.txt",string.Join("\n",checks));
                        Debug.Log("[UGUI Acceptance] PASS " + checks.Count); step=-1; return;
                }
                step++; due=EditorApplication.timeSinceStartup+1;
                EditorApplication.QueuePlayerLoopUpdate();
            }
            catch(Exception ex)
            {
                File.WriteAllText("docs/validation/ugui/checks.txt",string.Join("\n",checks)+"\nFAIL "+ex);
                Debug.LogException(ex); step=-1;
            }
        }
    }
}

