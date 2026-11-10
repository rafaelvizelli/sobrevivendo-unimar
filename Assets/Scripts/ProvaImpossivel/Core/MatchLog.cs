using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace ProvaImpossivel
{
    /// <summary>
    /// Registro de partidas para balancear com dados (e não no chute): uma linha CSV por partida em
    /// <c>Application.persistentDataPath/partidas.csv</c> + um resumo no log.
    /// Estático e sem dependências de propósito: os sistemas só chamam "aconteceu X"; quem escreve é o GameManager.
    /// O domínio não recarrega ao dar Play (EnterPlayModeOptions), por isso <see cref="Reset"/> roda a cada prova.
    /// </summary>
    public static class MatchLog
    {
        public static int ConhecidasTentadas, ConhecidasCertas, Chutes, ChutesCertos;
        public static int Celular, NerdCopias, NerdPegou, FundaoPedidos, FundaoColaBoa, FundaoColaRuim;
        public static int Eventos, CaraPaisagemOk, CaraPaisagemFalhou;
        static readonly StringBuilder flagrantes = new();

        /// <summary>No editor (onde também rodam os testes automatizados) grava num arquivo separado, para não misturar com partidas reais.</summary>
        public static string FilePath => Path.Combine(Application.persistentDataPath, Application.isEditor ? "partidas_editor.csv" : "partidas.csv");

        public static void Reset()
        {
            ConhecidasTentadas = ConhecidasCertas = Chutes = ChutesCertos = 0;
            Celular = NerdCopias = NerdPegou = FundaoPedidos = FundaoColaBoa = FundaoColaRuim = 0;
            Eventos = CaraPaisagemOk = CaraPaisagemFalhou = 0;
            flagrantes.Clear();
        }

        public static void Quiz(bool chute, bool ok)
        {
            if (chute) { Chutes++; if (ok) ChutesCertos++; }
            else { ConhecidasTentadas++; if (ok) ConhecidasCertas++; }
        }

        public static void Flagrante(Infracao kind)
        {
            if (flagrantes.Length > 0) flagrantes.Append('|');
            flagrantes.Append(kind);
        }

        const string Header = "data;dificuldade;resultado;nota;nota_minima;flagrantes;tipos_flagrante;tempo_restante_s;"
                            + "conhecidas_certas;conhecidas_tentadas;chutes_certos;chutes;celular;nerd_copias;nerd_pegou;"
                            + "fundao_pedidos;fundao_cola_boa;fundao_cola_ruim;favores_usados;eventos;cara_paisagem_ok;cara_paisagem_falhou";

        /// <summary>Grava a partida que acabou de terminar. Nunca derruba o jogo por falha de disco.</summary>
        public static void Write(DifficultySettings s, LevelResult result, float grade, int flagrantesCount, float timeLeft, int favorsLeft)
        {
            var ci = CultureInfo.InvariantCulture;
            string line = string.Join(";",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", ci), s.name, result, grade.ToString("0.0", ci), s.minGrade.ToString("0.0", ci),
                flagrantesCount, flagrantes.ToString(), Mathf.RoundToInt(timeLeft),
                ConhecidasCertas, ConhecidasTentadas, ChutesCertos, Chutes, Celular, NerdCopias, NerdPegou,
                FundaoPedidos, FundaoColaBoa, FundaoColaRuim, s.favors - favorsLeft, Eventos, CaraPaisagemOk, CaraPaisagemFalhou);
            Debug.Log("[Partida] " + Header + "\n[Partida] " + line);
            try
            {
                bool novo = !File.Exists(FilePath);
                File.AppendAllText(FilePath, (novo ? Header + "\n" : "") + line + "\n", Encoding.UTF8);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Partida] não consegui gravar " + FilePath + ": " + e.Message);
            }
        }
    }
}
