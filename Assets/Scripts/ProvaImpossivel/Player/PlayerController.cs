using UnityEngine;
using UnityEngine.InputSystem;

namespace ProvaImpossivel
{
    /// <summary>
    /// Jogador em terceira pessoa: anda (WASD), corre (Shift), senta/levanta, responde questões e usa o celular.
    /// Também informa ao professor o quanto o que ele está fazendo agora é suspeito.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController I { get; private set; }

        [Header("Movimento")]
        public float walkSpeed = 2.2f;
        public float runSpeed = 4.2f;
        public float turnSpeed = 12f;
        public float gravity = -15f;

        [Header("Lugar na sala")]
        public Transform seatPoint;
        public Transform standPoint;
        public CharacterVisual visual;

        [Header("Ações sentado")]
        public float phoneHoldTime = 3f;
        public float phoneSuspicion = 38f;
        public float quizCooldown = 6f;

        [Header("Suspeita por comportamento")]
        public float outOfSeatSuspicion = 4f;
        public float runningSuspicion = 12f;
        public float outOfSeatDistance = 1.4f;

        public bool IsSeated { get; private set; }
        public bool IsRunning { get; private set; }
        public bool HasPhone { get; set; } = true;

        [Header("Barulho (o professor ouve e vai ver de onde veio)")]
        public float runNoiseRadius = 6f;
        public float chairNoiseRadius = 3.5f;
        public float phoneNoiseRadius = 2.5f;
        public bool InputLocked => lockCount > 0 || GameManager.I == null || !GameManager.I.IsPlaying
                                   || GameManager.I.PhaseChangedFrame == Time.frameCount;

        /// <summary>Quanto o que o jogador está fazendo AGORA gera de suspeita por segundo (se o professor estiver vendo).</summary>
        public float CurrentSuspicionRate { get; private set; }
        public string CurrentSuspicionReason { get; private set; } = "";
        /// <summary>Tipo do que ele está fazendo agora (as regras usam isto; o texto acima é só para exibir).</summary>
        public Infracao CurrentInfracao { get; private set; }
        /// <summary>Consultas que ainda cabem na bateria do celular (a dificuldade define no começo da prova).</summary>
        public int PhoneCharges { get; set; } = 2;
        public bool CanUsePhone => HasPhone && PhoneCharges > 0;
        public bool UsingPhone => phoneProgress > 0f;

        public Vector3 HeadPosition => transform.position + Vector3.up * (IsSeated ? 1.3f : 1.7f);
        public Vector3 ChestPosition => transform.position + Vector3.up * (IsSeated ? 0.95f : 1.25f);

        CharacterController cc;
        Transform cam;
        float vy, phoneProgress, phoneCooldown, quizTimer, noiseTimer;
        int lockCount;
        float interactionRate;
        string interactionReason;
        Infracao interactionKind;
        // a tecla 1/2/3 que escolheu a dificuldade não pode virar "responder"/"celular" se continuar apertada
        bool waitMenuKeyRelease = true;

        void Awake()
        {
            I = this;
            cc = GetComponent<CharacterController>();
            if (visual == null) visual = GetComponent<CharacterVisual>();
            // colas novas (Bloco 15): criadas por código, a cena não precisa ser remontada
            Cheats = GetComponent<PlayerCheats>();
            if (Cheats == null) Cheats = gameObject.AddComponent<PlayerCheats>();
        }

        /// <summary>Colinha, bilhete, fone e garrafinha (Bloco 15).</summary>
        public PlayerCheats Cheats { get; private set; }

        /// <summary>Suspeita/s que o professor sente só por estar perto (colinha), e até que distância (m).</summary>
        public float CloseRangeRate => Cheats != null ? Cheats.CloseRangeRate : 0f;
        public float CloseRangeDistance => Cheats != null ? Cheats.CloseRangeDistance : 0f;

        void Start()
        {
            cam = Camera.main != null ? Camera.main.transform : null;
            SitDown(silent: true);
        }

        public void Lock(bool on) => lockCount = Mathf.Max(0, lockCount + (on ? 1 : -1));

        /// <summary>Cor da barra de "segurando": ação arriscada com o professor vendo (vermelho) ou não (verde).</summary>
        public HUD.HoldRisk HoldRiskNow(bool risky) =>
            !risky ? HUD.HoldRisk.Seguro : ProfessorAI.I != null && ProfessorAI.I.Visibility > 0f ? HUD.HoldRisk.Visto : HUD.HoldRisk.Escondido;

