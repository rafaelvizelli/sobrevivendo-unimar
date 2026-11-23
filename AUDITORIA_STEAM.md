# Auditoria Steam — Sobrevivendo à UNIMAR (Fase 1: A Prova Impossível)

Data: 23/11/2026 · Branch auditado: `personagens-kenney` (commit `c7fd7a2`) · Etapa 1 da "Operação Steam".
Nenhum arquivo de código, cena, prefab ou configuração foi alterado nesta etapa. Único arquivo criado: este.

**Legenda de evidência** (vale para todo o documento):
- **[código]** lido no código-fonte; **[teste]** visto na suíte automatizada desta auditoria;
- **[captura]** visto em capturas de tela existentes (`Captures/` e capturas de 18/11);
- **[dado]** vem do `partidas.csv`; **[inferência]** é opinião de design, ainda não validada com jogadores.

---

## 1. Diagnóstico geral

O projeto é uma **fatia vertical rica em sistemas e pobre em validação**. A engenharia está acima da média de
um projeto indie nessa fase: regras puras testáveis, números centralizados em `DifficultySettings`, regras decididas
pelo enum `Infracao`, 66 scripts, cerca de 240 testes e notas de desenvolvimento exemplares. A Fase 1 pode ser jogada
do menu até o resultado.

Mas o jogo ainda **não foi provado como jogo**. São 2 partidas humanas registradas (uma delas anterior a quase todas
as mecânicas atuais), nenhuma no Normal e nenhuma usando as 4 colas novas **[dado]**. Hoje não dá para afirmar que ele é
divertido, justo ou claro. Dá para afirmar que funciona.

Estimativa de prontidão para uma página da Steam com demo: **cerca de 35%**. O que falta não é mais código. É:
(1) provar e ajustar o núcleo com jogadores; (2) dar personalidade audiovisual ao professor e à sala; (3) dar ao
jogador metas além de "passar"; (4) colocar o básico comercial no lugar (controle, opções de vídeo, legendas/idioma,
licenças).

---

## 2. Estado atual (o que existe)

| Área | Estado | Evidência |
|---|---|---|
| Fluxo | Menu → (tutorial na 1ª vez) → prova → resultado com "ficha criminal" → R joga de novo / M menu | [código] `GameManager` |
| Dificuldades | Fácil / Normal / Difícil + "Treino", todos os números no Inspector do GameManager | [código] |
| Prova | 10 questões; N "conhecidas" (quiz com tempo), o resto é chute (25%) ou cola | [código] `ExamSystem`, `QuizBank` |
| Colas | Celular (2), Nerd (espiar), Fundão (favor), Colinha (Q), Garrafinha (G), Bilhete (B), Fone (F), com orçamento global por partida | [código] |
| Professor | Cone + linha de visão, audição (Noise), estados Tranquilo/Desconfiado/Investigando/Flagrante/Distraído, aviso de virada, "Atento" | [código] `ProfessorAI` |
| Colegas | Nerd (olhada de lado), Fundão (caos/cola), Dorminhoco (ronco), Puxa-saco (dedura), figurantes | [código] |
| Eventos | 6 oportunidades e 4 ameaças, sem repetir os 2 últimos | [código] `EventDirector` |
| Desafio "Cara de Paisagem" | Barra com zona verde (QTE) quando a investigação chega até você | [código] `FacePokerChallenge` |
| Visual | Quaternius low-poly com cabeção e enfeites; sala de um pacote de terceiros (`Assets/school`); objetos de cola na mão | [captura] |
| UI | Identidade "prova impressa" (Special Elite + Caveat); HUD montado em código com `UnityEngine.UI.Text` | [captura] [código] |
| Áudio | 100% sintetizado em código, 3D para passos/"hm?"; **sem música** | [código] `SoundFX` |
| Opções | Volume geral, volume de efeitos e sensibilidade. **Sem** vídeo, remapeamento, controle ou idioma | [código] `GameOptions` |
| Registro | `partidas.csv` com 30+ colunas por partida | [código] `MatchLog` |
| Testes | EditMode + PlayMode, incluindo simulação de equilíbrio e auditoria de risco | [teste] ver §9 |

---

## 3. Pontos fortes (preservar)

1. **O conceito.** "Colar na prova de um professor paranoico" é imediatamente entendível, é universal no Brasil e
   rende humor sem esforço. Tem pitch de uma linha, que é o ativo mais raro de um indie.
