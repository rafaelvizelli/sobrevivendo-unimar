using UnityEngine;

namespace ProvaImpossivel
{
    /// <summary>
    /// Orçamento de colas da partida (Bloco 15): quantas respostas TODAS as colas somadas ainda podem dar
    /// (celular, Nerd, Fundão, colinha, bilhete, fone, garrafinha). Assim colas novas dão variedade, não mais pontos.
    /// Colas com atraso (bilhete, fone) RESERVAM uma vaga ao serem pedidas: a resposta que ainda está a caminho
    /// já conta, e a vaga volta se ela se perder. Classe pura (sem Unity) para ser testável.
    /// </summary>
    public class CheatBudget
    {
        public int Max { get; }
        public int Used { get; private set; }
        public int Reserved { get; private set; }
        /// <summary>Vagas livres agora (descontando as respostas que estão a caminho).</summary>
        public int Available => Mathf.Max(0, Max - Used - Reserved);
        public bool Exhausted => Available <= 0;

        public CheatBudget(int max) => Max = Mathf.Max(0, max);

        /// <summary>Uma cola deu resposta agora (sem reserva). false = acabou o orçamento, a cola não vale.</summary>
        public bool TryUse()
        {
            if (Available <= 0) return false;
            Used++;
            return true;
        }

        /// <summary>Guarda uma vaga para uma resposta que vai chegar depois. false = sem vaga.</summary>
        public bool TryReserve()
        {
            if (Available <= 0) return false;
            Reserved++;
            return true;
        }

        /// <summary>A resposta reservada chegou e foi usada.</summary>
        public void Commit()
        {
            if (Reserved <= 0) return;
            Reserved--;
            Used++;
        }

        /// <summary>A resposta reservada se perdeu (bilhete caiu, celular confiscado): a vaga volta.</summary>
        public void Cancel()
        {
            if (Reserved > 0) Reserved--;
        }

        /// <summary>Orçamento da partida atual (sem partida, por exemplo em testes isolados, nada limita).</summary>
        public static CheatBudget Current => GameManager.I != null ? GameManager.I.Cheats : null;

        /// <summary>Ainda há vaga para uma cola dar resposta? (sem partida: sim)</summary>
        public static bool CanAnswer => Current == null || !Current.Exhausted;

        /// <summary>Gasta uma vaga no orçamento da partida (sem partida: sempre pode).</summary>
        public static bool Spend() => Current == null || Current.TryUse();
    }
}
