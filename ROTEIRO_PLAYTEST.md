# Roteiro de Playtest — Sobrevivendo à UNIMAR

Bloco S1 da Operação Steam. Objetivo: transformar cada partida de cada pessoa em dado útil. Leva ~25 min por pessoa.

## Antes
1. Use o executável (`Builds/Windows/SobrevivendoUnimar.exe`), não o editor.
2. Para a pessoa nova cair no tutorial sozinha (como alguém que acabou de comprar), apague a marca de "tutorial
   visto" no PowerShell antes de abrir o jogo:
   `Remove-ItemProperty -Path "HKCU:\Software\EstudioFundao\Sobrevivendo à UNIMAR" -Name "prova_impossivel.tutorial_visto*"`.
   (Alternativa: no menu, "Rever tutorial [T]".)
3. O registro fica em `%USERPROFILE%\AppData\LocalLow\EstudioFundao\Sobrevivendo à UNIMAR\partidas.csv`.
   Anote o horário de início de cada pessoa (a coluna `data` liga a linha à pessoa).

## Regras para quem aplica
- **Não explique nada.** Se a pessoa perguntar, responda "o que você acha?". Anote a pergunta (é um defeito de clareza).
- Peça para ela **pensar em voz alta** ("tô vendo que ele vai virar...").
- Fique atrás e anote os horários dos momentos abaixo.

## Sessão
| Passo | O que a pessoa faz | O que você observa e anota |
|---|---|---|
| 1 | Abre o jogo e faz o tutorial | Tempo até entender o que é cola; em qual passo travou; se leu o painel de colas |
| 2 | 1ª prova no **Normal** | Primeira cola tentada; se percebeu o pigarro antes do professor virar; reação ao 1º flagrante (riu? xingou? ficou confusa?) |
| 3 | Mais 2 provas no Normal (ou até ela querer parar) | Se ela pediu para jogar de novo **sem você sugerir**; se mudou de estratégia |
| 4 | 1 prova no Difícil (opcional) | Se a dificuldade parece justa ou "roubada" |

## As 3 perguntas (logo depois da última partida, nesta ordem)
1. **"Por que você passou/reprovou na última?"** → Se ela não souber dizer, o jogo não está mostrando a causa.
2. **"O que você faria diferente na próxima?"** → Se ela tiver um plano, existe domínio a aprender.
3. **"De 0 a 10, quanto você quer jogar mais uma agora?"** → Meta: média ≥ 7.

Opcional: "Qual foi o momento mais engraçado?" e "O que te irritou?".

## O que o `partidas.csv` responde
| Pergunta de design | Colunas |
|---|---|
| A dificuldade está certa? | `dificuldade`, `resultado`, `nota`, `partida_da_sessao` (a 1ª vs a 3ª partida) |
| Qual cola as pessoas usam (e qual ignoram)? | `celular`, `nerd_copias`, `fundao_pedidos`, `colinha_usos`…`fone_usos` |
| Qual cola é perigosa demais / segura demais? | `tipos_flagrante` vs. usos de cada cola |
| O professor pressiona o suficiente? | `investigacoes`, `suspeita_max`, `cara_paisagem_ok/falhou` |
| Quando o jogo "morde" pela primeira vez? | `primeiro_flagrante_s` |
| A sorte decide? | `chutes_certos` nas partidas aprovadas por pouco |
| Comparar versões | `versao`, `orcamento` |

## Metas (vêm de `AUDITORIA_STEAM.md`, §15)
- Aprovação no 1º contato: Fácil 70–90%, Normal 40–60%, Difícil 15–30%.
- ≥ 80% explicam sozinhos o orçamento de colas e como o professor vê/ouve.
- ≥ 70% querem "mais uma"; ≥ 50% jogam 3+ partidas sem você pedir.
- 1º momento de tensão (professor olhando enquanto a pessoa cola) em ≤ 60 s.

## Depois
Mande o `partidas.csv` e as anotações para a próxima sessão de desenvolvimento: os ajustes de número saem daí, não de
opinião.
