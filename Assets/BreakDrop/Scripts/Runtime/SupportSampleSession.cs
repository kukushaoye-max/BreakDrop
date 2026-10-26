#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;

namespace BreakDrop
{
    // Editor-only, in-memory play session. No production PlayerPrefs keys are written.
    public static class SupportSampleSession
    {
        public const string SessionKey = "BreakDrop.SupportSamples.Active";
        public const string DirectoryPath = "Assets/BreakDrop/Editor/LevelSamples";
        private static CandyEconomy economy;
        public static bool Active => SessionState.GetBool(SessionKey, false);
        public static int Selected => SessionState.GetInt(SessionKey + ".Selected", 1);
        public static LevelDefinition[] Catalog() => System.IO.Directory.GetFiles(DirectoryPath, "sample_*.json")
            .OrderBy(p => p, StringComparer.Ordinal).Select(p => UnityEngine.JsonUtility.FromJson<LevelDefinition>(File.ReadAllText(p))).ToArray();
        public static CandyEconomy Economy => economy ?? (economy = new CandyEconomy("", () => DateTimeOffset.UtcNow.ToUnixTimeSeconds(), _ => {}));
        public static void Clear() { economy = null; SessionState.SetBool(SessionKey, false); }
    }
}
#endif
