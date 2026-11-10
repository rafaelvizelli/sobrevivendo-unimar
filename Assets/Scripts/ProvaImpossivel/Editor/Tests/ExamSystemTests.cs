using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ProvaImpossivel.Tests
{
    /// <summary>Regras da prova: questões certas/erradas, anulação e nota. Não precisa entrar em Play.</summary>
    public class ExamSystemTests
    {
        GameObject go;
        ExamSystem exam;

        [SetUp]
        public void SetUp()
        {
            go = new GameObject("ExamSystem (teste)");
            exam = go.AddComponent<ExamSystem>();
            // fora do Play o Unity não chama Awake sozinho
            typeof(ExamSystem).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(exam, null);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(go);

        [Test]
        public void ComecaTudoEmBrancoComNotaZero()
        {
            Assert.AreEqual(10, exam.Questions.Length);
            Assert.AreEqual(0f, exam.Grade);
            Assert.IsTrue(exam.HasBlank);
            Assert.IsTrue(exam.HasRoom);
            Assert.AreEqual(10, exam.Count(QuestionState.EmBranco));
        }

        [Test]
        public void CertaVale1PontoEOcupaAPrimeiraEmBranco()
        {
            Assert.AreEqual(0, exam.MarkCorrect());
            Assert.AreEqual(1, exam.MarkCorrect());
            Assert.AreEqual(2f, exam.Grade);
            Assert.AreEqual(QuestionState.Certa, exam.Questions[1]);
        }

        [Test]
        public void ErradaNaoDaPontoEAcabaQuandoNaoHaBranco()
        {
            for (int i = 0; i < 10; i++) Assert.AreEqual(i, exam.MarkWrong());
            Assert.AreEqual(-1, exam.MarkWrong(), "sem questões em branco, MarkWrong deve falhar");
            Assert.AreEqual(0f, exam.Grade);
            Assert.IsFalse(exam.HasBlank);
            Assert.IsTrue(exam.HasRoom, "questões erradas ainda podem ser corrigidas");
        }

        [Test]
        public void CertaSemBrancoCorrigeUmaErrada()
        {
            for (int i = 0; i < 10; i++) exam.MarkWrong();
            Assert.AreEqual(0, exam.MarkCorrect());
            Assert.AreEqual(1f, exam.Grade);
        }

        [Test]
        public void ProvaCompletaNaoAceitaMaisPontos()
        {
            for (int i = 0; i < 10; i++) exam.MarkCorrect();
            Assert.AreEqual(10f, exam.Grade);
            Assert.IsFalse(exam.HasRoom);
            Assert.AreEqual(-1, exam.MarkCorrect(), "nota não pode passar de 10");
            Assert.AreEqual(10f, exam.Grade);
        }

        [Test]
        public void AnularTiraAUltimaCerta()
        {
            exam.MarkCorrect(); exam.MarkCorrect(); exam.MarkCorrect();
            Assert.IsTrue(exam.Annul());
            Assert.AreEqual(2f, exam.Grade);
            Assert.AreEqual(QuestionState.Errada, exam.Questions[2]);
        }

        [Test]
        public void AnularSemCertasZeraUmaEmBranco()
        {
            Assert.IsFalse(exam.Annul(), "sem questões certas não há ponto para tirar");
            Assert.AreEqual(0f, exam.Grade);
            Assert.AreEqual(1, exam.Count(QuestionState.Errada));
        }

        // ------------------------------------------------ questões que o jogador sabe (Bloco 1)

        [TestCase(4)]
        [TestCase(3)]
        [TestCase(2)]
        [TestCase(0)]
        public void SetKnownMarcaExatamenteAQuantidade(int n)
        {
            exam.SetKnown(n);
            Assert.AreEqual(n, exam.KnownCount);
            exam.SetKnown(n); // sortear de novo não acumula
            Assert.AreEqual(n, exam.KnownCount);
        }

        [Test]
        public void SetKnownNaoPassaDoTotal()
        {
            exam.SetKnown(99);
            Assert.AreEqual(10, exam.KnownCount);
        }

        [Test]
        public void ColaPreencheQuestaoQueNaoSabeAntes()
        {
            exam.SetKnown(3);
            for (int n = 0; n < 7; n++)
            {
                int i = exam.MarkCorrect();
                Assert.IsFalse(exam.Known[i], "a cola gastou uma questão que o jogador sabia responder sozinho");
            }
            Assert.AreEqual(3, exam.Count(QuestionState.EmBranco), "sobraram só as 3 que ele sabe");
        }

        [Test]
        public void ColaCorrigeErradaAntesDeGastarConhecida()
        {
            exam.SetKnown(3);
            int wrong = exam.FirstBlank(false);
            exam.MarkWrong(wrong);
            for (int n = 0; n < 6; n++) exam.MarkCorrect(); // as outras 6 desconhecidas
            Assert.AreEqual(wrong, exam.MarkCorrect(), "deveria corrigir a errada");
            Assert.AreEqual(QuestionState.Certa, exam.Questions[wrong]);
            Assert.AreEqual(3, exam.Count(QuestionState.EmBranco));
        }

        [Test]
        public void MarcarPorIndiceAfetaSoAQuestaoPedida()
        {
            exam.SetKnown(3);
            int k = exam.FirstBlank(true);
            Assert.AreEqual(k, exam.MarkCorrect(k));
            Assert.AreEqual(1f, exam.Grade);
            Assert.AreEqual(-1, exam.MarkCorrect(k), "questão já certa");
            Assert.AreEqual(-1, exam.MarkWrong(k), "só questão em branco pode virar errada");
            Assert.AreEqual(-1, exam.MarkWrong(-1));
            Assert.AreEqual(-1, exam.MarkCorrect(10));
        }

        [Test]
        public void ErradaSemIndicePrefereQuestaoQueNaoSabe()
        {
            exam.SetKnown(4);
            for (int n = 0; n < 6; n++) Assert.IsFalse(exam.Known[exam.MarkWrong()]);
            Assert.IsTrue(exam.Known[exam.MarkWrong()], "só sobraram as conhecidas");
        }

        [Test]
        public void ChuteErradoPodeSerCorrigidoPorCola()
        {
            exam.SetKnown(3);
            int chute = exam.FirstBlank(false);
            exam.MarkWrong(chute);
            for (int n = 0; n < 6; n++) exam.MarkCorrect();
            exam.MarkCorrect();
            Assert.AreEqual(QuestionState.Certa, exam.Questions[chute]);
        }

        [Test]
        public void BancoDeChutesTemQuatroOpcoesDistintasENaoRepete()
        {
            QuizBank.Reset();
            var vistos = new System.Collections.Generic.HashSet<string>();
            for (int n = 0; n < QuizBank.ChuteCount; n++)
            {
                var q = QuizBank.NextChute();
                Assert.AreEqual(4, q.options.Length, q.text);
                Assert.AreEqual(4, new System.Collections.Generic.HashSet<string>(q.options).Count, "alternativas repetidas: " + q.text);
                Assert.IsTrue(vistos.Add(q.text), "pergunta de chute repetida antes de usar todas: " + q.text);
            }
            Assert.GreaterOrEqual(QuizBank.ChuteCount, 10);
            QuizBank.NextChute(); // esgotou: recicla sem travar
        }

        // ---------------------------------------------------------- gabarito do chute sorteado por partida

        [Test]
        public void ChuteMesmaSementeGeraMesmoGabaritoEMesmaOrdem()
        {
            QuizBank.Reset(1234);
            var gabaritoA = QuizBank.GabaritoChutes;
            var ordemA = new System.Collections.Generic.List<string>();
            for (int n = 0; n < 10; n++) { var q = QuizBank.NextChute(); ordemA.Add(q.text + " => " + q.options[0]); }

            QuizBank.Reset(1234);
            CollectionAssert.AreEqual(gabaritoA, QuizBank.GabaritoChutes, "mesma semente, gabarito diferente");
            for (int n = 0; n < 10; n++)
            {
                var q = QuizBank.NextChute();
                Assert.AreEqual(ordemA[n], q.text + " => " + q.options[0], "mesma semente, pergunta/resposta diferente na posição " + n);
            }
        }

        [Test]
        public void ChuteSementesDiferentesGeramGabaritosDiferentes()
        {
            QuizBank.Reset(1);
            var a = QuizBank.GabaritoChutes;
            QuizBank.Reset(2);
            CollectionAssert.AreNotEqual(a, QuizBank.GabaritoChutes);

            // e o gabarito não fica preso na opção original: em várias partidas, cada pergunta já teve mais de uma certa
            var vistas = new System.Collections.Generic.HashSet<int>[QuizBank.ChuteCount];
            for (int i = 0; i < vistas.Length; i++) vistas[i] = new System.Collections.Generic.HashSet<int>();
            for (int seed = 0; seed < 40; seed++)
            {
                QuizBank.Reset(seed);
                var g = QuizBank.GabaritoChutes;
                for (int i = 0; i < g.Length; i++) vistas[i].Add(g[i]);
            }
            for (int i = 0; i < vistas.Length; i++)
                Assert.Greater(vistas[i].Count, 1, "a pergunta de chute " + i + " teve sempre a mesma resposta em 40 partidas");
        }

        [Test]
        public void ChuteNaoRepeteDentroDaPartida()
        {
            Assert.AreEqual(28, QuizBank.ChuteCount);
            for (int seed = 0; seed < 20; seed++)
            {
                QuizBank.Reset(seed);
                var vistos = new System.Collections.Generic.HashSet<string>();
                // uma partida tem 10 questões; mesmo usando o banco inteiro não pode repetir
                for (int n = 0; n < QuizBank.ChuteCount; n++)
                    Assert.IsTrue(vistos.Add(QuizBank.NextChute().text), "chute repetido na partida (semente " + seed + ")");
            }
        }

        [Test]
        public void ChuteSempreTemQuatroOpcoesEExatamenteUmaCerta()
        {
            for (int seed = 0; seed < 10; seed++)
            {
                QuizBank.Reset(seed);
                for (int n = 0; n < QuizBank.ChuteCount; n++)
                {
                    var q = QuizBank.NextChute();
                    Assert.AreEqual(4, q.options.Length, q.text);
                    Assert.AreEqual(4, new System.Collections.Generic.HashSet<string>(q.options).Count, "alternativas repetidas: " + q.text);
                    string certa = QuizBank.RespostaCertaDoDia(q.text);
                    Assert.IsNotNull(certa, q.text);
                    int certas = 0;
                    foreach (var o in q.options) if (o == certa) certas++;
                    Assert.AreEqual(1, certas, "precisa haver exatamente 1 certa: " + q.text);
                    Assert.AreEqual(certa, q.options[0], "a certa do dia tem que estar em options[0] (convenção do QuizChallenge)");
                }
            }
        }

        [Test]
        public void EventoChangedDisparaACadaMudanca()
        {
            int calls = 0;
            exam.Changed += () => calls++;
            exam.MarkCorrect();
            exam.MarkWrong();
            exam.Annul();
            Assert.AreEqual(3, calls);
        }
    }
}
