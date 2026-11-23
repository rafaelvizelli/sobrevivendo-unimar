using UnityEngine;

namespace ProvaImpossivel
{
    /// <summary>
    /// S4 (Operação Steam): sobrancelhas que atuam. O modelo 3D perdeu as expressões do boneco de primitivas; no
    /// professor, duas sobrancelhas grossas fazem o trabalho do rosto inteiro (é o que se lê de longe): uma sobe
    /// desconfiada, as duas descem bravas, sobem assustadas ao telefone. Só visual: lê <see cref="CharacterVisual.expression"/>.
    /// </summary>
    public class ExpressiveBrows : MonoBehaviour
    {
        /// <summary>
        /// Pose das sobrancelhas para uma expressão: deslocamento vertical (m, modelo em escala 1) e inclinação (graus;
        /// positivo = ponta de DENTRO para baixo, cara de bravo; negativo = ponta de dentro para cima, cara de aflito)
        /// de cada lado. Pura (testável).
        /// </summary>
        public readonly struct Pose
        {
            public readonly float leftUp, leftTilt, rightUp, rightTilt;
            public Pose(float leftUp, float leftTilt, float rightUp, float rightTilt)
            {
                this.leftUp = leftUp; this.leftTilt = leftTilt; this.rightUp = rightUp; this.rightTilt = rightTilt;
            }
        }

        public static Pose PoseFor(Expression e) => e switch
        {
            // uma lá em cima, a outra apertando o olho: "hmmm, sei..."
            Expression.Desconfiado => new Pose(-0.006f, 14f, 0.016f, -8f),
            Expression.Bravo => new Pose(-0.012f, 24f, -0.012f, 24f),
            Expression.Assustado => new Pose(0.018f, -14f, 0.018f, -14f),
            Expression.Nervoso => new Pose(0.01f, -16f, 0.01f, -16f),
            Expression.Feliz or Expression.Convencido => new Pose(0.012f, -4f, 0.016f, -6f),
            Expression.Dormindo => new Pose(-0.006f, 0f, -0.006f, 0f),
            _ => new Pose(0f, 0f, 0f, 0f),
        };

        [Tooltip("Velocidade com que as sobrancelhas mudam de pose (por segundo)")] public float speed = 12f;
        [Tooltip("Desconfiado: a sobrancelha levantada sobe e desce (m)")] public float suspiciousBounce = 0.003f;

        CharacterVisual visual;
        Transform left, right;
        Vector3 leftBase, rightBase, upLocal;
        Quaternion leftRot, rightRot;
        float lUp, lTilt, rUp, rTilt;

        public Transform Left => left;
        public Transform Right => right;

        /// <param name="upPerMeter">Vetor, no espaço da cabeça, de 1 m "para cima" do personagem (já com a escala do modelo).</param>
        public void Init(CharacterVisual v, Transform leftBrow, Transform rightBrow, Vector3 upPerMeter)
        {
            visual = v;
            left = leftBrow;
            right = rightBrow;
            leftBase = left.localPosition;
            rightBase = right.localPosition;
            leftRot = left.localRotation;
            rightRot = right.localRotation;
            upLocal = upPerMeter;
        }

        void LateUpdate()
        {
            if (visual == null || left == null || right == null) return;
            var p = PoseFor(visual.expression);
            float k = 1f - Mathf.Exp(-speed * Time.deltaTime);
            float bounce = visual.expression == Expression.Desconfiado ? Mathf.Sin(Time.time * 7f) * suspiciousBounce : 0f;
            lUp = Mathf.Lerp(lUp, p.leftUp, k);
            lTilt = Mathf.Lerp(lTilt, p.leftTilt, k);
            rUp = Mathf.Lerp(rUp, p.rightUp + bounce, k);
            rTilt = Mathf.Lerp(rTilt, p.rightTilt, k);
            // o comprimento da sobrancelha é o eixo x dela (direita do personagem). Girar +a no eixo z dela levanta a
            // ponta de fora (+x): na direita isso abaixa a ponta de dentro; na esquerda a ponta de dentro é a +x.
            left.localPosition = leftBase + upLocal * lUp;
            right.localPosition = rightBase + upLocal * rUp;
            left.localRotation = leftRot * Quaternion.AngleAxis(-lTilt, Vector3.forward);
            right.localRotation = rightRot * Quaternion.AngleAxis(rTilt, Vector3.forward);
        }
    }
}
