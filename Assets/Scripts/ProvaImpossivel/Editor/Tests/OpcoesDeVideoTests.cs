using NUnit.Framework;
using UnityEngine;

namespace ProvaImpossivel.Tests
{
    /// <summary>S8 (Operação Steam): opções de vídeo (tela cheia, vsync, qualidade).</summary>
    public class OpcoesDeVideoTests
    {
        [Test]
        public void QualidadeForaDaFaixaViraAMaisAlta()
        {
            Assert.AreEqual(1, GameOptions.ClampQuality(-1, 2), "-1 = padrão = a mais alta");
            Assert.AreEqual(1, GameOptions.ClampQuality(7, 2));
            Assert.AreEqual(0, GameOptions.ClampQuality(0, 2));
            Assert.AreEqual(0, GameOptions.ClampQuality(3, 0), "sem níveis, 0");
        }

        [Test]
        public void NomesDaQualidade()
        {
            Assert.AreEqual("Leve", GameOptions.QualityLabel(0, 2));
            Assert.AreEqual("Bonito", GameOptions.QualityLabel(1, 2));
            Assert.AreEqual("Médio", GameOptions.QualityLabel(1, 3));
            Assert.AreEqual("Única", GameOptions.QualityLabel(0, 1));
        }

        [Test]
        public void OpcoesDeVideoFicamSalvasESemMexerNoProjetoNoEditor()
        {
            bool tela = GameOptions.Fullscreen, vsync = GameOptions.VSync;
            int q = GameOptions.Quality;
            int nivelDoProjeto = QualitySettings.GetQualityLevel();
            int vsyncDoProjeto = QualitySettings.vSyncCount;
            try
            {
                GameOptions.SetFullscreen(false);
                GameOptions.SetVSync(false);
                GameOptions.SetQuality(0);
                Assert.IsFalse(GameOptions.Fullscreen);
                Assert.IsFalse(GameOptions.VSync);
                Assert.AreEqual(0, GameOptions.Quality);
                // no editor as opções só são guardadas: aplicar mudaria o ProjectSettings do projeto
                Assert.AreEqual(nivelDoProjeto, QualitySettings.GetQualityLevel(), "a qualidade do projeto mudou no editor");
                Assert.AreEqual(vsyncDoProjeto, QualitySettings.vSyncCount, "o vsync do projeto mudou no editor");
            }
            finally
            {
                GameOptions.SetFullscreen(tela);
                GameOptions.SetVSync(vsync);
                if (q < 0) { PlayerPrefs.DeleteKey(GameOptions.KeyQuality); PlayerPrefs.Save(); }
                else GameOptions.SetQuality(q);
            }
        }
    }
}
