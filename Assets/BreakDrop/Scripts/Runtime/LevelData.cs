using System;
using UnityEngine;

namespace BreakDrop
{
    [Serializable]
    public sealed class LevelDefinition
    {
        public int schemaVersion = 2;
        public MechanismDefinition[] mechanisms = Array.Empty<MechanismDefinition>();
        public int levelId = 1;
        public int bestMoves = 1;
        public float timeLimitSeconds = 60f;
        public bool towerLevel;
        public VectorData target = new VectorData();
        public VectorData goal = new VectorData();
        public float goalWidth = 3f;
        public float failY = -6f;
        public float failX = 5.5f;
        public float[] starHeights = { 1.5f, -0.5f, -2.5f };
        public int[] hintOrder = Array.Empty<int>();
        public BlockDefinition[] blocks = Array.Empty<BlockDefinition>();
    }

    [Serializable]
    public sealed class MechanismDefinition
    {
        public string type = "hinge";
        public int blockId;
        public float anchorX, anchorY;
        public float lowerAngle = -35, upperAngle = 35;
    }

    [Serializable]
    public sealed class VectorData
    {
        public float x;
        public float y;

        public Vector2 Value => new Vector2(x, y);
    }

    [Serializable]
    public sealed class BlockDefinition
    {
        public int id;
        public float x;
        public float y;
        public float width = 1f;
        public float height = 1f;
        public float rotation;
        public string shape = "Rectangle";
        public string art = "BlockB";
        public float mass = 1f;
        public bool breakable = true;
        public bool fixedBlock;
        public string color = "#FFFFFF";
    }
}
