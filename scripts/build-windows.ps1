param([string]$ElectronArchive,[string]$NodeExecutable="node",[string]$OutputDirectory)
$ErrorActionPreference="Stop"
$repo=Split-Path $PSScriptRoot -Parent
$runtime=Get-Content -LiteralPath (Join-Path $repo 'desktop\runtime.json') -Raw|ConvertFrom-Json
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if(-not(Test-Path -LiteralPath $compiler)){$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'}
if(-not(Test-Path -LiteralPath $compiler)){throw '.NET Framework C# compiler was not found.'}
$node=(Get-Command $NodeExecutable -ErrorAction Stop).Source
& $node (Join-Path $repo 'app\build.mjs')
if($LASTEXITCODE -ne 0){throw 'Web build failed.'}
if(-not $ElectronArchive){
 $cache=Join-Path $repo '.cache'
 New-Item -ItemType Directory -Path $cache -Force|Out-Null
 $ElectronArchive=Join-Path $cache ('electron-'+$runtime.version+'-win-x64.zip')
 if(-not(Test-Path -LiteralPath $ElectronArchive)){Write-Host 'Downloading the pinned official Electron runtime...';Invoke-WebRequest -Uri $runtime.url -OutFile $ElectronArchive -UseBasicParsing}
}
$ElectronArchive=[IO.Path]::GetFullPath($ElectronArchive)
if((Get-FileHash -LiteralPath $ElectronArchive -Algorithm SHA256).Hash -ne $runtime.sha256){throw 'Electron archive SHA-256 does not match the pinned official release.'}
$stage=Join-Path $repo ('.build\stage-'+[Guid]::NewGuid().ToString('N'))
Expand-Archive -LiteralPath $ElectronArchive -DestinationPath $stage
Rename-Item -LiteralPath (Join-Path $stage 'electron.exe') -NewName 'MOWJ.exe'
$appFolder=Join-Path $stage 'resources\app'
New-Item -ItemType Directory -Path $appFolder -Force|Out-Null
Copy-Item -LiteralPath (Join-Path $repo 'desktop\main.cjs') -Destination $appFolder
Copy-Item -LiteralPath (Join-Path $repo 'desktop\package.json') -Destination $appFolder
Copy-Item -LiteralPath (Join-Path $repo 'assets\mowj.ico') -Destination $appFolder
Copy-Item -LiteralPath (Join-Path $repo 'assets\mowj.ico') -Destination $stage
Copy-Item -LiteralPath (Join-Path $repo 'LICENSE') -Destination (Join-Path $stage 'LICENSE-MOWJ.txt')
Copy-Item -LiteralPath (Join-Path $repo 'THIRD_PARTY.md') -Destination $stage
$html=[IO.File]::ReadAllText((Join-Path $repo 'mowj-offline.html'))
$logo=[Convert]::ToBase64String([IO.File]::ReadAllBytes((Join-Path $repo 'assets\mowj-256.png')))
$html=$html.Replace('</head>','<link rel="icon" href="data:image/png;base64,'+$logo+'"></head>')
$html=$html.Replace('<b>MOWJ<span>®</span></b>','<img src="data:image/png;base64,'+$logo+'" width="44" height="44" alt="" style="display:block;margin-bottom:8px"><b>MOWJ<span>®</span></b>')
[IO.File]::WriteAllText((Join-Path $appFolder 'index.html'),$html,[Text.UTF8Encoding]::new($false))
$iconTool=Join-Path (Split-Path $stage -Parent) 'IconPatch.exe'
& $compiler /nologo /target:exe /optimize+ ('/out:'+$iconTool) (Join-Path $repo 'installer\IconPatch.cs')
if($LASTEXITCODE -ne 0){throw 'Icon utility compilation failed.'}
& $iconTool (Join-Path $stage 'MOWJ.exe') (Join-Path $repo 'assets\mowj.ico')
if($LASTEXITCODE -ne 0){throw 'Executable icon update failed.'}
function Compile-Installer([string]$Destination,[string]$Payload){
 $a=@('/nologo','/target:winexe','/platform:anycpu','/optimize+',('/out:'+$Destination),('/win32icon:'+(Join-Path $repo 'assets\mowj.ico')),('/win32manifest:'+(Join-Path $repo 'installer\app.manifest')),('/resource:'+(Join-Path $repo 'assets\mowj-logo.png')+',logo.png'),('/resource:'+(Join-Path $repo 'assets\mowj.ico')+',mowj.ico'),'/r:System.Drawing.dll','/r:System.Windows.Forms.dll','/r:System.IO.Compression.dll','/r:System.IO.Compression.FileSystem.dll','/r:System.Web.Extensions.dll')
 if($Payload){$a+=('/resource:'+$Payload+',payload.zip')}
 $a+=(Join-Path $repo 'installer\Installer.cs')
 & $compiler @a
 if($LASTEXITCODE -ne 0){throw 'Installer compilation failed.'}
}
Compile-Installer (Join-Path $stage 'Uninstall.exe') ''
$payload=Join-Path (Split-Path $stage -Parent) ('payload-'+[Guid]::NewGuid().ToString('N')+'.zip')
Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::CreateFromDirectory($stage,$payload,[IO.Compression.CompressionLevel]::Optimal,$false)
if(-not $OutputDirectory){$OutputDirectory=Join-Path $repo 'dist'}
$OutputDirectory=[IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Path $OutputDirectory -Force|Out-Null
$output=Join-Path $OutputDirectory 'MOWJ-Setup-0.4.1-win-x64.exe'
Compile-Installer $output $payload
(Get-FileHash -LiteralPath $output -Algorithm SHA256).Hash.ToLower()+'  '+[IO.Path]::GetFileName($output)|Set-Content -LiteralPath (Join-Path $OutputDirectory 'SHA256SUMS.txt') -Encoding ascii
Write-Host ('Built: '+$output)
Write-Host 'Nothing was installed or launched.'
