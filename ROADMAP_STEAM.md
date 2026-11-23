# Roadmap Steam — Sobrevivendo à UNIMAR

Data: 23/11/2026 · Etapa 6 da "Operação Steam" · Base: `AUDITORIA_STEAM.md` e `DIRECAO_CRIATIVA.md`.
Blocos pequenos e independentes, em ordem de execução. Prioridade: **P0** crítico · **P1** alto impacto ·
**P2** melhoria importante · **P3** acabamento futuro. Cada bloco entra nas `NOTAS_DESENVOLVIMENTO.md` com o que
mudou, os testes e como validar jogando.

Regras que valem para todos os blocos: as do `CLAUDE.md` (números em `DifficultySettings`, `Infracao`, estático
zerado a cada Play, sem "Montar Fase 1", testes + notas). Nenhum bloco altera a cena `.unity` por fora do editor.
Tudo o que for novo na cena é criado por código (o padrão que o projeto já usa com `PlayerCheats`).

---

## S0 — Higiene de produção · P0
- **Objetivo:** projeto seguro para mexer e para mostrar.
- **Problema:** T2 (valores de dificuldade talvez não aplicados), T8/T13 (licenças e nome), T10 (cena temporária).
- **Arquivos:** teste novo de valores absolutos na cena; `.gitignore`; `CREDITOS.md` (novo).
- **Mudanças:** teste que abre a cena e confere os números de cada dificuldade contra o código; se divergirem,
  corrigir pelo caminho que não mexe na cena à mão (ver o relatório do bloco); ignorar `InitTestScene*`; listar a
  licença de cada asset de terceiros.
- **Riscos:** baixo.
- **Testes:** EditMode novo (valores absolutos).
- **Aceitação:** os números do Inspector = os do código para as 4 dificuldades; `CREDITOS.md` com cada asset.
- **Impacto no jogador:** indireto (as dificuldades passam a ser o que o design diz).

## S1 — Kit de playtest · P0
- **Objetivo:** cada partida de um testador vira dado útil.
- **Problema:** C1, G3 (sem evidência; a simulação não mede risco acumulado).
- **Arquivos:** `Core/MatchLog.cs`, `ProfessorAI.cs` (contadores), `ROTEIRO_PLAYTEST.md` (novo).
- **Mudanças:** colunas novas no `partidas.csv`: investigações sofridas, tempo até o 1º flagrante, nota vinda de
  chute, nota vinda de cola, maior suspeita da partida, versão do jogo. Roteiro com tarefas, o que observar e 3
  perguntas pós-partida.
- **Riscos:** quebrar a leitura de CSVs antigos. Mitigação: colunas só no fim.
- **Testes:** EditMode (cabeçalho e linha), PlayMode (contadores sobem).
- **Aceitação:** uma partida gera a linha com as colunas novas preenchidas.
- **Impacto:** nenhum direto; habilita todos os ajustes futuros.

## S2 — Núcleo justo: risco no lugar do dado · P0
- **Objetivo:** quem joga bem passa; o risco vem de colar, não do chute.
- **Problema:** C2, C3, G4, G5.
- **Arquivos:** `GameManager.cs` (DifficultySettings), `ProfessorAI.cs`, `PlayerCheats.cs`, `Core/Noise.cs`,
  `ExamSystem.cs`, `SimulacaoDeEquilibrioTests.cs`, `AuditoriaRiscoColasTests.cs`.
- **Mudanças:**
  1. **Orçamento cobre o piso:** conhecidas + orçamento = nota mínima. Fácil 3+5 = 8 (mín. 6), Normal 3+4 = 7 (mín. 7),
     Difícil **2**+5 = 7 (mín. 7). O chute vira margem contra flagrantes. A dificuldade passa a ser *quantas colas
     você precisa tirar debaixo do nariz dele*, e no Difícil são 5 contra o professor mais atento.
  2. **Colinha vista de perto = flagrante na hora** (`colinhaInstantCatch`, visibilidade mínima por dificuldade).
  3. **Investigação acha provas:** o barulho carrega a `Infracao`; ao chegar, chance de achar o bilhete ou o
     celular que vibrou → flagrante do tipo certo (`bilheteFindChance`, `foneRingFindChance`).
  4. **O Dedurado não apaga o tipo** do que o Puxa-saco viu.
  5. **Bilhete responde primeiro uma questão que você não sabe.**
  6. Garrafinha: mantém a suspeita por ser vista (beber água de olho no professor é suspeito); decisão registrada.
- **Riscos:** o Normal ficar fácil. Mitigação: a simulação passa a ter meta nova (cuidadoso 70–90%, arrojado
  bem abaixo) e o playtest confirma.
- **Testes:** EditMode (regras puras novas, simulação com metas novas, piso coberto pelo orçamento); PlayMode (auditoria
  de risco: colinha vista perto = F; bilhete achado = F Bilhete; celular achado = F CelularTocando).
