# Regras do projeto (Sobrevivendo à UNIMAR)

- Tudo fica no namespace ProvaImpossivel, com comentários e textos em português e estrutura de classes em inglês.
- Todo número novo de equilíbrio vai em DifficultySettings e é repassado em GameManager.Begin().
- Regras decidem pelo enum Infracao, nunca pelo texto.
- Estado estático precisa ser zerado a cada Play (Domain Reload está desligado). Siga o padrão de Noise.ResetOnPlay e MatchLog.Reset.
- Proteja contra input duplicado no mesmo frame, como já é feito com PhaseChangedFrame.
- Não rode "Montar Fase 1": a cena tem ajustes manuais que seriam perdidos.
- Toda mudança vem com testes EditMode e uma entrada em NOTAS_DESENVOLVIMENTO.md.
- Antes de codar, leia os arquivos relacionados e me diga o plano em poucos passos. Depois de codar, rode os testes e diga como validar jogando.
- Não altere nada fora do escopo pedido.

## Referência rápida
- Estado atual, relatórios por bloco e o que falta validar: `NOTAS_DESENVOLVIMENTO.md` (ler primeiro).
- Testes em batch (com o Unity FECHADO):
  `"C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Unity.exe" -batchmode -projectPath <proj> -runTests -testPlatform EditMode|PlayMode -testResults <xml> -logFile <log>`
