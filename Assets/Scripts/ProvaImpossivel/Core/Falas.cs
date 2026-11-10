using System.Collections.Generic;
using UnityEngine;

namespace ProvaImpossivel
{
    /// <summary>Todas as falas do jogo num lugar só. <see cref="Pick"/> evita repetir a última frase de cada lista.</summary>
    public static class Falas
    {
        static readonly Dictionary<string[], int> last = new();

        /// <summary>Esquece a última frase de cada lista. O domínio não recarrega ao dar Play: roda a cada Play (padrão do Noise).</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset() => last.Clear();

        /// <summary>Quantas listas têm uma "última frase" lembrada (testes).</summary>
        public static int RememberedLists => last.Count;

        public static string Pick(string[] list)
        {
            if (list == null || list.Length == 0) return "";
            if (list.Length == 1) return list[0];
            last.TryGetValue(list, out int prev);
            int i;
            do i = Random.Range(0, list.Length); while (i == prev - 1);
            last[list] = i + 1;
            return list[i];
        }

        // ---------------------------------------------------------- professor
        public static readonly string[] ProfInicio =
        {
            "A prova começou. Boa sorte. Vocês vão precisar.",
            "Celular guardado, alma preparada. Podem começar.",
        };
        public static readonly string[] ProfTranquilo =
        {
            "Pode continuar, estou observando.",
            "Essa sala está silenciosa demais...",
            "Ninguém olha pro lado. O lado também não sabe.",
            "Lembrando: cola dá zero. E desgosto.",
            "Prova fácil, hein? Hehehe.",
            "Eu corrijo prova há 30 anos. Eu SINTO a cola.",
            "Quem terminar pode entregar. Quem não terminar... pode chorar.",
            "Tic-tac. Tic-tac.",
            "Na minha época a gente fazia prova de pé. Na neve.",
        };
        public static readonly string[] ProfEncarando = { "...", "......", "Hmmmmm.", "*encara em silêncio*" };
        public static readonly string[] ProfDesconfiado =
        {
            "Interessante... muito interessante.",
            "Você está olhando para onde?",
            "Hmm. Alguém aí está respirando estranho.",
            "Tem alguma coisa errada nessa fileira...",
            "Que barulhinho foi esse?",
            "Eu vi isso. Eu acho. Eu vi.",
        };
        public static readonly string[] ProfInvestigando =
        {
            "Deixa eu dar uma olhadinha por aqui...",
            "Não se mexam. Só estou passeando. Casualmente.",
            "Vamos ver, vamos ver...",
            "Esse cheiro... é cheiro de cola.",
            "Ninguém se mexe que eu tô chegando.",
        };
        public static readonly string[] ProfFlagrante =
        {
            "EU NÃO NASCI ONTEM!",
            "AHÁ! PEGUEI NO FLAGRA!",
            "Achou que eu não ia ver?! Eu tenho olhos nas costas!",
            "Que feio! Que FEIO!",
            "Isso vai pra ata! Com caneta VERMELHA!",
        };
        public static readonly string[] ProfVoltaLugar = { "Volta pro seu lugar. AGORA.", "Senta. E reza.", "Pro seu lugar, e sem gracinha." };
        public static readonly string[] ProfCaraPaisagemOk = { "Hm. Pode continuar.", "Tá bom... por enquanto.", "Engraçado. Jurava que...", "Ok. Mas eu tô de olho, hein." };
        public static readonly string[] ProfCaraPaisagemFalha =
        {
            "Por que você está suando tanto?",
            "Que cara é essa? Cara de quem colou!",
            "Respira, filho. Respira e me explica.",
            "Esse tremelique aí é o quê?",
        };
        public static readonly string[] ProfAlarmeFalso = { "Hmm... jurava que tinha visto algo.", "Deve ter sido o vento.", "Falso alarme. Dessa vez." };
        public static readonly string[] ProfLigacao =
        {
            "Alô? ... Mãe?! Agora não, tô aplicando prova!",
            "Alô? Não, eu NÃO quero mudar de operadora!",
            "Alô? Sim, o carro é meu. Como assim GUINCHADO?!",
            "Alô? ... Pizza? Não, eu não pedi pizza. ...De calabresa?",
        };
        public static readonly string[] ProfRonco = { "ACORDA! Aqui não é hotel!", "Quem tá roncando?! ...Ah, claro. Ele de novo.", "Isso é ronco ou motor de caminhão?!" };
        public static readonly string[] ProfProjetor = { "Quem mexeu no HDMI?!", "Ninguém sai! Isso foi SABOTAGEM!", "Funcionava ontem... juro que funcionava." };
        public static readonly string[] ProfBarulho = { "Que barulho foi esse aí?!", "Algum problema aí no fundão?!", "Juntem isso aí. Em SILÊNCIO." };
        public static readonly string[] ProfPuxaSaco = { "Sim, sim, vale. Agora volta pra prova.", "Não, não pode apagar o quadro.", "...Obrigado. A gravata foi presente." };
        public static readonly string[] ProfNerd = { "Silêncio, gênio.", "Parabéns. Agora fica quieto.", "Tem questão bônus não. Senta." };
        public static readonly string[] ProfTempo = { "Faltando UM minuto! Um!", "O tempo voa quando a gente não estuda, né?" };
        public static readonly string[] ProfDedurado = { "COMO É QUE É?!", "Ah, é? Deixa eu ver isso de perto." };

