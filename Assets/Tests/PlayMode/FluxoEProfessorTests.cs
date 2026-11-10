using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace ProvaImpossivel.Tests
{
    /// <summary>
    /// Testes de integração (PlayMode): carregam a cena real da fase e verificam
    /// o fluxo da partida, o reinício e as transições do professor. Rodar pelo Test Runner (aba PlayMode).
    /// O editor precisa estar em foco enquanto rodam (sem foco o Unity congela o Play).
    /// A cena é carregada pelo caminho, então precisa estar no Build Settings.
    /// </summary>
    public class FluxoEProfessorTests
    {
        const string ScenePath = "Assets/Scenes/Fase1_ProvaImpossivel.unity";

        // ------------------------------------------------------------------ utilidades

        internal static IEnumerator EnterGame()
        {
            // o Test Runner já está em Play, numa cena temporária própria: carrega a cena da fase
            var op = SceneManager.LoadSceneAsync(ScenePath, LoadSceneMode.Single);
            while (op != null && !op.isDone) yield return null;
            yield return WaitUntil(() => GameManager.I != null && HUD.I != null && GameManager.I.Phase == Phase.Intro, 15f, "a fase não chegou na introdução");
        }

        internal static IEnumerator WaitUntil(Func<bool> condition, float timeout, string failMessage)
        {
            float end = Time.realtimeSinceStartup + timeout;
            while (!condition())
            {
                if (Time.realtimeSinceStartup > end)
                    Assert.Fail(failMessage + " (esperou " + timeout + " s) | isPlaying=" + Application.isPlaying
                                + " cena=" + SceneManager.GetActiveScene().path + " frame=" + Time.frameCount
                                + " GM=" + (GameManager.I != null) + " HUD=" + (HUD.I != null)
                                + " fase=" + (GameManager.I != null ? GameManager.I.Phase.ToString() : "-"));
                yield return null;
            }
        }

        internal static IEnumerator Wait(float seconds)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < end) yield return null;
        }

        static int Count<T>() where T : Object => Object.FindObjectsByType<T>(FindObjectsInactive.Exclude).Length;

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
        }

        // ------------------------------------------------------------------ fluxo

        [UnityTest]
        public IEnumerator Fluxo_IntroPartidaPausaEntregaEReinicio()
        {
            yield return EnterGame();
            var gm = GameManager.I;
            int interativos = Interactable.All.Count;

            gm.Begin(Difficulty.Dificil);
            Assert.AreEqual(Phase.Playing, gm.Phase);
            Assert.AreEqual(gm.dificil.name, gm.Settings.name);
            Assert.AreEqual(gm.dificil.duration, gm.TimeLeft, 0.5f);
            Assert.AreEqual(gm.dificil.reactionTime, ProfessorAI.I.reactionTime, "dificuldade não chegou no professor");
            // sem janela (batch mode) o Unity não trava o cursor
            if (!Application.isBatchMode) Assert.AreEqual(CursorLockMode.Locked, Cursor.lockState);

            gm.Begin(Difficulty.Facil);
            Assert.AreEqual(gm.dificil.name, gm.Settings.name, "um segundo Begin não pode trocar a dificuldade");

            yield return Wait(0.5f);
            Assert.Less(gm.TimeLeft, gm.dificil.duration, "o cronômetro não andou");

            gm.Pause();
            Assert.AreEqual(Phase.Paused, gm.Phase);
            Assert.AreEqual(0f, Time.timeScale);
            float paused = gm.TimeLeft;
            yield return Wait(0.5f);
            Assert.AreEqual(paused, gm.TimeLeft, "o tempo andou com o jogo pausado");

            gm.Resume();
            Assert.AreEqual(Phase.Playing, gm.Phase);
            Assert.AreEqual(1f, Time.timeScale);

            gm.DeliverExam(); // nota 0: reprovado
            Assert.AreEqual(Phase.Ended, gm.Phase);
            Assert.AreEqual(CursorLockMode.None, Cursor.lockState);

            gm.Restart();
            yield return WaitUntil(() => GameManager.I != null && GameManager.I != gm && GameManager.I.Phase == Phase.Intro, 15f, "o reinício não voltou à introdução");
            yield return null;

            Assert.AreEqual(1, Count<EventSystem>(), "EventSystem duplicado");
            Assert.AreEqual(1, Count<HUD>(), "HUD duplicado");
            Assert.AreEqual(1, Count<GameManager>(), "GameManager duplicado");
            Assert.AreEqual(1, Count<ProfessorAI>(), "professor duplicado");
            Assert.AreEqual(1, Count<PlayerController>(), "jogador duplicado");
            Assert.AreEqual(1, Count<VisionConeView>(), "cone de visão duplicado");
            Assert.AreEqual(interativos, Interactable.All.Count, "lista de interativos com sobras do jogo anterior");
            Assert.AreEqual(0f, ExamSystem.I.Grade);
            Assert.AreEqual(0, GameManager.I.Flagrantes);
            Assert.AreEqual(1f, Time.timeScale);
        }

        [UnityTest]
        public IEnumerator Fluxo_TresFlagrantesEncerramComoFlagrado()
        {
            yield return EnterGame();
            var gm = GameManager.I;
            gm.Begin(Difficulty.Normal);
            for (int i = 0; i < gm.Settings.maxFlagrantes; i++) gm.ApplyFlagrante("teste");
            Assert.AreEqual(Phase.Playing, gm.Phase, "o final vem com atraso, para a reação do professor");
            yield return WaitUntil(() => gm.Phase == Phase.Ended, 6f, "3 flagrantes não encerraram a partida");
        }

        // ------------------------------------------------------------------ professor

        [UnityTest]
        public IEnumerator Professor_DesconfiaInvestigaFlagraEDaTregua()
        {
            yield return EnterGame();
            var gm = GameManager.I;
            gm.Begin(Difficulty.Normal);
            var prof = ProfessorAI.I;
            var pl = PlayerController.I;
            float mult = gm.normal.suspicionMultiplier;
            yield return null;

            prof.Alert(pl.transform.position, 35f / mult, "teste");
            Assert.AreEqual(ProfState.Desconfiado, prof.State, "suspeita 35 deveria deixar o professor desconfiado");

            pl.SetInteractionSuspicion(30f, "teste: colando");
            prof.Alert(pl.transform.position, 40f / mult, "teste");
            Assert.AreEqual(ProfState.Investigando, prof.State, "suspeita acima de 65 deveria fazer o professor investigar");

            yield return WaitUntil(() => prof.State == ProfState.Flagrante, 35f, "o professor não chegou ao flagrante");
            yield return WaitUntil(() => gm.Flagrantes == 1, 12f, "o flagrante não foi aplicado");
            Assert.AreEqual(1, ExamSystem.I.Count(QuestionState.Errada), "no Normal o flagrante sem pontos zera uma questão");

            pl.SetInteractionSuspicion(0f, "");
            yield return WaitUntil(() => prof.State == ProfState.Tranquilo && prof.InGrace, 8f, "depois do flagrante deveria vir a trégua");
            Assert.IsFalse(pl.InputLocked, "o jogador ficou travado depois do flagrante");
        }

        [UnityTest]
        public IEnumerator Professor_AoTelefoneNaoEnxerga()
        {
            yield return EnterGame();
            GameManager.I.Begin(Difficulty.Normal);
            var prof = ProfessorAI.I;
            var pl = PlayerController.I;
            var janela = Object.FindAnyObjectByType<EventDirector>().janela;
            yield return null;

            prof.Distract(janela.position, 5f, "", false, phone: true);
            pl.SetInteractionSuspicion(40f, "teste: colando");
            float before = prof.Suspicion;
            yield return Wait(2f);

            Assert.IsTrue(prof.OnPhone);
            Assert.AreEqual(0f, prof.Visibility, "ao telefone ele não pode enxergar");
            Assert.LessOrEqual(prof.Suspicion, before, "a suspeita subiu com ele ao telefone");
            pl.SetInteractionSuspicion(0f, "");
        }

        [UnityTest]
        public IEnumerator Visao_ParedeBloqueia()
        {
            yield return EnterGame();
            var inside = new Vector3(0f, 1.6f, 0f);
            Assert.IsTrue(Vision.LineOfSight(inside, new Vector3(0f, 1.6f, -3f)), "dentro da sala, acima das carteiras, deveria ver");
            Assert.IsFalse(Vision.LineOfSight(inside, new Vector3(0f, 1.6f, -9f)), "não pode ver através da parede da frente");
            Assert.IsFalse(Vision.LineOfSight(inside, new Vector3(-8f, 1.6f, 0f)), "não pode ver através da parede da porta");
        }
    }
}
