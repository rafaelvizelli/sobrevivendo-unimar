using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using static ProvaImpossivel.Tests.FluxoEProfessorTests;

namespace ProvaImpossivel.Tests
{
    /// <summary>Bloco 15 — as colas novas na cena real, com teclado virtual (Q, G, F) e o bilhete jogado no Nerd.</summary>
    public class ColasNovasPlayTests
    {
        static readonly MethodInfo FinishPhone = typeof(PlayerController).GetMethod("FinishPhoneConsult", BindingFlags.NonPublic | BindingFlags.Instance);

        Keyboard kb;
        InputSettings.BackgroundBehavior bgAntes;
#if UNITY_EDITOR
        InputSettings.EditorInputBehaviorInPlayMode editorAntes;
#endif
        readonly List<NoiseInfo> ruidos = new();
        void Ouvir(NoiseInfo n) => ruidos.Add(n);

        [SetUp]
        public void SetUp()
        {
            var settings = InputSystem.settings;
            bgAntes = settings.backgroundBehavior;
#if UNITY_EDITOR
            editorAntes = settings.editorInputBehaviorInPlayMode;
            settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            kb = InputSystem.AddDevice<Keyboard>("TecladoColasNovas");
            kb.MakeCurrent();
            ruidos.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            Noise.Emitted -= Ouvir;
            if (kb != null) InputSystem.RemoveDevice(kb);
            InputSystem.settings.backgroundBehavior = bgAntes;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode = editorAntes;
#endif
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
        }

        /// <summary>Carrega a cena, ajusta a dificuldade (só nesta cena: ela é recarregada a cada teste) e começa a prova.</summary>
        IEnumerator Comecar(Difficulty d, Action<DifficultySettings> ajuste = null)
        {
            yield return EnterGame();
            var gm = GameManager.I;
            ajuste?.Invoke(d == Difficulty.Facil ? gm.facil : d == Difficulty.Dificil ? gm.dificil : gm.normal);
            gm.Begin(d);
            Noise.Emitted += Ouvir;
            yield return Wait(0.2f);
        }

        IEnumerator Segurar(Key k, Func<bool> ate, float timeout, string falha)
        {
            InputSystem.QueueStateEvent(kb, new KeyboardState(k));
            yield return WaitUntil(ate, timeout, falha + " (input virtual: tecla " + k + ")");
            InputSystem.QueueStateEvent(kb, new KeyboardState());
            yield return null;
        }

        static int RespostasCertas => ExamSystem.I.Count(QuestionState.Certa);

        // ------------------------------------------------------------------ colinha

        [UnityTest]
        public IEnumerator ColinhaComQ_DaUmaRespostaEmBrancoEGastaCargaEOrcamento()
        {
            yield return Comecar(Difficulty.Normal);
            var c = PlayerController.I.Cheats;
            var gm = GameManager.I;
            int cargas = c.ColinhaCharges;
            Assert.AreEqual(gm.normal.colinhaCharges, cargas, "a dificuldade não chegou na colinha");
            yield return Segurar(Key.Q, () => RespostasCertas == 1, 4f, "segurar Q não deu a resposta da colinha");
            Assert.AreEqual(cargas - 1, c.ColinhaCharges);
            Assert.AreEqual(1, gm.Cheats.Used, "a colinha não gastou o orçamento");
            int i = Array.IndexOf(ExamSystem.I.Questions, QuestionState.Certa);
            Assert.IsFalse(ExamSystem.I.Known[i], "a colinha deveria ir numa questão que ele NÃO sabe");
            Assert.AreEqual(PlayerCheats.Hold.Nenhum, c.Holding);
            Assert.AreEqual((1, 1, 0), (MatchLog.ColinhaUsos, MatchLog.ColinhaCertas, MatchLog.ColinhaErradas), "registro no partidas.csv (Bloco 15b)");
        }

