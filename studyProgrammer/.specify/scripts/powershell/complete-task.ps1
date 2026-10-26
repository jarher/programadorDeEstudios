<#
.SYNOPSIS
Closes a task: runs the quality gate, marks it as done in tasks.md and commits.

.DESCRIPTION
1. Verifies the task exists and is still unchecked in tasks.md.
2. Runs dotnet build / dotnet test (unless skipped).
3. Flips "- [ ] **T-x.y**" to "- [x] **T-x.y**".
4. Delegates staging, commit and push to commit-task.ps1.

.EXAMPLE
.\complete-task.ps1 -TaskId T-1.3 -Type feat -Scope core -Message "models Topic and StudySession" -Agent 01-data-core

.EXAMPLE
.\complete-task.ps1 -TaskId T-1.1 -Message "solution and projects" -Paths src, tests -DryRun
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^T-\d+\.\d+$')]
    [string]$TaskId,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$Message,

    [ValidateSet('feat', 'fix', 'chore', 'docs', 'test', 'refactor', 'build', 'ci', 'perf')]
    [string]$Type = 'feat',

    [string]$Scope = '',

    [string]$Agent = '',

    [string[]]$Paths = @(),

    [string]$Remote = 'origin',

    [switch]$SkipBuild,

    [switch]$SkipTests,

    [switch]$NoPush,

    [switch]$DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Invoke-Native {
    param(
        [Parameter(Mandatory = $true)][string]$Exe,
        [Parameter(Mandatory = $true)][string[]]$Arguments
    )

    & $Exe @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw ("Command failed with exit code {0}: {1} {2}" -f $LASTEXITCODE, $Exe, ($Arguments -join ' '))
    }
}

function Get-SolutionPath {
    param([Parameter(Mandatory = $true)][string]$RepoRoot)

    $candidates = @(
        (Join-Path $RepoRoot 'StudyTracker.sln'),
        (Join-Path $RepoRoot 'studyProgrammer/StudyTracker.sln')
    )

    foreach ($candidate in $candidates) {
        if (Test-Path -LiteralPath $candidate) {
            return $candidate
        }
    }

    $found = Get-ChildItem -LiteralPath $RepoRoot -Filter '*.sln' -Recurse -Depth 3 -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -notmatch '[\\/](bin|obj|node_modules)[\\/]' } |
        Select-Object -First 1

    if ($null -eq $found) {
        return $null
    }

    return $found.FullName
}

$repoRoot = (& git rev-parse --show-toplevel 2>$null)
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($repoRoot)) {
    throw 'Not inside a git repository.'
}
$repoRoot = $repoRoot.Trim()

$featureConfigPath = Join-Path $repoRoot 'studyProgrammer/.specify/feature.json'
$featureDirectory = 'studyProgrammer/specs/001-study-tracker'

if (Test-Path -LiteralPath $featureConfigPath) {
    $featureConfig = Get-Content -LiteralPath $featureConfigPath -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($null -ne $featureConfig.feature_directory -and -not [string]::IsNullOrWhiteSpace($featureConfig.feature_directory)) {
        $featureDirectory = 'studyProgrammer/' + ($featureConfig.feature_directory -replace '\\', '/')
    }
}

$tasksPath = Join-Path $repoRoot (Join-Path $featureDirectory 'tasks.md')
if (-not (Test-Path -LiteralPath $tasksPath)) {
    throw "tasks.md not found: $tasksPath"
}

$content = Get-Content -LiteralPath $tasksPath -Raw -Encoding UTF8
$escapedId = [regex]::Escape($TaskId)

$donePattern = '(?m)^[ \t]*-[ \t]*\[[xX]\][ \t]*\*\*' + $escapedId + '\*\*'
if ([regex]::IsMatch($content, $donePattern)) {
    throw "$TaskId is already marked as completed in tasks.md."
}

$openPattern = '(?m)^(?<line>[ \t]*-[ \t]*)\[ \](?<rest>[ \t]*\*\*' + $escapedId + '\*\*)'
$match = [regex]::Match($content, $openPattern)
if (-not $match.Success) {
    throw "$TaskId not found in ${tasksPath} (expected an unchecked '- [ ] **$TaskId**' entry)."
}

$solution = Get-SolutionPath -RepoRoot $repoRoot

if (-not $SkipBuild) {
    if ($null -eq $solution) {
        throw 'No .sln found. Use -SkipBuild to bypass.'
    }
    Invoke-Native -Exe 'dotnet' -Arguments @('build', $solution, '--nologo')
}

if (-not $SkipTests) {
    if ($null -eq $solution) {
        throw 'No .sln found. Use -SkipTests to bypass.'
    }
    Invoke-Native -Exe 'dotnet' -Arguments @('test', $solution, '--nologo')
}

$insertAt = $match.Index + $match.Groups['line'].Length
$updated = $content.Substring(0, $insertAt) + '[x]' + $content.Substring($insertAt + 3)

if ($DryRun) {
    Write-Host '[dry-run] Tasks file : ' $tasksPath
    Write-Host '[dry-run] Task       : ' $TaskId
    Write-Host '[dry-run] Line       : ' $match.Value.Trim()
    Write-Host '[dry-run] New line   : ' ('- [x]' + $match.Value.Substring($match.Groups['line'].Length + 3).TrimEnd())
    Write-Host '[dry-run] Build      : ' (-not $SkipBuild)
    Write-Host '[dry-run] Tests      : ' (-not $SkipTests)
    Write-Host '[dry-run] Stage      : ' $(if ($Paths.Count -gt 0) { $Paths -join ', ' } else { 'all changes' })
    Write-Host '[dry-run] No changes were written.'
    return
}

$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText($tasksPath, $updated, $utf8NoBom)
Write-Host "Marked $TaskId as completed in $tasksPath"

$commitScript = Join-Path $PSScriptRoot 'commit-task.ps1'
if (-not (Test-Path -LiteralPath $commitScript)) {
    throw "commit-task.ps1 not found next to this script: $commitScript"
}

$commitArguments = @{
    TaskId  = $TaskId
    Message = $Message
    Type    = $Type
    Agent   = $Agent
    Remote  = $Remote
    NoPush  = $NoPush
}

if (-not [string]::IsNullOrWhiteSpace($Scope)) {
    $commitArguments['Scope'] = $Scope
}
if ($Paths.Count -gt 0) {
    $commitArguments['Paths'] = $Paths
}
else {
    $commitArguments['All'] = $true
}

& $commitScript @commitArguments
