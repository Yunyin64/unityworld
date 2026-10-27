param([ValidateSet('Project','Global')][string]$Scope = 'Project')
$ErrorActionPreference = 'Stop'

if ($Scope -eq 'Global') {
    $library = Join-Path $env:USERPROFILE 'AgentSkills'
    $entries = @((Join-Path $env:USERPROFILE '.agents/skills'), (Join-Path $env:USERPROFILE '.claude/skills'))
} else {
    $library = $PSScriptRoot
    $repo = Split-Path $library -Parent
    $entries = @((Join-Path $repo '.agents/skills'), (Join-Path $repo 'UnityWorld/.claude/skills'))
}
New-Item -ItemType Directory -Path $library -Force | Out-Null
foreach ($entry in $entries) { New-Item -ItemType Directory -Path $entry -Force | Out-Null }

# Nested skill packages retain their supporting files in the shared library.
$skills = @(Get-ChildItem -LiteralPath $library -Filter SKILL.md -File -Recurse)
$duplicates = $skills | Group-Object { $_.Directory.Name } | Where-Object Count -gt 1
if ($duplicates) { throw 'Duplicate skill folder names in library; resolve before connecting.' }
foreach ($skill in $skills) {
    foreach ($entry in $entries) {
        $link = Join-Path $entry $skill.Directory.Name
        # Preserve existing Claude package paths, including generator_pack.
        $relative = [IO.Path]::GetRelativePath($library, $skill.Directory.FullName)
        $existingPackage = Join-Path $entry $relative
        if ((Test-Path -LiteralPath (Join-Path $existingPackage 'SKILL.md')) -and
            ($existingPackage -ne $link)) {
            Write-Output "Existing package: $existingPackage"
            continue
        }
        $item = Get-Item -LiteralPath $link -Force -ErrorAction SilentlyContinue
        if ($item) {
            if ($item.LinkType -eq 'SymbolicLink' -and $item.Target -eq $skill.Directory.FullName) {
                Write-Output "OK: $link"
                continue
            }
            throw "Existing entry is not the expected link; preserved: $link"
        }
        New-Item -ItemType SymbolicLink -Path $link -Target $skill.Directory.FullName | Out-Null
        Write-Output "Linked: $link"
    }
}
