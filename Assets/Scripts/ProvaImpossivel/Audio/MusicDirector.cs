using System;
using UnityEngine;

namespace ProvaImpossivel
{
    /// <summary>
    /// S5 (Operação Steam): trilha adaptativa em 3 camadas que tocam juntas, em sincronia, e entram e saem pelo estado do
    /// professor: CALMA (baixo pizzicato "na ponta dos pés" + tique), DESCONFIANÇA (acordes em trêmulo) e PERSEGUIÇÃO
    /// (tambor e baixo dobrado). Mais dois estingers: o "uá-uá-uá-uáaa" de trombone no flagrante e o arpejo de alívio na
    /// aprovação. Dá para OUVIR que ele está investigando sem olhar.
    /// PROVISÓRIA: sintetizada em código, no mesmo padrão do <see cref="SoundFX"/>, até existir trilha composta (dívida
    /// registrada em NOTAS e no ROADMAP). Trocar = pôr clips de verdade em <see cref="layerClips"/> e nos estingers.
    /// </summary>
    public class MusicDirector : MonoBehaviour
    {
        public static MusicDirector I { get; private set; }

        [Tooltip("Volume máximo da trilha (abaixo dos sons de jogo: os passos e o 'hm?' são informação)")]
        [Range(0, 1)] public float musicMax = 0.22f;
        [Tooltip("Volume das camadas por segundo ao entrar/sair")] public float fadeSpeed = 0.6f;
        [Tooltip("No menu e na pausa a trilha fica nesta fração")] [Range(0, 1)] public float menuLevel = 0.5f;
        [Tooltip("Batidas por minuto")] public float bpm = 92f;

        const int Rate = 44100;
        AudioSource[] layers;
        AudioClip[] layerClips;
        AudioSource stinger;
        AudioClip flagranteClip, aprovadoClip;
        float[] gains = new float[3];
        ProfState lastProf;

        /// <summary>Volume atual de cada camada, 0 a 1 antes do volume geral (testes).</summary>
        public float Layer(int i) => i >= 0 && i < gains.Length ? gains[i] : 0f;
        public int StingerCount { get; private set; }

        /// <summary>
        /// Quanto de cada camada (calma, desconfiança, perseguição) deve tocar agora. Pura (testável).
        /// Fora da prova: só a calma, mais baixa. Na prova, a desconfiança sobe com a suspeita, a perseguição entra
        /// investigando ou no flagrante. Distraído (ao telefone etc.), a música relaxa.
        /// </summary>
        public static (float calma, float desconfianca, float perseguicao) Targets(bool playing, ProfState state, float suspicion01, float menuLevel)
        {
            if (!playing) return (menuLevel, 0f, 0f);
            float desconf = Mathf.Clamp01(Mathf.InverseLerp(0.2f, 0.6f, suspicion01));
            float perseg = state == ProfState.Investigando || state == ProfState.Flagrante ? 1f : 0f;
            if (state == ProfState.Desconfiado) desconf = Mathf.Max(desconf, 0.8f);
            if (state == ProfState.Distraido) desconf = Mathf.Min(desconf, 0.3f);
            if (perseg > 0f) desconf = 1f;
            return (1f, desconf, perseg);
        }

        void Awake()
        {
            I = this;
            layerClips = new[] { Calma(), Desconfianca(), Perseguicao() };
            layers = new AudioSource[3];
            double start = AudioSettings.dspTime + 0.2;
            for (int i = 0; i < 3; i++)
            {
                var s = gameObject.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.spatialBlend = 0f;
                s.loop = true;
                s.clip = layerClips[i];
                s.volume = 0f;
                s.ignoreListenerPause = true;
                s.PlayScheduled(start); // as três começam no mesmo instante do relógio de áudio: ficam em sincronia
                layers[i] = s;
            }
            stinger = gameObject.AddComponent<AudioSource>();
            stinger.playOnAwake = false;
            stinger.spatialBlend = 0f;
            flagranteClip = TromboneTriste();
            aprovadoClip = Alivio();
        }

        bool endHandled;