        /// <summary>Bloco 15b: colinha interrompida por flagrante entra no partidas.csv como usada e errada.</summary>
        [UnityTest]
        public IEnumerator ColinhaInterrompidaPorFlagranteContaComoErrada()
        {
            yield return Comecar(Difficulty.Normal);
            var c = PlayerController.I.Cheats;
            InputSystem.QueueStateEvent(kb, new KeyboardState(Key.Q));
            yield return WaitUntil(() => c.Holding == PlayerCheats.Hold.Colinha, 2f, "Q não começou a colinha");
            GameManager.I.ApplyFlagrante("teste", Infracao.ColinhaNaManga);
            InputSystem.QueueStateEvent(kb, new KeyboardState());
            yield return null;
            Assert.AreEqual((1, 0, 1), (MatchLog.ColinhaUsos, MatchLog.ColinhaCertas, MatchLog.ColinhaErradas));
            Assert.AreEqual(0, RespostasCertas);
        }

        [UnityTest]
        public IEnumerator ColinhaComOProfessorPertoSobeSuspeitaSemEleVer()
        {
            yield return Comecar(Difficulty.Normal);
            var prof = ProfessorAI.I;
            var p = PlayerController.I.transform;
            // professor num corredor ao lado da carteira (a menos da distância de perigo), de costas para o jogador
            float perigo = GameManager.I.normal.colinhaDangerDistance;
            Vector3 lugar = Vector3.zero;
            bool achou = false;
            for (int ang = 0; ang < 360 && !achou; ang += 30)
            {
                Vector3 tentativa = p.position + Quaternion.Euler(0f, ang, 0f) * Vector3.forward * 2f;
                if (NavMesh.SamplePosition(tentativa, out var hit, 1f, NavMesh.AllAreas)
                    && Flat.Distance(hit.position, p.position) is > 0.9f and var d && d < perigo - 0.3f)
                {
                    lugar = hit.position;
                    achou = true;
                }
            }
            Assert.IsTrue(achou, "sem NavMesh perto do jogador");
            prof.GetComponent<NavMeshAgent>().Warp(lugar);
            Vector3 deCostas = lugar - p.position; deCostas.y = 0f;
            prof.transform.rotation = Quaternion.LookRotation(deCostas);
            Assert.LessOrEqual(Flat.Distance(prof.transform.position, p.position), GameManager.I.normal.colinhaDangerDistance);

            InputSystem.QueueStateEvent(kb, new KeyboardState(Key.Q));
            bool sentiuSemVer = false;
            float antes = prof.Suspicion;
            for (float fim = Time.realtimeSinceStartup + 0.8f; Time.realtimeSinceStartup < fim;)
            {
                yield return null;
                if (prof.Visibility <= 0f && prof.Suspicion > antes + 0.01f) sentiuSemVer = true;
                antes = prof.Suspicion;
            }
            InputSystem.QueueStateEvent(kb, new KeyboardState());
            Assert.IsTrue(sentiuSemVer, "com a colinha e o professor a menos de " + GameManager.I.normal.colinhaDangerDistance
                                        + " m, a suspeita tinha que subir mesmo sem ele ver");
        }

        // ------------------------------------------------------------------ garrafinha

        [UnityTest]
        public IEnumerator GarrafinhaComG_DaRespostaEFazBarulhoDeGole()
        {
            yield return Comecar(Difficulty.Normal);
            int goles = SoundFX.PlayCount(Sfx.Gole);
            yield return Segurar(Key.G, () => RespostasCertas == 1, 4f, "segurar G não deu a resposta da garrafinha");
            Assert.AreEqual(goles + 1, SoundFX.PlayCount(Sfx.Gole), "o gole não tocou");
            var gole = ruidos.Find(n => n.what == Falas.RuidoGole);
            Assert.AreEqual(Falas.RuidoGole, gole.what, "o gole não virou ruído para o professor (Noise)");
            Assert.AreEqual(GameManager.I.normal.garrafaNoise, gole.amount);
            Assert.IsTrue(gole.byPlayer);
            Assert.AreEqual((1, 1, 0), (MatchLog.GarrafaUsos, MatchLog.GarrafaCertas, MatchLog.GarrafaErradas), "registro no partidas.csv (Bloco 15b)");
        }

        // ------------------------------------------------------------------ bilhete (fluxo completo)

