using UnityEngine;

namespace ProvaImpossivel
{
    /// <summary>
    /// Tema único da interface (Bloco 16, direção A: "prova impressa"). Folha creme, margem vermelha, texto
    /// datilografado, caneta azul e carimbos. Cores, fontes, tamanhos, espaçamentos, bordas e sprites gerados por
    /// código ficam SÓ aqui: UIFactory e HUD não têm cor solta.
    /// As cores de SINAL (amarelo, vermelho, verde...) são as de antes e continuam valendo para o que fica no mundo
    /// (ícones de humor, balões, painéis dos desafios): elas não mudaram.
    /// </summary>
    public static class UITheme
    {
        // ------------------------------------------------------------------ papel e tinta
        public static readonly Color Paper = new(0.957f, 0.929f, 0.839f);        // folha creme
        public static readonly Color PaperDark = new(0.890f, 0.851f, 0.737f);    // botão / caixinha no papel
        public static readonly Color PaperHover = new(1f, 0.976f, 0.902f);       // botão com o mouse em cima
        public static readonly Color PaperPressed = new(0.820f, 0.776f, 0.651f); // botão apertado
        public static readonly Color PaperDisabled = new(0.851f, 0.835f, 0.796f);
        public static readonly Color Ink = new(0.125f, 0.114f, 0.102f);          // máquina de escrever
        public static readonly Color InkSoft = new(0.235f, 0.215f, 0.185f);     // texto secundário
        public static readonly Color InkFaint = new(0.560f, 0.530f, 0.470f);     // cabeçalho impresso, desabilitado
        public static readonly Color Pen = new(0.090f, 0.180f, 0.600f);          // caneta azul
        public static readonly Color PenRed = new(0.690f, 0.090f, 0.090f);       // correção do professor
        public static readonly Color StampRed = new(0.720f, 0.080f, 0.080f);
        public static readonly Color StampBlue = new(0.090f, 0.200f, 0.620f);
        public static readonly Color InkGreen = new(0.090f, 0.400f, 0.160f);
        public static readonly Color InkAmber = new(0.490f, 0.310f, 0.000f);
        public static readonly Color InkOrange = new(0.600f, 0.260f, 0.000f);
        public static readonly Color Margin = new(0.850f, 0.250f, 0.250f, 0.75f); // margem vermelha da folha
        public static readonly Color Rule = new(0.420f, 0.560f, 0.780f, 0.35f);   // linha azul clarinha
        public static readonly Color Highlighter = new(1f, 0.890f, 0.180f);       // marca-texto (questão que sabe)
        public static readonly Color Edge = new(0.290f, 0.250f, 0.190f);          // borda da folha
        public static readonly Color DropShadow = new(0f, 0f, 0f, 0.45f);
        public static readonly Color Dim = new(0f, 0f, 0f, 0.82f);                // escurece a sala atrás do menu

        // ------------------------------------------------------------------ sinais (mundo, balões, desafios: iguais a antes)
        public static readonly Color SignalYellow = new(1f, 0.85f, 0.2f);
        public static readonly Color SignalRed = new(1f, 0.32f, 0.3f);
        public static readonly Color SignalGreen = new(0.4f, 0.95f, 0.45f);
        public static readonly Color SignalOrange = new(1f, 0.6f, 0.2f);
        public static readonly Color SignalGray = new(0.8f, 0.8f, 0.82f);
        public static readonly Color DarkPanel = new(0.1f, 0.11f, 0.16f, 0.88f);
        public static readonly Color FlashRed = new(1f, 0f, 0f, 0f);
        public static readonly Color LegacyOutline = new(0f, 0f, 0f, 0.85f);
        public static readonly Color LegacyShadow = new(0f, 0f, 0f, 0.7f);
        public static readonly Color LegacyTrack = new(0.04f, 0.04f, 0.07f, 1f);
        public static readonly Color LegacyHover = new(1.15f, 1.15f, 1.15f);
        public static readonly Color LegacyPressed = new(0.8f, 0.8f, 0.8f);
        // multiplicadores de cor (Selectable) e transparente
        public static readonly Color TintHover = new(1.12f, 1.12f, 1.12f);
        public static readonly Color TintPressed = new(0.85f, 0.85f, 0.85f);
        public static readonly Color Clear = new(1f, 1f, 1f, 0f);
        public static readonly Color BubblePaper = new(1f, 1f, 0.97f, 0.95f);
        public static readonly Color BubbleInk = new(0.08f, 0.08f, 0.1f);
        public static readonly Color BubbleNameShadow = new(0f, 0f, 0f, 0.9f);

