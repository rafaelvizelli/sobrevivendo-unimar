using UnityEngine;

namespace ProvaImpossivel
{
    /// <summary>Contas no plano do chão (ignorando a altura), usadas por professor e alunos.</summary>
    public static class Flat
    {
        /// <summary>Distância entre <paramref name="a"/> e <paramref name="b"/> ignorando o eixo Y.</summary>
        public static float Distance(Vector3 a, Vector3 b)
        {
            a.y = b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
