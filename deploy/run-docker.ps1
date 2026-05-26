#Requires -Version 5.1
$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

Write-Host "Checking Docker..."
docker ps | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "Docker engine is not ready. Common fixes:" -ForegroundColor Yellow
    Write-Host "  1. Start Docker Desktop and wait until it shows 'Running'"
    Write-Host "  2. Enable virtualization in BIOS"
    Write-Host "  3. Run as Administrator:"
    Write-Host "       wsl --install"
    Write-Host "       dism /online /enable-feature /featurename:VirtualMachinePlatform /all /norestart"
    Write-Host "       dism /online /enable-feature /featurename:Microsoft-Windows-Subsystem-Linux /all /norestart"
    Write-Host "  4. Reboot Windows, then run this script again"
    exit 1
}

Write-Host "Building and starting TgTodo stack..."
docker compose up -d --build

Write-Host ""
Write-Host "Ready: http://localhost:5000" -ForegroundColor Green
Write-Host "RabbitMQ UI: http://localhost:15672 (tgtodo / tgtodo)"
Write-Host "Logs: docker compose logs -f bff"