        [UnityTest]
        public IEnumerator BilheteParaONerd_FazBarulhoEVoltaComAResposta()
        {
            yield return Comecar(Difficulty.Normal, s =>
            {
                s.bilheteDelay = 1f;
                s.bilheteMissChancePerMeter = 0f;   // a mira/sorte é testada no EditMode; aqui é o fluxo
                s.bilheteLostDistance = 0f;
                s.nerdGlanceInterval = new Vector2(999f, 999f); // o Nerd fica escrevendo (aceita)
            });
            var c = PlayerController.I.Cheats;
            var nerd = UnityEngine.Object.FindAnyObjectByType<NerdStudent>();
            var budget = GameManager.I.Cheats;
            Assert.IsFalse(c.ThrowAt(null), "sem mirar em ninguém não pode jogar");
            Assert.IsTrue(nerd.AcceptsNote);

            Assert.IsTrue(c.ThrowAt(nerd), "não jogou o bilhete no Nerd");
            Assert.AreEqual(1, budget.Reserved, "o bilhete no ar tem que reservar uma vaga do orçamento");
            Assert.IsTrue(ruidos.Exists(n => n.what == Falas.RuidoPapel), "o arremesso não fez ruído (Noise)");
            Assert.IsFalse(c.ThrowAt(nerd), "dois bilhetes ao mesmo tempo");

            yield return WaitUntil(() => RespostasCertas == 1, 5f, "o bilhete não voltou com a resposta");
            Assert.AreEqual((1, 0), (budget.Used, budget.Reserved));
            Assert.AreEqual(GameManager.I.normal.bilheteCharges - 1, c.BilheteCharges);
            Assert.AreEqual((1, 1, 0), (MatchLog.BilheteUsos, MatchLog.BilheteCertas, MatchLog.BilheteErradas), "registro no partidas.csv (Bloco 15b)");
        }

        IEnumerator Tecla(Key k)
        {
            InputSystem.QueueStateEvent(kb, new KeyboardState(k));
            yield return null;
            InputSystem.QueueStateEvent(kb, new KeyboardState());
            yield return null;
        }

        /// <summary>B arma a mira (bolinha na mão, linha na tela, sem gastar nada); Espaço cancela SEM levantar.</summary>
        [UnityTest]
        public IEnumerator BilheteBArmaAMiraEEspacoCancelaSemLevantar()
        {
            yield return Comecar(Difficulty.Normal, s => s.nerdGlanceInterval = new Vector2(999f, 999f));
            var p = PlayerController.I;
            var c = p.Cheats;
            int cargas = c.BilheteCharges;
            Assert.IsTrue(p.IsSeated);

            yield return Tecla(Key.B);
            Assert.IsTrue(c.Aiming, "B tem que armar a mira, não jogar direto");
            Assert.IsFalse(c.Throwing, "armar não é jogar");
            yield return null;
            // quem está na mira decide: Dorminhoco/Puxa-saco = borracha (favor); senão, a bolinha do bilhete
            Assert.AreEqual(c.AimingAtFavor ? HandProp.Borracha : HandProp.Bilhete, p.visual.prop, "mirando, o objeto certo fica na mão");
            Assert.IsNotNull(c.Mira);
            Assert.IsTrue(c.Mira.Visible, "a linha da mira tem que aparecer");
            Assert.AreEqual(c.AimingAtFavor ? Infracao.Outra : Infracao.Bilhete, c.CurrentKind, "braço armado é suspeito (bilhete é cola; borracha, bagunça)");
            Assert.AreEqual(GameManager.I.Settings.bilheteAimSuspicion, c.CurrentRate, 1e-4f);
            Assert.AreEqual(cargas, c.BilheteCharges, "mirar não gasta bilhete");
            Assert.AreEqual(0, GameManager.I.Cheats.Reserved, "mirar não reserva o orçamento");

            yield return Tecla(Key.Space);
            Assert.IsFalse(c.Aiming, "Espaço cancela a mira");
            Assert.IsTrue(p.IsSeated, "cancelar a mira não pode levantar o jogador");
            Assert.IsFalse(c.Mira.Visible, "a linha some");
            Assert.AreEqual(cargas, c.BilheteCharges);
            yield return null;
            Assert.AreEqual(HandProp.Nenhum, p.visual.prop, "a bolinha volta pro bolso");
        }

