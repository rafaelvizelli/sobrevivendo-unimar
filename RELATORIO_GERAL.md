# Sobrevivendo à UNIMAR — Relatório geral do projeto (24/11/2026, versão 1.0.6)

Documento de passagem: estado completo do jogo, o que já foi feito, o que falta e o que vale melhorar, para outra
sessão do Claude continuar sem retrabalho. **Leia também `CLAUDE.md` (regras) e `NOTAS_DESENVOLVIMENTO.md`
(histórico detalhado por bloco).**

---

## 1. O jogo em uma frase
Jogo 3D de humor (Unity 6.6, URP) em que o jogador é um aluno numa prova impossível do Prof. Takanervoso e precisa
**colar sem ser pego** para tirar a nota mínima e entregar a prova na mesa do professor antes do tempo acabar.
Fase 1: "A Prova Impossível". Projeto de faculdade, entrega/apresentação em 24/11/2026.

## 2. Situação atual
| Item | Estado |
|---|---|
| Versão | **1.0.6** (`ProjectSettings` → `bundleVersion`) |
| Jogável de ponta a ponta | Sim: menu → tutorial opcional → prova → tela final (aprovado/reprovado) → jogar de novo |
| Build mais recente | `Builds/Versao_1.0.6/Sobrevivendo_a_UNIMAR_1.0.6/SobrevivendoUnimar.exe` (anteriores preservadas: `Builds/Entrega` 1.0.0, `Versao_1.0.1` … `Versao_1.0.5`) |
| Testes | ~169 EditMode + ~145 PlayMode. Última suíte completa verde: 1.0.4 (167/167 + 144/144). Suíte da 1.0.6 rodando no fechamento deste relatório: **conferir o resultado** |
| Git | branch `master`; tags `v1.0.0` (release publicada no GitHub) e `v1.0.1-ref` (local). **~12 commits locais ainda não enviados** (`git log origin/master..master`) |
| Repositório | https://github.com/rafaelvizelli/sobrevivendo-unimar — **PRIVADO** (o professor precisa ser convidado) |
| Playtest humano real | Poucas partidas: 10/11 (Difícil, reprovado 5,0) e 24/11 (1.0.1, Normal, aprovado 7,0, 0 flagrantes, 32 s sobrando). Dados em `%USERPROFILE%\AppData\LocalLow\EstudioFundao\Sobrevivendo à UNIMAR\partidas.csv` |
| Opinião do dono (24/11) | "O JOGO ESTÁ BEM LEGAL" |

## 3. Como o jogo funciona (mecânicas)
- **Prova (Quiz):** perguntas de múltipla escolha (`Challenges/QuizBank.cs`, `QuizChallenge.cs`). Sem cola o jogador
  só sabe algumas ("conhecidas"); o resto é chute ou cola. Nota mínima e duração por dificuldade:
  Fácil 8 min / nota 6; Normal 6 min / nota 7; Difícil 5 min / nota 7. Até 3 flagrantes.
- **Colas** (todas consomem o mesmo orçamento da partida, `maxCheatAnswers`):
  celular (2), colinha na manga (Q), garrafinha com resposta no rótulo (G), bilhete arremessado (B), fone com o primo
  (F), espiar o Nerd, pedir cola ao Fundão.
- **Professor (IA, `AI/ProfessorAI.cs`):** NavMesh, cone de visão (`Vision.cs`), suspeita que sobe conforme a ação ×
  visibilidade, reage a ruído (`Noise.cs`), investiga, dá flagrante (carimbo `FlagranteStamp`). Sobrancelhas
  expressivas, voz, câmera de flagrante.
- **Cara de Paisagem (`FacePokerChallenge.cs`):** o professor para do lado e encara; o jogador aperta Espaço com o
  ponteiro na faixa verde. Tem passo próprio no tutorial.
- **Colegas:** Nerd (olha em volta antes de você espiar — indicador de olho desenhado), Fundão (cola boa ou ruim),
  Dorminhoco, Puxa-saco (dedura), figurantes (mochila cai e distrai o professor).
- **Eventos (`Events/`):** relógio da aula, projetor, distrações. `CheatBudget` controla a economia.
- **Infrações** decididas pelo enum `Infracao` (`GameManager.cs`): Celular, EspiarNerd, ColaFundao, ForaDoLugar,
  Correndo, Dedurado, CaraDeCulpado, ColinhaNaManga, Bilhete, CelularTocando, Garrafinha.
- **Meta:** recordes, conquistas, `partidas.csv` com dezenas de colunas por partida (`MatchLog`).
- **Tutorial (`Core/Tutorial.cs`):** jogável, passos com treino (inclui Cara de Paisagem).
- **Controles:** teclado/mouse e gamepad (`Core/Controls.cs`). Opções de vídeo/áudio na pausa.

