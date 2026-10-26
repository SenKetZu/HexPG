using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VoidHex.UiKit
{
    /// <summary>
    /// Constructor de UI nativa (uGUI) con layout determinista.
    ///
    /// Se posiciona con anclas de esquina superior izquierda en vez de LayoutGroups: el resultado
    /// es predecible, no depende del orden de reconstrucción y evita el clásico "panel que no aparece"
    /// cuando un ContentSizeFitter pelea con un LayoutGroup.
    ///
    /// No conoce ningún dominio: recibe strings, números y callbacks.
    /// </summary>
    public static class Ui
    {
        public const float ReferenceWidth = 1920f;
        public const float ReferenceHeight = 1080f;

        // ------------------------------------------------------------------ canvas

        public static Canvas CreateCanvas(string name, int sortOrder = 100)
        {
            var existing = Object.FindAnyObjectByType<Canvas>();
            if (existing != null && existing.name == name) return existing;

            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortOrder;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            EnsureEventSystem(canvas);
            return canvas;
        }

        private static void EnsureEventSystem(Canvas canvas)
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null) return;

            var go = new GameObject("EventSystem", typeof(EventSystem));
            go.transform.SetParent(canvas.transform, false);

            // El proyecto usa el Input System nuevo (activeInputHandler = 1):
            // StandaloneInputModule quedaría inerte y ningún botón respondería.
            go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        // ------------------------------------------------------------------ rects

        /// <summary>Rect anclado arriba-izquierda: (x, y) desde la esquina superior izquierda del padre.</summary>
        public static RectTransform Rect(Transform parent, string name, float x, float y, float w, float h)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(w, h);
            return rect;
        }

        /// <summary>Rect que ocupa todo el padre (para fondos y contenedores de pantalla completa).</summary>
        public static RectTransform Stretch(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        // ------------------------------------------------------------------ superficies

        /// <summary>
        /// Panel con borde de 1 px y esquinas redondeadas.
        ///
        /// Devuelve el rect EXTERIOR: así <c>SetActive(false)</c> oculta el panel completo, incluido
        /// el borde. El relleno se crea como primer hijo, de modo que todo lo que se agregue después
        /// se dibuja por encima sin tocar el orden de la jerarquía.
        /// </summary>
        public static RectTransform Panel(
            Transform parent,
            string name,
            float x,
            float y,
            float w,
            float h,
            Color fill,
            int radius = 14,
            Color? border = null)
        {
            var outer = Rect(parent, name, x, y, w, h);
            var outline = outer.gameObject.AddComponent<Image>();
            outline.sprite = UiSprites.RoundedRect(radius);
            outline.type = Image.Type.Sliced;
            outline.color = border ?? UiTheme.Border;
            outline.raycastTarget = false;

            var inner = Rect(outer, name + "_fill", 1f, 1f, w - 2f, h - 2f);
            var background = inner.gameObject.AddComponent<Image>();
            background.sprite = UiSprites.RoundedRect(Mathf.Max(2, radius - 1));
            background.type = Image.Type.Sliced;
            background.color = fill;
            background.raycastTarget = false;

            return outer;
        }

        // ------------------------------------------------------------------ texto

        public static Text Label(
            Transform parent,
            string name,
            float x,
            float y,
            float w,
            float h,
            string text,
            int size,
            Color color,
            TextAnchor anchor = TextAnchor.UpperLeft,
            bool bold = false)
        {
            var rect = Rect(parent, name, x, y, w, h);
            var label = rect.gameObject.AddComponent<Text>();
            label.font = UiTheme.Font;
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = anchor;
            label.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.raycastTarget = false;
            label.supportRichText = true;
            return label;
        }

        /// <summary>Encabezado: kicker en versalitas + título grande + regla de acento.</summary>
        public static void Header(Transform parent, string kicker, string title, Color accent, float x = 0f, float y = 0f, float w = 400f)
        {
            Label(parent, "kicker", x, y, w, 16f, kicker.ToUpperInvariant(), UiTheme.KickerSize, accent, bold: true);
            Label(parent, "title", x, y + 18f, w, 38f, title, UiTheme.TitleSize, UiTheme.Text, bold: true);

            var rule = Rect(parent, "rule", x, y + 58f, 56f, 3f);
            var ruleImage = rule.gameObject.AddComponent<Image>();
            ruleImage.sprite = UiSprites.RoundedRect(2);
            ruleImage.type = Image.Type.Sliced;
            ruleImage.color = accent;
            ruleImage.raycastTarget = false;
        }

        /// <summary>Referencias tipadas a una cifra: evita buscar por ruta de nombre.</summary>
        public readonly struct StatView
        {
            public readonly Text Value;
            public readonly Text Caption;

            public StatView(Text value, Text caption)
            {
                Value = value;
                Caption = caption;
            }
        }

        /// <summary>Cifra destacada con etiqueta: la unidad visual de "evidencia" de estas pantallas.</summary>
        public static StatView Stat(
            Transform parent,
            string name,
            float x,
            float y,
            float w,
            string value,
            string caption,
            Color accent,
            float h = 78f)
        {
            var panel = Panel(parent, name, x, y, w, h, UiTheme.PanelAlt, 12, UiTheme.Border);
            var number = Label(panel, "value", 14f, 8f, w - 28f, 40f, value, UiTheme.BigNumberSize, accent, bold: true);
            var captionText = Label(panel, "caption", 14f, h - 32f, w - 28f, 18f, caption, UiTheme.SmallSize,
                UiTheme.Muted);
            return new StatView(number, captionText);
        }

        public static Text Row(Transform parent, string name, float x, float y, float w, string key, string value)
        {
            Label(parent, name + "_key", x, y, w * 0.55f, 22f, key, UiTheme.SmallSize, UiTheme.Muted);
            return Label(parent, name + "_value", x + w * 0.55f, y, w * 0.45f, 22f, value,
                UiTheme.SmallSize, UiTheme.Text, TextAnchor.UpperRight, bold: true);
        }

        // ------------------------------------------------------------------ controles

        public static Button Button(
            Transform parent,
            string name,
            float x,
            float y,
            float w,
            string title,
            Color accent,
            UnityAction onClick,
            float h = UiTheme.ButtonHeight,
            string subtitle = null)
        {
            var outer = Rect(parent, name, x, y, w, h);

            var frame = outer.gameObject.AddComponent<Image>();
            frame.sprite = UiSprites.RoundedRect(10);
            frame.type = Image.Type.Sliced;
            frame.color = accent.WithAlpha(0.55f);
            frame.raycastTarget = true;

            var fill = Rect(outer, "fill", 1f, 1f, w - 2f, h - 2f);
            var fillImage = fill.gameObject.AddComponent<Image>();
            fillImage.sprite = UiSprites.RoundedRect(9);
            fillImage.type = Image.Type.Sliced;
            fillImage.color = accent.Tint(0.72f);

            // Barra de acento a la izquierda: guía visual del tipo de acción.
            var strip = Rect(fill, "strip", 0f, 4f, 4f, Mathf.Max(8f, h - 10f));
            var stripImage = strip.gameObject.AddComponent<Image>();
            stripImage.sprite = UiSprites.RoundedRect(2);
            stripImage.type = Image.Type.Sliced;
            stripImage.color = accent;

            var hasSubtitle = !string.IsNullOrEmpty(subtitle);

            // El texto se reparte dentro de la altura disponible y la tipografía se ajusta a la caja.
            // Unity usa Truncate vertical: si la caja es más baja que la línea, el texto desaparece
            // por completo (así quedaban los botones del catálogo: barras sin ninguna etiqueta).
            var innerH = h - 2f;
            float labelTop;
            float labelH;
            float subtitleTop = 0f;
            var subtitleH = 0f;

            if (hasSubtitle)
            {
                var gap = Mathf.Max(1f, innerH * 0.04f);

                labelTop = Mathf.Max(1f, innerH * 0.08f);
                labelH = (innerH - gap) * 0.55f;
                subtitleTop = labelTop + labelH;
                subtitleH = Mathf.Max(8f, innerH - subtitleTop);
            }
            else
            {
                labelTop = 0f;
                labelH = innerH;
            }

            Label(fill, "label", 18f, labelTop, w - 40f, labelH,
                title, FitFont(UiTheme.ButtonSize, labelH), UiTheme.Text,
                hasSubtitle ? TextAnchor.UpperLeft : TextAnchor.MiddleLeft, bold: true);

            if (hasSubtitle)
            {
                Label(fill, "subtitle", 18f, subtitleTop, w - 40f, subtitleH, subtitle,
                    FitFont(UiTheme.SmallSize - 1, subtitleH), UiTheme.Muted.WithAlpha(0.95f),
                    TextAnchor.UpperLeft);
            }

            var button = outer.gameObject.AddComponent<Button>();
            button.targetGraphic = fillImage;
            button.transition = Selectable.Transition.ColorTint;

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.22f, 1.22f, 1.22f, 1f);
            colors.pressedColor = new Color(1.45f, 1.45f, 1.45f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.6f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            if (onClick != null) button.onClick.AddListener(onClick);
            return button;
        }

        /// <summary>
        /// Mayor tamaño de fuente que entra en una caja de texto sin que Unity la recorte.
        ///
        /// El Text de uGUI con <c>VerticalWrapMode.Truncate</c> no reduce la tipografía: descarta la
        /// línea que no entra. Por eso hay que calcularlo antes de crear la etiqueta.
        /// 1,35 es la altura de línea de la fuente LegacyRuntime respecto del tamaño de fuente.
        /// </summary>
        private static int FitFont(int preferred, float boxHeight)
        {
            var fitting = Mathf.FloorToInt(boxHeight / 1.35f);
            return Mathf.Clamp(Mathf.Min(preferred, fitting), 9, preferred);
        }

        /// <summary>Barra de progreso con etiqueta y valor numérico.</summary>
        public static Image Bar(
            Transform parent,
            string name,
            float x,
            float y,
            float w,
            string caption,
            float value,
            float max,
            Color accent,
            string valueText = null)
        {
            Label(parent, name + "_caption", x, y, w * 0.6f, 18f, caption, UiTheme.SmallSize, UiTheme.Muted);
            Label(parent, name + "_value", x + w * 0.6f, y, w * 0.4f, 18f,
                valueText ?? $"{value:0.##} / {max:0.##}", UiTheme.SmallSize, accent,
                TextAnchor.UpperRight, bold: true);

            var track = Rect(parent, name + "_track", x, y + 20f, w, 8f);
            var trackImage = track.gameObject.AddComponent<Image>();
            trackImage.sprite = UiSprites.RoundedRect(4);
            trackImage.type = Image.Type.Sliced;
            trackImage.color = UiTheme.PanelAlt;
            trackImage.raycastTarget = false;

            var ratio = max <= 0f ? 0f : Mathf.Clamp01(value / max);
            var fill = Rect(track, "fill", 0f, 0f, Mathf.Max(4f, w * ratio), 8f);
            var fillImage = fill.gameObject.AddComponent<Image>();
            fillImage.sprite = UiSprites.RoundedRect(4);
            fillImage.type = Image.Type.Sliced;
            fillImage.color = accent;
            fillImage.raycastTarget = false;

            return fillImage;
        }

        public static Image Dot(Transform parent, string name, float x, float y, float size, Color color)
        {
            var rect = Rect(parent, name, x, y, size, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = UiSprites.Circle(48);
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public static void Separator(Transform parent, float x, float y, float w, float alpha = 0.7f)
        {
            var rect = Rect(parent, "separator", x, y, w, 1f);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = UiTheme.Border.WithAlpha(alpha);
            image.raycastTarget = false;
        }
    }
}
