using NUnit.Framework;
using UnityEngine;

namespace ProvaImpossivel.Tests
{
    /// <summary>Colegas bloqueiam a visada, mas só até o topo da cabeça deles (Vision.CharacterBlocks).</summary>
    public class VisaoTests
    {
        static readonly Vector3 Olho = new(0f, 1.8f, -6f);

        [Test]
        public void ColegaNoCaminhoEscondeOQueEstaAbaixoDaCabecaDele()
        {
            var colega = new Vector3(0f, 0f, 0f);
            // olho a 1,8 m, alvo a 1,3 m: o raio passa pelo colega a ~1,42 m
            Assert.IsTrue(Vision.CharacterBlocks(colega, 1.45f, Olho, new Vector3(0f, 1.3f, 1.9f)), "cabeça de quem está sentado atrás");
            Assert.IsFalse(Vision.CharacterBlocks(colega, 1.35f, Olho, new Vector3(0f, 1.3f, 1.9f)), "colega mais baixo que o raio não esconde a cabeça");
            Assert.IsTrue(Vision.CharacterBlocks(colega, 1.4f, Olho, new Vector3(0f, 0.95f, 1.9f)), "peito de quem está sentado atrás");
        }

        [Test]
        public void ColegaBaixoNaoEscondeQuemEstaEmPe()
        {
            var colega = new Vector3(0f, 0f, 0f);
            Assert.IsFalse(Vision.CharacterBlocks(colega, 1.4f, Olho, new Vector3(0f, 1.7f, 1.0f)), "aluno sentado comum não cobre quem está em pé");
            Assert.IsTrue(Vision.CharacterBlocks(colega, 1.85f, Olho, new Vector3(0f, 1.7f, 1.0f)), "um grandão cobre");
        }

        [Test]
        public void QuemOlhaEQuemEOlhadoNaoBloqueiam()
        {
            // o próprio jogador (no fim do raio) e o próprio professor (no começo) não contam como obstáculo
            Assert.IsFalse(Vision.CharacterBlocks(new Vector3(0f, 0f, 1.9f), 2f, Olho, new Vector3(0f, 1.3f, 1.9f)), "o alvo");
            Assert.IsFalse(Vision.CharacterBlocks(new Vector3(0f, 0f, -6f), 2f, Olho, new Vector3(0f, 1.3f, 1.9f)), "quem olha");
        }

        [Test]
        public void ColegaForaDoTrechoNaoBloqueia()
        {
            // atrás do jogador (além do fim do raio): o ponto mais perto é o próprio alvo, que está acima da cabeça dele
            Assert.IsFalse(Vision.CharacterBlocks(new Vector3(0f, 0f, 4f), 1.2f, Olho, new Vector3(0f, 1.3f, 1.9f)));
        }
    }
}