        // ------------------------------------------------------------------ fontes
        /// <summary>Datilografada (títulos e texto). Special Elite, Apache 2.0 — Assets/Fonts/Resources.</summary>
        public static Font Typewriter => typewriter != null ? typewriter : (typewriter = LoadFont("SpecialElite-Regular", "Courier New"));
        /// <summary>À mão (anotações, comentários, notas). Caveat, SIL OFL 1.1 — Assets/Fonts/Resources.</summary>
        public static Font Hand => hand != null ? hand : (hand = LoadFont("Caveat-Regular", "Segoe Print"));
        static Font typewriter, hand;

        /// <summary>Alguma fonte do tema caiu na fonte do sistema (download/importação falhou)?</summary>
        public static bool UsingFallbackFont { get; private set; }

        static Font LoadFont(string resource, string osFallback)
        {
            var f = Resources.Load<Font>(resource);
            if (f != null) return f;
            UsingFallbackFont = true;
            Debug.LogWarning("[UITheme] fonte " + resource + " não encontrada; usando " + osFallback + " do sistema.");
            f = Font.CreateDynamicFontFromOSFont(osFallback, 32);
            return f != null ? f : UIFactory.Font;
        }

        // ------------------------------------------------------------------ tamanhos (referência 1920×1080)
        public const int SizeHuge = 84;     // título do jogo
        public const int SizeTitle = 60;    // título de tela
        public const int SizeHeading = 34;  // subtítulo, botão grande
        public const int SizeButton = 32;
        public const int SizeBody = 25;
        public const int SizeSmall = 23;    // menor tamanho de texto datilografado (a fonte é "gasta": menor que isso fica clara demais)
        public const int SizeHand = 34;     // a Caveat é baixinha: precisa de mais corpo para ler igual
        public const int SizeHandSmall = 30;
        public const int SizeStamp = 76;

        // ------------------------------------------------------------------ espaçamentos e bordas
        public const float Pad = 24f;
        public const float Gap = 16f;
        public const float Border = 3f;
        public const float MarginX = 86f;    // posição da margem vermelha na folha
        public const float StampAngle = -7f;

        /// <summary>Duração das transições de hover/clique (tempo real: funcionam na pausa).</summary>
        public const float Transition = 0.18f;

        // ------------------------------------------------------------------ cores no papel
        /// <summary>
        /// Cor de SINAL (as de sempre, feitas para fundo escuro) convertida em tinta legível no papel. As mensagens do
        /// topo continuam sendo chamadas com HUD.Yellow/Red/...; aqui elas viram tinta.
        /// </summary>
        public static Color OnPaper(Color signal)
        {
            if (Same(signal, SignalYellow)) return InkAmber;
            if (Same(signal, SignalRed)) return StampRed;
            if (Same(signal, SignalGreen)) return InkGreen;
            if (Same(signal, SignalOrange)) return InkOrange;
            if (Same(signal, SignalGray)) return InkSoft;
            if (Same(signal, Color.white)) return Ink;
            // cor desconhecida: escurece até dar contraste
            Color c = signal;
            for (int i = 0; i < 8 && Contrast(c, Paper) < 4.5f; i++) c = Color.Lerp(c, Ink, 0.25f);
            c.a = 1f;
            return c;
        }

        /// <summary>Cor do carimbo do resultado: vermelho para reprovado/expulso, azul para aprovado.</summary>
        public static Color StampFor(Color signal) => Same(signal, SignalRed) ? StampRed : StampBlue;

