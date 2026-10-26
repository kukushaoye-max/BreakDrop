using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BreakDrop.Editor
{
    public static class ViewportValidation
    {
        [MenuItem("Tools/Break & Drop/Validate Viewport Fit")]
        public static void Run()
        {
            Check("short window with device safe area", new Rect(0, 0, 390, 844), 390, 760);
            Check("design size", new Rect(0, 0, 390, 844), 390, 844);
            Check("tall phone with safe insets", new Rect(0, 34, 430, 864), 430, 932);
            Check("landscape", new Rect(0, 0, 844, 390), 844, 390);
            Check("missing safe area", new Rect(), 390, 760);
            Check("pixel ratio three", new Rect(0, 0, 1170, 2532), 1170, 2280);
            foreach (var size in new[] { new Vector2(360,640), new Vector2(390,844), new Vector2(430,932), new Vector2(1080,2400) })
            {
                Rect safe = MobileViewport.ResolveSafeArea(size.x, size.y, 0, 24, size.x, size.y - 34, 64);
                Rect view = DesignArt.FitViewport(safe, size.x, size.y);
                if (view.yMax > size.y - 72 + .01f || view.yMin < 34 - .01f)
                    throw new Exception("Capsule or home indicator overlap: " + size);
                Check("capsule and bottom gestures " + size, safe, size.x, size.y);
            }
            Rect missing = MobileViewport.ResolveSafeArea(390,844,0,0,0,0,0);
            if (missing != new Rect(0,0,390,844)) throw new Exception("Missing host safe area fallback");
            Directory.CreateDirectory("docs/validation/wechat-viewport");
            File.WriteAllText("docs/validation/wechat-viewport/unity-checks.txt",
                "PASS: eleven viewport cases, canvas bounds, design aspect and pointer mapping. Unity " + Application.unityVersion);
            Debug.Log("[BreakDrop Viewport] Eleven viewport regression cases passed.");
        }

        private static void Check(string name, Rect safe, float width, float height)
        {
            Rect view = DesignArt.FitViewport(safe, width, height);
            if (view.width <= 0 || view.height <= 0 || view.xMin < -.01f || view.yMin < -.01f ||
                view.xMax > width + .01f || view.yMax > height + .01f ||
                Mathf.Abs(view.width / view.height - 390f / 844f) > .0001f)
                throw new Exception("Viewport does not fit: " + name + " " + view);
            // The bottom selection button must remain on-canvas and map back to its design Y.
            float buttonY = 790f;
            float pixelY = view.yMax - buttonY * view.width / 390f;
            float restoredY = (view.yMax - pixelY) * 844f / view.height;
            if (pixelY < 0 || pixelY > height || Mathf.Abs(restoredY - buttonY) > .01f)
                throw new Exception("Viewport pointer mapping failed: " + name);
        }
    }
}

