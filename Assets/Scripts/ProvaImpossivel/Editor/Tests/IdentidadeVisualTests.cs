using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace ProvaImpossivel.Tests
{
    /// <summary>Bloco 16 — tema "prova impressa": fontes, acentos, contraste e nenhuma cor solta no HUD/UIFactory.</summary>
    public class IdentidadeVisualTests
    {
        const string Acentos = "ãçéõêÃÇÉÕÊáíóúâôàÁÍÓÚÂÔÀü×•—";

        [Test]
        public void FontesDoTemaVemDaPastaFontsENaoDoSistema()
        {
            var tw = Resources.Load<Font>("SpecialElite-Regular");
            var hand = Resources.Load<Font>("Caveat-Regular");
            Assert.IsNotNull(tw, "Special Elite não está em Assets/Fonts/Resources");
            Assert.IsNotNull(hand, "Caveat não está em Assets/Fonts/Resources");
            Assert.AreSame(tw, UITheme.Typewriter);
            Assert.AreSame(hand, UITheme.Hand);
            Assert.IsFalse(UITheme.UsingFallbackFont);
        }

        [Test]
        public void LicencasFicamJuntoDasFontes()
        {
            string dir = Path.Combine(Application.dataPath, "Fonts", "Resources");
            StringAssert.Contains("Apache License", File.ReadAllText(Path.Combine(dir, "SpecialElite-LICENSE.txt")));
            StringAssert.Contains("SIL Open Font License", File.ReadAllText(Path.Combine(dir, "Caveat-OFL.txt")));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void AcentosExistemNasDuasFontes(bool hand)
        {
            var f = hand ? UITheme.Hand : UITheme.Typewriter;
            foreach (int size in new[] { 18, 24, 34 })
            {
                f.RequestCharactersInTexture(Acentos, size, FontStyle.Normal);
                foreach (char c in Acentos)
                {
                    Assert.IsTrue(f.GetCharacterInfo(c, out var info, size, FontStyle.Normal), f.name + " sem o caractere '" + c + "' (" + size + ")");
                    Assert.Greater(info.advance, 0, f.name + ": '" + c + "' sem largura");
                }
            }
        }

        [Test]
        public void TextoNoPapelTemContrasteSuficiente()
        {
            var tintas = new (string nome, Color c)[]
            {
                ("Ink", UITheme.Ink), ("InkSoft", UITheme.InkSoft), ("Pen", UITheme.Pen), ("PenRed", UITheme.PenRed),
                ("StampRed", UITheme.StampRed), ("StampBlue", UITheme.StampBlue), ("InkGreen", UITheme.InkGreen),
                ("InkAmber", UITheme.InkAmber), ("InkOrange", UITheme.InkOrange),
            };
            foreach (var fundo in new[] { UITheme.Paper, UITheme.PaperDark, UITheme.PaperHover })
                foreach (var (nome, c) in tintas)
                    Assert.GreaterOrEqual(UITheme.Contrast(c, fundo), 4.5f, nome + " sobre " + fundo);
            // o número na caixinha certa/errada é papel sobre tinta
            Assert.GreaterOrEqual(UITheme.Contrast(UITheme.Paper, UITheme.InkGreen), 4.5f);
            Assert.GreaterOrEqual(UITheme.Contrast(UITheme.Paper, UITheme.StampRed), 4.5f);
            // texto na caneta sobre o marca-texto (questão que sabe)
            Assert.GreaterOrEqual(UITheme.Contrast(UITheme.Ink, UITheme.Highlighter), 4.5f);
            // cabeçalho impresso/desabilitado: só precisa ser visível (é decoração)
            Assert.GreaterOrEqual(UITheme.Contrast(UITheme.InkFaint, UITheme.Paper), 2.5f);
        }

        [Test]
        public void MensagensComCorDeSinalViramTintaLegivelNoPapel()
        {
            foreach (var sinal in new[] { HUD.Yellow, HUD.Red, HUD.Green, HUD.Orange, HUD.Gray, Color.white, new Color(0.6f, 0.9f, 1f) })
                Assert.GreaterOrEqual(UITheme.Contrast(UITheme.OnPaper(sinal), UITheme.Paper), 4.5f, "sinal " + sinal);
            Assert.AreEqual(UITheme.StampRed, UITheme.StampFor(HUD.Red), "reprovado/expulso: carimbo vermelho");
            Assert.AreEqual(UITheme.StampBlue, UITheme.StampFor(HUD.Green), "aprovado: carimbo azul");
            Assert.AreEqual(UITheme.StampBlue, UITheme.StampFor(HUD.Yellow), "no sufoco: carimbo azul");
        }

        [Test]
        public void CoresDeSinalDoMundoNaoMudaram()
        {
            // ícones de humor e balões continuam como estavam (o Bloco 16 é só menus e HUD)
            Assert.AreEqual(new Color(1f, 0.85f, 0.2f), HUD.Yellow);
            Assert.AreEqual(new Color(1f, 0.32f, 0.3f), HUD.Red);
            Assert.AreEqual(new Color(0.4f, 0.95f, 0.45f), HUD.Green);
            Assert.AreEqual(new Color(1f, 0.6f, 0.2f), HUD.Orange);
            Assert.AreEqual(new Color(0.8f, 0.8f, 0.82f), HUD.Gray);
            Assert.AreEqual(new Color(0.1f, 0.11f, 0.16f, 0.88f), HUD.PanelColor);
        }

        [TestCase("HUD.cs")]
        [TestCase("UIFactory.cs")]
        public void HudEUIFactoryNaoTemCorSolta(string arquivo)
        {
            string src = File.ReadAllText(Path.Combine(Application.dataPath, "Scripts", "ProvaImpossivel", "UI", arquivo));
            Assert.IsFalse(Regex.IsMatch(src, @"new\s+Color\s*\("), arquivo + " tem 'new Color(': a cor tem que vir do UITheme");
            Assert.IsFalse(Regex.IsMatch(src, @"Color\.(red|green|blue|yellow|black|gray|grey|cyan|magenta)\b"), arquivo + " usa cor pronta do Unity");
        }

        [Test]
        public void SpritesDoTemaSaoGeradosUmaVez()
        {
            Assert.IsNotNull(UITheme.PaperSprite);
            Assert.AreSame(UITheme.FrameSprite, UITheme.FrameSprite);
            Assert.Greater(UITheme.DoubleFrameSprite.border.x, 0f, "carimbo em 9 fatias");
            Assert.Greater(UITheme.KeycapSprite.border.x, 0f, "tecla em 9 fatias");
        }

        [Test]
        public void TransicaoEntreUmDecimoEUmQuartoDeSegundo()
        {
            Assert.That(UITheme.Transition, Is.InRange(0.15f, 0.25f));
        }

        // ------------------------------------------------------------------ textos do HUD

        [Test]
        public void LinhaDasColasViraTeclaETexto()
        {
            HUD.SplitKey("[Q] Colinha ×2", out var k, out var r);
            Assert.AreEqual(("Q", "Colinha ×2"), (k, r));
            HUD.SplitKey("[F] VIBRANDO! (F silencia)", out k, out r);
            Assert.AreEqual(("F", "VIBRANDO! (F silencia)"), (k, r));
            HUD.SplitKey("COLAS  •  restam 3", out k, out r);
            Assert.IsNull(k);
            Assert.AreEqual("COLAS  •  restam 3", r);
            HUD.SplitKey("[uma frase longa demais] x", out k, out _);
            Assert.IsNull(k, "colchete longo não é tecla");
        }

        [Test]
        public void TeclasDaAjudaFicamEmCanetaAzul()
        {
            string s = HUD.KeysInPen("[1] Responder    [Espaço] Levantar");
            string pen = "<color=#" + ColorUtility.ToHtmlStringRGB(UITheme.Pen) + ">";
            StringAssert.Contains(pen + "[1]</color>", s);
            StringAssert.Contains(pen + "[Espaço]</color>", s);
            Assert.AreEqual("sem teclas", HUD.KeysInPen("sem teclas"));
        }

        [Test]
        public void FlagrantesNoPapelNuncaPassamDoTeto()
        {
            string s = HUD.StrikesRich(HUD.StrikesLabel(2, HUD.MaxStrikeBoxes, 1));
            Assert.AreEqual(2, Regex.Matches(s, ">X<").Count);
            Assert.AreEqual(HUD.MaxStrikeBoxes, Regex.Matches(s, @"\[").Count);
            Assert.AreEqual(0, Regex.Matches(HUD.StrikesRich(HUD.StrikesLabel(3, 99, 0)), @"\[").Count, "treino: só o número");
        }

        [Test]
        public void DificuldadeMostraTempoENotaDasSettings()
        {
            var s = new DifficultySettings { duration = 480f, minGrade = 6f };
            Assert.AreEqual("8 min • nota mínima 6,0", HUD.DifficultyData(s));
            s.duration = 300f; s.minGrade = 7.5f;
            Assert.AreEqual("5 min • nota mínima 7,5", HUD.DifficultyData(s), "muda junto com o equilíbrio");
        }

        [Test]
        public void TextosNovosEstaoAcentuados()
        {
            StringAssert.Contains("À", Falas.JogoTitulo);
            StringAssert.Contains("Í", Falas.JogoSubtitulo);
            StringAssert.Contains("Ã", Falas.DificuldadeTitulo);
            StringAssert.Contains("â", Falas.InstrucoesControles);
            StringAssert.Contains("í", Falas.DificuldadeFacilFrase);
            StringAssert.Contains("Ç", Falas.InstrucoesTitulo);
        }
    }
}
