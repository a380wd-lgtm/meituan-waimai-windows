<#
.SYNOPSIS
独立验证 Codex launcher 的外部契约，不调用真实模型。
.DESCRIPTION
测试运行真实 launcher，只替换费用高且不可控的 Codex 外部进程。
所有临时项目、假程序及 JSON 报告均保留于 OutputRoot，便于复查。
#>
[CmdletBinding()]
param(
    [string]$LauncherPath = '',
    [string]$OutputRoot = (Join-Path $env:TEMP 'ai-collab-harness'),
    [string]$CasePattern = '*'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($LauncherPath)) {
    $LauncherPath = Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) 'Invoke-CodexWorker.ps1'
}
$script:Utf8 = New-Object System.Text.UTF8Encoding($false)
$script:RunRoot = Join-Path ([IO.Path]::GetFullPath($OutputRoot)) ('run-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [guid]::NewGuid().ToString('N').Substring(0, 8))
[void][IO.Directory]::CreateDirectory($script:RunRoot)
$script:Cases = New-Object System.Collections.Generic.List[object]
$script:ReportPath = Join-Path $script:RunRoot 'report.json'
$script:StartedAt = [DateTime]::UtcNow.ToString('o')
$script:LauncherHash = $null
$script:ExecutionLauncher = $null
$script:Handles = New-Object System.Collections.Generic.List[object]

function Write-JsonFile {
    param([string]$Path, [object]$Value)
    [IO.File]::WriteAllText($Path, ($Value | ConvertTo-Json -Depth 20), $script:Utf8)
}

function Save-Report {
    param([string]$Status, [string]$Reason = '')
    $failed = @($script:Cases | Where-Object { $_.status -eq 'failed' }).Count
    Write-JsonFile $script:ReportPath ([ordered]@{
        schema_version = 1; status = $Status; started_at = $script:StartedAt
        completed_at = [DateTime]::UtcNow.ToString('o'); launcher_path = $LauncherPath
        launcher_hash = $script:LauncherHash; launcher_snapshot = $script:ExecutionLauncher
        fake_worker_only = $true; case_pattern = $CasePattern; reason = $Reason; total = $script:Cases.Count
        passed = $script:Cases.Count - $failed; failed = $failed; cases = @($script:Cases.ToArray())
    })
    Write-Host ('JSON 测试报告：' + $script:ReportPath)
}

if (-not (Test-Path -LiteralPath $LauncherPath -PathType Leaf)) {
    Save-Report 'waiting_for_launcher' 'launcher 尚未创建；未执行任何验收，不能视为通过。'
    Write-Host '等待 launcher：尚无可验收的实现。'
    exit 2
}

# 固定本轮被测字节，避免实现方同时编辑原文件而让整轮混跑多个版本。
$script:LauncherHash = (Get-FileHash -LiteralPath $LauncherPath -Algorithm SHA256).Hash
$script:ExecutionLauncher = Join-Path $script:RunRoot 'Invoke-CodexWorker.snapshot.ps1'
[IO.File]::Copy($LauncherPath, $script:ExecutionLauncher, $false)
if ((Get-FileHash -LiteralPath $script:ExecutionLauncher -Algorithm SHA256).Hash -ne $script:LauncherHash -or (Get-FileHash -LiteralPath $LauncherPath -Algorithm SHA256).Hash -ne $script:LauncherHash) {
    Save-Report 'waiting_for_stable_launcher' '复制测试快照时 launcher 已变化；本轮不执行，需重试。'
    exit 2
}

# 假程序故意保持在进程边界：claim、状态机、日志、超时和验证仍由真实 launcher 执行。
$fakeSource = @'
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
public static class ControlledCodex {
    static readonly Encoding Utf8 = new UTF8Encoding(false);
    static void Put(string root, string file, string value) {
        File.WriteAllText(Path.Combine(root, file), value, Utf8);
    }
    public static int Main(string[] args) {
        Console.InputEncoding = Utf8;
        Console.OutputEncoding = Utf8;
        string probe = Environment.GetEnvironmentVariable("COLLAB_TEST_PROBE");
        if (args.Length == 1 && args[0] == "--version") {
            Console.WriteLine("codex-cli 0.0.0-collab-test");
            return 0;
        }
        if (args.Length > 0 && args[0] == "--verify-fail") {
            Put(probe, "verification-ran.txt", String.Join("\n", args));
            return 23;
        }
        if (args.Length > 0 && args[0] == "--verify-ok") {
            Put(probe, "verification-ran.txt", String.Join("\n", args));
            return 0;
        }
        if (args.Length > 0 && args[0] == "--hang-child") {
            Put(probe, "child.pid", Process.GetCurrentProcess().Id.ToString());
            while (true) Thread.Sleep(100);
        }
        string invocation = Process.GetCurrentProcess().Id + "-" + Guid.NewGuid().ToString("N");
        Put(probe, "exec-" + invocation + ".txt", String.Join("\n", args));
        Put(probe, "worker.pid", Process.GetCurrentProcess().Id.ToString());
        if (Environment.GetEnvironmentVariable("COLLAB_TEST_SCENARIO") == "blocked-stdin") {
            // 刻意不读标准输入，要求外层超时覆盖完整输入写入阶段。
            Put(probe, "stdin-not-read.txt", "未读取输入");
            while (true) Thread.Sleep(100);
        }
        if (Environment.GetEnvironmentVariable("COLLAB_TEST_SCENARIO") == "stdin-write-failure") {
            // 完全不消费大提示词，只输出合法完成证据后退出，让写入端确实遭遇断管。
            string earlyFinal = null;
            for (int i = 0; i < args.Length - 1; i++) {
                if (args[i] == "-o" || args[i] == "--output-last-message") earlyFinal = args[i + 1];
            }
            if (earlyFinal == null) return 90;
            Put(probe, "input-skipped.txt", "未读取任何提示词即退出");
            File.WriteAllText(earlyFinal, "提示词未传完，却返回最终回答。", Utf8);
            Console.WriteLine("{\"type\":\"thread.started\",\"thread_id\":\"fake-worker-thread-001\"}");
            Console.WriteLine("{\"type\":\"turn.completed\",\"usage\":{\"input_tokens\":0,\"cached_input_tokens\":0,\"output_tokens\":1}}");
            return 0;
        }
        string prompt = Console.In.ReadToEnd();
        Put(probe, "stdin-" + invocation + ".txt", prompt);
        Match match = Regex.Match(prompt, @"\[CASE:([a-z-]+)\]");
        string scenario = match.Success ? match.Groups[1].Value : "success";
        string final = null;
        for (int i = 0; i < args.Length - 1; i++) {
            if (args[i] == "-o" || args[i] == "--output-last-message") final = args[i + 1];
        }
        if (final == null || Array.IndexOf(args, "exec") < 0) {
            Console.Error.WriteLine("缺少 exec 或最终回答路径参数。");
            return 90;
        }
        if (scenario == "empty-stdout") return 19;
        Console.WriteLine("{\"type\":\"thread.started\",\"thread_id\":\"fake-worker-thread-001\"}");
        Console.WriteLine("{\"type\":\"turn.started\"}");
        if (scenario != "empty-stderr") Console.Error.WriteLine("独立 stderr 证据");
        Console.Out.Flush();
        Console.Error.Flush();
        if (scenario == "live-logs") {
            DateTime deadline = DateTime.UtcNow.AddSeconds(15);
            while (!File.Exists(Path.Combine(probe, "release")) && DateTime.UtcNow < deadline) Thread.Sleep(50);
        }
        if (scenario == "timeout") {
            var psi = new ProcessStartInfo(Assembly.GetExecutingAssembly().Location, "--hang-child");
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            Process.Start(psi);
            Console.Out.Flush();
            while (true) Thread.Sleep(100);
        }
        if (scenario == "orphaned-pipes") {
            var childInfo = new ProcessStartInfo(Assembly.GetExecutingAssembly().Location, "--hang-child");
            childInfo.UseShellExecute = false;
            childInfo.CreateNoWindow = true;
            Process.Start(childInfo);
            DateTime deadline = DateTime.UtcNow.AddSeconds(3);
            while (!File.Exists(Path.Combine(probe, "child.pid")) && DateTime.UtcNow < deadline) Thread.Sleep(20);
            Put(probe, "parent-exiting.txt", "父进程退出，子进程继续持有继承管道");
        }
        if (scenario == "concurrent") Thread.Sleep(1800);
        if (scenario == "nonzero") {
            File.WriteAllText(final, "非零退出仍有回答", Utf8);
            Console.WriteLine("{\"type\":\"turn.completed\",\"usage\":{\"input_tokens\":1,\"cached_input_tokens\":0,\"output_tokens\":1}}");
            return 17;
        }
        File.WriteAllText(final, scenario == "empty-final" ? " \r\n\t" : "中文核验：真实 launcher 的测试回答。", Utf8);
        if (scenario == "turn-failed") Console.WriteLine("{\"type\":\"turn.failed\",\"error\":{\"message\":\"故意注入失败\"}}");
        if (scenario != "missing-completed") Console.WriteLine("{\"type\":\"turn.completed\",\"usage\":{\"input_tokens\":1,\"cached_input_tokens\":0,\"output_tokens\":1}}");
        return 0;
    }
}
'@
$script:FakeExe = Join-Path $script:RunRoot 'fake codex.exe'

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

function Quote-NativeArgument {
    param([string]$Value)
    # 使用 Windows 原生 argv 转义，路径中的空格、引号及末尾反斜杠均作为数据传递。
    return '"' + [regex]::Replace([regex]::Replace($Value, '(\\*)"', '$1$1\"'), '(\\+)$', '$1$1') + '"'
}

function New-Fixture {
    param([string]$Name, [string]$Scenario = 'success')
    $root = Join-Path $script:RunRoot ($Name + ' 中文项目')
    $requests = Join-Path $root '.ai-collab\requests'
    $results = Join-Path $root '.ai-collab\results'
    $probe = Join-Path $root 'probe'
    foreach ($path in @($requests, $results, $probe)) { [void][IO.Directory]::CreateDirectory($path) }
    $task = 'task-' + $Name
    $request = [ordered]@{
        schema_version = 1; project_id = 'soul-windows-research'; task_id = $task
        idempotency_key = $task; reply_to = 'A2'; requester_session_id = 'test-coordinator-session'
        mode = 'review'; worktree_path = $root; owned_files = @(); prompt = "[CASE:$Scenario] 中文提示词，验证标准输入。"
        verification_cmd = $null
    }
    $requestPath = Join-Path $requests ($task + '.json')
    Write-JsonFile $requestPath $request
    Write-JsonFile (Join-Path $root '.ai-collab\binding.json') ([ordered]@{
        schema_version = 1; project_id = 'soul-windows-research'; project_root = $root
        coordinator = 'antigravity-native'; antigravity_session_id = 'test-coordinator-session'
        codex_gui_thread_id = 'test-gui-session-must-not-be-worker'; worker_identity = 'codex-cli-exec'
        default_worker_sandbox = 'read-only'; max_worker_concurrency = 1; default_timeout_seconds = 600
    })
    return [pscustomobject]@{ Name = $Name; Scenario = $Scenario; Root = $root; Probe = $probe; Request = $request; RequestPath = $requestPath; ResultPath = (Join-Path $results ($task + '.json')) }
}

function Start-Launcher {
    param([object]$Fixture, [int]$TimeoutSeconds = 10)
    $token = [guid]::NewGuid().ToString('N')
    $psi = New-Object Diagnostics.ProcessStartInfo
    $psi.FileName = Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
    $argv = @('-NoProfile', '-NonInteractive', '-ExecutionPolicy', 'Bypass', '-File', $script:ExecutionLauncher, '-RequestPath', $Fixture.RequestPath, '-ProjectRoot', $Fixture.Root, '-CodexExe', $script:FakeExe, '-TimeoutSeconds', [string]$TimeoutSeconds)
    $psi.Arguments = ($argv | ForEach-Object { Quote-NativeArgument $_ }) -join ' '
    $psi.UseShellExecute = $false
    $psi.CreateNoWindow = $true
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.EnvironmentVariables['COLLAB_TEST_PROBE'] = $Fixture.Probe
    $psi.EnvironmentVariables['COLLAB_TEST_SCENARIO'] = $Fixture.Scenario
    $process = New-Object Diagnostics.Process
    $process.StartInfo = $psi
    [void]$process.Start()
    $handle = [pscustomobject]@{
        Process = $process; OutputTask = $process.StandardOutput.ReadToEndAsync(); ErrorTask = $process.StandardError.ReadToEndAsync()
        StdoutPath = (Join-Path $Fixture.Probe ("launcher-$token.stdout.txt")); StderrPath = (Join-Path $Fixture.Probe ("launcher-$token.stderr.txt"))
        Completed = $false
    }
    $script:Handles.Add($handle)
    return $handle
}

function Stop-LauncherTree {
    param([object]$Handle)
    try {
        if (-not $Handle.Process.HasExited) {
            & (Join-Path $env:SystemRoot 'System32\taskkill.exe') /PID $Handle.Process.Id /T /F 2>&1 | Out-Null
            [void]$Handle.Process.WaitForExit(3000)
        }
    } catch {
        try { $Handle.Process.Kill(); [void]$Handle.Process.WaitForExit(1000) } catch { }
    }
}

function Complete-Launcher {
    param([object]$Handle, [int]$WaitSeconds = 25)
    $failure = ''; $outputText = ''; $errorText = ''; $code = $null
    try {
        if (-not $Handle.Process.WaitForExit($WaitSeconds * 1000)) {
            $failure = "launcher 超过测试外层时限 $WaitSeconds 秒，已请求终止其进程树。"
            Stop-LauncherTree $Handle
        }
        # 父进程结束不代表管道 EOF；子进程可能继承管道，不能无限等待 Result。
        $streams = [Threading.Tasks.Task[]]@($Handle.OutputTask, $Handle.ErrorTask)
        if (-not [Threading.Tasks.Task]::WaitAll($streams, 2000)) {
            $failure += ' 输出管道在进程退出后仍未关闭。'
        }
        if ($Handle.OutputTask.IsCompleted) { $outputText = $Handle.OutputTask.Result }
        if ($Handle.ErrorTask.IsCompleted) { $errorText = $Handle.ErrorTask.Result }
        if ($Handle.Process.HasExited) { $code = $Handle.Process.ExitCode }
    } finally {
        [IO.File]::WriteAllText($Handle.StdoutPath, $outputText, $script:Utf8)
        [IO.File]::WriteAllText($Handle.StderrPath, $errorText + $failure, $script:Utf8)
        $Handle.Process.Dispose()
        $Handle.Completed = $true
    }
    if ($failure) { throw $failure }
    return [pscustomobject]@{ ExitCode = $code; Stdout = $outputText; Stderr = $errorText; StdoutPath = $Handle.StdoutPath; StderrPath = $Handle.StderrPath }
}

function Invoke-Launcher {
    param([object]$Fixture, [int]$TimeoutSeconds = 10)
    return Complete-Launcher (Start-Launcher $Fixture $TimeoutSeconds)
}

function Read-Result {
    param([object]$Fixture)
    Assert-True (Test-Path -LiteralPath $Fixture.ResultPath -PathType Leaf) ('未生成持久结果：' + $Fixture.ResultPath)
    return [IO.File]::ReadAllText($Fixture.ResultPath, $script:Utf8) | ConvertFrom-Json
}

function Read-LiveText {
    param([string]$Path)
    # 日志仍有写入句柄时，读取端也必须允许共享写入；ReadAllText 默认共享方式过窄。
    $stream = [IO.File]::Open($Path, [IO.FileMode]::Open, [IO.FileAccess]::Read, ([IO.FileShare]::ReadWrite -bor [IO.FileShare]::Delete))
    $reader = New-Object IO.StreamReader($stream, $script:Utf8, $true)
    try { return $reader.ReadToEnd() } finally { $reader.Dispose() }
}

function Get-ExecCount {
    param([object]$Fixture)
    return @(Get-ChildItem -LiteralPath $Fixture.Probe -Filter 'exec-*.txt' -File).Count
}

function Initialize-TestRepository {
    param([object]$Fixture, [switch]$WithCommit)
    # 所有 Git 操作仅针对本轮临时夹具；注册关系测试需要有效 HEAD。
    $git = (Get-Command git.exe -ErrorAction Stop).Source
    & $git -C $Fixture.Root init --quiet
    Assert-True ($LASTEXITCODE -eq 0) '无法初始化临时 Git 夹具。'
    if ($WithCommit) {
        $emptyHooks = Join-Path $script:RunRoot 'empty-git-hooks'
        [void][IO.Directory]::CreateDirectory($emptyHooks)
        [IO.File]::WriteAllText((Join-Path $Fixture.Root 'fixture.txt'), '临时协作测试仓库。', $script:Utf8)
        & $git -C $Fixture.Root add -- fixture.txt
        Assert-True ($LASTEXITCODE -eq 0) '无法暂存夹具文件。'
        & $git -C $Fixture.Root -c 'user.name=Codex Collab Test' -c 'user.email=collab-test@example.invalid' -c 'commit.gpgsign=false' -c "core.hooksPath=$emptyHooks" commit --quiet --no-verify -m '初始化协作测试夹具'
        Assert-True ($LASTEXITCODE -eq 0) '无法提交临时 Git 夹具。'
    }
}

function Assert-Failed {
    param([object]$Fixture, [object]$Run, [string]$Status = 'failed')
    Assert-True ($Run.ExitCode -ne 0) '失败任务错误地返回了退出码 0。'
    $result = Read-Result $Fixture
    Assert-True ($result.status -eq $Status) ("期望状态 $Status，实际为 " + $result.status)
    Assert-True (-not [string]::IsNullOrWhiteSpace([string]$result.failure_reason)) '失败结果未记录原因。'
    return $result
}

function Assert-Rejected {
    param([object]$Run, [string]$ReasonPattern)
    Assert-True ($Run.ExitCode -ne 0) '不合法请求未以非零退出。'
    $response = $Run.Stdout.Trim() | ConvertFrom-Json
    Assert-True ($response.status -notin @('running', 'succeeded')) '拒绝请求却返回运行或成功状态。'
    Assert-True (-not [string]::IsNullOrWhiteSpace($response.failure_reason)) '拒绝响应缺少明确原因。'
    Assert-True ($response.failure_reason -match $ReasonPattern) ('拒绝原因未指向本次非法输入：' + $response.failure_reason)
}

function Stop-FixtureWorkers {
    param([object]$Fixture)
    # 只回收夹具记录且可执行文件确实属于本轮的进程，保留全部失败证据。
    foreach ($pidFile in @('worker.pid','child.pid')) {
        $path = Join-Path $Fixture.Probe $pidFile
        if (Test-Path -LiteralPath $path) {
            $recordedPid = [int][IO.File]::ReadAllText($path)
            $remaining = Get-Process -Id $recordedPid -ErrorAction SilentlyContinue
            if ($null -ne $remaining -and $remaining.Path -eq $script:FakeExe) {
                Stop-Process -Id $recordedPid -Force -ErrorAction SilentlyContinue
                [void]$remaining.WaitForExit(2000)
            }
        }
    }
}

function Assert-FixtureWorkerStopped {
    param([object]$Fixture, [string]$PidFile)
    $path = Join-Path $Fixture.Probe $PidFile
    Assert-True (Test-Path -LiteralPath $path) ("夹具未产生 $PidFile，不能证明该进程已回收。")
    $recordedPid = [int][IO.File]::ReadAllText($path)
    Assert-True ($null -eq (Get-Process -Id $recordedPid -ErrorAction SilentlyContinue)) ("launcher 返回后进程 $recordedPid 仍存在（$PidFile）。")
}

function Invoke-Case {
    param([string]$Name, [string]$BreakCaught, [scriptblock]$Body)
    if ($Name -notlike $CasePattern) { return }
    $clock = [Diagnostics.Stopwatch]::StartNew()
    $status = 'passed'; $detail = ''
    try { & $Body } catch { $status = 'failed'; $detail = $_.Exception.Message } finally {
        # 一个并发执行者失败时仍回收另一个；夹具异常不得留下 launcher。
        foreach ($handle in $script:Handles) {
            if (-not $handle.Completed) {
                Stop-LauncherTree $handle
                try { $null = Complete-Launcher $handle 1 } catch { $status = 'failed'; $detail += ' 清理：' + $_.Exception.Message }
            }
        }
    }
    $clock.Stop()
    $script:Cases.Add([pscustomobject]@{ name = $Name; status = $status; detects = $BreakCaught; duration_ms = $clock.ElapsedMilliseconds; detail = $detail })
    Write-Host ("[$status] $Name" + $(if ($detail) { ': ' + $detail } else { '' }))
}

try {
    Add-Type -TypeDefinition $fakeSource -OutputAssembly $script:FakeExe -OutputType ConsoleApplication
    Invoke-Case 'success-evidence' '只凭退出码判断成功，或缺失身份、日志与 UTF-8 stdin 证据。' {
        $fixture = New-Fixture 'success'
        $run = Invoke-Launcher $fixture
        Assert-True ($run.ExitCode -eq 0) ('成功输入返回非零：' + $run.Stderr + $run.Stdout)
        $result = Read-Result $fixture
        Assert-True ($result.status -eq 'succeeded') ('成功输入状态不正确：' + $result.status)
        foreach ($field in @('task_id','idempotency_key','request_hash','status','exit_code','worker_identity','worker_thread_id','codex_exe','codex_version','worktree_path','started_at','completed_at','summary_file','stdout_log','stderr_log','verification_passed','failure_reason')) {
            Assert-True ($result.PSObject.Properties.Name -contains $field) ("结果缺少字段 $field。")
        }
        Assert-True ($result.worker_thread_id -eq 'fake-worker-thread-001') 'worker_thread_id 未来自 thread.started。'
        Assert-True ($result.task_id -eq $fixture.Request.task_id -and $result.idempotency_key -eq $fixture.Request.task_id) '结果任务身份与请求不符。'
        Assert-True ($result.exit_code -eq 0) '结果退出码不正确。'
        Assert-True ($null -eq $result.verification_passed) '未配置验证命令时必须为 null。'
        Assert-True (-not [string]::IsNullOrWhiteSpace($result.request_hash)) '缺少请求摘要。'
        Assert-True (-not [string]::IsNullOrWhiteSpace($result.worker_identity)) '缺少 worker 身份。'
        Assert-True ($result.codex_version -match '0\.0\.0-collab-test') '未记录实际运行版本。'
        Assert-True ([IO.Path]::GetFullPath($result.codex_exe) -eq $script:FakeExe) '未记录指定可执行文件。'
        Assert-True ([IO.Path]::GetFullPath($result.worktree_path) -eq $fixture.Root) '执行目录不正确。'
        foreach ($field in @('summary_file','stdout_log','stderr_log')) {
            Assert-True (Test-Path -LiteralPath $result.$field -PathType Leaf) ("证据文件不存在：$field")
        }
        Assert-True ([IO.File]::ReadAllText($result.summary_file, $script:Utf8) -match '中文核验') '最终回答缺失或编码损坏。'
        $events = @([IO.File]::ReadAllLines($result.stdout_log, $script:Utf8) | Where-Object { $_.Trim() } | ForEach-Object { $_ | ConvertFrom-Json })
        Assert-True ($events.type -contains 'turn.completed') 'stdout 日志未保存完成事件。'
        Assert-True ([IO.File]::ReadAllText($result.stderr_log, $script:Utf8) -match 'stderr') 'stderr 未独立保存。'
        $stdin = @(Get-ChildItem -LiteralPath $fixture.Probe -Filter 'stdin-*.txt' -File)
        Assert-True ($stdin.Count -eq 1 -and [IO.File]::ReadAllText($stdin[0].FullName, $script:Utf8).Contains('中文提示词')) '提示词未通过 UTF-8 stdin 完整传入。'
        $workerArgs = [IO.File]::ReadAllLines(@(Get-ChildItem -LiteralPath $fixture.Probe -Filter 'exec-*.txt' -File)[0].FullName)
        Assert-True ($workerArgs -contains 'read-only') 'review 未指定只读沙箱。'
        Assert-True ($workerArgs -contains 'never') 'CLI 未禁用交互审批。'
        Assert-True ($workerArgs -contains $fixture.Root) 'CLI 未接收正确工作目录。'
        Assert-True ($workerArgs[-1] -eq '-') 'CLI 未使用 stdin 提示词入口。'
        $published = $run.Stdout.Trim() | ConvertFrom-Json
        Assert-True ($published.status -eq 'succeeded') 'stdout 未返回结果 JSON。'
    }
    Invoke-Case 'duplicate-preserves-result' '相同请求重复启动 worker，或改写已有成功结果。' {
        $fixture = New-Fixture 'duplicate'
        $first = Invoke-Launcher $fixture
        Assert-True ($first.ExitCode -eq 0) '首次请求未成功。'
        $before = [IO.File]::ReadAllText($fixture.ResultPath)
        $result = Read-Result $fixture
        $summaryBefore = [IO.File]::ReadAllText($result.summary_file)
        $second = Invoke-Launcher $fixture
        Assert-True ($second.ExitCode -eq 0) '成功任务重试必须返回 0。'
        Assert-True ((Get-ExecCount $fixture) -eq 1) '重复请求再次启动了 worker。'
        Assert-True ([IO.File]::ReadAllText($fixture.ResultPath) -ceq $before) '重复请求覆盖了原始结果。'
        Assert-True ([IO.File]::ReadAllText($result.summary_file) -ceq $summaryBefore) '重复请求改写了最终回答。'
        Assert-True (($second.Stdout.Trim() | ConvertFrom-Json).status -eq 'succeeded') '重试未返回原成功状态。'
    }
    Invoke-Case 'same-id-different-request' '相同 task_id 携带不同请求被误当成合法重试。' {
        $fixture = New-Fixture 'conflict'
        $first = Invoke-Launcher $fixture
        Assert-True ($first.ExitCode -eq 0) '首次请求未成功。'
        $before = [IO.File]::ReadAllText($fixture.ResultPath)
        $fixture.Request.prompt = '[CASE:success] 已变更的请求。'
        Write-JsonFile $fixture.RequestPath $fixture.Request
        $second = Invoke-Launcher $fixture
        Assert-Rejected $second 'hash|conflict|different|冲突|不同|摘要'
        Assert-True ((Get-ExecCount $fixture) -eq 1) '冲突请求启动了第二次 worker。'
        Assert-True ([IO.File]::ReadAllText($fixture.ResultPath) -ceq $before) '冲突请求覆盖了原结果。'
    }
    foreach ($scenario in @('nonzero','empty-final','missing-completed','turn-failed','empty-stdout')) {
        $caseScenario = $scenario
        Invoke-Case $caseScenario '缺失成功条件或出现失败事件时仍误报 succeeded。' {
            $fixture = New-Fixture $caseScenario $caseScenario
            $result = Assert-Failed $fixture (Invoke-Launcher $fixture)
            if ($caseScenario -eq 'nonzero') { Assert-True ($result.exit_code -eq 17) '未保留 worker 非零退出码 17。' }
        }
    }
    Invoke-Case 'empty-stderr-success' '正常空 stderr 触发参数绑定异常而丢失终态。' {
        $fixture = New-Fixture 'empty-stderr' 'empty-stderr'
        $run = Invoke-Launcher $fixture
        Assert-True ($run.ExitCode -eq 0) '正常空 stderr 未成功。'
        $result = Read-Result $fixture
        Assert-True ($result.status -eq 'succeeded') '正常空 stderr 未持久化 succeeded。'
        Assert-True ((Get-Item -LiteralPath $result.stderr_log).Length -eq 0) '空 stderr 未保留为空日志。'
    }
    Invoke-Case 'verification-nonzero' '验证命令失败仍标记成功，或未执行结构化验证命令。' {
        $fixture = New-Fixture 'verify-fail'
        $fixture.Request.verification_cmd = @{ executable = $script:FakeExe; args = @('--verify-fail', '带 空格的参数') }
        Write-JsonFile $fixture.RequestPath $fixture.Request
        $result = Assert-Failed $fixture (Invoke-Launcher $fixture)
        Assert-True ($result.verification_passed -eq $false) '验证失败未记录 false。'
        Assert-True (Test-Path -LiteralPath (Join-Path $fixture.Probe 'verification-ran.txt')) '验证命令没有执行。'
    }
    Invoke-Case 'verification-success' '验证通过未记录 true，或破坏结构化参数边界。' {
        $fixture = New-Fixture 'verify-ok'
        $fixture.Request.verification_cmd = @{ executable = $script:FakeExe; args = @('--verify-ok', '带 空格的参数', '引号"以及末尾\') }
        Write-JsonFile $fixture.RequestPath $fixture.Request
        $run = Invoke-Launcher $fixture
        Assert-True ($run.ExitCode -eq 0) '验证通过任务未成功。'
        $result = Read-Result $fixture
        Assert-True ($result.status -eq 'succeeded' -and $result.verification_passed -eq $true) '验证通过证据不正确。'
        $argsSeen = [IO.File]::ReadAllLines((Join-Path $fixture.Probe 'verification-ran.txt'), $script:Utf8)
        Assert-True ($argsSeen.Count -eq 3 -and $argsSeen[1] -ceq '带 空格的参数' -and $argsSeen[2] -ceq '引号"以及末尾\') '结构化验证参数被错误拆分或转义。'
    }
    Invoke-Case 'timeout-kills-process-tree' '超时只终止父进程，或在子进程仍活跃时宣称已停止。' {
        $fixture = New-Fixture 'timeout' 'timeout'
        try {
            $result = Assert-Failed $fixture (Invoke-Launcher $fixture 2) 'timed_out'
            foreach ($pidFile in @('worker.pid','child.pid')) {
                $path = Join-Path $fixture.Probe $pidFile
                Assert-True (Test-Path -LiteralPath $path) ("超时夹具未启动 $pidFile，无法证明进程树终止。")
                $recordedPid = [int][IO.File]::ReadAllText($path)
                Assert-True ($null -eq (Get-Process -Id $recordedPid -ErrorAction SilentlyContinue)) ("超时返回后进程 $recordedPid 仍存在（$pidFile）。")
            }
        } finally {
            # 仅清理测试替身的确切 PID；故障时保留所有磁盘证据。
            foreach ($pidFile in @('worker.pid','child.pid')) {
                $path = Join-Path $fixture.Probe $pidFile
                if (Test-Path -LiteralPath $path) {
                    $recordedPid = [int][IO.File]::ReadAllText($path)
                    $remaining = Get-Process -Id $recordedPid -ErrorAction SilentlyContinue
                    if ($null -ne $remaining -and $remaining.Path -eq $script:FakeExe) { Stop-Process -Id $recordedPid -Force -ErrorAction SilentlyContinue }
                }
            }
        }
    }
    Invoke-Case 'concurrent-single-claim' '两个同时提交的相同任务均启动 worker。' {
        $fixture = New-Fixture 'concurrent' 'concurrent'
        $one = Start-Launcher $fixture
        $two = Start-Launcher $fixture
        $runs = @((Complete-Launcher $one), (Complete-Launcher $two))
        foreach ($run in $runs) {
            $response = $run.Stdout.Trim() | ConvertFrom-Json
            Assert-True (-not [string]::IsNullOrWhiteSpace($response.status)) '并发请求未返回明确 JSON 状态。'
            if ($response.status -notin @('running','succeeded')) { Assert-True (-not [string]::IsNullOrWhiteSpace($response.failure_reason)) '并发请求的拒绝或失败缺少原因。' }
        }
        Assert-True ((Get-ExecCount $fixture) -eq 1) ('并发请求实际启动次数：' + (Get-ExecCount $fixture))
        Assert-True (@($runs | Where-Object { $_.ExitCode -eq 0 }).Count -ge 1) '并发请求没有成功执行者。'
        Assert-True ((Read-Result $fixture).status -eq 'succeeded') '并发结果未保留成功状态。'
    }
    Invoke-Case 'running-logs-visible' '运行中仅缓存输出，完成前无法查看当前 PID 与日志。' {
        $fixture = New-Fixture 'live-logs' 'live-logs'
        $handle = Start-Launcher $fixture 20
        $ready = $false
        try {
            $deadline = [DateTime]::UtcNow.AddSeconds(5)
            while ([DateTime]::UtcNow -lt $deadline -and -not $handle.Process.HasExited) {
                if (Test-Path -LiteralPath $fixture.ResultPath) {
                    $running = Read-Result $fixture
                    $fields = $running.PSObject.Properties.Name
                    if ($running.status -eq 'running' -and $fields -contains 'worker_pid' -and $fields -contains 'stdout_log' -and $fields -contains 'stderr_log') {
                        if ($running.worker_pid -gt 0 -and (Test-Path -LiteralPath $running.stdout_log) -and (Test-Path -LiteralPath $running.stderr_log)) {
                            $outNow = Read-LiveText $running.stdout_log
                            $errNow = Read-LiveText $running.stderr_log
                            if ($outNow.Contains('thread.started') -and $errNow.Contains('stderr')) { $ready = $true; break }
                        }
                    }
                }
                Start-Sleep -Milliseconds 100
            }
            Assert-True $ready 'worker 仍运行时，running 结果未提供 PID、stdout/stderr 路径与已落盘首批日志。'
            Assert-True ($null -ne (Get-Process -Id ([int]$running.worker_pid) -ErrorAction SilentlyContinue)) 'running 中的 worker_pid 不对应当前进程。'
            Assert-True (-not $handle.Process.HasExited) '日志可读时任务已退出，不能证明运行中可见。'
        } finally {
            [IO.File]::WriteAllText((Join-Path $fixture.Probe 'release'), '继续', $script:Utf8)
            $run = Complete-Launcher $handle
        }
        Assert-True ($run.ExitCode -eq 0) '实时日志测试释放 worker 后未成功完成。'
    }
    Invoke-Case 'task-id-path-traversal' 'task_id 路径穿越进入结果或 claim 路径。' {
        $fixture = New-Fixture 'traversal'
        $fixture.Request.task_id = '../escaped'
        $fixture.Request.idempotency_key = '../escaped'
        Write-JsonFile $fixture.RequestPath $fixture.Request
        $run = Invoke-Launcher $fixture
        Assert-Rejected $run 'task.?id|任务.?ID'
        Assert-True ((Get-ExecCount $fixture) -eq 0) '非法 task_id 启动了 worker。'
        Assert-True (@(Get-ChildItem -LiteralPath $fixture.Root -Recurse -File -Filter 'escaped*').Count -eq 0) '非法 task_id 造成路径穿越写入。'
    }
    Invoke-Case 'implement-rejects-main-checkout' 'implement 能在双方共用的主检出中写入。' {
        $fixture = New-Fixture 'implement-main'
        Initialize-TestRepository $fixture
        $fixture.Request.mode = 'implement'
        Write-JsonFile $fixture.RequestPath $fixture.Request
        $run = Invoke-Launcher $fixture
        Assert-Rejected $run 'implement|worktree|工作树|主目录|主检出|隔离'
        Assert-True ((Get-ExecCount $fixture) -eq 0) 'implement 主目录启动了 worker。'
    }
    Invoke-Case 'implement-rejects-main-subdirectory' 'implement 通过主检出的子目录绕过工作树隔离。' {
        $fixture = New-Fixture 'implement-subdir'
        Initialize-TestRepository $fixture
        $subdirectory = Join-Path $fixture.Root '业务 子目录'
        [void][IO.Directory]::CreateDirectory($subdirectory)
        $fixture.Request.mode = 'implement'
        $fixture.Request.worktree_path = $subdirectory
        Write-JsonFile $fixture.RequestPath $fixture.Request
        $run = Invoke-Launcher $fixture
        Assert-Rejected $run 'implement|worktree|工作树|主目录|主检出|隔离'
        Assert-True ((Get-ExecCount $fixture) -eq 0) 'implement 主检出子目录启动了 worker。'
    }
    Invoke-Case 'regression-blocked-stdin-timeout' '大提示词同步写入遇不读取 stdin 的 worker 时，绕过任务超时永久阻塞。' {
        $fixture = New-Fixture 'blocked-stdin' 'blocked-stdin'
        $fixture.Request.prompt = '[CASE:blocked-stdin] ' + ('x' * (2 * 1024 * 1024))
        Write-JsonFile $fixture.RequestPath $fixture.Request
        try {
            $run = Complete-Launcher (Start-Launcher $fixture 2) 8
            $result = Assert-Failed $fixture $run 'timed_out'
            Assert-True (Test-Path -LiteralPath (Join-Path $fixture.Probe 'stdin-not-read.txt')) '夹具未到达不读取 stdin 的分支。'
            Assert-FixtureWorkerStopped $fixture 'worker.pid'
        } finally { Stop-FixtureWorkers $fixture }
    }
    Invoke-Case 'regression-parent-exit-held-pipes' '父 worker 已退出而子进程保留输出管道时，无界等待 EOF 或误报完全回收。' {
        $fixture = New-Fixture 'orphaned-pipes' 'orphaned-pipes'
        try {
            $run = Complete-Launcher (Start-Launcher $fixture 2) 8
            Assert-True (Test-Path -LiteralPath (Join-Path $fixture.Probe 'parent-exiting.txt')) '夹具未到达父进程退出分支。'
            Assert-FixtureWorkerStopped $fixture 'worker.pid'
            $result = Read-Result $fixture
            $childPid = [int][IO.File]::ReadAllText((Join-Path $fixture.Probe 'child.pid'))
            $childAlive = $null -ne (Get-Process -Id $childPid -ErrorAction SilentlyContinue)
            if ($result.status -eq 'succeeded') {
                # 允许在时限内确实清理完整进程树、回收输出并满足所有成功门禁的实现。
                Assert-True ($run.ExitCode -eq 0 -and -not $childAlive) '成功结果仍遗留子进程，或 launcher 退出码非零。'
                if ($result.PSObject.Properties.Name -contains 'process_tree_terminated') {
                    Assert-True ($result.process_tree_terminated -eq $true) '成功结果却未确认进程树终止。'
                }
                Assert-True (-not [string]::IsNullOrWhiteSpace([IO.File]::ReadAllText($result.summary_file, $script:Utf8))) '成功结果没有非空 final。'
                $events = @([IO.File]::ReadAllLines($result.stdout_log, $script:Utf8) | Where-Object { $_.Trim() } | ForEach-Object { $_ | ConvertFrom-Json })
                Assert-True ($events.type -contains 'turn.completed' -and $events.type -notcontains 'turn.failed') '成功结果没有完整且有效的完成事件证据。'
            } else {
                Assert-True ($result.status -in @('failed','timed_out')) '遗留子进程管道任务仍处于 running 或未知状态。'
                Assert-True ($run.ExitCode -ne 0 -and -not [string]::IsNullOrWhiteSpace($result.failure_reason)) '管道回收失败未给出明确非零失败原因。'
                if ($childAlive) {
                    Assert-True ($result.PSObject.Properties.Name -contains 'process_tree_terminated' -and $result.process_tree_terminated -eq $false) '子进程仍存活却未明确报告进程树终止失败。'
                }
            }
        } finally { Stop-FixtureWorkers $fixture }
    }
    Invoke-Case 'regression-implement-unregistered-worktree' '仅检查外部目录存在 .git，即把伪造标记或无关仓库当作已注册独立工作树。' {
        $failures = New-Object System.Collections.Generic.List[string]
        foreach ($kind in @('forged-marker','unrelated-repository')) {
            $fixture = New-Fixture ('unregistered-' + $kind)
            Initialize-TestRepository $fixture -WithCommit
            $external = Join-Path $script:RunRoot ('外部目录 ' + $kind)
            [void][IO.Directory]::CreateDirectory($external)
            if ($kind -eq 'forged-marker') {
                [IO.File]::WriteAllText((Join-Path $external '.git'), 'gitdir: Z:\不存在的仓库', $script:Utf8)
            } else {
                Initialize-TestRepository ([pscustomobject]@{ Root = $external }) -WithCommit
            }
            $fixture.Request.mode = 'implement'
            $fixture.Request.worktree_path = $external
            Write-JsonFile $fixture.RequestPath $fixture.Request
            try {
                $run = Invoke-Launcher $fixture
                Assert-Rejected $run 'implement|worktree|repository|git|工作树|仓库|隔离'
                Assert-True ((Get-ExecCount $fixture) -eq 0) '未注册工作树启动了 worker。'
            } catch { $failures.Add($kind + ': ' + $_.Exception.Message) }
        }
        Assert-True ($failures.Count -eq 0) ($failures -join '；')
    }
    Invoke-Case 'implement-registered-worktree-success' '把合法已注册的独立 Git 工作树一并拒绝，或未使用正确写入沙箱和执行目录。' {
        $fixture = New-Fixture 'registered-worktree'
        Initialize-TestRepository $fixture -WithCommit
        $worktree = Join-Path $script:RunRoot '已注册 独立工作树'
        $git = (Get-Command git.exe -ErrorAction Stop).Source
        $emptyHooks = Join-Path $script:RunRoot 'empty-git-hooks'
        & $git -C $fixture.Root -c "core.hooksPath=$emptyHooks" worktree add --quiet --detach $worktree HEAD
        Assert-True ($LASTEXITCODE -eq 0) '无法创建已注册独立工作树夹具。'
        $fixture.Request.mode = 'implement'
        $fixture.Request.worktree_path = $worktree
        $fixture.Request.verification_cmd = @{ executable = $script:FakeExe; args = @('--verify-ok') }
        Write-JsonFile $fixture.RequestPath $fixture.Request
        $run = Invoke-Launcher $fixture
        Assert-True ($run.ExitCode -eq 0) ('合法独立工作树未成功：' + $run.Stdout + $run.Stderr)
        $result = Read-Result $fixture
        Assert-True ($result.status -eq 'succeeded' -and $result.verification_passed -eq $true) '合法工作树未完成或未通过验证。'
        Assert-True ((Get-ExecCount $fixture) -eq 1) '合法工作树未恰好启动一次 worker。'
        Assert-True ([IO.Path]::GetFullPath($result.worktree_path) -eq $worktree) '结果中的独立工作树路径不正确。'
        $argsSeen = [IO.File]::ReadAllLines(@(Get-ChildItem -LiteralPath $fixture.Probe -Filter 'exec-*.txt' -File)[0].FullName)
        Assert-True ($argsSeen -contains 'workspace-write' -and $argsSeen -contains $worktree) '合法工作树未使用 workspace-write 或执行目录有误。'
    }
    Invoke-Case 'regression-stdin-write-failure' 'worker 未消费完整提示词就返回合法完成证据，launcher 吞掉 stdin 断管异常并误报成功。' {
        $fixture = New-Fixture 'stdin-write-failure' 'stdin-write-failure'
        $fixture.Request.prompt = '[CASE:stdin-write-failure] ' + ('x' * (8 * 1024 * 1024))
        Write-JsonFile $fixture.RequestPath $fixture.Request
        try {
            $run = Complete-Launcher (Start-Launcher $fixture 3) 10
            Assert-True (Test-Path -LiteralPath (Join-Path $fixture.Probe 'input-skipped.txt')) '夹具未到达不消费提示词即返回完成的分支。'
            $result = Assert-Failed $fixture $run
            Assert-True ($result.failure_reason -match 'stdin|input|pipe|输入|标准输入|管道') ('失败原因未指向输入写入失败：' + $result.failure_reason)
            Assert-FixtureWorkerStopped $fixture 'worker.pid'
        } finally { Stop-FixtureWorkers $fixture }
    }
} catch {
    Save-Report 'harness_error' $_.Exception.Message
    Write-Host ('测试基础设施错误：' + $_.Exception.Message)
    exit 2
}

if ($script:Cases.Count -eq 0) {
    Save-Report 'no_cases_selected' ('CasePattern 未匹配任何测试：' + $CasePattern)
    exit 2
}
$failed = @($script:Cases | Where-Object { $_.status -eq 'failed' }).Count
Save-Report $(if ($failed -eq 0) { 'passed' } else { 'failed' })
Write-Host ("共 $($script:Cases.Count) 项，失败 $failed 项。")
if ($failed -gt 0) { exit 1 }
exit 0
