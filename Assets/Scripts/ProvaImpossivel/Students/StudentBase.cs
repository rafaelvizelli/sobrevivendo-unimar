using UnityEngine;

namespace ProvaImpossivel
{
    /// <summary>Aluno sentado: anima o visual, fala em balões e pode (ou não) ser interativo.</summary>
    public abstract class StudentBase : Interactable
    {
        public string displayName = "Aluno";
        public CharacterVisual visual;
        [Tooltip("Cor do nome no balão de fala")] public Color nameColor = Color.white;

        protected float idleTimer;

        protected virtual void Awake()
        {
            if (visual == null) visual = GetComponent<CharacterVisual>();
        }

        protected virtual void Start()
        {
            visual.seated = true;
            idleTimer = Random.Range(4f, 12f);
        }

        protected static bool Playing => GameManager.I != null && GameManager.I.IsPlaying;
        protected static ProfessorAI Prof => ProfessorAI.I;
        /// <summary>Ainda há favor da turma para pedir distração (Puxa-saco, Dorminhoco, Fundão)?</summary>
        protected static bool HasFavor => GameManager.I != null && GameManager.I.Favors > 0;
        /// <summary>Sufixo dos prompts que gastam favor ("— favor 2/3").</summary>
        protected static string FavorTag => GameManager.I != null && GameManager.I.Settings != null
            ? "  (favor: " + GameManager.I.Favors + "/" + GameManager.I.Settings.favors + ")" : "";
        /// <summary>
        /// Pedido de distração recusado porque o professor está investigando ou desconfiado (ProfessorAI.CanBeDistracted).
        /// Mostra o motivo e retorna true; nos outros estados não mostra nada e retorna false.
        /// </summary>
        protected static bool ToastDistractionRefused(ProfState stateBefore)
        {
            if (stateBefore == ProfState.Investigando)
                HUD.I.Toast("O professor está INVESTIGANDO: nenhuma bagunça tira ele do caminho agora. (favor não gasto)", HUD.Orange, 3f);
            else if (stateBefore == ProfState.Desconfiado && Prof != null && !Prof.distractWhileSuspicious)
                HUD.I.Toast("O professor está DESCONFIADO: ele nem olha pra bagunça. (favor não gasto)", HUD.Orange, 3f);
            else return false;
            return true;
        }

        public override bool CanInteract(PlayerController p) => false;
        public override void Interact(PlayerController p) { }

        public void Say(string text, float seconds = 2.8f)
        {
            HUD.I?.Say(transform, displayName, text, seconds, nameColor);
            visual.Talk(seconds * 0.8f);
        }

        /// <summary>Olhar ocioso: de vez em quando vira a cabeça para os lados.</summary>
        protected void IdleLook(float dt, float maxYaw = 35f)
        {
            idleTimer -= dt;
            if (idleTimer > 0f) return;
            idleTimer = Random.Range(3f, 9f);
            visual.headYaw = Random.value < 0.5f ? 0f : Random.Range(-maxYaw, maxYaw);
        }

        /// <summary>O jogador está a menos de <paramref name="meters"/> m (no plano) deste aluno?</summary>
        protected bool PlayerNear(float meters) =>
            PlayerController.I != null && FlatDistance(PlayerController.I.transform.position, transform.position) <= meters;

        protected static float FlatDistance(Vector3 a, Vector3 b)
        {
            a.y = b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
