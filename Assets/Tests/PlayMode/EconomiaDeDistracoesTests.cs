using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static ProvaImpossivel.Tests.FluxoEProfessorTests;

namespace ProvaImpossivel.Tests
{
    /// <summary>
    /// Bloco 3 — distrações viram decisão: favores da turma limitados por partida, gastos só quando funcionam,
    /// e nenhuma distração tira o professor de uma investigação em andamento.
    /// </summary>
    public class EconomiaDeDistracoesTests
    {
        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
        }

        static IEnumerator StartGame(Difficulty d)
        {
            yield return EnterGame();
            GameManager.I.Begin(d);
            yield return null;
        }

        [UnityTest]
        public IEnumerator FavoresPorDificuldade()
        {
            foreach (var (d, n) in new[] { (Difficulty.Facil, 5), (Difficulty.Normal, 2), (Difficulty.Dificil, 2) })
            {
                yield return StartGame(d);
                Assert.AreEqual(n, GameManager.I.Favors, d + ": favores no começo da prova");
            }
        }

        [UnityTest]
        public IEnumerator DistracaoNaoCancelaInvestigacao()
        {
            yield return StartGame(Difficulty.Normal);
            var prof = ProfessorAI.I;
            var p = PlayerController.I;
            prof.Alert(p.transform.position, 70f / GameManager.I.normal.suspicionMultiplier, "teste");
            Assert.AreEqual(ProfState.Investigando, prof.State);

            Assert.IsFalse(prof.Distract(Vector3.zero, 5f, "", true), "investigando, ele não pode ser distraído");
            Object.FindAnyObjectByType<SnitchStudent>().Interact(p);
            Assert.AreEqual(ProfState.Investigando, prof.State, "o pedido ao Puxa-saco tirou o professor da investigação");
            Assert.AreEqual(GameManager.I.Settings.favors, GameManager.I.Favors, "distração que não funcionou não pode gastar favor");
        }

        [UnityTest]
        public IEnumerator FavorGastoQuandoADistracaoFunciona()
        {
            yield return StartGame(Difficulty.Normal);
            int antes = GameManager.I.Favors;
            Object.FindAnyObjectByType<SleeperStudent>().Interact(PlayerController.I);
            Assert.AreEqual(ProfState.Distraido, ProfessorAI.I.State);
            Assert.AreEqual(antes - 1, GameManager.I.Favors);
        }

        /// <summary>Fundão: o favor só é gasto se ele fez algo por você (a bagunça funcionou ou o papel foi passado).</summary>
        [UnityTest]
        public IEnumerator FundaoSoCobraFavorQuandoFazAlgo()
        {
            yield return StartGame(Difficulty.Normal);
            var p = PlayerController.I;
            var prof = ProfessorAI.I;
            var fundao = Object.FindAnyObjectByType<BackRowStudent>();
            int favores = GameManager.I.Favors;

            prof.Alert(p.transform.position, 70f / GameManager.I.normal.suspicionMultiplier, "teste");
            Assert.AreEqual(ProfState.Investigando, prof.State, "pré-condição");
            fundao.Resolve(p, 0.1f); // derruba tudo: o professor investigando nem liga
            fundao.Resolve(p, 0.9f); // show: idem
            Assert.AreEqual(favores, GameManager.I.Favors, "bagunça que não distraiu não pode gastar favor");

            yield return StartGame(Difficulty.Normal);
            fundao = Object.FindAnyObjectByType<BackRowStudent>();
            fundao.Resolve(PlayerController.I, 0.1f);
            Assert.AreEqual(ProfState.Distraido, ProfessorAI.I.State, "com o professor tranquilo, a bagunça distrai");
            Assert.AreEqual(GameManager.I.Settings.favors - 1, GameManager.I.Favors, "bagunça que funcionou gasta o favor");
        }

