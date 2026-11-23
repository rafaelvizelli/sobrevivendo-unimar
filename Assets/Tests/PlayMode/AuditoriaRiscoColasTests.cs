using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using static ProvaImpossivel.Tests.FluxoEProfessorTests;

namespace ProvaImpossivel.Tests
{
    /// <summary>
    /// Bloco 15c, parte 2 — AUDITORIA do risco real das colas (só mede; não muda nada do jogo).
    ///
    /// Cada caso carrega a cena real, põe o professor num lugar de onde ele VÊ o jogador (linha de visão de verdade),
    /// executa a cola pelo teclado virtual (ou pela mesma chamada que o jogo faz, no bilhete) e depois deixa o
    /// professor agir sozinho por até 10 s, anotando o que acontece. A tabela vai para o log ("[Auditoria]") e para
    /// <c>Captures/AuditoriaRisco/auditoria.md</c>.
    ///
    /// Situações: (a) vendo a cola inteira de perto (~2 m); (b) vendo de longe, dentro do cone (~70% do alcance);
    /// (c) de costas, de perto, e se vira no meio da cola; (d) já desconfiado (suspeita 40), vendo de longe, e a cola
    /// feita duas vezes seguidas.
    ///
    /// Ajustes SÓ do teste (na cópia da cena carregada, para isolar a cola): sem acontecimentos nem Puxa-saco, o Nerd não
    /// olha de lado, o bilhete não erra a mesa, a resposta do fone chega em 1,5 s e (fora dos casos de toque) o celular
    /// não vibra. Nenhum número de suspeita, visão ou flagrante é mexido.
    ///
    /// O Puxa-saco continua OUVINDO (barulho seu perto dele faz ele virar e, se te vir colando, dedurar): isso é uma
    /// cadeia real do jogo e aparece na tabela como Infracao "Dedurado". Só a virada sorteada dele fica desligada.
    /// Investigando, o professor anda livremente até o lugar (o teste só o segura parado enquanto ele está calmo ou
    /// desconfiado).
    ///
    /// Os testes não falham por divergência com o pedido do Bloco 15 (isso é o relatório); eles só garantem que a
    /// medição valeu: o professor estava mesmo vendo o jogador e a cola foi feita.
    /// </summary>
    public class AuditoriaRiscoColasTests
    {
        public enum Cola { Colinha, Garrafinha, Bilhete, Fone, Celular, Nerd }
        public enum Situacao { A_PertoVendo, B_LongeNoCone, C_ViraNoMeio, D_DesconfiadoDuasVezes }

        struct Linha
        {
            public string cola, dif, situ, desfecho, infracao, obs;
            public float dist, suspFim, suspMax;
            public bool flagrante;
            public int flagrantes;
        }

        static readonly List<Linha> linhas = new();
        static readonly FieldInfo LastInfracao = typeof(ProfessorAI).GetField("lastInfracao", BindingFlags.NonPublic | BindingFlags.Instance);
        static readonly PropertyInfo SuspicionProp = typeof(ProfessorAI).GetProperty("Suspicion");
        static readonly FieldInfo PhoneCooldown = typeof(PlayerController).GetField("phoneCooldown", BindingFlags.NonPublic | BindingFlags.Instance);
        static readonly FieldInfo CamYaw = typeof(ThirdPersonCamera).GetField("yaw", BindingFlags.NonPublic | BindingFlags.Instance);

        Keyboard kb;
        InputSettings.BackgroundBehavior bgAntes;
#if UNITY_EDITOR
        InputSettings.EditorInputBehaviorInPlayMode editorAntes;
#endif

        [SetUp]
        public void SetUp()
        {
            var settings = InputSystem.settings;
            bgAntes = settings.backgroundBehavior;
#if UNITY_EDITOR
            editorAntes = settings.editorInputBehaviorInPlayMode;
            settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            kb = InputSystem.AddDevice<Keyboard>("TecladoAuditoria");
            kb.MakeCurrent();
        }

