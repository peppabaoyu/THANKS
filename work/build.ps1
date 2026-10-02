$ErrorActionPreference='Stop'
$compiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$arguments=@('/nologo','/target:winexe','/optimize+','/reference:System.Windows.Forms.dll','/reference:System.Drawing.dll','/reference:System.Xml.Linq.dll','/reference:System.Web.Extensions.dll',('/out:'+ (Join-Path $PSScriptRoot 'XieXie-next.exe')),('/win32icon:'+(Join-Path $PSScriptRoot 'assets\fluttershy.ico')),('/win32manifest:'+(Join-Path $PSScriptRoot 'app.manifest')))
foreach($file in @('Main.cs','ResponsiveUi.cs','Updates.cs','AppVersion.cs','FloatView.cs','SessionStore.cs','Themes.cs','CustomThemes.cs','StatisticsForm.cs','SelfTests.cs')){$arguments+=Join-Path $PSScriptRoot $file}
foreach($file in @('fluttershy.png','fluttershy.ico','twilight.png','twilight.ico','rainbow.png','rainbow.ico','pinkie.png','pinkie.ico','rarity.png','rarity.ico','spike.png','spike.ico','celestia.png','celestia.ico','rounded.ttf','FONT-LICENSE.txt')){$arguments+=('/resource:'+(Join-Path $PSScriptRoot ('assets\'+$file))+','+$file)}
& $compiler @arguments
if($LASTEXITCODE -ne 0){throw 'Build failed'}
Get-Item -LiteralPath (Join-Path $PSScriptRoot 'XieXie-next.exe') | Select-Object Name,Length


Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'app.config') -Destination (Join-Path $PSScriptRoot 'XieXie-next.exe.config') -Force
