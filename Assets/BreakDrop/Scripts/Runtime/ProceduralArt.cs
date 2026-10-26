using UnityEngine;

namespace BreakDrop
{
    public static class ProceduralArt
    {
        private static Sprite _square;
        private static Sprite _hex;

        public static Sprite Square
        {
            get
            {
                if (_square == null)
                {
                    Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
                    {
                        name = "BreakDrop_WhitePixel",
                        filterMode = FilterMode.Bilinear,
                        wrapMode = TextureWrapMode.Clamp,
                        hideFlags = HideFlags.HideAndDontSave
                    };
                    texture.SetPixel(0, 0, Color.white);
                    texture.Apply();
                    _square = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), Vector2.one * 0.5f, 1f);
                    _square.name = "BreakDrop_Square";
                    _square.hideFlags = HideFlags.HideAndDontSave;
                }

                return _square;
            }
        }

        public static Sprite Hex
        {
            get
            {
                if (_hex == null)
                {
                    const int size = 128;
                    Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
                    {
                        name = "BreakDrop_Hex",
                        filterMode = FilterMode.Bilinear,
                        wrapMode = TextureWrapMode.Clamp,
                        hideFlags = HideFlags.HideAndDontSave
                    };

                    Color clear = new Color(0f, 0f, 0f, 0f);
                    Color fill = Color.white;
                    float radius = size * 0.46f;
                    Vector2 center = Vector2.one * (size - 1) * 0.5f;

                    for (int y = 0; y < size; y++)
                    {
                        for (int x = 0; x < size; x++)
                        {
                            Vector2 point = new Vector2(x, y) - center;
                            float ax = Mathf.Abs(point.x);
                            float ay = Mathf.Abs(point.y);
                            bool inside = ax <= radius * 0.8660254f && ay <= radius - ax * 0.5773503f;
                            texture.SetPixel(x, y, inside ? fill : clear);
                        }
                    }

                    texture.Apply();
                    _hex = Sprite.Create(texture, new Rect(0f, 0f, size, size), Vector2.one * 0.5f, size);
                    _hex.name = "BreakDrop_Hex";
                    _hex.hideFlags = HideFlags.HideAndDontSave;
                }

                return _hex;
            }
        }

        public static Color ParseColor(string html, Color fallback)
        {
            return ColorUtility.TryParseHtmlString(html, out Color color) ? color : fallback;
        }
    }
}

