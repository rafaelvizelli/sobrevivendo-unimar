using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static ProvaImpossivel.Tests.FluxoEProfessorTests;

namespace ProvaImpossivel.Tests
{
    /// <summary>Bloco 14 — acabamento: tela de fim de partida com resumo e "jogar de novo".</summary>
    public class AcabamentoPlayTests
    {
        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
        }

        static Rect WorldRect(RectTransform r)
        {
            var c = new Vector3[4];
            r.GetWorldCorners(c);
            return Rect.MinMaxRect(c[0].x, c[0].y, c[2].x, c[2].y);
        }

        static RectTransform Find(Transform root, string name)
        {
            foreach (var r in root.GetComponentsInChildren<RectTransform>(true))
                if (r.name == name) return r;
            Assert.Fail("não achei '" + name + "' na tela de resultado");
            return null;
        }

        [UnityTest]
        public IEnumerator FimDaProvaMostraResumoCertoESemSobreposicao()
        {
            yield return EnterGame();
            var gm = GameManager.I;
            gm.Begin(Difficulty.Normal);
            yield return null;
            var exam = ExamSystem.I;
            exam.MarkCorrect();
            exam.MarkCorrect();
            exam.MarkWrong();
            MatchLog.Celular = 1;
            MatchLog.FundaoColaRuim = 1;
            gm.DeliverExam(); // nota 2: reprovado
            Assert.AreEqual(Phase.Ended, gm.Phase);

            var s = gm.LastSummary;
            Assert.AreEqual(LevelResult.Reprovado, s.result);
            Assert.AreEqual((2, 1, exam.questionCount - 3), (s.certas, s.erradas, s.emBranco));
            Assert.AreEqual(exam.Grade, s.grade);
            Assert.AreEqual(gm.Settings.minGrade, s.minGrade);
            Assert.AreEqual(1, s.colasCelular);
            Assert.AreEqual(1, s.colasFundao);
            Assert.AreEqual(0, s.favoresGastos);
            Assert.GreaterOrEqual(s.timeUsed, 0f);

            // layout: tudo dentro da caixa, colunas e botões sem se cruzar
            var panel = HUD.I.PanelRoot.Find("Resultado");
            Assert.IsNotNull(panel, "tela de resultado não apareceu");
            var box = WorldRect(Find(panel, "Caixa"));
            var parts = new List<Rect>
            {
                WorldRect(Find(panel, "ColunaProva")), WorldRect(Find(panel, "ColunaFicha")),
                WorldRect(Find(panel, "JogarDeNovo")), WorldRect(Find(panel, "Menu")), WorldRect(Find(panel, "Sair")),
                // S6: a dica (reprovado com questões em branco) e o rodapé com o recorde
                WorldRect(Find(panel, "Dica")), WorldRect(Find(panel, "Rodape")),
            };
            for (int i = 0; i < parts.Count; i++)
            {
                Assert.IsTrue(box.Contains(parts[i].min) && box.Contains(parts[i].max), "parte " + i + " saiu da caixa");
                for (int j = i + 1; j < parts.Count; j++)
                    Assert.IsFalse(parts[i].Overlaps(parts[j]), "partes " + i + " e " + j + " se sobrepõem");
            }
        }

