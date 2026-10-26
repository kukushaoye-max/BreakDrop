using System.Collections.Generic;
using UnityEngine;

namespace BreakDrop
{
    // Assets are exact Figma exports. Layout uses the approved 390 x 844 design.
    public static class DesignArt
    {
        public const float PixelsPerUnit = 52f;
        public static readonly Color Background = Hex("#101329");
        public static readonly Color Surface = Hex("#1C2140");
        public static readonly Color Text = Hex("#F5F9FF");
        public static readonly Color Muted = Hex("#9AA9C9");
        public static readonly Color Cyan = Hex("#57E6EB");
        public static readonly Color Gold = Hex("#FBE284");
        private static readonly Dictionary<string, Texture2D> Textures = new Dictionary<string, Texture2D>();
        private static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();

        public static Color Hex(string value) => ProceduralArt.ParseColor(value, Color.white);
        public static Texture2D Texture(string name)
        {
            if (!Textures.TryGetValue(name, out Texture2D texture))
            {
                texture = Resources.Load<Texture2D>("Art/" + name);
                if (texture == null) Debug.LogError("Missing Figma asset: " + name);
                Textures[name] = texture;
            }
            return texture;
        }

        public static Sprite Sprite(string name)
        {
            if (!Sprites.TryGetValue(name, out Sprite sprite))
            {
                Texture2D texture = Texture(name);
                if (texture == null) return ProceduralArt.Square;
                sprite = UnityEngine.Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                    Vector2.one * .5f, texture.width, 0, SpriteMeshType.FullRect);
                Sprites[name] = sprite;
            }
            return sprite;
        }

        public static SpriteRenderer Place(Transform parent, string name, string asset, Vector2 center, Vector2 size, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = Sprite(asset);
            Vector2 bounds = renderer.sprite.bounds.size;
            go.transform.localScale = new Vector3(size.x / bounds.x, size.y / bounds.y, 1);
            renderer.sortingOrder = order;
            return renderer;
        }

        public static Vector2 FromDesign(float x, float y) =>
            new Vector2((x - 195f) / PixelsPerUnit, (422f - y) / PixelsPerUnit);

        public static Rect Viewport => FitViewport(MobileViewport.SafeArea, Screen.width, Screen.height);

        public static Rect FitViewport(Rect safe, float width, float height)
        {
            // Browser hosts can report a device safe area larger than the game canvas.
            // Keep the same viewport for UI, camera and pointer conversion.
            width = Mathf.Max(1, width);
            height = Mathf.Max(1, height);
            float left = Mathf.Clamp(safe.xMin, 0, width);
            float bottom = Mathf.Clamp(safe.yMin, 0, height);
            float right = Mathf.Clamp(safe.xMax, 0, width);
            float top = Mathf.Clamp(safe.yMax, 0, height);
            safe = right > left && top > bottom
                ? Rect.MinMaxRect(left, bottom, right, top)
                : new Rect(0, 0, width, height);
            float scale = Mathf.Min(safe.width / 390f, safe.height / 844f);
            return new Rect(safe.center.x - 195f * scale, safe.center.y - 422f * scale,
                390f * scale, 844f * scale);
        }
    }
}

