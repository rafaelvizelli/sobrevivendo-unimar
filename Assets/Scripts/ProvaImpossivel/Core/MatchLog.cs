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
        /// <summary>
        /// Colas do Bloco 15 (registradas no Bloco 15b). Usos = cola feita (segurou até o fim, jogou o bilhete, mandou a
        /// mensagem) ou interrompida por flagrante. Certas = virou resposta certa. Erradas = não virou: flagrante no meio,
        /// bilhete interceptado/recusado/errou a mesa/lista do mercado, celular confiscado antes da resposta.
        /// Usos - certas - erradas = ainda esperando a resposta quando a prova acabou.
        /// </summary>
        public static int ColinhaUsos, ColinhaCertas, ColinhaErradas, GarrafaUsos, GarrafaCertas, GarrafaErradas;
        public static int BilheteUsos, BilheteCertas, BilheteErradas, FoneUsos, FoneCertas, FoneErradas;
        static readonly StringBuilder flagrantes = new();

        /// <summary>No editor (onde também rodam os testes automatizados) grava num arquivo separado, para não misturar com partidas reais.</summary>
        public static string FilePath => Path.Combine(Application.persistentDataPath, Application.isEditor ? "partidas_editor.csv" : "partidas.csv");

        public static void Reset()
        {
            ConhecidasTentadas = ConhecidasCertas = Chutes = ChutesCertos = 0;
            Celular = NerdCopias = NerdPegou = FundaoPedidos = FundaoColaBoa = FundaoColaRuim = 0;
            Eventos = CaraPaisagemOk = CaraPaisagemFalhou = 0;
            ColinhaUsos = ColinhaCertas = ColinhaErradas = GarrafaUsos = GarrafaCertas = GarrafaErradas = 0;
            BilheteUsos = BilheteCertas = BilheteErradas = FoneUsos = FoneCertas = FoneErradas = 0;
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
                            + "fundao_pedidos;fundao_cola_boa;fundao_cola_ruim;favores_usados;eventos;cara_paisagem_ok;cara_paisagem_falhou;"
                            // colunas novas SEMPRE no fim (Bloco 15b): quem lê as antigas pelo índice continua funcionando
                            + "colinha_usos;colinha_certas;colinha_erradas;garrafa_usos;garrafa_certas;garrafa_erradas;"
                            + "bilhete_usos;bilhete_certas;bilhete_erradas;fone_usos;fone_certas;fone_erradas";

        /// <summary>
        /// Arquivo de uma versão anterior (cabeçalho mais curto, que é o começo do atual): troca só a primeira linha pelo
        /// cabeçalho novo. As linhas antigas ficam como estão (com as colunas novas vazias), e as novas linhas batem com
        /// o cabeçalho. Cabeçalho desconhecido: não mexe. Público para testes.
        /// </summary>
        public static void UpgradeHeader(string path)
        {
            var lines = File.ReadAllLines(path, Encoding.UTF8);
            if (lines.Length == 0) return;
            string first = lines[0].TrimStart('﻿');
            if (first == Header || !Header.StartsWith(first + ";", StringComparison.Ordinal)) return;
            lines[0] = Header;
            File.WriteAllLines(path, lines, Encoding.UTF8);
        }

        /// <summary>Grava a partida que acabou de terminar. Nunca derruba o jogo por falha de disco.</summary>
        public static void Write(DifficultySettings s, LevelResult result, float grade, int flagrantesCount, float timeLeft, int favorsLeft)
        {
            var ci = CultureInfo.InvariantCulture;
            string line = string.Join(";",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", ci), s.name, result, grade.ToString("0.0", ci), s.minGrade.ToString("0.0", ci),
                flagrantesCount, flagrantes.ToString(), Mathf.RoundToInt(timeLeft),
                ConhecidasCertas, ConhecidasTentadas, ChutesCertos, Chutes, Celular, NerdCopias, NerdPegou,
                FundaoPedidos, FundaoColaBoa, FundaoColaRuim, s.favors - favorsLeft, Eventos, CaraPaisagemOk, CaraPaisagemFalhou,
                ColinhaUsos, ColinhaCertas, ColinhaErradas, GarrafaUsos, GarrafaCertas, GarrafaErradas,
                BilheteUsos, BilheteCertas, BilheteErradas, FoneUsos, FoneCertas, FoneErradas);
            Debug.Log("[Partida] " + Header + "\n[Partida] " + line);
            try
            {
                bool novo = !File.Exists(FilePath);
                if (!novo) UpgradeHeader(FilePath);
                File.AppendAllText(FilePath, (novo ? Header + "\n" : "") + line + "\n", Encoding.UTF8);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Partida] não consegui gravar " + FilePath + ": " + e.Message);
            }
        }
    }
}
