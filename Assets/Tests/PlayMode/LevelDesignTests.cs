using System.Collections;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;
using static ProvaImpossivel.Tests.FluxoEProfessorTests;

namespace ProvaImpossivel.Tests
{
    /// <summary>
    /// Level design da sala medido com a visão REAL do jogo (Vision.LineOfSight com colegas bloqueando):
    /// de quantos pontos da ronda do professor (e do Puxa-saco virado) cada trecho de rota é visto.
    /// </summary>
    public class LevelDesignTests
    {
        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
        }

        // carteiras: colunas (x) e fileiras (z) dos assentos, fileira 1 = frente
        static readonly float[] SeatX = { -3.85f, -2.50f, -1.15f, 1.05f, 2.40f, 3.75f };
        static readonly float[] SeatZ = { -3.63f, -1.73f, 0.17f, 2.07f, 3.97f, 5.87f };
        internal static Vector3 Seat(int row, int col) => new(SeatX[col - 1], 0f, SeatZ[row - 1]);

        internal const float StandingHead = 1.7f, StandingChest = 1.25f, SeatedHead = 1.3f, SeatedChest = 0.95f;

        internal class Route
        {
            public string name;
            public Vector3[] points;
            public Route(string n, params Vector2[] xz)
            {
                name = n;
                points = new Vector3[xz.Length];
                for (int i = 0; i < xz.Length; i++) points[i] = new Vector3(xz[i].x, 0f, xz[i].y);
            }
        }

        // ponto onde o jogador levanta (vão entre as colunas 4 e 5, na fileira dele)
        static readonly Vector2 Levanta = new(1.775f, 2.07f);

        internal static readonly Route[] Rotas =
        {
            // vãos livres medidos (RelatorioDoLayout): entre as fileiras 3/4 em z 0,9; 4/5 em z 2,8; 5/6 em z 4,7
            new("Nerd 1 — pela frente (vão apertado)", Levanta, new Vector2(1.775f, 0.9f), new Vector2(3.1f, 0.9f), new Vector2(3.1f, 1.5f)),
            new("Nerd 2 — por trás da carteira", Levanta, new Vector2(1.775f, 2.8f), new Vector2(3.1f, 2.8f), new Vector2(3.1f, 2.4f)),
            new("Nerd 3 — pelo corredor da janela", Levanta, new Vector2(1.775f, 2.8f), new Vector2(4.6f, 2.8f), new Vector2(4.6f, 2.07f)),
            new("Fundão 1 — corredor central", Levanta, new Vector2(1.775f, 2.8f), new Vector2(0f, 2.8f), new Vector2(0f, 4.7f), new Vector2(-3.17f, 4.7f)),
            new("Fundão 2 — pelo fundo da sala", Levanta, new Vector2(1.775f, 6.85f), new Vector2(-3.17f, 6.85f), new Vector2(-3.17f, 6.6f)),
        };

        internal static List<Vector3> Observadores(ProfessorAI prof, bool soDaFrente = false)
        {
            var obs = new List<Vector3> { prof.frontSpot, new(1.4f, 0f, -6.3f) }; // quadro e mesa do professor
            foreach (var t in prof.patrolPoints) if (t != null) obs.Add(t.position);
            if (soDaFrente) obs.RemoveAll(o => o.z > -3f); // só a frente da sala (quadro, mesa, primeira fileira)
            return obs;
        }

        internal static bool Ve(Vector3 eye, Vector3 feet, bool emPe, float maxDist)
        {
            Vector3 head = feet + Vector3.up * (emPe ? StandingHead : SeatedHead);
            Vector3 chest = feet + Vector3.up * (emPe ? StandingChest : SeatedChest);
            if (Vector3.Distance(eye, head) > maxDist) return false;
            return Vision.LineOfSight(eye, head) || Vision.LineOfSight(eye, chest);
        }