        public void SetInteractionSuspicion(float rate, string reason, Infracao kind = Infracao.Outra)
        {
            interactionRate = rate;
            interactionReason = reason;
            interactionKind = rate > 0f ? kind : Infracao.Nenhuma;
        }

        public void Say(string text, float seconds = 2.5f)
        {
            HUD.I?.Say(transform, "VOCÊ", text, seconds, HUD.Orange, priority: 1);
            visual?.Talk(seconds * 0.8f);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            var kb = Keyboard.current;
            bool canAct = !InputLocked && kb != null;
            if (quizTimer > 0f) quizTimer -= dt;
            if (phoneCooldown > 0f) phoneCooldown -= dt;
            if (noiseTimer > 0f) noiseTimer -= dt;

            if (IsSeated) UpdateSeated(kb, canAct, dt);
            else UpdateFree(kb, canAct, dt);

            UpdateSuspicion();
            UpdateLook(dt);
        }

        // ---------------------------------------------------------------- sentado

        void UpdateSeated(Keyboard kb, bool canAct, float dt)
        {
            if (seatPoint != null) transform.SetPositionAndRotation(seatPoint.position, seatPoint.rotation);
            bool holdingPhone = false;

            bool quizOpen = QuizChallenge.I != null && QuizChallenge.I.Active;
            if (waitMenuKeyRelease && kb != null && !kb.digit1Key.isPressed && !kb.digit2Key.isPressed && !kb.digit3Key.isPressed
                && !kb.numpad1Key.isPressed && !kb.numpad2Key.isPressed && !kb.numpad3Key.isPressed)
                waitMenuKeyRelease = false;
            if (canAct && !quizOpen && !waitMenuKeyRelease)
            {
                bool spaceFree = FacePokerChallenge.I == null || FacePokerChallenge.I.FinishedFrame != Time.frameCount;
                if (spaceFree && kb.spaceKey.wasPressedThisFrame) { StandUp(); return; }

                if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) TryAnswer();

                bool otherCheat = Cheats != null && Cheats.Holding != PlayerCheats.Hold.Nenhum; // uma cola de cada vez
                if (HasPhone && !otherCheat && (kb.digit2Key.isPressed || kb.numpad2Key.isPressed))
                {
                    if (phoneCooldown > 0f) HUD.I.SetHold(-1f, "");
                    else if (PhoneCharges <= 0) HUD.I.Toast("A bateria do celular ACABOU. Agora é Nerd, Fundão ou fé.", HUD.Gray, 2.5f);
                    else if (!ExamSystem.I.HasRoom) HUD.I.Toast("Você já acertou tudo. O Google não tem mais o que fazer.", HUD.Gray, 2f);
                    else if (!CheatBudget.CanAnswer) HUD.I.Toast(Falas.OrcamentoAcabou, HUD.Gray, 2.5f);
                    else
                    {
                        holdingPhone = true;
                        phoneProgress += dt / phoneHoldTime;
                        if (noiseTimer <= 0f)
                        {
                            noiseTimer = 1f;
                            MakeNoise(phoneNoiseRadius, 3f, "um 'tec-tec' de celular");
                            SoundFX.PlayAt(Sfx.Clique, transform.position, 0.35f, 1.3f); // o ruído que a IA ouve também soa pra você
                        }
                        HUD.I.SetHold(phoneProgress, "Pesquisando no celular... (segure 2)", HoldRiskNow(true));
                        if (phoneProgress >= 1f) FinishPhoneConsult();
                    }
                }
            }
            if (!holdingPhone && phoneProgress > 0f)
            {
                phoneProgress = 0f;
                HUD.I.SetHold(-1f, "");
            }

            visual.phone = Mathf.MoveTowards(visual.phone, holdingPhone ? 1f : 0f, dt * 5f);
            visual.writing = !holdingPhone && QuizChallenge.I != null && QuizChallenge.I.Active;