        void Update()
        {
            var gm = GameManager.I;
            var prof = ProfessorAI.I;
            bool playing = gm != null && gm.IsPlaying;
            var state = prof != null ? prof.State : ProfState.Tranquilo;
            if (playing && state == ProfState.Flagrante && lastProf != ProfState.Flagrante) PlayStinger(flagranteClip);
            // fim da prova: o resumo é montado logo depois da troca de fase, então confere no quadro seguinte
            if (gm != null && gm.Phase == Phase.Ended && !endHandled)
            {
                endHandled = true;
                var r = gm.LastSummary.result;
                if (!gm.InTutorial && (r == LevelResult.Aprovado || r == LevelResult.AprovadoNoSufoco)) PlayStinger(aprovadoClip);
            }
            lastProf = state;

            var (a, b, c) = Targets(playing, state, prof != null ? prof.Suspicion / 100f : 0f, menuLevel);
            // no flagrante e logo depois do estinger, as camadas saem do caminho
            bool stingerOn = stinger.isPlaying;
            float master = musicMax * GameOptions.MusicVolume;
            Fade(0, stingerOn ? 0.2f : a, master);
            Fade(1, stingerOn ? 0f : b, master);
            Fade(2, stingerOn ? 0f : c, master);
            stinger.volume = master * 1.6f;
        }

        void Fade(int i, float want, float master)
        {
            gains[i] = Mathf.MoveTowards(gains[i], want, Time.unscaledDeltaTime * fadeSpeed);
            layers[i].volume = gains[i] * master;
        }

        void PlayStinger(AudioClip clip)
        {
            stinger.Stop();
            stinger.clip = clip;
            stinger.Play();
            StingerCount++;
        }

        // ================================================================== síntese (provisória)

        float Beat => 60f / bpm;
        int LoopSamples => Mathf.RoundToInt(16 * Beat * Rate); // 4 compassos de 4

        static AudioClip Clip(string name, float[] d)
        {
            var c = AudioClip.Create("musica_" + name, d.Length, 1, Rate, false);
            c.SetData(d, 0);
            return c;
        }

        // Ré menor "na ponta dos pés": D F A F | D F G# A | Bb D F D | A C# E A (baixo, oitava 1–2)
        static readonly float[] BaixoNotas = { 73.42f, 87.31f, 110f, 87.31f, 73.42f, 87.31f, 103.83f, 110f, 58.27f, 73.42f, 87.31f, 73.42f, 55f, 69.30f, 82.41f, 55f };
        // acordes por compasso: Dm, Dm, Bb, A
        static readonly float[][] Acordes =
        {
            new[] { 146.83f, 174.61f, 220f }, new[] { 146.83f, 174.61f, 220f }, new[] { 116.54f, 146.83f, 174.61f }, new[] { 110f, 138.59f, 164.81f },
        };

        /// <summary>Corda beliscada: seno com harmônicos e queda rápida (pizzicato).</summary>
        static void Pizz(float[] d, int at, float f, float amp, float len)
        {
            int n = (int)(len * Rate);
            for (int i = 0; i < n && at + i < d.Length; i++)
            {
                float t = (float)i / Rate;
                float w = Mathf.Sin(2f * Mathf.PI * f * t) + 0.5f * Mathf.Sin(4f * Mathf.PI * f * t) + 0.2f * Mathf.Sin(6f * Mathf.PI * f * t);
                float env = Mathf.Clamp01(t / 0.004f) * Mathf.Exp(-t * 9f);
                d[at + i] += w * env * amp;
            }
        }

        /// <summary>Tique seco (ruído curtíssimo), o "relógio" da calma.</summary>
        static void Tick(float[] d, int at, float amp, System.Random rng)
        {
            int n = (int)(0.025f * Rate);
            for (int i = 0; i < n && at + i < d.Length; i++)
                d[at + i] += (float)(rng.NextDouble() * 2 - 1) * amp * (1f - (float)i / n);
        }

        AudioClip Calma()
        {
            var d = new float[LoopSamples];
            var rng = new System.Random(3);
            int beat = Mathf.RoundToInt(Beat * Rate);
            for (int b = 0; b < 16; b++)
            {
                Pizz(d, b * beat, BaixoNotas[b], 0.32f, 0.5f);
                Tick(d, b * beat + beat / 2, 0.06f, rng); // contratempo
            }
            return Clip("calma", d);
        }

