using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static ProvaImpossivel.Tests.FluxoEProfessorTests;

namespace ProvaImpossivel.Tests
{
    /// <summary>O estado do professor e dos colegas aparece no mundo (corpo e ícone), não no HUD.</summary>
    public class LeituraNoMundoPlayTests
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
        public IEnumerator ProfessorMostraOQueSenteEmCimaDaCabeca()
        {
            yield return StartGame(Difficulty.Normal);
            var prof = ProfessorAI.I;
            var icon = prof.GetComponent<MoodIcon>();
            Assert.IsNotNull(icon, "o professor deveria ter um ícone no mundo");

            prof.Alert(PlayerController.I.transform.position, 35f / GameManager.I.normal.suspicionMultiplier, "teste");
            yield return null;
            Assert.AreEqual("?", icon.Symbol, "desconfiado: '?' em cima da cabeça");
            Assert.IsTrue(icon.MeterVisible, "no Normal a barrinha discreta de suspeita aparece");

            yield return StartGame(Difficulty.Dificil);
            prof = ProfessorAI.I;
            icon = prof.GetComponent<MoodIcon>();
            prof.Alert(PlayerController.I.transform.position, 35f / GameManager.I.dificil.suspicionMultiplier, "teste");
            yield return null;
            Assert.AreEqual("?", icon.Symbol, "o '?' aparece em qualquer dificuldade");
            Assert.IsFalse(icon.MeterVisible, "no Difícil não tem barrinha (configurável)");
        }

        [UnityTest]
        public IEnumerator ProfessorPausaAntesDeVirar()
        {
            yield return StartGame(Difficulty.Normal);
            var prof = ProfessorAI.I;
            yield return null;
            // olhando para +X, mandar ele para um ponto bem atrás (-X) exige virar
            prof.transform.rotation = Quaternion.LookRotation(Vector3.right);
            Vector3 atras = prof.transform.position - Vector3.right * 4f;
            Assert.IsTrue(prof.WalkTo(atras, stareAtPlayer: false), "pré-condição: tranquilo, ele aceita mudar o rumo");
            Assert.IsTrue(prof.TurningSoon, "antes de virar ele deveria parar e pigarrear");
            yield return null;
            Assert.AreEqual("...", prof.GetComponent<MoodIcon>().Symbol, "ícone do aviso de virada");
            Vector3 antes = prof.transform.position;
            yield return Wait(GameManager.I.normal.turnTellSeconds * 0.5f);
            Assert.Less(Vector3.Distance(antes, prof.transform.position), 0.25f, "durante o aviso ele fica parado");
            yield return WaitUntil(() => !prof.TurningSoon, GameManager.I.normal.turnTellSeconds + 1f, "o aviso de virada não terminou");
        }

        [UnityTest]
        public IEnumerator DorminhocoAvisaAntesDeRoncar()
        {
            yield return StartGame(Difficulty.Normal);
            var dorminhoco = Object.FindAnyObjectByType<SleeperStudent>();
            dorminhoco.StartCoroutine(dorminhoco.SnoreWarning(1f));
            yield return null;
            Assert.IsTrue(dorminhoco.Nodding, "antes do ronco ele cabeceia");
            yield return WaitUntil(() => !dorminhoco.Nodding, 3f, "o aviso do ronco não terminou");
        }
    }
}
