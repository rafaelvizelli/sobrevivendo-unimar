# Monta Trailer_Previa.html: converte as capturas em JPEG 1600x900 e embute no HTML (arquivo único, abre offline).
# Uso: powershell -ExecutionPolicy Bypass -File Trailer\previa\montar_previa.ps1
Add-Type -AssemblyName System.Drawing
$aqui = Split-Path -Parent $MyInvocation.MyCommand.Path
$trailer = Split-Path -Parent $aqui
$proj = Split-Path -Parent $trailer
$cap = Join-Path $trailer 'capturas'
$fim = Join-Path $proj 'Captures\AuditoriaVisual5'

$imagens = [ordered]@{
  s03 = "$cap\03_sala.png";            s04 = "$cap\04_prof_desconfiado.png"
  s06 = "$cap\06_jogador_celular.png"; s07 = "$cap\07_nerd.png"
  s08 = "$cap\08_fundao.png";          s10a = "$cap\10a_dorminhoco.png"
  s10b = "$cap\10b_prof_telefone.png"; s11 = "$cap\11_hud_jogo.png"
  s12 = "$cap\12_puxasaco.png";        s13 = "$cap\13_sala_cima.png"
  s14 = "$cap\14_prof_bravo.png";      s14b = "$cap\14b_hud_flagrante.png"
  s18 = "$cap\18_nerd_stinger.png"
  fim_expulso = "$fim\11_fim_expulso_1920x1080.png"
  fim_aprovado = "$fim\09_fim_aprovado_1920x1080.png"
}

$codec = [System.Drawing.Imaging.ImageCodecInfo]::GetImageEncoders() | Where-Object { $_.MimeType -eq 'image/jpeg' }
$params = New-Object System.Drawing.Imaging.EncoderParameters 1
$params.Param[0] = New-Object System.Drawing.Imaging.EncoderParameter ([System.Drawing.Imaging.Encoder]::Quality), 82L

$partes = @()
foreach ($k in $imagens.Keys) {
  $src = [System.Drawing.Image]::FromFile($imagens[$k])
  $bmp = New-Object System.Drawing.Bitmap 1600, 900
  $gr = [System.Drawing.Graphics]::FromImage($bmp)
  $gr.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
  $gr.DrawImage($src, 0, 0, 1600, 900)
  $ms = New-Object System.IO.MemoryStream
  $bmp.Save($ms, $codec, $params)
  $partes += "`"$k`":`"data:image/jpeg;base64,$([Convert]::ToBase64String($ms.ToArray()))`""
  $gr.Dispose(); $bmp.Dispose(); $src.Dispose(); $ms.Dispose()
}

$utf8 = New-Object System.Text.UTF8Encoding $false
$modelo = [System.IO.File]::ReadAllText((Join-Path $aqui 'trailer_template.html'), $utf8)
$saida = $modelo.Replace('/*IMAGENS*/{}', '{' + ($partes -join ',') + '}')
$destino = Join-Path $aqui 'Trailer_Previa.html'
[System.IO.File]::WriteAllText($destino, $saida, $utf8)
"OK: $destino ($([math]::Round((Get-Item $destino).Length / 1MB, 1)) MB)"
