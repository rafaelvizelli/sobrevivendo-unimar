using UnityEngine;

namespace ProvaImpossivel
{
    /// <summary>"Já fez o tutorial?" guardado em PlayerPrefs: o tutorial só é oferecido na primeira vez.</summary>
    public static class TutorialFlag
    {
        public const string Key = "prova_impossivel.tutorial_visto";

        public static bool Done => PlayerPrefs.GetInt(Key, 0) == 1;

        public static void MarkDone()
        {
            PlayerPrefs.SetInt(Key, 1);
            PlayerPrefs.Save();
        }

        /// <summary>Volta a oferecer o tutorial (testes / quem quiser ver de novo do zero).</summary>
        public static void Clear()
        {
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// Passos do tutorial, NESTA ordem (S3, Operação Steam). O treino começa SENTADO, com o professor falando: andar
    /// e sentar se aprendem no caminho até o Nerd e até a mesa. O passo das colas de bolso (Q/G/B/F) apresenta o
    /// orçamento de colas, que antes nenhum passo explicava.
    /// </summary>
    public enum TutorialStep { Responder, Celular, ColasDeBolso, Nerd, Distracao, CaraDePaisagem, Entregar, Concluido }

    /// <summary>O que já aconteceu na partida de treino (lido das mecânicas reais: MatchLog, favores, jogador).</summary>
    public struct TutorialProgress
    {
        public int answers, phoneUses, pocketCheats, nerdCopies, favorsSpent;
        public bool delivered, faceDone; // faceDone: treinou a Cara de Paisagem (acertando ou não)
    }

    /// <summary>Regras puras do tutorial (sem Unity rodando, testáveis em EditMode).</summary>
    public static class TutorialSteps
    {
        public static readonly TutorialStep[] Order =
        {
            TutorialStep.Responder, TutorialStep.Celular, TutorialStep.ColasDeBolso, TutorialStep.Nerd,
            TutorialStep.Distracao, TutorialStep.CaraDePaisagem, TutorialStep.Entregar, TutorialStep.Concluido,
        };

        /// <summary>O passo atual já foi cumprido?</summary>
        public static bool IsDone(TutorialStep step, TutorialProgress p) => step switch
        {
            TutorialStep.Responder => p.answers > 0,
            TutorialStep.Celular => p.phoneUses > 0,
            TutorialStep.ColasDeBolso => p.pocketCheats > 0,
            TutorialStep.Nerd => p.nerdCopies > 0,
            TutorialStep.Distracao => p.favorsSpent > 0,
            TutorialStep.CaraDePaisagem => p.faceDone,
            TutorialStep.Entregar => p.delivered,
            _ => false,
        };

        /// <summary>Avança no máximo UM passo, e só se o atual foi cumprido (não dá pra pular etapas).</summary>
        public static TutorialStep Next(TutorialStep step, TutorialProgress p) =>
            step != TutorialStep.Concluido && IsDone(step, p) ? step + 1 : step;

        /// <summary>Texto curto do passo (fica em Falas).</summary>
        public static string Text(TutorialStep step) => step switch
        {
            TutorialStep.Responder => Falas.TutorialResponder,
            TutorialStep.Celular => Falas.TutorialCelular,
            TutorialStep.ColasDeBolso => Falas.TutorialColasDeBolso,
            TutorialStep.Nerd => Falas.TutorialNerd,
            TutorialStep.Distracao => Falas.TutorialDistracao,
            TutorialStep.CaraDePaisagem => Falas.TutorialCaraDePaisagem,
            TutorialStep.Entregar => Falas.TutorialEntregar,
            _ => "",
        };
    }

    /// <summary>
    /// Conduz a partida de treino: não cria mecânica nova, só observa as reais (MatchLog, favores, jogador
    /// sentado ou não) e mostra o próximo passo. Criado pelo GameManager quando o tutorial começa.
    /// </summary>
    public class TutorialDirector : MonoBehaviour
    {
        public TutorialStep Step { get; private set; } = TutorialSteps.Order[0];
        bool deliveredNow;

        void Start()
        {
            ProfessorAI.I?.Say(Falas.Pick(Falas.TutorialProfInicio), 4f);
            ShowStep();
        }

        TutorialProgress Read()
        {
            var gm = GameManager.I;
            return new TutorialProgress
            {
                answers = MatchLog.ConhecidasTentadas + MatchLog.Chutes,
                phoneUses = MatchLog.Celular,
                pocketCheats = MatchLog.ColinhaUsos + MatchLog.GarrafaUsos + MatchLog.BilheteUsos + MatchLog.FoneUsos,
                nerdCopies = MatchLog.NerdCopias,
                favorsSpent = gm != null && gm.Settings != null ? gm.Settings.favors - gm.Favors : 0,
                delivered = deliveredNow,
                faceDone = faceDone,
            };
        }

        // Entrega 1.0: treino da Cara de Paisagem — o professor "para do seu lado" e a barra abre de verdade, uma vez
        [Tooltip("Segundos entre o passo aparecer e o professor te encarar (dá tempo de ler)")] public float faceDelay = 2.5f;
        bool faceDone;
        float faceAt = -1f;

        /// <summary>O treino da Cara de Paisagem já abriu a barra? (testes)</summary>
        public bool FacePracticeStarted { get; private set; }

        void UpdateFacePractice()
        {
            if (Step != TutorialStep.CaraDePaisagem || FacePracticeStarted) return;
            if (faceAt < 0f) faceAt = Time.time + faceDelay;
            if (Time.time < faceAt) return;
            var fp = FacePokerChallenge.I;
            if (fp == null) { faceDone = true; return; } // sem o desafio na cena: não trava o treino
            if (fp.Active) return;                        // já tem uma aberta (o professor de verdade): espera
            FacePracticeStarted = true;
            ProfessorAI.I?.Say(Falas.Pick(Falas.ProfEncarando), 2f);
            fp.Begin(0f, ok => faceDone = true, practice: true);
        }

        void Update()
        {
            var gm = GameManager.I;
            if (gm == null || !gm.IsPlaying) return;
            UpdateFacePractice();
            var next = TutorialSteps.Next(Step, Read());
            if (next == Step) return;
            Step = next;
            SoundFX.Play(Sfx.Acerto);
            if (Step == TutorialStep.Concluido) gm.EndTutorial(completed: true);
            else ShowStep();
        }

        void ShowStep()
        {
            HUD.I.SetTutorial(TutorialSteps.Text(Step) + "    <color=#3C372F>[T] pular tutorial</color>");
            if (Step == TutorialStep.ColasDeBolso) HUD.I.Toast(Falas.TutorialOrcamento, HUD.Yellow, 7f, important: true);
        }

        /// <summary>O jogador entregou a prova na mesa: só conta no último passo.</summary>
        public void OnDeliver()
        {
            if (Step == TutorialStep.Entregar)
            {
                deliveredNow = true;
                return;
            }
            HUD.I.Toast(Falas.TutorialCedoDemais, HUD.Gray, 3f);
        }
    }
}
