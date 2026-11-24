using System;
using UnityEngine;
using UnityEngine.AI;

namespace ProvaImpossivel
{
    public enum ProfState { Tranquilo, Desconfiado, Investigando, Flagrante, Distraido }

    /// <summary>
    /// Prof. Takanervoso. Patrulha, desconfia, investiga e dá flagrante.
    /// Só percebe o que está no campo de visão (cone + linha de visada), o que ouve (Noise) ou o que alguém conta (Alert).
    /// Acontecimentos podem distraí-lo (Distract): ao telefone ele fica cego; distraído, só enxerga muito de perto.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class ProfessorAI : MonoBehaviour
    {
        public static ProfessorAI I { get; private set; }

        public CharacterVisual visual;
        public Transform[] patrolPoints;
        public Vector3 classCenter = new(0f, 0f, 0.5f);
        [Tooltip("Para onde ele volta depois de um flagrante")] public Vector3 frontSpot = new(0f, 0f, -5.4f);

        [Header("Visão (a dificuldade sobrescreve no início da prova)")]
        public float viewDistance = 12f;
        public float halfFovCalmo = 48f;
        public float halfFovAlerta = 63f;
        [Tooltip("Distraído ele só vê até aqui")] public float distractedViewDistance = 2.5f;
        [Tooltip("Graus além do cone em que o HUD avisa 'quase olhando'")] public float warnMargin = 25f;

        [Header("Suspeita (0 a 100)")]
        public float desconfiadoEm = 30f;
        public float investigaEm = 65f;
        public float decaimento = 6f;
        public float decaimentoInvestigando = 2.5f;
        public float reactionTime = 0.6f;
        public float graceAfterFlagrante = 10f;
        public float suspicionMultiplier = 1f;
        public float distractionMultiplier = 1f;

        [Header("Velocidades")]
        public float walkSpeed = 1.1f;
        public float approachSpeed = 0.9f;
        public float investigateSpeed = 1.9f;
        public float chaseSpeed = 2.6f;
        public float distractSpeed = 1.7f;

        [Header("Patrulha")]
        public Vector2 pauseTime = new(2f, 5f);
        [Range(0, 1)] public float chanceIrPertoDoJogador = 0.3f;

        public float Suspicion { get; private set; }
        public ProfState State { get; private set; }
        /// <summary>Quanto o professor está enxergando o jogador agora (0 = não vê).</summary>
        public float Visibility { get; private set; }
        /// <summary>0 a 1: o quanto ele está "percebendo" uma ação suspeita (tempo de reação).</summary>
        public float Notice { get; private set; }
        /// <summary>0 = olhar longe de você; 1 = te vendo. Entre os dois = "quase olhando".</summary>
        public float Awareness { get; private set; }
        public bool Busy => State == ProfState.Flagrante || inChallenge;
        public bool OnPhone => phoneCall && State == ProfState.Distraido;
        public bool InGrace => Time.time < graceUntil;
        /// <summary>"Agora EU vou ficar de olho": por alguns segundos depois de certos acontecimentos ele vê mais e reage mais rápido.</summary>
        public bool Alerted => Time.time < alertedUntil;
        float alertedUntil = -999f;
        [Header("Atento (depois de acontecimentos)")]
        [Tooltip("Graus a mais no campo de visão enquanto atento")] public float alertedExtraFov = 12f;
        [Tooltip("Multiplica o tempo de reação enquanto atento (menor = mais rápido)")] public float alertedReaction = 0.6f;

        /// <summary>Fica atento por <paramref name="seconds"/> s (a fala dele precisa ter consequência).</summary>
        public void BeAlert(float seconds) => alertedUntil = Mathf.Max(alertedUntil, Time.time + seconds);
        public event Action<ProfState> StateChanged;

        NavMeshAgent agent;
        float stateTime, pauseTimer, lastGainTime, distractTimer, repathTimer, graceUntil, decideAt = -1f;
        float lastNoiseToast = -999f; // começa "há muito tempo": o 1º ruído da partida também avisa (antes, nos 6 s iniciais, não avisava)
        bool paused, staring, approaching, arrivedHandled, inChallenge, flagApplied, phoneCall, walkingToDistraction, lockedPlayer;
        Vector3 suspectPos, distractPos, faceTarget;
        bool hasFace;
        string lastReason = "";
        Infracao lastInfracao = Infracao.Outra; // o que decide as consequências do flagrante (o texto é só exibição)
        int lastPoint = -1;

        void Awake()
        {
            I = this;
            agent = GetComponent<NavMeshAgent>();
            agent.updateRotation = false;
            if (visual == null) visual = GetComponent<CharacterVisual>();
        }

        void OnEnable() => Noise.Emitted += OnNoise;
        void OnDisable() => Noise.Emitted -= OnNoise;
        void OnNoise(NoiseInfo n) => HearNoise(n.position, n.radius, n.amount, n.what, n.kind);

        void Start()
        {
            // o agente fica DESLIGADO na cena: no executável, ligado desde o carregamento ele nascia antes do
            // NavMesh da cena ("Failed to create agent because there is no valid NavMesh"). No Start a malha já existe.
            agent.enabled = true;
            visual.footstepVolume = 0.8f; // patrulhando: passos normais (Enter aumenta ao investigar)
            faceTarget = classCenter;
            hasFace = true;
            var cone = new GameObject("Cone de Visao do Professor", typeof(MeshFilter), typeof(MeshRenderer)).AddComponent<VisionConeView>();
            cone.prof = this;
            mood = MoodIcon.For(this, 2.35f);
        }

        public void ApplySettings(DifficultySettings s)
        {
            suspicionMultiplier = s.suspicionMultiplier;
            viewDistance = s.viewDistance;
            halfFovCalmo = s.halfFov;
            halfFovAlerta = s.halfFov + 15f;
            reactionTime = s.reactionTime;
            decaimento = s.decayPerSecond;
            decaimentoInvestigando = s.decayPerSecond * 0.4f;
            graceAfterFlagrante = s.graceAfterFlagrante;
            distractionMultiplier = s.distractionMultiplier;
            distractWhileSuspicious = s.distractWhileSuspicious;
            turnTellSeconds = s.turnTellSeconds;
            showSuspicionMeter = s.showSuspicionMeter;
            neverCatches = s.professorNeverCatches;
            turnTellAngle = s.turnTellAngle;
            graceCatchSuspicion = s.graceCatchSuspicion;
            practiceCatchSuspicion = s.practiceCatchSuspicion;
            colinhaInstantCatch = s.colinhaInstantCatch;
            colinhaCatchVisibility = s.colinhaCatchVisibility;
            bilheteFindChance = s.bilheteFindChance;
            foneRingFindChance = s.foneRingFindChance;
            evidenceMemorySeconds = s.evidenceMemorySeconds;
            evidence = Infracao.Nenhuma;
        }

        [Header("Colas que ele pega na hora / provas que ele acha (S2; a dificuldade sobrescreve)")]
        public bool colinhaInstantCatch = true;
        public float colinhaCatchVisibility = 0.7f;
        [Range(0, 1)] public float bilheteFindChance = 0.5f;
        [Range(0, 1)] public float foneRingFindChance = 0.6f;
        public float evidenceMemorySeconds = 20f;

        /// <summary>
        /// Prova que pode estar com o jogador por causa de um barulho que ele ouviu (papel do bilhete, celular vibrando).
        /// Separada do <see cref="lastInfracao"/> de propósito: um "Dedurado" do Puxa-saco não apaga o que ele ouviu.
        /// </summary>
        public Infracao Evidence => Time.time <= evidenceUntil ? evidence : Infracao.Nenhuma;
        Infracao evidence = Infracao.Nenhuma;
        float evidenceUntil = -999f;

        [Tooltip("Desconfiado, ele ainda pode ser distraído? (a dificuldade sobrescreve)")]
        public bool distractWhileSuspicious;

        /// <summary>
        /// Regra única de quando uma distração funciona: com ele tranquilo ou já distraído (troca de foco).
        /// Investigando, dando flagrante ou num desafio, nunca; desconfiado, só se a dificuldade permitir.
        /// </summary>
        public static bool CanBeDistracted(ProfState state, bool busy, bool whileSuspicious) =>
            !busy && state != ProfState.Investigando && state != ProfState.Flagrante
            && (state != ProfState.Desconfiado || whileSuspicious);

        /// <summary>Uma distração funcionaria agora? (mesma regra do Distract, para quem quer saber antes de tentar)</summary>
        public bool CanBeDistractedNow => CanBeDistracted(State, Busy, distractWhileSuspicious);

        /// <summary>
        /// Pegou o jogador no ato por um meio que não é a visão (ex.: o papel da cola do Fundão caiu no pé dele).
        /// Respeita a trégua (aí ele só desconfia). Retorna true se virou flagrante.
        /// </summary>
        public bool CatchRedHanded(string reason, Infracao kind)
        {
            var p = PlayerController.I;
            if (Busy || p == null || GameManager.I == null || !GameManager.I.IsPlaying) return false;
            if (InGrace)
            {
                Alert(p.transform.position, graceCatchSuspicion, reason, kind);
                return false;
            }
            Suspicion = 100f;
            suspectPos = p.transform.position;
            StartFlagrante(reason, kind);
            return true;
        }

        /// <summary>
        /// Muda o rumo da ronda (acontecimento de ameaça): tranquilo, ele vai até <paramref name="position"/>
        /// e, ao chegar, para e encara o jogador. Retorna false se ele estiver ocupado com outra coisa.
        /// </summary>
        public bool WalkTo(Vector3 position, bool stareAtPlayer)
        {
            if (State != ProfState.Tranquilo || Busy) return false;
            paused = staring = false;
            forceStare = stareAtPlayer;
            return GoToWithTell(position, walkSpeed);
        }

        bool forceStare;

        void Update()
        {
            var gm = GameManager.I;
            bool playing = gm != null && gm.IsPlaying;
            // rede de segurança: se ainda assim o agente estiver fora da malha, reposiciona assim que houver NavMesh
            if (!agent.isOnNavMesh && NavMesh.SamplePosition(transform.position, out var navHit, 3f, NavMesh.AllAreas))
                agent.Warp(navHit.position);
            if (agent.isOnNavMesh) agent.isStopped = !playing;
            if (!playing) return;

            float dt = Time.deltaTime;
            stateTime += dt;
            Perceive(dt);

            switch (State)
            {
                case ProfState.Tranquilo: UpdateTranquilo(dt); break;
                case ProfState.Desconfiado: UpdateDesconfiado(); break;
                case ProfState.Investigando: UpdateInvestigando(dt); break;
                case ProfState.Flagrante: UpdateFlagrante(); break;
                case ProfState.Distraido: UpdateDistraido(dt); break;
            }

            UpdateRotation(dt);
            UpdateVisual();
        }

        // ------------------------------------------------------------------ percepção

        public float CurrentFov => (State == ProfState.Tranquilo || State == ProfState.Distraido ? halfFovCalmo : halfFovAlerta)
                                   + (Alerted && State != ProfState.Distraido ? alertedExtraFov : 0f);

        public float CurrentViewDistance =>
            OnPhone ? 0f : State == ProfState.Distraido ? distractedViewDistance : viewDistance;

        void Perceive(float dt)
        {
            var p = PlayerController.I;
            if (p == null) return;

            float dist = CurrentViewDistance;
            float fov = CurrentFov;
            Vector3 eye = visual.HeadPosition;
            Visibility = dist <= 0f ? 0f : Vision.Visibility(eye, visual.HeadForward, dist, fov, p.HeadPosition, p.ChestPosition);
            Awareness = ComputeAwareness(eye, p, dist, fov);

            if (State == ProfState.Flagrante || inChallenge)
            {
                Notice = 0f;
                return;
            }

            float rate = p.CurrentSuspicionRate;
            bool seesSomething = Visibility > 0f && rate > 0f;

            // tempo de reação: ele precisa "perceber" antes da suspeita disparar
            float react = Mathf.Max(0.05f, reactionTime * (Alerted ? alertedReaction : 1f));
            Notice = Mathf.MoveTowards(Notice, seesSomething ? 1f : 0f, dt / (seesSomething ? react : react * 0.5f));

            if (seesSomething)
            {
                float mul = (State == ProfState.Distraido ? 0.35f : 1f) * (InGrace ? 0.25f : 1f);
                Suspicion += SuspicionStep(rate, Visibility, suspicionMultiplier * mul, Notice, dt);
                lastGainTime = Time.time;
                suspectPos = p.transform.position;
                lastReason = p.CurrentSuspicionReason;
                lastInfracao = p.CurrentInfracao;
            }
            else if (CloseRangeSense(p, dt))
            {
                // sentiu de perto (colinha, Bloco 15): já somou; sem ver, não vira flagrante (o teto de 99 abaixo cuida)
            }
            else if (Time.time - lastGainTime > 1.5f)
            {
                Suspicion -= (State == ProfState.Investigando ? decaimentoInvestigando : decaimento) * dt;
            }
            Suspicion = Mathf.Clamp(Suspicion, 0f, 100f);
            MatchLog.Suspeita(Suspicion);

            if (InGrace)
            {
                Suspicion = Mathf.Min(Suspicion, investigaEm - 1f); // durante a trégua ele não parte pra cima
                return;
            }

            // S2: colinha vista bem de perto não espera a suspeita encher: é flagrante na hora
            if (seesSomething && CheatRules.ColinhaCaughtNow(colinhaInstantCatch, p.CurrentInfracao, Notice, Visibility,
                                                              colinhaCatchVisibility, State == ProfState.Distraido))
            {
                Suspicion = 100f;
                StartFlagrante(p.CurrentSuspicionReason, Infracao.ColinhaNaManga);
                return;
            }

            if (Suspicion >= 100f)
            {
                if (seesSomething) StartFlagrante(lastReason, lastInfracao);
                else Suspicion = 99f;
            }
            else if (Suspicion >= investigaEm && (State == ProfState.Tranquilo || State == ProfState.Desconfiado || State == ProfState.Distraido))
                Enter(ProfState.Investigando);
            else if (Suspicion >= desconfiadoEm && State == ProfState.Tranquilo)
                Enter(ProfState.Desconfiado);
        }

        /// <summary>
        /// "Sentido de perto" (Bloco 15): algumas colas (a colinha na manga) ele percebe sem olhar, só por estar perto —
        /// o farfalhar do papel, o braço esquisito. Soma suspeita e marca o lugar; ao telefone, nada.
        /// Retorna true se percebeu algo neste passo.
        /// </summary>
        bool CloseRangeSense(PlayerController p, float dt)
        {
            if (OnPhone) return false;
            float step = CheatRules.CloseRangeStep(p.CloseRangeRate, suspicionMultiplier * (State == ProfState.Distraido ? 0.35f : 1f) * (InGrace ? 0.25f : 1f),
                                                   FlatDistance(p.transform.position), p.CloseRangeDistance, dt);
            if (step <= 0f) return false;
            Suspicion = Mathf.Min(99f, Suspicion + step);
            lastGainTime = Time.time;
            suspectPos = p.transform.position;
            lastReason = p.CurrentSuspicionReason;
            lastInfracao = p.CurrentInfracao;
            return true;
        }

        /// <summary>
        /// Suspeita ganha num passo de tempo vendo algo suspeito. A rampa (Notice 0→1, no tempo de reação)
        /// faz o começo valer pouco. Pública e estática para a simulação de equilíbrio usar a mesma conta.
        /// </summary>
        public static float SuspicionStep(float rate, float visibility, float multiplier, float notice, float dt) =>
            rate * visibility * multiplier * Mathf.Lerp(0.15f, 1f, notice) * dt;

        /// <summary>
        /// Quanta suspeita o professor (calmo, sem trégua) acumula vendo, por <paramref name="seconds"/> s seguidos,
        /// uma ação com taxa <paramref name="rate"/> e visibilidade <paramref name="visibility"/>. Mesma conta do Perceive.
        /// </summary>
        public static float SuspicionIfSeen(DifficultySettings s, float rate, float visibility, float seconds, float start = 0f)
        {
            const float dt = 1f / 60f;
            float susp = start, notice = 0f, react = Mathf.Max(0.05f, s.reactionTime);
            for (float t = 0f; t < seconds; t += dt)
            {
                notice = Mathf.MoveTowards(notice, 1f, dt / react);
                susp += SuspicionStep(rate, visibility, s.suspicionMultiplier, notice, dt);
            }
            return susp;
        }

        float ComputeAwareness(Vector3 eye, PlayerController p, float dist, float fov)
        {
            if (Visibility > 0f) return 1f;
            if (dist <= 0f) return 0f;
            Vector3 to = p.ChestPosition - eye;
            if (to.magnitude > dist) return 0f;
            Vector3 flat = to; flat.y = 0f;
            float ang = Vector3.Angle(visual.HeadForward, flat);
            if (ang > fov + warnMargin) return 0f;
            if (!Vision.LineOfSight(eye, p.ChestPosition) && !Vision.LineOfSight(eye, p.HeadPosition)) return 0f;
            return Mathf.Lerp(0.8f, 0.1f, (ang - fov) / warnMargin);
        }

        /// <summary>Alguém (ex.: o Puxa-saco ou o Nerd) chamou a atenção do professor para um lugar.</summary>
        public void Alert(Vector3 position, float amount, string reason, Infracao kind = Infracao.Outra)
        {
            if (State == ProfState.Flagrante || inChallenge) return;
            Suspicion = Mathf.Min(99f, Suspicion + amount * suspicionMultiplier);
            if (InGrace) Suspicion = Mathf.Min(Suspicion, investigaEm - 1f);
            suspectPos = position;
            lastReason = reason;
            lastInfracao = kind;
            lastGainTime = Time.time;
            if (Suspicion >= investigaEm) Enter(ProfState.Investigando);
            else if (State == ProfState.Tranquilo || State == ProfState.Distraido) Enter(ProfState.Desconfiado);
        }

        /// <summary>
        /// Ouviu um barulho em <paramref name="position"/>. Ele não sabe QUEM fez: só olha/vai até o lugar.
        /// O que ele enxergar lá é que decide o resto.
        /// </summary>
        public void HearNoise(Vector3 position, float radius, float amount, string what, Infracao kind = Infracao.Outra)
        {
            if (Busy || OnPhone) return;
            Vector3 a = transform.position, b = position;
            a.y = b.y = 0f;
            if (Vector3.Distance(a, b) > radius) return;
            if (State == ProfState.Distraido && amount < 8f) return; // distraído ignora barulhinho
            if (CheatRules.LeavesEvidence(kind))
            {
                // S2: ele ouviu o papel / o celular: se for investigar aquele lugar, pode achar a prova
                evidence = kind;
                evidenceUntil = Time.time + evidenceMemorySeconds;
            }

            Suspicion = Mathf.Min(Mathf.Max(Suspicion, 0f) + amount * suspicionMultiplier, Mathf.Max(Suspicion, investigaEm + 5f));
            if (InGrace) Suspicion = Mathf.Min(Suspicion, investigaEm - 1f);
            suspectPos = position;
            lastReason = what;
            lastInfracao = Infracao.Outra;
            lastGainTime = Time.time;
            if (Time.time - lastNoiseToast > 6f)
            {
                lastNoiseToast = Time.time;
                SoundFX.PlayAt(Sfx.Ouviu, visual.HeadPosition); // o jogador OUVE que foi ouvido (e de onde vem o professor); o "?" aparece em cima da cabeça dele
            }
            if (Suspicion >= investigaEm) Enter(ProfState.Investigando);
            else if (State == ProfState.Tranquilo || State == ProfState.Distraido) Enter(ProfState.Desconfiado);
            else faceTarget = position;
        }

        /// <summary>Um acontecimento chama a atenção do professor (ele vai até lá ou só olha).</summary>
        public bool Distract(Vector3 position, float duration, string line, bool walkThere, bool phone = false)
        {
            // investigando (e, por padrão, desconfiado), ele está decidido: nenhuma bagunça tira ele do caminho
            // (senão pedir uma distração vira um "saia da prisão" justamente no momento de maior tensão)
            if (!CanBeDistracted(State, Busy, distractWhileSuspicious)) return false;
            Enter(ProfState.Distraido, silent: true);
            distractPos = position;
            distractTimer = duration * distractionMultiplier;
            phoneCall = phone;
            walkingToDistraction = walkThere;
            if (walkThere) GoTo(position, distractSpeed);
            else Stop();
            if (!string.IsNullOrEmpty(line)) Say(line, 3f);
            return true;
        }

        /// <summary>Encerra a distração atual (ex.: um acontecimento em cadeia puxa a atenção pra outro lugar).</summary>
        public void EndDistraction()
        {
            if (State == ProfState.Distraido) distractTimer = 0f;
        }

        // ------------------------------------------------------------------ estados

        void Enter(ProfState s, bool silent = false)
        {
            State = s;
            // investigando / indo dar o flagrante, os passos ficam pesados: dá para ouvir que ele está vindo
            visual.footstepVolume = s == ProfState.Investigando || s == ProfState.Flagrante ? 1.4f : 0.8f;
            stateTime = 0f;
            decideAt = -1f;
            paused = staring = approaching = arrivedHandled = forceStare = false;
            if (TurningSoon) { TurningSoon = false; visual.headPitch = 0f; }
            visual.headYaw = 0f;
            if (s != ProfState.Distraido) phoneCall = false;
            switch (s)
            {
                case ProfState.Desconfiado:
                    Stop();
                    faceTarget = suspectPos;
                    hasFace = true;
                    if (!silent) Say(Falas.Pick(Falas.ProfDesconfiado));
                    SoundFX.Play(Sfx.Alerta);
                    break;
                case ProfState.Investigando:
                    MatchLog.Investigacoes++;
                    if (!silent) Say(Falas.Pick(Falas.ProfInvestigando));
                    GoTo(suspectPos, investigateSpeed);
                    repathTimer = 1f;
                    SoundFX.Play(Sfx.Alerta);
                    break;
            }
            StateChanged?.Invoke(s);
        }

        void UpdateTranquilo(float dt)
        {
            // aviso de virada: parado, pigarreando, antes de sair para o outro lado
            if (TurningSoon)
            {
                visual.headYaw = 0f;
                visual.headPitch = -8f * Mathf.Abs(Mathf.Sin(Time.time * 9f)); // pigarro: a cabeça dá um tranco
                if (Time.time < turnTellUntil) return;
                TurningSoon = false;
                visual.headPitch = 0f;
                GoTo(pendingDest, pendingSpeed);
                return;
            }
            if (!paused)
            {
                if (!Arrived()) return;
                paused = true;
                pauseTimer = UnityEngine.Random.Range(pauseTime.x, pauseTime.y);
                var p = PlayerController.I;
                staring = !InGrace && p != null && (forceStare || FlatDistance(p.transform.position) < 3.2f && UnityEngine.Random.value < 0.35f);
                forceStare = false;
                if (staring) Say(Falas.Pick(Falas.ProfEncarando), 2.5f);
                else if (UnityEngine.Random.value < 0.3f) Say(Falas.Pick(Falas.ProfTranquilo));
                return;
            }

            pauseTimer -= dt;
            if (staring)
            {
                faceTarget = PlayerController.I.transform.position;
                visual.headYaw = 0f;
            }
            else
            {
                faceTarget = classCenter;
                visual.headYaw = Mathf.Sin(stateTime * 0.9f) * 60f;
            }
            hasFace = true;

            if (pauseTimer <= 0f)
            {
                paused = staring = false;
                visual.headYaw = 0f;
                GoToWithTell(NextPatrolPoint(), walkSpeed);
            }
        }

        void UpdateDesconfiado()
        {
            faceTarget = suspectPos;
            hasFace = true;
            if (!approaching && stateTime > 3f)
            {
                approaching = true;
                GoTo(suspectPos, approachSpeed);
            }
            if (approaching && Arrived()) visual.headYaw = Mathf.Sin(stateTime * 1.4f) * 45f;
            if (Suspicion < desconfiadoEm - 10f) Enter(ProfState.Tranquilo);
        }

        void UpdateInvestigando(float dt)
        {
            var p = PlayerController.I;
            if (inChallenge)
            {
                faceTarget = p.transform.position;
                hasFace = true;
                return;
            }
            if (arrivedHandled) return;

            // o jogador se comportou e a suspeita baixou: ele desiste no caminho
            if (Suspicion < desconfiadoEm)
            {
                Say(Falas.Pick(Falas.ProfAlarmeFalso));
                SoundFX.PlayAt(Sfx.Hmpf, visual.HeadPosition); // S4: contrariado
                Enter(ProfState.Desconfiado, silent: true);
                return;
            }

            repathTimer -= dt;
            if (repathTimer <= 0f)
            {
                repathTimer = 1f;
                GoTo(suspectPos, investigateSpeed);
            }

            float dPlayer = FlatDistance(p.transform.position);
            bool playerAtSpot = Flat.Distance(p.transform.position, suspectPos) < 2.5f;
            if (decideAt < 0f && (Arrived() || FlatDistance(suspectPos) < 1.6f))
            {
                // chegou: para, vira para o lugar e olha um instante ANTES de decidir
                Stop();
                decideAt = stateTime + 0.6f;
            }
            if (decideAt >= 0f)
            {
                faceTarget = playerAtSpot ? p.transform.position : suspectPos;
                hasFace = true;
                if (stateTime < decideAt) return;
                arrivedHandled = true;
                decideAt = -1f;

                var found = Evidence;
                evidence = Infracao.Nenhuma; // procurou: achando ou não, a prova não vale de novo
                if (Visibility > 0f && p.CurrentSuspicionRate > 0f)
                    StartFlagrante(p.CurrentSuspicionReason, p.CurrentInfracao);
                else if (playerAtSpot && dPlayer < 4.5f && found != Infracao.Nenhuma
                         && UnityEngine.Random.value < CheatRules.FindChance(found, bilheteFindChance, foneRingFindChance))
                {
                    // S2: ele vasculha a carteira e acha o bilhete / o celular que vibrou
                    Suspicion = 100f;
                    StartFlagrante(found == Infracao.Bilhete ? Falas.MotivoBilheteAchado : Falas.MotivoCelularAchado, found);
                }
                else if (playerAtSpot && dPlayer < 4.5f && FacePokerChallenge.I != null && !FacePokerChallenge.I.Active) // nunca duas ao mesmo tempo
                {
                    inChallenge = true;
                    Say(Falas.Pick(Falas.ProfEncarando), 2f);
                    FacePokerChallenge.I.Begin(Mathf.InverseLerp(4.5f, 1f, dPlayer), OnFacePokerDone);
                }
                else
                {
                    Say(Falas.Pick(Falas.ProfAlarmeFalso));
                    SoundFX.PlayAt(Sfx.Hmpf, visual.HeadPosition); // S4: contrariado
                    Suspicion = Mathf.Min(Suspicion, 40f);
                    Enter(ProfState.Desconfiado, silent: true);
                }
            }
            else if (stateTime > 20f)
            {
                Suspicion = Mathf.Min(Suspicion, 40f);
                Enter(ProfState.Desconfiado, silent: true);
            }
        }

        void OnFacePokerDone(bool ok)
        {
            inChallenge = false;
            if (ok) MatchLog.CaraPaisagemOk++; else MatchLog.CaraPaisagemFalhou++;
            if (ok)
            {
                Say(Falas.Pick(Falas.ProfCaraPaisagemOk));
                SoundFX.PlayAt(Sfx.Hmpf, visual.HeadPosition); // S4: contrariado
                Suspicion = Mathf.Max(0f, Suspicion - 45f);
                Enter(ProfState.Tranquilo);
                GoTo(NextPatrolPoint(), walkSpeed);
                return;
            }
            Say(Falas.Pick(Falas.ProfCaraPaisagemFalha));
            Suspicion = Mathf.Min(100f, Suspicion + 30f);
            if (Suspicion >= 100f) StartFlagrante("cara de culpado demais", Infracao.CaraDeCulpado);
            else Enter(ProfState.Desconfiado, silent: true);
        }

        [Tooltip("S4: segundos em que a câmera fica virada para ele no flagrante")] public float flagranteFocusSeconds = 2.2f;
        [Tooltip("Treino: ameaça, mas nunca dá flagrante (a dificuldade do tutorial liga)")] public bool neverCatches;
        [Tooltip("Pego no ato na trégua: suspeita no lugar do flagrante (a dificuldade sobrescreve)")] public float graceCatchSuspicion = 30f;
        [Tooltip("Treino: suspeita depois do 'isso era flagrante' (a dificuldade sobrescreve)")] public float practiceCatchSuspicion = 40f;

        void StartFlagrante(string reason, Infracao kind)
        {
            if (State == ProfState.Flagrante) return;
            if (neverCatches)
            {
                // treino: mesma percepção e mesma investigação; no lugar do flagrante, só o susto (e a lição)
                Say(Falas.Pick(Falas.TutorialProfQuaseFlagra), 3f);
                SoundFX.Play(Sfx.Alerta);
                Suspicion = practiceCatchSuspicion;
                Notice = 0f;
                graceUntil = Time.time + graceAfterFlagrante;
                Enter(ProfState.Desconfiado, silent: true);
                return;
            }
            lastReason = string.IsNullOrEmpty(reason) ? "atitude suspeita" : reason;
            lastInfracao = kind;
            Enter(ProfState.Flagrante, silent: true);
            flagApplied = false;
            Say(Falas.Pick(Falas.FlagranteDe(kind)), 3f); // fala própria de cada tipo de cola (Bloco 15)
            // S4: o momento é DELE: "a-HÁ!" e a câmera vira para ele enquanto ele vem
            SoundFX.PlayAt(Sfx.Aha, visual.HeadPosition);
            var cam = Camera.main != null ? Camera.main.GetComponent<ThirdPersonCamera>() : null;
            if (cam != null) cam.Focus(transform, flagranteFocusSeconds);
            GoTo(PlayerController.I.transform.position, chaseSpeed);
            PlayerController.I.Lock(true);
            lockedPlayer = true;
            SoundFX.Play(Sfx.Alerta);
        }

        void UpdateFlagrante()
        {
            var p = PlayerController.I;
            faceTarget = p.transform.position;
            hasFace = true;
            if (!flagApplied)
            {
                if (FlatDistance(p.transform.position) < 1.9f || Arrived() || stateTime > 7f)
                {
                    flagApplied = true;
                    Stop();
                    stateTime = 0f;
                    GameManager.I.ApplyFlagrante(lastReason, lastInfracao);
                }
                return;
            }
            if (stateTime > 2.8f)
            {
                Say(Falas.Pick(Falas.ProfVoltaLugar), 2.5f);
                p.SendBackToSeat();
                if (lockedPlayer) { p.Lock(false); lockedPlayer = false; }
                // trégua: ele volta pra frente da sala e não parte pra cima de novo logo em seguida
                Suspicion = 20f;
                Notice = 0f;
                graceUntil = Time.time + graceAfterFlagrante;
                Enter(ProfState.Tranquilo);
                GoTo(frontSpot, walkSpeed);
            }
        }

        void UpdateDistraido(float dt)
        {
            distractTimer -= dt;
            faceTarget = distractPos;
            hasFace = true;
            visual.headYaw = 0f;
            if (walkingToDistraction && FlatDistance(distractPos) < 1.6f)
            {
                walkingToDistraction = false;
                Stop();
            }
            if (distractTimer <= 0f)
            {
                phoneCall = false;
                Enter(Suspicion >= desconfiadoEm ? ProfState.Desconfiado : ProfState.Tranquilo, silent: true);
            }
        }

        // ------------------------------------------------------------------ movimento

        Vector3 NextPatrolPoint()
        {
            if (patrolPoints == null || patrolPoints.Length == 0) return transform.position;
            var p = PlayerController.I;
            int idx;
            if (p != null && !InGrace && UnityEngine.Random.value < chanceIrPertoDoJogador)
            {
                idx = 0;
                float best = float.MaxValue;
                for (int i = 0; i < patrolPoints.Length; i++)
                {
                    float d = (patrolPoints[i].position - p.transform.position).sqrMagnitude;
                    if (i != lastPoint && d < best) { best = d; idx = i; }
                }
            }
            else
            {
                do idx = UnityEngine.Random.Range(0, patrolPoints.Length);
                while (idx == lastPoint && patrolPoints.Length > 1);
            }
            lastPoint = idx;
            return patrolPoints[idx].position;
        }

        bool GoTo(Vector3 pos, float speed)
        {
            if (!agent.isOnNavMesh) return false;
            if (!NavMesh.SamplePosition(pos, out var hit, 3f, NavMesh.AllAreas)) return false;
            agent.speed = speed;
            agent.SetDestination(hit.position);
            hasFace = false;
            return true;
        }

        void Stop()
        {
            if (agent.isOnNavMesh) agent.ResetPath();
        }

        bool Arrived()
        {
            if (!agent.isOnNavMesh) return true;
            if (agent.pathPending) return false;
            if (!agent.hasPath) return true;
            return agent.remainingDistance <= agent.stoppingDistance + 0.15f;
        }

        void UpdateRotation(float dt)
        {
            Vector3 v = agent.velocity;
            v.y = 0f;
            Vector3 dir;
            if (v.sqrMagnitude > 0.04f) dir = v;
            else if (hasFace) { dir = faceTarget - transform.position; dir.y = 0f; }
            else return;
            if (dir.sqrMagnitude < 0.001f) return;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 6f * dt);
        }

