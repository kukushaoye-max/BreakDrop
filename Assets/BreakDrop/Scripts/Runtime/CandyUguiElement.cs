using UnityEngine;
namespace BreakDrop
{
    // Stable identity survives prefab serialization and display-name edits.
    [DisallowMultipleComponent]
    public sealed class CandyUguiElement : MonoBehaviour
    {
        [SerializeField] private string key;
        [SerializeField] private bool page;
        public string Key => key;
        public bool IsPage => page;
        public void Configure(string value, bool isPage = false) { key = value; page = isPage; }
    }
}
