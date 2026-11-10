using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ProvaImpossivel
{
    /// <summary>Atalhos para montar UI (uGUI) por código, com a fonte padrão do Unity.</summary>
    public static class UIFactory
    {
        static Font font;
        public static Font Font => font != null ? font : (font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static RectTransform Place(this RectTransform r, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            r.anchorMin = r.anchorMax = anchor;
            r.pivot = pivot;
            r.anchoredPosition = pos;
            r.sizeDelta = size;
            return r;
        }

        public static RectTransform Stretch(this RectTransform r, float pad = 0f)
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = new Vector2(pad, pad);
            r.offsetMax = new Vector2(-pad, -pad);
            return r;
        }

        public static Image Box(string name, Transform parent, Color color, bool outline = true)
        {
            var r = Rect(name, parent);
            var img = r.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            if (outline)
            {
                var o = r.gameObject.AddComponent<Outline>();
                o.effectColor = new Color(0f, 0f, 0f, 0.85f);
                o.effectDistance = new Vector2(3f, -3f);
            }
            return img;
        }

        public static Text Label(string name, Transform parent, string text, int size, Color color,
                                 TextAnchor anchor = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Bold)
        {
            var r = Rect(name, parent);
            var t = r.gameObject.AddComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = anchor;
            t.fontStyle = style;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            t.supportRichText = true;
            var sh = r.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0f, 0f, 0f, 0.7f);
            sh.effectDistance = new Vector2(2f, -2f);
            return t;
        }

        public static Button Button(string name, Transform parent, string text, Color bg, UnityAction onClick, int fontSize = 34)
        {
            var img = Box(name, parent, bg);
            img.raycastTarget = true;
            var b = img.gameObject.AddComponent<Button>();
            var colors = b.colors;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            b.colors = colors;
            var l = Label("Text", img.transform, text, fontSize, Color.white);
            l.rectTransform.Stretch(6f);
            b.onClick.AddListener(onClick);
            return b;
        }

        /// <summary>Barra horizontal: retorna a imagem de preenchimento (use fillAmount).</summary>
        public static Image Bar(string name, Transform parent, Color back, Color fill)
        {
            var bg = Box(name, parent, back);
            var f = Box("Fill", bg.transform, fill, false);
            f.rectTransform.Stretch(4f);
            f.type = Image.Type.Filled;
            f.fillMethod = Image.FillMethod.Horizontal;
            f.sprite = WhiteSprite;
            f.fillAmount = 0f;
            return f;
        }

        static Sprite white;
        public static Sprite WhiteSprite
        {
            get
            {
                if (white != null) return white;
                var tex = Texture2D.whiteTexture;
                white = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                return white;
            }
        }
    }
}
