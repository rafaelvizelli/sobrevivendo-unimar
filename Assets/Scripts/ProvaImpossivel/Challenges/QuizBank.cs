using System.Collections.Generic;
using UnityEngine;

namespace ProvaImpossivel
{
    /// <summary>
    /// Perguntas da prova do Prof. Takanervoso (Desafio B), em 3 níveis, mais as perguntas de CHUTE.
    /// A primeira opção de cada uma é a correta (nos chutes, a correta é sorteada a cada partida); a ordem é embaralhada no jogo.
    /// As erradas são engraçadas, mas sempre existe uma resposta coerente.
    /// </summary>
    public static class QuizBank
    {
        public class Question
        {
            public string text;
            public string[] options; // [0] = correta
            public int level;        // 0 fácil, 1 médio, 2 difícil
            public Question(int lvl, string t, params string[] o) { level = lvl; text = t; options = o; }
        }

        /// <summary>
        /// Questões que o jogador NÃO estudou: perguntas absurdas do Takanervoso, sem como saber a resposta.
        /// As 4 opções são igualmente plausíveis de propósito, então chutar acerta ~25%.
        /// Aqui NÃO existe gabarito fixo: a opção certa de cada uma é sorteada a cada partida (<see cref="Reset()"/>),
        /// senão bastaria decorar as respostas (o jogo mostra a certa quando você erra).
        /// </summary>
        static readonly Question[] chutes =
        {
            new(3, "Segundo o Takanervoso, qual a unidade oficial do desespero na semana de provas?",
                "Cafés por hora", "Abas abertas no navegador", "Áudios de 2 minutos no grupo", "Boletos por semestre"),
            new(3, "Quantas DPs um aluno precisa ter para desbloquear o 'modo sobrevivência'?",
                "Três no mesmo semestre", "Duas, se uma for de Cálculo", "Uma, mas com o mesmo professor duas vezes", "Nenhuma: basta o boleto atrasar"),
            new(3, "No trabalho em grupo de 5 pessoas, quantas fazem o trabalho de verdade?",
                "Uma e meia", "Duas, revezando", "Uma, que também apresenta", "Todas, segundo o relatório"),
            new(3, "Qual o horário oficial em que a impressora da faculdade quebra?",
                "15 minutos antes da entrega", "Exatamente quando é a sua vez", "Toda sexta-feira às 18h", "Assim que você paga a cópia"),
            new(3, "O professor passou trabalho na sexta-feira às 22h. Para quando é a entrega?",
                "Segunda às 7h30", "Domingo às 23h59", "Sábado ao meio-dia", "'Ontem', segundo o e-mail"),
            new(3, "Qual a primeira pergunta feita depois que o professor explica que a atividade vale nota?",
                "\"Professor, isso vale nota?\"", "\"Pode ser em dupla?\"", "\"Pode entregar à mão?\"", "\"Cai na prova?\""),
            new(3, "Qual a porcentagem mínima de presença para o aluno que só aparece em dia de prova?",
                "75%, como todo mundo", "50% mais um atestado criativo", "A que o professor esquecer de anotar", "Depende do número de chamadas por aula"),
            new(3, "Qual a combinação oficial de marmita da semana de provas?",
                "Arroz, feijão e ansiedade", "Miojo com ovo e esperança", "Pão de queijo e café requentado", "O que sobrou do RU de ontem"),
            new(3, "Na prova surpresa, qual a reação mais comum da turma?",
                "Fingir que sabia que ia ter", "Perguntar se pode consultar o caderno", "Pedir para fazer em dupla", "Abrir o grupo da sala no celular"),
            new(3, "Segundo a lenda, quantos cafés equivalem a uma noite de sono?",
                "Sete", "Quatro, se for expresso", "Doze, com açúcar", "Nenhum, mas a gente tenta"),
            new(3, "Qual a resposta-padrão do aluno que nunca aparece quando cobrado no trabalho em grupo?",
                "\"Eu fiz a capa\"", "\"Meu Wi-Fi caiu\"", "\"Ninguém me adicionou no grupo\"", "\"Achei que era pra semana que vem\""),
            new(3, "O que vale mais no fim do semestre?",
                "Um ponto extra na média", "Uma presença perdida", "O resumo da colega", "Um atestado com carimbo"),
            new(3, "Quanto tempo dura o 'intervalo de 10 minutos' do professor?",
                "Vinte e cinco minutos", "Exatamente dez, cronometrados", "Até acabar o café da cantina", "Até alguém perguntar se já pode voltar"),
            new(3, "Qual a forma mais eficaz de começar o e-mail pedindo prorrogação do trabalho?",
                "\"Prezado mestre\"", "\"Querido professor, tudo bem?\"", "\"Bom dia, desculpa o incômodo\"", "\"Professor, surgiu um imprevisto\""),
            new(3, "A que horas o aluno do Fundão chega na aula das 7h30?",
                "Às 7h40, com café na mão", "Exatamente na hora da chamada", "Só depois do intervalo", "Quando a prova já começou"),
            new(3, "Qual o melhor lugar para sentar no dia da prova?",
                "Do lado do Nerd", "Atrás do aluno mais alto", "Perto da porta", "Longe do ar-condicionado"),
            new(3, "Qual o prato mais servido no RU durante a semana de provas?",
                "Estrogonofe de ontem", "Frango com mais frango", "Arroz, feijão e farofa", "A lasanha misteriosa de quarta"),
            new(3, "Quantas vezes o aluno atualiza o portal até a nota aparecer?",
                "Quarenta e duas", "Uma por minuto, a madrugada inteira", "Até o portal cair", "Dezessete, e desiste"),
            new(3, "Qual a desculpa mais aceita para faltar na apresentação do seminário?",
                "Atestado de dentista", "Pneu furado na estrada", "O ônibus que não passou", "A avó doente (a terceira do semestre)"),
            new(3, "Para que serve a última página do caderno?",
                "Jogo da velha", "Contas do boleto", "Caricatura do professor", "Lista do que estudar 'amanhã'"),
            new(3, "Quantas páginas tem, na prática, um trabalho \"de no mínimo 10 páginas\"?",
                "Dez, com fonte 14", "Nove e meia, contando a capa", "Oito e muita margem", "Doze, contando as referências"),
            new(3, "Em que momento o aluno realmente começa a estudar para a prova?",
                "Na fila do xerox", "Na noite anterior, às 23h", "No ônibus, a caminho da faculdade", "Cinco minutos antes, no corredor"),
            new(3, "O que acontece com quem pergunta \"vai ter revisão?\"",
                "Ganha uma lista com 80 exercícios", "Ouve que a revisão é estudar", "Descobre que a revisão foi semana passada", "Ganha a antipatia da sala inteira"),
            new(3, "Qual nota o aluno considera \"passei bem\"?",
                "Seis cravado", "Qualquer coisa acima da média", "Cinco e meio arredondado", "A que livra do exame"),
            new(3, "Quantos grupos de WhatsApp tem uma turma do primeiro semestre?",
                "Sete, um deles sem o professor", "Três, e um está morto", "Doze, contando os de trabalho", "Um oficial e cinco secretos"),
            new(3, "Qual matéria todo calouro jura que vai ser tranquila?",
                "Cálculo I", "Estatística", "Metodologia científica", "Física geral"),
            new(3, "Qual a principal fonte de energia do aluno no fim do semestre?",
                "Café da máquina do corredor", "Energético de posto", "Raiva do boleto", "Pão de queijo da cantina"),
            new(3, "O professor disse \"essa cai com certeza\". O que cai na prova?",
                "Essa mesma, com outros números", "A que ele disse que não caía", "Uma que ninguém anotou", "Nenhuma das que ele passou"),
        };

