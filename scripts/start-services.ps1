# Start infrastructure and all TgTodo services (run from repo root)
$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot\..

Write-Host "Starting Docker (PostgreSQL + RabbitMQ)..."
docker compose -f deploy/docker-compose.yml up -d

Start-Sleep -Seconds 5

$services = @(
    @{ Name = "Identity"; Path = "src/Services/Identity/TgTodo.Identity.Api" },
    @{ Name = "Groups"; Path = "src/Services/Groups/TgTodo.Groups.Api" },
    @{ Name = "Tasks"; Path = "src/Services/Tasks/TgTodo.Tasks.Api" },
    @{ Name = "Gamification"; Path = "src/Services/Gamification/TgTodo.Gamification.Api" },
    @{ Name = "BFF"; Path = "src/Gateway/TgTodo.Bff" }
)

foreach ($s in $services) {
    Write-Host "Starting $($s.Name)..."
    Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$PWD\$($s.Path)'; dotnet run"
    Start-Sleep -Seconds 2
}

if ($env:BOT_TOKEN) {
    Write-Host "Starting Bot..."
    Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$PWD'; `$env:BOT_TOKEN='$env:BOT_TOKEN'; dotnet run --project src/Bot/TgTodo.Bot"
}

Write-Host ""
Write-Host "Mini App: http://localhost:5000"
Write-Host "Dev auth: header X-Dev-Telegram-Id (default 100000001 in MiniApp when not in Telegram)"
Write-Host "Bot: set BOT_TOKEN to enable Telegram bot (inline @bot in chats)"
