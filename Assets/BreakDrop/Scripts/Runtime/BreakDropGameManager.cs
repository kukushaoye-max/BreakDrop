using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BreakDrop
{
    public enum BreakDropGameState { Loading, Home, Ready, Playing, Resolving, Won, Failed }

    public sealed class BreakDropGameManager : MonoBehaviour
    {
        private const float StableSpeed = .10f;
        private const float StableRotation = 4f;
        private readonly List<BreakableBlock> _blocks = new List<BreakableBlock>();
        private readonly ContactPoint2D[] _contacts = new ContactPoint2D[16];
        private LevelDefinition[] _catalog;
        private Transform _levelRoot;
        private TargetPiece _target;
        private Collider2D _goalCollider;
        private LevelDefinition _level;
        private UndoSnapshot _undo;
        private ILevelMechanismState[] _mechanismStates = Array.Empty<ILevelMechanismState>();
        private string[] _initialMechanismStates = Array.Empty<string>();
        private PhysicsMaterial2D _material;
        private float _stableTime, _goalTime, _resolveTime;
        public BreakDropGameState State { get; private set; } = BreakDropGameState.Home;
        public int CurrentLevel { get; private set; } = 1;
        public int Moves { get; private set; }
        public int Stars { get; private set; }
        public float TimeRemaining { get; private set; }
        public int TimeRemainingSeconds => Mathf.CeilToInt(TimeRemaining);
        public bool Paused { get; private set; }
        public bool InputLocked { get; set; }
        public bool CanUndo => _undo != null && TimeRemaining > 0 &&
            State != BreakDropGameState.Loading && State != BreakDropGameState.Home && State != BreakDropGameState.Won;
        public bool CanHint => !Paused && State == BreakDropGameState.Playing && _level != null &&
            _level.hintOrder.Any(id => _blocks.Any(b => b.Id == id && !b.IsBroken && b.CanRemove));
        public string StatusMessage { get; private set; } = "";
        public TargetPiece Target => _target;
        public IReadOnlyList<BreakableBlock> Blocks => _blocks;
        public LevelDefinition Level => _level;
        public int LevelCount { get { EnsureCatalog(); return _catalog.Length; } }

        private void EnsureCatalog()
        {
            if (_catalog != null) return;
#if UNITY_EDITOR
            if (SupportSampleSession.Active) { SetEditorCatalog(SupportSampleSession.Catalog()); return; }
#endif
            _catalog = Resources.LoadAll<TextAsset>("Levels").Select(a => JsonUtility.FromJson<LevelDefinition>(a.text))
                .Where(l => l != null && l.towerLevel).OrderBy(l => l.levelId).ToArray();
            if (_catalog.Length == 0) throw new InvalidOperationException("No tower levels found.");
        }

        public void LoadSavedLevel()
        {
            EnsureCatalog();
            LoadLevel(Mathf.Clamp(PlayerPrefs.GetInt("breakdrop.tower.level", 1), 1, LevelCount));
        }

        public void ShowHome()
        {
            SetPaused(false);
            ClearLevel();
            _undo = null;
            State = BreakDropGameState.Home;
            StatusMessage = "";
            ConfigureCamera();
        }

        public void LoadLevel(int number)
        {
            EnsureCatalog();
            SetPaused(false);
            number = Mathf.Clamp(number, 1, _catalog.Length);
            if (_levelRoot != null && CurrentLevel == number)
            {
                ResetCurrentTower();
                return;
            }
            State = BreakDropGameState.Loading;
            ClearLevel();
            CurrentLevel = Mathf.Clamp(number, 1, _catalog.Length);
            _level = _catalog[CurrentLevel - 1];
            _undo = null;
            _stableTime = _goalTime = _resolveTime = 0;
            Moves = Stars = 0;
            TimeRemaining = Mathf.Max(1f, _level.timeLimitSeconds);
            StatusMessage = "";
            _levelRoot = new GameObject("TowerLevel_" + CurrentLevel).transform;
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(_levelRoot.gameObject,gameObject.scene);
            ConfigureCamera();
            if (_material == null) _material = new PhysicsMaterial2D("Tower friction") { friction = .7f, bounciness = 0 };
            CreateGoal();
            MechanismRegistry.Validate(_level);
            foreach (BlockDefinition data in _level.blocks) CreateBlock(data);
            foreach (var mechanism in _level.mechanisms ?? Array.Empty<MechanismDefinition>())
                MechanismRegistry.Attach(mechanism, _blocks.First(b => b.Id == mechanism.blockId).gameObject);
            _mechanismStates = _levelRoot.GetComponentsInChildren<MonoBehaviour>().OfType<ILevelMechanismState>().ToArray();
            _initialMechanismStates = _mechanismStates.Select(m => m.CaptureState()).ToArray();
            CreateTarget();
            Physics2D.SyncTransforms();
            foreach (BreakableBlock block in _blocks) block.Body.Sleep();
            _target.Body.Sleep();
            State = BreakDropGameState.Ready;
        }

        private void ResetCurrentTower()
        {
            // Retain native body identities on restart; disable simulation to discard stale contacts.
            State = BreakDropGameState.Loading;
            foreach (BreakableBlock b in _blocks) { b.ClearHint(); b.Body.simulated = false; }
            _target.Body.simulated = false;
            for (int i = 0; i < _blocks.Count; i++)
            {
                var b = _blocks[i];
                var d = _level.blocks[i];
                b.transform.SetPositionAndRotation(new Vector3(d.x, d.y, 0), Quaternion.Euler(0, 0, d.rotation));
                b.Body.position = new Vector2(d.x, d.y);
                b.Body.rotation = d.rotation;
                b.Body.velocity = Vector2.zero;
                b.Body.angularVelocity = 0;
                b.SetBroken(false);
            }
            for (int i = 0; i < _mechanismStates.Length; i++) _mechanismStates[i].RestoreState(_initialMechanismStates[i]);
            _target.transform.SetPositionAndRotation(_level.target.Value, Quaternion.identity);
            _target.Body.position = _level.target.Value;
            _target.Body.rotation = 0;
            _target.Body.velocity = Vector2.zero;
            _target.Body.angularVelocity = 0;
            _target.Body.simulated = true;
            Physics2D.SyncTransforms();
            foreach (BreakableBlock b in _blocks) b.Body.Sleep();
            _target.Body.Sleep();
            foreach (BreakParticle particle in _levelRoot.GetComponentsInChildren<BreakParticle>())
                particle.gameObject.SetActive(false);
            _undo = null;
            Moves = Stars = 0;
            TimeRemaining = Mathf.Max(1f, _level.timeLimitSeconds);
            _stableTime = _goalTime = _resolveTime = 0;
            StatusMessage = "";
            State = BreakDropGameState.Ready;
        }

        private void ClearLevel()
        {
            if (_levelRoot != null)
            {
                _levelRoot.gameObject.SetActive(false);
                if (Application.isPlaying) Destroy(_levelRoot.gameObject);
                else DestroyImmediate(_levelRoot.gameObject);
            }
            // Destroy is deferred until end of frame. Never reuse a cleared tower in that frame.
            _levelRoot = null;
            _blocks.Clear();
            _target = null;
            _goalCollider = null;
        }

        private void Update()
        {
            ConfigureCamera();
        }
        private void FixedUpdate() => TickPhysics(Time.fixedDeltaTime);

        // The same evaluator is used by live play and the editor's deterministic physics checks.
        public void TickPhysics(float dt)
        {
            if (Paused || _target == null || State == BreakDropGameState.Home ||
                State == BreakDropGameState.Won || State == BreakDropGameState.Failed) return;
            foreach (var mechanism in _mechanismStates) mechanism.Tick(dt);
            if (State == BreakDropGameState.Playing || State == BreakDropGameState.Resolving)
            {
                TimeRemaining = Mathf.Max(0f, TimeRemaining - dt);
                if (TimeRemaining <= 0f) { Fail("时间耗尽"); return; }
            }
            if (IsOutOfBounds(_target.Body.position)) { Fail("六边形离开安全区域"); return; }
            foreach (BreakableBlock block in _blocks)
                if (!block.IsBroken && block.Body.bodyType == RigidbodyType2D.Dynamic && IsOutOfBounds(block.Body.position))
                    block.SetBroken(true);
            Stars = Mathf.Clamp(_level.starHeights.Count(y => _target.Body.position.y <= y), 0, 3);
            bool stable = IsWorldStable();
            _stableTime = stable ? _stableTime + dt : 0;
            _goalTime = stable && HasGoalSupport() ? _goalTime + dt : 0;
            if (_goalTime >= .5f) { Win(); return; }
            if (State == BreakDropGameState.Ready || State == BreakDropGameState.Resolving)
            {
                _resolveTime += dt;
                if (_stableTime >= .25f)
                {
                    if (!_blocks.Exists(b => !b.IsBroken && b.CanRemove) && !HasGoalSupport())
                        Fail("积木已耗尽，尚未安全落地");
                    else
                    {
                        foreach (BreakableBlock b in _blocks)
                            if (!b.IsBroken && b.Body.bodyType == RigidbodyType2D.Dynamic) b.Body.Sleep();
                        _target.Body.Sleep();
                        State = BreakDropGameState.Playing;
                        StatusMessage = "";
                    }
                }
                else if (_resolveTime > 8) StatusMessage = "等待积木稳定，可使用回退糖或重开";
            }
        }

        public void TryBreak(BreakableBlock block)
        {
            if (InputLocked || Paused || State != BreakDropGameState.Playing || block == null ||
                block.IsBroken || !block.CanRemove || !_blocks.Contains(block)) return;
            foreach (BreakableBlock other in _blocks) other.ClearHint();
            _undo = Capture();
            block.SetBroken(true, true);
            // Removing support must wake the connected pile, including sleeping rigid bodies.
            foreach (BreakableBlock other in _blocks)
                if (!other.IsBroken && other.Body.bodyType == RigidbodyType2D.Dynamic) other.Body.WakeUp();
            _target.Body.WakeUp();
            Moves++;
            _stableTime = _goalTime = _resolveTime = 0;
            StatusMessage = "观察积木碰撞与下落…";
            GetComponent<CandyFeedback>()?.Break();
            State = BreakDropGameState.Resolving;
        }

        public void Restart() => LoadLevel(CurrentLevel);
        public void AddTime(float seconds)
        {
            if (TimeRemaining > 0 && (State == BreakDropGameState.Playing || State == BreakDropGameState.Resolving))
                TimeRemaining += Mathf.Max(0, seconds);
        }
        public void NextLevel() => LoadLevel(CurrentLevel % LevelCount + 1);
        public void SetPaused(bool paused)
        {
            Paused = paused;
#if UNITY_EDITOR
            if (!WorkshopSimulation) Time.timeScale = paused ? 0 : 1;
#else
            Time.timeScale = paused ? 0 : 1;
#endif
        }

        public void Undo()
        {
            if (!CanUndo) return;
            var snapshot = _undo;
            for (int i = 0; i < _mechanismStates.Length; i++) _mechanismStates[i].RestoreState(snapshot.mechanisms[i]);
            _undo = null;
            Moves = snapshot.moves;
            snapshot.target.Restore(_target.Body);
            for (int i = 0; i < _blocks.Count; i++)
            {
                _blocks[i].SetBroken(snapshot.broken[i]);
                snapshot.bodies[i].Restore(_blocks[i].Body);
            }
            foreach (BreakParticle particle in _levelRoot.GetComponentsInChildren<BreakParticle>())
                particle.gameObject.SetActive(false);
            Physics2D.SyncTransforms();
            _stableTime = _goalTime = _resolveTime = 0;
            State = BreakDropGameState.Ready;
            StatusMessage = "已恢复上一步，时间不返还";
        }

        public void Hint()
        {
            if (Paused || State != BreakDropGameState.Playing) return;
            foreach (BreakableBlock b in _blocks) b.ClearHint();
            foreach (int id in _level.hintOrder)
            {
                BreakableBlock b = _blocks.Find(candidate => candidate.Id == id && !candidate.IsBroken && candidate.CanRemove);
                if (b == null) continue;
                b.ShowHint();
                StatusMessage = "试试正在闪烁的积木";
                return;
            }
            StatusMessage = "观察剩余支撑，或回退后再试";
        }

        public bool HasGoalSupport()
        {
            if (_target == null || _goalCollider == null || !_target.Collider.IsTouching(_goalCollider)) return false;
            Bounds target = _target.Collider.bounds, goal = _goalCollider.bounds;
            // A hexagon can rest safely with its upper corners beyond the platform edge.
            // Require its center of mass above the platform and a real top contact, not full AABB containment.
            float centerX = _target.Body.worldCenterOfMass.x;
            if (centerX < goal.min.x || centerX > goal.max.x ||
                target.min.y < goal.max.y - .06f || target.min.y > goal.max.y + .06f) return false;
            int count = _target.Body.GetContacts(_contacts);
            for (int i = 0; i < count; i++)
                if ((_contacts[i].collider == _goalCollider || _contacts[i].otherCollider == _goalCollider)
                    && Mathf.Abs(_contacts[i].normal.y) > .65f
                    && _contacts[i].point.x >= goal.min.x - .02f && _contacts[i].point.x <= goal.max.x + .02f
                    && Mathf.Abs(_contacts[i].point.y - goal.max.y) <= .06f) return true;
            return false;
        }

        public bool IsWorldStable()
        {
            if (_target == null || !Stable(_target.Body)) return false;
            return _mechanismStates.All(m => m.IsStable) && _blocks.All(b => b.IsBroken || b.Body.bodyType == RigidbodyType2D.Static || Stable(b.Body));
        }
        private static bool Stable(Rigidbody2D body) => body.velocity.sqrMagnitude < StableSpeed * StableSpeed &&
            Mathf.Abs(body.angularVelocity) < StableRotation;
        private bool IsOutOfBounds(Vector2 p) => p.y < _level.failY || Mathf.Abs(p.x) > _level.failX;
        private void Win()
        {
            State = BreakDropGameState.Won;
            Stars = 3;
            StatusMessage = "安全落地！";
            if (!Application.isPlaying) return;
#if UNITY_EDITOR
            if (IsEditorSample) return;
#endif
            PlayerPrefs.SetInt("breakdrop.tower.level", CurrentLevel % LevelCount + 1);
            int best = PlayerPrefs.GetInt("breakdrop.tower.best." + _level.levelId, int.MaxValue);
            PlayerPrefs.SetInt("breakdrop.tower.best." + _level.levelId, Mathf.Min(best, Moves));
            PlayerPrefs.Save();
        }
        private void Fail(string message) { State = BreakDropGameState.Failed; StatusMessage = message; }
#if UNITY_EDITOR
        public bool WorkshopSimulation { get; set; }
        public bool IsEditorSample { get; private set; }
        public void SetEditorCatalog(LevelDefinition[] levels)
        {
            if (levels == null || levels.Length == 0 || levels.Any(l => l == null))
                throw new ArgumentException("A nonempty sample catalog is required.");
            ShowHome();
            _catalog = levels;
            IsEditorSample = true;
        }
        public void PreviewResultForAudit(bool won)
        {
            // Visual-only fixture: do not call Win or persist artificial progress to PlayerPrefs.
            State = won ? BreakDropGameState.Won : BreakDropGameState.Failed;
            Stars = won ? 3 : 1;
            Moves = 7;
            StatusMessage = won ? "安全落地！" : "六边形离开安全区域";
            SetPaused(true);
        }
#endif

        private void ConfigureCamera()
        {
#if UNITY_EDITOR
            if (WorkshopSimulation) return;
#endif
            Camera camera = Camera.main;
            if (camera == null || camera.gameObject.scene != gameObject.scene) return;
            camera.orthographic = true;
            camera.orthographicSize = 844f / DesignArt.PixelsPerUnit / 2;
            float centerY = 0;
            if (_level != null && State != BreakDropGameState.Home)
            {
                // Reserve the same HUD/toolbar bands as the approved artwork, without moving bodies.
                float top = _level.target.y + .85f, floor = _level.goal.y + .4f;
                float span = Mathf.Max(8.1f, top - floor);
                float viewHeight = Mathf.Max(14.8f, span * 844f / 414f);
                camera.orthographicSize = viewHeight / 2;
                centerY = floor + (584f / 844f - .5f) * viewHeight;
            }
            camera.transform.position = new Vector3(0, centerY, -10);
            camera.backgroundColor = DesignArt.Hex("#FEDBB6");
            camera.clearFlags = CameraClearFlags.SolidColor;
            Rect v = DesignArt.Viewport;
            camera.rect = new Rect(v.x / Screen.width, v.y / Screen.height, v.width / Screen.width, v.height / Screen.height);
            camera.aspect = 390f / 844f;
        }

        private void CreateGoal()
        {
            var go = new GameObject("Goal Platform");
            go.transform.SetParent(_levelRoot);
            go.transform.position = _level.goal.Value;
            _goalCollider = go.AddComponent<BoxCollider2D>();
            ((BoxCollider2D)_goalCollider).size = new Vector2(_level.goalWidth, .8f);
            _goalCollider.sharedMaterial = _material;
            go.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
            CandyArt.Place(go.transform, "Cake Platform", CandySkin.Sprite("Cake"),
                new Vector2(0, -.44f), new Vector2(_level.goalWidth + .7f, 1.68f), 2);
        }

        private void CreateBlock(BlockDefinition data)
        {
            var go = new GameObject("Block_" + data.id);
            go.transform.SetParent(_levelRoot);
            go.transform.position = new Vector2(data.x, data.y);
            go.transform.rotation = Quaternion.Euler(0, 0, data.rotation);
            var body = go.AddComponent<Rigidbody2D>();
            body.bodyType = data.fixedBlock ? RigidbodyType2D.Static : RigidbodyType2D.Dynamic;
            body.mass = Mathf.Max(.2f, data.mass);
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.sleepMode = RigidbodySleepMode2D.StartAwake;
            if (data.shape == "L3" || data.shape == "L2")
            {
                // Two non-overlapping boxes on one rigid body preserve the open concavity.
                AddBox(go, new Vector2(data.width, 1), new Vector2(0, (data.height - 1) / 2));
                AddBox(go, new Vector2(1, data.height - 1), new Vector2((1 - data.width) / 2, -.5f));
            }
            else AddBox(go, new Vector2(data.width, data.height), Vector2.zero);
            // Texture and collider share the same occupied silhouette; no opaque L notch.
            Color candy = CandyColor(data.art);
            var art = CandyArt.Place(go.transform, "Visual", CandyArt.Shape(data.shape, data.width, data.height, candy), Vector2.zero,
                new Vector2(data.width, data.height), 100 + data.id);
            var block = go.AddComponent<BreakableBlock>();
            block.Configure(this, data, art);
            _blocks.Add(block);
        }
        private void AddBox(GameObject go, Vector2 size, Vector2 offset)
        {
            var collider = go.AddComponent<BoxCollider2D>();
            // Small contact clearance prevents the initial grid from explosively separating.
            collider.size = size - Vector2.one * .015f;
            collider.offset = offset;
            collider.sharedMaterial = _material;
        }
        private void CreateTarget()
        {
            var go = new GameObject("Hex Target");
            go.transform.SetParent(_levelRoot);
            go.transform.position = _level.target.Value;
            var collider = go.AddComponent<PolygonCollider2D>();
            // Flat-topped shell, matching the approved rotated hex; glow has no collider.
            float r = 42f / DesignArt.PixelsPerUnit;
            collider.points = Enumerable.Range(0, 6).Select(i => new Vector2(
                Mathf.Cos(i * Mathf.PI / 3) * r, Mathf.Sin(i * Mathf.PI / 3) * r)).ToArray();
            collider.sharedMaterial = _material;
            var body = go.AddComponent<Rigidbody2D>();
            body.mass = 1.2f;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            CandyArt.Place(go.transform, "Hex Art", CandySkin.Sprite("Hex"),
                Vector2.zero, new Vector2(r * 2, r * 1.73205f), 1000);
            _target = go.AddComponent<TargetPiece>();
        }

        private static Color CandyColor(string art)
        {
            switch (art)
            {
                case "BlockA": case "BlockE": return DesignArt.Hex("#FFB22F");
                case "BlockB": case "BlockK": return DesignArt.Hex("#F34394");
                case "BlockC": case "BlockG": return DesignArt.Hex("#32CDEA");
                case "BlockD": case "BlockI": return DesignArt.Hex("#B371F1");
                case "BlockJ": case "BlockL": return DesignArt.Hex("#88D93E");
                default: return DesignArt.Hex("#F78C31");
            }
        }
        private UndoSnapshot Capture() => new UndoSnapshot
        {
            moves = Moves, target = BodySnapshot.Capture(_target.Body),
            bodies = _blocks.Select(b => BodySnapshot.Capture(b.Body)).ToArray(),
            mechanisms = _mechanismStates.Select(m => m.CaptureState()).ToArray(),
            broken = _blocks.Select(b => b.IsBroken).ToArray()
        };
        private sealed class UndoSnapshot
        {
            public int moves;
            public BodySnapshot target;
            public BodySnapshot[] bodies;
            public bool[] broken;
            public string[] mechanisms;
        }
        private struct BodySnapshot
        {
            public Vector2 position, velocity;
            public float rotation, angularVelocity;
            public bool simulated, sleeping;
            public static BodySnapshot Capture(Rigidbody2D b) => new BodySnapshot {
                position = b.position, velocity = b.velocity, rotation = b.rotation,
                angularVelocity = b.angularVelocity, simulated = b.simulated, sleeping = b.IsSleeping()
            };
            public void Restore(Rigidbody2D b)
            {
                b.simulated = simulated;
                b.transform.SetPositionAndRotation(position, Quaternion.Euler(0, 0, rotation));
                b.position = position; b.rotation = rotation;
                b.velocity = velocity; b.angularVelocity = angularVelocity;
                if (sleeping) b.Sleep(); else b.WakeUp();
            }
        }
        private void OnDestroy()
        {
            ClearLevel();
            if (_material != null) DestroyImmediate(_material);
#if UNITY_EDITOR
            if (!WorkshopSimulation) Time.timeScale = 1;
#else
            Time.timeScale = 1;
#endif
        }
    }
}
