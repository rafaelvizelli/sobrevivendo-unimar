using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace ProvaImpossivel
{
    /// <summary>Objeto que o professor leva embora num flagrante (decidido pelo tipo da infração).</summary>
    public enum CheatItem { Nenhum, Celular, Colinha, Garrafinha }

    /// <summary>Regras puras das colas novas (Bloco 15): testáveis sem cena.</summary>
    public static class CheatRules
    {
        /// <summary>O que é confiscado num flagrante deste tipo. Celular tocando também é celular.</summary>
        public static CheatItem ItemTakenFor(Infracao kind) => kind switch
        {
            Infracao.Celular or Infracao.CelularTocando => CheatItem.Celular,
            Infracao.ColinhaNaManga => CheatItem.Colinha,
            Infracao.Garrafinha => CheatItem.Garrafinha,
            _ => CheatItem.Nenhum,
        };

        /// <summary>
        /// Suspeita ganha num passo de tempo pelo "sentido de perto" (colinha): perto o bastante, o professor percebe
        /// o movimento mesmo sem você estar no cone de visão. Longe, nada.
        /// </summary>
        public static float CloseRangeStep(float rate, float multiplier, float distance, float maxDistance, float dt) =>
            rate > 0f && distance <= maxDistance ? rate * multiplier * dt : 0f;

        /// <summary>
        /// S2: a colinha VISTA vira flagrante na hora? Só se a regra estiver ligada, ele já tiver percebido (tempo de
        /// reação completo), estiver vendo bem (perto/no centro do olhar) e não estiver distraído. De longe ou de relance,
        /// ela continua só somando suspeita.
        /// </summary>
        public static bool ColinhaCaughtNow(bool instantCatch, Infracao doing, float notice, float visibility, float minVisibility, bool distracted) =>
            instantCatch && doing == Infracao.ColinhaNaManga && notice >= 1f && visibility >= minVisibility && !distracted;

        /// <summary>S2: barulhos que deixam prova com você (o professor pode achá-la ao investigar).</summary>
        public static bool LeavesEvidence(Infracao kind) => kind == Infracao.Bilhete || kind == Infracao.CelularTocando;

        /// <summary>S2: chance de o professor achar a prova deste tipo ao investigar.</summary>
        public static float FindChance(Infracao kind, float bilheteChance, float foneChance) => kind switch
        {
            Infracao.Bilhete => bilheteChance,
            Infracao.CelularTocando => foneChance,
            _ => 0f,
        };

        /// <summary>Chance de o bilhete cair fora da mesa: cresce com a distância até o destino.</summary>
        public static float BilheteMissChance(float meters, float perMeter) => Mathf.Clamp01(meters * perMeter);

        /// <summary>Professor (fora do telefone) perto de você ou do destino: o bilhete se perde.</summary>
        public static bool BilheteLost(float profToPlayer, float profToTarget, float lostDistance, bool profOnPhone) =>
            !profOnPhone && Mathf.Min(profToPlayer, profToTarget) < lostDistance;

        /// <summary>
        /// Quando (segundos depois do pedido) o celular vibra enquanto você espera a resposta do fone, ou -1 se não vibrar.
        /// <paramref name="rollChance"/> decide se vibra; <paramref name="rollWhen"/> decide quando (entre 20% e 90% da espera).
        /// </summary>
        public static float FoneRingTime(double rollChance, double rollWhen, float chance, float delay) =>
            rollChance < chance ? delay * Mathf.Lerp(0.2f, 0.9f, (float)rollWhen) : -1f;

        /// <summary>
        /// O que aparece na mão do jogador agora (só visual). Pesquisar no celular (tecla 2) mostra o celular; usar o
        /// fone (F) mostra o fone de ouvido com fio; segurar Q/G mostra a colinha/garrafinha; jogando o bilhete, a bolinha.
        /// </summary>
        public static HandProp PropInHand(bool googling, PlayerCheats.Hold holding, bool throwing) =>
            throwing ? HandProp.Bilhete
            : googling ? HandProp.Celular
            : holding == PlayerCheats.Hold.FoneEnviar || holding == PlayerCheats.Hold.FoneLer ? HandProp.Fone
            : holding == PlayerCheats.Hold.Colinha ? HandProp.Colinha
            : holding == PlayerCheats.Hold.Garrafinha ? HandProp.Garrafinha
            : HandProp.Nenhum;
    }

    /// <summary>Uma resposta que chega depois (bilhete voltando, mensagem do fone). Pura e testável.</summary>
    public class DelayedAnswer
    {
        float left;
        public bool Waiting { get; private set; }
        public bool Arrived { get; private set; }
        public float SecondsLeft => Waiting ? Mathf.Max(0f, left) : 0f;
        public bool Busy => Waiting || Arrived;

        public void Begin(float delay)
        {
            Waiting = true;
            Arrived = false;
            left = delay;
        }

        /// <summary>Passa o tempo. true no passo em que a resposta chega.</summary>
        public bool Tick(float dt)
        {
            if (!Waiting) return false;
            left -= dt;
            if (left > 0f) return false;
            Waiting = false;
            Arrived = true;
            return true;
        }

        public void Clear() => Waiting = Arrived = false;
    }

    /// <summary>
    /// As 4 colas novas do Bloco 15, todas SENTADO e com tecla própria (livres: Q, B, F, G). Cada uma troca risco por custo:
    /// - Colinha (Q, ~1 s): rápida e quase invisível de longe, mas com o professor PERTO ele percebe mesmo sem olhar.
    ///   Só serve para questão em branco.
    /// - Bilhete (B, mirando no Nerd ou no Fundão): resposta volta depois de um atraso, numa questão em branco qualquer.
    ///   O papel faz barulho, pode cair fora da mesa e se perde com o professor perto.
    /// - Fone (F): pede a resposta a alguém de fora. Demora, e enquanto espera o celular pode VIBRAR (barulho alto).
    /// - Garrafinha (G): beber água é quase normal de ver, mas o gole é barulhento. Só serve para questão em branco.
    /// Todas gastam do orçamento de colas da partida (<see cref="CheatBudget"/>). Os números vêm de DifficultySettings.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerCheats : MonoBehaviour
    {
        public enum Hold { Nenhum, Colinha, Garrafinha, FoneEnviar, FoneLer }

        DifficultySettings s = new();
        PlayerController player;

        public int ColinhaCharges { get; private set; }
        public int BilheteCharges { get; private set; }
        public int FoneCharges { get; private set; }
        public int GarrafaCharges { get; private set; }
        public bool HasColinha { get; private set; } = true;
        public bool HasGarrafa { get; private set; } = true;

        /// <summary>O que está segurando agora (barra de progresso).</summary>
        public Hold Holding { get; private set; }
        public float HoldProgress { get; private set; }

        /// <summary>Bilhete a caminho de volta (ou -1 se nenhum).</summary>
        public readonly DelayedAnswer bilhete = new();
        /// <summary>Resposta do contato de fora (fone).</summary>
        public readonly DelayedAnswer fone = new();
        string bilheteFrom = "";
        bool bilheteGood;
        float throwLeft, foneRingAt = -1f, ringLeft, ringSoundTimer, foneElapsed;
        StudentBase throwTarget;

        /// <summary>Celular vibrando agora (barulho; se o professor vê, é flagrante "celular tocando").</summary>
        public bool Ringing => ringLeft > 0f;
        public bool Throwing => throwLeft > 0f;

        // --- mira do bilhete: B arma (bolinha na mão, linha até o alvo), B de novo joga; Espaço ou outra cola cancela
        /// <summary>Quanto tempo do arremesso a bolinha ainda fica na mão antes de sair voando (s).</summary>
        public const float ReleaseTime = 0.12f;
        /// <summary>Diâmetro da bolinha de papel (m): maior que a real, para dar para ver de longe.</summary>
        public const float BallSize = 0.09f;
        bool aiming;
        int aimEndedFrame = -1;
        float releaseLeft;
        BilheteMira mira;
        PaperBallFlight flight;
        /// <summary>Mirando o bilhete agora.</summary>
        public bool Aiming => aiming;
        /// <summary>A bolinha está na mão (mirando, ou no começo do arremesso, antes de soltar).</summary>
        public bool BallInHand => aiming || (Throwing && releaseLeft > 0f);
        /// <summary>Espaço neste quadro é "cancelar a mira", não "levantar".</summary>
        public bool AimBlocksStandUp => aiming || aimEndedFrame == Time.frameCount;
        /// <summary>A mira (visual) e a bolinha voando agora (testes).</summary>
        public BilheteMira Mira => mira;
        public PaperBallFlight Flight => flight;
        /// <summary>Nome de quem está na mira agora ("" se ninguém).</summary>
        public string AimTargetName { get; private set; } = "";

        // --- o que o PlayerController repassa para o professor
        public float CurrentRate { get; private set; }
        public string CurrentReason { get; private set; } = "";
        public Infracao CurrentKind { get; private set; }
        /// <summary>Suspeita/s pelo "sentido de perto" (só a colinha) e até onde ele vale.</summary>
        public float CloseRangeRate => Holding == Hold.Colinha ? s.colinhaNearSuspicion : 0f;
        public float CloseRangeDistance => s.colinhaDangerDistance;

        void Awake() => player = GetComponent<PlayerController>();

        public void ApplySettings(DifficultySettings settings)
        {
            s = settings;
            ColinhaCharges = s.colinhaCharges;
            BilheteCharges = s.bilheteCharges;
            FoneCharges = s.foneCharges;
            GarrafaCharges = s.garrafaCharges;
            HasColinha = HasGarrafa = true;
            Holding = Hold.Nenhum;
            HoldProgress = 0f;
            bilhete.Clear();
            fone.Clear();
            throwLeft = ringLeft = 0f;
            foneRingAt = -1f;
            StopAim();
            releaseLeft = 0f;
            throwingEraser = aimingAtFavor = false;
            throwTarget = null;
            if (flight != null) { Destroy(flight.gameObject); flight = null; }
        }

        static bool Playing => GameManager.I != null && GameManager.I.IsPlaying;
        static CheatBudget Budget => CheatBudget.Current;

        void Update()
        {
            if (!Playing)
            {
                if (GameManager.I == null || GameManager.I.Phase != Phase.Paused) { HUD.I?.SetCheats(null); StopAim(); }
                CurrentRate = 0f;
                return;
            }
            float dt = Time.deltaTime;
            UpdateBilhete(dt);
            UpdateFone(dt);

            bool quizOpen = QuizChallenge.I != null && QuizChallenge.I.Active;
            bool canAct = player.IsSeated && !player.InputLocked && !quizOpen && !player.UsingPhone;
            if (!canAct) { CancelHold(); StopAim(); }
            else HandleInput(dt);
            UpdateMira();

            UpdateRisk();
            HUD.I.SetCheats(PanelText());
        }

        // ------------------------------------------------------------------ entrada (S7: teclado ou controle, pelo Controls)

        void HandleInput(float dt)
        {
            if (aiming) { HandleAim(); return; }
            // segurando alguma: só aquela tecla conta (sem duas colas ao mesmo tempo)
            if (Holding != Hold.Nenhum)
            {
                if (!Controls.Held(ActionFor(Holding))) { CancelHold(); return; }
                HoldProgress += dt / HoldTime(Holding);
                HUD.I.SetHold(HoldProgress, HoldLabel(Holding), RiskNow());
                if (HoldProgress >= 1f) FinishHold();
                return;
            }
            if (Throwing) return;

            if (Controls.Pressed(Acao.Colinha)) TryStart(Hold.Colinha);
            else if (Controls.Pressed(Acao.Garrafinha)) TryStart(Hold.Garrafinha);
            else if (Controls.Pressed(Acao.Fone)) PressFone();
            else if (Controls.Pressed(Acao.Bilhete)) StartAim();
        }

        /// <summary>Por que não dá para jogar um bilhete agora, sem contar a mira (null = dá).</summary>
        string WhyNotThrow() =>
            BilheteCharges <= 0 ? Falas.BilheteAcabou
            : bilhete.Busy ? Falas.BilheteEsperando
            : !ExamSystem.I.HasBlank ? Falas.SoEmBranco
            : !CheatBudget.CanAnswer ? Falas.OrcamentoAcabou : null;

        /// <summary>Alvo do bilhete (Nerd ou Fundão).</summary>
        public static bool IsNoteTarget(Interactable it) => it is NerdStudent || it is BackRowStudent;
        /// <summary>Alvo da borracha, que gasta um favor (Dorminhoco ou Puxa-saco).</summary>
        public static bool IsFavorTarget(Interactable it) => it is SleeperStudent || it is SnitchStudent;
        /// <summary>Ainda tem favor para gastar.</summary>
        static bool HasFavorLeft => GameManager.I != null && GameManager.I.Favors > 0;

        /// <summary>Este colega pode ser alvo agora: bilhete possível (Nerd/Fundão) ou favor possível (Dorminhoco/Puxa-saco).</summary>
        bool ValidTarget(Interactable it) =>
            it != null && it.isActiveAndEnabled
            && (IsNoteTarget(it) ? WhyNotThrow() == null
                : IsFavorTarget(it) && HasFavorLeft && it.CanInteract(player));

        /// <summary>
        /// B: arma o arremesso, se der para jogar alguma coisa (bilhete ou borracha). A mira aparece e o braço sobe;
        /// quem estiver na mira decide o que vai: bilhete no Nerd/Fundão, borracha no Dorminhoco/Puxa-saco.
        /// </summary>
        public bool StartAim()
        {
            if (aiming || Throwing) return false;
            string no = WhyNotThrow();
            if (no != null && !HasFavorLeft) { HUD.I.Toast(BilheteCharges > 0 ? no : Falas.MiraNada, HUD.Gray, 2.5f); return false; }
            aiming = true;
            SoundFX.PlayAt(Sfx.Papel, transform.position, 0.35f, 1.4f); // amassando a bolinha
            return true;
        }

        /// <summary>O que vai na mão mirando ou jogando: borracha (favor) ou a bolinha do bilhete.</summary>
        public HandProp ThrowProp =>
            (Throwing ? throwingEraser : aimingAtFavor) ? HandProp.Borracha : HandProp.Bilhete;
        /// <summary>Mirando no Dorminhoco/Puxa-saco agora (vai a borracha).</summary>
        public bool AimingAtFavor => aiming && aimingAtFavor;
        bool aimingAtFavor, throwingEraser;

        /// <summary>Sai da mira sem jogar (a bolinha volta para o bolso; nada gasto).</summary>
        public void StopAim()
        {
            if (!aiming) return;
            aiming = false;
            aimEndedFrame = Time.frameCount;
            AimTargetName = "";
            if (mira != null) mira.Hide();
        }

        /// <summary>Mirando: B de novo joga (se tiver alvo); Espaço, outra cola ou responder cancela.</summary>
        void HandleAim()
        {
            if (Controls.Pressed(Acao.Bilhete)) { ConfirmAim(AimTarget()); return; }
            if (Controls.Pressed(Acao.Levantar) || Controls.Pressed(Acao.Colinha) || Controls.Pressed(Acao.Garrafinha)
                || Controls.Pressed(Acao.Fone) || Controls.Pressed(Acao.Responder))
                StopAim();
        }

        /// <summary>
        /// Mirando, apertou B de novo: sem alvo, avisa e continua mirando; com alvo, joga.
        /// true = jogou.
        /// </summary>
        public bool ConfirmAim(StudentBase target)
        {
            if (!aiming) return false;
            if (target == null) { HUD.I.Toast(Falas.MiraSemAlvo, HUD.Gray, 2f); return false; }
            StopAim();
            return IsFavorTarget(target) ? ThrowEraserAt(target) : ThrowAt(target);
        }

        /// <summary>
        /// Joga a borracha no Dorminhoco ou no Puxa-saco: quando acerta, é como pedir o favor de perto (ele acorda
        /// gritando / chama o professor) e gasta um favor — só se a distração funcionar, igual ao pedido de perto.
        /// false = não deu para jogar (motivo no toast).
        /// </summary>
        public bool ThrowEraserAt(StudentBase target)
        {
            if (Throwing) return false;
            if (!IsFavorTarget(target)) return false;
            if (!HasFavorLeft) { HUD.I.Toast(Falas.MiraNada, HUD.Gray, 2.5f); return false; }
            if (!target.CanInteract(player)) { HUD.I.Toast(Falas.MiraSemAlvo, HUD.Gray, 2f); return false; }
            throwTarget = target;
            throwingEraser = true;
            throwLeft = Mathf.Max(0.05f, s.borrachaThrowTime);
            releaseLeft = Mathf.Min(ReleaseTime, throwLeft * 0.5f);
            SoundFX.PlayAt(Sfx.Clique, transform.position, 0.3f, 0.8f);
            return true;
        }

        /// <summary>A borracha chegou: acertou a cabeça do colega. Se ele ainda "topa" (dormindo / sem vigiar), faz o favor.</summary>
        void LandEraser(StudentBase t)
        {
            EndFlight(PaperBallFlight.End.Chao); // quica na cabeça e cai
            if (t != null && t.CanInteract(player)) t.Interact(player); // o favor (gasta só se o professor for ver)
            else HUD.I.Toast(Falas.BorrachaNaoAdiantou, HUD.Gray, 2.5f);
        }

        /// <summary>Onde a borracha acerta: a cabeça do colega.</summary>
        public static Vector3 HeadHitPoint(StudentBase t) =>
            t.visual != null ? t.visual.HeadPosition + Vector3.up * 0.05f : t.transform.position + Vector3.up * 1.3f;

        /// <summary>Onde a bolinha cai na carteira do colega: em cima da folha dele (ou na frente dele, sem folha).</summary>
        public static Vector3 LandingPoint(StudentBase t)
        {
            var paper = t != null && t.visual != null && t.visual.Skin != null ? t.visual.Skin.DeskPaper : null;
            if (paper != null) return paper.position + Vector3.up * (BallSize * 0.5f + 0.005f);
            return t.transform.position + t.transform.forward * 0.45f + Vector3.up * 1.05f;
        }

        /// <summary>De onde a bolinha sai: a bolinha na mão (ou um pouco acima do ombro, no boneco simples).</summary>
        Vector3 HandPoint()
        {
            var skin = player.visual != null ? player.visual.Skin : null;
            if (skin != null && skin.PropObject != null) return skin.PropObject.position;
            return player.HeadPosition + transform.right * 0.25f + Vector3.up * 0.15f;
        }

        /// <summary>Desenha a mira enquanto arma o arremesso.</summary>
        void UpdateMira()
        {
            if (!aiming) { if (mira != null) mira.Hide(); return; }
            if (mira == null)
            {
                var go = new GameObject("Mira do Bilhete");
                mira = go.AddComponent<BilheteMira>();
            }
            var t = AimTarget();
            AimTargetName = t != null ? t.displayName : "";
            aimingAtFavor = t != null ? IsFavorTarget(t) : WhyNotThrow() != null; // sem alvo: o que daria para jogar
            Vector3 from = HandPoint(), to;
            if (t != null) to = aimingAtFavor ? HeadHitPoint(t) : LandingPoint(t);
            else
            {
                // sem alvo: mostra onde cairia (no chão, 3 m à frente da câmera), em vermelho
                var cam = Camera.main;
                Vector3 look = cam != null ? cam.transform.forward : transform.forward;
                look.y = 0f;
                to = transform.position + look.normalized * 3f + Vector3.up * 0.02f;
            }
            mira.Show(from, to, t != null, t != null && aimingAtFavor ? BilheteMira.Favor : BilheteMira.Valid);
        }

        /// <summary>Solta a bolinha da mão: ela voa em arco até a carteira do alvo, no tempo que falta do arremesso.</summary>
        void LaunchFlight()
        {
            if (throwTarget == null) return;
            if (flight != null) Destroy(flight.gameObject);
            GameObject go;
            if (throwingEraser)
            {
                go = HandProps.BuildEraser(null).gameObject;
                go.name = "Borracha Voando";
                go.transform.localScale = Vector3.one * CharacterModelSkin.PropScale; // do tamanho que estava na mão
            }
            else go = PaperBall.Create(null, BallSize, "Bilhete Voando");
            flight = go.AddComponent<PaperBallFlight>();
            flight.Launch(HandPoint(), throwingEraser ? HeadHitPoint(throwTarget) : LandingPoint(throwTarget), throwLeft);
        }

        /// <summary>O que acontece com a bolinha que estava voando (só visual).</summary>
        void EndFlight(PaperBallFlight.End how)
        {
            if (flight != null) flight.Finish(how);
            flight = null;
        }

        static Acao ActionFor(Hold h) =>
            h == Hold.Colinha ? Acao.Colinha : h == Hold.Garrafinha ? Acao.Garrafinha : Acao.Fone;

        float HoldTime(Hold h) => Mathf.Max(0.05f, h switch
        {
            Hold.Colinha => s.colinhaTime,
            Hold.Garrafinha => s.garrafaTime,
            _ => s.foneUseTime,
        });

        static string HoldLabel(Hold h) => h switch
        {
            Hold.Colinha => Falas.SegurandoColinha,
            Hold.Garrafinha => Falas.SegurandoGarrafinha,
            Hold.FoneEnviar => Falas.SegurandoFoneEnviar,
            _ => Falas.SegurandoFoneLer,
        };

        /// <summary>Por que esta cola não pode começar agora (null = pode).</summary>
        public string WhyNot(Hold h)
        {
            var exam = ExamSystem.I;
            switch (h)
            {
                case Hold.Colinha:
                    if (!HasColinha) return Falas.ColinhaConfiscada;
                    if (ColinhaCharges <= 0) return Falas.ColinhaAcabou;
                    if (!exam.HasBlank) return Falas.SoEmBranco;
                    break;
                case Hold.Garrafinha:
                    if (!HasGarrafa) return Falas.GarrafaConfiscada;
                    if (GarrafaCharges <= 0) return Falas.GarrafaAcabou;
                    if (!exam.HasBlank) return Falas.SoEmBranco;
                    break;
                case Hold.FoneEnviar:
                    if (!player.HasPhone) return Falas.FoneSemCelular;
                    if (FoneCharges <= 0) return Falas.FoneAcabou;
                    if (!exam.HasRoom) return Falas.NadaACorrigir;
                    break;
                case Hold.FoneLer:
                    if (!player.HasPhone) return Falas.FoneSemCelular;
                    break;
            }
            if (h != Hold.FoneLer && !CheatBudget.CanAnswer) return Falas.OrcamentoAcabou;
            return null;
        }

        bool TryStart(Hold h)
        {
            string no = WhyNot(h);
            if (no != null)
            {
                HUD.I.Toast(no, HUD.Gray, 2.5f);
                return false;
            }
            Holding = h;
            HoldProgress = 0f;
            return true;
        }

        void PressFone()
        {
            if (Ringing)
            {
                // atender/silenciar: o barulho para, mas quem viu, viu
                ringLeft = 0f;
                player.Say(Falas.Pick(Falas.JogadorSilenciaCelular), 2f);
                return;
            }
            if (fone.Arrived) { TryStart(Hold.FoneLer); return; }
            if (fone.Waiting)
            {
                HUD.I.Toast(Falas.FoneAindaNao + " (" + Mathf.CeilToInt(fone.SecondsLeft) + " s)", HUD.Gray, 2f);
                return;
            }
            TryStart(Hold.FoneEnviar);
        }

        void CancelHold()
        {
            if (Holding == Hold.Nenhum) return;
            Holding = Hold.Nenhum;
            HoldProgress = 0f;
            HUD.I?.SetHold(-1f, "");
        }

        void FinishHold()
        {
            var h = Holding;
            Holding = Hold.Nenhum;
            HoldProgress = 0f;
            HUD.I.SetHold(-1f, "");
            var exam = ExamSystem.I;
            switch (h)
            {
                case Hold.Colinha:
                    ColinhaCharges--;
                    MatchLog.ColinhaUsos++;
                    if (CheatBudget.Spend() && exam.MarkCorrectBlank() >= 0) { MatchLog.ColinhaCertas++; Gain(Falas.ColinhaFuncionou); }
                    else MatchLog.ColinhaErradas++;
                    break;
                case Hold.Garrafinha:
                    GarrafaCharges--;
                    // o gole é o preço: barulho que o professor ouve (ele sabe o LUGAR, não quem foi)
                    player.MakeNoise(s.garrafaNoiseRadius, s.garrafaNoise, Falas.RuidoGole);
                    SoundFX.PlayAt(Sfx.Gole, transform.position);
                    MatchLog.GarrafaUsos++;
                    if (CheatBudget.Spend() && exam.MarkCorrectBlank() >= 0) { MatchLog.GarrafaCertas++; Gain(Falas.GarrafaFuncionou); }
                    else MatchLog.GarrafaErradas++;
                    break;
                case Hold.FoneEnviar:
                    FoneCharges--;
                    if (Budget != null && !Budget.TryReserve()) { HUD.I.Toast(Falas.OrcamentoAcabou, HUD.Gray, 2.5f); break; }
                    MatchLog.FoneUsos++;
                    fone.Begin(s.foneDelay);
                    foneElapsed = 0f;
                    foneRingAt = CheatRules.FoneRingTime(Random.value, Random.value, s.foneRingChance, s.foneDelay);
                    HUD.I.Toast(Falas.FoneEnviado, HUD.Yellow, 3f);
                    break;
                case Hold.FoneLer:
                    fone.Clear();
                    Budget?.Commit();
                    if (exam.MarkCorrect() >= 0) { MatchLog.FoneCertas++; Gain(Falas.FoneFuncionou); }
                    else MatchLog.FoneErradas++;
                    break;
            }
        }

        void Gain(string text)
        {
            SoundFX.Play(Sfx.Ponto);
            HUD.I.Toast("+" + GameManager.Fmt(ExamSystem.I.PointsPerQuestion) + " — " + text, HUD.Green, 2.5f);
        }

        // ------------------------------------------------------------------ bilhete

        /// <summary>Colega para quem o bilhete vai: o Nerd ou o Fundão mais perto da direção da câmera (dentro da mira).</summary>
        public StudentBase AimTarget()
        {
            var cam = Camera.main;
            Vector3 look = cam != null ? cam.transform.forward : transform.forward;
            look.y = 0f;
            StudentBase best = null;
            float bestAng = s.bilheteAimAngle;
            foreach (var it in Interactable.All)
            {
                // bilhete: Nerd/Fundão (se der para jogar bilhete); borracha: Dorminhoco/Puxa-saco (se o favor serve agora)
                if (!ValidTarget(it)) continue;
                Vector3 to = it.transform.position - transform.position;
                to.y = 0f;
                float ang = Vector3.Angle(look, to);
                if (ang <= bestAng) { bestAng = ang; best = (StudentBase)it; }
            }
            return best;
        }

        bool TryThrow() => ThrowAt(AimTarget());

        /// <summary>Joga o bilhete para <paramref name="target"/> (Nerd ou Fundão). false = não deu para jogar (motivo no toast).</summary>
        public bool ThrowAt(StudentBase target)
        {
            if (Throwing) return false;
            string no = BilheteCharges <= 0 ? Falas.BilheteAcabou
                      : bilhete.Busy ? Falas.BilheteEsperando
                      : !ExamSystem.I.HasBlank ? Falas.SoEmBranco
                      : !CheatBudget.CanAnswer ? Falas.OrcamentoAcabou
                      : target is not NerdStudent && target is not BackRowStudent ? Falas.BilheteSemMira : null;
            if (no != null)
            {
                HUD.I.Toast(no, HUD.Gray, 2.5f);
                return false;
            }
            Budget?.TryReserve();
            BilheteCharges--;
            MatchLog.BilheteUsos++;
            throwTarget = target;
            throwingEraser = false;
            throwLeft = Mathf.Max(0.05f, s.bilheteThrowTime);
            releaseLeft = Mathf.Min(ReleaseTime, throwLeft * 0.5f); // a bolinha sai da mão logo depois do braço descer
            player.MakeNoise(s.bilheteNoiseRadius, s.bilheteNoise, Falas.RuidoPapel, Infracao.Bilhete);
            SoundFX.PlayAt(Sfx.Papel, transform.position);
            return true;
        }

        void UpdateBilhete(float dt)
        {
            if (throwLeft > 0f)
            {
                if (releaseLeft > 0f)
                {
                    releaseLeft -= dt;
                    if (releaseLeft <= 0f) LaunchFlight();
                }
                throwLeft -= dt;
                if (throwLeft <= 0f) Land();
            }
            if (bilhete.Tick(dt)) DeliverBilhete();
        }

        /// <summary>O bilhete chegou (ou não) na mesa do colega.</summary>
        void Land()
        {
            var t = throwTarget;
            throwTarget = null;
            if (throwingEraser) { throwingEraser = false; LandEraser(t); return; } // borracha: não é bilhete
            if (t == null) { EndFlight(PaperBallFlight.End.Chao); Lose(Falas.BilheteSumiu); return; }
            var prof = ProfessorAI.I;
            float dist = Flat.Distance(transform.position, t.transform.position);
            if (prof != null && CheatRules.BilheteLost(Flat.Distance(prof.transform.position, transform.position),
                                                       Flat.Distance(prof.transform.position, t.transform.position),
                                                       s.bilheteLostDistance, prof.OnPhone))
            {
                // ele estava perto: o papel some no bolso dele. Se ainda por cima te viu jogando, é flagrante.
                EndFlight(PaperBallFlight.End.Some);
                if (prof.Visibility > 0f && prof.CatchRedHanded(Falas.MotivoBilhete, Infracao.Bilhete)) { Budget?.Cancel(); MatchLog.BilheteErradas++; return; }
                Lose(Falas.BilheteInterceptado);
                return;
            }
            if (Random.value < CheatRules.BilheteMissChance(dist, s.bilheteMissChancePerMeter)) { EndFlight(PaperBallFlight.End.Chao); Lose(Falas.BilheteErrouAMesa); return; }

            if (t is NerdStudent nerd)
            {
                if (!nerd.AcceptsNote) { EndFlight(PaperBallFlight.End.Chao); nerd.RefuseNote(); Lose(Falas.BilheteNerdRecusou); return; }
                nerd.ReceiveNote();
                bilheteGood = true;
                bilheteFrom = "o Nerd";
            }
            else
            {
                var fundao = (BackRowStudent)t;
                fundao.ReceiveNote();
                bilheteGood = Random.value < s.backRowGoodColaChance;
                bilheteFrom = "o Fundão";
            }
            EndFlight(PaperBallFlight.End.Mesa); // chegou: a bolinha pousa na carteira do colega
            bilhete.Begin(s.bilheteDelay);
        }

        /// <summary>O bilhete se perdeu no caminho (só o bilhete usa).</summary>
        void Lose(string why)
        {
            Budget?.Cancel();
            MatchLog.BilheteErradas++;
            HUD.I.Toast(why, HUD.Orange, 3f);
        }

        void DeliverBilhete()
        {
            bilhete.Clear();
            var exam = ExamSystem.I;
            if (!bilheteGood)
            {
                Budget?.Cancel();
                MatchLog.BilheteErradas++;
                HUD.I.Toast(Falas.BilheteRuim, HUD.Red, 3f);
                SoundFX.Play(Sfx.PerdePonto);
                return;
            }
            // S2: a resposta vem para uma questão que você NÃO sabe (antes podia ser uma que você responderia sozinho)
            int i = exam.MarkCorrectRandomBlankPreferUnknown(Random.value);
            if (i < 0) { Budget?.Cancel(); MatchLog.BilheteErradas++; HUD.I.Toast(Falas.BilheteSemLugar, HUD.Gray, 3f); return; }
            Budget?.Commit();
            MatchLog.BilheteCertas++;
            Gain("bilhete de " + bilheteFrom + ": questão " + (i + 1) + "!");
        }

        // ------------------------------------------------------------------ fone

        void UpdateFone(float dt)
        {
            if (ringLeft > 0f)
            {
                ringLeft -= dt;
                ringSoundTimer -= dt;
                if (ringSoundTimer <= 0f)
                {
                    ringSoundTimer = 0.6f;
                    SoundFX.PlayAt(Sfx.Vibra, transform.position);
                }
            }
            if (!fone.Busy) return;
            // celular confiscado no meio da espera: a resposta não chega mais
            if (!player.HasPhone)
            {
                fone.Clear();
                ringLeft = 0f;
                Budget?.Cancel();
                MatchLog.FoneErradas++;
                HUD.I.Toast(Falas.FonePerdido, HUD.Orange, 3f);
                return;
            }
            if (fone.Waiting)
            {
                foneElapsed += dt;
                if (foneRingAt >= 0f && foneElapsed >= foneRingAt)
                {
                    foneRingAt = -1f;
                    Ring();
                }
            }
            if (fone.Tick(dt)) HUD.I.Toast(Falas.FoneChegou, HUD.Yellow, 3f);
        }

        /// <summary>O celular vibra: barulho alto na sua carteira por alguns segundos (aperte F para silenciar).</summary>
        public void Ring()
        {
            ringLeft = s.foneRingSeconds;
            ringSoundTimer = 0f;
            player.MakeNoise(s.foneRingRadius, s.foneRingNoise, Falas.RuidoVibra, Infracao.CelularTocando);
        }

        // ------------------------------------------------------------------ suspeita e flagrante

        void UpdateRisk()
        {
            CurrentRate = 0f;
            CurrentReason = "";
            CurrentKind = Infracao.Nenhuma;
            void Use(float rate, string reason, Infracao kind)
            {
                if (rate <= CurrentRate) return;
                CurrentRate = rate; CurrentReason = reason; CurrentKind = kind;
            }
            switch (Holding)
            {
                case Hold.Colinha: Use(s.colinhaSuspicion, Falas.MotivoColinha, Infracao.ColinhaNaManga); break;
                case Hold.Garrafinha: Use(s.garrafaSuspicion, Falas.MotivoGarrafinha, Infracao.Garrafinha); break;
                case Hold.FoneEnviar:
                case Hold.FoneLer: Use(player.phoneSuspicion, Falas.MotivoFone, Infracao.Celular); break;
            }
            if (Throwing && !throwingEraser) Use(s.bilheteThrowSuspicion, Falas.MotivoBilhete, Infracao.Bilhete);
            // borracha: jogar coisa nos colegas é bagunça, não cola (Infracao.Outra; não confisca nada)
            if (Throwing && throwingEraser) Use(s.borrachaThrowSuspicion, Falas.MotivoBorracha, Infracao.Outra);
            // mirando: o bilhete é cola; a borracha (favor) é bagunça
            if (aiming && !aimingAtFavor) Use(s.bilheteAimSuspicion, Falas.MotivoMirandoBilhete, Infracao.Bilhete);
            if (aiming && aimingAtFavor) Use(s.bilheteAimSuspicion, Falas.MotivoBorracha, Infracao.Outra);
            if (Ringing) Use(s.foneRingSuspicion, Falas.MotivoCelularTocando, Infracao.CelularTocando);
        }

        /// <summary>Cor da barra: vermelho se ele vê (ou, na colinha, se está perto), verde se não.</summary>
        HUD.HoldRisk RiskNow()
        {
            var prof = ProfessorAI.I;
            if (prof == null) return HUD.HoldRisk.Escondido;
            bool near = Holding == Hold.Colinha && !prof.OnPhone
                        && Flat.Distance(prof.transform.position, transform.position) <= s.colinhaDangerDistance;
            return prof.Visibility > 0f || near ? HUD.HoldRisk.Visto : HUD.HoldRisk.Escondido;
        }

        /// <summary>Flagrante (já sem a advertência): leva a colinha / a garrafinha, e interrompe o que estava fazendo.</summary>
        public void OnFlagrante(Infracao kind, CheatItem taken)
        {
            // registro (Bloco 15b): cola interrompida pelo flagrante conta como usada e errada
            if (Holding == Hold.Colinha) { MatchLog.ColinhaUsos++; MatchLog.ColinhaErradas++; }
            else if (Holding == Hold.Garrafinha) { MatchLog.GarrafaUsos++; MatchLog.GarrafaErradas++; }
            if (throwLeft > 0f && !throwingEraser)
            {
                // o bilhete no ar não chega (BilheteUsos já contou ao jogar). O Land() não vai mais rodar,
                // então a vaga reservada do orçamento volta aqui (antes ficava presa até o fim da prova)
                MatchLog.BilheteErradas++;
                Budget?.Cancel();
            }
            throwTarget = null;
            throwingEraser = false; // borracha no ar: não chega, e o favor não é gasto
            EndFlight(PaperBallFlight.End.Some); // o professor pegou a bolinha
            StopAim();                           // e a que estava na mão volta para o bolso
            releaseLeft = 0f;
            CancelHold();
            throwLeft = 0f;
            ringLeft = 0f;
            if (taken == CheatItem.Colinha && HasColinha)
            {
                HasColinha = false;
                HUD.I.Toast(Falas.ColinhaConfiscadaAgora, HUD.Red, 3.5f, important: true);
            }
            if (taken == CheatItem.Garrafinha && HasGarrafa)
            {
                HasGarrafa = false;
                HUD.I.Toast(Falas.GarrafaConfiscadaAgora, HUD.Red, 3.5f, important: true);
            }
        }

        // ------------------------------------------------------------------ HUD

        /// <summary>Texto do painel discreto das colas (o HUD só mostra; quem sabe o estado é este componente).</summary>
        public string PanelText()
        {
            var b = Budget;
            string head = "COLAS" + (b != null ? "  •  restam " + b.Available + " resposta" + (b.Available == 1 ? "" : "s") : "");
            // S7: a tecla de cada cola é a do dispositivo em uso ([Q] no teclado, [LB] no controle)
            string q = Controls.Tag(Acao.Colinha), g = Controls.Tag(Acao.Garrafinha), bt = Controls.Tag(Acao.Bilhete), f = Controls.Tag(Acao.Fone);
            string colinha = !HasColinha ? q + " Colinha: confiscada" : q + " Colinha ×" + ColinhaCharges;
            string garrafa = !HasGarrafa ? g + " Garrafinha: confiscada" : g + " Garrafinha ×" + GarrafaCharges;
            string bil = bilhete.Waiting ? bt + " Bilhete voltando... " + Mathf.CeilToInt(bilhete.SecondsLeft) + " s"
                       : bt + " Bilhete ×" + BilheteCharges + " (mire)";
            string fon = Ringing ? f + " VIBRANDO! (" + Controls.Label(Acao.Fone) + " silencia)"
                       : fone.Arrived ? f + " Resposta chegou! (segure " + Controls.Label(Acao.Fone) + ")"
                       : fone.Waiting ? f + " Esperando resposta... " + Mathf.CeilToInt(fone.SecondsLeft) + " s"
                       : !player.HasPhone ? f + " Fone: sem celular"
                       : f + " Fone ×" + FoneCharges;
            return head + "\n" + colinha + "\n" + garrafa + "\n" + bil + "\n" + fon;
        }
    }
}
