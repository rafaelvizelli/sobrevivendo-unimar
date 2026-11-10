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

    /// <summary>Passos do tutorial, NESTA ordem.</summary>
    public enum TutorialStep { Andar, Sentar, Responder, Celular, Nerd, Distracao, Entregar, Concluido }

    /// <summary>O que já aconteceu na partida de treino (lido das mecânicas reais: MatchLog, favores, jogador).</summary>
    public struct TutorialProgress
    {
        public bool seated;
        public float distanceFromSeat;
        public int answers, phoneUses, nerdCopies, favorsSpent;
        public bool delivered;
    }

    /// <summary>Regras puras do tutorial (sem Unity rodando, testáveis em EditMode).</summary>
    public static class TutorialSteps
    {
        public static readonly TutorialStep[] Order =
        {
            TutorialStep.Andar, TutorialStep.Sentar, TutorialStep.Responder, TutorialStep.Celular,
            TutorialStep.Nerd, TutorialStep.Distracao, TutorialStep.Entregar, TutorialStep.Concluido,
        };

        /// <summary>Distância (m) que conta como "deu uma volta".</summary>
        public const float WalkDistance = 1.5f;

        /// <summary>O passo atual já foi cumprido?</summary>
        public static bool IsDone(TutorialStep step, TutorialProgress p) => step switch
        {
            TutorialStep.Andar => !p.seated && p.distanceFromSeat >= WalkDistance,
            TutorialStep.Sentar => p.seated,
            TutorialStep.Responder => p.answers > 0,
            TutorialStep.Celular => p.phoneUses > 0,
            TutorialStep.Nerd => p.nerdCopies > 0,
            TutorialStep.Distracao => p.favorsSpent > 0,
            TutorialStep.Entregar => p.delivered,
            _ => false,
        };

        /// <summary>Avança no máximo UM passo, e só se o atual foi cumprido (não dá pra pular etapas).</summary>
        public static TutorialStep Next(TutorialStep step, TutorialProgress p) =>
            step != TutorialStep.Concluido && IsDone(step, p) ? step + 1 : step;

        /// <summary>Texto curto do passo (fica em Falas).</summary>
        public static string Text(TutorialStep step) => step switch
        {
            TutorialStep.Andar => Falas.TutorialAndar,
            TutorialStep.Sentar => Falas.TutorialSentar,
            TutorialStep.Responder => Falas.TutorialResponder,
            TutorialStep.Celular => Falas.TutorialCelular,
            TutorialStep.Nerd => Falas.TutorialNerd,
            TutorialStep.Distracao => Falas.TutorialDistracao,
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
        public TutorialStep Step { get; private set; } = TutorialStep.Andar;
        bool deliveredNow;

        void Start()
        {
            ProfessorAI.I?.Say(Falas.Pick(Falas.TutorialProfInicio), 4f);
            ShowStep();
        }

        TutorialProgress Read()
        {
            var p = PlayerController.I;
            var gm = GameManager.I;
            float dist = 0f;
            if (p != null && p.seatPoint != null)
            {
                Vector3 a = p.transform.position, b = p.seatPoint.position;
                a.y = b.y = 0f;
                dist = Vector3.Distance(a, b);
            }
            return new TutorialProgress
            {
                seated = p != null && p.IsSeated,
                distanceFromSeat = dist,
                answers = MatchLog.ConhecidasTentadas + MatchLog.Chutes,
                phoneUses = MatchLog.Celular,
                nerdCopies = MatchLog.NerdCopias,
                favorsSpent = gm != null && gm.Settings != null ? gm.Settings.favors - gm.Favors : 0,
                delivered = deliveredNow,
            };
        }

        void Update()
        {
            var gm = GameManager.I;
            if (gm == null || !gm.IsPlaying) return;
            var next = TutorialSteps.Next(Step, Read());
            if (next == Step) return;
            Step = next;
            SoundFX.Play(Sfx.Acerto);
            if (Step == TutorialStep.Concluido) gm.EndTutorial(completed: true);
            else ShowStep();
        }

        void ShowStep() => HUD.I.SetTutorial(TutorialSteps.Text(Step) + "    <color=#999999>[T] pular tutorial</color>");

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