        // ---------------------------------------------------------- alunos
        public static readonly string[] NerdPercebeu =
        {
            "VAI ESTUDAR, PARASITA!",
            "Ei! Isso aqui tem direitos autorais!",
            "PROFESSOOOR! Tem gente lendo minha prova com os olhos!",
        };
        public static readonly string[] NerdGaba =
        {
            "Terminei. Posso fazer as questões bônus?",
            "Tão fácil que eu fiz duas vezes.",
            "Quem erra essa? Sério. Quem?",
            "Professor, a questão 7 tem um erro de digitação. Grave.",
        };
        public static readonly string[] NerdGrito = { "ISSO É FÁCIL DEMAIS! É SÉRIO QUE ISSO VALE NOTA?!", "AAAH! ESQUECI DE DEMONSTRAR O TEOREMA! ...brincadeira, demonstrei sim." };
        public static readonly string[] DorminhocoAcorda =
        {
            "A PROVA JÁ COMEÇOU?!",
            "Mãe, mais cinco minutos...",
            "QUEM? ONDE? QUE DIA É HOJE?",
            "Eu tava só... descansando os olhos.",
        };
        public static readonly string[] DorminhocoRonco = { "ZzzZZRRRRONC...", "RRRRONC... mimimimi...", "ZZZzzz... resposta C... zzz" };
        public static readonly string[] FundaoIdle = { "Psiu! Quer uma ideia? Tenho várias. Todas péssimas.", "Bora causar?", "Relaxa, eu tenho um plano." };
        public static readonly string[] FundaoDerruba = { "Segura essa! *derruba o estojo inteiro*", "Ops. Foi mal. Foi o vento.", "*CRASH* ...Ninguém viu nada." };
        public static readonly string[] FundaoColaBoa = { "Toma a cola. Confia no pai.", "Escrevi na mão, no braço e na alma. Toma." };
        public static readonly string[] FundaoColaRuim = { "Toma a cola. ...Opa, essa era a lista do mercado.", "Toma! ...Ih, essa é a cola de Química. Do ano passado." };
        public static readonly string[] FundaoShow = { "*começa a imitar o professor* 'EU NÃO NASCI ONTEEEM!'", "*faz beatbox no meio da prova*", "*levanta e faz a ola sozinho*" };
        public static readonly string[] PuxaSacoDedura =
        {
            "PROFESSOR! O de moletom laranja tá colando!",
            "Professor, com todo respeito... ELE TÁ COLANDO!",
            "Anotei tudo, professor. Hora, local e cara de culpado.",
        };
        public static readonly string[] PuxaSacoPergunta =
        {
            "Professor! Professor! Essa questão vale ponto extra?",
            "Professor, posso apagar o quadro pro senhor?",
            "Professor, que gravata linda! Onde comprou?",
        };
        public static readonly string[] FiguranteMochila = { "Ai! Minha mochila!", "Foi mal, foi mal!", "Meu estojo! Meu lanche!" };

        // ---------------------------------------------------------- jogador
        public static readonly string[] JogadorPensa = { "Pensa, pensa, pensa...", "Eu sei isso. Eu vi num vídeo. Em 2x.", "Ok, cérebro. Agora é com você." };
        public static readonly string[] JogadorPego = { "Professor, não é o que parece!", "Eu tava só... alongando o pescoço!", "Foi o vento, professor!" };

