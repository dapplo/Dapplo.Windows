<#
.SYNOPSIS
    Local build/test runner for Dapplo.Windows, driven by request files.

.DESCRIPTION
    Watches <repo>\.build-runner\requests for *.json request files and executes a FIXED set of actions:
      build   - dotnet build of src\Dapplo.Windows.sln
      test    - dotnet test of src\Dapplo.Windows.Tests (no build), optional framework and test filter
      verify  - build, then test
      pack    - dotnet pack of src\Dapplo.Windows.sln into .build-runner\packages (Release only)
    Nothing else can be executed: the request only selects an action, a configuration, an optional target framework
    and an optional, validated test filter. Output goes to <repo>\.build-runner\results\<id>.log and a summary
    (exit codes, test counts) to <id>.json.

    Tests marked [Trait("Category", "Interactive")] change the real desktop (they send input, replace the clipboard
    or write to the registry). They are excluded unless the request sets "interactive": true.

    Before each request, leftover processes started from the repository's src\*\bin folders (e.g. a test host that
    survived a killed run) are stopped, because they lock the build output.

    A running request is cancelled (process tree killed) when the file <repo>\.build-runner\cancel exists or after
    -TimeoutMinutes. Tests that hang longer than -TestHangTimeout are aborted by dotnet test (--blame-hang), the log
    then names the hanging test. When this script file changes, the runner restarts itself with the new version.

    This lets a coding assistant (or any tool that can write files into the repository) trigger builds and tests
    without having a shell on this machine. Start it once per session; stop it with Ctrl+C.

    Request file example (.build-runner\requests\<id>.json):
      { "id": "20260929-2300-verify", "action": "verify", "configuration": "Debug", "framework": "net10.0-windows", "filter": "FullyQualifiedName~Clipboard" }

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File tools\build-runner\Start-BuildRunner.ps1
#>
[CmdletBinding()]
param(
    [int]$PollSeconds = 2,
    [int]$TimeoutMinutes = 30,
    [string]$TestHangTimeout = '3m'
)

$ErrorActionPreference = 'Stop'
$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$RunnerDir = Join-Path $RepoRoot '.build-runner'
$RequestDir = Join-Path $RunnerDir 'requests'
$ResultDir = Join-Path $RunnerDir 'results'
$PackageDir = Join-Path $RunnerDir 'packages'
$CancelFile = Join-Path $RunnerDir 'cancel'
$Solution = Join-Path $RepoRoot 'src\Dapplo.Windows.sln'
$TestProject = Join-Path $RepoRoot 'src\Dapplo.Windows.Tests\Dapplo.Windows.Tests.csproj'

New-Item -ItemType Directory -Force -Path $RequestDir, $ResultDir | Out-Null

$Dotnet = (Get-Command dotnet.exe -ErrorAction SilentlyContinue).Source

