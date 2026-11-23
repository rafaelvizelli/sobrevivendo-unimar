using NUnit.Framework;
using UnityEngine;

namespace ProvaImpossivel.Tests
{
    /// <summary>S6 (Operação Steam): título de estilo, dica da derrota, recorde e conquistas.</summary>
    public class RecordesTests
    {
        static MatchSummary S(LevelResult r, float nota, int flagrantes = 0, int celular = 0, int nerd = 0, int fundao = 0, int furadas = 0,
                              int colinha = 0, int garrafa = 0, int bilhete = 0, int fone = 0, int erradas = 0, int emBranco = 0,
                              Infracao principal = Infracao.Nenhuma) =>
            new(r, "Teste", nota, 7f, 200f, flagrantes, 3, celular, nerd, fundao, furadas, 0, Mathf.RoundToInt(nota), erradas, emBranco,
                colinha, garrafa, bilhete, fone, principal);

        [Test]
        public void TituloSoParaQuemPassouEPeloJeito()
        {
            Assert.IsNull(Records.StyleTitle(S(LevelResult.Reprovado, 5f, celular: 2)));
            Assert.IsNull(Records.StyleTitle(S(LevelResult.Flagrado, 4f, flagrantes: 3)));
            Assert.AreEqual(Falas.TituloEstiloGenio, Records.StyleTitle(S(LevelResult.Aprovado, 10f, celular: 1, colinha: 2)));
            Assert.AreEqual(Falas.TituloEstiloBolso, Records.StyleTitle(S(LevelResult.Aprovado, 8f, colinha: 2, bilhete: 1)));
            Assert.AreEqual(Falas.TituloEstiloCaraDePau, Records.StyleTitle(S(LevelResult.Aprovado, 8f, flagrantes: 2, celular: 3)));
            StringAssert.StartsWith("SOBREVIVENTE DO 7", Records.StyleTitle(S(LevelResult.AprovadoNoSufoco, 8f, celular: 1)));
            StringAssert.StartsWith("SOBREVIVENTE DO 7", Records.StyleTitle(S(LevelResult.Aprovado, 7f, celular: 1)), "passou raspando");
            Assert.AreEqual(Falas.TituloEstiloNerd, Records.StyleTitle(S(LevelResult.Aprovado, 8f, flagrantes: 1, nerd: 2)));
            Assert.AreEqual(Falas.TituloEstiloHonesto, Records.StyleTitle(S(LevelResult.Aprovado, 8f)), "passou sem colar nada");
            Assert.AreEqual(Falas.TituloEstiloNinja, Records.StyleTitle(S(LevelResult.Aprovado, 8f, celular: 1, nerd: 1)));
        }

        [Test]
        public void DicaDizOQueTeDerrubou()
        {
            Assert.IsNull(Records.Tip(S(LevelResult.Aprovado, 8f), 4), "quem passou não ganha sermão");
            Assert.AreEqual(Falas.DicaFlagrante(Infracao.Celular),
                            Records.Tip(S(LevelResult.Flagrado, 3f, flagrantes: 3, principal: Infracao.Celular), 4));
            StringAssert.Contains("3 em branco", Records.Tip(S(LevelResult.Reprovado, 5f, emBranco: 3), 4));
            StringAssert.Contains("4 colas", Records.Tip(S(LevelResult.Reprovado, 5f, emBranco: 3), 4));
            Assert.AreEqual(Falas.DicaErradas, Records.Tip(S(LevelResult.Reprovado, 6f, erradas: 4), 4));
            foreach (Infracao k in System.Enum.GetValues(typeof(Infracao)))
                Assert.IsNotEmpty(Falas.DicaFlagrante(k), "sem dica para " + k);
        }

        [Test]
        public void SoNotaDeQuemPassouViraRecorde()
        {
            Assert.IsTrue(Records.IsNewRecord(-1f, S(LevelResult.Aprovado, 7f)), "primeira aprovação é recorde");
            Assert.IsFalse(Records.IsNewRecord(8f, S(LevelResult.Aprovado, 7f)));
            Assert.IsTrue(Records.IsNewRecord(8f, S(LevelResult.Aprovado, 9f)));
            Assert.IsFalse(Records.IsNewRecord(-1f, S(LevelResult.Reprovado, 6f)), "reprovado não tem recorde");
        }

        [Test]
        public void ConquistasDaPartida()
        {
            var e = Records.Earned(S(LevelResult.Aprovado, 10f, colinha: 1, garrafa: 1, bilhete: 1, fone: 1), hard: true);
            CollectionAssert.IsSupersetOf(e, new[] { Conquista.Passei, Conquista.LisoComoSabao, Conquista.SobreviventeDaUnimar,
                                                     Conquista.MagicoDoBolso, Conquista.GenioDoCrime });
            var f = Records.Earned(S(LevelResult.Flagrado, 2f, flagrantes: 3, fundao: 1, furadas: 1), hard: false);
            CollectionAssert.AreEquivalent(new[] { Conquista.FichaSuja, Conquista.ListaDoMercado }, f);
            CollectionAssert.Contains(Records.Earned(S(LevelResult.AprovadoNoSufoco, 7f, flagrantes: 1), false), Conquista.NoGrito);
            foreach (Conquista c in System.Enum.GetValues(typeof(Conquista)))
                Assert.AreNotEqual(c.ToString(), Falas.NomeConquista(c), "conquista sem nome bonito: " + c);
        }

        [Test]
        public void ProcessGravaRecordeEConquistaSoUmaVez()
        {
            int maskAntes = Records.AchievementMask;
            float recordeAntes = Records.BestGrade("Teste");
            Records.Clear("Teste");
            try
            {
                var a = Records.Process(S(LevelResult.Aprovado, 8f, celular: 1), 4, hard: false);
                Assert.IsTrue(a.newRecord);
                Assert.AreEqual(8f, a.bestGrade);
                CollectionAssert.Contains(a.newAchievements, Conquista.Passei);
                StringAssert.Contains("(NOVO!)", Records.FooterLine(a));
                StringAssert.Contains(Falas.NomeConquista(Conquista.Passei), Records.FooterLine(a));

                var b = Records.Process(S(LevelResult.Aprovado, 7f, celular: 1), 4, hard: false);
                Assert.IsFalse(b.newRecord, "7 não bate 8");
                Assert.AreEqual(8f, b.bestGrade, "o rodapé mostra o recorde que continua valendo");
                CollectionAssert.DoesNotContain(b.newAchievements, Conquista.Passei, "conquista já ganha não aparece de novo");
                Assert.AreEqual(8f, Records.BestGrade("Teste"));
            }
            finally
            {
                Records.Clear("Teste");
                PlayerPrefs.SetInt(Records.KeyAchievements, maskAntes);
                if (recordeAntes >= 0f) PlayerPrefs.SetFloat(Records.Prefix + "teste", recordeAntes);
                PlayerPrefs.Save();
            }
        }
    }
}
