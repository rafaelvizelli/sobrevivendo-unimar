using System;
using System.Text;
using NUnit.Framework;
using UnityEngine;

namespace ProvaImpossivel.Tests
{
    /// <summary>
    /// Simulação de equilíbrio (Monte Carlo): estima, para cada dificuldade, a chance de passar
    /// SEM colar (só conhecidas + chute) e COLANDO COM CUIDADO.
    ///
    /// É um MODELO, não o jogo rodando: a prova, o chute, os recursos (bateria, cópias, favores), os acontecimentos
    /// e a conta da suspeita vêm do jogo (DifficultySettings, ProfessorAI.SuspicionIfSeen, Vision.Weight);
    /// o comportamento do jogador (quando ele age, quanto ele se arrisca) são as SUPOSIÇÕES abaixo.
    /// Serve para comparar configurações entre si; os números finais se confirmam com partidas reais (partidas.csv).
    /// </summary>
    public class SimulacaoDeEquilibrioTests
    {
        // ------------------------------------------------------------------ suposições do modelo (não são regras do jogo)

        /// <summary>Chance de acertar uma questão que ele estudou, por quizTier (perguntas de conhecimento geral).</summary>
        static readonly double[] AcertoConhecidaPorTier = { 0.95, 0.9, 0.8 };
        const float TempoParaResponder = 4f, TempoFeedback = 1.6f;
        /// <summary>Quando o professor vê, ele está a 6 m, olhando a meio caminho entre o centro e a borda do cone.</summary>
        const float DistanciaReferencia = 6f;
        /// <summary>O jogador cuidadoso só age quando o HUD diz "fora da vista": sobra metade do risco do cone.</summary>
        const double FatorCuidado = 0.5;
        /// <summary>Risco residual de ser visto agindo durante uma distração (Puxa-saco, professor perto etc.).</summary>
        const double RiscoNaJanela = 0.03;
        /// <summary>Um favor pedido (Dorminhoco/Puxa-saco) exige andar até o colega: nem sempre sobra janela para agir.</summary>
        const double EficienciaJanelaDeFavor = 0.6;
        /// <summary>Chance de não soltar a tempo quando o Nerd para de escrever.</summary>
        const double FalhaAoSoltarNoNerd = 0.15;
        /// <summary>Espiar em pé e ser visto: o professor vem investigar e pode te achar fora do lugar.</summary>
        const double FlagraForaDoLugarAoInvestigar = 0.5;
        const float DuracaoMediaDeEvento = 8f;
        const float TempoCelular = 3f + 4f;            // segurar + recarga
        const float TempoNerdSentado = 3f + 6f;        // espiar + recarga do Nerd
        const float TempoNerdEmPe = 3f + 6f + 6f;      // + levantar, andar e voltar a sentar
        const float TempoFundao = 12f;                 // ir ao fundo da sala, pedir, voltar
        const float TempoEntrega = 10f;

        /// <summary>O que a simulação precisa saber de uma dificuldade (vem do jogo).</summary>
        public struct Modelo
        {
            public int nerdCopias;
            public bool nerdDeSentado;
            public float pesoOportunidade, pesoAmeaca;
            public float chanceFlagraFundao;
            public float alertaPorAmeaca;
        }

        /// <summary>Monta o modelo a partir dos números do jogo.</summary>
        public static Modelo ModeloDe(DifficultySettings s) => new()
        {
            nerdCopias = s.nerdCopies,
            nerdDeSentado = false, // NerdStudent.CanInteract exige estar em pé
            pesoOportunidade = s.opportunityEventWeight,
            pesoAmeaca = s.threatEventWeight,
            chanceFlagraFundao = s.backRowCaughtChance,
            alertaPorAmeaca = s.threatAlertSeconds,
        };

        public struct Resultado
        {
            public double semCola, comCola, flagradoComCola, colasUsadas;
        }

