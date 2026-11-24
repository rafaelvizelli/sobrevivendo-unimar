using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ProvaImpossivel.Tests
{
    /// <summary>Bloco 14 — acabamento: limpeza do projeto (etapa 1).</summary>
    public class AcabamentoTests
    {
        // ------------------------------------------------------------ etapa 1: limpeza

        [Test]
        public void NomeDoJogoEEstudioProprios()
        {
            Assert.AreEqual("Sobrevivendo à UNIMAR", PlayerSettings.productName);
            Assert.AreNotEqual("DefaultCompany", PlayerSettings.companyName);
            Assert.IsFalse(string.IsNullOrWhiteSpace(PlayerSettings.companyName));
        }

        [Test]
        public void VersaoDeEntregaNoRodapeDoMenu()
        {
            Assert.AreEqual("1.0.2", PlayerSettings.bundleVersion, "polimento visual de 24/11");
            string r = Falas.MenuRodape("1.0.0");
            StringAssert.StartsWith("v1.0.0", r);
            StringAssert.Contains("Estúdio Fundão", r);
        }

        [Test]
        public void RitmoDaAnimacaoAcompanhaAPassadaEOTamanho()
        {
            // na velocidade da passada, o clipe toca em 1×; o professor (15% maior) dá passos mais lentos
            Assert.AreEqual(1f, CharacterModelSkin.PlaybackRate(1.3f, CharacterModelSkin.WalkStride, 1f, 0.5f, 2.2f), 1e-4f);
            float aluno = CharacterModelSkin.PlaybackRate(1.1f, CharacterModelSkin.WalkStride, 1f, 0.5f, 2.2f);
            float prof = CharacterModelSkin.PlaybackRate(1.1f, CharacterModelSkin.WalkStride, 1.15f, 0.5f, 2.2f);
            Assert.Less(prof, aluno, "maior = passos mais lentos na mesma velocidade");
            Assert.AreEqual(2.2f, CharacterModelSkin.PlaybackRate(99f, CharacterModelSkin.WalkStride, 1f, 0.5f, 2.2f), "limite");
        }

        [Test]
        public void CorridaSoEntraQuandoOJogadorCorre()
        {
            Assert.AreEqual(0f, CharacterModelSkin.RunBlend(2.2f), "andando (2,2 m/s) não corre");
            Assert.AreEqual(0f, CharacterModelSkin.RunBlend(2.6f), "o professor perseguindo (2,6 m/s) anda rápido, não corre");
            Assert.AreEqual(1f, CharacterModelSkin.RunBlend(4.2f), "correndo (4,2 m/s) é corrida inteira");
            var set = Resources.Load<CharacterModelSet>("Personagens/ModelosPersonagens");
            Assert.IsNotNull(set.run, "o clipe Run do pacote tem que estar ligado");
            Assert.AreEqual("Run", set.run.name);
        }

        [Test]
        public void OlhoDoNerdEntraComPopEFormaDeAmendoa()
        {
            Assert.AreEqual(0f, MoodIcon.PopIn(0f), 1e-4f);
            Assert.AreEqual(1f, MoodIcon.PopIn(1f), 1e-4f);
            float max = 0f;
            for (int i = 0; i <= 20; i++) max = Mathf.Max(max, MoodIcon.PopIn(i / 20f));
            Assert.Greater(max, 1f, "passa um pouco do tamanho (pop)");
            Assert.Less(max, 1.25f, "mas sem exagero");
            Assert.Less(MoodIcon.AlmondField(0f, 0f), 0f, "centro dentro do olho");
            Assert.Greater(MoodIcon.AlmondField(0.95f, 0.5f), 0f, "canto de cima fora (ponta da amêndoa)");
        }

        [Test]
        public void MensagemCinzaEhAMenosPrioritaria()
        {
            Assert.IsTrue(HUD.IsInformative(HUD.Gray, false), "cinza comum é só informação");
            Assert.IsFalse(HUD.IsInformative(HUD.Gray, true), "importante nunca é descartável");
            Assert.IsFalse(HUD.IsInformative(HUD.Red, false));
            Assert.IsFalse(HUD.IsInformative(HUD.Yellow, false));
        }

        [Test]
        public void AvisoDePularTutorialLegivelNoPapel()
        {
            Assert.IsTrue(ColorUtility.TryParseHtmlString("#3C372F", out var c));
            Assert.GreaterOrEqual(UITheme.Contrast(c, UITheme.Paper), 4.5f, "[T] pular tutorial precisa de contraste de texto");
        }

        [Test]
        public void PacotesSemUsoSairamDoManifest()
        {
            string manifest = File.ReadAllText(Path.Combine("Packages", "manifest.json"));
            StringAssert.DoesNotContain("com.unity.timeline", manifest);
            StringAssert.DoesNotContain("com.unity.visualscripting", manifest);
            StringAssert.DoesNotContain("com.unity.collab-proxy", manifest);
        }

        [Test]
        public void DistanciaNoPlanoIgnoraAAltura()
        {
            var a = new Vector3(1f, 0f, 2f);
            var b = new Vector3(4f, 7f, 6f);
            Assert.AreEqual(5f, Flat.Distance(a, b), 1e-5f, "3-4-5 no chão; a altura não conta");
            Assert.AreEqual(Flat.Distance(a, b), Flat.Distance(b, a), 1e-6f);
            Assert.AreEqual(0f, Flat.Distance(a, new Vector3(1f, -3f, 2f)), 1e-6f);
        }

        [Test]
        public void FalasEsquecemAUltimaFraseNoReset()
        {
            Falas.Reset();
            Assert.AreEqual(0, Falas.RememberedLists);
            Falas.Pick(Falas.ProfTranquilo);
            Falas.Pick(Falas.NerdGaba);
            Assert.AreEqual(2, Falas.RememberedLists);
            Falas.Reset();
            Assert.AreEqual(0, Falas.RememberedLists);
        }

        // ------------------------------------------------------------ etapa 2: resumo do fim de partida

        static DifficultySettings Normal() => new() { name = "Normal", duration = 360f, minGrade = 7f, maxFlagrantes = 3, favors = 3 };

        [Test]
        public void ResumoMontaOsNumerosDaPartida()
        {
            MatchLog.Reset();
            MatchLog.Celular = 2;
            MatchLog.NerdCopias = 1;
            MatchLog.FundaoColaBoa = 1;
            MatchLog.FundaoColaRuim = 2;
            var s = MatchSummary.Build(Normal(), LevelResult.Aprovado, 7f, 155f, 1, 1, 7, 2, 1);
            MatchLog.Reset();

            Assert.AreEqual(LevelResult.Aprovado, s.result);
            Assert.AreEqual(7f, s.grade);
            Assert.AreEqual(7f, s.minGrade);
            Assert.AreEqual(205f, s.timeUsed, 1e-4f, "tempo usado = duração - tempo restante");
            Assert.AreEqual(1, s.flagrantes);
            Assert.AreEqual(3, s.maxFlagrantes);
            Assert.AreEqual(2, s.colasCelular);
            Assert.AreEqual(1, s.colasNerd);
            Assert.AreEqual(3, s.colasFundao, "boa + furada");
            Assert.AreEqual(2, s.colasFundaoFuradas);
            Assert.AreEqual(2, s.favoresGastos, "3 por partida, sobrou 1");
            Assert.AreEqual((7, 2, 1), (s.certas, s.erradas, s.emBranco));
            Assert.AreEqual("Normal", s.difficulty);
        }

        [Test]
        public void ResumoNuncaFicaNegativo()
        {
            MatchLog.Reset();
            var s = MatchSummary.Build(Normal(), LevelResult.Reprovado, 0f, -3f, 0, 99, 0, 0, 10);
            Assert.AreEqual(360f, s.timeUsed, 1e-4f, "tempo restante negativo conta como zero");
            Assert.AreEqual(0, s.favoresGastos, "sobrar mais favores do que o máximo não dá gasto negativo");
            var t = MatchSummary.Build(Normal(), LevelResult.Reprovado, 0f, 500f, 0, 3, 0, 0, 10);
            Assert.AreEqual(0f, t.timeUsed, 1e-4f);
        }

        [Test]
        public void ResumoViraTextoNoFormatoDoJogo()
        {
            MatchLog.Reset();
            MatchLog.FundaoColaRuim = 1;
            var s = MatchSummary.Build(Normal(), LevelResult.Flagrado, 4f, 155f, 3, 3, 4, 3, 3);
            MatchLog.Reset();

            var prova = s.ExamLines;
            Assert.AreEqual(4, prova.Length);
            Assert.AreEqual((Falas.ResumoNota, "4,0"), prova[0]);
            Assert.AreEqual((Falas.ResumoMinima, "7,0"), prova[1]);
            Assert.AreEqual((Falas.ResumoQuestoes, "4 / 3 / 3"), prova[2]);
            Assert.AreEqual((Falas.ResumoTempo, "03:25"), prova[3]);

            var ficha = s.CrimeLines;
            Assert.AreEqual(6, ficha.Length);
            Assert.AreEqual("3 de 3", ficha[0].value);
            Assert.AreEqual("0", ficha[1].value);
            Assert.AreEqual("0", ficha[2].value);
            Assert.AreEqual("1 (1 " + Falas.ResumoFundaoFurada + ")", ficha[3].value);
            Assert.AreEqual((Falas.ResumoColasDeBolso, "0 / 0 / 0 / 0"), ficha[4], "colas de bolso (Bloco 16)");
            Assert.AreEqual("0", ficha[5].value, "restaram os 3 favores: nenhum gasto");
        }

        [Test]
        public void ResumoMostraAsColasDeBolso()
        {
            MatchLog.Reset();
            MatchLog.ColinhaUsos = 2;
            MatchLog.GarrafaUsos = 1;
            MatchLog.BilheteUsos = 3;
            MatchLog.FoneUsos = 1;
            var s = MatchSummary.Build(Normal(), LevelResult.Aprovado, 7f, 100f, 0, 3, 7, 0, 3);
            MatchLog.Reset();
            Assert.AreEqual("2 / 1 / 3 / 1", s.CrimeLines[4].value, "colinha / garrafinha / bilhete / fone");
        }

        // ------------------------------------------------------------ etapa 3: opções (PlayerPrefs)

        static readonly string[] OptionKeys = { GameOptions.KeyMasterVolume, GameOptions.KeySfxVolume, GameOptions.KeyCameraSensitivity };

        /// <summary>Roda <paramref name="body"/> sem estragar as opções reais de quem testa no editor.</summary>
        static void WithCleanOptions(System.Action body)
        {
            var saved = new float?[OptionKeys.Length];
            for (int i = 0; i < OptionKeys.Length; i++)
                saved[i] = PlayerPrefs.HasKey(OptionKeys[i]) ? PlayerPrefs.GetFloat(OptionKeys[i]) : null;
            float listener = AudioListener.volume;
            try
            {
                GameOptions.ResetToDefaults();
                body();
            }
            finally
            {
                for (int i = 0; i < OptionKeys.Length; i++)
                    if (saved[i].HasValue) PlayerPrefs.SetFloat(OptionKeys[i], saved[i].Value);
                    else PlayerPrefs.DeleteKey(OptionKeys[i]);
                PlayerPrefs.Save();
                GameOptions.Load();
                AudioListener.volume = listener;
            }
        }

        [Test]
        public void OpcoesComecamNosPadroes() => WithCleanOptions(() =>
        {
            Assert.AreEqual(GameOptions.DefaultMasterVolume, GameOptions.MasterVolume);
            Assert.AreEqual(GameOptions.DefaultSfxVolume, GameOptions.SfxVolume);
            Assert.AreEqual(GameOptions.DefaultCameraSensitivity, GameOptions.CameraSensitivity);
        });

        [Test]
        public void OpcoesSalvamELeemDeVolta() => WithCleanOptions(() =>
        {
            GameOptions.SetMasterVolume(0.4f);
            GameOptions.SetSfxVolume(0.7f);
            GameOptions.SetCameraSensitivity(1.8f);
            Assert.AreEqual(0.4f, PlayerPrefs.GetFloat(GameOptions.KeyMasterVolume), 1e-6f, "salvou no disco");
            Assert.AreEqual(0.4f, AudioListener.volume, 1e-6f, "volume geral aplicado na hora");

            // simula abrir o jogo de novo: zera a memória e lê do disco
            GameOptions.ResetToDefaultsInMemoryOnly();
            Assert.AreEqual(GameOptions.DefaultSfxVolume, GameOptions.SfxVolume);
            GameOptions.Load();
            Assert.AreEqual(0.4f, GameOptions.MasterVolume, 1e-6f);
            Assert.AreEqual(0.7f, GameOptions.SfxVolume, 1e-6f);
            Assert.AreEqual(1.8f, GameOptions.CameraSensitivity, 1e-6f);
        });

        [Test]
        public void OpcoesRespeitamMinimoEMaximo() => WithCleanOptions(() =>
        {
            GameOptions.SetMasterVolume(-1f);
            GameOptions.SetSfxVolume(5f);
            GameOptions.SetCameraSensitivity(0f);
            Assert.AreEqual(GameOptions.MinVolume, GameOptions.MasterVolume);
            Assert.AreEqual(GameOptions.MaxVolume, GameOptions.SfxVolume);
            Assert.AreEqual(GameOptions.MinCameraSensitivity, GameOptions.CameraSensitivity);
            GameOptions.SetCameraSensitivity(99f);
            Assert.AreEqual(GameOptions.MaxCameraSensitivity, GameOptions.CameraSensitivity);

            // valor estragado no disco (editado à mão) também é corrigido ao ler
            PlayerPrefs.SetFloat(GameOptions.KeyMasterVolume, 7f);
            PlayerPrefs.SetFloat(GameOptions.KeyCameraSensitivity, -2f);
            GameOptions.Load();
            Assert.AreEqual(GameOptions.MaxVolume, GameOptions.MasterVolume);
            Assert.AreEqual(GameOptions.MinCameraSensitivity, GameOptions.CameraSensitivity);
            Assert.AreEqual(GameOptions.DefaultCameraSensitivity, GameOptions.ClampSensitivity(float.NaN));
        });

        [Test]
        public void ChavesDasOpcoesTemOPrefixoDoJogo()
        {
            foreach (var k in OptionKeys) StringAssert.StartsWith(GameOptions.Prefix, k);
            Assert.AreEqual(OptionKeys.Length, new System.Collections.Generic.HashSet<string>(OptionKeys).Count, "chaves repetidas");
            Assert.LessOrEqual(GameOptions.MinCameraSensitivity, GameOptions.DefaultCameraSensitivity);
            Assert.GreaterOrEqual(GameOptions.MaxCameraSensitivity, GameOptions.DefaultCameraSensitivity);
        }

        // ------------------------------------------------------------ etapa 4: áudio ambiente

        [Test]
        public void AmbienteSoNaProvaEAbaixaParaSonsImportantesETensao()
        {
            var m = new AudioMix();
            Assert.AreEqual(0f, m.AmbientGain(false, false, 0f), "fora da prova (menu, pausa, fim): silêncio");
            Assert.AreEqual(1f, m.AmbientGain(true, false, 0f));
            Assert.AreEqual(m.duckTo, m.AmbientGain(true, true, 0f), 1e-6f, "som importante abaixa o fundo");
            Assert.AreEqual(1f - m.tensionDucksAmbient, m.AmbientGain(true, false, 1f), 1e-6f, "tensão no máximo abaixa o fundo");
            Assert.AreEqual(m.AmbientGain(true, false, 1f), m.AmbientGain(true, false, 5f), 1e-6f, "tensão acima do máximo não passa do limite");
            Assert.Less(m.duckTo, 0.5f);
            Assert.IsTrue(AudioMix.IsImportant(Sfx.Ouviu));
            Assert.IsTrue(AudioMix.IsImportant(Sfx.Flagrante));
            Assert.IsFalse(AudioMix.IsImportant(Sfx.Clique));
        }

        [Test]
        public void ClipsDeAmbienteNaoEstouram()
        {
            foreach (var clip in new[] { SoundFX.MurmurClip(), SoundFX.ClockClip(), SoundFX.PencilClip() })
            {
                var d = new float[clip.samples];
                clip.GetData(d, 0);
                float peak = 0f;
                foreach (float x in d) peak = Mathf.Max(peak, Mathf.Abs(x));
                Assert.Greater(peak, 0.1f, clip.name + " está mudo");
                Assert.LessOrEqual(peak, SoundFX.AmbientClipPeak + 1e-4f, clip.name + " passou do pico combinado");
                Object.DestroyImmediate(clip);
            }
        }

        [Test]
        public void MurmurioFazLoopSemEstalo()
        {
            var clip = SoundFX.MurmurClip();
            var d = new float[clip.samples];
            clip.GetData(d, 0);
            Object.DestroyImmediate(clip);
            // o fim emenda no começo: o salto do último para o primeiro sample é do tamanho de um passo normal do sinal
            float maxStep = 0f;
            for (int i = 1; i < d.Length; i++) maxStep = Mathf.Max(maxStep, Mathf.Abs(d[i] - d[i - 1]));
            Assert.LessOrEqual(Mathf.Abs(d[0] - d[d.Length - 1]), maxStep + 1e-4f);
        }

        [Test]
        public void TituloDoResultadoPorTipo()
        {
            Assert.AreEqual(Falas.TituloAprovado, MatchSummary.Title(LevelResult.Aprovado));
            Assert.AreEqual(Falas.TituloSufoco, MatchSummary.Title(LevelResult.AprovadoNoSufoco));
            Assert.AreEqual(Falas.TituloReprovado, MatchSummary.Title(LevelResult.Reprovado));
            Assert.AreEqual(Falas.TituloExpulso, MatchSummary.Title(LevelResult.Flagrado));
        }
    }
}