        /// <summary>Mirando: B de novo sem alvo não joga; com alvo, a bolinha (papel amassado) voa em arco e pousa na carteira.</summary>
        [UnityTest]
        public IEnumerator BilheteMiradoVoaEmArcoEPousaNaCarteiraDoNerd()
        {
            yield return Comecar(Difficulty.Normal, s =>
            {
                s.bilheteDelay = 5f;
                s.bilheteMissChancePerMeter = 0f;
                s.bilheteLostDistance = 0f;
                s.nerdGlanceInterval = new Vector2(999f, 999f);
            });
            var c = PlayerController.I.Cheats;
            var nerd = UnityEngine.Object.FindAnyObjectByType<NerdStudent>();
            Assert.IsTrue(c.StartAim());
            Assert.IsFalse(c.ConfirmAim(null), "sem alvo não joga");
            Assert.IsTrue(c.Aiming, "sem alvo continua mirando");
            Assert.IsTrue(c.ConfirmAim(nerd), "com o Nerd na mira, joga");
            Assert.IsFalse(c.Aiming);
            Assert.IsTrue(c.Throwing);

            yield return Wait(PlayerCheats.ReleaseTime + 0.08f);
            var voo = c.Flight;
            Assert.IsNotNull(voo, "a bolinha tem que sair voando da mão");
            Assert.AreEqual(PaperBallFlight.End.Voando, voo.State);
            StringAssert.StartsWith("Sphere_PapelAmassado", voo.GetComponent<MeshFilter>().sharedMesh.name, "a bolinha nova (papel amassado)");
            float subiu = voo.transform.position.y;
            yield return WaitUntil(() => !c.Throwing, 3f, "o arremesso não terminou");
            yield return null;
            Assert.IsTrue(voo != null, "pousou: a bolinha fica uns segundos na carteira");
            Assert.AreEqual(PaperBallFlight.End.Mesa, voo.State, "acertou o Nerd: pousa na carteira");
            Assert.Less(Vector3.Distance(voo.transform.position, PlayerCheats.LandingPoint(nerd)), 0.05f, "pousa em cima da folha dele");
            Assert.Greater(subiu, PlayerController.I.ChestPosition.y - 0.6f, "voou por cima, não rente ao chão");
        }

        /// <summary>Teclas separadas: B = bilhete, V = borracha. V arma a borracha; B troca para o bilhete; Espaço cancela.</summary>
        [UnityTest]
        public IEnumerator VArmaABorrachaEBTrocaParaOBilhete()
        {
            yield return Comecar(Difficulty.Normal, s => { s.eventInterval = new Vector2(9999f, 9999f); s.nerdGlanceInterval = new Vector2(999f, 999f); });
            var p = PlayerController.I;
            var c = p.Cheats;
            int favores = GameManager.I.Favors, cargas = c.BilheteCharges;

            yield return Tecla(Key.V);
            Assert.IsTrue(c.Aiming, "V arma a mira");
            Assert.IsTrue(c.AimingAtFavor, "V é a borracha");
            yield return null;
            Assert.AreEqual(HandProp.Borracha, p.visual.prop, "com V, a borracha fica na mão");

            yield return Tecla(Key.B);
            Assert.IsTrue(c.Aiming, "B mirando a borracha troca de mira, não cancela");
            Assert.IsFalse(c.AimingAtFavor, "agora é o bilhete");
            yield return null;
            Assert.AreEqual(HandProp.Bilhete, p.visual.prop, "com B, a bolinha fica na mão");

            yield return Tecla(Key.Space);
            Assert.IsFalse(c.Aiming, "Espaço cancela");
            Assert.IsTrue(p.IsSeated, "e não levanta");
            Assert.AreEqual(favores, GameManager.I.Favors, "nada gasto");
            Assert.AreEqual(cargas, c.BilheteCharges, "nada gasto");
        }

