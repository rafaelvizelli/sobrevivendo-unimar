using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProvaImpossivel
{
    /// <summary>
    /// Sorteia acontecimentos cômicos durante a prova: OPORTUNIDADES (distraem o professor) ou AMEAÇAS (ele fica
    /// atento / vem pro seu lado), pelos pesos da dificuldade. As oportunidades podem ter desdobramentos
    /// sorteados (começo → consequência → reação), então partidas diferentes
    /// terminam de jeitos diferentes. Nunca repete os dois últimos acontecimentos.
    /// </summary>
    public class EventDirector : MonoBehaviour
    {
        public SleeperStudent dorminhoco;
        public BackRowStudent fundao;
        public NerdStudent nerd;
        public SnitchStudent puxaSaco;
        public ProjectorDevice projetor;
        public ExtraStudent[] figurantes;
        [Tooltip("Para onde o professor olha quando atende o telefone")] public Transform janela;
        public float firstEventAt = 30f;

        float next;
        readonly List<string> history = new();
        bool running;

        void Start()
        {
            next = firstEventAt;
            if (puxaSaco == null) puxaSaco = FindAnyObjectByType<SnitchStudent>();
        }

        static bool Playing => GameManager.I != null && GameManager.I.IsPlaying;
        static ProfessorAI Prof => ProfessorAI.I;
        /// <summary>A cadeia continua só se o jogo segue e o professor não está ocupado com um flagrante/desafio.</summary>
        static bool CanContinue => Playing && Prof != null && !Prof.Busy;

        void Update()
        {
            if (!Playing || running) return;
            var prof = Prof;
            // desconfiado também espera: com ele desconfiado as distrações não pegam, e a ameaça já está na sala
            if (prof == null || prof.Busy || prof.State != ProfState.Tranquilo) return;

            next -= Time.deltaTime;
            if (next > 0f) return;
            var s = GameManager.I.Settings;
            next = UnityEngine.Random.Range(s.eventInterval.x, s.eventInterval.y);
            RunRandom(s);
        }

        /// <summary>
        /// Sorteio ponderado: devolve o índice escolhido em <paramref name="weights"/> para um número <paramref name="roll"/>
        /// entre 0 e 1. Pesos ≤ 0 nunca saem; se todos forem ≤ 0, devolve -1. Estático e puro para ser testável.
        /// </summary>
        public static int WeightedIndex(IReadOnlyList<float> weights, double roll)
        {
            double total = 0;
            for (int i = 0; i < weights.Count; i++) if (weights[i] > 0f) total += weights[i];
            if (total <= 0) return -1;
            double acc = 0, target = Math.Clamp(roll, 0.0, 0.999999) * total;
            for (int i = 0; i < weights.Count; i++)
            {
                if (weights[i] <= 0f) continue;
                acc += weights[i];
                if (target < acc) return i;
            }
            for (int i = weights.Count - 1; i >= 0; i--) if (weights[i] > 0f) return i;
            return -1;
        }

        /// <summary>
        /// Escolhe se o próximo acontecimento é AMEAÇA (true) ou OPORTUNIDADE (false), pelos pesos da dificuldade.
        /// Se um dos lados não tiver nenhum acontecimento disponível, usa o outro.
        /// </summary>
        public static bool PickThreat(float opportunityWeight, float threatWeight, double roll, bool hasOpportunities = true, bool hasThreats = true)
        {
            if (!hasThreats) return false;
            if (!hasOpportunities) return true;
            return WeightedIndex(new[] { opportunityWeight, threatWeight }, roll) == 1;
        }

        void RunRandom(DifficultySettings s)
        {
            var good = new List<(string id, Func<IEnumerator> run)>();
            if (dorminhoco != null) good.Add(("ronco", Ronco));
            if (fundao != null) good.Add(("fundao", FundaoDerruba));
            if (projetor != null && !projetor.Failing) good.Add(("projetor", ProjetorCaos));
            if (janela != null) good.Add(("ligacao", Ligacao));
            if (nerd != null) good.Add(("nerd", NerdSurta));
            if (figurantes != null && Array.Exists(figurantes, f => f != null && f.CanDropBag)) good.Add(("mochila", Mochila));

            var bad = new List<(string id, Func<IEnumerator> run)>();
            if (PlayerController.I != null)
            {
                bad.Add(("celulares", RecolheCelulares));
                bad.Add(("ronda", MudaARonda));
                if (FiguranteMaisPerto() != null)
                {
                    bad.Add(("silencio", PedemSilencio));
                    bad.Add(("espirro", Espirro));
                }
            }
            if (good.Count == 0 && bad.Count == 0) return;

            bool threat = PickThreat(s.opportunityEventWeight, s.threatEventWeight, UnityEngine.Random.value, good.Count > 0, bad.Count > 0);
            var options = threat ? bad : good;
            var fresh = options.FindAll(o => !history.Contains(o.id));
            if (fresh.Count > 0) options = fresh;
            var pick = options[UnityEngine.Random.Range(0, options.Count)];
            history.Add(pick.id);
            MatchLog.Eventos++;
            while (history.Count > 2) history.RemoveAt(0);
            StartCoroutine(Run(pick.run()));
        }

        IEnumerator Run(IEnumerator chain)
        {
            running = true;
            yield return chain;
            running = false;
        }

        // Sem texto de "ACONTECIMENTO" nem "janela de oportunidade" no HUD: o jogador percebe pelo que acontece
        // na sala (falas, sons, o professor andando). Os balões de fala de cada acontecimento continuam.

        static DifficultySettings Settings => GameManager.I.Settings;

        /// <summary>O figurante sentado mais perto do jogador (quem reclama/espirra "do seu lado").</summary>
        ExtraStudent FiguranteMaisPerto()
        {
            var p = PlayerController.I;
            if (p == null || figurantes == null) return null;
            ExtraStudent best = null;
            float bestD = float.MaxValue;
            foreach (var f in figurantes)
            {
                if (f == null) continue;
                float d = (f.transform.position - p.transform.position).sqrMagnitude;
                if (d < bestD) { bestD = d; best = f; }
            }
            return best;
        }

        /// <summary>Ponto de patrulha mais perto do jogador (para onde o professor vai quando a ronda "muda de rumo").</summary>
        static Vector3 PontoPertoDoJogador()
        {
            var p = PlayerController.I;
            var pts = Prof.patrolPoints;
            if (pts == null || pts.Length == 0) return p.transform.position;
            Vector3 best = pts[0].position;
            float bestD = float.MaxValue;
            foreach (var t in pts)
            {
                if (t == null) continue;
                float d = (t.position - p.transform.position).sqrMagnitude;
                if (d < bestD) { bestD = d; best = t.position; }
            }
            return best;
        }

        // ------------------------------------------------------------- ameaças

        // conferência de celulares: ele fica atento e vem até o seu corredor; quem estiver no celular agora é pego
        IEnumerator RecolheCelulares()
        {
            Prof.Say("Celular em cima da mesa, tela pra baixo! Vou passar conferindo um por um.", 3.5f);
            Prof.BeAlert(Settings.threatAlertSeconds);
            Prof.WalkTo(PontoPertoDoJogador(), stareAtPlayer: true);
            yield break;
        }

        // um colega do seu lado reclama do barulho: a suspeita sobe e o professor olha pra cá
        IEnumerator PedemSilencio()
        {
            var f = FiguranteMaisPerto();
            f.Say("Shhh! Dá pra parar com esse barulho aí? Tem gente tentando pensar!", 3f);
            yield return new WaitForSeconds(1.2f);
            if (!CanContinue) yield break;
            Prof.Alert(PlayerController.I.transform.position, Settings.threatSuspicion, "alguém reclamou de barulho aí");
        }

        // o professor resolve fazer a ronda pelo seu lado da sala e para pra encarar
        IEnumerator MudaARonda()
        {
            Prof.Say("Hmm... hoje eu vou dar uma volta por ESSE lado da sala.", 3f);
            Prof.WalkTo(PontoPertoDoJogador(), stareAtPlayer: true);
            yield break;
        }

        // um colega do seu lado espirra: barulho que o professor ouve de qualquer lugar e que aponta pra sua fileira
        IEnumerator Espirro()
        {
            var f = FiguranteMaisPerto();
            f.Say("ATCHIM!", 1.5f);
            Noise.Emit(f.transform.position, 30f, Settings.threatSuspicion * 1.5f, "um espirro", byPlayer: false);
            yield break;
        }

        // ------------------------------------------------------------- oportunidades (cadeias)

        // projetor falha → Fundão aproveita o escuro → professor vai pra cima dele → Puxa-saco pode virar pra vigiar
        IEnumerator ProjetorCaos()
        {
            projetor.Fail(9f);
            yield return new WaitForSeconds(3.5f);
            if (!CanContinue || fundao == null || UnityEngine.Random.value > 0.6f) yield break;

            fundao.Say("*no escuro* Ô professor, tá passando o quê, Netflix? KKKK", 3f);
            Prof.EndDistraction();
            yield return null;
            Prof.Distract(fundao.transform.position, 6f, "QUEM FALOU ISSO?! Foi você, né, do fundão?!", true);
            yield return new WaitForSeconds(2.5f);
            if (!CanContinue || puxaSaco == null || UnityEngine.Random.value > 0.6f) yield break;

            puxaSaco.ForceWatch();
            puxaSaco.Say("*aproveita a confusão e vira pra trás pra vigiar*", 2.5f);
        }

        // ronco → professor acorda o Dorminhoco → às vezes o Nerd reclama → professor volta irritado
        IEnumerator Ronco()
        {
            yield return dorminhoco.SnoreWarning(Settings.snoreWarningSeconds); // cabeceia e os "z" crescem antes
            if (!CanContinue) yield break;
            dorminhoco.Snore();
            yield return new WaitForSeconds(5.5f);
            if (!CanContinue || nerd == null || UnityEngine.Random.value > 0.5f) yield break;

            nerd.Say("Dá pra parar? Tô tentando fazer a questão bônus!", 3f);
            yield return new WaitForSeconds(1.5f);
            if (!CanContinue) yield break;
            Prof.Say("Silêncio! Agora EU vou ficar de olho em TODO MUNDO.", 3f);
            Prof.BeAlert(12f); // a ameaça vale: a janela de oportunidade cobra o preço logo depois
        }

        // telefone → (às vezes) o Fundão dá a dica → fim da ligação com o professor mais atento
        IEnumerator Ligacao()
        {
            SoundFX.PlayAt(Sfx.Evento, Prof.visual.HeadPosition); // o toque do celular dele (som no mundo, não aviso)
            Prof.Distract(janela.position, 9f, Falas.Pick(Falas.ProfLigacao), false, phone: true);
            yield return new WaitForSeconds(3f);
            if (CanContinue && fundao != null && UnityEngine.Random.value < 0.5f)
                fundao.Say("Psiu! Ele tá no telefone... É AGORA, VAI!", 2.5f);
            while (CanContinue && Prof.OnPhone) yield return null;
            if (CanContinue)
            {
                Prof.Say("...Onde eu estava? Ah, sim. VIGIANDO VOCÊS.", 3f);
                Prof.BeAlert(12f);
            }
        }

        // Nerd surta → às vezes o Fundão manda ele calar a boca → o professor olha pros dois
        IEnumerator NerdSurta()
        {
            nerd.ShoutEasy();
            yield return new WaitForSeconds(2.5f);
            if (!CanContinue || fundao == null || UnityEngine.Random.value > 0.5f) yield break;

            fundao.Say("Ô gênio, fala baixo que tem gente sofrendo aqui!", 3f);
            Prof.EndDistraction();
            yield return null;
            Prof.Distract(fundao.transform.position, 4f, "Silêncio, vocês dois!", false);
        }

        // mochila cai → às vezes o Puxa-saco vai "ajudar" e prende o professor mais tempo
        IEnumerator Mochila()
        {
            var bags = Array.FindAll(figurantes, f => f != null && f.CanDropBag);
            var who = bags[UnityEngine.Random.Range(0, bags.Length)];
            who.DropBag();
            yield return new WaitForSeconds(2f);
            if (!CanContinue || puxaSaco == null || UnityEngine.Random.value > 0.5f) yield break;

            puxaSaco.Say("Professor, deixa que eu ajudo a juntar! O senhor é muito ocupado!", 3f);
            Prof.EndDistraction();
            yield return null;
            Prof.Distract(who.transform.position, 7f, "Ah... obrigado. Pelo menos alguém aqui presta.", true);
        }

        IEnumerator FundaoDerruba()
        {
            fundao.DropMaterials(false);
            yield break;
        }
    }
}
