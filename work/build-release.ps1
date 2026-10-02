param([string]$Destination=(Join-Path $PSScriptRoot '..\outputs'))
$ErrorActionPreference='Stop'
$Destination=[IO.Path]::GetFullPath($Destination)
New-Item -ItemType Directory -Path $Destination -Force | Out-Null
& (Join-Path $PSScriptRoot 'build.ps1')
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$docs=Join-Path $PSScriptRoot '..\outputs'
$argsList=@('/nologo','/target:winexe','/optimize+','/reference:System.Windows.Forms.dll','/reference:System.Drawing.dll',('/out:'+(Join-Path $Destination 'XieXie-Setup.exe')),('/win32icon:'+(Join-Path $PSScriptRoot 'assets\fluttershy.ico')),('/resource:'+(Join-Path $PSScriptRoot 'XieXie-next.exe')+',歇歇.exe'),('/resource:'+(Join-Path $PSScriptRoot 'app.config')+',歇歇.exe.config'),('/resource:'+(Join-Path $docs '使用说明.html')+',使用说明.html'),('/resource:'+(Join-Path $PSScriptRoot 'assets\FONT-LICENSE.txt')+',字体许可.txt'),(Join-Path $PSScriptRoot 'Setup.cs'))
& $compiler @argsList
if($LASTEXITCODE -ne 0){throw 'Installer build failed'}
$portable=Join-Path $PSScriptRoot 'release-portable';New-Item -ItemType Directory -Path $portable -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'XieXie-next.exe') -Destination (Join-Path $portable '歇歇.exe') -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'app.config') -Destination (Join-Path $portable '歇歇.exe.config') -Force
Copy-Item -LiteralPath (Join-Path $docs '使用说明.html') -Destination $portable -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'assets\FONT-LICENSE.txt') -Destination (Join-Path $portable '字体许可.txt') -Force
Compress-Archive -Path (Join-Path $portable '*') -DestinationPath (Join-Path $Destination 'XieXie-Portable.zip') -Force
Get-FileHash -LiteralPath (Join-Path $Destination 'XieXie-Setup.exe'),(Join-Path $Destination 'XieXie-Portable.zip') -Algorithm SHA256 | ForEach-Object {$_.Hash.ToLower()+'  '+[IO.Path]::GetFileName($_.Path)} | Set-Content -LiteralPath (Join-Path $Destination 'SHA256SUMS.txt') -Encoding ascii