        public static Resultado Simular(DifficultySettings s, Modelo m, int partidas, int seed)
        {
            var rng = new System.Random(seed);
            double acertoConhecida = AcertoConhecidaPorTier[Mathf.Clamp(s.quizTier, 0, 2)];
            float visRef = Vision.Weight(DistanciaReferencia, s.viewDistance, s.halfFov * 0.5f, s.halfFov);
            float suspCelular = ProfessorAI.SuspicionIfSeen(s, s.phoneSuspicion, visRef, 3f);
            float suspNerd = ProfessorAI.SuspicionIfSeen(s, s.copySuspicion, visRef, 3f);
            double caraDePaisagem = ChanceCaraDePaisagem(s);
            double intervaloNerd = (s.nerdGlanceInterval.x + s.nerdGlanceInterval.y) * 0.5;
            // começar a espiar logo depois de uma olhada só é seguro se a próxima não puder vir antes de 3 s + aviso
            double chanceInterrompeNerd = 0.05 + Math.Max(0.0, (3.0 + 1.5 - s.nerdGlanceInterval.x)
                                          / Math.Max(0.1, s.nerdGlanceInterval.y - s.nerdGlanceInterval.x));
            chanceInterrompeNerd = Math.Min(0.9, chanceInterrompeNerd);
            if (s.nerdGlanceInterval.x < 3f + 1.5f) chanceInterrompeNerd = Math.Max(chanceInterrompeNerd, (3.0 + 1.5) / intervaloNerd);
            float cicloPergunta = TempoParaResponder + TempoFeedback + s.quizCooldown;

            int passouSemCola = 0, passouComCola = 0, flagrado = 0;
            long colas = 0;
            for (int n = 0; n < partidas; n++)
            {
                // -------- respostas próprias: conhecidas e chutes (é o mesmo começo nas duas estratégias)
                int certas = 0;
                float tempo = 0f;
                for (int q = 0; q < 10; q++)
                {
                    if (tempo + cicloPergunta > s.duration - TempoEntrega) break;
                    tempo += cicloPergunta;
                    bool conhecida = q < s.knownQuestions;
                    if (rng.NextDouble() < (conhecida ? acertoConhecida : 0.25)) certas++;
                }
                if (certas >= s.minGrade - 0.001f)
                {
                    passouSemCola++;
                    passouComCola++;
                    continue;
                }

                // -------- acontecimentos da partida
                int janelas = 0, ameacas = 0;
                for (float t = 30f; t < s.duration; t += DuracaoMediaDeEvento + Lerp(s.eventInterval, rng.NextDouble()))
                {
                    double total = m.pesoOportunidade + m.pesoAmeaca;
                    bool ameaca = total > 0 && rng.NextDouble() < m.pesoAmeaca / total;
                    if (ameaca) ameacas++;
                    else janelas++;
                }
                double fracAtento = Math.Min(1.0, ameacas * m.alertaPorAmeaca / s.duration);
                double coneNaSala = Math.Min(0.9, (2.0 * s.halfFov / 360.0) * (s.viewDistance / 12.0));
                double riscoForaDaJanela = Math.Min(0.9, coneNaSala * FatorCuidado * (1.0 + fracAtento));

                // -------- colas, com cuidado: chutou tudo antes; agora corrige as erradas até a nota mínima
                int celular = s.phoneCharges, nerd = m.nerdCopias, favores = s.favors, flagrantes = 0;
                bool celularConfiscado = false;
                while (certas < s.minGrade - 0.001f && flagrantes < s.maxFlagrantes)
                {
                    int acao = !celularConfiscado && celular > 0 ? 0 : nerd > 0 ? 1 : favores > 0 ? 2 : -1;
                    if (acao < 0) break;
                    float custo = acao == 0 ? TempoCelular : acao == 1 ? (m.nerdDeSentado ? TempoNerdSentado : TempoNerdEmPe) : TempoFundao;
                    if (tempo + custo > s.duration - TempoEntrega) break;
                    tempo += custo;
                    colas++;

                    if (acao == 2)
                    {
                        // Fundão (BackRowStudent): 35% derruba tudo, 35% cola (60% boa), 30% show
                        favores--;
                        double r = rng.NextDouble();
                        if (r < 0.35 || r >= 0.7) { janelas++; continue; } // a bagunça vira uma janela
                        bool pego = rng.NextDouble() < riscoForaDaJanela || rng.NextDouble() < m.chanceFlagraFundao;
                        if (pego) { Flagrante(ref flagrantes, ref certas, s); continue; }
                        if (rng.NextDouble() < 0.6) certas++;
                        continue;
                    }

                    // janela: um acontecimento bom, ou um favor gasto pedindo distração
                    bool naJanela = false;
                    if (janelas > 0) { janelas--; naJanela = true; }
                    else if (favores > 0 && (celular > 0 || nerd > 0))
                    {
                        favores--;
                        naJanela = rng.NextDouble() < EficienciaJanelaDeFavor;
                    }
                    double risco = naJanela ? RiscoNaJanela : riscoForaDaJanela;

                    if (acao == 1)
                    {
                        if (rng.NextDouble() < chanceInterrompeNerd)
                        {
                            // ele parou de escrever no meio: soltou (perdeu a vez) ou foi pego (escândalo, sem ponto)
                            if (rng.NextDouble() < FalhaAoSoltarNoNerd) nerd--;
                            continue;
                        }
                    }

                    if (rng.NextDouble() < risco)
                    {
                        float susp = acao == 0 ? suspCelular : suspNerd;
                        bool emPe = acao == 1 && !m.nerdDeSentado;
                        bool flagra = susp >= 100f
                                      || (emPe && rng.NextDouble() < FlagraForaDoLugarAoInvestigar)
                                      || (rng.NextDouble() > caraDePaisagem && susp + 30f >= 100f);
                        if (flagra)
                        {
                            bool advertencia = s.firstFlagranteIsWarning && flagrantes == 0;
                            Flagrante(ref flagrantes, ref certas, s);
                            if (acao == 0) { celular--; if (s.confiscatePhone && !advertencia) celularConfiscado = true; }
                            continue;
                        }
                    }
                    certas++;
                    if (acao == 0) celular--; else nerd--;
                }
                bool passou = flagrantes < s.maxFlagrantes && certas >= s.minGrade - 0.001f;
                if (passou) passouComCola++;
                if (flagrantes >= s.maxFlagrantes) flagrado++;
            }
            return new Resultado
            {
                semCola = passouSemCola / (double)partidas,
                comCola = passouComCola / (double)partidas,
                flagradoComCola = flagrado / (double)partidas,
                colasUsadas = colas / (double)partidas,
            };
        }

