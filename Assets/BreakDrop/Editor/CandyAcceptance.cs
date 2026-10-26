using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace BreakDrop.Editor
{
    public static class CandyAcceptance
    {
        private static readonly List<string> Checks = new List<string>();
        private static void Check(string name, bool pass)
        {
            Checks.Add((pass ? "PASS " : "FAIL ") + name);
            if (!pass) throw new Exception(name);
        }
        [MenuItem("Tools/Break & Drop/Validate Candy Systems")]
        public static void Run()
        {
            Checks.Clear();
            try
            {
                long now = 1801900790; string saved = "";
                var e = new CandyEconomy("", () => now, s => saved = s);
                Check("initial energy and one-time item grant", e.Energy == 10 && e.Count(CandyItem.Hint) == 3);
                Check("start costs one", e.SpendEnergy() && e.Energy == 9);
                now += 599; Check("no early recovery", e.Energy == 9 && e.SecondsToRecovery == 1);
                now++; Check("ten-minute recovery", e.Energy == 10);
                for (int i = 0; i < 10; i++) Check("spend " + i, e.SpendEnergy());
                Check("zero energy blocked", !e.SpendEnergy());
                now += 1200; Check("offline recovery", e.Energy == 2);
                e.Save();
                var reloaded = new CandyEconomy(saved, () => now, s => saved = s);
                Check("persist inventory without duplicate welcome grant", reloaded.Energy == 2 && reloaded.Count(CandyItem.Hint) == 3);
                for (int i = 0; i < 3; i++) Check("hint consumes", reloaded.Consume(CandyItem.Hint));
                Check("empty/limit not consumed", !reloaded.Consume(CandyItem.Hint));
                Check("daily no ad required", reloaded.ClaimDaily());
                Check("daily cannot repeat", !reloaded.ClaimDaily());
                Check("daily does not reset per-run cap", !reloaded.Consume(CandyItem.Hint));
                reloaded.BeginRun(); Check("new run resets cap", reloaded.Consume(CandyItem.Hint));
                Check("time first", reloaded.Consume(CandyItem.Time)); Check("time second", reloaded.Consume(CandyItem.Time));
                Check("time cap", !reloaded.Consume(CandyItem.Time));
                string day = reloaded.Today; now += 86400; Check("Beijing daily reset", reloaded.Today != day && reloaded.ClaimDaily());
                long effective = reloaded.Now; now -= 86400 * 2; Check("clock rollback bounded locally", reloaded.Now >= effective);
                foreach (string asset in new[] { "Background", "HomeTower", "Panel" })
                {
                    string path = "Assets/BreakDrop/Resources/Candy/" + asset + ".png";
                    var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                    importer.mipmapEnabled = false; importer.alphaIsTransparency = true;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.maxTextureSize = 2048; importer.SaveAndReimport();
                    Check("asset " + asset, Resources.Load<Texture2D>("Candy/" + asset) != null);
                }
                var l = CandyArt.Shape("L3", 3, 3, Color.magenta).texture;
                Check("L notch remains transparent", l.GetPixel(240, 48).a == 0 && l.GetPixel(48, 48).a > .9f);
                var stale = CandyArt.Shape("Rect",1,1,Color.cyan);
                var staleTexture = stale.texture;
                UnityEngine.Object.DestroyImmediate(stale);
                UnityEngine.Object.DestroyImmediate(staleTexture);
                var recovered = CandyArt.Shape("Rect",1,1,Color.cyan);
                Check("destroyed sprite cache recovers after Play exit", recovered != null && recovered.texture != null);
                foreach (string name in new[] { "HomeLive", "SelectLive", "GameLive", "StateIcons", "ButtonBlank",
                    "SettingsPanel", "EnergyPanel", "SuppliesPanel", "DailyPanel", "PausePanel", "RestartPanel", "NoAdPanel", "WinPanel", "FailPanel", "WorldPieces" })
                {
                    var texture = CandySkin.Image(name);
                    Check("approved runtime asset " + name, texture != null);
                    Check("readable native-resolution artwork " + name, texture.isReadable && texture.width > 800);
                }
                var host = new GameObject("Candy Validation Host");
                var game = host.AddComponent<BreakDropGameManager>();
                try
                {
                    Physics2D.simulationMode = SimulationMode2D.Script;
                    Physics2D.gravity = new Vector2(0, -14);
                    game.LoadLevel(1);
                    game.ShowHome(); game.LoadLevel(1);
                    Check("same-frame Home to same level rebuild", game.Target != null && game.Blocks.Count == 10);
                    for (int i = 0; i < 120; i++) { Physics2D.Simulate(1f / 60); game.TickPhysics(1f / 60); }
                    Check("level plays", game.State == BreakDropGameState.Playing);
                    float t = game.TimeRemaining; game.SetPaused(true); game.TickPhysics(1); Check("pause freezes timer", game.TimeRemaining == t);
                    game.SetPaused(false); game.AddTime(30); Check("time item adds thirty", Mathf.Abs(game.TimeRemaining - t - 30) < .01f);
                    var block = game.Blocks.First(b => b.Id == game.Level.hintOrder[0]);
                    game.TryBreak(block); Check("tap removes physics", block.IsBroken && !block.Body.simulated && game.CanUndo);
                    t = game.TimeRemaining; game.Undo(); Check("undo restores without time refund", !block.IsBroken && game.TimeRemaining == t);
                    game.TickPhysics(1000); // Ready transitions to Playing, then expire.
                    game.TickPhysics(1000);
                    Check("timeout cannot undo", game.TimeRemaining == 0 && !game.CanUndo);
                    game.AddTime(30); Check("time item cannot revive", game.TimeRemaining == 0);
                }
                finally { UnityEngine.Object.DestroyImmediate(host); Physics2D.simulationMode = SimulationMode2D.FixedUpdate; Time.timeScale = 1; }
                Debug.Log("CANDY_ACCEPTANCE_PASS " + Checks.Count);
            }
            catch (Exception ex) { Checks.Add("FAIL " + ex); Debug.LogException(ex); }
            Directory.CreateDirectory("docs/validation/candy");
            File.WriteAllText("docs/validation/candy/systems.txt", Application.unityVersion + "\n" + string.Join("\n", Checks));
            if (Application.isBatchMode) EditorApplication.Exit(Checks.Any(c => c.StartsWith("FAIL")) ? 1 : 0);
        }
    }
}