        [TearDown]
        public void TearDown()
        {
            if (kb != null) InputSystem.RemoveDevice(kb);
            InputSystem.settings.backgroundBehavior = bgAntes;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode = editorAntes;
#endif
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
        }

        [OneTimeTearDown]
        public void Relatorio()
        {
            if (linhas.Count == 0) return;
            var sb = new StringBuilder();
            sb.Append("| Cola | Dificuldade | Situação | Dist. (m) | Suspeita no fim | Suspeita máx. | Infracao | Houve flagrante? | Flagrantes acumulados | Desfecho | Obs. |\n");
            sb.Append("|---|---|---|---|---|---|---|---|---|---|---|\n");
            foreach (var l in linhas)
                sb.AppendFormat("| {0} | {1} | {2} | {3:0.0} | {4:0} | {5:0} | {6} | {7} | {8} | {9} | {10} |\n",
                    l.cola, l.dif, l.situ, l.dist, l.suspFim, l.suspMax, l.infracao, l.flagrante ? "SIM" : "não", l.flagrantes, l.desfecho, l.obs);
            Debug.Log("[Auditoria]\n" + sb);
            try
            {
                string dir = Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? ".", "Captures", "AuditoriaRisco");
                Directory.CreateDirectory(dir);
                File.WriteAllText(Path.Combine(dir, "auditoria.md"), sb.ToString(), Encoding.UTF8);
            }
            catch (Exception e) { Debug.LogWarning("[Auditoria] não gravei o arquivo: " + e.Message); }
        }

        // ------------------------------------------------------------------ testes (um por cola e dificuldade)

        [UnityTest, Timeout(400000)] public IEnumerator Colinha_Normal() => Auditar(Cola.Colinha, Difficulty.Normal);
        [UnityTest, Timeout(400000)] public IEnumerator Colinha_Dificil() => Auditar(Cola.Colinha, Difficulty.Dificil);
        [UnityTest, Timeout(400000)] public IEnumerator Garrafinha_Normal() => Auditar(Cola.Garrafinha, Difficulty.Normal);
        [UnityTest, Timeout(400000)] public IEnumerator Garrafinha_Dificil() => Auditar(Cola.Garrafinha, Difficulty.Dificil);
        [UnityTest, Timeout(400000)] public IEnumerator Bilhete_Normal() => Auditar(Cola.Bilhete, Difficulty.Normal);
        [UnityTest, Timeout(400000)] public IEnumerator Bilhete_Dificil() => Auditar(Cola.Bilhete, Difficulty.Dificil);
        [UnityTest, Timeout(400000)] public IEnumerator Fone_Normal() => Auditar(Cola.Fone, Difficulty.Normal);
        [UnityTest, Timeout(400000)] public IEnumerator Fone_Dificil() => Auditar(Cola.Fone, Difficulty.Dificil);
        [UnityTest, Timeout(400000)] public IEnumerator Celular_Normal() => Auditar(Cola.Celular, Difficulty.Normal);
        [UnityTest, Timeout(400000)] public IEnumerator Celular_Dificil() => Auditar(Cola.Celular, Difficulty.Dificil);
        [UnityTest, Timeout(400000)] public IEnumerator Nerd_Normal() => Auditar(Cola.Nerd, Difficulty.Normal);
        [UnityTest, Timeout(400000)] public IEnumerator Nerd_Dificil() => Auditar(Cola.Nerd, Difficulty.Dificil);

        /// <summary>O pedido do Bloco 15 para o fone: o celular TOCANDO perto do professor (de costas e olhando).</summary>
        [UnityTest, Timeout(400000)] public IEnumerator FoneTocando_Normal() => AuditarToque(Difficulty.Normal);
        [UnityTest, Timeout(400000)] public IEnumerator FoneTocando_Dificil() => AuditarToque(Difficulty.Dificil);

