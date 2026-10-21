param(
    [Parameter(Mandatory = $true)]
    [string]$RequestPath,

    [Parameter(Mandatory = $true)]
    [string]$ProjectRoot,

    [string]$CodexExe = '',

    [int]$TimeoutSeconds = 600
)

$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

if (-not ('AiCollab.LiveProcessRunner' -as [type])) {
    Add-Type -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AiCollab
{
    public sealed class LiveProcessResult
    {
        public int Pid;
        public bool TimedOut;
        public bool ProcessTreeTerminated;
        public bool StdinSucceeded;
        public string StdinError;
        public bool StreamReadSucceeded;
        public string StreamReadError;
        public int ExitCode;
        public string StdOut;
        public string StdErr;
    }

    public static class LiveProcessRunner
    {
        private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);
        private const uint TH32CS_SNAPPROCESS = 0x00000002;
        private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

        private sealed class TrackedProcessNode : IDisposable
        {
            public int Pid;
            public Process Handle;
            public IntPtr NativeHandle;
            public DateTime StartTimeUtc;
            public DateTime? ExitedTimeUtc;
            public bool UnverifiableIdentity;

            public bool IsSameAliveProcess()
            {
                if (UnverifiableIdentity || Handle == null || NativeHandle == IntPtr.Zero || NativeHandle == INVALID_HANDLE_VALUE)
                {
                    return false;
                }
                try
                {
                    if (Handle.HasExited)
                    {
                        if (!ExitedTimeUtc.HasValue)
                        {
                            try { ExitedTimeUtc = Handle.ExitTime.ToUniversalTime(); }
                            catch { ExitedTimeUtc = DateTime.UtcNow; }
                        }
                        return false;
                    }
                    DateTime currentStart = Handle.StartTime.ToUniversalTime();
                    if (currentStart != StartTimeUtc)
                    {
                        UnverifiableIdentity = true;
                        return false;
                    }
                    return true;
                }
                catch
                {
                    UnverifiableIdentity = true;
                    return false;
                }
            }

            public void Dispose()
            {
                if (Handle != null)
                {
                    try { Handle.Dispose(); } catch { }
                    Handle = null;
                }
                NativeHandle = IntPtr.Zero;
            }
        }

        private sealed class StreamPumpResult
        {
            public string Text;
            public string Error;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct PROCESSENTRY32
        {
            public uint dwSize;
            public uint cntUsage;
            public uint th32ProcessID;
            public IntPtr th32DefaultHeapID;
            public uint th32ModuleID;
            public uint cntThreads;
            public uint th32ParentProcessID;
            public int pcPriClassBase;
            public uint dwFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szExeFile;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateToolhelp32Snapshot(uint dwFlags, uint th32ProcessID);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool Process32First(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool Process32Next(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        private static void CollectDescendants(Dictionary<int, TrackedProcessNode> trackedNodes)
        {
            foreach (var kv in trackedNodes)
            {
                kv.Value.IsSameAliveProcess();
            }

            IntPtr snap = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
            if (snap == IntPtr.Zero || snap == INVALID_HANDLE_VALUE) return;
            try
            {
                var entries = new List<KeyValuePair<int, int>>();
                PROCESSENTRY32 pe = new PROCESSENTRY32();
                pe.dwSize = (uint)Marshal.SizeOf(typeof(PROCESSENTRY32));
                if (Process32First(snap, ref pe))
                {
                    do
                    {
                        entries.Add(new KeyValuePair<int, int>((int)pe.th32ProcessID, (int)pe.th32ParentProcessID));
                    } while (Process32Next(snap, ref pe));
                }

                bool added;
                do
                {
                    added = false;
                    for (int i = 0; i < entries.Count; i++)
                    {
                        int childPid = entries[i].Key;
                        int parentPid = entries[i].Value;
                        if (childPid <= 0 || trackedNodes.ContainsKey(childPid)) continue;

                        TrackedProcessNode parentNode;
                        if (!trackedNodes.TryGetValue(parentPid, out parentNode)) continue;
                        if (parentNode.UnverifiableIdentity) continue;

                        Process childProc = null;
                        try
                        {
                            childProc = Process.GetProcessById(childPid);
                            IntPtr childNativeHandle = childProc.Handle;
                            DateTime childStartUtc = childProc.StartTime.ToUniversalTime();
                            if (childStartUtc < parentNode.StartTimeUtc)
                            {
                                childProc.Dispose();
                                continue;
                            }
                            if (parentNode.ExitedTimeUtc.HasValue && childStartUtc > parentNode.ExitedTimeUtc.Value)
                            {
                                childProc.Dispose();
                                continue;
                            }
                            DateTime? childExitedUtc = null;
                            if (childProc.HasExited)
                            {
                                try { childExitedUtc = childProc.ExitTime.ToUniversalTime(); }
                                catch { childExitedUtc = DateTime.UtcNow; }
                            }
                            trackedNodes[childPid] = new TrackedProcessNode
                            {
                                Pid = childPid,
                                Handle = childProc,
                                NativeHandle = childNativeHandle,
                                StartTimeUtc = childStartUtc,
                                ExitedTimeUtc = childExitedUtc,
                                UnverifiableIdentity = (childNativeHandle == IntPtr.Zero || childNativeHandle == INVALID_HANDLE_VALUE)
                            };
                            added = true;
                        }
                        catch (ArgumentException)
                        {
                            if (childProc != null)
                            {
                                try { childProc.Dispose(); } catch { }
                            }
                        }
                        catch
                        {
                            if (childProc != null)
                            {
                                try { childProc.Dispose(); } catch { }
                            }
                            trackedNodes[childPid] = new TrackedProcessNode
                            {
                                Pid = childPid,
                                Handle = null,
                                NativeHandle = IntPtr.Zero,
                                StartTimeUtc = DateTime.MinValue,
                                ExitedTimeUtc = null,
                                UnverifiableIdentity = true
                            };
                            added = true;
                        }
                    }
                } while (added);
            }
            catch { }
            finally
            {
                CloseHandle(snap);
            }
        }

        private static bool HasAnyAliveDescendant(Dictionary<int, TrackedProcessNode> trackedNodes, int rootPid)
        {
            CollectDescendants(trackedNodes);
            foreach (var kv in trackedNodes)
            {
                if (kv.Key == rootPid) continue;
                if (kv.Value.IsSameAliveProcess()) return true;
            }
            return false;
        }

        private static bool TerminateTrackedTree(Dictionary<int, TrackedProcessNode> trackedNodes)
        {
            CollectDescendants(trackedNodes);
            bool anyUnverifiable = false;

            foreach (var kv in trackedNodes)
            {
                TrackedProcessNode node = kv.Value;
                if (node.UnverifiableIdentity)
                {
                    anyUnverifiable = true;
                    continue;
                }
                if (node.IsSameAliveProcess())
                {
                    try
                    {
                        node.Handle.Kill();
                    }
                    catch { }
                }
            }

            DateTime verifyDeadline = DateTime.UtcNow.AddMilliseconds(2500);
            while (DateTime.UtcNow < verifyDeadline)
            {
                CollectDescendants(trackedNodes);
                bool anyAlive = false;
                foreach (var kv in trackedNodes)
                {
                    TrackedProcessNode node = kv.Value;
                    if (node.UnverifiableIdentity)
                    {
                        anyUnverifiable = true;
                        continue;
                    }
                    if (node.IsSameAliveProcess())
                    {
                        anyAlive = true;
                        try
                        {
                            node.Handle.Kill();
                        }
                        catch { }
                    }
                }
                if (!anyAlive)
                {
                    return !anyUnverifiable;
                }
                Thread.Sleep(35);
            }

            foreach (var kv in trackedNodes)
            {
                if (kv.Value.UnverifiableIdentity || kv.Value.IsSameAliveProcess())
                {
                    return false;
                }
            }
            return !anyUnverifiable;
        }

        private static Task<StreamPumpResult> PumpStreamAsync(Stream input, string logFilePath)
        {
            return Task.Run(() =>
            {
                using (var ms = new MemoryStream())
                {
                    byte[] buffer = new byte[1024];
                    int read;
                    string pumpErr = null;
                    try
                    {
                        while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            ms.Write(buffer, 0, read);
                            if (!string.IsNullOrEmpty(logFilePath))
                            {
                                bool written = false;
                                Exception lastWriteEx = null;
                                for (int attempt = 0; attempt < 5; attempt++)
                                {
                                    try
                                    {
                                        using (var fs = new FileStream(logFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                                        {
                                            fs.Write(buffer, 0, read);
                                            fs.Flush();
                                        }
                                        written = true;
                                        break;
                                    }
                                    catch (Exception wex)
                                    {
                                        lastWriteEx = wex;
                                        Thread.Sleep(5);
                                    }
                                }
                                if (!written && lastWriteEx != null && pumpErr == null)
                                {
                                    pumpErr = "Failed to append live log: " + lastWriteEx.Message;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        pumpErr = "Stream read error: " + ex.Message;
                    }
                    return new StreamPumpResult
                    {
                        Text = Utf8NoBom.GetString(ms.ToArray()),
                        Error = pumpErr
                    };
                }
            });
        }

        private static StreamPumpResult ReadStreamOrFileFallback(Task<StreamPumpResult> streamTask, string logFilePath, int waitMs)
        {
            try
            {
                if (streamTask != null && streamTask.Wait(waitMs))
                {
                    return streamTask.Result ?? new StreamPumpResult { Text = string.Empty, Error = null };
                }
            }
            catch (Exception ex)
            {
                return new StreamPumpResult { Text = string.Empty, Error = "Stream task faulted: " + ex.Message };
            }
            string fallbackText = string.Empty;
            if (!string.IsNullOrEmpty(logFilePath) && File.Exists(logFilePath))
            {
                try
                {
                    using (var fs = new FileStream(logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var sr = new StreamReader(fs, Utf8NoBom))
                    {
                        fallbackText = sr.ReadToEnd();
                    }
                }
                catch { }
            }
            return new StreamPumpResult
            {
                Text = fallbackText,
                Error = "Stream did not close before drain deadline."
            };
        }

        public static LiveProcessResult Run(
            string filePath,
            string arguments,
            string workingDirectory,
            string stdinText,
            string stdoutLogPath,
            string stderrLogPath,
            int timeoutMs,
            Action<int> onStarted)
        {
            var psi = new ProcessStartInfo
            {
                FileName = filePath,
                Arguments = arguments ?? string.Empty,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            if (!string.IsNullOrWhiteSpace(workingDirectory))
            {
                psi.WorkingDirectory = workingDirectory;
            }
            if (stdinText != null)
            {
                psi.RedirectStandardInput = true;
            }

            if (!string.IsNullOrEmpty(stdoutLogPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(stdoutLogPath));
                using (new FileStream(stdoutLogPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)) { }
            }
            if (!string.IsNullOrEmpty(stderrLogPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(stderrLogPath));
                using (new FileStream(stderrLogPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)) { }
            }

            DateTime deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs > 0 ? timeoutMs : 10000);
            var trackedNodes = new Dictionary<int, TrackedProcessNode>();

            try
            {
                using (var proc = new Process { StartInfo = psi })
                {
                    if (!proc.Start())
                    {
                        throw new InvalidOperationException("Failed to start process: " + filePath);
                    }

                    int pid = proc.Id;
                    DateTime rootStartUtc = DateTime.MinValue;
                    bool rootUnverifiable = false;
                    Process rootDupHandle = null;
                    IntPtr rootNativeHandle = IntPtr.Zero;
                    try
                    {
                        rootDupHandle = Process.GetProcessById(pid);
                        rootNativeHandle = rootDupHandle.Handle;
                        rootStartUtc = rootDupHandle.StartTime.ToUniversalTime();
                        if (rootNativeHandle == IntPtr.Zero || rootNativeHandle == INVALID_HANDLE_VALUE)
                        {
                            rootUnverifiable = true;
                        }
                    }
                    catch
                    {
                        rootUnverifiable = true;
                    }

                    trackedNodes[pid] = new TrackedProcessNode
                    {
                        Pid = pid,
                        Handle = rootDupHandle,
                        NativeHandle = rootNativeHandle,
                        StartTimeUtc = rootStartUtc,
                        ExitedTimeUtc = null,
                        UnverifiableIdentity = rootUnverifiable
                    };
                    CollectDescendants(trackedNodes);

                    if (onStarted != null)
                    {
                        onStarted(pid);
                    }

                    Task<StreamPumpResult> outTask = PumpStreamAsync(proc.StandardOutput.BaseStream, stdoutLogPath);
                    Task<StreamPumpResult> errTask = PumpStreamAsync(proc.StandardError.BaseStream, stderrLogPath);

                    Task<string> inTask = null;
                    if (stdinText != null)
                    {
                        inTask = Task.Run(() =>
                        {
                            try
                            {
                                byte[] inBytes = Utf8NoBom.GetBytes(stdinText);
                                proc.StandardInput.BaseStream.Write(inBytes, 0, inBytes.Length);
                                proc.StandardInput.BaseStream.Flush();
                                try { proc.StandardInput.Close(); } catch { }
                                return (string)null;
                            }
                            catch (Exception ex)
                            {
                                try { proc.StandardInput.Close(); } catch { }
                                return "Standard input (stdin) pipe write failed: " + ex.Message;
                            }
                        });
                    }

                    bool exited = false;
                    while (DateTime.UtcNow < deadline)
                    {
                        CollectDescendants(trackedNodes);
                        if (proc.WaitForExit(40))
                        {
                            exited = true;
                            break;
                        }
                    }

                    if (!exited)
                    {
                        bool treeStopped = TerminateTrackedTree(trackedNodes);
                        StreamPumpResult outPartial = ReadStreamOrFileFallback(outTask, stdoutLogPath, 400);
                        StreamPumpResult errPartial = ReadStreamOrFileFallback(errTask, stderrLogPath, 400);
                        return new LiveProcessResult
                        {
                            Pid = pid,
                            TimedOut = true,
                            ProcessTreeTerminated = treeStopped,
                            StdinSucceeded = false,
                            StdinError = "Process timed out before completion.",
                            StreamReadSucceeded = false,
                            StreamReadError = "Process timed out before completion.",
                            ExitCode = -1,
                            StdOut = outPartial.Text,
                            StdErr = errPartial.Text
                        };
                    }

                    int exitCode = proc.ExitCode;
                    if (trackedNodes.ContainsKey(pid) && !trackedNodes[pid].ExitedTimeUtc.HasValue)
                    {
                        try { trackedNodes[pid].ExitedTimeUtc = proc.ExitTime.ToUniversalTime(); }
                        catch { trackedNodes[pid].ExitedTimeUtc = DateTime.UtcNow; }
                    }
                    CollectDescendants(trackedNodes);

                    int remainingMs = (int)Math.Max(100, Math.Min(400, (deadline - DateTime.UtcNow).TotalMilliseconds));
                    bool streamsFinished = false;
                    try
                    {
                        var waitList = new List<Task> { outTask, errTask };
                        if (inTask != null) waitList.Add(inTask);
                        streamsFinished = Task.WaitAll(waitList.ToArray(), remainingMs);
                    }
                    catch { }

                    bool treeTerminated = true;
                    if (!streamsFinished || HasAnyAliveDescendant(trackedNodes, pid))
                    {
                        treeTerminated = TerminateTrackedTree(trackedNodes);
                    }
                    foreach (var kv in trackedNodes)
                    {
                        if (kv.Value.UnverifiableIdentity)
                        {
                            treeTerminated = false;
                            break;
                        }
                    }

                    bool stdinOk = true;
                    string stdinErr = null;
                    if (inTask != null)
                    {
                        try
                        {
                            if (!inTask.Wait(300))
                            {
                                stdinOk = false;
                                stdinErr = "Standard input (stdin) write did not complete within deadline.";
                            }
                            else if (!string.IsNullOrEmpty(inTask.Result))
                            {
                                stdinOk = false;
                                stdinErr = inTask.Result;
                            }
                        }
                        catch (Exception ex)
                        {
                            stdinOk = false;
                            stdinErr = "Standard input (stdin) write task faulted: " + ex.Message;
                        }
                    }

                    StreamPumpResult stdOutRes = ReadStreamOrFileFallback(outTask, stdoutLogPath, 400);
                    StreamPumpResult stdErrRes = ReadStreamOrFileFallback(errTask, stderrLogPath, 400);
                    bool streamOk = string.IsNullOrEmpty(stdOutRes.Error) && string.IsNullOrEmpty(stdErrRes.Error);
                    string streamErr = !string.IsNullOrEmpty(stdOutRes.Error) ? stdOutRes.Error : stdErrRes.Error;

                    return new LiveProcessResult
                    {
                        Pid = pid,
                        TimedOut = false,
                        ProcessTreeTerminated = treeTerminated,
                        StdinSucceeded = stdinOk,
                        StdinError = stdinErr,
                        StreamReadSucceeded = streamOk,
                        StreamReadError = streamErr,
                        ExitCode = exitCode,
                        StdOut = stdOutRes.Text,
                        StdErr = stdErrRes.Text
                    };
                }
            }
            finally
            {
                foreach (var kv in trackedNodes)
                {
                    kv.Value.Dispose();
                }
            }
        }
    }
}
'@
}

function Write-JsonStdoutAndExit {
    param(
        [Parameter(Mandatory = $true)]$Object,
        [Parameter(Mandatory = $true)][int]$ExitCode
    )
    $json = $Object | ConvertTo-Json -Depth 10
    [Console]::Out.WriteLine($json)
    exit $ExitCode
}

function Write-FileAtomic {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][AllowEmptyString()][string]$Content
    )
    $dir = Split-Path -Parent $Path
    if (-not (Test-Path -LiteralPath $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
    }
    $tmpPath = Join-Path $dir (([System.IO.Path]::GetFileName($Path)) + ".tmp.$PID." + [Guid]::NewGuid().ToString('N'))
    [System.IO.File]::WriteAllText($tmpPath, $Content, $utf8NoBom)
    if (Test-Path -LiteralPath $Path) {
        [System.IO.File]::Replace($tmpPath, $Path, [NullString]::Value)
    } else {
        [System.IO.File]::Move($tmpPath, $Path)
    }
}

function Get-StringSha256 {
    param([Parameter(Mandatory = $true)][AllowEmptyString()][string]$InputText)
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $bytes = $utf8NoBom.GetBytes($InputText)
        $hash = $sha.ComputeHash($bytes)
        return ([BitConverter]::ToString($hash)).Replace('-', '').ToLowerInvariant()
    } finally {
        $sha.Dispose()
    }
}

function Quote-ProcessArgument {
    param([AllowEmptyString()][string]$Arg)
    if ($null -eq $Arg -or $Arg.Length -eq 0) { return '""' }
    if ($Arg -notmatch '[\s"]') { return $Arg }
    $escaped = $Arg -replace '(\\*)"', '$1$1\"'
    $escaped = $escaped -replace '(\\+)$', '$1$1'
    return '"' + $escaped + '"'
}

function Join-ProcessArguments {
    param([string[]]$ArgsList)
    if (-not $ArgsList -or $ArgsList.Count -eq 0) { return '' }
    $quoted = foreach ($a in $ArgsList) { Quote-ProcessArgument -Arg ([string]$a) }
    return ($quoted -join ' ')
}

function Invoke-ProcessCapture {
    param(
        [Parameter(Mandatory = $true)][string]$FilePath,
        [string[]]$ArgumentList = @(),
        [string]$WorkingDirectory = '',
        [string]$StdinText = $null,
        [string]$StdoutLogPath = '',
        [string]$StderrLogPath = '',
        [int]$TimeoutMs = 10000,
        [scriptblock]$OnStarted = $null
    )
    $argStr = Join-ProcessArguments -ArgsList $ArgumentList
    $action = $null
    if ($null -ne $OnStarted) {
        $action = [System.Action[int]]{ param($procPid) & $OnStarted $procPid }
    }
    return [AiCollab.LiveProcessRunner]::Run(
        $FilePath,
        $argStr,
        $WorkingDirectory,
        $StdinText,
        $StdoutLogPath,
        $StderrLogPath,
        $TimeoutMs,
        $action
    )
}

function Resolve-VerifiedCodexExecutable {
    param([string]$ExplicitCodexExe)
    $candidates = New-Object System.Collections.Generic.List[string]
    if (-not [string]::IsNullOrWhiteSpace($ExplicitCodexExe)) {
        if (-not (Test-Path -LiteralPath $ExplicitCodexExe -PathType Leaf)) {
            throw "Explicit -CodexExe does not exist: $ExplicitCodexExe"
        }
        $candidates.Add((Resolve-Path -LiteralPath $ExplicitCodexExe).Path)
    } else {
        $pathCmd = Get-Command 'codex.exe' -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($pathCmd -and $pathCmd.Source -and ([System.IO.Path]::GetFileName($pathCmd.Source) -ieq 'codex.exe')) {
            $candidates.Add($pathCmd.Source)
        }
        $installRoot = Join-Path $env:LOCALAPPDATA 'OpenAI\Codex\bin'
        if (Test-Path -LiteralPath $installRoot -PathType Container) {
            $installed = Get-ChildItem -LiteralPath $installRoot -Recurse -Filter 'codex.exe' -File -ErrorAction SilentlyContinue |
                Sort-Object LastWriteTime -Descending
            foreach ($item in $installed) {
                if (-not $candidates.Contains($item.FullName)) {
                    $candidates.Add($item.FullName)
                }
            }
        }
    }

    foreach ($cand in $candidates) {
        try {
            $verRes = Invoke-ProcessCapture -FilePath $cand -ArgumentList @('--version') -TimeoutMs 8000
            if ((-not $verRes.TimedOut) -and ($verRes.ExitCode -eq 0) -and (-not [string]::IsNullOrWhiteSpace($verRes.StdOut))) {
                return [pscustomobject]@{
                    Executable = $cand
                    Version    = $verRes.StdOut.Trim()
                }
            }
        } catch {
            if (-not [string]::IsNullOrWhiteSpace($ExplicitCodexExe)) {
                throw
            }
        }
    }
    throw "No runnable codex.exe passed '--version' verification."
}

function Test-DistinctGitWorktree {
    param(
        [Parameter(Mandatory = $true)][string]$CandidateWorktree,
        [Parameter(Mandatory = $true)][string]$MainProjectRoot
    )
    $candNorm = [System.IO.Path]::GetFullPath($CandidateWorktree).TrimEnd('\', '/')
    $mainNorm = [System.IO.Path]::GetFullPath($MainProjectRoot).TrimEnd('\', '/')

    if ([string]::Equals($candNorm, $mainNorm, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $false
    }
    if ($candNorm.StartsWith($mainNorm + '\', [System.StringComparison]::OrdinalIgnoreCase) -or
        $mainNorm.StartsWith($candNorm + '\', [System.StringComparison]::OrdinalIgnoreCase)) {
        return $false
    }
    $gitMarker = Join-Path $candNorm '.git'
    if (-not (Test-Path -LiteralPath $gitMarker)) {
        return $false
    }

    try {
        $wtListRes = Invoke-ProcessCapture -FilePath 'git.exe' -ArgumentList @('-c', 'core.quotePath=false', '-C', $mainNorm, 'worktree', 'list', '--porcelain') -TimeoutMs 8000
        if ($wtListRes.TimedOut -or ($wtListRes.ExitCode -ne 0) -or [string]::IsNullOrWhiteSpace($wtListRes.StdOut)) {
            return $false
        }

        $registeredNonMain = $false
        $lines = $wtListRes.StdOut -split "`r?`n"
        foreach ($line in $lines) {
            if ($line.StartsWith('worktree ')) {
                $rawWtPath = $line.Substring(9).Trim()
                if ([string]::IsNullOrWhiteSpace($rawWtPath)) { continue }
                try {
                    $wtNorm = [System.IO.Path]::GetFullPath(($rawWtPath -replace '/', '\')).TrimEnd('\', '/')
                    if ([string]::Equals($wtNorm, $candNorm, [System.StringComparison]::OrdinalIgnoreCase) -and
                        (-not [string]::Equals($wtNorm, $mainNorm, [System.StringComparison]::OrdinalIgnoreCase))) {
                        $registeredNonMain = $true
                        break
                    }
                } catch {}
            }
        }
        if (-not $registeredNonMain) {
            return $false
        }

        $candCheckRes = Invoke-ProcessCapture -FilePath 'git.exe' -ArgumentList @('-c', 'core.quotePath=false', '-C', $candNorm, 'rev-parse', '--show-toplevel') -TimeoutMs 5000
        if ($candCheckRes.TimedOut -or ($candCheckRes.ExitCode -ne 0) -or [string]::IsNullOrWhiteSpace($candCheckRes.StdOut)) {
            return $false
        }
        $candTopNorm = [System.IO.Path]::GetFullPath(($candCheckRes.StdOut.Trim() -replace '/', '\')).TrimEnd('\', '/')
        if (-not [string]::Equals($candTopNorm, $candNorm, [System.StringComparison]::OrdinalIgnoreCase)) {
            return $false
        }
        return $true
    } catch {
        return $false
    }
}

# 1. 参数与请求信封 v1 校验
if ($TimeoutSeconds -le 0) {
    Write-JsonStdoutAndExit -Object ([ordered]@{
        status         = 'invalid_request'
        failure_reason = 'TimeoutSeconds must be positive.'
    }) -ExitCode 2
}

if (-not (Test-Path -LiteralPath $ProjectRoot -PathType Container)) {
    Write-JsonStdoutAndExit -Object ([ordered]@{
        status         = 'invalid_request'
        failure_reason = "ProjectRoot does not exist: $ProjectRoot"
    }) -ExitCode 2
}
$resolvedProjectRoot = [System.IO.Path]::GetFullPath((Resolve-Path -LiteralPath $ProjectRoot).Path).TrimEnd('\', '/')

if (-not (Test-Path -LiteralPath $RequestPath -PathType Leaf)) {
    Write-JsonStdoutAndExit -Object ([ordered]@{
        status         = 'invalid_request'
        failure_reason = "RequestPath does not exist: $RequestPath"
    }) -ExitCode 2
}

$rawRequestJson = [System.IO.File]::ReadAllText((Resolve-Path -LiteralPath $RequestPath).Path, [System.Text.Encoding]::UTF8)
try {
    $req = $rawRequestJson | ConvertFrom-Json
} catch {
    Write-JsonStdoutAndExit -Object ([ordered]@{
        status         = 'invalid_request'
        failure_reason = "Invalid JSON in RequestPath: $($_.Exception.Message)"
    }) -ExitCode 2
}

$taskId = if ($null -ne $req.task_id) { [string]$req.task_id } else { '' }
if ($taskId -notmatch '^[A-Za-z0-9][A-Za-z0-9_-]{0,79}$') {
    Write-JsonStdoutAndExit -Object ([ordered]@{
        task_id        = $taskId
        status         = 'invalid_request'
        failure_reason = 'task_id must match ^[A-Za-z0-9][A-Za-z0-9_-]{0,79}$'
    }) -ExitCode 2
}

$idempotencyKey = if ($null -ne $req.idempotency_key -and -not [string]::IsNullOrWhiteSpace([string]$req.idempotency_key)) {
    [string]$req.idempotency_key
} else {
    $taskId
}
if ($idempotencyKey -cne $taskId) {
    Write-JsonStdoutAndExit -Object ([ordered]@{
        task_id         = $taskId
        idempotency_key = $idempotencyKey
        status          = 'invalid_request'
        failure_reason  = 'In v1 envelope, idempotency_key must equal task_id.'
    }) -ExitCode 2
}

$mode = if ($null -ne $req.mode -and -not [string]::IsNullOrWhiteSpace([string]$req.mode)) { ([string]$req.mode).Trim().ToLowerInvariant() } else { 'review' }
if ($mode -notin @('review', 'contract-test', 'implement')) {
    Write-JsonStdoutAndExit -Object ([ordered]@{
        task_id        = $taskId
        status         = 'invalid_request'
        failure_reason = "Unsupported mode '$mode'. Allowed: review, contract-test, implement."
    }) -ExitCode 2
}

$promptText = if ($null -ne $req.prompt) { [string]$req.prompt } else { '' }
if ([string]::IsNullOrWhiteSpace($promptText)) {
    Write-JsonStdoutAndExit -Object ([ordered]@{
        task_id        = $taskId
        status         = 'invalid_request'
        failure_reason = 'prompt must be non-empty.'
    }) -ExitCode 2
}

$worktreePathRaw = if ($null -ne $req.worktree_path) { [string]$req.worktree_path } else { '' }
$targetWorkDir = $resolvedProjectRoot
$sandboxMode = 'read-only'

if ($mode -eq 'implement') {
    if ([string]::IsNullOrWhiteSpace($worktreePathRaw) -or (-not (Test-Path -LiteralPath $worktreePathRaw -PathType Container))) {
        Write-JsonStdoutAndExit -Object ([ordered]@{
            task_id        = $taskId
            status         = 'invalid_request'
            failure_reason = 'implement mode requires an existing worktree_path.'
        }) -ExitCode 2
    }
    $resolvedWorktree = [System.IO.Path]::GetFullPath((Resolve-Path -LiteralPath $worktreePathRaw).Path).TrimEnd('\', '/')
    if (-not (Test-DistinctGitWorktree -CandidateWorktree $resolvedWorktree -MainProjectRoot $resolvedProjectRoot)) {
        Write-JsonStdoutAndExit -Object ([ordered]@{
            task_id        = $taskId
            status         = 'invalid_request'
            failure_reason = 'implement mode worktree_path must be a distinct Git worktree outside ProjectRoot.'
        }) -ExitCode 2
    }
    $targetWorkDir = $resolvedWorktree
    $sandboxMode = 'workspace-write'
} elseif (-not [string]::IsNullOrWhiteSpace($worktreePathRaw)) {
    if (-not (Test-Path -LiteralPath $worktreePathRaw -PathType Container)) {
        Write-JsonStdoutAndExit -Object ([ordered]@{
            task_id        = $taskId
            status         = 'invalid_request'
            failure_reason = "Specified worktree_path does not exist: $worktreePathRaw"
        }) -ExitCode 2
    }
    $targetWorkDir = [System.IO.Path]::GetFullPath((Resolve-Path -LiteralPath $worktreePathRaw).Path).TrimEnd('\', '/')
}

# 校验结构化 verification_cmd
$verificationSpec = $null
if ($null -ne $req.verification_cmd) {
    if ($req.verification_cmd -is [string] -or [string]::IsNullOrWhiteSpace([string]$req.verification_cmd.executable)) {
        Write-JsonStdoutAndExit -Object ([ordered]@{
            task_id        = $taskId
            status         = 'invalid_request'
            failure_reason = 'verification_cmd must be null or a structured object {executable, args}.'
        }) -ExitCode 2
    }
    $vExe = [string]$req.verification_cmd.executable
    $vArgs = @()
    if ($null -ne $req.verification_cmd.args) {
        foreach ($a in $req.verification_cmd.args) { $vArgs += [string]$a }
    }
    $verificationSpec = [ordered]@{
        executable = $vExe
        args       = $vArgs
    }
}

$ownedFiles = @()
if ($null -ne $req.owned_files) {
    foreach ($f in $req.owned_files) { $ownedFiles += [string]$f }
}

$canonicalVerification = if ($null -ne $verificationSpec) {
    ($verificationSpec.executable + '|' + ($verificationSpec.args -join "`u{001F}"))
} else {
    ''
}
$canonicalPayload = (
    "v1`n" +
    $taskId + "`n" +
    $idempotencyKey + "`n" +
    $mode + "`n" +
    $targetWorkDir.ToLowerInvariant() + "`n" +
    $promptText + "`n" +
    $canonicalVerification + "`n" +
    ($ownedFiles -join "`u{001F}")
)
$requestHash = Get-StringSha256 -InputText $canonicalPayload

# 2. 准备目录与原子 FileMode.CreateNew Claim 抢占
$collabDir = Join-Path $resolvedProjectRoot '.ai-collab'
$claimsDir = Join-Path $collabDir 'claims'
$resultsDir = Join-Path $collabDir 'results'
$runsRootDir = Join-Path $collabDir 'runs'

foreach ($d in @($collabDir, $claimsDir, $resultsDir, $runsRootDir)) {
    if (-not (Test-Path -LiteralPath $d)) {
        New-Item -ItemType Directory -Path $d -Force | Out-Null
    }
}

$claimPath = Join-Path $claimsDir "$taskId.claim.json"
$resultPath = Join-Path $resultsDir "$taskId.json"

$claimedByUs = $false
$claimObj = [ordered]@{
    schema_version  = 1
    task_id         = $taskId
    idempotency_key = $idempotencyKey
    request_hash    = $requestHash
    claimed_at      = (Get-Date).ToUniversalTime().ToString('o')
    pid             = $PID
}
$claimJson = $claimObj | ConvertTo-Json -Depth 5

try {
    $fs = [System.IO.File]::Open($claimPath, [System.IO.FileMode]::CreateNew, [System.IO.FileAccess]::Write, [System.IO.FileShare]::Read)
    try {
        $bytes = $utf8NoBom.GetBytes($claimJson)
        $fs.Write($bytes, 0, $bytes.Length)
        $fs.Flush()
        $claimedByUs = $true
    } finally {
        $fs.Dispose()
    }
} catch [System.IO.IOException] {
    $claimedByUs = $false
}

if (-not $claimedByUs) {
    $existingClaim = $null
    for ($retry = 0; $retry -lt 20; $retry++) {
        try {
            if (Test-Path -LiteralPath $claimPath) {
                $rawClaim = [System.IO.File]::ReadAllText($claimPath, [System.Text.Encoding]::UTF8)
                if (-not [string]::IsNullOrWhiteSpace($rawClaim)) {
                    $existingClaim = $rawClaim | ConvertFrom-Json
                    break
                }
            }
        } catch {}
        Start-Sleep -Milliseconds 25
    }

    if ($null -eq $existingClaim -or [string]::IsNullOrWhiteSpace([string]$existingClaim.request_hash)) {
        Write-JsonStdoutAndExit -Object ([ordered]@{
            task_id        = $taskId
            status         = 'conflict'
            failure_reason = 'Existing claim file is unreadable, incomplete, or stale unknown lock.'
        }) -ExitCode 3
    }

    $hashMatch = ([string]$existingClaim.request_hash -eq $requestHash)
    $keyMatch = ([string]$existingClaim.idempotency_key -eq $idempotencyKey)

    if ((-not $hashMatch) -or (-not $keyMatch)) {
        Write-JsonStdoutAndExit -Object ([ordered]@{
            task_id               = $taskId
            idempotency_key       = $idempotencyKey
            request_hash          = $requestHash
            existing_request_hash = [string]$existingClaim.request_hash
            status                = 'conflict'
            failure_reason        = 'Task ID already claimed with a different request payload or idempotency_key.'
        }) -ExitCode 3
    }

    for ($waitRes = 0; $waitRes -lt 10; $waitRes++) {
        if (Test-Path -LiteralPath $resultPath -PathType Leaf) {
            try {
                $existingResultRaw = [System.IO.File]::ReadAllText($resultPath, [System.Text.Encoding]::UTF8)
                if (-not [string]::IsNullOrWhiteSpace($existingResultRaw)) {
                    $existingResultObj = $existingResultRaw | ConvertFrom-Json
                    if ([string]$existingResultObj.status -eq 'succeeded') {
                        [Console]::Out.WriteLine($existingResultRaw.Trim())
                        exit 0
                    } else {
                        if ([string]::IsNullOrWhiteSpace([string]$existingResultObj.failure_reason)) {
                            $existingResultObj | Add-Member -NotePropertyName 'failure_reason' -NotePropertyValue 'Task is already claimed and running in another worker process.' -Force
                            Write-JsonStdoutAndExit -Object $existingResultObj -ExitCode 1
                        }
                        [Console]::Out.WriteLine($existingResultRaw.Trim())
                        exit 1
                    }
                }
            } catch {}
        }
        Start-Sleep -Milliseconds 25
    }

    Write-JsonStdoutAndExit -Object ([ordered]@{
        task_id         = $taskId
        idempotency_key = $idempotencyKey
        request_hash    = $requestHash
        status          = 'running'
        failure_reason  = 'Task is already claimed and running in another worker process.'
        claimed_pid     = $existingClaim.pid
    }) -ExitCode 4
}

# 3. 创建唯一运行目录并解析 codex.exe
$runId = (Get-Date).ToUniversalTime().ToString('yyyyMMddTHHmmssfffZ') + "-pid$PID-" + ([Guid]::NewGuid().ToString('N').Substring(0, 8))
$runDir = Join-Path (Join-Path $runsRootDir $taskId) $runId
New-Item -ItemType Directory -Path $runDir -Force | Out-Null

$promptFilePath = Join-Path $runDir 'prompt.txt'
$summaryFilePath = Join-Path $runDir 'final.md'
$stdoutLogPath = Join-Path $runDir 'stdout.jsonl'
$stderrLogPath = Join-Path $runDir 'stderr.log'
[System.IO.File]::WriteAllText($promptFilePath, $promptText, $utf8NoBom)
[System.IO.File]::WriteAllText($stdoutLogPath, '', $utf8NoBom)
[System.IO.File]::WriteAllText($stderrLogPath, '', $utf8NoBom)

$startedAt = (Get-Date).ToUniversalTime().ToString('o')
$resolvedCodex = $null
try {
    $resolvedCodex = Resolve-VerifiedCodexExecutable -ExplicitCodexExe $CodexExe
} catch {
    $failedInit = [ordered]@{
        schema_version         = 1
        task_id                = $taskId
        idempotency_key        = $idempotencyKey
        request_hash           = $requestHash
        reply_to               = $(if ($null -ne $req.reply_to) { [string]$req.reply_to } else { $null })
        mode                   = $mode
        status                 = 'failed'
        exit_code              = -1
        worker_identity        = 'codex-cli-exec'
        worker_pid             = $null
        worker_thread_id       = $null
        thread_id              = $null
        codex_exe              = $CodexExe
        resolved_codex_exe     = $CodexExe
        codex_version          = $null
        worktree_path          = $targetWorkDir
        sandbox_mode           = $sandboxMode
        owned_files            = $ownedFiles
        started_at             = $startedAt
        completed_at           = (Get-Date).ToUniversalTime().ToString('o')
        prompt_file            = $promptFilePath
        summary_file           = $summaryFilePath
        final_message_file     = $summaryFilePath
        stdout_log             = $stdoutLogPath
        stdout_events_file     = $stdoutLogPath
        stderr_log             = $stderrLogPath
        stderr_file            = $stderrLogPath
        verification_passed    = $null
        verification_exit_code = $null
        failure_reason         = $_.Exception.Message
    }
    Write-FileAtomic -Path $resultPath -Content ($failedInit | ConvertTo-Json -Depth 10)
    Write-JsonStdoutAndExit -Object $failedInit -ExitCode 1
}

# 4. 启动 codex.exe exec Worker，进程启动瞬间立即将 worker_pid 与实时日志路径原子写入 running 结果
$resumeThreadId = if ($null -ne $req.PSObject.Properties['resume_thread_id'] -and -not [string]::IsNullOrWhiteSpace([string]$req.resume_thread_id)) {
    ([string]$req.resume_thread_id).Trim()
} else { '' }
if ($resumeThreadId -ieq '--last') {
    Write-JsonStdoutAndExit -Object ([ordered]@{
        task_id        = $taskId
        status         = 'invalid_request'
        failure_reason = 'resume_thread_id must be an explicit worker_thread_id UUID; --last is forbidden.'
    }) -ExitCode 2
}

$codexArgs = @(
    '-a', 'never',
    'exec',
    '-C', $targetWorkDir,
    '-s', $sandboxMode
)
if (-not [string]::IsNullOrWhiteSpace($resumeThreadId)) {
    $codexArgs += @('resume', $resumeThreadId, '--json', '-o', $summaryFilePath, '-')
} else {
    $codexArgs += @('--json', '-o', $summaryFilePath, '-')
}

$onWorkerStarted = {
    param([int]$workerPid)
    $runningEnvelope = [ordered]@{
        schema_version         = 1
        task_id                = $taskId
        idempotency_key        = $idempotencyKey
        request_hash           = $requestHash
        reply_to               = $(if ($null -ne $req.reply_to) { [string]$req.reply_to } else { $null })
        mode                   = $mode
        status                 = 'running'
        exit_code              = $null
        worker_identity        = 'codex-cli-exec'
        worker_pid             = $workerPid
        worker_thread_id       = $null
        thread_id              = $null
        codex_exe              = $resolvedCodex.Executable
        resolved_codex_exe     = $resolvedCodex.Executable
        codex_version          = $resolvedCodex.Version
        worktree_path          = $targetWorkDir
        sandbox_mode           = $sandboxMode
        owned_files            = $ownedFiles
        started_at             = $startedAt
        completed_at           = $null
        prompt_file            = $promptFilePath
        summary_file           = $summaryFilePath
        final_message_file     = $summaryFilePath
        stdout_log             = $stdoutLogPath
        stdout_events_file     = $stdoutLogPath
        stderr_log             = $stderrLogPath
        stderr_file            = $stderrLogPath
        verification_passed    = $null
        verification_exit_code = $null
        failure_reason         = $null
    }
    Write-FileAtomic -Path $resultPath -Content ($runningEnvelope | ConvertTo-Json -Depth 10)
}

$execRes = Invoke-ProcessCapture `
    -FilePath $resolvedCodex.Executable `
    -ArgumentList $codexArgs `
    -WorkingDirectory $targetWorkDir `
    -StdinText $promptText `
    -StdoutLogPath $stdoutLogPath `
    -StderrLogPath $stderrLogPath `
    -TimeoutMs ($TimeoutSeconds * 1000) `
    -OnStarted $onWorkerStarted

$completedAt = (Get-Date).ToUniversalTime().ToString('o')

# 5. 解析 stdout JSONL 事件流与 final 输出
$workerThreadId = $null
$turnCompleted = $false
$turnFailed = $false

if (-not [string]::IsNullOrWhiteSpace($execRes.StdOut)) {
    $eventLines = $execRes.StdOut -split "`r?`n"
    foreach ($line in $eventLines) {
        $trimmed = $line.Trim()
        if ([string]::IsNullOrWhiteSpace($trimmed) -or (-not $trimmed.StartsWith('{'))) { continue }
        try {
            $evt = $trimmed | ConvertFrom-Json
            if ($evt.type -eq 'thread.started' -and $evt.thread_id) {
                $workerThreadId = [string]$evt.thread_id
            } elseif ($evt.type -eq 'turn.completed') {
                $turnCompleted = $true
            } elseif ($evt.type -eq 'turn.failed' -or $evt.type -eq 'error') {
                $turnFailed = $true
            }
        } catch {}
    }
}

$finalMessageContent = ''
$hasNonEmptyFinal = $false
if (Test-Path -LiteralPath $summaryFilePath -PathType Leaf) {
    $finalMessageContent = [System.IO.File]::ReadAllText($summaryFilePath, [System.Text.Encoding]::UTF8)
    if (-not [string]::IsNullOrWhiteSpace($finalMessageContent)) {
        $hasNonEmptyFinal = $true
    }
}

$status = 'failed'
$failureReason = $null
if ($execRes.TimedOut) {
    $status = 'timed_out'
    $treeNote = if ($execRes.ProcessTreeTerminated) { 'Process tree verified terminated.' } else { 'WARNING: Process tree termination could not be confirmed.' }
    $failureReason = "Worker timed out after $TimeoutSeconds seconds. $treeNote"
} elseif (-not $execRes.ProcessTreeTerminated) {
    $status = 'failed'
    $failureReason = 'Descendant worker processes holding pipes or still running could not be terminated.'
} elseif (-not $execRes.StdinSucceeded) {
    $status = 'failed'
    $failureReason = if (-not [string]::IsNullOrWhiteSpace($execRes.StdinError)) { $execRes.StdinError } else { 'Standard input (stdin) pipe write failed.' }
} elseif (-not $execRes.StreamReadSucceeded) {
    $status = 'failed'
    $failureReason = if (-not [string]::IsNullOrWhiteSpace($execRes.StreamReadError)) { $execRes.StreamReadError } else { 'Standard output/error stream read failed.' }
} elseif ($execRes.ExitCode -ne 0) {
    $status = 'failed'
    $failureReason = "Worker exited with non-zero code: $($execRes.ExitCode)."
} elseif ($turnFailed) {
    $status = 'failed'
    $failureReason = 'JSONL stream reported turn.failed or error event.'
} elseif (-not $turnCompleted) {
    $status = 'failed'
    $failureReason = 'JSONL stream missing required turn.completed event.'
} elseif (-not $hasNonEmptyFinal) {
    $status = 'failed'
    $failureReason = 'Final output file (-o) missing or empty.'
} else {
    $status = 'succeeded'
}

# 6. 若配置了结构化 verification_cmd，直接通过 ProcessStartInfo 执行并纳入完成门禁
$verificationPassed = $null
$verificationExitCode = $null

if ($null -ne $verificationSpec -and $status -eq 'succeeded') {
    try {
        $vRes = Invoke-ProcessCapture `
            -FilePath $verificationSpec.executable `
            -ArgumentList $verificationSpec.args `
            -WorkingDirectory $targetWorkDir `
            -TimeoutMs 120000
        $verificationExitCode = $vRes.ExitCode
        $verificationPassed = (
            (-not $vRes.TimedOut) -and
            $vRes.ProcessTreeTerminated -and
            $vRes.StdinSucceeded -and
            $vRes.StreamReadSucceeded -and
            ($vRes.ExitCode -eq 0)
        )
        if (-not $verificationPassed) {
            $status = 'failed'
            $failureReason = "Verification command failed (ExitCode=$verificationExitCode, TimedOut=$($vRes.TimedOut), ProcessTreeTerminated=$($vRes.ProcessTreeTerminated), StreamOk=$($vRes.StreamReadSucceeded))."
        }
    } catch {
        $verificationExitCode = -1
        $verificationPassed = $false
        $status = 'failed'
        $failureReason = "Verification command execution error: $($_.Exception.Message)"
    }
}

$resultEnvelope = [ordered]@{
    schema_version          = 1
    task_id                 = $taskId
    idempotency_key         = $idempotencyKey
    request_hash            = $requestHash
    reply_to                = $(if ($null -ne $req.reply_to) { [string]$req.reply_to } else { $null })
    mode                    = $mode
    status                  = $status
    exit_code               = $execRes.ExitCode
    worker_identity         = 'codex-cli-exec'
    worker_pid              = $execRes.Pid
    worker_thread_id        = $workerThreadId
    thread_id               = $workerThreadId
    codex_exe               = $resolvedCodex.Executable
    resolved_codex_exe      = $resolvedCodex.Executable
    codex_version           = $resolvedCodex.Version
    worktree_path           = $targetWorkDir
    sandbox_mode            = $sandboxMode
    owned_files             = $ownedFiles
    started_at              = $startedAt
    completed_at            = $completedAt
    prompt_file             = $promptFilePath
    summary_file            = $summaryFilePath
    final_message_file      = $summaryFilePath
    stdout_log              = $stdoutLogPath
    stdout_events_file      = $stdoutLogPath
    stderr_log              = $stderrLogPath
    stderr_file             = $stderrLogPath
    turn_completed          = $turnCompleted
    turn_failed             = $turnFailed
    has_non_empty_final     = $hasNonEmptyFinal
    process_tree_terminated = $execRes.ProcessTreeTerminated
    verification_passed     = $verificationPassed
    verification_exit_code  = $verificationExitCode
    failure_reason          = $failureReason
}

Write-FileAtomic -Path $resultPath -Content ($resultEnvelope | ConvertTo-Json -Depth 10)

if ($status -eq 'succeeded') {
    Write-JsonStdoutAndExit -Object $resultEnvelope -ExitCode 0
} else {
    Write-JsonStdoutAndExit -Object $resultEnvelope -ExitCode 1
}
