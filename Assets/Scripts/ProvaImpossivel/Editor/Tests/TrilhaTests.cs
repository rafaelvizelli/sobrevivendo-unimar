using NUnit.Framework;

namespace ProvaImpossivel.Tests
{
    /// <summary>S5 (Operação Steam): qual camada da trilha toca em cada situação, e o volume da música nas opções.</summary>
    public class TrilhaTests
    {
        [Test]
        public void ForaDaProvaSoACalmaMaisBaixa()
        {
            var t = MusicDirector.Targets(false, ProfState.Investigando, 1f, 0.5f);
            Assert.AreEqual(0.5f, t.calma);
            Assert.AreEqual(0f, t.desconfianca);
            Assert.AreEqual(0f, t.perseguicao, "no menu/pausa não tem perseguição, nem se ele estava investigando");
        }

        [Test]
        public void ACadaEstadoDoProfessorAMusicaMuda()
        {
            var calmo = MusicDirector.Targets(true, ProfState.Tranquilo, 0f, 0.5f);
            Assert.AreEqual((1f, 0f, 0f), calmo, "tranquilo e sem suspeita: só a calma");

            var desconfiado = MusicDirector.Targets(true, ProfState.Desconfiado, 0.35f, 0.5f);
            Assert.GreaterOrEqual(desconfiado.desconfianca, 0.8f, "desconfiado: entra o trêmulo");
            Assert.AreEqual(0f, desconfiado.perseguicao);

            var investigando = MusicDirector.Targets(true, ProfState.Investigando, 0.7f, 0.5f);
            Assert.AreEqual(1f, investigando.perseguicao, "investigando: entra o tambor");
            Assert.AreEqual(1f, investigando.desconfianca);

            var distraido = MusicDirector.Targets(true, ProfState.Distraido, 0.9f, 0.5f);
            Assert.LessOrEqual(distraido.desconfianca, 0.3f, "ao telefone, a música relaxa (é a sua janela)");
        }

        [Test]
        public void SuspeitaSobeADesconfiancaAosPoucos()
        {
            float antes = MusicDirector.Targets(true, ProfState.Tranquilo, 0.25f, 0.5f).desconfianca;
            float depois = MusicDirector.Targets(true, ProfState.Tranquilo, 0.5f, 0.5f).desconfianca;
            Assert.Greater(depois, antes);
        }

        [Test]
        public void VolumeDaMusicaFicaSalvoEDentroDosLimites()
        {
            float antes = GameOptions.MusicVolume;
            try
            {
                GameOptions.SetMusicVolume(2f);
                Assert.AreEqual(GameOptions.MaxVolume, GameOptions.MusicVolume, "acima do máximo é corrigido");
                GameOptions.SetMusicVolume(0.35f);
                GameOptions.ResetToDefaultsInMemoryOnly();
                Assert.AreEqual(GameOptions.DefaultMusicVolume, GameOptions.MusicVolume);
                GameOptions.Load(); // "abre o jogo de novo": lê do disco
                Assert.AreEqual(0.35f, GameOptions.MusicVolume, 1e-5f, "o volume da música não foi salvo");
            }
            finally { GameOptions.SetMusicVolume(antes); }
        }
    }
}
