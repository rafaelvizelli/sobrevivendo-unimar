using NUnit.Framework;

namespace ProvaImpossivel.Tests
{
    /// <summary>
    /// Acontecimentos: sorteio ponderado entre ameaças e oportunidades (EventDirector) e a regra única de
    /// quando uma distração funciona (ProfessorAI.CanBeDistracted).
    /// </summary>
    public class AcontecimentosTests
    {
        // ------------------------------------------------------------------ escolha ponderada

        [Test]
        public void SorteioPonderadoSegueOsPesos()
        {
            var rng = new System.Random(2024);
            var pesos = new[] { 1f, 3f, 0f, 6f };
            var cont = new int[pesos.Length];
            const int N = 40000;
            for (int i = 0; i < N; i++) cont[EventDirector.WeightedIndex(pesos, rng.NextDouble())]++;
            Assert.AreEqual(0.1, cont[0] / (double)N, 0.01);
            Assert.AreEqual(0.3, cont[1] / (double)N, 0.01);
            Assert.AreEqual(0, cont[2], "peso zero nunca pode sair");
            Assert.AreEqual(0.6, cont[3] / (double)N, 0.01);
        }

        [Test]
        public void SorteioPonderadoNasBordas()
        {
            var pesos = new[] { 1f, 1f };
            Assert.AreEqual(0, EventDirector.WeightedIndex(pesos, 0.0));
            Assert.AreEqual(1, EventDirector.WeightedIndex(pesos, 1.0), "roll = 1 não pode estourar a lista");
            Assert.AreEqual(1, EventDirector.WeightedIndex(new[] { 0f, 2f }, 0.0), "pula o peso zero no começo");
            Assert.AreEqual(-1, EventDirector.WeightedIndex(new[] { 0f, 0f }, 0.5), "sem peso nenhum: -1");
            Assert.AreEqual(-1, EventDirector.WeightedIndex(new[] { -1f }, 0.5), "peso negativo não conta");
        }

        [Test]
        public void AmeacaOuOportunidadePelosPesosDaDificuldade()
        {
            foreach (var (op, ameaca) in new[] { (0.65f, 0.35f), (0.5f, 0.5f), (0.35f, 0.65f) })
            {
                var rng = new System.Random(7);
                int ameacas = 0;
                const int N = 20000;
                for (int i = 0; i < N; i++) if (EventDirector.PickThreat(op, ameaca, rng.NextDouble())) ameacas++;
                Assert.AreEqual(ameaca / (op + ameaca), ameacas / (double)N, 0.015, "pesos " + op + "/" + ameaca);
            }
        }

        [Test]
        public void SemAcontecimentoDeUmLadoUsaOOutro()
        {
            Assert.IsFalse(EventDirector.PickThreat(0f, 1f, 0.5, hasOpportunities: true, hasThreats: false));
            Assert.IsTrue(EventDirector.PickThreat(1f, 0f, 0.5, hasOpportunities: false, hasThreats: true));
            Assert.IsFalse(EventDirector.PickThreat(1f, 0f, 0.99), "peso de ameaça zero nunca sorteia ameaça");
            Assert.IsTrue(EventDirector.PickThreat(0f, 1f, 0.01), "peso de oportunidade zero nunca sorteia oportunidade");
        }

        [Test]
        public void PesosDasDificuldades_FacilMaisOportunidade_DificilMaisAmeaca()
        {
            var go = new UnityEngine.GameObject("GameManager (valores do código)");
            go.SetActive(false);
            var gm = go.AddComponent<GameManager>();
            try
            {
                float Ameaca(DifficultySettings s) => s.threatEventWeight / (s.threatEventWeight + s.opportunityEventWeight);
                Assert.Less(Ameaca(gm.facil), 0.5f, "Fácil: mais oportunidades");
                Assert.AreEqual(0.5f, Ameaca(gm.normal), 0.05f, "Normal: metade e metade");
                Assert.Greater(Ameaca(gm.dificil), 0.5f, "Difícil: mais ameaças");
                foreach (var s in new[] { gm.facil, gm.normal, gm.dificil })
                {
                    Assert.GreaterOrEqual(s.eventInterval.x, 30f, s.name + ": intervalo mínimo entre acontecimentos");
                    Assert.Greater(s.eventInterval.y - s.eventInterval.x, 10f, s.name + ": o intervalo precisa variar");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        // ------------------------------------------------------------------ regra de distração

        [Test]
        public void DistracaoSoFuncionaComProfessorTranquiloOuJaDistraido()
        {
            Assert.IsTrue(ProfessorAI.CanBeDistracted(ProfState.Tranquilo, false, false));
            Assert.IsTrue(ProfessorAI.CanBeDistracted(ProfState.Distraido, false, false), "trocar de distração (cadeias) continua valendo");
            Assert.IsFalse(ProfessorAI.CanBeDistracted(ProfState.Desconfiado, false, false), "desconfiado não cai em distração");
            Assert.IsFalse(ProfessorAI.CanBeDistracted(ProfState.Investigando, false, false));
            Assert.IsFalse(ProfessorAI.CanBeDistracted(ProfState.Flagrante, false, false));
            Assert.IsFalse(ProfessorAI.CanBeDistracted(ProfState.Tranquilo, true, false), "ocupado (desafio/flagrante) nunca");
        }

        [Test]
        public void DistracaoComDesconfiadoEhConfiguravel()
        {
            Assert.IsTrue(ProfessorAI.CanBeDistracted(ProfState.Desconfiado, false, true), "com a opção ligada, desconfiado pode ser distraído");
            Assert.IsFalse(ProfessorAI.CanBeDistracted(ProfState.Investigando, false, true), "investigando, nunca (mesmo com a opção)");
        }
    }
}
