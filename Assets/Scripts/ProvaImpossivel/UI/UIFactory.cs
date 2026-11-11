using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ProvaImpossivel
{
    /// <summary>
    /// Atalhos para montar UI (uGUI) por código. Duas famílias:
    /// - Box/Label/Button/Slider/Bar: estilo escuro de antes, com a fonte padrão do Unity. Usado pelo que fica no
    ///   mundo ou fora do Bloco 16 (ícones de humor, balões, painéis dos desafios A e B).
    /// - Paper/Ink/PaperButton/PaperSlider/Stamp/Keycap: componentes da "prova impressa" (Bloco 16).
    /// Todas as cores, fontes e sprites vêm do <see cref="UITheme"/>.
    /// </summary>
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

        /// <summary>Atalho comum: âncora e pivô no topo-centro do pai.</summary>
        public static RectTransform Top(this RectTransform r, float x, float y, float w, float h) =>
            r.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(x, y), new Vector2(w, h));

        public static RectTransform Stretch(this RectTransform r, float pad = 0f)
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = new Vector2(pad, pad);
            r.offsetMax = new Vector2(-pad, -pad);
            return r;
        }

        // =================================================================== estilo escuro (antigo)

        public static Image Box(string name, Transform parent, Color color, bool outline = true)
        {
            var r = Rect(name, parent);
            var img = r.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            if (outline)
            {
                var o = r.gameObject.AddComponent<Outline>();
                o.effectColor = UITheme.LegacyOutline;
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
            sh.effectColor = UITheme.LegacyShadow;
            sh.effectDistance = new Vector2(2f, -2f);
            return t;
        }

        public static Button Button(string name, Transform parent, string text, Color bg, UnityAction onClick, int fontSize = 34)
        {
            var img = Box(name, parent, bg);
            img.raycastTarget = true;
            var b = img.gameObject.AddComponent<Button>();
            var colors = b.colors;
            colors.highlightedColor = UITheme.LegacyHover;
            colors.pressedColor = UITheme.LegacyPressed;
            b.colors = colors;
            var l = Label("Text", img.transform, text, fontSize, Color.white);
            l.rectTransform.Stretch(6f);
            b.onClick.AddListener(onClick);
            return b;
        }

        /// <summary>Slider horizontal escuro. Funciona com o jogo pausado: a UI usa tempo não escalado.</summary>
        public static Slider Slider(string name, Transform parent, float min, float max, float value, Color fillColor, UnityAction<float> onChange)
        {
            var bg = Box(name, parent, UITheme.LegacyTrack);
            bg.raycastTarget = true;
            var fillArea = Rect("Area", bg.transform).Stretch(6f);
            var fill = Box("Fill", fillArea, fillColor, false);
            var handle = Box("Pegador", null, Color.white);
            return WireSlider(bg.gameObject, fillArea, fill, handle, new Vector2(24f, 12f), 12f, min, max, value, onChange);
        }

        static Slider WireSlider(GameObject root, RectTransform fillArea, Image fill, Image handle, Vector2 handleSize, float inset,
                                 float min, float max, float value, UnityAction<float> onChange)
        {
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = new Vector2(0f, 1f);
            fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;
            var handleArea = Rect("AreaPegador", root.transform).Stretch();
            handleArea.offsetMin = new Vector2(inset, 0f);
            handleArea.offsetMax = new Vector2(-inset, 0f);
            handle.transform.SetParent(handleArea, false);
            handle.raycastTarget = true;
            handle.rectTransform.anchorMin = Vector2.zero;
            handle.rectTransform.anchorMax = new Vector2(0f, 1f);
            handle.rectTransform.sizeDelta = handleSize;

            var s = root.AddComponent<Slider>();
            s.fillRect = fill.rectTransform;
            s.handleRect = handle.rectTransform;
            s.targetGraphic = handle;
            s.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            s.minValue = min;
            s.maxValue = max;
            s.SetValueWithoutNotify(value);
            s.onValueChanged.AddListener(onChange);
            return s;
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

        // =================================================================== prova impressa (Bloco 16)

        static Image Sliced(string name, Transform parent, Sprite sprite, Color color)
        {
            var r = Rect(name, parent);
            var img = r.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>
        /// Folha de papel: creme, borda fina, sombra embaixo. <paramref name="margin"/> desenha a margem vermelha;
        /// <paramref name="header"/> escreve o cabeçalho impresso da prova ("UNIMAR — avaliação ... Aluno ... Nota").
        /// </summary>
        public static Image Paper(string name, Transform parent, bool margin = true, bool header = false)
        {
            var img = Sliced(name, parent, UITheme.PaperSprite, UITheme.Paper);
            var sh = img.gameObject.AddComponent<Shadow>();
            sh.effectColor = UITheme.DropShadow;
            sh.effectDistance = new Vector2(7f, -7f);
            if (margin)
            {
                var m = Sliced("Margem", img.transform, UIFactory.WhiteSprite, UITheme.Margin);
                m.type = Image.Type.Simple;
                m.rectTransform.anchorMin = new Vector2(0f, 0f);
                m.rectTransform.anchorMax = new Vector2(0f, 1f);
                m.rectTransform.pivot = new Vector2(0f, 0.5f);
                m.rectTransform.offsetMin = new Vector2(UITheme.MarginX, 3f);
                m.rectTransform.offsetMax = new Vector2(UITheme.MarginX + 3f, -3f);
            }
            if (header)
            {
                var h = Ink("Cabecalho", img.transform, Falas.FolhaCabecalho, UITheme.SizeSmall, UITheme.InkFaint, TextAnchor.UpperLeft);
                h.rectTransform.Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(UITheme.MarginX + 22f, -16f), new Vector2(1100f, 30f));
                h.horizontalOverflow = HorizontalWrapMode.Overflow;
            }
            return img;
        }

        /// <summary>Linha fina (régua do papel ou sublinhado).</summary>
        public static Image Line(string name, Transform parent, Color color, float x, float y, float w, float h = 2f)
        {
            var l = Sliced(name, parent, WhiteSprite, color);
            l.type = Image.Type.Simple;
            l.rectTransform.Top(x, y, w, h);
            return l;
        }

        /// <summary>Texto no papel: fonte do tema, sem sombra e sem negrito sintético (é o que borrava).</summary>
        public static Text Ink(string name, Transform parent, string text, int size, Color color,
                               TextAnchor anchor = TextAnchor.MiddleCenter, bool hand = false)
        {
            var r = Rect(name, parent);
            var t = r.gameObject.AddComponent<Text>();
            t.font = hand ? UITheme.Hand : UITheme.Typewriter;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = anchor;
            t.fontStyle = FontStyle.Normal;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            t.supportRichText = true;
            t.lineSpacing = hand ? 0.9f : 1.05f;
            return t;
        }

        /// <summary>
        /// Botão de papel: folha mais escura com contorno a tinta. Hover clareia a folha, o texto vira caneta azul e
        /// (com <paramref name="checkbox"/>) aparece um X à mão no "( )"; clique encolhe. Tudo em 0,18 s, tempo real.
        /// </summary>
        public static Button PaperButton(string name, Transform parent, string text, UnityAction onClick,
                                         int fontSize = UITheme.SizeButton, bool checkbox = false)
        {
            var bg = Sliced(name, parent, UITheme.PaperSprite, Color.white);
            bg.raycastTarget = true;
            var frame = Sliced("Contorno", bg.transform, UITheme.FrameSprite, UITheme.Ink);
            frame.rectTransform.Stretch();
            var b = bg.gameObject.AddComponent<Button>();
            b.targetGraphic = bg;
            var c = b.colors;
            c.normalColor = UITheme.PaperDark;
            c.highlightedColor = UITheme.PaperHover;
            c.selectedColor = UITheme.PaperDark;  // depois do clique não fica "aceso"
            c.pressedColor = UITheme.PaperPressed;
            c.disabledColor = UITheme.PaperDisabled;
            c.colorMultiplier = 1f;
            c.fadeDuration = UITheme.Transition;
            b.colors = c;
            var nav = b.navigation; nav.mode = Navigation.Mode.None; b.navigation = nav; // teclado: atalhos do GameManager

            var label = Ink("Text", bg.transform, text, fontSize, UITheme.Ink);
            label.rectTransform.Stretch(8f);
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            Text mark = null;
            if (checkbox)
            {
                var box = Ink("Caixinha", bg.transform, "(    )", fontSize, UITheme.Ink, TextAnchor.MiddleLeft);
                box.rectTransform.Stretch(18f);
                mark = Ink("X", bg.transform, "X", fontSize + 18, UITheme.Pen, TextAnchor.MiddleLeft, hand: true);
                mark.rectTransform.Stretch(18f);
                mark.rectTransform.offsetMin = new Vector2(34f, 0f);
                label.alignment = TextAnchor.MiddleLeft;
                label.rectTransform.offsetMin = new Vector2(118f, 8f);
            }
            var feel = bg.gameObject.AddComponent<PaperButtonFeel>();
            feel.label = label;
            feel.mark = mark;
            feel.target = b;
            b.onClick.AddListener(onClick);
            return b;
        }

        /// <summary>Slider de papel: régua a tinta, preenchimento de caneta azul e pegador em forma de tecla.</summary>
        public static Slider PaperSlider(string name, Transform parent, float min, float max, float value, UnityAction<float> onChange)
        {
            var root = Sliced(name, parent, WhiteSprite, UITheme.Clear); // área de clique invisível
            root.raycastTarget = true;
            var track = Sliced("Regua", root.transform, WhiteSprite, UITheme.InkSoft);
            track.type = Image.Type.Simple;
            track.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            track.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            track.rectTransform.sizeDelta = new Vector2(-8f, 3f);
            var fillArea = Rect("Area", root.transform);
            fillArea.anchorMin = new Vector2(0f, 0.5f);
            fillArea.anchorMax = new Vector2(1f, 0.5f);
            fillArea.sizeDelta = new Vector2(-28f, 8f);
            var fill = Sliced("Fill", fillArea, WhiteSprite, UITheme.Pen);
            fill.type = Image.Type.Simple;
            var handle = Sliced("Pegador", null, UITheme.KeycapSprite, UITheme.PaperDark);
            var s = WireSlider(root.gameObject, fillArea, fill, handle, new Vector2(26f, 8f), 14f, min, max, value, onChange);
            var c = s.colors;
            c.normalColor = Color.white;
            c.highlightedColor = UITheme.TintHover;
            c.pressedColor = UITheme.TintPressed;
            c.selectedColor = Color.white;
            c.fadeDuration = UITheme.Transition;
            s.colors = c;
            var nav = s.navigation; nav.mode = Navigation.Mode.None; s.navigation = nav;
            return s;
        }

        /// <summary>
        /// Carimbo: texto datilografado girado dentro de moldura dupla, vermelho ou azul. O tamanho acompanha o texto.
        /// </summary>
        public static RectTransform Stamp(string name, Transform parent, string text, Color color, int size = UITheme.SizeStamp,
                                          float angle = UITheme.StampAngle)
        {
            var r = Rect(name, parent);
            var frame = Sliced("Moldura", r, UITheme.DoubleFrameSprite, color);
            frame.rectTransform.Stretch();
            // carimbo pequeno = traço mais fino (a moldura acompanha o tamanho da letra)
            frame.pixelsPerUnitMultiplier = Mathf.Max(1f, UITheme.SizeStamp * 0.8f / size);
            var t = Ink("Texto", r, text, size, color);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.rectTransform.Stretch();
            // a Special Elite tem "teto" alto: desce um pouco para o texto ficar no meio da moldura
            t.rectTransform.offsetMin = new Vector2(0f, -size * 0.1f);
            t.rectTransform.offsetMax = new Vector2(0f, -size * 0.1f);
            r.sizeDelta = new Vector2(t.preferredWidth + size * 0.9f, t.preferredHeight + size * 0.7f);
            r.localRotation = Quaternion.Euler(0f, 0f, angle);
            return r;
        }

        /// <summary>Etiqueta de tecla ("Q", "Esc"...): tecla de papel com a letra datilografada.</summary>
        public static Image Keycap(string name, Transform parent, string key, int size = UITheme.SizeSmall)
        {
            var img = Sliced(name, parent, UITheme.KeycapSprite, UITheme.PaperDark);
            var t = Ink("Letra", img.transform, key, size, UITheme.Ink);
            t.rectTransform.Stretch();
            t.rectTransform.offsetMin = new Vector2(2f, 3f); // a borda de baixo da tecla é mais grossa
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            img.rectTransform.sizeDelta = new Vector2(Mathf.Max(size * 1.45f, t.preferredWidth + 16f), size * 1.45f);
            return img;
        }

        /// <summary>Moldura de 1 traço (miolo transparente) na cor pedida.</summary>
        public static Image Frame(string name, Transform parent, Color color) => Sliced(name, parent, UITheme.FrameSprite, color);
    }
}