        IEnumerator JogarBorracha(StudentBase alvo, string efeito)
        {
            var gm = GameManager.I;
            var c = PlayerController.I.Cheats;
            int favores = gm.Favors, bilhetes = c.BilheteCharges;
            Assert.Greater(favores, 0);
            Assert.IsTrue(alvo.CanInteract(PlayerController.I), alvo.name + " tem que estar disponível para o favor");
            Assert.IsTrue(c.StartEraserAim(), "V arma a borracha");
            Assert.IsTrue(c.ConfirmAim(alvo), "mirando nele, joga a borracha");
            Assert.IsTrue(c.Throwing);
            yield return null;
            Assert.AreEqual(HandProp.Borracha, PlayerController.I.visual.prop, "antes de soltar, é a borracha que está na mão");
            Assert.AreEqual(Infracao.Outra, c.CurrentKind, "jogar borracha é bagunça (não é cola)");
            Assert.AreEqual(gm.Settings.borrachaThrowSuspicion, c.CurrentRate, 1e-4f);
            yield return Wait(PlayerCheats.ReleaseTime + 0.05f);
            Assert.IsNotNull(c.Flight, "a borracha tem que sair voando");
            Assert.AreEqual("Borracha Voando", c.Flight.name);
            yield return WaitUntil(() => !c.Throwing, 3f, "o arremesso da borracha não terminou");
            yield return null;
            Assert.AreEqual(favores - 1, gm.Favors, "acertou e o professor foi ver: gasta 1 favor");
            Assert.AreEqual(bilhetes, c.BilheteCharges, "borracha não gasta bilhete");
            Assert.AreEqual(0, gm.Cheats.Reserved, "borracha não mexe no orçamento de colas");
            Assert.IsTrue(HUD.I.ToastTexts().Exists(t => t.Contains(efeito)), "o favor tem que acontecer: " + efeito);
        }

        /// <summary>B mirando no Dorminhoco joga a borracha: ele acorda gritando, o professor vai ver, gasta 1 favor.</summary>
        [UnityTest]
        public IEnumerator BorrachaNoDorminhocoAcordaEleEGastaUmFavor()
        {
            yield return Comecar(Difficulty.Normal, s => { s.eventInterval = new Vector2(9999f, 9999f); s.snitchInterval = new Vector2(999f, 999f); });
            yield return JogarBorracha(UnityEngine.Object.FindAnyObjectByType<SleeperStudent>(), "acordou GRITANDO");
        }

        /// <summary>B mirando no Puxa-saco joga a borracha: ele chama o professor pra uma "dúvida", gasta 1 favor.</summary>
        [UnityTest]
        public IEnumerator BorrachaNoPuxaSacoChamaOProfessorEGastaUmFavor()
        {
            yield return Comecar(Difficulty.Normal, s => { s.eventInterval = new Vector2(9999f, 9999f); s.snitchInterval = new Vector2(999f, 999f); });
            yield return JogarBorracha(UnityEngine.Object.FindAnyObjectByType<SnitchStudent>(), "Puxa-saco chamou o professor");
        }

        [UnityTest]
        public IEnumerator NerdOlhandoDevolveOBilhete()
        {
            yield return Comecar(Difficulty.Normal, s => { s.bilheteMissChancePerMeter = 0f; s.bilheteLostDistance = 0f; });
            var c = PlayerController.I.Cheats;
            var nerd = UnityEngine.Object.FindAnyObjectByType<NerdStudent>();
            nerd.GlanceSoon(); // parou de escrever: vai olhar
            Assert.IsFalse(nerd.AcceptsNote);
            Assert.IsTrue(c.ThrowAt(nerd));
            yield return WaitUntil(() => !c.Throwing, 2f, "o arremesso não terminou");
            Assert.IsFalse(c.bilhete.Busy, "o Nerd em alerta não podia aceitar");
            Assert.AreEqual((0, 0), (GameManager.I.Cheats.Used, GameManager.I.Cheats.Reserved), "a vaga do bilhete devolvido tem que voltar");
            Assert.AreEqual((1, 0, 1), (MatchLog.BilheteUsos, MatchLog.BilheteCertas, MatchLog.BilheteErradas), "registro no partidas.csv (Bloco 15b)");
        }

