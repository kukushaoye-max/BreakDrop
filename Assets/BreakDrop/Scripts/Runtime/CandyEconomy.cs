using System;
using UnityEngine;

namespace BreakDrop
{
    public enum CandyItem { Hint, Undo, Time }

    // Local prototype store. Production rewarded ads must use an authoritative server receipt.
    [Serializable]
    public sealed class CandySave
    {
        public int version = 1, energy = 10;
        public int[] inventory = { 3, 3, 3 };
        public long recoveryAt, lastSeen;
        public string dailyClaim = "";
        public bool music = true, effects = true, vibration;
    }

    public sealed class CandyEconomy
    {
        public const int Capacity = 10, RecoverySeconds = 600;
        private const string Key = "breakdrop.candy.save.v1";
        private readonly Func<long> _clock;
        private readonly Action<string> _write;
        public CandySave Data { get; private set; }
        private readonly int[] _used = new int[3];
        public static readonly string[] Names = { "灵感糖", "回退糖", "时间糖" };
        public static readonly int[] Limits = { 3, 3, 2 };
        public int Energy { get { Refresh(); return Data.energy; } }
        public int SecondsToRecovery { get { Refresh(); return Data.energy >= Capacity ? 0 :
            (int)Math.Max(0, Data.recoveryAt - Now); } }
        public long Now => Math.Max(_clock(), Data.lastSeen);
        public string Today => DateTimeOffset.FromUnixTimeSeconds(Now).ToOffset(TimeSpan.FromHours(8)).ToString("yyyy-MM-dd");
        public bool CanClaimDaily => Data.dailyClaim != Today;
        public int Count(CandyItem item) => Data.inventory[(int)item];
        public int Remaining(CandyItem item) => Limits[(int)item] - _used[(int)item];

        public CandyEconomy(string json, Func<long> clock, Action<string> write)
        {
            _clock = clock; _write = write;
            try { Data = string.IsNullOrEmpty(json) ? new CandySave() : JsonUtility.FromJson<CandySave>(json); }
            catch (ArgumentException) { Data = new CandySave(); }
            if (Data == null) Data = new CandySave();
            if (Data.inventory == null || Data.inventory.Length != 3) Data.inventory = new[] { 0, 0, 0 };
            Data.energy = Mathf.Clamp(Data.energy, 0, Capacity);
            for (int i = 0; i < 3; i++) Data.inventory[i] = Math.Max(0, Data.inventory[i]);
            if (Data.energy < Capacity && Data.recoveryAt <= 0) Data.recoveryAt = Now + RecoverySeconds;
            Refresh(); Save();
        }

        public static CandyEconomy Load()
        {
#if UNITY_EDITOR
            if (SupportSampleSession.Active) return SupportSampleSession.Economy;
#endif
            return new CandyEconomy(PlayerPrefs.GetString(Key, ""),
            () => DateTimeOffset.UtcNow.ToUnixTimeSeconds(), json => { PlayerPrefs.SetString(Key, json); PlayerPrefs.Save(); });
        }

        public void Refresh()
        {
            long now = Now;
            if (Data.energy >= Capacity) { Data.recoveryAt = 0; return; }
            if (Data.recoveryAt <= 0) Data.recoveryAt = now + RecoverySeconds;
            if (now < Data.recoveryAt) return;
            long earned = 1 + (now - Data.recoveryAt) / RecoverySeconds;
            Data.energy = (int)Math.Min(Capacity, Data.energy + earned);
            Data.recoveryAt = Data.energy == Capacity ? 0 : Data.recoveryAt + earned * RecoverySeconds;
            Save();
        }

        public bool SpendEnergy()
        {
            Refresh();
            if (Data.energy <= 0) return false;
            if (Data.energy == Capacity) Data.recoveryAt = Now + RecoverySeconds;
            Data.energy--; Save(); return true;
        }

        public void BeginRun() { Array.Clear(_used, 0, _used.Length); }
        public bool Consume(CandyItem item)
        {
            int i = (int)item;
            if (Data.inventory[i] <= 0 || _used[i] >= Limits[i]) return false;
            Data.inventory[i]--; _used[i]++; Save(); return true;
        }

        public bool ClaimDaily()
        {
            Refresh();
            if (!CanClaimDaily) return false;
            Data.dailyClaim = Today;
            for (int i = 0; i < 3; i++) Data.inventory[i]++;
            Save(); return true;
        }

        public void Save() { Data.lastSeen = Now; _write?.Invoke(JsonUtility.ToJson(Data)); }
    }
}