## 4. Visual, áudio e interface
- Personagens 3D **Quaternius Ultimate Modular Men/Women** (CC0) com clipes Idle/Walk/Run tocados por
  `PlayableGraph` em `Visual/CharacterModelSkin.cs` + poses por código por cima (sentar, escrever, celular, cola,
  garrafinha, fone, dormir, braços cruzados, mão levantada).
- Correções de animação feitas hoje (24/11), todas medidas e com teste:
  - **1.0.3** pelve: os clipes Walk/Run do pacote giram a pelve ~27°; `AlignPelvis` desfaz por quadro.
  - **1.0.4** pés: pontas viradas para dentro (35°/20°); `StraightenFoot` endireita pela linha tornozelo→ponta.
  - **1.0.5** mãos na prova: IK põe as mãos em cima da carteira (antes atravessavam), lápis da carteira vai para a mão.
  - **1.0.6** mão fechada: o `Curl` dos dedos começava no osso errado nos modelos que usam `Index1` (mão aberta em
    garra — valia para todas as poses); e cada aluno escreve do seu jeito (`WriteStyle`: ritmo, pausas para pensar,
    debruçar, cabeça inclinada, ~35% com o queixo no punho).
- Cenário: sala montada a partir do pacote "school" (ver pendência de licença) + `Visual/Cenografia.cs` (lousa com a
  piada "O QUE O ZERO DISSE PARA O OITO? / QUE CINTO BONITO!" em giz, desenhada em tempo de execução).
- UI uGUI (`Text`, não TextMeshPro), tema "prova impressa" (`UITheme`, fontes Special Elite e Caveat), CanvasScaler
  1920×1080. Auditoria automática garante que perguntas, alternativas, balões, tutorial e ajuda cabem nas caixas.
- Áudio todo **sintetizado por código** (`Audio/SoundFX.cs`, `MusicDirector.cs` com trilha adaptativa em camadas e
  ducking, `AudioMix.cs`). Sem música de terceiros.

## 5. Estrutura do código
- `Assets/Scripts/ProvaImpossivel/` — 50 scripts de jogo (~11,9 mil linhas) + 28 de editor. Pastas: AI, Audio,
  Challenges, Core, Events, Interaction, Player, Students, UI, Visual, Editor.
- Maiores arquivos: `UI/HUD.cs` (~1090), `Visual/CharacterModelSkin.cs` (~1000), `AI/ProfessorAI.cs` (~850),
  `Core/GameManager.cs` (~610). Textos/falas centralizados em `Core/Falas.cs`.
- Cena: `Assets/Scenes/Fase1_ProvaImpossivel.unity` (tem ajustes manuais).
- Testes: `Assets/Scripts/ProvaImpossivel/Editor/Tests` (19 arquivos EditMode) e `Assets/Tests/PlayMode` (22 arquivos).
- Ferramentas de editor úteis:
  - Build: `-batchmode -quit -executeMethod ProvaImpossivel.EditorTools.BuildJogo.Menu` → `Builds/Windows/`
    (copiar para `Builds/Versao_X/…` sem a pasta `*DontShip*` e com `Licencas/`).
  - Capturas: `-executeMethod ProvaImpossivel.EditorTools.TrailerShots.Batch -trailerOnly <ids>` → PNGs em
    `Trailer/clips/<id>/`. Planos de diagnóstico: `diag_prof_lado`, `diag_prof_frente`, `diag_jogador`, `diag_nerd`,
    `diag_lousa`, `diag_prova`, `diag_turma`, `diag_comparar_*` (nunca entram no trailer).
  - `Editor/DiagAnimacao.cs`: mede passada, pelve, pés e ossos dos dedos dos modelos.
- Documentos: `CLAUDE.md`, `NOTAS_DESENVOLVIMENTO.md`, `README.md`, `CREDITOS.md`, `ROTEIRO_PLAYTEST.md`,
  `AUDITORIA_STEAM.md`, `DIRECAO_CRIATIVA.md`, `ROADMAP_STEAM.md`.

## 6. Regras que NÃO podem ser quebradas
(De `CLAUDE.md` e dos pedidos do dono.)
1. Namespace `ProvaImpossivel`; comentários/textos em português, nomes de classe em inglês.
2. Números de equilíbrio só em `DifficultySettings` (repassados em `GameManager.Begin()`).
3. Regras decidem pelo enum `Infracao`, nunca pelo texto.
4. Domain Reload desligado: todo estado `static` é zerado no Play (padrão `Noise.ResetOnPlay`, `MatchLog.Reset`).
5. Proteger contra input duplicado no mesmo frame (`PhaseChangedFrame`).
6. **NUNCA rodar "Montar Fase 1"** (apaga ajustes manuais da cena). Não alterar cena/prefab de forma destrutiva.
7. Toda mudança com testes + entrada em `NOTAS_DESENVOLVIMENTO.md`. Testes em batch com o **Unity fechado**, nunca
   duas suítes ao mesmo tempo.