        AudioClip Desconfianca()
        {
            var d = new float[LoopSamples];
            int bar = Mathf.RoundToInt(4 * Beat * Rate);
            for (int c = 0; c < 4; c++)
                for (int i = 0; i < bar; i++)
                {
                    float t = (float)i / Rate;
                    float s = 0f;
                    foreach (float f in Acordes[c])
                        s += Mathf.Sin(2f * Mathf.PI * f * t) + 0.3f * Mathf.Sin(4f * Mathf.PI * f * t * 1.003f);
                    float trem = 0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * 7f * t); // trêmulo de cordas
                    float env = Mathf.Clamp01(t / 0.15f) * Mathf.Clamp01((bar - i) / (0.1f * Rate));
                    d[c * bar + i] += s * trem * env * 0.07f;
                }
            return Clip("desconfianca", d);
        }

        AudioClip Perseguicao()
        {
            var d = new float[LoopSamples];
            var rng = new System.Random(5);
            int beat = Mathf.RoundToInt(Beat * Rate);
            for (int b = 0; b < 16; b++)
            {
                // tambor grave em toda batida, mais forte no 1 de cada compasso
                int n = (int)(0.18f * Rate);
                float amp = b % 4 == 0 ? 0.55f : 0.38f;
                float ph = 0f;
                for (int i = 0; i < n && b * beat + i < d.Length; i++)
                {
                    float t = (float)i / Rate;
                    ph += Mathf.Lerp(130f, 55f, t / 0.18f) / Rate;
                    d[b * beat + i] += Mathf.Sin(2f * Mathf.PI * ph) * Mathf.Exp(-t * 18f) * amp;
                }
                // chimbal em colcheias e o baixo uma oitava acima, apressado
                Tick(d, b * beat, 0.08f, rng);
                Tick(d, b * beat + beat / 2, 0.05f, rng);
                Pizz(d, b * beat + beat / 2, BaixoNotas[b] * 2f, 0.18f, 0.25f);
            }
            return Clip("perseguicao", d);
        }

        /// <summary>"uá-uá-uá-uáaa": trombone triste descendo (Si♭, Lá, Lá♭, Sol), o último com vibrato.</summary>
        static AudioClip TromboneTriste()
        {
            float[] notas = { 233.08f, 220f, 207.65f, 196f };
            float[] durs = { 0.32f, 0.32f, 0.32f, 1.1f };
            float total = 0f;
            foreach (float x in durs) total += x;
            var d = new float[(int)(total * Rate)];
            int at = 0;
            float ph = 0f;
            for (int k = 0; k < 4; k++)
            {
                int n = (int)(durs[k] * Rate);
                for (int i = 0; i < n && at + i < d.Length; i++)
                {
                    float t = (float)i / Rate;
                    float vib = k == 3 ? 1f + 0.025f * Mathf.Sin(2f * Mathf.PI * 5.5f * t) * Mathf.Clamp01(t / 0.3f) : 1f;
                    ph += notas[k] * vib / Rate;
                    float w = 0f;
                    for (int h = 1; h <= 6; h++) w += Mathf.Sin(2f * Mathf.PI * ph * h) / h; // dente de serra suave: metal
                    // "uá": cada nota abre (ataque lento) e fecha (o surdina do trombone)
                    float env = Mathf.Clamp01(t / 0.06f) * Mathf.Clamp01((n - i) / (0.05f * Rate));
                    float wah = k == 3 ? 0.7f + 0.3f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / durs[k])) : 0.6f + 0.4f * Mathf.Sin(Mathf.PI * t / durs[k]);
                    d[at + i] = w * env * wah * 0.32f;
                }
                at += n;
            }
            return Clip("flagrante", d);
        }

        /// <summary>Alívio: arpejo de Ré maior subindo (a menor da trilha "resolve") e o acorde soando.</summary>
        static AudioClip Alivio()
        {
            float[] notas = { 146.83f, 185f, 220f, 293.66f };
            var d = new float[(int)(1.6f * Rate)];
            for (int k = 0; k < notas.Length; k++) Pizz(d, (int)(k * 0.09f * Rate), notas[k] * 2f, 0.3f, 1.2f);
            return Clip("aprovado", d);
        }
    }
}
