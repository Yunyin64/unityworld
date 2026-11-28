[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $Godot,
    [Parameter(Mandatory)] [string] $OutputDirectory
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).ProviderPath
$exportFolder = [IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Path $exportFolder -Force | Out-Null

# Data 和 src 是运行时的普通目录，和导出的程序放同一层。
$executable = Join-Path $exportFolder 'Xianxia.exe'
$log = Join-Path $exportFolder 'export.log'
& $Godot --headless --path $projectRoot --export-release 'Windows Desktop' $executable *> $log
$exitCode = $LASTEXITCODE
$errors = Select-String -LiteralPath $log -Pattern '^ERROR:|\berror (?:CS|MSB|NETSDK|NU)\d+'
if ($exitCode -ne 0 -or $errors -or -not (Test-Path -LiteralPath $executable)) {
    throw "Godot 导出失败，退出码 $exitCode。日志：$log"
}
foreach ($source in @('UnityWorld/Data', 'src')) {
    Copy-Item -LiteralPath (Join-Path $projectRoot $source) -Destination $exportFolder -Recurse -Force
}
Write-Output "Windows 发布目录：$exportFolder"
