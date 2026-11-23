using System.Collections.Generic;
using UnityEngine;

namespace ProvaImpossivel
{
    public enum Sfx { Ponto, PerdePonto, Flagrante, Evento, Alerta, Clique, Acerto, Erro, Sinal, Passo, Cadeira, Ouviu, Coracao,
                      Papel, Vibra, Gole, Aha, Hmpf }

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
        [Tooltip("Distância máxima em que um som 3D ainda é ouvido (m)")] public float maxHearDistance = 16f;
        [Tooltip("Volumes relativos do ambiente e da tensão (Bloco 14)")] public AudioMix mix = new();

        AudioSource source, tension;
        // ambiente (Bloco 14): dois loops e uma fonte para os sons esporádicos, no mesmo objeto (como source/tension)
        AudioSource murmurLoop, clockLoop, ambientShot;
        AudioClip pencilClip;
        float nextPencil, nextChair, duckUntil = -1f, ambientGain;
        AudioSource[] pool;
        int poolNext;
        readonly Dictionary<Sfx, AudioClip> clips = new();
        // contagem de sons tocados por tipo (diagnóstico e testes automatizados)
        static readonly Dictionary<Sfx, int> counts = new();
        static readonly Dictionary<Ambiente, int> ambientCounts = new();

        void Awake()
        {
            I = this;
            counts.Clear(); // o domínio não recarrega entre Plays: zera junto com a cena
            ambientCounts.Clear();
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
            clips[Sfx.Papel] = PaperFlick();
            clips[Sfx.Vibra] = Buzz();
            clips[Sfx.Gole] = Gulp();
            clips[Sfx.Aha] = Aha();
            clips[Sfx.Hmpf] = Hmpf();
            // S5: a trilha adaptativa mora no mesmo objeto do som (criada por código: a cena não precisa ser remontada)
            if (GetComponent<MusicDirector>() == null) gameObject.AddComponent<MusicDirector>();

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

            murmurLoop = Loop(MurmurClip());
            clockLoop = Loop(ClockClip());
            ambientShot = gameObject.AddComponent<AudioSource>();
            ambientShot.playOnAwake = false;
            ambientShot.spatialBlend = 0f;
            pencilClip = PencilClip();
            nextPencil = Random.Range(mix.pencilInterval.x, mix.pencilInterval.y);
            nextChair = Random.Range(mix.chairInterval.x, mix.chairInterval.y);
        }

