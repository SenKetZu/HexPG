using System.Collections.Generic;
using UnityEngine;

namespace VoidHex.UiKit
{
    /// <summary>
    /// Sprites generados en runtime: rectángulos redondeados 9-slice y círculos.
    ///
    /// Por qué generarlos en vez de usar assets: el repositorio queda reproducible sin archivos
    /// binarios ni dependencias de importación, y el resultado se ve igual en cualquier máquina.
    /// </summary>
    public static class UiSprites
    {
        private static readonly Dictionary<int, Sprite> Rounded = new Dictionary<int, Sprite>();
        private static readonly Dictionary<int, Sprite> Circles = new Dictionary<int, Sprite>();

        /// <summary>Rectángulo redondeado con borde 9-slice: se estira sin deformar las esquinas.</summary>
        public static Sprite RoundedRect(int radius = 12)
        {
            if (Rounded.TryGetValue(radius, out var cached) && cached != null) return cached;

            var size = radius * 2 + 8;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = $"UiRounded{radius}",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };

            var pixels = new Color32[size * size];
            var center = (size - 1) * 0.5f;
            var half = center;
            var inner = half - radius;

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = Mathf.Abs(x - center);
                    var dy = Mathf.Abs(y - center);

                    // Distancia al rectángulo redondeado (SDF aproximada).
                    var qx = Mathf.Max(dx - inner, 0f);
                    var qy = Mathf.Max(dy - inner, 0f);
                    var distance = Mathf.Sqrt(qx * qx + qy * qy) - radius;

                    var alpha = Mathf.Clamp01(0.5f - distance);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);

            var border = new Vector4(radius + 1, radius + 1, radius + 1, radius + 1);
            var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f),
                100f, 0, SpriteMeshType.FullRect, border);
            sprite.name = $"UiRounded{radius}";
            sprite.hideFlags = HideFlags.HideAndDontSave;

            Rounded[radius] = sprite;
            return sprite;
        }

        /// <summary>Círculo suave para indicadores de PA y viñetas.</summary>
        public static Sprite Circle(int size = 48)
        {
            if (Circles.TryGetValue(size, out var cached) && cached != null) return cached;

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = $"UiCircle{size}",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };

            var pixels = new Color32[size * size];
            var center = (size - 1) * 0.5f;
            var radius = center - 1f;

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = x - center;
                    var dy = y - center;
                    var distance = Mathf.Sqrt(dx * dx + dy * dy) - radius;
                    var alpha = Mathf.Clamp01(0.5f - distance);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);

            var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            sprite.name = $"UiCircle{size}";
            sprite.hideFlags = HideFlags.HideAndDontSave;

            Circles[size] = sprite;
            return sprite;
        }
    }
}
