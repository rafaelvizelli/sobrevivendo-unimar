using UnityEngine;

namespace ProvaImpossivel
{
    /// <summary>
    /// O Cara do Fundão. Pedir uma ideia a ele dá um resultado aleatório:
    /// derrubar tudo (distração), passar uma cola (que pode ser a lista do mercado) ou dar um show (distração).
    /// </summary>
    public class BackRowStudent : StudentBase
    {
        public float ideaCooldown = 30f;
        [Range(0, 1)] public float colaCorretaChance = 0.6f;

        float cooldown, nextIdle;

        public override string Prompt => "Pedir uma ideia ao Fundão" + FavorTag;

        protected override void Start()
        {
            base.Start();
            displayName = "FUNDÃO";
            nextIdle = Random.Range(25f, 45f);
        }

        public override bool CanInteract(PlayerController p) => Playing && cooldown <= 0f && HasFavor;

        [Header("Barulho do pedido (o professor ouve o LUGAR, como os outros ruídos)")]
        [Tooltip("Raio em que o 'psiu!' é ouvido (m)")] public float whisperRadius = 5f;
        [Tooltip("Suspeita do 'psiu!' (abaixo de 8 o professor distraído ignora)")] public float whisperNoise = 6f;

        public override void Interact(PlayerController p) => Resolve(p, Random.value);

        /// <summary>
        /// Pedido ao Fundão com o resultado <paramref name="roll"/> (0 a 1) já sorteado (público para testes):
        /// abaixo de 0,35 derruba tudo, até 0,7 passa cola, acima dá um show.
        /// O favor só é gasto se ele fez algo por você: a distração funcionou ou o papel foi passado.
        /// </summary>
        public void Resolve(PlayerController p, float roll)
        {
            // prova sem nada a melhorar (ou orçamento de colas esgotado, Bloco 15): a "cola" não teria efeito, então ele só faz bagunça
            bool colaUtil = ExamSystem.I.HasRoom && CheatBudget.CanAnswer;
            float colaAte = dropChance + colaChance;
            bool cola = colaUtil && roll >= dropChance && roll < colaAte;
            bool derruba = !cola && roll < colaAte;

            // bagunça que não teria efeito (professor investigando/desconfiado/ocupado): ele nem tenta.
            // Sem "psiu", sem recarga e sem favor gasto — o pedido simplesmente não acontece.
            if (!cola && (Prof == null || !Prof.CanBeDistractedNow))
            {
                if (Prof == null || !ToastDistractionRefused(Prof.State))
                    HUD.I.Toast("O Fundão olhou pro professor e desistiu: agora não dá. (favor não gasto)", HUD.Gray, 3f);
                return;
            }

            cooldown = ideaCooldown;
            MatchLog.FundaoPedidos++;
            bool fezAlgo;
            if (cola)
            {
                Whisper(); // pedir a cola é cochichar
                fezAlgo = GiveCola(p);
            }
            else
            {
                fezAlgo = derruba ? DropMaterials(true) : ShowOff();
                Whisper(); // depois da bagunça: distraído, o professor ignora o cochicho (não sabota a própria distração)
            }
            if (fezAlgo) GameManager.I.SpendFavor();
        }

        [Header("Resultado do pedido (a dificuldade sobrescreve)")]
        [Tooltip("Chance (0 a 1) de derrubar tudo")] [Range(0, 1)] public float dropChance = 0.35f;
        [Tooltip("Chance (0 a 1) de passar cola (o resto é o show)")] [Range(0, 1)] public float colaChance = 0.35f;

        /// <summary>Pedir algo ao Fundão é cochichar no fundo da sala: com o professor perto (e atento), ele olha pra cá.</summary>
        void Whisper() => Noise.Emit(transform.position, whisperRadius, whisperNoise, "um 'psiu, passa a cola!' no fundão", byPlayer: true);

        /// <summary>Derruba estojo e materiais: o professor vai até o fundão. Retorna true se ele foi.</summary>
        public bool DropMaterials(bool askedByPlayer)
        {
            Say(Falas.Pick(Falas.FundaoDerruba), 3f);
            SoundFX.Play(Sfx.Evento);
            var before = Prof != null ? Prof.State : ProfState.Tranquilo;
            bool ok = Prof != null && Prof.Distract(transform.position, 8f, Falas.Pick(Falas.ProfBarulho), true);
            if (askedByPlayer && (ok || !ToastDistractionRefused(before)))
                HUD.I.Toast(ok ? "O Fundão derrubou TUDO de propósito. O professor foi lá. Distração!" : "O Fundão derrubou tudo... e ninguém ligou. (favor não gasto)", ok ? HUD.Yellow : HUD.Gray, 3f);
            return ok;
        }

