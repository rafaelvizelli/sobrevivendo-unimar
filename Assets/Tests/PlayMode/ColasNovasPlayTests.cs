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
        }

        // ------------------------------------------------------------------ HUD

        [UnityTest]
        public IEnumerator PainelDasColasApareceSemEncostarNaNota()
        {
            yield return Comecar(Difficulty.Normal);
            yield return null;
            var hud = HUD.I;
            Assert.IsTrue(hud.CheatsPanel.gameObject.activeInHierarchy, "o painel das colas não apareceu");
            var texto = hud.CheatsPanel.GetComponentInChildren<UnityEngine.UI.Text>();
            StringAssert.Contains("[Q]", texto.text);
            StringAssert.Contains("[B]", texto.text);
            StringAssert.Contains("[F]", texto.text);
            StringAssert.Contains("[G]", texto.text);
            Assert.LessOrEqual(texto.preferredHeight, hud.CheatsPanel.rect.height - 12f, "o texto não cabe no painel");

            Rect R(RectTransform r) { var c = new Vector3[4]; r.GetWorldCorners(c); return Rect.MinMaxRect(c[0].x, c[0].y, c[2].x, c[2].y); }
            Assert.IsFalse(R(hud.CheatsPanel).Overlaps(R(hud.GradePanel)), "o painel das colas cobre o painel da NOTA");
        }
    }
}
