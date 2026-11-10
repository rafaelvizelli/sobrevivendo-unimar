using UnityEngine;

namespace ProvaImpossivel
{
    /// <summary>
    /// Desenha no chão o campo de visão do professor, com cor pelo nível de perigo. O cone mostra EXATAMENTE onde
    /// ele veria o jogador na postura atual (sentado ou em pé): cada ponto do cone é testado com a mesma regra do
    /// jogo (Vision.LineOfSight, cabeça OU peito, com paredes e colegas bloqueando). Onde não vê, fica transparente
    /// — as sombras atrás dos colegas e das paredes aparecem no chão.
    /// Criado em tempo de execução pelo ProfessorAI; não precisa estar na cena.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class VisionConeView : MonoBehaviour
    {
        public ProfessorAI prof;
        [Tooltip("Quantos raios (fatias) o cone tem")] public int segments = 24;
        [Tooltip("Distância (m) entre as amostras ao longo de cada raio")] public float step = 0.5f;
        [Tooltip("Refaz o teste de visão a cada N frames (a forma acompanha o professor todo frame)")] public int refreshEvery = 2;

        static readonly Color Calm = new(1f, 0.95f, 0.5f, 0.07f);
        static readonly Color Alert = new(1f, 0.55f, 0.1f, 0.16f);
        static readonly Color Danger = new(1f, 0.1f, 0.1f, 0.26f);
        static readonly Color Distracted = new(0.4f, 1f, 0.5f, 0.08f);

        // alturas da cabeça e do peito do jogador em cada postura (as mesmas de PlayerController.HeadPosition/ChestPosition)
        const float SeatedHead = 1.3f, SeatedChest = 0.95f, StandingHead = 1.7f, StandingChest = 1.25f;

        Mesh mesh;
        Vector3[] verts;
        Color[] colors;
        bool[] seen;
        int samples, frame;
        MeshRenderer mr;

        void Awake()
        {
            mesh = new Mesh { name = "Cone de Visao" };
            mesh.MarkDynamic();
            GetComponent<MeshFilter>().sharedMesh = mesh;
            mr = GetComponent<MeshRenderer>();
            mr.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }

        void Build(int rays, int perRay)
        {
            samples = perRay;
            int n = 1 + rays * perRay; // vértice 0 = pé do professor
            verts = new Vector3[n];
            colors = new Color[n];
            seen = new bool[n];
            var tris = new System.Collections.Generic.List<int>();
            for (int r = 0; r < rays - 1; r++)
            {
                // primeiro anel: triângulo com o centro
                tris.Add(0); tris.Add(V(r, 0)); tris.Add(V(r + 1, 0));
                for (int s = 0; s < perRay - 1; s++)
                {
                    int a = V(r, s), b = V(r, s + 1), c = V(r + 1, s), d = V(r + 1, s + 1);
                    tris.Add(a); tris.Add(b); tris.Add(c);
                    tris.Add(c); tris.Add(b); tris.Add(d);
                }
            }
            mesh.Clear();
            mesh.vertices = verts;
            mesh.colors = colors;
            mesh.triangles = tris.ToArray();
        }

        int V(int ray, int sample) => 1 + ray * samples + sample;

        void LateUpdate()
        {
            var gm = GameManager.I;
            float dist = prof != null ? prof.CurrentViewDistance : 0f;
            bool show = prof != null && gm != null && (gm.IsPlaying || gm.Phase == Phase.Paused) && dist > 0f;
            if (mr.enabled != show) mr.enabled = show;
            if (!show) return;

            int rays = segments + 1;
            int perRay = Mathf.Max(2, Mathf.CeilToInt(dist / step) + 1);
            if (verts == null || verts.Length != 1 + rays * perRay) Build(rays, perRay);

            transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            Vector3 eye = prof.visual.HeadPosition;
            Vector3 origin = new(prof.transform.position.x, 0.03f, prof.transform.position.z);
            Vector3 fwd = prof.visual.HeadForward;
            fwd.y = 0f;
            fwd.Normalize();
            float half = prof.CurrentFov;

            var pl = PlayerController.I;
            bool seated = pl == null || pl.IsSeated;
            float headH = seated ? SeatedHead : StandingHead, chestH = seated ? SeatedChest : StandingChest;
            bool refresh = ++frame % Mathf.Max(1, refreshEvery) == 0;

            Color c = prof.State == ProfState.Distraido ? Distracted
                    : prof.Visibility > 0f && pl != null && pl.CurrentSuspicionRate > 0f ? Danger
                    : prof.Notice > 0.05f || prof.State != ProfState.Tranquilo ? Alert
                    : Calm;
            Color edge = c; edge.a *= 0.35f;
            Color hidden = c; hidden.a = 0f;

            verts[0] = origin;
            colors[0] = c;
            for (int r = 0; r < rays; r++)
            {
                float a = Mathf.Lerp(-half, half, r / (float)(rays - 1));
                Vector3 dir = Quaternion.Euler(0f, a, 0f) * fwd;
                for (int s = 0; s < perRay; s++)
                {
                    float d = Mathf.Min(dist, (s + 1) * step);
                    Vector3 p = origin + dir * d;
                    int v = V(r, s);
                    verts[v] = p;
                    if (refresh)
                    {
                        // mesma regra do jogo: vê se enxergar a cabeça OU o peito do jogador naquele lugar
                        Vector3 feet = new(p.x, 0f, p.z);
                        seen[v] = Vision.LineOfSight(eye, feet + Vector3.up * headH) || Vision.LineOfSight(eye, feet + Vector3.up * chestH);
                    }
                    colors[v] = seen[v] ? Color.Lerp(c, edge, d / dist) : hidden;
                }
            }
            mesh.vertices = verts;
            mesh.colors = colors;
            mesh.RecalculateBounds();
        }

        /// <summary>O cone está mostrando este ponto do chão como visto? (testes)</summary>
        public bool ShowsAsSeen(Vector3 groundPoint)
        {
            if (verts == null) return false;
            int best = -1;
            float bestD = float.MaxValue;
            for (int i = 1; i < verts.Length; i++)
            {
                Vector3 d = verts[i] - groundPoint; d.y = 0f;
                if (d.sqrMagnitude < bestD) { bestD = d.sqrMagnitude; best = i; }
            }
            return best > 0 && bestD < step * step && seen[best];
        }
    }
}
