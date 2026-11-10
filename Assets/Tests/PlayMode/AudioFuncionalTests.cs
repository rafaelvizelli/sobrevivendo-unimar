using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static ProvaImpossivel.Tests.FluxoEProfessorTests;

namespace ProvaImpossivel.Tests
{
    /// <summary>
    /// Bloco 5 — áudio com função de jogo: passos 3D do professor, "hm?" quando ele ouve um ruído,
    /// batimento de tensão que só existe com suspeita alta. (Testa que os sons TOCAM; se soam bem, só ouvindo.)
    /// </summary>
    public class AudioFuncionalTests
    {
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
            yield return null;
        }

        [UnityTest]
        public IEnumerator TodoSomTemClip()
        {
            yield return EnterGame();
            foreach (Sfx s in Enum.GetValues(typeof(Sfx)))
            {
                int antes = SoundFX.PlayCount(s);
                SoundFX.PlayAt(s, Vector3.zero, 0f);
                Assert.AreEqual(antes + 1, SoundFX.PlayCount(s), "sem clip para " + s);
            }
        }

        [UnityTest]
        public IEnumerator ProfessorAndandoFazPassos()
        {
            yield return StartNormal();
            var prof = ProfessorAI.I;
            // manda ele andar até o fundo da sala (como numa investigação)
            prof.Alert(PlayerController.I.transform.position, 70f / GameManager.I.normal.suspicionMultiplier, "teste");
            yield return WaitUntil(() => SoundFX.PlayCount(Sfx.Passo) >= 3, 10f, "o professor andou sem fazer barulho de passos");
        }

        [UnityTest]
        public IEnumerator ProfessorFazHmQuandoOuveRuido()
        {
            yield return StartNormal();
            var prof = ProfessorAI.I;
            int antes = SoundFX.PlayCount(Sfx.Ouviu);
            prof.HearNoise(prof.transform.position + Vector3.right, 5f, 3f, "um teste");
            Assert.AreEqual(antes + 1, SoundFX.PlayCount(Sfx.Ouviu));
        }

        [UnityTest]
        public IEnumerator BatimentoSoComSuspeitaAltaESomeNaPausa()
        {
            yield return StartNormal();
            yield return Wait(0.5f);
            Assert.AreEqual(0f, SoundFX.I.TensionLevel, 0.001f, "sem suspeita não pode haver batimento");

            ProfessorAI.I.Alert(PlayerController.I.transform.position, 70f / GameManager.I.normal.suspicionMultiplier, "teste");
            yield return WaitUntil(() => SoundFX.I.TensionLevel > 0.1f, 3f, "investigando, o batimento deveria aparecer");

            GameManager.I.Pause();
            yield return WaitUntil(() => SoundFX.I.TensionLevel < 0.001f, 3f, "na pausa o batimento deveria sumir");
            GameManager.I.Resume();
        }
    }
}
