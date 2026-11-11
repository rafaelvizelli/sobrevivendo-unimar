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

        // ---------------------------------------------------------- colas novas (Bloco 15)
        public static readonly string[] ProfFlagraColinha =
        {
            "Abre a mão. ABRE A MÃO. ...Isso é a fórmula de Bhaskara tatuada?!",
            "Manga comprida em pleno calor de 35 graus. Eu SABIA.",
            "Que mão fechada é essa? Tá segurando o quê, a resposta da 4?",
        };
        public static readonly string[] ProfFlagraBilhete =
        {
            "Correio aéreo? Na MINHA prova? Isso aqui vai pro museu da vergonha.",
            "Bilhetinho? Que fofo. Que FOFO. Zero pra você.",
            "Voou um papel e pousou um flagrante.",
        };
        public static readonly string[] ProfFlagraCelularTocando =
        {
            "De quem é esse celular VIBRANDO?! ...Ah, é seu. Que surpresa.",
            "Atende. Atende, vai. Diz pra pessoa que você foi pego.",
            "Esse toque aí é a sua DP chegando.",
        };
        public static readonly string[] ProfFlagraGarrafinha =
        {
            "Água com rótulo de 40 linhas? Que marca é essa, 'Gabarito Mineral'?",
            "Bebe devagar... e me dá essa garrafa. Que ingrediente é 'resposta da 7'?",
            "Hidratação demais pra quem não estudou nada.",
        };

        /// <summary>Fala do professor no flagrante, pelo TIPO da infração (as colas novas têm a sua).</summary>
        public static string[] FlagranteDe(Infracao kind) => kind switch
        {
            Infracao.ColinhaNaManga => ProfFlagraColinha,
            Infracao.Bilhete => ProfFlagraBilhete,
            Infracao.CelularTocando => ProfFlagraCelularTocando,
            Infracao.Garrafinha => ProfFlagraGarrafinha,
            _ => ProfFlagrante,
        };

        // motivo do flagrante (aparece no "FLAGRANTE! (...)") e o que o professor "ouve"
        public const string MotivoColinha = "colinha na manga";
        public const string MotivoBilhete = "bilhete voando";
        public const string MotivoFone = "mexendo no celular";
        public const string MotivoCelularTocando = "celular tocando na prova";
        public const string MotivoGarrafinha = "lendo o rótulo da garrafinha";
        public const string RuidoPapel = "um papel voando";
        public const string RuidoVibra = "um celular vibrando";
        public const string RuidoGole = "um gole MUITO barulhento";

        // barra de "segurando"
        public const string SegurandoColinha = "Lendo a colinha na manga... (solte se ele chegar perto!)";
        public const string SegurandoGarrafinha = "Bebendo água... e lendo o rótulo";
        public const string SegurandoFoneEnviar = "Mandando 'me salva, questão 5' pro primo...";
        public const string SegurandoFoneLer = "Lendo a resposta do primo...";

        // resultados
        public const string ColinhaFuncionou = "a colinha salvou!";
        public const string GarrafaFuncionou = "o rótulo da garrafinha sabia!";
        public const string FoneFuncionou = "o primo respondeu certinho!";
        public const string FoneEnviado = "Mensagem enviada. Agora é esperar... e torcer pro celular não vibrar.";
        public const string FoneChegou = "Chegou a resposta do primo! Segure F pra ler.";
        public const string FoneAindaNao = "O primo ainda está digitando...";
        public const string FonePerdido = "O celular foi confiscado. A resposta do primo agora é do professor.";
        public const string BilheteSemMira = "Olhe pro Nerd ou pro Fundão antes de jogar o bilhete (B).";
        public const string BilheteEsperando = "Já tem um bilhete no ar. Espere a resposta voltar.";
        public const string BilheteInterceptado = "O professor estava perto. O bilhete sumiu no bolso dele.";
        public const string BilheteErrouAMesa = "O bilhete caiu no chão do corredor. Ninguém vai buscar.";
        public const string BilheteNerdRecusou = "O Nerd estava de olho e devolveu o bilhete amassado.";
        public const string BilheteSumiu = "O bilhete se perdeu no caminho.";
        public const string BilheteRuim = "Voltou o bilhete: 'não sei, bjs'. Pelo menos foi sincero.";
        public const string BilheteSemLugar = "O bilhete voltou, mas você já respondeu tudo.";
        public const string ColinhaConfiscada = "A colinha foi confiscada. Sua manga agora está vazia (e suada).";
        public const string ColinhaAcabou = "A colinha acabou. A tinta da caneta borrou o resto.";
        public const string GarrafaConfiscada = "A garrafinha foi confiscada. Sede E reprovação.";
        public const string GarrafaAcabou = "A garrafinha está vazia. O rótulo também já foi lido.";
        public const string FoneSemCelular = "Sem celular, sem primo. O fone sozinho só toca silêncio.";
        public const string FoneAcabou = "O primo parou de responder. Bloqueou você, provavelmente.";
        public const string BilheteAcabou = "Acabou o papel dos bilhetes. Só sobrou a prova (não rasgue).";
        public const string SoEmBranco = "Essa cola só serve pra questão em branco, e não sobrou nenhuma.";
        public const string NadaACorrigir = "Você já acertou tudo. O primo pode descansar.";
        public const string OrcamentoAcabou = "Você já colou tudo que dava nesta prova. O universo cansou de te ajudar.";
        public const string ColinhaConfiscadaAgora = "Colinha CONFISCADA até o fim da prova!";
        public const string GarrafaConfiscadaAgora = "Garrafinha CONFISCADA! O professor vai ler o rótulo com calma.";

        public static readonly string[] JogadorSilenciaCelular = { "*aperta todos os botões ao mesmo tempo*", "Não é meu! ...É meu.", "Shhh, celular, shhh!" };
        public static readonly string[] NerdBilheteAceito = { "*suspira* Tá. Mas é a última vez. (não é)", "Isso é contra os meus princípios. Toma." };
        public static readonly string[] NerdBilheteRecusado = { "Bilhete? Eu tô VENDO você, hein!", "Devolvido. Com correção em vermelho." };
        public static readonly string[] FundaoBilhete = { "Correio do fundão, ao seu dispor.", "Deixa comigo. Ou com o meu primo." };

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

        // ---------------------------------------------------------- interface "prova impressa" (Bloco 16)
        public const string FolhaCabecalho = "UNIMAR — AVALIAÇÃO BIMESTRAL     Aluno: ____________________     Nota: _____";
        public const string JogoTitulo = "SOBREVIVENDO À UNIMAR";
        public const string JogoSubtitulo = "FASE 1 — A PROVA IMPOSSÍVEL";
        public const string MenuHistoria = "Você não estudou. A prova é do Prof. Takanervoso, o homem que \"não nasceu ontem\".\n"
                                         + "Tire a nota mínima antes do sinal: respondendo o que sabe, colando do Nerd, usando o\n"
                                         + "celular ou aproveitando o caos da sala. Três flagrantes e você está fora.";
        public const string MenuRecadoProfessor = "Não estudou, né? Tô de olho.\n— Prof. T.";
        public const string MenuCarimbo = "NÃO ESTUDOU";
        public const string BotaoJogar = "JOGAR";
        public const string BotaoTutorialPrimeira = "TUTORIAL (1 min)  [T]";
        public const string BotaoTutorialRever = "Rever tutorial  [T]";
        public const string BotaoPularTutorial = "Pular tutorial";
        public const string MenuDicaPrimeiraVez = "Primeira vez? Qualquer dificuldade começa pelo treino de 1 minuto.";
        public const string MenuDica = "Dica: observe a sala. Professor, Nerd e Dorminhoco avisam antes, no corpo, não na tela.";
        public const string MenuAtalhos = "(ou aperte 1, 2 ou 3 pra ir direto à prova)";

        public const string DificuldadeTitulo = "QUESTÃO 1 (vale a sua vida acadêmica)";
        public const string DificuldadeEnunciado = "Assinale a dificuldade. Clique, ou aperte 1, 2 ou 3.";
        public const string DificuldadeFacilFrase = "Professor míope. Esqueceu os óculos e a vontade de dar aula.";
        public const string DificuldadeNormalFrase = "O clássico. Ele não nasceu ontem, mas dorme cedo.";
        public const string DificuldadeDificilFrase = "Professor com radar. Ele ouve você PENSAR em colar.";
        /// <summary>"{0} min • nota mínima {1}": tempo e nota vêm das DifficultySettings.</summary>
        public const string DificuldadeDados = "{0} min • nota mínima {1}";
        public const string InstrucoesTitulo = "INSTRUÇÕES";
        public const string InstrucoesControles =
            "WASD andar • Mouse câmera • Shift correr • Esc pausar\n"
          + "E interagir (alguns: segure) • Sentado: Espaço levanta\n"
          + "1 responder o que sabe / chutar • 2 celular (segure)\n"
          + "Colas: Q colinha • G garrafinha • B bilhete (mire) • F fone\n"
          + "Cara de Paisagem: ESPAÇO com o ponteiro no verde";
        public const string BotaoVoltar = "VOLTAR  [Esc]";

        public const string PausaTitulo = "PAUSADO";
        public const string PausaFrase = "O professor também congelou. Aproveite pra respirar.";
        public const string BotaoContinuar = "CONTINUAR  [Esc]";
        public const string BotaoReiniciar = "REINICIAR FASE  [R]";
        public const string BotaoTentarDeNovo = "TENTAR DE NOVO  [R]";

        public const string HudTempo = "TEMPO";
        public const string HudNota = "NOTA";
        public const string HudNotaJaDa = "já dá! Entregue na mesa (E)";
        /// <summary>"faltam {0} pra passar" (anotação à mão no painel da NOTA).</summary>
        public const string HudNotaFaltam = "faltam {0} pra passar";
        public const string HudTreino = "TREINO";
        public const string ResumoColasDeBolso = "Colas de bolso (Q/G/B/F)";
    }
}