function Invoke-Step {
    param([string]$Name, [string]$Exe, [string[]]$Arguments, [string]$Log)

    Add-Content -Path $Log -Encoding UTF8 -Value ("==== {0}: {1} {2}" -f $Name, $Exe, ($Arguments -join ' '))
    $out = "$Log.$Name.out"
    $err = "$Log.$Name.err"
    $proc = Start-Process -FilePath $Exe -ArgumentList $Arguments -WorkingDirectory $RepoRoot -NoNewWindow -PassThru `
        -RedirectStandardOutput $out -RedirectStandardError $err
    # Touch the handle right away, otherwise ExitCode stays empty for processes started with redirection
    $null = $proc.Handle
    $deadline = (Get-Date).AddMinutes($TimeoutMinutes)
    $stopReason = $null
    while (-not $proc.WaitForExit(1000)) {
        if (Test-Path $CancelFile) { $stopReason = 'CANCELLED' }
        elseif ((Get-Date) -gt $deadline) { $stopReason = "TIMED OUT after $TimeoutMinutes minutes" }
        if ($stopReason) {
            # Kill the whole tree (e.g. testhost.exe started by dotnet test)
            & taskkill.exe /T /F /PID $proc.Id 2>&1 | Out-Null
            $proc.WaitForExit(10000) | Out-Null
            break
        }
    }
    # A test host that outlived dotnet test (e.g. after a blame-hang abort) still holds the redirected output files
    Stop-StaleRepoProcesses -Log $Log
    if ($stopReason) {
        Remove-Item -Path $CancelFile -Force -ErrorAction SilentlyContinue
        Add-Content -Path $Log -Encoding UTF8 -Value "==== $Name $stopReason"
        $code = -1
    } else {
        $code = $proc.ExitCode
    }
    foreach ($f in @($out, $err)) {
        if (Test-Path $f) {
            for ($attempt = 1; $attempt -le 10; $attempt++) {
                try {
                    Get-Content -Path $f -Encoding UTF8 -ErrorAction Stop | Add-Content -Path $Log -Encoding UTF8
                    Remove-Item $f -Force -ErrorAction Stop
                    break
                } catch {
                    if ($attempt -eq 10) { Add-Content -Path $Log -Encoding UTF8 -Value "==== could not collect $f : $($_.Exception.Message)" }
                    Start-Sleep -Seconds 2
                }
            }
        }
    }
    Add-Content -Path $Log -Encoding UTF8 -Value "==== $Name exit code: $code"
    return $code
}

function Stop-StaleRepoProcesses {
    param([string]$Log)
    # Test hosts left behind by a killed or hung run keep files in bin\ locked (MSB3021). Only processes whose
    # executable lives inside this repository's src\*\bin folders are stopped.
    $binRoot = Join-Path $RepoRoot 'src'
    $stale = @(Get-CimInstance Win32_Process -ErrorAction SilentlyContinue | Where-Object {
        $_.ExecutablePath -and $_.ExecutablePath.StartsWith($binRoot, [StringComparison]::OrdinalIgnoreCase) -and $_.ExecutablePath -match '\\bin\\'
    })
    foreach ($p in $stale) {
        Add-Content -Path $Log -Encoding UTF8 -Value ("==== stopping stale process {0} (PID {1})" -f $p.ExecutablePath, $p.ProcessId)
        & taskkill.exe /T /F /PID $p.ProcessId 2>&1 | Out-Null
    }
}

function Get-TrxCounts {
    param([string]$Path)
    if (-not (Test-Path $Path)) { return $null }
    try {
        [xml]$trx = Get-Content -Path $Path -Raw -Encoding UTF8
        $c = $trx.TestRun.ResultSummary.Counters
        $failed = @($trx.TestRun.Results.UnitTestResult | Where-Object { $_.outcome -eq 'Failed' } | ForEach-Object { $_.testName })
        return [ordered]@{ total = [int]$c.total; executed = [int]$c.executed; passed = [int]$c.passed; failed = [int]$c.failed; not_executed = [int]$c.notExecuted; failed_tests = $failed }
    } catch { return $null }
}

function Invoke-Request {
    param([string]$File)

    $request = Get-Content -Path $File -Raw -Encoding UTF8 | ConvertFrom-Json
    $id = [string]$request.id
    if ($id -notmatch '^[A-Za-z0-9_.-]{1,80}$') { throw "Invalid request id '$id'." }

    $action = ([string]$request.action).ToLowerInvariant()
    if ($action -notin @('build', 'test', 'verify', 'pack')) { throw "Invalid action '$action' (allowed: build, test, verify, pack)." }

    $configuration = if ($request.configuration) { [string]$request.configuration } else { 'Debug' }
    if ($configuration -notin @('Debug', 'Release')) { throw "Invalid configuration '$configuration'." }
    if ($action -eq 'pack') { $configuration = 'Release' }

    $framework = [string]$request.framework
    if ($framework -and $framework -notmatch '^net[0-9.]+(-windows)?$') { throw "Invalid framework '$framework'." }

    $filter = [string]$request.filter
    if ($filter -and $filter -notmatch '^[A-Za-z0-9_.=~!&|(),* -]{1,300}$') { throw "Invalid test filter." }
    $interactive = [bool]$request.interactive
    if (-not $interactive) {
        $filter = if ($filter) { "($filter)&Category!=Interactive" } else { 'Category!=Interactive' }
    }

    $log = Join-Path $ResultDir "$id.log"
    Set-Content -Path $log -Encoding UTF8 -Value ("Request {0}: action={1} configuration={2} framework={3} filter={4} started={5:o}" -f $id, $action, $configuration, $framework, $filter, (Get-Date))

    $summary = [ordered]@{ id = $id; action = $action; configuration = $configuration; framework = $framework; filter = $filter; started = (Get-Date).ToString('o') }
    if (-not $Dotnet) { throw 'dotnet not found.' }
    Stop-StaleRepoProcesses -Log $log

    if ($action -in @('build', 'verify')) {
        $buildArgs = @('build', "`"$Solution`"", '-c', $configuration, '--nologo', '-v:minimal', '-clp:Summary;ErrorsOnly;WarningsOnly')
        $summary.build_exit_code = Invoke-Step -Name 'build' -Exe $Dotnet -Log $log -Arguments $buildArgs
    }

    if ($action -eq 'test' -or ($action -eq 'verify' -and $summary.build_exit_code -eq 0)) {
        # One trx per target framework: <id>_<tfm>_<timestamp>.trx
        $trx = "$id*.trx"
        Get-ChildItem -Path $ResultDir -Filter "$id*.trx" -File -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue
        $testArgs = @('test', "`"$TestProject`"", '-c', $configuration, '--no-build', '--nologo',
            '--logger', "`"trx;LogFilePrefix=$id`"", '--logger', '"console;verbosity=normal"', '--results-directory', "`"$ResultDir`"",
            '--blame-hang', '--blame-hang-timeout', $TestHangTimeout, '--blame-hang-dump-type', 'none',
            '--filter', "`"$filter`"")
        if ($framework) { $testArgs += @('--framework', $framework) }
        $summary.test_exit_code = Invoke-Step -Name 'test' -Exe $Dotnet -Log $log -Arguments $testArgs
        $summary.trx = $trx
        $summary.tests = @(Get-ChildItem -Path $ResultDir -Filter "$id*.trx" -File -ErrorAction SilentlyContinue | Sort-Object Name | ForEach-Object {
            $counts = Get-TrxCounts -Path $_.FullName
            if ($counts) { $counts.file = $_.Name }
            $counts
        })
    }

    if ($action -eq 'pack') {
        New-Item -ItemType Directory -Force -Path $PackageDir | Out-Null
        $summary.pack_exit_code = Invoke-Step -Name 'pack' -Exe $Dotnet -Log $log -Arguments @(
            'pack', "`"$Solution`"", '-c', 'Release', '--nologo', '-v:minimal', '-o', "`"$PackageDir`"")
    }

    $summary.finished = (Get-Date).ToString('o')
    $summary.success = (($null -eq $summary.build_exit_code) -or ($summary.build_exit_code -eq 0)) -and
                       (($null -eq $summary.test_exit_code) -or ($summary.test_exit_code -eq 0)) -and
                       (($null -eq $summary.pack_exit_code) -or ($summary.pack_exit_code -eq 0)) -and
                       -not ($action -eq 'verify' -and $null -eq $summary.test_exit_code)
    return $summary
}

