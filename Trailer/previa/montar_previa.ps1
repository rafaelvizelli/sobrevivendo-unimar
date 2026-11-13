# Monta a prévia do trailer v2 e (opcional) exporta os 3 cortes.
#
#   powershell -ExecutionPolicy Bypass -File Trailer\previa\montar_previa.ps1
#       -> gera Trailer\previa\Trailer_Previa.html a partir do trailer_template.html
#   powershell -ExecutionPolicy Bypass -File Trailer\previa\montar_previa.ps1 -Exportar
#       -> também exporta mestre, 30 s 16:9 e 30 s 9:16 (.webm e .mp4) em Trailer\export\
#          e 6 quadros de verificação por corte em Trailer\verificacao\ (precisa de Node e Chrome)
#   powershell -ExecutionPolicy Bypass -File Trailer\previa\montar_previa.ps1 -Verificar
#       -> checagem determinística dos 3 cortes (peças que piscam, texto fora da caixa, margem segura, som):
#          capturas e relatório em Trailer\verificacao\atual\; falha se achar problema
#
# O LINK do quadro final se troca aqui (parâmetro -Link) ou no padrão abaixo.
# -Credito põe o crédito da trilha no quadro final (use quando houver faixa de terceiros; veja Trailer\audio\LICENCAS.txt).
param(
  [string]$Link = 'Lista de desejos: link em breve',
  [string]$Credito = '',
  [switch]$Exportar,
  [switch]$Verificar
)
$ErrorActionPreference = 'Stop'
$aqui = Split-Path -Parent $MyInvocation.MyCommand.Path
$trailer = Split-Path -Parent $aqui
$utf8 = New-Object System.Text.UTF8Encoding $false

$clips = Join-Path $trailer 'clips'
if (-not (Test-Path (Join-Path $clips '00_folha.mp4'))) {
  Write-Warning "Faltam os planos em $clips. Gere no Unity: Prova Impossível > Trailer > Gerar planos."
}

$modelo = [System.IO.File]::ReadAllText((Join-Path $aqui 'trailer_template.html'), $utf8)
$linkJs = $Link.Replace('\', '\\').Replace('"', '\"')
$licencas = Join-Path $trailer 'audio\LICENCAS.txt'
if ((Test-Path $licencas) -and -not $Credito) { Write-Warning "Existe $licencas mas -Credito está vazio: confira se a licença exige crédito no quadro final." }
$creditoJs = $Credito.Replace('\', '\\').Replace('"', '\"')
$saida = $modelo.Replace('{{LINK}}', $linkJs).Replace('{{CREDITO}}', $creditoJs)
$destino = Join-Path $aqui 'Trailer_Previa.html'
[System.IO.File]::WriteAllText($destino, $saida, $utf8)
"OK: $destino"
"Para assistir: node `"$aqui\servidor.mjs`" e abra http://127.0.0.1:8765/previa/Trailer_Previa.html"

if ($Verificar) {
  if (-not (Get-Command node -ErrorAction SilentlyContinue)) { throw 'Node não encontrado (precisa para verificar).' }
  & node (Join-Path $aqui 'servidor.mjs') --verificar
  if ($LASTEXITCODE -ne 0) { throw "A verificação achou problemas (veja o relatório acima e em Trailer\verificacao\atual\)." }
}

if ($Exportar) {
  if (-not (Get-Command node -ErrorAction SilentlyContinue)) { throw 'Node não encontrado (precisa para exportar).' }
  & node (Join-Path $aqui 'servidor.mjs') --exportar
  if ($LASTEXITCODE -ne 0) { throw "A exportação falhou (código $LASTEXITCODE)." }
  if (Get-Command ffmpeg -ErrorAction SilentlyContinue) { 'ffmpeg encontrado (não é necessário: o .mp4 já sai da página).' }
  Get-ChildItem (Join-Path $trailer 'export') | ForEach-Object { '{0}  {1:N1} MB' -f $_.Name, ($_.Length / 1MB) }
}