        [UnityTest]
        public IEnumerator PausaTemAsTresOpcoesQueValemNaHora()
        {
            float master = GameOptions.MasterVolume, sfx = GameOptions.SfxVolume, sens = GameOptions.CameraSensitivity, musica = GameOptions.MusicVolume;
            yield return EnterGame();
            var gm = GameManager.I;
            gm.Begin(Difficulty.Normal);
            yield return null;
            Assert.IsTrue(HUD.I.ToastsVisible, "pré-condição: o começo da prova mostra mensagens no topo");
            gm.Pause();
            yield return null;
            try
            {
                Assert.IsFalse(HUD.I.ToastsVisible, "as mensagens do topo ficaram por cima da pausa");
                var panel = HUD.I.PanelRoot.Find("Pausa");
                Assert.IsNotNull(panel, "pausa não apareceu");
                var sliders = panel.GetComponentsInChildren<UnityEngine.UI.Slider>();
                int graficos = QualitySettings.names.Length > 1 ? 1 : 0; // S8: "Gráficos" só aparece com mais de um nível
                Assert.AreEqual(6 + graficos, sliders.Length, "volume geral, efeitos, música (S5), sensibilidade, tela, vsync (S8) e, se houver, gráficos");

                sliders[0].value = 0.3f;
                Assert.AreEqual(0.3f, GameOptions.MasterVolume, 1e-5f);
                Assert.AreEqual(0.3f, AudioListener.volume, 1e-5f, "volume geral não foi aplicado");
                sliders[1].value = 0.6f;
                Assert.AreEqual(0.6f, GameOptions.SfxVolume, 1e-5f);
                sliders[2].value = 0.4f;
                Assert.AreEqual(0.4f, GameOptions.MusicVolume, 1e-5f, "volume da música (S5)");
                sliders[3].value = 2f;
                Assert.AreEqual(2f, GameOptions.CameraSensitivity, 1e-5f);
                Assert.AreEqual(GameOptions.MaxCameraSensitivity, sliders[3].maxValue, "o slider usa os mesmos limites");

                // as linhas de opção não se cruzam com os botões
                var rows = new List<Rect>();
                foreach (Transform c in panel.GetComponentsInChildren<Transform>())
                    if (c.name == "Opcao" || c.name == "Botao") rows.Add(WorldRect((RectTransform)c));
                Assert.AreEqual(9 + graficos, rows.Count);
                for (int i = 0; i < rows.Count; i++)
                    for (int j = i + 1; j < rows.Count; j++)
                        Assert.IsFalse(rows[i].Overlaps(rows[j]), "itens " + i + " e " + j + " da pausa se sobrepõem");
            }
            finally
            {
                GameOptions.SetMasterVolume(master);
                GameOptions.SetSfxVolume(sfx);
                GameOptions.SetCameraSensitivity(sens);
                GameOptions.SetMusicVolume(musica);
                gm.Resume();
            }
            Assert.IsTrue(HUD.I.ToastsVisible, "as mensagens não voltaram depois da pausa");
        }

        // ------------------------------------------------------------ áudio ambiente

        static float Peak(AudioClip c)
        {
            var d = new float[c.samples * c.channels];
            c.GetData(d, 0);
            float p = 0f;
            foreach (float x in d) p = Mathf.Max(p, Mathf.Abs(x));
            return p;
        }

        [UnityTest]
        public IEnumerator AmbienteTocaSoDuranteAProva()
        {
            yield return EnterGame();
            yield return Wait(0.3f);
            Assert.AreEqual(0f, SoundFX.I.AmbientGain, 1e-4f, "no menu a sala fica em silêncio");
            GameManager.I.Begin(Difficulty.Normal);
            yield return WaitUntil(() => SoundFX.I.AmbientGain > 0.15f, 6f, "o ambiente não entrou na prova");
            Assert.Greater(SoundFX.I.AmbientVolume(Ambiente.Murmurio), 0f);
            Assert.Greater(SoundFX.I.AmbientVolume(Ambiente.Relogio), 0f);

            GameManager.I.Pause();
            yield return WaitUntil(() => SoundFX.I.AmbientGain < 0.001f, 2f, "na pausa o ambiente deveria sumir");
            GameManager.I.Resume();
        }

        [UnityTest]
        public IEnumerator SomImportanteAbaixaOAmbiente()
        {
            yield return EnterGame();
            GameManager.I.Begin(Difficulty.Normal);
            var fx = SoundFX.I;
            // a introdução da prova toca o sinal (importante): espera o ambiente voltar
            yield return WaitUntil(() => fx.AmbientGain > fx.mix.duckTo + 0.2f, 8f, "o ambiente não subiu");
            var prof = ProfessorAI.I;
            SoundFX.PlayAt(Sfx.Ouviu, prof.transform.position);
            yield return Wait(0.3f);
            Assert.LessOrEqual(fx.AmbientGain, fx.mix.duckTo + 0.01f, "o \"hm?\" tocou e o fundo não abaixou");
        }