        /// <summary>Fonte 2D em loop que já começa tocando em silêncio (o volume é que entra e sai).</summary>
        AudioSource Loop(AudioClip clip)
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.spatialBlend = 0f;
            s.loop = true;
            s.clip = clip;
            s.volume = 0f;
            s.Play();
            return s;
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
                target = s * mix.tensionMax;
                pitch = Mathf.Lerp(0.85f, 1.6f, s);
            }
            tension.volume = Mathf.MoveTowards(tension.volume, Mathf.Min(target, mix.tensionMax), Time.unscaledDeltaTime * 0.6f);
            tension.pitch = Mathf.MoveTowards(tension.pitch, pitch, Time.unscaledDeltaTime);
            if (tension.volume > 0.001f && !tension.isPlaying) tension.Play();
            else if (tension.volume <= 0.001f && tension.isPlaying) tension.Stop();

            UpdateAmbient(gm != null && gm.IsPlaying);
        }

        /// <summary>
        /// Ambiente da sala: só durante a prova (some no menu, na pausa e no fim), abaixa quando toca um som
        /// importante e quando a tensão sobe. Lápis e cadeira aparecem de vez em quando, de um lado ou do outro.
        /// </summary>
        void UpdateAmbient(bool playing)
        {
            float tension01 = mix.tensionMax > 0f ? tension.volume / mix.tensionMax : 0f;
            float target = mix.AmbientGain(playing, Time.unscaledTime < duckUntil, tension01);
            // abaixar é rápido (o som importante não pode esperar); voltar é devagar
            float speed = target < ambientGain ? 4f : mix.fadeSpeed;
            ambientGain = Mathf.MoveTowards(ambientGain, target, Time.unscaledDeltaTime * speed);
            murmurLoop.volume = mix.murmur * ambientGain;
            clockLoop.volume = mix.clock * ambientGain;
            ambientShot.volume = ambientGain;

            if (!playing) return;
            nextPencil -= Time.deltaTime;
            nextChair -= Time.deltaTime;
            if (nextPencil <= 0f)
            {
                nextPencil = Random.Range(mix.pencilInterval.x, mix.pencilInterval.y);
                PlayAmbient(Ambiente.Lapis, pencilClip, Random.Range(0.9f, 1.15f));
            }
            if (nextChair <= 0f)
            {
                nextChair = Random.Range(mix.chairInterval.x, mix.chairInterval.y);
                PlayAmbient(Ambiente.Cadeira, clips[Sfx.Cadeira], Random.Range(0.75f, 0.95f)); // mais grave: cadeira "dos outros"
            }
        }

        void PlayAmbient(Ambiente a, AudioClip clip, float pitch)
        {
            ambientCounts[a] = AmbientCount(a) + 1;
            ambientShot.panStereo = Random.Range(-0.7f, 0.7f);
            ambientShot.pitch = pitch;
            ambientShot.PlayOneShot(clip, mix.Level(a));
        }

        /// <summary>Volume atual do batimento de tensão (0 = em silêncio).</summary>
        public float TensionLevel => tension != null ? tension.volume : 0f;

        /// <summary>Multiplicador atual do ambiente (0 = sala em silêncio, 1 = volume cheio do mix).</summary>
        public float AmbientGain => ambientGain;

        /// <summary>Volume efetivo de um som de fundo agora (antes do volume geral).</summary>
        public float AmbientVolume(Ambiente a) => mix.Level(a) * ambientGain;

        public static int PlayCount(Sfx s) => counts.TryGetValue(s, out int n) ? n : 0;
        public static int AmbientCount(Ambiente a) => ambientCounts.TryGetValue(a, out int n) ? n : 0;

        static void Count(Sfx s)
        {
            counts[s] = PlayCount(s) + 1;
            if (AudioMix.IsImportant(s)) I.duckUntil = Time.unscaledTime + I.mix.duckSeconds;
        }

        /// <summary>Som de interface (2D, igual em qualquer lugar).</summary>
        public static void Play(Sfx s)
        {
            if (I == null || !I.clips.TryGetValue(s, out var c)) return;
            Count(s);
            I.source.PlayOneShot(c, I.volume * GameOptions.SfxVolume);
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
            src.PlayOneShot(c, I.volume * vol * GameOptions.SfxVolume);
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

        /// <summary>Bilhete (Bloco 15): papel amassado voando — chiado curto e agudo que some rápido.</summary>
        static AudioClip PaperFlick()
        {
            var rnd = new System.Random(23);
            int n = (int)(Rate * 0.16f);
            var d = new float[n];
            float prev = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float noise = (float)(rnd.NextDouble() * 2.0 - 1.0);
                float hp = noise - prev; // passa-alta simples: só o "chhh" do papel
                prev = noise;
                float crackle = rnd.NextDouble() < 0.02 ? 1f : 0.35f;
                d[i] = hp * crackle * Mathf.Exp(-t * 22f) * 0.45f;
            }
            return Clip("papel", d);
        }

        /// <summary>Celular vibrando na mesa (Bloco 15): zumbido grave em dois pulsos ("bzz-bzz").</summary>
        static AudioClip Buzz()
        {
            int n = (int)(Rate * 0.5f);
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                bool on = t < 0.18f || (t > 0.26f && t < 0.44f);
                if (!on) continue;
                float w = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * 150f * t)) * 0.5f + 0.3f * Mathf.Sin(2f * Mathf.PI * 300f * t);
                d[i] = w * 0.45f;
            }
            return Clip("vibra", d);
        }

        /// <summary>Gole de água (Bloco 15): dois "glup" graves que sobem de tom.</summary>
        static AudioClip Gulp()
        {
            int n = (int)(Rate * 0.45f);
            var d = new float[n];
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float local = t < 0.2f ? t : t - 0.22f;
                if (local < 0f || local > 0.18f) continue;
                phase += Mathf.Lerp(110f, 260f, local / 0.18f) / Rate;
                float env = Mathf.Sin(Mathf.PI * local / 0.18f);
                d[i] = Mathf.Sin(2f * Mathf.PI * phase) * env * 0.6f;
            }
            return Clip("gole", d);
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

        /// <summary>
        /// Voz do professor (S4): sílabas com a mesma voz "anasalada" do "hm?", cada uma com tom inicial e final (Hz),
        /// duração (s), volume e pausa depois (s). Provisória até existir voz gravada.
        /// </summary>
        static float[] Voice((float f0, float f1, float dur, float amp, float gap)[] syllables)
        {
            float total = 0f;
            foreach (var s in syllables) total += s.dur + s.gap;
            var d = new float[(int)(Rate * total) + 1];
            int at = 0;
            float phase = 0f;
            foreach (var s in syllables)
            {
                int n = (int)(Rate * s.dur);
                for (int i = 0; i < n && at + i < d.Length; i++)
                {
                    float t = (float)i / Rate, k = (float)i / n;
                    float f = Mathf.Lerp(s.f0, s.f1, k) * (1f + 0.02f * Mathf.Sin(2f * Mathf.PI * 6f * t)); // vibrato de teatro
                    phase += f / Rate;
                    float w = Mathf.Sin(2f * Mathf.PI * phase) + 0.45f * Mathf.Sin(4f * Mathf.PI * phase) + 0.2f * Mathf.Sin(6f * Mathf.PI * phase);
                    float env = Mathf.Clamp01(t / 0.02f) * Mathf.Clamp01((1f - k) / 0.2f);
                    d[at + i] = w * env * s.amp;
                }
                at += n + (int)(Rate * s.gap);
            }
            return d;
        }

        /// <summary>"a-HÁ!": a sílaba curta e a segunda subindo, mais alta (o flagrante é o momento dele).</summary>
        static AudioClip Aha() => Clip("aha", Voice(new[] { (190f, 175f, 0.11f, 0.3f, 0.05f), (240f, 330f, 0.26f, 0.45f, 0f) }));

        /// <summary>"hmpf": descendo, contrariado (alarme falso, você escapou), com o sopro do "pf" no fim.</summary>
        static AudioClip Hmpf()
        {
            var d = Voice(new[] { (150f, 105f, 0.28f, 0.35f, 0f) });
            var noise = new System.Random(7);
            int tail = (int)(Rate * 0.08f);
            var outd = new float[d.Length + tail];
            System.Array.Copy(d, outd, d.Length);
            for (int i = 0; i < tail; i++)
                outd[d.Length + i] = (float)(noise.NextDouble() * 2.0 - 1.0) * 0.18f * (1f - (float)i / tail);
            return Clip("hmpf", outd);
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

        // ------------------------------------------------------------------ ambiente (Bloco 14)

        /// <summary>Pico (0 a 1) de um trecho sintetizado: o mix conta com clips de ambiente sem estourar.</summary>
        public const float AmbientClipPeak = 0.6f;

        /// <summary>
        /// Murmúrio da turma (8 s, em loop): várias "vozes" de ruído na faixa da fala, cada uma falando em sílabas
        /// e pausas. O fim é misturado com o começo para o loop não estalar.
        /// </summary>
        public static AudioClip MurmurClip()
        {
            const float seconds = 8f, fadeSeconds = 0.6f;
            int n = (int)(Rate * seconds), fade = (int)(Rate * fadeSeconds);
            var raw = new float[n + fade];
            var rnd = new System.Random(23);
            for (int v = 0; v < 6; v++)
            {
                float lowA = 0f, lowB = 0f, env = 0f, target = 0f;
                int nextChange = 0;
                // cada voz tem um "timbre" (faixa) um pouco diferente
                float cutHigh = 0.05f + 0.03f * (float)rnd.NextDouble(), cutLow = 0.008f + 0.006f * (float)rnd.NextDouble();
                for (int i = 0; i < raw.Length; i++)
                {
                    if (i >= nextChange)
                    {
                        // sílaba (0,08–0,25 s) ou pausa (0,2–1,2 s)
                        bool talk = rnd.NextDouble() < 0.6;
                        target = talk ? 0.4f + 0.6f * (float)rnd.NextDouble() : 0f;
                        nextChange = i + (int)(Rate * (talk ? 0.08 + 0.17 * rnd.NextDouble() : 0.2 + rnd.NextDouble()));
                    }
                    env += (target - env) * 0.0015f;
                    float noise = (float)(rnd.NextDouble() * 2.0 - 1.0);
                    lowA += (noise - lowA) * cutHigh; // tira o chiado (agudo)
                    lowB += (lowA - lowB) * cutLow;   // tira o grave
                    raw[i] += (lowA - lowB) * env;
                }
            }
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float k = i < fade ? (float)i / fade : 1f;
                d[i] = raw[i] * k + (i < fade ? raw[n + i] * (1f - k) : 0f);
            }
            return Clip("murmurio", Normalize(d, AmbientClipPeak));
        }

        /// <summary>Tique-taque (2 s, em loop): "tic" agudo no segundo par, "tac" mais grave no ímpar.</summary>
        public static AudioClip ClockClip()
        {
            var d = new float[Rate * 2];
            var rnd = new System.Random(5);
            Tick(d, 0, 2600f, rnd);
            Tick(d, Rate, 1900f, rnd);
            return Clip("relogio", Normalize(d, AmbientClipPeak));

            static void Tick(float[] d, int at, float freq, System.Random rnd)
            {
                int len = (int)(Rate * 0.03f);
                for (int i = 0; i < len; i++)
                {
                    float t = (float)i / Rate;
                    float noise = (float)(rnd.NextDouble() * 2.0 - 1.0);
                    d[at + i] = (Mathf.Sin(2f * Mathf.PI * freq * t) * 0.7f + noise * 0.3f) * Mathf.Exp(-t * 220f);
                }
            }
        }

        /// <summary>Lápis riscando (≈1,2 s): 3 a 5 traços de ruído agudo, com pausinhas entre eles.</summary>
        public static AudioClip PencilClip()
        {
            var rnd = new System.Random(17);
            var d = new float[(int)(Rate * 1.2f)];
            int pos = (int)(Rate * 0.02f);
            float low = 0f;
            int strokes = 3 + rnd.Next(3);
            for (int s = 0; s < strokes && pos < d.Length; s++)
            {
                int len = (int)(Rate * (0.08 + 0.14 * rnd.NextDouble()));
                for (int i = 0; i < len && pos + i < d.Length; i++)
                {
                    float noise = (float)(rnd.NextDouble() * 2.0 - 1.0);
                    low += (noise - low) * 0.12f;
                    float high = noise - low; // só o agudo: o "chhh" do grafite
                    float env = Mathf.Sin(Mathf.PI * i / len);
                    d[pos + i] = high * env;
                }
                pos += len + (int)(Rate * (0.04 + 0.12 * rnd.NextDouble()));
            }
            return Clip("lapis", Normalize(d, AmbientClipPeak));
        }

        static float[] Normalize(float[] d, float peak)
        {
            float max = 0f;
            foreach (float x in d) max = Mathf.Max(max, Mathf.Abs(x));
            if (max <= 0f) return d;
            float k = peak / max;
            for (int i = 0; i < d.Length; i++) d[i] *= k;
            return d;
        }

        /// <summary>Clip de um som de jogo, como o jogo usa (testes comparam com o ambiente).</summary>
        public AudioClip ClipOf(Sfx s) => clips.TryGetValue(s, out var c) ? c : null;
    }
}