        /// <summary>
        /// Bug do Bloco 15b: flagrante com o bilhete no ar não devolvia a vaga reservada — o jogador perdia uma resposta
        /// do orçamento até o fim da prova.
        /// </summary>
        [UnityTest]
        public IEnumerator FlagranteComBilheteNoArDevolveAVagaDoOrcamento()
        {
            yield return Comecar(Difficulty.Normal, s => s.bilheteThrowTime = 2f); // arremesso longo: dá tempo do flagrante
            var c = PlayerController.I.Cheats;
            var budget = GameManager.I.Cheats;
            int livres = budget.Available;
            Assert.IsTrue(c.ThrowAt(UnityEngine.Object.FindAnyObjectByType<NerdStudent>()));
            Assert.AreEqual(livres - 1, budget.Available, "o bilhete no ar reserva uma vaga");

            GameManager.I.ApplyFlagrante("teste", Infracao.Bilhete);
            Assert.IsFalse(c.Throwing);
            Assert.AreEqual((0, 0), (budget.Used, budget.Reserved), "a vaga do bilhete cortado tinha que voltar");
            Assert.AreEqual(livres, budget.Available);

            yield return Wait(2.5f); // e o bilhete cortado não chega depois
            Assert.IsFalse(c.bilhete.Busy);
            Assert.AreEqual(0, RespostasCertas);
            Assert.AreEqual((1, 0, 1), (MatchLog.BilheteUsos, MatchLog.BilheteCertas, MatchLog.BilheteErradas));
        }

        // ------------------------------------------------------------------ fone

        [UnityTest]
        public IEnumerator FoneComF_PedeVibraEDepoisLeAResposta()
        {
            yield return Comecar(Difficulty.Normal, s =>
            {
                s.foneDelay = 1.5f;
                s.foneUseTime = 0.3f;
                s.foneRingChance = 1f; // vibra com certeza
                s.foneRingSeconds = 0.2f;
            });
            var c = PlayerController.I.Cheats;
            int vibras = SoundFX.PlayCount(Sfx.Vibra);
            yield return Segurar(Key.F, () => c.fone.Waiting, 3f, "segurar F não mandou a mensagem");
            Assert.AreEqual(1, GameManager.I.Cheats.Reserved);
            Assert.AreEqual(0, RespostasCertas, "a resposta não pode chegar na hora");

            yield return WaitUntil(() => c.fone.Arrived, 4f, "a resposta do fone não chegou");
            Assert.Greater(SoundFX.PlayCount(Sfx.Vibra), vibras, "o celular não vibrou enquanto esperava");
            Assert.IsTrue(ruidos.Exists(n => n.what == Falas.RuidoVibra), "a vibração não virou ruído (Noise)");
            yield return WaitUntil(() => !c.Ringing, 2f, "não parou de vibrar");

            yield return Wait(0.1f);
            yield return Segurar(Key.F, () => RespostasCertas == 1, 3f, "segurar F de novo não leu a resposta");
            Assert.AreEqual((1, 0), (GameManager.I.Cheats.Used, GameManager.I.Cheats.Reserved));
            Assert.IsFalse(c.fone.Busy);
            Assert.AreEqual((1, 1, 0), (MatchLog.FoneUsos, MatchLog.FoneCertas, MatchLog.FoneErradas), "registro no partidas.csv (Bloco 15b)");
        }

        // ------------------------------------------------------------------ orçamento e confisco

        [UnityTest]
        public IEnumerator OrcamentoValeParaTodasAsColas()
        {
            yield return Comecar(Difficulty.Dificil);
            var gm = GameManager.I;
            var p = PlayerController.I;
            Assert.AreEqual(gm.dificil.maxCheatAnswers, gm.Cheats.Max);
            for (int i = 0; i < gm.Cheats.Max; i++) FinishPhone.Invoke(p, null);
            Assert.AreEqual(gm.Cheats.Max, RespostasCertas);
            Assert.IsTrue(gm.Cheats.Exhausted);

            Assert.AreEqual(Falas.OrcamentoAcabou, p.Cheats.WhyNot(PlayerCheats.Hold.Colinha), "colinha");
            Assert.AreEqual(Falas.OrcamentoAcabou, p.Cheats.WhyNot(PlayerCheats.Hold.Garrafinha), "garrafinha");
            Assert.AreEqual(Falas.OrcamentoAcabou, p.Cheats.WhyNot(PlayerCheats.Hold.FoneEnviar), "fone");
            Assert.IsFalse(p.Cheats.ThrowAt(UnityEngine.Object.FindAnyObjectByType<NerdStudent>()), "bilhete");
            p.StandUp();
            Assert.IsFalse(UnityEngine.Object.FindAnyObjectByType<NerdStudent>().CanInteract(p), "Nerd");

            // Fundão: o sorteio cairia na cola (0,35–0,7), mas sem orçamento ele só faz bagunça
            int notas = RespostasCertas;
            UnityEngine.Object.FindAnyObjectByType<BackRowStudent>().Resolve(p, 0.5f);
            Assert.AreEqual(notas, RespostasCertas, "o Fundão passou cola sem orçamento");
            Assert.AreEqual(0, MatchLog.FundaoColaBoa + MatchLog.FundaoColaRuim);
        }

