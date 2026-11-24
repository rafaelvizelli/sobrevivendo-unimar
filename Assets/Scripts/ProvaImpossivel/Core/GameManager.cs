using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace ProvaImpossivel
{
    public enum Difficulty { Facil, Normal, Dificil }
    public enum Phase { Intro, Playing, Paused, Ended }
    public enum LevelResult { Aprovado, AprovadoNoSufoco, Reprovado, Flagrado }

    /// <summary>
    /// O QUE o jogador estava fazendo de errado. As regras (ex.: confiscar o celular) decidem por aqui,
    /// nunca pelo texto do motivo, que é só para exibir e pode mudar à vontade.
    /// </summary>
    public enum Infracao { Nenhuma, Celular, EspiarNerd, ColaFundao, ForaDoLugar, Correndo, Dedurado, CaraDeCulpado, Outra,
                           ColinhaNaManga, Bilhete, CelularTocando, Garrafinha }

    /// <summary>
    /// TODOS os números de balanceamento de uma dificuldade, num lugar só (editáveis no Inspector do GameManager).
    /// Referência de ritmo: suspeita/seg = taxa da ação × visibilidade (0,35 a 1,4) × multiplicador, depois do tempo de reação.
    /// </summary>
    [Serializable]
    public class DifficultySettings
    {
        public string name = "Normal";
        [Tooltip("Duração da prova em segundos")] public float duration = 360f;
        [Tooltip("Nota mínima (0 a 10)")] public float minGrade = 7f;
        [Tooltip("Flagrantes até ser expulso")] public int maxFlagrantes = 3;

        [Header("Professor")]
        [Tooltip("Multiplica todo ganho de suspeita")] public float suspicionMultiplier = 0.8f;
        [Tooltip("Até onde ele enxerga (m)")] public float viewDistance = 12f;
        [Tooltip("Meio ângulo do campo de visão quando calmo (graus)")] public float halfFov = 48f;
        [Tooltip("Segundos vendo algo suspeito até a suspeita começar a subir de verdade")] public float reactionTime = 0.6f;
        [Tooltip("Quanto a suspeita cai por segundo quando você se comporta")] public float decayPerSecond = 6f;
        [Tooltip("Depois de um flagrante, segundos em que ele dá um tempo e volta pra frente")] public float graceAfterFlagrante = 10f;

        [Header("Penalidades")]
        [Tooltip("O primeiro flagrante é só uma advertência (não tira ponto)")] public bool firstFlagranteIsWarning;
        [Tooltip("Ser pego no celular faz o professor confiscá-lo")] public bool confiscatePhone = true;
        [Tooltip("Treino: o professor ameaça mas nunca dá flagrante (só a dificuldade do tutorial usa)")] public bool professorNeverCatches;

        [Header("Prova (Desafio B)")]
        [Tooltip("Quantas das 10 questões o jogador sabe responder sozinho. As outras: chute (~25%) ou cola.")]
        [Range(0, 10)] public int knownQuestions = 3;
        [Tooltip("Segundos para responder cada pergunta")] public float quizTime = 11f;
        [Tooltip("Segundos 'pensando' entre uma pergunta e outra")] public float quizCooldown = 8f;
        [Tooltip("0 = perguntas mais fáceis, 2 = mais difíceis")] [Range(0, 2)] public int quizTier = 1;

        [Header("Ações arriscadas (suspeita por segundo enquanto o professor vê)")]
        public float phoneSuspicion = 75f;
        public float copySuspicion = 30f;
        [Tooltip("Consultas no celular por partida (a bateria está em 3%)")] public int phoneCharges = 1;
        [Tooltip("Segundos entre as olhadas de lado do Nerd (se ele olhar enquanto você espia, ele te pega)")]
        public Vector2 nerdGlanceInterval = new(2f, 12f);
        [Tooltip("Cópias que dá pra tirar do Nerd por partida")] public int nerdCopies = 2;
        [Tooltip("Chance de a cola do Fundão ser pega (flagrante) mesmo com o professor sem olhar pra você (ao telefone, nunca)")]
        [Range(0, 1)] public float backRowCaughtChance = 0.3f;

        [Header("Acontecimentos: ameaças x oportunidades")]
        [Tooltip("Peso de sortear um acontecimento BOM (distrai o professor)")] public float opportunityEventWeight = 0.5f;
        [Tooltip("Peso de sortear um acontecimento RUIM (o professor fica atento / vem pro seu lado)")] public float threatEventWeight = 0.5f;
        [Tooltip("Segundos de 'ATENTO' depois de uma ameaça")] public float threatAlertSeconds = 12f;
        [Tooltip("Suspeita que uma ameaça (pedido de silêncio, espirro) joga no professor")] public float threatSuspicion = 40f;
        [Tooltip("Desconfiado, ele ainda pode ser distraído? (falso = só funciona com ele tranquilo ou já distraído)")]
        public bool distractWhileSuspicious;

        [Header("Leitura: o que os personagens mostram no corpo (ajuda visual)")]
        [Tooltip("Segundos que o professor para (pigarro + '...') antes de se virar para outro lado da sala")]
        public float turnTellSeconds = 0.6f;
        [Tooltip("Segundos de aviso do Dorminhoco (cabeça balançando, 'zzz' crescendo) antes do ronco")]
        public float snoreWarningSeconds = 3f;
        [Tooltip("Barrinha discreta de suspeita em cima da cabeça do professor")]
        public bool showSuspicionMeter = true;
        [Tooltip("Ícones de aviso em cima do Nerd e do Puxa-saco (além dos sinais no corpo deles)")]
        public bool showStudentHints = true;
        [Tooltip("Distância (m) até onde o jogador vê os ícones de aviso do Nerd e do Puxa-saco")]
        public float studentHintDistance = 3.4f;
        [Tooltip("Acima deste ângulo (graus) a mudança de rumo do professor conta como 'virar' e ganha o aviso")]
        public float turnTellAngle = 100f;

        [Header("Detalhes (antes fixos no código)")]
        [Tooltip("Raio (m) do espirro do colega (ameaça)")] public float sneezeRadius = 30f;
        [Tooltip("Suspeita do espirro = threatSuspicion × isto")] public float sneezeNoiseFactor = 1.5f;
        [Tooltip("Pego no ato durante a trégua: em vez do flagrante, esta suspeita")] public float graceCatchSuspicion = 30f;
        [Tooltip("Treino: suspeita que sobra depois do 'isso era flagrante'")] public float practiceCatchSuspicion = 40f;
        [Tooltip("Fundão: chance (0 a 1) de derrubar tudo")] [Range(0, 1)] public float backRowDropChance = 0.35f;
        [Tooltip("Fundão: chance (0 a 1) de passar cola (depois de 'derrubar'; o resto é o show)")] [Range(0, 1)] public float backRowColaChance = 0.35f;
        [Tooltip("Fundão: chance de a cola ser boa (senão é a lista do mercado)")] [Range(0, 1)] public float backRowGoodColaChance = 0.6f;
        [Tooltip("Nerd: segundos parado (aviso) antes de olhar de lado")] public float nerdGlanceWarning = 1.5f;
        [Tooltip("Nerd: segundos olhando de lado")] public float nerdGlanceDuration = 1.8f;

        [Header("Acontecimentos")]
        public Vector2 eventInterval = new(38f, 56f);
        [Tooltip("Multiplica a duração das distrações do professor")] public float distractionMultiplier = 1f;
        [Tooltip("Intervalo entre as viradas do Puxa-saco")] public Vector2 snitchInterval = new(12f, 20f);
        [Tooltip("Favores da turma por partida: pedir distração ao Puxa-saco, Dorminhoco ou Fundão gasta um")]
        public int favors = 2;
        [Tooltip("Segundos do arremesso da borracha (favor com mira, B no Dorminhoco ou no Puxa-saco)")] public float borrachaThrowTime = 0.5f;
        [Tooltip("Suspeita/s se o professor vê você jogando a borracha")] public float borrachaThrowSuspicion = 30f;

        [Header("Desafio A — Cara de Paisagem")]
        [Tooltip("Velocidade do ponteiro (idas e voltas por segundo)")] public float facePokerSpeed = 0.6f;
        [Tooltip("Largura da zona verde (fração da barra)")] public float facePokerZone = 0.22f;
        [Tooltip("Erros permitidos")] public int facePokerMisses = 1;

        [Header("Orçamento de colas (Bloco 15)")]
        [Tooltip("Respostas que TODAS as colas somadas podem dar na partida (celular, Nerd, Fundão, colinha, bilhete, fone, garrafinha). " +
                 "S2: conhecidas + orçamento = nota mínima (quem cola sem ser pego passa; o chute é só margem)")]
        public int maxCheatAnswers = 4;

        [Header("Colinha na manga (Q, sentado)")]
        [Tooltip("Consultas por partida")] public int colinhaCharges = 2;
        [Tooltip("Segundos segurando Q para ler")] public float colinhaTime = 1f;
        [Tooltip("Suspeita/s se o professor VÊ (como celular e Nerd)")] public float colinhaSuspicion = 55f;
        [Tooltip("Perto assim (m), ele percebe mesmo sem olhar pra você")] public float colinhaDangerDistance = 3.5f;
        [Tooltip("Suspeita/s quando ele está perto, mesmo fora do cone de visão")] public float colinhaNearSuspicion = 50f;
        [Tooltip("S2: colinha VISTA de perto (depois do tempo de reação) é flagrante na hora, sem esperar a suspeita chegar a 100")]
        public bool colinhaInstantCatch = true;
        [Tooltip("S2: visibilidade mínima (0,35 longe/borda a 1,4 colado/centro) para a colinha vista virar flagrante na hora")]
        public float colinhaCatchVisibility = 0.7f;

        [Header("Investigação acha provas (S2)")]
        [Tooltip("Chance (0 a 1) de, investigando o barulho do papel, ele achar o bilhete com você → flagrante 'Bilhete'")]
        [Range(0, 1)] public float bilheteFindChance = 0.5f;
        [Tooltip("Chance (0 a 1) de, investigando o celular que vibrou, ele achar o celular → flagrante 'CelularTocando'")]
        [Range(0, 1)] public float foneRingFindChance = 0.6f;
        [Tooltip("Segundos em que o barulho de uma cola ainda deixa a prova 'achável' na sua carteira")]
        public float evidenceMemorySeconds = 20f;

        [Header("Bilhete (B, sentado, mirando no Nerd ou no Fundão)")]
        [Tooltip("Bilhetes por partida")] public int bilheteCharges = 2;
        [Tooltip("Segundos até a resposta voltar")] public float bilheteDelay = 8f;
        [Tooltip("Segundos do arremesso (a ação suspeita)")] public float bilheteThrowTime = 0.6f;
        [Tooltip("Suspeita/s mirando o bilhete (braço armado com a bolinha na mão, antes de jogar)")] public float bilheteAimSuspicion = 10f;
        [Tooltip("Suspeita/s se o professor vê o arremesso")] public float bilheteThrowSuspicion = 60f;
        [Tooltip("Raio (m) em que o arremesso é ouvido")] public float bilheteNoiseRadius = 5f;
        [Tooltip("Suspeita do barulho do papel")] public float bilheteNoise = 6f;
        [Tooltip("Chance (0 a 1) de errar a mesa, por metro até o destino")] [Range(0, 1)] public float bilheteMissChancePerMeter = 0.06f;
        [Tooltip("Professor a menos disto (m) de você ou do destino: o bilhete se perde")] public float bilheteLostDistance = 2.5f;
        [Tooltip("Graus entre a câmera e o colega para valer como mira")] public float bilheteAimAngle = 30f;

        [Header("Fone com contato de fora (F, sentado)")]
        [Tooltip("Pedidos por partida")] public int foneCharges = 1;
        [Tooltip("Segundos até a resposta chegar")] public float foneDelay = 15f;
        [Tooltip("Segundos segurando F para mandar a mensagem e para ler a resposta (é usar o celular)")] public float foneUseTime = 1.2f;
        [Tooltip("Chance (0 a 1) de o celular vibrar/tocar enquanto você espera")] [Range(0, 1)] public float foneRingChance = 0.45f;
        [Tooltip("Raio (m) em que o toque é ouvido")] public float foneRingRadius = 6f;
        [Tooltip("Suspeita do toque (alto o bastante faz o professor investigar)")] public float foneRingNoise = 45f;
        [Tooltip("Segundos tocando")] public float foneRingSeconds = 3f;
        [Tooltip("Suspeita/s se o professor vê o celular tocando")] public float foneRingSuspicion = 60f;

        [Header("Garrafinha com rótulo (G, sentado)")]
        [Tooltip("Consultas por partida")] public int garrafaCharges = 2;
        [Tooltip("Segundos segurando G (bebendo e lendo o rótulo)")] public float garrafaTime = 1.5f;
        [Tooltip("Suspeita/s se o professor vê (beber água é quase normal)")] public float garrafaSuspicion = 12f;
        [Tooltip("Raio (m) em que o gole é ouvido")] public float garrafaNoiseRadius = 5f;
        [Tooltip("Suspeita do gole barulhento (acima de 8, nem distraído ele ignora)")] public float garrafaNoise = 14f;
    }

    /// <summary>Fluxo da fase: introdução, cronômetro, pausa, flagrantes, entrega da prova e finais.</summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager I { get; private set; }

        public DifficultySettings facil = new()
        {
            name = "Fácil", duration = 480f, minGrade = 6f, maxFlagrantes = 3,
            suspicionMultiplier = 0.5f, viewDistance = 10f, halfFov = 40f, reactionTime = 1.0f, decayPerSecond = 8f, graceAfterFlagrante = 14f,
            firstFlagranteIsWarning = true, confiscatePhone = true,
            knownQuestions = 3, quizTime = 15f, quizCooldown = 3f, quizTier = 0,
            phoneSuspicion = 50f, copySuspicion = 26f, phoneCharges = 3, nerdGlanceInterval = new Vector2(6f, 20f), nerdCopies = 3,
            backRowCaughtChance = 0.15f, opportunityEventWeight = 0.65f, threatEventWeight = 0.35f, threatAlertSeconds = 10f, threatSuspicion = 25f,
            turnTellSeconds = 0.9f, snoreWarningSeconds = 4f, showSuspicionMeter = true, showStudentHints = true,
            eventInterval = new Vector2(30f, 46f), distractionMultiplier = 1.4f, snitchInterval = new Vector2(18f, 28f), favors = 5,
            facePokerSpeed = 0.45f, facePokerZone = 0.28f, facePokerMisses = 2,
            maxCheatAnswers = 5,
            colinhaCharges = 3, colinhaTime = 1f, colinhaSuspicion = 40f, colinhaDangerDistance = 2.5f, colinhaNearSuspicion = 35f,
            colinhaInstantCatch = true, colinhaCatchVisibility = 0.9f, bilheteFindChance = 0.3f, foneRingFindChance = 0.4f,
            bilheteCharges = 3, bilheteDelay = 6f, bilheteThrowTime = 0.6f, bilheteThrowSuspicion = 45f, bilheteNoiseRadius = 4f, bilheteNoise = 5f,
            bilheteMissChancePerMeter = 0.04f, bilheteLostDistance = 2f, bilheteAimAngle = 35f,
            foneCharges = 1, foneDelay = 12f, foneUseTime = 1.2f, foneRingChance = 0.3f, foneRingRadius = 5f, foneRingNoise = 30f,
            foneRingSeconds = 3f, foneRingSuspicion = 45f,
            garrafaCharges = 3, garrafaTime = 1.5f, garrafaSuspicion = 8f, garrafaNoiseRadius = 4f, garrafaNoise = 10f,
        };
        public DifficultySettings normal = new();
        public DifficultySettings dificil = new()
        {
            name = "Difícil", duration = 300f, minGrade = 7f, maxFlagrantes = 3,
            suspicionMultiplier = 1.1f, viewDistance = 14f, halfFov = 55f, reactionTime = 0.35f, decayPerSecond = 4f, graceAfterFlagrante = 7f,
            firstFlagranteIsWarning = false, confiscatePhone = true,
            knownQuestions = 3, quizTime = 8f, quizCooldown = 14f, quizTier = 2,
            phoneSuspicion = 85f, copySuspicion = 34f, phoneCharges = 1, nerdGlanceInterval = new Vector2(1.5f, 9f), nerdCopies = 2,
            backRowCaughtChance = 0.45f, opportunityEventWeight = 0.35f, threatEventWeight = 0.65f, threatAlertSeconds = 15f, threatSuspicion = 50f,
            turnTellSeconds = 0.35f, snoreWarningSeconds = 2f, showSuspicionMeter = false, showStudentHints = false,
            eventInterval = new Vector2(45f, 65f), distractionMultiplier = 0.8f, snitchInterval = new Vector2(8f, 14f), favors = 2,
            facePokerSpeed = 0.75f, facePokerZone = 0.17f, facePokerMisses = 1,
            maxCheatAnswers = 4,
            colinhaCharges = 1, colinhaTime = 1.2f, colinhaSuspicion = 65f, colinhaDangerDistance = 4.5f, colinhaNearSuspicion = 65f,
            colinhaInstantCatch = true, colinhaCatchVisibility = 0.6f, bilheteFindChance = 0.7f, foneRingFindChance = 0.8f,
            bilheteCharges = 2, bilheteDelay = 10f, bilheteThrowTime = 0.6f, bilheteThrowSuspicion = 70f, bilheteNoiseRadius = 6f, bilheteNoise = 7f,
            bilheteMissChancePerMeter = 0.08f, bilheteLostDistance = 3f, bilheteAimAngle = 25f,
            foneCharges = 1, foneDelay = 18f, foneUseTime = 1.2f, foneRingChance = 0.6f, foneRingRadius = 7f, foneRingNoise = 60f,
            foneRingSeconds = 3f, foneRingSuspicion = 75f,
            garrafaCharges = 2, garrafaTime = 1.5f, garrafaSuspicion = 16f, garrafaNoiseRadius = 6f, garrafaNoise = 18f,
        };
        /// <summary>Prova de TREINO (tutorial): professor que não flagra, sem acontecimentos sorteados, uma de cada cola.</summary>
        public DifficultySettings tutorial = new()
        {
            name = "Treino", duration = 600f, minGrade = 1f, maxFlagrantes = 99,
            suspicionMultiplier = 0.5f, viewDistance = 10f, halfFov = 40f, reactionTime = 1.0f, decayPerSecond = 8f, graceAfterFlagrante = 6f,
            firstFlagranteIsWarning = true, confiscatePhone = false, professorNeverCatches = true,
            knownQuestions = 1, quizTime = 20f, quizCooldown = 2f, quizTier = 0,
            phoneSuspicion = 50f, copySuspicion = 26f, phoneCharges = 1, nerdGlanceInterval = new Vector2(12f, 20f), nerdCopies = 1,
            backRowCaughtChance = 0f, opportunityEventWeight = 1f, threatEventWeight = 0f, threatAlertSeconds = 10f, threatSuspicion = 25f,
            distractWhileSuspicious = true,
            turnTellSeconds = 0.9f, snoreWarningSeconds = 4f, showSuspicionMeter = true, showStudentHints = true,
            eventInterval = new Vector2(9999f, 9999f), distractionMultiplier = 1.4f, snitchInterval = new Vector2(25f, 35f), favors = 2,
            facePokerSpeed = 0.45f, facePokerZone = 0.28f, facePokerMisses = 2,
            maxCheatAnswers = 10,
            colinhaCharges = 1, colinhaTime = 1f, colinhaSuspicion = 40f, colinhaDangerDistance = 2.5f, colinhaNearSuspicion = 35f,
            bilheteCharges = 1, bilheteDelay = 5f, bilheteThrowTime = 0.6f, bilheteThrowSuspicion = 45f, bilheteNoiseRadius = 4f, bilheteNoise = 5f,
            bilheteMissChancePerMeter = 0f, bilheteLostDistance = 2f, bilheteAimAngle = 40f,
            foneCharges = 1, foneDelay = 8f, foneUseTime = 1.2f, foneRingChance = 0.5f, foneRingRadius = 5f, foneRingNoise = 30f,
            foneRingSeconds = 3f, foneRingSuspicion = 45f,
            garrafaCharges = 1, garrafaTime = 1.5f, garrafaSuspicion = 8f, garrafaNoiseRadius = 4f, garrafaNoise = 10f,
        };
        [Tooltip("Abaixo disso (segundos restantes) a entrega conta como 'no sufoco'")]
        public float sufocoSeconds = 30f;

        public Phase Phase { get; private set; } = Phase.Intro;
        public DifficultySettings Settings { get; private set; }
        public float TimeLeft { get; private set; }
        public int Flagrantes { get; private set; }
        /// <summary>
        /// Favores que a turma ainda faz por você nesta partida. Limite por partida (e não recarga) de propósito:
        /// o jogador entende "gastei meu favor", e cada distração pedida vira uma decisão de quando usar.
        /// </summary>
        public int Favors { get; private set; }
        /// <summary>Orçamento de respostas de cola da partida (Bloco 15), somando todas as colas.</summary>
        public CheatBudget Cheats { get; private set; } = new(0);
        public bool IsPlaying => Phase == Phase.Playing;
        /// <summary>Frame em que a fase mudou (a tecla que escolheu o menu não vale também como ação no jogo).</summary>
        public int PhaseChangedFrame { get; private set; } = -1;
        public float ElapsedFraction => Settings == null ? 0f : 1f - TimeLeft / Settings.duration;
        public event Action<Phase> PhaseChanged;

        bool warned60, warned30;

        void Awake()
        {
            I = this;
            Settings = normal;
            TimeLeft = normal.duration;
            Time.timeScale = 1f;
            // S9: a cenografia (luz de fim de tarde, cartazes, ventiladores...) é criada por código, só visual
            if (GetComponent<Cenografia>() == null) gameObject.AddComponent<Cenografia>();
        }

        void Start()
        {
            SetCursor(false);
            if (pendingPlayAgain is Difficulty again)
            {
                pendingPlayAgain = null;
                Begin(again);
                return;
            }
            HUD.I.ShowIntro(ChooseFromMenu, BeginTutorial, SkipTutorialFromMenu, !TutorialFlag.Done);
        }

        /// <summary>Está na prova de treino (tutorial)?</summary>
        public bool InTutorial { get; private set; }

        /// <summary>
        /// Já tem a nota mínima (prova de verdade, em andamento): o professor para de desconfiar de você andar,
        /// levantar, correr ou fazer barulho — vale em qualquer dificuldade. COLAR na frente dele continua dando flagrante.
        /// Se um flagrante zerar questão e a nota cair, volta ao normal.
        /// </summary>
        public bool JaPassou => Phase == Phase.Playing && !InTutorial && ExamSystem.I != null && Settings != null
                                && ExamSystem.I.Grade >= Settings.minGrade;
        bool avisouPassou;
        TutorialDirector tutorialDirector;

        /// <summary>
        /// Escolha no menu (clique ou tecla). Na primeira vez, qualquer dificuldade leva ao treino antes;
        /// quem não quiser aperta "Pular tutorial". Os testes chamam <see cref="Begin"/> direto, que é sempre a prova.
        /// </summary>
        public void ChooseFromMenu(Difficulty d)
        {
            if (Phase != Phase.Intro) return;
            if (!TutorialFlag.Done)
            {
                BeginTutorial();
                HUD.I.Toast(Falas.TutorialPrimeiraVez, HUD.Yellow, 4f);
                return;
            }
            Begin(d);
        }

        void SkipTutorialFromMenu()
        {
            TutorialFlag.MarkDone();
            HUD.I.Toast(Falas.TutorialPulado, HUD.Gray, 3f);
        }

        /// <summary>Começa a prova de treino: mesma cena e mesmas mecânicas, com a dificuldade "Treino".</summary>
        public void BeginTutorial()
        {
            if (Phase != Phase.Intro) return;
            InTutorial = true;
            StartMatch(tutorial);
            tutorialDirector = gameObject.AddComponent<TutorialDirector>();
        }

        /// <summary>Fim do treino (concluído ou pulado): marca como visto e mostra a tela final (R volta ao menu).</summary>
        public void EndTutorial(bool completed)
        {
            if (!InTutorial || Phase == Phase.Ended) return;
            TutorialFlag.MarkDone();
            SetPhase(Phase.Ended);
            HUD.I.SetTutorial(null);
            SoundFX.Play(completed ? Sfx.Ponto : Sfx.Clique);
            HUD.I.ShowResult(completed ? Falas.TutorialConcluidoTitulo : "TUTORIAL PULADO",
                             completed ? Falas.TutorialConcluido : Falas.TutorialPulado,
                             "Agora escolha a dificuldade e faça a prova de verdade.", HUD.Green, Restart, Quit);
        }

        public void Begin(Difficulty d)
        {
            if (Phase != Phase.Intro) return; // clique duplo / clique + tecla no mesmo frame
            CurrentDifficulty = d;
            StartMatch(d == Difficulty.Facil ? facil : d == Difficulty.Dificil ? dificil : normal);
            HUD.I.Toast("A PROVA COMEÇOU! Tire pelo menos " + Fmt(Settings.minGrade) + " e entregue na mesa do professor.", HUD.Yellow, 4f);
            HUD.I.Toast("Você só estudou " + Settings.knownQuestions + " questões (moldura amarela). A turma cobre " + Settings.maxCheatAnswers
                        + " colas. O resto: chute e reza.", HUD.Orange, 6f);
            ProfessorAI.I.Say(Falas.Pick(Falas.ProfInicio), 3.5f);
        }

        /// <summary>Espalha os números da dificuldade e começa a partida (prova ou treino).</summary>
        void StartMatch(DifficultySettings s)
        {
            Settings = s;
            TimeLeft = Settings.duration;
            Favors = Settings.favors;
            avisouPassou = false;
            Cheats = new CheatBudget(Settings.maxCheatAnswers);
            MatchLog.Reset();
            // espalha os números da dificuldade para quem usa
            ProfessorAI.I.ApplySettings(Settings);
            PlayerController.I.phoneSuspicion = Settings.phoneSuspicion;
            PlayerController.I.quizCooldown = Settings.quizCooldown;
            PlayerController.I.PhoneCharges = Settings.phoneCharges;
            PlayerController.I.Cheats.ApplySettings(Settings);
            foreach (var n in FindObjectsByType<NerdStudent>(FindObjectsInactive.Exclude))
            {
                n.copySuspicion = Settings.copySuspicion;
                n.glanceInterval = Settings.nerdGlanceInterval;
                n.maxCopies = Settings.nerdCopies;
                n.showHint = Settings.showStudentHints;
                n.hintDistance = Settings.studentHintDistance;
                n.glanceWarning = Settings.nerdGlanceWarning;
                n.glanceDuration = Settings.nerdGlanceDuration;
            }
            foreach (var sn in FindObjectsByType<SnitchStudent>(FindObjectsInactive.Exclude))
            {
                sn.showHint = Settings.showStudentHints;
                sn.hintDistance = Settings.studentHintDistance;
            }
            foreach (var b in FindObjectsByType<BackRowStudent>(FindObjectsInactive.Exclude))
            {
                b.dropChance = Settings.backRowDropChance;
                b.colaChance = Settings.backRowColaChance;
                b.colaCorretaChance = Settings.backRowGoodColaChance;
            }
            if (FacePokerChallenge.I != null)
            {
                FacePokerChallenge.I.baseSpeed = Settings.facePokerSpeed;
                FacePokerChallenge.I.zoneWidth = Settings.facePokerZone;
                FacePokerChallenge.I.missesAllowed = Settings.facePokerMisses;
            }
            QuizBank.Reset(); // gabarito do chute novo a cada partida (antes: no Awake do QuizChallenge)
            ExamSystem.I.SetKnown(Settings.knownQuestions);
            SetPhase(Phase.Playing);
            HUD.I.HideIntro();
            SoundFX.Play(Sfx.Sinal);
        }

        void Update()
        {
            // atalhos para os menus (caso o clique não chegue). S7: teclado ou controle, pelo Controls
            if (Phase == Phase.Intro)
            {
                if (Controls.Pressed(Acao.MenuFacil)) { ChooseFromMenu(Difficulty.Facil); return; }
                if (Controls.Pressed(Acao.MenuNormal)) { ChooseFromMenu(Difficulty.Normal); return; }
                if (Controls.Pressed(Acao.MenuDificil)) { ChooseFromMenu(Difficulty.Dificil); return; }
                if (Controls.Pressed(Acao.Tutorial)) { BeginTutorial(); return; }
            }
            // no treino, T pula (no mesmo frame em que o treino começou não vale)
            if (InTutorial && Phase == Phase.Playing && PhaseChangedFrame != Time.frameCount && Controls.Pressed(Acao.Tutorial))
            {
                EndTutorial(completed: false);
                return;
            }
            // fim: R joga de novo (mesma dificuldade), M volta ao menu; na pausa, R reinicia como antes.
            // No frame em que a prova acabou a tecla não vale (input duplicado).
            if (Phase == Phase.Ended && PhaseChangedFrame != Time.frameCount)
            {
                if (Controls.Pressed(Acao.JogarDeNovo)) { PlayAgain(); return; }
                if (Controls.Pressed(Acao.VoltarAoMenu)) { Restart(); return; }
            }
            if (Phase == Phase.Paused && Controls.Pressed(Acao.JogarDeNovo)) { Restart(); return; }

            if (Controls.Pressed(Acao.Pausa))
            {
                if (Phase == Phase.Playing) Pause();
                else if (Phase == Phase.Paused) Resume();
            }

            if (Phase != Phase.Playing) return;

            TimeLeft -= Time.deltaTime;
            // nota mínima garantida: o professor relaxa (uma vez a cada vez que a nota chega lá)
            bool passou = JaPassou;
            if (passou && !avisouPassou)
            {
                ProfessorAI.I?.Relax();
                HUD.I.Toast(Falas.NotaGarantida, HUD.Green, 4f, important: true);
            }
            avisouPassou = passou;
            if (!warned60 && TimeLeft <= 60f)
            {
                warned60 = true;
                HUD.I.Toast("FALTA 1 MINUTO!", HUD.Red, 3f, important: true);
                ProfessorAI.I.Say(Falas.Pick(Falas.ProfTempo), 3f);
                SoundFX.Play(Sfx.Alerta);
            }
            if (!warned30 && TimeLeft <= 30f)
            {
                warned30 = true;
                HUD.I.Toast("30 SEGUNDOS! É agora ou DP!", HUD.Red, 3f, important: true);
                SoundFX.Play(Sfx.Alerta);
            }
            if (TimeLeft <= 0f && InTutorial)
            {
                EndTutorial(completed: false);
                return;
            }
            if (TimeLeft <= 0f)
            {
                TimeLeft = 0f;
                SoundFX.Play(Sfx.Sinal);
                End(ExamSystem.I.Grade >= Settings.minGrade ? LevelResult.AprovadoNoSufoco : LevelResult.Reprovado, true);
            }
        }

        // ------------------------------------------------------------- regras

        /// <summary>O professor pegou o jogador: tira ponto, conta flagrante e pode encerrar a fase.</summary>
        /// <param name="reason">Texto para o jogador ler (só exibição).</param>
        /// <param name="kind">O que ele estava fazendo: é isto que decide as consequências (ex.: confisco).</param>
        public void ApplyFlagrante(string reason, Infracao kind = Infracao.Outra)
        {
            if (Phase != Phase.Playing) return;
            Flagrantes++;
            SoundFX.Play(Sfx.Flagrante);
            HUD.I.Flash(new Color(1f, 0.1f, 0.1f, 0.45f));

            MatchLog.Flagrante(kind, Settings.duration - TimeLeft);
            bool warningOnly = Settings.firstFlagranteIsWarning && Flagrantes == 1;
            string penalty;
            if (warningOnly) penalty = "só uma ADVERTÊNCIA desta vez";
            else penalty = ExamSystem.I.Annul() ? "-" + Fmt(ExamSystem.I.PointsPerQuestion) + " ponto" : "uma questão zerada";
            HUD.I.Toast("FLAGRANTE! (" + reason + ") " + penalty + "  [" + Flagrantes + "/" + Settings.maxFlagrantes + "]", HUD.Red, 4f, important: true);
            FlagranteStamp.Show(warningOnly ? Falas.CarimboAdvertencia : Falas.CarimboFlagrante); // S4: o carimbo bate na tela

            var p = PlayerController.I;
            // o que ele leva embora depende só do TIPO da infração (Bloco 15: celular tocando também é celular)
            var taken = CheatRules.ItemTakenFor(kind);
            if (Settings.confiscatePhone && !warningOnly && taken == CheatItem.Celular && p.HasPhone)
            {
                p.HasPhone = false;
                HUD.I.Toast("Celular CONFISCADO até o fim da prova!", HUD.Red, 3.5f, important: true);
            }
            if (!warningOnly && p.Cheats != null) p.Cheats.OnFlagrante(kind, taken);
            p.Say(Falas.Pick(Falas.JogadorPego), 2.5f);

            if (Flagrantes >= Settings.maxFlagrantes)
                StartCoroutine(EndAfter(2.5f, LevelResult.Flagrado));
        }

        /// <summary>Gasta um favor da turma (quem chama só gasta se a distração funcionou). false = acabaram.</summary>
        public bool SpendFavor()
        {
            if (Favors <= 0) return false;
            Favors--;
            HUD.I.Toast(Favors > 0 ? "Favor gasto. A turma ainda te deve " + Favors + "." : "Esse foi o ÚLTIMO favor. A turma cansou de você.",
                        Favors > 0 ? HUD.Gray : HUD.Orange, 2.5f);
            return true;
        }

        public void DeliverExam()
        {
            if (Phase != Phase.Playing) return;
            if (InTutorial) { tutorialDirector?.OnDeliver(); return; } // no treino, entregar é um dos passos
            float g = ExamSystem.I.Grade;
            if (g >= Settings.minGrade) End(TimeLeft <= sufocoSeconds ? LevelResult.AprovadoNoSufoco : LevelResult.Aprovado, false);
            else End(LevelResult.Reprovado, false);
        }

        IEnumerator EndAfter(float seconds, LevelResult r)
        {
            yield return new WaitForSeconds(seconds);
            End(r, false);
        }

        void End(LevelResult r, bool timeUp)
        {
            if (Phase == Phase.Ended) return;
            SetPhase(Phase.Ended);
            float g = ExamSystem.I.Grade;
            string msg;
            Color color;
            switch (r)
            {
                case LevelResult.Aprovado:
                    msg = Falas.Pick(Falas.FinalAprovado); color = HUD.Green; break;
                case LevelResult.AprovadoNoSufoco:
                    msg = (timeUp ? "O sinal tocou e você entregou a prova no grito. " : "") + Falas.Pick(Falas.FinalSufoco);
                    color = HUD.Yellow; break;
                case LevelResult.Flagrado:
                    msg = Falas.Pick(Falas.FinalFlagrado); color = HUD.Red; break;
                default:
                    msg = (timeUp ? "O tempo acabou. " : "Você entregou a prova com nota insuficiente. ") + Falas.Pick(Falas.FinalReprovado);
                    color = HUD.Red; break;
            }
            if (r == LevelResult.Reprovado || r == LevelResult.Flagrado) SoundFX.Play(Sfx.Erro);
            else SoundFX.Play(Sfx.Ponto);
            MatchLog.Write(Settings, r, g, Flagrantes, TimeLeft, Favors);
            var exam = ExamSystem.I;
            LastSummary = MatchSummary.Build(Settings, r, g, TimeLeft, Flagrantes, Favors,
                                             exam.Count(QuestionState.Certa), exam.Count(QuestionState.Errada), exam.Count(QuestionState.EmBranco));
            // S6: título de estilo, dica, recorde e conquistas (grava o recorde e as conquistas novas)
            var extras = Records.Process(LastSummary, Settings.maxCheatAnswers, CurrentDifficulty == Difficulty.Dificil);
            HUD.I.ShowMatchResult(LastSummary, msg, color, PlayAgain, Restart, Quit, extras);
        }

        /// <summary>Resumo mostrado na última tela de fim de prova (testes).</summary>
        public MatchSummary LastSummary { get; private set; }

        /// <summary>Dificuldade da prova em andamento (para "jogar de novo").</summary>
        public Difficulty CurrentDifficulty { get; private set; } = Difficulty.Normal;

        /// <summary>
        /// "Jogar de novo" recarrega a cena e já começa na mesma dificuldade, sem passar pelo menu.
        /// Estático porque sobrevive ao recarregamento da cena; zerado a cada Play (o domínio não recarrega).
        /// </summary>
        static Difficulty? pendingPlayAgain;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlay() => pendingPlayAgain = null;

        /// <summary>Recomeça a prova na mesma dificuldade (no treino, volta ao menu).</summary>
        public void PlayAgain()
        {
            if (InTutorial) { Restart(); return; }
            pendingPlayAgain = CurrentDifficulty;
            Restart();
        }

        // ------------------------------------------------------------- pausa / reinício

#if !UNITY_EDITOR
        // no jogo compilado, sair da janela pausa (em vez de congelar no meio de uma ação)
        void OnApplicationFocus(bool focus)
        {
            if (!focus && Phase == Phase.Playing) Pause();
        }
#endif

        public void Pause()
        {
            if (Phase != Phase.Playing) return;
            SetPhase(Phase.Paused);
            Time.timeScale = 0f;
            HUD.I.ShowPause(Resume, Restart, Quit);
        }

        public void Resume()
        {
            if (Phase != Phase.Paused) return;
            HUD.I.HidePause();
            Time.timeScale = 1f;
            SetPhase(Phase.Playing);
        }

        public void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void SetPhase(Phase p)
        {
            Phase = p;
            PhaseChangedFrame = Time.frameCount;
            SetCursor(p == Phase.Playing);
            PhaseChanged?.Invoke(p);
        }

        static void SetCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        public static string Fmt(float v) => v.ToString("0.0").Replace('.', ',');
    }
}
