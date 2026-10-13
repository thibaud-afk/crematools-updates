param([string]$MakeNsis = 'makensis.exe')
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
$publish = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot 'publish'))
if(-not (Test-Path (Join-Path $publish 'CremaTools.exe'))){throw 'Executez dotnet publish avant ce script.'}
if(-not $publish.StartsWith($PSScriptRoot+[IO.Path]::DirectorySeparatorChar)){throw 'Dossier de publication inattendu.'}
Get-ChildItem -LiteralPath $publish -Recurse -File -Filter '*.pdb' | ForEach-Object {Remove-Item -LiteralPath $_.FullName}
Copy-Item LISEZ-MOI.txt,THIRD-PARTY-NOTICES.txt,LICENSE $publish
New-Item -ItemType Directory -Force -Path release | Out-Null
$lines=[Collections.Generic.List[string]]::new()
Get-ChildItem -LiteralPath $publish -Recurse -File | Where-Object {-not $_.FullName.Substring($publish.Length+1).StartsWith('test-results\') -and $_.Name -ne 'self-test-error.txt'} | ForEach-Object {$lines.Add('Delete "$INSTDIR\'+$_.FullName.Substring($publish.Length+1)+'"')}
Get-ChildItem -LiteralPath $publish -Recurse -Directory | Where-Object {-not $_.FullName.Substring($publish.Length+1).StartsWith('test-results')} | Sort-Object {$_.FullName.Length} -Descending | ForEach-Object {$lines.Add('RMDir "$INSTDIR\'+$_.FullName.Substring($publish.Length+1)+'"')}
[IO.File]::WriteAllLines((Join-Path $PSScriptRoot 'uninstall-files.nsh'),$lines,[Text.UTF8Encoding]::new($true))
& $MakeNsis /V2 installer.nsi
if($LASTEXITCODE -ne 0){throw 'La compilation a echoue.'}
