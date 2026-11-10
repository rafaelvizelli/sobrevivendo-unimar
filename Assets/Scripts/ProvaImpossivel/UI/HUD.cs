using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ProvaImpossivel
{
    /// <summary>
    /// Interface inteira montada por código: tempo, nota/questões, flagrantes e favores, prompts, mensagens,
    /// balões de fala e telas de introdução / pausa / resultado. O estado do professor e dos colegas NÃO fica aqui:
    /// aparece no mundo, no corpo deles (MoodIcon, cone de visão). Por isso o HUD só lê GameManager e ExamSystem.
    /// </summary>
    public class HUD : MonoBehaviour
    {
        public static HUD I { get; private set; }

        public static readonly Color Yellow = new(1f, 0.85f, 0.2f);
        public static readonly Color Red = new(1f, 0.32f, 0.3f);
        public static readonly Color Green = new(0.4f, 0.95f, 0.45f);
        public static readonly Color Orange = new(1f, 0.6f, 0.2f);
        public static readonly Color Gray = new(0.8f, 0.8f, 0.82f);
        public static readonly Color PanelColor = new(0.1f, 0.11f, 0.16f, 0.88f);

        /// <summary>Raiz para painéis de desafios (fica acima do HUD).</summary>
        public RectTransform PanelRoot { get; private set; }

        RectTransform root, hudRoot, bubbleRoot, toastRoot;
        Text timerText, gradeTitle, gradeText, strikesText, promptText, holdText, seatedHelp, tutorialText;
        Image tutorialBox;
        Image timerBox, holdFill, flash, promptBox;
        Image[] slots, knownFrames;
        GameObject introPanel, pausePanel, resultPanel;
        float flashAlpha;
        Color flashColor;

        [Tooltip("Máximo de mensagens empilhadas no topo")] public int maxToasts = 2;
        [Tooltip("Máximo de balões de fala ao mesmo tempo")] public int maxBubbles = 3;

        class Toast_ { public RectTransform rect; public Image box; public Text text; public float until; public bool important; public string raw; }
        readonly List<Toast_> toasts = new();

        class Bubble { public RectTransform rect; public Image box; public Text name, text; public Transform target; public float until, born; public int priority; }
        readonly List<Bubble> bubbles = new();

        void Awake()
        {
            I = this;
            EnsureEventSystem();
            BuildCanvas();
            BuildHud();
        }

        // =================================================================== build

        static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            DontDestroyOnLoad(es);
        }

        void BuildCanvas()
        {
            var go = new GameObject("HUD Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            root = (RectTransform)go.transform;

            bubbleRoot = UIFactory.Rect("Baloes", root).Stretch();
            hudRoot = UIFactory.Rect("HUD", root).Stretch();
            PanelRoot = UIFactory.Rect("Paineis", root).Stretch();
            toastRoot = UIFactory.Rect("Mensagens", root).Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -124f), new Vector2(1000f, 400f));
            flash = UIFactory.Box("Flash", root, new Color(1, 0, 0, 0), false);
            flash.rectTransform.Stretch();
        }

        void BuildHud()
        {
            // cronômetro
            timerBox = UIFactory.Box("Tempo", hudRoot, PanelColor);
            timerBox.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(240f, 96f));
            var cap = UIFactory.Label("Rotulo", timerBox.transform, "TEMPO", 18, Gray);
            cap.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -4f), new Vector2(200f, 24f));
            timerText = UIFactory.Label("Valor", timerBox.transform, "06:00", 54, Color.white);
            timerText.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 4f), new Vector2(230f, 66f));

            // nota
            var grade = UIFactory.Box("Nota", hudRoot, PanelColor);
            grade.rectTransform.Place(new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-16f, -16f), new Vector2(440f, 158f));
            gradeTitle = UIFactory.Label("Titulo", grade.transform, "NOTA", 20, Yellow, TextAnchor.UpperLeft);
            gradeTitle.rectTransform.Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -10f), new Vector2(410f, 26f));
            gradeTitle.horizontalOverflow = HorizontalWrapMode.Overflow;
            gradeText = UIFactory.Label("Valor", grade.transform, "0,0 / 6,0", 44, Color.white, TextAnchor.UpperRight);
            gradeText.rectTransform.Place(new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-16f, -6f), new Vector2(300f, 54f));
            slots = new Image[10];
            knownFrames = new Image[10];
            for (int i = 0; i < slots.Length; i++)
            {
                // moldura amarela (atrás da caixinha) = questão que o jogador sabe responder sozinho
                knownFrames[i] = UIFactory.Box("Sabe" + (i + 1), grade.transform, Yellow, false);
                knownFrames[i].rectTransform.Place(new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(14f + i * 41f, 42f), new Vector2(40f, 40f));
                knownFrames[i].enabled = false;
                slots[i] = UIFactory.Box("Q" + (i + 1), grade.transform, new Color(0.35f, 0.35f, 0.4f));
                slots[i].rectTransform.Place(new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(18f + i * 41f, 46f), new Vector2(32f, 32f));
                var n = UIFactory.Label("N", slots[i].transform, (i + 1).ToString(), 16, Color.white);
                n.rectTransform.Stretch();
            }
            strikesText = UIFactory.Label("Flagrantes", grade.transform, "", 20, Gray, TextAnchor.LowerLeft);
            strikesText.rectTransform.Place(new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(16f, 8f), new Vector2(410f, 30f));


            // prompt de interação + barra de segurar
            promptBox = UIFactory.Box("Prompt", hudRoot, new Color(0.08f, 0.08f, 0.1f, 0.85f));
            promptBox.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(820f, 58f));
            promptText = UIFactory.Label("Texto", promptBox.transform, "", 28, Yellow);
            promptText.rectTransform.Stretch(6f);
            promptBox.gameObject.SetActive(false);

            holdFill = UIFactory.Bar("Segurar", hudRoot, new Color(0f, 0f, 0f, 0.7f), Yellow);
            ((RectTransform)holdFill.transform.parent).Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 104f), new Vector2(640f, 36f));
            holdText = UIFactory.Label("Texto", holdFill.transform.parent, "", 20, Color.white);
            holdText.rectTransform.Stretch();
            holdFill.transform.parent.gameObject.SetActive(false);

            seatedHelp = UIFactory.Label("Ajuda", hudRoot, "", 22, Color.white);
            seatedHelp.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(1300f, 40f));

            // passo do tutorial (só no treino): uma linha, acima do prompt
            tutorialBox = UIFactory.Box("Tutorial", hudRoot, new Color(0.15f, 0.25f, 0.5f, 0.92f));
            tutorialBox.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 220f), new Vector2(1250f, 56f));
            tutorialText = UIFactory.Label("Texto", tutorialBox.transform, "", 24, Color.white);
            tutorialText.rectTransform.Stretch(8f);
            tutorialBox.gameObject.SetActive(false);

            hudRoot.gameObject.SetActive(false);
        }

        // =================================================================== update

        void Update()
        {
            var gm = GameManager.I;
            bool show = gm != null && (gm.Phase == Phase.Playing || gm.Phase == Phase.Paused);
            if (hudRoot.gameObject.activeSelf != show) hudRoot.gameObject.SetActive(show);
            if (show) RefreshHud(gm);
            UpdateToasts();
            UpdateFlash();
        }

        void LateUpdate() => UpdateBubbles();

        void RefreshHud(GameManager gm)
        {
            float left = gm.TimeLeft;
            timerText.text = FormatTime(left);
            bool urgent = left <= 60f;
            timerText.color = urgent ? (Mathf.Repeat(Time.time, 1f) < 0.5f ? Red : Color.white) : Color.white;

            var exam = ExamSystem.I;
            float min = gm.Settings.minGrade;
            if (exam != null)
            {
                float g = exam.Grade;
                gradeText.text = GameManager.Fmt(g) + " / " + GameManager.Fmt(min);
                gradeText.color = g >= min ? Green : Color.white;
                for (int i = 0; i < slots.Length && i < exam.Questions.Length; i++)
                {
                    slots[i].color = exam.Questions[i] switch
                    {
                        QuestionState.Certa => new Color(0.2f, 0.75f, 0.3f),
                        QuestionState.Errada => new Color(0.8f, 0.2f, 0.2f),
                        _ => new Color(0.35f, 0.35f, 0.4f),
                    };
                    knownFrames[i].enabled = exam.Known[i];
                }
                // o título da nota faz o papel do antigo painel de objetivo
                gradeTitle.text = g >= min ? "NOTA — já dá! Entregue na mesa (E)" : "NOTA";
                gradeTitle.color = g >= min ? Green : Yellow;
            }
            // as caixinhas já mostram o máximo de flagrantes; o espaço vai para os favores da turma (Bloco 3)
            strikesText.text = "Flagrantes: " + Dots(gm.Flagrantes, gm.Settings.maxFlagrantes) + "    Favores: " + gm.Favors;

        }

        static string Dots(int n, int max)
        {
            var s = "";
            for (int i = 0; i < max; i++) s += i < n ? "[X]" : "[  ]";
            return s;
        }

        public static string FormatTime(float seconds)
        {
            seconds = Mathf.Max(0f, seconds);
            int m = (int)(seconds / 60f), s = (int)(seconds % 60f);
            return m.ToString("00") + ":" + s.ToString("00");
        }

        // =================================================================== prompts

        public void SetPrompt(string text)
        {
            bool on = !string.IsNullOrEmpty(text);
            if (promptBox.gameObject.activeSelf != on) promptBox.gameObject.SetActive(on);
            if (on) promptText.text = text;
        }

        /// <summary>Risco da ação que o jogador está segurando: quem chama decide (o HUD não lê o professor).</summary>
        public enum HoldRisk { Seguro, Escondido, Visto }

        public void SetHold(float progress, string label, HoldRisk risk = HoldRisk.Seguro)
        {
            bool on = progress >= 0f;
            var go = holdFill.transform.parent.gameObject;
            if (go.activeSelf != on) go.SetActive(on);
            if (!on) return;
            holdFill.fillAmount = Mathf.Clamp01(progress);
            holdText.text = label;
            // ação arriscada sendo vista = barra vermelha; arriscada escondida = verde
            holdFill.color = risk == HoldRisk.Visto ? Red : risk == HoldRisk.Escondido ? Green : Yellow;
        }

        public void SetSeatedHelp(string text) => seatedHelp.text = text ?? "";

        /// <summary>Passo atual do tutorial (null esconde).</summary>
        public void SetTutorial(string text)
        {
            bool on = !string.IsNullOrEmpty(text);
            if (tutorialBox.gameObject.activeSelf != on) tutorialBox.gameObject.SetActive(on);
            if (on) tutorialText.text = text;
        }

        // =================================================================== mensagens

        /// <param name="important">Mensagens importantes (flagrante, desafios) nunca são descartadas por mensagens comuns.</param>
        public void Toast(string text, Color color, float seconds = 3f, bool important = false)
        {
            // mesma mensagem já na tela: só renova o tempo
            var same = toasts.Find(x => x.raw == text);
            if (same != null)
            {
                same.until = Time.unscaledTime + seconds;
                return;
            }
            // abre espaço: descarta primeiro a mais antiga não importante
            while (toasts.Count >= maxToasts)
            {
                int idx = toasts.FindLastIndex(x => !x.important);
                if (idx < 0)
                {
                    if (!important) return;
                    idx = toasts.Count - 1;
                }
                Destroy(toasts[idx].rect.gameObject);
                toasts.RemoveAt(idx);
            }

            var box = UIFactory.Box("Mensagem", toastRoot, new Color(0.08f, 0.08f, 0.1f, 0.9f));
            var t = UIFactory.Label("Texto", box.transform, text, important ? 25 : 22, color);
            box.rectTransform.anchorMin = box.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            box.rectTransform.pivot = new Vector2(0.5f, 1f);
            box.rectTransform.sizeDelta = new Vector2(980f, 10f);
            t.rectTransform.Stretch(10f);
            float h = Mathf.Max(52f, t.preferredHeight + 22f);
            box.rectTransform.sizeDelta = new Vector2(980f, h);
            toasts.Insert(0, new Toast_ { rect = box.rectTransform, box = box, text = t, until = Time.unscaledTime + seconds, important = important, raw = text });
        }

        void UpdateToasts()
        {
            float y = 0f;
            for (int i = toasts.Count - 1; i >= 0; i--)
            {
                if (Time.unscaledTime > toasts[i].until)
                {
                    Destroy(toasts[i].rect.gameObject);
                    toasts.RemoveAt(i);
                }
            }
            foreach (var t in toasts)
            {
                float a = Mathf.Clamp01((t.until - Time.unscaledTime) / 0.4f);
                var c = t.box.color; c.a = 0.9f * a; t.box.color = c;
                var tc = t.text.color; tc.a = a; t.text.color = tc;
                t.rect.anchoredPosition = Vector2.Lerp(t.rect.anchoredPosition, new Vector2(0f, -y), Time.unscaledDeltaTime * 12f);
                y += t.rect.sizeDelta.y + 8f;
            }
        }

        public void Flash(Color c)
        {
            flashColor = c;
            flashAlpha = c.a;
        }

        void UpdateFlash()
        {
            flashAlpha = Mathf.MoveTowards(flashAlpha, 0f, Time.unscaledDeltaTime * 0.8f);
            var c = flashColor; c.a = flashAlpha;
            flash.color = c;
        }

        // =================================================================== balões de fala

        /// <param name="priority">Quando há balões demais, os de menor prioridade somem primeiro (professor = 2).</param>
        public void Say(Transform target, string speaker, string text, float seconds, Color nameColor, int priority = 0)
        {
            if (target == null || string.IsNullOrEmpty(text)) return;
            var b = bubbles.Find(x => x.target == target);
            if (b == null)
            {
                b = new Bubble { target = target };
                b.box = UIFactory.Box("Balao", bubbleRoot, new Color(1f, 1f, 0.97f, 0.95f));
                b.rect = b.box.rectTransform;
                b.rect.anchorMin = b.rect.anchorMax = new Vector2(0.5f, 0.5f);
                b.rect.pivot = new Vector2(0.5f, 0f);
                b.name = UIFactory.Label("Nome", b.rect, "", 17, nameColor, TextAnchor.UpperLeft);
                b.name.rectTransform.Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(12f, -6f), new Vector2(360f, 22f));
                var sh = b.name.GetComponent<Shadow>();
                sh.effectColor = new Color(0f, 0f, 0f, 0.9f);
                b.text = UIFactory.Label("Fala", b.rect, "", 22, new Color(0.08f, 0.08f, 0.1f), TextAnchor.UpperLeft, FontStyle.Normal);
                Destroy(b.text.GetComponent<Shadow>());
                bubbles.Add(b);
            }
            b.name.text = speaker;
            b.name.color = nameColor;
            b.text.text = text;
            float w = Mathf.Clamp(text.Length * 11f + 40f, 160f, 400f);
            b.text.rectTransform.Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(12f, string.IsNullOrEmpty(speaker) ? -8f : -28f), new Vector2(w - 24f, 10f));
            float h = b.text.preferredHeight + (string.IsNullOrEmpty(speaker) ? 18f : 38f);
            b.text.rectTransform.sizeDelta = new Vector2(w - 24f, h);
            b.rect.sizeDelta = new Vector2(w, h);
            b.until = Time.time + seconds;
            b.born = Time.time;
            b.priority = priority;
            b.rect.gameObject.SetActive(true);
            b.rect.SetAsLastSibling();

            // limite de balões: some primeiro o de menor prioridade e mais antigo
            var alive = bubbles.FindAll(x => x.target != null && Time.time < x.until);
            if (alive.Count > maxBubbles)
            {
                alive.Sort((x, y) => x.priority != y.priority ? x.priority.CompareTo(y.priority) : x.born.CompareTo(y.born));
                for (int i = 0; i < alive.Count - maxBubbles; i++) alive[i].until = 0f;
            }
        }

        void UpdateBubbles()
        {
            var cam = Camera.main;
            if (cam == null) return;
            Vector2 half = bubbleRoot.rect.size * 0.5f;
            var placed = new List<Bubble>();
            foreach (var b in bubbles)
            {
                bool alive = b.target != null && Time.time < b.until;
                if (b.rect.gameObject.activeSelf != alive) b.rect.gameObject.SetActive(alive);
                if (!alive) continue;
                placed.Add(b);

                Vector3 world = b.target.position + Vector3.up * 2.15f;
                Vector3 sp = cam.WorldToScreenPoint(world);
                bool behind = sp.z < 0f;
                if (behind) { sp.x = Screen.width - sp.x; sp.y = 0f; }
                RectTransformUtility.ScreenPointToLocalPointInRectangle(bubbleRoot, sp, null, out var lp);
                float w = b.rect.sizeDelta.x * 0.5f, h = b.rect.sizeDelta.y;
                lp.x = Mathf.Clamp(lp.x, -half.x + w + 10f, half.x - w - 10f);
                lp.y = Mathf.Clamp(lp.y, -half.y + 180f, half.y - h - 175f);
                b.rect.anchoredPosition = lp;
                float dist = Vector3.Distance(cam.transform.position, world);
                b.rect.localScale = Vector3.one * Mathf.Clamp(1.25f - dist * 0.04f, 0.75f, 1f);
            }

            // empurra balões que se sobrepõem (de baixo pra cima)
            placed.Sort((x, y) => x.rect.anchoredPosition.y.CompareTo(y.rect.anchoredPosition.y));
            for (int i = 1; i < placed.Count; i++)
            {
                var b = placed[i];
                for (int j = 0; j < i; j++)
                {
                    var o = placed[j];
                    Vector2 pb = b.rect.anchoredPosition, po = o.rect.anchoredPosition;
                    float wb = b.rect.sizeDelta.x * b.rect.localScale.x * 0.5f, wo = o.rect.sizeDelta.x * o.rect.localScale.x * 0.5f;
                    float ho = o.rect.sizeDelta.y * o.rect.localScale.y;
                    bool overlapX = Mathf.Abs(pb.x - po.x) < wb + wo + 6f;
                    bool overlapY = pb.y < po.y + ho + 6f && pb.y + b.rect.sizeDelta.y > po.y;
                    if (overlapX && overlapY) b.rect.anchoredPosition = new Vector2(pb.x, po.y + ho + 6f);
                }
            }

        }

        // =================================================================== telas

        /// <param name="firstTime">Primeira vez: escolher a dificuldade leva ao treino, e aparece "Pular tutorial".</param>
        public void ShowIntro(Action<Difficulty> onChoose, Action onTutorial = null, Action onSkipTutorial = null, bool firstTime = false)
        {
            introPanel = Dim("Introducao");
            var box = UIFactory.Box("Caixa", introPanel.transform, PanelColor);
            box.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1240f, 900f));
            var t = box.transform;
            AddText(t, "SOBREVIVENDO À UNIMAR", 66, Yellow, new Vector2(0f, -30f), new Vector2(1180f, 80f));
            AddText(t, "FASE 1 — A PROVA IMPOSSÍVEL", 40, Color.white, new Vector2(0f, -110f), new Vector2(1180f, 50f));
            AddText(t, "Você não estudou. A prova é do Prof. Takanervoso, o homem que \"não nasceu ontem\".\n"
                     + "Consiga a nota mínima antes do sinal — respondendo o que sabe, colando do Nerd,\n"
                     + "usando o celular ou aproveitando o caos da sala. Mas cuidado: 3 flagrantes e você está fora.",
                     24, Gray, new Vector2(0f, -175f), new Vector2(1160f, 110f), FontStyle.Normal);
            AddText(t, "<b>CONTROLES</b>\n"
                     + "WASD andar  •  Mouse câmera  •  Shift correr  •  E interagir (alguns: segure)  •  Esc pausar\n"
                     + "Sentado:  1 responder o que sabe / chutar  •  2 celular (segure; bateria fraca)  •  Espaço levantar\n"
                     + "Cara de Paisagem (Desafio A): ESPAÇO quando o ponteiro estiver na zona verde",
                     22, Color.white, new Vector2(0f, -310f), new Vector2(1160f, 130f), FontStyle.Normal);
            AddText(t, "ESCOLHA A DIFICULDADE  (clique, ou aperte 1, 2 ou 3)", 28, Yellow, new Vector2(0f, -480f), new Vector2(1180f, 40f));

            string[] names = { "FÁCIL", "NORMAL", "DIFÍCIL" };
            // tempo e nota vêm das DifficultySettings (antes eram texto fixo e ficavam errados quando o equilíbrio mudava)
            var gm = GameManager.I;
            string Desc(DifficultySettings s, string apelido) =>
                Mathf.RoundToInt(s.duration / 60f) + " min • nota " + GameManager.Fmt(s.minGrade) + "\n" + apelido;
            string[] desc =
            {
                Desc(gm.facil, "professor míope"),
                Desc(gm.normal, "o clássico"),
                Desc(gm.dificil, "professor com radar"),
            };
            Color[] cols = { new(0.2f, 0.6f, 0.3f), new(0.85f, 0.55f, 0.1f), new(0.75f, 0.15f, 0.15f) };
            for (int i = 0; i < 3; i++)
            {
                var d = (Difficulty)i;
                var b = UIFactory.Button("Botao" + names[i], t, names[i], cols[i], () => onChoose(d), 36);
                b.GetComponent<RectTransform>().Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2((i - 1) * 380f, -540f), new Vector2(340f, 80f));
                AddText(t, desc[i], 22, Gray, new Vector2((i - 1) * 380f, -630f), new Vector2(340f, 70f), FontStyle.Normal);
            }
            const string DicaNormal = "Dica: observe a sala. Professor, Nerd e Dorminhoco avisam antes, no corpo, não na tela.";
            var dica = AddText(t, firstTime ? "Primeira vez? Qualquer dificuldade começa pelo treino de 1 minuto." : DicaNormal,
                               20, new Color(0.6f, 0.9f, 1f), new Vector2(0f, -810f), new Vector2(1160f, 60f), FontStyle.Italic);
            // tutorial: na primeira vez é o caminho padrão (com opção de pular); depois, fica como "rever"
            if (onTutorial != null)
            {
                var tut = UIFactory.Button("BotaoTUTORIAL", t, firstTime ? "TUTORIAL (1 min)  [T]" : "Rever tutorial  [T]",
                                           new Color(0.25f, 0.45f, 0.8f), () => onTutorial(), firstTime ? 30 : 24);
                tut.GetComponent<RectTransform>().Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(firstTime ? -190f : 0f, -715f), new Vector2(firstTime ? 420f : 320f, 64f));
                if (firstTime && onSkipTutorial != null)
                {
                    GameObject skip = null;
                    skip = UIFactory.Button("BotaoPULAR", t, "Pular tutorial", new Color(0.3f, 0.3f, 0.35f), () =>
                    {
                        onSkipTutorial();
                        if (skip != null) skip.SetActive(false);
                        tut.GetComponentInChildren<Text>().text = "Rever tutorial  [T]";
                        dica.text = DicaNormal;
                    }, 24).gameObject;
                    skip.GetComponent<RectTransform>().Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(230f, -715f), new Vector2(300f, 64f));
                }
            }
        }

        public void HideIntro()
        {
            if (introPanel != null) Destroy(introPanel);
        }

        public void ShowPause(Action resume, Action restart, Action quit)
        {
            HidePause();
            pausePanel = Dim("Pausa");
            var box = UIFactory.Box("Caixa", pausePanel.transform, PanelColor);
            box.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(640f, 560f));
            AddText(box.transform, "PAUSADO", 60, Yellow, new Vector2(0f, -30f), new Vector2(600f, 80f));
            AddText(box.transform, "O professor também congelou. Aproveite pra respirar.", 22, Gray, new Vector2(0f, -110f), new Vector2(600f, 40f), FontStyle.Italic);
            AddButton(box.transform, "CONTINUAR", new Color(0.2f, 0.6f, 0.3f), resume, -180f);
            AddButton(box.transform, "REINICIAR FASE", new Color(0.85f, 0.55f, 0.1f), restart, -290f);
            AddButton(box.transform, "SAIR", new Color(0.6f, 0.2f, 0.2f), quit, -400f);
        }

        public void HidePause()
        {
            if (pausePanel != null) Destroy(pausePanel);
        }

        public void ShowResult(string title, string message, string stats, Color color, Action restart, Action quit)
        {
            HidePause();
            foreach (var t in toasts) Destroy(t.rect.gameObject);
            toasts.Clear();
            if (resultPanel != null) Destroy(resultPanel);
            resultPanel = Dim("Resultado");
            var box = UIFactory.Box("Caixa", resultPanel.transform, PanelColor);
            box.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100f, 680f));
            AddText(box.transform, title, 84, color, new Vector2(0f, -30f), new Vector2(1060f, 110f));
            AddText(box.transform, message, 32, Color.white, new Vector2(0f, -160f), new Vector2(1000f, 120f), FontStyle.Normal);
            AddText(box.transform, stats, 26, Gray, new Vector2(0f, -300f), new Vector2(1000f, 120f), FontStyle.Normal);
            var a = UIFactory.Button("Reiniciar", box.transform, "TENTAR DE NOVO (R)",new Color(0.2f, 0.6f, 0.3f), () => restart(), 34);
            a.GetComponent<RectTransform>().Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-230f, -520f), new Vector2(400f, 90f));
            var q = UIFactory.Button("Sair", box.transform, "SAIR", new Color(0.6f, 0.2f, 0.2f), () => quit(), 34);
            q.GetComponent<RectTransform>().Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(230f, -520f), new Vector2(400f, 90f));
        }

        GameObject Dim(string name)
        {
            var dim = UIFactory.Box(name, PanelRoot, new Color(0f, 0f, 0f, 0.72f), false);
            dim.raycastTarget = true;
            dim.rectTransform.Stretch();
            dim.transform.SetAsLastSibling();
            return dim.gameObject;
        }

        static Text AddText(Transform parent, string text, int size, Color color, Vector2 pos, Vector2 sizeDelta, FontStyle style = FontStyle.Bold)
        {
            var l = UIFactory.Label("Texto", parent, text, size, color, TextAnchor.UpperCenter, style);
            l.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), pos, sizeDelta);
            return l;
        }

        static void AddButton(Transform parent, string text, Color color, Action onClick, float y)
        {
            var b = UIFactory.Button("Botao", parent, text, color, () => onClick(), 32);
            b.GetComponent<RectTransform>().Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(440f, 84f));
        }
    }
}