        void UpdateVisual()
        {
            visual.expression = State switch
            {
                ProfState.Flagrante => Expression.Bravo,
                ProfState.Desconfiado or ProfState.Investigando => Expression.Desconfiado,
                ProfState.Distraido => phoneCall ? Expression.Assustado : Expression.Bravo,
                _ => staring || Notice > 0.3f ? Expression.Desconfiado : Expression.Neutro,
            };
            bool still = agent.velocity.sqrMagnitude < 0.04f;
            visual.armsCrossed = Mathf.MoveTowards(visual.armsCrossed, still && (State == ProfState.Tranquilo || staring || inChallenge) ? 1f : 0f, Time.deltaTime * 3f);
            visual.handRaise = Mathf.MoveTowards(visual.handRaise, OnPhone ? 0.6f : 0f, Time.deltaTime * 3f);
            visual.prop = OnPhone ? HandProp.Celular : HandProp.Nenhum; // ao telefone: o celular aparece na orelha

            // o que ele está sentindo aparece em cima da cabeça (no mundo), não no HUD
            if (mood != null)
            {
                var (symbol, color, pulse) = MoodFor(State, Notice, OnPhone, TurningSoon, Alerted);
                float s = Suspicion / 100f;
                float meter = showSuspicionMeter && (s > 0.01f || State != ProfState.Tranquilo) && State != ProfState.Distraido ? s : -1f;
                mood.Show(symbol, color, pulse, meter, s < 0.3f ? HUD.Green : s < 0.65f ? HUD.Yellow : HUD.Red);
            }
        }

