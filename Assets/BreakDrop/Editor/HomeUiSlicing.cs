using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BreakDrop.Editor
{
    [InitializeOnLoad]
    public static class HomeUiSlicing
    {
        private const string Atlas = "Assets/BreakDrop/Resources/UI/Home/HomeAtlas.png";
        private const string Request = "Temp/breakdrop-home-slices.request";
        private static readonly string[] Names = { "Energy", "Settings", "Title", "Hero", "Start Game", "Level Select" };
        static HomeUiSlicing() { EditorApplication.update += Poll; }
        private static void Poll()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists(Request)) return;
            var action = File.ReadAllText(Request).Trim(); File.Delete(Request);
            try { if (action == "slice") Slice(); else if (action == "prune") Prune(); else if (action == "check") CheckButtons(); }
            catch (Exception ex) { Debug.LogException(ex); File.WriteAllText("Temp/home-slices-error.txt", ex.ToString()); }
        }
        [MenuItem("Tools/Break & Drop/UI/Slice Home Functional Sprites")]
        public static void Slice()
        {
            AssetDatabase.ImportAsset(Atlas, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(Atlas);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.isReadable = true; importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 4096;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Atlas);
            // Measured alpha silhouettes on the approved 1086 x 1448 atlas, top-left origin.
            var bounds = new[] {
                new Rect(24,186,531,169), new Rect(682,128,281,279),
                new Rect(0,576,583,387), new Rect(576,499,494,510),
                new Rect(18,1125,592,221), new Rect(627,1153,448,170)
            };
            if(texture.width != 1086 || texture.height != 1448)
                throw new Exception("Home atlas changed dimensions; review sprite boundaries before importing.");
            var slices = new SpriteMetaData[6];
            for (int index = 0; index < Names.Length; index++)
            {
                var box = bounds[index];
                slices[index] = new SpriteMetaData { name = Names[index],
                    rect = new Rect(box.x,texture.height-box.yMax,box.width,box.height),
                    pivot = Vector2.one * .5f, alignment = (int)SpriteAlignment.Center };
            }
            importer.spritesheet = slices;
            importer.isReadable = false;
            importer.SaveAndReimport();
            Directory.CreateDirectory("docs/validation/home-slices");
            File.WriteAllLines("docs/validation/home-slices/sprites.txt", slices.Select(s => s.name + ": " + s.rect));
            Debug.Log("[Home UI] Six transparent functional sprites imported. Build Canvas to persist hierarchy.");
        }
        private static void CheckButtons()
        {
            var hud = UnityEngine.Object.FindObjectOfType<BreakDropHud>();
            if (!Application.isPlaying || !hud) throw new Exception("Enter Play first.");
            var lines = new System.Collections.Generic.List<string>();
            foreach (string name in new[]{"Energy","Settings","Level Select","Start Game"})
            {
                hud.PreviewForAudit("home"); hud.SendMessage("LateUpdate");
                var home = hud.GetComponentInChildren<CandyUguiView>(true).transform.Find("SafeArea/Pages/Home");
                var button = home.Find(name).GetComponent<Button>();
                if (!button.GetComponent<Image>().sprite || !button.interactable) throw new Exception("Button inactive: " + name);
                button.onClick.Invoke(); hud.SendMessage("LateUpdate"); hud.SendMessage("LateUpdate");
                bool passed = name=="Energy" ? hud.CurrentDialog==BreakDropHud.Dialog.Energy :
                    name=="Settings" ? hud.CurrentDialog==BreakDropHud.Dialog.Settings :
                    name=="Start Game" ? hud.GetComponent<BreakDropGameManager>().State==BreakDropGameState.Ready :
                    home.parent.Find("LevelSelect").gameObject.activeSelf;
                if (!passed) throw new Exception("Wrong click action: " + name);
                lines.Add("PASS " + name + " sprite and click action");
            }
            hud.PreviewForAudit("home"); hud.SendMessage("LateUpdate");
            File.WriteAllLines("docs/validation/home-slices/buttons.txt",lines);
        }
        private static void Prune()
        {
            var root = PrefabUtility.LoadPrefabContents(UguiAuthoring.PrefabPath);
            try
            {
                var home = root.transform.Find("SafeArea/Pages/Home");
                if (!home) throw new Exception("Home page missing");
                var expected = new[] { "Background", "Title", "Hero", "Energy", "Energy Count", "Settings", "Start Game", "Level Select" };
                foreach (Transform child in home.Cast<Transform>().ToArray())
                    if (!expected.Contains(child.name)) UnityEngine.Object.DestroyImmediate(child.gameObject);
                if (home.childCount != 8) throw new Exception("Expected eight Home controls, found " + home.childCount);
                foreach (string name in new[]{"Energy","Settings","Start Game","Level Select"})
                {
                    var button = home.Find(name).GetComponent<Button>();
                    if (!button || !button.GetComponent<Image>().sprite) throw new Exception("Missing visible button sprite: " + name);
                }
                PrefabUtility.SaveAsPrefabAsset(root,UguiAuthoring.PrefabPath);
                File.WriteAllText("docs/validation/home-slices/hierarchy.txt",string.Join("\n",home.Cast<Transform>().Select(t=>"Canvas/SafeArea/Pages/Home/"+t.name)));
                Debug.Log("[Home UI] PASS separate background, title, hero, four sprite buttons and dynamic energy text.");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}