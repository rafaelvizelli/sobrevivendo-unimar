# Sobrevivendo à UNIMAR — notas de desenvolvimento

Última atualização: 10/11/2026 (Bloco 16 etapa 2 — identidade visual "prova impressa")

## Onde paramos
- Fase 1 ("A Prova Impossível") jogável de ponta a ponta, com tutorial. Código no GitHub (privado):
  https://github.com/rafaelvizelli/sobrevivendo-unimar (branch master). Regras do projeto em `CLAUDE.md`.
- Blocos 1–7 (04/11): prova que exige cola, identidade das colas, economia de distrações, estabilidade, áudio,
  ruído como evento, falas com consequência.
- Blocos de 09/11 (auditoria nota 5,0): configuração (SampleScene, MCP fixado, confirmação do "Montar Fase 1");
  gabarito do chute sorteado; **8** cola necessária + ameaças; **9** leitura no corpo + HUD enxuta;
  **10** tutorial jogável; **11** level design (colegas bloqueiam a visão, rotas).
- **12** (10/11) consolidação: números fixos movidos para `DifficultySettings`, bugs do Fundão/cone/ícones,
  grandões voltaram a 1×, testes que faltavam, executável regenerado.
- **13** (10/11) defeitos visuais da auditoria. **14** (10/11) acabamento: nome do jogo/estúdio (dados mudaram de
  pasta!), pacotes sem uso removidos, tela final com resumo + jogar de novo, opções na pausa, áudio ambiente.
- **15** (10/11) colas novas: colinha (Q), bilhete (B), fone (F), garrafinha (G) + orçamento de colas da partida
  (`maxCheatAnswers`, vale para TODAS as colas). Playtest real até agora: 1 partida (Difícil, reprovado 5,0).
- **16** (10/11) identidade visual dos menus: etapa 1 feita (3 direções + protótipos em `Captures/MenuOpcoes/`);
  Etapa 2 feita: direção A "prova impressa" (tema único `UITheme`, fontes Special Elite e Caveat).
- **15b** (10/11) `partidas.csv` registra as colas novas (colunas no fim); ícone do Nerd com teto de tamanho;
  simulação com perfil arrojado e flagrantes/partida. Bug do flagrante com bilhete no ar (prendia 1 vaga do
  orçamento) corrigido.
- 18/11: personagens 3D Quaternius (commit `c7fd7a2`, branch `personagens-kenney`).
- **Operação Steam (23/11)**: `AUDITORIA_STEAM.md`, `DIRECAO_CRIATIVA.md`, `ROADMAP_STEAM.md` (com status por
  bloco), `CREDITOS.md`, `ROTEIRO_PLAYTEST.md`. Blocos S0–S8 e S12 feitos (seção "Operação Steam" abaixo): núcleo
  justo (orçamento cobre a nota mínima, colinha pega na hora, investigação acha provas), tutorial novo, professor com
  sobrancelhas/voz/câmera/carimbo no flagrante, trilha adaptativa provisória, título/dica/recorde/conquistas,
  controle (gamepad), opções de vídeo. Executável 0.2.0 gerado. **Nada commitado** (o usuário faz).
- Próximo: **PLAYTEST HUMANO** com `ROTEIRO_PLAYTEST.md` (o S2 mudou números por hipótese); decisões do dono: nome
  "UNIMAR", origem do pacote `school`, visual da sala (S9).

## Como rodar os testes
- Batch (Unity FECHADO): `Unity.exe -batchmode -projectPath <proj> -runTests -testPlatform EditMode|PlayMode -testResults <xml> -logFile <log>`.
  No Linux o executável do editor é `~/Unity/Hub/Editor/6000.6.3f1/Editor/Unity` (mesmos argumentos).
- No editor: Window > General > Test Runner (abas EditMode e PlayMode), com a janela do Unity em foco.
- Em batch o cursor não trava: o teste de fluxo pula só essa checagem (`Application.isBatchMode`).

## Relatórios dos blocos

### Bloco 1 — a prova que exige cola (04/11)
- Linha de base antes de mexer: EditMode 8/8; PlayMode 4/5 (falha: checagem de cursor travado, impossível em batch;
  a falha escondia o resto do teste de fluxo, que nunca tinha rodado).
- Regras novas: o jogador só sabe algumas questões (Fácil 4, Normal 3, Difícil 2; `DifficultySettings.knownQuestions`,
  valores também gravados na cena), sorteadas no início e marcadas com moldura amarela no HUD.
  [1] responde primeiro as conhecidas (Desafio B normal); acabando, vira CHUTE (12 perguntas absurdas, 4 opções, ~25%).
  Cola (celular/Nerd/Fundão) preenche primeiro as que ele não sabe, depois corrige erradas, por último as conhecidas.
- Bug da tecla 1: `QuizChallenge` ignora as teclas no frame em que abriu (`openedFrame`).
- Arquivos: ExamSystem, QuizChallenge, QuizBank, PlayerController, GameManager, HUD, cena (3 linhas), testes.
- Testes depois: EditMode 19/19, PlayMode 9/9 (incl. tecla 1 com teclado virtual e sorteio uniforme do chute).
- Validado no editor com teclado/mouse reais: menu (clique), moldura nas 3 conhecidas, Desafio B abre sem responder,
  segundo aperto responde, chute aparece quando as conhecidas acabam, chute errado cai numa desconhecida.
- Riscos conhecidos: perguntas de chute se repetem entre partidas (12) — o jogador pode decorar o gabarito;
  1-2 perguntas têm resposta "adivinhável" pelo humor. Equilíbrio ainda sem partida completa registrada.

### Bloco 2 — identidade das colas (04/11)
- `enum Infracao` (GameManager.cs): o confisco decide pelo TIPO (`kind == Infracao.Celular`), não mais por
  `reason.Contains("celular")`. O tipo nasce no Player (celular/fora do lugar/correndo) ou no Interactable
  (`SuspicionKind`; Nerd = EspiarNerd) e passa pelo ProfessorAI até `ApplyFlagrante(reason, kind)`.
- Celular: bateria por partida (`phoneCharges`: Fácil 3, Normal 2, Difícil 2). Sem bateria ≠ confiscado.
  Fácil agora também confisca (o 1º flagrante lá continua sendo só advertência).
- Nerd: saiu o sorteio de 15%. Ele para de escrever (aviso, 1,5 s) e olha de lado (1,8 s) a cada
  `nerdGlanceInterval` (Fácil 16-24 s, Normal 11-17 s, Difícil 7-12 s). Só pega quem espia DURANTE a olhada.
  HUD avisa ("parou de escrever... vai olhar!") só quando o jogador está perto do Nerd.
- Fundão: pedir faz um "psiu" (ruído raio 5 m, 6 de suspeita — o professor distraído ignora). Com a prova cheia
  ele não oferece cola; cola ruim sem questão em branco não diz mais que você errou.
- Cena: valores novos gravados nas 3 dificuldades (confiscatePhone, phoneCharges, nerdGlanceInterval).
- Testes: EditMode 19/19, PlayMode 17/17 (+8: bateria, confisco por tipo, advertência do Fácil, tipo chegando do
  professor ao confisco, Nerd sem sorteio, psiu perto/longe, cola com prova cheia).
- Falha encontrada no caminho: o 1º teste do "psiu" misturava o ruído com o resultado sorteado do pedido (cola vista
  pelo professor = alerta legítimo). Cochicho isolado em `Whisper()`; teste refeito.
- NÃO validado ainda no editor com olhos: a animação/cabeça do Nerd olhando de lado e o texto do HUD.

### Bloco 3 — economia de distrações (04/11)
- Favores da turma por partida (`DifficultySettings.favors`: Fácil 5, Normal 3, Difícil 2; `GameManager.Favors`,
  `SpendFavor()`), compartilhados por Puxa-saco, Dorminhoco e Fundão. Puxa-saco/Dorminhoco só gastam se a
  distração funcionou; o Fundão sempre cobra (a ideia dele pode virar cola). Sem favores, ninguém aceita pedido.
  HUD: "Flagrantes: [ ][ ][ ]    Favores: N" e "(favor: N/M)" nos prompts.
- `ProfessorAI.Distract` recusa durante Investigando: nenhuma distração (pedida ou em cadeia) tira o professor de
  uma investigação. O NPC avisa "(favor não gasto)".
- Bug da auditoria corrigido: a 1ª virada do Puxa-saco agora é sorteada quando a prova começa (antes usava o Normal).
- Testes: EditMode 19/19, PlayMode 22/22 (+5).

