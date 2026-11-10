using System.Collections.Generic;
using UnityEngine;

namespace ProvaImpossivel
{
    public enum Sfx { Ponto, PerdePonto, Flagrante, Evento, Alerta, Clique, Acerto, Erro, Sinal, Passo, Cadeira, Ouviu, Coracao }

    /// <summary>
    /// Efeitos sonoros sintetizados na hora (sem depender de arquivos de áudio).
    /// Além dos efeitos de interface (2D), o áudio faz parte da furtividade (Bloco 5):
    /// - sons POSICIONAIS (3D) para passos, cadeira e o "hm?" do professor: dá para saber onde ele está sem olhar;
    /// - camada de TENSÃO (batimento) que só aparece com suspeita alta e acelera com ela.
    /// Quando existirem arquivos de áudio de verdade, basta trocar o clip de cada Sfx em <see cref="clips"/>;
    /// o resto do jogo continua chamando Play/PlayAt igual.
    /// </summary>
    public class SoundFX : MonoBehaviour
    {
        public static SoundFX I { get; private set; }
        [Range(0, 1)] public float volume = 0.5f;
        [Tooltip("Volume máximo do batimento de tensão (fica baixo de propósito para não irritar)")]
        [Range(0, 1)] public float tensionVolume = 0.35f;
        [Tooltip("Distância máxima em que um som 3D ainda é ouvido (m)")] public float maxHearDistance = 16f;

        AudioSource source, tension;
        AudioSource[] pool;
        int poolNext;
        readonly Dictionary<Sfx, AudioClip> clips = new();
        // contagem de sons tocados por tipo (diagnóstico e testes automatizados)
        static readonly Dictionary<Sfx, int> counts = new();

        void Awake()
        {
            I = this;
            counts.Clear(); // o domínio não recarrega entre Plays: zera junto com a cena
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;

            clips[Sfx.Ponto] = Tones("ponto", new[] { 660f, 880f, 1320f }, 0.08f, 0.5f);
            clips[Sfx.Acerto] = Tones("acerto", new[] { 784f, 1046f }, 0.1f, 0.5f);
            clips[Sfx.PerdePonto] = Tones("perde", new[] { 440f, 330f, 220f }, 0.12f, 0.5f);
            clips[Sfx.Erro] = Tones("erro", new[] { 196f, 147f }, 0.16f, 0.6f, square: true);
            clips[Sfx.Flagrante] = Tones("flagra", new[] { 220f, 0f, 220f, 0f, 220f }, 0.11f, 0.6f, square: true);
            clips[Sfx.Evento] = Tones("evento", new[] { 523f, 659f }, 0.07f, 0.4f);
            clips[Sfx.Alerta] = Tones("alerta", new[] { 988f, 0f, 988f }, 0.06f, 0.35f);
            clips[Sfx.Clique] = Tones("clique", new[] { 1200f }, 0.03f, 0.3f);
            clips[Sfx.Sinal] = Tones("sinal", new[] { 880f, 880f, 880f, 880f }, 0.18f, 0.4f, square: true);
            clips[Sfx.Passo] = Footstep();
            clips[Sfx.Cadeira] = ChairScrape();
            clips[Sfx.Ouviu] = Hmm();
            clips[Sfx.Coracao] = Heartbeat();

            // fontes 3D reaproveitadas (vários passos/ruídos ao mesmo tempo sem criar objetos durante o jogo)
            pool = new AudioSource[8];
            for (int i = 0; i < pool.Length; i++)
            {
                var go = new GameObject("Som 3D " + i);
                go.transform.SetParent(transform, false);
                var s = go.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.spatialBlend = 1f;
                s.rolloffMode = AudioRolloffMode.Linear;
                s.minDistance = 1.5f;
                s.maxDistance = maxHearDistance;
                s.dopplerLevel = 0f;
                pool[i] = s;
            }

            tension = gameObject.AddComponent<AudioSource>();
            tension.playOnAwake = false;
            tension.spatialBlend = 0f;
            tension.loop = true;
            tension.clip = clips[Sfx.Coracao];
            tension.volume = 0f;
        }

        void Update()
        {
            // tensão: só existe durante a prova e só com suspeita acima do "desconfiado"; investigando, sobe mais.
            // Usa tempo não escalado: na pausa (timeScale 0) o batimento precisa sumir mesmo assim.
            var gm = GameManager.I;
            var prof = ProfessorAI.I;
            float target = 0f, pitch = 0.85f;
            if (gm != null && gm.IsPlaying && prof != null)
            {
                float s = Mathf.InverseLerp(30f, 100f, prof.Suspicion);
                if (prof.State == ProfState.Investigando || prof.State == ProfState.Flagrante) s = Mathf.Max(s, 0.75f);
                target = s * tensionVolume;
                pitch = Mathf.Lerp(0.85f, 1.6f, s);
            }
            tension.volume = Mathf.MoveTowards(tension.volume, target, Time.unscaledDeltaTime * 0.6f);
            tension.pitch = Mathf.MoveTowards(tension.pitch, pitch, Time.unscaledDeltaTime);
            if (tension.volume > 0.001f && !tension.isPlaying) tension.Play();
            else if (tension.volume <= 0.001f && tension.isPlaying) tension.Stop();
        }

