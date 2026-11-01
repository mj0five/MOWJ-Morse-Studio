param()
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if(-not(Test-Path -LiteralPath $compiler)){$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'}
$testOut=Join-Path $repo '.build\layout-tests'
New-Item -ItemType Directory -Path $testOut -Force|Out-Null
foreach($case in @(@('LayoutChecks','LayoutChecks.cs'),@('UninstallLayoutChecks','UninstallLayoutChecks.cs'))){
 $exe=Join-Path $testOut ($case[0]+'.exe')
 $compilerArguments=@('/nologo','/target:exe',('/main:'+$case[0]),('/out:'+$exe),('/win32manifest:'+(Join-Path $repo 'installer\app.manifest')),('/resource:'+(Join-Path $repo 'assets\mowj-logo.png')+',logo.png'),('/resource:'+(Join-Path $repo 'assets\mowj.ico')+',mowj.ico'),'/r:System.Drawing.dll','/r:System.Windows.Forms.dll','/r:System.Core.dll','/r:System.IO.Compression.dll','/r:System.IO.Compression.FileSystem.dll','/r:System.Web.Extensions.dll',(Join-Path $repo 'installer\Installer.cs'),(Join-Path $repo ('tests\'+$case[1])))
 & $compiler @compilerArguments
 if($LASTEXITCODE -ne 0){throw 'Layout test compilation failed.'}
 & $exe $testOut
 if($LASTEXITCODE -ne 0){throw 'Layout test failed.'}
}
Write-Host 'Layout renders written under .build/layout-tests. No installation was performed.'
