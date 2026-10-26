using System.Collections.Generic;
using UnityEngine;
namespace BreakDrop
{
    // Source rectangles are measured on the approved artwork; UI geometry stays data-driven.
    public static class CandySkin
    {
        private static readonly Dictionary<string, Texture2D> Images = new Dictionary<string, Texture2D>();
        public static Texture2D Image(string name)
        {
            if (!Images.TryGetValue(name, out var t) || t == null) Images[name] = t = Resources.Load<Texture2D>("Candy/Approved/" + name);
            return t;
        }
        public static Rect R(float x, float y, float w, float h, float sw = 904, float sh = 1740) =>
            new Rect(x * 390 / sw, y * 844 / sh, w * 390 / sw, h * 844 / sh);
        public static Sprite Sprite(string key)
        {
            var t = Image("WorldPieces");
            Rect p = key == "Hex" ? new Rect(49, 750, 480, 401) : new Rect(576, 850, 651, 303);
            return UnityEngine.Sprite.Create(t, new Rect(p.x, t.height - p.yMax, p.width, p.height),
                Vector2.one * .5f, 96, 0, SpriteMeshType.FullRect);
        }
    }
}

