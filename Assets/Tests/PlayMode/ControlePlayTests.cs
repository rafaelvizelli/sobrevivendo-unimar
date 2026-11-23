using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using static ProvaImpossivel.Tests.FluxoEProfessorTests;

namespace ProvaImpossivel.Tests
{
    /// <summary>
    /// S7 (Operação Steam): uma prova jogada com um CONTROLE virtual (mesma técnica dos testes com teclado virtual:
    /// dispositivo de teste + entrada indo para o jogo mesmo sem foco).
    /// </summary>
    public class ControlePlayTests
    {
        Gamepad pad;
        InputSettings.BackgroundBehavior bgAntes;
#if UNITY_EDITOR
        InputSettings.EditorInputBehaviorInPlayMode editorAntes;
#endif

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
            pad = InputSystem.AddDevice<Gamepad>("ControleTeste");
            pad.MakeCurrent();
        }

        [TearDown]
        public void TearDown()
        {
            if (pad != null) InputSystem.RemoveDevice(pad);
            InputSystem.settings.backgroundBehavior = bgAntes;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode = editorAntes;
#endif
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
        }

        /// <summary>Aperta e solta um botão (2 frames apertado, para o wasPressedThisFrame ser visto).</summary>
        IEnumerator Aperta(GamepadButton b)
        {
            InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(b));
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(pad, new GamepadState());
            yield return null;
        }

        [UnityTest]
        public IEnumerator ProvaPeloControle_MenuResponderPausaLevantarEAndar()
        {
            InputSystem.QueueStateEvent(pad, new GamepadState());
            yield return EnterGame();
            var gm = GameManager.I;
            bool tutorialAntes = TutorialFlag.Done;
            TutorialFlag.MarkDone(); // vai direto para a prova

            yield return Aperta(GamepadButton.South); // A = Normal
            yield return WaitUntil(() => gm.IsPlaying, 3f, "A no menu não começou a prova");
            Assert.AreEqual(Difficulty.Normal, gm.CurrentDifficulty);
            Assert.IsTrue(Controls.UsingGamepad, "a ajuda tinha que passar a mostrar os botões do controle");
            yield return Wait(0.3f);

            // X = responder: abre a pergunta; X/Y/A/B escolhem a alternativa
            var p = PlayerController.I;
            yield return Aperta(GamepadButton.West);
            yield return WaitUntil(() => QuizChallenge.I.Active, 2f, "X não abriu a pergunta");
            StringAssert.Contains("X, Y, A ou B", QuizHelp(), "a ajuda da pergunta tinha que mostrar os botões do controle");
            yield return Aperta(GamepadButton.North);
            yield return WaitUntil(() => QuizChallenge.I.Answered || !QuizChallenge.I.Active, 2f, "Y não respondeu");
            yield return WaitUntil(() => !QuizChallenge.I.Active, 4f, "a pergunta não fechou");

            // painel de colas com os botões do controle
            StringAssert.Contains("[LB]", p.Cheats.PanelText());

            // Start pausa e despausa
            yield return Aperta(GamepadButton.Start);
            Assert.AreEqual(Phase.Paused, gm.Phase, "Start não pausou");
            yield return Aperta(GamepadButton.Start);
            Assert.AreEqual(Phase.Playing, gm.Phase, "Start não despausou");

            // B levanta; analógico esquerdo anda
            yield return Aperta(GamepadButton.East);
            Assert.IsFalse(p.IsSeated, "B não levantou");
            Vector3 antes = p.transform.position;
            InputSystem.QueueStateEvent(pad, new GamepadState { leftStick = new Vector2(0f, 1f) });
            yield return Wait(0.6f);
            InputSystem.QueueStateEvent(pad, new GamepadState());
            yield return null;
            Assert.Greater(Flat.Distance(antes, p.transform.position), 0.4f, "o analógico esquerdo não andou");
            if (!tutorialAntes) TutorialFlag.Clear();
        }

        static string QuizHelp()
        {
            foreach (var t in HUD.I.GetComponentsInChildren<UnityEngine.UI.Text>(true))
                if (t.text.Contains("antes do tempo") || t.text.Contains("e reze")) return t.text;
            return "";
        }
    }
}
