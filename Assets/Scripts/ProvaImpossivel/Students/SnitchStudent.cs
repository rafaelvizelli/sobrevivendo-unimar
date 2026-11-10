using UnityEngine;

namespace ProvaImpossivel
{
    /// <summary>
    /// O Puxa-saco. Senta na frente e de tempos em tempos vira para trás para vigiar a sala.
    /// Se te vir fazendo algo suspeito, dedura (o professor vem investigar).
    /// Interação: pedir para ele tirar uma "dúvida" com o professor (distração).
    /// </summary>
    public class SnitchStudent : StudentBase
    {
        public float watchDuration = 3.5f;
        public float viewDistance = 10f;
        public float halfFov = 55f;
        [Tooltip("Segundos vendo algo suspeito até dedurar")] public float timeToSnitch = 0.6f;
        public float questionCooldown = 40f;
        [Tooltip("Ícone de aviso em cima dele (a dificuldade decide; a cabeça virando existe sempre)")] public bool showHint = true;
        [Tooltip("Distância (m) até onde o jogador vê o ícone (a dificuldade sobrescreve)")] public float hintDistance = 3.4f;
        MoodIcon hint;

        public bool Watching { get; private set; }
        /// <summary>Falta pouco para ele virar (o HUD avisa o jogador com antecedência).</summary>
        public bool AboutToWatch => !Watching && Playing && nextWatch < 2f && visual.handRaise <= 0f;

        /// <summary>Faz ele virar agora (usado por acontecimentos em cadeia).</summary>
        public void ForceWatch() => nextWatch = Mathf.Min(nextWatch, 1.5f);

        // o Puxa-saco também ouve: barulho seu perto dele (cadeira, correr, "psiu" ao Fundão) faz ele virar para vigiar.
        // Uma ação, duas consequências: é o tipo de cadeia que torna a sala uma "máquina de caos".
        protected override void OnEnable()
        {
            base.OnEnable();
            Noise.Emitted += OnNoise;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Noise.Emitted -= OnNoise;
        }

        void OnNoise(NoiseInfo n)
        {
            if (!Playing || !n.byPlayer || Watching || visual.handRaise > 0f || !n.HeardFrom(transform.position)) return;
            if (nextWatch <= 1.5f) return; // já ia virar
            ForceWatch();
            Say("Hã?! Ouvi " + n.what + "...", 2f);
        }

        float nextWatch, watchEnd, seen, snitchCooldown, questionTimer;

        public override string Prompt => "Pedir pro Puxa-saco tirar uma 'dúvida'" + FavorTag;
        // a 1ª virada é sorteada só quando a prova começa (no Start a dificuldade ainda não foi escolhida)
        bool scheduledForMatch;

        protected override void Start()
        {
            base.Start();
            displayName = "PUXA-SACO";
            hint = MoodIcon.For(this, 1.85f);
            ScheduleWatch();
        }

        void ScheduleWatch()
        {
            var s = GameManager.I != null ? GameManager.I.Settings.snitchInterval : new Vector2(10f, 18f);
            nextWatch = Random.Range(s.x, s.y);
        }

        public override bool CanInteract(PlayerController p) => Playing && questionTimer <= 0f && !Watching && HasFavor;

        public override void Interact(PlayerController p)
        {
            questionTimer = questionCooldown;
            Say(Falas.Pick(Falas.PuxaSacoPergunta), 3f);
            visual.handRaise = 1f;
            Invoke(nameof(LowerHand), 5f);
            nextWatch = Mathf.Max(nextWatch, 9f);
            var before = Prof != null ? Prof.State : ProfState.Tranquilo;
            bool ok = Prof != null && Prof.Distract(transform.position, 8f, Falas.Pick(Falas.ProfPuxaSaco), true);
            if (ok) GameManager.I.SpendFavor();
            if (ok || !ToastDistractionRefused(before)) HUD.I.Toast(ok ? "O Puxa-saco chamou o professor pra uma 'dúvida'. Ele está ocupado!" : "O Puxa-saco levantou a mão... e foi ignorado.", ok ? HUD.Yellow : HUD.Gray, 3f);
        }

        void LowerHand() => visual.handRaise = 0f;

        void Update()
        {
            float dt = Time.deltaTime;
            if (snitchCooldown > 0f) snitchCooldown -= dt;
            if (questionTimer > 0f) questionTimer -= dt;
            visual.writing = !Watching;
            visual.expression = Watching ? Expression.Desconfiado : Expression.Feliz;
            if (hint != null)
            {
                bool perto = showHint && PlayerNear(hintDistance);
                if (perto && Watching) hint.Show("O_O", HUD.Red, 0.6f);
                else if (perto && AboutToWatch) hint.Show("...", HUD.Orange, 0.4f);
                else hint.Show("", Color.clear);
            }
            if (!Playing) return;
            if (!scheduledForMatch)
            {
                scheduledForMatch = true;
                ScheduleWatch();
            }

            if (!Watching)
            {
                nextWatch -= dt;
                if (nextWatch <= 0f && visual.handRaise <= 0f)
                {
                    Watching = true;
                    watchEnd = Time.time + watchDuration;
                    seen = 0f;
                    visual.headYaw = 175f; // vira a cabeça igual coruja
                    HUD.I?.Say(transform, displayName, "*gira a cabeça 180° e fica vigiando*", 2f, nameColor);
                }
                return;
            }

            // vigiando: só dedura se VER o jogador fazendo algo suspeito
            var p = PlayerController.I;
            float vis = Vision.Visibility(visual.HeadPosition, visual.HeadForward, viewDistance, halfFov, p.HeadPosition, p.ChestPosition);
            if (vis > 0f && p.CurrentSuspicionRate > 10f) seen += dt;
            if (seen >= timeToSnitch && snitchCooldown <= 0f)
            {
                snitchCooldown = 20f;
                seen = 0f;
                Say(Falas.Pick(Falas.PuxaSacoDedura), 3.5f);
                SoundFX.Play(Sfx.Alerta);
                HUD.I.Toast("O PUXA-SACO TE DEDUROU! O professor está vindo!", HUD.Red, 3.5f);
                Prof?.Say(Falas.Pick(Falas.ProfDedurado), 2f);
                Prof?.Alert(p.transform.position, 40f, "dedurado pelo Puxa-saco", Infracao.Dedurado);
            }

            if (Time.time >= watchEnd)
            {
                Watching = false;
                visual.headYaw = 0f;
                ScheduleWatch();
            }
        }
    }
}