        /// <summary>
        /// S2 (Operação Steam), este sim com asserção: colinha lida pelo teclado com o professor a ~2 m olhando para você
        /// é flagrante na hora, do tipo ColinhaNaManga (antes a suspeita parava em ~39 e nunca virava flagrante).
        /// </summary>
        [UnityTest, Timeout(60000)]
        public IEnumerator S2_ColinhaVistaDePertoEFlagranteNaHora()
        {
            yield return Comecar(Difficulty.Normal, ringChance: 0f);
            var prof = ProfessorAI.I;
            var p = PlayerController.I;
            Vector3? lugar = AcharLugar(prof, p, 1.6f, 2.4f);
            Assert.IsTrue(lugar.HasValue, "sem lugar com visão do jogador");
            Posar(prof, p, lugar.Value, true);
            yield return null;

            InputSystem.QueueStateEvent(kb, new KeyboardState(Key.Q));
            for (float fim = Time.time + 4f; Time.time < fim && prof.State != ProfState.Flagrante;)
            {
                Posar(prof, p, lugar.Value, true);
                yield return null;
            }
            InputSystem.QueueStateEvent(kb, new KeyboardState());
            yield return null;

            Assert.AreEqual(ProfState.Flagrante, prof.State, "colinha vista de perto tinha que ser flagrante na hora");
            Assert.AreEqual(Infracao.ColinhaNaManga, LastInfracao.GetValue(prof));
        }

        IEnumerator Auditar(Cola cola, Difficulty d)
        {
            foreach (Situacao s in Enum.GetValues(typeof(Situacao)))
                yield return Caso(cola, d, s);
        }

        IEnumerator AuditarToque(Difficulty d)
        {
            foreach (bool olhando in new[] { false, true })
            {
                yield return Comecar(d, ringChance: 0f);
                var gm = GameManager.I;
                var prof = ProfessorAI.I;
                var p = PlayerController.I;
                Vector3? lugar = AcharLugar(prof, p, 1.6f, 2.4f);
                Assert.IsTrue(lugar.HasValue, "sem lugar com visão perto do jogador");
                float dist = Flat.Distance(lugar.Value, p.transform.position);
                Posar(prof, p, lugar.Value, olhando);
                yield return null;

                var l = new Linha { cola = "Fone (celular TOCANDO)", dif = gm.Settings.name, dist = dist,
                                    situ = olhando ? "perto, olhando" : "perto, de costas" };
                p.Cheats.Ring();
                bool viu = false;
                float max = 0f;
                for (float t = 0f; t < gm.Settings.foneRingSeconds + 0.2f && prof.State != ProfState.Flagrante; t += Time.deltaTime)
                {
                    Posar(prof, p, lugar.Value, olhando);
                    yield return null;
                    viu |= prof.Visibility > 0f;
                    max = Mathf.Max(max, prof.Suspicion);
                }
                if (olhando) Assert.IsTrue(viu, "o professor não estava vendo o jogador (caso inválido)");
                l.obs = viu ? "viu o jogador" : "não via o jogador";
                yield return Observar(l, max);
            }
        }

        // ------------------------------------------------------------------ um caso