2. **A leitura no corpo.** Pigarro antes de virar, Nerd que para de escrever antes de olhar, Dorminhoco que balança
   antes de roncar, "hm?" posicional. É design de furtividade de verdade: o perigo é anunciado e justo **[código]**.
3. **Identidade da UI "prova impressa".** O menu e a tela final (carimbo REPROVADO, "ficha criminal") têm cara
   própria e são o melhor material visual do jogo hoje **[captura]**.
4. **Humor do texto.** As perguntas de chute ("unidade oficial do desespero", "Uma e meia" no trabalho em grupo) e as
   falas do Takanervoso têm voz brasileira e universitária autêntica **[código]** `QuizBank`, `Falas`.
5. **Arquitetura de regras.** Regras puras e estáticas (`CheatRules`, `CanBeDistracted`, `SuspicionStep`, `MoodFor`)
   testáveis sem cena; enum `Infracao` em vez de texto; proteção contra input duplicado no mesmo frame.
6. **Disciplina de projeto.** `CLAUDE.md`, notas por bloco com o que foi e o que não foi visto, e telemetria
   (`partidas.csv`) já pronta para o playtest.
7. **Orçamento de colas.** Um teto global por partida obriga a escolher **qual** cola usar, em vez de usar todas.
   É a decisão de design mais interessante do jogo.

---

## 4. Problemas críticos (P0, bloqueiam qualquer avanço sério)

### C1. Nenhuma validação com jogadores
2 partidas: Fácil em 04/11 (antes dos Blocos 1–15) e Difícil em 10/11 **[dado]**. Zero no Normal, zero com
Q/G/B/F. Todos os números são "chute educado", como as próprias notas dizem. Qualquer ajuste de equilíbrio hoje é
especulação.

### C2. No Normal e no Difícil, jogar perfeitamente não garante passar: o dado decide
Conta direta dos números **[código]** `GameManager`:

| | Conhecidas | Orçamento de colas | Pontos garantidos | Nota mínima | Falta vir do chute (25% cada) | Teto de aprovação com jogo perfeito |
|---|---|---|---|---|---|---|
| Fácil | 3 | 5 | 8 | 6 | 0 | 100% |
| Normal | 3 | 3 | 6 | 7 | ≥1 acerto em 4 chutes | **≈68%** (1 − 0,75⁴) |
| Difícil | 3 | 2 | 5 | 7 | ≥2 acertos em 5 chutes | **≈37%** |

