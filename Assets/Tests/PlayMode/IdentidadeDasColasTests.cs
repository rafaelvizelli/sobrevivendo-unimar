using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;
using static ProvaImpossivel.Tests.FluxoEProfessorTests;

namespace ProvaImpossivel.Tests
{
    /// <summary>
    /// Bloco 2 — cada cola com identidade própria: celular com bateria, Nerd que olha de lado (sem sorteio),
    /// Fundão que faz barulho, e consequências decididas pelo TIPO da infração (não pelo texto).
    /// </summary>
    public class IdentidadeDasColasTests
    {
        static readonly MethodInfo FinishPhone = typeof(PlayerController).GetMethod("FinishPhoneConsult", BindingFlags.NonPublic | BindingFlags.Instance);
        static readonly MethodInfo Whisper = typeof(BackRowStudent).GetMethod("Whisper", BindingFlags.NonPublic | BindingFlags.Instance);
        static readonly MethodInfo GiveCola = typeof(BackRowStudent).GetMethod("GiveCola", BindingFlags.NonPublic | BindingFlags.Instance);

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

        // ------------------------------------------------------------------ celular

        [UnityTest]
        public IEnumerator CelularTemBateriaPorDificuldade()
        {
            // v1.1.3 (24/11): Fácil 3→2 (equilíbrio "Fácil mais difícil")
            foreach (var (d, cargas) in new[] { (Difficulty.Facil, 2), (Difficulty.Normal, 1), (Difficulty.Dificil, 1) })
            {
                yield return StartGame(d);
                Assert.AreEqual(cargas, PlayerController.I.PhoneCharges, d + ": cargas do celular");
                Assert.IsTrue(PlayerController.I.CanUsePhone);
            }
        }

        [UnityTest]
        public IEnumerator BateriaAcabaDepoisDasConsultas()
        {
            yield return StartGame(Difficulty.Normal);
            var p = PlayerController.I;
            int cargas = p.PhoneCharges;
            for (int i = 0; i < cargas; i++) FinishPhone.Invoke(p, null);
            Assert.AreEqual((float)cargas, ExamSystem.I.Grade, "cada consulta vale uma questão");
            Assert.AreEqual(0, p.PhoneCharges);
            Assert.IsFalse(p.CanUsePhone, "sem bateria o celular não pode mais ser usado");
            Assert.IsTrue(p.HasPhone, "sem bateria não é o mesmo que confiscado");
        }

        // ------------------------------------------------------------------ confisco por tipo

        [UnityTest]
        public IEnumerator ConfiscoDependeDoTipoNaoDoTexto()
        {
            yield return StartGame(Difficulty.Normal);
            GameManager.I.ApplyFlagrante("um texto que não fala de telefone", Infracao.Celular);
            Assert.IsFalse(PlayerController.I.HasPhone, "flagrante do tipo Celular deveria confiscar");

            yield return StartGame(Difficulty.Normal);
            GameManager.I.ApplyFlagrante("usando o celular (mas o tipo é outro)", Infracao.EspiarNerd);
            Assert.IsTrue(PlayerController.I.HasPhone, "o texto não pode mais decidir o confisco");
        }

        [UnityTest]
        public IEnumerator NoFacilOPrimeiroFlagranteSoAdverte()
        {
            yield return StartGame(Difficulty.Facil);
            GameManager.I.ApplyFlagrante("celular", Infracao.Celular);
            Assert.IsTrue(PlayerController.I.HasPhone, "no Fácil o 1º flagrante é só advertência");
            GameManager.I.ApplyFlagrante("celular", Infracao.Celular);
            Assert.IsFalse(PlayerController.I.HasPhone, "no Fácil o 2º flagrante no celular confisca");
        }

        [UnityTest]
        public IEnumerator FlagranteDoProfessorCarregaOTipoAteOConfisco()
        {
            yield return StartGame(Difficulty.Normal);
            var prof = ProfessorAI.I;
            var p = PlayerController.I;
            p.SetInteractionSuspicion(30f, "um motivo qualquer", Infracao.Celular);
            yield return null;
            Assert.AreEqual(Infracao.Celular, p.CurrentInfracao);

            prof.Alert(p.transform.position, 80f / GameManager.I.normal.suspicionMultiplier, "teste");
            yield return WaitUntil(() => GameManager.I.Flagrantes == 1, 40f, "o professor não aplicou o flagrante");
            Assert.IsFalse(p.HasPhone, "o tipo Celular deveria ter chegado ao GameManager e confiscado");
            p.SetInteractionSuspicion(0f, "");
            yield return null;
            Assert.AreEqual(Infracao.Nenhuma, p.CurrentInfracao);
        }

        // ------------------------------------------------------------------ Nerd

