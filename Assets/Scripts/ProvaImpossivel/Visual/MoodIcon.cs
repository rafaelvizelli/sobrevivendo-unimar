using UnityEngine;
using UnityEngine.UI;

namespace ProvaImpossivel
{
    /// <summary>
    /// Ícone NO MUNDO acima da cabeça de um personagem ("?", "!", "..."), virado para a câmera, com uma barrinha
    /// opcional embaixo. É assim que o professor (e os colegas) mostram o que estão sentindo, em vez de texto no HUD.
    /// Quem é dono decide o que mostrar a cada frame (<see cref="Show"/>); o ícone só desenha.
    /// </summary>
    public class MoodIcon : MonoBehaviour
    {
        [Tooltip("Altura do ícone acima do pé do personagem (m)")] public float height = 2.3f;
        [Tooltip("Tamanho do ícone no mundo")] public float scale = 0.006f;

        Canvas canvas;
        Text symbol;
        Image meterBack, meterFill;
        float pulse, meter = -1f;

        /// <summary>Cria (ou devolve) o ícone de um personagem.</summary>
        public static MoodIcon For(Component owner, float height = 2.3f)
        {
            var icon = owner.GetComponent<MoodIcon>();
            if (icon == null) icon = owner.gameObject.AddComponent<MoodIcon>();
            icon.height = height;
            return icon;
        }

        void Awake()
        {
            var go = new GameObject("Icone de Humor", typeof(RectTransform), typeof(Canvas));
            go.transform.SetParent(transform, false);
            canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(300f, 260f);

            symbol = UIFactory.Label("Simbolo", rt, "", 96, HUD.Yellow);
            symbol.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0f), new Vector2(0f, -40f), new Vector2(300f, 120f));
            symbol.horizontalOverflow = HorizontalWrapMode.Overflow;
            // contorno preto grosso: o ícone fica legível sobre parede clara, quadro verde ou teto
            var contorno = symbol.gameObject.AddComponent<Outline>();
            contorno.effectColor = Color.black;
            contorno.effectDistance = new Vector2(5f, -5f);