        /// <summary>Volume atual do batimento de tensão (0 = em silêncio).</summary>
        public float TensionLevel => tension != null ? tension.volume : 0f;

        public static int PlayCount(Sfx s) => counts.TryGetValue(s, out int n) ? n : 0;

        static void Count(Sfx s) => counts[s] = PlayCount(s) + 1;

        /// <summary>Som de interface (2D, igual em qualquer lugar).</summary>
        public static void Play(Sfx s)
        {
            if (I == null || !I.clips.TryGetValue(s, out var c)) return;
            Count(s);
            I.source.PlayOneShot(c, I.volume);
        }

        /// <summary>Som no mundo (3D): o volume e o lado dependem de onde ele acontece em relação à câmera.</summary>
        public static void PlayAt(Sfx s, Vector3 position, float vol = 1f, float pitch = 1f)
        {
            if (I == null || !I.clips.TryGetValue(s, out var c)) return;
            Count(s);
            var src = I.pool[I.poolNext];
            I.poolNext = (I.poolNext + 1) % I.pool.Length;
            src.transform.position = position;
            src.pitch = pitch;
            src.PlayOneShot(c, I.volume * vol);
        }

        // ------------------------------------------------------------------ síntese

        static AudioClip Tones(string name, float[] freqs, float noteLen, float amp, bool square = false)
        {
            const int rate = 44100;
            int per = (int)(rate * noteLen);
            var data = new float[per * freqs.Length];
            for (int n = 0; n < freqs.Length; n++)
            {
                float f = freqs[n];
                for (int i = 0; i < per; i++)
                {
                    if (f <= 0f) continue;
                    float tt = (float)i / rate;
                    float env = Mathf.Clamp01(i / 200f) * Mathf.Clamp01((per - i) / (per * 0.4f));
                    float w = Mathf.Sin(2f * Mathf.PI * f * tt);
                    if (square) w = Mathf.Sign(w) * 0.5f;
                    data[n * per + i] = w * env * amp;
                }
            }
            return Clip(name, data);
        }

        const int Rate = 44100;

        static AudioClip Clip(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>Passo de sapato no piso: estalo de ruído filtrado + baque grave curto.</summary>
        static AudioClip Footstep()
        {
            var rnd = new System.Random(7);
            int n = (int)(Rate * 0.09f);
            var d = new float[n];
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float noise = (float)(rnd.NextDouble() * 2.0 - 1.0);
                lp += (noise - lp) * 0.25f; // passa-baixa simples: tira o chiado agudo
                float click = lp * Mathf.Exp(-t * 60f);
                float thump = Mathf.Sin(2f * Mathf.PI * 95f * t) * Mathf.Exp(-t * 45f);
                d[i] = (click * 0.9f + thump * 0.6f) * 0.7f;
            }
            return Clip("passo", d);
        }

        /// <summary>Cadeira arrastando: ronco áspero que desce de tom (dente de serra + ruído).</summary>
        static AudioClip ChairScrape()
        {
            var rnd = new System.Random(11);
            int n = (int)(Rate * 0.35f);
            var d = new float[n];
            float phase = 0f, lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate, k = (float)i / n;
                phase += Mathf.Lerp(140f, 85f, k) / Rate;
                float saw = (phase % 1f) * 2f - 1f;
                float noise = (float)(rnd.NextDouble() * 2.0 - 1.0);
                lp += (noise - lp) * 0.3f;
                float env = Mathf.Clamp01(t / 0.02f) * (1f - k);
                d[i] = (saw * 0.35f + lp * 0.5f) * env * 0.6f;
            }
            return Clip("cadeira", d);
        }

        /// <summary>"Hm?" do professor ao ouvir algo: tom grave que sobe no fim, como uma pergunta.</summary>
        static AudioClip Hmm()
        {
            int n = (int)(Rate * 0.42f);
            var d = new float[n];
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate, k = (float)i / n;
                float f = k < 0.55f ? 150f : Mathf.Lerp(150f, 230f, (k - 0.55f) / 0.45f);
                phase += f / Rate;
                float w = Mathf.Sin(2f * Mathf.PI * phase) + 0.35f * Mathf.Sin(4f * Mathf.PI * phase); // voz "anasalada"
                float env = Mathf.Clamp01(t / 0.03f) * Mathf.Clamp01((1f - k) / 0.15f);
                d[i] = w * env * 0.4f;
            }
            return Clip("hm", d);
        }

        /// <summary>Batimento "tum-tum" (1 s, em loop). A velocidade sobe com o pitch da fonte de tensão.</summary>
        static AudioClip Heartbeat()
        {
            int n = Rate;
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float a = Beat(t, 0f, 1f) + Beat(t, 0.26f, 0.7f);
                d[i] = a * 0.8f;
            }
            return Clip("coracao", d);

            static float Beat(float t, float at, float amp)
            {
                float x = t - at;
                if (x < 0f) return 0f;
                return Mathf.Sin(2f * Mathf.PI * 52f * x) * Mathf.Exp(-x * 18f) * amp;
            }
        }
    }
}