        // ---------------------------------------------------------- finais
        public static readonly string[] FinalAprovado =
        {
            "Você passou! Seus pais nunca saberão como.",
            "Aprovado! O diploma ainda está longe, mas hoje é festa.",
            "Passou! O professor ainda está tentando entender como.",
        };
        public static readonly string[] FinalSufoco =
        {
            "Faltando segundos! Seu coração ainda não voltou pro lugar.",
            "Passou raspando. Raspando tanto que saiu faísca.",
        };
        public static readonly string[] FinalReprovado =
        {
            "Nos vemos na DP. Traga lanche.",
            "A nota foi tão baixa que o professor pediu desculpas por ela.",
            "Reprovado. Mas com estilo. Pouco estilo.",
        };
        public static readonly string[] FinalFlagrado =
        {
            "Três flagrantes. Sua prova agora está emoldurada na sala dos professores.",
            "Expulso da sala. Sua cola agora é peça de museu.",
        };

        // ---------------------------------------------------------- tela de fim de partida (resumo)
        public const string TituloAprovado = "APROVADO!";
        public const string TituloSufoco = "APROVADO NO SUFOCO!";
        public const string TituloReprovado = "REPROVADO";
        public const string TituloExpulso = "EXPULSO!";
        public const string ResumoProva = "A PROVA";
        public const string ResumoCrimes = "A FICHA CRIMINAL";
        public const string ResumoNota = "Nota final";
        public const string ResumoMinima = "Nota mínima";
        public const string ResumoQuestoes = "Certas / erradas / em branco";
        public const string ResumoTempo = "Tempo usado";
        public const string ResumoFlagrantes = "Flagrantes";
        public const string ResumoCelular = "Colas do celular";
        public const string ResumoNerd = "Colas do Nerd";
        public const string ResumoFundao = "Colas do Fundão";
        public const string ResumoFundaoFurada = "furada";
        public const string ResumoFavores = "Favores gastos";
        public const string ResumoRodape = "Dificuldade: ";
        public const string BotaoJogarDeNovo = "JOGAR DE NOVO (R)";
        public const string BotaoMenu = "MENU (M)";
        public const string BotaoSair = "SAIR";

        // ---------------------------------------------------------- opções (pausa)
        public const string OpcaoVolumeGeral = "Volume geral";
        public const string OpcaoVolumeEfeitos = "Volume dos efeitos";
        public const string OpcaoSensibilidade = "Sensibilidade da câmera";

        // ---------------------------------------------------------- tutorial (prova de treino)
        public const string TutorialAndar = "Levante (Espaço) e dê uma voltinha (WASD). Hoje ninguém te prende.";
        public const string TutorialSentar = "Volte pra sua carteira e aperte E. Cadeira arrastando faz barulho, viu?";
        public const string TutorialResponder = "Aperte 1 e responda a questão que você estudou (moldura amarela). Sim, UMA.";
        public const string TutorialCelular = "Segure 2 pra pesquisar no celular. De preferência com o professor olhando pro outro lado.";
        public const string TutorialNerd = "Levante, fique do lado do Nerd e segure E pra espiar. Se ele PARAR de escrever, solta!";
        public const string TutorialDistracao = "Gaste um favor: cutuque o Dorminhoco ou peça pro Puxa-saco tirar uma 'dúvida' (E).";
        public const string TutorialEntregar = "Agora leve a prova até a mesa do professor e segure E. Simples. Na teoria.";
        public const string TutorialCedoDemais = "Calma! O treino ainda não acabou. Termine o passo de agora antes de entregar.";
        public const string TutorialPrimeiraVez = "Primeiro o treino. Depois, a prova de verdade (e a DP, se der ruim).";
        public const string TutorialConcluidoTitulo = "TREINO CONCLUÍDO!";
        public const string TutorialConcluido = "Você andou, respondeu, pesquisou, espiou, distraiu e entregou. Na prova de verdade, o Takanervoso não finge que não vê.";
        public const string TutorialPulado = "Tutorial pulado. Coragem ou excesso de confiança? A prova vai dizer.";
        public static readonly string[] TutorialProfInicio =
        {
            "Prova de TREINO. Hoje eu finjo que não vejo. Só hoje.",
            "Isso aqui é treino. Errar pode. Na de verdade... aí é DP.",
        };
        public static readonly string[] TutorialProfQuaseFlagra =
        {
            "Na prova de verdade, isso era FLAGRANTE. Hoje passa.",
            "Eu vi, hein. Sorte sua que é treino.",
            "Anotado. Mentalmente. Pra semana que vem.",
        };
    }
}
