using UnityEngine;

namespace ProvaImpossivel
{
    /// <summary>
    /// O Nerd. Sabe tudo e escreve rápido. DESAFIO C (Oportunidade Perfeita): segurar E ao lado dele copia uma questão,
    /// mas é muito suspeito se o professor estiver olhando — e o Nerd também vigia a própria prova.
    /// De tempos em tempos ele PARA DE ESCREVER (aviso) e depois OLHA DE LADO: se você estiver espiando nessa hora,
    /// ele te pega e faz escândalo. Não há sorteio: o risco é ler o Nerd (decisão de design do Bloco 2).
    /// </summary>
    public class NerdStudent : StudentBase
    {
        public float copyTime = 3f;
        public int maxCopies = 4;
        public float copySuspicion = 32f;
        [Tooltip("Segundos entre as olhadas de lado (a dificuldade sobrescreve)")] public Vector2 glanceInterval = new(11f, 17f);
        [Tooltip("Segundos de aviso (para de escrever) antes de olhar")] public float glanceWarning = 1.5f;
        [Tooltip("Segundos olhando de lado")] public float glanceDuration = 1.8f;
        [Tooltip("Ícone de aviso em cima dele (a dificuldade decide; o sinal no corpo existe sempre)")] public bool showHint = true;
        MoodIcon hint;

        int copies;
        float cooldown, coverUntil, nextBrag, nextGlance, glanceTimer;
        bool tutorialShown;
        enum Olhar { Escrevendo, Aviso, OlhandoDeLado }
        Olhar olhar;

        /// <summary>Está olhando de lado agora (quem estiver espiando é pego).</summary>
        public bool Glancing => olhar == Olhar.OlhandoDeLado;
        /// <summary>Parou de escrever: vai olhar de lado em instantes.</summary>
        public bool AboutToGlance => olhar == Olhar.Aviso;

        public override string Prompt => "Espiar a prova do Nerd";
        public override string HoldLabel => "Espiando a prova do Nerd... (solte se ele parar de escrever!)";
        public override float HoldTime => copyTime;
        public override float SuspicionPerSecond => copySuspicion;
        public override string SuspicionReason => "espiando a prova do Nerd";
        public override Infracao SuspicionKind => Infracao.EspiarNerd;

        protected override void Start()
        {
            base.Start();
            displayName = "NERD";
            hint = MoodIcon.For(this, 1.85f);
            nextBrag = Random.Range(40f, 70f);
            nextGlance = Random.Range(glanceInterval.x, glanceInterval.y);
        }

        // sentado não dá: a carteira do jogador fica colada na do Nerd, e espiar sem levantar era cola sem risco.
        // Em pé, perto dele, você fica exposto (e o professor investigando te acha fora do lugar).
        public override bool CanInteract(PlayerController p) =>
            Playing && !p.IsSeated && copies < maxCopies && cooldown <= 0f && Time.time > coverUntil && ExamSystem.I.HasRoom;

        public override void OnHoldStart(PlayerController p)
        {
            if (!tutorialShown)
            {
                tutorialShown = true;
                HUD.I.Toast("DESAFIO C: espie com o professor sem olhar pra você. Se o Nerd PARAR DE ESCREVER, solte: ele vai olhar de lado!", HUD.Yellow, 5f);
            }
        }

        public override bool OnHoldTick(PlayerController p, float t)
        {
            if (Glancing)
            {
                Notice(p);
                return false;
            }
            return true;
        }

        /// <summary>Começa o aviso de olhada agora (testes e acontecimentos).</summary>
        public void GlanceSoon()
        {
            if (olhar != Olhar.Escrevendo) return;
            olhar = Olhar.Aviso;
            glanceTimer = glanceWarning;
        }