            // a barrinha fica ACIMA do ícone (embaixo ela cobria o rosto do personagem)
            meterBack = UIFactory.Box("Barra", rt, new Color(0f, 0f, 0f, 0.7f), false);
            meterBack.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 100f), new Vector2(110f, 14f));
            meterFill = UIFactory.Box("Preenchido", meterBack.rectTransform, HUD.Yellow, false);
            meterFill.rectTransform.anchorMin = Vector2.zero;
            meterFill.rectTransform.anchorMax = new Vector2(0f, 1f);
            meterFill.rectTransform.pivot = new Vector2(0f, 0.5f);
            meterFill.rectTransform.offsetMin = meterFill.rectTransform.offsetMax = Vector2.zero;
            BuildEye(symbol.rectTransform);
            Show("", Color.clear);
        }

        // ------------------------------------------------------------------ olho desenhado (Nerd olhando de lado)

        /// <summary>
        /// Símbolo que vira o OLHO desenhado em vez de texto. O nome continua "O_O" (quem mostra e os testes não mudam);
        /// antes era o texto "O_O" em fonte de sistema, que destoava da "prova impressa".
        /// </summary>
        public const string EyeKey = "O_O";
        /// <summary>Para onde a pupila olha (o jogador). Sem alvo, ela fica no meio.</summary>
        public Transform lookTarget;
        GameObject eye;
        RectTransform eyeRoot, iris;
        float eyeAppear, irisX;
        static Sprite almondSprite, inkSprite, irisSprite;
        // centro do olho no retângulo de 116 px: o olho fica um pouco abaixo do meio (em cima vai a sobrancelha)
        const float EyeCenterY = -12f;

        /// <summary>O olho desenhado está aparecendo? (testes)</summary>
        public bool EyeVisible => eye != null && eye.activeInHierarchy && eyeAppear > 0.01f;
        /// <summary>Posição da pupila: -1 = olhando para a esquerda da tela, +1 = direita. Testes.</summary>
        public float EyeLook => irisX;

        void BuildEye(RectTransform parent)
        {
            EnsureEyeSprites();
            eye = new GameObject("Olho", typeof(RectTransform));
            eyeRoot = (RectTransform)eye.transform;
            eyeRoot.SetParent(parent, false);
            eyeRoot.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(220f, 116f));
            // sombra leve atrás (lê sobre parede clara ou lousa verde)
            var sombra = UIFactory.Box("Sombra", eyeRoot, new Color(0f, 0f, 0f, 0.35f), false);
            sombra.sprite = almondSprite;
            sombra.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(5f, -6f), new Vector2(220f, 116f));
            // branco do olho: também é a máscara que corta a íris na borda
            var branco = UIFactory.Box("Branco", eyeRoot, UITheme.Paper, false);
            branco.sprite = almondSprite;
            branco.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(220f, 116f)); // mesmo retângulo da tinta: contorno em cima do branco
            branco.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            var irisImg = UIFactory.Box("Iris", branco.rectTransform, Color.white, false);
            irisImg.sprite = irisSprite;
            iris = irisImg.rectTransform;
            iris.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, EyeCenterY), new Vector2(64f, 64f));
            // contorno a tinta + sobrancelha desconfiada por cima (fora da máscara)
            var tinta = UIFactory.Box("Tinta", eyeRoot, UITheme.Ink, false);
            tinta.sprite = inkSprite;
            tinta.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(220f, 116f));
            foreach (var g in eye.GetComponentsInChildren<Graphic>()) g.raycastTarget = false;
            eye.SetActive(false);
        }

        /// <summary>Forma de amêndoa do olho: negativo dentro, positivo fora (em unidades da metade da altura). Pura.</summary>
        public static float AlmondField(float x, float y) => Mathf.Abs(y) - Mathf.Pow(Mathf.Max(0f, 1f - x * x), 0.7f); // x, y em [-1, 1]; 0,7 = olho mais aberto

        static void EnsureEyeSprites()
        {
            if (almondSprite != null) return;
            const int W = 256, H = 128;
            var fill = new Texture2D(W, H, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "olho_branco" };
            var ink = new Texture2D(W, H, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "olho_tinta" };
            var fp = new Color32[W * H];
            var ip = new Color32[W * H];
            for (int py = 0; py < H; py++)
                for (int px = 0; px < W; px++)
                {
                    // o olho ocupa a parte de baixo (meia altura 0,85); em cima fica a sobrancelha
                    float x = (px + 0.5f) / W * 2f - 1f;
                    float y = ((py + 0.5f) / H * 2f - 1f) * 1.4f + 0.3f;
                    float x2 = x / 0.92f;
                    float d = AlmondField(x2, y / 0.85f) * 0.85f;                 // ~distância em unidades da altura
                    float aa = 2.4f / H;
                    float dentro = Mathf.Clamp01(0.5f - d / aa);
                    float borda = Mathf.Clamp01(1f - Mathf.Abs(d) / 0.075f) * Mathf.Clamp01(1.4f - Mathf.Abs(x2)); // traço mais fino nas pontas
                    borda = Mathf.Clamp01((borda - 0.25f) * 3f);
                    // sobrancelha: traço grosso inclinado (desce para o lado de dentro = desconfiado), um pouco curvo
                    float by = 1.27f - 0.16f * x + 0.1f * (1f - x * x);
                    float sob = Mathf.Clamp01(1f - Mathf.Abs(y - by) / 0.11f) * Mathf.Clamp01((0.82f - Mathf.Abs(x + 0.05f)) * 6f);
                    sob = Mathf.Clamp01((sob - 0.2f) * 2.5f);
                    float a = Mathf.Max(borda, sob);
                    fp[py * W + px] = new Color32(255, 255, 255, (byte)(dentro * 255f));
                    ip[py * W + px] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            fill.SetPixels32(fp); fill.Apply(false, true);
            ink.SetPixels32(ip); ink.Apply(false, true);
            almondSprite = Sprite.Create(fill, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 100f);
            inkSprite = Sprite.Create(ink, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 100f);

            const int S = 96;
            var ir = new Texture2D(S, S, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "olho_iris" };
            var rp = new Color32[S * S];
            Color red = UITheme.StampRed, tinta = UITheme.Ink;
            for (int py = 0; py < S; py++)
                for (int px = 0; px < S; px++)
                {
                    float x = (px + 0.5f) / S * 2f - 1f, y = (py + 0.5f) / S * 2f - 1f;
                    float r = Mathf.Sqrt(x * x + y * y);
                    float aa = 2f / S;
                    Color c = Color.Lerp(red, tinta, Mathf.Clamp01((r - 0.78f) / aa));          // aro escuro
                    c = Color.Lerp(tinta, c, Mathf.Clamp01((r - 0.42f) / aa));                  // pupila
                    float brilho = Mathf.Clamp01(1f - Mathf.Sqrt((x + 0.25f) * (x + 0.25f) + (y - 0.3f) * (y - 0.3f)) / 0.16f);
                    c = Color.Lerp(c, Color.white, Mathf.Clamp01(brilho * 3f));                  // reflexo
                    c.a = Mathf.Clamp01((0.96f - r) / aa);
                    rp[py * S + px] = c;
                }
            ir.SetPixels32(rp); ir.Apply(false, true);
            irisSprite = Sprite.Create(ir, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>Escala de entrada do olho (0→1 com um leve passar do ponto). Pura.</summary>
        public static float PopIn(float t)
        {
            t = Mathf.Clamp01(t);
            const float k = 1.7f;
            float u = t - 1f;
            return 1f + (k + 1f) * u * u * u + k * u * u;
        }

        void UpdateEye(Camera cam, float dt)
        {
            bool want = current == EyeKey;
            eyeAppear = want ? Mathf.MoveTowards(eyeAppear, 1f, dt * 5f) : Mathf.MoveTowards(eyeAppear, 0f, dt * 7f);
            bool show = eyeAppear > 0.001f;
            if (eye.activeSelf != show) eye.SetActive(show);
            if (!show) return;
            eyeRoot.localScale = Vector3.one * (want ? PopIn(eyeAppear) : eyeAppear);
            // a pupila desliza para o lado do jogador (na tela) e dá umas "conferidas" rápidas
            float alvo = 0f;
            if (lookTarget != null)
            {
                float lado = Vector3.Dot(lookTarget.position - transform.position, cam.transform.right);
                alvo = Mathf.Clamp(lado * 2f, -1f, 1f);
            }
            alvo *= 0.85f + 0.15f * Mathf.Sin(Time.time * 5.3f);
            irisX = Mathf.MoveTowards(irisX, alvo, dt * 4f);
            iris.anchoredPosition = new Vector2(irisX * 52f, EyeCenterY);
        }

        /// <summary>Texto atual do ícone (testes).</summary>
        public string Symbol => current;
        string current = "";

        /// <summary>
        /// Como o símbolo é desenhado: reticências viram três círculos ("•••", que existem na fonte) — os pontos da fonte
        /// em negrito e grandes pareciam três quadrados. Os outros símbolos ficam como estão. Pura (testável).
        /// </summary>
        public static string Glyph(string text) => text == "..." ? "•••" : text ?? "";
        /// <summary>
        /// Altura máxima do símbolo na tela, em fração da altura da tela (Bloco 15b). Sentado ao lado do Nerd (1,35 m)
        /// o "O_O" ocupava ~37% da altura da tela e cobria a HUD; agora ele nunca passa disso, por mais perto que esteja.
        /// 0,11 fica acima do tamanho que o ícone já tinha de 4,8 m para lá, de frente (FOV 60: ~0,10): o ícone do professor
        /// longe não muda. Só encolhe o que estiver perto da câmera (inclusive o professor muito perto, na borda da tela).
        /// </summary>
        public const float MaxScreenFraction = 0.11f;
        /// <summary>Altura (px do canvas) da caixa do símbolo.</summary>
        const float SymbolBoxHeight = 120f;

        /// <summary>Altura do símbolo no mundo (m) na escala base.</summary>
        public static float SymbolWorldHeight(float baseScale) => SymbolBoxHeight * baseScale;

        /// <summary>Fração da altura da tela que um objeto de <paramref name="worldHeight"/> m ocupa a <paramref name="dist"/> m. Pura (testável).</summary>
        public static float ScreenFraction(float worldHeight, float dist, float fovDeg) =>
            worldHeight / (2f * Mathf.Max(dist, 0.01f) * Mathf.Tan(fovDeg * 0.5f * Mathf.Deg2Rad));

        /// <summary>
        /// Multiplicador da escala do ícone pela distância: longe, cresce um pouco para continuar legível (0,8 a 1,8×);
        /// perto, encolhe para não passar de <see cref="MaxScreenFraction"/> da tela. Pura (testável).
        /// </summary>
        /// <param name="depth">Profundidade na direção da câmera (é ela que decide o tamanho na tela; ao lado, é menor que a distância).</param>
        public static float DistanceFactor(float dist, float depth, float fovDeg, float worldHeight)
        {
            float f = Mathf.Clamp(dist / 6f, 0.8f, 1.8f);
            float frac = ScreenFraction(worldHeight * f, depth, fovDeg);
            return frac > MaxScreenFraction ? f * MaxScreenFraction / frac : f;
        }

        /// <summary>Área do símbolo na tela no último frame (px). Testes.</summary>
        public Rect ScreenRect { get; private set; }

        /// <summary>A barrinha está aparecendo?</summary>
        public bool MeterVisible => meterBack != null && meterBack.gameObject.activeSelf;

        /// <param name="pulseAmount">0 = parado; 1 = pulsando forte (perigo).</param>
        /// <param name="meter01">0 a 1 = mostra a barrinha; negativo = esconde.</param>
        public void Show(string text, Color color, float pulseAmount = 0f, float meter01 = -1f, Color meterColor = default)
        {
            if (symbol == null) return;
            current = text ?? "";
            symbol.text = current == EyeKey ? "" : Glyph(current); // o "O_O" vira o olho desenhado (UpdateEye)
            symbol.fontSize = current == "..." ? 120 : 96; // os pontos são pequenos: maiores para aparecer de longe
            symbol.color = color;
            pulse = pulseAmount;
            meter = meter01;
            bool showMeter = meter01 >= 0f;
            if (meterBack.gameObject.activeSelf != showMeter) meterBack.gameObject.SetActive(showMeter);
            if (showMeter)
            {
                meterFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(meter01), 1f);
                meterFill.color = meterColor == default ? color : meterColor;
            }
        }

        void LateUpdate()
        {
            var cam = Camera.main;
            var gm = GameManager.I;
            // o olho ainda saindo (encolhendo) também mantém o ícone ligado
            bool on = cam != null && gm != null && gm.IsPlaying && (current.Length > 0 || meter >= 0f || eyeAppear > 0.001f);
            if (canvas.enabled != on) canvas.enabled = on;
            if (!on) return;
            UpdateEye(cam, Time.deltaTime);
            var t = canvas.transform;
            t.position = transform.position + Vector3.up * height;
            t.rotation = cam.transform.rotation; // sempre de frente pra câmera
            float s = scale * (1f + pulse * 0.18f * Mathf.Sin(Time.time * 12f));
            // longe, cresce um pouco para continuar legível; perto, encolhe até o teto de tamanho na tela (Bloco 15b)
            Vector3 toIcon = t.position - cam.transform.position;
            float depth = Vector3.Dot(toIcon, cam.transform.forward);
            t.localScale = Vector3.one * s * DistanceFactor(toIcon.magnitude, depth, cam.fieldOfView, SymbolWorldHeight(scale));

            // avisa o HUD onde o ícone está na tela (as mensagens do topo desviam dele)
            if (current.Length > 0 && HUD.I != null)
            {
                var c = new Vector3[4];
                symbol.rectTransform.GetWorldCorners(c);
                Vector2 min = new(float.MaxValue, float.MaxValue), max = new(float.MinValue, float.MinValue);
                foreach (var p in c)
                {
                    Vector3 sp = cam.WorldToScreenPoint(p);
                    if (sp.z < 0f) return;
                    min = Vector2.Min(min, sp); max = Vector2.Max(max, sp);
                }
                ScreenRect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
                HUD.I.ReportWorldIcon(ScreenRect);
            }
        }
    }
}
