using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProvaImpossivel
{
    /// <summary>Conquistas locais (S6). Os nomes do enum servem de id para virarem conquistas da Steam depois.</summary>
    public enum Conquista { Passei, LisoComoSabao, SobreviventeDaUnimar, NoGrito, MagicoDoBolso, FichaSuja, ListaDoMercado, GenioDoCrime }

    /// <summary>O que a tela final mostra além do resumo (S6).</summary>
    public struct ResultExtras
    {
        /// <summary>Título pelo jeito que você passou (null se não passou).</summary>
        public string title;
        /// <summary>O que te derrubou e o que fazer da próxima vez (null se não há o que dizer).</summary>
        public string tip;
        /// <summary>Melhor nota nesta dificuldade, já contando esta partida.</summary>
        public float bestGrade;
        public bool newRecord;
        public List<Conquista> newAchievements;
    }

    /// <summary>
    /// S6 (Operação Steam): motivo para jogar de novo. Título de estilo, dica da derrota, recorde por dificuldade e
    /// conquistas locais. Regras puras (testáveis); só <see cref="Process"/> lê e grava PlayerPrefs.
    /// </summary>
    public static class Records
    {
        public const string Prefix = GameOptions.Prefix + "recorde.";
        public const string KeyAchievements = GameOptions.Prefix + "conquistas";

        // ------------------------------------------------------------------ título

        /// <summary>Título pelo jeito que você passou, em ordem de raridade (o mais raro ganha). Reprovado: null.</summary>
        public static string StyleTitle(MatchSummary s)
        {
            bool passou = s.result == LevelResult.Aprovado || s.result == LevelResult.AprovadoNoSufoco;
            if (!passou) return null;
            int bolso = s.colinha + s.garrafa + s.bilhete + s.fone;
            int colas = bolso + s.colasCelular + s.colasNerd + s.colasFundao;
            if (colas == 0) return Falas.TituloEstiloHonesto;
            if (s.grade >= 9.99f && s.flagrantes == 0) return Falas.TituloEstiloGenio;
            if (s.flagrantes == 0 && bolso >= 3 && s.colasCelular == 0 && s.colasNerd == 0) return Falas.TituloEstiloBolso;
            if (s.flagrantes >= 2) return Falas.TituloEstiloCaraDePau;
            if (s.result == LevelResult.AprovadoNoSufoco || s.grade < s.minGrade + 0.5f) return string.Format(Falas.TituloEstiloSobrevivente, GameManager.Fmt(s.minGrade));
            if (s.colasNerd >= 2) return Falas.TituloEstiloNerd;
            if (s.colasCelular >= 2) return Falas.TituloEstiloGoogle;
            if (s.colasFundao >= 1) return Falas.TituloEstiloFundao;
            if (s.flagrantes == 0) return Falas.TituloEstiloNinja;
            return Falas.TituloEstiloProfissional;
        }

        // ------------------------------------------------------------------ dica

        /// <summary>
        /// O que te derrubou e o que fazer na próxima. Expulso ou com flagrantes: pelo tipo de flagrante que mais
        /// aconteceu. Reprovado sem flagrante: pelo que sobrou na prova. Aprovado: null (o título fala por si).
        /// </summary>
        public static string Tip(MatchSummary s, int budget)
        {
            bool passou = s.result == LevelResult.Aprovado || s.result == LevelResult.AprovadoNoSufoco;
            if (passou) return null;
            if (s.flagrantes > 0 && s.flagranteMaisComum != Infracao.Nenhuma)
                return Falas.DicaFlagrante(s.flagranteMaisComum);
            if (s.emBranco > 0) return string.Format(Falas.DicaEmBranco, s.emBranco, budget);
            if (s.erradas > 0) return Falas.DicaErradas;
            return null;
        }

        // ------------------------------------------------------------------ recorde

        /// <summary>A nota desta partida bate o recorde? (só nota de quem passou conta; recorde -1 = nenhum ainda)</summary>
        public static bool IsNewRecord(float previousBest, MatchSummary s) =>
            (s.result == LevelResult.Aprovado || s.result == LevelResult.AprovadoNoSufoco) && s.grade > previousBest + 0.001f;

        // ------------------------------------------------------------------ conquistas

        /// <summary>Conquistas que esta partida cumpre (as já ganhas são filtradas por quem chama).</summary>
        public static List<Conquista> Earned(MatchSummary s, bool hard)
        {
            var list = new List<Conquista>();
            bool passou = s.result == LevelResult.Aprovado || s.result == LevelResult.AprovadoNoSufoco;
            if (passou) list.Add(Conquista.Passei);
            if (passou && s.flagrantes == 0) list.Add(Conquista.LisoComoSabao);
            if (passou && hard) list.Add(Conquista.SobreviventeDaUnimar);
            if (s.result == LevelResult.AprovadoNoSufoco) list.Add(Conquista.NoGrito);
            if (s.colinha > 0 && s.garrafa > 0 && s.bilhete > 0 && s.fone > 0) list.Add(Conquista.MagicoDoBolso);
            if (s.result == LevelResult.Flagrado) list.Add(Conquista.FichaSuja);
            if (s.colasFundaoFuradas > 0) list.Add(Conquista.ListaDoMercado);
            if (passou && s.grade >= 9.99f) list.Add(Conquista.GenioDoCrime);
            return list;
        }

        public static int Mask(IEnumerable<Conquista> list)
        {
            int m = 0;
            foreach (var c in list) m |= 1 << (int)c;
            return m;
        }

        public static bool Has(int mask, Conquista c) => (mask & (1 << (int)c)) != 0;

        // ------------------------------------------------------------------ gravação

        static string BestKey(string difficulty) => Prefix + difficulty.ToLowerInvariant();

        public static float BestGrade(string difficulty) => PlayerPrefs.GetFloat(BestKey(difficulty), -1f);
        public static int AchievementMask => PlayerPrefs.GetInt(KeyAchievements, 0);

        /// <summary>Calcula tudo para a tela final e grava recorde e conquistas novas. Nunca derruba o jogo.</summary>
        public static ResultExtras Process(MatchSummary s, int budget, bool hard)
        {
            var e = new ResultExtras { title = StyleTitle(s), tip = Tip(s, budget), newAchievements = new List<Conquista>() };
            try
            {
                float best = BestGrade(s.difficulty);
                e.newRecord = IsNewRecord(best, s);
                e.bestGrade = e.newRecord ? s.grade : best;
                if (e.newRecord) PlayerPrefs.SetFloat(BestKey(s.difficulty), s.grade);

                int mask = AchievementMask;
                foreach (var c in Earned(s, hard))
                    if (!Has(mask, c)) { e.newAchievements.Add(c); mask |= 1 << (int)c; }
                PlayerPrefs.SetInt(KeyAchievements, mask);
                PlayerPrefs.Save();
            }
            catch (Exception ex) { Debug.LogWarning("[Recordes] não consegui gravar: " + ex.Message); }
            return e;
        }

        /// <summary>Apaga recordes e conquistas (testes).</summary>
        public static void Clear(params string[] difficulties)
        {
            foreach (var d in difficulties) PlayerPrefs.DeleteKey(BestKey(d));
            PlayerPrefs.DeleteKey(KeyAchievements);
            PlayerPrefs.Save();
        }

        /// <summary>Linha de rodapé da tela final: recorde (e "NOVO!") e conquistas novas.</summary>
        public static string FooterLine(ResultExtras e)
        {
            var parts = new List<string>();
            if (e.bestGrade >= 0f) parts.Add(Falas.ResumoRecorde + GameManager.Fmt(e.bestGrade) + (e.newRecord ? " " + Falas.ResumoRecordeNovo : ""));
            if (e.newAchievements != null)
                foreach (var c in e.newAchievements) parts.Add(Falas.ResumoConquista + Falas.NomeConquista(c));
            return string.Join("   •   ", parts);
        }
    }
}
