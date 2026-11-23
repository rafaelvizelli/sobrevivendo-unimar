using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static ProvaImpossivel.Tests.FluxoEProfessorTests;

namespace ProvaImpossivel.Tests
{
    /// <summary>Operação Steam (blocos S1 em diante), testes com a cena real da Fase 1.</summary>
    public class OperacaoSteamPlayTests
    {
        static IEnumerator ComecaNormal()
        {
            yield return EnterGame();
            GameManager.I.Begin(Difficulty.Normal);
            yield return null;
        }

        [TearDown]
        public void Limpa() => Time.timeScale = 1f;

        // ------------------------------------------------------------------ S1: kit de playtest

        [UnityTest]
        public IEnumerator S1_InvestigacaoESuspeitaMaximaVaoProRegistro()
        {
            yield return ComecaNormal();
            Assert.AreEqual(0, MatchLog.Investigacoes, "a prova nova começa zerada");
            var prof = ProfessorAI.I;
            var p = PlayerController.I;
            prof.Alert(p.transform.position, 80f / GameManager.I.normal.suspicionMultiplier, "teste");
            yield return null;
            Assert.AreEqual(ProfState.Investigando, prof.State, "pré-condição");
            Assert.AreEqual(1, MatchLog.Investigacoes);
            Assert.GreaterOrEqual(MatchLog.SuspeitaMaxima, 65f, "a maior suspeita da partida tem que ter sido registrada");
        }

        // ------------------------------------------------------------------ S2: investigação acha provas

