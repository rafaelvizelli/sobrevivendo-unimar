# Sobrevivendo à UNIMAR — Fase 1: A Prova Impossível

> Um assalto de comédia de 6 minutos onde o cofre é a sua prova e o segurança é um professor paranoico.

Jogo 3D de furtividade e comédia universitária feito em **Unity 6.6 (URP)** como trabalho de faculdade.
Você não estudou. A prova é do Prof. Takanervoso, o homem que "não nasceu ontem". Tire a nota mínima antes do
sinal: respondendo o que sabe, colando do Nerd, usando o celular ou aproveitando o caos da sala.
Três flagrantes e você está fora.

**Versão de entrega:** 1.0.1 (24/11/2026) · Estúdio Fundão

## Como jogar

1. Baixe o `.zip` do executável, extraia a pasta inteira e abra `SobrevivendoUnimar.exe` (Windows 64 bits).
2. Na primeira vez, o jogo oferece um tutorial de 1 minuto (dá para pular).
3. Escolha a dificuldade: Fácil, Normal ou Difícil.

### Controles

| Ação | Teclado e mouse | Controle |
|---|---|---|
| Andar / câmera / correr | WASD / mouse / Shift | analógicos / L3 |
| Responder o que sabe / chutar | 1 | X |
| Celular (segurar) | 2 | Y |
| Colas: colinha, garrafinha, bilhete, fone | Q, G, B, F | LB, RB, LT, RT |
| Interagir / levantar | E / Espaço | A / B |
| Cara de Paisagem | Espaço com o ponteiro no verde | A |
| Pausa | Esc | Start |

## O que tem no jogo

- **Professor com IA própria:** patrulha, cone de visão, desconfiança que sobe com barulho e movimento, investigação
  de pistas (bilhete, celular) e reações com voz, sobrancelhas e carimbo de "flagrante".
- **Colegas que ajudam e atrapalham:** o Nerd (cola arriscada, avisa no corpo antes de olhar), o Fundão (favores),
  o Dorminhoco e acontecimentos aleatórios na sala.
- **Orçamento de colas:** não dá para colar tudo; cada tipo de cola é uma aposta diferente.
- **Cara de Paisagem:** minijogo de timing para disfarçar quando o professor chega perto.
- **Rejogabilidade:** título pelo seu estilo de jogo, recordes por dificuldade, 8 conquistas e "ficha criminal" no fim.
- **Trilha adaptativa e áudio 3D** sintetizados em código (passos do professor dão para ouvir de que lado ele está).
- **Opções:** volumes, tela cheia/janela, VSync; suporte completo a controle.

## Técnico

- Unity 6000.6.3f1, Universal Render Pipeline, Input System novo, AI Navigation (NavMesh).
- Código em `Assets/Scripts/ProvaImpossivel/` (~12 mil linhas de C#), organizado em `AI`, `Audio`, `Challenges`,
  `Core`, `Events`, `Interaction`, `Player`, `Students`, `UI` e `Visual`.
- Testes automatizados: **158 EditMode + 129 PlayMode** (Unity Test Framework), rodando em batch.
- Registro de partidas em CSV para equilibrar a dificuldade com dados reais
  (`%USERPROFILE%\AppData\LocalLow\EstudioFundao\Sobrevivendo à UNIMAR\partidas.csv`).
- Documentos de projeto: `DIRECAO_CRIATIVA.md`, `AUDITORIA_STEAM.md`, `ROADMAP_STEAM.md`, `NOTAS_DESENVOLVIMENTO.md`
  (diário de desenvolvimento) e `ROTEIRO_PLAYTEST.md`.

### Abrir o projeto

Abra a pasta no Unity Hub com a versão 6000.6.3f1 e carregue a cena `Assets/Scenes/Fase1_ProvaImpossivel.unity`.
O executável é gerado pelo menu do editor (`ProvaImpossivel.EditorTools.BuildJogo`).

## Créditos

Personagens Quaternius (CC0), fontes Special Elite (Apache 2.0) e Caveat (SIL OFL). Áudio, textos, perguntas e
falas originais. Lista completa em [`CREDITOS.md`](CREDITOS.md).

Projeto acadêmico, sem fins comerciais. "UNIMAR" é usado como ambientação do trabalho.