        [UnityTest]
        public IEnumerator LapisECadeiraAparecemDeVezEmQuando()
        {
            yield return EnterGame();
            GameManager.I.Begin(Difficulty.Normal);
            yield return null;
            int lapis = SoundFX.AmbientCount(Ambiente.Lapis), cadeira = SoundFX.AmbientCount(Ambiente.Cadeira);
            Time.timeScale = 8f; // os intervalos contam no tempo de jogo: acelera para não esperar 30 s
            yield return WaitUntil(() => SoundFX.AmbientCount(Ambiente.Lapis) > lapis && SoundFX.AmbientCount(Ambiente.Cadeira) > cadeira,
                                   12f, "lápis/cadeira não tocaram");
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator AmbienteFicaBemAbaixoDosSonsDeJogo()
        {
            yield return EnterGame();
            var fx = SoundFX.I;
            // som de jogo mais baixo: passo do professor andando devagar (45% do volume dos passos), longe não conta
            float passo = fx.volume * 0.45f * Peak(fx.ClipOf(Sfx.Passo));
            float hm = fx.volume * 0.7f * Peak(fx.ClipOf(Sfx.Ouviu)); // o pigarro é o "hm" mais baixo
            foreach (Ambiente a in System.Enum.GetValues(typeof(Ambiente)))
            {
                float clipPeak = a == Ambiente.Cadeira ? Peak(fx.ClipOf(Sfx.Cadeira)) : SoundFX.AmbientClipPeak;
                float amb = fx.mix.Level(a) * clipPeak;
                Assert.Less(amb, passo * 0.5f, a + " no volume cheio já passa da metade de um passo do professor");
                Assert.Less(amb, hm * 0.5f, a + " compete com o \"hm\"");
            }

            // tensão: sobe com a suspeita e nunca passa do teto do mix
            GameManager.I.Begin(Difficulty.Normal);
            ProfessorAI.I.Alert(PlayerController.I.transform.position, 70f / GameManager.I.normal.suspicionMultiplier, "teste");
            float max = 0f;
            for (float end = Time.realtimeSinceStartup + 2.5f; Time.realtimeSinceStartup < end;)
            {
                max = Mathf.Max(max, fx.TensionLevel);
                yield return null;
            }
            Assert.Greater(max, 0.1f, "a tensão não subiu com a suspeita");
            Assert.LessOrEqual(max, fx.mix.tensionMax + 1e-4f, "a tensão passou do teto");
        }

        [UnityTest]
        public IEnumerator JogarDeNovoRecomecaNaMesmaDificuldade()
        {
            yield return EnterGame();
            var gm = GameManager.I;
            gm.Begin(Difficulty.Dificil);
            yield return null;
            gm.DeliverExam();
            Assert.AreEqual(Phase.Ended, gm.Phase);

            gm.PlayAgain();
            yield return WaitUntil(() => GameManager.I != null && GameManager.I != gm && GameManager.I.Phase == Phase.Playing, 15f,
                                   "jogar de novo não começou a prova direto");
            Assert.AreEqual(Difficulty.Dificil, GameManager.I.CurrentDifficulty);
            Assert.AreEqual(GameManager.I.dificil.name, GameManager.I.Settings.name);
            Assert.AreEqual(0, GameManager.I.Flagrantes);
            Assert.AreEqual(0f, ExamSystem.I.Grade);

            // e o pedido não fica "grudado": voltar ao menu cai na introdução
            var gm2 = GameManager.I;
            gm2.Restart();
            yield return WaitUntil(() => GameManager.I != null && GameManager.I != gm2 && GameManager.I.Phase == Phase.Intro, 15f,
                                   "depois do jogar de novo, o menu não voltou a aparecer");
        }
    }
}
