using NUnit.Framework;

namespace ProvaImpossivel.Tests
{
    /// <summary>Bloco 13 — regras puras da interface (o que aparece na tela).</summary>
    public class InterfaceTests
    {
        [Test]
        public void FlagrantesDesenhaCaixinhasAteOTeto()
        {
            string s = HUD.StrikesLabel(1, 3, 2);
            Assert.AreEqual(3, Contar(s, "["), "uma caixinha por flagrante permitido");
            Assert.AreEqual(1, Contar(s, "[X]"));
            StringAssert.Contains("Favores: 2", s);
            Assert.AreEqual(HUD.MaxStrikeBoxes, Contar(HUD.StrikesLabel(0, HUD.MaxStrikeBoxes, 0), "["), "no teto ainda desenha");
        }

        [Test]
        public void FlagrantesAcimaDoTetoMostraSoONumero()
        {
            string s = HUD.StrikesLabel(2, 99, 2); // o treino tem limite 99
            Assert.AreEqual(0, Contar(s, "["), "acima do teto nenhuma caixinha");
            StringAssert.Contains("Flagrantes: 2", s);
            Assert.Less(s.Length, 40, "a linha tem que caber no painel da NOTA");
            Assert.AreEqual(0, Contar(HUD.StrikesLabel(0, HUD.MaxStrikeBoxes + 1, 0), "["));
        }

        [Test]
        public void ReticenciasViramCirculos()
        {
            Assert.AreEqual("•••", MoodIcon.Glyph("..."), "os pontos da fonte pareciam quadrados");
            Assert.AreEqual("?", MoodIcon.Glyph("?"));
            Assert.AreEqual("!!!", MoodIcon.Glyph("!!!"));
            Assert.AreEqual("O_O", MoodIcon.Glyph("O_O"));
            Assert.AreEqual("", MoodIcon.Glyph(null));
        }

        static int Contar(string s, string sub)
        {
            int n = 0;
            for (int i = s.IndexOf(sub, System.StringComparison.Ordinal); i >= 0; i = s.IndexOf(sub, i + sub.Length, System.StringComparison.Ordinal)) n++;
            return n;
        }
    }
}
