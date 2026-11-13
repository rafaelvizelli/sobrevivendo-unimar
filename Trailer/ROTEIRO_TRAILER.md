# Trailer v2.1 — Sobrevivendo à UNIMAR (Fase 1: A Prova Impossível)

- **Conceito:** a prova é o cofre, a cola é o plano. Comédia de assalto.
- **Cortes:** mestre de 45 s + gag de 2,5 s (16:9), 30 s 16:9 e 30 s 9:16 (vertical, para redes).
- **Imagem:** planos em movimento gravados na cena real (`Prova Impossível > Trailer > Gerar planos`), com câmera de cinema,
  vinheta leve, profundidade de campo e cor quente. Nada de objeto falso: as ações são as mecânicas do jogo.
- **Identidade:** "prova impressa": papel creme com textura, azul-caneta, vermelho-carimbo, grão leve. Só duas fontes:
  Special Elite (máquina de escrever) e Caveat (caneta). Carimbos sempre com impacto (tremor curto, rotação, tinta falhada).
- **Legendas:** toda fala tem legenda embutida (nome + fala, numa faixa de papel na base da margem segura), além do balão
  do próprio jogo, maior na captura.
- **Falas:** todas existem no jogo (`Falas.cs`), exceto as marcadas **[NOVA]**, que só existem no trailer.

## Mestre (16:9)

| Tempo | Plano (clip) | Texto / legenda | Som |
|---|---|---|---|
| 0:00 | Folha da prova na carteira, câmera descendo (`00_folha`) | Datilografado: "Você tinha três semanas." **[NOVA]** · carimbo vermelho NÃO ESTUDOU (1,9 s) | Relógio, papel, máquina de escrever, carimbo grave |
| 0:03 | Professor andando entre as carteiras, câmera baixa atrás das cadeiras (`01_prof_anda`) | PROF.: "Boa sorte. Vocês vão precisar." | Murmúrio da sala |
| 0:06 | Letreiro na folha | "Você não estudou." riscado à caneta → "Você tem um PLANO." | Entra o tema do plano (120 BPM) |
| 0:09 | Regra de três, uma por batida (1,5 s cada): `02_celular`, `03_nerd` (com "O_O"), `04_fundao`, `05_fe` | Celular. (com o aparelho desenhado por cima do plano: tela acesa, "Pesquisando no celular...", notificação, tecla 2) / Nerd. / Fundão. / Fé. **["Fé" é piada de texto]** · FUNDÃO: "Toma a cola. Confia no pai." | Prato a cada corte |
| 0:15 | Colas de bolso, cartão de tecla + plano curto, entradas em chicote: `06_colinha`, `07_garrafinha`, `08_bilhete`, `09_fone` | Q Colinha "na manga" · G Garrafinha "o rótulo sabe tudo" · B Bilhete "mire bem!" · F Fone "reze pra não tocar" | "Tic" + nota a cada cartão, "whoosh" no chicote |
| 0:21 | Dorminhoco cabeceando (zZZ crescendo) e roncando (`10_dorminhoco`) | DORMINHOCO: "ZZZzzz... resposta C... zzz" | Ronco |
| 0:23 | Puxa-saco se vira para delatar (`11_puxasaco`) | PUXA-SACO: "PROFESSOOOR! Ele tá respirando na direção da minha prova!" **[NOVA]** | |
| 0:26 | Close do professor (`12_prof_sinto`) | PROF.: "Eu corrijo prova há 30 anos. Eu SINTO a cola." | Corte seco para o silêncio; pulso grave; "hm?" |
| 0:30 | Vista de cima, a imagem perde a cor (`13_cone`, quadros 30 a 80, reenquadrado e em câmera lenta) | | Batimento, passos |
| 0:33 | Flagrante: professor bravo, "!!!", clarão vermelho (`14_flagrante`) | Carimbo FLAGRANTE! [1/3] + tremor | Silêncio antes, pancada no clarão |
| 0:35 | Contagem final: a HUD real (`15_hud`) com inserções do professor (`12_prof_sinto`), cortes na batida: 1,5 s · 0,5 · 0,5 · 0,25 · 0,25; preto em 0:37,85 | Datilografado: "Tire a nota antes do sinal." (atravessa os cortes) | Baixo e bumbo dobrando, caixa acelerando, subida, sinal da escola (0:36,9), silêncio de 0,15 s |
| 0:38 | Tela final real APROVADO cai com soco de zoom, tremor e clarão (`16_aprovado`) | VOCÊ: "Professor, dá pra arredondar?" · PROF.: "Dá. Para baixo." **[NOVA]** (uma faixa só, sem piscar) · carimbo EXPULSO! por cima (0:41), com tremor e clarão vermelho | Impacto grave + metais em ré maior; silêncio antes do EXPULSO; impacto + metais dissonantes |
| 0:42 | Quadro final sobre a sala desfocada (`19_sala`): a folha cai, o título é batido à máquina letra a letra (0:42,1 a 0:42,7), a régua é riscada, a fase é datilografada, o bilhete é escrito à caneta | SOBREVIVENDO À UNIMAR / Fase 1 — A Prova Impossível / "Não estudou, né? Tô de olho. — Prof. T." / {LINK} / Estúdio Fundão · carimbo azul EM BREVE · PC (0:43,2), com tremor e clarão. Completo e parado de 0:43,2 a 0:45 | Tema completo na entrada, teclas da máquina, stinger de metais na última letra, impacto e acorde final curto no carimbo |
| 0:45 | Gag final: Nerd levanta a mão (`18_nerd_final`) | NERD: "Professor, a questão 7 tem um erro de digitação. Grave." · preto | "Plim" |