            var exam = ExamSystem.I;
            int knownBlanks = 0, unknownBlanks = 0;
            for (int i = 0; i < exam.Questions.Length; i++)
                if (exam.Questions[i] == QuestionState.EmBranco)
                {
                    if (exam.Known[i]) knownBlanks++;
                    else unknownBlanks++;
                }
            // textos curtos: a linha de ajuda é centralizada e, longa demais, invade o painel de suspeita
            string help = knownBlanks > 0 ? "[1] Responder (sabe " + knownBlanks + ")"
                        : unknownBlanks > 0 ? "[1] Chutar (" + unknownBlanks + " que não sabe)"
                        : "[1] Nada em branco (erradas: só cola)";
            if (quizTimer > 0f && knownBlanks + unknownBlanks > 0) help = "[1] Pensando... " + Mathf.CeilToInt(quizTimer) + "s";
            help += !HasPhone ? "    [2] Celular confiscado"
                  : PhoneCharges <= 0 ? "    [2] Celular sem bateria"
                  : "    [2] Celular (segure; bateria p/ " + PhoneCharges + ")";
            help += "    [Espaço] Levantar";
            HUD.I.SetSeatedHelp(InputLocked ? null : help);
        }

        /// <summary>
        /// Terminou de segurar o celular: uma questão certa e uma carga a menos. A bateria (3%) é o que
        /// impede o celular de resolver a prova sozinho: ele é a cola mais rápida, mas acaba.
        /// </summary>
        void FinishPhoneConsult()
        {
            phoneProgress = 0f;
            phoneCooldown = 4f;
            PhoneCharges--;
            MatchLog.Celular++;
            if (CheatBudget.Spend()) ExamSystem.I.MarkCorrect(); // orçamento de colas da partida (Bloco 15)
            SoundFX.Play(Sfx.Ponto);
            HUD.I.Toast("+" + GameManager.Fmt(ExamSystem.I.PointsPerQuestion) + " — O Google sabia!"
                        + (PhoneCharges > 0 ? "" : " ...e a bateria morreu."), HUD.Green, 2.5f);
            HUD.I.SetHold(-1f, "");
        }

        void TryAnswer()
        {
            if (QuizChallenge.I == null || QuizChallenge.I.Active) return;
            if (quizTimer > 0f) return;
            if (!ExamSystem.I.HasBlank)
            {
                HUD.I.Toast("Você já tentou todas. As erradas só se corrigem com 'ajuda' (Nerd, celular, Fundão).", HUD.Gray, 3f);
                return;
            }
            // primeiro as que ele sabe (Desafio B justo); quando acabam, só resta chutar as outras
            var exam = ExamSystem.I;
            int target = exam.FirstBlank(true);
            bool chute = target < 0;
            if (chute) target = exam.FirstBlank(false);
            Say(chute ? "Essa eu não estudei... vai no chute." : Falas.Pick(Falas.JogadorPensa), 2f);
            // marca a questão que foi aberta (não a "próxima livre"), senão acertar uma conhecida gastaria uma desconhecida
            QuizChallenge.I.Begin(GameManager.I.Settings.quizTime, ok =>
            {
                quizTimer = quizCooldown;
                MatchLog.Quiz(chute, ok);
                if (ok) exam.MarkCorrect(target);
                else exam.MarkWrong(target);
            }, chute);
        }

        public void SitDown(bool silent = false)
        {
            IsSeated = true;
            IsRunning = false;
            cc.enabled = false;
            if (seatPoint != null) transform.SetPositionAndRotation(seatPoint.position, seatPoint.rotation);
            visual.seated = true;
            if (!silent)
            {
                SoundFX.PlayAt(Sfx.Cadeira, transform.position);
                MakeNoise(chairNoiseRadius, 3f, "uma cadeira arrastando");
            }
        }

        /// <summary>Barulho na posição do jogador. Quem ouve (professor, Puxa-saco) sabe o LUGAR, não quem foi.</summary>
        public void MakeNoise(float radius, float amount, string what) =>
            Noise.Emit(transform.position, radius, amount, what, byPlayer: true);

        public void StandUp()
        {
            IsSeated = false;
            visual.seated = false;
            visual.phone = 0f;
            phoneProgress = 0f;
            HUD.I.SetHold(-1f, "");
            HUD.I.SetSeatedHelp(null);
            if (standPoint != null) transform.position = standPoint.position;
            cc.enabled = true;
            vy = 0f;
            SoundFX.PlayAt(Sfx.Cadeira, transform.position);
            MakeNoise(chairNoiseRadius, 3f, "uma cadeira arrastando");
        }

        /// <summary>Depois de um flagrante o professor manda o aluno de volta ao lugar.</summary>
        public void SendBackToSeat()
        {
            if (!IsSeated) SitDown();
        }

        // ---------------------------------------------------------------- andando

        void UpdateFree(Keyboard kb, bool canAct, float dt)
        {
            Vector2 mv = Vector2.zero;
            bool run = false;
            if (canAct)
            {
                mv.x = (kb.dKey.isPressed ? 1 : 0) - (kb.aKey.isPressed ? 1 : 0);
                mv.y = (kb.wKey.isPressed ? 1 : 0) - (kb.sKey.isPressed ? 1 : 0);
                run = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
            }

            Vector3 fwd = cam != null ? cam.forward : Vector3.forward;
            Vector3 right = cam != null ? cam.right : Vector3.right;
            fwd.y = 0f; right.y = 0f;
            fwd.Normalize(); right.Normalize();
            Vector3 dir = fwd * mv.y + right * mv.x;
            if (dir.sqrMagnitude > 1f) dir.Normalize();

            IsRunning = run && dir.sqrMagnitude > 0.01f;
            float speed = IsRunning ? runSpeed : walkSpeed;

            if (dir.sqrMagnitude > 0.01f)
            {
                var target = Quaternion.LookRotation(dir);
                transform.rotation = Quaternion.Slerp(transform.rotation, target, turnSpeed * dt);
            }

            vy = cc.isGrounded ? -2f : vy + gravity * dt;
            cc.Move((dir * speed + Vector3.up * vy) * dt);

            if (IsRunning && noiseTimer <= 0f)
            {
                noiseTimer = 0.6f;
                MakeNoise(runNoiseRadius, 6f, "passos correndo");
            }

            HUD.I.SetSeatedHelp(InputLocked ? null : "[E] perto da sua carteira para sentar    [Shift] Correr");
        }

        // ---------------------------------------------------------------- suspeita / visual

        void UpdateSuspicion()
        {
            float rate = 0f;
            string reason = "";
            var kind = Infracao.Nenhuma;

            if (UsingPhone) { rate = phoneSuspicion; reason = "usando o celular"; kind = Infracao.Celular; }
            if (interactionRate > rate) { rate = interactionRate; reason = interactionReason; kind = interactionKind; }
            if (Cheats != null && Cheats.CurrentRate > rate) { rate = Cheats.CurrentRate; reason = Cheats.CurrentReason; kind = Cheats.CurrentKind; }

            if (!IsSeated && rate <= 0f && seatPoint != null)
            {
                Vector3 a = transform.position, b = seatPoint.position;
                a.y = b.y = 0f;
                if (Vector3.Distance(a, b) > outOfSeatDistance)
                {
                    rate = IsRunning ? runningSuspicion : outOfSeatSuspicion;
                    reason = IsRunning ? "correndo no meio da prova" : "passeando fora do lugar";
                    kind = IsRunning ? Infracao.Correndo : Infracao.ForaDoLugar;
                }
            }
            CurrentSuspicionRate = rate;
            CurrentSuspicionReason = reason;
            CurrentInfracao = kind;
        }

        void UpdateLook(float dt)
        {
            float s = ProfessorAI.I != null ? ProfessorAI.I.Suspicion / 100f : 0f;
            visual.sweat = Mathf.MoveTowards(visual.sweat, Mathf.Max(s, FacePokerChallenge.I != null && FacePokerChallenge.I.Active ? 0.9f : 0f), dt);
            if (FacePokerChallenge.I != null && FacePokerChallenge.I.Active) visual.expression = Expression.Nervoso;
            else if (CurrentSuspicionRate > 10f) visual.expression = Expression.Desconfiado;
            else visual.expression = s > 0.5f ? Expression.Nervoso : Expression.Neutro;

            // ao usar o celular / espiar, vira a cabeça na direção da câmera (mostra pra onde ele "olha")
            if (IsSeated && cam != null)
            {
                Vector3 f = cam.forward; f.y = 0f;
                float yaw = Vector3.SignedAngle(transform.forward, f, Vector3.up);
                visual.headYaw = Mathf.Clamp(yaw, -70f, 70f);
            }
            else visual.headYaw = 0f;
        }
    }
}
