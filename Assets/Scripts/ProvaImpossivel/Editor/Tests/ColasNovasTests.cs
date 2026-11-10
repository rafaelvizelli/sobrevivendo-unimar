using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace ProvaImpossivel.Tests
{
    /// <summary>Bloco 15 — colinha, bilhete, fone e garrafinha: regras puras, orçamento e números por dificuldade.</summary>
    public class ColasNovasTests
    {
        // ------------------------------------------------------------------ orçamento

        [Test]
        public void OrcamentoAcabaDepoisDoMaximo()
        {
            var b = new CheatBudget(3);
            Assert.IsTrue(b.TryUse());
            Assert.IsTrue(b.TryUse());
            Assert.IsTrue(b.TryUse());
            Assert.IsFalse(b.TryUse(), "passou do orçamento");
            Assert.AreEqual(3, b.Used);
            Assert.IsTrue(b.Exhausted);
        }

        [Test]
        public void ReservaDeRespostaACaminhoContaNoOrcamento()
        {
            var b = new CheatBudget(2);
            Assert.IsTrue(b.TryReserve(), "bilhete no ar");
            Assert.AreEqual(1, b.Available);
            Assert.IsTrue(b.TryUse(), "celular");
            Assert.IsFalse(b.TryUse(), "a vaga do bilhete a caminho já estava tomada");
            Assert.IsFalse(b.TryReserve());

            b.Cancel(); // o bilhete caiu no chão: a vaga volta
            Assert.AreEqual(1, b.Available);
            Assert.IsTrue(b.TryReserve());
            b.Commit(); // chegou
            Assert.AreEqual((2, 0, 0), (b.Used, b.Reserved, b.Available));
            b.Commit(); // commit sem reserva não faz nada
            b.Cancel();
            Assert.AreEqual((2, 0), (b.Used, b.Reserved));
        }

        [Test]
        public void SemPartidaOOrcamentoNaoBloqueia()
        {
            Assert.IsNull(GameManager.I, "pré-condição: sem GameManager");
            Assert.IsTrue(CheatBudget.CanAnswer);
            Assert.IsTrue(CheatBudget.Spend());
        }

        // ------------------------------------------------------------------ regras por Infracao

        [Test]
        public void ConfiscoDecideSoPeloTipoDaInfracao()
        {
            Assert.AreEqual(CheatItem.Celular, CheatRules.ItemTakenFor(Infracao.Celular));
            Assert.AreEqual(CheatItem.Celular, CheatRules.ItemTakenFor(Infracao.CelularTocando), "celular tocando também é celular");
            Assert.AreEqual(CheatItem.Colinha, CheatRules.ItemTakenFor(Infracao.ColinhaNaManga));
            Assert.AreEqual(CheatItem.Garrafinha, CheatRules.ItemTakenFor(Infracao.Garrafinha));
            foreach (var k in new[] { Infracao.Bilhete, Infracao.EspiarNerd, Infracao.ColaFundao, Infracao.Outra, Infracao.Nenhuma, Infracao.CaraDeCulpado })
                Assert.AreEqual(CheatItem.Nenhum, CheatRules.ItemTakenFor(k), k + " não leva nada");
        }

        [Test]
        public void CadaInfracaoNovaTemFalaPropriaDeFlagrante()
        {
            foreach (var k in new[] { Infracao.ColinhaNaManga, Infracao.Bilhete, Infracao.CelularTocando, Infracao.Garrafinha })
            {
                var falas = Falas.FlagranteDe(k);
                Assert.AreNotSame(Falas.ProfFlagrante, falas, k + " usa a fala genérica");
                Assert.GreaterOrEqual(falas.Length, 2, k + ": poucas falas (vai repetir)");
                foreach (var f in falas) Assert.IsFalse(string.IsNullOrWhiteSpace(f));
            }
            Assert.AreSame(Falas.ProfFlagrante, Falas.FlagranteDe(Infracao.Celular), "as antigas continuam com a fala de sempre");
        }

        // ------------------------------------------------------------------ colinha: sentido de perto

        [Test]
        public void ColinhaDePertoSobeSuspeitaMesmoSemVer()
        {
            Assert.AreEqual(50f * 0.8f * 0.5f, CheatRules.CloseRangeStep(50f, 0.8f, 2f, 3.5f, 0.5f), 1e-4f);
            Assert.AreEqual(0f, CheatRules.CloseRangeStep(50f, 0.8f, 3.6f, 3.5f, 0.5f), "longe, nada");
            Assert.AreEqual(0f, CheatRules.CloseRangeStep(0f, 0.8f, 1f, 3.5f, 0.5f), "sem colinha, nada");
        }

        // ------------------------------------------------------------------ bilhete

        [Test]
        public void BilheteErraMaisDeLonge()
        {
            Assert.AreEqual(0.06f * 1.5f, CheatRules.BilheteMissChance(1.5f, 0.06f), 1e-5f);
            Assert.Greater(CheatRules.BilheteMissChance(5f, 0.06f), CheatRules.BilheteMissChance(1.5f, 0.06f));
            Assert.AreEqual(1f, CheatRules.BilheteMissChance(100f, 0.06f), "nunca passa de 100%");
        }

        [Test]
        public void BilheteSePerdeComOProfessorPerto()
        {
            Assert.IsTrue(CheatRules.BilheteLost(1.5f, 8f, 2.5f, false), "perto de você");
            Assert.IsTrue(CheatRules.BilheteLost(8f, 1f, 2.5f, false), "perto do destino");
            Assert.IsFalse(CheatRules.BilheteLost(8f, 6f, 2.5f, false), "longe dos dois");
            Assert.IsFalse(CheatRules.BilheteLost(1f, 1f, 2.5f, true), "ao telefone ele não pega nada");
        }

        // ------------------------------------------------------------------ fone e atrasos

        [Test]
        public void RespostaComAtrasoChegaNaHoraCerta()
        {
            var d = new DelayedAnswer();
            Assert.IsFalse(d.Busy);
            d.Begin(15f);
            Assert.IsFalse(d.Tick(14.9f));
            Assert.IsTrue(d.Waiting);
            Assert.AreEqual(0.1f, d.SecondsLeft, 1e-3f);
            Assert.IsTrue(d.Tick(0.2f), "chegou");
            Assert.IsTrue(d.Arrived);
            Assert.IsFalse(d.Tick(1f), "só avisa uma vez");
            d.Clear();
            Assert.IsFalse(d.Busy);
        }

        [Test]
        public void CelularVibraSoAsVezesEDentroDaEspera()
        {
            Assert.AreEqual(-1f, CheatRules.FoneRingTime(0.5, 0.5, 0.45f, 15f), "sorteio acima da chance: não vibra");
            float t0 = CheatRules.FoneRingTime(0.1, 0.0, 0.45f, 15f), t1 = CheatRules.FoneRingTime(0.1, 1.0, 0.45f, 15f);
            Assert.AreEqual(3f, t0, 1e-4f, "no mínimo a 20% da espera");
            Assert.AreEqual(13.5f, t1, 1e-4f, "no máximo a 90% da espera (antes de a resposta chegar)");
        }

        // ------------------------------------------------------------------ prova: colas que só preenchem em branco

        static ExamSystem NovaProva(out GameObject go)
        {
            go = new GameObject("ExamSystem (teste)");
            var exam = go.AddComponent<ExamSystem>();
            typeof(ExamSystem).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(exam, null);
            return exam;
        }

        [Test]
        public void ColinhaEGarrafinhaNaoCorrigemErradas()
        {
            var exam = NovaProva(out var go);
            try
            {
                for (int i = 0; i < 10; i++) exam.MarkWrong(i);
                Assert.AreEqual(-1, exam.MarkCorrectBlank(), "sem questão em branco, nada a preencher");
                exam.Questions[4] = QuestionState.EmBranco;
                Assert.AreEqual(4, exam.MarkCorrectBlank());
                Assert.AreEqual(1f, exam.Grade);
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void BilheteCaiNumaQuestaoEmBrancoSorteada()
        {
            var q = new QuestionState[10];
            for (int i = 0; i < 10; i++) q[i] = i % 3 == 0 ? QuestionState.EmBranco : QuestionState.Certa; // em branco: 0, 3, 6, 9
            Assert.AreEqual(0, ExamSystem.RandomBlank(q, 0.0));
            Assert.AreEqual(3, ExamSystem.RandomBlank(q, 0.3));
            Assert.AreEqual(6, ExamSystem.RandomBlank(q, 0.6));
            Assert.AreEqual(9, ExamSystem.RandomBlank(q, 1.0), "roll = 1 não pode estourar");
            Assert.AreEqual(-1, ExamSystem.RandomBlank(new QuestionState[3] { QuestionState.Certa, QuestionState.Errada, QuestionState.Certa }, 0.5));
        }

        // ------------------------------------------------------------------ números por dificuldade

        static GameObject gmGo;
        static GameManager gm;

        [OneTimeSetUp]
        public void Criar()
        {
            gmGo = new GameObject("GameManager (teste)");
            gmGo.SetActive(false); // sem Awake: não vira o GameManager.I
            gm = gmGo.AddComponent<GameManager>();
        }

        [OneTimeTearDown]
        public void Destruir() => Object.DestroyImmediate(gmGo);

        [Test]
        public void TodaDificuldadeTemAsColasNovasConfiguradas()
        {
            foreach (var s in new[] { gm.facil, gm.normal, gm.dificil, gm.tutorial })
            {
                Assert.Greater(s.maxCheatAnswers, 0, s.name);
                Assert.Greater(s.colinhaCharges + s.bilheteCharges + s.foneCharges + s.garrafaCharges, 0, s.name);
                Assert.Greater(s.colinhaTime, 0f, s.name);
                Assert.Greater(s.garrafaTime, 0f, s.name);
                Assert.Greater(s.foneUseTime, 0f, s.name);
                Assert.Greater(s.bilheteThrowTime, 0f, s.name);
                Assert.Greater(s.bilheteDelay, 0f, s.name);
                Assert.Greater(s.foneDelay, s.bilheteDelay, s.name + ": o fone demora mais que o bilhete");
                Assert.Greater(s.colinhaDangerDistance, 0f, s.name);
                // garrafinha: menos risco visual que o celular, mais risco sonoro que o "tec-tec" do celular (raio 2,5, ruído 3)
                Assert.Less(s.garrafaSuspicion, s.phoneSuspicion, s.name + ": garrafinha tem que ser menos visível que o celular");
                Assert.Greater(s.garrafaNoise, 3f, s.name + ": o gole tem que ser mais barulhento que o celular");
                Assert.Greater(s.garrafaNoiseRadius, 2.5f, s.name);
            }
        }

        [Test]
        public void OrcamentoECargasDiminuemComADificuldade()
        {
            Assert.Greater(gm.facil.maxCheatAnswers, gm.normal.maxCheatAnswers);
            Assert.Greater(gm.normal.maxCheatAnswers, gm.dificil.maxCheatAnswers);
            Assert.GreaterOrEqual(gm.facil.colinhaCharges, gm.normal.colinhaCharges);
            Assert.GreaterOrEqual(gm.normal.colinhaCharges, gm.dificil.colinhaCharges);
            Assert.Less(gm.facil.colinhaDangerDistance, gm.dificil.colinhaDangerDistance, "no Difícil ele percebe de mais longe");
            Assert.Less(gm.facil.foneRingChance, gm.dificil.foneRingChance);
            Assert.Less(gm.facil.bilheteMissChancePerMeter, gm.dificil.bilheteMissChancePerMeter);
            Assert.Less(gm.facil.foneDelay, gm.dificil.foneDelay);
            // no Difícil o toque do celular é alto o bastante para o professor investigar (65 = ProfessorAI.investigaEm)
            Assert.GreaterOrEqual(gm.dificil.foneRingNoise * gm.dificil.suspicionMultiplier, 65f);
        }

        /// <summary>
        /// Nenhuma cola nova pode ser estritamente melhor que outra: cada uma tem um ponto fraco próprio.
        /// (Tabela de identidade: o que conta é que nenhuma vence todas as colunas.)
        /// </summary>
        [Test]
        public void NenhumaColaEEstritamenteMelhor()
        {
            var s = gm.normal;
            // colunas: tempo de mão, risco visual, ruído, atraso (menor melhor); corrige errada (sim melhor); precisa levantar (não melhor)
            var colas = new (string nome, float tempo, float visual, float ruido, bool corrige, float atraso, bool emPe)[]
            {
                ("celular", 3f, s.phoneSuspicion, 3f, true, 0f, false),
                ("Nerd", 3f, s.copySuspicion, 0f, true, 0f, true), // em pé: fora do lugar, e ele olha de lado
                ("colinha", s.colinhaTime, s.colinhaSuspicion + s.colinhaNearSuspicion, 0f, false, 0f, false),
                ("bilhete", s.bilheteThrowTime, s.bilheteThrowSuspicion, s.bilheteNoise, false, s.bilheteDelay, false),
                ("fone", 2f * s.foneUseTime, s.phoneSuspicion, s.foneRingNoise * s.foneRingChance, true, s.foneDelay, false),
                ("garrafinha", s.garrafaTime, s.garrafaSuspicion, s.garrafaNoise, false, 0f, false),
            };
            for (int i = 0; i < colas.Length; i++)
                for (int j = 0; j < colas.Length; j++)
                {
                    if (i == j) continue;
                    var a = colas[i];
                    var b = colas[j];
                    bool domina = a.tempo <= b.tempo && a.visual <= b.visual && a.ruido <= b.ruido && (a.corrige || !b.corrige) && a.atraso <= b.atraso && (!a.emPe || b.emPe);
                    Assert.IsFalse(domina, a.nome + " é melhor ou igual a " + b.nome + " em tudo");
                }
        }

        // ------------------------------------------------------------------ teclas

        [Test]
        public void TeclasNovasNaoConflitamComAsExistentes()
        {
            string raiz = Path.Combine(Application.dataPath, "Scripts", "ProvaImpossivel");
            var usos = new System.Collections.Generic.Dictionary<string, string>();
            foreach (var arquivo in Directory.GetFiles(raiz, "*.cs", SearchOption.AllDirectories))
            {
                if (arquivo.Contains(Path.DirectorySeparatorChar + "Tests" + Path.DirectorySeparatorChar)) continue;
                foreach (Match m in Regex.Matches(File.ReadAllText(arquivo), @"\b([a-z])Key\b"))
                {
                    string tecla = m.Groups[1].Value, nome = Path.GetFileName(arquivo);
                    if (tecla == "q" || tecla == "b" || tecla == "f" || tecla == "g")
                        Assert.AreEqual("PlayerCheats.cs", nome, "a tecla " + tecla.ToUpper() + " das colas novas também é usada em " + nome);
                }
            }
        }
    }
}
