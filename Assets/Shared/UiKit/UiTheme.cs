using UnityEngine;

namespace VoidHex.UiKit
{
    /// <summary>
    /// Paleta y tipografía compartidas por las tres pantallas.
    /// Es puramente visual: no conoce ningún dominio, ningún contrato y ningún prototipo.
    /// </summary>
    public static class UiTheme
    {
        // Superficies
        public static readonly Color Backdrop = new Color32(0x0A, 0x10, 0x1C, 0xFF);
        public static readonly Color Panel = new Color32(0x13, 0x1C, 0x2E, 0xFF);
        public static readonly Color PanelAlt = new Color32(0x1B, 0x27, 0x40, 0xFF);
        public static readonly Color Border = new Color32(0x2C, 0x3C, 0x58, 0xFF);

        // Texto
        public static readonly Color Text = new Color32(0xF2, 0xF6, 0xFC, 0xFF);
        public static readonly Color Muted = new Color32(0x8F, 0xA3, 0xBC, 0xFF);
        public static readonly Color Dim = new Color32(0x5A, 0x6B, 0x83, 0xFF);

        // Acentos por prototipo (coinciden con la presentación)
        public static readonly Color Teal = new Color32(0x2D, 0xD4, 0xBF, 0xFF);
        public static readonly Color Violet = new Color32(0xA7, 0x8B, 0xFA, 0xFF);
        public static readonly Color Amber = new Color32(0xFB, 0xBF, 0x24, 0xFF);
        public static readonly Color Success = new Color32(0x4A, 0xDE, 0x80, 0xFF);
        public static readonly Color Danger = new Color32(0xF8, 0x71, 0x71, 0xFF);

        public const int TitleSize = 26;
        public const int KickerSize = 12;
        public const int BodySize = 15;
        public const int SmallSize = 13;
        public const int BigNumberSize = 34;
        public const int ButtonSize = 15;
        public const int ButtonHeight = 44;

        private static Font _font;

        /// <summary>
        /// Fuente incorporada del editor: no requiere importar ningún asset, así el proyecto
        /// se ve bien al clonarlo. Se prueban los dos nombres conocidos según versión de Unity.
        /// </summary>
        public static Font Font
        {
            get
            {
                if (_font != null) return _font;

                _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (_font == null) _font = Resources.GetBuiltinResource<Font>("Arial.ttf");

                return _font;
            }
        }

        public static Color WithAlpha(this Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        /// <summary>Versión oscurecida para fondos de botón.</summary>
        public static Color Tint(this Color color, float amount)
        {
            return new Color(
                Mathf.Lerp(color.r, 0f, amount),
                Mathf.Lerp(color.g, 0f, amount),
                Mathf.Lerp(color.b, 0f, amount),
                color.a);
        }
    }
}