        static readonly System.Reflection.FieldInfo LastInfracao =
            typeof(ProfessorAI).GetField("lastInfracao", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        /// <summary>Normal sem acontecimentos sorteados nem viradas do Puxa-saco (só o que o teste provoca).</summary>
        static IEnumerator ComecaNormalIsolado()
        {
            yield return EnterGame();
            var s = GameManager.I.normal;
            s.eventInterval = new Vector2(9999f, 9999f);
            s.snitchInterval = new Vector2(9999f, 9999f);
            s.nerdGlanceInterval = new Vector2(999f, 999f);
            GameManager.I.Begin(Difficulty.Normal);
            yield return null;
        }

        /// <summary>Ele ouviu o papel do bilhete perto de você e vem investigar (sem ver cola nenhuma no caminho).</summary>
        static void OuveBilhete(ProfessorAI prof, PlayerController p) =>
            prof.HearNoise(p.transform.position, 100f, 100f, "teste: papel", Infracao.Bilhete);

        [UnityTest]
        public IEnumerator S2_InvestigandoOPapelEleAchaOBilhete()
        {
            yield return ComecaNormalIsolado();
            var prof = ProfessorAI.I;
            var p = PlayerController.I;
            prof.bilheteFindChance = 1f; // sempre acha: o teste é do caminho, não do sorteio
            OuveBilhete(prof, p);
            Assert.AreEqual(ProfState.Investigando, prof.State, "pré-condição: o papel fez ele investigar");
            yield return WaitUntil(() => prof.State == ProfState.Flagrante || (FacePokerChallenge.I != null && FacePokerChallenge.I.Active),
                                   30f, "ele não chegou a decidir nada");
            Assert.AreEqual(ProfState.Flagrante, prof.State, "chegou na carteira e não achou o bilhete");
            Assert.AreEqual(Infracao.Bilhete, LastInfracao.GetValue(prof), "o flagrante tem que ser do tipo Bilhete");
        }

        [UnityTest]
        public IEnumerator S2_SemAcharAProvaVaiParaACaraDePaisagem()
        {
            yield return ComecaNormalIsolado();
            var prof = ProfessorAI.I;
            var p = PlayerController.I;
            prof.bilheteFindChance = 0f;
            OuveBilhete(prof, p);
            yield return WaitUntil(() => prof.State == ProfState.Flagrante || (FacePokerChallenge.I != null && FacePokerChallenge.I.Active),
                                   30f, "ele não chegou a decidir nada");
            Assert.IsTrue(FacePokerChallenge.I.Active, "sem achar nada, o jogador ainda tem a Cara de Paisagem");
        }

        [UnityTest]
        public IEnumerator S2_DeduradoNaoApagaOQueEleOuviu()
        {
            yield return ComecaNormalIsolado();
            var prof = ProfessorAI.I;
            var p = PlayerController.I;
            prof.HearNoise(p.transform.position, 100f, 5f, "teste: papel", Infracao.Bilhete);
            Assert.AreEqual(Infracao.Bilhete, prof.Evidence);
            prof.Alert(p.transform.position, 10f, "dedurado pelo Puxa-saco", Infracao.Dedurado);
            Assert.AreEqual(Infracao.Bilhete, prof.Evidence, "o 'Dedurado' apagou a prova que ele tinha ouvido");
        }

        [UnityTest]
        public IEnumerator S2_ProvaVelhaNaoValeMais()
        {
            yield return ComecaNormalIsolado();
            var prof = ProfessorAI.I;
            prof.evidenceMemorySeconds = 0.2f;
            prof.HearNoise(PlayerController.I.transform.position, 100f, 5f, "teste: papel", Infracao.Bilhete);
            Assert.AreEqual(Infracao.Bilhete, prof.Evidence);
            yield return Wait(0.4f);
            Assert.AreEqual(Infracao.Nenhuma, prof.Evidence, "barulho de muito tempo atrás não deixa prova");
        }

        // ------------------------------------------------------------------ S4: o professor estrela

        /// <summary>Altura (y) da ponta de dentro e da ponta de fora de uma sobrancelha.</summary>
        static (float dentro, float fora) Pontas(Transform brow, bool direita)
        {
            Vector3 meio = brow.right * (brow.lossyScale.x * 0.5f);
            // na sobrancelha direita a ponta de dentro fica para o lado do nariz (-x dela); na esquerda, +x
            Vector3 dentro = direita ? brow.position - meio : brow.position + meio;
            Vector3 fora = direita ? brow.position + meio : brow.position - meio;
            return (dentro.y, fora.y);
        }

        [UnityTest]
        public IEnumerator S4_SobrancelhasDoProfessorAtuam()
        {
            yield return EnterGame(); // no menu o professor não mexe na própria expressão: o teste manda
            var prof = ProfessorAI.I;
            var brows = prof.GetComponentInChildren<ExpressiveBrows>();
            Assert.IsNotNull(brows, "o professor (modelo 3D) não ganhou sobrancelhas");

            prof.visual.expression = Expression.Neutro;
            yield return Wait(0.6f);
            float neutroE = brows.Left.position.y, neutroD = brows.Right.position.y;

            prof.visual.expression = Expression.Bravo;
            yield return Wait(0.6f);
            var e = Pontas(brows.Left, false);
            var d = Pontas(brows.Right, true);
            Assert.Less(e.dentro, e.fora, "bravo: a ponta de dentro da esquerda tinha que descer");
            Assert.Less(d.dentro, d.fora, "bravo: a ponta de dentro da direita tinha que descer");

            prof.visual.expression = Expression.Assustado;
            yield return Wait(0.6f);
            Assert.Greater(brows.Left.position.y, neutroE + 0.01f, "assustado: as sobrancelhas sobem");
            Assert.Greater(brows.Right.position.y, neutroD + 0.01f);

            prof.visual.expression = Expression.Desconfiado;
            yield return Wait(0.6f);
            Assert.Greater(brows.Right.position.y - neutroD, brows.Left.position.y - neutroE + 0.015f, "desconfiado: uma sobe mais que a outra");
        }

        [UnityTest]
        public IEnumerator S4_FlagranteViraACameraEBateOCarimbo()
        {
            yield return ComecaNormalIsolado();
            var prof = ProfessorAI.I;
            var cam = Camera.main.GetComponent<ThirdPersonCamera>();
            float fovAntes = Camera.main.fieldOfView;
            int ahaAntes = SoundFX.PlayCount(Sfx.Aha);

            Assert.IsTrue(prof.CatchRedHanded("teste", Infracao.Celular), "pré-condição: flagrante");
            Assert.AreEqual(ahaAntes + 1, SoundFX.PlayCount(Sfx.Aha), "o professor tinha que dizer 'a-HÁ!'");
            yield return Wait(0.6f);
            Assert.Greater(cam.FocusWeight, 0.5f, "a câmera não virou para o professor");
            Assert.Less(Camera.main.fieldOfView, fovAntes - 1f, "a câmera não fechou (zoom)");

            yield return WaitUntil(() => GameManager.I.Flagrantes == 1, 10f, "o flagrante não foi aplicado");
            var stamp = HUD.I.GetComponent<FlagranteStamp>();
            Assert.IsNotNull(stamp, "sem carimbo");
            Assert.IsTrue(stamp.Visible, "o carimbo tinha que estar na tela");

            yield return Wait(3.5f);
            Assert.IsFalse(stamp.Visible, "o carimbo tinha que sumir");
            Assert.AreEqual(0f, cam.FocusWeight, 0.001f, "a câmera tinha que voltar");
            Assert.AreEqual(fovAntes, Camera.main.fieldOfView, 0.1f);
        }

        // ------------------------------------------------------------------ S5: trilha adaptativa

        [UnityTest]
        public IEnumerator S5_TrilhaSobeComAInvestigacaoEToca_OEstingerNoFlagrante()
        {
            yield return ComecaNormalIsolado();
            var m = MusicDirector.I;
            Assert.IsNotNull(m, "a trilha não foi criada junto do som");
            yield return Wait(1.5f);
            Assert.Greater(m.Layer(0), 0.5f, "na prova tranquila a camada calma toca");
            Assert.Less(m.Layer(2), 0.05f, "sem investigação não tem tambor");

            var prof = ProfessorAI.I;
            prof.Alert(PlayerController.I.transform.position, 80f / GameManager.I.normal.suspicionMultiplier, "teste");
            yield return Wait(2f);
            Assert.AreEqual(ProfState.Investigando, prof.State, "pré-condição");
            Assert.Greater(m.Layer(2), 0.6f, "investigando, o tambor tinha que entrar");
        }

        [UnityTest]
        public IEnumerator S5_FlagranteTocaOTromboneEAbaixaAsCamadas()
        {
            yield return ComecaNormalIsolado();
            var m = MusicDirector.I;
            yield return Wait(1f);
            int antes = m.StingerCount;
            Assert.IsTrue(ProfessorAI.I.CatchRedHanded("teste", Infracao.Celular), "pré-condição");
            yield return null;
            yield return null;
            Assert.AreEqual(antes + 1, m.StingerCount, "o flagrante tinha que tocar o trombone");
            yield return Wait(1f);
            Assert.LessOrEqual(m.Layer(2), 0.05f, "durante o trombone o tambor sai do caminho");
            Assert.LessOrEqual(m.Layer(0), 0.45f, "durante o trombone a calma abaixa");
        }

        [UnityTest]
        public IEnumerator S5_VolumeDaMusicaZeroSilenciaATrilha()
        {
            float antes = GameOptions.MusicVolume;
            try
            {
                yield return ComecaNormalIsolado();
                GameOptions.SetMusicVolume(0f);
                yield return Wait(0.3f);
                foreach (var s in MusicDirector.I.GetComponents<AudioSource>())
                    if (s.clip != null && s.clip.name.StartsWith("musica_"))
                        Assert.AreEqual(0f, s.volume, 1e-5f, s.clip.name + " ainda toca com o volume da música em 0");
            }
            finally { GameOptions.SetMusicVolume(antes); }
        }

        // ------------------------------------------------------------------ S6: título, dica, recorde

        static Transform Achar(Transform root, string name)
        {
            if (root.name == name) return root;
            foreach (Transform c in root) { var r = Achar(c, name); if (r != null) return r; }
            return null;
        }

        [UnityTest]
        public IEnumerator S6_AprovadoGanhaTituloDeEstiloERecorde()
        {
            yield return ComecaNormalIsolado();
            var exam = ExamSystem.I;
            for (int i = 0; i < 8; i++) exam.MarkCorrect();
            MatchLog.Celular = 1;
            GameManager.I.DeliverExam();
            yield return null;
            var panel = HUD.I.PanelRoot.Find("Resultado");
            Assert.IsNotNull(panel);
            Assert.IsNotNull(Achar(panel, "TituloEstilo"), "aprovado tinha que ganhar o carimbo do título de estilo");
            Assert.IsNull(Achar(panel, "Dica"), "quem passou não recebe dica");
            var rodape = Achar(panel, "Rodape").GetComponent<UnityEngine.UI.Text>();
            StringAssert.Contains(Falas.ResumoRecorde, rodape.text, "o rodapé mostra o recorde");
            Assert.GreaterOrEqual(Records.BestGrade(GameManager.I.normal.name), 8f, "o recorde foi gravado");
        }

        [UnityTest]
        public IEnumerator S1_PrimeiroFlagranteGuardaOSegundoDaProva()
        {
            yield return ComecaNormal();
            yield return Wait(0.5f);
            GameManager.I.ApplyFlagrante("teste", Infracao.Celular);
            Assert.That(MatchLog.PrimeiroFlagranteEm, Is.InRange(0.3f, 3f), "segundos de prova até o 1º flagrante");
        }
    }
}
