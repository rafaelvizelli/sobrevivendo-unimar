using System;
using UnityEngine;

namespace ProvaImpossivel
{
    public enum QuestionState { EmBranco, Certa, Errada }

    /// <summary>
    /// A prova: N questões, cada uma em branco, certa ou errada. Nota = certas × (10 / N).
    /// Só algumas o jogador sabe (Known): essas ele responde no Desafio B; as outras, só chutando ou colando.
    /// </summary>
    public class ExamSystem : MonoBehaviour
    {
        public static ExamSystem I { get; private set; }

        public int questionCount = 10;
        public QuestionState[] Questions { get; private set; }
        /// <summary>Questões que o jogador sabe responder sozinho (sorteadas no começo da prova).</summary>
        public bool[] Known { get; private set; }
        public event Action Changed;

        public float Grade
        {
            get
            {
                int c = 0;
                foreach (var q in Questions) if (q == QuestionState.Certa) c++;
                return c * (10f / questionCount);
            }
        }

        /// <summary>Ainda existe questão que não está certa?</summary>
        public bool HasRoom => Array.IndexOf(Questions, QuestionState.EmBranco) >= 0 || Array.IndexOf(Questions, QuestionState.Errada) >= 0;

        public float PointsPerQuestion => 10f / questionCount;

        /// <summary>Ainda há questão em branco (que dá pra tentar responder sozinho)?</summary>
        public bool HasBlank => Array.IndexOf(Questions, QuestionState.EmBranco) >= 0;

        public int Count(QuestionState s)
        {
            int c = 0;
            foreach (var q in Questions) if (q == s) c++;
            return c;
        }

        void Awake()
        {
            I = this;
            Questions = new QuestionState[questionCount];
            Known = new bool[questionCount];
        }

        /// <summary>Sorteia quais <paramref name="count"/> questões o jogador sabe (chamado no começo da prova).</summary>
        public void SetKnown(int count)
        {
            Array.Clear(Known, 0, Known.Length);
            count = Mathf.Clamp(count, 0, questionCount);
            for (int marked = 0; marked < count;)
            {
                int i = UnityEngine.Random.Range(0, questionCount);
                if (Known[i]) continue;
                Known[i] = true;
                marked++;
            }
            Changed?.Invoke();
        }

        public int KnownCount
        {
            get
            {
                int c = 0;
                foreach (bool k in Known) if (k) c++;
                return c;
            }
        }

        /// <summary>Primeira questão em branco que o jogador sabe (ou não sabe). -1 se não houver.</summary>
        public int FirstBlank(bool known)
        {
            for (int i = 0; i < Questions.Length; i++)
                if (Questions[i] == QuestionState.EmBranco && Known[i] == known) return i;
            return -1;
        }

        /// <summary>
        /// Cola: marca uma questão como certa. Prioridade: em branco que ele NÃO sabe, depois corrige uma errada,
        /// por último uma em branco que ele sabe (essa daria pra responder sozinho). Retorna o índice ou -1.
        /// </summary>
        public int MarkCorrect()
        {
            int i = FirstBlank(false);
            if (i < 0) i = Array.IndexOf(Questions, QuestionState.Errada);
            if (i < 0) i = FirstBlank(true);
            return MarkCorrect(i);
        }

        /// <summary>
        /// Cola que só serve para questão AINDA NÃO RESPONDIDA (colinha, garrafinha): primeiro as em branco que ele
        /// não sabe, depois as em branco que sabe. Não corrige erradas (isso é o que celular, Nerd e fone fazem).
        /// </summary>
        public int MarkCorrectBlank()
        {
            int i = FirstBlank(false);
            if (i < 0) i = FirstBlank(true);
            return MarkCorrect(i);
        }

        /// <summary>
        /// Índice da questão em branco número ⌊roll × total⌋ (roll de 0 a 1), ou -1 se não houver. Pura (testável):
        /// o bilhete volta com a resposta de uma questão em branco QUALQUER, que pode ser uma que ele já sabia.
        /// </summary>
        public static int RandomBlank(QuestionState[] questions, double roll)
        {
            int blanks = 0;
            foreach (var q in questions) if (q == QuestionState.EmBranco) blanks++;
            if (blanks == 0) return -1;
            int k = Math.Min(blanks - 1, (int)(Math.Clamp(roll, 0.0, 1.0) * blanks));
            for (int i = 0; i < questions.Length; i++)
                if (questions[i] == QuestionState.EmBranco && k-- == 0) return i;
            return -1;
        }

        /// <summary>
        /// S2: como <see cref="RandomBlank"/>, mas sorteia primeiro entre as em branco que ele NÃO sabe; só se não houver
        /// nenhuma, entre as que ele sabe. Pura (testável). Assim a cola nunca é gasta numa questão que ele faria sozinho.
        /// </summary>
        public static int RandomBlankPreferUnknown(QuestionState[] questions, bool[] known, double roll)
        {
            int unknownBlanks = 0;
            for (int i = 0; i < questions.Length; i++)
                if (questions[i] == QuestionState.EmBranco && !known[i]) unknownBlanks++;
            if (unknownBlanks == 0) return RandomBlank(questions, roll);
            int k = Math.Min(unknownBlanks - 1, (int)(Math.Clamp(roll, 0.0, 1.0) * unknownBlanks));
            for (int i = 0; i < questions.Length; i++)
                if (questions[i] == QuestionState.EmBranco && !known[i] && k-- == 0) return i;
            return -1;
        }

        /// <summary>Marca como certa uma questão em branco sorteada, preferindo as que ele não sabe. Retorna o índice ou -1.</summary>
        public int MarkCorrectRandomBlankPreferUnknown(double roll) => MarkCorrect(RandomBlankPreferUnknown(Questions, Known, roll));

        /// <summary>Marca como certa uma questão em branco sorteada. Retorna o índice ou -1.</summary>
        public int MarkCorrectRandomBlank(double roll) => MarkCorrect(RandomBlank(Questions, roll));

        /// <summary>Marca a questão <paramref name="i"/> como certa. Retorna o índice ou -1.</summary>
        public int MarkCorrect(int i)
        {
            if (i < 0 || i >= Questions.Length || Questions[i] == QuestionState.Certa) return -1;
            Questions[i] = QuestionState.Certa;
            Changed?.Invoke();
            return i;
        }

        /// <summary>Gasta uma questão em branco com resposta errada (primeiro as que ele não sabe). Retorna o índice ou -1.</summary>
        public int MarkWrong()
        {
            int i = FirstBlank(false);
            if (i < 0) i = FirstBlank(true);
            return MarkWrong(i);
        }

        /// <summary>Marca a questão em branco <paramref name="i"/> como errada. Retorna o índice ou -1.</summary>
        public int MarkWrong(int i)
        {
            if (i < 0 || i >= Questions.Length || Questions[i] != QuestionState.EmBranco) return -1;
            Questions[i] = QuestionState.Errada;
            Changed?.Invoke();
            return i;
        }

        /// <summary>Penalidade: anula uma questão certa (ou zera uma em branco). Retorna true se tirou ponto.</summary>
        public bool Annul()
        {
            int i = Array.LastIndexOf(Questions, QuestionState.Certa);
            if (i >= 0)
            {
                Questions[i] = QuestionState.Errada;
                Changed?.Invoke();
                return true;
            }
            MarkWrong();
            return false;
        }
    }
}