        IEnumerator Caso(Cola cola, Difficulty d, Situacao s)
        {
            yield return Comecar(d, ringChance: 0f);
            var gm = GameManager.I;
            var prof = ProfessorAI.I;
            var p = PlayerController.I;
            var nerd = UnityEngine.Object.FindAnyObjectByType<NerdStudent>();

            if (cola == Cola.Nerd)
            {
                // espiar o Nerd é em pé, ao lado dele: põe o jogador no corredor colado na carteira do Nerd
                p.StandUp();
                yield return null;
                Vector3? ao = PertoDoNerd(nerd);
                Assert.IsTrue(ao.HasValue, "sem NavMesh ao lado do Nerd");
                var cc = p.GetComponent<CharacterController>();
                cc.enabled = false;
                p.transform.position = ao.Value;
                cc.enabled = true;
                Vector3 para = nerd.transform.position - p.transform.position;
                CamYaw.SetValue(UnityEngine.Object.FindAnyObjectByType<ThirdPersonCamera>(), Quaternion.LookRotation(new Vector3(para.x, 0f, para.z)).eulerAngles.y);
                yield return null;
                yield return null;
            }

            bool longe = s == Situacao.B_LongeNoCone || s == Situacao.D_DesconfiadoDuasVezes;
            float alcance = gm.Settings.viewDistance;
            Vector3? lugar = longe ? AcharLugar(prof, p, alcance * 0.55f, alcance * 0.8f) : AcharLugar(prof, p, 1.6f, 2.4f);
            Assert.IsTrue(lugar.HasValue, "sem lugar com visão do jogador para " + s);
            float dist = Flat.Distance(lugar.Value, p.transform.position);

            bool deCostas = s == Situacao.C_ViraNoMeio;
            Posar(prof, p, lugar.Value, !deCostas);
            yield return null;

            if (s == Situacao.D_DesconfiadoDuasVezes)
            {
                SuspicionProp.SetValue(prof, 40f);
                prof.Alert(p.transform.position, 0f, "auditoria: já desconfiado"); // entra em Desconfiado sem somar nada
                Posar(prof, p, lugar.Value, true);
                yield return null;
            }

            var l = new Linha { cola = cola.ToString(), dif = gm.Settings.name, situ = Nome(s), dist = dist };
            float max = prof.Suspicion;
            bool viu = false;
            float meio = MeioDaCola(cola, gm.Settings, p, nerd);
            int vezes = s == Situacao.D_DesconfiadoDuasVezes ? 2 : 1;
            int feitas = 0;
            string obs = "";

            for (int v = 0; v < vezes && prof.State != ProfState.Flagrante; v++)
            {
                if (v > 0)
                {
                    // a 2ª logo que o jogo deixar: só o celular e o Nerd têm recarga curta (espera, com ele ainda olhando);
                    // nas outras, ou dá na hora ou não dá (sem carga / bilhete ainda voltando)
                    float ate = Time.time + (cola == Cola.Celular || cola == Cola.Nerd ? 8f : 0f);
                    while (Time.time < ate && !PodeFazer(cola, p, nerd) && prof.State != ProfState.Flagrante)
                    {
                        Posar(prof, p, lugar.Value, true);
                        yield return null;
                        max = Mathf.Max(max, prof.Suspicion);
                    }
                    if (!PodeFazer(cola, p, nerd)) { obs += "sem 2ª vez (" + PorQueNao(cola, p, nerd) + "); "; break; }
                }
                float inicio = Time.time;
                bool virou = !deCostas;
                var exec = Plano(Executar(cola, p, nerd, () =>
                {
                    if (!virou && Time.time - inicio >= meio) virou = true;
                    Posar(prof, p, lugar.Value, virou);
                }));
                while (exec.MoveNext())
                {
                    yield return exec.Current;
                    viu |= prof.Visibility > 0f;
                    max = Mathf.Max(max, prof.Suspicion);
                }
                feitas++;
                if (cola == Cola.Nerd && v + 1 < vezes && prof.State != ProfState.Flagrante)
                    yield return null; // continua em pé ao lado do Nerd para a 2ª espiada
            }
            if (cola == Cola.Nerd && prof.State != ProfState.Flagrante)
            {
                // depois de espiar, o jogador volta a sentar (no jogo: 1–2 s andando; aqui, na hora). Sem isso, o que
                // se mede é "ficou em pé fora do lugar na frente dele", não a espiada
                p.SendBackToSeat();
                obs += "voltou a sentar na hora; ";
            }
            Assert.Greater(feitas, 0, "a cola não foi feita");
            Assert.IsTrue(viu, "o professor não chegou a ver o jogador (caso inválido): " + s);
            if (vezes == 2 && feitas == 2) obs += "feita 2×; ";
            if (cola == Cola.Bilhete && MatchLog.BilheteErradas > 0) obs += "bilhete se perdeu/interceptado; ";
            l.obs = obs.TrimEnd(' ', ';');
            yield return Observar(l, max);
        }