        static bool Same(Color a, Color b) => Mathf.Abs(a.r - b.r) < 0.02f && Mathf.Abs(a.g - b.g) < 0.02f && Mathf.Abs(a.b - b.b) < 0.02f;

        /// <summary>Contraste WCAG entre duas cores opacas (1 a 21). Texto normal precisa de 4,5.</summary>
        public static float Contrast(Color a, Color b)
        {
            float la = Luminance(a), lb = Luminance(b);
            return (Mathf.Max(la, lb) + 0.05f) / (Mathf.Min(la, lb) + 0.05f);
        }

        static float Luminance(Color c)
        {
            float L(float v) => v <= 0.03928f ? v / 12.92f : Mathf.Pow((v + 0.055f) / 1.055f, 2.4f);
            return 0.2126f * L(c.r) + 0.7152f * L(c.g) + 0.0722f * L(c.b);
        }

        // ------------------------------------------------------------------ sprites gerados por código
        static Sprite frame, doubleFrame, keycap, paper;

        /// <summary>Folha: miolo branco com leve granulado e borda fina (9 fatias; a cor vem do Image).</summary>
        public static Sprite PaperSprite => paper != null ? paper : (paper = MakePaper());
        /// <summary>Moldura de 1 traço (botões, caixinhas, slider). Miolo transparente.</summary>
        public static Sprite FrameSprite => frame != null ? frame : (frame = MakeFrame(32, new[] { (0, 3) }, 6));
        /// <summary>Moldura de 2 traços (carimbo).</summary>
        public static Sprite DoubleFrameSprite => doubleFrame != null ? doubleFrame : (doubleFrame = MakeFrame(48, new[] { (0, 5), (9, 11) }, 14));
        /// <summary>Tecla (cantos arredondados, borda e "sombra" embaixo). Miolo branco: a cor vem do Image.</summary>
        public static Sprite KeycapSprite => keycap != null ? keycap : (keycap = MakeKeycap());

        static Texture2D NewTex(int size)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            return t;
        }

        static Sprite Finish(Texture2D t, float border)
        {
            t.Apply(false, true);
            var s = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                                  new Vector4(border, border, border, border));
            s.hideFlags = HideFlags.DontSave;
            return s;
        }

        /// <summary>Moldura: cada par (de, até) é uma faixa de traço a partir da borda, em pixels.</summary>
        static Sprite MakeFrame(int size, (int from, int to)[] bands, int border)
        {
            var t = NewTex(size);
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    int d = Mathf.Min(Mathf.Min(x, y), Mathf.Min(size - 1 - x, size - 1 - y));
                    bool on = false;
                    foreach (var b in bands) if (d >= b.from && d < b.to) on = true;
                    px[y * size + x] = on ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
                }
            t.SetPixels32(px);
            return Finish(t, border);
        }

        static Sprite MakePaper()
        {
            const int size = 64;
            var t = NewTex(size);
            var px = new Color32[size * size];
            var rnd = new System.Random(16);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    int d = Mathf.Min(Mathf.Min(x, y), Mathf.Min(size - 1 - x, size - 1 - y));
                    byte v = (byte)(d < 2 ? 120 : 255 - rnd.Next(0, 6)); // borda escurecida + granulado leve
                    px[y * size + x] = new Color32(v, v, v, 255);
                }
            t.SetPixels32(px);
            return Finish(t, 8);
        }

        static Sprite MakeKeycap()
        {
            const int size = 32, r = 6;
            var t = NewTex(size);
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    // retângulo arredondado
                    float cx = Mathf.Clamp(x, r, size - 1 - r), cy = Mathf.Clamp(y, r, size - 1 - r);
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                    if (dist > r) { px[y * size + x] = new Color32(0, 0, 0, 0); continue; }
                    bool edge = dist > r - 2.2f || x < 2 || x > size - 3 || y < 4 || y > size - 3;
                    // embaixo a borda é mais grossa: parece uma tecla
                    px[y * size + x] = edge ? new Color32(40, 36, 30, 255) : new Color32(255, 255, 255, 255);
                }
            t.SetPixels32(px);
            return Finish(t, 9);
        }
    }
}