Isso antes de qualquer flagrante (cada um anula 1 ponto). Um jogador que executa tudo sem erro reprova **1 em cada 3
vezes no Normal** por pura sorte, e a tela final não diz isso. É o tipo de coisa que gera review negativa ("perdi
sem ter feito nada errado"). A simulação de 10/11 ("Normal ≈ 64%") bate com essa conta. O teto baixo é estrutural,
não é questão de habilidade.
**[inferência]** A tensão deveria vir de *quanto risco você aceita*, não do dado no final.

### C3. As colas curtas não podem dar flagrante direto. O juiz real é um QTE genérico
O flagrante exige suspeita 100 **enquanto ele vê** (`ProfessorAI.Perceive`). Colinha (1 s), garrafinha (1,5 s) e
arremesso do bilhete (0,6 s) acabam antes de chegar lá. A auditoria 15c mediu: colinha vista inteira a 2,5 m = 39 de
suspeita no Normal **[código]** **[teste da 15c]**. Consequências:
- o risco dessas colas vira "o professor vem investigar → pare tudo → passe na Cara de Paisagem";
- a **Cara de Paisagem** (barra com zona verde, 3 acertos) vira o portão de quase todo o risco do jogo, e é a mecânica
  menos temática e menos original do pacote;
- estratégia dominante provável **[inferência]**: usar colinha/garrafinha a qualquer hora, parar quando aparecer o
  "!", passar no QTE (no Fácil o QTE permite 2 erros).
Os 4 desvios da 15c (colinha, bilhete achado, celular tocando de costas e garrafinha vista) continuam sem decisão.

### C4. O tutorial ensina 3 das 7 formas de colar
Passos: Andar, Sentar, Responder, Celular, Nerd, Distração, Entregar **[código]** `Tutorial.cs`. Não ensina:
colinha, garrafinha, bilhete, fone, o **orçamento de colas**, a **Cara de Paisagem** e como ler o cone e o "?". O
jogador da Steam vai descobrir Q/G/B/F lendo um painel de texto no canto da tela.

---

## 5. Problemas de gameplay

| # | Problema | Evidência | Impacto |
|---|---|---|---|
| G1 | Resultado depende do chute (ver C2) | [código] | Alto |
| G2 | Colas curtas sem flagrante direto; QTE decide (ver C3) | [código][teste 15c] | Alto |
| G3 | **Risco acumulado não é modelado** na simulação (cada cola avaliada sozinha), então o equilíbrio "medido" é otimista | notas, `SimulacaoDeEquilibrioTests` | Alto |
| G4 | Bilhete pode trazer a resposta de uma questão que você **já sabia** (`RandomBlank` inclui as conhecidas): a cola é gasta para nada | [código] `ExamSystem.RandomBlank` | Médio: sensação de injustiça |
| G5 | Ouvir barulho grava `Infracao.Outra`, e o "Dedurado" do Puxa-saco sobrescreve o tipo. O professor nunca "acha" o bilhete nem o celular que tocou | [código] `HearNoise`, `Alert` | Médio |
| G6 | Excesso de teclas: 1, 2, Q, G, B, F, E, Espaço, Shift, Esc, R, M, T. Algumas são "segure", outras "aperte", outras "mire" | [código] | Médio/alto na 1ª sessão |
| G7 | Partida de 5 a 8 min numa sala só, com a mesma prova. Os eventos variam, mas a estrutura é idêntica a cada partida | [código] | Alto para rejogabilidade |
| G8 | Nenhuma meta além de "passar": sem nota de estilo, recorde, conquista ou desafio. A "ficha criminal" mostra números mas não premia nada | [captura][código] | Alto para rejogabilidade |
| G9 | Fácil pode ficar trivial: 8 pontos garantidos sem chute, 2 erros permitidos no QTE e o 1º flagrante é só advertência **[inferência]** | [código] | Médio |
| G10 | Celular de 3 s com taxa 75/s: visto, quase sempre vira flagrante. Fora da vista, é a cola mais segura que existe. Risco binário: decide o cone, não a habilidade **[inferência]** | [código] | Médio |

---

## 6. Problemas visuais e de áudio

| # | Problema | Evidência |
|---|---|---|
| V1 | **O professor não tem rosto expressivo** desde os Quaternius (as expressões se perderam; sobram balão e ícone). O antagonista e mascote é o personagem menos expressivo da sala | notas, [código] `CharacterModelSkin` |
| V2 | Pela câmera do jogo, o professor ocupa uma área pequena no fundo da sala, com a mesma paleta e escala dos alunos. Não se destaca como ameaça | [captura] `camjogo_Celular.png` |
| V3 | Sala genérica de pacote: paredes vazias, iluminação chapada, uma escada no meio da sala, o projetor tapando a lousa (o texto da prova na lousa fica cortado). Nada diz "UNIMAR" ou "Brasil" | [captura] `01_sala_frente.png` |
| V4 | Mistura de estilos: Quaternius (sombreado liso) + pacote `school` + enfeites de primitivas + UI de papel texturizada. Falta uma regra de material/iluminação comum | [captura] |
| V5 | HUD carregado: 2 faixas de aviso no topo + nota + painel de colas + ajuda embaixo + barra de ação, por cima da lousa. Muito texto com cinza sobre papel em tamanho pequeno | [captura] `06_hud_flagrante...png` |
| V6 | Sem animação de reação: flagrante, Cara de Paisagem e aprovação acontecem em painéis de UI, não no mundo (sem câmera, sem pose, sem "momento") | [código] |
| V7 | **Sem música.** Num jogo de tensão + comédia, a trilha faz metade do trabalho (subir na investigação, cair no alívio) | [código] |
| V8 | Áudio 100% sintetizado: funciona como sinal, mas soa como protótipo. Sem voz/grunhidos do professor ("HMMM?", "AHÁ!") | [código] `SoundFX` |
| V9 | As mulheres (rig Generic) não têm animação de andar nem de ficar paradas; a pose é refeita por código a cada quadro **[inferência: devem parecer duras se andarem]** | notas |
| V10 | Não foi visto: uma partida inteira jogada à mão com os modelos 3D. As capturas de 18/11 foram tiradas em batch, por teste | notas |

---

## 7. Problemas técnicos

| # | Problema | Risco | Evidência |
|---|---|---|---|
| T1 | **Cena com ajustes manuais + menu "Montar Fase 1" destrutivo.** Duas fontes de verdade; impede level design seguro e qualquer fase nova | Alto | `CLAUDE.md`, notas |
| T2 | ~~Cena salva antes do Bloco 15: valores de dificuldade talvez não aplicados.~~ **Resolvido no S0:** o teste `ValoresNaCenaTests` abre a cena e compara as 4 dificuldades campo a campo com o código: são iguais (a Unity usa o valor do código para campos que não estão gravados) | — | teste S0 |
| T3 | Só teclado e mouse, lidos direto de `Keyboard.current`/`Mouse.current`; o `InputSystem_Actions` do projeto não é usado. Sem remapeamento e **sem controle/Steam Deck** | Alto (comercial) | [código] |
| T4 | Sem opções de vídeo (resolução, tela cheia/janela, vsync, qualidade, FOV) | Médio | [código] |
| T5 | Textos espalhados: há literais em `GameManager`, `FacePokerChallenge`, `PlayerController`, `EventDirector` além de `Falas`. Localização (inglês) exigiria uma varredura | Médio/alto se quiser alcance fora do Brasil | [código] |
| T6 | `HUD.cs` com 1033 linhas e `ProfessorAI.cs` com 789. UI inteira criada por código com o `Text` legado (sem TextMeshPro): difícil de iterar visualmente | Médio | [código] |
| T7 | Singletons estáticos `I` em quase tudo (GameManager, HUD, ProfessorAI, PlayerController, ExamSystem...). Ok para 1 fase; fica frágil com várias cenas e carregamentos | Médio (futuro) | [código] |
| T8 | **Licença do pacote `Assets/school` não registrada** no projeto (nomes em russo, "doroga — krest"; nenhum arquivo de licença). Precisa saber a origem antes de vender | Alto (legal) | [código] |
| T9 | Branch atual (`personagens-kenney`) só local: não está no GitHub nem no `master`, e o nome não condiz mais com o conteúdo | Médio | git |
| T10 | Cena temporária do test runner esquecida na raiz de `Assets/` (`InitTestScene…unity`) | Baixo | git status |
| T11 | `bundleVersion 0.1.0`; janela 1024 de largura por padrão e não redimensionável; sem tela de créditos/licenças (CC0 não exige, fontes podem exigir) | Baixo/médio | `ProjectSettings` |
| T13 | **"UNIMAR" é o nome de uma universidade real.** Vender um jogo com o nome (e a sigla no papel da prova) exige decisão do dono: autorização ou nome fictício para a versão comercial | Alto (legal/marca) | menu, `UITheme` |
| T12 | Desempenho nunca medido em máquina comum (sombras, cone de visão como mesh dinâmica, modelos skinned + IK por código a cada quadro em ~30 personagens) | Médio, **não medido** | [código] |

---

## 8. Experiência comercial (olhar de quem comprou na Steam)

- **Primeiros 30 s:** um menu bonito de papel com um parágrafo para ler. A primeira vez força o tutorial (bom), mas
  o tutorial começa por "andar" e "sentar": os primeiros 30 s não mostram o professor, a tensão nem a piada. **[inferência]**
- **Clareza do objetivo:** "tire 7 e entregue na mesa" aparece em toast. O porquê de não conseguir 7 sem colar só fica
  claro depois de entender conhecidas, chute e orçamento. **[inferência]**
- **Feedback:** bom no professor (ícones, barrinha, passos). Fraco nas recompensas: "+1,0 — O Google sabia!" em toast é
  o único pagamento de uma cola bem-sucedida. **[captura]**
- **Ritmo:** cooldown de 8 s entre questões no Normal e 14 s no Difícil, o "tempo morto" do jogo; os eventos vêm a cada
  38–56 s. Precisa de playtest para saber se há tédio. **[inferência]**
- **Resultado:** ótimo visual. Faltam: o porquê da derrota, o melhor resultado anterior e um motivo para tentar de novo.
- **Reiniciar:** excelente (R na tela final).
- **Acessibilidade:** sem remapeamento, sem tamanho de texto, sem legenda para os sinais sonoros (o "hm?" e os passos
  são informação de jogo), sem modo daltônico (o perigo é verde/amarelo/vermelho).
- **Duração do conteúdo:** 1 fase de ~6 min × 3 dificuldades. Sozinho, isso sustenta uma **demo gratuita**, não um
  produto pago. **[inferência]**

---

## 9. Testes nesta auditoria

Rodados em batch, com o Unity fechado, sem nenhuma alteração no projeto:

- EditMode: **131/131 passaram** (0 falhas, 0 ignorados).
- PlayMode: **108/108 passaram** (0 falhas, 0 ignorados), incluindo os 14 da auditoria de risco 15c e os de modelos 3D.
- Depois da rodada, o `git status` só mostra este documento e a `InitTestScene` que já existia: os testes não
  alteraram a cena nem os assets.

O que a suíte **não** cobre: diversão, clareza para quem nunca jogou, o valor absoluto de cada dificuldade na cena (T2;
os testes comparam relativos), desempenho e uma partida inteira jogada à mão com os modelos 3D. Passar 100% aqui
significa "não quebrou", não "está bom".

---

## 10. Os 10 problemas mais importantes (em ordem)

1. **C1:** zero playtest real com as mecânicas atuais.
2. **C2:** no Normal/Difícil o chute decide; jogo perfeito reprova 1/3 (Normal) a 2/3 (Difícil) das vezes.
3. **C3:** colas curtas sem flagrante; a Cara de Paisagem (QTE genérico) é o juiz de quase todo o risco.
4. **C4:** o tutorial não ensina 4 colas, o orçamento nem a Cara de Paisagem.
5. **V1/V2:** professor sem expressão e pequeno na tela; o antagonista não tem presença.
6. **V7/V8:** sem música e sem voz; o áudio soa como protótipo.
7. **G7/G8:** nada para buscar além de passar; rejogabilidade depende só dos eventos.
8. **T3/T4:** sem controle, sem remapeamento, sem opções de vídeo.
9. **T8:** licença do cenário desconhecida (risco legal para vender).
10. **T1:** a cena como fonte de verdade não está resolvida ("Montar Fase 1" destrutivo). (T2 foi conferido no S0: sem problema.)

## 11. As 10 oportunidades de maior impacto

1. **Playtest guiado** (5–8 pessoas, roteiro + `partidas.csv` + 3 perguntas). Custo baixo, define todo o resto.
2. **Trocar sorte por risco:** o que falta para a nota mínima vem de uma cola *arriscada* extra ou de "arriscar um
   chute informado" (ex.: dica do Fundão = 50/50), não de 25% cego.
3. **Flagrante instantâneo de perto** para colinha/bilhete/celular tocando (já proposto na 15c), com chance de
   "achar" na investigação. O risco passa a depender de quando e onde você cola.
4. **Cara de Paisagem temática:** em vez da barra, desviar o olhar / segurar a respiração / dar uma desculpa (escolha
   de fala com tempo). Mesma função, 10× mais memorável e "printável".
5. **O professor como estrela:** rosto com 4–5 expressões (textura ou blendshape simples), escala ~1,15×, cor/silhueta
   própria, voz em grunhidos. É ele que vai na capa da Steam.
6. **Trilha adaptativa simples:** 1 loop com 3 camadas (calmo / desconfiado / investigando) + estinger no flagrante.
7. **"Nota de estilo" na ficha criminal:** título engraçado conforme o jeito que você passou ("Ninja do Fundão",
   "Sobrevivente do 6,9"), recorde por dificuldade e 6–10 conquistas locais. Dá motivo para jogar de novo.
8. **Momento de câmera no flagrante:** zoom no professor apontando, quadro congelado com carimbo "FLAGRANTE!" (o estilo
   de papel já existe). É o GIF da página da Steam.
9. **Sala com cara de UNIMAR/Brasil:** cartazes, ventilador de teto, ar-condicionado pingando, cadeiras de braço
   tortas, lousa sem o projetor na frente, luz de fim de tarde. Barato e dá identidade.
10. **Input via Input Actions + controle:** abre Steam Deck e remapeamento de uma vez.

## 12. O que deve ser preservado

- O pitch, o nome, o Prof. Takanervoso e o tom do humor (perguntas de chute, falas).
- A leitura no corpo (pigarro, Nerd parando, Dorminhoco balançando) e os sons posicionais como informação.
- O orçamento global de colas.
- A identidade "prova impressa" do menu e da tela final.
- A arquitetura de regras puras + enum `Infracao` + `DifficultySettings` + testes + `partidas.csv`.
- Os modelos Quaternius com cabeção (melhoraram a leitura e o humor; falta acabamento, não troca).

## 13. O que NÃO vale a pena desenvolver agora

- **Fase 2 ou novas salas.** Antes de provar que a Fase 1 diverte, mais conteúdo multiplica um núcleo não validado.
- **Novas colas.** Já são 7; o problema é clareza e risco, não quantidade.
- **Multiplayer, editor de fases, progressão longa/meta-jogo com moeda.**
- **Trocar o pacote de personagens de novo.** Kenney e Genies já custaram dias; os Quaternius servem.
- **Reescrever o HUD em TMP/UI Toolkit inteiro agora.** Só se um bloco de UI exigir; antes, simplificar o que aparece.
- **Localização completa** antes de decidir o mercado. Mas centralizar os textos (T5) é barato e vale já.
- **Mais auditoria e simulação sem jogadores.** O retorno disso acabou.

## 14. Primeiros blocos recomendados (detalhe completo virá no ROADMAP_STEAM.md)

| Ordem | Bloco | Prioridade | Por quê primeiro |
|---|---|---|---|
| S0 | **Higiene de produção:** confirmar T2 (valores na cena), licença do `school` (T8), branch no GitHub/merge (T9), limpar `InitTestScene` (T10), build novo | P0 | Barato e destrava o resto com segurança |
| S1 | **Playtest instrumentado:** roteiro de teste, 3 perguntas pós-partida, colunas que faltam no CSV (nota antes do chute, tempo até o 1º flagrante, investigações por cola) | P0 | Sem dados, todo ajuste é chute |
| S2 | **Decisões da 15c + sorte → risco:** flagrante direto de perto, "achar" bilhete/celular, e rever a matemática do C2 (com hipótese documentada) | P0 | Corrige o núcleo antes de enfeitar |
| S3 | **Tutorial completo e mais curto:** ensinar orçamento, as 4 colas e a Cara de Paisagem; o professor aparecendo nos primeiros 30 s | P1 | Primeira impressão |
| S4 | **Professor estrela:** expressões, escala/silhueta, voz em grunhidos, momento de câmera no flagrante | P1 | Maior ganho perceptivo por hora |
| S5 | **Trilha adaptativa + troca de 4–5 sons-chave** | P1 | Tensão e comédia |

Depois: Cara de Paisagem temática (P1), nota de estilo/recordes/conquistas (P1), input com controle/remapeamento e
opções de vídeo (P1), sala com identidade (P2), HUD mais enxuto (P2), centralizar textos (P2), medição de desempenho (P2).

---

## 15. Critérios objetivos de "pronto para demo na Steam"

| Critério | Meta mensurável |
|---|---|
| Validação | ≥ 30 partidas de ≥ 8 pessoas diferentes registradas no `partidas.csv` |
| Justiça | No Normal, jogo "sem erros" aprova ≥ 90% (simulação + playtest); toda reprovação tem causa mostrada na tela final |
| Dificuldade | Taxa de aprovação no 1º contato: Fácil 70–90%, Normal 40–60%, Difícil 15–30% |
| Clareza | ≥ 80% dos testadores explicam sem ajuda o que é o orçamento de colas e como o professor vê/ouve |
| Diversão | ≥ 70% respondem "jogaria mais uma" logo após a 1ª partida; ≥ 50% jogam 3+ partidas espontaneamente |
| Primeira impressão | Tempo até o primeiro momento de tensão (professor olhando enquanto você cola) ≤ 60 s na 1ª sessão |
| Estabilidade | 0 erros/exceções no `Player.log` em 10 partidas seguidas; suíte de testes 100% |
| Desempenho | ≥ 60 FPS em 1080p numa GPU integrada recente (ex.: Iris Xe / Radeon Vega 8), medido |
| Controles | Jogável inteiro com controle; teclas remapeáveis; opções de vídeo básicas |
| Legal | Licença de cada asset de terceiros registrada em `CREDITOS.md` e numa tela de créditos |
| Apresentação | Professor com ≥ 4 expressões visíveis pela câmera do jogo; música com ≥ 2 estados de tensão |
