using UnityEngine;

namespace ProvaImpossivel
{
    /// <summary>
    /// Mira do bilhete (só visual, só o jogador vê): arco tracejado da mão até onde a bolinha vai cair e um anel
    /// pulsando no alvo. Amarelo = tem alvo (Nerd ou Fundão); vermelho = não tem (a bolinha cairia no chão).
    /// </summary>
    public class BilheteMira : MonoBehaviour
    {
        public const int ArcPoints = 28, RingPoints = 32;
        public static readonly Color Valid = new(1f, 0.82f, 0.18f, 0.95f);
        public static readonly Color Invalid = new(1f, 0.3f, 0.25f, 0.8f);
        /// <summary>Mirando a borracha (favor) no Dorminhoco ou no Puxa-saco: rosa da borracha.</summary>
        public static readonly Color Favor = new(1f, 0.5f, 0.78f, 0.95f);

        LineRenderer arc, ring;
        static Material lineMat; // Sprites/Default: sempre incluído na build (o cone de visão também usa)
        static Texture2D dash;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlay() { lineMat = null; dash = null; }

        public bool Visible => arc != null && arc.enabled;
        public Color CurrentColor => arc != null ? arc.startColor : Color.clear;
        /// <summary>Onde o arco termina agora (testes).</summary>
        public Vector3 End => arc != null && arc.positionCount > 0 ? arc.GetPosition(arc.positionCount - 1) : Vector3.zero;

        void Awake()
        {
            arc = MakeLine("Arco", 0.03f, ArcPoints, false);
            arc.textureMode = LineTextureMode.Tile; // tracejado
            ring = MakeLine("Anel", 0.025f, RingPoints, true);
            Hide();
        }

        LineRenderer MakeLine(string n, float width, int points, bool loop)
        {
            var go = new GameObject(n);
            go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.loop = loop;
            lr.positionCount = points;
            lr.widthMultiplier = width;
            lr.numCapVertices = 2;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.sharedMaterial = LineMaterial();
            return lr;
        }

        static Material LineMaterial()
        {
            if (lineMat != null) return lineMat;
            if (dash == null)
            {
                // traço e espaço (o LineRenderer repete a textura ao longo do arco)
                dash = new Texture2D(8, 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear, name = "Tracejado" };
                for (int x = 0; x < 8; x++) dash.SetPixel(x, 0, x < 5 ? Color.white : new Color(1f, 1f, 1f, 0f));
                dash.Apply();
            }
            lineMat = new Material(Shader.Find("Sprites/Default")) { name = "MiraBilhete", mainTexture = dash };
            lineMat.mainTextureScale = new Vector2(1f / 0.12f, 1f); // um traço a cada ~12 cm
            return lineMat;
        }

        /// <summary>Mostra o arco de <paramref name="from"/> até <paramref name="to"/>.</summary>
        public void Show(Vector3 from, Vector3 to, bool valid) => Show(from, to, valid, Valid);

        /// <summary>Mostra o arco; com alvo, na cor <paramref name="validColor"/> (amarelo bilhete, rosa borracha).</summary>
        public void Show(Vector3 from, Vector3 to, bool valid, Color validColor)
        {
            Color c = valid ? validColor : Invalid;
            float h = PaperBallFlight.ArcHeight(Vector3.Distance(from, to));
            arc.enabled = ring.enabled = true;
            for (int i = 0; i < ArcPoints; i++)
                arc.SetPosition(i, PaperBallFlight.Arc(from, to, h, i / (float)(ArcPoints - 1)));
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(c, 0f), new GradientColorKey(c, 1f) },
                      new[] { new GradientAlphaKey(0.15f, 0f), new GradientAlphaKey(c.a, 0.35f), new GradientAlphaKey(c.a, 1f) });
            arc.colorGradient = g;
            // anel pulsando no ponto de queda
            float r = (valid ? 0.24f : 0.18f) * (1f + 0.12f * Mathf.Sin(Time.unscaledTime * 8f));
            for (int i = 0; i < RingPoints; i++)
            {
                float a = i / (float)RingPoints * Mathf.PI * 2f;
                ring.SetPosition(i, to + new Vector3(Mathf.Cos(a) * r, 0.01f, Mathf.Sin(a) * r));
            }
            ring.startColor = ring.endColor = c;
        }

        public void Hide()
        {
            if (arc != null) arc.enabled = false;
            if (ring != null) ring.enabled = false;
        }
    }
}
