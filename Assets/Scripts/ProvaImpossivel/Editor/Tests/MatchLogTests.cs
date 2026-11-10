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

        /// <summary>
        /// Bloco 15b: as colas novas entram como 12 colunas NO FIM. As 22 antigas ficam onde estavam (quem lê o CSV pelo
        /// índice continua funcionando) e o cabeçalho tem o mesmo número de colunas que cada linha.
        /// </summary>
        [Test]
        public void ColasNovasNoFimDoCsv()
        {
            MatchLog.Reset();
            MatchLog.ColinhaUsos = 3; MatchLog.ColinhaCertas = 2; MatchLog.ColinhaErradas = 1;
            MatchLog.GarrafaUsos = 1; MatchLog.GarrafaCertas = 1;
            MatchLog.BilheteUsos = 2; MatchLog.BilheteErradas = 2;
            MatchLog.FoneUsos = 1; MatchLog.FoneCertas = 0; MatchLog.FoneErradas = 1;
            MatchLog.Eventos = 7;

            var s = new DifficultySettings { name = "Teste", minGrade = 6f, favors = 3 };
            MatchLog.Write(s, LevelResult.Aprovado, 7f, 0, 10f, favorsLeft: 3);
            var linhas = File.ReadAllLines(MatchLog.FilePath);
            var head = linhas[0].Split(';');
            var c = linhas[^1].Split(';');

            Assert.AreEqual(34, head.Length, "22 colunas antigas + 12 das colas novas");
            Assert.AreEqual(head.Length, c.Length, "linha com o mesmo número de colunas do cabeçalho");
            // as antigas não andaram
            Assert.AreEqual("data", head[0]);
            Assert.AreEqual("eventos", head[19]);
            Assert.AreEqual("7", c[19]);
            Assert.AreEqual("cara_paisagem_falhou", head[21]);
            // as novas, na ordem
            string[] novas = { "colinha_usos", "colinha_certas", "colinha_erradas", "garrafa_usos", "garrafa_certas", "garrafa_erradas",
                               "bilhete_usos", "bilhete_certas", "bilhete_erradas", "fone_usos", "fone_certas", "fone_erradas" };
            string[] valores = { "3", "2", "1", "1", "1", "0", "2", "0", "2", "1", "0", "1" };
            for (int i = 0; i < novas.Length; i++)
            {
                Assert.AreEqual(novas[i], head[22 + i]);
                Assert.AreEqual(valores[i], c[22 + i], novas[i]);
            }
        }

        /// <summary>Um partidas.csv do Bloco 14/15 (22 colunas) ganha o cabeçalho novo; as partidas antigas ficam intactas.</summary>
        [Test]
        public void CsvAntigoGanhaCabecalhoNovoSemPerderPartidas()
        {
            const string antigo = "data;dificuldade;resultado;nota;nota_minima;flagrantes;tipos_flagrante;tempo_restante_s;"
                                + "conhecidas_certas;conhecidas_tentadas;chutes_certos;chutes;celular;nerd_copias;nerd_pegou;"
                                + "fundao_pedidos;fundao_cola_boa;fundao_cola_ruim;favores_usados;eventos;cara_paisagem_ok;cara_paisagem_falhou";
            const string partida = "2026-11-10 15:00:00;Normal;Aprovado;6.0;6.0;0;;12;3;3;2;4;1;0;0;1;0;0;1;5;1;0";
            string path = Path.Combine(Path.GetTempPath(), "partidas_teste_15b.csv");
            File.WriteAllLines(path, new[] { antigo, partida });
            try
            {
                MatchLog.UpgradeHeader(path);
                var linhas = File.ReadAllLines(path);
                Assert.AreEqual(2, linhas.Length);
                Assert.AreEqual(34, linhas[0].Split(';').Length, "cabeçalho novo");
                StringAssert.StartsWith(antigo + ";colinha_usos", linhas[0]);
                Assert.AreEqual(partida, linhas[1], "a partida antiga não pode mudar");

                MatchLog.UpgradeHeader(path); // de novo: já está atualizado, nada muda
                Assert.AreEqual(linhas, File.ReadAllLines(path));

                File.WriteAllLines(path, new[] { "outra;coisa", "1;2" }); // cabeçalho que não é nosso: não mexe
                MatchLog.UpgradeHeader(path);
                Assert.AreEqual("outra;coisa", File.ReadAllLines(path)[0]);
            }
            finally { File.Delete(path); }
        }

        [Test]
        public void ResetZeraAsColasNovas()
        {
            MatchLog.ColinhaUsos = MatchLog.GarrafaCertas = MatchLog.BilheteErradas = MatchLog.FoneUsos = 4;
            MatchLog.Reset();
            Assert.AreEqual(0, MatchLog.ColinhaUsos);
            Assert.AreEqual(0, MatchLog.GarrafaCertas);
            Assert.AreEqual(0, MatchLog.BilheteErradas);
            Assert.AreEqual(0, MatchLog.FoneUsos);
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
