<#
.SYNOPSIS
Creates a conventional commit for a task and pushes it to the GitHub remote.

.DESCRIPTION
Stages the selected paths (or all changes), optionally runs the build/test
quality gate, creates a commit with the Task trailer and pushes main to the
remote (git add . / git commit -m / git push origin main).

.EXAMPLE
.\commit-task.ps1 -TaskId T-1.3 -Type feat -Scope core -Message "models Topic and StudySession" -All -Build -Test

.EXAMPLE
.\commit-task.ps1 -TaskId T-1.4 -Message "DatabaseInitializer" -Paths src/StudyTracker.Core/Data/DatabaseInitializer.cs -DryRun
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

    [switch]$All,

    [switch]$Build,

    [switch]$Test,

    [switch]$NoPush,

    [string]$Remote = 'origin',

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

Push-Location -LiteralPath $repoRoot
try {
    $currentBranch = (& git branch --show-current)
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($currentBranch)) {
        throw 'Unable to determine the current branch (detached HEAD or invalid repository).'
    }
    $currentBranch = $currentBranch.Trim()

    if ($currentBranch -ne 'main') {
        throw "commit-task.ps1 publishes to 'main'. Current branch is '$currentBranch'; checkout main first."
    }

    if (-not $All -and $Paths.Count -eq 0) {
        throw 'Nothing to stage. Pass -All or -Paths <path1,path2>.'
    }

    $solution = Get-SolutionPath -RepoRoot $repoRoot

    $buildPlanned = [bool]$Build
    $testPlanned = [bool]$Test

    if (($buildPlanned -or $testPlanned) -and $null -eq $solution -and -not $DryRun) {
        throw 'No .sln found. Create the solution first or omit -Build/-Test.'
    }

    $subject = if ([string]::IsNullOrWhiteSpace($Scope)) {
        '{0}: {1} {2}' -f $Type, $TaskId, $Message.Trim()
    }
    else {
        '{0}({1}): {2} {3}' -f $Type, $Scope.Trim(), $TaskId, $Message.Trim()
    }
    $subject = $subject.TrimEnd('.')

    $bodyLines = @("Task: $TaskId")
    if (-not [string]::IsNullOrWhiteSpace($Agent)) {
        $bodyLines += "Agent: $($Agent.Trim())"
    }
    $bodyLines += ('Tests: ' + $(if ($testPlanned) { 'dotnet test executed' } else { 'not run' }))
    $bodyLines += ('Build: ' + $(if ($buildPlanned) { 'dotnet build executed' } else { 'not run' }))
    $commitBody = $bodyLines -join "`n"

    if ($DryRun) {
        Write-Host '[dry-run] Repository : ' $repoRoot
        Write-Host '[dry-run] Branch     : ' $currentBranch
        Write-Host '[dry-run] Subject    : ' $subject
        Write-Host '[dry-run] Body       :' ($bodyLines -join ' | ')
        Write-Host '[dry-run] Solution   : ' $(if ($null -eq $solution) { '(not found)' } else { $solution })
        Write-Host '[dry-run] Build      : ' $buildPlanned
        Write-Host '[dry-run] Tests      : ' $testPlanned
        Write-Host '[dry-run] Push       : ' (-not $NoPush)
        if ($All) {
            Write-Host '[dry-run] Stage      : all changes (git add .)'
        }
        else {
            Write-Host '[dry-run] Stage      :' ($Paths -join ', ')
        }
        return
    }

    if ($buildPlanned) {
        Invoke-Native -Exe 'dotnet' -Arguments @('build', $solution, '--nologo')
    }

    if ($testPlanned) {
        Invoke-Native -Exe 'dotnet' -Arguments @('test', $solution, '--nologo')
    }

    if ($All) {
        Invoke-Native -Exe 'git' -Arguments @('add', '.')
    }
    else {
        foreach ($path in $Paths) {
            if (-not (Test-Path -LiteralPath $path)) {
                throw "Path not found: $path"
            }
            Invoke-Native -Exe 'git' -Arguments @('add', '--', $path)
        }
    }

    $staged = @(& git diff --cached --name-only)
    if ($LASTEXITCODE -ne 0) {
        throw 'git diff --cached failed.'
    }
    if ($staged.Count -eq 0) {
        throw 'No staged changes to commit.'
    }

    Write-Host ('Staged files: {0}' -f $staged.Count)

    $commitMessage = $subject + "`n`n" + $commitBody
    Invoke-Native -Exe 'git' -Arguments @('commit', '-m', $commitMessage)

    $commitHash = (& git rev-parse --short HEAD).Trim()
    if ($LASTEXITCODE -ne 0) {
        throw 'Unable to read the new commit hash.'
    }

    if (-not $NoPush) {
        $upstream = (& git for-each-ref --format='%(upstream:short)' "refs/heads/$currentBranch")
        $hasUpstream = (-not [string]::IsNullOrWhiteSpace($upstream))

        if ($hasUpstream) {
            Invoke-Native -Exe 'git' -Arguments @('push', $Remote, 'main')
        }
        else {
            Invoke-Native -Exe 'git' -Arguments @('push', '-u', $Remote, 'main')
        }
    }

    Write-Host ''
    Write-Host '=== Commit created ==='
    Write-Host ('Task    : {0}' -f $TaskId)
    Write-Host ('Commit  : {0}' -f $commitHash)
    Write-Host ('Branch  : {0}' -f $currentBranch)
    Write-Host ('Subject : {0}' -f $subject)
    Write-Host ('Pushed  : {0}' -f (-not $NoPush))
}
finally {
    Pop-Location
}
