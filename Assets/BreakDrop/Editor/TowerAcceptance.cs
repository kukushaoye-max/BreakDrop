using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BreakDrop.Editor
{
    public static class TowerAcceptance
    {
        [Serializable] private sealed class Report
        {
            public string unityVersion;
            public List<string> checks = new List<string>();
            public List<string> failures = new List<string>();
            public List<string> runs = new List<string>();
        }
        private static Report _report;
        private const float Dt = 1f / 60;

        [MenuItem("Tools/Break & Drop/Validate Tower Physics")]
        public static void Run()
        {
            _report = new Report { unityVersion = Application.unityVersion };
            SimulationMode2D previous = Physics2D.simulationMode;
            Scene active = SceneManager.GetActiveScene();
            if (!Application.isBatchMode && string.IsNullOrEmpty(active.path))
                throw new InvalidOperationException("Save the current scene before running physics validation.");
            Scene testScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                Application.isBatchMode ? NewSceneMode.Single : NewSceneMode.Additive);
            PhysicsScene2D physics = Physics2D.defaultPhysicsScene;
            SceneManager.SetActiveScene(testScene);
            var host = new GameObject("Acceptance Host");
            var manager = host.AddComponent<BreakDropGameManager>();
            try
            {
                if (UnityEngine.Object.FindObjectsOfType<Rigidbody2D>().Any(b => b.gameObject.scene != testScene))
                    throw new InvalidOperationException("Run validation with no other live physics scene open.");
                Physics2D.simulationMode = SimulationMode2D.Script;
                Physics2D.gravity = new Vector2(0, -14);
                Physics2D.velocityIterations = 12;
                Physics2D.positionIterations = 8;
                ImportArt();
                Check("Twenty tower levels loaded", manager.LevelCount == 20);
                var usedArt = new HashSet<string>();
                var usedAngles = new HashSet<int>();
                var layoutSignatures = new HashSet<string>();
                LevelDefinition previousLevel = null;
                for (int level = 1; level <= manager.LevelCount; level++)
                {
                    int expectedBlocks = level <= 5 ? 10 : level <= 15 ? 18 : 21;
                    manager.LoadLevel(level);
                    foreach (BlockDefinition block in manager.Level.blocks)
                    {
                        usedArt.Add(block.art);
                        usedAngles.Add(Mathf.RoundToInt(block.rotation));
                    }
                    Check("L" + level + " uses the requested block count", manager.Blocks.Count == expectedBlocks);
                    int minimumArt = level <= 5 ? 4 : level <= 15 ? 6 : 7;
                    int minimumRoute = level <= 5 ? 4 : level <= 15 ? 7 : 10;
                    int maximumRoute = level <= 5 ? 6 : level <= 15 ? 11 : 15;
                    int l3Count = manager.Level.blocks.Count(b => b.shape == "L3");
                    int l2Count = manager.Level.blocks.Count(b => b.shape == "L2");
                    int verticalCount = manager.Level.blocks.Count(PrototypeLevelBatchGenerator.IsVerticalBar);
                    Check("L" + level + " meets art variety", manager.Level.blocks.Select(b => b.art).Distinct().Count() >= minimumArt);
                    Check("L" + level + " meets L-piece rules", l3Count >= 1 &&
                        (level <= 5 || l2Count >= 1) && (level <= 15 || l2Count + l3Count >= 3));
                    Check("L" + level + " meets vertical-bar rules", level == 1 ||
                        verticalCount >= (level <= 15 ? 1 : 2));
                    Check("L" + level + " meets cavity rules",
                        PrototypeLevelBatchGenerator.CountInternalCavities(manager.Level) >=
                        (level <= 5 ? 0 : level <= 15 ? 1 : 2));
                    Check("L" + level + " solution length is in tier range",
                        manager.Level.hintOrder.Length >= minimumRoute && manager.Level.hintOrder.Length <= maximumRoute);
                    Check("L" + level + " solution keeps decoy blocks",
                        manager.Level.hintOrder.Length < manager.Level.blocks.Length);
                    Check("L" + level + " layout signature is unique",
                        layoutSignatures.Add(PrototypeLevelBatchGenerator.LayoutSignature(manager.Level)));
                    if (previousLevel != null)
                    {
                        Check("L" + level + " adjacent overlap is at most 60%",
                            PrototypeLevelBatchGenerator.AdjacentOverlap(previousLevel, manager.Level) <= .60f);
                        Check("L" + level + " changes at least two structural features",
                            PrototypeLevelBatchGenerator.AdjacentFeatureDifferences(previousLevel, manager.Level) >= 2);
                    }
                    previousLevel = manager.Level;
                    BreakDropGameState? firstOutcome = null;
                    for (int run = 0; run < 10; run++)
                    {
                        manager.LoadLevel(level);
                        var original = manager.Blocks.Select(b => b.Body.position).ToArray();
                        Vector2 originalTarget = manager.Target.Body.position;
                        Step(manager, physics, 180);
                        Check("L" + level + " initial stack stable #" + run, manager.State == BreakDropGameState.Playing &&
                            Vector2.Distance(originalTarget, manager.Target.Body.position) < .15f &&
                            manager.Blocks.Where((b, i) => Vector2.Distance(b.Body.position, original[i]) > .15f).Count() == 0);
                        if (run == 0)
                            Check("L" + level + " target has one unambiguous support",
                                CountTargetSupports(manager) == 1);
                        if (run == 0 && level == 6)
                        {
                            BlockDefinition lData = manager.Level.blocks.First(b => b.shape == "L3");
                            BreakableBlock l = manager.Blocks.First(b => b.Id == lData.id);
                            Vector2 notch = l.transform.TransformPoint(new Vector2(.5f, -.5f));
                            Check("L shape uses two colliders and an open concavity", l.Colliders.Length == 2 &&
                                !l.Colliders.Any(c => c.OverlapPoint(notch)));
                        }
                        int[] order = manager.Level.hintOrder;
                        foreach (int id in order)
                        {
                            if (manager.State != BreakDropGameState.Playing) break;
                            var block = manager.Blocks.First(b => b.Id == id);
                            if (block.IsBroken) continue;
                            manager.TryBreak(block);
                            Check("Every collider removed L" + level + " block " + id, block.Colliders.All(c => !c.enabled));
                            Resolve(manager, physics);
                        }
                        Step(manager, physics, 60);
                        if (firstOutcome == null) firstOutcome = manager.State;
                        // Physics acceptance is the same supported outcome, not pixel-identical landing poses.
                        Check("L" + level + " consistent supported outcome #" + run,
                            firstOutcome.Value == manager.State &&
                            (manager.State != BreakDropGameState.Won || manager.HasGoalSupport()));
                        Check("L" + level + " reaches supported win #" + run,
                            manager.State == BreakDropGameState.Won && manager.HasGoalSupport());
                        _report.runs.Add("L" + level + " #" + run + ": " + manager.State +
                            ", moves=" + manager.Moves + ", target=" + manager.Target.Body.position.ToString("F4"));
                    }
                    if (level >= 6)
                    {
                        manager.LoadLevel(level);
                        int[] hintIds = manager.Level.hintOrder;
                        int[] wrongIds = manager.Level.blocks.Select(b => b.id).Except(hintIds).ToArray();
                        bool hazardousBranch = false;
                        foreach (int wrongId in wrongIds)
                        {
                            manager.LoadLevel(level);
                            Step(manager, physics, 180);
                            Vector2 targetBefore = manager.Target.Body.position;
                            var positionsBefore = manager.Blocks.ToDictionary(b => b.Id, b => b.Body.position);
                            BreakableBlock wrong = manager.Blocks.First(b => b.Id == wrongId);
                            manager.TryBreak(wrong);
                            Resolve(manager, physics);
                            Step(manager, physics, 30);
                            hazardousBranch = manager.State == BreakDropGameState.Failed ||
                                Vector2.Distance(targetBefore, manager.Target.Body.position) > .05f ||
                                manager.Blocks.Any(b => !b.IsBroken &&
                                    Vector2.Distance(positionsBefore[b.Id], b.Body.position) > .08f);
                            if (hazardousBranch) break;
                        }
                        Check("L" + level + " has a risky wrong-first branch", hazardousBranch);
                    }
                }
                Check("All eleven block art assets are used", usedArt.SetEquals(new[]
                {
                    "BlockA", "BlockB", "BlockC", "BlockD", "BlockE", "BlockG",
                    "BlockI", "BlockJ", "BlockK", "BlockL", "BlockR"
                }));
                Check("Only the four approved angles are used",
                    usedAngles.SetEquals(new[] { 0, 90, 270, 360 }));
                manager.LoadLevel(6); Step(manager, physics, 180);
                var before = manager.Blocks.Select(b => b.Body.position).ToArray();
                var beforeTarget = manager.Target.Body.position;
                float topY = manager.Blocks.Max(b => b.Body.position.y);
                var removed = manager.Blocks.Where(b => Mathf.Abs(b.Body.position.y - topY) < .01f)
                    .First(b => b.Colliders.Any(c => beforeTarget.x > c.bounds.min.x &&
                        beforeTarget.x < c.bounds.max.x));
                manager.TryBreak(removed); Step(manager, physics, 35);
                Check("Removing a key support causes physical fall", manager.Target.Body.position.y < beforeTarget.y - .05f);
                manager.Undo();
                Check("Undo restores every block and target", !removed.IsBroken &&
                    Vector2.Distance(manager.Target.Body.position, beforeTarget) < .0001f &&
                    manager.Blocks.Where((b, i) => Vector2.Distance(b.Body.position, before[i]) > .0001f).Count() == 0);
                manager.Restart(); Step(manager, physics, 180);
                Check("Restart restores all 18 pieces", manager.Blocks.Count == 18 && manager.Blocks.All(b => !b.IsBroken));
                manager.SetPaused(true); int count = manager.Moves;
                manager.TryBreak(manager.Blocks[0]);
                Check("Pause rejects world taps", manager.Moves == count);
                manager.SetPaused(false);
                manager.Target.Body.position = new Vector2(6, 0); Step(manager, physics, 2);
                Check("Out of bounds fails", manager.State == BreakDropGameState.Failed);
                manager.LoadLevel(6); Step(manager, physics, 180);
                manager.Target.Body.position = new Vector2(manager.Level.goal.x, manager.Level.goal.y - 1);
                manager.Target.Body.velocity = Vector2.zero;
                Physics2D.SyncTransforms();
                Check("Below platform cannot win", !manager.HasGoalSupport());
                manager.Restart(); Step(manager, physics, 30);
                float timerBeforePause = manager.TimeRemaining;
                manager.SetPaused(true); manager.TickPhysics(2f);
                Check("Pause freezes countdown", Mathf.Approximately(manager.TimeRemaining, timerBeforePause));
                manager.SetPaused(false); manager.TickPhysics(manager.TimeRemaining + .1f);
                Check("Countdown reaching zero fails the level", manager.State == BreakDropGameState.Failed &&
                    manager.TimeRemainingSeconds == 0 && manager.StatusMessage == "时间耗尽");
                manager.Restart();
                Check("Restart resets countdown",
                    Mathf.Approximately(manager.TimeRemaining, manager.Level.timeLimitSeconds));
                manager.ShowHome();
                Check("Home clears playable physics", manager.State == BreakDropGameState.Home && manager.Target == null);
            }
            catch (Exception e) { _report.failures.Add(e.ToString()); Debug.LogException(e); }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
                if (active.IsValid() && active.isLoaded) SceneManager.SetActiveScene(active);
                if (SceneManager.sceneCount > 1) EditorSceneManager.CloseScene(testScene, true);
                Physics2D.simulationMode = previous;
                Directory.CreateDirectory("docs/validation");
                File.WriteAllText("docs/validation/tower-physics.json", JsonUtility.ToJson(_report, true));
                Debug.Log("TOWER ACCEPTANCE: " + _report.checks.Count + " checks, " + _report.failures.Count + " failures");
                if (Application.isBatchMode) EditorApplication.Exit(_report.failures.Count == 0 ? 0 : 1);
            }
        }

        private static void Step(BreakDropGameManager manager, PhysicsScene2D physics, int ticks)
        {
            for (int i = 0; i < ticks; i++) { physics.Simulate(Dt); manager.TickPhysics(Dt); }
        }
        private static void Resolve(BreakDropGameManager manager, PhysicsScene2D physics)
        {
            for (int i = 0; i < 1200 && (manager.State == BreakDropGameState.Resolving || manager.State == BreakDropGameState.Ready); i++)
                Step(manager, physics, 1);
        }

        private static int CountTargetSupports(BreakDropGameManager manager)
        {
            var contacts = new ContactPoint2D[16];
            int count = manager.Target.Collider.GetContacts(contacts);
            var supports = new HashSet<BreakableBlock>();
            for (int i = 0; i < count; i++)
            {
                BreakableBlock first = contacts[i].collider == null ? null :
                    contacts[i].collider.GetComponentInParent<BreakableBlock>();
                BreakableBlock second = contacts[i].otherCollider == null ? null :
                    contacts[i].otherCollider.GetComponentInParent<BreakableBlock>();
                if (first != null && !first.IsBroken) supports.Add(first);
                if (second != null && !second.IsBroken) supports.Add(second);
            }
            return supports.Count;
        }
        private static void Check(string name, bool ok)
        {
            if (ok) _report.checks.Add(name); else _report.failures.Add(name);
        }

        // Bounded authoring aid: every candidate is evaluated by the real 2D solver.
        public static void FindSolution()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Physics2D.simulationMode = SimulationMode2D.Script;
            Physics2D.gravity = new Vector2(0, -14);
            Physics2D.velocityIterations = 12;
            Physics2D.positionIterations = 8;
            var host = new GameObject("Solution Search");
            var manager = host.AddComponent<BreakDropGameManager>();
            var physics = Physics2D.defaultPhysicsScene;
            var beam = new List<int[]> { Array.Empty<int>() };
            var seen = new HashSet<string>();
            int tested = 0;
            int[] solution = null;
            manager.LoadLevel(2);
            int[] blockIds = manager.Blocks.Select(b => b.Id).ToArray();
            for (int depth = 0; depth < blockIds.Length && solution == null; depth++)
            {
                var candidates = new List<Tuple<int[], float>>();
                foreach (int[] prefix in beam)
                {
                    foreach (int id in blockIds)
                    {
                        if (prefix.Contains(id)) continue;
                        int[] sequence = prefix.Concat(new[] { id }).ToArray();
                        if (!seen.Add(string.Join(",", sequence))) continue;
                        manager.LoadLevel(2); Step(manager, physics, 60);
                        foreach (int move in sequence)
                        {
                            if (manager.State != BreakDropGameState.Playing) break;
                            var block = manager.Blocks.First(b => b.Id == move);
                            if (block.IsBroken) break;
                            manager.TryBreak(block); Resolve(manager, physics);
                            Step(manager, physics, 35);
                        }
                        tested++;
                        if (manager.State == BreakDropGameState.Won) { solution = sequence; break; }
                        if (manager.State != BreakDropGameState.Playing || manager.Moves != sequence.Length) continue;
                        float score = manager.Target.Body.position.y +
                            Mathf.Abs(manager.Target.Body.position.x - manager.Level.goal.x) * 1.5f;
                        candidates.Add(Tuple.Create(sequence, score));
                    }
                    if (solution != null) break;
                }
                beam = candidates.OrderBy(c => c.Item2).Take(6).Select(c => c.Item1).ToList();
                Debug.Log("Search depth=" + depth + ", candidates=" + candidates.Count + ", tested=" + tested);
                if (beam.Count == 0) break;
            }
            Directory.CreateDirectory("docs/validation");
            File.WriteAllText("docs/validation/solution-search.txt",
                "tested=" + tested + "\nsolution=" + (solution == null ? "none" : string.Join(",", solution)));
            UnityEngine.Object.DestroyImmediate(host);
            EditorApplication.Exit(solution == null ? 1 : 0);
        }
        public static void ImportArt()
        {
            foreach (string path in Directory.GetFiles("Assets/BreakDrop/Resources/Art", "*.png"))
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(path.Replace('\\', '/'));
                if (importer == null) continue;
                importer.textureType = TextureImporterType.Default;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.filterMode = FilterMode.Bilinear;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.SaveAndReimport();
            }
        }

        public static void BuildPreview()
        {
            BuildPreview(BuildOptions.None);
        }

        public static void BuildCapturePreview()
        {
            BuildPreview(BuildOptions.Development);
        }

        private static void BuildPreview(BuildOptions options)
        {
            ImportArt();
            PlayerSettings.defaultScreenWidth = 390;
            PlayerSettings.defaultScreenHeight = 844;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { "Assets/BreakDrop/Scenes/Prototype.unity" },
                locationPathName = "Builds/Preview/BreakDrop.exe",
                target = BuildTarget.StandaloneWindows64,
                options = options
            });
            Debug.Log("PREVIEW BUILD: " + result.summary.result);
            EditorApplication.Exit(result.summary.totalErrors == 0 ? 0 : 1);
        }
    }
}
