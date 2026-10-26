using UnityEngine;
using UnityEngine.EventSystems;
namespace BreakDrop
{
    [RequireComponent(typeof(BreakDropGameManager))]
    public sealed class BreakDropHud : MonoBehaviour
    {
        private static BreakDropHud _active;
        private CandyUguiView _ui;
        private BreakDropGameManager _manager;
        private CandyFeedback _feedback;
        private Font _font, _bold;
        public CandyEconomy Economy { get; private set; }
        public enum Dialog { None, Settings, Pause, Energy, Supplies, Daily, Restart, NoAd, Message }
        public Dialog CurrentDialog { get; private set; }
        private Dialog _returnDialog;
        private bool _select, _wasPaused;
        private string _message = "";
        private float _retryAt;
        private Texture2D _panel;
        public static bool BlocksWorldInput
        {
            get
            {
                if (_active == null) return false;
                if (_active._select || _active.CurrentDialog != Dialog.None || _active._manager.Paused ||
                    _active._manager.State == BreakDropGameState.Won || _active._manager.State == BreakDropGameState.Failed) return true;
                if (EventSystem.current != null)
                {
                    if (EventSystem.current.IsPointerOverGameObject()) return true;
                    for (int i = 0; i < Input.touchCount; i++)
                        if (EventSystem.current.IsPointerOverGameObject(Input.GetTouch(i).fingerId)) return true;
                }
                Rect v = DesignArt.Viewport; Vector2 p = Input.mousePosition;
                if (!v.Contains(p)) return true;
                float y = (v.yMax - p.y) * 844 / v.height;
                return y < 156 || y > 665;
            }
        }
        private void Awake()
        {
            _active = this; _manager = GetComponent<BreakDropGameManager>();
            _font = Resources.Load<Font>("Fonts/NotoSansSC-Regular");
            _bold = Resources.Load<Font>("Fonts/NotoSansSC-Bold");
            Economy = CandyEconomy.Load();
            _feedback = gameObject.AddComponent<CandyFeedback>(); _feedback.Configure(Economy);
            _panel = CandyArt.Texture("Panel");
            CandyArt.Background(transform);
            _ui = GetComponentInChildren<CandyUguiView>(true);
            if (!_ui)
            {
                var prefab = Resources.Load<CandyUguiView>(CandyUguiView.ResourcePath);
                _ui = prefab ? Instantiate(prefab, transform) : new GameObject("Canvas", typeof(RectTransform)).AddComponent<CandyUguiView>();
                _ui.transform.SetParent(transform, false);
                _ui.name = "Canvas";
            }
            _ui.Initialize(_font, _bold);
        }
        private void Update()
        {
            Economy.Refresh();
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (CurrentDialog != Dialog.None) CloseDialog();
                else if (_select) _select = false;
                else if (_manager.State != BreakDropGameState.Home) OpenDialog(Dialog.Pause);
                else OpenDialog(Dialog.Settings);
            }
        }
        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                Economy.Save();
                if (_manager.State != BreakDropGameState.Home && CurrentDialog == Dialog.None) OpenDialog(Dialog.Pause);
            }
            else Economy.Refresh();
        }
        private void OnDestroy()
        {
            Economy?.Save(); if (_active == this) _active = null;
        }
        public void OpenDialog(Dialog dialog)
        {
            if (CurrentDialog == Dialog.None) _wasPaused = _manager.Paused;
            CurrentDialog = dialog;
            if (_manager.State != BreakDropGameState.Home) _manager.SetPaused(true);
        }
        public void CloseDialog()
        {
            CurrentDialog = Dialog.None;
            _manager.SetPaused(_wasPaused);
        }
        public bool StartLevel(int number)
        {
            if (Economy.Energy < 1) { OpenDialog(Dialog.Energy); return false; }
            // Load first: an invalid level or failed construction must not charge the player.
            _manager.LoadLevel(number);
            Economy.SpendEnergy(); Economy.BeginRun();
            _select = false; CurrentDialog = Dialog.None; _wasPaused = false;
            return true;
        }
        private void StartSaved()
        {
#if UNITY_EDITOR
            if (_manager.IsEditorSample) { StartLevel(_manager.CurrentLevel); return; }
#endif
            StartLevel(Mathf.Clamp(PlayerPrefs.GetInt("breakdrop.tower.level", 1), 1, _manager.LevelCount));
        }
        public bool UseItem(CandyItem item)
        {
            if (CurrentDialog != Dialog.None) return false;
            bool allowed = item == CandyItem.Undo ? _manager.CanUndo :
                item == CandyItem.Hint ? _manager.CanHint :
                _manager.TimeRemaining > 0 && (_manager.State == BreakDropGameState.Playing || _manager.State == BreakDropGameState.Resolving);
            if (!allowed) { ShowMessage("当前状态无法使用" + CandyEconomy.Names[(int)item]); return false; }
            if (Economy.Remaining(item) <= 0) { ShowMessage("本局" + CandyEconomy.Names[(int)item] + "使用次数已达上限"); return false; }
            if (Economy.Count(item) <= 0) { OpenDialog(Dialog.Supplies); return false; }
            if (!Economy.Consume(item)) return false;
            if (item == CandyItem.Undo) _manager.Undo();
            else if (item == CandyItem.Hint) _manager.Hint();
            else _manager.AddTime(30);
            return true;
        }
        private void ShowMessage(string text) { _message = text; OpenDialog(Dialog.Message); }
        private void RequestAd()
        {
            _returnDialog = CurrentDialog; _retryAt = Time.realtimeSinceStartup + 15;
            OpenDialog(Dialog.NoAd);
        }
        private void LateUpdate()
        {
            _ui.Begin();
            bool modal = CurrentDialog != Dialog.None;
            bool result = _manager.State == BreakDropGameState.Won || _manager.State == BreakDropGameState.Failed;
            _ui.Interactable = !modal && !result;
            _ui.PageGroup(_select ? "Pages/LevelSelect" : _manager.State == BreakDropGameState.Home ? "Pages/Home" : "Pages/Game");
            if (_select) DrawSelect(); else if (_manager.State == BreakDropGameState.Home) DrawHome(); else DrawGame();
            _ui.Interactable = !modal;
            if (result && !_select) { _ui.PageGroup("Results/" + _manager.State); DrawResult(); }
            _ui.Interactable = true;
            if (CurrentDialog != Dialog.None) { _ui.PageGroup("Dialogs/" + CurrentDialog); DrawDialog(); }
            _ui.End();
        }
        private static Rect R(float x, float y, float w, float h, float sw = 904, float sh = 1740) => CandySkin.R(x,y,w,h,sw,sh);
        private void DrawHome()
        {
            _ui.HomeBackground();
            _ui.HomeArt("Title", R(88, 113, 713, 451));
            _ui.HomeArt("Hero", R(113, 551, 687, 692));
            if (_ui.HomeButton("Energy", R(20, 18, 321, 111))) OpenDialog(Dialog.Energy);
            _ui.Label(R(127, 43, 118, 51), Economy.Energy + "/10", 17, true,
                new Color(.30f,.15f,.08f), id:"Energy Count");
            if (_ui.HomeButton("Settings", R(766, 19, 120, 115))) OpenDialog(Dialog.Settings);
            if (_ui.HomeButton("Start Game", new Rect(55,606,280,106), false)) StartSaved();
            if (_ui.HomeButton("Level Select", new Rect(55,722,280,106), false)) _select = true;
        }
        private void DrawSelect()
        {
#if UNITY_EDITOR
            if (_manager.IsEditorSample)
            {
                _ui.Full("GameBackdrop");
                Text(new Rect(30,130,330,48),"支撑关系样板",28,true);
                string[] names={"L形承托","双块共托","桥梁换支点","错层承接","组合支撑"};
                for(int i=0;i<5;i++)
                    if(Button(new Rect(55,220+i*85,280,64),(i+1)+" · "+names[i]))StartLevel(i+1);
                if(Button(new Rect(95,690,200,55),"返回首页"))_select=false;
                return;
            }
#endif
            _ui.Full("SelectLive");
            LiveText(R(155,52,100,45,853,1844),Economy.Energy+"/10",21);
            if(Hit(R(24,24,340,90,853,1844),"Energy")) OpenDialog(Dialog.Energy);
            if(Hit(R(730,28,96,105,853,1844),"Back Home")) _select=false;
            for(int i=0;i<_manager.LevelCount;i++)
            {
                Rect r=R(71+i%4*180,631+i/4*180,167,165,853,1844);
                if(i+1==_manager.CurrentLevel)
                {
                    _ui.Region("StateIcons",new Rect(202,63,775,750),r);
                    Text(new Rect(r.x+8,r.y+17,r.width-16,40),(i+1).ToString("00"),30,true,CandyArt.Cream);
                }
                if(Hit(r,"Level " + (i+1))) StartLevel(i+1);
            }
        }
        private void DrawGame()
        {
            // GameLive is behind the physics world, so HUD and landscape share one seamless canvas.
            LiveText(R(341,55,229,72,903,1742),"第"+_manager.CurrentLevel.ToString("00")+"关",28);
            LiveText(R(103,189,99,45,903,1742),Economy.Energy+"/10",17);
            int sec=_manager.TimeRemainingSeconds;
            LiveText(R(409,181,164,57,903,1742),(sec/60).ToString("00")+":"+(sec%60).ToString("00"),29);
            LiveText(R(371,274,178,34,903,1742),"步数 "+_manager.Moves.ToString("00"),16);
            if(Hit(R(20,28,115,118,903,1742),"Back Home")) {_manager.ShowHome();return;}
            if(Hit(R(771,28,115,118,903,1742),"Pause")) OpenDialog(Dialog.Pause);
            if(Hit(R(20,170,250,80,903,1742),"Energy")) OpenDialog(Dialog.Energy);
            if(_manager.Level!=null&&Camera.main!=null)
                for(int i=0;i<_manager.Level.starHeights.Length;i++)
                {
                    Vector3 p=Camera.main.WorldToScreenPoint(new Vector3(0,_manager.Level.starHeights[i],0));
                    Rect v=DesignArt.Viewport;
                    float y=(v.yMax-p.y)*844/v.height;
                    _ui.Star(new Rect(10,y-16,30,32),true,"Progress Star " + i,true);
                    for(int j=0;j<6;j++) _ui.Fill(new Rect(46+j*7,y,4,1),CandyArt.Chocolate,id:"Progress Line " + i + "-" + j,dynamicPosition:true);
                }
            DrawItem(R(105,1470,188,169,903,1742),CandyItem.Undo);
            DrawItem(R(363,1470,179,169,903,1742),CandyItem.Hint);
            DrawItem(R(610,1470,188,169,903,1742),CandyItem.Time);
        }
        private void DrawItem(Rect r,CandyItem item)
        {
            Rect label=new Rect(r.x-12,r.yMax+3,r.width+24,23);
            Text(label,CandyEconomy.Names[(int)item]+" ×"+Economy.Count(item),14,true);
            Text(new Rect(label.x,label.yMax,label.width,16),"本局剩余 "+Economy.Remaining(item)+" 次",10);
            // Empty stock remains tappable so it can lead to supplies; a used-up run cap is disabled.
            bool enabled=_ui.Interactable;
            _ui.Interactable=enabled&&Economy.Remaining(item)>0;
            if(Hit(r,item.ToString())) UseItem(item);
            _ui.Interactable=enabled;
        }
        private void DrawResult()
        {
            Shade();
            bool won=_manager.State==BreakDropGameState.Won;
            _ui.Full(won?"WinPanel":"FailPanel");
            if(won)
            {
                Text(R(308,1009,265,49,879,1789),"步数 "+_manager.Moves.ToString("00"),18,true);
                if(Hit(R(128,1089,632,171,879,1789),"Next Level")) StartLevel(_manager.CurrentLevel%_manager.LevelCount+1);
                // The art includes an undo control, but success is terminal and it stays disabled.
                DisabledButton(R(172,1276,535,103,879,1789));
                if(Hit(R(217,1400,461,105,879,1789),"Back Home")) _manager.ShowHome();
            }
            else
            {
                for(int i=0;i<3;i++) _ui.Star(R(224+i*150,608,107,107,903,1742),i<_manager.Stars);
                Text(R(145,745,620,120,903,1742),_manager.StatusMessage,24,true);
                if(Hit(R(141,917,623,191,903,1742),"Retry")) StartLevel(_manager.CurrentLevel);
                bool enabled=_ui.Interactable;_ui.Interactable=enabled&&_manager.CanUndo;
                if(Hit(R(131,1122,638,141,903,1742),"Undo")) UseItem(CandyItem.Undo);
                _ui.Interactable=enabled;
                if(!_manager.CanUndo)DisabledButton(R(131,1122,638,141,903,1742));
                if(Hit(R(203,1281,493,124,903,1742),"Back Home")) _manager.ShowHome();
            }
        }
        private void DrawDialog()
        {
            Shade();
            if(CurrentDialog==Dialog.Settings)
            {
                _ui.Full("SettingsPanel");
                SettingSwitch(738,Economy.Data.music,()=>{Economy.Data.music=!Economy.Data.music;});
                SettingSwitch(881,Economy.Data.effects,()=>{Economy.Data.effects=!Economy.Data.effects;});
                SettingSwitch(1025,Economy.Data.vibration,()=>{Economy.Data.vibration=!Economy.Data.vibration;});
                if(Hit(R(731,462,132,134),"Close")||Hit(R(142,1198,623,165),"Confirm")) CloseDialog();
                return;
            }
            if(CurrentDialog==Dialog.Energy)
            {
                // Extraction crop normalized back to original approved modal bounding box.
                _ui.Region("EnergyPanel",new Rect(20,218,900,1300),new Rect(41,162,307,536));
                Text(new Rect(123,376,148,47),Economy.Energy+" / 10",29,true);
                int sec=Economy.SecondsToRecovery;
                Text(new Rect(107,469,173,26),Economy.Energy==10?"体力已满":"下次恢复 "+(sec/60).ToString("00")+":"+(sec%60).ToString("00"),15,true);
                Text(new Rect(89,580,214,43),"每日上限3次 · 合计上限10次\n广告服务待接入",12,true);
                if(Hit(new Rect(299,184,51,63),"Close")) CloseDialog();
                if(Hit(new Rect(96,630,195,53),"Confirm")) CloseDialog();
                bool enabled=_ui.Interactable;_ui.Interactable=enabled&&Economy.Energy<10;
                if(Hit(new Rect(68,500,269,72),"Watch Ad")) RequestAd();
                _ui.Interactable=enabled;
                if(Economy.Energy==10) Text(new Rect(78,538,250,22),"体力已满，无需补充",13,true);
                if(Button(new Rect(44,720,145,43),"每日免费补给")) CurrentDialog=Dialog.Daily;
                if(Button(new Rect(201,720,145,43),"道具补给")) CurrentDialog=Dialog.Supplies;
                return;
            }
            if(CurrentDialog==Dialog.Supplies)
            {
                _ui.Full("SuppliesPanel");
                if(Hit(R(737,287,124,120,941,1672),"Close")) CloseDialog();
                for(int i=0;i<3;i++)
                {
                    Text(R(368,644+i*217,105,40,941,1672),"库存 "+Economy.Count((CandyItem)i),13,true);
                    Text(R(569,565+i*217,212,43,941,1672),"每日上限3次",12,true);
                    if(Hit(R(542,621+i*217,267,105,941,1672),"Watch Ad " + (CandyItem)i)) RequestAd();
                }
                Text(R(190,1194,560,65,941,1672),"激励合计上限10次 · 广告服务待接入",14,true);
                return;
            }
            if(CurrentDialog==Dialog.Daily)
            {
                _ui.Full("DailyPanel");
                if(Hit(R(718,398,131,130,941,1672),"Close")) CloseDialog();
                Rect claim=R(202,975,537,126,941,1672);
                bool enabled=_ui.Interactable;
                _ui.Interactable=enabled&&Economy.CanClaimDaily;
                if(Hit(claim,"Claim Daily")&&Economy.ClaimDaily()) ShowMessage("领取成功\n三种道具各+1，已放入库存");
                _ui.Interactable=enabled;
                if(!Economy.CanClaimDaily) { DisabledButton(claim); Text(claim,"今日已领取",19,true); }
                return;
            }
            if(CurrentDialog==Dialog.Pause)
            {
                _ui.Region("PausePanel",new Rect(12,242,940,1140),new Rect(23,190,344,450));
                if(Hit(new Rect(50,441,290,99),"Resume")) CloseDialog();
                if(Hit(new Rect(80,546,233,70),"Back Home")) {CloseDialog();_manager.ShowHome();}
                if(Button(new Rect(68,665,111,40),"设置")) CurrentDialog=Dialog.Settings;
                if(Button(new Rect(205,665,111,40),"重新开始")) CurrentDialog=Dialog.Restart;
                return;
            }
            if(CurrentDialog==Dialog.Restart)
            {
                _ui.Full("RestartPanel");
                Text(R(267,916,402,53,941,1672),"当前体力 "+Economy.Energy+" / 10",19,true);
                if(Hit(R(698,248,182,169,941,1672),"Close")||Hit(R(227,1183,490,114,941,1672),"Cancel")) CloseDialog();
                if(Hit(R(150,1005,645,166,941,1672),"Confirm Restart")) StartLevel(_manager.CurrentLevel);
                return;
            }
            if(CurrentDialog==Dialog.NoAd)
            {
                _ui.Full("NoAdPanel");
                Text(R(226,1063,487,106,941,1672),Mathf.Max(0,Mathf.CeilToInt(_retryAt-Time.realtimeSinceStartup))>0?
                    Mathf.CeilToInt(_retryAt-Time.realtimeSinceStartup)+"秒后重试":"重试",26,true,CandyArt.Cream);
                // Cover the baked return button with the existing transparent cookie
                // artwork, so its new size does not carry a rectangular panel patch.
                Rect backButton=R(155,1198,637,151,941,1672);
                _ui.Region("ButtonBlank",new Rect(60,80,2055,584),backButton,id:"Back Button Artwork");
                _ui.Label(backButton,"返回",30,true,CandyArt.Chocolate,id:"Return Label");
                if(Hit(R(728,306,171,172,941,1672),"Close")||Hit(backButton,"Back")) CurrentDialog=_returnDialog;
                bool enabled=_ui.Interactable; _ui.Interactable=enabled&&Time.realtimeSinceStartup>=_retryAt;
                if(Hit(R(155,1040,637,151,941,1672),"Retry Ad")) _retryAt=Time.realtimeSinceStartup+15;
                _ui.Interactable=enabled;
                Text(new Rect(78,739,234,26),"广告服务尚未接入，不发放奖励",13,true,CandyArt.Cream);
                return;
            }
            Frame(new Rect(25,211,340,437));
            // Generic messages close through their primary acknowledgement button.
            // A cropped close from a composite panel includes part of its border; do not draw that artifact.
            Text(new Rect(48,257,294,47),"温馨提示",25,true);
            Text(new Rect(62,327,266,140),_message,21,true);
            if(Button(new Rect(66,528,258,56),"知道了",true)) CloseDialog();
        }
        private void SettingSwitch(float y,bool value,System.Action change)
        {
            _ui.Region("Settings",value?new Rect(564,737,190,98):new Rect(564,1024,190,98),R(564,y,190,98));
            if(Hit(R(137,y-15,627,128), y==738 ? "Toggle Music" : y==881 ? "Toggle Effects" : "Toggle Vibration")) {change();Economy.Save();_feedback.Apply();}
        }
        private void LiveText(Rect r,string text,int size) => _ui.Label(r,text,size,true,CandyArt.Chocolate,true);
        private bool Button(Rect r,string text,bool primary=false)
        {
            if(primary) _ui.Region("WorldPieces",new Rect(33,322,701,194),r);
            else _ui.Image("Button Background",CandySkin.Image("ButtonBlank"),r,new Rect(0,0,1,1),Color.white);
            Text(r,text,r.height<35?13:19,true,primary?CandyArt.Cream:CandyArt.Chocolate);
            return Hit(r,text);
        }
        private bool Hit(Rect r,string title=null)
        {
            bool hit=_ui.Hit(r,title);
            if(hit)_feedback.Click();
            return hit;
        }
        private void Text(Rect r,string text,int size,bool bold=false,Color? color=null) =>
            _ui.Label(r,text,size,bold,color??CandyArt.Chocolate);
        private void Fill(Rect r,Color color) => _ui.Fill(r,color);
        private void DisabledButton(Rect r) => _ui.Fill(r,new Color(.5f,.45f,.5f,.35f));
        private void Shade() => _ui.Fill(new Rect(0,0,390,844),new Color(.1f,.05f,.15f,.55f),true);
        private void Frame(Rect r) => _ui.Image("Message Panel",_panel,r,new Rect(0,0,1,1),Color.white);
#if UNITY_EDITOR
        // Editor visual checks use a disposable in-memory save, never mutate player progress.
        public void PreviewForAudit(string page)
        {
            Economy = new CandyEconomy("",()=>1801900800,_=>{});
            Economy.Data.energy=6; Economy.Save(); _feedback.Configure(Economy);
            CurrentDialog=Dialog.None;_select=false;_wasPaused=false;_manager.ShowHome();
            if(page=="select") _select=true;
            else if(page=="game"||page=="pause"||page=="restart"||page=="win"||page=="fail")
            {
                _manager.LoadLevel(8);_manager.SetPaused(true);
                if(page=="pause")OpenDialog(Dialog.Pause);
                if(page=="restart")OpenDialog(Dialog.Restart);
                if(page=="win"||page=="fail")_manager.PreviewResultForAudit(page=="win");
            }
            else if(page=="message")ShowMessage("领取成功\n三种道具各+1，已放入库存");
            else if(System.Enum.TryParse(page,true,out Dialog d)&&d!=Dialog.None) OpenDialog(d);
        }
#endif
    }
}


