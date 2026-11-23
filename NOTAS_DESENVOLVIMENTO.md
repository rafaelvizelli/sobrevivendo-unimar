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
- Próximo: jogar com o executável novo (menus e HUD de papel), decidir os ajustes da auditoria 15c e PLAYTEST
  HUMANO com as colas novas (`partidas.csv`).

## Como rodar os testes
- Batch (Unity FECHADO): `Unity.exe -batchmode -projectPath <proj> -runTests -testPlatform EditMode|PlayMode -testResults <xml> -logFile <log>`.
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

## Para validar de manhã (precisa de um humano)
1. Jogar uma partida COMPLETA em cada dificuldade no executável e conferir se dá pra passar e se é divertido.
   Cada partida grava uma linha em `%USERPROFILE%\AppData\LocalLow\EstudioFundao\Sobrevivendo à UNIMAR\partidas.csv`
   (antes do Bloco 14: `LocalLow\DefaultCompany\JogoFaculdade`).
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
