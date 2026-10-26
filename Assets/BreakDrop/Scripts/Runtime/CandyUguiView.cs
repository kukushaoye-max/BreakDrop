using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BreakDrop
{
    [DisallowMultipleComponent]
    public sealed class CandyUguiView : MonoBehaviour
    {
        public const string ResourcePath = "UI/CandyCanvas";
        private sealed class Page
        {
            public RectTransform Root;
            public readonly Dictionary<string, GameObject> Controls = new Dictionary<string, GameObject>();
            public readonly HashSet<string> Used = new HashSet<string>();
        }
        private readonly Dictionary<string, Page> pages = new Dictionary<string, Page>();
        private readonly HashSet<Button> boundButtons = new HashSet<Button>();
        private readonly HashSet<string> clicks = new HashSet<string>();
        private RectTransform viewport;
        private GameObject modalBlocker;
        private Material artworkMaterial;
        private Page page;
        private string pageName;
        private Font regular, bold;
        public bool Interactable { get; set; } = true;

        public void Initialize(Font normalFont, Font boldFont)
        {
            if (viewport) return;
            regular = normalFont; bold = boldFont;
            var canvas = GetComponent<Canvas>();
            if (!canvas) canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = GetComponent<CanvasScaler>();
            if (!scaler) scaler = gameObject.AddComponent<CanvasScaler>();
            // Shared viewport fitting scales UI and the physics camera identically.
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1;
            if (!GetComponent<GraphicRaycaster>()) gameObject.AddComponent<GraphicRaycaster>();
            var blocker = transform.Find("ModalInputBlocker") as RectTransform;
            if (!blocker)
            {
                blocker = CreateRect("ModalInputBlocker", transform);
                blocker.anchorMin = Vector2.zero; blocker.anchorMax = Vector2.one;
                blocker.offsetMin = blocker.offsetMax = Vector2.zero;
                var image = blocker.gameObject.AddComponent<Image>();
                image.color = Color.clear; image.raycastTarget = true;
            }
            artworkMaterial = Resources.Load<Material>("UI/SquareCornerArtwork");
            // Remove the previous full-screen extension from existing Canvas prefabs.
            for (int i=0;i<4;i++)
            {
                var edge=transform.Find("Background Edge " + i);
                if (!edge) continue;
                edge.gameObject.SetActive(false);
                Destroy(edge.gameObject);
            }
            modalBlocker = blocker.gameObject;
            viewport = transform.Find("SafeArea") as RectTransform;
            if (!viewport) viewport = CreateRect("SafeArea", transform);
            viewport.anchorMin = viewport.anchorMax = viewport.pivot = Vector2.zero;
            foreach (var element in viewport.GetComponentsInChildren<CandyUguiElement>(true))
            {
                if (!element.IsPage) continue;
                var saved = new Page { Root = (RectTransform)element.transform };
                foreach (var control in element.GetComponentsInChildren<CandyUguiElement>(true))
                    if (!control.IsPage) saved.Controls.Add(control.Key, control.gameObject);
                pages.Add(element.Key, saved);
                saved.Root.gameObject.SetActive(false);
            }
            if (!EventSystem.current && !FindObjectOfType<EventSystem>())
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }
        public void Begin()
        {
            Rect v = DesignArt.Viewport;
            viewport.anchoredPosition = v.position;
            viewport.sizeDelta = new Vector2(390,844);
            viewport.localScale = Vector3.one * (v.width / 390f);
            foreach (var p in pages.Values) p.Used.Clear();
        }
        public void PageGroup(string name)
        {
            pageName = name;
            if (!pages.TryGetValue(name, out page))
            {
                string[] parts = name.Split('/');
                var group = viewport.Find(parts[0]) as RectTransform;
                if (!group) { group = CreateRect(parts[0], viewport); SetRect(group,new Rect(0,0,390,844)); }
                page = new Page { Root = CreateRect(parts[1], group) };
                SetRect(page.Root, new Rect(0,0,390,844));
                page.Root.gameObject.AddComponent<CandyUguiElement>().Configure(name,true);
                pages.Add(name, page);
            }
            page.Root.gameObject.SetActive(true);
            page.Root.parent.SetAsLastSibling();
            page.Root.SetAsLastSibling();
        }
        public void End()
        {
            bool overlay = false;
            foreach (var pair in pages)
            {
                var p = pair.Value;
                p.Root.gameObject.SetActive(p.Used.Count > 0);
                foreach (var control in p.Controls) control.Value.SetActive(p.Used.Contains(control.Key));
                if (p.Used.Count > 0 && !pair.Key.StartsWith("Pages/")) overlay = true;
            }
            modalBlocker.SetActive(overlay);
            clicks.Clear();
        }
        private static RectTransform CreateRect(string name, Transform parent)
        {
            var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            r.gameObject.layer = LayerMask.NameToLayer("UI");
            r.SetParent(parent, false); return r;
        }
        private static void SetRect(RectTransform r, Rect box)
        {
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0,1);
            r.anchoredPosition = new Vector2(box.x, -box.y); r.sizeDelta = box.size;
        }
        private static string Slot(Rect rect) => string.Format(CultureInfo.InvariantCulture,
            "{0:F3},{1:F3},{2:F3},{3:F3}",rect.x,rect.y,rect.width,rect.height);
        private T Control<T>(string kind, Rect rect, string id, out bool created, bool dynamicPosition = false) where T : Component
        {
            string key = kind + ":" + (id ?? Slot(rect));
            created = !page.Controls.TryGetValue(key,out var go);
            if (created)
            {
                go = CreateRect(id ?? kind,page.Root).gameObject;
                go.AddComponent<T>(); go.AddComponent<CandyUguiElement>().Configure(key);
                page.Controls.Add(key,go);
                SetRect((RectTransform)go.transform,rect);
            }
            // Keep authored RectTransforms; only progress markers follow the moving world.
            if (dynamicPosition) SetRect((RectTransform)go.transform,rect);
            page.Used.Add(key); go.SetActive(true); go.transform.SetAsLastSibling();
            return go.GetComponent<T>();
        }
        public void Image(string name,Texture texture,Rect rect,Rect uv,Color color,bool block=false,string id=null,bool dynamicPosition=false)
        {
            var image=Control<RawImage>("Image",rect,id,out bool created,dynamicPosition);
            if(created) image.name=id??name;
            image.texture=texture; image.uvRect=uv; image.color=color; image.raycastTarget=block;
        }
        private static readonly Dictionary<string, Sprite> homeSprites = new Dictionary<string, Sprite>();
        private static Sprite HomeSprite(string name)
        {
            if (homeSprites.Count == 0)
                foreach (var sprite in Resources.LoadAll<Sprite>("UI/Home/HomeAtlas")) homeSprites[sprite.name] = sprite;
            return homeSprites.TryGetValue(name, out var result) ? result : null;
        }
        public void HomeArt(string name, Rect rect)
        {
            var graphic = Control<Image>("Sprite", rect, name, out bool created);
            graphic.sprite = HomeSprite(name);
            graphic.color = Color.white;
            graphic.raycastTarget = false;
            graphic.preserveAspect = true;
        }
        public bool HomeButton(string name, Rect rect, bool preserveAspect = true)
        {
            bool clicked = Hit(rect, name);
            var button = page.Controls["Button:" + name].GetComponent<Button>();
            var graphic = button.GetComponent<Image>();
            graphic.sprite = HomeSprite(name);
            graphic.color = Color.white;
            graphic.preserveAspect = preserveAspect;
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.pressedColor = new Color(.83f, .83f, .83f, 1);
            colors.highlightedColor = Color.white;
            colors.selectedColor = Color.white;
            button.colors = colors;
            return clicked;
        }
        public void HomeBackground()
        {
            Image("Background", Resources.Load<Texture2D>("Candy/Background"), new Rect(0,0,390,844),
                new Rect(0,0,1,1), Color.white, id:"Background");
        }
        public void Full(string name)
        {
            Image(name,CandySkin.Image(name),new Rect(0,0,390,844),new Rect(0,0,1,1),Color.white,id:"Artwork");
            if (!pageName.StartsWith("Pages/"))
            {
                page.Controls["Image:Artwork"].GetComponent<RawImage>().material=null;
                return;
            }
            page.Controls["Image:Artwork"].GetComponent<RawImage>().material=artworkMaterial;
        }
        public void Region(string name,Rect source,Rect destination,string id=null,bool dynamicPosition=false)
        {
            var t=CandySkin.Image(name); if(!t)return;
            Image(name,t,destination,new Rect(source.x/t.width,1-source.yMax/t.height,source.width/t.width,source.height/t.height),Color.white,id:id,dynamicPosition:dynamicPosition);
        }
        public void Star(Rect destination,bool earned,string id=null,bool dynamicPosition=false) => Region("StateIcons",earned?new Rect(1153,50,410,390):new Rect(1153,461,410,386),destination,id,dynamicPosition);
        public void Fill(Rect r,Color color,bool block=false,string id=null,bool dynamicPosition=false) => Image(block?"ModalBlocker":"Tint",Texture2D.whiteTexture,r,new Rect(0,0,1,1),color,block,id,dynamicPosition);
        public void Label(Rect r,string value,int size,bool strong,Color color,bool fit=false,string id=null)
        {
            var label=Control<Text>("Text",r,id,out bool created);
            if(created)
            {
                label.name=id??"Text "+value.Replace("\n"," ");
                label.font=strong?bold:regular; label.fontSize=size;
                label.alignment=TextAnchor.MiddleCenter; label.color=color; label.raycastTarget=false;
                label.horizontalOverflow=HorizontalWrapMode.Wrap; label.verticalOverflow=VerticalWrapMode.Truncate;
                label.resizeTextForBestFit=fit; label.resizeTextMinSize=8; label.resizeTextMaxSize=size;
            }
            // Noto CJK line metrics can exceed the requested font size. Fit instead of
            // dropping the entire line when a compact design rectangle is shorter.
            label.resizeTextForBestFit=true;
            if(label.text!=value)label.text=value;
        }
        public bool Hit(Rect r,string title=null)
        {
            var button=Control<Button>("Button",r,title,out bool created);
            string id=pageName+":"+button.GetComponent<CandyUguiElement>().Key;
            if(created)
            {
                var graphic=button.gameObject.AddComponent<Image>(); graphic.color=Color.clear;
                button.targetGraphic=graphic; button.transition=Selectable.Transition.None;
                button.navigation=new Navigation { mode=Navigation.Mode.None };
                button.name=title??"Button "+Slot(r);
            }
            button.interactable=Interactable;
            if(boundButtons.Add(button))button.onClick.AddListener(()=>
            {
                if(button.IsActive()&&button.IsInteractable())clicks.Add(id);
            });
            return Interactable&&clicks.Remove(id);
        }
    }
}