        /// <summary>Anota a suspeita do fim da cola e deixa o professor agir sozinho por até 10 s.</summary>
        IEnumerator Observar(Linha l, float max)
        {
            var gm = GameManager.I;
            var prof = ProfessorAI.I;
            l.suspFim = prof.Suspicion;
            bool investigou = prof.State == ProfState.Investigando, desconfiou = prof.State != ProfState.Tranquilo;
            bool caraDePaisagem = false;
            for (float t = 0f; t < 10f; t += Time.deltaTime)
            {
                if (prof.State == ProfState.Flagrante || gm.Flagrantes > 0) break;
                if (FacePokerChallenge.I != null && FacePokerChallenge.I.Active) { caraDePaisagem = true; break; }
                investigou |= prof.State == ProfState.Investigando;
                desconfiou |= prof.State != ProfState.Tranquilo;
                max = Mathf.Max(max, prof.Suspicion);
                yield return null;
            }
            if (prof.State == ProfState.Flagrante || gm.Flagrantes > 0)
            {
                l.flagrante = true;
                l.infracao = LastInfracao.GetValue(prof).ToString();
                float ate = Time.time + 9f;
                while (gm.Flagrantes == 0 && Time.time < ate) yield return null; // ele vai até a carteira e aplica
                l.desfecho = "FLAGRANTE";
            }
            else
            {
                l.infracao = LastInfracao.GetValue(prof) + " (registrada)";
                l.desfecho = caraDePaisagem ? "investigou → Cara de Paisagem (depende do jogador)"
                           : investigou ? "investigou, sem achar"
                           : desconfiou ? "só desconfiou"
                           : "nada";
            }
            l.suspMax = Mathf.Max(max, l.suspFim);
            l.flagrantes = gm.Flagrantes;
            linhas.Add(l);
            Debug.Log("[Auditoria] " + l.cola + " | " + l.dif + " | " + l.situ + " | susp. fim " + l.suspFim.ToString("0") + " | " + l.infracao
                      + " | flagrante " + l.flagrante + " | " + l.desfecho);
        }

        // ------------------------------------------------------------------ execução de cada cola

        static int Certas => ExamSystem.I.Count(QuestionState.Certa);

        /// <summary>Achata corrotinas aninhadas: o caso enxerga CADA frame (para medir a visão e a suspeita frame a frame).</summary>
        static IEnumerator Plano(IEnumerator e)
        {
            var pilha = new Stack<IEnumerator>();
            pilha.Push(e);
            while (pilha.Count > 0)
            {
                var topo = pilha.Peek();
                if (!topo.MoveNext()) { pilha.Pop(); continue; }
                if (topo.Current is IEnumerator filho) { pilha.Push(filho); continue; }
                yield return topo.Current;
            }
        }

