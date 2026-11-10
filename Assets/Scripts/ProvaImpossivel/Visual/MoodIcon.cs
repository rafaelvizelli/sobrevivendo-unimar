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
            Show("", Color.clear);
        }

        /// <summary>Texto atual do ícone (testes).</summary>
        public string Symbol => current;
        string current = "";

        /// <summary>
        /// Como o símbolo é desenhado: reticências viram três círculos ("•••", que existem na fonte) — os pontos da fonte
        /// em negrito e grandes pareciam três quadrados. Os outros símbolos ficam como estão. Pura (testável).
        /// </summary>
        public static string Glyph(string text) => text == "..." ? "•••" : text ?? "";
        /// <summary>A barrinha está aparecendo?</summary>
        public bool MeterVisible => meterBack != null && meterBack.gameObject.activeSelf;

        /// <param name="pulseAmount">0 = parado; 1 = pulsando forte (perigo).</param>
        /// <param name="meter01">0 a 1 = mostra a barrinha; negativo = esconde.</param>
        public void Show(string text, Color color, float pulseAmount = 0f, float meter01 = -1f, Color meterColor = default)
        {
            if (symbol == null) return;
            current = text ?? "";
            symbol.text = Glyph(current);
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
            bool on = cam != null && gm != null && gm.IsPlaying && (current.Length > 0 || meter >= 0f);
            if (canvas.enabled != on) canvas.enabled = on;
            if (!on) return;
            var t = canvas.transform;
            t.position = transform.position + Vector3.up * height;
            t.rotation = cam.transform.rotation; // sempre de frente pra câmera
            float s = scale * (1f + pulse * 0.18f * Mathf.Sin(Time.time * 12f));
            // longe, cresce um pouco para continuar legível
            float dist = Vector3.Distance(cam.transform.position, t.position);
            t.localScale = Vector3.one * s * Mathf.Clamp(dist / 6f, 0.8f, 1.8f);

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
                HUD.I.ReportWorldIcon(Rect.MinMaxRect(min.x, min.y, max.x, max.y));
            }
        }
    }
}
