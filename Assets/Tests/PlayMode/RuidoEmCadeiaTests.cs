using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;
using static ProvaImpossivel.Tests.FluxoEProfessorTests;

namespace ProvaImpossivel.Tests
{
    /// <summary>
    /// Bloco 6 — ruído como evento: quem faz barulho não conhece quem ouve; professor e Puxa-saco reagem cada um
    /// do seu jeito, e os ouvintes não se acumulam entre reinícios (o domínio não recarrega ao dar Play).
    /// </summary>
    public class RuidoEmCadeiaTests
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
        public IEnumerator OuvintesNaoSeAcumulamEntreReinicios()
        {
            yield return EnterGame();
            Assert.AreEqual(2, Noise.ListenerCount, "professor + Puxa-saco");
            GameManager.I.Restart();
            yield return WaitUntil(() => GameManager.I != null && GameManager.I.Phase == Phase.Intro && ProfessorAI.I != null, 15f, "não reiniciou");
            yield return null;
            Assert.AreEqual(2, Noise.ListenerCount, "ouvintes da cena anterior ficaram pendurados no evento");
        }

        [UnityTest]
        public IEnumerator RuidoDoJogadorChegaAoProfessorSemReferenciaDireta()
        {
            yield return StartNormal();
            var prof = ProfessorAI.I;
            var p = PlayerController.I;
            Assert.IsTrue(NavMesh.SamplePosition(p.transform.position, out var hit, 3f, NavMesh.AllAreas));
            prof.GetComponent<NavMeshAgent>().Warp(hit.position);
            Assert.AreEqual(0f, prof.Suspicion, "pré-condição");
            p.MakeNoise(4f, 3f, "um barulho de teste");
            Assert.Greater(prof.Suspicion, 0f, "o professor perto não ouviu o ruído emitido pelo jogador");
        }

        [UnityTest]
        public IEnumerator PuxaSacoViraQuandoOuveBarulhoPerto()
        {
            yield return StartNormal();
            var puxa = Object.FindAnyObjectByType<SnitchStudent>();
            Assert.IsFalse(puxa.Watching, "pré-condição");

            // longe: ele não liga
            Noise.Emit(puxa.transform.position + Vector3.right * 9f, 4f, 3f, "algo longe", byPlayer: true);
            yield return Wait(2.2f);
            Assert.IsFalse(puxa.Watching, "barulho fora do raio fez o Puxa-saco virar");

            // barulho de NPC (não do jogador): também não
            Noise.Emit(puxa.transform.position, 4f, 3f, "o Fundão", byPlayer: false);
            yield return Wait(2.2f);
            Assert.IsFalse(puxa.Watching, "Puxa-saco só reage a barulho do jogador");

            // perto: vira para vigiar em instantes
            Noise.Emit(puxa.transform.position + Vector3.right, 4f, 3f, "uma cadeira", byPlayer: true);
            Assert.IsTrue(puxa.AboutToWatch, "deveria avisar que vai virar");
            yield return WaitUntil(() => puxa.Watching, 3f, "o Puxa-saco ouviu e não virou");
        }
    }
}