        IEnumerator Executar(Cola cola, PlayerController p, NerdStudent nerd, Action aCadaFrame)
        {
            var prof = ProfessorAI.I;
            var c = p.Cheats;
            bool Pego() => prof.State == ProfState.Flagrante;
            IEnumerator Segurar(Key k, Func<bool> ate, float timeout)
            {
                InputSystem.QueueStateEvent(kb, new KeyboardState(k));
                for (float fim = Time.time + timeout; Time.time < fim && !ate() && !Pego();)
                {
                    aCadaFrame();
                    yield return null;
                }
                InputSystem.QueueStateEvent(kb, new KeyboardState());
                aCadaFrame();
                yield return null;
            }

            switch (cola)
            {
                case Cola.Colinha:
                {
                    int antes = MatchLog.ColinhaUsos;
                    yield return Segurar(Key.Q, () => MatchLog.ColinhaUsos > antes, 4f);
                    break;
                }
                case Cola.Garrafinha:
                {
                    int antes = MatchLog.GarrafaUsos;
                    yield return Segurar(Key.G, () => MatchLog.GarrafaUsos > antes, 4f);
                    break;
                }
                case Cola.Bilhete:
                {
                    aCadaFrame();
                    Assert.IsTrue(c.ThrowAt(nerd), "não deu para jogar o bilhete");
                    while (c.Throwing && !Pego())
                    {
                        aCadaFrame();
                        yield return null;
                    }
                    aCadaFrame();
                    yield return null;
                    break;
                }
                case Cola.Fone:
                {
                    yield return Segurar(Key.F, () => c.fone.Waiting, 4f);
                    for (float fim = Time.time + 4f; Time.time < fim && !c.fone.Arrived && !Pego();)
                    {
                        aCadaFrame();
                        yield return null;
                    }
                    if (c.fone.Arrived && !Pego())
                    {
                        int antes = MatchLog.FoneCertas + MatchLog.FoneErradas;
                        yield return Segurar(Key.F, () => MatchLog.FoneCertas + MatchLog.FoneErradas > antes, 4f);
                    }
                    break;
                }
                case Cola.Celular:
                {
                    int antes = Certas;
                    yield return Segurar(Key.Digit2, () => Certas > antes, p.phoneHoldTime + 2f);
                    break;
                }
                case Cola.Nerd:
                {
                    int antes = MatchLog.NerdCopias;
                    yield return Segurar(Key.E, () => MatchLog.NerdCopias > antes, nerd.copyTime + 2f);
                    break;
                }
            }
        }

        static bool PodeFazer(Cola cola, PlayerController p, NerdStudent nerd) => PorQueNao(cola, p, nerd) == null;

        static string PorQueNao(Cola cola, PlayerController p, NerdStudent nerd)
        {
            var c = p.Cheats;
            if (!CheatBudget.CanAnswer) return "orçamento";
            switch (cola)
            {
                case Cola.Colinha: return c.WhyNot(PlayerCheats.Hold.Colinha) != null ? "colinha: " + c.WhyNot(PlayerCheats.Hold.Colinha) : null;
                case Cola.Garrafinha: return c.WhyNot(PlayerCheats.Hold.Garrafinha) != null ? "garrafinha: " + c.WhyNot(PlayerCheats.Hold.Garrafinha) : null;
                case Cola.Fone: return c.fone.Busy ? "fone ocupado" : c.WhyNot(PlayerCheats.Hold.FoneEnviar) != null ? "fone: " + c.WhyNot(PlayerCheats.Hold.FoneEnviar) : null;
                case Cola.Bilhete: return c.BilheteCharges <= 0 ? "sem bilhete" : c.bilhete.Busy ? "bilhete voltando" : c.Throwing ? "bilhete no ar" : null;
                case Cola.Celular: return !p.HasPhone ? "celular confiscado" : p.PhoneCharges <= 0 ? "sem bateria" : (float)PhoneCooldown.GetValue(p) > 0f ? "recarga do celular" : null;
                case Cola.Nerd: return nerd.CanInteract(p) ? null : "Nerd em recarga/sem cópias";
            }
            return null;
        }

        /// <summary>Metade do tempo da ação suspeita (é quando, na situação c, ele se vira).</summary>
        static float MeioDaCola(Cola cola, DifficultySettings s, PlayerController p, NerdStudent nerd) => cola switch
        {
            Cola.Colinha => s.colinhaTime * 0.5f,
            Cola.Garrafinha => s.garrafaTime * 0.5f,
            Cola.Bilhete => s.bilheteThrowTime * 0.5f,
            Cola.Fone => s.foneUseTime * 0.5f,
            Cola.Celular => p.phoneHoldTime * 0.5f,
            _ => nerd.copyTime * 0.5f,
        };

