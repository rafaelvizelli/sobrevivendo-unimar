using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using static ProvaImpossivel.Tests.FluxoEProfessorTests;

namespace ProvaImpossivel.Tests
{
    /// <summary>Bloco 16 — identidade visual: menu em duas folhas, HUD sem sobreposição, botões com transição curta.</summary>
    public class IdentidadeVisualPlayTests
    {
        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
        }

        static Rect R(RectTransform r)
        {
            var c = new Vector3[4];
            r.GetWorldCorners(c);
            return Rect.MinMaxRect(c[0].x, c[0].y, c[2].x, c[2].y);
        }

        static RectTransform Named(string name)
        {
            var go = GameObject.Find(name);
            Assert.IsNotNull(go, "não achei '" + name + "' (ativo)");
            return (RectTransform)go.transform;
        }

        static void SemSobreposicao(List<(string nome, Rect r)> itens, Rect dentro)
        {
            for (int i = 0; i < itens.Count; i++)
            {
                Assert.IsTrue(dentro.Contains(itens[i].r.min) && dentro.Contains(itens[i].r.max), itens[i].nome + " saiu da folha/tela");
                for (int j = i + 1; j < itens.Count; j++)
                    Assert.IsFalse(itens[i].r.Overlaps(itens[j].r), itens[i].nome + " cobre " + itens[j].nome);
            }
        }

        [UnityTest]
        public IEnumerator MenuTemCapaEFolhaDeDificuldadeComDadosReais()
        {
            bool tutorialVisto = TutorialFlag.Done;
            TutorialFlag.MarkDone();
            try
            {
                yield return EnterGame();
                var hud = HUD.I;
                var gm = GameManager.I;
                var capa = Named("Caixa");
                var botoes = new List<(string, Rect)>();
                foreach (var n in new[] { "BotaoJOGAR", "BotaoTUTORIAL", "BotaoSAIR", "Rodape", "Carimbo" }) botoes.Add((n, R(Named(n))));
                SemSobreposicao(botoes, R(capa));
                // entrega 24/11: rodapé com a versão do jogo
                StringAssert.Contains("v" + Application.version, Named("Rodape").GetComponent<Text>().text);
                Assert.IsFalse(hud.DifficultyPageOpen);

                Named("BotaoJOGAR").GetComponent<Button>().onClick.Invoke();
                Assert.IsTrue(hud.DifficultyPageOpen, "JOGAR abre a folha da dificuldade");
                hud.ShowMenuPage(); // mesmo frame: ignorado (proteção de input duplicado)
                Assert.IsTrue(hud.DifficultyPageOpen, "voltar no mesmo frame em que abriu não vale");
                Assert.AreEqual(Phase.Intro, gm.Phase);

                var folha = R(Named("Folha"));
                var partes = new List<(string, Rect)>();
                var sets = new[] { gm.facil, gm.normal, gm.dificil };
                string[] nomes = { "FÁCIL", "NORMAL", "DIFÍCIL" };
                for (int i = 0; i < 3; i++)
                {
                    var dados = Named("Dados" + nomes[i]).GetComponent<Text>();
                    Assert.AreEqual(HUD.DifficultyData(sets[i]), dados.text, "tempo e nota vêm das DifficultySettings");
                    StringAssert.Contains(GameManager.Fmt(sets[i].minGrade), dados.text);
                    partes.Add(("Botao" + nomes[i], R(Named("Botao" + nomes[i]))));
                    partes.Add(("Dados" + nomes[i], R(dados.rectTransform)));
                    partes.Add(("Frase" + nomes[i], R(Named("Frase" + nomes[i]))));
                    Assert.LessOrEqual(dados.preferredWidth, dados.rectTransform.rect.width, "dados de " + nomes[i] + " não cabem");
                }
                partes.Add(("BotaoVOLTAR", R(Named("BotaoVOLTAR"))));
                SemSobreposicao(partes, folha);

                yield return null;
                hud.ShowMenuPage();
                Assert.IsFalse(hud.DifficultyPageOpen, "VOLTAR fecha a folha");
                Assert.IsNotNull(GameObject.Find("BotaoJOGAR"), "a capa voltou");
            }
            finally
            {
                if (!tutorialVisto) TutorialFlag.Clear();
            }
        }

        [UnityTest]
        public IEnumerator HudDePapelSemSobreposicaoEDentroDaTela()
        {
            yield return EnterGame();
            var gm = GameManager.I;
            gm.Begin(Difficulty.Normal);
            for (int i = 0; i < 5; i++) yield return null;
            var hud = HUD.I;
            // o jogador mexe no prompt e na barra a cada frame: liga os dois e mede no mesmo frame
            hud.SetPrompt("[E] Segure para entregar a prova");
            hud.SetHold(0.5f, "Entregando...", HUD.HoldRisk.Visto);
            var areas = hud.HudPanelRects();
            Assert.GreaterOrEqual(areas.Count, 6, "tempo, NOTA, COLAS, prompt, barra e ajuda");
            var tela = hud.CanvasRect;
            for (int i = 0; i < areas.Count; i++)
            {
                Assert.IsTrue(tela.Contains(areas[i].min) && tela.Contains(areas[i].max), "painel " + i + " saiu da tela");
                for (int j = i + 1; j < areas.Count; j++)
                    Assert.IsFalse(areas[i].Overlaps(areas[j]), "painéis " + i + " e " + j + " se sobrepõem");
            }
            Assert.IsTrue(hud.CheatsTextFits());
            // o texto da linha de flagrantes cabe sem quebrar
            var linha = hud.GradePanel.Find("Flagrantes").GetComponent<Text>();
            Assert.AreEqual(1, linha.cachedTextGenerator.lineCount, "a linha de flagrantes quebrou");
            hud.SetPrompt(null);
            hud.SetHold(-1f, "");
        }

        [UnityTest]
        public IEnumerator BotaoReageAoMouseEmTempoRealMesmoPausado()
        {
            yield return EnterGame();
            var gm = GameManager.I;
            gm.Begin(Difficulty.Normal);
            yield return null;
            gm.Pause();
            yield return null;
            Assert.AreEqual(0f, Time.timeScale);
            var feel = HUD.I.PanelRoot.Find("Pausa").GetComponentInChildren<PaperButtonFeel>();
            Assert.IsNotNull(feel);
            var b = feel.GetComponent<Button>();
            Assert.That(b.colors.fadeDuration, Is.InRange(0.15f, 0.25f));
            feel.OnPointerEnter(null);
            yield return Wait(UITheme.Transition + 0.1f);
            Assert.AreEqual(1f, feel.Hover, 1e-3f, "o hover não terminou com o jogo pausado");
            Assert.AreEqual(UITheme.Pen, feel.label.color, "com o mouse em cima o texto vira caneta azul");
            feel.OnPointerExit(null);
            yield return Wait(UITheme.Transition + 0.1f);
            Assert.AreEqual(0f, feel.Hover, 1e-3f);
            Assert.IsFalse(feel.GetComponentInChildren<Text>().raycastTarget, "o texto não pode roubar o clique");
            gm.Resume();
        }
    }
}
