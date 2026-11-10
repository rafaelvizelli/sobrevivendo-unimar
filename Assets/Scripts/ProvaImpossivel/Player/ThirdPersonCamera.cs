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

        float yaw, pitch = 15f, curDist;
        bool wasPlaying;
        readonly RaycastHit[] hits = new RaycastHit[16];

        void Start()
        {
            if (target != null) yaw = target.transform.eulerAngles.y;
            curDist = distance;
        }

        void LateUpdate()
        {
            if (target == null) return;
            var gm = GameManager.I;
            var mouse = Mouse.current;
            bool playing = gm != null && gm.IsPlaying;
            if (playing && !wasPlaying && gm.ElapsedFraction < 0.01f)
            {
                // começo da prova: câmera atrás do jogador, olhando para o quadro
                yaw = target.transform.eulerAngles.y;
                pitch = 15f;
            }
            wasPlaying = playing;
            if (mouse != null && playing)
            {
                Vector2 d = mouse.delta.ReadValue() * (sensitivity * GameOptions.CameraSensitivity);
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
            transform.SetPositionAndRotation(pivot + back * curDist + lift, rot);
        }
    }
}
