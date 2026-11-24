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

        // ------------------------------------------------------------------ Cara de Paisagem

        [UnityTest]
        public IEnumerator ProvaQueAcabaNoMeioDaCaraDePaisagemFechaOPainel()
        {
            yield return ComecaNormalIsolado();
            var fp = FacePokerChallenge.I;
            bool avisou = false;
            fp.Begin(0.5f, _ => avisou = true);
            yield return null;
            Assert.IsTrue(fp.Active, "pré-condição");
            GameManager.I.DeliverExam();
            yield return null;
            yield return null;
            Assert.IsFalse(fp.Active, "o desafio tinha que fechar com a prova encerrada");
            Assert.IsNull(GameObject.Find("Desafio A"), "o painel não pode ficar na tela de resultado");
            Assert.IsFalse(avisou, "fechar sem resultado não conta como acerto nem erro");
        }

        [UnityTest]
        public IEnumerator PainelDaCaraDePaisagemNoPapelComTextosQueCabem()
        {
            yield return ComecaNormalIsolado();
            var fp = FacePokerChallenge.I;
            fp.Begin(1f, _ => { });
            yield return null;
            var painel = GameObject.Find("Desafio A");
            Assert.IsNotNull(painel);
            var titulo = painel.transform.Find("Titulo").GetComponent<UnityEngine.UI.Text>();
            Assert.AreEqual(UITheme.StampRed, titulo.color, "título carimbado em vermelho, como a HUD de papel");
            foreach (var nome in new[] { "Titulo", "Ajuda", "Status", "Tempo", "Feedback" })
            {
                var t = painel.transform.Find(nome).GetComponent<UnityEngine.UI.Text>();
                var r = t.rectTransform.rect;
                Assert.GreaterOrEqual(UITheme.Contrast(t.color, UITheme.Paper), 4.5f, nome + " ilegível no papel");
                Assert.LessOrEqual(t.preferredHeight, r.height + 2f, nome + " não cabe na altura");
            }
            // espera o status ser escrito e confere a largura dele (a linha mais longa do painel)
            yield return Wait(0.2f);
            var status = painel.transform.Find("Status").GetComponent<UnityEngine.UI.Text>();
            Assert.LessOrEqual(status.cachedTextGenerator.lineCount, 1, "status quebrou em duas linhas");
            fp.Cancel();
        }

        // ------------------------------------------------------------------ HUD

        [UnityTest]
        public IEnumerator MensagemCinzaNaoTiraOutraDaTela()
        {
            yield return ComecaNormalIsolado();
            var hud = HUD.I;
            hud.Toast("primeira", HUD.Yellow, 10f);
            hud.Toast("segunda", HUD.Red, 10f);
            hud.Toast("informativa", HUD.Gray, 10f);
            yield return null;
            var textos = hud.ToastTexts();
            CollectionAssert.Contains(textos, "primeira");
            CollectionAssert.Contains(textos, "segunda");
            CollectionAssert.DoesNotContain(textos, "informativa", "com a pilha cheia, a cinza fica de fora");
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