        static void Flagrante(ref int flagrantes, ref int certas, DifficultySettings s)
        {
            bool advertencia = s.firstFlagranteIsWarning && flagrantes == 0;
            flagrantes++;
            if (!advertencia && certas > 0) certas--; // ExamSystem.Annul
        }

        static float Lerp(Vector2 v, double t) => (float)(v.x + (v.y - v.x) * t);

        /// <summary>Chance de vencer o Desafio A: 3 acertos antes de passar do limite de tremidas.</summary>
        static double ChanceCaraDePaisagem(DifficultySettings s)
        {
            double p = Math.Min(0.95, 0.5 + s.facePokerZone * 1.5); // suposição: acerto por aperto
            double chance = 0;
            for (int erros = 0; erros <= s.facePokerMisses; erros++)
                chance += Combinacoes(2 + erros, erros) * Math.Pow(p, 3) * Math.Pow(1 - p, erros);
            return chance;
        }

        static double Combinacoes(int n, int k)
        {
            double c = 1;
            for (int i = 1; i <= k; i++) c = c * (n - k + i) / i;
            return c;
        }

        // ------------------------------------------------------------------ testes

        const int Partidas = 20000;

        static GameObject go;
        static GameManager gm;

        [OneTimeSetUp]
        public void Criar()
        {
            // inativo: o Awake não roda (não vira o GameManager.I); só queremos os números padrão do código
            go = new GameObject("GameManager da simulação");
            go.SetActive(false);
            gm = go.AddComponent<GameManager>();
        }

        [OneTimeTearDown]
        public void Destruir() => UnityEngine.Object.DestroyImmediate(go);

