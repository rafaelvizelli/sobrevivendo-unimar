using UnityEngine;
using UnityEngine.InputSystem;

namespace ProvaImpossivel
{
    /// <summary>Câmera orbital em terceira pessoa (mouse) que se aproxima em vez de atravessar paredes.</summary>
    public class ThirdPersonCamera : MonoBehaviour
    {
        public PlayerController target;
        public float sensitivity = 0.12f;
        public float distance = 3.2f;
        public float seatedDistance = 2.4f;
        public float minPitch = -25f, maxPitch = 65f;
        public float collisionRadius = 0.2f;
        [Header("Sentado (no meio das carteiras)")]
        [Tooltip("Sentado, ignora obstáculos menores que isto (m): carteiras, cadeiras e colegas. Paredes, chão e o professor continuam bloqueando.")]
        public float seatedIgnoreObstaclesSmallerThan = 1.6f;
        [Tooltip("Sentado, a câmera não desce abaixo disto (graus): mais baixo ela fica entre as cabeças dos colegas")]
        public float seatedMinPitch = -5f;
        [Tooltip("Quanto a câmera sobe (m) quando uma parede a empurra até a nuca do personagem")]
        public float squeezeLift = 0.75f;
        public Vector3 shoulderOffset = new(0.35f, 0f, 0f);

        [Tooltip("S7: analógico direito todo inclinado = tantos \"pixels de mouse\" por segundo (com a sensibilidade 0,12: ~150°/s)")]
        public float stickSpeed = 1250f;

        [Header("Momento do flagrante (S4)")]
        [Tooltip("Quanto o campo de visão fecha ao focar o professor (1 = nada)")] public float focusZoom = 0.62f;
        [Tooltip("Velocidade com que a câmera vira para o foco e volta")] public float focusSpeed = 6f;

        float yaw, pitch = 15f, curDist;
        bool wasPlaying;
        readonly RaycastHit[] hits = new RaycastHit[16];
        Transform focus;
        float focusUntil, focusWeight, baseFov;
        Camera cam;

        void Start()
        {
            if (target != null) yaw = target.transform.eulerAngles.y;
            curDist = distance;
            cam = GetComponent<Camera>();
            if (cam != null) baseFov = cam.fieldOfView;
        }

        /// <summary>
        /// S4: por <paramref name="seconds"/> s a câmera vira e fecha em <paramref name="who"/> (o professor no flagrante),
        /// depois volta sozinha para onde estava. O mouse não mexe nela enquanto isso (o jogador está travado mesmo).
        /// </summary>
        public void Focus(Transform who, float seconds)
        {
            focus = who;
            focusUntil = Time.time + seconds;
        }

        /// <summary>Quanto a câmera está no foco agora (0 = normal, 1 = toda no foco). Testes.</summary>
        public float FocusWeight => focusWeight;

        void LateUpdate()
        {
            if (target == null) return;
            var gm = GameManager.I;
            // S7: mouse ou analógico direito (pelo Controls)
            bool playing = gm != null && gm.IsPlaying;
            if (playing && !wasPlaying && gm.ElapsedFraction < 0.01f)
            {
                // começo da prova: câmera atrás do jogador, olhando para o quadro
                yaw = target.transform.eulerAngles.y;
                pitch = 15f;
            }
            wasPlaying = playing;
            bool focusing = focus != null && Time.time < focusUntil && playing;
            focusWeight = Mathf.MoveTowards(focusWeight, focusing ? 1f : 0f, Time.deltaTime * focusSpeed * 0.5f);
            if (playing && !focusing)
            {
                Vector2 d = Controls.Look(stickSpeed) * (sensitivity * GameOptions.CameraSensitivity);
                yaw += d.x;
                pitch = Mathf.Clamp(pitch - d.y, minPitch, maxPitch);
            }
            else if (gm != null && gm.Phase == Phase.Intro)
            {
                yaw += Time.deltaTime * 6f; // giro lento atrás do menu
            }

            if (target.IsSeated) pitch = Mathf.Max(pitch, seatedMinPitch);
            var rot = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 pivot = target.transform.position + Vector3.up * (target.IsSeated ? 1.35f : 1.65f) + rot * shoulderOffset;
            float want = target.IsSeated ? seatedDistance : distance;

            // não atravessa paredes: encurta até o primeiro obstáculo que não é personagem
            Vector3 back = rot * Vector3.back;
            float allowed = want;
            int n = Physics.SphereCastNonAlloc(pivot, collisionRadius, back, hits, want, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                // colide com paredes, móveis E com os outros personagens (só ignora o próprio jogador)
                if (hits[i].collider.GetComponentInParent<PlayerController>() == target) continue;
                // sentado, a carteira/colega de trás encurtava a câmera até dentro da própria cabeça (visto no executável).
                // Atravessar um objeto pequeno é melhor: os bonecos e móveis não desenham o lado de dentro.
                if (target.IsSeated)
                {
                    Vector3 s = hits[i].collider.bounds.size;
                    if (Mathf.Max(s.x, Mathf.Max(s.y, s.z)) < seatedIgnoreObstaclesSmallerThan) continue;
                }
                if (hits[i].distance < allowed) allowed = Mathf.Max(0.3f, hits[i].distance - 0.05f);
            }
            curDist = allowed < curDist ? allowed : Mathf.Lerp(curDist, allowed, Time.unscaledDeltaTime * 5f);

            // espremida contra uma parede, a câmera fica colada na nuca e a cabeça tapa a tela: sobe um pouco
            // (proporcional ao aperto) para olhar por cima da cabeça, mantendo a mesma direção do olhar
            float squeeze = 1f - Mathf.Clamp01(curDist / want);
            Vector3 lift = Vector3.up * (squeeze * squeezeLift);
            Vector3 pos = pivot + back * curDist + lift;
            Quaternion look = rot;
            if (focusWeight > 0f && focus != null)
            {
                // olha para a cabeça de quem está no foco, mantendo a posição (o jogador continua no quadro)
                Vector3 to = focus.position + Vector3.up * 1.6f - pos;
                if (to.sqrMagnitude > 0.01f) look = Quaternion.Slerp(rot, Quaternion.LookRotation(to), Smooth(focusWeight));
            }
            transform.SetPositionAndRotation(pos, look);
            if (cam != null && baseFov > 0f) cam.fieldOfView = Mathf.Lerp(baseFov, baseFov * focusZoom, Smooth(focusWeight));
        }

        static float Smooth(float t) => t * t * (3f - 2f * t);
    }
}
