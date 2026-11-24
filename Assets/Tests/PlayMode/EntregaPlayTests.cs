using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static ProvaImpossivel.Tests.FluxoEProfessorTests;

namespace ProvaImpossivel.Tests
{
    /// <summary>Polimento da entrega 1.0 (24/11): trilha, Cara de Paisagem no tutorial e correções do fluxo.</summary>
    public class EntregaPlayTests
    {
        [TearDown]
        public void Limpa() => Time.timeScale = 1f;

        /// <summary>Normal sem acontecimentos sorteados nem viradas do Puxa-saco (só o que o teste provoca).</summary>
        static IEnumerator ComecaNormalIsolado()
        {
            yield return EnterGame();
            var s = GameManager.I.normal;
            s.eventInterval = new Vector2(9999f, 9999f);
            s.snitchInterval = new Vector2(9999f, 9999f);
            s.nerdGlanceInterval = new Vector2(999f, 999f);
            GameManager.I.Begin(Difficulty.Normal);
            yield return null;
        }

        // ------------------------------------------------------------------ trilha

        [UnityTest]
        public IEnumerator TrilhaCalmaSemEstouroESemEstaloNoLoop()
        {
            yield return ComecaNormalIsolado();
            var m = MusicDirector.I;
            Assert.IsNotNull(m);
            for (int i = 0; i < 3; i++) Assert.LessOrEqual(m.LayerPeak(i), 1f, "camada " + i + " estoura");
            Assert.LessOrEqual(m.LayerPeak(0), 0.8f + 1e-4f, "a calma é normalizada com folga");
            Assert.Less(m.LoopSeam(0), 0.02f, "a calma dá a volta no loop sem degrau (estalo)");
            Assert.Less(m.LoopSeam(1), 0.02f, "a desconfiança dá a volta no loop sem degrau (estalo)");
            Assert.AreEqual(3, CountLayerSources(), "uma fonte por camada, sem duplicar (o objeto também guarda os efeitos)");
            Assert.AreEqual(1, Object.FindObjectsByType<MusicDirector>(FindObjectsInactive.Include).Length, "uma trilha só");
        }

        static int CountLayerSources()
        {
            int n = 0;
            foreach (var s in Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include))
                if (s.clip != null && s.clip.name.StartsWith("musica_") && s.loop) n++;
            return n;
        }

        [UnityTest]
        public IEnumerator HmDoProfessorAbaixaATrilhaEElaVolta()
        {
            yield return ComecaNormalIsolado();
            var m = MusicDirector.I;
            yield return Wait(2f); // o sinal de início também é som importante: deixa passar
            Assert.Greater(m.Duck, 0.95f, "pré-condição: trilha cheia");
            SoundFX.Play(Sfx.Ouviu);
            yield return Wait(0.6f);
            Assert.Less(m.Duck, 0.7f, "com o 'hm?' a trilha tinha que abaixar");
            yield return Wait(2.5f);
            Assert.Greater(m.Duck, 0.95f, "depois a trilha volta");
        }

        [UnityTest]
        public IEnumerator FimDaProvaTiraATrilhaDevagar()
        {
            yield return ComecaNormalIsolado();
            var m = MusicDirector.I;
            yield return Wait(1.5f);
            Assert.Greater(m.Layer(0), 0.5f, "pré-condição");
            GameManager.I.DeliverExam();
            yield return Wait(0.3f);
            Assert.Greater(m.Layer(0), 0.2f, "sai devagar, não corta seco");
            yield return Wait(2.5f);
            Assert.Less(m.Layer(0), 0.02f, "na tela de resultado a trilha já saiu");
        }
    }
}
