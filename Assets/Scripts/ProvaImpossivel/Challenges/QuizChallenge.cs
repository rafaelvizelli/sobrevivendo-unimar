using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ProvaImpossivel
{
    /// <summary>
    /// DESAFIO B — RESPOSTA SOB PRESSÃO. Pergunta de múltipla escolha com tempo (teclas 1 a 4).
    /// Seguro (não gera suspeita), mas o jogador só sabe poucas questões (ExamSystem.Known).
    /// Nas que ele não sabe, o mesmo painel vira CHUTE: pergunta absurda, ~25% de acerto.
    /// </summary>
    public class QuizChallenge : MonoBehaviour
    {
        public static QuizChallenge I { get; private set; }
        public bool Active { get; private set; }
        /// <summary>A pergunta aberta é um chute (questão que o jogador não sabe)?</summary>
        public bool IsChute { get; private set; }
        /// <summary>Pergunta aberta agora (para testes).</summary>
        public QuizBank.Question Current => question;

        GameObject panel;
        Text titleText, helpText, questionText, timerText, feedbackText;
        // a tecla 1 que abriu a pergunta (PlayerController) não pode valer também como resposta "1" no mesmo frame
        int openedFrame = -1;
        readonly Image[] optionBoxes = new Image[4];
        readonly Text[] optionTexts = new Text[4];
        Image timerFill;

        QuizBank.Question question;
        int[] order;
        float timeLeft, total, closeAt;
        bool answered;
        Action<bool> done;
        bool result;

        void Awake()
        {
            I = this;
        }

        void Start() => Build();

        void Build()
        {
            var box = UIFactory.Box("Desafio B", HUD.I.PanelRoot, HUD.PanelColor);
            box.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -70f), new Vector2(1000f, 520f));
            panel = box.gameObject;
            var t = box.transform;

            titleText = UIFactory.Label("Titulo", t, "", 28, HUD.Yellow);
            titleText.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(960f, 40f));
            helpText = UIFactory.Label("Ajuda", t, "", 19, HUD.Gray, TextAnchor.MiddleCenter, FontStyle.Italic);
            helpText.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -56f), new Vector2(960f, 28f));

            questionText = UIFactory.Label("Pergunta", t, "", 30, Color.white);
            questionText.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -96f), new Vector2(940f, 90f));

            for (int i = 0; i < 4; i++)
            {
                int col = i % 2, row = i / 2;
                optionBoxes[i] = UIFactory.Box("Opcao" + (i + 1), t, new Color(0.22f, 0.24f, 0.32f));
                optionBoxes[i].rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(col == 0 ? -235f : 235f, -205f - row * 100f), new Vector2(450f, 84f));
                optionTexts[i] = UIFactory.Label("Texto", optionBoxes[i].transform, "", 24, Color.white, TextAnchor.MiddleLeft);
                optionTexts[i].rectTransform.Stretch(14f);
            }

            timerFill = UIFactory.Bar("Tempo", t, new Color(0f, 0f, 0f, 0.6f), HUD.Yellow);
            ((RectTransform)timerFill.transform.parent).Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 58f), new Vector2(900f, 26f));
            timerText = UIFactory.Label("Segundos", timerFill.transform.parent, "", 18, Color.white);
            timerText.rectTransform.Stretch();
            feedbackText = UIFactory.Label("Feedback", t, "", 26, HUD.Green);
            feedbackText.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(960f, 40f));
            panel.SetActive(false);
        }

        /// <param name="chute">true = questão que o jogador não sabe: pergunta absurda do banco de chutes.</param>
        public void Begin(float seconds, Action<bool> onDone, bool chute = false)
        {
            IsChute = chute;
            question = chute ? QuizBank.NextChute()
                             : QuizBank.Next(GameManager.I != null && GameManager.I.Settings != null ? GameManager.I.Settings.quizTier : 1);
            titleText.text = chute ? "CHUTE — ESSA VOCÊ NÃO ESTUDOU" : "DESAFIO B — RESPOSTA SOB PRESSÃO";
            helpText.text = chute
                ? "Não faz a menor ideia? Escolha 1, 2, 3 ou 4 e reze. Errou = questão perdida (só cola corrige)."
                : "Aperte 1, 2, 3 ou 4 antes do tempo acabar. Acertou = ponto. Errou = questão perdida.";
            openedFrame = Time.frameCount;
            order = new[] { 0, 1, 2, 3 };
            for (int i = 3; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }
            questionText.text = question.text;
            for (int i = 0; i < 4; i++)
            {
                optionTexts[i].text = (i + 1) + ")  " + question.options[order[i]];
                optionBoxes[i].color = new Color(0.22f, 0.24f, 0.32f);
            }
            feedbackText.text = "";
            total = timeLeft = seconds;
            answered = false;
            done = onDone;
            Active = true;
            panel.SetActive(true);
            panel.transform.SetAsLastSibling();
        }

        void Update()
        {
            if (!Active) return;
            if (answered)
            {
                if (Time.time >= closeAt) Close();
                return;
            }
            var gm = GameManager.I;
            if (gm == null || !gm.IsPlaying) return;

            timeLeft -= Time.deltaTime;
            timerFill.fillAmount = Mathf.Clamp01(timeLeft / total);
            timerFill.color = timeLeft < 3f ? HUD.Red : HUD.Yellow;
            timerText.text = Mathf.CeilToInt(Mathf.Max(0f, timeLeft)) + "s";

            var kb = Keyboard.current;
            if (kb != null && Time.frameCount != openedFrame)
            {
                if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) Answer(0);
                else if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) Answer(1);
                else if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) Answer(2);
                else if (kb.digit4Key.wasPressedThisFrame || kb.numpad4Key.wasPressedThisFrame) Answer(3);
            }
            if (!answered && timeLeft <= 0f) Answer(-1);
        }

        /// <summary>Já respondeu (o painel ainda mostra o resultado por 1,6 s antes de fechar)?</summary>
        public bool Answered => Active && answered;

        /// <summary>Posição (0 a 3) em que a alternativa correta foi sorteada nesta pergunta.</summary>
        public int CorrectSlot => Array.IndexOf(order, 0);

        /// <summary>Responde a alternativa <paramref name="slot"/> (0 a 3; -1 = tempo esgotado). Público para testes.</summary>
        public void Answer(int slot)
        {
            if (!Active || answered) return;
            answered = true;
            int correctSlot = Array.IndexOf(order, 0);
            result = slot == correctSlot;
            optionBoxes[correctSlot].color = new Color(0.2f, 0.65f, 0.3f);
            if (slot >= 0 && !result) optionBoxes[slot].color = new Color(0.75f, 0.2f, 0.2f);

            if (result)
            {
                feedbackText.text = IsChute
                    ? "ACERTOU NO CHUTE! +" + GameManager.Fmt(ExamSystem.I.PointsPerQuestion) + " — a fé move montanhas (e notas)."
                    : "ACERTOU! +" + GameManager.Fmt(ExamSystem.I.PointsPerQuestion) + " — aquele vídeo em 2x serviu pra alguma coisa!";
                feedbackText.color = HUD.Green;
                SoundFX.Play(Sfx.Acerto);
            }
            else
            {
                feedbackText.text = slot < 0 ? "O TEMPO ACABOU! Você ficou olhando pro teto."
                    : IsChute ? "ERROU O CHUTE! O gabarito do Takanervoso é um mistério." : "ERROU! Com muita confiança, mas errou.";
                feedbackText.color = HUD.Red;
                SoundFX.Play(Sfx.Erro);
            }
            closeAt = Time.time + 1.6f;
        }

        void Close()
        {
            Active = false;
            panel.SetActive(false);
            var cb = done;
            done = null;
            cb?.Invoke(result);
        }
    }
}