8. Git: um commit por bloco; nada de reset destrutivo, force push ou reescrever histórico. Não apagar scripts,
   arquivos ou testes; não alterar teste só para passar. Não sobrescrever builds anteriores (pasta nova por versão).
9. Não inventar sucesso: não dizer que algo foi validado jogando sem ter jogado; não dizer que publicou sem publicar.
10. Ao testar o executável, **não passar `-screen-width/-screen-height`**: fica gravado no registro e o jogo passa a
    abrir em 1280×720 (deixa o gráfico "muito feio").

## 7. Pendências para fechar a entrega (prioridade máxima)
1. Conferir o resultado da suíte completa da 1.0.6 (EditMode + PlayMode, 0 falhas).
2. Gerar `Builds/Sobrevivendo_a_UNIMAR_1.0.6_Windows.zip` (a partir de `Builds/Versao_1.0.6/…`).
3. Escrever `ENTREGA_FINAL_1.0.6.md` (o que é, como jogar, controles, versão, testes, créditos).
4. Atualizar `README.md` (ainda diz 1.0.1 e contagem antiga de testes).
5. `git push` do master (sem force) e criar a release `v1.0.6` no GitHub com o .zip.
6. Atualizar os slides (artifact https://claude.ai/artifact/CfSQGVrnVvUEZxGgPsMcPr): capa diz "versão 1.0.0", slide
   técnico diz "287 testes"; preencher "[Seu nome]" e "[link do GitHub]".
7. **Dono:** convidar o professor no repositório privado; jogar 1 partida completa na 1.0.6; levar o .zip num pendrive.

## 8. Problemas conhecidos e riscos
- **Licença do pacote "school" DESCONHECIDA** (`Assets/school/`, nomes em russo, sem arquivo de licença). Para a
  faculdade não impede, mas para publicar/vender precisa provar a origem ou substituir (ver `CREDITOS.md`).
- Calcanhar sobe alto atrás em alguns passos (vem do clipe Walk do pacote).
- Equilíbrio com pouca evidência real (2 partidas humanas registradas); a simulação avalia cada cola isolada.
- A cabeça dos alunos escrevendo segue o "boneco invisível" que a visão do jogo usa: eles não olham muito para a
  folha (mudar isso exige cuidado para não afetar a lógica de olhar do Nerd/visão).
- O polegar fecha menos que os outros dedos (fator 0,45 no `Curl`).
- UI em `Text` legado; arquivos grandes (`HUD.cs`, `CharacterModelSkin.cs`) acumulando responsabilidades.
- Trilha musical marcada como provisória.

## 9. Sugestões de melhoria (por risco)
### Baixo risco (dá para fazer com segurança, uma por bloco, com teste)
- Alunos escrevendo olharem mais para a folha (só a cabeça do modelo, sem mexer no olhar lógico).
- Polegar fechando junto no punho (testar com celular/garrafinha/fone, que usam a mesma mão).
- Mais perguntas no `QuizBank` (variedade entre partidas) — auditoria de caixas de texto já cobre o tamanho.
- Mais falas engraçadas do professor e dos colegas em `Falas.cs` (sem mudar regras).
- Pequenas animações de "vida": aluno coçando a cabeça na pausa, apagando com a borracha, olhando o relógio.
- Revisar o mix de áudio com fone (roteiro em `NOTAS_DESENVOLVIMENTO.md`, "Para validar").
### Médio risco (precisa de playtest antes/depois)
- Ajustar números de dificuldade **com dados** do `partidas.csv` (rodar `ROTEIRO_PLAYTEST.md` com 3–5 pessoas).
- Corrigir o calcanhar alto (limitar a dobra do joelho para trás na fase de balanço do Walk).
- Substituir o pacote "school" por cenário de licença conhecida (CC0) — mexe muito no visual.
### Alto risco / pós-entrega
- Fase 2 (nova sala/professor), modo história, localização em inglês, migração para TextMeshPro, dividir `HUD.cs`
  e `CharacterModelSkin.cs`, trilha definitiva, preparação Steam (ver `ROADMAP_STEAM.md`).

## 10. Como pedir a próxima rodada ao Claude (sugestão de prompt)
> Leia `CLAUDE.md`, `RELATORIO_GERAL.md` e `NOTAS_DESENVOLVIMENTO.md` do projeto `C:\Users\rafae\JogoFaculdade`.
> Primeiro feche as pendências da seção 7 (sem mexer no jogo). Depois, se eu aprovar, faça as melhorias de **baixo
> risco** da seção 9, uma por bloco: diga o plano em poucos passos, implemente, rode os testes relacionados com o
> Unity fechado, confira em captura (`TrailerShots -trailerOnly diag_*`), registre em `NOTAS_DESENVOLVIMENTO.md`,
> faça um commit por bloco e gere a build numa pasta nova `Builds/Versao_X`. Respeite todas as regras da seção 6.