- **Aceitação:** jogo perfeito simulado aprova ≥ 90% no Normal; colinha vista a 2,5 m dá flagrante; investigar barulho
  de bilhete pode achar o bilhete.
- **Impacto:** perder passa a ser culpa sua (e isso é divertido).

## S3 — Tutorial completo e curto · P1
- **Objetivo:** ensinar tudo o que existe, em menos tempo.
- **Problema:** C4.
- **Arquivos:** `Core/Tutorial.cs`, `Core/Falas.cs`, `TutorialTests`, `TutorialPlayTests`.
- **Mudanças:** passo novo "Colas de bolso" (Q/G/B/F, qualquer uma) com a explicação do orçamento; o passo "Andar" sai
  da frente (o jogo começa sentado, com o professor falando); mensagens com o porquê.
- **Riscos:** baixo.
- **Testes:** EditMode (ordem e condição dos passos), PlayMode (treino completo pelo teclado virtual).
- **Aceitação:** o treino passa pelas colas de bolso e explica o orçamento; o professor fala no 1º segundo.
- **Impacto:** a primeira sessão deixa de esconder metade do jogo.

## S4 — O professor estrela · P1
- **Objetivo:** presença e personalidade do antagonista.
- **Problema:** V1, V2, V6, V8.
- **Arquivos:** `Visual/CharacterLooks.cs`, `Visual/CharacterModelSkin.cs` (ou componente novo `ProfessorFace`),
  `Audio/SoundFX.cs`, `UI/HUD.cs` (carimbo), `ProfessorAI.cs` (gatilhos), câmera.
- **Mudanças:** sobrancelhas grossas e móveis + olhos por cima dos óculos conforme `Expression`; escala ~1,15×;
  "voz" em grunhidos sintetizados por estado ("hm?", "ahá!", "hmpf"); no flagrante, a câmera fecha nele por 1,2 s e um
  carimbo "FLAGRANTE!" bate na tela.
- **Riscos:** IK/pose conflitar com a escala; a câmera do flagrante atrapalhar o controle. Mitigação: a câmera só
  age com o jogador travado (já acontece no flagrante).
- **Testes:** PlayMode (expressão muda as sobrancelhas; escala; carimbo aparece e some; câmera volta).
- **Aceitação:** as 4 expressões dele são distinguíveis pela câmera do jogo (captura); o flagrante tem momento.
- **Impacto:** o maior ganho perceptivo por hora de trabalho.

## S5 — Música adaptativa · P1
- **Objetivo:** tensão e comédia pelo som.
- **Problema:** V7, V8.
- **Arquivos:** `Audio/MusicDirector.cs` (novo), `Audio/AudioMix.cs`, `GameOptions` (volume da música).
- **Mudanças:** trilha em camadas (base calma, pulso de desconfiança, percussão de investigação) que sobem e descem com
  o estado do professor; estinger no flagrante e na aprovação; volume próprio nas opções. **Provisória:** sintetizada
  em código, no mesmo padrão do `SoundFX`, até existir trilha composta (registrado como dívida).
- **Riscos:** cansar o ouvido; tapar os passos. Mitigação: mixagem abaixo dos sons de jogo + teste de volume.
- **Testes:** EditMode (estado → camadas), PlayMode (muda com o professor; respeita o volume).
- **Aceitação:** dá para *ouvir* que ele está investigando sem olhar.
- **Impacto:** alto em tensão.

## S6 — Título de estilo, recordes e conquistas locais · P1
- **Objetivo:** motivo para jogar de novo.
- **Problema:** G7, G8.
- **Arquivos:** `Core/MatchSummary.cs`, `Core/Records.cs` (novo), `UI/HUD.cs` (tela final).
- **Mudanças:** título pelo jeito que você passou (regras puras); melhor nota e menor tempo por dificuldade; 8
  conquistas locais com nomes engraçados; a ficha criminal mostra o que te derrubou + uma dica.
- **Riscos:** baixo.
- **Testes:** EditMode (título, recorde, conquistas), PlayMode (tela final mostra).
- **Aceitação:** cada final mostra título/recorde/dica; os recordes sobrevivem a reiniciar o jogo.

## S7 — Controles: controle (gamepad) e mapa único de entrada · P1
- **Objetivo:** Steam Deck e controle; base para remapear.
- **Problema:** T3.
- **Arquivos:** `Core/Controls.cs` (novo, ponto único de entrada), todos os leitores de `Keyboard.current`.
- **Mudanças:** cada ação do jogo vira uma consulta a `Controls` (teclado **ou** controle); câmera no analógico
  direito; ajuda na tela troca o ícone da tecla conforme o último dispositivo.
- **Riscos:** regressão de input (o projeto já teve problemas com input duplicado). Mitigação: `Controls` mantém a
  mesma semântica de `wasPressedThisFrame`/`isPressed`; todos os testes de teclado virtual continuam.
