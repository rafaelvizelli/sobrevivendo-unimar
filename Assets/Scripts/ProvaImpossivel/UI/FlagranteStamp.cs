using UnityEngine;

namespace ProvaImpossivel
{
    /// <summary>
    /// S4 (Operação Steam): o momento do flagrante. Um carimbo vermelho "FLAGRANTE!" bate no meio da tela (entra grande e
    /// assenta, como carimbo de verdade), fica um instante e some. Mesmo estilo de papel/carimbo do menu e da tela final.
    /// Fica no objeto do HUD; um por vez (um flagrante novo reinicia a animação).
    /// </summary>
    public class FlagranteStamp : MonoBehaviour
    {
        [Tooltip("Segundos da batida (grande → tamanho normal)")] public float slamTime = 0.16f;
        [Tooltip("Segundos parado na tela")] public float holdTime = 1.1f;
        [Tooltip("Segundos sumindo")] public float fadeTime = 0.35f;
        [Tooltip("Tamanho em que o carimbo começa a batida")] public float startScale = 2.4f;

        RectTransform stamp;
        CanvasGroup group;
        float shownAt = -999f;

        /// <summary>O carimbo está na tela agora? (testes)</summary>
        public bool Visible => stamp != null && stamp.gameObject.activeSelf;

        /// <summary>Bate o carimbo com <paramref name="text"/>.</summary>
        public static FlagranteStamp Show(string text)
        {
            var hud = HUD.I;
            if (hud == null) return null;
            var fs = hud.GetComponent<FlagranteStamp>();
            if (fs == null) fs = hud.gameObject.AddComponent<FlagranteStamp>();
            fs.Hit(text);
            return fs;
        }

        void Hit(string text)
        {
            if (stamp != null) Destroy(stamp.gameObject);
            stamp = UIFactory.Stamp("Carimbo Flagrante", HUD.I.PanelRoot, text, UITheme.StampRed, UITheme.SizeStamp + 36, -9f);
            stamp.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), stamp.sizeDelta);
            // um pedaço de papel atrás: sem ele, o carimbo vermelho some em cima da lousa e da sala (visto em captura)
            var papel = UIFactory.Box("Papel", stamp, new Color(UITheme.Paper.r, UITheme.Paper.g, UITheme.Paper.b, 0.92f), false);
            papel.rectTransform.Stretch(-18f);
            papel.raycastTarget = false;
            papel.transform.SetAsFirstSibling();
            group = stamp.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            stamp.SetAsLastSibling();
            shownAt = Time.unscaledTime;
            Animate();
        }

        void Update()
        {
            if (stamp == null || !stamp.gameObject.activeSelf) return;
            Animate();
        }

        void Animate()
        {
            float t = Time.unscaledTime - shownAt;
            float s = t < slamTime ? Mathf.Lerp(startScale, 1f, (t / slamTime) * (t / slamTime)) : 1f;
            stamp.localScale = Vector3.one * s;
            float a = t < slamTime + holdTime ? Mathf.Clamp01(t / (slamTime * 0.5f)) : 1f - (t - slamTime - holdTime) / fadeTime;
            group.alpha = Mathf.Clamp01(a);
            if (t > slamTime + holdTime + fadeTime) stamp.gameObject.SetActive(false);
        }
    }
}
