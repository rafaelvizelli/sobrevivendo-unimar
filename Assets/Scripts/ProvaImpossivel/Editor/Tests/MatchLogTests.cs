using System.IO;
using NUnit.Framework;

namespace ProvaImpossivel.Tests
{
    /// <summary>Registro de partidas (Bloco 4): uma linha CSV por partida, com os contadores zerados a cada prova.</summary>
    public class MatchLogTests
    {
        [Test]
        public void GravaUmaLinhaComOsContadoresDaPartida()
        {
            MatchLog.Reset();
            MatchLog.Quiz(chute: false, ok: true);
            MatchLog.Quiz(chute: false, ok: false);
            MatchLog.Quiz(chute: true, ok: true);
            MatchLog.Celular++;
            MatchLog.Flagrante(Infracao.Celular);
            MatchLog.Flagrante(Infracao.EspiarNerd);

            var s = new DifficultySettings { name = "Teste", minGrade = 6f, favors = 3 };
            int antes = File.Exists(MatchLog.FilePath) ? File.ReadAllLines(MatchLog.FilePath).Length : 0;
            MatchLog.Write(s, LevelResult.Reprovado, 4f, 2, 30.4f, favorsLeft: 1);

            var linhas = File.ReadAllLines(MatchLog.FilePath);
            Assert.AreEqual(antes == 0 ? 2 : antes + 1, linhas.Length, "deveria acrescentar uma linha (e o cabeçalho se o arquivo era novo)");
            StringAssert.StartsWith("data;dificuldade;", linhas[0]);
            var c = linhas[^1].Split(';');
            Assert.AreEqual("Teste", c[1]);
            Assert.AreEqual("Reprovado", c[2]);
            Assert.AreEqual("4.0", c[3]);
            Assert.AreEqual("Celular|EspiarNerd", c[6]);
            Assert.AreEqual("30", c[7]);
            Assert.AreEqual("1", c[8], "conhecidas certas");
            Assert.AreEqual("2", c[9], "conhecidas tentadas");
            Assert.AreEqual("1", c[10], "chutes certos");
            Assert.AreEqual("1", c[12], "celular");
            Assert.AreEqual("2", c[18], "favores usados");
        }

        [Test]
        public void ResetZeraTudo()
        {
            MatchLog.Quiz(true, true);
            MatchLog.NerdCopias = 3;
            MatchLog.Reset();
            Assert.AreEqual(0, MatchLog.Chutes);
            Assert.AreEqual(0, MatchLog.NerdCopias);
        }
    }
}
