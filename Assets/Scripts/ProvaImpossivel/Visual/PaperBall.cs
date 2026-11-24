using System.Collections.Generic;
using UnityEngine;

namespace ProvaImpossivel
{
    /// <summary>
    /// Bolinha de papel amassado feita por código: esfera facetada com amassados (cada face vira um plano) e folha de
    /// caderno por cima — linhas azuis e a margem vermelha aparecem em direções diferentes em cada dobra.
    /// A malha tem raio máximo 0,5 (como a esfera do Unity) e se chama "Sphere…": a checagem de "objeto dentro da mão"
    /// (<see cref="HandProps.Touches"/>) a trata como esfera.
    /// </summary>
    public static class PaperBall
    {
        static Mesh mesh;
        static Material material;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlay() { mesh = null; material = null; } // Domain Reload desligado: recria a cada Play

        /// <summary>Cria a bolinha (filha de <paramref name="parent"/>, ou solta se null), com <paramref name="diameter"/> metros.</summary>
        public static GameObject Create(Transform parent, float diameter, string name = "Bolinha")
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.localScale = Vector3.one * diameter;
            go.AddComponent<MeshFilter>().sharedMesh = Mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = Material;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        public static Mesh Mesh => mesh != null ? mesh : (mesh = BuildMesh(7));
        public static Material Material => material != null ? material : (material = BuildMaterial());

        /// <summary>Raio de cada ponto da esfera amassada (0,5 × 0,72..1). Puro: mesma semente, mesmo amassado.</summary>
        public static float Crumple(Vector3 unit, int seed)
        {
            float n = Mathf.Sin(unit.x * 12.9898f + unit.y * 78.233f + unit.z * 37.719f + seed * 4.13f) * 43758.5453f;
            n -= Mathf.Floor(n); // 0..1
            return 0.5f * Mathf.Lerp(0.72f, 1f, n);
        }

        /// <summary>Icosfera (1 subdivisão, 80 faces) com amassados, faces planas e UV de um pedaço da folha por face.</summary>
        public static Mesh BuildMesh(int seed)
        {
            float t = (1f + Mathf.Sqrt(5f)) / 2f;
            var v = new List<Vector3>
            {
                new(-1, t, 0), new(1, t, 0), new(-1, -t, 0), new(1, -t, 0),
                new(0, -1, t), new(0, 1, t), new(0, -1, -t), new(0, 1, -t),
                new(t, 0, -1), new(t, 0, 1), new(-t, 0, -1), new(-t, 0, 1),
            };
            for (int i = 0; i < v.Count; i++) v[i] = v[i].normalized;
            var f = new List<int>
            {
                0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11, 1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
                3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9, 4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1,
            };
            // uma subdivisão: cada triângulo vira 4
            var mid = new Dictionary<long, int>();
            int Mid(int a, int b)
            {
                long k = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
                if (mid.TryGetValue(k, out int idx)) return idx;
                v.Add(((v[a] + v[b]) * 0.5f).normalized);
                mid[k] = v.Count - 1;
                return v.Count - 1;
            }
            var f2 = new List<int>();
            for (int i = 0; i < f.Count; i += 3)
            {
                int a = f[i], b = f[i + 1], c = f[i + 2];
                int ab = Mid(a, b), bc = Mid(b, c), ca = Mid(c, a);
                f2.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
            }
            // amassa: cada vértice num raio diferente
            for (int i = 0; i < v.Count; i++) v[i] *= Crumple(v[i], seed);

            // faces planas (vértices separados por face) e um pedaço da folha, girado, em cada face
            var pos = new List<Vector3>();
            var uv = new List<Vector2>();
            var tri = new List<int>();
            var rnd = new System.Random(seed);
            for (int i = 0; i < f2.Count; i += 3)
            {
                Vector3 a = v[f2[i]], b = v[f2[i + 1]], c = v[f2[i + 2]];
                float ang = (float)rnd.NextDouble() * Mathf.PI * 2f, sc = 3.2f;
                Vector2 o = new((float)rnd.NextDouble(), (float)rnd.NextDouble());
                Vector2 U(Vector3 p)
                {
                    // projeta no plano da face e gira: as linhas do caderno ficam tortas, como papel amassado
                    Vector3 n = Vector3.Cross(b - a, c - a).normalized;
                    Vector3 e1 = (b - a).normalized, e2 = Vector3.Cross(n, e1);
                    Vector2 q = new(Vector3.Dot(p - a, e1), Vector3.Dot(p - a, e2));
                    float cs = Mathf.Cos(ang), sn = Mathf.Sin(ang);
                    return o + new Vector2(q.x * cs - q.y * sn, q.x * sn + q.y * cs) * sc;
                }
                int baseIdx = pos.Count;
                pos.Add(a); pos.Add(b); pos.Add(c);
                uv.Add(U(a)); uv.Add(U(b)); uv.Add(U(c));
                tri.Add(baseIdx); tri.Add(baseIdx + 1); tri.Add(baseIdx + 2);
            }
            var m = new Mesh { name = "Sphere_PapelAmassado" };
            m.SetVertices(pos);
            m.SetUVs(0, uv);
            m.SetTriangles(tri, 0);
            m.RecalculateNormals(); // vértices separados: normal da face (facetado)
            m.RecalculateBounds();
            return m;
        }