        [UnityTest]
        public IEnumerator FlagranteConfiscaPeloTipoDaInfracao()
        {
            yield return Comecar(Difficulty.Normal);
            var gm = GameManager.I;
            var p = PlayerController.I;
            Assert.IsFalse(gm.Settings.firstFlagranteIsWarning, "pré-condição: no Normal o primeiro flagrante já vale");

            gm.ApplyFlagrante("teste", Infracao.ColinhaNaManga);
            Assert.IsFalse(p.Cheats.HasColinha, "colinha não foi confiscada");
            Assert.IsTrue(p.HasPhone, "flagrante de colinha não leva o celular");
            Assert.IsTrue(p.Cheats.HasGarrafa);
            Assert.AreEqual(Falas.ColinhaConfiscada, p.Cheats.WhyNot(PlayerCheats.Hold.Colinha));

            gm.ApplyFlagrante("teste", Infracao.CelularTocando);
            Assert.IsFalse(p.HasPhone, "celular tocando também confisca o celular");
            Assert.IsTrue(p.Cheats.HasGarrafa);
            Assert.AreEqual(Falas.FoneSemCelular, p.Cheats.WhyNot(PlayerCheats.Hold.FoneEnviar), "sem celular, sem fone");
        }

        [UnityTest]
        public IEnumerator FoneEsperandoSePerdeSeOCelularForConfiscado()
        {
            yield return Comecar(Difficulty.Normal, s => { s.foneUseTime = 0.2f; s.foneRingChance = 0f; });
            var c = PlayerController.I.Cheats;
            yield return Segurar(Key.F, () => c.fone.Waiting, 3f, "segurar F não mandou a mensagem");
            GameManager.I.ApplyFlagrante("teste", Infracao.Celular);
            yield return null;
            yield return null;
            Assert.IsFalse(c.fone.Busy, "a resposta continuou vindo para um celular confiscado");
            Assert.AreEqual(0, GameManager.I.Cheats.Reserved, "a vaga reservada tinha que voltar");
            Assert.AreEqual((1, 0, 1), (MatchLog.FoneUsos, MatchLog.FoneCertas, MatchLog.FoneErradas), "registro no partidas.csv (Bloco 15b)");
        }

        // ------------------------------------------------------------------ HUD

        [UnityTest]
        public IEnumerator PainelDasColasApareceSemEncostarNaNota()
        {
            yield return Comecar(Difficulty.Normal);
            yield return null;
            var hud = HUD.I;
            Assert.IsTrue(hud.CheatsPanel.gameObject.activeInHierarchy, "o painel das colas não apareceu");
            // Bloco 16: uma etiqueta de tecla por cola, na ordem do painel
            CollectionAssert.AreEqual(new[] { "Q", "G", "B", "F", "V" }, hud.CheatKeys(), "V = borracha (favor com mira, 1.1.2)");
            Assert.IsTrue(hud.CheatsTextFits(), "o texto não cabe no painel");

            Rect R(RectTransform r) { var c = new Vector3[4]; r.GetWorldCorners(c); return Rect.MinMaxRect(c[0].x, c[0].y, c[2].x, c[2].y); }
            Assert.IsFalse(R(hud.CheatsPanel).Overlaps(R(hud.GradePanel)), "o painel das colas cobre o painel da NOTA");
        }
    }
}
