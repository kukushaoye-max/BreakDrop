using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace BreakDrop
{
    // Factories extend the schema without teaching the editor about component implementations.
    public static class MechanismRegistry
    {
        private static readonly Dictionary<string, Action<MechanismDefinition, GameObject>> factories =
            new Dictionary<string, Action<MechanismDefinition, GameObject>> { { "hinge", AddHinge } };
        public static bool Supports(string type) => type != null && factories.ContainsKey(type);
        public static void Register(string type, Action<MechanismDefinition, GameObject> factory)
        {
            if (string.IsNullOrEmpty(type) || factory == null || factories.ContainsKey(type))
                throw new ArgumentException("Duplicate or invalid mechanism factory");
            factories.Add(type, factory);
        }
        public static void Validate(LevelDefinition level)
        {
            if (level.schemaVersion > 2) throw new ArgumentException("Unsupported level format");
            var ids = new HashSet<int>();
            foreach (var m in level.mechanisms ?? Array.Empty<MechanismDefinition>())
            {
                var b = level.blocks.FirstOrDefault(x => x.id == m.blockId);
                if (!Supports(m.type) || !ids.Add(m.blockId) || b == null || b.breakable || b.fixedBlock)
                    throw new ArgumentException("Mechanism must reference a unique, non-removable dynamic block");
                if (m.type == "hinge" && (b.shape != "Rectangle" || m.lowerAngle > 0 || m.upperAngle < 0 || m.lowerAngle >= m.upperAngle || m.lowerAngle < -90 || m.upperAngle > 90 ||
                    float.IsNaN(m.lowerAngle) || float.IsNaN(m.upperAngle) || float.IsNaN(m.anchorX) || float.IsNaN(m.anchorY) || Mathf.Abs(m.anchorX) > b.width / 2 || Mathf.Abs(m.anchorY) > b.height / 2))
                    throw new ArgumentException("Invalid hinge anchor or angle limits");
            }
        }
        public static void Attach(MechanismDefinition data, GameObject block) => factories[data.type](data, block);
        private static void AddHinge(MechanismDefinition data, GameObject block)
        {
            var hinge = block.AddComponent<HingeJoint2D>();
            hinge.autoConfigureConnectedAnchor = false;
            hinge.anchor = new Vector2(data.anchorX, data.anchorY);
            hinge.connectedAnchor = block.transform.TransformPoint(hinge.anchor);
            hinge.useMotor = false; hinge.useLimits = true;
            hinge.limits = new JointAngleLimits2D { min = data.lowerAngle, max = data.upperAngle };
            CandyArt.Place(block.transform, "Fixed Pivot", CandyArt.Shape("Rectangle", .18f, .18f, Color.white),
                hinge.anchor, new Vector2(.18f, .18f), 900);
        }
    }
    // Future moving platforms/springs implement this contract; neither is registered in v1.
    public interface ILevelMechanismState
    {
        void Tick(float dt);
        string CaptureState();
        void RestoreState(string state);
        bool IsStable { get; }
    }
}