        float FlatDistance(Vector3 p) => Flat.Distance(transform.position, p);

        // ------------------------------------------------------------------ leitura no corpo

        [Header("Leitura (a dificuldade sobrescreve)")]
        [Tooltip("Segundos parado (pigarro + '...') antes de se virar para o outro lado")] public float turnTellSeconds = 0.6f;
        [Tooltip("Barrinha discreta de suspeita em cima da cabeça")] public bool showSuspicionMeter = true;
        [Tooltip("Acima deste ângulo (graus) a mudança de rumo conta como 'virar' (a dificuldade sobrescreve)")] public float turnTellAngle = 100f;

        MoodIcon mood;
        float turnTellUntil;
        Vector3 pendingDest;
        float pendingSpeed;

        /// <summary>Está parado avisando que vai se virar (pigarro + "...")?</summary>
        public bool TurningSoon { get; private set; }

        /// <summary>
        /// Anda até <paramref name="pos"/>; se for preciso dar as costas para onde ele olha agora (mais de
        /// <see cref="turnTellAngle"/>°), antes para um instante e pigarreia: o jogador vê e ouve que ele vai virar.
        /// </summary>
        bool GoToWithTell(Vector3 pos, float speed)
        {
            Vector3 dir = pos - transform.position;
            dir.y = 0f;
            if (turnTellSeconds <= 0f || dir.sqrMagnitude < 0.25f || Vector3.Angle(transform.forward, dir) < turnTellAngle)
                return GoTo(pos, speed);
            Stop();
            TurningSoon = true;
            turnTellUntil = Time.time + turnTellSeconds;
            pendingDest = pos;
            pendingSpeed = speed;
            SoundFX.PlayAt(Sfx.Ouviu, visual.HeadPosition, 0.7f, 0.62f); // "hm-hm" grave: pigarro, não o "hm?" de quem ouviu
            return true;
        }