        [Test]
        public void TabelaDeEquilibrio()
        {
            var sb = new StringBuilder("\nDificuldade | Sem cola | Com cola (cuidado) | Expulso colando | Colas/partida | Celular visto 3 s (susp.)\n");
            foreach (var s in new[] { gm.facil, gm.normal, gm.dificil })
            {
                var r = Simular(s, ModeloDe(s), Partidas, 12345);
                float visRef = Vision.Weight(DistanciaReferencia, s.viewDistance, s.halfFov * 0.5f, s.halfFov);
                sb.AppendFormat("{0,-11} | {1,7:P1} | {2,17:P1} | {3,14:P1} | {4,13:0.0} | {5,6:0}\n",
                    s.name, r.semCola, r.comCola, r.flagradoComCola, r.colasUsadas, ProfessorAI.SuspicionIfSeen(s, s.phoneSuspicion, visRef, 3f));
            }
            Debug.Log("[Equilibrio]" + sb);
            TestContext.WriteLine(sb.ToString());
        }

        /// <summary>Meta do equilíbrio: no Normal, ~5% passa sem colar e ~50–60% passa colando com cuidado.</summary>
        [Test]
        public void MetaDoNormal_ColaPrecisaSerNecessariaENaoGarantida()
        {
            var r = Simular(gm.normal, ModeloDe(gm.normal), Partidas, 12345);
            Assert.LessOrEqual(r.semCola, 0.08, "Normal: passar sem colar está fácil demais");
            Assert.That(r.comCola, Is.InRange(0.45, 0.70), "Normal: colando com cuidado deveria passar uns 50–60%");
        }

        [Test]
        public void DificuldadesEmOrdem()
        {
            var f = Simular(gm.facil, ModeloDe(gm.facil), Partidas, 1);
            var n = Simular(gm.normal, ModeloDe(gm.normal), Partidas, 1);
            var d = Simular(gm.dificil, ModeloDe(gm.dificil), Partidas, 1);
            Assert.Greater(f.comCola, n.comCola, "Fácil tem que ser mais fácil que o Normal");
            Assert.Greater(n.comCola, d.comCola, "Normal tem que ser mais fácil que o Difícil");
            Assert.Less(f.semCola, 0.35, "mesmo no Fácil, colar tem que fazer diferença");
        }

        /// <summary>No Normal, o celular visto por 3 s dentro do cone (6 m, a meio caminho da borda) dá flagrante — antes de a consulta terminar.</summary>
        [Test]
        public void CelularVistoNoNormalDaFlagrante()
        {
            var s = gm.normal;
            float vis = Vision.Weight(DistanciaReferencia, s.viewDistance, s.halfFov * 0.5f, s.halfFov);
            Assert.GreaterOrEqual(ProfessorAI.SuspicionIfSeen(s, s.phoneSuspicion, vis, 2.8f), 100f,
                "o flagrante tem que vir antes dos 3 s (senão a consulta termina e ele não vê mais nada)");
            // de relance (meio segundo) ainda não é flagrante: dá pra se arrepender
            Assert.Less(ProfessorAI.SuspicionIfSeen(s, s.phoneSuspicion, vis, 0.5f), 30f);
        }

        [Test]
        public void CelularVistoNoFacilSoDaSusto()
        {
            var s = gm.facil;
            float vis = Vision.Weight(DistanciaReferencia, s.viewDistance, s.halfFov * 0.5f, s.halfFov);
            float susp = ProfessorAI.SuspicionIfSeen(s, s.phoneSuspicion, vis, 3f);
            Assert.That(susp, Is.InRange(30f, 99f), "no Fácil, ser visto no celular deixa o professor desconfiado, sem flagrante direto");
        }

        [Test]
        public void MesmaSementeMesmoResultado()
        {
            var a = Simular(gm.normal, ModeloDe(gm.normal), 2000, 7);
            var b = Simular(gm.normal, ModeloDe(gm.normal), 2000, 7);
            Assert.AreEqual(a.semCola, b.semCola);
            Assert.AreEqual(a.comCola, b.comCola);
        }
    }
}
