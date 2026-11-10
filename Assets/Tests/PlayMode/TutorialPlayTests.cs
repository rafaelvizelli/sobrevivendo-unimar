using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static ProvaImpossivel.Tests.FluxoEProfessorTests;

namespace ProvaImpossivel.Tests
{
    /// <summary>Tutorial jogável: mesma cena e mecânicas reais, professor que não flagra, entrega encerra o treino.</summary>
    public class TutorialPlayTests
    {
        int? flagAntes;

        [SetUp]
        public void Guardar() => flagAntes = PlayerPrefs.HasKey(TutorialFlag.Key) ? PlayerPrefs.GetInt(TutorialFlag.Key) : null;

        [TearDown]
        public void Devolver()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            if (flagAntes.HasValue) PlayerPrefs.SetInt(TutorialFlag.Key, flagAntes.Value);
            else PlayerPrefs.DeleteKey(TutorialFlag.Key);
            PlayerPrefs.Save();
        }

        [UnityTest]
        public IEnumerator PrimeiraVezOMenuLevaAoTreino()
        {
            TutorialFlag.Clear();
            yield return EnterGame();
            GameManager.I.ChooseFromMenu(Difficulty.Normal);
            Assert.IsTrue(GameManager.I.InTutorial, "na primeira vez, escolher a dificuldade começa o treino");
            Assert.AreEqual(GameManager.I.tutorial.name, GameManager.I.Settings.name);
            Assert.IsNotNull(Object.FindAnyObjectByType<TutorialDirector>());

            TutorialFlag.MarkDone();
            yield return EnterGame();
            GameManager.I.ChooseFromMenu(Difficulty.Normal);
            Assert.IsFalse(GameManager.I.InTutorial, "depois de visto, o menu vai direto pra prova");
            Assert.AreEqual(GameManager.I.normal.name, GameManager.I.Settings.name);
        }

        [UnityTest]
        public IEnumerator NoTreinoOProfessorNaoFlagra()
        {
            yield return EnterGame();
            GameManager.I.BeginTutorial();
            yield return null;
            var prof = ProfessorAI.I;
            var p = PlayerController.I;
            p.SetInteractionSuspicion(60f, "teste: colando", Infracao.Outra);
            prof.Alert(p.transform.position, 99f, "teste");
            yield return Wait(3f);
            Assert.AreNotEqual(ProfState.Flagrante, prof.State, "no treino ele só ameaça");
            Assert.AreEqual(0, GameManager.I.Flagrantes);
            Assert.AreEqual(Phase.Playing, GameManager.I.Phase);
            p.SetInteractionSuspicion(0f, "");
        }

        /// <summary>O treino inteiro, de ponta a ponta, só com as mecânicas reais (sem mexer nos contadores por fora).</summary>
        [UnityTest]
        public IEnumerator TreinoCompletoPelasMecanicasReais()
        {
            TutorialFlag.Clear();
            yield return EnterGame();
            var gm = GameManager.I;
            gm.BeginTutorial();
            yield return null;
            var dir = Object.FindAnyObjectByType<TutorialDirector>();
            var p = PlayerController.I;
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

            // andar: levanta e sai do lugar
            p.StandUp();
            var cc = p.GetComponent<CharacterController>();
            cc.enabled = false;
            p.transform.position = p.seatPoint.position + new Vector3(0f, 0f, -(TutorialSteps.WalkDistance + 0.5f));
            cc.enabled = true;
            yield return WaitUntil(() => dir.Step == TutorialStep.Sentar, 2f, "andar não completou");

            p.SitDown();
            yield return WaitUntil(() => dir.Step == TutorialStep.Responder, 2f, "sentar não completou");

            // responder: a tecla 1 chama TryAnswer; aqui chamamos direto e respondemos certo
            typeof(PlayerController).GetMethod("TryAnswer", flags).Invoke(p, null);
            Assert.IsTrue(QuizChallenge.I.Active, "a questão deveria abrir");
            QuizChallenge.I.Answer(QuizChallenge.I.CorrectSlot);
            yield return WaitUntil(() => dir.Step == TutorialStep.Celular, 4f, "responder não completou");

            typeof(PlayerController).GetMethod("FinishPhoneConsult", flags).Invoke(p, null);
            yield return WaitUntil(() => dir.Step == TutorialStep.Nerd, 2f, "celular não completou");

            p.StandUp();
            var nerd = Object.FindAnyObjectByType<NerdStudent>();
            Assert.IsTrue(nerd.CanInteract(p), "em pé, o Nerd pode ser espiado");
            nerd.Interact(p);
            yield return WaitUntil(() => dir.Step == TutorialStep.Distracao, 2f, "Nerd não completou");

            Object.FindAnyObjectByType<SleeperStudent>().Interact(p);
            yield return WaitUntil(() => dir.Step == TutorialStep.Entregar, 2f, "distração não completou (favor não foi gasto?)");

            gm.DeliverExam();
            yield return WaitUntil(() => gm.Phase == Phase.Ended, 2f, "entregar não encerrou o treino");
            Assert.IsTrue(TutorialFlag.Done, "concluir marca o tutorial como visto");
            Assert.AreEqual(0, gm.Flagrantes);
        }

        [UnityTest]
        public IEnumerator EntregarAntesDaHoraNaoEncerraEPularMarcaComoVisto()
        {
            TutorialFlag.Clear();
            yield return EnterGame();
            var gm = GameManager.I;
            gm.BeginTutorial();
            yield return null;
            gm.DeliverExam();
            Assert.AreEqual(Phase.Playing, gm.Phase, "entregar no primeiro passo não pode encerrar o treino");
            Assert.AreEqual(TutorialStep.Andar, Object.FindAnyObjectByType<TutorialDirector>().Step);

            gm.EndTutorial(completed: false);
            Assert.AreEqual(Phase.Ended, gm.Phase);
            Assert.IsTrue(TutorialFlag.Done, "pular o tutorial também conta como visto");
        }
    }
}