### Bloco 4 — estabilidade e validação (04/11)
- Registro de partidas (`Core/MatchLog.cs`): uma linha CSV por partida em
  `%USERPROFILE%\AppData\LocalLow\DefaultCompany\JogoFaculdade\partidas.csv` (no editor/testes: `partidas_editor.csv`)
  [desde o Bloco 14: `LocalLow\EstudioFundao\Sobrevivendo à UNIMAR\`]
  + linha "[Partida]" no log. Colunas: dificuldade, resultado, nota, flagrantes e tipos, tempo restante, conhecidas
  certas/tentadas, chutes, celular, Nerd (cópias/pego), Fundão (pedidos/cola boa/ruim), favores, eventos, Cara de Paisagem.
- NavMesh: o erro "Failed to create agent because there is no valid NavMesh" CONTINUAVA no executável (a correção
  de 27/10 só recuperava depois). Causa: o agente nascia antes dos dados do NavMesh. Agora o NavMeshAgent fica
  desligado na cena e o `ProfessorAI.Start` liga. Executável testado: 0 linhas de NavMesh no Player.log.
- Câmera sentada (validada no executável girando o mouse): problema confirmado em 2 ângulos. Sentado, a câmera
  ignora obstáculos < 1,6 m (carteiras, cadeiras, colegas) e não desce abaixo de -5°; espremida por parede, sobe até
  0,75 m para olhar por cima da cabeça.
- Avisos de API obsoleta (`FindObjectsSortMode`) removidos.
- Executável regenerado: `Builds/Windows/SobrevivendoUnimar.exe` (04/11). Validado: menu (clique), HUD novo,
  molduras, favores, evento do telefone, Nerd olhando de lado, professor patrulhando, câmera.
- Testes: EditMode 21/21, PlayMode 22/22.
- Ainda não feito: uma partida COMPLETA real em cada dificuldade (precisa de um humano jogando; o registro agora grava).

### Bloco 5 — áudio com função de jogo (04/11)
- Tudo sintetizado no próprio `SoundFX` (não há arquivos de áudio no projeto; trocar o clip de cada `Sfx` basta
  quando houver). Novos: `Passo`, `Cadeira`, `Ouviu` ("hm?"), `Coracao`. `SoundFX.PlayAt(sfx, posição)` usa 8 fontes
  3D reaproveitadas (volume/lado pela posição em relação à câmera, até 16 m).
- Passos de todos os personagens sincronizados com a animação (meio ciclo da perna); mais fortes quanto mais rápido.
  Professor: 0,8 patrulhando, 1,4 investigando/indo dar flagrante (dá pra ouvir que ele está vindo).
- "Hm?" 3D do professor quando ele ouve um ruído (junto com o aviso no HUD).
- Ruídos do jogador agora soam: cadeira ao sentar/levantar, "tec" baixinho do celular a cada ruído emitido.
- Tensão: batimento 2D em loop, só com suspeita > 30 (ou investigando), acelera com a suspeita, volume máx. 0,35,
  some na pausa e fora da partida.
- Bug corrigido: nos primeiros 6 s de jogo o professor ouvia ruídos sem avisar (`lastNoiseToast` começava em 0).
- Testes: EditMode 21/21, PlayMode 26/26 (+4: todo Sfx tem clip, passos do professor, "hm?", batimento/pausa).
- NÃO validado: a QUALIDADE dos sons (eu não consigo ouvir). Validar com fone: passos à esquerda/direita, volume
  do batimento, se o "hm?" é reconhecível. Ajustes: Inspector do SoundFX (volume, tensionVolume, maxHearDistance).

### Bloco 6 — ruído como evento (04/11)
- `Core/Noise.cs`: `Noise.Emit(pos, raio, quantidade, o que, byPlayer)` + `event Noise.Emitted`. Jogador (cadeira,
  correr, celular) e o "psiu" do Fundão emitem; professor e Puxa-saco assinam em OnEnable/OnDisable.
  Limpeza estática no início de cada Play (`RuntimeInitializeOnLoadMethod`), pois o domínio não recarrega.
  Não usei ScriptableObject channels: peso sem ganho neste tamanho de jogo.
- Puxa-saco agora OUVE: barulho do jogador dentro do raio faz ele avisar ("Hã?! Ouvi ...") e virar em 1,5 s.
  Ignora barulho de NPC e de longe.
- Testes: PlayMode 29/29 (+3: sem vazamento de ouvintes após reinício, ruído chega ao professor pelo evento,
  Puxa-saco perto/longe/NPC).

### Bloco 7 — falas com consequência e menos texto (04/11)
- "Agora EU vou ficar de olho em TODO MUNDO" (ronco) e "VIGIANDO VOCÊS" (fim da ligação) agora valem:
  `ProfessorAI.BeAlert(12 s)` → campo de visão +12° e reação ×0,6; HUD mostra "ATENTO!" (laranja).
  A janela de oportunidade passa a cobrar um preço logo depois.
- Avisos simultâneos no topo: 3 → 2 (cena e código). Texto de controles do menu atualizado (chute, bateria).
- Testes: EditMode 21/21, PlayMode 30/30 (+1).
- Executável final da noite gerado e verificado: menu, partida, pausa (Esc), reinício (R); 0 erros de NavMesh,
  0 exceções no Player.log.

### 09/11 — auditoria + correções pontuais
- Auditoria de 09/11 (nota 5,0): o chute dava pra decorar (12 perguntas, gabarito fixo, a certa aparece em verde
  ao errar); Nerd copiável sentado (1,35 m < range 1,9); celular visto raramente dá flagrante no Fácil/Normal;
  eventos demais e anunciados. Regras do projeto agora em `CLAUDE.md`.
- Configuração: SampleScene fora das Build Settings (o arquivo continua; o "Montar Fase 1" usa ele como origem);
  `com.coplaydev.unity-mcp` fixado no commit `81542a7e` (o que já estava instalado, v10.3.0) em vez de `#main`;
  "Montar Fase 1" pede confirmação antes de apagar `== PROVA IMPOSSIVEL ==` (em batch o diálogo responde sim sozinho).

### Gabarito do chute sorteado por partida (09/11)
- `QuizBank`: o chute não tem mais gabarito fixo. `Reset()` (no Awake do `QuizChallenge`, a cada carga da cena)
  sorteia a opção certa de cada pergunta; `Reset(int seed)` deixa reproduzível. Gerador próprio (`System.Random`),
  não o `UnityEngine.Random`. `NextChute` devolve uma cópia com a certa do dia em `options[0]` — o `QuizChallenge`
  não mudou e continua mostrando a certa em verde ao errar (agora isso não ensina nada para a próxima partida).
- Banco de chutes: 12 → 28 perguntas (4 opções cada). Uma partida usa no máximo 10, então não repete.
- Testes: EditMode 25/25 (+4: mesma semente = mesmo gabarito e ordem, sementes diferentes = gabaritos diferentes e
  nenhuma pergunta presa numa resposta, sem repetição, 4 opções com exatamente 1 certa); PlayMode 30/30.
- NÃO validado jogando: se as 16 perguntas novas são engraçadas e se alguma "parece" ter resposta óbvia (agora
  a "óbvia" só acerta 25%, o que pode até virar piada).

### Bloco 8 — a cola volta a ser necessária + acontecimentos de ameaça (09/11)
- Simulação de equilíbrio (`Editor/Tests/SimulacaoDeEquilibrioTests.cs`, Monte Carlo, 20 mil partidas): usa as
  contas do jogo (`ProfessorAI.SuspicionIfSeen`, `Vision.Weight`, `DifficultySettings`) e SUPOSIÇÕES de jogador
  (constantes no topo do arquivo: acerto das conhecidas por tier, "cuidado" = metade do risco do cone etc.).
  É modelo, não partida: compara configurações; os números reais vêm do partidas.csv.
  | | Sem cola antes → depois | Com cola (cuidado) antes → depois |
  |---|---|---|
  | Fácil | 74,6% → 22,1% | 100% → 100% |
  | Normal | 19,5% → 5,6% | 100% → 63,3% |
  | Difícil | 1,8% → 4,2% | 99% → 28,1% |
  Celular visto 3 s (6 m, meio do cone): suspeita 23/51/88 → 39/113/198 (Normal e Difícil = flagrante antes dos 3 s).
- Colas: Nerd só com o jogador EM PÉ (`NerdStudent.CanInteract`); cópias `nerdCopies` 3/2/2; olhadas do Nerd com
  mínimo baixo (6–20 / 2–12 / 1,5–9 s): não há mais janela segura garantida. Celular `phoneSuspicion` 50/75/85,
  `phoneCharges` 3/1/1. Fundão: `backRowCaughtChance` 0,15/0,30/0,45 (+ sempre se o professor vê; ao telefone,
  nunca) via `ProfessorAI.CatchRedHanded`; o favor só é gasto se a bagunça distraiu ou o papel foi passado
  (`BackRowStudent.Resolve(p, roll)`, testável).
- Prova: conhecidas 3/3/3, nota mínima 6/7/7, favores 5/2/2. O texto do menu agora lê tempo e nota das settings.
- Acontecimentos: 4 ameaças novas no `EventDirector` (conferência de celulares → professor ATENTO vem ao seu
  corredor e encara; colega pede silêncio → +`threatSuspicion`; professor muda a ronda pro seu lado; espirro do
  colega → ruído que ele ouve de qualquer lugar). Pesos `opportunityEventWeight`/`threatEventWeight`
  0,65/0,35 · 0,5/0,5 · 0,35/0,65; `eventInterval` 30–46 / 38–56 / 45–65 s. Sem textos "ACONTECIMENTO"/"janela
  de oportunidade" (ficam falas, sons e o professor andando); a ligação ganhou o toque 3D do celular dele.
  Cadeias antigas mantidas. Acontecimentos só começam com o professor TRANQUILO.
- Distração: regra única `ProfessorAI.CanBeDistracted` — tranquilo ou já distraído; investigando/flagrante/desafio
  nunca; desconfiado só se `distractWhileSuspicious` (falso nas 3). Pedido recusado avisa e não gasta favor.
- Cena: valores das 3 dificuldades editados direto no .unity (sem "Montar Fase 1"); teste PlayMode
  `ValoresDaCenaIguaisAosDoCodigo` garante que a cena e o código não se separam (a simulação lê o código).
- Testes: EditMode 38/38 (+13: simulação, metas, celular, sorteio ponderado, regra de distração),
  PlayMode 35/35 (+5: Nerd em pé, Fundão favor/flagrante, desconfiado não se distrai, cena = código).
- NÃO validado jogando: dá pra chegar perto do Nerd em pé (corredor entre as carteiras)? As ameaças são legíveis
  sem texto? O Normal com 1 carga de celular ficou punitivo? Executável NÃO foi regenerado.

### Bloco 9 — leitura no corpo, HUD enxuto (09/11)
- `Visual/MoodIcon.cs`: ícone NO MUNDO em cima da cabeça (canvas world space virado pra câmera) + barrinha
  opcional. Professor: "?" que cresce e esquenta enquanto ele percebe (substitui "ELE ESTÁ PERCEBENDO" do HUD),
  "?" desconfiado, "!" investigando, "!!!" flagrante, "..." ao telefone ou antes de virar, "o.o" atento.
  Mapeamento puro em `ProfessorAI.MoodFor` (testado). Nerd e Puxa-saco: "..." antes / "O_O" durante a olhada.
- Professor avisa antes de virar: mudança de rumo > 100° (ronda e `WalkTo`) → para `turnTellSeconds`, pigarro
  3D ("hm" grave, `Sfx.Ouviu` com pitch 0,62) e cabeça dando tranco; depois anda (`GoToWithTell`, `TurningSoon`).
  Investigar/flagrante não esperam (urgente).
- Dorminhoco: `SnoreWarning` antes do ronco (cabeceia, "z" → "zZz" → "zZZZZ...") por `snoreWarningSeconds`;
  o sono normal virou um "z..." baixinho para não confundir. `EventDirector.Ronco` espera o aviso.
- HUD: ficou tempo, nota/questões (o título vira "já dá! Entregue na mesa"), flagrantes e favores, + prompts,
  ajuda de controles, mensagens e balões. Saíram painel de objetivo, painel de suspeita, "olho", aviso central,
  aviso do Puxa-saco/Nerd e marcador do professor. O toast "O professor ouviu X" também saiu (fica o "hm?" + "?").
  O HUD não lê mais ProfessorAI/PlayerController/NerdStudent/SnitchStudent (teste EditMode garante);
  a cor da barra de "segurando" vem de quem chama (`HUD.HoldRisk`, `PlayerController.HoldRiskNow`).
- Ajuda visual por dificuldade (`DifficultySettings`): `turnTellSeconds` 0,9/0,6/0,35, `snoreWarningSeconds`
  4/3/2, `showSuspicionMeter` e `showStudentHints` sim/sim/não. O cone de visão continua nas 3.
- Testes: EditMode 43/43 (+5), PlayMode 38/38 (+3). Cena: 4 campos novos por dificuldade.
- NÃO validado com olhos: tamanho/legibilidade do ícone de longe, se o balão de fala cobre o ícone, se o pigarro
  é reconhecível, se no Difícil (sem barra nem dicas) a leitura continua justa. Executável NÃO regenerado.

### Bloco 10 — tutorial jogável (09/11)
- `Core/Tutorial.cs`: `TutorialFlag` (PlayerPrefs `prova_impossivel.tutorial_visto`), `TutorialStep` (Andar →
  Sentar → Responder → Celular → Nerd → Distração → Entregar), `TutorialSteps` (condições puras, avança 1 passo
  por vez) e `TutorialDirector`, que só OBSERVA as mecânicas reais (MatchLog, favores, sentado/distância do
  assento) e mostra o passo numa linha do HUD (`HUD.SetTutorial`).
- Dificuldade "Treino" em `DifficultySettings` (`GameManager.tutorial`, também na cena): `professorNeverCatches`
  (no lugar do flagrante ele diz "na prova de verdade isso era FLAGRANTE" e dá trégua), sem acontecimentos
  sorteados, 1 conhecida, 1 celular, 1 cópia do Nerd, 2 favores, 10 min (sem pressa).
- Menu: na 1ª vez, escolher qualquer dificuldade leva ao treino; botões "TUTORIAL (1 min) [T]" e "Pular tutorial".
  Depois, "Rever tutorial". No treino, T pula. Entregar antes do último passo só avisa. Concluir ou pular marca a
  flag e mostra a tela final (R volta ao menu). Treino não grava no partidas.csv.
  `GameManager.Begin(d)` continua sendo a prova direto (os testes usam); o menu chama `ChooseFromMenu`.
- Textos em `Falas.cs` (Tutorial*).
- Testes: EditMode 49/49 (+6: flag, ordem, não pula etapas, textos curtos, treino não flagra), PlayMode 42/42
  (+4: 1ª vez vai ao treino, professor não flagra, entregar cedo, treino completo pelas mecânicas reais).
  Os testes guardam e devolvem a flag do PlayerPrefs.
- NÃO validado jogando: se dá ~60 s de verdade, se o passo do Nerd (chegar perto em pé) é claro.
  Para ver de novo do zero: apagar a chave `prova_impossivel.tutorial_visto` (ou `TutorialFlag.Clear()`).

### Bloco 11 — level design: colegas bloqueiam a visão, grandões e rotas (09/11)
- `Vision.LineOfSight`: colegas agora bloqueiam, mas só ABAIXO do topo da cabeça deles (`Vision.CharacterBlocks`,
  pura e testada); quem olha e quem é olhado (até 0,6 m das pontas) não contam. Vale para professor, "quase
  olhando" e Puxa-saco. Conta: olho do professor ~1,8 m; aluno sentado comum ~1,35 m esconde o PEITO de quem está
  atrás, quase nunca a cabeça. Pontos cegos de verdade só atrás dos grandões.
- Cena (editada direto, sem "Montar Fase 1"; backup no scratchpad): PEDRÃO saiu da F2 C1 para a F3 C5 (carteira
  vazia na frente do jogador) e CAIO da F5 C3 para a F6 C3, cada um com mochila, prova e lápis; PEDRÃO, CAIO e
  LARI (F3 C6, na frente do Nerd) escalados 1,45× ("time de basquete").
- Medido com a visão real (`Tests/PlayMode/LevelDesignTests`, `RelatorioDoLayout` imprime a tabela):
  | | exposição média | Puxa-saco vê | comprimento |
  |---|---|---|---|
  | Assento (sentado) | 100% → 81% (da frente da sala 100% → 75%) | | |
  | Nerd 1 — frente, vão apertado (z 0,9) | 80% | 5/8 | 3,1 m, cola longe da ronda |
  | Nerd 3 — corredor da janela | 83% | 8/10 | 4,3 m, cola a 0,7 m de uma parada da ronda |
  | Fundão 1 — corredor central | 90% | 17/17 | 7,6 m |
  | Fundão 2 — fundo da sala, atrás do CAIO | 71% (cola: 25% da frente) | 9/21 | 10,0 m |
  Vãos livres para o jogador (cápsula 0,2): entre as fileiras 3/4 só em z ≈ 0,9; 4/5 em 2,8; 5/6 em 4,7.
- Testes: EditMode 53/53 (+4 `VisaoTests`), PlayMode 47/47 (+5 level design). Simulação de equilíbrio não modela
  cobertura: na prática o celular sentado ficou um pouco mais seguro com o professor na frente da sala.
- Visto em imagem renderizada (batch): os grandões leem bem do quadro. PROBLEMA VISUAL CONHECIDO: escalados,
  eles sentam ~25 cm acima da cadeira (o quadril sobe com a escala). Não corrigi (etapa sem visual).
- Executável NÃO regenerado. NÃO jogado: se o vão apertado (fileiras 3/4) é passável com o controle de verdade.

### Bloco 12 — consolidação para playtest (10/11)
- Sem feature nova e sem mudar equilíbrio. Números que eram fixos no código foram para `DifficultySettings` com os
  MESMOS valores (e para a cena nas 4 dificuldades): `sneezeRadius` 30, `sneezeNoiseFactor` 1,5,
  `graceCatchSuspicion` 30, `practiceCatchSuspicion` 40, `backRowDropChance` 0,35, `backRowColaChance` 0,35,
  `backRowGoodColaChance` 0,6, `nerdGlanceWarning` 1,5, `nerdGlanceDuration` 1,8, `turnTellAngle` 100;
  novo `studentHintDistance` 3,4 (o alcance do antigo aviso do Nerd).
  ÚNICA mudança de efeito: o "ATENTO" depois do ronco/ligação (`EventDirector`) usa `threatAlertSeconds`
  (era 12 fixo; agora 10 no Fácil, 12 no Normal, 15 no Difícil), como pedido.
- Fundão (`BackRowStudent.Resolve`): se a bagunça não pode funcionar (`ProfessorAI.CanBeDistractedNow` falso), ele
  nem tenta — sem "psiu", sem recarga, sem favor. O "psiu" da bagunça vem DEPOIS da distração (não sabota mais).
- Cone (`VisionConeView`): agora é uma malha de amostras (24 raios, a cada 0,5 m) testada com a mesma regra do jogo
  (`Vision.LineOfSight`, cabeça OU peito do jogador na postura atual); onde ele não vê, transparente. Teste
  `ConeMostraExatamenteOQueOProfessorVe` confere vértice a vértice (60 vistos, 9 escondidos na pose do teste).
- Ícones do Nerd/Puxa-saco só até `studentHintDistance` (`StudentBase.PlayerNear`).
- `QuizBank.Reset()` com `[RuntimeInitializeOnLoadMethod]` (padrão do Noise) + a cada partida em
  `GameManager.StartMatch`; saiu do `QuizChallenge.Awake`.
- DECISÃO DOS GRANDÕES: **B** — PEDRÃO, CAIO e LARI voltaram a 1× (posições do Bloco 11 mantidas). Visto em imagem
  renderizada: sentam na cadeira, sem flutuar nem atravessar. Consequência medida: com colegas normais, o assento e
  as rotas do Nerd ficam 100% expostos (o colega da frente esconde só o PEITO); as rotas do Fundão continuam
  diferentes (central 100% / Puxa-saco 17 de 17 x fundo 82% / 9 de 21). Rotas do Nerd diferem só em comprimento e
  proximidade da ronda. `LevelDesignTests` ajustados para essa geometria.
- Testes: EditMode 54/54 (+1), PlayMode 58/58 (+11: 4 ameaças, ronco espera o aviso, Fundão recusado, ícone de
  perto, cone = visão, gabarito por partida, botões do menu, tecla T pula o tutorial).
- Executável regenerado (10/11 00:50; Build Succeeded, 0 erros, 104 MB). Player.log ao abrir: 0 exceções, 0 erros de
  NavMesh, sem erro do shader do cone; só os avisos antigos de pós-processamento do URP removido do build.
- NÃO verificado: uma partida rodando DENTRO do executável (as teclas enviadas por fora não chegaram ao jogo; o log
  termina no carregamento); a aparência do novo cone na tela (a foto de teste não pegou o cone).

### Bloco 13 — correção dos defeitos visuais da auditoria (10/11)
- Só interface/visual; sem mexer em Vision.cs, na lógica do ProfessorAI, em números de equilíbrio ou escalas.
  Antes/depois: `Captures/AuditoriaVisual` x `Captures/AuditoriaVisual2` (pasta fora do git).
- Flagrantes do tutorial (99 caixinhas): `HUD.StrikesLabel` (pura) — até `HUD.MaxStrikeBoxes` = 5 caixinhas; acima,
  "Flagrantes: n". O texto também é cortado na largura do painel. Visto: "Flagrantes: 0" no treino.
- Balões: `HUD.KeepInSafeArea` empurra cada balão para fora das áreas do HUD (tempo, NOTA, mensagens, prompt, barra,
  ajuda, tutorial) — nunca fica embaixo de um painel. Mensagens do topo: `MoodIcon` informa ao HUD onde o ícone está
  na tela (`HUD.ReportWorldIcon`; o HUD continua sem ler o professor); se a pilha cobrir um ícone, vai para o lado
  (sem invadir tempo/NOTA, `InvadePainel`) ou desce para baixo do ícone (`HUD.ToastOffset`).
- `MoodIcon`: barrinha de suspeita ACIMA do símbolo; contorno preto grosso; "..." desenhado como "•••"
  (`MoodIcon.Glyph`, só no desenho — `Symbol` e o ProfessorAI continuam com "..."), maior (120).
- Cone (`VisionConeView`): piso de opacidade `minAlpha` = 0,2 (com 0,1 ainda sumia de longe) e recorte nos limites
  da sala (área da malha de navegação + 0,6 m; `InsideRoom`) — recorte só visual; teste do cone compara só dentro.
- Linha de controles: fundo escuro do tamanho do texto + contorno.
- Testes: EditMode 57/57 (+3 `InterfaceTests`), PlayMode 60/60 (+2 `InterfacePlayTests`: balão nunca em cima do HUD,
  mensagens desviam de um ícone).
- Visto nas capturas: tudo acima corrigido. Observações: com o ícone do Nerd grande perto da câmera, as mensagens
  descem para o meio da tela (não há espaço ao lado sem cobrir a NOTA); o balão de fala do professor ainda pode
  ficar por cima do "?" dele (não estava no escopo). Executável NÃO regenerado neste bloco.

### Bloco 14 — acabamento (10/11)
- Sem mexer em números de `DifficultySettings`, na visão (`Vision`) nem nas regras de suspeita. Linha de base antes:
  EditMode 57/57, PlayMode 60/60, git limpo.
- **Etapa 1 — limpeza.** `ProjectSettings`: companyName `EstudioFundao`, productName `Sobrevivendo à UNIMAR`.
  Dados do jogo MUDARAM de pasta: agora `%USERPROFILE%\AppData\LocalLow\EstudioFundao\Sobrevivendo à UNIMAR\`
  (partidas.csv, Player.log). PlayerPrefs (flag do tutorial, opções) no Registro: `HKCU\Software\EstudioFundao\Sobrevivendo à UNIMAR`
  (no editor: `HKCU\Software\Unity\UnityEditor\EstudioFundao\...`). Os antigos (`LocalLow\DefaultCompany\JogoFaculdade`,
  com o partidas.csv de 04/11, e `HKCU\Software\DefaultCompany\JogoFaculdade`) NÃO foram apagados nem copiados: o jogo
  só não os lê mais (a flag do tutorial não existia lá — nada a migrar).
  Timeline, Visual Scripting e Collab sem nenhuma referência (só a lista padrão de tipos do `SceneTemplateSettings`,
  que é do Unity): removidos do `manifest.json`. `Core/Flat.cs` (`Flat.Distance`) substitui as duas cópias de
  FlatDistance (`StudentBase`, `ProfessorAI`). `Falas.Reset()` com `[RuntimeInitializeOnLoadMethod]` (padrão do Noise).
- **Etapa 2 — fim de partida.** `Core/MatchSummary.cs` (puro): resultado, nota/mínima, tempo usado, certas/erradas/em
  branco (ExamSystem), flagrantes, colas por tipo (MatchLog: Celular, NerdCopias, Fundão = boa + furada), favores gastos.
  Nenhum contador novo foi preciso. `HUD.ShowMatchResult`: duas colunas ("A PROVA" / "A FICHA CRIMINAL") e botões
  JOGAR DE NOVO (R), MENU (M), SAIR. `GameManager.PlayAgain`: recarrega a cena e já começa na mesma dificuldade
  (estático `pendingPlayAgain`, zerado a cada Play); no treino volta ao menu. R/M ignorados no frame em que a prova acaba.
  Título de "Flagrado" agora é "EXPULSO!". Textos em `Falas` (seção "tela de fim de partida").
- **Etapa 3 — opções.** `Core/GameOptions.cs`: volume geral (`AudioListener.volume`), volume dos efeitos (multiplica
  `SoundFX.Play/PlayAt`), sensibilidade da câmera (multiplica a da cena, 0,25× a 3×). Padrões/limites em constantes,
  chaves `sobrevivendo_unimar.*`, valores corrigidos ao ler, carregados antes da cena. Sliders na pausa
  (`HUD.AddOption`, `UIFactory.Slider`). As mensagens do topo somem enquanto a pausa está aberta (a caixa maior as cobria).
- **Etapa 4 — ambiente.** `Audio/AudioMix.cs` (Inspector do SoundFX, campo `mix`): TODOS os volumes relativos de
  ambiente e tensão (o antigo `tensionVolume` virou `mix.tensionMax`, mesmo valor 0,35). `SoundFX`: murmúrio (loop 8 s),
  tique-taque (loop 2 s), lápis e cadeira esporádicos, três AudioSources no próprio objeto do SoundFX. Só durante a
  prova; abaixa para 20% por 1,2 s quando toca "hm?", flagrante, alerta, erro ou sinal; e cai até 60% com a tensão.
  A tensão é o batimento que já existia, agora com teto explícito. Ambiente segue só o volume geral.
- Testes: EditMode 72/72 (+15 `AcabamentoTests`), PlayMode 67/67 (+7 `AcabamentoPlayTests`).
- Capturas (teste PlayMode temporário, apagado): `Captures/AuditoriaVisual3/` 01 aprovado, 02 expulso, 03 pausa.
- Executável regenerado (10/11 12:27; Build Succeeded, 0 erros, 102 MB). Player.log novo: 0 exceções, 0 erros
  (só os avisos antigos de shaders de pós-processamento). O log termina no carregamento, como no Bloco 12.
- NÃO verificado: como o áudio SOA (testes medem volume, não ouvido); partida jogada dentro do executável.

### Bloco 15 — novas maneiras de colar (10/11)
- Motivo: o jogador achou poucas maneiras de colar. Linha de base: EditMode 72/72, PlayMode 67/67; simulação Normal
  com cola 63,3%. Playtest real até aqui: 1 partida (Difícil, reprovado 5,0; errou 2 das 3 conhecidas, 8 s por questão).
- Teclas em uso antes: WASD, Shift, E, Espaço, 1–4 (resposta), Esc, Enter/1/2/3/T (menu), R/M (fim/pausa).
  Novas (livres, só SENTADO): **Q** colinha, **B** bilhete, **F** fone, **G** garrafinha. `ColasNovasTests` confere
  que Q/B/F/G só aparecem em `PlayerCheats.cs`.
- **Orçamento** (`Core/CheatBudget.cs`, puro): `maxCheatAnswers` respostas por partida para TODAS as colas somadas.
  Celular (`PlayerController.FinishPhoneConsult`), Nerd (`NerdStudent.CanInteract/Interact`), cola boa do Fundão
  (`BackRowStudent.Resolve/GiveCola`: sem orçamento ele só faz bagunça) e as 4 novas. Bilhete e fone RESERVAM a vaga
  ao serem pedidos; se a resposta se perde, a vaga volta. Painel mostra "restam N respostas".
- **Colas novas** (`Player/PlayerCheats.cs`, criado por código no `PlayerController.Awake`; a cena não mudou):
  - Colinha (Q, segurar ~1 s): resposta numa questão EM BRANCO (não corrige errada). Se o professor está a menos de
    `colinhaDangerDistance`, ele percebe SEM ver: `ProfessorAI.CloseRangeSense` (no `Perceive`) soma
    `colinhaNearSuspicion`/s; sem ver não vira flagrante direto (teto 99), mas leva a investigar. Infração `ColinhaNaManga`.
  - Bilhete (B, mirando com a câmera no Nerd ou no Fundão): ruído `Noise` pequeno; perdido se cair fora da mesa
    (chance por metro: Nerd a 1,35 m, Fundão a 7,3 m) ou se o professor estiver perto de você/do destino (e, se ele
    te vê nessa hora, flagrante `Bilhete`). O Nerd só aceita escrevendo (mesma janela da espiada). Volta depois de
    `bilheteDelay` numa questão em branco SORTEADA (pode ser uma que você sabia). Fundão: pode voltar "não sei, bjs".
  - Fone (F): segurar para mandar (é usar o celular), resposta chega depois de `foneDelay`, segurar F de novo para ler
    (corrige errada, como o celular). Enquanto espera pode VIBRAR (`foneRingChance`): ruído forte; se ele vê, flagrante
    `CelularTocando`. F silencia. Celular confiscado = resposta perdida.
  - Garrafinha (G, segurar 1,5 s): pouco suspeita de ver, mas o gole é ruído (`garrafaNoise`). Só questão em branco.
    Infração `Garrafinha`.
- Regras por enum: `CheatRules.ItemTakenFor` — Celular e CelularTocando confiscam o celular; ColinhaNaManga leva a
  colinha; Garrafinha leva a garrafinha (`GameManager.ApplyFlagrante` → `PlayerCheats.OnFlagrante`). Falas de flagrante
  próprias por tipo (`Falas.FlagranteDe`, usada em `ProfessorAI.StartFlagrante`). Sons novos sintetizados: Papel,
  Vibra, Gole (`SoundFX`). HUD: painel "COLAS" logo abaixo do da NOTA (`HUD.SetCheats`), fora das áreas dos balões.
- Valores (Fácil / Normal / Difícil / Treino):
  orçamento 5/3/2/10 · colinha cargas 3/2/1/1, tempo 1/1/1,2/1 s, suspeita se vê 40/55/65/40, perigo a 2,5/3,5/4,5/2,5 m,
  suspeita de perto 35/50/65/35 · bilhete 3/2/2/1, volta em 6/8/10/5 s, erro por metro 4/6/8/0%, perdido a 2/2,5/3/2 m,
  ruído 5/6/7/5 (raio 4/5/6/4), mira 35/30/25/40° · fone 1/1/1/1, chega em 12/15/18/8 s, vibra 30/45/60/50%,
  toque 30/45/60/30 (raio 5/6/7/5), visto tocando 45/60/75/45 · garrafinha 3/2/2/1, 1,5 s, suspeita se vê 8/12/16/8,
  gole 10/14/18/10 (raio 4/5/6/4).
- Simulação (`SimulacaoDeEquilibrioTests`, mesmo jogador cuidadoso; agora usa primeiro as colas de questão em branco,
  depois chuta, depois corrige; o caminho antigo continua idêntico e é testado):

  | Dificuldade | Sem cola antes | Sem cola depois | Com cola antes | Com cola depois | Orçamento |
  |---|---|---|---|---|---|
  | Fácil | 22,1% | 22,1% | 100,0% | 100,0% | 5 |
  | Normal | 5,6% | 5,7% | 63,3% | **64,0%** | 3 |
  | Difícil | 4,2% | 4,3% | 28,1% | 26,6% | 2 |

  Sem orçamento as colas novas dariam 100% / 100% / 96,6%. Normal por orçamento: 1 → 19,6%, 2 → 38,0%, **3 → 64,0%**,
  4 → 90,4%. NÃO existe orçamento inteiro dentro da meta 50–60%: ficou 3 (o mais perto, 4 pontos acima). Para cair
  na faixa sem mexer nas colas antigas seria preciso outra alavanca (decisão do usuário). O teste da meta continua
  com a faixa de antes (45–70%).
- Testes: EditMode 90/90 (+16 `ColasNovasTests`, +2 na simulação), PlayMode 77/77 (+10 `ColasNovasPlayTests`, com
  teclado virtual Q/G/F e bilhete no Nerd).
- Capturas (teste PlayMode temporário, apagado): `Captures/AuditoriaVisual4/` 01 painel, 02 segurando a colinha,
  03 bilhete e fone a caminho, 04 vibrando, 05/06 orçamento esgotado + colinha confiscada (1920×1080 e 1280×720).
- Tutorial (NÃO mexido): precisaria de um passo "use uma cola sentado (Q/G)" e uma frase explicando o orçamento
  ("cada prova tem um limite de respostas coladas") e o bilhete com mira.
- NÃO feito/verificado: `partidas.csv` não registra as colas novas (feito no Bloco 15b); não há animação própria (manga, garrafa, bilhete voando); o som novo não foi ouvido; não jogado no
  executável (não regenerado neste bloco).
- Observação vista nas capturas (fora do escopo): o ícone "O_O" do Nerd fica enorme na tela porque a carteira dele é
  colada à do jogador. (Corrigido no Bloco 15b.)

### Bloco 16 — etapa 2: identidade visual "prova impressa" (10/11)
- Direção A (escolhida pelo usuário): folha creme, margem vermelha, texto datilografado, caneta azul, carimbos.
  Só interface: nenhuma regra, número de equilíbrio ou teste de equilíbrio mudou.
- Fontes em `Assets/Fonts/Resources/` (pasta Resources para carregar por código), cada uma com a licença ao lado:
  Special Elite (`SpecialElite-Regular.ttf`, Apache 2.0, `SpecialElite-LICENSE.txt`) e Caveat (`Caveat-Regular.ttf`,
  SIL Open Font License 1.1, `Caveat-OFL.txt`). Baixadas do repositório google/fonts (Special Elite) e
  googlefonts/caveat (Caveat estática). Sem negrito sintético (era o que borrava nos protótipos). Se faltar uma, o
  tema cai na fonte do sistema e avisa no log (`UITheme.UsingFallbackFont`).
- `UI/UITheme.cs` (NOVO): o tema inteiro. Cores de papel/tinta, cores de SINAL (as antigas, para mundo, balões e
  desafios), fontes, tamanhos, espaçamentos, transição (0,18 s), contraste WCAG (`Contrast`), `OnPaper` (cor de sinal
  das mensagens vira tinta legível no papel), `StampFor` (carimbo azul aprovado / vermelho reprovado-expulso) e os
  sprites gerados por código (folha, moldura, moldura dupla do carimbo, tecla).
- `UI/UIFactory.cs`: componentes de papel `Paper`, `Ink`, `PaperButton` (normal/hover/clique/desabilitado, "( )" com
  X à mão no hover), `PaperSlider`, `Stamp`, `Keycap`, `Frame`, `Line`. A família antiga (Box/Label/Button/Slider/Bar)
  continua para MoodIcon, balões e desafios A/B, que NÃO mudaram. Nenhuma cor solta (teste lê o arquivo).
- `UI/PaperButtonFeel.cs` (NOVO): hover/clique em tempo real (funciona pausado), não consome clique.
- `UI/HUD.cs`:
  - `ShowIntro` agora tem duas folhas: capa (`BuildMenuPage`: JOGAR, TUTORIAL, SAIR, "Pular tutorial" na 1ª vez) e
    folha de prova (`BuildDifficultyPage`: 3 dificuldades com tempo e nota de `DifficultySettings` via
    `DifficultyData`, frase engraçada, instruções com as colas Q/G/B/F, VOLTAR). `ShowDifficultyPage`/`ShowMenuPage`
    com proteção de mesmo frame; Esc volta. As teclas 1/2/3/Enter/T continuam no GameManager (não mexido).
  - `ShowPause`: 3 sliders de papel com valor à caneta. `ShowMatchResult`/`ShowResult`: carimbo + comentário do
    professor à caneta vermelha + colunas "A PROVA" e "A FICHA CRIMINAL" pautadas.
  - HUD: tempo, NOTA (nota à caneta, anotação "faltam X pra passar"/"já dá!", caixinhas-tecla, marca-texto amarelo
    nas que sabe), COLAS com etiqueta de tecla por linha (`SetCheats` separa "[Q] ..." com `SplitKey`), prompt,
    barra de segurar (cor meio transparente), ajuda com teclas em caneta azul (`KeysInPen`), tutorial com carimbo
    TREINO, mensagens do topo em tiras de papel (importantes com moldura vermelha). Balões e ícones: iguais.
- `MatchSummary`: linha nova "Colas de bolso (Q/G/B/F)" na ficha (usos de colinha/garrafinha/bilhete/fone do
  `MatchLog`). `Falas`: textos novos do menu, dificuldade, instruções, pausa e HUD.
- Testes: `IdentidadeVisualTests` (EditMode: fontes carregadas e licenças, acentos ã ç é õ ê etc. em 18/24/34 px,
  contraste ≥ 4,5 de todas as tintas no papel, mensagens com cor de sinal legíveis, cores do mundo intactas, sem
  `new Color(` no HUD/UIFactory, `SplitKey`, `KeysInPen`, flagrantes no teto, dados da dificuldade);
  `IdentidadeVisualPlayTests` (capa/folha/voltar + mesmo frame, dados reais, sem sobreposição; HUD sem
  sobreposição e dentro da tela; hover em tempo real com o jogo pausado). Ajustados: ficha com 6 linhas
  (`AcabamentoTests`), painel das colas por etiquetas (`ColasNovasPlayTests`).
- Capturas (teste PlayMode temporário, apagado): `Captures/AuditoriaVisual5/` 01–14 em 1920×1080 e 1280×720
  (menu 1ª vez/normal/hover, dificuldade, HUD início/flagrante+segurando, pausa, reprovado, aprovado, sufoco,
  expulso, passo do tutorial, mensagens do topo, fim do treino).
- Visto nas capturas e corrigido: escurecimento atrás do menu fraco (0,62 em espaço linear quase não escurece →
  0,82), texto pequeno claro demais (a fonte é "gasta": tamanho mínimo 23 e tinta secundária mais escura), números
  das teclas pequenos, moldura grossa no carimbo pequeno, texto do carimbo alto, entrelinha das instruções.
- Defeitos que sobraram (vistos): em 1280×720 o texto de 23 px vira ~15 px (legível, mas pequeno); folha da
  dificuldade com espaço vazio acima do VOLTAR; o "( )" do botão TUTORIAL é menor que o dos outros (fonte menor);
  o X do hover encosta nos parênteses.
- NÃO verificado: hover/clique com mouse de verdade (testado chamando os eventos), Esc na folha com teclado real
  (o teste chama `ShowMenuPage`), som do clique, outras proporções de tela (só 16:9).
- Testes: EditMode 120/120 (+17), PlayMode 97/97 (+3). Build ok (103 MB); Player.log da abertura (menu): 0 exceções,
  sem aviso de fonte reserva.

### Bloco 16 — identidade visual dos menus (10/11) — etapa 1
- 3 direções (prova impressa, caderno pautado, lousa com giz) aplicadas por cima do menu atual por um teste PlayMode
  temporário (apagado): `Captures/MenuOpcoes/` 0_atual, A, B, C. Nenhum código do jogo mudou para isso.
- `Assets/Fonts/` não existe: os protótipos usam fontes do Windows (Courier New, Ink Free, Segoe Print, Impact) com
  negrito sintético, que borra em tamanho pequeno. Esperando a escolha para a etapa 2.

### Bloco 15b — colas no CSV e ícone do Nerd (10/11)
- Nenhum número de `DifficultySettings` mudou.
- `partidas.csv`: 12 colunas novas NO FIM (as 22 antigas não mudaram de lugar): `colinha_/garrafa_/bilhete_/fone_` +
  `usos;certas;erradas`. Usos = cola feita (segurou até o fim, jogou o bilhete, mandou a mensagem) ou interrompida
  por flagrante; certas = virou resposta; erradas = não virou (flagrante no meio, bilhete interceptado/recusado/errou
  a mesa/lista do mercado, celular confiscado antes da resposta). usos − certas − erradas = esperando quando a prova
  acabou. Um `partidas.csv` antigo ganha o cabeçalho novo na primeira partida gravada (`MatchLog.UpgradeHeader`:
  troca só a 1ª linha, e só se o cabeçalho antigo for o começo do novo); as partidas antigas ficam intactas, com as
  colunas novas vazias.
- Ícone de humor (`MoodIcon`): teto de tamanho na tela `MaxScreenFraction = 0.11` (fração da altura), calculado pela
  profundidade na direção da câmera. Sentado ao lado do Nerd o "O_O" ocupava ~37% da altura; de 4,8 m para lá, de
  frente, o tamanho é o mesmo de antes (~10%). Teste PlayMode: com a câmera virada para o Nerd, o ícone fica no
  teto e não encosta nos painéis NOTA e COLAS.
- Simulação: perfil ARROJADO acrescentado (age sem esperar "fora da vista", cola até o orçamento acabar mesmo já
  passando, demora a silenciar o celular, usa a colinha com o professor perto, joga o bilhete sem conferir) e a coluna
  flagrantes/partida. Tabela de suspeita "se ele vê a cola inteira".
  | Dificuldade | Perfil | Sem cola | Aprovação | Expulsão | Flagrantes/partida | Colas/partida | Orçamento |
  |---|---|---|---|---|---|---|---|
  | Fácil | cuidadoso | 22,1% | 100% | 0,0% | 0,00 | 3,3 | 5 |
  | Fácil | arrojado | 22,4% | 100% | 0,0% | 0,00 | 6,3 | 5 |
  | Normal | cuidadoso | 5,7% | 64,0% | 0,0% | 0,00 | 3,5 | 3 |
  | Normal | arrojado | 5,7% | 64,5% | 0,0% | 0,00 | 4,0 | 3 |
  | Difícil | cuidadoso | 4,3% | 26,6% | 0,0% | 0,17 | 3,1 | 2 |
  | Difícil | arrojado | 4,0% | 19,9% | 0,2% | 0,52 | 3,6 | 2 |
  Suspeita se ele vê a cola inteira (100 = flagrante, 65+ = investiga): Normal colinha 23, garrafinha 8, bilhete 11,
  fone 39 por vez (celular 3 s: 113). Fácil 7/3/3/12. Difícil 57/18/26/74.
  LEITURA: no modelo, as colas novas NUNCA dão flagrante no Fácil e no Normal, nem para o arrojado; com o orçamento
  gasto nelas, ele nem chega ao celular/Nerd. Ser arrojado não custa nada fora do Difícil. Limite do modelo: cada
  cola é avaliada sozinha (no jogo a suspeita acumula e decai entre ações), então o risco real é maior que o
  simulado — confirmar com o `partidas.csv`.
- Achado: flagrante com o bilhete no ar (`OnFlagrante` zera `throwLeft`) não devolvia a vaga reservada do orçamento —
  o `Land()` nunca rodava e o jogador perdia 1 resposta até o fim da prova. CORRIGIDO logo depois (commit à parte):
  `OnFlagrante` devolve a vaga (`Budget.Cancel`) e esquece o alvo. Teste PlayMode
  `FlagranteComBilheteNoArDevolveAVagaDoOrcamento` (falha sem a correção, passa com ela). Com advertência (1º
  flagrante no Fácil) o arremesso não é cortado e o bilhete segue normal, como antes. Testes: EditMode 103/103,
  PlayMode 80/80; executável regenerado, Player.log com 0 exceções (menu).
- Testes: EditMode 103/103, PlayMode 79/79. Executável regenerado; Player.log com 0 exceções (só menu aberto ~25 s;
  partida não jogada no executável).
- Arquivos: MatchLog, PlayerCheats (contagem), MoodIcon, testes (MatchLogTests, IconeDoNerdTests novo,
  SimulacaoDeEquilibrioTests, ColasNovasPlayTests, ConsolidacaoTests).

### Bloco 15c parte 2 — auditoria do risco real das colas (10/11)
- SÓ medição: nenhum código de jogo, número ou regra mudou. Teste novo `Assets/Tests/PlayMode/AuditoriaRiscoColasTests.cs`
  (14 testes): cena real, professor posto onde VÊ o jogador, cola feita pelo teclado virtual (bilhete por `ThrowAt`),
  depois o professor age sozinho por até 10 s. Tabela completa em `Captures/AuditoriaRisco/auditoria.md` (gerada a cada
  rodada; fora do git). Uma amostra por caso: há aleatoriedade (ronda, Puxa-saco), os números variam um pouco.
- Isolamento só no teste: sem acontecimentos nem virada sorteada do Puxa-saco, Nerd sem olhada, bilhete sem errar a
  mesa, resposta do fone em 1,5 s. O Puxa-saco continua OUVINDO (a vibração faz ele virar e dedurar: "Dedurado").
  Investigando, o professor anda livre (segurar parado fazia a investigação "chegar" na hora, a 10 m).
- Resultado (Normal / Difícil; susp. no fim da cola; F = flagrante; CP = investigou → Cara de Paisagem):
  | Cola | a) perto vendo | b) longe no cone | c) vira no meio | d) desconfiado, 2× |
  |---|---|---|---|---|
  | Colinha | 39 / 85 CP | 18 / 43 | 33 / 78 CP | 79 CP / 83 CP (Difícil: 1 carga) |
  | Garrafinha | 25 / 48 | 7 / 14 | 17 / 33 | 55 / 69 CP |
  | Bilhete | F Bilhete / F Bilhete (interceptado) | 9 / 20 | F / F | 49 / 60 (2º bilhete ainda voltando) |
  | Fone | 64 CP / 79 CP | 63 / F Celular | 74 CP / 81 CP | 68 CP / F Celular (1 carga) |
  | Celular (ref.) | 74 CP / 81 CP | 85 CP / F | 74 CP / 82 CP | 87 CP / F |
  | Nerd (ref.) | 78 CP / F EspiarNerd | 37 / 58 | 36 / 62 | F ForaDoLugar / F ForaDoLugar |
  Fone tocando (3 s): de costas 63 / 99 CP; olhando 99 CP / 99 CP, todos com "Dedurado" (Puxa-saco). Numa rodada com o
  professor mantido olhando, o toque visto deu F CelularTocando nas duas dificuldades.
- Divergências com o pedido do Bloco 15 (NÃO corrigidas):
  1. Colinha: sobe aos poucos como celular/Nerd (`PlayerCheats.UpdateRisk` → taxa `colinhaSuspicion`;
     `ProfessorAI.Perceive` só dá flagrante com suspeita 100 vendo; `CloseRangeSense` trava em 99). Nunca chegou a
     100: no Normal, vista inteira a 2,5 m = 39. Precisaria: em `Perceive`, se vê (`Notice` = 1 e visibilidade acima
     de um mínimo) com `CurrentInfracao == ColinhaNaManga`, `StartFlagrante` direto.
  2. Bilhete: flagrante direto só pela interceptação (`PlayerCheats.Land`, professor a menos de `bilheteLostDistance`
     e vendo). Ouvir o papel (`ProfessorAI.HearNoise`) grava `Infracao.Outra`, e a investigação (`UpdateInvestigando`)
     só dá flagrante se ele VIR uma cola em andamento; senão vai para Cara de Paisagem (falha = `CaraDeCulpado`).
     Nunca "acha o bilhete". Precisaria: `NoiseInfo` levar a Infracao; na chegada da investigação por bilhete, uma
     chance de achar → flagrante `Infracao.Bilhete`.
  3. Fone tocando: o toque é ruído (`PlayerCheats.Ring` → `MakeNoise`, Infracao `Outra`); flagrante
     `CelularTocando` só se ele VIR o celular tocando até a suspeita 100. Ouvindo de costas, investiga e cai na Cara
     de Paisagem. Além disso o "Dedurado" do Puxa-saco (`ProfessorAI.Alert`) sobrescreve o tipo registrado.
     Precisaria: o mesmo caminho do bilhete (achar o celular na investigação → `CelularTocando`).
  4. Garrafinha: nunca deu flagrante direto ✓. Mas além do barulho ela tem suspeita por VER (`garrafaSuspicion`
     12/16 por s): vista 2× desconfiado chega a 55–69 e pode levar à Cara de Paisagem. Se "só o barulho" for literal,
     `garrafaSuspicion` = 0.
- Ajustes propostos (NÃO aplicados):
  | Campo (DifficultySettings) | Atual (Normal / Difícil) | Proposto | Efeito esperado no risco |
  |---|---|---|---|
  | `colinhaInstantCatch` (novo, bool) | — (não existe) | true / true | colinha vista = flagrante direto (a, c: de 39/33 → F) |
  | `colinhaCatchVisibility` (novo) | — | 0,5 / 0,35 | só conta se ele vê bem (perto/centro do cone); de longe (b) continua escapando |
  | `bilheteFindChance` (novo) | — (0) | 0,5 / 0,7 | investigação pelo papel acha o bilhete → F `Bilhete` metade/70% das vezes |
  | `bilheteNoiseRadius` | 5 / 6 | 6 / 7 | ouve o papel de mais longe (b continua fora: 9,6 / 10,9 m) |
  | `foneRingFindChance` (novo) | — (0) | 0,6 / 0,8 | celular que tocou e foi investigado → F `CelularTocando` |
  | `garrafaSuspicion` | 12 / 16 | 0 / 0 (só se "só barulho" for literal) | garrafinha vista deixa de somar; risco = só o gole |
  Os 3 "chance de achar" precisam de código novo (Infracao no ruído + decisão na chegada da investigação).
  Reavaliar a simulação e o `partidas.csv` depois: a colinha instantânea sobe bem o risco no Normal.
- Testes: EditMode 103/103; PlayMode completo 94/94 (80 de antes + 14 da auditoria).

### Trailer — roteiro e prévia (13/11)
- Fora do jogo: nenhuma linha de código do jogo, cena ou equilíbrio mudou. Tudo em `Trailer/` (fora de `Assets/`).
- `Trailer/ROTEIRO_TRAILER.md`: roteiro de ~65 s em 18 planos (abertura, o plano, o perigo, o desfecho,
  encerramento), trilha por trecho e passos para o trailer final.
- `Trailer/capturas/`: 13 planos 1920×1080 da cena real (closes posados com AI pausada, balões do próprio HUD,
  HUD real e flagrante real via `ApplyFlagrante`). Teste PlayMode temporário em batch, apagado depois.
- `Trailer/previa/`: animatic em HTML (canvas + Web Audio). Trilha e efeitos sintetizados (sem faixa de
  terceiros). `montar_previa.ps1` embute as capturas e gera `Trailer_Previa.html`; o botão Gravar exporta .webm.
- Visto: folha de contato com 20 quadros renderizada no Chrome headless. Não visto: o som (só ouvindo).

### Trailer v2 — planos em movimento, 3 cortes (13/11)
- Fora do jogo: nenhum código do jogo, cena, regra ou equilíbrio mudou. Novos: `Editor/Trailer/` (asmdef próprio, só
  editor) e `Trailer/`. Piadas removidas do trailer: a do boleto (folha da abertura v1; no quadro da cena é tirada só na
  memória durante a captura) e a da pizza (plano 10 da v1). `Trailer/capturas/` (fotos da v1) apagada.
- Ferramenta: menu *Prova Impossível > Trailer > Gerar planos* ou batch `-executeMethod
  ProvaImpossivel.EditorTools.TrailerShots.Batch [-trailerOnly id,id]`. Recarrega a cena por plano, começa no Fácil,
  aciona as mecânicas reais (teclado virtual para Q/G/F/2; `ThrowAt`, `GlanceSoon`, `SnoreWarning`, `ForceWatch`,
  `WalkTo`, `Alert`, `CatchRedHanded`, `ShowMatchResult`) e grava PNG 1920×1080 30 fps + .mp4 (MediaEncoder do Unity,
  sem ffmpeg) em `Trailer/clips/`. Mundo com Volume temporário (vinheta, DoF, cor quente); a interface é desenhada
  por uma câmera própria sem pós e composta por cima (o URP limpa a imagem numa câmera base com Clear Nothing).
- Edição: `Trailer/previa/trailer_template.html` (3 cortes: mestre 47,5 s, 30 s 16:9, 30 s 9:16). Exporta .webm
  (VP9+Opus) e .mp4 (H.264) quadro a quadro com WebCodecs; `montar_previa.ps1 -Exportar` roda tudo via
  `servidor.mjs` + Chrome sem janela. Trilha sintetizada (provisória), com ducking nas falas.
- Limites: o jogo não desenha o bilhete voando nem mira (o plano mostra o arremesso real); 5,9 APROVADO não existe nas
  regras (a tela real mostra 6,0/6,0 no Fácil); cone de visão discreto no estado tranquilo.
- Correção (13/11, pedido do usuário: "ele fica mexendo muito a cabeça de um lado pro outro"): no jogo a cabeça do
  jogador sentado segue a câmera (`PlayerController.UpdateLook`); nos planos a câmera fica na frente dele e o
  enquadramento (`Rig.Rel`) usa a direção da cabeça, então os dois se perseguiam. `Rig.Capture` agora fixa
  `headYaw` do jogador em `Rig.PlayerHeadYaw` (0) a cada quadro, entre o Update e o LateUpdate. Só a ferramenta do
  trailer mudou; o jogo não. Sem teste automatizado novo (só editor, depende do Play): conferido nos quadros.

### Trailer — correções e novo final (13/11)
- Só `Trailer/` mudou (template, `montar_previa.ps1`, `servidor.mjs`, roteiro). Jogo, cena, regras, equilíbrio e a
  ferramenta de captura (`Editor/Trailer/`) não foram tocados; os planos em `clips/` são os mesmos.
- Celular: o jogo não tem modelo do aparelho (`CharacterVisual.phone` só muda a pose dos braços e da cabeça) e, no
  plano `02_celular`, as mãos ficam atrás da carteira. O aparelho agora é desenhado pelo trailer (`phone()`): recorte de
  papel com contorno à caneta, tela acesa, "Pesquisando no celular..." (texto do HUD), notificação, tecla 2 e uma seta
  até as mãos do jogador.
- Cartões Q/G/B/F: a largura reservava 16 px a menos do que a posição do texto e nenhuma margem direita.
  `cardLayout()` mede o texto mais longo dos quatro, dá a mesma caixa a todos e encolhe a fonte (mínimo 34 px) se não
  couber na margem segura.
- Caixas piscando: (1) o plano `13_cone` tem balões do próprio jogo gravados na imagem, pulando de um canto para o
  outro com a câmera em movimento; o corte agora usa só o trecho em que eles ficam na faixa de cima (quadros 30 a 80),
  reenquadrado sem essa faixa e em câmera lenta com mistura de quadros. (2) Entre "dá pra arredondar?" e "Dá. Para
  baixo." a faixa da legenda sumia por 2 quadros e voltava com outro tamanho; agora é uma faixa só, da largura da
  maior fala. A correção de verdade do (1) é regravar o plano com `ShowBubbles = false` na ferramenta do Unity.
- Final novo (do 0:35 em diante no mestre; do 0:21 nos de 30 s): cortes na batida, subida, um instante de preto e
  silêncio, impacto com tremor e clarão em cada carimbo, título batido à máquina, tema completo na capa.
- Som: a prévia toca a mesma trilha do arquivo (sintetizada de uma vez, `renderAudio`), com limitador em −1 dBFS.
  Continua provisória (não existe `Trailer/audio/`).
- Checagem automática: `montar_previa.ps1 -Verificar` (ou `node Trailer/previa/servidor.mjs --verificar`) avança o
  tempo quadro a quadro e confere, nos 3 cortes: cada peça aparece uma vez e pelo tempo planejado, nenhuma caixa de
  fala pisca ou se sobrepõe, texto dentro da caixa (12 px) e da margem segura, balão do jogo entrando/saindo no plano,
  pico e volume do som, música abaixando em toda fala. Capturas e relatórios em `Trailer/verificacao/atual/`.
- Visto em captura: celular, os 4 cartões, quadro final e os últimos 15 s (a cada 0,1 s) nos 3 cortes. Não visto:
  o som de ouvido, o ritmo dos cortes em movimento e a prévia tocando ao vivo no navegador.

## Operação Steam (23/11) — auditoria, direção e blocos S0…
Documentos novos na raiz: `AUDITORIA_STEAM.md` (Etapa 1), `DIRECAO_CRIATIVA.md` (Etapa 2), `ROADMAP_STEAM.md`
(Etapa 6), `CREDITOS.md`. O usuário autorizou seguir os blocos sem pedir aprovação a cada um (23/11). Sem commits:
o usuário faz.

### S0 — higiene de produção (23/11)
- Teste novo `ValoresNaCenaTests` (EditMode): abre a cena da Fase 1 em modo aditivo (fecha sem salvar) e compara
  campo a campo as 4 dificuldades do `GameManager` gravado na cena com as do código. **Passou**: a suspeita da
  auditoria (cena salva antes do Bloco 15 com valores velhos) não se confirmou. A Unity usa o valor do código para
  campos que não estão na cena. O teste fica como trava: se alguém mexer num número só no Inspector, ele avisa.
  Ao mudar um número de equilíbrio daqui em diante, mudar no código **e** conferir que a cena não tem um valor
  diferente gravado (o teste aponta).
- `.gitignore`: `Assets/InitTestScene*` (cena temporária que o Test Runner deixa para trás).
- `CREDITOS.md`: licença de cada asset de terceiros. Quaternius (CC0), Special Elite (Apache 2.0) e Caveat (OFL)
  confirmadas. **Pendente:** o pacote `Assets/school` não tem licença nem origem registradas. Também registrado
  o risco do nome "UNIMAR" (universidade real) numa versão comercial.
- Não feito (depende do usuário): push/merge do branch, origem do pacote `school`, decisão sobre o nome.

### S1 — kit de playtest (23/11)
- `ROTEIRO_PLAYTEST.md` (novo): como aplicar (sem explicar nada, pensar em voz alta), a sessão (tutorial → 3 provas no
  Normal → 1 no Difícil), as 3 perguntas pós-partida e que coluna do CSV responde cada pergunta de design.
- `partidas.csv`: 6 colunas novas **no fim** (as 34 antigas não andaram; o cabeçalho de arquivos antigos é atualizado
  pelo `UpgradeHeader` de sempre): `investigacoes` (vezes que o professor partiu para investigar),
  `suspeita_max` (0–100), `primeiro_flagrante_s` (segundo da prova; vazio se não houve), `partida_da_sessao`
  (1 = a primeira desde que o jogo abriu: curva de aprendizado), `orcamento` e `versao` (comparar versões).
- Código: `MatchLog` (campos, `Suspeita()`, `Flagrante(kind, elapsed)`, `PartidaDaSessao` zerado a cada Play),
  `ProfessorAI.Enter(Investigando)` conta, `ProfessorAI.Perceive` registra a suspeita, `GameManager.ApplyFlagrante`
  passa o segundo da prova.
- Testes: EditMode `MatchLogTests` (2 novos + contagem de colunas 34 → 40), PlayMode `OperacaoSteamPlayTests` (novo:
  investigação e suspeita máxima vão para o registro; segundo do 1º flagrante). 8/8 e 2/2.
- Como validar: jogar uma prova e abrir o `partidas.csv`: a linha nova tem 40 colunas e as 6 últimas preenchidas.

### S2 — núcleo justo: risco no lugar do dado (23/11)
**Hipótese documentada** (números mudaram sem playtest, como o roadmap prevê; confirmar com o `partidas.csv`):
- *Problema:* com 3 conhecidas + orçamento 3 (Normal) a nota garantida era 6 para mínimo 7: o jogador sem nenhum erro
  reprovava ~1 em 3 por causa do chute (25%). No Difícil (3 + 2 = 5), ~2 em 3.
- *Mudança:* **orçamento cobre o piso**. Normal `maxCheatAnswers` 3 → **4**, Difícil 2 → **4** (Fácil 5, sem
  mudança). Conhecidas continuam 3 nas três (o Difícil chegou a ir para 2 + 5, mas `knownQuestions` está gravado na
  cena e o roadmap não mexe no `.unity` por fora; ficou 3 + 4). O Difícil passa a apertar o **professor**, não o dado.
- *Esperado:* a reprovação vem de flagrantes (erro do jogador). Meta de gente de verdade no 1º contato: Normal 40–60%.
  Se o playtest der muito acima disso, o próximo ajuste é de percepção (colinha/celular), não do orçamento.

Regras novas (decisões da auditoria 15c):
1. **Colinha vista bem = flagrante na hora** (`colinhaInstantCatch`, `colinhaCatchVisibility` 0,9 / 0,7 / 0,6 para
   Fácil / Normal / Difícil). Precisa: ele ter percebido (tempo de reação completo), visibilidade acima do mínimo
   (peso de `Vision.Weight`: perto e no centro do olhar) e não estar distraído. Regra pura `CheatRules.ColinhaCaughtNow`,
   aplicada em `ProfessorAI.Perceive` depois da trégua. O Difícil começou em 0,5 (o proposto na 15c), mas a auditoria
   mostrou flagrante até a 10,9 m (quase qualquer ponto do cone): subiu para 0,6.
2. **A investigação acha provas.** O barulho agora carrega a `Infracao` (`NoiseInfo.kind`, `Noise.Emit(..., kind)`,
   `PlayerController.MakeNoise(..., kind)`): papel do bilhete = `Bilhete`, celular vibrando = `CelularTocando`. Ao
   ouvir, o professor guarda a prova (`ProfessorAI.Evidence`, vale `evidenceMemorySeconds` = 20 s). Se ele investigar,
   chegar com você no lugar e não te vir colando, sorteia `bilheteFindChance` (0,3 / 0,5 / 0,7) ou `foneRingFindChance`
   (0,4 / 0,6 / 0,8): achou = flagrante do tipo certo ("bilhete achado na sua carteira" / "o celular que vibrou estava
   com você"); não achou = Cara de Paisagem, como antes. A prova vale uma vez só.
3. **O "Dedurado" não apaga o que ele ouviu:** a prova fica num campo separado do `lastInfracao`.
4. **Garrafinha sem mudança** (decisão registrada): continua somando suspeita quando vista (beber de olho no
   professor é suspeito) e o gole **não** deixa prova (`LeavesEvidence(Garrafinha)` = falso: água é desculpa plausível).
5. **Bilhete responde uma questão que você NÃO sabe** (`ExamSystem.RandomBlankPreferUnknown`; antes podia cair numa
   que você faria sozinho e a cola era gasta à toa). Só cai numa conhecida se não sobrar nenhuma desconhecida em branco.

Simulação (`SimulacaoDeEquilibrioTests`): o modelo aprendeu as duas regras (colinha vista na distância de referência =
flagrante quando a dificuldade pega de lá; bilhete ouvido + investigação = chance de achar). Aprovação, cuidadoso /
arrojado: Fácil 100 / 100%, **Normal 90 / 90%**, **Difícil ~65 / ~46%** (antes: Normal 64%, Difícil 28–39%).
- Limite conhecido do modelo: no Normal ele não produz flagrantes nem para o arrojado (a visibilidade de referência,
  6 m a meio cone, fica abaixo do 0,7 da colinha, e ele não acumula suspeita entre ações). Por isso a meta dos testes
  é "o cuidadoso passa" e não "humanos passam X%": essa resposta vem do playtest.
- Testes que mudaram de meta, com motivo no próprio teste: `MetaDoNormal_…` (45–70% → 85–97%: o teto antigo era o
  dado) e `OrcamentoECargasDiminuemComADificuldade` (o Difícil não tem orçamento menor que o Normal; agora confere que
  ele pega a colinha de mais longe e acha mais provas). Novos: `OrcamentoCobreONotaMinima`, `NoDificilArriscarSemOlharCustaCaro`.

Testes:
- EditMode: `ColasNovasTests` +3 (`ColinhaVistaBemDePertoEFlagranteNaHora`, `SoBilheteECelularTocandoDeixamProvaParaAchar`,
  `BilhetePrefereQuestaoQueVoceNaoSabe`) e os 2 da simulação acima. Suíte: 139/139.
- PlayMode: `OperacaoSteamPlayTests` +4 (achou o bilhete → flagrante Bilhete; não achou → Cara de Paisagem; Dedurado
  não apaga a prova; prova velha expira), `AuditoriaRiscoColasTests.S2_ColinhaVistaDePertoEFlagranteNaHora` (teclado
  virtual, professor a ~2 m: flagrante ColinhaNaManga). Suíte completa antes do ajuste do Difícil: 110/110; os 7 novos/
  alterados depois: 7/7.
- Auditoria de risco atualizada (`Captures/AuditoriaRisco/auditoria.md`): colinha a) perto vendo → **F** no Normal e no
  Difícil (antes 39/85 sem flagrante); de longe no Normal continua escapando (18); bilhete visto perto → F; celular
  tocando no Difícil → F CelularTocando.

Como validar jogando (Normal):
- Com o professor perto e olhando para você, segure **Q**: flagrante "colinha na manga". Com ele longe ou de lado,
  a colinha passa.
- Jogue um bilhete (**B**) com o professor de costas mas perto (ele ouve o papel): se ele vier investigar e você não
  estiver colando, às vezes ele **acha o bilhete** (metade das vezes no Normal).
- Passe na prova colando 4 vezes sem ser pego: tem que dar 7,0 sem depender de chute.

### S3 — tutorial completo e mais curto (23/11)
- Passos agora: **Responder → Celular → Colas de bolso → Nerd → Distração → Entregar** (antes: Andar → Sentar →
  Responder → Celular → Nerd → Distração → Entregar). Os passos Andar e Sentar saíram: o treino começa sentado,
  com o professor falando, e andar se aprende no caminho até o Nerd ("Levante (Espaço), ande (WASD) até o Nerd...").
- Passo novo **Colas de bolso**: cumpre com qualquer uma de Q/G/B/F (`MatchLog.ColinhaUsos + GarrafaUsos +
  BilheteUsos + FoneUsos`). Ao entrar nele, um aviso importante explica o **orçamento de colas** (que nenhum passo
  explicava). O resumo do fim do treino cita "colou do bolso".
- A prova de verdade avisa no começo quantas colas a turma cobre ("A turma cobre 4 colas. O resto: chute e reza.").
- Código: `Core/Tutorial.cs` (enum, `TutorialProgress.pocketCheats`, `Order`, aviso no `ShowStep`), `Core/Falas.cs`
  (textos; saíram `TutorialAndar`/`TutorialSentar`), `GameManager.Begin` (aviso do orçamento).
- Não mudou: o que o treino ainda não ensina é a **Cara de Paisagem** (ela precisa de uma investigação; forçar uma no
  treino fica para o S13, quando o minijogo for redesenhado).
- Testes: EditMode `TutorialTests` (ordem nova, percorre os 6 passos, não pula etapa) e PlayMode `TutorialPlayTests`
  (treino completo pelas mecânicas reais, agora com o bilhete no passo das colas de bolso). 139/139 e 4/4.
- Como validar jogando: menu → "Rever tutorial [T]". Conferir que começa sentado com o professor falando, que o
  passo das colas aparece depois do celular com o aviso do orçamento, e que qualquer uma de Q/G/B/F avança.

### S4 — o professor estrela (23/11)
- **Sobrancelhas que atuam** (`Visual/ExpressiveBrows.cs`, novo; enfeite `Accessory.Sobrancelhas` = 1024): duas
  sobrancelhas grossas (primitivas, cor quase preta) presas na cabeça, por cima das pintadas do modelo, que mudam com
  `CharacterVisual.expression`: **Desconfiado** = uma aperta o olho e a outra sobe (e balança); **Bravo** = V sobre os
  olhos; **Assustado** (ao telefone) = sobem, aflitas; Nervoso/Feliz/Dormindo têm a sua. Pose = regra pura
  `ExpressiveBrows.PoseFor`. O modelo perdeu as expressões do boneco de primitivas; agora o professor voltou a ter rosto.
- **O maior da sala:** escala do professor 1,06 → **1,15** (asset `ModelosPersonagens.asset` e `CharacterModelSetup.Cast`,
  os dois iguais; o teste confere).
- **Momento do flagrante:** ao começar o flagrante ele diz **"a-HÁ!"** (`Sfx.Aha`, voz sintetizada) e a câmera do jogo
  **vira e fecha nele** por 2,2 s (`ThirdPersonCamera.Focus`, zoom 0,62 do FOV, volta sozinha; o mouse não mexe nela
  enquanto isso, e o jogador já está travado no flagrante). Quando o flagrante é aplicado, um **carimbo "FLAGRANTE!"**
  (ou "ADVERTÊNCIA" no 1º do Fácil) bate no meio da tela: entra grande, assenta, some em ~1,6 s (`UI/FlagranteStamp.cs`,
  novo; mesmo carimbo do menu/tela final, com um papel atrás).
- **"hmpf"** (`Sfx.Hmpf`) quando ele desiste: alarme falso e Cara de Paisagem bem-sucedida.
- Vozes: sintetizadas no padrão do `SoundFX` (`Voice(...)`), **provisórias** até existir voz gravada.
- Visto em captura (teste temporário em batch, depois movido para fora do projeto): rosto nas 4 expressões, câmera do
  jogo durante o flagrante (dá para ler o "bravo" com o zoom) e o carimbo na tela. Ajustes feitos pelas capturas: as
  sobrancelhas começaram na testa (subiram para dentro do cabelo no "desconfiado") e desceram para cima dos olhos; o
  carimbo sumia em cima da lousa e ganhou o papel atrás.
- **Limite:** pela câmera normal (professor lá na frente, ~10 m), o rosto ainda é pequeno demais para ler; de longe
  quem informa continua sendo o ícone ("?", "!", "!!!") e a barrinha. O rosto aparece quando ele chega perto, no
  flagrante e na Cara de Paisagem.
- Testes: EditMode `ProfessorEstrelaTests` (novo: poses legíveis; professor ≥ 1,08× o maior aluno, com sobrancelhas,
  asset = código). PlayMode `OperacaoSteamPlayTests`: `S4_SobrancelhasDoProfessorAtuam` (mede as pontas das
  sobrancelhas no mundo: bravo = ponta de dentro mais baixa; assustado = sobem; desconfiado = assimétrico) e
  `S4_FlagranteViraACameraEBateOCarimbo` ("a-HÁ!" tocou, câmera virou e fechou, carimbo apareceu e sumiu, câmera voltou).
  `AudioFuncionalTests.TodoSomTemClip` cobre os 2 sons novos.
- Como validar jogando: deixe o professor te pegar no celular (segure 2 com ele olhando): a câmera vira para ele, ele
  grita "a-HÁ!", vem bravo (sobrancelhas em V) e o carimbo bate na tela. Com fone: o "a-HÁ!" e o "hmpf" soam como
  voz ou como apito? (são sintetizados).

### S5 — trilha adaptativa (23/11) — PROVISÓRIA
- `Audio/MusicDirector.cs` (novo), criado pelo `SoundFX.Awake` no mesmo objeto (a cena não muda). Três camadas de 4
  compassos (92 BPM, ré menor), que começam juntas no relógio de áudio (`PlayScheduled`) e só mudam de volume:
  - **Calma:** baixo em pizzicato "na ponta dos pés" + tique no contratempo. Sempre na prova; no menu e na pausa, a metade.
  - **Desconfiança:** acordes em trêmulo de cordas. Sobe com a suspeita (20% → 60%) e vai a 80% ou mais com ele Desconfiado.
    Ao telefone/distraído, cai (a música avisa que é a sua janela).
  - **Perseguição:** tambor grave em toda batida + chimbal + baixo dobrado. Só investigando ou no flagrante.
  - Estingers: **trombone triste "uá-uá-uá-uáaa"** no flagrante (as camadas abaixam enquanto ele toca) e um **arpejo
    de alívio** quando você é aprovado.
  - Regra pura `MusicDirector.Targets(playing, estado, suspeita, nível do menu)`.
- **Volume da música** nas opções da pausa (`GameOptions.MusicVolume`, padrão 80%, salvo em PlayerPrefs). A pausa ganhou
  uma linha (caixa 760 → 832 px; os botões desceram 72 px).
- Mixagem: máximo 0,22 × volume da música, abaixo dos sons de jogo (passos e "hm?" são informação).
- **Provisória:** sintetizada em código, como o resto do áudio. **Não foi ouvida** (o agente não tem como ouvir): o que
  está validado é a lógica (qual camada toca quando, sincronia, volume). Dívida registrada: trocar por trilha composta.
- Testes: EditMode `TrilhaTests` (novo, 4: camadas por estado, suspeita sobe aos poucos, volume salvo e limitado).
  PlayMode `OperacaoSteamPlayTests` +3 (investigação faz o tambor entrar; flagrante toca o trombone e abaixa as camadas;
  volume 0 silencia) e `AcabamentoPlayTests` (pausa com 4 sliders, o 3º é a música; 7 linhas sem se sobrepor).
  145/145 e 22/22.
- Como validar jogando (com fone): a música de fundo é agradável ou irrita depois de 3 partidas? Dá para perceber,
  **sem olhar**, que ele começou a investigar (entra o tambor)? O trombone do flagrante é engraçado? O slider
  "Volume da música" na pausa funciona?

### S6 — título de estilo, dica, recorde e conquistas (23/11)
- `Core/Records.cs` (novo), regras puras + gravação em PlayerPrefs (prefixo `sobrevivendo_unimar.`):
  - **Título de estilo** (só para quem passou), do mais raro para o mais comum: ALUNO EXEMPLAR (?!) (sem colar nada),
    GÊNIO DO CRIME (10 sem flagrante), MÁGICO DO BOLSO (3+ colas de bolso, sem celular/Nerd, sem flagrante), CARA DE PAU
    (2+ flagrantes), SOBREVIVENTE DO 7,0 (no sufoco ou raspando), PARASITA DO NERD, DEPENDENTE DO GOOGLE, AMIGO DO FUNDÃO,
    NINJA DA PROVA (sem flagrante), COLADOR PROFISSIONAL. Aparece como um 2º carimbo, azul, no canto da tela final.
  - **Dica** (só para quem não passou): pelo tipo de flagrante que mais aconteceu (`MatchLog.FlagranteMaisComum`, novo
    campo `MatchSummary.flagranteMaisComum`), ou, sem flagrante, pelo que sobrou ("sobraram 3 em branco. A turma cobria
    4 colas: o 7 estava ali."). Textos em `Falas.DicaFlagrante` (todo tipo de `Infracao` tem dica; o teste confere).
  - **Recorde** = melhor nota de aprovação por dificuldade, no rodapé ("Recorde: 8,0 (NOVO!)").
  - **8 conquistas locais** (`enum Conquista`, os nomes servem de id para a Steam depois): Passei!, Liso como sabão,
    Sobrevivente da UNIMAR (passar no Difícil), No grito, Mágico do bolso (as 4 colas de bolso numa partida), Ficha
    suja (expulso), Lista do mercado (cola furada do Fundão), Gênio do crime (10). Conquista nova aparece no rodapé;
    cada uma só uma vez.
- Tela final: caixa 800 → 860 px, linha da dica à caneta azul (-646), rodapé (-694), botões desceram para -740.
  `GameManager.End` chama `Records.Process` e passa o resultado (`ResultExtras`) ao `HUD.ShowMatchResult` (parâmetro
  opcional; o trailer continua chamando sem ele).
- Visto em captura (teste temporário, movido para fora): aprovado com o carimbo "NINJA DA PROVA" e o recorde; reprovado
  com a dica do celular. Nada sobreposto.
- Observação: os recordes e conquistas do **editor** (testes) ficam nas PlayerPrefs do editor; o executável usa as dele.
- Testes: EditMode `RecordesTests` (novo, 5: título, dica, recorde, conquistas, gravação sem repetir conquista). PlayMode
  `OperacaoSteamPlayTests.S6_AprovadoGanhaTituloDeEstiloERecorde` e o layout da tela final em `AcabamentoPlayTests`
  (agora com a dica e o rodapé). 150/150 e 19/19.
- Como validar jogando: passe numa prova e veja o carimbo azul do título; reprove de propósito e leia a dica (faz
  sentido com o que aconteceu?); passe de novo com nota maior e confira o "(NOVO!)".

### S7 — controle (gamepad) e ponto único de entrada (23/11)
- `Core/Controls.cs` (novo): enum `Acao` + `Controls.Pressed/Held/Move/Look`. Cada ação vale pelo **teclado** (as mesmas
  teclas de sempre) **ou pelo controle**, com a mesma semântica de antes (`wasPressedThisFrame`/`isPressed`), então as
  proteções de input duplicado continuam. Nenhum script de jogo lê mais `Keyboard.current`/`Mouse.current` direto
  (GameManager, HUD, PlayerController, PlayerCheats, PlayerInteraction, ThirdPersonCamera, QuizChallenge,
  FacePokerChallenge passaram a usar o `Controls`).
- Mapa do controle (layout Xbox; PlayStation/Steam Deck na mesma posição): analógico esquerdo anda (devagar se pouco
  inclinado), direito gira a câmera (`ThirdPersonCamera.stickSpeed`), L3 corre. **X** responde, **Y** celular (segure),
  **B** levanta, **A** interage / Cara de Paisagem. **LB** colinha, **RB** garrafinha, **LT** fone, **RT** bilhete.
  **Start** pausa. Na pergunta: X, Y, A, B = alternativas 1–4. Menu: X Fácil, A Normal, Y Difícil, Select tutorial,
  B volta. Fim: Y joga de novo, Select volta ao menu.
- **A ajuda na tela troca sozinha** para o dispositivo usado por último (`Controls.UsingGamepad`, `Controls.Tag/Label`):
  linha de ajuda sentado/em pé, painel das colas (`[LB] Colinha ×2`), "[A] ..." da interação, alternativas da pergunta
  ("X)  ..."), ajuda da pergunta e da Cara de Paisagem. As instruções do menu ganharam uma linha do controle.
- **Não feito** (fica para depois): remapear teclas (o `Controls` é o lugar certo para isso), ícones gráficos de
  botões (hoje são letras), textos fixos do tutorial e dos menus que citam teclas do teclado ("Aperte 1...", "[Esc]"
  nos botões), vibração. Os botões de papel dos menus continuam sendo de mouse; com o controle, os atalhos acima fazem
  o mesmo papel.
- Testes: EditMode `ControlesTests` (novo: toda ação tem tecla e botão; teclado = teclas de sempre; ações da prova não
  dividem tecla nem botão; 4 alternativas distintas) e `ColasNovasTests.TeclasNovasNaoConflitamComAsExistentes`
  (agora as teclas Q/B/F/G só podem aparecer no `Controls.cs`). PlayMode `ControlePlayTests` (novo, controle virtual:
  A no menu começa o Normal; X abre a pergunta e a ajuda mostra "X, Y, A ou B"; Y responde; painel mostra "[LB]";
  Start pausa e despausa; B levanta; analógico anda). Suítes completas: **EditMode 154/154, PlayMode 122/122** (todos
  os testes antigos de teclado virtual continuam passando).
- Observação: a 1ª rodada completa foi interrompida pelo Claude Code por falta de memória no sistema; o Unity em batch
  continuou sozinho e terminou (122/122). Para não pesar, rodar as suítes uma de cada vez.
- Como validar jogando: ligue um controle (Xbox/PS/Steam Deck) e jogue uma prova inteira sem tocar no teclado. A ajuda
  na tela deve trocar para X/Y/LB... ao usar o controle e voltar para 1/2/Q... ao usar o teclado.

### S8 — opções de vídeo (23/11) — parcial
- Na pausa: **Tela** (Cheia/Janela), **Sincronia (VSync)** (Sim/Não) e **Gráficos** (Leve/…/Bonito; só aparece se o
  projeto tiver mais de um nível de qualidade para a plataforma — hoje o Windows só inclui "PC", então ela fica
  escondida). Réguas de posições inteiras, no mesmo estilo das outras. A caixa da pausa cresceu (832 → 950 px) e as
  linhas ficaram a cada 64 px.
- `GameOptions`: `Fullscreen`, `VSync`, `Quality` (PlayerPrefs), `ClampQuality`/`QualityLabel` (puras) e `ApplyVideo`
  (roda ao abrir o jogo e a cada mudança). **Só aplica no executável**: no editor, mexer em qualidade/vsync alteraria o
  `ProjectSettings` (os testes rodam no editor), então lá as opções só são guardadas. Sem vsync, teto de 144 FPS.
- Visto em captura: a pausa com as 7 linhas, sem sobreposição (antes de esconder "Gráficos").
- **Não feito** (fica no roadmap): resolução, tamanho do texto da HUD, legendas para os sinais sonoros, modo daltônico.
- Testes: EditMode `OpcoesDeVideoTests` (novo: faixa e nomes da qualidade; opções salvas; no editor o ProjectSettings
  não muda). PlayMode `AcabamentoPlayTests` (pausa com 6 ou 7 réguas e 9 ou 10 linhas sem se sobrepor). 23/23 e 7/7.
- Como validar jogando (no executável): Esc → Tela: Janela (a janela aparece) → Cheia. VSync: Não → o jogo não deve
  ficar travado em 60 FPS. Fechar e abrir de novo: as escolhas continuam.

### S12 — desempenho medido (23/11) — só medição
- PlayMode `DesempenhoTests` (novo): prova no Normal, 2 s de aquecimento + 10 s medindo. Grava
  `Captures/Desempenho/desempenho.md` (fora do git). Só falha se o quadro médio passar de 100 ms.
- Resultado nesta máquina (RTX 2060, Ryzen 7 2700X; batch, janela 640×480, sem vsync): **3,6 ms por quadro em média
  (~278 FPS), p95 4,7 ms, pior 12,4 ms**. Scripts: Update 0,21 ms, LateUpdate 1,21 ms (é onde ficam as poses dos 14
  modelos 3D com IK por código: a parte mais cara do código do jogo, mas pequena).
- **Não responde** à meta da auditoria (≥ 60 FPS em 1080p num vídeo integrado): a janela do batch é pequena e a placa é
  dedicada. Para isso, rodar o executável em 1080p num notebook comum (o `Player.log` não mede FPS; dá para usar o
  overlay de FPS da Steam/NVIDIA/AMD).
- Correção na auditoria: são 14 personagens com modelo (não ~30 como estimado em T12).

### S9 — sala com identidade (23/11)
- Contexto: o objetivo real do usuário é **apresentar o jogo ao professor** ("Steam" era jeito de dizer "quero o jogo
  muito bom"). A sala é a primeira coisa que quem assiste vê.
- `Visual/Cenografia.cs` (novo), criado pelo `GameManager.Awake`. Tudo por código (a cena `.unity` não muda) e **tudo
  sem colisor**: NavMesh, visão do professor e câmera continuam iguais (teste confere).
  - **Fim de tarde:** sol a 17° de elevação, alaranjado (1; 0,76; 0,52), entrando pelas janelas e desenhando faixas no
    chão; luzes do teto a 72%; ambiente mais quente; céu de pôr do sol (cópia do material do céu, com tom alaranjado).
  - **Lá fora:** prédios do campus (reitoria, biblioteca, cantina), árvores e calçada, visíveis pelas janelas.
  - **Ventiladores de teto** girando (2, no corredor do meio).
  - **Ar-condicionado pingando num balde azul** (parede esquerda; uma gota a cada 1,4 s).
  - **8 cartazes** com humor de faculdade (`Falas.Cartazes`): churrasco do DA, "proibido comer (exceto pastel)",
    monitoria na sala 404, achados e perdidos, Wi-Fi, trancamento "prazo: ontem", "vende-se calculadora", "NÃO ABRA:
    provas de 2009" no armário. Papel colorido, título datilografado em vermelho, texto à caneta azul, fita crepe.
  - **A estante de metal do fundo (parecia uma escada) virou armário de aço** com caixas de provas em cima. Só a
    aparência: os renderers da estante foram desligados e o colisor dela continua.
  - **Projetor** 28% menor, encolhido em direção ao teto (tapava a lousa). **Lousa com letra de giz** (Caveat), giz e
    apagador no parapeito.
- Visto em captura (teste temporário, movido para fora): câmera do jogo, frente, fundo, parede esquerda, mural, ar-
  condicionado, armário, ventilador, janela. Ajuste feito pelas capturas: o céu azul de meio-dia pela janela não
  combinava com a luz → céu alaranjado + campus lá fora.
- Testes: PlayMode `OperacaoSteamPlayTests` +3 (cenografia sem nenhum colisor e estante só escondida; sol baixo e
  alaranjado, ventilador girando; o raio de visão do professor é o mesmo com e sem a cenografia). Também passaram
  `LevelDesignTests`, `EquilibrioNaCenaTests` e `LeituraNoMundoPlayTests` (24/24). EditMode 157/157.
- Como validar jogando: olhe em volta sentado (cartazes, ventilador, balde pingando, pôr do sol pela janela). Confira
  se a luz de fim de tarde não deixou nada escuro demais para ler (professor, Nerd, ícones).

### Fechamento do dia (23/11)
- Suítes completas no fim, rodadas uma de cada vez: **EditMode 157/157, PlayMode 123/123**.
- Versão 0.1.0 → **0.2.0** (`ProjectSettings` → `bundleVersion`; vai na coluna `versao` do `partidas.csv`).
- Executável regenerado (0 erros, 115 MB) e aberto por 20 s: chega à renderização, 0 exceções no `Player.log` (só os
  avisos de sempre do URP sobre shaders de profundidade de campo fora do build).
- A cena `.unity` não foi alterada. Nada commitado.

### Trailer v3 — mesmo roteiro, jogo novo (23/11)
- Pedido do usuário: refazer o trailer "com o mesmo roteiro mas no novo modelo do jogo". Jogo, cena, regras e equilíbrio
  não mudaram: só a ferramenta de captura (`Editor/Trailer/TrailerShotsPlanos.cs`, só editor, fora do build), o
  `Trailer/previa/trailer_template.html` e o `Trailer/ROTEIRO_TRAILER.md`.
- Os 20 planos foram regravados na cena atual (personagens Quaternius, objetos na mão, sobrancelhas do professor, sala
  S9). A v2.1 (planos e vídeos de 13/11) foi movida para `C:\Users\rafae\JogoFaculdade_trailer_v1\`.
- Ajustes de câmera por causa dos modelos novos: `01_prof_anda` (câmera no corredor; um aluno tapava o professor),
  `02_celular` (recua para pegar rosto + aparelho), `11_puxasaco` (câmera do lado para onde ela se vira: antes era a
  nuca), `12_prof_sinto` (professor 1,15× com cabelo alto: não corta a cabeça), `04_fundao` e `13_cone` sem os balões do
  jogo (saíam cortados).
- `14_flagrante`: a captura desenha a malha do corpo um quadro atrás dos enfeites presos ao osso (o render manual
  acontece antes do skinning do quadro). Com o professor andando, o bigode e as sobrancelhas "descolavam" do rosto.
  **No jogo isso não acontece** (o render normal vem depois do skinning). No plano, 0,1 s depois do flagrante (o "!!!"
  já apareceu) a IA e o NavMeshAgent param e ele vira de frente para o jogador, bravo.
- Montagem: o celular desenhado por cima (`phone()`) saiu, porque o jogo agora mostra o aparelho na mão. Garrafinha e
  fone começam mais cedo no corte (0,15 s e 0,05 s), porque o objeto só aparece no primeiro segundo do plano.
- Checagem automática (`montar_previa.ps1 -Verificar`): OK nos 3 cortes. Exportados `Trailer/export/trailer_mestre`,
  `trailer_c30` e `trailer_v30` (.mp4 e .webm; os 6 abrem e têm áudio). Conferidos em quadros: os 20 planos crus e as
  capturas da verificação (celular, cartões, tela final, capa). Não visto/ouvido: o vídeo tocando ao vivo e o som
  (trilha sintetizada, a mesma da v2.1).
- Sem teste automatizado novo: é ferramenta de editor que depende de Play e render (mesmo critério do trailer v2).
- Detalhe que ficou: a tela final do plano `16_aprovado` mostra "Recorde: 0,0" (o resumo é montado direto, sem passar
  pelos recordes).

### Trailer v3.1 — bugs do trailer e gag final novo (23/11)
- Pedido do usuário (depois de ver o v3): "o professor anda todo bugado, algumas coisas atravessando a mão do boneco,
  garrafinha meio bugada" e trocar o final do Nerd por "eu, o aluno, tentando colar, falando que vou levar DP de novo".
- **Captura** (`Editor/Trailer/TrailerShots.cs`): o quadro agora é desenhado por um gancho no fim do `PostLateUpdate`
  (PlayerLoop), no mesmo quadro do jogo e depois da deformação das malhas. Antes era desenhado na volta da corrotina, e o
  corpo ficava um quadro atrás do bigode, das sobrancelhas e dos objetos da mão (eles "descolavam" com o movimento).
  O gancho é posto em `Rig.Fresh` e tirado em `Rig.Teardown`; se não rodar, a captura volta ao jeito antigo e avisa.
- **No jogo** (`Visual/CharacterModelSkin.cs`, só visual, nenhuma regra muda):
  - Garrafinha: a cabeça inclina para trás ANTES de posicionar a garrafa (antes o queixo subia para dentro da tampa); a
    mão pega perto da tampa (no meio, o alvo ficava além do braço); o punho aponta os dedos em volta da garrafa e a
    palma fica virada para ela (só girar o punho deixava a palma ~50° torta, os dedos entravam na garrafa e ela era
    empurrada ~10 cm para longe da mão); dedos só encostados (6° em vez de 25°); na subida a garrafa sai de pé e deita
    aos poucos (antes aparecia deitada, atravessada na frente da mão ainda na carteira). Resultado medido: tampa a 3 cm
    da boca (antes ~9 cm) e palma alinhada (0°).
  - Fone: o antebraço vai mais para a frente que para dentro (o polegar entrava no queixo).
  - Caminhada (modelos Humanoid): o passo do pacote cruza um pé na frente do outro e as pernas do professor pareciam
    trançar. Cada coxa gira para fora o mínimo para o pé ficar a 6 cm (× escala) do seu lado do quadril
    (`CharacterModelSkin.MinFootOut`).
- **Gag final novo** (`18_dp_final`, substitui `18_nerd_final`): você suando, lendo a colinha na manga (Q real); em 1 s
  vira a cabeça e encara a câmera. Legenda VOCÊ: "Relaxa... no máximo eu levo DP de novo." **[NOVA]**, trombone triste
  sintetizado (`fiasco`) em 47,1 s e preto em 48,3 s. O mestre passou de 47,5 s para 48,5 s; os cortes de 30 s não mudam.
- Testes novos (PlayMode, `ModelosPersonagensPlayTests`): `GarrafinhaSaiDePeEDeitaAosPoucos`, `FoneNaoEnfiaAMaoNoRosto`,
  `ProfessorAndandoNaoCruzaAsPernas`; `GarrafinhaVaiAteABoca` ficou mais exigente (tampa a menos de 6 cm da boca, era
  15 cm). Os 17 testes da classe passam.
- Validar jogando: segurar G (garrafinha) e F (fone) e olhar de perto (câmera livre); ver o professor andando de frente.

### Entrega ao professor — versão 1.0.0 (24/11)
- Escopo do dia: só polimento seguro. Versão 0.2.0 → **1.0.0** (`bundleVersion`).
- Capa do menu ganhou um rodapé "v1.0.0 · Estúdio Fundão" no canto de baixo à esquerda (`Falas.MenuRodape`, `HUD.BuildMenuPage`,
  objeto `Rodape`). Nada de jogo mudou.
- Testes: EditMode `AcabamentoTests.VersaoDeEntregaNoRodapeDoMenu`; PlayMode `MenuTemCapaEFolhaDeDificuldadeComDadosReais`
  agora confere que rodapé e carimbo não cobrem os botões nem saem da folha, e que o rodapé mostra a versão.
- Suítes completas, uma de cada vez: **EditMode 158/158, PlayMode 129/129**.
- Executável regenerado (0 erros, 115 MB) e empacotado em `Builds/Entrega/SobrevivendoUnimar_v1.0.0.zip` (41 MB; sem a
  pasta `_BackUpThisFolder_ButDontShipItWithYourGame`; com `Licencas/` = licenças das fontes + `CREDITOS.md`). Aberto da
  pasta de entrega por 20 s: 0 exceções no `Player.log`.
- `README.md` novo na raiz (para quem abrir o repositório): como jogar, controles, o que tem no jogo, parte técnica, créditos.
- Validar jogando: abrir o .zip extraído e conferir o rodapé na capa do menu.

### Modo entrega — versão 1.0.1 (24/11, noite)
Polimento em 3 blocos pequenos, cada um testado e commitado antes do próximo. Nenhuma cena, prefab ou número de
dificuldade foi alterado.

**Bloco 1 — trilha** (`Audio/MusicDirector.cs`, `Audio/SoundFX.cs`). A trilha adaptativa existente foi mantida (mesmas 3
camadas sincronizadas, mesmas fontes; nada de sistema novo):
- Camada CALMA refeita: colchão sustentado (os acordes Rém–Rém–Si♭–Lá, ataque de 0,7 s e saída de 0,9 s por cima do
  próximo; as amostras dão a volta no fim do loop, então a emenda não tem degrau). O baixo "na ponta dos pés" ficou
  espaçado (1 e 3 de cada compasso) e o tique virou um por compasso. Normalizada com pico 0,8.
- A trilha abaixa para 45% enquanto toca um som importante ("hm?", alerta, flagrante, erro, sinal — a mesma lista que já
  abaixava o ambiente, `SoundFX.ImportantSoundNow`) e volta devagar. O estinger não abaixa.
- Prova encerrada (`Phase.Ended`): as camadas saem em ~1,7 s; só o estinger de aprovação toca na tela de resultado.
  Menu e pausa continuam como antes (só a calma, a 50%).
- **Não ouvido por um humano.** Os testes garantem pico, emenda, fontes únicas e os volumes; o gosto precisa de fone.

**Bloco 2 — Cara de Paisagem no tutorial** (`Core/Tutorial.cs`, `Core/Falas.cs`, `Challenges/FacePokerChallenge.cs`,
`AI/ProfessorAI.cs`):
- Passo novo `TutorialStep.CaraDePaisagem` entre "gastar um favor" e "entregar": 2,5 s depois do texto, o professor
  diz uma fala de encarar e a barra abre de verdade em **modo treino** (`Begin(..., practice: true)`). Acertando ou
  errando, o passo conclui; a mensagem final explica o que aconteceria na prova (funciona: ele desiste e a suspeita
  cai, mas só salva quando ele não te VIU colando; tremeu: ele desconfia mais e pode virar flagrante).
- Correção de bug: se a prova acabasse no meio do desafio (tempo, tutorial pulado), o painel ficava na tela por baixo do
  resultado com o jogador travado. Agora `FacePokerChallenge.Cancel()` fecha sem resultado.
- O professor não abre uma segunda Cara de Paisagem com uma já ativa.

**Bloco 3 — HUD** (`UI/HUD.cs`, `Challenges/FacePokerChallenge.cs`, `Core/GameManager.cs`, `Students/SnitchStudent.cs`):
- "FALTA 1 MINUTO!", "30 SEGUNDOS!" e "O PUXA-SACO TE DEDUROU!" agora são mensagens importantes (não podiam ser
  descartadas por uma comum).
- Mensagem cinza (só informação/recusa) com a pilha cheia fica de fora em vez de expulsar outra (`HUD.IsInformative`).
- "[T] pular tutorial" saiu do cinza #999 (contraste 2,6:1 no papel) para a tinta suave do tema.
- Painel da Cara de Paisagem no visual "prova impressa" (papel, moldura vermelha, fonte de máquina, tinta), no lugar
  do painel escuro antigo. Mesma posição, mesma barra.

**Testes novos:** EditMode `TrilhaTests` (+2), `AcabamentoTests` (+2), `TutorialTests` (atualizados);
PlayMode `EntregaPlayTests` (6: trilha sem estouro/estalo/fontes duplicadas, "hm?" abaixa e volta, fim tira a trilha,
painel fecha no fim da prova, painel de papel com textos que cabem, cinza não expulsa) e `TutorialPlayTests` (o treino
completo agora passa pela Cara de Paisagem e confere que tremer não sobe a suspeita).

**Validar jogando (ninguém ouviu/viu ainda):** com fone, ouvir a calma por 1 minuto (agradável? loop perceptível?),
deixar o professor dizer "hm?" (a música abaixa?), terminar uma prova (a música sai?); no tutorial, o passo da Cara de
Paisagem (o painel de papel ficou bonito e legível? ele tampa o texto do passo — o texto aparece 2,5 s antes).

### Polimento visual — versão 1.0.2 (24/11, noite)
Referência de segurança: commit `11d0868` (tag local `v1.0.1-ref`) e build em `Builds/Versao_1.0.1`. Um commit por bloco.
Diagnóstico com captura real: planos `diag_*` na ferramenta do trailer (`-trailerOnly diag_prof_lado,...`; nunca entram
no trailer) e `Editor/DiagAnimacao.cs` (mede a passada dos clipes).

**Bloco 1 — caminhada** (`Visual/CharacterModelSkin.cs`, `Visual/CharacterModelSet.cs`, `Resources/Personagens/
ModelosPersonagens.asset`, `AI/ProfessorAI.cs`). Medido: Walk 1,33 s ≈ 1,3 m/s de passada; Run 0,8 s ≈ 2,7 m/s; o Walk
do pacote anda "de passarela" (o pé esquerdo vai até 16 cm para o lado direito).
- Causas: corrida tocava o Walk acelerado até o limite (pé deslizando ~30%); ritmo `velocidade/1,3` ignorava o tamanho
  (professor 1,15×); folga dos pés de 6 cm não vencia a passarela; o professor cruzava os braços em qualquer quase
  parada (andava com a mão na barriga); o corpo dele seguia `agent.velocity`, que oscila nos desvios.
- Correções: clipe Run ligado (`CharacterModelSet.run`), misturado só acima de 2,8 m/s (`RunBlend`: jogador correndo;
  o professor perseguindo a 2,6 m/s continua andando); ritmo = velocidade / (passada × tamanho) (`PlaybackRate`); folga
  dos pés 9 cm; tronco 4° à frente andando; professor cruza os braços só depois de 0,6 s parado, vira pela
  `desiredVelocity` com `turnRate` 8 (era 6). IA, NavMesh e velocidades de deslocamento: sem mudança.
- Pendente: o calcanhar sobe alto atrás em alguns passos — é do clipe do pacote.

**Bloco 2 — indicador do Nerd** (`Visual/MoodIcon.cs`, `Students/NerdStudent.cs`): o texto "O_O" (fonte de sistema,
vermelho com contorno) virou um olho desenhado por código — amêndoa creme com contorno a tinta, íris vermelha de
carimbo com pupila e reflexo, sobrancelha desconfiada; a pupila olha para o lado do jogador na tela; entra com "pop" e
sai encolhendo. O símbolo continua "O_O" por dentro (`MoodIcon.EyeKey`): a lógica do Nerd e os testes não mudaram.

**Bloco 3 — caixas de texto** (`UI/HUD.cs`, `Challenges/QuizBank.cs`). Auditoria automática (`EntregaPlayTests`) com
TODAS as perguntas, alternativas e falas do jogo, os passos do tutorial e prompt/ajuda longos:
- Bug achado e corrigido: em fala curta ("Hm.", "Silêncio!", "...") o nome "PROF. TAKANERVOSO" saía do balão
  (o balão tinha 160 px; o nome, 360 fixos). Agora o balão tem pelo menos a largura do nome.
- Prompt de interação, texto do tutorial e barra de ajuda tinham altura/largura fixas: agora se ajustam ao texto
  (mínimo = o tamanho de antes). A fonte não diminuiu. O prompt só mede de novo quando o texto muda.
- Perguntas e alternativas: todas já cabiam (a auditoria passa sem mudança no painel da prova).
- O projeto usa uGUI `Text` (não TextMeshPro); com `CanvasScaler` de referência 1920×1080, resoluções menores escalam
  o layout inteiro.

**Bloco 4 — lousa** (`Visual/Cenografia.cs`, `Core/Falas.cs`): a lousa ganhou a piada de tiozão "O QUE O ZERO DISSE PARA
O OITO? / QUE CINTO BONITO!" em letra de giz (Caveat), resposta em giz amarelo maior e sublinhada, assinatura
"— Prof. T. (essa é boa, hein?)" e o desenho de um 0 e um 8 com olhinhos, o 8 de cinto com fivela. Feito em tempo de
execução no canvas que já existe na cena (a cena não mudou). Textos em `Falas.LousaPergunta/Resposta/Assinatura`.

**Validar jogando:** andar e correr com o jogador (vira bem? desliza?); ver o professor andando de frente e de lado; o
olho do Nerd; um balão de fala curta do professor; ler a lousa sentado na carteira.

### Caminhada — versões 1.0.3 e 1.0.4 (24/11, tarde)
O usuário testou a 1.0.2 e a caminhada continuava torta; depois, na 1.0.3, "o pé fica virando de lado". As duas causas
estão **nos clipes do pacote** (também num modelo limpo, sem nada do jogo), medidas por `Editor/DiagAnimacao.Pelve` e
`DiagAnimacao.Pes`:
- **1.0.3 — pelve** (commit `663fb80`): Walk/Run/Idle_Neutral giram a pelve ~26,7° (constante; Idle −0,3°, repouso 0°).
  `CharacterModelSkin.AlignPelvis` mede o giro a cada quadro, gira o quadril de volta e devolve o giro ao abdômen (o
  tronco fica onde a animação pôs). Medido depois: pelve 0,0°, ombros ~2° (o modelo limpo tem 1,9°).
- **1.0.4 — pés**: com a pelve alinhada, as pontas dos pés ficavam viradas para dentro (andando: esquerdo ~35°,
  direito ~20°; parado: esquerdo ~15°). `StraightenFoot` endireita cada pé para 6° para fora (`FootOutAngle`): metade
  do giro na canela (em torno dela mesma; o tornozelo não sai do lugar) e o resto no pé. A direção é medida pela linha
  tornozelo → ponta (`Foot.X_end`) vista de cima: um eixo do osso dizia "reto" no impulso, mas o pé também tomba de lado
  e a ponta estava 25° para fora (o pé de trás "de lado" na visão de trás). Pé quase na vertical (`FootFlatness`
  < 0,4) não é corrigido.
- Medido no jogo (professor andando, 498 quadros): ponta dos pés sempre em ±6,0°. Captura de trás, de lado e do
  jogador conferida.
- Testes: EditMode `GiroDaPelveMedidoPelaLinhaDasCoxas`, `PeVoltaParaAFrenteUmPoucoParaFora`; PlayMode
  `AndandoAPelveFicaAlinhadaComOCorpo`, `AndandoOsPesApontamParaAFrente` (andando e parado, erro < 4°).
- Continua do clipe: o calcanhar sobe alto atrás em alguns passos.
- **Validar jogando:** olhar o professor andando de trás e de frente (pontas dos pés para a frente) e correr com o
  jogador vendo os pés de lado.
- Usuário (24/11): "os pés melhoraram bastante".

### Mãos na prova — versão 1.0.5 (24/11, noite)
Pedido: "a mão dos bonecos enquanto fazem a prova estão meio que atravessando a mesa e não parece que estão fazendo a
prova". Captura `diag_prova` (ferramenta do trailer: figurante escrevendo de lado, de frente e de cima): as mãos
paravam na borda da carteira com os dedos dentro do tampo, longe da folha, e o lápis ficava solto na mesa.
- Causas: a pose sentada/escrevendo era só de direções fixas (não sabia onde está o tampo); e o modelo desce para o
  assento DEPOIS das poses dos braços, levando as mãos junto para dentro da mesa.
- Correção (`Visual/CharacterModelSkin.cs`): ao sentar, cada personagem acha a folha da SUA carteira (objeto "Prova"
  da cena, o mais perto na frente) e o lápis ao lado dela ("Lapis"). Com a folha achada, IK de braço põe o centro da
  palma 3 cm acima do tampo (`PalmAboveDesk`), descontando o deslocamento do assento (`SeatShift`): a mão esquerda
  segura o canto da folha e a direita escreve na folha com um vaivém pequeno; escrevendo, o tronco debruça 9°
  (`WriteLean`). O lápis da cena é estático (static batching: movê-lo não muda o desenho), então ele é escondido e
  uma cópia vai para a mão, com a ponta na folha e os dedos em pinça; parou de escrever (celular, cola, garrafinha,
  dormir, braços cruzados...), o lápis volta para a mesa. Sem folha na frente, fica a pose antiga. A cena não mudou.
- Teste PlayMode `FazendoAProvaAsMaosFicamEmCimaDaCarteiraEOLapisNaFolha`: todo figurante escrevendo — as duas
  palmas entre 1 e 15 cm acima do tampo, a mão que escreve a menos de 20 cm da folha, lápis na mão com a ponta na
  folha e o lápis da mesa escondido (sem lápis duplicado).
- Conferido em captura: figurante (de lado, de frente e de cima) e o Nerd.
- **Validar jogando:** olhar os colegas fazendo a prova (lápis na mão, mão na folha) e, sentado, abrir a prova (E)
  e ver a mão do jogador.
- Usuário (24/11): "ficou legal, mas eles estão tipo com a mão aberta [...] quero a mão fechada e quero também que os
  personagens cada um mexa de uma forma, não todos iguais" → versão 1.0.6 abaixo. (A suíte completa da 1.0.5 foi
  interrompida para seguir com este pedido; rodou inteira na 1.0.6.)

### Mão fechada e cada um escreve do seu jeito — versão 1.0.6 (24/11, noite)
- **Causa da mão aberta (bug antigo, valia para tudo que fecha a mão):** os modelos do pacote não têm os mesmos nomes
  nos ossos dos dedos. No terno a base do dedo é `Index2` (juntas 2-3-4); nos outros modelos é `Index1` (1-2-3) e
  `Index2` já é o meio do dedo. O `Curl` começava sempre no "2": nesses modelos a base do dedo nunca dobrava (medido:
  base 16°, meio 92° com 90° pedidos) e a mão ficava aberta, em garra. Agora cada dedo dobra a partir da sua base
  (`curlRoots`: "X1" se existir, senão "X2"); medido depois: base 76°. Palma, direção dos dedos e encaixe dos objetos
  (celular, garrafinha, cola, fone) continuam medidos como antes (`fingers`), e os testes de objeto na mão passam.
- Mãos na carteira fechadas e de lado (dedo mínimo no tampo): `FistGrip` 65°, lápis em pinça `PencilGrip` 50°; palma
  a 4 cm do tampo (`PalmAboveDesk`, a altura da mão fechada).
- **Cada aluno do seu jeito** (`WriteStyle`, sorteado por `StyleFor(seed + lugar)`): ritmo do lápis (4,5–9 rad/s),
  tamanho do vaivém, onde escreve na folha, quanto debruça (4–14°), cabeça inclinada de lado (±9°), pausas para pensar
  (escreve 2,5–7 s e para 0,8–2,2 s, ciclos desencontrados: a mão e a ponta do lápis sobem do papel) e ~35% apoiam o
  queixo no punho esquerdo em vez de segurar a folha.
- Testes: EditMode `CadaAlunoEscreveDoSeuJeito` (mesma chave = mesmo jeito; 40 chaves dão ritmos diferentes; uns com
  queixo, a maioria sem), `PausaParaPensarEntraESaiSuave`; PlayMode `FazendoAProvaAsMaosFicamEmCimaDaCarteiraEOLapisNaFolha`
  agora também exige a base do dedo dobrada (> 35°) nas mãos da carteira, o punho perto do rosto em quem apoia o
  queixo, ponta do lápis na folha fora das pausas e ritmos diferentes na turma.
- Conferido em captura (`diag_prova` agora prefere um aluno que apoia o queixo; `diag_turma` mostra a turma da frente).
- **Validar jogando:** olhar a turma fazendo a prova (punho fechado com o lápis, uns pensando com a mão no queixo,
  ritmos e pausas diferentes).
- Usuário (24/11): "salve tudo, mas não suba ainda" → tag local `v1.0.6-ref` e backup em
  `C:\Users\rafae\JogoFaculdade_backups\2026-11-24_v1.0.6\` (git bundle + .zip + partidas.csv). Nada enviado ao GitHub.

### Ir entregar a prova não é "passear" — versão 1.0.7 (24/11, noite)
Achado na análise geral (pedido: "analise todo o jogo e vê se tem algum errinho"): na partida do usuário na 1.0.6
(Normal, reprovado 4,0) os DOIS flagrantes foram `ForaDoLugar`. Pelo código, de pé a mais de 1,4 m da carteira a
suspeita subia sempre ("passeando fora do lugar"), **inclusive indo entregar a prova** — que o próprio jogo manda
fazer. (Não dá para afirmar que foi isso na partida dele; mas era possível e injusto.)
- Correção (`Player/PlayerController.cs`): `GoingToDeliver` = de pé, sem correr, e andando na direção da mesa do
  professor (desvio até `deliverWalkAngle` 55°, para caber o caminho pelos corredores) ou parado perto dela
  (`deliverDeskRadius` 2,5 m, segurando E). Nesse caso não conta como passear. Correr continua contando, e qualquer
  cola no caminho também (a taxa da cola vem antes). Parado longe da mesa, ou andando para o fundo/para os lados,
  continua sendo passear. Função pura `HeadingToDesk`. A mesa é achada uma vez (sem estado estático).
- Testes: EditMode `AndarAteAMesaDoProfessorEIrEntregarNaoPassear` (reto, pelo corredor, de lado, para trás, parado
  longe, parado na mesa); PlayMode `IrEntregarAProvaNaoContaComoPassear` (parado atrás da carteira = ForaDoLugar;
  na frente da mesa = nenhuma infração e taxa 0).
- Outros pontos da análise (sem mudança): textos revisados (ortografia e números/teclas batem com o código); log das
  partidas sem erros; sem `Debug.Log` sobrando no jogo. Pequenos: dicas/tutorial citam teclas do teclado mesmo no
  controle; números de suspeita do jogador ficam no `PlayerController` (não em `DifficultySettings`) — dívida antiga.
- **Validar jogando:** com a nota já suficiente, levantar e andar até a mesa do professor com ele olhando: a suspeita
  não deve subir; correr até lá ou ir para o fundo da sala continua chamando atenção.

### Bilhete com mira e bolinha de papel amassado — versão 1.0.8 (24/11, noite)
Pedido: "pra jogar a bolinha de papel tenha uma espécie de mira e ter que apertar o botão novamente para jogar, e
quero que a bolinha seja mais bonitinha, está muito feia". Antes: B jogava na hora no Nerd/Fundão mais perto da
direção da câmera; a bolinha (esfera + cubo) só aparecia na mão por 0,6 s e não voava.
- **Mira** (`Player/PlayerCheats.cs`): B arma (`StartAim`, só se daria para jogar: cargas, bilhete esperando, questão
  em branco, orçamento — nada é gasto ainda); a bolinha fica na mão com o braço armado (pose `throwW` que já existia)
  e aparece a mira (`Visual/BilheteMira.cs`): arco da mão até a carteira do alvo + anel pulsando onde cai, amarelo com
  alvo (Nerd/Fundão), vermelho sem alvo (o arco aponta para o chão 3 m à frente). A barra de ajuda mostra
  "MIRANDO: B joga no NERD    Espaço Cancelar". **B de novo** joga (`ConfirmAim`; sem alvo avisa e continua mirando).
  **Espaço**, outra cola ou responder cancela; Espaço mirando NÃO levanta (`AimBlocksStandUp`, vale nos dois sentidos
  da ordem dos Updates). Celular bloqueado mirando. Mirar com o braço armado é suspeito: `bilheteAimSuspicion`
  (novo em `DifficultySettings`, 10/s; o arremesso é 45–60/s), tipo `Infracao.Bilhete`. Flagrante cancela a mira.
- **Voo** (`Visual/PaperBall.cs`, `PaperBallFlight`): a bolinha sai da mão 0,12 s depois de jogar (`ReleaseTime`) e voa
  em arco girando até a folha do colega (`LandingPoint`), no tempo do arremesso. O resultado continua decidido no
  `Land()` (regras iguais): acertou = pousa na folha e fica 3 s; errou a mesa / o Nerd recusou = quica e cai no chão;
  professor pegou = some.
- **Bolinha nova**: malha feita por código — icosfera com amassados, 80 faces planas, cada face com um pedaço de
  folha de caderno (pauta azul, margem vermelha, rabisco de caneta) em direção diferente. Raio máx. 0,5 e nome
  "Sphere…" (a checagem de objeto na mão a trata como esfera). Mesma bolinha na mão e no voo. Cache estático zerado
  a cada Play.
- Textos: instruções e tutorial dizem "B bilhete (B mira, B joga)".
- Testes: EditMode `BolinhaDePapelAmassadoEArcoDoArremesso`; PlayMode `BilheteBArmaAMiraEEspacoCancelaSemLevantar`
  (tecla B virtual: mira, bolinha na mão, linha visível, suspeita de mira, nada gasto; Espaço cancela sem levantar) e
  `BilheteMiradoVoaEmArcoEPousaNaCarteiraDoNerd`. Colas/personagens/risco: 52/52.
- Conferido em captura `diag_bilhete` (mirando no Nerd de trás do ombro, voo e a bolinha pousada de perto).
- **Validar jogando:** sentado, B, girar a câmera até o Nerd (linha amarela), B de novo; testar Espaço cancelando.

### Com a nota mínima, o professor relaxa — versão 1.0.9 (24/11, noite)
Pedido: "depois que o aluno terminar as questões necessárias, quero que o professor pare de ficar achando que estou
colando, para eu entregar a prova sem ele desconfiar, independente do nível de dificuldade". Decisão do dono
(pergunta feita): **colar depois disso ainda dá flagrante**; o resto não.
- `GameManager.JaPassou` = prova de verdade em andamento (não o treino) e nota ≥ nota mínima, em qualquer
  dificuldade. Ao chegar lá (cada vez que chega, se um flagrante derrubar a nota e ela voltar): `ProfessorAI.Relax()`
  zera suspeita, "percepção" e prova ouvida; se estava desconfiado/investigando, volta à ronda; fala uma de
  `Falas.ProfRelaxou`. Toast verde importante `Falas.NotaGarantida` ("vá entregar... colar ainda é flagrante").
- Enquanto `JaPassou`: fora do lugar / correndo não gera suspeita (`PlayerController`); barulho do jogador que não é
  cola (passos, cadeira) é ignorado pelo professor (`IgnoresNoiseAfterPassing`); barulho de cola (papel do bilhete,
  celular vibrando) ou feito colando continua valendo. O Puxa-saco só dedura com taxa > 10 (só cola), então também
  não dedura mais andar/correr. Colas (celular, colinha, garrafinha, bilhete, fone, Nerd, Fundão) seguem iguais.
  Flagrante em andamento, Cara de Paisagem em andamento e distração/telefone não são interrompidos.
- Teste PlayMode `ComNotaMinimaOProfessorRelaxaMasColarAindaConta`: desconfiado → nota mínima → suspeita 0, saiu de
  desconfiado/investigando, toast; de pé longe da carteira sem infração; passos não sobem a suspeita; armando o
  bilhete volta a ser `Infracao.Bilhete` com taxa > 0; barulho de papel ainda sobe a suspeita.
- **Validar jogando:** responder/colar até "já dá!", ver o aviso verde, levantar e ir (até correndo) para a mesa:
  a suspeita não sobe. Usar o celular na frente dele ainda dá flagrante.

### Favor com mira: borracha no Dorminhoco e no Puxa-saco — versão 1.1.0 (24/11, noite)
Pedido: "uma mecânica usando a mesma forma de mirar só que com os FAVORES: jogar uma borracha no Dorminhoco para
ele fazer barulho e distrair o professor, e no Puxa-saco também".
- **Mesma mira (B), o alvo decide**: Nerd/Fundão = bilhete (como antes); Dorminhoco/Puxa-saco = **borracha**, que
  gasta 1 favor. B no controle já não tinha botão livre, por isso um sistema só. `PlayerCheats.ValidTarget`: o bilhete
  vale se der para jogar bilhete; a borracha, se ainda houver favor e o colega "topa" agora (`CanInteract`: o
  Dorminhoco dormindo, o Puxa-saco sem vigiar, sem recarga). B arma se der para jogar qualquer um dos dois.
- Mira rosa (`BilheteMira.Favor`) até a cabeça do colega; na mão e no voo, a borracha escolar rosa e azul
  (`HandProps.BuildEraser`, `HandProp.Borracha` novo no fim do enum; mesma pose de arremesso). Barra de ajuda:
  "MIRANDO: B joga a BORRACHA no DORMINHOCO (1 favor)".
- Acertou (`LandEraser`): quica na cabeça e cai; se ele ainda topa, é o mesmo favor de perto (`Interact`): o
  Dorminhoco acorda gritando / o Puxa-saco chama o professor pra uma "dúvida"; o favor só é gasto se o professor for
  ver (regra que já existia). Se não topa mais: "não adiantou nada (favor não gasto)".
- Risco: jogar ou mirar a borracha é **bagunça, não cola** (`Infracao.Outra`, "jogando borracha nos colegas"):
  `borrachaThrowSuspicion` 30/s (novo em `DifficultySettings`; o bilhete é 45–60) e `borrachaThrowTime` 0,5 s.
  Não mexe em bilhetes nem no orçamento de colas. Flagrante com a borracha no ar: ela não chega e o favor não é gasto.
- Pedir o favor de perto (E) continua funcionando. Textos: instruções "B mira e joga (bilhete ou borracha)" e o passo
  de distração do tutorial cita a borracha.
- Testes: PlayMode `BorrachaNoDorminhocoAcordaEleEGastaUmFavor`, `BorrachaNoPuxaSacoChamaOProfessorEGastaUmFavor`
  (borracha na mão, risco "Outra" 30/s, voo "Borracha Voando", 1 favor gasto, bilhetes e orçamento intactos, toast do
  favor); `BilheteBArmaAMira...` e `ComNotaMinima...` agora conferem o objeto/infração pelo alvo da mira (com a câmera
  padrão a mira pode cair no Dorminhoco/Puxa-saco). EditMode: limites de `borracha*`. 34/34 nas fixtures de colas e
  entrega.
- Captura `diag_borracha`: mira rosa até o Dorminhoco, borracha voando, ele acorda e o professor vai até ele.
- **Validar jogando:** sentado, B, girar a câmera até o Dorminhoco (mira rosa), B de novo; idem no Puxa-saco.

### Borracha na tecla V — versão 1.1.1 (24/11, noite)
O usuário estranhou a borracha no B ("mas B não é o botão da bolinha"). Agora são teclas separadas:
- **B** (controle RT) = bilhete, só mira no Nerd/Fundão. **V** (controle D-pad ↑, que não era usado na prova) =
  borracha, só mira no Dorminhoco/Puxa-saco. Nova ação `Acao.Borracha` (no FIM do enum: ele indexa o cache de teclas
  do `Controls`), rótulos "V" / "D-pad".
- A mira tem modo (`aimingAtFavor`, decidido pela tecla, não mais pelo alvo): a mesma tecla que armou joga; a outra
  troca de bilhete para borracha (ou o contrário) sem cancelar; Espaço cancela. Mensagens de "sem alvo" por modo
  (`MiraSemAlvo`, `MiraSemAlvoBorracha`); sem favor: `SemFavor`. Barra de ajuda: "BORRACHA: V joga no DORMINHOCO
  (1 favor)" / "BILHETE: B joga no NERD".
- Textos: instruções "B bilhete • F fone • V borracha (favor)" e "D-pad borracha" no controle; tutorial de distração
  cita o V.
- Teste PlayMode `VArmaABorrachaEBTrocaParaOBilhete` (teclas virtuais V, B, Espaço). Colas/entrega/controle: 36/36;
  EditMode (acabamento, controles, personagens): 43/43.
- A suíte completa da 1.1.0 foi interrompida (mudei o código durante ela); roda inteira na 1.1.1.

### Tecla V no painel das colas — versão 1.1.2 (24/11, noite)
Usuário: "não está aparecendo a tecla V como nova utilidade, escrito para o player ver (mas a funcionalidade está bem
legal)". O painel COLAS (direita, embaixo da NOTA) listava só Q/G/B/F.
- `PlayerCheats.PanelText` ganhou a 5ª linha: "[V] Borracha ×N (favor)" (N = favores que sobram; no controle a
  etiqueta é "D-pad") ou "Borracha: sem favores" (em cinza, como "confiscada").
- `HUD.SetCheats`: a folha agora cresce com o número de linhas (era fixa para 4); o pivô é em cima, então cresce para
  baixo, sem encostar na NOTA.
- Testes: `PainelDasColasApareceSemEncostarNaNota` agora espera Q, G, B, F, **V** (o painel mudou de verdade) e continua
  exigindo que tudo caiba e não cubra a NOTA; `IdentidadeVisualPlayTests` (nenhum painel da HUD se sobrepõe nem sai da
  tela) passou com a folha maior. 40/40 (colas, identidade visual, interface, entrega).
- A suíte completa da 1.1.1 foi interrompida para esta mudança; roda inteira na 1.1.2.

### Equilíbrio: Fácil mais difícil, Difícil com +1 cola — versão 1.1.3 (24/11, noite)
Pedido: "o Fácil está MUITO fácil, dê uma dificultada nos níveis mais baixos; e no Difícil só tem como colar em poucas
questões, aumente mais uma pra colar". Evidência (partidas.csv): Fácil na 1.0.3 aprovado 6,0 com 374 s sobrando (de
480); Normal 7,0 com 32 s sobrando e um reprovado 4,0 (equilibrado: **não mexido**).
Valores reais lidos da cena (`Editor/DiagAnimacao.Equilibrio`, sem alterar): a prova tem 10 questões de 1 ponto e 3
conhecidas; o Difícil tinha orçamento 4 → 3 + 4 = 7 = nota mínima (qualquer cola que desse errado reprovava).
- **Difícil**: `maxCheatAnswers` 4 → **5** (só no código: o campo não é gravado na cena).
- **Fácil** (código e cena, `ValoresNaCenaTests` exige os dois iguais; só as 8 linhas do bloco `facil` da cena, cópia
  de segurança antes): nota mínima 6 → **7**, tempo 480 → **420** s, `suspicionMultiplier` 0,5 → **0,65**,
  `reactionTime` 1,0 → **0,85**, visão 10 m/40° → **11 m/44°**, `phoneCharges` 3 → **2**, `favors` 5 → **4**.
  Mantidos: advertência no 1º flagrante, medidor de suspeita, orçamento 5, colinha/garrafinha/bilhete 3.
- Simulação (`SimulacaoDeEquilibrioTests`, jogador modelo): passar **sem colar** no Fácil caiu para 6,3% (agora é
  preciso colar); colando com cuidado: Fácil 99,7% > Normal 90,4% > Difícil 84,2% (com a folga de 1 cola). Testes de
  equilíbrio e da cena: 42/42 (ordem das dificuldades, orçamento cobre a nota mínima, celular no Fácil só dá susto).
- A simulação é um modelo; o número real vem de jogar (partidas.csv).
- **Validar jogando:** uma partida no Fácil (tem que precisar colar) e uma no Difícil (painel COLAS: "restam 5").

### Barra de segurar: o texto saía da caixa — e as auditorias mediam errado — versão 1.1.4 (24/11, noite)
Usuário: "quando seguro Q, a barra de baixo": a frase "Lendo a colinha na manga... (solte se ele chegar perto!)" saía
da caixa. A barra tinha largura fixa de 640.
- **Por que a auditoria (1.0.2) dizia que cabia:** os testes em lote rodam numa janela de **640×480** (canvas na escala
  0,38), e o `preferredWidth`/`preferredHeight` do Unity mede na escala da janela — a fonte é gerada pequena e a
  medida sai ~10% menor. Medido: colinha 577 na janela do teste × **646** na tela 1920×1080 (caixa 640); espiar o
  Nerd 631 × **704** (também estourava).
- `HUD.WidthAtFullHd` / `HeightAtFullHd` / `TextSizeAtFullHd`: medem o texto como na tela de referência (escala 1),
  em qualquer janela. Usadas em todas as caixas que se ajustam ao texto (prompt, barra de segurar, ajuda, tutorial,
  avisos, balões; no jogo em 1920×1080 dá o mesmo que antes) e nas auditorias.
- A barra de segurar agora acompanha o texto (640 a 1600 de largura): colinha 686, Nerd 744.
- Auditorias refeitas com a medida real: TODAS as perguntas e alternativas, todas as falas nos balões, tutorial,
  prompt, ajuda, menu de dificuldade, Cara de Paisagem, painel das colas — só a barra de segurar estourava. 40/40.
- Teste `TutorialPromptEAjudaCabemNasCaixas` ganhou todas as frases da barra de segurar (colas, celular, espiar o Nerd,
  entregar, todos os "segure E" da sala), em largura e altura.

### Clique esquerdo joga o bilhete / a borracha — versão 1.1.5 (24/11, noite)
Pedido: "na hora de jogar a borracha e o bilhete, ao invés de apertar o mesmo botão, quero jogar apertando o botão
esquerdo do mouse".
- Mirando (B bilhete / V borracha): **clique esquerdo joga** (`Controls.ClickPressed`). A mesma tecla de novo
  **cancela** (como o Espaço); a outra tecla troca de mira. No controle (sem mouse) o mesmo botão que armou joga
  (RT / D-pad), como antes.
- Barra de ajuda: "BILHETE: [Clique] joga no NERD" (`Controls.ThrowTag`: no controle mostra o botão). Textos:
  instruções "B bilhete / V borracha (mire e clique)"; tutorial "mira e clique pra jogar" / "V e clicar pra jogar a
  borracha".
- Teste PlayMode `CliqueEsquerdoJogaEAMesmaTeclaCancela` (teclado e mouse virtuais: B arma, B cancela sem levantar e
  sem gastar; câmera virada pro Nerd, B, clique esquerdo → jogou e gastou 1 bilhete). Colas/entrega/identidade
  visual/tutorial: 43/43.

### Suporte a Linux (11/11)
- Motivo: trabalhar também no notebook (Debian 13, Intel Iris Xe). O jogo em si já era portátil: o `MatchLog` usa
  `Application.persistentDataPath` e não há caminho de Windows no código do jogo. Só o build e a documentação eram Windows.
- `BuildJogo`: novo menu *Prova Impossível > Gerar Build Linux* (`MenuLinux`) → `Builds/Linux/SobrevivendoUnimar.x86_64`.
  `OutputFor(BuildTarget)` (puro) escolhe o caminho; plataforma não suportada é recusada sem chamar o BuildPipeline.
  Build Windows, `Output` e `Menu` continuam iguais.
- Caminhos no Linux: editor `~/Unity/Hub/Editor/6000.6.3f1/Editor/Unity`; partidas em
  `~/.config/unity3d/EstudioFundao/Sobrevivendo à UNIMAR/partidas.csv` (antes do Bloco 14: `DefaultCompany/JogoFaculdade`).
- Testes: +3 EditMode (`BuildJogoTests`); a assembly de testes EditMode agora referencia `ProvaImpossivel.Editor`.
- Testes no notebook (Linux, Unity 6000.6.3f1 em batch): EditMode 60/60 (+3), PlayMode 60/60.
- Ao abrir o projeto, o Unity no Linux reescreveu sozinho `.vscode/settings.json` (nome da solution) e
  `UniversalRenderPipelineGlobalSettings.asset` (esvaziou `m_RuntimeSettings`). Revertidos, fora do escopo;
  se aparecerem de novo no `git status`, não commitar sem conferir.
- Build Linux gerada em batch (11/11 21:15): Build Succeeded, 0 erros, 104 MB, 230 s (1ª vez compila shaders).
  Aberta por 25 s no notebook: Player.log sem exceções e sem erro de NavMesh. A build também mexeu sozinha em
  `ProjectSettings/UnityConnectSettings.asset` (m_Enabled 1 → 0): revertido.
- NÃO feito ainda: jogar uma partida completa na build Linux (precisa de um humano).

## Para validar de manhã (precisa de um humano)
1. Jogar uma partida COMPLETA em cada dificuldade no executável e conferir se dá pra passar e se é divertido.
   Cada partida grava uma linha em `%USERPROFILE%\AppData\LocalLow\EstudioFundao\Sobrevivendo à UNIMAR\partidas.csv`
   (antes do Bloco 14: `LocalLow\DefaultCompany\JogoFaculdade`).
   Linux: `~/.config/unity3d/EstudioFundao/Sobrevivendo à UNIMAR/partidas.csv`.
   Bloco 14: ouvir o ambiente com fone (murmúrio/relógio/lápis audíveis mas sem tapar o "hm?" e os passos?),
   mexer nos sliders da pausa, e testar R / M na tela final.
2. Com fone: passos do professor (dá pra saber de que lado ele está?), "hm?", batimento (irritante ou não?).
3. Nerd: dá pra perceber que ele parou de escrever antes de olhar? A olhada é justa?
4. Os números (bateria 3/2/2, favores 5/3/2, conhecidas 4/3/2, olhadas do Nerd) são chute educado: ajustar com os
   dados do partidas.csv. Ficam no Inspector do GameManager.

## Problemas conhecidos (para blocos futuros)
- Equilíbrio sem evidência: 1 partida real antiga (Fácil, antes do Bloco 1).
- A simulação avalia cada cola sozinha (sem acumular suspeita entre ações): no Fácil/Normal as colas novas nunca dão
  flagrante no modelo. Comparar com o `partidas.csv`.

- Personagens de primitivas, animação procedural, áudio sintetizado.
- O menu *Montar Fase 1* recria tudo sob `== PROVA IMPOSSIVEL ==` (apaga ajustes manuais).
- Domínio não recarrega ao dar Play (`EnterPlayModeOptions`): cuidado com variáveis `static`.
- A aba de jogo pode ficar em modo **Simulator** (celular): o jogo aparece espremido. Trocar no menu do canto esquerdo da aba para **Game**.
- No editor, o input só chega ao jogo com a aba Game em foco (o script `FocusGameViewOnPlay` foca ao dar Play).

### Personagens 3D — histórico (17–18/11)
- Genies Avatar SDK (17–18/11): abandonado. Não compila no Unity 6.6, e no 6.3 as roupas vinham sem textura. Está em
  `git stash` no branch `unity63-genies`.
- Kenney Mini Characters (madrugada de 18/11): funcionou, mas o usuário não gostou do visual. Foi substituído pela
  Quaternius (abaixo). Os arquivos da Kenney e o `KenneyModelImport.cs` foram MOVIDOS (não apagados) para
  `C:\Users\rafae\JogoFaculdade_backup_kenney`, fora do projeto.

### Personagens 3D — Quaternius + objetos de cola na mão (18/11, branch `personagens-kenney`)
- Pedido do usuário: personagens mais legais e **engraçados**, e os itens de cola **visíveis na mão** (celular,
  colinha, garrafinha etc.).
- Pacote: **Quaternius Ultimate Modular Men/Women** (CC0, quaternius.com). Em `Assets/Art/Personagens/Quaternius/`:
  7 homens (`Homem_*`), 6 mulheres (`Mulher_*`), `Animacoes_Homem.fbx`/`Animacoes_Mulher.fbx` e `License.txt`.
- Importação (`Editor/QuaterniusModelImport.cs`, menu *Prova Impossível > Personagens > Reimportar Quaternius*):
  - Homens e animações em **Humanoid**, com os ossos mapeados pelo nome, porque o mapeamento automático tomava o
    osso "Root" por quadril. O esqueleto das animações é diferente: "Body" faz o papel do quadril, "Wrist" o da mão,
    e a ponta da canela o do pé.
  - Mulheres em **Generic**: as pernas delas saem da raiz, no chão, e o Humanoid não aceita.
  - A correção do mapa vale a partir da 2ª importação; o batch `CharacterModelSetup.Batch` já reimporta.
- Escalação (`Editor/CharacterModelSetup.cs`, menu *Configurar modelos*). Todo mundo tem cabeção (1,3–1,45×).
  - **Jogador:** Casual, moletom roxo pintado de laranja.
  - **Professor:** Suit, paletó marrom, gravata vermelha, cabelo grisalho, **bigodão** e **pança com dois botões dourados**.
  - **Nerd:** Casual2, camisa azul-clara, **óculos fundo de garrafa** e **gravata-borboleta**.
  - **Dorminhoco:** Beach, de chinelo, com **máscara de dormir rosa** na testa.
  - **Fundão:** Punk, moicano, **óculos escuros** e **cordão de ouro**.
  - **Puxa-saco:** Mulher_Suit com **gravatinha vermelha**.
  - **Figurantes:** 7 modelos sorteados pelo seed, com a cor da roupa sorteada.
  - Os enfeites são primitivas presas aos ossos (`Visual/CharacterLooks.cs`).
  - As cores do FBX vêm em espaço linear e são convertidas (`.gamma`).
- Poses (`Visual/CharacterModelSkin.cs`, reescrito):
  - Os homens tocam Idle/Walk do pacote por Playables.
  - Tudo o mais é feito por código, **apontando os ossos para direções do espaço do personagem**, o que vale para
    qualquer esqueleto com os nomes do pacote: sentar, escrever, dormir, braços cruzados, mão levantada, celular
    (no colo, de lado), colinha na manga, garrafinha, arremesso do bilhete e celular na orelha.
  - Os dedos fecham segurando, e a palma vira para o lado certo.
  - Nos modelos sem animação (mulheres), a pose é refeita do zero a cada quadro. Os dedos também, em todos, porque
    não fazem parte do mapa humanoide.
- **Objetos na mão** (`Visual/HandProps.cs`, enum `HandProp`): Celular (capinha rosa, tela acesa), Colinha (papel com
  linhas), Garrafinha (garrafa azul com rótulo) e Bilhete (bolinha de papel).
  - São 1,35× o tamanho real, para ler pela câmera que fica atrás do jogador. As poses levam os objetos para o lado
    ou para cima do corpo pelo mesmo motivo.
  - `CharacterVisual.prop` diz o que está na mão. Para o jogador, vem de `CheatRules.PropInHand`: tecla 2 ou F
    mostram o celular, segurar Q a colinha, segurar G a garrafinha e o arremesso (B) a bolinha.
  - O professor ao telefone (`OnPhone`) aparece com o celular na orelha.
  - Só visual: nenhuma regra mudou.
- A pele (`CharacterModelSkin`) continua só copiando o estado do boneco de primitivas invisível, que segue sendo a
  referência do jogo (visão, cabeça, passos). Sem o asset (ou com `useModels` desligado), os bonecos de primitivas
  voltam e o objeto aparece na mão deles.
- Perdas que continuam: as expressões do rosto não aparecem no modelo (balões e ícones continuam).
- Testes:
  - EditMode `ModelosPersonagensTests` (11): escalação, cores e materiais que existem, enfeites dos principais,
    ossos e rig (homens Humanoid com o quadril certo, mulheres Generic), loop de Idle/Walk e `PropInHand`.
  - PlayMode `ModelosPersonagensPlayTests` (9): veste o modelo e o boneco some, principais com modelos diferentes,
    cabeção e enfeites sem colisor, altura sentado, rosto segue o olhar do jogo, mão levantada acima da cabeça,
    cada objeto aparece perto da mão e acima da carteira, garrafinha vai até a boca, professor anda com a caminhada.
- Visto em captura (teste PlayMode temporário em batch, fora do projeto agora): sala inteira, cada principal de
  perto, figurantes, os 4 objetos no jogador de frente e pela câmera do jogo, nerd de mão levantada, dorminhoco
  dormindo, professor ao telefone, de braços cruzados e andando.
- Resultado: EditMode 131/131, PlayMode 106/106 (os testes de jogabilidade de antes continuam passando). Executável
  regenerado (`Builds/Windows/SobrevivendoUnimar.exe`, 0 erros no build) e aberto: 0 erros no Player.log.
- Não visto: uma partida inteira jogada à mão. Não commitado (o usuário faz os commits).
- Como validar jogando: dar Play na Fase 1 e, sentado:
  - segurar **2** (celular de lado na mão), **Q** (braço esquerdo sobe com a colinha), **G** (garrafinha virada na
    boca) e **B** mirando no Nerd (braço armado com a bolinha);
  - esperar o professor atender o telefone (celular na orelha).
  Para comparar com os bonecos antigos, desmarcar `useModels` em `Assets/Resources/Personagens/ModelosPersonagens.asset`.

### Objetos sem atravessar a mão + fone de ouvido de verdade (18/11, branch `personagens-kenney`)
- Retorno do usuário depois de jogar: o celular, a colinha e a garrafinha **atravessavam a mão**, e usar o fone (F)
  mostrava **o celular** em vez de um fone.
- Causas, vistas em captura:
  - os dedos fechavam por cima do objeto (o celular tinha os dedos saindo pela tela);
  - o objeto ficava a uma distância fixa do osso, menor que a grossura da mão;
  - a garrafa ficava alinhada com o antebraço, e não atravessada na palma;
  - `PropInHand` mandava o fone para `HandProp.Celular`.
- Correções (só visual; nenhuma regra mudou):
  - **Encostar na palma** (`CharacterModelSkin.RestOnPalm`): a cada quadro, o objeto começa no meio da palma e é
    afastado na direção para onde a palma olha até nenhum ponto da mão ficar dentro dele. Os pontos são o osso da
    mão, o meio da palma e cada junta de cada dedo, com folga de meio dedo (`FingerRadius`) e meia palma
    (`PalmRadius`). O teste geométrico (`HandProps.Touches`) usa as peças reais do objeto (caixa, cilindro, esfera).
  - **Dedos por objeto**: celular e papel com a mão quase aberta por baixo (10°), garrafa com os dedos em volta
    (25°; mais que isso, a ponta dos dedos entra na garrafa), bolinha no punho (62°), microfone do fone em pinça (45°).
  - **Garrafinha**: a ordem foi invertida. Primeiro decide-se onde a garrafa fica no gole
    (`CharacterModelSkin.BottleFrame`: tampa na boca, fundo para cima e para a frente). Depois a mão é levada até
    ela com **IK de braço e antebraço** (`Reach`, novo: cotovelo para fora e para baixo), com a palma encostada por
    baixo e pela direita. A garrafa ficou 1,1× (`BottleScale`), não 1,35×, porque já é comprida. A boca agora é um
    ponto guardado na cabeça, que acompanha o cabeção.
  - Tentado e desfeito: com a garrafa quase em pé (para aparecer acima da cabeça pela câmera de trás), ela ficava
    de pé na frente da cara, com a mão aberta na testa. Pela câmera do jogo, o gole aparece como o braço erguido até
    a boca, com a garrafa em parte escondida pela cabeça.
  - **Fone** (novo `HandProp.Fone`): fone branco na orelha direita (exagerado, 1,4× o tamanho dos outros
    objetos), fio até a mão, que segura o microfone perto do queixo ("alô, primo?"), e fio descendo até o bolso.
    A cabeça inclina um pouco. Teclas F (mandar) e F (ler) mostram o fone; tecla 2 continua sendo o celular.
- Testes:
  - EditMode `ObjetoNaMaoSegueOQueOJogadorEstaFazendo`: FoneEnviar e FoneLer dão `HandProp.Fone`.
  - PlayMode `ObjetosNaoAtravessamAMao` (novo): figurante e jogador, celular, colinha e garrafinha. Nenhum osso da
    mão pode ficar dentro do objeto, e o objeto não pode descolar da mão.
  - PlayMode `FoneApareceNaOrelhaENaoNoCelular` (novo): fone perto da cabeça, microfone na mão, fio ligado, sem tela
    de celular e sem colisor.
  - PlayMode `GarrafinhaVaiAteABoca` mede agora a distância da **tampa** até a **boca** (`MouthPoint`), que tem de
    ficar abaixo de 15 cm (antes era o meio da garrafa até o osso da cabeça), e confere o fundo para cima.
  - Os testes de objeto esperam por **tempo** (`Segura`, 1 s), não por quadros: no batch os quadros são curtos e
    a pose de beber (0,33 s para entrar) às vezes não tinha assentado, e a tampa ficava a 15,8 cm da boca.
  - Resultado: EditMode 131/131; PlayMode 107/108 na rodada completa (a falha era só essa espera da garrafinha),
    depois `ModelosPersonagensPlayTests` 11/11 com a espera por tempo. Build gerado sem erros.
- Visto em captura (teste temporário, apagado depois): celular, colinha, garrafinha e fone de frente, de lado e de
  trás, no jogador e numa figurante, e pela câmera do jogo.
- Como validar jogando (sentado):
  - segurar **2**: o celular fica deitado na palma, sem dedo saindo pela tela;
  - segurar **Q**: a colinha fica na palma, virada para o rosto;
  - segurar **G**: a mão fica na frente do rosto, a garrafa atravessada na mão e a tampa na boca;
  - segurar **F**: fone branco na orelha e mão no queixo com o microfone do fio, sem nenhum celular.

## Onde está cada coisa
- Cena: `Assets/Scenes/Fase1_ProvaImpossivel.unity` · Código: `Assets/Scripts/ProvaImpossivel/`
- Testes: `Assets/Scripts/ProvaImpossivel/Editor/Tests` (EditMode) e `Assets/Tests/PlayMode` (PlayMode).
- Balanceamento: Inspector do `GameManager` (Fácil / Normal / Difícil).
- Play de fora do Unity: criar `Temp/play_fase1.sinal` (script `Editor/PlayPorSinal.cs`) com a janela do Unity em foco.
- Executável: menu *Prova Impossível > Gerar Build Windows* → `Builds/Windows/SobrevivendoUnimar.exe` (regenerado em 10/11, Bloco 14). Em batch: `-quit -executeMethod ProvaImpossivel.EditorTools.BuildJogo.Menu`.
  Linux: menu *Gerar Build Linux* → `Builds/Linux/SobrevivendoUnimar.x86_64` (batch: `...BuildJogo.MenuLinux`).
