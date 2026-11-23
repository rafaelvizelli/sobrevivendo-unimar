using System;
using UnityEngine;

namespace ProvaImpossivel
{
    /// <summary>Um barulho na sala: onde, até que distância se ouve, quanto chama atenção e o que foi.</summary>
    public readonly struct NoiseInfo
    {
        public readonly Vector3 position;
        public readonly float radius, amount;
        public readonly string what;
        /// <summary>Foi o jogador (ou alguém a pedido dele)? NPCs vigias só reagem a esses.</summary>
        public readonly bool byPlayer;
        /// <summary>
        /// S2: de que cola veio o barulho (Bilhete, CelularTocando...) ou Outra. Quem ouve continua sem saber QUEM foi;
        /// isto só diz o que o professor pode achar se for investigar o lugar.
        /// </summary>
        public readonly Infracao kind;

        public NoiseInfo(Vector3 position, float radius, float amount, string what, bool byPlayer, Infracao kind = Infracao.Outra)
        {
            this.position = position;
            this.radius = radius;
            this.amount = amount;
            this.what = what;
            this.byPlayer = byPlayer;
            this.kind = kind;
        }

        public bool HeardFrom(Vector3 listener)
        {
            Vector3 d = listener - position;
            d.y = 0f;
            return d.magnitude <= radius;
        }
    }

    /// <summary>
    /// Canal de ruídos da sala (Bloco 6). Quem faz barulho não precisa saber quem escuta: professor e Puxa-saco
    /// assinam em OnEnable e saem em OnDisable. Um evento C# estático basta para um jogo deste tamanho
    /// (ScriptableObject channels seriam peso sem ganho). Como o domínio não recarrega ao dar Play,
    /// os assinantes também são limpos no início de cada Play.
    /// </summary>
    public static class Noise
    {
        public static event Action<NoiseInfo> Emitted;

        public static void Emit(Vector3 position, float radius, float amount, string what, bool byPlayer, Infracao kind = Infracao.Outra) =>
            Emitted?.Invoke(new NoiseInfo(position, radius, amount, what, byPlayer, kind));

        /// <summary>Quantos sistemas estão ouvindo (testes de vazamento entre reinícios).</summary>
        public static int ListenerCount => Emitted?.GetInvocationList().Length ?? 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlay() => Emitted = null;
    }
}
