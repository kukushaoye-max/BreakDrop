using System.Collections.Generic;
using UnityEngine;

namespace BreakDrop
{
    // Runtime candy primitives use the authored physics silhouette, including transparent L notches.
    public static class CandyArt
    {
        public static readonly Color Chocolate = DesignArt.Hex("#582614");
        public static readonly Color Cream = DesignArt.Hex("#FFF0D5");
        public static readonly Color Pink = DesignArt.Hex("#F62F86");
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
        public static Texture2D Texture(string name) => Resources.Load<Texture2D>("Candy/" + name);
        public static Sprite Shape(string shape, float width, float height, Color color)
        {
            string key = shape + width + ":" + height + ":" + color;
            if (Cache.TryGetValue(key, out Sprite found) && found != null && found.texture != null) return found;
            int w = Mathf.RoundToInt(width * 96), h = Mathf.RoundToInt(height * 96);
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { name = "Candy " + shape, filterMode = FilterMode.Bilinear };
            var pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float px = (x + .5f) / 96f, py = (y + .5f) / 96f;
                float d;
                if (shape == "Hex")
                {
                    float dx = Mathf.Abs(px - width / 2), dy = Mathf.Abs(py - height / 2);
                    d = Mathf.Min(height / 2 - dy, (width / 2 - dx) * .8660254f - dy * .5f);
                }
                else if (shape == "L3" || shape == "L2")
                    d = Mathf.Max(RoundDistance(px, py, 0, height - 1, width, 1),
                        RoundDistance(px, py, 0, 0, 1, height));
                else d = RoundDistance(px, py, 0, 0, width, height);
                if (d < 0) { pixels[y * w + x] = Color.clear; continue; }
                // Sample the actual candy surface; warp the L arms independently so the notch
                // remains exactly one grid-unit thick, matching its compound colliders.
                var atlas = CandySkin.Image("WorldPieces");
                float sx, sy;
                if (shape == "L3" || shape == "L2")
                {
                    float top = height - py;
                    sx = 789 + (px < 1 ? px * 194 : 194 + (px - 1) / (width - 1) * 220);
                    sy = 145 + (top < 1 ? top * 194 : 194 + (top - 1) / (height - 1) * 209);
                }
                else { sx = 33 + px / width * 701; sy = 322 + (1 - py / height) * 194; }
                Color sample = atlas.GetPixelBilinear(sx / atlas.width, 1 - sy / atlas.height);
                Color.RGBToHSV(color, out float hue, out float sat, out float val);
                Color.RGBToHSV(sample, out _, out float sampleSat, out float sampleVal);
                Color c = Color.HSVToRGB(hue, Mathf.Clamp01(sampleSat * 1.15f), sampleVal);
                c.a = Mathf.Clamp01(d * 96) * sample.a;
                pixels[y * w + x] = c;
            }
            t.SetPixels(pixels); t.Apply();
            found = Sprite.Create(t, new Rect(0, 0, w, h), Vector2.one * .5f, 96);
            Cache[key] = found; return found;
        }
        private static float RoundDistance(float x, float y, float left, float bottom, float w, float h)
        {
            float r = .09f;
            Vector2 q = new Vector2(Mathf.Abs(x - left - w / 2) - w / 2 + r,
                Mathf.Abs(y - bottom - h / 2) - h / 2 + r);
            return r - (new Vector2(Mathf.Max(q.x, 0), Mathf.Max(q.y, 0)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0));
        }
        public static SpriteRenderer Place(Transform parent, string name, Sprite sprite, Vector2 center, Vector2 size, int order)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = center;
            var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = sprite; sr.sortingOrder = order;
            go.transform.localScale = new Vector3(size.x / sprite.bounds.size.x, size.y / sprite.bounds.size.y, 1);
            return sr;
        }
        public static void Background(Transform parent)
        {
            var t = CandySkin.Image("GameLive");
            if (t == null) return;
            Sprite s = Sprite.Create(t, new Rect(0, 0, t.width, t.height), Vector2.one * .5f, 100);
            var bg = Place(parent, "Candy Landscape", s, Vector2.zero, new Vector2(390, 844) / DesignArt.PixelsPerUnit, -100);
            bg.gameObject.AddComponent<CandyBackdrop>();
        }
    }
}
