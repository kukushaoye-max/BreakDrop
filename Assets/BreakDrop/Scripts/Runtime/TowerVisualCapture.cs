#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace BreakDrop
{
    // Opt-in development capture. Normal play never adds this component.
    public sealed class TowerVisualCapture : MonoBehaviour
    {
        public string DirectoryPath;
        private IEnumerator Start()
        {
            Directory.CreateDirectory(DirectoryPath);
            var manager = GetComponent<BreakDropGameManager>();
            manager.InputLocked = true;
            yield return new WaitForSecondsRealtime(1);
            yield return Capture("home.png");
            manager.LoadLevel(manager.LevelCount);
            yield return new WaitForSecondsRealtime(3);
            yield return Capture("gameplay.png");
            var before = manager.Target.Body.position;
            manager.InputLocked = false;
            manager.TryBreak(manager.Blocks.Last());
            manager.InputLocked = true;
            yield return new WaitForSecondsRealtime(.25f);
            yield return Capture("gameplay-falling.png");
            yield return new WaitForSecondsRealtime(1);
            manager.Undo();
            yield return new WaitForSecondsRealtime(1);
            yield return Capture("gameplay-undo.png");
            File.WriteAllText(Path.Combine(DirectoryPath, "visual-capture.txt"),
                "Captured Home, Gameplay, falling and Undo. State=" + manager.State +
                "; target restored delta=" + Vector2.Distance(before, manager.Target.Body.position));
            Application.Quit();
        }
        private IEnumerator Capture(string filename)
        {
            yield return new WaitForEndOfFrame();
            var texture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
            texture.Apply();
            File.WriteAllBytes(Path.Combine(DirectoryPath, filename), texture.EncodeToPNG());
            Destroy(texture);
        }
    }
}
#endif
