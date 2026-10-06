param(
    [string]$RepoRoot = "C:\Users\taise\Documents\GitHub\ChaosDraftParty",
    [string]$Inbox = "work\claude_bridge\inbox.md",
    [string]$Outbox = "work\claude_bridge\outbox_latest.md",
    [string]$ArchiveDir = "work\claude_bridge\archive",
    [int]$PollSeconds = 3
)

$ErrorActionPreference = "Stop"

$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
[Console]::InputEncoding = $utf8NoBom
[Console]::OutputEncoding = $utf8NoBom
$OutputEncoding = $utf8NoBom
$env:PYTHONIOENCODING = "utf-8"
$env:LANG = "ja_JP.UTF-8"

Set-Location -LiteralPath $RepoRoot

$inboxPath = Join-Path $RepoRoot $Inbox
$outboxPath = Join-Path $RepoRoot $Outbox
$archivePath = Join-Path $RepoRoot $ArchiveDir
$pidPath = Join-Path $RepoRoot "work\claude_bridge\bridge.pid"

New-Item -ItemType Directory -Force -Path (Split-Path -Parent $inboxPath) | Out-Null
New-Item -ItemType Directory -Force -Path (Split-Path -Parent $outboxPath) | Out-Null
New-Item -ItemType Directory -Force -Path $archivePath | Out-Null

if (!(Test-Path -LiteralPath $inboxPath)) {
    @(
        "# Claude bridge inbox",
        "",
        "Write the prompt below the separator and save.",
        "The bridge sends it to: claude -c -p",
        "",
        "---",
        ""
    ) -join [Environment]::NewLine | Set-Content -LiteralPath $inboxPath -Encoding UTF8
}

$PID | Set-Content -LiteralPath $pidPath -Encoding ASCII

Write-Host "=== Claude file bridge ===" -ForegroundColor Cyan
Write-Host "Repo: $RepoRoot"
Write-Host "Inbox: $inboxPath"
Write-Host "Outbox: $outboxPath"
Write-Host "Archive: $archivePath"
Write-Host "Poll: ${PollSeconds}s"
Write-Host ""
Write-Host "Usage: write a prompt in inbox.md and save. Press Ctrl+C to stop." -ForegroundColor Yellow
Write-Host ""

$lastSentHash = ""
$busy = $false

function Get-BridgePrompt {
    param([string]$Path)

    $raw = Get-Content -LiteralPath $Path -Raw -Encoding UTF8
    if ([string]::IsNullOrWhiteSpace($raw)) {
        return ""
    }

    $parts = $raw -split "(?m)^---\s*$", 2
    if ($parts.Count -eq 2) {
        return $parts[1].Trim()
    }

    return $raw.Trim()
}

function Get-TextHash {
    param([string]$Text)
    $sha = [System.Security.Cryptography.SHA256]::Create()
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($Text)
    return ([System.BitConverter]::ToString($sha.ComputeHash($bytes)) -replace "-", "")
}

while ($true) {
    try {
        if ($busy) {
            Start-Sleep -Seconds $PollSeconds
            continue
        }

        $prompt = Get-BridgePrompt -Path $inboxPath
        if ([string]::IsNullOrWhiteSpace($prompt)) {
            Start-Sleep -Seconds $PollSeconds
            continue
        }

        $hash = Get-TextHash -Text $prompt
        if ($hash -eq $lastSentHash) {
            Start-Sleep -Seconds $PollSeconds
            continue
        }

        $busy = $true
        $startedAt = Get-Date
        $stamp = $startedAt.ToString('yyyyMMdd_HHmmss')
        $archivePrompt = Join-Path $archivePath "$stamp.prompt.md"
        $archiveOutput = Join-Path $archivePath "$stamp.output.md"

        $prompt | Set-Content -LiteralPath $archivePrompt -Encoding UTF8

        Write-Host "Sending inbox prompt to Claude -c..." -ForegroundColor Green

        $output = & claude -c -p $prompt --dangerously-skip-permissions 2>&1 | Out-String
        $finishedAt = Get-Date

        $body = @(
            "# Claude bridge output",
            "",
            "- Started: $($startedAt.ToString('s'))",
            "- Finished: $($finishedAt.ToString('s'))",
            "- Prompt archive: $archivePrompt",
            "",
            "## Output",
            "",
            '```text',
            $output,
            '```'
        ) -join [Environment]::NewLine

        $body | Set-Content -LiteralPath $outboxPath -Encoding UTF8
        $body | Set-Content -LiteralPath $archiveOutput -Encoding UTF8

        $lastSentHash = $hash
        Write-Host "Claude finished. Outbox updated." -ForegroundColor Green
    }
    catch {
        $failedAt = Get-Date
        $message = $_ | Out-String
        $body = @(
            "# Claude bridge error",
            "",
            "- Time: $($failedAt.ToString('s'))",
            "",
            '```text',
            $message,
            '```'
        ) -join [Environment]::NewLine
        $body | Set-Content -LiteralPath $outboxPath -Encoding UTF8
        Write-Host "Bridge error. See outbox." -ForegroundColor Red
    }
    finally {
        $busy = $false
    }

    Start-Sleep -Seconds $PollSeconds
}
