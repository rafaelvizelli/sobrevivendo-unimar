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

        // --- suposições das colas novas (Bloco 15)
        /// <summary>Distância da carteira do jogador até a do Nerd (alvo do bilhete do jogador cuidadoso), medida na cena: 1,35 m.</summary>
        const float DistanciaNerd = 1.35f;
        /// <summary>O cuidadoso só usa a colinha com o professor longe; sobra este risco de ele chegar perto no meio.</summary>
        const double RiscoProfessorChegaPerto = 0.06;
        /// <summary>Bilhete: chance residual de o Nerd parar de escrever / o professor estar perto (o cuidadoso olha antes).</summary>
        const double BilhetePerdidoMesmoComCuidado = 0.05;
        /// <summary>O cuidadoso silencia o celular assim que vibra (F): o professor ouve, mas quase nunca vê tocando.</summary>
        const double VeOCelularTocando = 0.15;
        /// <summary>Segundos extras de cada cola nova (olhar em volta, se ajeitar).</summary>
        const float FolgaPorCola = 2f;

        /// <summary>
        /// Chance de o professor ouvir um ruído de raio <paramref name="raio"/>: ele costuma estar a 2–10 m do jogador.
        /// </summary>
        static double ChanceDeOuvir(float raio) => Math.Clamp((raio - 2.0) / 8.0, 0.0, 0.95);

        /// <summary>O que a simulação precisa saber de uma dificuldade (vem do jogo).</summary>
        public struct Modelo
        {
            public int nerdCopias;
            public bool nerdDeSentado;
            public float pesoOportunidade, pesoAmeaca;
            public float chanceFlagraFundao;
            public float alertaPorAmeaca;
            /// <summary>Usa colinha, bilhete, fone e garrafinha (Bloco 15)? Falso = o modelo de antes, idêntico.</summary>
            public bool colasNovas;
            /// <summary>Respostas que todas as colas somadas podem dar (DifficultySettings.maxCheatAnswers).</summary>
            public int orcamento;
        }

        /// <summary>Monta o modelo a partir dos números do jogo (com as colas novas e o orçamento).</summary>
        public static Modelo ModeloDe(DifficultySettings s) => new()
        {
            nerdCopias = s.nerdCopies,
            nerdDeSentado = false, // NerdStudent.CanInteract exige estar em pé
            pesoOportunidade = s.opportunityEventWeight,
            pesoAmeaca = s.threatEventWeight,
            chanceFlagraFundao = s.backRowCaughtChance,
            alertaPorAmeaca = s.threatAlertSeconds,
            colasNovas = true,
            orcamento = s.maxCheatAnswers,
        };

        /// <summary>O modelo como era antes do Bloco 15: só celular, Nerd e Fundão, sem orçamento.</summary>
        public static Modelo ModeloAntigo(DifficultySettings s)
        {
            var m = ModeloDe(s);
            m.colasNovas = false;
            m.orcamento = int.MaxValue;
            return m;
        }

        /// <summary>Uma cola nova do ponto de vista do modelo: quanto o professor acumula se vê, se fica perto e se ouve.</summary>
        struct ColaNova
        {
            public float tempo, suspSeVe, ganhoPerto, ganhoRuido;
            public double chancePerto, chanceOuvir, chancePerder;
        }

        /// <summary>
        /// Resolve uma cola nova: 0 = deu resposta, 1 = perdida (sem resposta), 2 = flagrante.
        /// Visto agindo: flagrante se passar de 100, ou se ele vier investigar e a Cara de Paisagem falhar.
        /// Sentido de perto (colinha) e ruído: sem ver, só a investigação + Cara de Paisagem pode virar flagrante.
        /// </summary>
        static int ResolverColaNova(System.Random rng, ColaNova c, double risco, double caraDePaisagem, ref int atencao)
        {
            if (atencao > 0) { risco = Math.Min(0.95, risco * 2.0); atencao--; } // ele ouviu algo antes e está olhando pra cá
            bool visto = rng.NextDouble() < risco;
            float susp = visto ? c.suspSeVe : 0f;
            if (rng.NextDouble() < c.chancePerto) susp += c.ganhoPerto;
            if (rng.NextDouble() < c.chanceOuvir)
            {
                atencao++;
                susp += c.ganhoRuido;
            }
            if (visto && susp >= 100f) return 2;
            if (susp >= 65f && rng.NextDouble() > caraDePaisagem && susp + 30f >= 100f) return 2; // investigou e você tremeu
            if (rng.NextDouble() < c.chancePerder) return 1;
            return 0;
        }

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

            // colas novas (Bloco 15): a mesma conta de suspeita do jogo, com as suposições do jogador cuidadoso acima
            float mult = s.suspicionMultiplier;
            var colinha = new ColaNova
            {
                tempo = s.colinhaTime + FolgaPorCola,
                suspSeVe = ProfessorAI.SuspicionIfSeen(s, s.colinhaSuspicion, visRef, s.colinhaTime),
                chancePerto = RiscoProfessorChegaPerto,
                ganhoPerto = s.colinhaNearSuspicion * mult * s.colinhaTime,
            };
            var garrafa = new ColaNova
            {
                tempo = s.garrafaTime + FolgaPorCola,
                suspSeVe = ProfessorAI.SuspicionIfSeen(s, s.garrafaSuspicion, visRef, s.garrafaTime),
                chanceOuvir = ChanceDeOuvir(s.garrafaNoiseRadius),
                ganhoRuido = s.garrafaNoise * mult,
            };
            var bilhete = new ColaNova
            {
                tempo = s.bilheteThrowTime + FolgaPorCola,
                suspSeVe = ProfessorAI.SuspicionIfSeen(s, s.bilheteThrowSuspicion, visRef, s.bilheteThrowTime),
                chanceOuvir = ChanceDeOuvir(s.bilheteNoiseRadius),
                ganhoRuido = s.bilheteNoise * mult,
                chancePerder = Math.Min(1.0, CheatRules.BilheteMissChance(DistanciaNerd, s.bilheteMissChancePerMeter) + BilhetePerdidoMesmoComCuidado),
            };
            // fone: mandar + ler = duas vezes o celular por foneUseTime; o toque é ruído (raramente visto: ele silencia)
            var foneUso = new ColaNova
            {
                tempo = 2f * s.foneUseTime + FolgaPorCola,
                suspSeVe = ProfessorAI.SuspicionIfSeen(s, s.phoneSuspicion, visRef, s.foneUseTime),
            };

            int passouSemCola = 0, passouComCola = 0, flagrado = 0;
            long colas = 0;
            var acertou = new bool[10];
            for (int n = 0; n < partidas; n++)
            {
                // -------- respostas próprias: conhecidas e chutes (é o mesmo começo nas duas estratégias)
                int certas = 0, respondidas = 0;
                float tempo = 0f;
                for (int q = 0; q < 10; q++)
                {
                    acertou[q] = false;
                    if (tempo + cicloPergunta > s.duration - TempoEntrega) continue;
                    tempo += cicloPergunta;
                    respondidas++;
                    bool conhecida = q < s.knownQuestions;
                    if (rng.NextDouble() < (conhecida ? acertoConhecida : 0.25)) { certas++; acertou[q] = true; }
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
                bool celularConfiscado = false, foneChega = false;
                int orcamento = m.orcamento;

                if (m.colasNovas)
                {
                    // refaz a prova na ordem do cuidadoso (Bloco 15): conhecidas, depois as colas que só servem para
                    // questão EM BRANCO (colinha, bilhete, garrafinha), depois chuta o resto e só então corrige as erradas
                    int conhecidas = Math.Min(s.knownQuestions, respondidas);
                    certas = 0;
                    for (int q = 0; q < conhecidas; q++) if (acertou[q]) certas++;
                    tempo = conhecidas * cicloPergunta;
                    int atencao = 0;

                    // fone: pede logo no começo, a resposta chega enquanto ele faz o resto (e o celular pode vibrar)
                    if (s.foneCharges > 0 && orcamento > 0)
                    {
                        tempo += s.foneUseTime + FolgaPorCola;
                        colas++;
                        var envio = foneUso;
                        envio.tempo = s.foneUseTime;
                        int r = ResolverColaNova(rng, envio, riscoForaDaJanela, caraDePaisagem, ref atencao);
                        if (r == 2)
                        {
                            bool adv = s.firstFlagranteIsWarning && flagrantes == 0;
                            Flagrante(ref flagrantes, ref certas, s);
                            if (s.confiscatePhone && !adv) celularConfiscado = true;
                        }
                        else
                        {
                            foneChega = true;
                            if (rng.NextDouble() < s.foneRingChance && rng.NextDouble() < ChanceDeOuvir(s.foneRingRadius))
                            {
                                atencao++;
                                float ganho = s.foneRingNoise * mult;
                                bool visto = rng.NextDouble() < VeOCelularTocando;
                                bool flagra = (visto && ganho + ProfessorAI.SuspicionIfSeen(s, s.foneRingSuspicion, visRef, 1f) >= 100f)
                                              || (ganho >= 65f && rng.NextDouble() > caraDePaisagem && ganho + 30f >= 100f);
                                if (flagra)
                                {
                                    bool adv = s.firstFlagranteIsWarning && flagrantes == 0;
                                    Flagrante(ref flagrantes, ref certas, s);
                                    if (s.confiscatePhone && !adv) { celularConfiscado = true; foneChega = false; }
                                }
                            }
                        }
                    }

                    int proxima = conhecidas, nColinha = s.colinhaCharges, nBilhete = s.bilheteCharges, nGarrafa = s.garrafaCharges;
                    while (certas < s.minGrade - 0.001f && orcamento > (foneChega ? 1 : 0) && proxima < 10 && flagrantes < s.maxFlagrantes)
                    {
                        int qual = nColinha > 0 ? 0 : nBilhete > 0 ? 1 : nGarrafa > 0 ? 2 : -1;
                        if (qual < 0) break;
                        var c = qual == 0 ? colinha : qual == 1 ? bilhete : garrafa;
                        if (tempo + c.tempo > s.duration - TempoEntrega) break;
                        tempo += c.tempo;
                        colas++;
                        if (qual == 0) nColinha--; else if (qual == 1) nBilhete--; else nGarrafa--;
                        int r = ResolverColaNova(rng, c, riscoForaDaJanela, caraDePaisagem, ref atencao);
                        if (r == 2)
                        {
                            bool adv = s.firstFlagranteIsWarning && flagrantes == 0;
                            Flagrante(ref flagrantes, ref certas, s);
                            if (!adv && qual == 0) nColinha = 0; // colinha confiscada
                            if (!adv && qual == 2) nGarrafa = 0; // garrafinha confiscada
                            continue;
                        }
                        if (r == 1) continue; // bilhete perdido: a vaga do orçamento volta
                        certas++;
                        orcamento--;
                        proxima++;
                    }
                    // chuta as que sobraram em branco (mesmo sorteio de antes, se deu tempo)
                    for (int q = proxima; q < 10; q++)
                    {
                        if (tempo + cicloPergunta > s.duration - TempoEntrega) break;
                        tempo += cicloPergunta;
                        if (q < respondidas ? acertou[q] : rng.NextDouble() < 0.25) certas++;
                    }
                }

                while (certas < s.minGrade - 0.001f && flagrantes < s.maxFlagrantes)
                {
                    if (orcamento <= 0) break; // orçamento de colas esgotado: nenhuma cola dá mais resposta
                    if (foneChega)
                    {
                        // a resposta do fone chegou: ler é mexer no celular de novo
                        foneChega = false;
                        if (celularConfiscado) continue;
                        tempo += s.foneUseTime;
                        colas++;
                        int atencaoLeitura = 0;
                        var leitura = foneUso;
                        int r = ResolverColaNova(rng, leitura, riscoForaDaJanela, caraDePaisagem, ref atencaoLeitura);
                        if (r == 2)
                        {
                            bool adv = s.firstFlagranteIsWarning && flagrantes == 0;
                            Flagrante(ref flagrantes, ref certas, s);
                            if (s.confiscatePhone && !adv) celularConfiscado = true;
                            continue;
                        }
                        certas++;
                        orcamento--;
                        continue;
                    }
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
                        if (rng.NextDouble() < 0.6) { certas++; orcamento--; }
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
                    orcamento--;
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
            var sb = new StringBuilder();
            void Tabela(string titulo, Func<DifficultySettings, Modelo> modelo)
            {
                sb.Append("\n" + titulo + "\nDificuldade | Sem cola | Com cola (cuidado) | Expulso colando | Colas/partida | Orçamento | Celular visto 3 s (susp.)\n");
                foreach (var s in new[] { gm.facil, gm.normal, gm.dificil })
                {
                    var m = modelo(s);
                    var r = Simular(s, m, Partidas, 12345);
                    float visRef = Vision.Weight(DistanciaReferencia, s.viewDistance, s.halfFov * 0.5f, s.halfFov);
                    sb.AppendFormat("{0,-11} | {1,7:P1} | {2,17:P1} | {3,14:P1} | {4,13:0.0} | {5,9} | {6,6:0}\n",
                        s.name, r.semCola, r.comCola, r.flagradoComCola, r.colasUsadas,
                        m.orcamento == int.MaxValue ? "-" : m.orcamento.ToString(), ProfessorAI.SuspicionIfSeen(s, s.phoneSuspicion, visRef, 3f));
                }
            }
            Tabela("ANTES (Bloco 14: celular, Nerd, Fundão; sem orçamento)", ModeloAntigo);
            Tabela("DEPOIS (Bloco 15: + colinha, bilhete, fone, garrafinha; com orçamento)", ModeloDe);
            Tabela("SÓ PARA COMPARAR: colas novas SEM orçamento (o que o orçamento segura)", s => { var m = ModeloDe(s); m.orcamento = int.MaxValue; return m; });
            sb.Append("\nNormal por orçamento (com as colas novas):");
            for (int o = 1; o <= 5; o++)
            {
                var m = ModeloDe(gm.normal);
                m.orcamento = o;
                sb.AppendFormat("  {0} → {1:P1}", o, Simular(gm.normal, m, Partidas, 12345).comCola);
            }
            sb.Append('\n');
            Debug.Log("[Equilibrio]" + sb);
            TestContext.WriteLine(sb.ToString());
        }

        /// <summary>
        /// Meta do equilíbrio: no Normal, ~5% passa sem colar e ~50–60% passa colando com cuidado (com as colas novas).
        /// A faixa aceita (45–70%) é a mesma de antes do Bloco 15: com o orçamento inteiro, 3 respostas dão ~64% e 2 dão ~38%
        /// (não existe valor inteiro dentro de 50–60%; ver NOTAS, Bloco 15).
        /// </summary>
        [Test]
        public void MetaDoNormal_ColaPrecisaSerNecessariaENaoGarantida()
        {
            var r = Simular(gm.normal, ModeloDe(gm.normal), Partidas, 12345);
            Assert.LessOrEqual(r.semCola, 0.08, "Normal: passar sem colar está fácil demais");
            Assert.That(r.comCola, Is.InRange(0.45, 0.70), "Normal: colando com cuidado deveria passar uns 50–60%");
        }

        /// <summary>Bloco 15: as colas novas dão variedade, não pontos. Quem segura é o orçamento.</summary>
        [Test]
        public void OrcamentoSeguraAsColasNovas()
        {
            foreach (var s in new[] { gm.facil, gm.normal, gm.dificil })
            {
                var com = Simular(s, ModeloDe(s), Partidas, 3);
                var m = ModeloDe(s);
                m.orcamento = int.MaxValue;
                var sem = Simular(s, m, Partidas, 3);
                Assert.LessOrEqual(com.comCola, sem.comCola + 1e-9, s.name + ": o orçamento não pode AUMENTAR a aprovação");
            }
            // no Normal, sem orçamento as colas novas estourariam a meta (é por isso que ele existe)
            var mn = ModeloDe(gm.normal);
            mn.orcamento = int.MaxValue;
            Assert.Greater(Simular(gm.normal, mn, Partidas, 3).comCola, 0.60, "sem orçamento as colas novas deveriam passar da meta");
        }

        /// <summary>O modelo antigo continua dando exatamente o número de antes (a tabela "antes" é comparável).</summary>
        [Test]
        public void ModeloAntigoNaoMudou()
        {
            var r = Simular(gm.normal, ModeloAntigo(gm.normal), Partidas, 12345);
            Assert.AreEqual(0.633, r.comCola, 0.0005, "o caminho antigo da simulação mudou sem querer");
            Assert.AreEqual(0.056, r.semCola, 0.0005);
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
