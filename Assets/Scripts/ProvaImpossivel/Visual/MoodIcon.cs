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
            rt.sizeDelta = new Vector2(300f, 160f);

            symbol = UIFactory.Label("Simbolo", rt, "", 96, HUD.Yellow);
            symbol.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0f), new Vector2(0f, -40f), new Vector2(300f, 120f));
            symbol.horizontalOverflow = HorizontalWrapMode.Overflow;

            meterBack = UIFactory.Box("Barra", rt, new Color(0f, 0f, 0f, 0.55f), false);
            meterBack.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -55f), new Vector2(110f, 12f));
            meterFill = UIFactory.Box("Preenchido", meterBack.rectTransform, HUD.Yellow, false);
            meterFill.rectTransform.anchorMin = Vector2.zero;
            meterFill.rectTransform.anchorMax = new Vector2(0f, 1f);
            meterFill.rectTransform.pivot = new Vector2(0f, 0.5f);
            meterFill.rectTransform.offsetMin = meterFill.rectTransform.offsetMax = Vector2.zero;
            Show("", Color.clear);
        }

        /// <summary>Texto atual do ícone (testes).</summary>
        public string Symbol => symbol != null ? symbol.text : "";
        /// <summary>A barrinha está aparecendo?</summary>
        public bool MeterVisible => meterBack != null && meterBack.gameObject.activeSelf;

        /// <param name="pulseAmount">0 = parado; 1 = pulsando forte (perigo).</param>
        /// <param name="meter01">0 a 1 = mostra a barrinha; negativo = esconde.</param>
        public void Show(string text, Color color, float pulseAmount = 0f, float meter01 = -1f, Color meterColor = default)
        {
            if (symbol == null) return;
            symbol.text = text ?? "";
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
            bool on = cam != null && gm != null && gm.IsPlaying && (symbol.text.Length > 0 || meter >= 0f);
            if (canvas.enabled != on) canvas.enabled = on;
            if (!on) return;
            var t = canvas.transform;
            t.position = transform.position + Vector3.up * height;
            t.rotation = cam.transform.rotation; // sempre de frente pra câmera
            float s = scale * (1f + pulse * 0.18f * Mathf.Sin(Time.time * 12f));
            // longe, cresce um pouco para continuar legível
            float dist = Vector3.Distance(cam.transform.position, t.position);
            t.localScale = Vector3.one * s * Mathf.Clamp(dist / 6f, 0.8f, 1.8f);
        }
    }
}