        static readonly List<int> usedChutes = new();
        /// <summary>Índice (0 a 3, nas opções originais) da resposta certa "do dia" de cada chute. Sorteado em Reset.</summary>
        static readonly int[] gabarito = new int[chutes.Length];
        // gerador próprio (e não UnityEngine.Random) para o sorteio ser reproduzível nos testes com uma semente
        static System.Random rng = new();

        /// <summary>
        /// Próxima pergunta de chute (não repete até usar todas; uma partida usa no máximo 10).
        /// Devolve uma CÓPIA com a certa do dia em options[0], a convenção que o QuizChallenge já usa.
        /// </summary>
        public static Question NextChute()
        {
            if (usedChutes.Count >= chutes.Length) usedChutes.Clear();
            int pick;
            do pick = rng.Next(chutes.Length);
            while (usedChutes.Contains(pick));
            usedChutes.Add(pick);

            var src = chutes[pick];
            int certa = gabarito[pick];
            var opts = new string[src.options.Length];
            opts[0] = src.options[certa];
            for (int i = 0, k = 1; i < src.options.Length; i++)
                if (i != certa) opts[k++] = src.options[i];
            return new Question(src.level, src.text, opts);
        }

        /// <summary>Resposta certa desta partida para a pergunta de chute com este texto (null se não existir). Para testes.</summary>
        public static string RespostaCertaDoDia(string pergunta)
        {
            for (int i = 0; i < chutes.Length; i++)
                if (chutes[i].text == pergunta) return chutes[i].options[gabarito[i]];
            return null;
        }