Write-Host "Dapplo.Windows build runner watching $RequestDir (dotnet: $Dotnet). Ctrl+C to stop."

$ScriptPath = $PSCommandPath
$ScriptVersion = (Get-Item $ScriptPath).LastWriteTimeUtc
Remove-Item -Path $CancelFile -Force -ErrorAction SilentlyContinue

while ($true) {
    # Restart with the new version when this script was updated
    if ((Get-Item $ScriptPath).LastWriteTimeUtc -ne $ScriptVersion) {
        Write-Host 'Runner script changed, restarting...'
        & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $ScriptPath -PollSeconds $PollSeconds -TimeoutMinutes $TimeoutMinutes -TestHangTimeout $TestHangTimeout
        exit $LASTEXITCODE
    }

    foreach ($file in Get-ChildItem -Path $RequestDir -Filter '*.json' -File | Sort-Object LastWriteTime) {
        $processing = "$($file.FullName).processing"
        try { Move-Item -Path $file.FullName -Destination $processing -Force } catch { continue }
        $resultName = [IO.Path]::GetFileNameWithoutExtension($file.Name)
        try {
            Write-Host ("[{0:HH:mm:ss}] Running {1}" -f (Get-Date), $file.Name)
            $summary = Invoke-Request -File $processing
            $resultName = $summary.id
        } catch {
            $summary = [ordered]@{ id = $resultName; success = $false; error = $_.Exception.Message; finished = (Get-Date).ToString('o') }
        }
        $json = $summary | ConvertTo-Json -Depth 5
        $tmp = Join-Path $ResultDir "$resultName.json.tmp"
        Set-Content -Path $tmp -Encoding UTF8 -Value $json
        Move-Item -Path $tmp -Destination (Join-Path $ResultDir "$resultName.json") -Force
        Remove-Item -Path $processing -Force -ErrorAction SilentlyContinue
        Write-Host ("[{0:HH:mm:ss}] Finished {1}: success={2}" -f (Get-Date), $resultName, $summary.success)
    }
    Start-Sleep -Seconds $PollSeconds
}
