using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ProvaImpossivel
{
    /// <summary>
    /// Interface inteira montada por código: tempo, nota/questões, flagrantes e favores, prompts, mensagens,
    /// balões de fala e telas de menu / dificuldade / pausa / resultado. O estado do professor e dos colegas NÃO fica
    /// aqui: aparece no mundo, no corpo deles (MoodIcon, cone de visão). Por isso o HUD só lê GameManager e ExamSystem.
    /// Visual (Bloco 16): "prova impressa" — tudo vem do <see cref="UITheme"/> pelos componentes de papel da UIFactory.
    /// </summary>
    public class HUD : MonoBehaviour
    {
        public static HUD I { get; private set; }

        // cores de SINAL (as de sempre): continuam valendo para o mundo, os balões e os desafios
        public static readonly Color Yellow = UITheme.SignalYellow;
        public static readonly Color Red = UITheme.SignalRed;
        public static readonly Color Green = UITheme.SignalGreen;
        public static readonly Color Orange = UITheme.SignalOrange;
        public static readonly Color Gray = UITheme.SignalGray;
        public static readonly Color PanelColor = UITheme.DarkPanel;

        /// <summary>Raiz para painéis de desafios (fica acima do HUD).</summary>
        public RectTransform PanelRoot { get; private set; }

        RectTransform root, hudRoot, bubbleRoot, toastRoot;
        Text timerText, gradeTitle, gradeText, gradeNote, strikesText, promptText, holdText, seatedHelp, tutorialText;
        Image tutorialBox, cheatsBox;
        Text cheatsHeader;
        Image timerBox, holdFill, flash, promptBox, seatedHelpBox;
        RectTransform gradePanel;
        Canvas hudCanvas;
        Image[] slots, knownFrames;
        Text[] slotNumbers;
        GameObject introPanel, pausePanel, resultPanel;
        float flashAlpha;
        Color flashColor;

        [Tooltip("Máximo de mensagens empilhadas no topo")] public int maxToasts = 2;
        [Tooltip("Máximo de balões de fala ao mesmo tempo")] public int maxBubbles = 3;

        class Toast_ { public RectTransform rect; public CanvasGroup group; public Text text; public float until; public bool important; public string raw; }
        readonly List<Toast_> toasts = new();

        class Bubble { public RectTransform rect; public Image box; public Text name, text; public Transform target; public float until, born; public int priority; }
        readonly List<Bubble> bubbles = new();

        // tamanhos fixos do HUD (referência 1920×1080)
        const float GradeW = 440f, GradeH = 196f, CheatsGap = 12f, CheatRowH = 34f;
        const int SizeTimer = 54, SizeGrade = 56;

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
            hudCanvas = canvas;
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            canvas.pixelPerfect = false;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            root = (RectTransform)go.transform;

            bubbleRoot = UIFactory.Rect("Baloes", root).Stretch();
            hudRoot = UIFactory.Rect("HUD", root).Stretch();
            PanelRoot = UIFactory.Rect("Paineis", root).Stretch();
            toastRoot = UIFactory.Rect("Mensagens", root).Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), ToastBase, new Vector2(1000f, 400f));
            flash = UIFactory.Box("Flash", root, UITheme.FlashRed, false);
            flash.rectTransform.Stretch();
        }

        void BuildHud()
        {
            // cronômetro: etiqueta de papel no topo
            timerBox = UIFactory.Paper("Tempo", hudRoot, margin: false);
            timerBox.rectTransform.Top(0f, -16f, 240f, 96f);
            var cap = UIFactory.Ink("Rotulo", timerBox.transform, Falas.HudTempo, UITheme.SizeSmall, UITheme.InkSoft);
            cap.rectTransform.Top(0f, -6f, 200f, 24f);
            timerText = UIFactory.Ink("Valor", timerBox.transform, "06:00", SizeTimer, UITheme.Ink);
            timerText.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 2f), new Vector2(230f, 64f));

            // nota: folha com o título datilografado, a nota "à caneta", as questões e os flagrantes
            var grade = UIFactory.Paper("Nota", hudRoot, margin: false);
            gradePanel = grade.rectTransform;
            grade.rectTransform.Place(new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-16f, -16f), new Vector2(GradeW, GradeH));
            gradeTitle = UIFactory.Ink("Titulo", grade.transform, Falas.HudNota, UITheme.SizeHeading - 6, UITheme.Ink, TextAnchor.UpperLeft);
            gradeTitle.rectTransform.Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -12f), new Vector2(150f, 34f));
            gradeText = UIFactory.Ink("Valor", grade.transform, "0,0 / 6,0", SizeGrade, UITheme.PenRed, TextAnchor.UpperRight, hand: true);
            gradeText.rectTransform.Place(new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-16f, -2f), new Vector2(260f, 60f));
            gradeNote = UIFactory.Ink("Anotacao", grade.transform, "", UITheme.SizeHandSmall, UITheme.PenRed, TextAnchor.UpperLeft, hand: true);
            gradeNote.rectTransform.Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -58f), new Vector2(GradeW - 32f, 34f));
            gradeNote.horizontalOverflow = HorizontalWrapMode.Overflow;
            slots = new Image[10];
            knownFrames = new Image[10];
            slotNumbers = new Text[10];
            for (int i = 0; i < slots.Length; i++)
            {
                // marca-texto amarelo (atrás da caixinha) = questão que o jogador sabe responder sozinho
                knownFrames[i] = UIFactory.Box("Sabe" + (i + 1), grade.transform, UITheme.Highlighter, false);
                knownFrames[i].rectTransform.Place(Vector2.zero, Vector2.zero, new Vector2(14f + i * 41f, 42f), new Vector2(40f, 40f));
                knownFrames[i].enabled = false;
                slots[i] = UIFactory.Keycap("Q" + (i + 1), grade.transform, (i + 1).ToString(), 22);
                slots[i].rectTransform.Place(Vector2.zero, Vector2.zero, new Vector2(18f + i * 41f, 46f), new Vector2(32f, 32f));
                slotNumbers[i] = slots[i].GetComponentInChildren<Text>();
            }
            strikesText = UIFactory.Ink("Flagrantes", grade.transform, "", UITheme.SizeSmall, UITheme.Ink, TextAnchor.LowerLeft);
            strikesText.rectTransform.Place(Vector2.zero, Vector2.zero, new Vector2(16f, 8f), new Vector2(GradeW - 32f, 30f));
            strikesText.horizontalOverflow = HorizontalWrapMode.Wrap; // nunca transborda o painel da NOTA
            strikesText.verticalOverflow = VerticalWrapMode.Truncate;
            strikesText.resizeTextForBestFit = true;
            strikesText.resizeTextMinSize = 15;
            strikesText.resizeTextMaxSize = UITheme.SizeSmall;

            // colas de bolso (Bloco 15): folha logo abaixo da NOTA, uma linha por tecla (etiqueta + texto)
            cheatsBox = UIFactory.Paper("Colas", hudRoot, margin: false);
            cheatsBox.rectTransform.Place(new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-16f, -16f - GradeH - CheatsGap),
                                          new Vector2(GradeW, 44f + CheatRowH * 4f + 10f));
            cheatsHeader = UIFactory.Ink("Cabecalho", cheatsBox.transform, "", UITheme.SizeSmall, UITheme.Ink, TextAnchor.MiddleLeft);
            cheatsHeader.rectTransform.Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -8f), new Vector2(GradeW - 32f, 32f));
            cheatsHeader.horizontalOverflow = HorizontalWrapMode.Wrap;
            cheatsHeader.verticalOverflow = VerticalWrapMode.Truncate;
            UIFactory.Line("Regua", cheatsBox.transform, UITheme.Rule, 0f, -42f, GradeW - 24f);
            cheatsBox.gameObject.SetActive(false);

            // prompt de interação + barra de segurar
            promptBox = UIFactory.Paper("Prompt", hudRoot, margin: false);
            promptBox.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(820f, 58f));
            promptText = UIFactory.Ink("Texto", promptBox.transform, "", UITheme.SizeBody + 1, UITheme.Ink);
            promptText.rectTransform.Stretch(6f);
            promptBox.gameObject.SetActive(false);

            var holdBack = UIFactory.Paper("Segurar", hudRoot, margin: false);
            holdBack.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 104f), new Vector2(640f, 36f));
            holdFill = UIFactory.Box("Fill", holdBack.transform, UITheme.Pen, false);
            holdFill.rectTransform.Stretch(4f);
            holdFill.type = Image.Type.Filled;
            holdFill.fillMethod = Image.FillMethod.Horizontal;
            holdFill.sprite = UIFactory.WhiteSprite;
            holdFill.fillAmount = 0f;
            holdText = UIFactory.Ink("Texto", holdBack.transform, "", UITheme.SizeSmall, UITheme.Ink);
            holdText.rectTransform.Stretch();
            holdBack.gameObject.SetActive(false);

            // linha de controles: tira de papel do tamanho do texto, teclas em caneta azul
            seatedHelpBox = UIFactory.Paper("AjudaFundo", hudRoot, margin: false);
            seatedHelpBox.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(600f, 44f));
            seatedHelpBox.gameObject.SetActive(false);
            seatedHelp = UIFactory.Ink("Ajuda", seatedHelpBox.transform, "", UITheme.SizeSmall + 1, UITheme.Ink);
            seatedHelp.rectTransform.Stretch();
            seatedHelp.horizontalOverflow = HorizontalWrapMode.Overflow;

            // passo do tutorial (só no treino): bilhete à mão, acima do prompt
            tutorialBox = UIFactory.Paper("Tutorial", hudRoot, margin: false);
            tutorialBox.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 218f), new Vector2(1250f, 60f));
            var treino = UIFactory.Stamp("Carimbo", tutorialBox.transform, Falas.HudTreino, UITheme.StampBlue, UITheme.SizeSmall, -4f);
            treino.Place(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(14f, 0f), treino.sizeDelta);
            tutorialText = UIFactory.Ink("Texto", tutorialBox.transform, "", UITheme.SizeHandSmall + 2, UITheme.Pen, TextAnchor.MiddleLeft, hand: true);
            tutorialText.rectTransform.Stretch(8f);
            tutorialText.rectTransform.offsetMin = new Vector2(treino.sizeDelta.x + 30f, 4f);
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
            UpdateIntroKeys();
            UpdateToasts();
            UpdateFlash();
        }

        void LateUpdate()
        {
            UpdateBubbles();
            // os ícones do mundo informados neste frame valem para o próximo (a ordem dos LateUpdate não é garantida)
            (iconRects, iconRectsNext) = (iconRectsNext, iconRects);
            iconRectsNext.Clear();
        }

        // ------------------------------------------------------------------ áreas da tela

        // ícones de humor do mundo (MoodIcon) em coordenadas de tela: o HUD não lê o professor; quem desenha avisa
        List<Rect> iconRects = new(), iconRectsNext = new();

        /// <summary>Um ícone do mundo (ex.: o "?" do professor) ocupa esta área da tela neste frame (MoodIcon chama).</summary>
        public void ReportWorldIcon(Rect screenRect) => iconRectsNext.Add(screenRect);

        Camera CanvasCamera => hudCanvas != null && hudCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? hudCanvas.worldCamera : null;

        /// <summary>Retângulo de um elemento da UI no espaço local de <paramref name="space"/>.</summary>
        static Rect LocalRect(RectTransform r, RectTransform space)
        {
            var c = new Vector3[4];
            r.GetWorldCorners(c);
            Vector2 a = space.InverseTransformPoint(c[0]), b = space.InverseTransformPoint(c[2]);
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }

        /// <summary>Áreas ocupadas pelo HUD (tempo, NOTA, mensagens, prompt, barra, ajuda, tutorial), onde balão não entra.</summary>
        readonly List<Rect> hudAreas = new();
        void CollectHudAreas()
        {
            hudAreas.Clear();
            if (!hudRoot.gameObject.activeInHierarchy) return;
            void Add(RectTransform r) { if (r != null && r.gameObject.activeInHierarchy) hudAreas.Add(LocalRect(r, bubbleRoot)); }
            Add(timerBox.rectTransform);
            Add(gradePanel);
            Add(promptBox.rectTransform);
            Add((RectTransform)holdFill.transform.parent);
            Add(seatedHelpBox.rectTransform);
            Add(tutorialBox.rectTransform);
            Add(cheatsBox.rectTransform);
            foreach (var t in toasts) Add(t.rect);
        }

        /// <summary>Algum balão de fala visível está em cima de uma área do HUD? (testes)</summary>
        public bool AnyBubbleOverlapsHud()
        {
            CollectHudAreas();
            foreach (var b in bubbles)
            {
                if (!b.rect.gameObject.activeSelf) continue;
                var r = LocalRect(b.rect, bubbleRoot);
                foreach (var a in hudAreas) if (r.Overlaps(a)) return true;
            }
            return false;
        }

        /// <summary>Retângulos (no espaço do canvas) de todos os painéis do HUD visíveis agora (testes de layout).</summary>
        public List<Rect> HudPanelRects()
        {
            var list = new List<Rect>();
            foreach (var r in new[] { timerBox.rectTransform, gradePanel, cheatsBox.rectTransform, promptBox.rectTransform,
                                      (RectTransform)holdFill.transform.parent, seatedHelpBox.rectTransform, tutorialBox.rectTransform })
                if (r.gameObject.activeInHierarchy) list.Add(LocalRect(r, root));
            return list;
        }

        /// <summary>Tamanho do canvas (unidades de UI) — o que cabe na tela.</summary>
        public Rect CanvasRect => root.rect;

        void RefreshHud(GameManager gm)
        {
            float left = gm.TimeLeft;
            timerText.text = FormatTime(left);
            bool urgent = left <= 60f;
            timerText.color = urgent && Mathf.Repeat(Time.time, 1f) < 0.5f ? UITheme.StampRed : UITheme.Ink;

            var exam = ExamSystem.I;
            float min = gm.Settings.minGrade;
            if (exam != null)
            {
                float g = exam.Grade;
                bool passa = g >= min;
                gradeText.text = GameManager.Fmt(g) + " / " + GameManager.Fmt(min);
                gradeText.color = passa ? UITheme.InkGreen : UITheme.PenRed;
                for (int i = 0; i < slots.Length && i < exam.Questions.Length; i++)
                {
                    var q = exam.Questions[i];
                    slots[i].color = q switch
                    {
                        QuestionState.Certa => UITheme.InkGreen,
                        QuestionState.Errada => UITheme.StampRed,
                        _ => UITheme.Paper,
                    };
                    slotNumbers[i].color = q == QuestionState.EmBranco ? UITheme.Ink : UITheme.Paper;
                    knownFrames[i].enabled = exam.Known[i];
                }
                // a anotação à mão faz o papel do antigo painel de objetivo
                gradeNote.text = passa ? Falas.HudNotaJaDa : string.Format(Falas.HudNotaFaltam, GameManager.Fmt(min - g));
                gradeNote.color = passa ? UITheme.InkGreen : UITheme.PenRed;
            }
            strikesText.text = StrikesRich(StrikesLabel(gm.Flagrantes, gm.Settings.maxFlagrantes, gm.Favors));
        }

        /// <summary>Acima disto, a linha de flagrantes mostra o número em vez das caixinhas (o treino tem limite 99).</summary>
        public const int MaxStrikeBoxes = 5;

        /// <summary>
        /// Texto da linha "Flagrantes ... Favores" do painel da NOTA. Pura (testável): até <see cref="MaxStrikeBoxes"/>
        /// desenha uma caixinha por flagrante permitido; acima disso, só "n/max" — nunca transborda o painel.
        /// </summary>
        public static string StrikesLabel(int flagrantes, int max, int favors)
        {
            string f;
            if (max > MaxStrikeBoxes) f = "Flagrantes: " + flagrantes;
            else
            {
                var s = new System.Text.StringBuilder("Flagrantes: ");
                for (int i = 0; i < max; i++) s.Append(i < flagrantes ? "[X]" : "[  ]");
                f = s.ToString();
            }
            return f + "    Favores: " + favors;
        }

        /// <summary>Linha de flagrantes no papel: os X viram tinta vermelha, as caixinhas vazias ficam estreitas.</summary>
        public static string StrikesRich(string label) =>
            label.Replace("[X]", "[<color=#" + ColorUtility.ToHtmlStringRGB(UITheme.StampRed) + ">X</color>]")
                 .Replace("[  ]", "[ ]").Replace("    ", "   ");

        /// <summary>Teclas "[1]", "[Espaço]"... de uma linha de ajuda em caneta azul (rich text).</summary>
        public static string KeysInPen(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            string pen = "<color=#" + ColorUtility.ToHtmlStringRGB(UITheme.Pen) + ">";
            var sb = new System.Text.StringBuilder(text.Length + 32);
            for (int i = 0; i < text.Length; i++)
            {
                int end = text[i] == '[' ? text.IndexOf(']', i) : -1;
                if (end > i && end - i <= 10) { sb.Append(pen).Append(text, i, end - i + 1).Append("</color>"); i = end; }
                else sb.Append(text[i]);
            }
            return sb.ToString();
        }

        public static string FormatTime(float seconds)
        {
            seconds = Mathf.Max(0f, seconds);
            int m = (int)(seconds / 60f), s = (int)(seconds % 60f);
            return m.ToString("00") + ":" + s.ToString("00");
        }

        // =================================================================== prompts

        string promptRaw;

        public void SetPrompt(string text)
        {
            bool on = !string.IsNullOrEmpty(text);
            if (promptBox.gameObject.activeSelf != on) promptBox.gameObject.SetActive(on);
            if (!on || text == promptRaw) return; // todo quadro: só mede de novo quando o texto muda
            promptRaw = text;
            promptText.text = KeysInPen(text);
            // a caixa acompanha o texto: mais larga (até 1100) e, se ainda quebrar, mais alta (nunca menor que antes)
            var pr = promptBox.rectTransform;
            pr.sizeDelta = new Vector2(Mathf.Clamp(promptText.preferredWidth + 40f, 820f, 1100f), 58f);
            pr.sizeDelta = new Vector2(pr.sizeDelta.x, Mathf.Max(58f, promptText.preferredHeight + 16f));
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
            // ação arriscada sendo vista = vermelho; arriscada escondida = verde; segura = caneta azul.
            // Meio transparente: o texto a tinta continua legível por cima.
            holdFill.color = HoldColor(risk);
        }

        /// <summary>Cor do preenchimento da barra de segurar (meio transparente sobre o papel).</summary>
        public static Color HoldColor(HoldRisk risk)
        {
            Color c = risk == HoldRisk.Visto ? UITheme.StampRed : risk == HoldRisk.Escondido ? UITheme.InkGreen : UITheme.Pen;
            c.a = 0.45f;
            return c;
        }

        public void SetSeatedHelp(string text)
        {
            bool on = !string.IsNullOrEmpty(text);
            seatedHelp.text = on ? KeysInPen(text) : "";
            if (seatedHelpBox.gameObject.activeSelf != on) seatedHelpBox.gameObject.SetActive(on);
            // sem quebra de linha: a caixa vai até quase a largura da tela (1300 cortava as ajudas mais longas)
            if (on) seatedHelpBox.rectTransform.sizeDelta = new Vector2(Mathf.Min(1840f, seatedHelp.preferredWidth + 40f), 44f);
        }

        // ------------------------------------------------------------------ colas de bolso

        class CheatRow { public RectTransform rect; public Image key; public Text keyText, text; }
        readonly List<CheatRow> cheatRows = new();
        string cheatsRaw;

        /// <summary>Painel das colas novas (Bloco 15): teclas, cargas e o que está a caminho (null esconde).</summary>
        public void SetCheats(string text)
        {
            bool on = !string.IsNullOrEmpty(text);
            if (cheatsBox.gameObject.activeSelf != on) cheatsBox.gameObject.SetActive(on);
            if (!on || cheatsRaw == text) return;
            cheatsRaw = text;
            var lines = text.Split('\n');
            cheatsHeader.text = lines[0];
            int n = 0;
            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrEmpty(lines[i])) continue;
                var row = CheatRowAt(n++);
                SplitKey(lines[i], out string key, out string rest);
                row.key.gameObject.SetActive(key != null);
                if (key != null) row.keyText.text = key;
                row.text.text = rest;
                row.text.color = rest.Contains("!") ? UITheme.StampRed : rest.Contains("confiscad") || rest.Contains("sem celular") ? UITheme.InkFaint : UITheme.Ink;
                row.rect.gameObject.SetActive(true);
            }
            for (int i = n; i < cheatRows.Count; i++) cheatRows[i].rect.gameObject.SetActive(false);
        }

        CheatRow CheatRowAt(int i)
        {
            while (cheatRows.Count <= i)
            {
                int k = cheatRows.Count;
                var r = UIFactory.Rect("Linha" + (k + 1), cheatsBox.transform);
                r.Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -50f - k * CheatRowH), new Vector2(GradeW - 32f, CheatRowH - 4f));
                var key = UIFactory.Keycap("Tecla", r, "Q", 22);
                key.rectTransform.Place(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(30f, 30f));
                var t = UIFactory.Ink("Texto", r, "", UITheme.SizeSmall - 1, UITheme.Ink, TextAnchor.MiddleLeft);
                t.rectTransform.Stretch();
                t.rectTransform.offsetMin = new Vector2(42f, 0f);
                t.horizontalOverflow = HorizontalWrapMode.Wrap;
                t.verticalOverflow = VerticalWrapMode.Truncate;
                t.resizeTextForBestFit = true;
                t.resizeTextMinSize = 14;
                t.resizeTextMaxSize = UITheme.SizeSmall - 1;
                cheatRows.Add(new CheatRow { rect = r, key = key, keyText = key.GetComponentInChildren<Text>(), text = t });
            }
            return cheatRows[i];
        }

        /// <summary>"[Q] Colinha ×2" → tecla "Q" e texto "Colinha ×2". Sem "[x]" no começo, a tecla é null. Pura.</summary>
        public static void SplitKey(string line, out string key, out string rest)
        {
            key = null;
            rest = line ?? "";
            int end = rest.StartsWith("[") ? rest.IndexOf(']') : -1;
            if (end < 2 || end > 8) return;
            key = rest.Substring(1, end - 1);
            rest = rest.Substring(end + 1).TrimStart();
        }

        /// <summary>Teclas visíveis no painel das colas, na ordem (testes).</summary>
        public List<string> CheatKeys()
        {
            var keys = new List<string>();
            foreach (var r in cheatRows)
                if (r.rect.gameObject.activeSelf && r.key.gameObject.activeSelf) keys.Add(r.keyText.text);
            return keys;
        }

        /// <summary>Todos os textos do painel das colas cabem na largura e na altura do painel? (testes)</summary>
        public bool CheatsTextFits()
        {
            if (cheatsHeader.preferredWidth > cheatsHeader.rectTransform.rect.width + 1f) return false;
            foreach (var r in cheatRows)
            {
                if (!r.rect.gameObject.activeSelf) continue;
                var gen = r.text.cachedTextGenerator;
                if (gen.lineCount > 1 || LocalRect(r.rect, cheatsBox.rectTransform).yMin < cheatsBox.rectTransform.rect.yMin) return false;
            }
            return true;
        }

        /// <summary>Área do painel das colas e do painel da NOTA (testes de sobreposição).</summary>
        public RectTransform CheatsPanel => cheatsBox.rectTransform;
        public RectTransform GradePanel => gradePanel;

        /// <summary>Passo atual do tutorial (null esconde).</summary>
        public void SetTutorial(string text)
        {
            bool on = !string.IsNullOrEmpty(text);
            if (tutorialBox.gameObject.activeSelf != on) tutorialBox.gameObject.SetActive(on);
            if (!on) return;
            tutorialText.text = text;
            // texto longo (passo + "[T] pular tutorial") quebra em duas linhas: a folha cresce para cima em vez de cortar
            var tr = tutorialBox.rectTransform;
            tr.sizeDelta = new Vector2(tr.sizeDelta.x, Mathf.Max(60f, tutorialText.preferredHeight + 18f));
        }

        // =================================================================== mensagens

        /// <param name="color">Cor de sinal (HUD.Yellow, HUD.Red...): no papel vira a tinta equivalente.</param>
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
            // Entrega 1.0: mensagem só informativa (cinza: "favor não gasto", "bateria acabou") nunca tira outra da tela
            if (IsInformative(color, important) && toasts.Count >= maxToasts) return;
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

            // tira de papel; as importantes ganham moldura vermelha
            var box = UIFactory.Paper("Mensagem", toastRoot, margin: false);
            var group = box.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            if (important)
            {
                UIFactory.Frame("Moldura", box.transform, UITheme.StampRed).rectTransform.Stretch(4f);
            }
            var t = UIFactory.Ink("Texto", box.transform, text, important ? UITheme.SizeBody : UITheme.SizeSmall + 1, UITheme.OnPaper(color));
            box.rectTransform.anchorMin = box.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            box.rectTransform.pivot = new Vector2(0.5f, 1f);
            box.rectTransform.sizeDelta = new Vector2(980f, 10f);
            t.rectTransform.Stretch(14f);
            float h = Mathf.Max(52f, t.preferredHeight + 26f);
            box.rectTransform.sizeDelta = new Vector2(980f, h);
            toasts.Insert(0, new Toast_ { rect = box.rectTransform, group = group, text = t, until = Time.unscaledTime + seconds, important = important, raw = text });
        }

        /// <summary>Mensagem de menor prioridade: cinza (recusa / informação) e não importante. Pura (testes).</summary>
        public static bool IsInformative(Color color, bool important) => !important && color == Gray;

        /// <summary>Textos das mensagens na tela agora, da mais nova à mais antiga (testes).</summary>
        public List<string> ToastTexts() => toasts.ConvertAll(t => t.raw);

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
            float largura = 0f;
            foreach (var t in toasts)
            {
                t.group.alpha = Mathf.Clamp01((t.until - Time.unscaledTime) / 0.4f);
                t.rect.anchoredPosition = Vector2.Lerp(t.rect.anchoredPosition, new Vector2(0f, -y), Time.unscaledDeltaTime * 12f);
                y += t.rect.sizeDelta.y + 8f;
                largura = Mathf.Max(largura, t.rect.sizeDelta.x);
            }
            // as mensagens não podem cobrir o "?" do professor (nem outro ícone do mundo): se cobrirem, saem do caminho
            Vector2 alvo = ToastOffset(largura, y);
            toastRoot.anchoredPosition = Vector2.Lerp(toastRoot.anchoredPosition, ToastBase + alvo, Time.unscaledDeltaTime * 10f);
        }

        static readonly Vector2 ToastBase = new(0f, -124f);

        /// <summary>Quanto a pilha de mensagens está deslocada da posição normal agora (testes).</summary>
        public Vector2 ToastStackOffset => toastRoot.anchoredPosition - ToastBase;

        /// <summary>
        /// Deslocamento da pilha de mensagens (largura × altura, presa no topo) para não cobrir nenhum ícone do mundo:
        /// tenta para o lado oposto ao ícone; se não couber na tela, desce para logo abaixo do ícone.
        /// </summary>
        Vector2 ToastOffset(float largura, float altura)
        {
            if (largura <= 0f || iconRects.Count == 0) return Vector2.zero;
            var cam = CanvasCamera;
            float topo = root.rect.yMax + ToastBase.y;
            Rect pilha = new(-largura * 0.5f, topo - altura, largura, altura);
            float meia = root.rect.width * 0.5f;
            Vector2 off = Vector2.zero;
            foreach (var s in iconRects)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(root, s.min, cam, out var a);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(root, s.max, cam, out var b);
                Rect icone = Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
                Rect atual = new(pilha.x + off.x, pilha.y + off.y, pilha.width, pilha.height);
                if (!atual.Overlaps(icone)) continue;
                float esq = atual.xMax - icone.xMin + 12f, dir = icone.xMax - atual.xMin + 12f;
                bool vaiEsq = icone.center.x >= atual.center.x;
                float dx = vaiEsq ? -esq : dir;
                Rect deLado = new(atual.x + dx, atual.y, atual.width, atual.height);
                // ao lado só se couber na tela E não invadir os painéis do topo (tempo, NOTA)
                if (Mathf.Abs(off.x + dx) + largura * 0.5f <= meia - 16f && !InvadePainel(deLado)) off.x += dx;
                else off.y -= atual.yMax - icone.yMin + 12f; // não cabe ao lado: desce para baixo do ícone
            }
            return off;
        }

        /// <summary>Um retângulo (no espaço do <c>root</c>) cobre o painel do tempo ou o da NOTA?</summary>
        bool InvadePainel(Rect r)
        {
            foreach (var p in new[] { gradePanel, timerBox.rectTransform, cheatsBox.rectTransform })
                if (p != null && p.gameObject.activeInHierarchy && r.Overlaps(LocalRect(p, root))) return true;
            return false;
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

        // =================================================================== balões de fala (iguais a antes)

        /// <param name="priority">Quando há balões demais, os de menor prioridade somem primeiro (professor = 2).</param>
        public void Say(Transform target, string speaker, string text, float seconds, Color nameColor, int priority = 0)
        {
            if (target == null || string.IsNullOrEmpty(text)) return;
            var b = bubbles.Find(x => x.target == target);
            if (b == null)
            {
                b = new Bubble { target = target };
                b.box = UIFactory.Box("Balao", bubbleRoot, UITheme.BubblePaper);
                b.rect = b.box.rectTransform;
                b.rect.anchorMin = b.rect.anchorMax = new Vector2(0.5f, 0.5f);
                b.rect.pivot = new Vector2(0.5f, 0f);
                b.name = UIFactory.Label("Nome", b.rect, "", 17, nameColor, TextAnchor.UpperLeft);
                b.name.rectTransform.Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(12f, -6f), new Vector2(360f, 22f));
                var sh = b.name.GetComponent<Shadow>();
                sh.effectColor = UITheme.BubbleNameShadow;
                b.text = UIFactory.Label("Fala", b.rect, "", 22, UITheme.BubbleInk, TextAnchor.UpperLeft, FontStyle.Normal);
                Destroy(b.text.GetComponent<Shadow>());
                bubbles.Add(b);
            }
            b.name.text = speaker;
            b.name.color = nameColor;
            b.text.text = text;
            float w = Mathf.Clamp(text.Length * 11f + 40f, 160f, 400f);
            // fala curta com nome comprido ("Hm." do PROF. TAKANERVOSO): o nome saía do balão. O balão tem pelo menos
            // a largura do nome (até o máximo), e o nome ocupa a largura do balão.
            if (!string.IsNullOrEmpty(speaker)) w = Mathf.Clamp(Mathf.Max(w, b.name.preferredWidth + 28f), 160f, 400f);
            b.name.rectTransform.sizeDelta = new Vector2(w - 24f, 22f);
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
            CollectHudAreas();
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
                RectTransformUtility.ScreenPointToLocalPointInRectangle(bubbleRoot, sp, CanvasCamera, out var lp);
                float dist = Vector3.Distance(cam.transform.position, world);
                float scale = Mathf.Clamp(1.25f - dist * 0.04f, 0.75f, 1f);
                b.rect.localScale = Vector3.one * scale;
                b.rect.anchoredPosition = KeepInSafeArea(lp, b.rect.sizeDelta * scale, half);
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

        /// <summary>
        /// Posição (pé do balão, centro em x) dentro da tela e fora das áreas do HUD. Se cair em cima de um painel,
        /// o balão sai pelo lado livre mais perto (para baixo de um painel do topo, para cima de um de baixo) — nunca
        /// fica embaixo do painel.
        /// </summary>
        Vector2 KeepInSafeArea(Vector2 lp, Vector2 size, Vector2 half)
        {
            const float m = 10f;
            float w = size.x * 0.5f, h = size.y;
            for (int pass = 0; pass < 3; pass++)
            {
                lp.x = Mathf.Clamp(lp.x, -half.x + w + m, half.x - w - m);
                lp.y = Mathf.Clamp(lp.y, -half.y + m, half.y - h - m);
                bool moved = false;
                foreach (var a in hudAreas)
                {
                    var r = new Rect(lp.x - w, lp.y, size.x, h);
                    if (!r.Overlaps(a)) continue;
                    // empurra pelo menor caminho: para os lados ou para fora do painel (de cima pra baixo, de baixo pra cima)
                    float paraBaixo = r.yMax - (a.yMin - 6f), paraCima = (a.yMax + 6f) - r.yMin;
                    float paraEsq = r.xMax - (a.xMin - 6f), paraDir = (a.xMax + 6f) - r.xMin;
                    float vertical = a.center.y > 0f ? paraBaixo : paraCima;
                    float lateral = Mathf.Min(paraEsq, paraDir);
                    if (lateral < vertical && lp.x - w - (paraEsq <= paraDir ? paraEsq : -paraDir) > -half.x + m
                        && lp.x + w + (paraEsq <= paraDir ? -paraEsq : paraDir) < half.x - m)
                        lp.x += paraEsq <= paraDir ? -paraEsq : paraDir;
                    else lp.y += a.center.y > 0f ? -paraBaixo : paraCima;
                    moved = true;
                }
                if (!moved) break;
            }
            return lp;
        }

        // =================================================================== telas

        // menu: duas folhas dentro do mesmo painel (capa e escolha da dificuldade)
        GameObject menuPage, difficultyPage;
        int pageChangedFrame = -1;

        /// <summary>A folha de escolha da dificuldade está aberta? (testes)</summary>
        public bool DifficultyPageOpen => difficultyPage != null && difficultyPage.activeInHierarchy;

        /// <param name="firstTime">Primeira vez: escolher a dificuldade leva ao treino, e aparece "Pular tutorial".</param>
        public void ShowIntro(Action<Difficulty> onChoose, Action onTutorial = null, Action onSkipTutorial = null, bool firstTime = false)
        {
            HideIntro();
            introPanel = Dim("Introducao");
            menuPage = BuildMenuPage(onTutorial, onSkipTutorial, firstTime);
            difficultyPage = BuildDifficultyPage(onChoose);
            difficultyPage.SetActive(false);
        }

        /// <summary>Capa: título, recado do professor, JOGAR / TUTORIAL / SAIR.</summary>
        GameObject BuildMenuPage(Action onTutorial, Action onSkipTutorial, bool firstTime)
        {
            var box = UIFactory.Paper("Caixa", introPanel.transform, margin: true, header: true);
            box.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1180f, 900f));
            var t = box.transform;
            const float cx = 40f; // centro do texto: à direita da margem
            var title = AddInk(t, Falas.JogoTitulo, UITheme.SizeHuge - 6, UITheme.Ink, new Vector2(cx, -70f), new Vector2(1040f, 96f));
            title.horizontalOverflow = HorizontalWrapMode.Overflow;
            UIFactory.Line("Sublinhado", t, UITheme.Ink, cx, -170f, 900f, 3f);
            AddInk(t, Falas.JogoSubtitulo, UITheme.SizeHeading, UITheme.InkSoft, new Vector2(cx, -188f), new Vector2(1040f, 44f));
            AddInk(t, Falas.MenuHistoria, UITheme.SizeSmall, UITheme.Ink, new Vector2(cx, -262f), new Vector2(1040f, 110f));

            // recado à mão do professor, em vermelho, torto
            var recado = AddInk(t, Falas.MenuRecadoProfessor, UITheme.SizeHand, UITheme.PenRed, new Vector2(350f, -400f), new Vector2(330f, 90f), hand: true);
            recado.alignment = TextAnchor.UpperLeft;
            recado.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 4f);

            var jogar = UIFactory.PaperButton("BotaoJOGAR", t, Falas.BotaoJogar, ShowDifficultyPage, UITheme.SizeHeading, checkbox: true);
            jogar.GetComponent<RectTransform>().Top(-150f, -400f, 440f, 84f);
            Text dica = null;
            if (onTutorial != null)
            {
                var tut = UIFactory.PaperButton("BotaoTUTORIAL", t, firstTime ? Falas.BotaoTutorialPrimeira : Falas.BotaoTutorialRever,
                                                () => onTutorial(), UITheme.SizeButton - 4, checkbox: true);
                tut.GetComponent<RectTransform>().Top(-150f, -504f, 440f, 84f);
                if (firstTime && onSkipTutorial != null)
                {
                    GameObject skip = null;
                    skip = UIFactory.PaperButton("BotaoPULAR", t, Falas.BotaoPularTutorial, () =>
                    {
                        onSkipTutorial();
                        if (skip != null) skip.SetActive(false);
                        tut.GetComponentInChildren<PaperButtonFeel>().label.text = Falas.BotaoTutorialRever;
                        if (dica != null) dica.text = Falas.MenuDica;
                    }, UITheme.SizeSmall).gameObject;
                    skip.GetComponent<RectTransform>().Top(250f, -514f, 280f, 64f);
                }
            }
            var sair = UIFactory.PaperButton("BotaoSAIR", t, Falas.BotaoSair, () => GameManager.I?.Quit(), UITheme.SizeButton, checkbox: true);
            sair.GetComponent<RectTransform>().Top(-150f, -608f, 440f, 84f);

            dica = AddInk(t, firstTime ? Falas.MenuDicaPrimeiraVez : Falas.MenuDica, UITheme.SizeHandSmall, UITheme.Pen,
                          new Vector2(cx, -716f), new Vector2(1000f, 40f), hand: true);
            AddInk(t, Falas.MenuAtalhos, UITheme.SizeSmall, UITheme.InkSoft, new Vector2(-140f, -790f), new Vector2(720f, 30f));
            // rodapé com a versão e o estúdio, no canto de baixo à esquerda (o carimbo fica à direita)
            var rodape = UIFactory.Ink("Rodape", t, Falas.MenuRodape(Application.version), UITheme.SizeSmall, UITheme.InkSoft, TextAnchor.LowerLeft);
            rodape.rectTransform.Place(new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(UITheme.MarginX + 24f, 22f), new Vector2(520f, 32f));
            var carimbo = UIFactory.Stamp("Carimbo", t, Falas.MenuCarimbo, UITheme.StampRed, UITheme.SizeHeading + 4);
            carimbo.Place(new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-40f, 34f), carimbo.sizeDelta);
            return box.gameObject;
        }

        /// <summary>Folha de prova: as 3 dificuldades (tempo e nota das DifficultySettings) e as instruções.</summary>
        GameObject BuildDifficultyPage(Action<Difficulty> onChoose)
        {
            var box = UIFactory.Paper("Folha", introPanel.transform, margin: true, header: true);
            box.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1240f, 920f));
            var t = box.transform;
            const float cx = 40f;
            AddInk(t, Falas.DificuldadeTitulo, UITheme.SizeHeading + 4, UITheme.Ink, new Vector2(cx, -62f), new Vector2(1100f, 50f));
            AddInk(t, Falas.DificuldadeEnunciado, UITheme.SizeHand, UITheme.Pen, new Vector2(cx, -116f), new Vector2(1100f, 44f), hand: true);

            string[] names = { "FÁCIL", "NORMAL", "DIFÍCIL" };
            string[] frases = { Falas.DificuldadeFacilFrase, Falas.DificuldadeNormalFrase, Falas.DificuldadeDificilFrase };
            // tempo e nota vêm das DifficultySettings (nunca digitados aqui)
            var gm = GameManager.I;
            DifficultySettings[] sets = { gm != null ? gm.facil : null, gm != null ? gm.normal : null, gm != null ? gm.dificil : null };
            for (int i = 0; i < 3; i++)
            {
                var d = (Difficulty)i;
                float y = -180f - i * 118f;
                var b = UIFactory.PaperButton("Botao" + names[i], t, (i + 1) + ") " + names[i], () => onChoose(d), UITheme.SizeButton, checkbox: true);
                b.GetComponent<RectTransform>().Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(UITheme.MarginX + 40f, y), new Vector2(380f, 84f));
                var dados = UIFactory.Ink("Dados" + names[i], t, DifficultyData(sets[i]), UITheme.SizeBody, UITheme.Ink, TextAnchor.UpperLeft);
                dados.rectTransform.Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(UITheme.MarginX + 450f, y - 4f), new Vector2(660f, 34f));
                var frase = UIFactory.Ink("Frase" + names[i], t, frases[i], UITheme.SizeHandSmall + 2, UITheme.InkSoft, TextAnchor.UpperLeft, hand: true);
                frase.rectTransform.Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(UITheme.MarginX + 450f, y - 40f), new Vector2(660f, 44f));
            }

            UIFactory.Line("Regua", t, UITheme.Rule, cx, -540f, 1060f);
            AddInk(t, Falas.InstrucoesTitulo, UITheme.SizeBody, UITheme.Ink, new Vector2(cx, -560f), new Vector2(1060f, 34f));
            var instr = AddInk(t, Falas.InstrucoesControles, UITheme.SizeBody, UITheme.Ink, new Vector2(cx, -600f), new Vector2(1060f, 220f));
            instr.lineSpacing = 1.3f;

            var voltar = UIFactory.PaperButton("BotaoVOLTAR", t, Falas.BotaoVoltar, ShowMenuPage, UITheme.SizeSmall + 2);
            voltar.GetComponent<RectTransform>().Place(new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(UITheme.MarginX + 40f, 30f), new Vector2(300f, 64f));
            return box.gameObject;
        }

        /// <summary>"8 min • nota mínima 6,0" a partir da dificuldade (pura: testável).</summary>
        public static string DifficultyData(DifficultySettings s) =>
            s == null ? "" : string.Format(Falas.DificuldadeDados, Mathf.RoundToInt(s.duration / 60f), GameManager.Fmt(s.minGrade));

        /// <summary>Abre a folha da dificuldade (botão JOGAR).</summary>
        public void ShowDifficultyPage()
        {
            if (menuPage == null || difficultyPage == null || pageChangedFrame == Time.frameCount) return;
            pageChangedFrame = Time.frameCount;
            menuPage.SetActive(false);
            difficultyPage.SetActive(true);
            SoundFX.Play(Sfx.Clique);
        }

        /// <summary>Volta para a capa (botão VOLTAR ou Esc).</summary>
        public void ShowMenuPage()
        {
            if (menuPage == null || difficultyPage == null || pageChangedFrame == Time.frameCount) return;
            pageChangedFrame = Time.frameCount;
            difficultyPage.SetActive(false);
            menuPage.SetActive(true);
            SoundFX.Play(Sfx.Clique);
        }

        void UpdateIntroKeys()
        {
            // S7: Esc no teclado, B no controle
            if (DifficultyPageOpen && pageChangedFrame != Time.frameCount && Controls.Pressed(Acao.Voltar)) ShowMenuPage();
        }

        public void HideIntro()
        {
            if (introPanel != null) Destroy(introPanel);
            introPanel = menuPage = difficultyPage = null;
        }

        public void ShowPause(Action resume, Action restart, Action quit)
        {
            HidePause();
            pausePanel = Dim("Pausa");
            var box = UIFactory.Paper("Caixa", pausePanel.transform, margin: true);
            box.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(780f, 950f));
            // a caixa da pausa alcança a faixa das mensagens do topo: elas somem enquanto a pausa está aberta
            toastRoot.gameObject.SetActive(false);
            const float cx = 40f;
            AddInk(box.transform, Falas.PausaTitulo, UITheme.SizeTitle, UITheme.Ink, new Vector2(cx, -30f), new Vector2(660f, 74f));
            AddInk(box.transform, Falas.PausaFrase, UITheme.SizeHandSmall + 2, UITheme.Pen, new Vector2(cx, -104f), new Vector2(660f, 44f), hand: true);

            // opções (Bloco 14): salvas na hora, valem para as próximas partidas também. S8: linhas a cada 64 px
            const float y0 = -162f, dy = 64f;
            AddOption(box.transform, Falas.OpcaoVolumeGeral, GameOptions.MinVolume, GameOptions.MaxVolume, GameOptions.MasterVolume,
                      GameOptions.SetMasterVolume, Percent, y0);
            AddOption(box.transform, Falas.OpcaoVolumeEfeitos, GameOptions.MinVolume, GameOptions.MaxVolume, GameOptions.SfxVolume,
                      GameOptions.SetSfxVolume, Percent, y0 - dy);
            AddOption(box.transform, Falas.OpcaoVolumeMusica, GameOptions.MinVolume, GameOptions.MaxVolume, GameOptions.MusicVolume,
                      GameOptions.SetMusicVolume, Percent, y0 - 2 * dy); // S5
            AddOption(box.transform, Falas.OpcaoSensibilidade, GameOptions.MinCameraSensitivity, GameOptions.MaxCameraSensitivity,
                      GameOptions.CameraSensitivity, GameOptions.SetCameraSensitivity, v => v.ToString("0.00").Replace('.', ',') + "×", y0 - 3 * dy);
            // S8: vídeo (réguas de posições inteiras: 0/1 para ligar/desligar, uma posição por nível de qualidade)
            AddOption(box.transform, Falas.OpcaoTela, 0f, 1f, GameOptions.Fullscreen ? 1f : 0f,
                      v => GameOptions.SetFullscreen(v > 0.5f), v => v > 0.5f ? Falas.OpcaoTelaCheia : Falas.OpcaoTelaJanela, y0 - 4 * dy).wholeNumbers = true;
            AddOption(box.transform, Falas.OpcaoVSync, 0f, 1f, GameOptions.VSync ? 1f : 0f,
                      v => GameOptions.SetVSync(v > 0.5f), v => v > 0.5f ? Falas.OpcaoLigado : Falas.OpcaoDesligado, y0 - 5 * dy).wholeNumbers = true;
            int levels = Mathf.Max(1, QualitySettings.names.Length);
            int q = GameOptions.ClampQuality(GameOptions.Quality < 0 ? QualitySettings.GetQualityLevel() : GameOptions.Quality, levels);
            // com um nível só (o Windows do projeto hoje só inclui "PC"), a régua não teria o que escolher: não aparece
            if (levels > 1)
                AddOption(box.transform, Falas.OpcaoQualidade, 0f, levels - 1, q,
                          v => GameOptions.SetQuality(Mathf.RoundToInt(v)), v => GameOptions.QualityLabel(Mathf.RoundToInt(v), levels), y0 - 6 * dy).wholeNumbers = true;

            AddButton(box.transform, Falas.BotaoContinuar, resume, -626f);
            AddButton(box.transform, Falas.BotaoReiniciar, restart, -722f);
            AddButton(box.transform, Falas.BotaoSair, quit, -818f);
        }

        static string Percent(float v) => Mathf.RoundToInt(v * 100f) + "%";

        /// <summary>Linha de opção: rótulo datilografado à esquerda, régua no meio, valor à caneta à direita.</summary>
        static Slider AddOption(Transform parent, string label, float min, float max, float value, Action<float> set, Func<float, string> show, float y)
        {
            var row = UIFactory.Rect("Opcao", parent).Top(40f, y, 640f, 60f);
            var l = UIFactory.Ink("Rotulo", row, label, UITheme.SizeSmall, UITheme.Ink, TextAnchor.MiddleLeft);
            l.rectTransform.Place(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(300f, 60f));
            var v = UIFactory.Ink("Valor", row, show(value), UITheme.SizeHand, UITheme.Pen, TextAnchor.MiddleRight, hand: true);
            v.rectTransform.Place(new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(96f, 60f));
            var s = UIFactory.PaperSlider("Slider", row, min, max, value, x =>
            {
                set(x);
                v.text = show(x);
            });
            s.GetComponent<RectTransform>().Place(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(310f, 0f), new Vector2(226f, 40f));
            return s;
        }

        /// <summary>Há mensagens do topo visíveis agora? (testes: a pausa não pode ficar embaixo delas)</summary>
        public bool ToastsVisible => toastRoot.gameObject.activeInHierarchy && toasts.Count > 0;

        public void HidePause()
        {
            if (pausePanel != null) Destroy(pausePanel);
            toastRoot.gameObject.SetActive(true);
        }

        /// <summary>
        /// Fim da prova (Bloco 14/16): carimbo do resultado, comentário do professor à caneta, resumo em duas colunas
        /// (a prova x a ficha criminal) e botões jogar de novo (R), menu (M) e sair. Caixa de 1240×800.
        /// </summary>
        public void ShowMatchResult(MatchSummary sum, string message, Color color, Action playAgain, Action menu, Action quit,
                                    ResultExtras extras = default)
        {
            ClearForResult();
            resultPanel = Dim("Resultado");
            var box = UIFactory.Paper("Caixa", resultPanel.transform, margin: true, header: true);
            box.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1240f, 860f));
            var t = box.transform;
            var stamp = UIFactory.Stamp("Carimbo", t, MatchSummary.Title(sum.result), UITheme.StampFor(color), UITheme.SizeStamp, -3f);
            stamp.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(40f, -135f), stamp.sizeDelta);
            // S6: o título de estilo é um segundo carimbo, azul, no canto (a "medalha" de quem passou)
            if (!string.IsNullOrEmpty(extras.title))
            {
                var titulo = UIFactory.Stamp("TituloEstilo", t, extras.title, UITheme.StampBlue, UITheme.SizeSmall + 4, 6f);
                titulo.Place(new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-50f, -95f), titulo.sizeDelta);
            }
            AddInk(t, message, UITheme.SizeHand, UITheme.PenRed, new Vector2(40f, -226f), new Vector2(1060f, 70f), hand: true);

            SummaryColumn(t, "ColunaProva", Falas.ResumoProva, sum.ExamLines, -240f);
            SummaryColumn(t, "ColunaFicha", Falas.ResumoCrimes, sum.CrimeLines, 330f);
            // S6: o que te derrubou (à caneta azul), e o rodapé com dificuldade, recorde e conquistas novas
            if (!string.IsNullOrEmpty(extras.tip))
                AddInk(t, Falas.DicaPrefixo + extras.tip, UITheme.SizeHandSmall + 2, UITheme.Pen, new Vector2(40f, -646f), new Vector2(1060f, 40f), hand: true)
                    .name = "Dica";
            string rodape = Falas.ResumoRodape + sum.difficulty;
            string extra = Records.FooterLine(extras);
            if (extra.Length > 0) rodape += "   •   " + extra;
            AddInk(t, rodape, UITheme.SizeSmall, UITheme.InkSoft, new Vector2(40f, -694f), new Vector2(1100f, 28f)).name = "Rodape";

            ResultButton(t, "JogarDeNovo", Falas.BotaoJogarDeNovo, playAgain, -260f, 400f);
            ResultButton(t, "Menu", Falas.BotaoMenu, menu, 120f, 300f);
            ResultButton(t, "Sair", Falas.BotaoSair, quit, 420f, 240f);
        }

        /// <summary>Uma coluna do resumo: cabeçalho sublinhado e linhas "rótulo ...... valor à caneta".</summary>
        static void SummaryColumn(Transform parent, string name, string header, (string label, string value)[] lines, float x)
        {
            const float w = 520f, line = 46f, top = 54f;
            var col = UIFactory.Rect(name, parent).Top(x, -300f, w, top + line * 6 + 8f);
            var h = UIFactory.Ink("Cabecalho", col, header, UITheme.SizeBody + 1, UITheme.Ink, TextAnchor.MiddleLeft);
            h.rectTransform.Top(0f, 0f, w, 40f);
            UIFactory.Line("Sublinhado", col, UITheme.Ink, 0f, -42f, w, 2f);
            for (int i = 0; i < lines.Length; i++)
            {
                float y = -top - i * line;
                UIFactory.Line("Pauta", col, UITheme.Rule, 0f, y - line + 6f, w, 2f);
                var l = UIFactory.Ink("Rotulo", col, lines[i].label, UITheme.SizeSmall - 1, UITheme.InkSoft, TextAnchor.LowerLeft);
                l.rectTransform.Top(-w * 0.5f + 175f, y, 350f, line - 8f);
                var v = UIFactory.Ink("Valor", col, lines[i].value, UITheme.SizeHand, UITheme.Pen, TextAnchor.LowerRight, hand: true);
                v.rectTransform.Top(w * 0.5f - 85f, y + 2f, 170f, line - 6f);
                v.horizontalOverflow = HorizontalWrapMode.Overflow;
            }
        }

        static void ResultButton(Transform parent, string name, string text, Action onClick, float x, float width)
        {
            var b = UIFactory.PaperButton(name, parent, text, () => onClick(), UITheme.SizeButton - 2);
            b.GetComponent<RectTransform>().Top(x, -740f, width, 84f);
        }

        void ClearForResult()
        {
            HidePause();
            foreach (var t in toasts) Destroy(t.rect.gameObject);
            toasts.Clear();
            if (resultPanel != null) Destroy(resultPanel);
        }

        /// <summary>Tela final simples (fim do treino): carimbo, frase, uma linha e dois botões.</summary>
        public void ShowResult(string title, string message, string stats, Color color, Action restart, Action quit)
        {
            ClearForResult();
            resultPanel = Dim("Resultado");
            var box = UIFactory.Paper("Caixa", resultPanel.transform, margin: true, header: true);
            box.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100f, 680f));
            var stamp = UIFactory.Stamp("Carimbo", box.transform, title, UITheme.StampFor(color), UITheme.SizeStamp - 8, -3f);
            stamp.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(40f, -130f), stamp.sizeDelta);
            AddInk(box.transform, message, UITheme.SizeHand, UITheme.PenRed, new Vector2(40f, -220f), new Vector2(920f, 120f), hand: true);
            AddInk(box.transform, stats, UITheme.SizeBody, UITheme.Ink, new Vector2(40f, -360f), new Vector2(920f, 80f));
            var a = UIFactory.PaperButton("Reiniciar", box.transform, Falas.BotaoTentarDeNovo, () => restart(), UITheme.SizeButton);
            a.GetComponent<RectTransform>().Top(-190f, -520f, 400f, 88f);
            var q = UIFactory.PaperButton("Sair", box.transform, Falas.BotaoSair, () => quit(), UITheme.SizeButton);
            q.GetComponent<RectTransform>().Top(270f, -520f, 400f, 88f);
        }

        GameObject Dim(string name)
        {
            var dim = UIFactory.Box(name, PanelRoot, UITheme.Dim, false);
            dim.raycastTarget = true;
            dim.rectTransform.Stretch();
            dim.transform.SetAsLastSibling();
            return dim.gameObject;
        }

        static Text AddInk(Transform parent, string text, int size, Color color, Vector2 pos, Vector2 sizeDelta, bool hand = false)
        {
            var l = UIFactory.Ink("Texto", parent, text, size, color, TextAnchor.UpperCenter, hand);
            l.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), pos, sizeDelta);
            return l;
        }

        static void AddButton(Transform parent, string text, Action onClick, float y)
        {
            var b = UIFactory.PaperButton("Botao", parent, text, () => onClick(), UITheme.SizeButton - 2);
            b.GetComponent<RectTransform>().Top(40f, y, 460f, 84f);
        }
    }
}