        /// <summary>Cópia do gabarito sorteado (índice da certa em cada chute). Para testes.</summary>
        public static int[] GabaritoChutes => (int[])gabarito.Clone();

        static readonly Question[] all =
        {
            // ---------------- fáceis
            new(0, "Quanto é 2 + 2 × 2?", "6", "8", "4", "Professor, isso não cai na prova"),
            new(0, "Qual destes NÃO é um estado físico da matéria?", "Cansado", "Sólido", "Líquido", "Gasoso"),
            new(0, "Raiz quadrada de 81:", "9", "8", "18", "Uma raiz muito brava"),
            new(0, "Quanto é 7 × 8?", "56", "54", "48", "Um número feio"),
            new(0, "Qual a capital do Brasil?", "Brasília", "Rio de Janeiro", "São Paulo", "Marília, no coração"),
            new(0, "Fórmula química da água:", "H2O", "CO2", "H2O2", "Galão de 20 litros"),
            new(0, "Quantos lados tem um hexágono?", "6", "8", "5", "Depende do lado"),
            new(0, "Qual planeta é conhecido como Planeta Vermelho?", "Marte", "Vênus", "Júpiter", "O da conta de luz"),
            new(0, "25% de 200 é:", "50", "25", "75", "Desconto de Black Friday falso"),
            new(0, "Um aluno estudou 0 horas e dormiu 8. Quantas horas ele estudou?", "0", "8", "4", "Depende do ChatGPT"),

            // ---------------- médias
            new(1, "Um trem sai de Marília às 8h a 60 km/h. A que horas ele percorre 120 km?", "10h", "9h", "8h30", "Nunca, é greve"),
            new(1, "Em binário, '10' vale:", "Dois", "Dez", "Um", "Uma piada nerd"),
            new(1, "Quem escreveu 'Dom Casmurro'?", "Machado de Assis", "José de Alencar", "Prof. Takanervoso", "Capitu, com ghost-writer"),
            new(1, "Qual o maior órgão do corpo humano?", "A pele", "O fígado", "O coração", "O boleto"),
            new(1, "Você tem 10 minutos para 5 questões. Quanto tempo por questão?", "2 minutos", "5 minutos", "1 minuto", "Pânico"),
            new(1, "Um café da cantina custa R$ 6. Com R$ 20, quantos cafés você compra e quanto sobra?", "3 cafés e R$ 2 de troco", "3 cafés e R$ 4 de troco", "4 cafés e nada de troco", "Nenhum, a cantina não aceita moeda"),
            new(1, "Em que ano terminou a Segunda Guerra Mundial?", "1945", "1939", "1918", "Quando acabou a bateria do celular"),
            new(1, "Quantos minutos tem 3 horas e meia?", "210", "180", "330", "Mais do que a paciência do professor"),
            new(1, "Qual o plural de 'cidadão'?", "Cidadãos", "Cidadões", "Cidadães", "Cidadãozinhos"),
            new(1, "Qual a média das notas 4, 6 e 8?", "6", "5", "18", "6,5 arredondando com o coração"),
            new(1, "Se o professor está de costas, ele está:", "Todas as alternativas", "Distraído", "Olhando pelo reflexo da janela", "Testando você"),

            // ---------------- difíceis
            new(2, "Qual é a derivada de x²?", "2x", "x", "x³/3", "Um trauma"),
            new(2, "Quem pintou 'Abaporu'?", "Tarsila do Amaral", "Anita Malfatti", "Di Cavalcanti", "Candido Portinari"),
            new(2, "Qual é a unidade de resistência elétrica?", "Ohm", "Volt", "Ampère", "Watt"),
            new(2, "3 impressoras imprimem 3 provas em 3 minutos. Quanto tempo 100 impressoras levam para imprimir 100 provas?", "3 minutos", "100 minutos", "33 minutos", "Depende, sempre falta tinta"),
            new(2, "Qual o próximo número: 1, 1, 2, 3, 5, 8, ...?", "13", "11", "12", "16"),
            new(2, "Quantos ossos tem o corpo humano adulto, aproximadamente?", "206", "306", "186", "Todos doendo depois da prova"),
            new(2, "'Penso, logo existo' é de:", "Descartes", "Sócrates", "Kant", "Do aluno que lembrou da prova hoje"),
            new(2, "Qual é o menor número primo?", "2", "1", "0", "3"),
            new(2, "Você anda 3 m para o norte e 4 m para o leste. A que distância está do ponto de partida?", "5 m", "7 m", "1 m", "12 m"),
        };