        /// <summary>Fração dos pontos da ronda de onde o professor vê o jogador nesse lugar (sem contar pra onde ele olha).</summary>
        internal static float Exposicao(ProfessorAI prof, Vector3 feet, bool emPe, float viewDist, bool soDaFrente = false)
        {
            float eyeH = prof.visual.HeadPosition.y - prof.transform.position.y;
            var obs = Observadores(prof, soDaFrente);
            int vistos = 0;
            foreach (var o in obs) if (Ve(o + Vector3.up * eyeH, feet, emPe, viewDist)) vistos++;
            return vistos / (float)obs.Count;
        }

        internal static bool PuxaSacoVe(SnitchStudent puxa, Vector3 feet)
        {
            Vector3 eye = puxa.visual.HeadPosition;
            Vector3 to = feet - eye; to.y = 0f;
            if (Vector3.Angle(-puxa.transform.forward, to) > puxa.halfFov) return false; // virado pra trás
            return Ve(eye, feet, true, puxa.viewDistance);
        }

        internal static string Medir(ProfessorAI prof, SnitchStudent puxa, float viewDist, out float[] media)
        {
            var sb = new StringBuilder();
            media = new float[Rotas.Length];
            for (int r = 0; r < Rotas.Length; r++)
            {
                var rota = Rotas[r];
                float soma = 0f;
                int puxaVe = 0, passos = 0;
                // amostra cada trecho a cada 0,5 m
                for (int i = 0; i + 1 < rota.points.Length; i++)
                {
                    Vector3 a = rota.points[i], b = rota.points[i + 1];
                    int n = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(a, b) / 0.5f));
                    for (int k = 0; k < n; k++)
                    {
                        Vector3 p = Vector3.Lerp(a, b, k / (float)n);
                        soma += Exposicao(prof, p, true, viewDist);
                        if (PuxaSacoVe(puxa, p)) puxaVe++;
                        passos++;
                    }
                }
                Vector3 fim = rota.points[^1];
                float expFim = Exposicao(prof, fim, true, viewDist);
                float frenteFim = Exposicao(prof, fim, true, viewDist, soDaFrente: true);
                media[r] = soma / passos;
                sb.AppendFormat("{0,-36} exposição média {1,5:P0} | no ponto da cola {2,5:P0} (da frente da sala {6,5:P0}) | Puxa-saco vê {3}/{4} trechos | {5:0.0} m\n",
                    rota.name, media[r], expFim, puxaVe, passos, Comprimento(rota), frenteFim);
            }
            Vector3 assento = PlayerController.I.seatPoint.position;
            sb.AppendFormat("Assento do jogador (sentado): exposição {0:P0}; da frente da sala {1:P0}\n",
                Exposicao(prof, assento, false, viewDist), Exposicao(prof, assento, false, viewDist, soDaFrente: true));
            return sb.ToString();
        }

        static float Comprimento(Route r)
        {
            float d = 0f;
            for (int i = 0; i + 1 < r.points.Length; i++) d += Vector3.Distance(r.points[i], r.points[i + 1]);
            return d;
        }

        /// <summary>O jogador (cápsula de raio 0,2) cabe em pé neste ponto? Personagens não contam (ele passa do lado).</summary>
        internal static bool Cabe(Vector3 p)
        {
            var cols = Physics.OverlapCapsule(p + Vector3.up * 0.3f, p + Vector3.up * 1.5f, 0.2f, ~0, QueryTriggerInteraction.Ignore);
            foreach (var c in cols) if (c.GetComponentInParent<CharacterVisual>() == null) return false;
            return true;
        }

        // ------------------------------------------------------------------ medidas da sala (layout gravado na cena)

        struct Medida { public float exposicao, frenteNoFim, puxaSaco, comprimento; }

        static Medida Medir(ProfessorAI prof, SnitchStudent puxa, Route rota, float viewDist)
        {
            float soma = 0f;
            int puxaVe = 0, passos = 0;
            for (int i = 0; i + 1 < rota.points.Length; i++)
            {
                Vector3 a = rota.points[i], b = rota.points[i + 1];
                int n = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(a, b) / 0.5f));
                for (int k = 0; k < n; k++)
                {
                    Vector3 p = Vector3.Lerp(a, b, k / (float)n);
                    soma += Exposicao(prof, p, true, viewDist);
                    if (PuxaSacoVe(puxa, p)) puxaVe++;
                    passos++;
                }
            }
            return new Medida
            {
                exposicao = soma / passos,
                frenteNoFim = Exposicao(prof, rota.points[^1], true, viewDist, soDaFrente: true),
                puxaSaco = puxaVe / (float)passos,
                comprimento = Comprimento(rota),
            };
        }

        static float DistanciaDaRonda(ProfessorAI prof, Vector3 p)
        {
            float best = float.MaxValue;
            foreach (var t in prof.patrolPoints)
            {
                if (t == null) continue;
                Vector3 d = t.position - p; d.y = 0f;
                best = Mathf.Min(best, d.magnitude);
            }
            return best;
        }

        static IEnumerator Sala()
        {
            yield return EnterGame();
            GameManager.I.Begin(Difficulty.Normal);
            yield return null;
        }

        /// <summary>Relatório do layout (não falha): a tabela que orienta os ajustes de posição.</summary>
        [UnityTest]
        public IEnumerator RelatorioDoLayout()
        {
            yield return Sala();
            Debug.Log("[LevelDesign]\n" + Medir(ProfessorAI.I, Object.FindAnyObjectByType<SnitchStudent>(), GameManager.I.normal.viewDistance, out _));
            var sb = new StringBuilder("[Vaos]\n");
            void Horizontal(string nome, float x0, float x1, float z0, float z1)
            {
                sb.Append(nome).Append(": ");
                for (float z = z0; z <= z1 + 0.001f; z += 0.1f)
                {
                    bool livre = true;
                    for (float x = Mathf.Min(x0, x1); x <= Mathf.Max(x0, x1); x += 0.05f) if (!Cabe(new Vector3(x, 0f, z))) { livre = false; break; }
                    if (livre) sb.Append(z.ToString("0.0")).Append(' ');
                }
                sb.Append('\n');
            }
            void Vertical(string nome, float z0, float z1, float xa, float xb)
            {
                sb.Append(nome).Append(": ");
                for (float x = xa; x <= xb + 0.001f; x += 0.05f)
                {
                    bool livre = true;
                    for (float z = Mathf.Min(z0, z1); z <= Mathf.Max(z0, z1); z += 0.05f) if (!Cabe(new Vector3(x, 0f, z))) { livre = false; break; }
                    if (livre) sb.Append(x.ToString("0.00")).Append(' ');
                }
                sb.Append('\n');
            }
            Horizontal("fileiras 3/4, x 1,8..3,1", 1.775f, 3.07f, 0.3f, 1.9f);
            Horizontal("fileiras 4/5, x 1,8..3,1", 1.775f, 3.07f, 2.2f, 3.9f);
            Horizontal("fileiras 4/5, x 0..1,8", 0f, 1.775f, 2.2f, 3.9f);
            Horizontal("fileiras 4/5, x 1,8..4,6", 1.775f, 4.6f, 2.2f, 3.9f);
            Horizontal("fileiras 5/6, x -3,2..0", -3.17f, 0f, 4.1f, 5.8f);
            Horizontal("fundo, x -3,2..1,8", -3.17f, 1.775f, 6.0f, 7.5f);
            Vertical("vão col 4/5, z 2..6,9", 2.07f, 6.85f, 1.5f, 2.1f);
            Vertical("vão col 5/6, z 0,9..3", 0.9f, 3.0f, 2.8f, 3.4f);
            Vertical("corredor central, z 3..4,9", 3.0f, 4.92f, -0.4f, 0.4f);
            Vertical("janela, z 2..3", 2.07f, 3.0f, 4.2f, 5.0f);
            Debug.Log(sb.ToString());
        }

        [UnityTest]
        public IEnumerator ColegaNaFrenteEscondeSoOPeitoDoJogadorSentado()
        {
            // Bloco 12: os colegas voltaram ao tamanho normal (decisão B). Sentado, quem está na frente esconde o PEITO
            // do jogador, mas a cabeça continua visível de pé: ponto cego parcial, não total.
            yield return Sala();
            var prof = ProfessorAI.I;
            float eyeH = prof.visual.HeadPosition.y - prof.transform.position.y;
            Vector3 assento = PlayerController.I.seatPoint.position;
            Vector3 olho = new(3f, eyeH, -5.4f); // parada da ronda na frente, alinhada com a coluna do jogador
            Assert.IsFalse(Vision.LineOfSight(olho, assento + Vector3.up * SeatedChest), "o colega da frente (PEDRÃO) deveria esconder o peito do jogador");
            Assert.IsTrue(Vision.LineOfSight(olho, assento + Vector3.up * SeatedHead), "a cabeça do jogador sentado continua visível por cima do colega");
            Assert.IsTrue(Ve(olho, assento, false, 30f), "visto (a cabeça basta)");
        }

        [UnityTest]
        public IEnumerator RotasCabemOJogadorEmPe()
        {
            yield return Sala();
            foreach (var rota in Rotas)
                for (int i = 0; i + 1 < rota.points.Length; i++)
                    for (float t = 0f; t <= 1f; t += 0.1f)
                    {
                        Vector3 p = Vector3.Lerp(rota.points[i], rota.points[i + 1], t);
                        Assert.IsTrue(Cabe(p), rota.name + ": o jogador não passa em " + p);
                    }
        }

        [UnityTest]
        public IEnumerator DuasRotasDeRiscoDiferenteAteONerd()
        {
            yield return Sala();
            var prof = ProfessorAI.I;
            var puxa = Object.FindAnyObjectByType<SnitchStudent>();
            float vd = GameManager.I.normal.viewDistance;
            // com os colegas em tamanho normal (Bloco 12), as duas rotas são igualmente vistas do professor e do Puxa-saco;
            // o que muda é o tempo exposto (comprimento) e onde a cola acontece (longe x colada na ronda)
            var frente = Medir(prof, puxa, Rotas[0], vd);  // curta, vão apertado na frente da carteira
            var janela = Medir(prof, puxa, Rotas[2], vd);  // longa, pelo corredor da janela (colado na ronda)
            Assert.Greater(janela.comprimento - frente.comprimento, 1f, "a rota da janela é a mais longa");
            // o ponto da cola pela janela fica colado numa parada da ronda (o professor passa do lado e pode parar pra encarar)
            Assert.Less(DistanciaDaRonda(prof, Rotas[2].points[^1]), 1f, "a cola pela janela deveria ficar colada na ronda");
            Assert.Greater(DistanciaDaRonda(prof, Rotas[0].points[^1]), 2f, "a cola pela frente deveria ficar longe das paradas da ronda");
        }

        [UnityTest]
        public IEnumerator DuasRotasDeRiscoDiferenteAteOFundao()
        {
            yield return Sala();
            var prof = ProfessorAI.I;
            var puxa = Object.FindAnyObjectByType<SnitchStudent>();
            float vd = GameManager.I.normal.viewDistance;
            var central = Medir(prof, puxa, Rotas[3], vd); // curta, no corredor da ronda
            var fundo = Medir(prof, puxa, Rotas[4], vd);   // longa, atrás da última fileira (CAIO grandão)
            Assert.Greater(fundo.comprimento, central.comprimento, "a rota do fundo é a mais longa");
            Assert.Greater(central.exposicao - fundo.exposicao, 0.1f, "a rota do fundo tem que ser mais escondida do professor");
            Assert.Greater(central.puxaSaco - fundo.puxaSaco, 0.3f, "o Puxa-saco vê a rota central e quase não vê a do fundo");
        }
    }
}