        void UpdateGlance(float dt)
        {
            switch (olhar)
            {
                case Olhar.Escrevendo:
                    nextGlance -= dt;
                    if (nextGlance <= 0f) GlanceSoon();
                    break;
                case Olhar.Aviso:
                    glanceTimer -= dt;
                    if (glanceTimer <= 0f)
                    {
                        olhar = Olhar.OlhandoDeLado;
                        glanceTimer = glanceDuration;
                    }
                    break;
                case Olhar.OlhandoDeLado:
                    glanceTimer -= dt;
                    if (glanceTimer <= 0f)
                    {
                        olhar = Olhar.Escrevendo;
                        nextGlance = Random.Range(glanceInterval.x, glanceInterval.y);
                        visual.headYaw = 0f; // volta a olhar a prova (senão a cabeça virada parece que ainda está vigiando)
                    }
                    break;
            }
        }

        public override void Interact(PlayerController p)
        {
            copies++;
            MatchLog.NerdCopias++;
            cooldown = 6f;
            ExamSystem.I.MarkCorrect();
            SoundFX.Play(Sfx.Ponto);
            HUD.I.Toast("DESAFIO C CONCLUÍDO! +" + GameManager.Fmt(ExamSystem.I.PointsPerQuestion) + " — cópia perfeita ("
                        + copies + "/" + maxCopies + ")", HUD.Green, 3f);
            if (copies >= maxCopies) HUD.I.Toast("O Nerd virou a prova pro outro lado. Acabou a fonte.", HUD.Gray, 3f);
        }

        void Notice(PlayerController p)
        {
            MatchLog.NerdPegou++;
            Say(Falas.Pick(Falas.NerdPercebeu), 3f);
            visual.expression = Expression.Bravo;
            coverUntil = Time.time + 12f;
            cooldown = 12f;
            SoundFX.Play(Sfx.Erro);
            HUD.I.Toast("DESAFIO C FALHOU: o Nerd olhou de lado, te pegou espiando e fez escândalo!", HUD.Red, 3.5f);
            Prof?.Alert(p.transform.position, 25f, "o Nerd fez escândalo", Infracao.EspiarNerd);
            olhar = Olhar.Escrevendo;
            nextGlance = Random.Range(glanceInterval.x, glanceInterval.y);
        }

        /// <summary>Acontecimento: o Nerd grita que a prova está fácil demais (distrai o professor).</summary>
        public void ShoutEasy()
        {
            Say(Falas.Pick(Falas.NerdGrito), 3.5f);
            visual.handRaise = 1f;
            Prof?.Distract(transform.position, 5f, Falas.Pick(Falas.ProfNerd), false);
            Invoke(nameof(LowerHand), 3f);
        }

        void LowerHand() => visual.handRaise = 0f;

        void Update()
        {
            if (cooldown > 0f) cooldown -= Time.deltaTime;
            bool covering = Time.time < coverUntil;
            if (Playing && !covering) UpdateGlance(Time.deltaTime);

            // o sinal tem que ser visível na sala, não só no HUD: para de escrever e vira a cabeça pro lado do jogador
            visual.writing = !covering && olhar == Olhar.Escrevendo;
            visual.expression = covering ? Expression.Bravo : olhar != Olhar.Escrevendo ? Expression.Desconfiado : Expression.Convencido;
            visual.armsCrossed = covering ? 1f : 0f;
            if (hint != null)
            {
                if (showHint && olhar == Olhar.OlhandoDeLado) hint.Show("O_O", HUD.Red, 0.8f);
                else if (showHint && olhar == Olhar.Aviso) hint.Show("...", HUD.Yellow, 0.4f);
                else hint.Show("", Color.clear);
            }
            if (!Playing) return;

            if (olhar == Olhar.OlhandoDeLado && PlayerController.I != null)
            {
                Vector3 to = PlayerController.I.transform.position - transform.position;
                to.y = 0f;
                visual.headYaw = Mathf.Clamp(Vector3.SignedAngle(transform.forward, to, Vector3.up), -85f, 85f);
            }
            else if (olhar == Olhar.Aviso) visual.headYaw = 0f;
            else IdleLook(Time.deltaTime, 15f);
            nextBrag -= Time.deltaTime;
            if (nextBrag <= 0f)
            {
                nextBrag = Random.Range(45f, 80f);
                Say(Falas.Pick(Falas.NerdGaba));
            }
        }
    }
}
