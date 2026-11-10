using UnityEngine;
using UnityEngine.InputSystem;

namespace ProvaImpossivel
{
    /// <summary>Escolhe o interativo mais próximo na direção da câmera, mostra "[E] ..." e trata apertar/segurar E.</summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerInteraction : MonoBehaviour
    {
        [Tooltip("Ângulo máximo entre a câmera e o objeto para ele ser escolhido")]
        public float maxAngle = 75f;

        PlayerController player;
        Transform cam;
        Interactable current;
        bool holding;
        float hold;

        public Interactable Current => current;

        void Awake() => player = GetComponent<PlayerController>();
        void Start() => cam = Camera.main != null ? Camera.main.transform : null;

        void Update()
        {
            if (player.InputLocked || (QuizChallenge.I != null && QuizChallenge.I.Active))
            {
                Cancel();
                current = null;
                HUD.I.SetPrompt(null);
                return;
            }

            var best = FindBest();
            if (best != current) Cancel();
            current = best;

            if (current == null)
            {
                HUD.I.SetPrompt(null);
                return;
            }

            bool isHold = current.HoldTime > 0f;
            HUD.I.SetPrompt("[E] " + current.Prompt + (isHold ? "  (segure)" : ""));

            var e = Keyboard.current?.eKey;
            if (e == null) return;

            if (!isHold)
            {
                if (e.wasPressedThisFrame)
                {
                    SoundFX.Play(Sfx.Clique);
                    current.Interact(player);
                }
                return;
            }

            if (e.isPressed)
            {
                if (!holding)
                {
                    holding = true;
                    hold = 0f;
                    current.OnHoldStart(player);
                }
                hold += Time.deltaTime;
                float t = hold / current.HoldTime;
                player.SetInteractionSuspicion(current.SuspicionPerSecond, current.SuspicionReason, current.SuspicionKind);
                HUD.I.SetHold(t, current.HoldLabel, player.HoldRiskNow(current.SuspicionPerSecond > 0f));
                if (!current.OnHoldTick(player, t))
                {
                    Cancel();
                    return;
                }
                if (t >= 1f)
                {
                    var it = current;
                    holding = false;
                    player.SetInteractionSuspicion(0f, "");
                    HUD.I.SetHold(-1f, "");
                    it.Interact(player);
                }
            }
            else Cancel();
        }

        void Cancel()
        {
            if (!holding) return;
            holding = false;
            hold = 0f;
            player.SetInteractionSuspicion(0f, "");
            HUD.I.SetHold(-1f, "");
            current?.OnHoldCancel(player);
        }

        Interactable FindBest()
        {
            Vector3 origin = transform.position;
            Vector3 look = cam != null ? cam.forward : transform.forward;
            look.y = 0f;
            Interactable best = null;
            float bestScore = float.MaxValue;
            foreach (var it in Interactable.All)
            {
                if (it == null || !it.isActiveAndEnabled || !it.CanInteract(player)) continue;
                Vector3 to = it.Position - origin;
                to.y = 0f;
                float d = to.magnitude;
                if (d > it.range) continue;
                float ang = d < 0.6f ? 0f : Vector3.Angle(look, to);
                if (ang > maxAngle) continue;
                float score = d + ang * 0.02f;
                if (score < bestScore) { bestScore = score; best = it; }
            }
            return best;
        }
    }
}
