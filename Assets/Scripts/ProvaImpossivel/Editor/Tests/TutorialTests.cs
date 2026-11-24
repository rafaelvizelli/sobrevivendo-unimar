using NUnit.Framework;
using UnityEngine;

namespace ProvaImpossivel.Tests
{
    /// <summary>Tutorial: a flag de "já vi" (PlayerPrefs) e a ordem dos passos.</summary>
    public class TutorialTests
    {
        int? flagAntes;

        // os testes mexem no PlayerPrefs de verdade: guarda e devolve o valor de quem está jogando
        [SetUp]
        public void Guardar() => flagAntes = PlayerPrefs.HasKey(TutorialFlag.Key) ? PlayerPrefs.GetInt(TutorialFlag.Key) : null;

        [TearDown]
        public void Devolver()
        {
            if (flagAntes.HasValue) PlayerPrefs.SetInt(TutorialFlag.Key, flagAntes.Value);
            else PlayerPrefs.DeleteKey(TutorialFlag.Key);
            PlayerPrefs.Save();
        }

        // ------------------------------------------------------------------ flag

        [Test]
        public void QuizBankZeraNoPadraoDoNoise()
        {
            // CLAUDE.md: estado estático zerado a cada Play como Noise.ResetOnPlay (RuntimeInitializeOnLoadMethod)
            var reset = typeof(QuizBank).GetMethod("Reset", System.Type.EmptyTypes);
            Assert.IsNotNull(reset);
            Assert.IsNotNull(System.Attribute.GetCustomAttribute(reset, typeof(RuntimeInitializeOnLoadMethodAttribute)),
                "QuizBank.Reset() deveria ter [RuntimeInitializeOnLoadMethod]");
        }

        [Test]
        public void FlagComecaDesligadaELigaAoTerminar()
        {
            TutorialFlag.Clear();
            Assert.IsFalse(TutorialFlag.Done, "sem a chave, o tutorial ainda não foi visto");
            TutorialFlag.MarkDone();
            Assert.IsTrue(TutorialFlag.Done);
            Assert.AreEqual(1, PlayerPrefs.GetInt(TutorialFlag.Key), "gravado no PlayerPrefs");
            TutorialFlag.MarkDone();
            Assert.IsTrue(TutorialFlag.Done, "marcar de novo não desfaz");
            TutorialFlag.Clear();
            Assert.IsFalse(TutorialFlag.Done);
        }

        // ------------------------------------------------------------------ ordem dos passos

        /// <summary>S3: começa sentado respondendo; as colas de bolso (com o orçamento) entram depois do celular.</summary>
        [Test]
        public void OrdemDosPassos()
        {
            CollectionAssert.AreEqual(new[]
            {
                TutorialStep.Responder, TutorialStep.Celular, TutorialStep.ColasDeBolso, TutorialStep.Nerd,
                TutorialStep.Distracao, TutorialStep.CaraDePaisagem, TutorialStep.Entregar, TutorialStep.Concluido,
            }, TutorialSteps.Order);
        }

        [Test]
        public void PercorreTodosOsPassosNaOrdem()
        {
            var p = new TutorialProgress();
            var step = TutorialSteps.Order[0];
            Assert.AreEqual(TutorialStep.Responder, step, "o treino começa sentado, respondendo");
            Assert.AreEqual(TutorialStep.Responder, TutorialSteps.Next(step, p), "sem responder: não avança");
            p.answers = 1;
            step = TutorialSteps.Next(step, p); Assert.AreEqual(TutorialStep.Celular, step);
            p.phoneUses = 1;
            step = TutorialSteps.Next(step, p); Assert.AreEqual(TutorialStep.ColasDeBolso, step);
            Assert.AreEqual(TutorialStep.ColasDeBolso, TutorialSteps.Next(step, p), "sem usar colinha/garrafinha/bilhete/fone: não avança");
            p.pocketCheats = 1;
            step = TutorialSteps.Next(step, p); Assert.AreEqual(TutorialStep.Nerd, step);
            p.nerdCopies = 1;
            step = TutorialSteps.Next(step, p); Assert.AreEqual(TutorialStep.Distracao, step);
            p.favorsSpent = 1;
            step = TutorialSteps.Next(step, p); Assert.AreEqual(TutorialStep.CaraDePaisagem, step);
            Assert.AreEqual(TutorialStep.CaraDePaisagem, TutorialSteps.Next(step, p), "sem treinar a Cara de Paisagem: não avança");
            p.faceDone = true;
            step = TutorialSteps.Next(step, p); Assert.AreEqual(TutorialStep.Entregar, step);
            p.delivered = true;
            step = TutorialSteps.Next(step, p); Assert.AreEqual(TutorialStep.Concluido, step);
            Assert.AreEqual(TutorialStep.Concluido, TutorialSteps.Next(step, p), "concluído é o fim");
        }

        [Test]
        public void NaoPulaEtapasMesmoComTudoFeito()
        {
            // fez tudo fora de ordem (ex.: usou o celular antes de responder): ainda avança um passo por vez
            var tudo = new TutorialProgress
            {
                answers = 1, phoneUses = 1, pocketCheats = 1, nerdCopies = 1, favorsSpent = 1, delivered = true,
            };
            Assert.AreEqual(TutorialStep.Celular, TutorialSteps.Next(TutorialStep.Responder, tudo), "avança só um");
            Assert.AreEqual(TutorialStep.Nerd, TutorialSteps.Next(TutorialStep.ColasDeBolso, tudo), "avança só um");
            var nada = new TutorialProgress { phoneUses = 1, nerdCopies = 1 };
            Assert.AreEqual(TutorialStep.Responder, TutorialSteps.Next(TutorialStep.Responder, nada), "sem responder, não sai do primeiro passo");
        }

        [Test]
        public void CadaPassoTemTextoCurtoNasFalas()
        {
            foreach (var s in TutorialSteps.Order)
            {
                if (s == TutorialStep.Concluido) continue;
                string t = TutorialSteps.Text(s);
                Assert.IsFalse(string.IsNullOrEmpty(t), s + " sem texto");
                Assert.LessOrEqual(t.Length, 100, s + ": texto longo demais para uma linha do HUD");
            }
        }

        [Test]
        public void DificuldadeDeTreinoNaoFlagraENaoSorteiaAcontecimentos()
        {
            var go = new GameObject("GameManager (valores do código)");
            go.SetActive(false);
            var gm = go.AddComponent<GameManager>();
            try
            {
                var t = gm.tutorial;
                Assert.IsTrue(t.professorNeverCatches, "no treino o professor não flagra de verdade");
                Assert.IsFalse(gm.facil.professorNeverCatches || gm.normal.professorNeverCatches || gm.dificil.professorNeverCatches,
                    "na prova de verdade ele flagra");
                Assert.AreEqual(0f, t.threatEventWeight, "treino sem ameaças sorteadas");
                Assert.Greater(t.eventInterval.x, t.duration, "treino sem acontecimentos sorteados");
                Assert.GreaterOrEqual(t.knownQuestions, 1, "precisa de uma questão conhecida pro passo 'responder'");
                Assert.GreaterOrEqual(t.phoneCharges, 1);
                Assert.GreaterOrEqual(t.nerdCopies, 1);
                Assert.GreaterOrEqual(t.favors, 1);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
