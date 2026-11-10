using UnityEngine;

namespace ProvaImpossivel
{
    /// <summary>
    /// Campo de visão com linha de visada real: paredes e móveis bloqueiam, e os COLEGAS também — mas só até o
    /// topo da cabeça deles (um aluno sentado esconde o peito de quem está atrás; um grandão esconde a cabeça).
    /// </summary>
    public static class Vision
    {
        static readonly RaycastHit[] hits = new RaycastHit[32];

        /// <summary>Personagens a menos disto (m, no plano) das pontas do raio são quem olha ou quem é olhado: não bloqueiam.</summary>
        public const float EndpointIgnoreRadius = 0.6f;

        public static bool LineOfSight(Vector3 from, Vector3 to)
        {
            Vector3 d = to - from;
            float len = d.magnitude;
            if (len < 0.01f) return true;
            int n = Physics.RaycastNonAlloc(from, d / len, hits, len - 0.05f, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var cv = hits[i].collider.GetComponentInParent<CharacterVisual>();
                if (cv != null && !CharacterBlocks(cv.transform.position, cv.HeadPosition.y, from, to)) continue;
                return false;
            }
            return true;
        }

        /// <summary>
        /// O raio <paramref name="from"/>→<paramref name="to"/> passa pelo colega (em pé em <paramref name="feet"/>,
        /// topo da cabeça em <paramref name="headTop"/>) abaixo da cabeça dele? O colisor do colega cobre a altura de
        /// alguém em pé mesmo quando ele está sentado, então a altura real decide. Pura (testável).
        /// </summary>
        public static bool CharacterBlocks(Vector3 feet, float headTop, Vector3 from, Vector3 to)
        {
            Vector2 a = new(from.x, from.z), b = new(to.x, to.z), c = new(feet.x, feet.z);
            if ((c - a).magnitude < EndpointIgnoreRadius || (c - b).magnitude < EndpointIgnoreRadius) return false;
            Vector2 ab = b - a;
            float t = ab.sqrMagnitude < 0.0001f ? 0f : Mathf.Clamp01(Vector2.Dot(c - a, ab) / ab.sqrMagnitude);
            float rayHeight = Mathf.Lerp(from.y, to.y, t);
            return rayHeight <= headTop;
        }

        /// <summary>
        /// 0 = não vê. Acima de 0 = vê, com peso maior quando perto e no centro do olhar.
        /// Testa cabeça e peito do alvo; basta um dos dois estar visível.
        /// </summary>
        public static float Visibility(Vector3 eye, Vector3 forward, float maxDistance, float halfFov, Vector3 head, Vector3 chest)
        {
            float best = 0f;
            best = Mathf.Max(best, Sample(eye, forward, maxDistance, halfFov, head));
            best = Mathf.Max(best, Sample(eye, forward, maxDistance, halfFov, chest));
            return best;
        }

        static float Sample(Vector3 eye, Vector3 forward, float maxDistance, float halfFov, Vector3 target)
        {
            Vector3 to = target - eye;
            float d = to.magnitude;
            if (d > maxDistance) return 0f;
            Vector3 flat = to; flat.y = 0f;
            Vector3 f = forward; f.y = 0f;
            float ang = flat.sqrMagnitude < 0.0001f ? 0f : Vector3.Angle(f, flat);
            if (ang > halfFov) return 0f;
            if (!LineOfSight(eye, target)) return 0f;
            return Weight(d, maxDistance, ang, halfFov);
        }

        /// <summary>
        /// Peso da visibilidade de um alvo já dentro do cone e sem obstáculo: maior perto e no centro do olhar.
        /// Separado para a simulação de equilíbrio usar a MESMA conta do jogo.
        /// </summary>
        public static float Weight(float distance, float maxDistance, float angle, float halfFov) =>
            Mathf.Lerp(1.4f, 0.35f, distance / maxDistance) * Mathf.Lerp(1f, 0.55f, angle / halfFov);
    }
}
