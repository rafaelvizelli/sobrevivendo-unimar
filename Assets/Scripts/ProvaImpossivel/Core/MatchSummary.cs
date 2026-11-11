using UnityEngine;

namespace ProvaImpossivel
{
    /// <summary>
    /// Números da tela de fim de partida (Bloco 14). Monta tudo a partir do que já existe: ExamSystem (questões),
    /// GameManager (nota, tempo, flagrantes, favores) e MatchLog (colas por tipo). Puro: dá para testar sem a cena.
    /// </summary>
    public readonly struct MatchSummary
    {
        public readonly LevelResult result;
        public readonly string difficulty;
        public readonly float grade, minGrade, timeUsed;
        public readonly int flagrantes, maxFlagrantes;
        public readonly int colasCelular, colasNerd, colasFundao, colasFundaoFuradas;
        public readonly int favoresGastos;
        public readonly int certas, erradas, emBranco;
        /// <summary>Colas de bolso (Bloco 15) feitas: colinha, garrafinha, bilhete, fone (usos do MatchLog).</summary>
        public readonly int colinha, garrafa, bilhete, fone;

        public MatchSummary(LevelResult result, string difficulty, float grade, float minGrade, float timeUsed,
                            int flagrantes, int maxFlagrantes, int colasCelular, int colasNerd, int colasFundao, int colasFundaoFuradas,
                            int favoresGastos, int certas, int erradas, int emBranco,
                            int colinha = 0, int garrafa = 0, int bilhete = 0, int fone = 0)
        {
            this.colinha = colinha;
            this.garrafa = garrafa;
            this.bilhete = bilhete;
            this.fone = fone;
            this.result = result;
            this.difficulty = difficulty;
            this.grade = grade;
            this.minGrade = minGrade;
            this.timeUsed = timeUsed;
            this.flagrantes = flagrantes;
            this.maxFlagrantes = maxFlagrantes;
            this.colasCelular = colasCelular;
            this.colasNerd = colasNerd;
            this.colasFundao = colasFundao;
            this.colasFundaoFuradas = colasFundaoFuradas;
            this.favoresGastos = favoresGastos;
            this.certas = certas;
            this.erradas = erradas;
            this.emBranco = emBranco;
        }

        /// <summary>Resumo da partida que acabou. Colas por tipo vêm do <see cref="MatchLog"/> (zerado a cada partida).</summary>
        public static MatchSummary Build(DifficultySettings s, LevelResult result, float grade, float timeLeft,
                                         int flagrantes, int favorsLeft, int certas, int erradas, int emBranco)
        {
            return new MatchSummary(result, s.name, grade, s.minGrade,
                                    Mathf.Max(0f, s.duration - Mathf.Max(0f, timeLeft)),
                                    flagrantes, s.maxFlagrantes,
                                    MatchLog.Celular, MatchLog.NerdCopias,
                                    MatchLog.FundaoColaBoa + MatchLog.FundaoColaRuim, MatchLog.FundaoColaRuim,
                                    Mathf.Max(0, s.favors - favorsLeft), certas, erradas, emBranco,
                                    MatchLog.ColinhaUsos, MatchLog.GarrafaUsos, MatchLog.BilheteUsos, MatchLog.FoneUsos);
        }

        public static string Title(LevelResult r) => r switch
        {
            LevelResult.Aprovado => Falas.TituloAprovado,
            LevelResult.AprovadoNoSufoco => Falas.TituloSufoco,
            LevelResult.Flagrado => Falas.TituloExpulso,
            _ => Falas.TituloReprovado,
        };

        /// <summary>Coluna "a prova": (rótulo, valor) por linha.</summary>
        public (string label, string value)[] ExamLines => new[]
        {
            (Falas.ResumoNota, GameManager.Fmt(grade)),
            (Falas.ResumoMinima, GameManager.Fmt(minGrade)),
            (Falas.ResumoQuestoes, certas + " / " + erradas + " / " + emBranco),
            (Falas.ResumoTempo, HUD.FormatTime(timeUsed)),
        };

        /// <summary>Coluna "ficha criminal": flagrantes, colas por tipo e favores.</summary>
        public (string label, string value)[] CrimeLines => new[]
        {
            (Falas.ResumoFlagrantes, flagrantes + " de " + maxFlagrantes),
            (Falas.ResumoCelular, colasCelular.ToString()),
            (Falas.ResumoNerd, colasNerd.ToString()),
            (Falas.ResumoFundao, colasFundao + (colasFundaoFuradas > 0 ? " (" + colasFundaoFuradas + " " + Falas.ResumoFundaoFurada + ")" : "")),
            (Falas.ResumoColasDeBolso, colinha + " / " + garrafa + " / " + bilhete + " / " + fone),
            (Falas.ResumoFavores, favoresGastos.ToString()),
        };
    }
}