        /// <summary>Folha de caderno: creme, linhas azul-claras, margem vermelha e um rabisco de caneta.</summary>
        static Texture2D BuildTexture()
        {
            const int N = 128;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, true) { name = "FolhaDeCaderno", wrapMode = TextureWrapMode.Repeat };
            var papel = new Color(0.98f, 0.965f, 0.9f);
            var linha = new Color(0.55f, 0.72f, 0.95f);
            var margem = new Color(0.93f, 0.42f, 0.42f);
            var caneta = new Color(0.18f, 0.25f, 0.62f);
            var px = new Color[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    Color c = papel;
                    if (y % 16 == 0) c = linha;                                  // pauta
                    if (x == 22 || x == 23) c = margem;                          // margem
                    // "resposta" rabiscada entre duas pautas
                    if (y % 32 >= 5 && y % 32 <= 7 && x > 30 && x < 30 + (y / 32 % 3 + 2) * 18
                        && Mathf.Sin(x * 0.9f + y) > -0.3f) c = caneta;
                    px[y * N + x] = c;
                }
            tex.SetPixels(px);
            tex.Apply(true);
            return tex;
        }

        static Material BuildMaterial()
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "PapelAmassado" };
            var tex = BuildTexture();
            m.SetTexture("_BaseMap", tex);
            m.mainTexture = tex;
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Smoothness", 0.08f);
            return m;
        }
    }

    /// <summary>
    /// Voo da bolinha jogada: sobe em arco até a carteira do colega girando. Quem decide o resultado é o
    /// <see cref="PlayerCheats"/> (no Land); aqui é só o visual: pousar na mesa, cair no chão ou sumir.
    /// </summary>
    public class PaperBallFlight : MonoBehaviour
    {
        public enum End { Voando, Mesa, Chao, Some }

        Vector3 from, to, spinAxis, vel;
        float height, duration, t, life;
        public End State { get; private set; } = End.Voando;
        /// <summary>0..1 do caminho (testes).</summary>
        public float Progress => duration > 0f ? Mathf.Clamp01(t / duration) : 1f;

        /// <summary>Ponto do arco em <paramref name="k"/> (0..1): reta de a até b + parábola de altura h. Puro.</summary>
        public static Vector3 Arc(Vector3 a, Vector3 b, float h, float k) =>
            Vector3.Lerp(a, b, k) + Vector3.up * (h * 4f * k * (1f - k));

        /// <summary>Altura do arco para a distância (m): passa por cima das cabeças da fileira do meio.</summary>
        public static float ArcHeight(float meters) => 0.35f + 0.12f * meters;

        public void Launch(Vector3 a, Vector3 b, float seconds)
        {
            from = a; to = b;
            height = ArcHeight(Vector3.Distance(a, b));
            duration = Mathf.Max(0.05f, seconds);
            t = 0f;
            spinAxis = Random.onUnitSphere;
            transform.position = a;
            State = End.Voando;
        }

        /// <summary>O resultado chegou: pousa na carteira, cai no chão ou some (o professor pegou).</summary>
        public void Finish(End how)
        {
            if (State != End.Voando) return;
            State = how;
            if (how == End.Some) { Destroy(gameObject); return; }
            transform.position = to;
            life = how == End.Mesa ? 3f : 5f;
            // errou: quica para fora da mesa e cai
            vel = how == End.Chao ? (Vector3.ProjectOnPlane(to - from, Vector3.up).normalized * 0.9f + Vector3.up * 0.8f) : Vector3.zero;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (State == End.Voando)
            {
                t += dt;
                transform.position = Arc(from, to, height, Progress);
                transform.Rotate(spinAxis, 720f * dt, Space.World);
                return;
            }
            if (State == End.Chao && transform.position.y > 0.045f)
            {
                vel += Vector3.down * 9.8f * dt;
                Vector3 p = transform.position + vel * dt;
                if (p.y <= 0.045f) { p.y = 0.045f; vel = Vector3.zero; }
                transform.position = p;
                transform.Rotate(spinAxis, 400f * dt, Space.World);
            }
            life -= dt;
            if (life <= 0f) Destroy(gameObject);
        }
    }
}