- **Testes:** EditMode (mapa), PlayMode (ações por controle virtual).
- **Aceitação:** partida inteira jogável só com controle.

## S8 — Opções de vídeo e acessibilidade básica · P2
- Tela cheia/janela, resolução, vsync, qualidade; tamanho do texto da HUD; legenda para sinais sonoros ("[passos
  pesados à esquerda]"). Arquivos: `GameOptions`, `HUD`. Testes: EditMode (salvar/carregar), PlayMode (aplicar).

## S9 — Sala com identidade · P2
- Luz de fim de tarde, ventilador de teto, cartazes, lousa sem o projetor na frente, remover a escada. Tudo criado por
  código num componente "cenografia", sem tocar na cena à mão. Precisa de captura e de aprovação visual do dono.

## S10 — HUD mais enxuto · P2
- Uma faixa de aviso por vez, painel de colas recolhível, contraste dos textos cinza. Captura antes/depois.

## S11 — Textos centralizados · P2
- Mover os literais de `GameManager`, `FacePokerChallenge`, `PlayerController` e `EventDirector` para `Falas`. Pré-
  requisito de qualquer localização.

## S12 — Desempenho medido · P2
- Medição de FPS em batch com cena cheia, perfil das partes caras (pose por código, cone), meta de 60 FPS em vídeo
  integrado.

## S13 — Cara de Paisagem temática · P1 (depois do playtest)
- Trocar a barra por uma interação de "disfarçar" (desviar o olhar e respirar no ritmo) mantendo a mesma interface
  `Begin(closeness, onDone)`. Fica depois do S1 porque o playtest dirá se o problema é o minijogo ou a frequência dele.

## S14 — Página da Steam e demo · P3
- Cápsulas, 5 capturas, GIF do flagrante, trailer atualizado, texto da loja, build de demo. Depende da decisão do
  nome (T13).

---

## Status (23/11, fim do dia)

| Bloco | Status | Observação |
|---|---|---|
| S0 Higiene | ✅ feito | Valores da cena conferidos (sem problema); `CREDITOS.md`. Pendente do dono: origem do pacote `school`, nome "UNIMAR" |
| S1 Kit de playtest | ✅ feito | `ROTEIRO_PLAYTEST.md` + 6 colunas no CSV. **Falta: as partidas com pessoas** |
| S2 Núcleo justo | ✅ feito (hipótese) | Orçamento cobre o piso (Normal/Difícil 3+4); colinha pega na hora; investigação acha bilhete/celular. Confirmar com playtest |
| S3 Tutorial | ✅ feito | Começa sentado; passo das colas de bolso + orçamento. Cara de Paisagem ainda não é ensinada (S13) |
| S4 Professor estrela | ✅ feito | Sobrancelhas expressivas, 1,15×, "a-HÁ!", câmera e carimbo no flagrante. Vozes provisórias |
| S5 Trilha | ✅ feito (provisória) | 3 camadas + estingers, sintetizada; **não ouvida** pelo agente |
| S6 Rejogabilidade | ✅ feito | Título de estilo, dica, recorde, 8 conquistas locais |
| S7 Controle | ✅ feito (parcial) | Jogo inteiro no controle; ajuda troca sozinha. Faltam: remapear, ícones gráficos, textos fixos de menu/tutorial |
| S8 Vídeo/acessibilidade | 🟡 parcial | Tela cheia/janela e VSync. Faltam: resolução, tamanho de texto, legendas de som, daltonismo |
| S9 Sala com identidade | ⏸ aguardando | Precisa de aprovação visual do dono |
| S10 HUD enxuto | ⏸ não começado | P2 |
| S11 Textos centralizados | ⏸ não começado | Depende da decisão de mercado/idioma |
| S12 Desempenho | 🟡 medido | 278 FPS numa RTX 2060 em janela pequena; falta medir em vídeo integrado 1080p |
| S13 Cara de Paisagem temática | ⏸ depois do playtest | |
| S14 Página da Steam | ⏸ bloqueado | Depende do nome (T13) |

Executável regenerado (versão 0.2.0) em `Builds/Windows/SobrevivendoUnimar.exe`.

## Ordem de execução
S0 → S1 → S2 → S3 → S4 → S5 → S6 → S7 → S8, e depois S9–S14 conforme o playtest e as decisões do dono do projeto
(nome/marca, aprovação visual da sala, mercado e idioma).

## O que depende do dono do projeto (não decidido aqui)
- Nome "UNIMAR" na versão comercial (T13).
- Origem/licença do pacote `Assets/school` (T8).
- Fazer os playtests com pessoas (S1 entrega o kit; as partidas são humanas).
- Aprovar o visual da sala (S9) e o estilo final da trilha (S5 entrega uma provisória).
