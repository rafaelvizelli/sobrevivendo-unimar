# Sobrevivendo à UNIMAR — notas de desenvolvimento

Última atualização: 10/11/2026 (Bloco 14 — acabamento)

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
- Próximo: PLAYTEST HUMANO (tutorial + 3 partidas por dificuldade, `partidas.csv`) e ajuste de números pelos dados.
  Depois: direção visual (precisa de autorização).

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

- Personagens de primitivas, animação procedural, áudio sintetizado.
- O menu *Montar Fase 1* recria tudo sob `== PROVA IMPOSSIVEL ==` (apaga ajustes manuais).
- Domínio não recarrega ao dar Play (`EnterPlayModeOptions`): cuidado com variáveis `static`.
- A aba de jogo pode ficar em modo **Simulator** (celular): o jogo aparece espremido. Trocar no menu do canto esquerdo da aba para **Game**.
- No editor, o input só chega ao jogo com a aba Game em foco (o script `FocusGameViewOnPlay` foca ao dar Play).

## Onde está cada coisa
- Cena: `Assets/Scenes/Fase1_ProvaImpossivel.unity` · Código: `Assets/Scripts/ProvaImpossivel/`
- Testes: `Assets/Scripts/ProvaImpossivel/Editor/Tests` (EditMode) e `Assets/Tests/PlayMode` (PlayMode).
- Balanceamento: Inspector do `GameManager` (Fácil / Normal / Difícil).
- Play de fora do Unity: criar `Temp/play_fase1.sinal` (script `Editor/PlayPorSinal.cs`) com a janela do Unity em foco.
- Executável: menu *Prova Impossível > Gerar Build Windows* → `Builds/Windows/SobrevivendoUnimar.exe` (regenerado em 10/11, Bloco 14). Em batch: `-quit -executeMethod ProvaImpossivel.EditorTools.BuildJogo.Menu`.
