using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.UI;
using static ProvaImpossivel.Tests.FluxoEProfessorTests;

namespace ProvaImpossivel.Tests
{
    /// <summary>
    /// Bloco 12 — consolidação: efeito de cada ameaça, ronco esperando o aviso, Fundão sem custo quando não tenta,
    /// ícones só de perto, cone igual à visão do jogo, gabarito novo por partida, tutorial pelo menu e pela tecla T.
    /// </summary>
    public class ConsolidacaoTests
    {
        int? flagAntes;

        [SetUp]
        public void Guardar() => flagAntes = PlayerPrefs.HasKey(TutorialFlag.Key) ? PlayerPrefs.GetInt(TutorialFlag.Key) : null;

        [TearDown]
        public void Devolver()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            if (flagAntes.HasValue) PlayerPrefs.SetInt(TutorialFlag.Key, flagAntes.Value);
            else PlayerPrefs.DeleteKey(TutorialFlag.Key);
            PlayerPrefs.Save();
        }

        static IEnumerator StartNormal()
        {
            yield return EnterGame();
            GameManager.I.Begin(Difficulty.Normal);
            yield return null;
        }

        /// <summary>Roda um acontecimento (coroutine privada do EventDirector) do jeito que o sorteio rodaria.</summary>
        static EventDirector Rodar(string acontecimento)
        {
            var dir = Object.FindAnyObjectByType<EventDirector>();
            var m = typeof(EventDirector).GetMethod(acontecimento, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(m, acontecimento);
            dir.StartCoroutine((IEnumerator)m.Invoke(dir, null));
            return dir;
        }

        static Vector3 ParadaMaisPertoDoJogador(ProfessorAI prof)
        {
            Vector3 p = PlayerController.I.transform.position, best = Vector3.zero;
            float bestD = float.MaxValue;
            foreach (var t in prof.patrolPoints)
            {
                float d = (t.position - p).sqrMagnitude;
                if (d < bestD) { bestD = d; best = t.position; }
            }
            return best;
        }

        // ------------------------------------------------------------------ ameaças: o que muda no professor

        [UnityTest]
        public IEnumerator Ameaca_RecolheCelulares_DeixaAtentoEVemProCorredorDoJogador()
        {
            yield return StartNormal();
            var prof = ProfessorAI.I;
            Assert.IsFalse(prof.Alerted, "pré-condição");
            Rodar("RecolheCelulares");
            yield return null;
            Assert.IsTrue(prof.Alerted, "a conferência de celulares deixa o professor ATENTO");
            yield return WaitUntil(() => !prof.TurningSoon, 3f, "o aviso de virada não terminou");
            yield return null;
            var agent = prof.GetComponent<NavMeshAgent>();
            Vector3 alvo = ParadaMaisPertoDoJogador(prof);
            Vector3 d = agent.destination - alvo; d.y = 0f;
            Assert.Less(d.magnitude, 1f, "ele deveria ir até a parada da ronda mais perto do jogador");
        }

        [UnityTest]
        public IEnumerator Ameaca_MudaARonda_VaiProLadoDoJogador()
        {
            yield return StartNormal();
            var prof = ProfessorAI.I;
            Rodar("MudaARonda");
            yield return null;
            Assert.IsFalse(prof.Alerted, "mudar a ronda não deixa atento (é a outra ameaça)");
            yield return WaitUntil(() => !prof.TurningSoon, 3f, "o aviso de virada não terminou");
            yield return null;
            var agent = prof.GetComponent<NavMeshAgent>();
            Vector3 d = agent.destination - ParadaMaisPertoDoJogador(prof); d.y = 0f;
            Assert.Less(d.magnitude, 1f, "a ronda deveria mudar para o lado do jogador");
        }

        [UnityTest]
        public IEnumerator Ameaca_PedemSilencio_SobeASuspeitaEOlhaPraCa()
        {
            yield return StartNormal();
            var prof = ProfessorAI.I;
            float antes = prof.Suspicion;
            Rodar("PedemSilencio");
            yield return Wait(1.5f);
            Assert.Greater(prof.Suspicion, antes, "o pedido de silêncio deveria subir a suspeita");
            Assert.That(prof.State, Is.EqualTo(ProfState.Desconfiado).Or.EqualTo(ProfState.Investigando), "ele deveria desconfiar");
        }

        [UnityTest]
        public IEnumerator Ameaca_Espirro_OProfessorOuveDeLonge()
        {
            yield return StartNormal();
            var prof = ProfessorAI.I;
            Rodar("Espirro");
            yield return null;
            Assert.Greater(prof.Suspicion, 0f, "o espirro deveria ser ouvido (raio da sala inteira)");
            Assert.AreNotEqual(ProfState.Tranquilo, prof.State);
        }

        // ------------------------------------------------------------------ ronco espera o aviso

        [UnityTest]
        public IEnumerator Ronco_EsperaOAvisoDoDorminhoco()
        {
            yield return StartNormal();
            var prof = ProfessorAI.I;
            var dorminhoco = Object.FindAnyObjectByType<SleeperStudent>();
            float aviso = GameManager.I.Settings.snoreWarningSeconds;
            float inicio = Time.time;
            Rodar("Ronco");
            yield return Wait(0.3f);
            Assert.IsTrue(dorminhoco.Nodding, "antes do ronco ele cabeceia");
            Assert.AreNotEqual(ProfState.Distraido, prof.State, "o professor não pode ir antes do ronco");
            yield return WaitUntil(() => prof.State == ProfState.Distraido, aviso + 2f, "o ronco não distraiu o professor");
            Assert.GreaterOrEqual(Time.time - inicio, aviso - 0.1f, "o ronco veio antes do fim do aviso");
        }

        // ------------------------------------------------------------------ Fundão: pedido recusado não custa nada

        [UnityTest]
        public IEnumerator Fundao_PedidoRecusadoNaoTemPsiuNemRecarga()
        {
            yield return StartNormal();
            var p = PlayerController.I;
            var prof = ProfessorAI.I;
            var fundao = Object.FindAnyObjectByType<BackRowStudent>();
            prof.Alert(p.transform.position, 70f / GameManager.I.normal.suspicionMultiplier, "teste");
            Assert.AreEqual(ProfState.Investigando, prof.State, "pré-condição");

            int ruidos = 0;
            void Conta(NoiseInfo n) => ruidos++;
            Noise.Emitted += Conta;
            try
            {
                fundao.Resolve(p, 0.1f); // derrubar tudo: recusado (investigando)
                fundao.Resolve(p, 0.9f); // show: recusado
            }
            finally { Noise.Emitted -= Conta; }
            Assert.AreEqual(0, ruidos, "pedido recusado não pode fazer 'psiu'");
            Assert.AreEqual(GameManager.I.Settings.favors, GameManager.I.Favors, "nem gastar favor");
            Assert.IsTrue(fundao.CanInteract(p), "nem entrar em recarga (dá pra pedir de novo)");
        }

        // ------------------------------------------------------------------ ícones só de perto

        [UnityTest]
        public IEnumerator IconeDoNerdSoApareceDePerto()
        {
            yield return StartNormal();
            var nerd = Object.FindAnyObjectByType<NerdStudent>();
            var icon = nerd.GetComponent<MoodIcon>();
            var p = PlayerController.I;
            nerd.GlanceSoon();
            yield return null;
            Assert.AreEqual("...", icon.Symbol, "sentado do lado do Nerd (1,35 m): o aviso aparece");

            p.StandUp();
            var cc = p.GetComponent<CharacterController>();
            cc.enabled = false;
            p.transform.position = new Vector3(-4.6f, 0f, -5f); // outro canto da sala
            cc.enabled = true;
            yield return null;
            Assert.AreEqual("", icon.Symbol, "longe (além de studentHintDistance) o ícone some");
        }

        /// <summary>
        /// Bloco 15b: sentado, com a carteira colada na do Nerd, o "O_O" tem tamanho limitado na tela e não cobre
        /// os painéis do HUD (tempo, NOTA, COLAS).
        /// </summary>
        [UnityTest]
        public IEnumerator IconeDoNerdDePertoTemTamanhoLimitadoENaoCobreAHud()
        {
            yield return StartNormal();
            var nerd = Object.FindAnyObjectByType<NerdStudent>();
            var icon = nerd.GetComponent<MoodIcon>();
            // gira a câmera para o lado do Nerd (como quem confere se ele vai olhar); a câmera guarda o giro em "yaw"
            var rig = Object.FindAnyObjectByType<ThirdPersonCamera>();
            Vector3 para = nerd.transform.position - PlayerController.I.transform.position;
            typeof(ThirdPersonCamera).GetField("yaw", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(rig, Quaternion.LookRotation(new Vector3(para.x, 0f, para.z)).eulerAngles.y);
            nerd.GlanceSoon();
            yield return WaitUntil(() => nerd.Glancing, 4f, "o Nerd não chegou a olhar de lado");
            yield return null;
            yield return null;
            Assert.AreEqual("O_O", icon.Symbol);
            var r = icon.ScreenRect;
            // a caixa do símbolo, com folga para a pulsação do "O_O" (até +18%)
            Assert.LessOrEqual(r.height, Screen.height * MoodIcon.MaxScreenFraction * 1.2f,
                "o ícone passou do teto de tamanho (" + r.height + " px de " + Screen.height + ")");
            Assert.Greater(r.height, Screen.height * 0.03f, "o ícone ficou pequeno demais para ler");

            var hud = HUD.I;
            Rect R(RectTransform t) { var c = new Vector3[4]; t.GetWorldCorners(c); return Rect.MinMaxRect(c[0].x, c[0].y, c[2].x, c[2].y); }
            Assert.IsFalse(r.Overlaps(R(hud.GradePanel)), "o ícone cobre o painel da NOTA");
            Assert.IsFalse(r.Overlaps(R(hud.CheatsPanel)), "o ícone cobre o painel das COLAS");
        }

        // ------------------------------------------------------------------ cone = visão do jogo

        [UnityTest]
        public IEnumerator ConeMostraExatamenteOQueOProfessorVe()
        {
            yield return StartNormal();
            yield return Wait(0.3f);
            GameManager.I.Pause(); // congela o professor (pose fixa) — o cone continua sendo desenhado na pausa
            var prof = ProfessorAI.I;
            var cone = Object.FindAnyObjectByType<VisionConeView>();
            for (int i = 0; i < 4; i++) yield return null; // o cone refaz o teste de visão a cada 2 frames

            Vector3 eye = prof.visual.HeadPosition;
            Vector3 fwd = prof.visual.HeadForward; fwd.y = 0f; fwd.Normalize();
            Vector3 pe = new(prof.transform.position.x, 0f, prof.transform.position.z);
            int rays = cone.segments + 1, vistos = 0, escondidos = 0;
            // exatamente nos vértices do cone: raios 1/6, 1/2 e 5/6 do leque, a cada "step" metros
            foreach (int r in new[] { cone.segments / 6, cone.segments / 2, cone.segments * 5 / 6 })
            {
                float a = Mathf.Lerp(-prof.CurrentFov, prof.CurrentFov, r / (float)(rays - 1));
                Vector3 dir = Quaternion.Euler(0f, a, 0f) * fwd;
                for (float d = cone.step; d < prof.CurrentViewDistance - 0.01f; d += cone.step)
                {
                    Vector3 chao = pe + dir * d;
                    if (!cone.InsideRoom(chao)) continue; // fora da sala o cone não é desenhado (recorte visual do Bloco 13)
                    bool jogo = Vision.LineOfSight(eye, chao + Vector3.up * SeatedHeadH) || Vision.LineOfSight(eye, chao + Vector3.up * SeatedChestH);
                    Assert.AreEqual(jogo, cone.ShowsAsSeen(chao), "cone diferente da visão do jogo em " + chao + " (jogo vê: " + jogo + ")");
                    if (jogo) vistos++; else escondidos++;
                }
            }
            Assert.Greater(vistos, 5, "o cone deveria ter área vista");
            Debug.Log("[Cone] vértices conferidos: " + vistos + " vistos, " + escondidos + " escondidos");
            GameManager.I.Resume();
        }

        const float SeatedHeadH = 1.3f, SeatedChestH = 0.95f;

        // ------------------------------------------------------------------ gabarito novo a cada partida

        [UnityTest]
        public IEnumerator GabaritoDoChuteMudaACadaPartida()
        {
            yield return StartNormal();
            var a = QuizBank.GabaritoChutes;
            yield return StartNormal();
            CollectionAssert.AreNotEqual(a, QuizBank.GabaritoChutes, "cada partida deveria sortear um gabarito novo (GameManager.StartMatch)");
        }

        // ------------------------------------------------------------------ tutorial: botões do menu e tecla T

        [UnityTest]
        public IEnumerator MenuPularEDepoisRever()
        {
            TutorialFlag.Clear();
            yield return EnterGame();
            var pular = GameObject.Find("BotaoPULAR");
            var tutorial = GameObject.Find("BotaoTUTORIAL");
            Assert.IsNotNull(pular, "na primeira vez o menu tem 'Pular tutorial'");
            Assert.IsNotNull(tutorial);
            pular.GetComponent<Button>().onClick.Invoke();
            Assert.IsTrue(TutorialFlag.Done, "pular marca como visto");
            Assert.IsFalse(pular.activeSelf, "o botão de pular some");
            Assert.AreEqual(Phase.Intro, GameManager.I.Phase, "pular não começa partida nenhuma");
            tutorial.GetComponent<Button>().onClick.Invoke();
            Assert.IsTrue(GameManager.I.InTutorial, "o botão do tutorial começa o treino");
        }

        [UnityTest]
        public IEnumerator TeclaTPulaOTutorial()
        {
            TutorialFlag.Clear();
            yield return EnterGame();
            GameManager.I.BeginTutorial();
            yield return null;
            var settings = InputSystem.settings;
            var bgAntes = settings.backgroundBehavior;
#if UNITY_EDITOR
            var editorAntes = settings.editorInputBehaviorInPlayMode;
            settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            var kb = InputSystem.AddDevice<Keyboard>("TesteTecladoT");
            kb.MakeCurrent();
            try
            {
                yield return Wait(0.1f);
                InputSystem.QueueStateEvent(kb, new KeyboardState(Key.T));
                yield return WaitUntil(() => GameManager.I.Phase == Phase.Ended, 2f, "a tecla T não pulou o tutorial (input virtual não chegou?)");
                InputSystem.QueueStateEvent(kb, new KeyboardState());
                Assert.IsTrue(TutorialFlag.Done, "pular com T marca como visto");
            }
            finally
            {
                InputSystem.RemoveDevice(kb);
                settings.backgroundBehavior = bgAntes;
#if UNITY_EDITOR
                settings.editorInputBehaviorInPlayMode = editorAntes;
#endif
            }
        }
    }
}
