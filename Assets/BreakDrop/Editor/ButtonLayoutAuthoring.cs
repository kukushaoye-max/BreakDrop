using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BreakDrop.Editor
{
    // Explicit migration of existing authored RectTransforms. Runtime keeps them editable.
    [InitializeOnLoad]
    public static class ButtonLayoutAuthoring
    {
        private const string Request = "Temp/breakdrop-button-layout.request";
        static ButtonLayoutAuthoring() { EditorApplication.update += Poll; }
        private static void Poll()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists(Request)) return;
            File.Delete(Request);
            try { Apply(); }
            catch (Exception ex) { Debug.LogException(ex); }
        }

        [MenuItem("Tools/Break & Drop/UI/Unify Button Sizes")]
        public static void Apply()
        {
            var root = PrefabUtility.LoadPrefabContents(UguiAuthoring.PrefabPath);
            try
            {
                Set(root,"Pages/Home","Start Game",new Rect(55,606,280,106));
                Set(root,"Pages/Home","Level Select",new Rect(55,722,280,106));
                Set(root,"Dialogs/NoAd","Retry Ad",CandySkin.R(155,1040,637,151,941,1672));
                Set(root,"Dialogs/NoAd","Back",CandySkin.R(155,1198,637,151,941,1672));
                SetReturnArtwork(root);
                for (int i=0;i<3;i++)
                    Set(root,"Dialogs/Supplies","Watch Ad " + (CandyItem)i,
                        CandySkin.R(542,621+i*217,267,105,941,1672));
                PrefabUtility.SaveAsPrefabAsset(root,UguiAuthoring.PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
            Debug.Log("[Button Layout] Saved equal-sized Home, NoAd and Supplies button groups.");
        }

        private static void Set(GameObject root,string page,string name,Rect box)
        {
            var group=root.GetComponentsInChildren<CandyUguiElement>(true)
                .First(x=>x.IsPage && x.Key==page);
            var button=group.GetComponentsInChildren<CandyUguiElement>(true)
                .First(x=>x.Key=="Button:"+name);
            var rect=(RectTransform)button.transform;
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);
            rect.anchoredPosition=new Vector2(box.x,-box.y);
            rect.sizeDelta=box.size;
            if(page=="Pages/Home") button.GetComponent<Image>().preserveAspect=false;
        }

        private static void SetReturnArtwork(GameObject root)
        {
            var page=root.GetComponentsInChildren<CandyUguiElement>(true)
                .First(x=>x.IsPage && x.Key=="Dialogs/NoAd");
            Rect box=CandySkin.R(155,1198,637,151,941,1672);
            var art=Ensure<RawImage>(page,"Image:Back Button Artwork","Back Button Artwork",box);
            var texture=CandySkin.Image("ButtonBlank");
            art.texture=texture;
            art.uvRect=new Rect(60f/texture.width,1-664f/texture.height,2055f/texture.width,584f/texture.height);
            art.color=Color.white;art.raycastTarget=false;
            var label=Ensure<Text>(page,"Text:Return Label","Return Label",box);
            label.text="返回";label.font=Resources.Load<Font>("Fonts/NotoSansSC-Bold");
            label.fontSize=30;label.color=CandyArt.Chocolate;label.alignment=TextAnchor.MiddleCenter;
            label.resizeTextForBestFit=true;label.resizeTextMinSize=8;label.resizeTextMaxSize=30;
            label.raycastTarget=false;
        }

        private static T Ensure<T>(CandyUguiElement page,string key,string name,Rect box) where T:Component
        {
            var element=page.GetComponentsInChildren<CandyUguiElement>(true).FirstOrDefault(x=>x.Key==key);
            if(!element)
            {
                var go=new GameObject(name,typeof(RectTransform),typeof(T));
                go.layer=LayerMask.NameToLayer("UI");go.transform.SetParent(page.transform,false);
                element=go.AddComponent<CandyUguiElement>();element.Configure(key);
            }
            var rect=(RectTransform)element.transform;
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);
            rect.anchoredPosition=new Vector2(box.x,-box.y);rect.sizeDelta=box.size;
            rect.SetAsLastSibling();
            return element.GetComponent<T>();
        }
    }
}
