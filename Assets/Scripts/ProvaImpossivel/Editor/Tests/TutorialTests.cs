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

        [Test]
        public void OrdemDosPassos()
        {
            CollectionAssert.AreEqual(new[]
            {
                TutorialStep.Andar, TutorialStep.Sentar, TutorialStep.Responder, TutorialStep.Celular,
                TutorialStep.Nerd, TutorialStep.Distracao, TutorialStep.Entregar, TutorialStep.Concluido,
            }, TutorialSteps.Order);
        }

        [Test]
        public void PercorreTodosOsPassosNaOrdem()
        {
            var p = new TutorialProgress { seated = true };
            var step = TutorialStep.Andar;
            Assert.AreEqual(TutorialStep.Andar, TutorialSteps.Next(step, p), "sentado e parado: não avança");

            p.seated = false; p.distanceFromSeat = 0.5f;
            Assert.AreEqual(TutorialStep.Andar, TutorialSteps.Next(step, p), "levantou mas não andou");
            p.distanceFromSeat = TutorialSteps.WalkDistance + 0.1f;
            step = TutorialSteps.Next(step, p); Assert.AreEqual(TutorialStep.Sentar, step);

            Assert.AreEqual(TutorialStep.Sentar, TutorialSteps.Next(step, p), "ainda em pé");
            p.seated = true;
            step = TutorialSteps.Next(step, p); Assert.AreEqual(TutorialStep.Responder, step);
            p.answers = 1;
            step = TutorialSteps.Next(step, p); Assert.AreEqual(TutorialStep.Celular, step);
            p.phoneUses = 1;
            step = TutorialSteps.Next(step, p); Assert.AreEqual(TutorialStep.Nerd, step);
            p.nerdCopies = 1;
            step = TutorialSteps.Next(step, p); Assert.AreEqual(TutorialStep.Distracao, step);
            p.favorsSpent = 1;
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
                seated = true, distanceFromSeat = 0f, answers = 1, phoneUses = 1, nerdCopies = 1, favorsSpent = 1, delivered = true,
            };
            Assert.AreEqual(TutorialStep.Andar, TutorialSteps.Next(TutorialStep.Andar, tudo), "sem ter andado, não sai do primeiro passo");
            Assert.AreEqual(TutorialStep.Celular, TutorialSteps.Next(TutorialStep.Responder, tudo), "avança só um");
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
