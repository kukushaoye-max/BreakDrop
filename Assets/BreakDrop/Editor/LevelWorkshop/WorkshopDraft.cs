using System;
using UnityEngine;
namespace BreakDrop.Editor
{
    [Serializable] public sealed class WorkshopRange
    {
        public int blockId;
        public string property = "mass";
        public float min = 1, max = 1, step = .25f;
    }
    [Serializable] public sealed class WorkshopDraft : ScriptableObject
    {
        public string title = "新关卡", theme = "自定义", status = "草稿", report = "", validatedHash = "";
        public int seed, ordinal;
        public string replayJson = "", difficulty = "未评估";
        public bool selectedForRelease, template;
        public int[] lockedBlocks = Array.Empty<int>();
        public float positionVariation = .25f;
        public WorkshopRange[] ranges = Array.Empty<WorkshopRange>();
        public string generatorVersion = "workshop-v1";
        public LevelDefinition level = new LevelDefinition { towerLevel = true };
    }
}
