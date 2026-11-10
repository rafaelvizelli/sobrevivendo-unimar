using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace ProvaImpossivel.Tests
{
    /// <summary>
    /// Leitura no mundo: o professor e os colegas mostram o que sentem no corpo (ícone em cima da cabeça,
    /// pausa antes de virar, "zzz" crescendo), e o HUD fica só com tempo, nota, flagrantes e favores.
    /// </summary>
    public class LeituraNoMundoTests
    {
        // ------------------------------------------------------------------ ícone do professor

        [Test]
        public void IconeDoProfessorPorEstado()
        {
            Assert.AreEqual("!!!", ProfessorAI.MoodFor(ProfState.Flagrante, 0f, false, false, false).symbol);
            Assert.AreEqual("!", ProfessorAI.MoodFor(ProfState.Investigando, 0f, false, false, false).symbol);
            Assert.AreEqual("?", ProfessorAI.MoodFor(ProfState.Desconfiado, 0f, false, false, false).symbol);
            Assert.AreEqual("...", ProfessorAI.MoodFor(ProfState.Distraido, 0f, true, false, false).symbol, "ao telefone");
            Assert.AreEqual("", ProfessorAI.MoodFor(ProfState.Tranquilo, 0f, false, false, false).symbol, "tranquilo: nada");
            Assert.AreEqual("...", ProfessorAI.MoodFor(ProfState.Tranquilo, 0f, false, true, false).symbol, "pigarreando antes de virar");
        }

        [Test]
        public void IconeDoProfessorCresceEEsquentaQuandoEleEstaPercebendo()
        {
            var pouco = ProfessorAI.MoodFor(ProfState.Tranquilo, 0.2f, false, false, false);
            var muito = ProfessorAI.MoodFor(ProfState.Tranquilo, 0.9f, false, false, false);
            Assert.AreEqual("?", pouco.symbol, "começou a perceber: '?' (era o 'ELE ESTÁ PERCEBENDO' do HUD)");
            Assert.Greater(muito.pulse, pouco.pulse, "quanto mais ele percebe, mais o '?' pulsa");
            Assert.Greater(muito.color.r - muito.color.g, pouco.color.r - pouco.color.g, "e mais vermelho ele fica");
            Assert.AreEqual("?", ProfessorAI.MoodFor(ProfState.Tranquilo, 0.5f, false, true, false).symbol,
                "perceber o jogador tem prioridade sobre o aviso de virada");
        }

        // ------------------------------------------------------------------ Dorminhoco

        [Test]
        public void ZzzDoDorminhocoCrescemAntesDoRonco()
        {
            string a = SleeperStudent.PreSnoreText(0.1f), b = SleeperStudent.PreSnoreText(0.5f), c = SleeperStudent.PreSnoreText(0.9f);
            Assert.Less(a.Length, b.Length);
            Assert.Less(b.Length, c.Length);
        }

        // ------------------------------------------------------------------ ajuda visual por dificuldade

        [Test]
        public void AjudaVisualDiminuiComADificuldade()
        {
            var go = new GameObject("GameManager (valores do código)");
            go.SetActive(false);
            var gm = go.AddComponent<GameManager>();
            try
            {
                Assert.Greater(gm.facil.turnTellSeconds, gm.normal.turnTellSeconds, "Fácil: pausa antes de virar maior");
                Assert.Greater(gm.normal.turnTellSeconds, gm.dificil.turnTellSeconds);
                Assert.Greater(gm.dificil.turnTellSeconds, 0f, "mesmo no Difícil o professor avisa que vai virar (justo)");
                Assert.Greater(gm.facil.snoreWarningSeconds, gm.dificil.snoreWarningSeconds);
                Assert.Greater(gm.dificil.snoreWarningSeconds, 0f);
                Assert.IsTrue(gm.facil.showSuspicionMeter && gm.facil.showStudentHints, "Fácil: todas as ajudas");
                Assert.IsFalse(gm.dificil.showSuspicionMeter || gm.dificil.showStudentHints, "Difícil: só o corpo e o cone");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        // ------------------------------------------------------------------ HUD desacoplado

        [Test]
        public void HudNaoLeProfessorJogadorNerdNemPuxaSaco()
        {
            string path = Path.Combine(Application.dataPath, "Scripts/ProvaImpossivel/UI/HUD.cs");
            Assert.IsTrue(File.Exists(path), path);
            // ignora comentários: só o código conta
            string code = Regex.Replace(File.ReadAllText(path), @"//.*", "");
            foreach (var tipo in new[] { "ProfessorAI", "PlayerController", "NerdStudent", "SnitchStudent" })
                Assert.IsFalse(Regex.IsMatch(code, @"\b" + tipo + @"\b"), "o HUD voltou a ler " + tipo + " diretamente");
        }
    }
}