        [UnityTest]
        public IEnumerator NerdSoPegaQuandoOlhaDeLado()
        {
            yield return StartGame(Difficulty.Normal);
            var nerd = Object.FindAnyObjectByType<NerdStudent>();
            var p = PlayerController.I;
            Assert.IsNotNull(nerd);
            p.StandUp(); // sentado não dá pra espiar (o Nerd fica fora do alcance de quem está sentado)
            Assert.IsTrue(nerd.CanInteract(p), "pré-condição: em pé, o Nerd pode ser espiado");

            // escrevendo: espiar nunca é percebido (antes era um sorteio de 15%)
            for (int i = 0; i < 300; i++) Assert.IsTrue(nerd.OnHoldTick(p, 0.6f), "o Nerd percebeu sem estar olhando");

            nerd.GlanceSoon();
            Assert.IsTrue(nerd.AboutToGlance, "deveria haver um aviso antes da olhada");
            Assert.IsTrue(nerd.OnHoldTick(p, 0.6f), "durante o aviso ainda dá tempo de soltar");

            yield return WaitUntil(() => nerd.Glancing, 3f, "o aviso não virou olhada");
            float antes = ProfessorAI.I.Suspicion;
            Assert.IsFalse(nerd.OnHoldTick(p, 0.6f), "olhando de lado ele deveria pegar quem está espiando");
            Assert.IsFalse(nerd.CanInteract(p), "depois de pegar, o Nerd esconde a prova");
            Assert.Greater(ProfessorAI.I.Suspicion, antes, "o escândalo do Nerd deveria chamar o professor");
        }

        [UnityTest]
        public IEnumerator NerdSoComJogadorEmPe()
        {
            yield return StartGame(Difficulty.Normal);
            var nerd = Object.FindAnyObjectByType<NerdStudent>();
            var p = PlayerController.I;
            Assert.IsTrue(p.IsSeated, "pré-condição: a prova começa com o jogador sentado");
            Assert.IsFalse(nerd.CanInteract(p), "sentado, o jogador não pode espiar o Nerd");
            p.StandUp();
            Assert.IsTrue(nerd.CanInteract(p), "em pé, pode");
            Assert.AreEqual(GameManager.I.Settings.nerdCopies, nerd.maxCopies, "as cópias vêm da dificuldade");
        }

        // ------------------------------------------------------------------ Fundão

        [UnityTest]
        public IEnumerator PsiuDoFundaoSoChamaAtencaoComProfessorPerto()
        {
            yield return StartGame(Difficulty.Normal);
            var fundao = Object.FindAnyObjectByType<BackRowStudent>();
            var prof = ProfessorAI.I;
            var agent = prof.GetComponent<NavMeshAgent>();

            // longe: o professor não ouve
            Assert.IsTrue(NavMesh.SamplePosition(prof.frontSpot, out var longe, 3f, NavMesh.AllAreas));
            agent.Warp(longe.position);
            Vector3 d = longe.position - fundao.transform.position; d.y = 0f;
            Assert.Greater(d.magnitude, fundao.whisperRadius, "pré-condição: a frente da sala fica fora do raio do psiu");
            Assert.AreEqual(0f, prof.Suspicion, "pré-condição: cena nova começa sem suspeita");
            Whisper.Invoke(fundao, null); // só o cochicho (o resultado do pedido é sorteado e tem regras próprias)
            Assert.AreEqual(0f, prof.Suspicion, 0.001f, "o professor longe não deveria ouvir o 'psiu'");

            // perto: ele ouve o cochicho e olha pra lá
            yield return StartGame(Difficulty.Normal);
            fundao = Object.FindAnyObjectByType<BackRowStudent>();
            prof = ProfessorAI.I;
            agent = prof.GetComponent<NavMeshAgent>();
            Assert.IsTrue(NavMesh.SamplePosition(fundao.transform.position, out var perto, 3f, NavMesh.AllAreas));
            agent.Warp(perto.position);
            Assert.AreEqual(0f, prof.Suspicion, "pré-condição: cena nova começa sem suspeita");
            Whisper.Invoke(fundao, null); // só o cochicho (o resultado do pedido é sorteado e tem regras próprias)
            Assert.Greater(prof.Suspicion, 0f, "o professor perto deveria ouvir o 'psiu'");
        }

        [UnityTest]
        public IEnumerator ColaDoFundaoNaoMexeEmProvaCompleta()
        {
            yield return StartGame(Difficulty.Normal);
            var fundao = Object.FindAnyObjectByType<BackRowStudent>();
            for (int i = 0; i < 10; i++) ExamSystem.I.MarkCorrect();
            for (int i = 0; i < 30; i++) GiveCola.Invoke(fundao, new object[] { PlayerController.I });
            Assert.AreEqual(10f, ExamSystem.I.Grade);
        }
    }
}