        // chance de cada nível (fácil, médio, difícil) para cada "tier" de dificuldade
        static readonly float[][] mix =
        {
            new[] { 0.7f, 0.3f, 0f },
            new[] { 0.3f, 0.5f, 0.2f },
            new[] { 0.1f, 0.45f, 0.45f },
        };

        static readonly List<int> used = new();

        public static Question Next(int tier)
        {
            tier = Mathf.Clamp(tier, 0, 2);
            float r = Random.value;
            int level = r < mix[tier][0] ? 0 : r < mix[tier][0] + mix[tier][1] ? 1 : 2;

            var pool = new List<int>();
            for (int pass = 0; pass < 2 && pool.Count == 0; pass++)
            {
                for (int i = 0; i < all.Length; i++)
                    if (all[i].level == level && !used.Contains(i)) pool.Add(i);
                if (pool.Count == 0) used.RemoveAll(i => all[i].level == level); // esgotou o nível: recicla
            }
            int pick = pool[Random.Range(0, pool.Count)];
            used.Add(pick);
            return all[pick];
        }

        /// <summary>
        /// Começo de partida: esquece as perguntas já usadas e sorteia um gabarito novo para os chutes.
        /// Chamado no Awake do QuizChallenge, ou seja, a cada carga da cena (o domínio não recarrega entre Plays).
        /// </summary>
        public static void Reset() => Reset(System.Guid.NewGuid().GetHashCode());

        /// <summary>Igual a <see cref="Reset()"/>, com semente fixa: mesma semente = mesmo gabarito e mesma ordem (testes).</summary>
        public static void Reset(int seed)
        {
            used.Clear();
            usedChutes.Clear();
            rng = new System.Random(seed);
            for (int i = 0; i < gabarito.Length; i++)
                gabarito[i] = rng.Next(chutes[i].options.Length);
        }

        /// <summary>Quantas perguntas de chute existem (para testes).</summary>
        public static int ChuteCount => chutes.Length;
    }
}