        /// <summary>
        /// O que o ícone em cima da cabeça mostra para cada estado. Pura e estática (testável):
        /// "?" que cresce e esquenta conforme ele percebe, "!" investigando, "!!!" no flagrante, "..." pigarreando
        /// antes de virar ou distraído.
        /// </summary>
        public static (string symbol, Color color, float pulse) MoodFor(ProfState state, float notice, bool onPhone, bool turningSoon, bool alerted)
        {
            switch (state)
            {
                case ProfState.Flagrante: return ("!!!", HUD.Red, 1f);
                case ProfState.Investigando: return ("!", HUD.Orange, 0.6f);
                case ProfState.Desconfiado: return ("?", HUD.Yellow, 0.3f);
                case ProfState.Distraido: return (onPhone ? "..." : "", HUD.Green, 0f);
            }
            if (notice > 0.05f) return ("?", Color.Lerp(HUD.Yellow, HUD.Red, notice), notice);
            if (turningSoon) return ("...", HUD.Gray, 0f);
            if (alerted) return ("o.o", HUD.Orange, 0f);
            return ("", Color.clear, 0f);
        }

        public void Say(string text, float seconds = 3f)
        {
            HUD.I?.Say(transform, "PROF. TAKANERVOSO", text, seconds, HUD.Red, priority: 2);
            visual.Talk(seconds * 0.8f);
        }
    }
}
