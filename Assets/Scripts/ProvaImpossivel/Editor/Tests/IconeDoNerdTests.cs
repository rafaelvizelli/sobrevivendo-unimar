using NUnit.Framework;

namespace ProvaImpossivel.Tests
{
    /// <summary>
    /// Bloco 15b: o ícone de humor (MoodIcon) tem um teto de tamanho na tela. Sentado ao lado do Nerd (carteiras coladas)
    /// o "O_O" ocupava mais de um terço da altura da tela.
    /// </summary>
    public class IconeDoNerdTests
    {
        const float Fov = 60f;                                     // câmera do jogador na cena
        static readonly float Altura = MoodIcon.SymbolWorldHeight(0.006f); // escala padrão do ícone

        static float NaTela(float dist) => MoodIcon.ScreenFraction(Altura * MoodIcon.DistanceFactor(dist, dist, Fov, Altura), dist, Fov);

        [Test]
        public void SemOTetoOIconeDePertoEraEnorme()
        {
            // a conta antiga (só 0,8 a 1,8×) na distância da carteira do Nerd: é o defeito que o teto corrige
            float antes = MoodIcon.ScreenFraction(Altura * 0.8f, 1.0f, Fov);
            Assert.Greater(antes, 0.3f);
        }

        [TestCase(0.5f)]
        [TestCase(1.0f)]
        [TestCase(1.35f)]
        [TestCase(2.5f)]
        public void DePertoNuncaPassaDoTeto(float dist)
        {
            Assert.LessOrEqual(NaTela(dist), MoodIcon.MaxScreenFraction + 1e-4f, "a " + dist + " m");
        }

        [TestCase(5f)]
        [TestCase(8f)]
        [TestCase(10f)]
        public void LongeNaoMudou(float dist)
        {
            // de 4,8 m para lá (o professor, em geral) o tamanho é o mesmo de antes do teto
            float antigo = UnityEngine.Mathf.Clamp(dist / 6f, 0.8f, 1.8f);
            Assert.AreEqual(antigo, MoodIcon.DistanceFactor(dist, dist, Fov, Altura), 1e-4f, "a " + dist + " m");
        }

        [Test]
        public void ContinuaLegivelDePerto()
        {
            // o teto não pode sumir com o ícone: perto, ele fica do tamanho do teto (não menor que o de longe)
            Assert.AreEqual(MoodIcon.MaxScreenFraction, NaTela(1.0f), 1e-4f);
            Assert.GreaterOrEqual(NaTela(1.0f), NaTela(8f) - 1e-4f);
        }
    }
}
