using System;
using UnityEngine;

namespace ProvaImpossivel
{
    /// <summary>Sons de fundo da sala (Bloco 14), gerados por código como o resto do áudio.</summary>
    public enum Ambiente { Murmurio, Relogio, Lapis, Cadeira }

    /// <summary>
    /// Volumes RELATIVOS do áudio de fundo e da tensão, num lugar só (Inspector do SoundFX, campo "mix").
    /// Os valores são frações do volume geral (que é aplicado por cima, no AudioListener).
    /// Regra: o fundo fica sempre bem abaixo dos sons de jogo ("hm?", passos 3D, flagrante) e ABAIXA quando eles tocam.
    /// </summary>
    [Serializable]
    public class AudioMix
    {
        [Header("Ambiente (sempre baixo)")]
        [Tooltip("Murmúrio da turma (loop)")] [Range(0, 0.3f)] public float murmur = 0.05f;
        [Tooltip("Tique-taque do relógio (loop)")] [Range(0, 0.3f)] public float clock = 0.035f;
        [Tooltip("Lápis riscando (de vez em quando)")] [Range(0, 0.3f)] public float pencil = 0.04f;
        [Tooltip("Cadeira arrastando longe (de vez em quando)")] [Range(0, 0.3f)] public float chair = 0.05f;
        [Tooltip("Segundos entre um lápis riscando e outro")] public Vector2 pencilInterval = new(4f, 11f);
        [Tooltip("Segundos entre uma cadeira arrastando e outra")] public Vector2 chairInterval = new(14f, 30f);

        [Header("Tensão (o batimento que já existia)")]
        [Tooltip("Volume máximo do batimento (fica baixo de propósito para não irritar)")] [Range(0, 1)] public float tensionMax = 0.35f;
        [Tooltip("Com a tensão no máximo, o ambiente cai esta fração (a sala 'some' quando você fica nervoso)")]
        [Range(0, 1)] public float tensionDucksAmbient = 0.6f;

        [Header("Prioridade dos sons de jogo")]
        [Tooltip("Enquanto toca um som importante, o ambiente fica nesta fração")] [Range(0, 1)] public float duckTo = 0.2f;
        [Tooltip("Segundos de ambiente abaixado depois de um som importante")] public float duckSeconds = 1.2f;
        [Tooltip("Velocidade (volume por segundo) com que o ambiente sobe ou desce")] public float fadeSpeed = 0.25f;

        /// <summary>Sons de jogo que o ambiente nunca pode tapar: quando tocam, o fundo abaixa.</summary>
        public static bool IsImportant(Sfx s) => s == Sfx.Ouviu || s == Sfx.Flagrante || s == Sfx.Alerta || s == Sfx.Erro || s == Sfx.Sinal;

        /// <summary>
        /// Multiplicador do ambiente (0 a 1): zero fora da prova (menu, pausa, fim), abaixado por som importante
        /// e pela tensão (<paramref name="tension01"/> = batimento atual / máximo).
        /// </summary>
        public float AmbientGain(bool playing, bool ducked, float tension01)
        {
            if (!playing) return 0f;
            float g = ducked ? duckTo : 1f;
            return g * (1f - tensionDucksAmbient * Mathf.Clamp01(tension01));
        }

        public float Level(Ambiente a) => a switch
        {
            Ambiente.Murmurio => murmur,
            Ambiente.Relogio => clock,
            Ambiente.Lapis => pencil,
            _ => chair,
        };
    }
}