        /// <summary>Passa o papel da cola. Retorna true se o papel foi passado (mesmo que tenha dado flagrante).</summary>
        bool GiveCola(PlayerController p)
        {
            if (!ExamSystem.I.HasRoom) return false; // prova cheia: não há o que colar

            // passar papel é arriscado: se o professor estiver te vendo, é flagrante; e mesmo sem olhar pra você
            // o papel pode cair / fazer barulho (chance da dificuldade). Ao telefone, ele não percebe nada.
            var prof = Prof;
            float chance = GameManager.I != null && GameManager.I.Settings != null ? GameManager.I.Settings.backRowCaughtChance : 0f;
            bool pego = prof != null && !prof.OnPhone && (prof.Visibility > 0f || Random.value < chance);
            if (pego && prof.CatchRedHanded("pegando cola do Fundão", Infracao.ColaFundao))
            {
                Say("Ih... o papel caiu bem no pé dele.", 3f);
                return true;
            }

            bool good = Random.value < colaCorretaChance;
            if (good && !CheatBudget.Spend()) good = false; // sem orçamento a cola "boa" não vale nada
            if (good)
            {
                MatchLog.FundaoColaBoa++;
                Say(Falas.Pick(Falas.FundaoColaBoa), 3f);
                ExamSystem.I.MarkCorrect();
                SoundFX.Play(Sfx.Ponto);
                HUD.I.Toast("+" + GameManager.Fmt(ExamSystem.I.PointsPerQuestion) + " — A cola do Fundão funcionou?! Inacreditável.", HUD.Green, 3f);
            }
            else
            {
                MatchLog.FundaoColaRuim++;
                Say(Falas.Pick(Falas.FundaoColaRuim), 3f);
                // só "estraga" uma questão se houver uma em branco; senão a mensagem não pode dizer que errou algo
                bool estragou = ExamSystem.I.MarkWrong() >= 0;
                SoundFX.Play(Sfx.PerdePonto);
                HUD.I.Toast(estragou ? "A cola era a lista do mercado. Você escreveu 'arroz, feijão, ovo' na questão. ERRADA!"
                                     : "A cola era a lista do mercado. Ainda bem que você desconfiou e não copiou.", estragou ? HUD.Red : HUD.Gray, 3.5f);
            }
            return true;
        }

        bool ShowOff()
        {
            Say(Falas.Pick(Falas.FundaoShow), 3.5f);
            SoundFX.Play(Sfx.Evento);
            visual.handRaise = 1f;
            Invoke(nameof(LowerHand), 3f);
            var before = Prof != null ? Prof.State : ProfState.Tranquilo;
            bool ok = Prof != null && Prof.Distract(transform.position, 7f, "Algum problema aí no fundão?!", true);
            if (ok || !ToastDistractionRefused(before))
                HUD.I.Toast(ok ? "O Fundão resolveu dar um show. O professor está indo lá, furioso. Distração!" : "O Fundão deu um show... pra ninguém. (favor não gasto)", ok ? HUD.Yellow : HUD.Gray, 3f);
            return ok;
        }

        void LowerHand() => visual.handRaise = 0f;

        /// <summary>Recebeu um bilhete do jogador (Bloco 15): a resposta volta depois (e pode ser a lista do mercado).</summary>
        public void ReceiveNote() => Say(Falas.Pick(Falas.FundaoBilhete), 2.5f);

        void Update()
        {
            if (cooldown > 0f) cooldown -= Time.deltaTime;
            visual.leanBack = 14f;
            visual.writing = false;
            visual.expression = Expression.Convencido;
            if (!Playing) return;
            IdleLook(Time.deltaTime, 50f);
            nextIdle -= Time.deltaTime;
            if (nextIdle <= 0f)
            {
                nextIdle = Random.Range(35f, 60f);
                Say(Falas.Pick(Falas.FundaoIdle));
            }
        }
    }
}
