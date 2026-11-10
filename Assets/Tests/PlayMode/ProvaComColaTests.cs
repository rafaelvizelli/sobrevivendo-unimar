using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using static ProvaImpossivel.Tests.FluxoEProfessorTests;

namespace ProvaImpossivel.Tests
{
    /// <summary>
    /// Bloco 1 — "a prova que exige cola": o jogador só sabe algumas questões (por dificuldade);
    /// nas outras ele chuta (~25%) ou cola. Testes com a cena real (PlayMode).
    /// </summary>
    public class ProvaComColaTests
    {
        static readonly MethodInfo TryAnswer = typeof(PlayerController).GetMethod("TryAnswer", BindingFlags.NonPublic | BindingFlags.Instance);

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
        }

        static IEnumerator StartNormal()
        {
            yield return EnterGame();
            GameManager.I.Begin(Difficulty.Normal);
            PlayerController.I.quizCooldown = 0f; // sem "pensando..." entre perguntas, para o teste andar rápido
            yield return null;
        }

        /// <summary>Abre a próxima pergunta como se o jogador apertasse 1 sentado.</summary>
        static void Abrir() => TryAnswer.Invoke(PlayerController.I, null);

        static IEnumerator Responder(bool certo)
        {
            var quiz = QuizChallenge.I;
            quiz.Answer(certo ? quiz.CorrectSlot : (quiz.CorrectSlot + 1) % 4);
            yield return WaitUntil(() => !quiz.Active, 4f, "o painel da pergunta não fechou");
        }

        [UnityTest]
        public IEnumerator CadaDificuldadeTemSuasQuestoesConhecidas()
        {
            var esperado = new[] { (Difficulty.Facil, 3), (Difficulty.Normal, 3), (Difficulty.Dificil, 3) };
            foreach (var (d, n) in esperado)
            {
                yield return EnterGame();
                GameManager.I.Begin(d);
                Assert.AreEqual(n, ExamSystem.I.KnownCount, d + ": quantidade de questões conhecidas");
                Assert.AreEqual(n, GameManager.I.Settings.knownQuestions, d + ": valor salvo na cena");
            }
        }

        [UnityTest]
        public IEnumerator ConhecidasUsamDesafioBDepoisSoSobraChute()
        {
            yield return StartNormal();
            var exam = ExamSystem.I;

            // as 3 conhecidas: Desafio B normal, cada acerto marca exatamente uma conhecida
            for (int n = 0; n < 3; n++)
            {
                int alvo = exam.FirstBlank(true);
                Abrir();
                Assert.IsTrue(QuizChallenge.I.Active);
                Assert.IsFalse(QuizChallenge.I.IsChute, "questão conhecida não pode virar chute");
                Assert.AreNotEqual(3, QuizChallenge.I.Current.level, "questão conhecida veio do banco de chutes");
                yield return Responder(true);
                Assert.AreEqual(QuestionState.Certa, exam.Questions[alvo], "o acerto não marcou a questão que foi aberta");
            }
            Assert.AreEqual(3f, exam.Grade);
            Assert.AreEqual(7, exam.Count(QuestionState.EmBranco), "acertar conhecidas não pode gastar desconhecidas");

            // acabaram as conhecidas: agora é chute
            int chute = exam.FirstBlank(false);
            Abrir();
            Assert.IsTrue(QuizChallenge.I.IsChute);
            Assert.AreEqual(3, QuizChallenge.I.Current.level, "chute deveria vir do banco de perguntas absurdas");
            Assert.AreEqual(0f, PlayerController.I.CurrentSuspicionRate, "chutar não pode gerar suspeita");
            yield return Responder(false);
            Assert.AreEqual(QuestionState.Errada, exam.Questions[chute]);
            Assert.AreEqual(3f, exam.Grade);

            // e um chute certo dá ponto
            Abrir();
            yield return Responder(true);
            Assert.AreEqual(4f, exam.Grade);

            // a errada continua corrigível por cola (mesmo caminho do celular / Nerd / Fundão)
            for (int n = 0; n < 5; n++) exam.MarkCorrect();
            Assert.AreEqual(chute, exam.MarkCorrect(), "a cola deveria corrigir o chute errado");
        }

        [UnityTest]
        public IEnumerator AlternativaCertaDoChuteESorteadaUniformemente()
        {
            yield return StartNormal();
            var quiz = QuizChallenge.I;
            var cont = new int[4];
            const int N = 4000;
            for (int n = 0; n < N; n++)
            {
                quiz.Begin(10f, null, chute: true);
                cont[quiz.CorrectSlot]++;
            }
            for (int s = 0; s < 4; s++)
                Assert.AreEqual(0.25, cont[s] / (double)N, 0.03, "posição " + (s + 1) + " da alternativa certa fora de 25%");
            quiz.Answer(-1);
            yield return WaitUntil(() => !quiz.Active, 4f, "o painel não fechou");
        }

        /// <summary>
        /// Tecla 1 real (teclado virtual): o mesmo aperto que abre a pergunta não pode respondê-la.
        /// Sem a proteção de frame, dependendo da ordem de execução dos scripts, o 1 virava a resposta "1".
        /// </summary>
        [UnityTest]
        public IEnumerator TeclaUmAbreMasNaoResponde()
        {
            yield return StartNormal();
            var settings = InputSystem.settings;
            var bgAntes = settings.backgroundBehavior;
#if UNITY_EDITOR
            var editorAntes = settings.editorInputBehaviorInPlayMode;
            settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            var kb = InputSystem.AddDevice<Keyboard>("TesteTeclado");
            kb.MakeCurrent();
            try
            {
                yield return Wait(0.2f); // o jogador libera as teclas do menu
                InputSystem.QueueStateEvent(kb, new KeyboardState(Key.Digit1));
                yield return WaitUntil(() => QuizChallenge.I.Active, 2f, "a tecla 1 não abriu a pergunta (input virtual não chegou)");
                yield return null;
                Assert.IsFalse(QuizChallenge.I.Answered, "a mesma tecla 1 abriu E respondeu a pergunta");
                InputSystem.QueueStateEvent(kb, new KeyboardState());
                yield return Wait(0.2f);
                Assert.IsFalse(QuizChallenge.I.Answered, "soltar a tecla não pode responder");

                // um NOVO aperto de 1 responde normalmente (a alternativa 1)
                InputSystem.QueueStateEvent(kb, new KeyboardState(Key.Digit1));
                yield return WaitUntil(() => QuizChallenge.I.Answered, 2f, "um segundo aperto de 1 deveria responder");
                InputSystem.QueueStateEvent(kb, new KeyboardState());
                yield return WaitUntil(() => !QuizChallenge.I.Active, 4f, "o painel não fechou");
            }
            finally
            {
                InputSystem.RemoveDevice(kb);
                settings.backgroundBehavior = bgAntes;
#if UNITY_EDITOR
                settings.editorInputBehaviorInPlayMode = editorAntes;
#endif
            }
        }
    }
}
