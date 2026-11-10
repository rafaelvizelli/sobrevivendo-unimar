using UnityEngine;

namespace ProvaImpossivel
{
    /// <summary>
    /// Desenha no chão o campo de visão do professor (cortado pelas paredes), com cor pelo nível de perigo.
    /// Criado em tempo de execução pelo ProfessorAI; não precisa estar na cena.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class VisionConeView : MonoBehaviour
    {
        public ProfessorAI prof;
        public int segments = 28;
        [Tooltip("Altura dos raios que cortam o cone (acima das carteiras, para só as paredes cortarem)")]
        public float rayHeight = 1.6f;

        static readonly Color Calm = new(1f, 0.95f, 0.5f, 0.07f);
        static readonly Color Alert = new(1f, 0.55f, 0.1f, 0.16f);
        static readonly Color Danger = new(1f, 0.1f, 0.1f, 0.26f);
        static readonly Color Distracted = new(0.4f, 1f, 0.5f, 0.08f);

        Mesh mesh;
        Vector3[] verts;
        Color[] colors;
        int[] tris;
        MeshRenderer mr;
        readonly RaycastHit[] hits = new RaycastHit[16];

        void Awake()
        {
            mesh = new Mesh { name = "Cone de Visao" };
            mesh.MarkDynamic();
            GetComponent<MeshFilter>().sharedMesh = mesh;
            mr = GetComponent<MeshRenderer>();
            mr.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            verts = new Vector3[segments + 2];
            colors = new Color[segments + 2];
            tris = new int[segments * 3];
            for (int i = 0; i < segments; i++) { tris[i * 3] = 0; tris[i * 3 + 1] = i + 1; tris[i * 3 + 2] = i + 2; }
        }

        void LateUpdate()
        {
            var gm = GameManager.I;
            float dist = prof != null ? prof.CurrentViewDistance : 0f;
            bool show = prof != null && gm != null && (gm.IsPlaying || gm.Phase == Phase.Paused) && dist > 0f;
            if (mr.enabled != show) mr.enabled = show;
            if (!show) return;

            transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            Vector3 eye = prof.visual.HeadPosition;
            Vector3 origin = new(prof.transform.position.x, 0.03f, prof.transform.position.z);
            Vector3 fwd = prof.visual.HeadForward;
            float half = prof.CurrentFov;

            var pl = PlayerController.I;
            Color c = prof.State == ProfState.Distraido ? Distracted
                    : prof.Visibility > 0f && pl != null && pl.CurrentSuspicionRate > 0f ? Danger
                    : prof.Notice > 0.05f || prof.State != ProfState.Tranquilo ? Alert
                    : Calm;
            Color edge = c; edge.a *= 0.35f;

            verts[0] = origin;
            colors[0] = c;
            Vector3 rayOrigin = new(eye.x, rayHeight, eye.z);
            for (int i = 0; i <= segments; i++)
            {
                float a = Mathf.Lerp(-half, half, i / (float)segments);
                Vector3 dir = Quaternion.Euler(0f, a, 0f) * fwd;
                float len = Clip(rayOrigin, dir, dist);
                verts[i + 1] = origin + dir * len;
                colors[i + 1] = edge;
            }
            mesh.Clear();
            mesh.vertices = verts;
            mesh.colors = colors;
            mesh.triangles = tris;
            mesh.RecalculateBounds();
        }

        float Clip(Vector3 from, Vector3 dir, float max)
        {
            int n = Physics.RaycastNonAlloc(from, dir, hits, max, ~0, QueryTriggerInteraction.Ignore);
            float best = max;
            for (int i = 0; i < n; i++)
            {
                if (hits[i].collider.GetComponentInParent<CharacterVisual>() != null) continue;
                if (hits[i].distance < best) best = hits[i].distance;
            }
            return best;
        }
    }
}