        /// <summary>A cola do Fundão pode dar flagrante (chance da dificuldade); com o professor ao telefone, nunca.</summary>
        [UnityTest]
        public IEnumerator ColaDoFundaoPodeDarFlagrante()
        {
            yield return StartGame(Difficulty.Normal);
            var fundao = Object.FindAnyObjectByType<BackRowStudent>();
            GameManager.I.Settings.backRowCaughtChance = 1f;
            fundao.Resolve(PlayerController.I, 0.5f); // 0,35 a 0,7 = passa a cola
            Assert.AreEqual(ProfState.Flagrante, ProfessorAI.I.State, "com chance 1, passar a cola tinha que dar flagrante");
            Assert.AreEqual(GameManager.I.Settings.favors - 1, GameManager.I.Favors, "o papel foi passado: o favor foi usado");

            yield return StartGame(Difficulty.Normal);
            fundao = Object.FindAnyObjectByType<BackRowStudent>();
            var prof = ProfessorAI.I;
            GameManager.I.Settings.backRowCaughtChance = 1f;
            Assert.IsTrue(prof.Distract(Vector3.zero, 10f, "", false, phone: true));
            fundao.Resolve(PlayerController.I, 0.5f);
            Assert.AreNotEqual(ProfState.Flagrante, prof.State, "ao telefone ele não percebe a cola");
            Assert.AreEqual(1, ExamSystem.I.Count(QuestionState.Certa) + ExamSystem.I.Count(QuestionState.Errada), "a cola (boa ou ruim) foi usada");
        }

        /// <summary>Regra nova: desconfiado, ele não cai em distração (DifficultySettings.distractWhileSuspicious = falso).</summary>
        [UnityTest]
        public IEnumerator DistracaoNaoTiraDoDesconfiado()
        {
            yield return StartGame(Difficulty.Normal);
            var prof = ProfessorAI.I;
            var p = PlayerController.I;
            prof.Alert(p.transform.position, 35f / GameManager.I.normal.suspicionMultiplier, "teste");
            Assert.AreEqual(ProfState.Desconfiado, prof.State, "pré-condição");
            Assert.IsFalse(prof.Distract(Vector3.zero, 5f, "", true), "desconfiado, ele não pode ser distraído");
            int favores = GameManager.I.Favors;
            Object.FindAnyObjectByType<SleeperStudent>().Interact(p);
            Assert.AreEqual(ProfState.Desconfiado, prof.State, "cutucar o Dorminhoco tirou o professor do desconfiado");
            Assert.AreEqual(favores, GameManager.I.Favors, "distração recusada não pode gastar favor");
        }

        [UnityTest]
        public IEnumerator SemFavoresNinguemAjuda()
        {
            yield return StartGame(Difficulty.Normal);
            var p = PlayerController.I;
            var dorminhoco = Object.FindAnyObjectByType<SleeperStudent>();
            var puxaSaco = Object.FindAnyObjectByType<SnitchStudent>();
            var fundao = Object.FindAnyObjectByType<BackRowStudent>();
            Assert.IsTrue(dorminhoco.CanInteract(p) && puxaSaco.CanInteract(p) && fundao.CanInteract(p), "pré-condição: todos disponíveis");

            while (GameManager.I.SpendFavor()) { }
            Assert.AreEqual(0, GameManager.I.Favors);
            Assert.IsFalse(dorminhoco.CanInteract(p), "Dorminhoco");
            Assert.IsFalse(puxaSaco.CanInteract(p), "Puxa-saco");
            Assert.IsFalse(fundao.CanInteract(p), "Fundão");
        }

        /// <summary>Bug da auditoria: a 1ª virada do Puxa-saco era sorteada com o intervalo do Normal em qualquer dificuldade.</summary>
        [UnityTest]
        public IEnumerator PrimeiraViradaDoPuxaSacoUsaADificuldadeEscolhida()
        {
            var nextWatch = typeof(SnitchStudent).GetField("nextWatch", BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var d in new[] { Difficulty.Dificil, Difficulty.Facil })
            {
                yield return StartGame(d);
                yield return null;
                var s = GameManager.I.Settings.snitchInterval;
                float v = (float)nextWatch.GetValue(Object.FindAnyObjectByType<SnitchStudent>());
                Assert.That(v, Is.InRange(s.x - 0.2f, s.y), d + ": 1ª virada fora do intervalo da dificuldade");
            }
        }
    }
}