## 30 s (16:9 e 9:16)

| Tempo | Plano | Texto |
|---|---|---|
| 0:00 | `00_folha` | "Você tinha três semanas." · NÃO ESTUDOU (1,55 s) |
| 0:02,5 | Letreiro | "Você não estudou." → "Você tem um PLANO." |
| 0:04,5 | Regra de três, 1 s cada | Celular. / Nerd. / Fundão. / Fé. |
| 0:08,5 | Colas de bolso, 1 s cada | Q / G / B / F |
| 0:12,5 | `11_puxasaco` | PUXA-SACO **[NOVA]** |
| 0:15 | `12_prof_sinto` | "Eu SINTO a cola." (silêncio) |
| 0:18 | `13_cone` → 0:20 `14_flagrante` (no último segundo, cortes secos de aproximação em 0:21 · 0:21,5 · 0:21,75 e preto em 0:21,88) | FLAGRANTE! [1/3] |
| 0:22 | `16_aprovado` (cai com impacto) | "Dá pra arredondar?" / "Dá. Para baixo." · EXPULSO! (0:24,75) |
| 0:25,5 | Quadro final (título batido à máquina) | EM BREVE · PC (0:26,8) · {LINK} |

No vertical, cada plano é reenquadrado no centro da ação. Letreiros e legendas ficam dentro da margem segura
(14% de cima e 24% de baixo livres para a interface das redes).

## Observações

- **APROVADO com nota 5,9:** pelas regras do jogo, 5,9 é reprovado em qualquer dificuldade (o mínimo do Fácil é 6,0).
  A tela final capturada é a real e mostra 6,0 de 6,0 no Fácil. A piada do "arredondar" segue nas legendas.
- **Bilhete:** o jogo não desenha o papel voando nem uma mira. O plano mostra o arremesso real (som, a câmera chicoteando
  do jogador até a mesa do Nerd).
- **Quadro-negro:** a primeira questão do quadro da cena é uma das piadas removidas. Durante a captura, a ferramenta tira essa questão só na
  memória. A cena no disco não muda.
