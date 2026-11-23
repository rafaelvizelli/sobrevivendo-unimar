using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ProvaImpossivel
{
    /// <summary>
    /// DESAFIO A — CARA DE PAISAGEM. O professor parou do seu lado e está te encarando.
    /// Aperte ESPAÇO quando o ponteiro passar na zona verde para controlar a respiração.
    /// Quanto mais perto ele está, mais rápido o ponteiro e menor a zona.
    /// </summary>
    public class FacePokerChallenge : MonoBehaviour
    {
        public static FacePokerChallenge I { get; private set; }
        public bool Active { get; private set; }
        /// <summary>Frame em que o desafio terminou (para o ESPAÇO não fazer o jogador levantar junto).</summary>
        public int FinishedFrame { get; private set; } = -1;

        public int hitsNeeded = 3;
        public int missesAllowed = 1;
        public float duration = 10f;
        [Tooltip("Idas e voltas por segundo do ponteiro (a dificuldade sobrescreve)")] public float baseSpeed = 0.6f;
        [Tooltip("Largura da zona verde, fração da barra (a dificuldade sobrescreve)")] public float zoneWidth = 0.22f;
        [Tooltip("Segundos de 'Prepare-se' antes de valer")] public float prepTime = 1.5f;

        GameObject panel;
        RectTransform track, zone, pointer;
        Text statusText, timerText, feedbackText, help;

        /// <summary>S7: o texto de ajuda mostra a tecla do dispositivo em uso (ESPAÇO ou A).</summary>
        static string HelpText() => "O professor está te encarando. Aperte " + Controls.Label(Acao.CaraDePaisagem).ToUpperInvariant()
                                    + " quando o ponteiro estiver na zona verde.";
        float timeLeft, speed, zoneCenter, curZone, t, feedbackUntil, prepLeft;
        int hits, misses;
        Action<bool> done;

        void Awake() => I = this;
        void Start() => Build();

        void Build()
        {
            var box = UIFactory.Box("Desafio A", HUD.I.PanelRoot, HUD.PanelColor);
            box.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 230f), new Vector2(900f, 260f));
            panel = box.gameObject;
            var tr = box.transform;

            var title = UIFactory.Label("Titulo", tr, "DESAFIO A — CARA DE PAISAGEM!", 30, HUD.Yellow);
            title.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(860f, 40f));
            help = UIFactory.Label("Ajuda", tr, HelpText(), 20, HUD.Gray, TextAnchor.MiddleCenter, FontStyle.Italic);
            help.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -54f), new Vector2(860f, 30f));

            var trackImg = UIFactory.Box("Trilho", tr, new Color(0.75f, 0.2f, 0.2f));
            track = trackImg.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -104f), new Vector2(780f, 44f));
            var zoneImg = UIFactory.Box("ZonaVerde", track, new Color(0.25f, 0.8f, 0.35f), false);
            zone = zoneImg.rectTransform;
            zone.anchorMin = new Vector2(0f, 0f);
            zone.anchorMax = new Vector2(0f, 1f);
            zone.pivot = new Vector2(0.5f, 0.5f);
            var ptrImg = UIFactory.Box("Ponteiro", track, Color.white);
            pointer = ptrImg.rectTransform;
            pointer.anchorMin = new Vector2(0f, 0f);
            pointer.anchorMax = new Vector2(0f, 1f);
            pointer.pivot = new Vector2(0.5f, 0.5f);
            pointer.sizeDelta = new Vector2(10f, 22f);

            statusText = UIFactory.Label("Status", tr, "", 24, Color.white, TextAnchor.MiddleLeft);
            statusText.rectTransform.Place(new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(60f, 50f), new Vector2(500f, 34f));
            timerText = UIFactory.Label("Tempo", tr, "", 24, Color.white, TextAnchor.MiddleRight);
            timerText.rectTransform.Place(new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-60f, 50f), new Vector2(300f, 34f));
            feedbackText = UIFactory.Label("Feedback", tr, "", 24, HUD.Green);
            feedbackText.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(860f, 34f));
            panel.SetActive(false);
        }

        /// <param name="closeness">0 = professor longe, 1 = colado em você.</param>
        public void Begin(float closeness, Action<bool> onDone)
        {
            closeness = Mathf.Clamp01(closeness);
            done = onDone;
            hits = misses = 0;
            timeLeft = duration;
            prepLeft = prepTime;
            // mais perto = um pouco mais rápido e zona um pouco menor (no máximo +25% / -20%)
            speed = baseSpeed * Mathf.Lerp(1f, 1.25f, closeness);
            curZone = zoneWidth * Mathf.Lerp(1f, 0.8f, closeness);
            t = UnityEngine.Random.value;
            NewZone();
            help.text = HelpText();
            Feedback("Prepare-se... o professor está te encarando!", HUD.Yellow, prepTime);
            Active = true;
            panel.SetActive(true);
            panel.transform.SetAsLastSibling();
            PlayerController.I.Lock(true);
            SoundFX.Play(Sfx.Alerta);
        }

        void NewZone()
        {
            zoneCenter = UnityEngine.Random.Range(curZone, 1f - curZone);
            float w = track.rect.width;
            zone.anchoredPosition = new Vector2(zoneCenter * w, 0f);
            zone.sizeDelta = new Vector2(curZone * w, 0f);
        }

        void Update()
        {
            if (!Active) return;
            var gm = GameManager.I;
            if (gm == null || !gm.IsPlaying) return;

            float dt = Time.deltaTime;
            t += dt * speed;
            float pos = Mathf.PingPong(t, 1f);
            pointer.anchoredPosition = new Vector2(pos * track.rect.width, 0f);

            statusText.text = "Calma: " + hits + "/" + hitsNeeded + "    Tremidas: " + misses + " (limite " + missesAllowed + ")";
            if (Time.time > feedbackUntil) feedbackText.text = "";

            // contagem de preparação: o ponteiro já anda, mas ainda não vale
            if (prepLeft > 0f)
            {
                prepLeft -= dt;
                timerText.text = "Prepare-se " + Mathf.CeilToInt(prepLeft);
                return;
            }
            timeLeft -= dt;
            timerText.text = Mathf.CeilToInt(Mathf.Max(0f, timeLeft)) + "s";

            // S7: ESPAÇO no teclado, A no controle
            if (Controls.Pressed(Acao.CaraDePaisagem)) Press();

            if (!Active) return;
            if (hits >= hitsNeeded) Finish(true, "");
            else if (misses > missesAllowed) Finish(false, "você errou o tempo " + misses + " vezes (o limite era " + missesAllowed + ")");
            else if (timeLeft <= 0f) Finish(false, "o tempo acabou antes de você acertar " + hitsNeeded + " vezes");
        }

        /// <summary>O jogador apertou ESPAÇO (chamado pelo Update; público para testes automatizados).</summary>
        public void Press()
        {
            if (!Active || prepLeft > 0f) return;
            float pos = PointerPosition;
            // tolerância de meio "ponteiro" para não punir quase-acertos
            if (Mathf.Abs(pos - zoneCenter) <= curZone * 0.5f + 0.01f)
            {
                hits++;
                SoundFX.Play(Sfx.Acerto);
                Feedback("Inspira... expira... (cara de paisagem)", HUD.Green);
                speed *= 1.12f;
                NewZone();
            }
            else
            {
                misses++;
                SoundFX.Play(Sfx.Erro);
                Feedback(pos < zoneCenter ? "*glup* Cedo demais! Espere o ponteiro entrar no verde." : "*glup* Tarde demais! Aperte assim que entrar no verde.", HUD.Red);
            }
        }

        public float PointerPosition => Mathf.PingPong(t, 1f);
        public float ZoneCenter => zoneCenter;
        public float ZoneWidth => curZone;
        public bool Preparing => prepLeft > 0f;

        void Feedback(string s, Color c, float seconds = 1f)
        {
            feedbackText.text = s;
            feedbackText.color = c;
            feedbackUntil = Time.time + seconds;
        }

        void Finish(bool ok, string why)
        {
            Active = false;
            FinishedFrame = Time.frameCount;
            panel.SetActive(false);
            PlayerController.I.Lock(false);
            HUD.I.Toast(ok ? "DESAFIO A CONCLUÍDO! Cara de paisagem nível Monalisa. A suspeita caiu bastante."
                           : "DESAFIO A FALHOU: " + why + ". O professor ficou mais desconfiado.", ok ? HUD.Green : HUD.Red, 4f, important: true);
            var cb = done;
            done = null;
            cb?.Invoke(ok);
        }
    }
}
