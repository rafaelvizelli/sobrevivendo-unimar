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

        // ------------------------------------------------------------------ caminhada (polimento visual)

        [UnityTest]
        public IEnumerator ProfessorAndaSemBracoCruzadoESemPesNaLinhaDoMeio()
        {
            yield return ComecaNormalIsolado();
            var prof = ProfessorAI.I;
            var skin = prof.visual.Skin;
            prof.WalkTo(prof.transform.position + prof.transform.forward * 4f, false);
            float piorBraco = 0f, piorPe = 1f;
            int quadros = 0;
            float fim = Time.time + 2f;
            while (Time.time < fim)
            {
                yield return null;
                if (skin.WalkWeight < 0.9f) continue;
                quadros++;
                piorBraco = Mathf.Max(piorBraco, prof.visual.armsCrossed);
                Vector3 r = prof.transform.right;
                piorPe = Mathf.Min(piorPe, -Vector3.Dot(skin.LeftFoot.position - skin.Hips.position, r),
                                   Vector3.Dot(skin.RightFoot.position - skin.Hips.position, r));
            }
            Assert.Greater(quadros, 10, "a caminhada não chegou a entrar");
            Assert.Less(piorBraco, 0.05f, "andando, ele não pode estar de braços cruzados (mão na barriga)");
            Assert.Greater(piorPe, 0.04f, "cada pé fica do seu lado do quadril (sem andar de passarela): " + piorPe);
            Assert.Less(skin.RunWeight, 0.05f, "andando devagar não toca a corrida");
        }

        [UnityTest]
        public IEnumerator ProfessorParadoContinuaCruzandoOsBracos()
        {
            yield return ComecaNormalIsolado();
            var prof = ProfessorAI.I;
            yield return Wait(2.5f);
            if (prof.State == ProfState.Tranquilo && UnityEngine.AI.NavMesh.SamplePosition(prof.transform.position, out _, 1f, UnityEngine.AI.NavMesh.AllAreas))
            {
                // parado e tranquilo por mais de 0,6 s: os braços cruzam como antes
                prof.WalkTo(prof.transform.position, false);
                yield return Wait(1.8f);
                if (prof.visual.Skin.WalkWeight < 0.1f)
                    Assert.Greater(prof.visual.armsCrossed, 0.5f, "parado e tranquilo, ele cruza os braços");
            }
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