        static string Nome(Situacao s) => s switch
        {
            Situacao.A_PertoVendo => "a) vendo de perto",
            Situacao.B_LongeNoCone => "b) vendo de longe (no cone)",
            Situacao.C_ViraNoMeio => "c) de costas, vira no meio",
            _ => "d) já desconfiado, 2× seguidas",
        };

        // ------------------------------------------------------------------ cena

        IEnumerator Comecar(Difficulty d, float ringChance)
        {
            Time.timeScale = 1f;
            InputSystem.QueueStateEvent(kb, new KeyboardState());
            yield return EnterGame();
            var gm = GameManager.I;
            var s = d == Difficulty.Dificil ? gm.dificil : gm.normal;
            // só isolamento (ver o resumo da classe): nada de suspeita, visão ou flagrante muda
            s.eventInterval = new Vector2(9999f, 9999f);
            s.snitchInterval = new Vector2(9999f, 9999f);
            s.nerdGlanceInterval = new Vector2(999f, 999f);
            s.bilheteMissChancePerMeter = 0f;
            s.foneDelay = 1.5f;
            s.foneRingChance = ringChance;
            gm.Begin(d);
            yield return Wait(0.3f);
        }

        /// <summary>Professor parado em <paramref name="pos"/>, olhando para o jogador (ou de costas para ele).</summary>
        static void Posar(ProfessorAI prof, PlayerController p, Vector3 pos, bool olhando)
        {
            // investigando ou indo dar o flagrante: deixa ele ANDAR até o lugar (segurar ele parado faria a investigação
            // "chegar" na hora, a 10 m, e decidir de longe — coisa que no jogo não acontece)
            if (prof.State == ProfState.Flagrante || prof.State == ProfState.Investigando) return;
            var agent = prof.GetComponent<NavMeshAgent>();
            if (agent.isOnNavMesh) { agent.Warp(pos); agent.isStopped = true; }
            else prof.transform.position = pos;
            Vector3 to = p.transform.position - pos;
            to.y = 0f;
            prof.transform.rotation = Quaternion.LookRotation(olhando ? to : -to);
            prof.visual.headYaw = 0f;
            prof.visual.headPitch = 0f;
        }

        /// <summary>Um ponto da NavMesh entre <paramref name="dMin"/> e <paramref name="dMax"/> do jogador de onde o professor o vê.</summary>
        static Vector3? AcharLugar(ProfessorAI prof, PlayerController p, float dMin, float dMax)
        {
            Vector3 olho = prof.visual.HeadPosition - prof.transform.position;
            for (float d = dMax; d >= dMin - 0.01f; d -= 0.4f)
                for (int ang = 0; ang < 360; ang += 15)
                {
                    Vector3 tentativa = p.transform.position + Quaternion.Euler(0f, ang, 0f) * Vector3.forward * d;
                    if (!NavMesh.SamplePosition(tentativa, out var hit, 0.6f, NavMesh.AllAreas)) continue;
                    float real = Flat.Distance(hit.position, p.transform.position);
                    if (real < dMin - 0.2f || real > dMax + 0.2f) continue;
                    Vector3 eye = hit.position + olho;
                    Vector3 fwd = p.HeadPosition - eye;
                    fwd.y = 0f;
                    if (Vision.Visibility(eye, fwd.normalized, prof.viewDistance, prof.halfFovCalmo, p.HeadPosition, p.ChestPosition) > 0f)
                        return hit.position;
                }
            return null;
        }

        static Vector3? PertoDoNerd(NerdStudent nerd)
        {
            for (float d = 0.7f; d <= 1.3f; d += 0.2f)
                for (int ang = 0; ang < 360; ang += 20)
                {
                    Vector3 t = nerd.transform.position + Quaternion.Euler(0f, ang, 0f) * Vector3.forward * d;
                    if (NavMesh.SamplePosition(t, out var hit, 0.3f, NavMesh.AllAreas)) return hit.position;
                }
            return null;
        }
    }
}
