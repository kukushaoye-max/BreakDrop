using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BreakDrop.Editor
{
    internal static class GeneratedLevelPhysicsValidator
    {
        private const float Step = 1f / 60f;
        private const int MaxResolveSteps = 1200;

        internal static bool Validate(LevelDefinition level, int repetitions, out string reason)
        {
            SimulationMode2D previousMode = Physics2D.simulationMode;
            Rigidbody2D[] existingBodies = Resources.FindObjectsOfTypeAll<Rigidbody2D>()
                .Where(body => body.gameObject.scene.IsValid()).ToArray();
            bool[] previousSimulation = existingBodies.Select(body => body.simulated).ToArray();
            Physics2D.simulationMode = SimulationMode2D.Script;
            for (int i = 0; i < existingBodies.Length; i++) existingBodies[i].simulated = false;
            try
            {
                for (int run = 0; run < repetitions; run++)
                {
                    Vector2 finalPosition;
                    float finalRotation;
                    if (!RunOnce(level, out finalPosition, out finalRotation, out reason)) return false;
                }
                reason = null;
                return true;
            }
            finally
            {
                for (int i = 0; i < existingBodies.Length; i++)
                    if (existingBodies[i] != null) existingBodies[i].simulated = previousSimulation[i];
                Physics2D.simulationMode = previousMode;
            }
        }

        private static bool RunOnce(LevelDefinition level, out Vector2 finalPosition,
            out float finalRotation, out string reason)
        {
            Scene previousScene = SceneManager.GetActiveScene();
            bool reuseEmptyUntitled = previousScene.IsValid() && string.IsNullOrEmpty(previousScene.path) &&
                previousScene.rootCount == 0;
            if (previousScene.IsValid() && string.IsNullOrEmpty(previousScene.path) &&
                previousScene.rootCount > 0)
                throw new InvalidOperationException("Save the current scene before generating validated levels.");
            Scene scene = reuseEmptyUntitled ? previousScene :
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            string previousName = scene.name;
            scene.name = "BreakDropValidation_" + Guid.NewGuid().ToString("N");
            PhysicsScene2D physics = Physics2D.defaultPhysicsScene;
            var material = new PhysicsMaterial2D("Validation friction") { friction = .7f, bounciness = 0 };
            var blocks = new Dictionary<int, SimBlock>();
            Rigidbody2D targetBody = null;
            Collider2D targetCollider = null;
            Collider2D goalCollider = null;
            try
            {
                var goal = NewObject(scene, "Goal", level.goal.Value);
                goalCollider = AddBox(goal, new Vector2(level.goalWidth, .8f), Vector2.zero, material);

                foreach (BlockDefinition data in level.blocks)
                {
                    GameObject go = NewObject(scene, "Block_" + data.id, new Vector2(data.x, data.y));
                    go.transform.rotation = Quaternion.Euler(0, 0, data.rotation);
                    var body = go.AddComponent<Rigidbody2D>();
                    body.mass = Mathf.Max(.2f, data.mass);
                    body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                    var colliders = new List<Collider2D>();
                    if (data.shape == "L3" || data.shape == "L2")
                    {
                        colliders.Add(AddBox(go, new Vector2(data.width, 1),
                            new Vector2(0, (data.height - 1) / 2), material));
                        colliders.Add(AddBox(go, new Vector2(1, data.height - 1),
                            new Vector2((1 - data.width) / 2, -.5f), material));
                    }
                    else colliders.Add(AddBox(go, new Vector2(data.width, data.height), Vector2.zero, material));
                    blocks.Add(data.id, new SimBlock(body, colliders.ToArray()));
                }

                GameObject target = NewObject(scene, "Target", level.target.Value);
                var polygon = target.AddComponent<PolygonCollider2D>();
                float radius = 42f / DesignArt.PixelsPerUnit;
                polygon.points = Enumerable.Range(0, 6).Select(i => new Vector2(
                    Mathf.Cos(i * Mathf.PI / 3) * radius,
                    Mathf.Sin(i * Mathf.PI / 3) * radius)).ToArray();
                polygon.sharedMaterial = material;
                targetCollider = polygon;
                targetBody = target.AddComponent<Rigidbody2D>();
                targetBody.mass = 1.2f;
                targetBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

                if (!Resolve(physics, targetBody, blocks.Values, level, out reason))
                {
                    finalPosition = targetBody.position;
                    finalRotation = targetBody.rotation;
                    return false;
                }

                foreach (int blockId in level.hintOrder)
                {
                    SimBlock block;
                    if (!blocks.TryGetValue(blockId, out block) || !block.body.simulated)
                    {
                        reason = "hintOrder contains a missing or repeated block id";
                        finalPosition = targetBody.position;
                        finalRotation = targetBody.rotation;
                        return false;
                    }
                    block.body.simulated = false;
                    foreach (SimBlock remaining in blocks.Values)
                        if (remaining.body.simulated) remaining.body.WakeUp();
                    targetBody.WakeUp();
                    if (!Resolve(physics, targetBody, blocks.Values, level, out reason))
                    {
                        finalPosition = targetBody.position;
                        finalRotation = targetBody.rotation;
                        return false;
                    }
                }

                for (int i = 0; i < 120; i++) physics.Simulate(Step);
                finalPosition = targetBody.position;
                finalRotation = targetBody.rotation;
                if (!targetCollider.IsTouching(goalCollider))
                {
                    reason = "authored hint route did not place target on goal";
                    return false;
                }
                if (!IsStable(targetBody))
                {
                    reason = "target did not settle on goal";
                    return false;
                }
                reason = null;
                return true;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(material);
                if (reuseEmptyUntitled)
                {
                    foreach (GameObject root in scene.GetRootGameObjects())
                        UnityEngine.Object.DestroyImmediate(root);
                    scene.name = previousName;
                }
                else if (scene.IsValid()) EditorSceneManager.CloseScene(scene, true);
                if (previousScene.IsValid() && previousScene.isLoaded) SceneManager.SetActiveScene(previousScene);
            }
        }

        private static bool Resolve(PhysicsScene2D physics, Rigidbody2D target,
            IEnumerable<SimBlock> blocks, LevelDefinition level, out string reason)
        {
            float stableTime = 0;
            for (int i = 0; i < MaxResolveSteps; i++)
            {
                physics.Simulate(Step);
                if (target.position.y < level.failY || Mathf.Abs(target.position.x) > level.failX)
                {
                    reason = "target left the safe bounds during the hint route";
                    return false;
                }
                bool stable = IsStable(target) && blocks.Where(b => b.body.simulated).All(b => IsStable(b.body));
                stableTime = stable ? stableTime + Step : 0;
                if (stableTime >= .25f)
                {
                    target.Sleep();
                    foreach (SimBlock block in blocks)
                        if (block.body.simulated) block.body.Sleep();
                    reason = null;
                    return true;
                }
            }
            reason = "physics did not settle within the validation window";
            return false;
        }

        private static bool IsStable(Rigidbody2D body)
        {
            return body.IsSleeping() || (body.velocity.sqrMagnitude < .0025f &&
                Mathf.Abs(body.angularVelocity) < .5f);
        }

        private static GameObject NewObject(Scene scene, string name, Vector2 position)
        {
            var go = new GameObject(name);
            SceneManager.MoveGameObjectToScene(go, scene);
            go.transform.position = position;
            return go;
        }

        private static BoxCollider2D AddBox(GameObject go, Vector2 size, Vector2 offset,
            PhysicsMaterial2D material)
        {
            var collider = go.AddComponent<BoxCollider2D>();
            collider.size = size - Vector2.one * .015f;
            collider.offset = offset;
            collider.sharedMaterial = material;
            return collider;
        }

        private sealed class SimBlock
        {
            public readonly Rigidbody2D body;
            public readonly Collider2D[] colliders;
            public SimBlock(Rigidbody2D body, Collider2D[] colliders)
            {
                this.body = body;
                this.colliders = colliders;
            }
        }
    }
}