- **{LINK}:** troque em `previa/montar_previa.ps1` (parâmetro `-Link`, ou o valor padrão na linha `[string]$Link = ...`). O padrão é "Lista de desejos: link em breve".
- **Celular:** o jogo não tem modelo do aparelho (só a pose dos braços), e no plano as mãos ficam atrás da carteira. O aparelho
  é um elemento gráfico do trailer (`phone()` no template), no estilo "prova impressa". O texto da tela é o do HUD do jogo.
- **Balões do jogo no `13_cone`:** o plano foi gravado com os balões ligados e, com a câmera em movimento, eles pulam de um
  canto para o outro. O corte usa só os quadros 30 a 80 (em que eles ficam na faixa de cima) e reenquadra sem essa faixa.
  O certo é regravar o plano com `ShowBubbles = false` na ferramenta do Unity; aí o enquadramento original pode voltar.
- **Gag do Nerd:** o balão de cima é o do próprio jogo (gravado no plano) e a faixa de baixo é a legenda do trailer. Mantido.

## Trilha

Provisória, sintetizada na própria prévia (sem faixa de terceiros; não existe `Trailer/audio/`). A página sintetiza a
trilha inteira de uma vez e toca na prévia exatamente o que vai para o arquivo.

- Final: contagem com subida, silêncio de um instante, impacto grave em cada carimbo, tema completo na capa, stinger de
  metais no título, acorde final com cauda curta e o "plim" no gag do Nerd.
- Ducking: a música abaixa para ~38% enquanto há legenda de fala (a verificação confere que toda fala está coberta).
- Sem clipping: um limitador segura os picos em −1 dBFS do começo ao fim. O final fica mais cheio que o meio
  (RMS −11,9 contra −13,0 dBFS no mestre) sem passar desse teto.
- Faixa de terceiros: ainda não há suporte a `audio/trilha_final.mp3`. Quando houver uma, o crédito entra no quadro
  final pelo parâmetro `-Credito` do `montar_previa.ps1`.

## Arquivos

- `../Assets/Scripts/ProvaImpossivel/Editor/Trailer/`: a ferramenta de captura (só editor, fora do build).
- `clips/NN_nome/` (PNG 1920×1080, 30 fps) e `clips/NN_nome.mp4`: os planos. Gerados, fora do git.
- `previa/trailer_template.html`: a edição (os 3 cortes, letreiros, carimbos, legendas, trilha e exportação).
- `previa/montar_previa.ps1`: gera `previa/Trailer_Previa.html`. Com `-Exportar`, gera também os vídeos (e confere que cada
  arquivo abre e tem áudio). Com `-Verificar`, roda a checagem automática dos 3 cortes.
- `previa/servidor.mjs`: serve a prévia (http://127.0.0.1:8765/previa/Trailer_Previa.html) e recebe a exportação.
- `export/`: `trailer_mestre`, `trailer_c30` e `trailer_v30`, em `.webm` (VP9 + Opus) e `.mp4` (H.264). Fora do git.
- `verificacao/`: 6 quadros por corte (da exportação) e `verificacao/atual/` (da checagem: celular, cartões, quadro final,
  os últimos 15 s a cada 0,5 s, folhas de contato a cada 0,1 s e `relatorio_<corte>.txt`). Fora do git.

## Checagem automática

`node Trailer/previa/servidor.mjs --verificar` avança o tempo quadro a quadro (sem depender do relógio) e falha se:
uma peça (legenda, letreiro, cartão, carimbo, celular, quadro final) aparecer mais de uma vez ou fora do tempo planejado;
duas caixas de fala aparecerem juntas ou uma sumir e outra entrar em menos de 0,3 s no mesmo plano; o texto ficar a
menos de 12 px da borda da caixa ou a caixa sair da margem segura; um balão do jogo entrar ou sair no meio de um plano
dos últimos 15 s; o som passar do teto, o final não ficar mais cheio que o meio, ou uma fala ficar sem ducking.
