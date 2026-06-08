#!/usr/bin/env pwsh
<#
.SYNOPSIS
    知识库文本索引查询脚本
    对 knowledge_index.txt 做多关键词 grep，返回匹配的 entry.md 路径 + 模块简介。

.DESCRIPTION
    用法：
      powershell .context/code/generator_pack/kb_index_query.ps1 artifact talent
      powershell .context/code/generator_pack/kb_index_query.ps1 自走棋 战斗

    输出：每个匹配目录的 entry.md 完整路径 + 模块标题 + 文档数（紧凑格式）。
    Agent 据此选 3~5 个最相关的 entry.md，Read 后从文件清单中挑选知识文档加载。

    查询策略：
      - 关键词间 OR 语义
      - 命中 >30 → 提示缩小关键词，列出全部目录供参考
      - 命中 0 → 提示扩大关键词
#>

param(
    [Parameter(Mandatory=$true, Position=0)]
    [string]$FirstKeyword,
    [Parameter(Position=1, ValueFromRemainingArguments=$true)]
    [string[]]$MoreKeywords
)

$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new()

$Keywords = @($FirstKeyword) + ($MoreKeywords | Where-Object { $_ })

$rootDir = Split-Path -Parent $PSScriptRoot
$indexFile = Join-Path $rootDir "index\knowledge_index.txt"

if (-not (Test-Path $indexFile)) {
    Write-Error "Index file not found: $indexFile"
    exit 1
}

$pattern = ($Keywords | ForEach-Object { [regex]::Escape($_) }) -join "|"
$matches = Select-String -Path $indexFile -Pattern $pattern -CaseSensitive:$false

if (-not $matches) {
    Write-Output "[no_match] No matching directories. Try broader keywords or proceed to code search (Q3)."
    exit 0
}

$paths = $matches | ForEach-Object {
    $line = $_.Line
    $tabIndex = $line.IndexOf("`t")
    if ($tabIndex -gt 0) { $line.Substring(0, $tabIndex) }
} | Sort-Object -Unique

$Utf8NoBom = New-Object System.Text.UTF8Encoding $false

function ReadEntryLines($path) {
    if (-not (Test-Path $path)) { return @() }
    return [System.IO.File]::ReadAllLines($path, $Utf8NoBom)
}

function Get-ModuleBlurb($entryPath) {
    $dir = Split-Path -Parent $entryPath
    # Main doc = name.category.md (2 dots), skip subs like name.sub.category.md (3+ dots)
    $mainDocs = @(Get-ChildItem $dir -Filter "*.module.md" -File) + @(Get-ChildItem $dir -Filter "*.designdata.md" -File) | Where-Object {
        ($_.Name.Split('.').Count - 1) -eq 2
    } | Sort-Object Name | Select-Object -First 1
    if ($mainDocs) {
        $lines = [System.IO.File]::ReadAllLines($mainDocs.FullName, $Utf8NoBom)
        $top = @($lines | Select-Object -First 9) -join "`n"
        return $top
    }
    # Fallback for concept/experience: show entry.md table with full file paths
    $el = [System.IO.File]::ReadAllLines($entryPath, $Utf8NoBom)
    $out = @()
    foreach ($line in $el) {
        if ($line -match '^\|\s*\[([^\]]+)\]\(([^\)]+)\)') {
            $fname = $Matches[1]
            $fullFile = Join-Path $dir $fname
            $escapedOld = [regex]::Escape($Matches[2])
            $line = $line -replace "$escapedOld\)", "$fullFile)"
        }
        $out += $line
    }
    return ($out -join "`n")
}

if ($paths.Count -gt 30) {
    Write-Output "[too_many] $($paths.Count) directories matched. Try more specific keywords."
    Write-Output ""
    Write-Output "Matched directories:"
    $paths | ForEach-Object {
        $full = Join-Path $rootDir $_
        $lines = ReadEntryLines $full
        $title = if ($lines.Count -gt 0 -and $lines[0] -match "^#\s+(.+?)\s+[—\-]") { $Matches[1] } else { "" }
        $docCount = ($lines | Select-String "^\\| \[" | Measure-Object).Count
        Write-Output "  $full  |  $title  ($docCount docs)"
        $b = Get-ModuleBlurb $full
        if ($b) { Write-Output $b; Write-Output "" }
    }
    exit 0
}

$total = $paths.Count
$idx = 0
foreach ($relPath in $paths) {
    $idx++
    $fullPath = Join-Path $rootDir $relPath
    $lines = ReadEntryLines $fullPath
    $title = if ($lines.Count -gt 0 -and $lines[0] -match "^#\s+(.+?)\s+[—\-]") { $Matches[1] } else { "" }
    $docCount = ($lines | Select-String "^\\| \[" | Measure-Object).Count
    Write-Output "[$idx/$total] $fullPath  |  $title  ($docCount docs)"
    $b = Get-ModuleBlurb $fullPath
    if ($b) { Write-Output $b; Write-Output "" }
}
